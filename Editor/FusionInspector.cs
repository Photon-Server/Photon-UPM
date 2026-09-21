namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using Unity.Profiling;
	using UnityEngine;
	using UnityEditor;

	public sealed partial class FusionInspector : EditorWindow
	{
		public const int FILTER_MODES       = 3;
		public const int STATE_SIZE_UNKNOWN = -999;

		public static readonly float   LeftWindowOffset    = 4.0f;
		public static readonly float   RightWindowOffset   = 16.0f;
		public static readonly float   LineHeight          = EditorGUIUtility.singleLineHeight + 1.0f;
		public static readonly float   LinePadding         = 1.0f;
		public static readonly float   HeaderHeight        = LineHeight + 3.0f;
		public static readonly float   FooterHeight        = LineHeight + 3.0f;
		public static readonly Color   HeaderColor         = new Color(0.2f, 0.2f, 0.2f, 1.0f);
		public static readonly Color   HoverColor          = new Color(0.25f, 0.25f, 0.25f, 1.0f);
		public static readonly Color   HighlightColor      = new Color(0.24f, 0.24f, 0.24f, 1.0f);
		public static readonly Color   SelectionColor      = new Color(0.24f, 0.377f, 0.569f, 1.0f);
		public static readonly Color   BackgroundColor     = new Color(0.22f, 0.22f, 0.22f, 1.0f);
		public static readonly Color   NormalTextColor     = new Color(0.92f, 0.92f, 0.92f, 1.0f);
		public static readonly Color   ActiveTextColor     = Color.white;
		public static readonly Color   InactiveTextColor   = Color.grey;
		public static readonly Color   StateAuthorityColor = Color.green;
		public static readonly Color   InputAuthorityColor = Color.yellow;
		public static readonly Color   ProxyColor          = new Color(1.0f, 0.25f, 0.0f, 1.0f);
		public static readonly Color   ExcludedColor       = Color.grey;
		public static readonly Color   TooltipColor        = new Color(0.22f, 0.22f, 0.22f, 1.0f);
		public static readonly float   SeparatorSize       = 1.5f;
		public static readonly Color   SeparatorColor      = new Color(0.175f, 0.175f, 0.175f, 1.0f);
		public static readonly Color[] FilterColors        = new Color[] { Color.white, Color.green, Color.red };
		public static readonly float   FilterFieldWidth    = 200.0f;
		public static readonly float   FilterFieldSpacing  = 24.0f;
		public static readonly float   SelectionSize       = 0.333f;
		public static readonly float   SelectionDelay      = 0.35f;
		public static readonly float   TooltipShowDelay    = 0.4f;
		public static readonly float   TooltipHideDelay    = 0.1f;
		public static readonly float   CategorySpacing     = 20.0f;

		private FusionInspectorDB     _db;
		private ComponentsInspector   _componentsInspector;
		private SceneObjectsInspector _sceneObjectsInspector;
		private PrefabsInspector      _prefabsInspector;
		private RPCsInspector         _rpcsInspector;
		private GUIContent[]          _sections;
		private GUIContent[]          _rpcViewSections;
		private Assembly              _assembly;
		private GUIContent            _versionContent;
		private GUIContent[]          _runnerContents;
		private List<NetworkRunner>   _runners = new List<NetworkRunner>();
		private string                _tooltipText;
		private double                _tooltipTime;
		private double                _tooltipSeenTime;
		private Vector2               _tooltipPosition;
		private bool                  _tooltipVisible;

		private GUIIntStat     _rttStat;
		private GUITrafficStat _trafficInStat;
		private GUITrafficStat _trafficOutStat;
		private GUIPlayersStat _playerCountStat;
		private GUITimeStat    _sessionTimeStat;
		private GUITimeStat    _captureTimeStat;
		private GUIIntStat[]   _gameModeStats;
		private GUIContent     _versionIconContent;
		private GUIContent     _gameModeIconContent;
		private GUIContent     _playerCountIconContent;
		private GUIContent     _playerCountMasterIconContent;
		private GUIContent     _sessionTimeIconContent;
		private GUIContent     _captureTimeIconContent;
		private GUIContent     _rttIconContent;
		private GUIContent     _trafficInIconContent;
		private GUIContent     _trafficOutIconContent;
		private GUIContent     _resetContent;
		private GUIStyle       _tooltipStyle;
		private GUIContent     _tooltipContent;

		private static readonly GUIContent _clearFilterContent = new GUIContent(GUISymbols.CROSS, "Clear filter");

		private static GameObject[]   _pendingSelection;
		private static ProfilerMarker _profilerMarker = new ProfilerMarker($"{nameof(FusionInspector)}.DrawGUI");

		private static readonly List<FusionInspectorDB> _liveDatabases = new List<FusionInspectorDB>();

		[MenuItem("Tools/Fusion/Inspector")]
		public static void Create()
		{
#if FUSION_INSPECTOR_DOCKED
			CreateWindow<FusionInspector>("Fusion Inspector");
#else
			FusionInspector inspector = EditorWindow.GetWindow<FusionInspector>(true, "Fusion Inspector");
			inspector.ShowUtility();
#endif
		}

		public void OnEnable()
		{
			minSize = new Vector2(256.0f, 128.0f);
		}

		public void OnDestroy()
		{
			if (_db != null)
			{
				_liveDatabases.Remove(_db);

				UnityEngine.Object.DestroyImmediate(_db);
				_db = default;
			}
		}

		internal static void ApplyLinePadding(ref Rect rect, float padding = 0.0f)
		{
			if (padding <= 0.0f)
			{
				padding = FusionInspector.LinePadding;
			}

			rect.yMin += padding;
			rect.yMax -= padding;
		}

		internal static void RestoreLinePadding(ref Rect rect, float padding = 0.0f)
		{
			if (padding <= 0.0f)
			{
				padding = FusionInspector.LinePadding;
			}

			rect.yMin -= padding;
			rect.yMax += padding;
		}

		internal static void ApplyFilterRect(ref Rect rect, float labelXMax)
		{
			rect.xMin = Mathf.Min(Mathf.Max(rect.xMax - FilterFieldWidth, labelXMax + FilterFieldSpacing), rect.xMax);

			rect.y += 0.5f;
			if (rect.height > EditorGUIUtility.singleLineHeight)
			{
				rect.y += (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;
				rect.height = EditorGUIUtility.singleLineHeight;
			}
		}

		internal static string DrawFilterField(Rect rect, string value)
		{
			value = EditorGUI.TextField(rect, value, GUIStyles.SearchField);

			if (string.IsNullOrEmpty(value) == false)
			{
				Rect clearRect = rect;
				clearRect.xMax = rect.xMin;
				clearRect.xMin = clearRect.xMax - rect.height;

				GUIColor.Set(Color.red);
				if (GUI.Button(clearRect, _clearFilterContent, GUIStyles.Icon) == true)
				{
					value = default;
					GUI.FocusControl(null);
				}
				GUIColor.Reset();
			}

			return value;
		}

		internal static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			rect.xMin += rect.width;
			rect.width = stat.Width;

			GUISeparators.AddX(rect.xMin);

			GUIColor.Set(FilterColors[(int)filter]);
			if (GUI.Button(rect, stat.Content, stat.Style) == true)
			{
				filter = (EFilterMode)(((int)filter + 1) % FILTER_MODES);
			}
			GUIColor.Reset();
		}

		internal static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref int sortMode, int primarySortMode, int secondarySortMode)
		{
			rect.xMin += rect.width;
			rect.width = stat.Width;

			GUISeparators.AddX(rect.xMin);

			if (GUI.Button(rect, stat.GetContent(sortMode), stat.Style) == true)
			{
				sortMode = sortMode == primarySortMode ? secondarySortMode : primarySortMode;
			}

			if (icon != null)
			{
				ApplyLinePadding(ref rect, 2.0f);

				if (GUI.Button(rect, icon, stat.Style) == true)
				{
					sortMode = sortMode == primarySortMode ? secondarySortMode : primarySortMode;
				}

				RestoreLinePadding(ref rect, 2.0f);
			}
		}

		internal static void DrawIcon(GUIIconStat stat, ref Rect rect, bool isOn)
		{
			rect.xMin += rect.width;
			rect.width = stat.Width;

			if (isOn == true)
			{
				EditorGUI.LabelField(rect, stat.IconContent, GUIStyles.Icon);
			}
		}

		internal static float GetCategoryWidth(string header, params string[] toggles)
		{
			GUIContent content = new GUIContent(header);

			float width = EditorStyles.toggle.CalcSize(content).x - EditorStyles.label.CalcSize(content).x + EditorStyles.boldLabel.CalcSize(content).x;

			foreach (string toggle in toggles)
			{
				content.text = toggle;

				width = Mathf.Max(width, EditorStyles.toggle.CalcSize(content).x);
			}

			return width + CategorySpacing;
		}

		private void OnGUI()
		{
			if (Event.current.type == EventType.Layout)
				return;

			string guiTooltipBackup = GUI.tooltip;

			bool resetGUITooltip = EditorApplication.isPlaying == true && EditorApplication.isPaused == false;
			if (resetGUITooltip)
			{
				GUI.tooltip = string.Empty;
			}

			Initialize();

			_db.Update();

			if (_pendingSelection != null)
			{
				SelectPendingObjects();
			}

			Rect drawRect = new Rect(default, position.size);

			TextureUtility.DrawTexture(drawRect, BackgroundColor);

			float buttonHeight = EditorStyles.toolbarButton.fixedHeight;
			Rect  sectionsRect = new Rect(drawRect.x, drawRect.y, drawRect.width, buttonHeight);

			if (_db.Runners.Count > 1)
			{
				Rect popupRect = sectionsRect;
				popupRect.width = sectionsRect.width / (_sections.Length + 2);
				sectionsRect.xMin += popupRect.width;

				DrawRunnersGUI(popupRect);
			}

			float buttonWidth = sectionsRect.width / (_sections.Length + 1);

			for (int i = 0, count = _sections.Length; i < count; ++i)
			{
				Rect buttonRect = new Rect(sectionsRect.x + buttonWidth * i, sectionsRect.y, buttonWidth, buttonHeight);

				if (i == 3)
				{
					bool isOverviewSelected = _db.CurrentSection == i && _db.RPCs.View == ERPCView.Overview;
					bool isLogSelected      = _db.CurrentSection == i && _db.RPCs.View == ERPCView.Log;

					if (GUI.Toggle(buttonRect, isOverviewSelected, _rpcViewSections[0], EditorStyles.toolbarButton) == true && isOverviewSelected == false)
					{
						_db.SetCurrentSection(i);
						_db.RPCs.View = ERPCView.Overview;
					}

					buttonRect.x += buttonWidth;

					if (GUI.Toggle(buttonRect, isLogSelected, _rpcViewSections[1], EditorStyles.toolbarButton) == true && isLogSelected == false)
					{
						_db.SetCurrentSection(i);
						_db.RPCs.View = ERPCView.Log;
					}
				}
				else
				{
					if (GUI.Toggle(buttonRect, _db.CurrentSection == i, _sections[i], EditorStyles.toolbarButton) == true)
					{
						if (_db.CurrentSection != i)
						{
							_db.SetCurrentSection(i);
						}
					}
				}
			}

			_profilerMarker.Begin();

			try
			{
				drawRect.yMin += buttonHeight;
				drawRect.yMax -= FooterHeight;

				if (_db.CurrentSection == 0)
				{
					if (_sceneObjectsInspector == null)
					{
						_sceneObjectsInspector = new SceneObjectsInspector();
					}

					_sceneObjectsInspector.DrawGUI(position, drawRect, _db);
				}
				else if (_db.CurrentSection == 1)
				{
					if (_prefabsInspector == null)
					{
						_prefabsInspector = new PrefabsInspector();
					}

					_prefabsInspector.DrawGUI(position, drawRect, _db);
				}
				else if (_db.CurrentSection == 2)
				{
					if (_componentsInspector == null)
					{
						_componentsInspector = new ComponentsInspector();
					}

					_componentsInspector.DrawGUI(position, drawRect, _db);
				}
				else if (_db.CurrentSection == 3)
				{
					if (_rpcsInspector == null)
					{
						_rpcsInspector = new RPCsInspector();
					}

					_rpcsInspector.DrawGUI(position, drawRect, _db);
				}

				drawRect.y += drawRect.height;
				drawRect.height = FooterHeight;

				DrawFooterGUI(drawRect, Application.isPlaying == true && _db.CurrentRunner != null && _db.CurrentRunner.IsRunning == true);

				DrawTooltipGUI();
			}
			finally
			{
				if (resetGUITooltip == true)
				{
					GUI.tooltip = guiTooltipBackup;
				}

				_profilerMarker.End();
			}

			Repaint();
		}

		private void DrawRunnersGUI(Rect popupRect)
		{
			int currentRunnerIndex = _db.Runners.IndexOf(_db.CurrentRunner);

			bool rebuildContents = _runnerContents == null || _runners.Count != _db.Runners.Count;
			if (rebuildContents == false)
			{
				for (int i = 0, count = _runners.Count; i < count; ++i)
				{
					if (ReferenceEquals(_runners[i], _db.Runners[i]) == false)
					{
						rebuildContents = true;
						break;
					}
				}
			}

			if (rebuildContents == true)
			{
				_runners.Clear();
				_runners.AddRange(_db.Runners);

				_runnerContents = new GUIContent[_runners.Count];
				for (int i = 0, count = _runners.Count; i < count; ++i)
				{
					_runnerContents[i] = new GUIContent(_runners[i].name);
				}
			}

			int pendingRunnerIndex = EditorGUI.Popup(popupRect, currentRunnerIndex, _runnerContents, EditorStyles.toolbarPopup);
			if (pendingRunnerIndex != currentRunnerIndex)
			{
				_db.PendingRunner = _db.Runners[pendingRunnerIndex];
			}
		}

		private void DrawFooterGUI(Rect rect, bool hasRunningSession)
		{
			const float spacing = 16.0f;

			ApplyLinePadding(ref rect);

			Rect footerRect = rect;
			footerRect.xMin += LeftWindowOffset;
			footerRect.xMax -= 8.0f;

			//----------------------------------------------------------------

			DrawFooterStat(ref footerRect, _versionIconContent, _versionContent, 0.0f, spacing);

			//----------------------------------------------------------------

			if (hasRunningSession == true)
			{
				if (_db.CurrentRunner.GameMode != default)
				{
					try
					{
						GUIIntStat gameModeStat = _gameModeStats[(int)_db.CurrentRunner.GameMode];
						DrawFooterStat(ref footerRect, _gameModeIconContent, gameModeStat.GetContent(_db.CurrentRunner.TickRate), gameModeStat.GetWidth(_db.CurrentRunner.TickRate), spacing);
					}
					catch {}
				}

				//----------------------------------------------------------------

				DrawFooterStat(ref footerRect, _sessionTimeIconContent, _sessionTimeStat.GetContent(_db.SessionTime), _sessionTimeStat.Width, spacing);

				//----------------------------------------------------------------

				bool     isMaster = false;
				GameMode gameMode = _db.CurrentRunner.GameMode;

				if (gameMode == GameMode.Shared)
				{
					if (_db.CurrentRunner.IsSharedModeMasterClient == true)
					{
						isMaster = true;
						_playerCountMasterIconContent.tooltip = "Active Player Count (Local Player ID)\n\nThe local player is the Master Client.";
					}
				}
				else if (gameMode == GameMode.Host)
				{
					isMaster = true;
					_playerCountMasterIconContent.tooltip = "Active Player Count (Local Player ID)\n\nThe local peer is the Host.";
				}
				else if (gameMode == GameMode.Server)
				{
					isMaster = true;
					_playerCountMasterIconContent.tooltip = "Active Player Count\n\nThe local peer is the Server.";
				}

				if (isMaster == true)
				{
					DrawFooterStat(ref footerRect, _playerCountMasterIconContent, Color.yellow, _playerCountStat.GetContent(_db.PlayerCount, _db.LocalPlayer), _playerCountStat.Width, spacing);
				}
				else
				{
					DrawFooterStat(ref footerRect, _playerCountIconContent, _playerCountStat.GetContent(_db.PlayerCount, _db.LocalPlayer), _playerCountStat.Width, spacing);
				}

				//----------------------------------------------------------------

				if (_db.SmoothRTTMs > 0)
				{
					DrawFooterStat(ref footerRect, _rttIconContent, _rttStat.GetContent(_db.SmoothRTTMs), _rttStat.Width, spacing);
				}

				//----------------------------------------------------------------

				if (_db.InBandwidth > 0.0f)
				{
					DrawFooterStat(ref footerRect, _trafficInIconContent, Color.green, _trafficInStat.GetContent(_db.InBandwidth), _trafficInStat.Width, spacing);
				}

				//----------------------------------------------------------------

				if (_db.OutBandwidth > 0.0f)
				{
					DrawFooterStat(ref footerRect, _trafficOutIconContent, Color.red, _trafficOutStat.GetContent(_db.OutBandwidth), _trafficOutStat.Width, spacing);
				}

				//----------------------------------------------------------------

				float footerTop    = rect.yMin - LinePadding;
				float footerHeight = rect.height + LinePadding * 2.0f;

				Rect resetRect = new Rect(rect.xMax - footerHeight, footerTop, footerHeight, footerHeight);

				Rect captureTimeRect = footerRect;
				captureTimeRect.xMin = resetRect.xMin - (captureTimeRect.height - 4.0f) - _captureTimeStat.Width;

				Rect captureBackgroundRect = new Rect(captureTimeRect.xMin - 4.0f, footerTop, rect.xMax - captureTimeRect.xMin + 4.0f, footerHeight);
				TextureUtility.DrawTexture(captureBackgroundRect, BackgroundColor);
				TextureUtility.DrawTexture(new Rect(captureBackgroundRect.xMin, footerTop, SeparatorSize, footerHeight), SeparatorColor);
				TextureUtility.DrawTexture(new Rect(resetRect.xMin, footerTop, SeparatorSize, footerHeight), SeparatorColor);

				DrawFooterStat(ref captureTimeRect, _captureTimeIconContent, _captureTimeStat.GetContent(_db.CaptureTime), _captureTimeStat.Width, spacing);

				if (resetRect.Contains(Event.current.mousePosition) == true)
				{
					Rect resetHoverRect = resetRect;
					resetHoverRect.xMin += SeparatorSize;
					TextureUtility.DrawTexture(resetHoverRect, HoverColor);
				}

				if (GUI.Button(resetRect, _resetContent, GUIStyles.Icon) == true)
				{
					_db.ResetSession(true);
				}
			}

			RestoreLinePadding(ref rect);

			TextureUtility.DrawTexture(new Rect(0.0f, rect.yMin - SeparatorSize * 0.5f, position.width, SeparatorSize), SeparatorColor);
		}

		private static void DrawFooterStat(ref Rect footerRect, GUIContent iconContent, GUIContent statContent, float statWidth, float spacing)
		{
			Rect iconRect = footerRect;
			iconRect.width = iconRect.height;
			GUI.Label(iconRect, iconContent, GUIStyles.Icon);

			footerRect.xMin += iconRect.width - 4.0f;

			Rect statRect = footerRect;
			statRect.width = statWidth > 0.0f ? statWidth : GUIStyles.LeftLabel.CalcSize(statContent).x;
			GUI.Label(statRect, statContent, GUIStyles.LeftLabel);
			footerRect.xMin += statRect.width + spacing;
		}

		private static void DrawFooterStat(ref Rect footerRect, GUIContent iconContent, Color iconColor, GUIContent statContent, float statWidth, float spacing)
		{
			GUIColor.Set(iconColor);
			Rect iconRect = footerRect;
			iconRect.width = iconRect.height;
			GUI.Label(iconRect, iconContent, GUIStyles.Icon);
			GUIColor.Reset();

			footerRect.xMin += iconRect.width - 4.0f;

			Rect statRect = footerRect;
			statRect.width = statWidth > 0.0f ? statWidth : GUIStyles.LeftLabel.CalcSize(statContent).x;
			GUI.Label(statRect, statContent, GUIStyles.LeftLabel);
			footerRect.xMin += statRect.width + spacing;
		}

		private void DrawTooltipGUI()
		{
			if (Event.current.type != EventType.Repaint)
				return;

			if (EditorApplication.isPlaying == false || EditorApplication.isPaused == true)
			{
				_tooltipText    = default;
				_tooltipVisible = false;
				return;
			}

			string  tooltip       = GUI.tooltip;
			double  time          = EditorApplication.timeSinceStartup;
			Vector2 mousePosition = Event.current.mousePosition;

			if (string.IsNullOrEmpty(tooltip) == true)
			{
				if (_tooltipVisible == false || (time - _tooltipSeenTime) > TooltipHideDelay)
				{
					_tooltipText    = default;
					_tooltipVisible = false;
					return;
				}

				tooltip = _tooltipText;
			}
			else
			{
				if (tooltip != _tooltipText)
				{
					_tooltipText     = tooltip;
					_tooltipTime     = time;
					_tooltipSeenTime = time;
					_tooltipPosition = mousePosition;

					if (_tooltipVisible == false)
						return;
				}

				_tooltipSeenTime = time;

				if (_tooltipVisible == false)
				{
					if (Vector2.Distance(mousePosition, _tooltipPosition) > 8.0f)
					{
						_tooltipTime     = time;
						_tooltipPosition = mousePosition;
						return;
					}

					if ((time - _tooltipTime) < TooltipShowDelay)
						return;

					_tooltipVisible = true;
				}
			}

			_tooltipContent.text = tooltip;

			float maxWidth = Mathf.Min(320.0f, position.width - 16.0f);

			_tooltipStyle.wordWrap = false;
			float width = Mathf.Min(Mathf.Ceil(_tooltipStyle.CalcSize(_tooltipContent).x) + 2.0f, maxWidth);
			_tooltipStyle.wordWrap = true;

			float height = Mathf.Ceil(_tooltipStyle.CalcHeight(_tooltipContent, width));

			Rect tooltipRect = new Rect(mousePosition.x + 12.0f, mousePosition.y + 18.0f, width, height);

			if (tooltipRect.xMax > position.size.x - 4.0f)
			{
				tooltipRect.x = position.size.x - tooltipRect.width - 4.0f;
			}
			if (tooltipRect.yMax > position.size.y - 4.0f)
			{
				tooltipRect.y = mousePosition.y - tooltipRect.height - 6.0f;
			}

			tooltipRect.x = Mathf.Max(tooltipRect.x, 4.0f);
			tooltipRect.y = Mathf.Max(tooltipRect.y, 4.0f);

			Rect borderRect = tooltipRect;
			borderRect.xMin -= 1.0f;
			borderRect.xMax += 1.0f;
			borderRect.yMin -= 1.0f;
			borderRect.yMax += 1.0f;

			TextureUtility.DrawTexture(borderRect, SeparatorColor);
			TextureUtility.DrawTexture(tooltipRect, TooltipColor);

			GUI.Label(tooltipRect, _tooltipContent, _tooltipStyle);
		}

		private void SelectPendingObjects()
		{
			GameObject[] pendingSelection = _pendingSelection;
			_pendingSelection = null;

			if (pendingSelection == null || pendingSelection.Length == 0)
				return;

			foreach (PrefabInfo prefab in _db.Prefabs.All)
			{
				prefab.IsSelected = false;
			}

			foreach (SceneObjectInfo sceneObject in _db.SceneObjects.All)
			{
				sceneObject.IsSelected = false;
			}

			bool hasSceneObjects = false;
			bool hasPrefabs      = false;

			foreach (GameObject pendingObject in pendingSelection)
			{
				if (pendingObject == null)
					continue;

				if (EditorUtility.IsPersistent(pendingObject) == true)
				{
					NetworkObject networkObject = pendingObject.GetComponentInChildren<NetworkObject>(true);

					foreach (PrefabInfo prefab in _db.Prefabs.All)
					{
						if (prefab.GameObject == pendingObject || (networkObject != null && prefab.NetworkObject == networkObject))
						{
							prefab.IsSelected = true;
							hasPrefabs = true;
						}
					}
				}
				else
				{
					NetworkObject networkObject = pendingObject.GetComponentInParent<NetworkObject>(true);
					if (networkObject == null)
						continue;

					foreach (SceneObjectInfo sceneObject in _db.SceneObjects.All)
					{
						if (sceneObject.NetworkObject == networkObject)
						{
							sceneObject.IsSelected = true;
							hasSceneObjects = true;
						}
					}
				}
			}

			if (hasSceneObjects == true)
			{
				_db.SetCurrentSection(0);
			}
			else if (hasPrefabs == true)
			{
				_db.SetCurrentSection(1);
			}
		}

		private void Initialize()
		{
			if (_db == null)
			{
				// The database intentionally doesn't survive domain reload. Instances orphaned by
				// a reload or by a closed window are destroyed before a new instance is created,
				// _liveDatabases is cleared on domain reload so all surviving instances are orphans.
				FusionInspectorDB[] databases = Resources.FindObjectsOfTypeAll<FusionInspectorDB>();
				foreach (FusionInspectorDB database in databases)
				{
					if (_liveDatabases.Contains(database) == false)
					{
						UnityEngine.Object.DestroyImmediate(database);
					}
				}

				_db = ScriptableObject.CreateInstance<FusionInspectorDB>();
				_db.hideFlags = HideFlags.HideAndDontSave;

				_liveDatabases.Add(_db);
			}

			if (_sections == null || _sections.Length != 4)
			{
				_sections = new GUIContent[]
				{
					new GUIContent(" Scene Objects", EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D),
					new GUIContent(" Prefabs",       EditorGUIUtility.IconContent("d_Prefab Icon").image as Texture2D),
					new GUIContent(" Components",    EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D),
					new GUIContent(" RPCs",          EditorGUIUtility.IconContent("ParticleSystemForceField Gizmo").image as Texture2D),
				};
			}

			if (_rpcViewSections == null || _rpcViewSections.Length != 2)
			{
				_rpcViewSections = new GUIContent[]
				{
					new GUIContent(" RPCs",       _sections[3].image),
					new GUIContent(" RPCs - Log", EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow@2x").image as Texture2D),
				};
			}

			if (_tooltipStyle == null)
			{
				_tooltipStyle          = new GUIStyle(EditorStyles.label);
				_tooltipStyle.wordWrap = true;
				_tooltipStyle.richText = false;
				_tooltipStyle.padding  = new RectOffset(6, 6, 4, 4);

				_tooltipContent = new GUIContent();
			}

			if (_gameModeStats == null)
			{
				GUIIntStat empty = new GUIIntStat(GUIStyles.LeftLabel, "{0}Hz", 888, "Game Mode - Tick Rate");

				_gameModeStats = new GUIIntStat[] { empty, empty, empty, empty, empty, empty, empty, empty };
				_gameModeStats[(int)GameMode.Single]           = new GUIIntStat(GUIStyles.LeftLabel, $"{nameof(GameMode.Single)} - {{0}}Hz",           888, "Game Mode - Tick Rate");
				_gameModeStats[(int)GameMode.Shared]           = new GUIIntStat(GUIStyles.LeftLabel, $"{nameof(GameMode.Shared)} - {{0}}Hz",           888, "Game Mode - Tick Rate");
				_gameModeStats[(int)GameMode.Server]           = new GUIIntStat(GUIStyles.LeftLabel, $"{nameof(GameMode.Server)} - {{0}}Hz",           888, "Game Mode - Tick Rate");
				_gameModeStats[(int)GameMode.Host]             = new GUIIntStat(GUIStyles.LeftLabel, $"{nameof(GameMode.Host)} - {{0}}Hz",             888, "Game Mode - Tick Rate");
				_gameModeStats[(int)GameMode.Client]           = new GUIIntStat(GUIStyles.LeftLabel, $"{nameof(GameMode.Client)} - {{0}}Hz",           888, "Game Mode - Tick Rate");
				_gameModeStats[(int)GameMode.AutoHostOrClient] = new GUIIntStat(GUIStyles.LeftLabel, $"{nameof(GameMode.AutoHostOrClient)} - {{0}}Hz", 888, "Game Mode - Tick Rate");

				float trafficWidth = GUIStyles.LeftLabel.CalcSize(new GUIContent("888 MB")).x;

				_rttStat         = new GUIIntStat(GUIStyles.LeftLabel, "{0}ms", 888, "Round Trip Time");
				_trafficInStat   = new GUITrafficStat(GUIStyles.LeftLabel, "{0}", 888888888, "Traffic In", trafficWidth);
				_trafficOutStat  = new GUITrafficStat(GUIStyles.LeftLabel, "{0}", 888888888, "Traffic Out", trafficWidth);
				_playerCountStat = new GUIPlayersStat(GUIStyles.LeftLabel, "Active Player Count (Local Player ID)", "Active Player Count");
				_sessionTimeStat = new GUITimeStat(GUIStyles.LeftLabel, "{0}", TimeSpan.FromHours(23), "Fusion Session Play Time");
				_captureTimeStat = new GUITimeStat(GUIStyles.LeftLabel, "{0}", TimeSpan.FromHours(23), "Capture Time");

				_versionIconContent           = new GUIContent(EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow@2x").image, "Fusion SDK Version");
				_gameModeIconContent          = new GUIContent(EditorGUIUtility.IconContent("d_CloudConnect@2x").image,  "Game Mode - Tick Rate");
				_playerCountIconContent       = new GUIContent(EditorGUIUtility.IconContent("d_account@2x").image,       "Active Player Count (Local Player ID)");
				_playerCountMasterIconContent = new GUIContent(_playerCountIconContent.image, "Active Player Count (Local Player ID)\n\nThe local player is the Master Client.");
				_sessionTimeIconContent       = new GUIContent(EditorGUIUtility.IconContent("d_PlayButton@2x").image,    "Fusion Session Play Time");
				_captureTimeIconContent       = new GUIContent(EditorGUIUtility.IconContent("d_Record On@2x").image,     "Capture Time");
				_rttIconContent               = new GUIContent(EditorGUIUtility.IconContent("d_GizmosToggle@2x").image, "Round Trip Time");
				_trafficInIconContent         = new GUIContent("↓", "Traffic In");
				_trafficOutIconContent        = new GUIContent("↑", "Traffic Out");
				_resetContent                 = new GUIContent("↺", "Reset session statistics and recorded RPC events");
			}

			Assembly assembly = typeof(NetworkObject).Assembly;
			if (_assembly != assembly)
			{
				_assembly = assembly;

				string assemblyFileVersion   = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
				string assemblyConfiguration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;

				if (string.IsNullOrEmpty(assemblyFileVersion)   == true) { assemblyFileVersion   = "0.0.0"; }
				if (string.IsNullOrEmpty(assemblyConfiguration) == true) { assemblyConfiguration = "";      }

				assemblyConfiguration = assemblyConfiguration.Replace("Unity ", "");

				_versionContent = new GUIContent($"{assemblyConfiguration} {assemblyFileVersion}", "Fusion SDK Version");
			}
		}

		[MenuItem("GameObject/Select in Fusion Inspector", false)]
		private static void SelectSceneObjects()
		{
			SelectObjects(Selection.gameObjects);
		}

		[MenuItem("GameObject/Select in Fusion Inspector", true)]
		private static bool ValidateSelectSceneObjects()
		{
			foreach (GameObject gameObject in Selection.gameObjects)
			{
				if (EditorUtility.IsPersistent(gameObject) == false && gameObject.GetComponentInParent<NetworkObject>(true) != null)
					return true;
			}

			return false;
		}

		[MenuItem("Assets/Select in Fusion Inspector", false)]
		private static void SelectPrefabs()
		{
			SelectObjects(Selection.gameObjects);
		}

		[MenuItem("Assets/Select in Fusion Inspector", true)]
		private static bool ValidateSelectPrefabs()
		{
			foreach (GameObject gameObject in Selection.gameObjects)
			{
				if (EditorUtility.IsPersistent(gameObject) == true && gameObject.GetComponentInChildren<NetworkObject>(true) != null)
					return true;
			}

			return false;
		}

		private static void SelectObjects(GameObject[] gameObjects)
		{
			if (gameObjects == null || gameObjects.Length == 0)
				return;

			_pendingSelection = gameObjects;

			if (HasOpenInstances<FusionInspector>() == true)
			{
				FocusWindowIfItsOpen(typeof(FusionInspector));
			}
			else
			{
				Create();
			}
		}
	}
}
