namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	internal sealed class RPCsInspector
	{
		private Vector2 _scrollPosition;
		private Vector2 _eventsScrollPosition;
		private bool    _eventsStickToBottom = true;
		private object  _selectionObject;
		private double  _selectionTime;

		private RPCEvent            _selectedEvent;
		private bool                _hasSelectedEvent;
		private List<RPCEvent>      _filteredEvents = new List<RPCEvent>();
		private List<RPCEvent>      _filteredEventsSource;
		private string              _filteredRPCNameFilter;
		private string              _filteredObjectNameFilter;
		private int                 _filteredComponentFilterVersion;
		private PlayerRef           _filteredSourceFilter;
		private PlayerRef           _filteredTargetFilter;
		private PlayerRef           _filteredStateAuthorityFilter;
		private PlayerRef           _filteredInputAuthorityFilter;
		private PlayerRef           _filteredAnyPlayerFilter;
		private NetworkObject       _filteredObjectFilter;
		private ERPCEventField      _filteredVisibleFields;
		private int                 _filteredRPCEventCount;
		private List<ComponentInfo> _tempComponents = new List<ComponentInfo>();

		private HashSet<ComponentInfo> _eventTypes = new HashSet<ComponentInfo>();
		private List<RPCEvent>         _eventTypesSource;
		private int                    _eventTypesCount;
		private float                  _eventTypeWidth;

		private IntLabel _totalSent     = new IntLabel();
		private IntLabel _totalReceived = new IntLabel();

		private GUINameStat _methodStat;

		private GUIIconStat _isReliableStat;
		private GUIIconStat _isTickAlignedStat;
		private GUIIconStat _hasRpcTargetStat;
		private GUIIconStat _invokeLocalStat;

		private GUIStat     _sourcesStat;
		private GUIStat     _targetsStat;

		private GUISortStat _sentStat;
		private GUISortStat _receivedStat;

		private GUIContent  _eventMethodContent;
		private GUIContent  _eventSentContent;
		private GUIContent  _eventReceivedContent;
		private GUIStat     _eventTypeStat;
		private GUINameStat _eventRpcStat;
		private GUIContent  _eventRpcContent;
		private GUIStat     _eventObjectStat;
		private GUIContent  _objectContent;
		private GUIStat     _eventFrameStat;
		private GUIStat     _eventTickStat;
		private GUIIconStat _eventResultStat;
		private GUIContent  _eventResultContent;
		private GUIStat     _eventSourceStat;
		private GUIStat     _eventTargetStat;
		private GUIStat     _eventStateAuthorityStat;
		private GUIStat     _eventInputAuthorityStat;
		private GUIContent  _localStateAuthorityContent;
		private GUIContent  _localInputAuthorityContent;

		private GUIContent   _scriptContent;
		private GUIContent   _methodContent;
		private GUIContent   _totalContent;
		private GUIContent   _fieldsContent;
		private GUIContent   _filterContent;
		private GUIContent   _toggleContent;
		private GUIContent[] _sourceFlagContents;
		private GUIContent[] _sourceExcludedFlagContents;
		private GUIContent[] _targetFlagContents;
		private GUIContent[] _targetExcludedFlagContents;

		private float _sortWidth;
		private float _fieldsWidth1;
		private float _fieldsWidth2;
		private float _eventTypeMinWidth;
		private float _eventTypeIconWidth;
		private float _eventsFieldsWidth;
		private float _eventIconLabelOffset;
		private float _eventMethodLabelOffset;
		private float _localDotWidth;

		private GUIContent _eventSourceFilterContent;
		private GUIContent _eventTargetFilterContent;
		private GUIContent _eventStateAuthorityFilterContent;
		private GUIContent _eventInputAuthorityFilterContent;
		private GUIContent _eventObjectFilterContent;
		private GUIContent _eventAnyPlayerFilterContent;
		private GUIContent _removeFilterContent;
		private PlayerRef  _eventSourceFilterPlayer;
		private PlayerRef  _eventTargetFilterPlayer;
		private PlayerRef  _eventStateAuthorityFilterPlayer;
		private PlayerRef  _eventInputAuthorityFilterPlayer;
		private PlayerRef  _eventAnyPlayerFilterPlayer;
		private string     _eventObjectFilterName;

		private static float _flagSlotWidth;

		private static Dictionary<int, string> _tickLabels     = new Dictionary<int, string>();
		private static Dictionary<int, string> _playerLabels   = new Dictionary<int, string>();
		private static Dictionary<int, string> _resultTooltips = new Dictionary<int, string>();

		public RPCsInspector()
		{
			Texture2D scriptIcon      = EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D;
			Texture2D rpcIcon         = EditorGUIUtility.IconContent("d_EventTrigger Icon").image as Texture2D;
			Texture2D fieldsIcon      = EditorGUIUtility.IconContent("d__Menu@2x").image as Texture2D;
			Texture2D filterIcon      = EditorGUIUtility.IconContent("d_FilterByType@2x").image as Texture2D;
			Texture2D gameObjectIcon  = EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D;
			Texture2D reliableIcon    = EditorGUIUtility.IconContent("Git@2x").image as Texture2D;
			Texture2D tickAlignedIcon = EditorGUIUtility.IconContent("d_SnapIncrement@2x").image as Texture2D;
			Texture2D rpcTargetIcon   = EditorGUIUtility.IconContent("d_OrientationGizmo@2x").image as Texture2D;
			Texture2D invokeLocalIcon = EditorGUIUtility.IconContent("d_pick@2x").image as Texture2D;
			Texture2D resultIcon      = EditorGUIUtility.IconContent("d_Progress@2x").image as Texture2D;

			float headerHeight = FusionInspector.HeaderHeight;

			_methodStat = new GUINameStat(GUIStyles.LeftBoldLabel, "RPC", "RPC method name", (int)ERPCSortMode.NameHierarchyAscending, (int)ERPCSortMode.NameHierarchyDescending);

			_isReliableStat    = new GUIIconStat(GUIStyles.Icon, reliableIcon,    "✔", $"Is Reliable ({nameof(RpcChannel)}.{nameof(RpcChannel.Reliable)})", headerHeight);
			_isTickAlignedStat = new GUIIconStat(GUIStyles.Icon, tickAlignedIcon, "✔", $"Is Tick Aligned",                                                  headerHeight);
			_hasRpcTargetStat  = new GUIIconStat(GUIStyles.Icon, rpcTargetIcon,   "✔", $"Has Player Target\n\nThe RPC is sent to a specific player via a [{nameof(RpcTargetAttribute)}] parameter.", headerHeight);
			_invokeLocalStat   = new GUIIconStat(GUIStyles.Icon, invokeLocalIcon, "✔", $"Invokes Locally on the Sender",                                  headerHeight);

			_sourcesStat = new GUIStat(GUIStyles.RightBoldLabel, "Sources", $"Allowed {nameof(RpcSources)} that may invoke this RPC\n\nDots from left to right:\n{nameof(RpcSources.StateAuthority)}\n{nameof(RpcSources.InputAuthority)}\n{nameof(RpcSources.Proxies)}");
			_targetsStat = new GUIStat(GUIStyles.RightBoldLabel, "Targets", $"{nameof(RpcTargets)} that will execute this RPC\n\nDots from left to right:\n{nameof(RpcTargets.StateAuthority)}\n{nameof(RpcTargets.InputAuthority)}\n{nameof(RpcTargets.Proxies)}");

			_sentStat     = new GUISortStat(GUIStyles.RightBoldLabel, "Sent",     "# of times the RPC was sent from the local peer",   (int)ERPCSortMode.SentAscending,     (int)ERPCSortMode.SentDescending);
			_receivedStat = new GUISortStat(GUIStyles.RightBoldLabel, "Received", "# of times the RPC was received by the local peer", (int)ERPCSortMode.ReceivedAscending, (int)ERPCSortMode.ReceivedDescending);

			_scriptContent = new GUIContent(scriptIcon);
			_methodContent = new GUIContent(rpcIcon);
			_totalContent  = new GUIContent("Total", "Totals for all listed RPCs");

			_eventMethodContent   = new GUIContent();
			_eventSentContent     = new GUIContent("↑", "Sent from the local peer");
			_eventReceivedContent = new GUIContent("↓", "Received from a remote peer");

			_fieldsContent = new GUIContent(fieldsIcon, "Visible fields");
			_filterContent = new GUIContent(filterIcon, "Filter by component type");
			_toggleContent = new GUIContent();

			_sourceFlagContents = new GUIContent[]
			{
				new GUIContent(GUISymbols.DOT, $"The RPC can be invoked by the {nameof(RpcSources.StateAuthority)}"),
				new GUIContent(GUISymbols.DOT, $"The RPC can be invoked by the {nameof(RpcSources.InputAuthority)}"),
				new GUIContent(GUISymbols.DOT, $"The RPC can be invoked by {nameof(RpcSources.Proxies)}"),
			};

			_sourceExcludedFlagContents = new GUIContent[]
			{
				new GUIContent(GUISymbols.DOT, $"The RPC cannot be invoked by the {nameof(RpcSources.StateAuthority)}"),
				new GUIContent(GUISymbols.DOT, $"The RPC cannot be invoked by the {nameof(RpcSources.InputAuthority)}"),
				new GUIContent(GUISymbols.DOT, $"The RPC cannot be invoked by {nameof(RpcSources.Proxies)}"),
			};

			_targetFlagContents = new GUIContent[]
			{
				new GUIContent(GUISymbols.DOT, $"The RPC is executed on the {nameof(RpcTargets.StateAuthority)}"),
				new GUIContent(GUISymbols.DOT, $"The RPC is executed on the {nameof(RpcTargets.InputAuthority)}"),
				new GUIContent(GUISymbols.DOT, $"The RPC is executed on {nameof(RpcTargets.Proxies)}"),
			};

			_targetExcludedFlagContents = new GUIContent[]
			{
				new GUIContent(GUISymbols.DOT, $"The RPC is not executed on the {nameof(RpcTargets.StateAuthority)}"),
				new GUIContent(GUISymbols.DOT, $"The RPC is not executed on the {nameof(RpcTargets.InputAuthority)}"),
				new GUIContent(GUISymbols.DOT, $"The RPC is not executed on {nameof(RpcTargets.Proxies)}"),
			};

			_sortWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"{GUISymbols.ARROW_UP} ")).x - GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"")).x;

			_flagSlotWidth = GUIStyles.Icon.CalcSize(new GUIContent("S")).x + 4.0f;

			float flagsWidth   = _flagSlotWidth * 3.0f + 4.0f;
			float sourcesWidth = Mathf.Max(GUIStyles.RightBoldLabel.CalcSize(new GUIContent(" Sources ")).x, flagsWidth);
			float targetsWidth = Mathf.Max(GUIStyles.RightBoldLabel.CalcSize(new GUIContent(" Targets ")).x, flagsWidth);

			_sourcesStat.Width = sourcesWidth;
			_targetsStat.Width = targetsWidth;

			_sentStat.Width     += _sortWidth;
			_receivedStat.Width += _sortWidth;

			_eventTypeStat   = new GUIStat(GUIStyles.LeftBoldLabel, "Type", "Component the RPC is declared in");
			_eventRpcStat    = new GUINameStat(GUIStyles.LeftBoldLabel, "RPC", "RPC method name", -1, -1);
			_eventRpcContent = new GUIContent("RPC", "RPC method name");
			_eventObjectStat = new GUIStat(GUIStyles.LeftBoldLabel, "Object", "Game object the RPC was invoked on (instance RPCs only)");
			_objectContent   = new GUIContent(gameObjectIcon);
			_eventFrameStat  = new GUIStat(GUIStyles.RightBoldLabel, "Frame", "Frame in which the RPC was recorded (Time.frameCount)");
			_eventTickStat   = new GUIStat(GUIStyles.RightBoldLabel, "Tick", "Tick in which the RPC was sent or executed");
			_eventSourceStat = new GUIStat(GUIStyles.RightBoldLabel, "Source", "Player that invoked the RPC");
			_eventTargetStat = new GUIStat(GUIStyles.RightBoldLabel, "Target", $"Player target of the RPC ([{nameof(RpcTargetAttribute)}] parameter), or the declared {nameof(RpcTargets)}");

			_eventStateAuthorityStat = new GUIStat(GUIStyles.RightBoldLabel, "S-Authority", "State authority of the object at the time of the event (PlayerRef)");
			_eventInputAuthorityStat = new GUIStat(GUIStyles.RightBoldLabel, "I-Authority", "Input authority of the object at the time of the event (PlayerRef)");
			_eventResultStat         = new GUIIconStat(GUIStyles.Icon, resultIcon, "✔", "RPC send / execute result", headerHeight);
			_eventResultContent      = new GUIContent();

			_localStateAuthorityContent = new GUIContent("•", "Local player has state authority over this object");
			_localInputAuthorityContent = new GUIContent("•", "Local player has input authority over this object");

			_localDotWidth = GUIStyles.Dot.CalcSize(_localStateAuthorityContent).x;

			_eventSourceFilterContent         = new GUIContent();
			_eventTargetFilterContent         = new GUIContent();
			_eventStateAuthorityFilterContent = new GUIContent();
			_eventInputAuthorityFilterContent = new GUIContent();
			_eventObjectFilterContent         = new GUIContent();
			_eventAnyPlayerFilterContent      = new GUIContent();
			_removeFilterContent              = new GUIContent(GUISymbols.CROSS, "Remove filter");

			float eventRowHeight = FusionInspector.LineHeight - FusionInspector.LinePadding * 2.0f;
			_eventIconLabelOffset   = GUIStyles.ObjectField.padding.left + (eventRowHeight - GUIStyles.ObjectField.padding.vertical) - GUIStyles.LeftBoldLabel.padding.left;
			_eventMethodLabelOffset = FusionInspector.LineHeight - 2.0f + GUIStyles.ObjectField.padding.left - GUIStyles.LeftBoldLabel.padding.left;
			_eventTypeIconWidth     = eventRowHeight - GUIStyles.ObjectField.padding.vertical;

			_eventTypeMinWidth = _eventIconLabelOffset + GUIStyles.LeftBoldLabel.CalcSize(new GUIContent(" Type ")).x + FusionInspector.HeaderHeight;
			float playerLabelWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent(" Player 8888 ")).x;

			_eventFrameStat.Width  = GUIStyles.RightBoldLabel.CalcSize(new GUIContent(" 8888888 ")).x;
			_eventTickStat.Width   = GUIStyles.RightBoldLabel.CalcSize(new GUIContent(" 8888888 ")).x;
			_eventSourceStat.Width = Mathf.Max(GUIStyles.RightBoldLabel.CalcSize(new GUIContent(" Source ")).x, playerLabelWidth);
			_eventTargetStat.Width = Mathf.Max(GUIStyles.RightBoldLabel.CalcSize(new GUIContent(" Target ")).x, playerLabelWidth, _flagSlotWidth * 3.0f + 4.0f);

			_eventStateAuthorityStat.Width = Mathf.Max(GUIStyles.RightBoldLabel.CalcSize(new GUIContent(" S-Authority ")).x, playerLabelWidth);
			_eventInputAuthorityStat.Width = Mathf.Max(GUIStyles.RightBoldLabel.CalcSize(new GUIContent(" I-Authority ")).x, playerLabelWidth);

			_fieldsWidth1 = FusionInspector.GetCategoryWidth("Configuration", "Sources", "Targets", "Is Reliable", "Is Tick Aligned", "Has Player Target", "Invoke Local");
			_fieldsWidth2 = FusionInspector.GetCategoryWidth("Runtime", "Sent", "Received");

			_eventsFieldsWidth = FusionInspector.GetCategoryWidth("General", "Object", "Frame", "Tick", "Source", "Target", "State Authority", "Input Authority", "Result");
		}

		public void DrawGUI(Rect window, Rect region, FusionInspectorDB db)
		{
			GameMode  gameMode         = db.CurrentRunner != null ? db.CurrentRunner.GameMode : default;
			ERPCField visibleFields    = db.RPCs.OverviewVisibleFields | ERPCField.Name;
			ERPCField toggleableFields = ~ERPCField.Name;

			db.RPCs.OverviewVisibleFields = visibleFields;

			bool showRuntimeInfo = gameMode != default;

			if (db.RPCs.View == ERPCView.Log)
			{
				DrawEventsGUI(window, region, db);
				return;
			}

			if (showRuntimeInfo == false)
			{
				visibleFields &= ~ERPCField.Sent;
				visibleFields &= ~ERPCField.Received;

				toggleableFields &= ~ERPCField.Sent;
				toggleableFields &= ~ERPCField.Received;
			}

			DrawRPCsGUI(window, region, db, visibleFields, toggleableFields);
		}

		private void DrawRPCsGUI(Rect window, Rect region, FusionInspectorDB db, ERPCField visibleFields, ERPCField toggleableFields)
		{
			RPCs rpcs = db.RPCs;

			bool showSources       = visibleFields.Has(ERPCField.Sources);
			bool showTargets       = visibleFields.Has(ERPCField.Targets);
			bool showIsReliable    = visibleFields.Has(ERPCField.IsReliable);
			bool showIsTickAligned = visibleFields.Has(ERPCField.IsTickAligned);
			bool showHasRpcTarget  = visibleFields.Has(ERPCField.HasRpcTarget);
			bool showInvokeLocal   = visibleFields.Has(ERPCField.InvokeLocal);

			bool showSent     = visibleFields.Has(ERPCField.Sent);
			bool showReceived = visibleFields.Has(ERPCField.Received);
			bool showTotal    = showSent == true || showReceived == true;

			GUISeparators.Clear();

			region.xMin += FusionInspector.LeftWindowOffset;
			region.xMax -= FusionInspector.RightWindowOffset;

			_methodStat.Width = region.width;

			if (showSources       == true) { _methodStat.Width -= _sourcesStat.Width;       }
			if (showTargets       == true) { _methodStat.Width -= _targetsStat.Width;       }
			if (showIsReliable    == true) { _methodStat.Width -= _isReliableStat.Width;    } else { rpcs.IsReliableFilter    = default; }
			if (showIsTickAligned == true) { _methodStat.Width -= _isTickAlignedStat.Width; } else { rpcs.IsTickAlignedFilter = default; }
			if (showHasRpcTarget  == true) { _methodStat.Width -= _hasRpcTargetStat.Width;  } else { rpcs.HasRpcTargetFilter  = default; }
			if (showInvokeLocal   == true) { _methodStat.Width -= _invokeLocalStat.Width;   } else { rpcs.InvokeLocalFilter   = default; }

			if (showSent     == true) { _methodStat.Width -= _sentStat.Width;     }
			if (showReceived == true) { _methodStat.Width -= _receivedStat.Width; }

			Rect headerRect = region;
			headerRect.width  = _methodStat.Width;
			headerRect.height = FusionInspector.HeaderHeight;

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, window.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			GUIContent methodContent = _methodStat.GetContent(rpcs.All.Count, rpcs.Filtered.Count, default, (int)rpcs.SortMode);

			Rect nameRect = headerRect;
			nameRect.xMin += _methodStat.IsSorted((int)rpcs.SortMode) ? 0.0f : _sortWidth;

			Rect nameFilterRect = headerRect;
			nameFilterRect.xMax -= 4.0f;
			FusionInspector.ApplyFilterRect(ref nameFilterRect, nameRect.xMin + _methodStat.Style.CalcSize(methodContent).x);
			rpcs.RPCNameFilter = FusionInspector.DrawFilterField(nameFilterRect, rpcs.RPCNameFilter);

			nameRect.xMax = nameFilterRect.xMin;
			if (GUI.Button(nameRect, methodContent, _methodStat.Style) == true)
			{
				ToggleSortMode(ref rpcs.SortMode, ERPCSortMode.NameHierarchyAscending, ERPCSortMode.NameHierarchyDescending);
			}

			if (showSources       == true) { DrawLabelHeader(_sourcesStat,  ref headerRect); }
			if (showTargets       == true) { DrawLabelHeader(_targetsStat,  ref headerRect); }
			if (showIsReliable    == true) { DrawHeader(_isReliableStat,    ref headerRect, ref rpcs.IsReliableFilter);    }
			if (showIsTickAligned == true) { DrawHeader(_isTickAlignedStat, ref headerRect, ref rpcs.IsTickAlignedFilter); }
			if (showHasRpcTarget  == true) { DrawHeader(_hasRpcTargetStat,  ref headerRect, ref rpcs.HasRpcTargetFilter);  }
			if (showInvokeLocal   == true) { DrawHeader(_invokeLocalStat,   ref headerRect, ref rpcs.InvokeLocalFilter);   }

			if (showSent     == true) { DrawHeader(_sentStat,     null, ref headerRect, ref rpcs.SortMode, ERPCSortMode.SentDescending,     ERPCSortMode.SentAscending);     }
			if (showReceived == true) { DrawHeader(_receivedStat, null, ref headerRect, ref rpcs.SortMode, ERPCSortMode.ReceivedDescending, ERPCSortMode.ReceivedAscending); }

			headerRect.xMin += headerRect.width;
			headerRect.width = FusionInspector.RightWindowOffset;
			if (GUI.Button(headerRect, _fieldsContent, GUIStyles.Icon) == true)
			{
				rpcs.OverviewVisibleFieldsFoldout = !rpcs.OverviewVisibleFieldsFoldout;
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			region.yMin += headerRect.height;

			DrawFieldsGUI(ref region, rpcs, visibleFields, toggleableFields);

			GUISeparators.AddY(region.y);

			bool canShowHierarchy = rpcs.SortMode == ERPCSortMode.NameHierarchyAscending || rpcs.SortMode == ERPCSortMode.NameHierarchyDescending;

			int classHeaderCount = 0;
			if (canShowHierarchy == true)
			{
				ComponentInfo previousComponent = null;
				for (int i = 0, count = rpcs.Filtered.Count; i < count; ++i)
				{
					ComponentInfo currentComponent = rpcs.Filtered[i].Component;
					if (currentComponent != previousComponent)
					{
						++classHeaderCount;
						previousComponent = currentComponent;
					}
				}
			}

			if (showTotal == true)
			{
				region.yMax -= FusionInspector.LineHeight + 2.0f;
			}

			Rect scrollRect     = new Rect(region.x, region.y, region.width + FusionInspector.RightWindowOffset, region.height);
			Rect scrollViewRect = new Rect(0.0f, 0.0f, region.width, FusionInspector.LineHeight * (rpcs.Filtered.Count + classHeaderCount));

			GUISeparators.AddX(headerRect.xMin);

			_scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, scrollViewRect);

			float         indent        = 16.0f;
			float         minScrollY    = _scrollPosition.y;
			float         maxScrollY    = _scrollPosition.y + region.height;
			Rect          drawRect      = new Rect(0.0f, 0.0f, 0.0f, FusionInspector.LineHeight);
			Vector2       mousePosition = Event.current.mousePosition;
			ComponentInfo lastComponent = null;

			for (int i = 0, count = rpcs.Filtered.Count; i < count; ++i)
			{
				RPCInfo rpc = rpcs.Filtered[i];

				if (canShowHierarchy == true && rpc.Component != lastComponent)
				{
					lastComponent = rpc.Component;

					if (drawRect.y > maxScrollY)
						break;

					if (drawRect.yMax >= minScrollY)
					{
						Rect classRect = drawRect;
						classRect.xMin  = 0.0f;
						classRect.width = _methodStat.Width;

						if (mousePosition.x >= 0.0f && mousePosition.x <= region.width && mousePosition.y >= minScrollY && mousePosition.y <= maxScrollY && mousePosition.y > classRect.yMin && mousePosition.y < classRect.yMax)
						{
							Rect hoverRect = classRect;
							hoverRect.xMin = 0.0f;
							hoverRect.xMax = window.width;
							hoverRect.yMin = Mathf.Max(minScrollY, hoverRect.yMin + 0.25f);
							hoverRect.yMax = Mathf.Min(hoverRect.yMax, maxScrollY);
							TextureUtility.DrawTexture(hoverRect, FusionInspector.HoverColor);

							hoverRect.width = scrollRect.x;
							hoverRect.y += scrollRect.y - _scrollPosition.y;
							GUIHighlights.Add(hoverRect, FusionInspector.HoverColor);
						}

						FusionInspector.ApplyLinePadding(ref classRect);

						_scriptContent.text = rpc.Component.TypeName;
						classRect.height += 1.0f; // GUI.Button dead zone fix
						if (GUI.Button(classRect, _scriptContent, GUIStyles.ObjectField) == true && rpc.Component.Script != null)
						{
							if (Event.current.button == 1)
							{
								string assetPath = AssetDatabase.GetAssetPath(rpc.Component.Script);

								GenericMenu menu = new GenericMenu();
								menu.AddItem(new GUIContent("Copy Name"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, rpc.Component.TypeName);
								menu.AddItem(new GUIContent("Copy Path"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, assetPath);
								menu.AddItem(new GUIContent("Copy Full Path"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, string.IsNullOrEmpty(assetPath) == false ? System.IO.Path.GetFullPath(assetPath) : "");
								menu.AddSeparator("");
								menu.AddItem(new GUIContent(Application.platform == RuntimePlatform.OSXEditor ? "Reveal in Finder" : "Show in Explorer"), false, (data) => { EditorUtility.RevealInFinder((string)data); }, assetPath);
								menu.ShowAsContext();
							}
							else if (Event.current.button == 0)
							{
								if (_selectionObject == rpc.Component && EditorApplication.timeSinceStartup < (_selectionTime + FusionInspector.SelectionDelay))
								{
									AssetDatabase.OpenAsset(rpc.Component.Script);
									_selectionObject = default;
									_selectionTime   = default;
								}
								else
								{
									EditorGUIUtility.PingObject(rpc.Component.Script);
									_selectionObject = rpc.Component;
									_selectionTime   = EditorApplication.timeSinceStartup;
								}
							}
						}
						classRect.height -= 1.0f; // GUI.Button dead zone fix
					}

					drawRect.y += drawRect.height;
				}

				if (drawRect.y > maxScrollY)
					break;

				if (drawRect.yMax < minScrollY)
				{
					drawRect.y += drawRect.height;
					continue;
				}

				drawRect.xMin  = 0.0f;
				drawRect.width = _methodStat.Width;

				if (mousePosition.x >= 0.0f && mousePosition.x <= region.width && mousePosition.y >= minScrollY && mousePosition.y <= maxScrollY && mousePosition.y > drawRect.yMin && mousePosition.y < drawRect.yMax)
				{
					Rect hoverRect = drawRect;
					hoverRect.xMin = 0.0f;
					hoverRect.xMax = window.width;
					hoverRect.yMin = Mathf.Max(minScrollY, hoverRect.yMin + 0.25f);
					hoverRect.yMax = Mathf.Min(hoverRect.yMax, maxScrollY);
					TextureUtility.DrawTexture(hoverRect, FusionInspector.HoverColor);

					hoverRect.width = scrollRect.x;
					hoverRect.y += scrollRect.y - _scrollPosition.y;
					GUIHighlights.Add(hoverRect, FusionInspector.HoverColor);
				}

				FusionInspector.ApplyLinePadding(ref drawRect);

				if (canShowHierarchy == true)
				{
					drawRect.xMin += indent;
					drawRect.xMin += GUIStyles.ObjectField.padding.left;
					EditorGUI.LabelField(new Rect(drawRect.x - indent, drawRect.y, indent, drawRect.height), GUISymbols.DERIVE, GUIStyles.Icon);
					drawRect.xMin -= GUIStyles.ObjectField.padding.left;
				}

				_methodContent.text    = canShowHierarchy == true ? rpc.MethodName : rpc.MethodHierarchy;
				_methodContent.tooltip = rpc.MethodDeclaration;
				drawRect.height += 1.0f; // GUI.Button dead zone fix
				if (GUI.Button(drawRect, _methodContent, GUIStyles.ObjectField) == true && rpc.Component.Script != null)
				{
					if (Event.current.button == 1)
					{
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Copy Name"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, rpc.MethodName);
						menu.AddItem(new GUIContent("Copy Path"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, rpc.MethodHierarchy);
						menu.ShowAsContext();
					}
					else
					{
						if (_selectionObject == rpc && EditorApplication.timeSinceStartup < (_selectionTime + FusionInspector.SelectionDelay))
						{
							int methodLineNumber = ScriptUtility.GetLineNumber(rpc.Method);
							AssetDatabase.OpenAsset(rpc.Component.Script, methodLineNumber);
							_selectionObject = default;
							_selectionTime   = default;
						}
						else
						{
							_selectionObject = rpc;
							_selectionTime   = EditorApplication.timeSinceStartup;
						}
					}
				}
				drawRect.height -= 1.0f; // GUI.Button dead zone fix

				if (canShowHierarchy == true)
				{
					drawRect.xMin -= indent;
				}

				if (showSources == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _sourcesStat.Width;

					DrawFlags(drawRect, _sourceFlagContents, _sourceExcludedFlagContents, (rpc.Sources & RpcSources.StateAuthority) == RpcSources.StateAuthority, (rpc.Sources & RpcSources.InputAuthority) == RpcSources.InputAuthority, (rpc.Sources & RpcSources.Proxies) == RpcSources.Proxies);
				}

				if (showTargets == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _targetsStat.Width;

					DrawFlags(drawRect, _targetFlagContents, _targetExcludedFlagContents, (rpc.Targets & RpcTargets.StateAuthority) == RpcTargets.StateAuthority, (rpc.Targets & RpcTargets.InputAuthority) == RpcTargets.InputAuthority, (rpc.Targets & RpcTargets.Proxies) == RpcTargets.Proxies);
				}

				if (showIsReliable    == true) { FusionInspector.DrawIcon(_isReliableStat,    ref drawRect, rpc.IsReliable);    }
				if (showIsTickAligned == true) { FusionInspector.DrawIcon(_isTickAlignedStat, ref drawRect, rpc.IsTickAligned); }
				if (showHasRpcTarget  == true) { FusionInspector.DrawIcon(_hasRpcTargetStat,  ref drawRect, rpc.HasRpcTarget);  }
				if (showInvokeLocal   == true) { FusionInspector.DrawIcon(_invokeLocalStat,   ref drawRect, rpc.InvokeLocal);   }

				if (showSent == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _sentStat.Width;

					if (rpc.Sent.Value > 0)
					{
						EditorGUI.LabelField(drawRect, rpc.Sent.GetLabel(), GUIStyles.RightLabel);
					}
				}

				if (showReceived == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _receivedStat.Width;

					if (rpc.Received.Value > 0)
					{
						EditorGUI.LabelField(drawRect, rpc.Received.GetLabel(), GUIStyles.RightLabel);
					}
				}

				FusionInspector.RestoreLinePadding(ref drawRect);

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			GUIHighlights.DrawAll();

			float scrollHeight = Mathf.Min(scrollRect.height, scrollViewRect.height);

			if (showTotal == true)
			{
				GUISeparators.AddY(scrollRect.yMin + scrollHeight + 2.0f);
				GUISeparators.AddY(scrollRect.yMin + scrollHeight + 2.0f + FusionInspector.LineHeight);

				DrawTotalGUI(window, new Rect(region.x, scrollRect.yMin + scrollHeight + 2.0f, region.width, FusionInspector.LineHeight), rpcs, showSources, showTargets, showIsReliable, showIsTickAligned, showHasRpcTarget, showInvokeLocal, showSent, showReceived);

				GUISeparators.DrawAll(new Rect(0.0f, 0.0f, window.width, window.height), headerRect.yMin, headerRect.height, scrollRect.yMin, scrollHeight + 2.0f);
			}
			else
			{
				GUISeparators.AddY(scrollRect.yMin + scrollHeight);

				GUISeparators.DrawAll(new Rect(0.0f, 0.0f, window.width, window.height), headerRect.yMin, headerRect.height, scrollRect.yMin, scrollHeight);
			}
		}

		private void DrawEventsGUI(Rect window, Rect region, FusionInspectorDB db)
		{
			RPCs rpcs = db.RPCs;

			// The RPC and Type columns are always visible.
			ERPCEventField visibleFields = rpcs.EventsVisibleFields | ERPCEventField.RPC | ERPCEventField.Type;

			rpcs.EventsVisibleFields = visibleFields;

			bool showObject         = visibleFields.Has(ERPCEventField.Object);
			bool showFrame          = visibleFields.Has(ERPCEventField.Frame);
			bool showTick           = visibleFields.Has(ERPCEventField.Tick);
			bool showSource         = visibleFields.Has(ERPCEventField.Source);
			bool showTarget         = visibleFields.Has(ERPCEventField.Target);
			bool showStateAuthority = visibleFields.Has(ERPCEventField.StateAuthority);
			bool showInputAuthority = visibleFields.Has(ERPCEventField.InputAuthority);
			bool showResult         = visibleFields.Has(ERPCEventField.Result);

			if (showObject == false)
			{
				rpcs.ObjectNameFilter = default;
			}

			List<RPCEvent> rpcEvents = db.RPCEvents;

			int totalEventCount = rpcEvents != null ? rpcEvents.Count : 0;

			UpdateEventTypeWidth(rpcs, rpcEvents);

			bool hasNameFilter       = string.IsNullOrWhiteSpace(rpcs.RPCNameFilter) == false;
			bool hasObjectNameFilter = string.IsNullOrWhiteSpace(rpcs.ObjectNameFilter) == false;
			bool hasComponentFilter  = rpcs.ComponentFilterInclude.Count > 0 || rpcs.ComponentFilterExclude.Count > 0;
			bool hasPlayerFilter     = rpcs.EventSourceFilter.IsNone == false || rpcs.EventTargetFilter.IsNone == false || rpcs.EventStateAuthorityFilter.IsNone == false || rpcs.EventInputAuthorityFilter.IsNone == false || rpcs.EventAnyPlayerFilter.IsNone == false;
			bool hasObjectFilter     = ReferenceEquals(rpcs.EventObjectFilter, null) == false;

			if ((hasNameFilter == true || hasObjectNameFilter == true || hasComponentFilter == true || hasPlayerFilter == true || hasObjectFilter == true) && rpcEvents != null)
			{
				UpdateFilteredEvents(rpcs, rpcEvents);
				rpcEvents = _filteredEvents;
			}
			else
			{
				// Forces a full rebuild once any filter is enabled again.
				_filteredEventsSource = default;
			}

			int eventCount = rpcEvents != null ? rpcEvents.Count : 0;

			GUISeparators.Clear();

			region.xMin += FusionInspector.LeftWindowOffset;
			region.xMax -= FusionInspector.RightWindowOffset;

			float flexibleWidth = region.width;

			if (showFrame          == true) { flexibleWidth -= _eventFrameStat.Width;          }
			if (showTick           == true) { flexibleWidth -= _eventTickStat.Width;           }
			if (showSource         == true) { flexibleWidth -= _eventSourceStat.Width;         }
			if (showTarget         == true) { flexibleWidth -= _eventTargetStat.Width;         }
			if (showStateAuthority == true) { flexibleWidth -= _eventStateAuthorityStat.Width; }
			if (showInputAuthority == true) { flexibleWidth -= _eventInputAuthorityStat.Width; }
			if (showResult         == true) { flexibleWidth -= _eventResultStat.Width;         }

			// The type column fits the longest recorded type name, the rest is split between RPC and Object.
			_eventTypeStat.Width   = Mathf.Min(Mathf.Max(_eventTypeMinWidth, _eventTypeWidth), flexibleWidth * 0.5f);
			_eventObjectStat.Width = showObject == true ? (flexibleWidth - _eventTypeStat.Width) * 0.5f : 0.0f;
			_eventRpcStat.Width    = flexibleWidth - _eventTypeStat.Width - _eventObjectStat.Width;

			Rect headerRect = region;
			headerRect.width  = _eventTypeStat.Width;
			headerRect.height = FusionInspector.HeaderHeight;

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, window.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			Rect componentFilterRect = headerRect;
			componentFilterRect.xMin = componentFilterRect.xMax - FusionInspector.HeaderHeight;
			GUIColor.Set(FusionInspector.FilterColors[Mathf.Min(rpcs.ComponentFilterInclude.Count + rpcs.ComponentFilterExclude.Count, 1)]);
			if (GUI.Button(componentFilterRect, _filterContent, GUIStyles.Icon) == true)
			{
				if (Event.current.button == 1)
				{
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Reset"), false, () =>
					{
						rpcs.ComponentFilter.Clear();
						++rpcs.ComponentFilterVersion;
					});
					menu.ShowAsContext();
				}
				else
				{
					rpcs.ComponentFilterFoldout = !rpcs.ComponentFilterFoldout;
					rpcs.EventsVisibleFieldsFoldout = false;
				}
			}
			GUIColor.Reset();

			Rect typeRect = headerRect;
			typeRect.xMin += _eventIconLabelOffset;
			typeRect.xMax = componentFilterRect.xMin;
			EditorGUI.LabelField(typeRect, _eventTypeStat.Content, _eventTypeStat.Style);

			headerRect.xMin += headerRect.width;
			headerRect.width = _eventRpcStat.Width;

			GUISeparators.AddX(headerRect.xMin);

			GUIContent rpcContent = totalEventCount > 0 ? _eventRpcStat.GetContent(totalEventCount, eventCount, default, default) : _eventRpcContent;

			Rect nameRect = headerRect;
			nameRect.xMin += _eventMethodLabelOffset;

			Rect nameFilterRect = headerRect;
			nameFilterRect.xMax -= 4.0f;
			FusionInspector.ApplyFilterRect(ref nameFilterRect, nameRect.xMin + _eventRpcStat.Style.CalcSize(rpcContent).x);
			rpcs.RPCNameFilter = FusionInspector.DrawFilterField(nameFilterRect, rpcs.RPCNameFilter);

			nameRect.xMax = nameFilterRect.xMin;
			EditorGUI.LabelField(nameRect, rpcContent, _eventRpcStat.Style);

			if (showObject == true)
			{
				headerRect.xMin += headerRect.width;
				headerRect.width = _eventObjectStat.Width;

				GUISeparators.AddX(headerRect.xMin);

				Rect objectRect = headerRect;
				objectRect.xMin += _eventIconLabelOffset;

				Rect objectFilterRect = headerRect;
				objectFilterRect.xMax -= 4.0f;
				FusionInspector.ApplyFilterRect(ref objectFilterRect, objectRect.xMin + _eventObjectStat.Style.CalcSize(_eventObjectStat.Content).x);
				rpcs.ObjectNameFilter = FusionInspector.DrawFilterField(objectFilterRect, rpcs.ObjectNameFilter);

				objectRect.xMax = objectFilterRect.xMin;
				EditorGUI.LabelField(objectRect, _eventObjectStat.Content, _eventObjectStat.Style);
			}

			if (showFrame          == true) { DrawLabelHeader(_eventFrameStat,          ref headerRect); }
			if (showTick           == true) { DrawLabelHeader(_eventTickStat,          ref headerRect); }
			if (showSource         == true) { DrawLabelHeader(_eventSourceStat,         ref headerRect); }
			if (showTarget         == true) { DrawLabelHeader(_eventTargetStat,         ref headerRect); }
			if (showStateAuthority == true) { DrawLabelHeader(_eventStateAuthorityStat, ref headerRect); }
			if (showInputAuthority == true) { DrawLabelHeader(_eventInputAuthorityStat, ref headerRect); }
			if (showResult         == true) { DrawLabelHeader(_eventResultStat,         ref headerRect); }

			headerRect.xMin += headerRect.width;
			headerRect.width = FusionInspector.RightWindowOffset;

			GUISeparators.AddX(headerRect.xMin);

			if (GUI.Button(headerRect, _fieldsContent, GUIStyles.Icon) == true)
			{
				rpcs.EventsVisibleFieldsFoldout = !rpcs.EventsVisibleFieldsFoldout;
				rpcs.ComponentFilterFoldout = false;
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			region.yMin += FusionInspector.HeaderHeight;

			DrawEventsFieldsGUI(ref region, rpcs);
			DrawComponentFilterGUI(ref region, rpcs, db.Components);
			DrawEventsFilterBarGUI(window, ref region, rpcs);

			GUISeparators.AddY(region.y);

			if (eventCount == 0)
			{
				EditorGUI.LabelField(region, "No RPCs recorded", GUIStyles.Icon);
			}

			Rect scrollRect     = new Rect(region.x, region.y, region.width + FusionInspector.RightWindowOffset, region.height);
			Rect scrollViewRect = new Rect(0.0f, 0.0f, region.width, FusionInspector.LineHeight * eventCount);

			// When scrolled to the very bottom, the view sticks to the most recent event.
			float bottomScrollY = Mathf.Max(0.0f, scrollViewRect.height - scrollRect.height);
			if (_eventsStickToBottom == true)
			{
				_eventsScrollPosition.y = bottomScrollY;
			}

			_eventsScrollPosition = GUI.BeginScrollView(scrollRect, _eventsScrollPosition, scrollViewRect);

			_eventsStickToBottom = _eventsScrollPosition.y >= (bottomScrollY - 1.0f);

			float   minScrollY    = _eventsScrollPosition.y;
			float   maxScrollY    = _eventsScrollPosition.y + region.height;
			Vector2 mousePosition = Event.current.mousePosition;
			Rect    drawRect      = new Rect(0.0f, 0.0f, 0.0f, FusionInspector.LineHeight);

			for (int i = 0; i < eventCount; ++i)
			{
				if (drawRect.y > maxScrollY)
					break;

				if (drawRect.yMax < minScrollY)
				{
					drawRect.y += drawRect.height;
					continue;
				}

				RPCEvent rpcEvent = rpcEvents[i];

				bool isSent = rpcEvent.Type == NetworkRunnerDebugRpcEventType.LocalCall;

				bool isSelected    = _hasSelectedEvent == true && IsSameEvent(rpcEvent, _selectedEvent) == true;
				bool isHighlighted = _hasSelectedEvent == true && isSelected == false && ReferenceEquals(_selectedEvent.Object, null) == false && ReferenceEquals(rpcEvent.Object, _selectedEvent.Object) == true;

				if (isSelected == true)
				{
					Rect selectionRect = drawRect;
					selectionRect.xMin = 0.0f;
					selectionRect.xMax = window.width;
					selectionRect.yMin = Mathf.Max(minScrollY, selectionRect.yMin + 0.25f);
					selectionRect.yMax = Mathf.Min(selectionRect.yMax, maxScrollY);
					TextureUtility.DrawTexture(selectionRect, FusionInspector.SelectionColor);

					selectionRect.width = scrollRect.x;
					selectionRect.y += scrollRect.y - _eventsScrollPosition.y;
					GUIHighlights.Add(selectionRect, FusionInspector.SelectionColor);
				}
				else if (mousePosition.x >= 0.0f && mousePosition.x <= region.width && mousePosition.y >= minScrollY && mousePosition.y <= maxScrollY && mousePosition.y > drawRect.yMin && mousePosition.y < drawRect.yMax)
				{
					Rect hoverRect = drawRect;
					hoverRect.xMin = 0.0f;
					hoverRect.xMax = window.width;
					hoverRect.yMin = Mathf.Max(minScrollY, hoverRect.yMin + 0.25f);
					hoverRect.yMax = Mathf.Min(hoverRect.yMax, maxScrollY);
					TextureUtility.DrawTexture(hoverRect, FusionInspector.HoverColor);

					hoverRect.width = scrollRect.x;
					hoverRect.y += scrollRect.y - _eventsScrollPosition.y;
					GUIHighlights.Add(hoverRect, FusionInspector.HoverColor);
				}
				else if (isHighlighted == true)
				{
					Rect highlightRect = drawRect;
					highlightRect.xMin = 0.0f;
					highlightRect.xMax = window.width;
					highlightRect.yMin = Mathf.Max(minScrollY, highlightRect.yMin + 0.25f);
					highlightRect.yMax = Mathf.Min(highlightRect.yMax, maxScrollY);
					TextureUtility.DrawTexture(highlightRect, FusionInspector.HighlightColor);

					highlightRect.width = scrollRect.x;
					highlightRect.y += scrollRect.y - _eventsScrollPosition.y;
					GUIHighlights.Add(highlightRect, FusionInspector.HighlightColor);
				}

				drawRect.xMin  = 0.0f;
				drawRect.width = _eventTypeStat.Width;

				FusionInspector.ApplyLinePadding(ref drawRect);

				RPCInfo rpcInfo = default;
				if (rpcEvent.Method != null)
				{
					rpcs.TryGetInfo(rpcEvent.Method, out rpcInfo);
				}

				if (rpcInfo != null)
				{
					ComponentInfo component = rpcInfo.Component;

					_scriptContent.text = component.TypeName;
					drawRect.height += 1.0f; // GUI.Button dead zone fix
					if (GUI.Button(drawRect, _scriptContent, GUIStyles.ObjectField) == true && component.Script != null)
					{
						if (Event.current.button == 1)
						{
							string assetPath = AssetDatabase.GetAssetPath(component.Script);

							GenericMenu menu = new GenericMenu();
							menu.AddItem(new GUIContent("Copy Name"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, component.TypeName);
							menu.AddItem(new GUIContent("Copy Path"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, assetPath);
							menu.AddItem(new GUIContent("Copy Full Path"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, string.IsNullOrEmpty(assetPath) == false ? System.IO.Path.GetFullPath(assetPath) : "");
							menu.AddSeparator("");
							menu.AddItem(new GUIContent(Application.platform == RuntimePlatform.OSXEditor ? "Reveal in Finder" : "Show in Explorer"), false, (data) => { EditorUtility.RevealInFinder((string)data); }, assetPath);
							menu.ShowAsContext();
						}
						else if (Event.current.button == 0)
						{
							ToggleSelectedEvent(rpcEvent);

							if (_selectionObject == component && EditorApplication.timeSinceStartup < (_selectionTime + FusionInspector.SelectionDelay))
							{
								AssetDatabase.OpenAsset(component.Script);
								_selectionObject = default;
								_selectionTime   = default;
							}
							else
							{
								EditorGUIUtility.PingObject(component.Script);
								_selectionObject = component;
								_selectionTime   = EditorApplication.timeSinceStartup;
							}
						}
					}
					drawRect.height -= 1.0f; // GUI.Button dead zone fix
				}

				drawRect.xMin += drawRect.width;
				drawRect.width = _eventRpcStat.Width;

				Rect directionRect = drawRect;
				directionRect.width = FusionInspector.LineHeight;
				directionRect.x += 2.0f;

				GUIColor.Set(isSent == true ? Color.red : Color.green);
				EditorGUI.LabelField(directionRect, isSent == true ? _eventSentContent : _eventReceivedContent, GUIStyles.Icon);
				GUIColor.Reset();

				Rect methodRect = drawRect;
				methodRect.xMin += directionRect.width - 2.0f;

				_eventMethodContent.text    = rpcInfo != null ? rpcInfo.MethodName : (rpcEvent.Method != null ? rpcEvent.Method.Name : "Unknown");
				_eventMethodContent.tooltip = rpcInfo != null ? rpcInfo.MethodDeclaration : "";

				methodRect.height += 1.0f; // GUI.Button dead zone fix
				if (GUI.Button(methodRect, _eventMethodContent, GUIStyles.ObjectField) == true)
				{
					if (Event.current.button == 1)
					{
						if (rpcInfo != null)
						{
							string methodName = rpcInfo.MethodName;

							GenericMenu menu = new GenericMenu();
							menu.AddItem(new GUIContent($"Filter RPC: {methodName}"), rpcs.RPCNameFilter == methodName, () => { rpcs.RPCNameFilter = rpcs.RPCNameFilter == methodName ? default : methodName; });
							menu.AddSeparator("");
							menu.AddItem(new GUIContent("Copy Name"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, rpcInfo.MethodName);
							menu.AddItem(new GUIContent("Copy Path"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, rpcInfo.MethodHierarchy);
							menu.ShowAsContext();
						}
					}
					else if (Event.current.button == 0)
					{
						ToggleSelectedEvent(rpcEvent);

						if (rpcInfo != null && rpcInfo.Component.Script != null)
						{
							if (_selectionObject == rpcInfo && EditorApplication.timeSinceStartup < (_selectionTime + FusionInspector.SelectionDelay))
							{
								int methodLineNumber = ScriptUtility.GetLineNumber(rpcInfo.Method);
								AssetDatabase.OpenAsset(rpcInfo.Component.Script, methodLineNumber);
								_selectionObject = default;
								_selectionTime   = default;
							}
							else
							{
								_selectionObject = rpcInfo;
								_selectionTime   = EditorApplication.timeSinceStartup;
							}
						}
					}
				}

				if (showObject == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _eventObjectStat.Width;
				}

				if (showObject == true && string.IsNullOrEmpty(rpcEvent.ObjectName) == false)
				{
					NetworkBehaviour behaviour = rpcEvent.Behaviour;

					bool isDespawned = ReferenceEquals(rpcEvent.Object, null) == false && rpcEvent.Object == null;

					_objectContent.text    = rpcEvent.ObjectName;
					_objectContent.tooltip = isDespawned == true ? "The object is already despawned." : "";
					drawRect.height += 1.0f; // GUI.Button dead zone fix
					GUIColor.Set(isDespawned == true ? FusionInspector.InactiveTextColor : FusionInspector.ActiveTextColor);
					if (GUI.Button(drawRect, _objectContent, GUIStyles.ObjectField) == true)
					{
						if (Event.current.button == 1)
						{
							GenericMenu menu = new GenericMenu();
							if (ReferenceEquals(rpcEvent.Object, null) == false)
							{
								NetworkObject filterObject     = rpcEvent.Object;
								string        filterObjectName = rpcEvent.ObjectName;

								menu.AddItem(new GUIContent($"Filter Object: {filterObjectName}"), ReferenceEquals(rpcs.EventObjectFilter, filterObject) == true, () =>
								{
									if (ReferenceEquals(rpcs.EventObjectFilter, filterObject) == true)
									{
										rpcs.EventObjectFilter     = default;
										rpcs.EventObjectFilterName = default;
									}
									else
									{
										rpcs.EventObjectFilter     = filterObject;
										rpcs.EventObjectFilterName = filterObjectName;
									}
								});
							}

							string objectName = rpcEvent.ObjectName;
							menu.AddItem(new GUIContent($"Filter Name: {objectName}"), rpcs.ObjectNameFilter == objectName, () => { rpcs.ObjectNameFilter = rpcs.ObjectNameFilter == objectName ? default : objectName; });

							menu.AddSeparator("");
							menu.AddItem(new GUIContent("Copy Name"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, rpcEvent.ObjectName);
							if (behaviour != null)
							{
								menu.AddItem(new GUIContent("Copy Path"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, GetHierarchyPath(behaviour.transform));
							}
							menu.ShowAsContext();
						}
						else if (Event.current.button == 0)
						{
							ToggleSelectedEvent(rpcEvent);

							if (behaviour != null)
							{
								if (_selectionObject == (object)behaviour && EditorApplication.timeSinceStartup < (_selectionTime + FusionInspector.SelectionDelay))
								{
									Selection.SetActiveObjectWithContext(behaviour.gameObject, behaviour.gameObject);
									_selectionObject = default;
									_selectionTime   = default;
								}
								else
								{
									EditorGUIUtility.PingObject(behaviour.gameObject);
									_selectionObject = behaviour;
									_selectionTime   = EditorApplication.timeSinceStartup;
								}
							}
						}
					}
					GUIColor.Reset();
					drawRect.height -= 1.0f; // GUI.Button dead zone fix

					if ((rpcEvent.HasStateAuthority == true || rpcEvent.HasInputAuthority == true) && drawRect.yMin >= minScrollY && drawRect.yMax <= maxScrollY)
					{
						Rect dotRect = drawRect;
						dotRect.xMin  = drawRect.xMax - _localDotWidth - 3.0f;
						dotRect.width = _localDotWidth;

						if (rpcEvent.HasStateAuthority == true)
						{
							GUIColor.Set(Color.green);
							EditorGUI.LabelField(dotRect, _localStateAuthorityContent, GUIStyles.Dot);
							GUIColor.Reset();
						}

						dotRect.x -= _localDotWidth - 4.0f;

						if (rpcEvent.HasInputAuthority == true)
						{
							GUIColor.Set(Color.yellow);
							EditorGUI.LabelField(dotRect, _localInputAuthorityContent, GUIStyles.Dot);
							GUIColor.Reset();
						}
					}
				}

				if (showFrame == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _eventFrameStat.Width;

					EditorGUI.LabelField(drawRect, GetTickLabel(rpcEvent.Frame), GUIStyles.RightLabel);
				}

				if (showTick == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _eventTickStat.Width;

					EditorGUI.LabelField(drawRect, GetTickLabel(rpcEvent.Tick), GUIStyles.RightLabel);
				}

				if (showSource == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _eventSourceStat.Width;

					if (rpcEvent.SourcePlayer != PlayerRef.None)
					{
						DrawPlayerLabel(drawRect, rpcEvent.SourcePlayer, rpcEvent.LocalPlayer);

						if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && drawRect.Contains(Event.current.mousePosition) == true)
						{
							PlayerRef sourcePlayer = rpcEvent.SourcePlayer;

							GenericMenu menu = new GenericMenu();
							menu.AddItem(new GUIContent($"Filter Source: {GetPlayerLabel(sourcePlayer)}"), rpcs.EventSourceFilter == sourcePlayer, () => { rpcs.EventSourceFilter = rpcs.EventSourceFilter == sourcePlayer ? PlayerRef.None : sourcePlayer; });
							menu.AddItem(new GUIContent($"Filter Any: {GetPlayerLabel(sourcePlayer)}"), rpcs.EventAnyPlayerFilter == sourcePlayer, () => { rpcs.EventAnyPlayerFilter = rpcs.EventAnyPlayerFilter == sourcePlayer ? PlayerRef.None : sourcePlayer; });
							menu.ShowAsContext();

							Event.current.Use();
						}
					}
				}

				if (showTarget == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _eventTargetStat.Width;

					if (rpcEvent.TargetPlayer != PlayerRef.None)
					{
						DrawPlayerLabel(drawRect, rpcEvent.TargetPlayer, rpcEvent.LocalPlayer);

						if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && drawRect.Contains(Event.current.mousePosition) == true)
						{
							PlayerRef targetPlayer = rpcEvent.TargetPlayer;

							GenericMenu menu = new GenericMenu();
							menu.AddItem(new GUIContent($"Filter Target: {GetPlayerLabel(targetPlayer)}"), rpcs.EventTargetFilter == targetPlayer, () => { rpcs.EventTargetFilter = rpcs.EventTargetFilter == targetPlayer ? PlayerRef.None : targetPlayer; });
							menu.AddItem(new GUIContent($"Filter Any: {GetPlayerLabel(targetPlayer)}"), rpcs.EventAnyPlayerFilter == targetPlayer, () => { rpcs.EventAnyPlayerFilter = rpcs.EventAnyPlayerFilter == targetPlayer ? PlayerRef.None : targetPlayer; });
							menu.ShowAsContext();

							Event.current.Use();
						}
					}
					else if (rpcInfo != null)
					{
						DrawFlags(drawRect, _targetFlagContents, _targetExcludedFlagContents, (rpcInfo.Targets & RpcTargets.StateAuthority) == RpcTargets.StateAuthority, (rpcInfo.Targets & RpcTargets.InputAuthority) == RpcTargets.InputAuthority, (rpcInfo.Targets & RpcTargets.Proxies) == RpcTargets.Proxies);
					}
				}

				if (showStateAuthority == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _eventStateAuthorityStat.Width;

					if (rpcEvent.StateAuthority != PlayerRef.None)
					{
						DrawPlayerLabel(drawRect, rpcEvent.StateAuthority, rpcEvent.LocalPlayer);

						if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && drawRect.Contains(Event.current.mousePosition) == true)
						{
							PlayerRef stateAuthority = rpcEvent.StateAuthority;

							GenericMenu menu = new GenericMenu();
							menu.AddItem(new GUIContent($"Filter S-Authority: {GetPlayerLabel(stateAuthority)}"), rpcs.EventStateAuthorityFilter == stateAuthority, () => { rpcs.EventStateAuthorityFilter = rpcs.EventStateAuthorityFilter == stateAuthority ? PlayerRef.None : stateAuthority; });
							menu.AddItem(new GUIContent($"Filter Any: {GetPlayerLabel(stateAuthority)}"), rpcs.EventAnyPlayerFilter == stateAuthority, () => { rpcs.EventAnyPlayerFilter = rpcs.EventAnyPlayerFilter == stateAuthority ? PlayerRef.None : stateAuthority; });
							menu.ShowAsContext();

							Event.current.Use();
						}
					}
				}

				if (showInputAuthority == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _eventInputAuthorityStat.Width;

					if (rpcEvent.InputAuthority != PlayerRef.None)
					{
						DrawPlayerLabel(drawRect, rpcEvent.InputAuthority, rpcEvent.LocalPlayer);

						if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && drawRect.Contains(Event.current.mousePosition) == true)
						{
							PlayerRef inputAuthority = rpcEvent.InputAuthority;

							GenericMenu menu = new GenericMenu();
							menu.AddItem(new GUIContent($"Filter I-Authority: {GetPlayerLabel(inputAuthority)}"), rpcs.EventInputAuthorityFilter == inputAuthority, () => { rpcs.EventInputAuthorityFilter = rpcs.EventInputAuthorityFilter == inputAuthority ? PlayerRef.None : inputAuthority; });
							menu.AddItem(new GUIContent($"Filter Any: {GetPlayerLabel(inputAuthority)}"), rpcs.EventAnyPlayerFilter == inputAuthority, () => { rpcs.EventAnyPlayerFilter = rpcs.EventAnyPlayerFilter == inputAuthority ? PlayerRef.None : inputAuthority; });
							menu.ShowAsContext();

							Event.current.Use();
						}
					}
				}

				if (showResult == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _eventResultStat.Width;

					if (rpcEvent.InvokeInfo.HasValue == true)
					{
						RpcInvokeInfo invokeInfo = rpcEvent.InvokeInfo.Value;

#if FUSION_2_1_OR_NEWER
						RpcSendMessageResult sendMessageResult = invokeInfo.SendMessageResult;
#else
						RpcSendMessageResult sendMessageResult = invokeInfo.SendResult.Result;
#endif

						bool isOk = (sendMessageResult & RpcSendMessageResult.MaskNotSent) == 0 || invokeInfo.LocalInvokeResult == RpcLocalInvokeResult.Invoked;
						if (isOk == true)
						{
							_eventResultContent.text    = GUISymbols.CHECKMARK;
							_eventResultContent.tooltip = "OK";

							EditorGUI.LabelField(drawRect, _eventResultContent, GUIStyles.Icon);
						}
						else
						{
							_eventResultContent.text    = GUISymbols.CROSS;
							_eventResultContent.tooltip = GetResultTooltip(sendMessageResult);

							GUIColor.Set(Color.red);
							EditorGUI.LabelField(drawRect, _eventResultContent, GUIStyles.Icon);
							GUIColor.Reset();
						}
					}
				}

				FusionInspector.RestoreLinePadding(ref drawRect);

				if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && mousePosition.x >= 0.0f && mousePosition.x <= region.width && mousePosition.y > drawRect.yMin && mousePosition.y < drawRect.yMax)
				{
					ToggleSelectedEvent(rpcEvent);

					Event.current.Use();
				}

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			GUIHighlights.DrawAll();

			float scrollHeight = Mathf.Min(scrollRect.height, scrollViewRect.height);

			GUISeparators.AddY(scrollRect.yMin + scrollHeight);

			GUISeparators.DrawAll(new Rect(0.0f, 0.0f, window.width, window.height), headerRect.yMin, headerRect.height, scrollRect.yMin, scrollHeight);
		}

		private void DrawTotalGUI(Rect window, Rect totalRect, RPCs rpcs, bool showSources, bool showTargets, bool showIsReliable, bool showIsTickAligned, bool showHasRpcTarget, bool showInvokeLocal, bool showSent, bool showReceived)
		{
			int totalSent     = 0;
			int totalReceived = 0;

			for (int i = 0, count = rpcs.Filtered.Count; i < count; ++i)
			{
				RPCInfo rpc = rpcs.Filtered[i];

				totalSent     += rpc.Sent.Value;
				totalReceived += rpc.Received.Value;
			}

			_totalSent.Value     = totalSent;
			_totalReceived.Value = totalReceived;

			TextureUtility.DrawTexture(new Rect(0.0f, totalRect.yMin, window.width, totalRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref totalRect);

			Rect drawRect = totalRect;
			drawRect.width = _methodStat.Width;

			Rect totalLabelRect = drawRect;
			totalLabelRect.xMin += GUIStyles.ObjectField.padding.left + (totalLabelRect.height - GUIStyles.ObjectField.padding.vertical) - GUIStyles.LeftBoldLabel.padding.left + 2.0f;

			if (rpcs.SortMode == ERPCSortMode.NameHierarchyAscending || rpcs.SortMode == ERPCSortMode.NameHierarchyDescending)
			{
				totalLabelRect.xMin += 16.0f;
			}

			EditorGUI.LabelField(totalLabelRect, _totalContent, GUIStyles.LeftBoldLabel);

			if (showSources       == true) { drawRect.xMin += drawRect.width; drawRect.width = _sourcesStat.Width;       }
			if (showTargets       == true) { drawRect.xMin += drawRect.width; drawRect.width = _targetsStat.Width;       }
			if (showIsReliable    == true) { drawRect.xMin += drawRect.width; drawRect.width = _isReliableStat.Width;    }
			if (showIsTickAligned == true) { drawRect.xMin += drawRect.width; drawRect.width = _isTickAlignedStat.Width; }
			if (showHasRpcTarget  == true) { drawRect.xMin += drawRect.width; drawRect.width = _hasRpcTargetStat.Width;  }
			if (showInvokeLocal   == true) { drawRect.xMin += drawRect.width; drawRect.width = _invokeLocalStat.Width;   }

			if (showSent == true)
			{
				drawRect.xMin += drawRect.width;
				drawRect.width = _sentStat.Width;

				if (_totalSent.Value > 0)
				{
					EditorGUI.LabelField(drawRect, _totalSent.GetLabel(), GUIStyles.RightBoldLabel);
				}
			}

			if (showReceived == true)
			{
				drawRect.xMin += drawRect.width;
				drawRect.width = _receivedStat.Width;

				if (_totalReceived.Value > 0)
				{
					EditorGUI.LabelField(drawRect, _totalReceived.GetLabel(), GUIStyles.RightBoldLabel);
				}
			}

			FusionInspector.RestoreLinePadding(ref totalRect);
		}

		private void DrawEventsFieldsGUI(ref Rect sectionRect, RPCs rpcs)
		{
			if (rpcs.EventsVisibleFieldsFoldout == false)
				return;

			float xOffset = 3.0f;
			float yOffset = 6.0f;

			Rect innerRect = sectionRect;
			innerRect.xMin += xOffset;
			innerRect.xMax -= xOffset;
			innerRect.height = EditorGUIUtility.singleLineHeight;

			float widthMultiplier = Mathf.Min(1.0f, innerRect.width / _eventsFieldsWidth);

			Rect columnsRect = innerRect;
			columnsRect.xMax = columnsRect.xMin + _eventsFieldsWidth * widthMultiplier;

			columnsRect.y += yOffset;

			DrawCategoryToggle(ref columnsRect, "General", ERPCEventField.General);
			columnsRect.y += yOffset;

			DrawToggle(ref columnsRect, "Object",          _eventObjectStat.Content.tooltip,         ERPCEventField.Object);
			DrawToggle(ref columnsRect, "Frame",           _eventFrameStat.Content.tooltip,          ERPCEventField.Frame);
			DrawToggle(ref columnsRect, "Tick",            _eventTickStat.Content.tooltip,           ERPCEventField.Tick);
			DrawToggle(ref columnsRect, "Source",          _eventSourceStat.Content.tooltip,         ERPCEventField.Source);
			DrawToggle(ref columnsRect, "Target",          _eventTargetStat.Content.tooltip,         ERPCEventField.Target);
			DrawToggle(ref columnsRect, "State Authority", _eventStateAuthorityStat.Content.tooltip, ERPCEventField.StateAuthority);
			DrawToggle(ref columnsRect, "Input Authority", _eventInputAuthorityStat.Content.tooltip, ERPCEventField.InputAuthority);
			DrawToggle(ref columnsRect, "Result",          _eventResultStat.Content.tooltip,         ERPCEventField.Result);

			columnsRect.y += yOffset;

			sectionRect.yMin = columnsRect.yMin;

			return;

			void DrawCategoryToggle(ref Rect rect, string label, ERPCEventField categoryFields)
			{
				ERPCEventField enabledFields = rpcs.EventsVisibleFields & categoryFields;

				bool allEnabled = enabledFields == categoryFields;

				EditorGUI.showMixedValue = allEnabled == false && enabledFields != ERPCEventField.None;
				bool toggleState = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width - FusionInspector.CategorySpacing * widthMultiplier, rect.height), label, allEnabled, EditorStyles.boldLabel);
				EditorGUI.showMixedValue = false;

				if (toggleState != allEnabled)
				{
					rpcs.EventsSetVisibleFields(categoryFields, toggleState);
				}

				rect.y += rect.height;

				TextureUtility.DrawTexture(new Rect(rect.x, rect.y + 2.0f, rect.width - FusionInspector.CategorySpacing * widthMultiplier, FusionInspector.SeparatorSize), FusionInspector.SeparatorColor);
			}

			void DrawToggle(ref Rect rect, string label, string tooltip, ERPCEventField field)
			{
				_toggleContent.text    = label;
				_toggleContent.tooltip = tooltip;

				bool previousState = rpcs.EventsVisibleFields.Has(field);
				bool currentState  = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width - FusionInspector.CategorySpacing * widthMultiplier, rect.height), _toggleContent, previousState);

				if (currentState != previousState)
				{
					rpcs.EventsToggleVisibleField(field);
				}

				rect.y += rect.height;
			}
		}

		private void UpdateEventTypeWidth(RPCs rpcs, List<RPCEvent> rpcEvents)
		{
			if (_eventTypesSource != rpcEvents)
			{
				_eventTypesSource = rpcEvents;
				_eventTypesCount  = 0;
				_eventTypeWidth   = 0.0f;

				_eventTypes.Clear();
			}

			if (rpcEvents == null)
				return;

			for (int i = _eventTypesCount, count = rpcEvents.Count; i < count; ++i)
			{
				RPCEvent rpcEvent = rpcEvents[i];

				if (rpcEvent.Method == null || rpcs.TryGetInfo(rpcEvent.Method, out RPCInfo rpcInfo) == false)
					continue;

				if (_eventTypes.Add(rpcInfo.Component) == false)
					continue;

				_toggleContent.text    = rpcInfo.Component.TypeName;
				_toggleContent.tooltip = default;

				float width = GUIStyles.ObjectField.CalcSize(_toggleContent).x + _eventTypeIconWidth + 12.0f;
				if (width > _eventTypeWidth)
				{
					_eventTypeWidth = width;
				}
			}

			_eventTypesCount = rpcEvents.Count;
		}

		private void DrawEventsFilterBarGUI(Rect window, ref Rect region, RPCs rpcs)
		{
			bool hasSourceFilter         = rpcs.EventSourceFilter.IsNone == false;
			bool hasTargetFilter         = rpcs.EventTargetFilter.IsNone == false;
			bool hasStateAuthorityFilter = rpcs.EventStateAuthorityFilter.IsNone == false;
			bool hasInputAuthorityFilter = rpcs.EventInputAuthorityFilter.IsNone == false;
			bool hasAnyPlayerFilter      = rpcs.EventAnyPlayerFilter.IsNone == false;
			bool hasObjectFilter         = ReferenceEquals(rpcs.EventObjectFilter, null) == false;

			if (hasSourceFilter == false && hasTargetFilter == false && hasStateAuthorityFilter == false && hasInputAuthorityFilter == false && hasAnyPlayerFilter == false && hasObjectFilter == false)
				return;

			if (hasAnyPlayerFilter == true && _eventAnyPlayerFilterPlayer != rpcs.EventAnyPlayerFilter)
			{
				_eventAnyPlayerFilterPlayer = rpcs.EventAnyPlayerFilter;
				_eventAnyPlayerFilterContent.text = $"Any: {GetPlayerLabel(_eventAnyPlayerFilterPlayer)}";
			}

			if (hasObjectFilter == true && _eventObjectFilterName != rpcs.EventObjectFilterName)
			{
				_eventObjectFilterName = rpcs.EventObjectFilterName;
				_eventObjectFilterContent.text = $"Object: {_eventObjectFilterName}";
			}

			if (hasSourceFilter == true && _eventSourceFilterPlayer != rpcs.EventSourceFilter)
			{
				_eventSourceFilterPlayer = rpcs.EventSourceFilter;
				_eventSourceFilterContent.text = $"Source: {GetPlayerLabel(_eventSourceFilterPlayer)}";
			}

			if (hasTargetFilter == true && _eventTargetFilterPlayer != rpcs.EventTargetFilter)
			{
				_eventTargetFilterPlayer = rpcs.EventTargetFilter;
				_eventTargetFilterContent.text = $"Target: {GetPlayerLabel(_eventTargetFilterPlayer)}";
			}

			if (hasStateAuthorityFilter == true && _eventStateAuthorityFilterPlayer != rpcs.EventStateAuthorityFilter)
			{
				_eventStateAuthorityFilterPlayer = rpcs.EventStateAuthorityFilter;
				_eventStateAuthorityFilterContent.text = $"S-Authority: {GetPlayerLabel(_eventStateAuthorityFilterPlayer)}";
			}

			if (hasInputAuthorityFilter == true && _eventInputAuthorityFilterPlayer != rpcs.EventInputAuthorityFilter)
			{
				_eventInputAuthorityFilterPlayer = rpcs.EventInputAuthorityFilter;
				_eventInputAuthorityFilterContent.text = $"I-Authority: {GetPlayerLabel(_eventInputAuthorityFilterPlayer)}";
			}

			Rect barRect = region;
			barRect.height = FusionInspector.LineHeight;

			TextureUtility.DrawTexture(new Rect(0.0f, barRect.yMin, window.width, barRect.height), FusionInspector.HeaderColor);

			region.yMin += barRect.height;

			if (hasObjectFilter == true && DrawFilterChip(ref barRect, _eventObjectFilterContent) == true)
			{
				rpcs.EventObjectFilter     = default;
				rpcs.EventObjectFilterName = default;
			}

			if (hasAnyPlayerFilter == true && DrawFilterChip(ref barRect, _eventAnyPlayerFilterContent) == true)
			{
				rpcs.EventAnyPlayerFilter = PlayerRef.None;
			}

			if (hasSourceFilter == true && DrawFilterChip(ref barRect, _eventSourceFilterContent) == true)
			{
				rpcs.EventSourceFilter = PlayerRef.None;
			}

			if (hasTargetFilter == true && DrawFilterChip(ref barRect, _eventTargetFilterContent) == true)
			{
				rpcs.EventTargetFilter = PlayerRef.None;
			}

			if (hasStateAuthorityFilter == true && DrawFilterChip(ref barRect, _eventStateAuthorityFilterContent) == true)
			{
				rpcs.EventStateAuthorityFilter = PlayerRef.None;
			}

			if (hasInputAuthorityFilter == true && DrawFilterChip(ref barRect, _eventInputAuthorityFilterContent) == true)
			{
				rpcs.EventInputAuthorityFilter = PlayerRef.None;
			}
		}

		private bool DrawFilterChip(ref Rect rect, GUIContent content)
		{
			Rect buttonRect = rect;
			buttonRect.width = rect.height;

			GUIColor.Set(Color.red);
			bool remove = GUI.Button(buttonRect, _removeFilterContent, GUIStyles.Icon);
			GUIColor.Reset();

			Rect labelRect = rect;
			labelRect.xMin  = buttonRect.xMax - 4.0f;
			labelRect.width = GUIStyles.LeftLabel.CalcSize(content).x;
			EditorGUI.LabelField(labelRect, content, GUIStyles.LeftLabel);

			rect.xMin = labelRect.xMax + 16.0f;

			return remove;
		}

		private void UpdateFilteredEvents(RPCs rpcs, List<RPCEvent> rpcEvents)
		{
			if (_filteredEventsSource != rpcEvents || _filteredRPCNameFilter != rpcs.RPCNameFilter || _filteredObjectNameFilter != rpcs.ObjectNameFilter || _filteredComponentFilterVersion != rpcs.ComponentFilterVersion || _filteredSourceFilter != rpcs.EventSourceFilter || _filteredTargetFilter != rpcs.EventTargetFilter || _filteredStateAuthorityFilter != rpcs.EventStateAuthorityFilter || _filteredInputAuthorityFilter != rpcs.EventInputAuthorityFilter || _filteredAnyPlayerFilter != rpcs.EventAnyPlayerFilter || _filteredVisibleFields != rpcs.EventsVisibleFields || ReferenceEquals(_filteredObjectFilter, rpcs.EventObjectFilter) == false)
			{
				_filteredEventsSource           = rpcEvents;
				_filteredRPCNameFilter          = rpcs.RPCNameFilter;
				_filteredObjectNameFilter       = rpcs.ObjectNameFilter;
				_filteredComponentFilterVersion = rpcs.ComponentFilterVersion;
				_filteredSourceFilter           = rpcs.EventSourceFilter;
				_filteredTargetFilter           = rpcs.EventTargetFilter;
				_filteredStateAuthorityFilter   = rpcs.EventStateAuthorityFilter;
				_filteredInputAuthorityFilter   = rpcs.EventInputAuthorityFilter;
				_filteredAnyPlayerFilter        = rpcs.EventAnyPlayerFilter;
				_filteredVisibleFields          = rpcs.EventsVisibleFields;
				_filteredObjectFilter           = rpcs.EventObjectFilter;
				_filteredRPCEventCount          = 0;

				_filteredEvents.Clear();
			}

			bool hasRPCNameFilter        = string.IsNullOrWhiteSpace(rpcs.RPCNameFilter) == false;
			bool hasObjectNameFilter     = string.IsNullOrWhiteSpace(rpcs.ObjectNameFilter) == false;
			bool hasSourceFilter         = rpcs.EventSourceFilter.IsNone == false;
			bool hasTargetFilter         = rpcs.EventTargetFilter.IsNone == false;
			bool hasStateAuthorityFilter = rpcs.EventStateAuthorityFilter.IsNone == false;
			bool hasInputAuthorityFilter = rpcs.EventInputAuthorityFilter.IsNone == false;
			bool hasAnyPlayerFilter      = rpcs.EventAnyPlayerFilter.IsNone == false;
			bool hasObjectFilter         = ReferenceEquals(rpcs.EventObjectFilter, null) == false;

			bool showSource         = rpcs.EventsVisibleFields.Has(ERPCEventField.Source);
			bool showTarget         = rpcs.EventsVisibleFields.Has(ERPCEventField.Target);
			bool showStateAuthority = rpcs.EventsVisibleFields.Has(ERPCEventField.StateAuthority);
			bool showInputAuthority = rpcs.EventsVisibleFields.Has(ERPCEventField.InputAuthority);

			for (int i = _filteredRPCEventCount, count = rpcEvents.Count; i < count; ++i)
			{
				RPCEvent rpcEvent = rpcEvents[i];

				string  methodName = rpcEvent.Method != null ? rpcEvent.Method.Name : "";
				RPCInfo rpcInfo    = default;

				if (rpcEvent.Method != null && rpcs.TryGetInfo(rpcEvent.Method, out rpcInfo) == true)
				{
					methodName = rpcInfo.MethodName;
				}

				if (hasRPCNameFilter == true && methodName.Contains(rpcs.RPCNameFilter, System.StringComparison.OrdinalIgnoreCase) == false)
					continue;

				if (hasObjectNameFilter == true && (rpcEvent.ObjectName == null || rpcEvent.ObjectName.Contains(rpcs.ObjectNameFilter, System.StringComparison.OrdinalIgnoreCase) == false))
					continue;

				if (hasObjectFilter == true && ReferenceEquals(rpcEvent.Object, rpcs.EventObjectFilter) == false)
					continue;

				if (hasSourceFilter == true && rpcEvent.SourcePlayer != rpcs.EventSourceFilter)
					continue;
				if (hasTargetFilter == true && rpcEvent.TargetPlayer != rpcs.EventTargetFilter)
					continue;
				if (hasStateAuthorityFilter == true && rpcEvent.StateAuthority != rpcs.EventStateAuthorityFilter)
					continue;
				if (hasInputAuthorityFilter == true && rpcEvent.InputAuthority != rpcs.EventInputAuthorityFilter)
					continue;

				if (hasAnyPlayerFilter == true && (showSource == false || rpcEvent.SourcePlayer != rpcs.EventAnyPlayerFilter) && (showTarget == false || rpcEvent.TargetPlayer != rpcs.EventAnyPlayerFilter) && (showStateAuthority == false || rpcEvent.StateAuthority != rpcs.EventAnyPlayerFilter) && (showInputAuthority == false || rpcEvent.InputAuthority != rpcs.EventAnyPlayerFilter))
					continue;

				ComponentInfo component = rpcInfo != null ? rpcInfo.Component : default;

				if (rpcs.ComponentFilterInclude.Count > 0 && ContainsComponent(rpcs.ComponentFilterInclude, component) == false)
					continue;
				if (rpcs.ComponentFilterExclude.Count > 0 && ContainsComponent(rpcs.ComponentFilterExclude, component) == true)
					continue;

				_filteredEvents.Add(rpcEvent);
			}

			_filteredRPCEventCount = rpcEvents.Count;
		}

		private static bool ContainsComponent(HashSet<ComponentInfo> components, ComponentInfo component)
		{
			while (component != null)
			{
				if (components.Contains(component) == true)
					return true;

				component = component.Base;
			}

			return false;
		}

		private void DrawComponentFilterGUI(ref Rect sectionRect, RPCs rpcs, Components components)
		{
			rpcs.ComponentFilterInclude.Clear();
			rpcs.ComponentFilterExclude.Clear();

			if (rpcs.ComponentFilterFoldout == false)
			{
				foreach (KeyValuePair<ComponentInfo, EFilterMode> filter in rpcs.ComponentFilter)
				{
					if (filter.Value == EFilterMode.Include)
					{
						rpcs.ComponentFilterInclude.Add(filter.Key);
					}
					else if (filter.Value == EFilterMode.Exclude)
					{
						rpcs.ComponentFilterExclude.Add(filter.Key);
					}
				}

				return;
			}

			_tempComponents.Clear();
			for (int i = 0, count = components.All.Count; i < count; ++i)
			{
				ComponentInfo component = components.All[i];
				if (component.RPCs.Length > 0)
				{
					_tempComponents.Add(component);
				}
			}

			if (components.SortMode != EComponentSortMode.TypeNameAscending)
			{
				Components.Sort(_tempComponents, EComponentSortMode.TypeNameAscending);
			}

			float rowWidth = Mathf.Clamp(sectionRect.width, 160.0f, 224.0f);
			int   columns  = Mathf.Max(1, Mathf.FloorToInt(sectionRect.width / rowWidth));
			int   rows     = _tempComponents.Count / columns;

			if (_tempComponents.Count > (rows * columns))
			{
				++rows;
			}

			float xOffset    = 3.0f;
			float yOffset    = 6.0f;
			float lineHeight = EditorGUIUtility.singleLineHeight;

			Rect filterRect = sectionRect;
			filterRect.height = rows * lineHeight + yOffset * 2;
			sectionRect.yMin += filterRect.height;

			filterRect.xMin += xOffset;
			filterRect.xMax -= xOffset;
			filterRect.yMin += yOffset;
			filterRect.yMax -= yOffset;

			for (int i = 0, count = _tempComponents.Count; i < count; ++i)
			{
				int column = i / rows;
				int row    = i - rows * column;

				ComponentInfo component = _tempComponents[i];

				Rect componentRect = filterRect;
				componentRect.width /= columns;
				componentRect.height = lineHeight;
				componentRect.x += componentRect.width * column;
				componentRect.y += componentRect.height * row;

				rpcs.ComponentFilter.TryGetValue(component, out EFilterMode filterMode);

				if (EditorGUI.ToggleLeft(componentRect, component.TypeName, false, GUIStyles.ActiveToggle) == true)
				{
					filterMode = (EFilterMode)(((int)filterMode + 1) % FusionInspector.FILTER_MODES);
					rpcs.ComponentFilter[component] = filterMode;
					++rpcs.ComponentFilterVersion;
				}

				if (filterMode == EFilterMode.Include)
				{
					rpcs.ComponentFilterInclude.Add(component);
					EditorGUI.LabelField(componentRect, GUISymbols.CHECKMARK);
				}
				else if (filterMode == EFilterMode.Exclude)
				{
					rpcs.ComponentFilterExclude.Add(component);
					EditorGUI.LabelField(componentRect, GUISymbols.CROSS);
				}
			}

			_tempComponents.Clear();
		}

		private static string GetHierarchyPath(Transform transform)
		{
			if (transform == null)
				return "";

			string path = transform.name;

			Transform parent = transform.parent;
			while (parent != null)
			{
				path = $"{parent.name}/{path}";
				parent = parent.parent;
			}

			return path;
		}

		private static string GetTickLabel(int tick)
		{
			if (_tickLabels.TryGetValue(tick, out string label) == false)
			{
				label = tick.ToString();
				_tickLabels.Add(tick, label);
			}

			return label;
		}

		private static bool IsSameEvent(in RPCEvent e1, in RPCEvent e2)
		{
			return e1.Frame == e2.Frame && e1.Tick == e2.Tick && e1.Type == e2.Type && e1.Method == e2.Method && ReferenceEquals(e1.Behaviour, e2.Behaviour) == true && e1.SourcePlayer == e2.SourcePlayer && e1.TargetPlayer == e2.TargetPlayer;
		}

		private void ToggleSelectedEvent(in RPCEvent rpcEvent)
		{
			if (_hasSelectedEvent == true && IsSameEvent(rpcEvent, _selectedEvent) == true)
			{
				_hasSelectedEvent = false;
				_selectedEvent    = default;
			}
			else
			{
				_hasSelectedEvent = true;
				_selectedEvent    = rpcEvent;
			}
		}

		private static string GetResultTooltip(RpcSendMessageResult result)
		{
			switch (result)
			{
#if FUSION_2_1_OR_NEWER
				case RpcSendMessageResult.TargetObjectNotInPlayerInterest:                 return "Not sent - the target object is not in the target player's interest.";
				case RpcSendMessageResult.TargetPlayerNotAvailable:                        return "Not sent - the target player is not available.";
				case RpcSendMessageResult.NotInvokableDuringResim:                         return "Not sent - RPCs cannot be invoked during resimulation.";
				case RpcSendMessageResult.PayloadSizeExceeded:                             return "Not sent - the RPC payload exceeds the maximum allowed size.";
				case RpcSendMessageResult.InsufficientSourceAuthority:                     return "Not sent - the local peer does not match the declared RpcSources (insufficient source authority).";
				case RpcSendMessageResult.TargetPlayerIsLocalPlayer:                       return "Not sent - the target player is the local player, but the RPC is not invokable locally.";
				case RpcSendMessageResult.NoActiveConnections:                             return "Not sent - there are no active connections.";
				case RpcSendMessageResult.InsufficientTargetAuthority:                     return "Not sent - the target does not match the declared RpcTargets (insufficient target authority).";
				case RpcSendMessageResult.NoConnectionsWithAuthorityOrInterest:            return "Not sent - no connected peer has matching authority or interest in the object.";
#else
				case RpcSendMessageResult.NotSentTargetObjectNotInPlayerInterest:          return "Not sent - the target object is not in the target player's interest.";
				case RpcSendMessageResult.NotSentTargetClientNotAvailable:                 return "Not sent - the target player is not available.";
				case RpcSendMessageResult.NotSentBroadcastNoActiveConnections:             return "Not sent - broadcast RPC with no active connections.";
				case RpcSendMessageResult.NotSentBroadcastNoConfirmedNorInterestedClients: return "Not sent - no connected client has confirmed the object or has interest in it.";
#endif
			}

			if (_resultTooltips.TryGetValue((int)result, out string tooltip) == false)
			{
				tooltip = result.ToString();
				_resultTooltips.Add((int)result, tooltip);
			}

			return tooltip;
		}

		private static void DrawPlayerLabel(Rect rect, PlayerRef player, PlayerRef localPlayer)
		{
			if (player == localPlayer && player.IsNone == false)
			{
				GUIColor.Set(Color.green);
				EditorGUI.LabelField(rect, GetPlayerLabel(player), GUIStyles.RightLabel);
				GUIColor.Reset();
			}
			else
			{
				EditorGUI.LabelField(rect, GetPlayerLabel(player), GUIStyles.RightLabel);
			}
		}

		private static string GetPlayerLabel(PlayerRef player)
		{
			if (player == PlayerRef.None)
				return "";

			if (player.IsRealPlayer == true)
			{
				if (_playerLabels.TryGetValue(player.PlayerId, out string label) == false)
				{
					label = $"Player {player.PlayerId}";
					_playerLabels.Add(player.PlayerId, label);
				}

				return label;
			}

#if FUSION_2_1_OR_NEWER
			if (player.IsInternalMasterClientIdentifier == true)
				return "Master";
#else
			if (player.IsMasterClient == true)
				return "Master";
#endif

			return "Invalid";
		}

		private void DrawFieldsGUI(ref Rect sectionRect, RPCs rpcs, ERPCField visibleFields, ERPCField toggleableFields)
		{
			if (rpcs.OverviewVisibleFieldsFoldout == false)
				return;

			float xOffset = 3.0f;
			float yOffset = 6.0f;

			Rect innerRect = sectionRect;
			innerRect.xMin += xOffset;
			innerRect.xMax -= xOffset;
			innerRect.height = EditorGUIUtility.singleLineHeight;

			float totalWidth      = _fieldsWidth1 + _fieldsWidth2;
			float widthMultiplier = Mathf.Min(1.0f, innerRect.width / totalWidth);

			//----------------------------------------------------------------

			Rect configRect = innerRect;
			configRect.xMin = innerRect.xMin;
			configRect.xMax = configRect.xMin + _fieldsWidth1 * widthMultiplier;

			configRect.y += yOffset;

			DrawCategoryToggle(ref configRect, "Configuration", ERPCField.Configuration);
			configRect.y += yOffset;

			DrawToggle(ref configRect, "Sources",           _sourcesStat.Content.tooltip,       ERPCField.Sources);
			DrawToggle(ref configRect, "Targets",           _targetsStat.Content.tooltip,       ERPCField.Targets);
			DrawToggle(ref configRect, "Is Reliable",       _isReliableStat.Content.tooltip,    ERPCField.IsReliable);
			DrawToggle(ref configRect, "Is Tick Aligned",   _isTickAlignedStat.Content.tooltip, ERPCField.IsTickAligned);
			DrawToggle(ref configRect, "Has Player Target", _hasRpcTargetStat.Content.tooltip,  ERPCField.HasRpcTarget);
			DrawToggle(ref configRect, "Invoke Local",      _invokeLocalStat.Content.tooltip,   ERPCField.InvokeLocal);

			configRect.y += yOffset;

			//----------------------------------------------------------------

			Rect runtimeRect = innerRect;
			runtimeRect.xMin = configRect.xMax + xOffset;
			runtimeRect.xMax = runtimeRect.xMin + _fieldsWidth2 * widthMultiplier;

			runtimeRect.y += yOffset;

			DrawCategoryToggle(ref runtimeRect, "Runtime", ERPCField.Runtime);
			runtimeRect.y += yOffset;

			DrawToggle(ref runtimeRect, "Sent",     _sentStat.Content.tooltip,     ERPCField.Sent);
			DrawToggle(ref runtimeRect, "Received", _receivedStat.Content.tooltip, ERPCField.Received);

			runtimeRect.y += yOffset;

			//----------------------------------------------------------------

			sectionRect.yMin = Mathf.Max(configRect.yMin, runtimeRect.yMin);

			return;

			void DrawCategoryToggle(ref Rect rect, string label, ERPCField categoryFields)
			{
				ERPCField enabledFields = rpcs.OverviewVisibleFields & categoryFields;

				bool allEnabled = enabledFields == categoryFields;

				EditorGUI.BeginDisabledGroup((toggleableFields & categoryFields) == ERPCField.None);

				EditorGUI.showMixedValue = allEnabled == false && enabledFields != ERPCField.None;
				bool toggleState = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width - FusionInspector.CategorySpacing * widthMultiplier, rect.height), label, allEnabled, EditorStyles.boldLabel);
				EditorGUI.showMixedValue = false;

				EditorGUI.EndDisabledGroup();

				if (toggleState != allEnabled)
				{
					rpcs.SetVisibleFields(categoryFields, toggleState);
				}

				rect.y += rect.height;

				TextureUtility.DrawTexture(new Rect(rect.x, rect.y + 2.0f, rect.width - FusionInspector.CategorySpacing * widthMultiplier, FusionInspector.SeparatorSize), FusionInspector.SeparatorColor);
			}

			void DrawToggle(ref Rect rect, string label, string tooltip, ERPCField field)
			{
				_toggleContent.text    = label;
				_toggleContent.tooltip = tooltip;

				EditorGUI.BeginDisabledGroup(toggleableFields.Has(field) == false);

				bool previousState = rpcs.OverviewVisibleFields.Has(field);
				bool currentState  = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width - FusionInspector.CategorySpacing * widthMultiplier, rect.height), _toggleContent, previousState);

				EditorGUI.EndDisabledGroup();

				if (currentState != previousState)
				{
					rpcs.ToggleVisibleField(field);
				}

				rect.y += rect.height;
			}
		}

		private static void DrawLabelHeader(GUIStat stat, ref Rect rect)
		{
			rect.xMin += rect.width;
			rect.width = stat.Width;

			GUISeparators.AddX(rect.xMin);

			EditorGUI.LabelField(rect, stat.Content, stat.Style);
		}

		private static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			FusionInspector.DrawHeader(stat, ref rect, ref filter);
		}

		private static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref ERPCSortMode sortMode, ERPCSortMode primarySortMode, ERPCSortMode secondarySortMode)
		{
			int intSortMode = (int)sortMode;
			FusionInspector.DrawHeader(stat, icon, ref rect, ref intSortMode, (int)primarySortMode, (int)secondarySortMode);
			sortMode = (ERPCSortMode)intSortMode;
		}

		private static void ToggleSortMode(ref ERPCSortMode sortMode, ERPCSortMode primaryMode, ERPCSortMode secondaryMode)
		{
			sortMode = sortMode == primaryMode ? secondaryMode : primaryMode;
		}

		private static void DrawFlags(Rect rect, GUIContent[] flagContents, GUIContent[] excludedFlagContents, bool hasStateAuthority, bool hasInputAuthority, bool hasProxies)
		{
			Rect slotRect = rect;
			slotRect.xMin  = rect.xMax - _flagSlotWidth * 3.0f - 3.0f;
			slotRect.width = _flagSlotWidth;

			DrawFlag(slotRect, flagContents[0], excludedFlagContents[0], hasStateAuthority, FusionInspector.StateAuthorityColor);
			slotRect.x += _flagSlotWidth;
			DrawFlag(slotRect, flagContents[1], excludedFlagContents[1], hasInputAuthority, FusionInspector.InputAuthorityColor);
			slotRect.x += _flagSlotWidth;
			DrawFlag(slotRect, flagContents[2], excludedFlagContents[2], hasProxies, FusionInspector.ProxyColor);
		}

		private static void DrawFlag(Rect rect, GUIContent flagContent, GUIContent excludedFlagContent, bool isIncluded, Color includedColor)
		{
			if (isIncluded == true)
			{
				GUIColor.Set(includedColor);
				EditorGUI.LabelField(rect, flagContent, GUIStyles.Icon);
				GUIColor.Reset();
			}
			else
			{
				GUIColor.Set(FusionInspector.ExcludedColor);
				EditorGUI.LabelField(rect, excludedFlagContent, GUIStyles.Icon);
				GUIColor.Reset();
			}
		}
	}
}
