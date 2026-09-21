namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	internal sealed class SceneObjectsInspector
	{
		private Vector2                _scrollPosition;
		private double                 _selectionTime;
		private List<SceneObjectInfo>  _selection = new List<SceneObjectInfo>();
		private object                 _scriptSelection;
		private double                 _scriptSelectionTime;
		private Vector2                _scriptScrollPosition;
		private MemoryLabel            _totalSelectionChanges = new MemoryLabel(true);
		private IntLabel               _totalComponents = new IntLabel();
		private MemoryLabel            _totalStateSize = new MemoryLabel(false);
		private MemoryLabel            _totalStateChangesSum = new MemoryLabel(true);
		private MemoryPerSecondLabel   _totalStateChangesAverage = new MemoryPerSecondLabel();
		private HashSet<ComponentInfo> _tempScripts = new HashSet<ComponentInfo>();
		private List<ComponentInfo>    _tempComponents = new List<ComponentInfo>();

		private GUINameStat _objectStat;
		private GUISortStat _componentsStat;
		private GUIIconStat _isMasterClientObjectStat;
		private GUIIconStat _allowStateAuthorityOverrideStat;
		private GUIIconStat _destroyWhenStateAuthorityLeavesStat;
		private GUISortStat _interestModeStat;

		private GUIIconStat _isInSimulationStat;
		private GUIIconStat _hasStateAuthorityStat;
		private GUIIconStat _hasInputAuthorityStat;
		private GUISortStat _networkIdStat;
		private GUISortStat _stateAuthorityStat;
		private GUISortStat _inputAuthorityStat;
		private GUISortStat _totalStateChangesStat;
		private GUISortStat _averageStateChangesStat;
		private GUISortStat _stateSizeStat;
		private GUISortStat _distanceStat;

		private GUIContent   _objectContent;
		private GUIContent   _componentsContent;
		private GUIContent   _componentsIconContent;
		private GUIContent[] _interestModeContents;
		private GUIContent   _localStateAuthorityContent;
		private GUIContent   _localInputAuthorityContent;
		private GUIContent   _stateSizeContent;
		private GUIContent   _filterContent;
		private GUIContent   _fieldsContent;
		private GUIContent   _scriptContent;
		private GUIContent   _toggleContent;
		private GUIContent   _totalContent;
		private GUIContent   _stateAuthorityFilterContent;
		private GUIContent   _inputAuthorityFilterContent;
		private GUIContent   _interestModeFilterContent;
		private GUIContent   _removeFilterContent;

		private PlayerRef      _stateAuthorityFilterPlayer;
		private PlayerRef      _inputAuthorityFilterPlayer;
		private int            _interestModeFilterMode = -1;
		private PlayerRefLabel _authorityFilterLabel = new PlayerRefLabel();

		private float _sortWidth;
		private float _fieldsWidth1;
		private float _fieldsWidth2;
		private float _fieldsWidth3;
		private float _localDotWidth;

		public SceneObjectsInspector()
		{
			Texture2D gameObjectIcon = EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D;
			Texture2D scriptIcon     = EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D;
			Texture2D filterIcon     = EditorGUIUtility.IconContent("d_FilterByType@2x").image as Texture2D;
			Texture2D fieldsIcon     = EditorGUIUtility.IconContent("d__Menu@2x").image as Texture2D;

			float headerHeight   = FusionInspector.HeaderHeight;
			float stateSizeWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent("8888 B")).x;

			_objectStat = new GUINameStat(GUIStyles.LeftBoldLabel, "Object", "Name of the object", (int)ESceneObjectSortMode.NameAscending, (int)ESceneObjectSortMode.NameDescending);

			_componentsStat                      = new GUISortStat(GUIStyles.RightBoldLabel, "     ", "# of NetworkBehaviour components on the object", (int)ESceneObjectSortMode.ComponentsAscending, (int)ESceneObjectSortMode.ComponentsDescending);
			_isMasterClientObjectStat            = new GUIIconStat(GUIStyles.Icon, "❖", "✔", $"Is Master Client Object (Shared Mode)", headerHeight);
			_allowStateAuthorityOverrideStat     = new GUIIconStat(GUIStyles.Icon, "✪", "✔", $"Allow State Authority Override (Shared Mode)", headerHeight);
			_destroyWhenStateAuthorityLeavesStat = new GUIIconStat(GUIStyles.Icon, "☢", "✔", $"Destroy When State Authority Leaves (Shared Mode)", headerHeight);
			_interestModeStat                    = new GUISortStat(GUIStyles.RightBoldLabel, "Interest", "Object Interest Mode", (int)ESceneObjectSortMode.InterestModeAscending, (int)ESceneObjectSortMode.InterestModeDescending);

			_isInSimulationStat      = new GUIIconStat(GUIStyles.Icon, "☀", "✔", $"Is In Simulation", headerHeight);
			_hasStateAuthorityStat   = new GUIIconStat(GUIStyles.Icon, "◈", "✔", $"Has State Authority", headerHeight);
			_hasInputAuthorityStat   = new GUIIconStat(GUIStyles.Icon, "⦿", "✔", $"Has Input Authority", headerHeight);
			_networkIdStat           = new GUISortStat(GUIStyles.RightBoldLabel, "Network Id", "NetworkId of the network object", (int)ESceneObjectSortMode.NetworkIdAscending, (int)ESceneObjectSortMode.NetworkIdDescending);
			_stateAuthorityStat      = new GUISortStat(GUIStyles.RightBoldLabel, "S-Authority", "State authority of the network object (PlayerRef)", (int)ESceneObjectSortMode.StateAuthorityAscending, (int)ESceneObjectSortMode.StateAuthorityDescending);
			_inputAuthorityStat      = new GUISortStat(GUIStyles.RightBoldLabel, "I-Authority", "Input authority of the network object (PlayerRef)", (int)ESceneObjectSortMode.InputAuthorityAscending, (int)ESceneObjectSortMode.InputAuthorityDescending);
			_totalStateChangesStat   = new GUISortStat(GUIStyles.RightBoldLabel, "Σ Changes", "Total changes in networked state in Bytes", (int)ESceneObjectSortMode.TotalStateChangesAscending, (int)ESceneObjectSortMode.TotalStateChangesDescending);
			_averageStateChangesStat = new GUISortStat(GUIStyles.RightBoldLabel, "Ø Changes", "Average changes in networked state in Bytes per second", (int)ESceneObjectSortMode.AverageStateChangesAscending, (int)ESceneObjectSortMode.AverageStateChangesDescending);
			_stateSizeStat           = new GUISortStat(GUIStyles.RightBoldLabel, "State", "Size of the networked state of the object in Bytes", (int)ESceneObjectSortMode.StateSizeAscending, (int)ESceneObjectSortMode.StateSizeDescending, stateSizeWidth);
			_distanceStat            = new GUISortStat(GUIStyles.RightBoldLabel, "Distance", "Distance from Player Object / Camera", (int)ESceneObjectSortMode.DistanceAscending, (int)ESceneObjectSortMode.DistanceDescending);

			_objectContent              = new GUIContent(gameObjectIcon);
			_componentsContent          = new GUIContent("", "# of NetworkBehaviour components on the object");
			_componentsIconContent      = new GUIContent(scriptIcon, "# of NetworkBehaviour components on the object");
			_interestModeContents       = new GUIContent[] { new GUIContent("Area"), new GUIContent("Global"), new GUIContent("Explicit") };
			_localStateAuthorityContent = new GUIContent("•", "Local player has state authority over this object");
			_localInputAuthorityContent = new GUIContent("•", "Local player has input authority over this object");
			_stateSizeContent           = new GUIContent("", "Size of the networked state of the object in Bytes");
			_filterContent              = new GUIContent(filterIcon, "Filter by component type");
			_fieldsContent              = new GUIContent(fieldsIcon, "Visible fields");
			_scriptContent              = new GUIContent(scriptIcon);
			_toggleContent              = new GUIContent();
			_totalContent               = new GUIContent("Total", "Totals for all listed objects");

			_stateAuthorityFilterContent = new GUIContent();
			_inputAuthorityFilterContent = new GUIContent();
			_interestModeFilterContent   = new GUIContent();
			_removeFilterContent         = new GUIContent(GUISymbols.CROSS, "Remove filter");

			_sortWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"{GUISymbols.ARROW_UP} ")).x - GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"")).x;

			_componentsStat.Width          += _sortWidth;
			_interestModeStat.Width        += _sortWidth;
			_networkIdStat.Width           += _sortWidth;
			_stateAuthorityStat.Width      += _sortWidth;
			_inputAuthorityStat.Width      += _sortWidth;
			_totalStateChangesStat.Width   += _sortWidth;
			_averageStateChangesStat.Width += _sortWidth;
			_stateSizeStat.Width           += _sortWidth;
			_distanceStat.Width            += _sortWidth;

			float playerLabelWidth = GUIStyles.RightLabel.CalcSize(new GUIContent(" Player 8888 ")).x;

			_stateAuthorityStat.Width = Mathf.Max(_stateAuthorityStat.Width, playerLabelWidth);
			_inputAuthorityStat.Width = Mathf.Max(_inputAuthorityStat.Width, playerLabelWidth);

			_fieldsWidth1  = FusionInspector.GetCategoryWidth("Configuration", "Components (NB)", "Is Master Client Object", "Allow State Authority Override", "Destroy When State Authority Leaves", "Object Interest Mode");
			_fieldsWidth2  = FusionInspector.GetCategoryWidth("Runtime", "Is In Simulation", "Has State Authority", "Has Input Authority", "Network Id", "State Authority", "Input Authority", "Σ State Changes", "Ø State Changes");
			_fieldsWidth3  = FusionInspector.GetCategoryWidth("Other", "State Size", "Distance");
			_localDotWidth = GUIStyles.Dot.CalcSize(_localStateAuthorityContent).x;
		}

		public void DrawGUI(Rect window, Rect region, FusionInspectorDB db)
		{
			GameMode          gameMode         = db.CurrentRunner != null ? db.CurrentRunner.GameMode : default;
			ESceneObjectField visibleFields    = db.SceneObjects.VisibleFields | ESceneObjectField.Object;
			ESceneObjectField toggleableFields = ~ESceneObjectField.Object;

			bool showRuntimeInfo = gameMode != default;
			if (showRuntimeInfo == false)
			{
				visibleFields &= ~ESceneObjectField.IsInSimulation;
				visibleFields &= ~ESceneObjectField.HasStateAuthority;
				visibleFields &= ~ESceneObjectField.HasInputAuthority;
				visibleFields &= ~ESceneObjectField.NetworkId;
				visibleFields &= ~ESceneObjectField.StateAuthority;
				visibleFields &= ~ESceneObjectField.InputAuthority;
				visibleFields &= ~ESceneObjectField.TotalStateChanges;
				visibleFields &= ~ESceneObjectField.AverageStateChanges;

				toggleableFields &= ~ESceneObjectField.IsInSimulation;
				toggleableFields &= ~ESceneObjectField.HasStateAuthority;
				toggleableFields &= ~ESceneObjectField.HasInputAuthority;
				toggleableFields &= ~ESceneObjectField.NetworkId;
				toggleableFields &= ~ESceneObjectField.StateAuthority;
				toggleableFields &= ~ESceneObjectField.InputAuthority;
				toggleableFields &= ~ESceneObjectField.TotalStateChanges;
				toggleableFields &= ~ESceneObjectField.AverageStateChanges;
			}

			bool showSharedModeInfo = gameMode == default || gameMode == GameMode.Shared;
			if (showSharedModeInfo == false)
			{
				visibleFields &= ~ESceneObjectField.IsMasterClientObject;
				visibleFields &= ~ESceneObjectField.AllowStateAuthorityOverride;
				visibleFields &= ~ESceneObjectField.DestroyWhenStateAuthorityLeaves;

				toggleableFields &= ~ESceneObjectField.IsMasterClientObject;
				toggleableFields &= ~ESceneObjectField.AllowStateAuthorityOverride;
				toggleableFields &= ~ESceneObjectField.DestroyWhenStateAuthorityLeaves;
			}

			DrawSceneObjectsGUI(window, ref region, db, visibleFields, toggleableFields);

			region.yMin += Mathf.Clamp(region.height, 0.0f, 2.0f);

			DrawSelectionGUI(window, ref region, db);
		}

		private void DrawSceneObjectsGUI(Rect window, ref Rect region, FusionInspectorDB db, ESceneObjectField visibleFields, ESceneObjectField toggleableFields)
		{
			SceneObjects sceneObjects = db.SceneObjects;
			if (sceneObjects.All.Count <= 0)
				return;

			int minSelection = int.MaxValue;
			int maxSelection = int.MinValue;

			_selection.Clear();
			for (int i = 0, count = sceneObjects.Filtered.Count; i < count; ++i)
			{
				SceneObjectInfo sceneObject = sceneObjects.Filtered[i];
				if (sceneObject.IsSelected == true)
				{
					_selection.Add(sceneObject);

					minSelection = Mathf.Min(minSelection, i);
					maxSelection = Mathf.Max(maxSelection, i);
				}
			}

			Rect content = region;

			float selectionRegionHeight = Mathf.Clamp(region.height - FusionInspector.HeaderHeight - FusionInspector.LineHeight, 0.0f, region.height * FusionInspector.SelectionSize);
			if (selectionRegionHeight > 0.0f && _selection.Count > 0)
			{
				region.yMin = region.yMax - selectionRegionHeight;
				content.height -= selectionRegionHeight;
			}

			bool showComponents                      = visibleFields.Has(ESceneObjectField.Components);
			bool showIsMasterClientObject            = visibleFields.Has(ESceneObjectField.IsMasterClientObject);
			bool showAllowStateAuthorityOverride     = visibleFields.Has(ESceneObjectField.AllowStateAuthorityOverride);
			bool showDestroyWhenStateAuthorityLeaves = visibleFields.Has(ESceneObjectField.DestroyWhenStateAuthorityLeaves);
			bool showInterestMode                    = visibleFields.Has(ESceneObjectField.InterestMode);

			bool showIsInSimulation                  = visibleFields.Has(ESceneObjectField.IsInSimulation);
			bool showHasStateAuthority               = visibleFields.Has(ESceneObjectField.HasStateAuthority);
			bool showHasInputAuthority               = visibleFields.Has(ESceneObjectField.HasInputAuthority);
			bool showNetworkId                       = visibleFields.Has(ESceneObjectField.NetworkId);
			bool showStateAuthority                  = visibleFields.Has(ESceneObjectField.StateAuthority);
			bool showInputAuthority                  = visibleFields.Has(ESceneObjectField.InputAuthority);
			bool showTotalStateChanges               = visibleFields.Has(ESceneObjectField.TotalStateChanges);
			bool showAverageStateChanges             = visibleFields.Has(ESceneObjectField.AverageStateChanges);
			bool showStateSize                       = visibleFields.Has(ESceneObjectField.StateSize);
			bool showDistance                        = visibleFields.Has(ESceneObjectField.Distance);

			GUISeparators.Clear();

			content.xMin += FusionInspector.LeftWindowOffset;
			content.xMax -= FusionInspector.RightWindowOffset;

			_objectStat.Width = content.width;

			if (showComponents                      == true) { _objectStat.Width -= _componentsStat.Width;                      }
			if (showIsMasterClientObject            == true) { _objectStat.Width -= _isMasterClientObjectStat.Width;            } else { sceneObjects.IsMasterClientObjectFilter            = default; }
			if (showAllowStateAuthorityOverride     == true) { _objectStat.Width -= _allowStateAuthorityOverrideStat.Width;     } else { sceneObjects.AllowStateAuthorityOverrideFilter     = default; }
			if (showDestroyWhenStateAuthorityLeaves == true) { _objectStat.Width -= _destroyWhenStateAuthorityLeavesStat.Width; } else { sceneObjects.DestroyWhenStateAuthorityLeavesFilter = default; }
			if (showInterestMode                    == true) { _objectStat.Width -= _interestModeStat.Width;                    }

			if (showIsInSimulation                  == true) { _objectStat.Width -= _isInSimulationStat.Width;                  } else { sceneObjects.IsInSimulationFilter                  = default; }
			if (showHasStateAuthority               == true) { _objectStat.Width -= _hasStateAuthorityStat.Width;               } else { sceneObjects.HasStateAuthorityFilter               = default; }
			if (showHasInputAuthority               == true) { _objectStat.Width -= _hasInputAuthorityStat.Width;               } else { sceneObjects.HasInputAuthorityFilter               = default; }
			if (showNetworkId                       == true) { _objectStat.Width -= _networkIdStat.Width;                       }
			if (showStateAuthority                  == true) { _objectStat.Width -= _stateAuthorityStat.Width;                  }
			if (showInputAuthority                  == true) { _objectStat.Width -= _inputAuthorityStat.Width;                  }
			if (showTotalStateChanges               == true) { _objectStat.Width -= _totalStateChangesStat.Width;               }
			if (showAverageStateChanges             == true) { _objectStat.Width -= _averageStateChangesStat.Width;             }
			if (showStateSize                       == true) { _objectStat.Width -= _stateSizeStat.Width;                       }
			if (showDistance                        == true) { _objectStat.Width -= _distanceStat.Width;                        }

			Rect headerRect = content;
			headerRect.width  = _objectStat.Width;
			headerRect.height = FusionInspector.HeaderHeight;

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, window.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			Rect componentFilterRect = headerRect;
			componentFilterRect.xMin = componentFilterRect.xMax - FusionInspector.HeaderHeight;
			GUIColor.Set(FusionInspector.FilterColors[Mathf.Min(sceneObjects.ComponentFilterInclude.Count + sceneObjects.ComponentFilterExclude.Count, 1)]);
			if (GUI.Button(componentFilterRect, _filterContent, GUIStyles.Icon) == true)
			{
				if (Event.current.button == 1)
				{
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Reset"), false, () =>
					{
						for (int i = 0, count = db.Components.All.Count; i < count; ++i)
						{
							db.Components.All[i].SceneObjects.FilterMode = EFilterMode.None;
						}
					});
					menu.ShowAsContext();
				}
				else
				{
					sceneObjects.ComponentFilterFoldout = !sceneObjects.ComponentFilterFoldout;
					sceneObjects.VisibleFieldsFoldout = false;
				}
			}
			GUIColor.Reset();

			GUIContent nameContent = _objectStat.GetContent(sceneObjects.All.Count, sceneObjects.Filtered.Count, _selection.Count, (int)sceneObjects.SortMode);

			Rect nameRect = headerRect;
			nameRect.xMin += _objectStat.IsSorted((int)sceneObjects.SortMode) ? 0.0f : _sortWidth;

			Rect nameFilterRect = headerRect;
			nameFilterRect.xMax = componentFilterRect.xMin;
			FusionInspector.ApplyFilterRect(ref nameFilterRect, nameRect.xMin + _objectStat.Style.CalcSize(nameContent).x);
			sceneObjects.ObjectNameFilter = FusionInspector.DrawFilterField(nameFilterRect, sceneObjects.ObjectNameFilter);

			nameRect.xMax = nameFilterRect.xMin;
			if (GUI.Button(nameRect, nameContent, _objectStat.Style) == true)
			{
				ToggleSortMode(ref sceneObjects.SortMode, ESceneObjectSortMode.NameAscending, ESceneObjectSortMode.NameDescending);
			}

			if (showComponents                      == true) { DrawHeader(_componentsStat, _componentsIconContent, ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.ComponentsDescending, ESceneObjectSortMode.ComponentsAscending); }
			if (showIsMasterClientObject            == true) { DrawHeader(_isMasterClientObjectStat,               ref headerRect, ref sceneObjects.IsMasterClientObjectFilter);            }
			if (showAllowStateAuthorityOverride     == true) { DrawHeader(_allowStateAuthorityOverrideStat,        ref headerRect, ref sceneObjects.AllowStateAuthorityOverrideFilter);     }
			if (showDestroyWhenStateAuthorityLeaves == true) { DrawHeader(_destroyWhenStateAuthorityLeavesStat,    ref headerRect, ref sceneObjects.DestroyWhenStateAuthorityLeavesFilter); }
			if (showInterestMode                    == true) { DrawHeader(_interestModeStat, null,                 ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.InterestModeAscending, ESceneObjectSortMode.InterestModeDescending); }

			if (showIsInSimulation                  == true) { DrawHeader(_isInSimulationStat,                     ref headerRect, ref sceneObjects.IsInSimulationFilter);    }
			if (showHasStateAuthority               == true) { DrawHeader(_hasStateAuthorityStat,                  ref headerRect, ref sceneObjects.HasStateAuthorityFilter); }
			if (showHasInputAuthority               == true) { DrawHeader(_hasInputAuthorityStat,                  ref headerRect, ref sceneObjects.HasInputAuthorityFilter); }
			if (showNetworkId                       == true) { DrawHeader(_networkIdStat, null,                    ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.NetworkIdAscending,            ESceneObjectSortMode.NetworkIdDescending);          }
			if (showStateAuthority                  == true) { DrawHeader(_stateAuthorityStat, null,               ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.StateAuthorityAscending,       ESceneObjectSortMode.StateAuthorityDescending);     }
			if (showInputAuthority                  == true) { DrawHeader(_inputAuthorityStat, null,               ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.InputAuthorityAscending,       ESceneObjectSortMode.InputAuthorityDescending);     }
			if (showTotalStateChanges               == true) { DrawHeader(_totalStateChangesStat, null,            ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.TotalStateChangesDescending,   ESceneObjectSortMode.TotalStateChangesAscending);   }
			if (showAverageStateChanges             == true) { DrawHeader(_averageStateChangesStat, null,          ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.AverageStateChangesDescending, ESceneObjectSortMode.AverageStateChangesAscending); }
			if (showStateSize                       == true) { DrawHeader(_stateSizeStat, null,                    ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.StateSizeDescending,           ESceneObjectSortMode.StateSizeAscending);           }
			if (showDistance                        == true) { DrawHeader(_distanceStat, null,                     ref headerRect, ref sceneObjects.SortMode, ESceneObjectSortMode.DistanceAscending,             ESceneObjectSortMode.DistanceDescending);           }

			headerRect.xMin += headerRect.width;
			headerRect.width = FusionInspector.RightWindowOffset;
			if (GUI.Button(headerRect, _fieldsContent, GUIStyles.Icon) == true)
			{
				sceneObjects.VisibleFieldsFoldout = !sceneObjects.VisibleFieldsFoldout;
				sceneObjects.ComponentFilterFoldout = false;
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			content.yMin += headerRect.height;

			DrawFieldsGUI(ref content, sceneObjects, visibleFields, toggleableFields);
			DrawComponentFilterGUI(ref content, sceneObjects, db.Components);
			DrawFilterBarGUI(window, ref content, sceneObjects);

			GUISeparators.AddY(content.y);

			// One line is reserved for the always visible Total row below the list.
			content.yMax -= FusionInspector.LineHeight + 2.0f;

			Rect scrollRect     = new Rect(content.x, content.y, content.width + FusionInspector.RightWindowOffset, content.height);
			Rect scrollViewRect = new Rect(0.0f, 0.0f, content.width, FusionInspector.LineHeight * sceneObjects.Filtered.Count);

			GUISeparators.AddX(headerRect.xMin);

			_scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, scrollViewRect);

			float   minScrollY    = _scrollPosition.y;
			float   maxScrollY    = _scrollPosition.y + content.height;
			Rect    drawRect      = new Rect(0.0f, 0.0f, 0.0f, FusionInspector.LineHeight);
			Vector2 mousePosition = Event.current.mousePosition;

			for (int i = 0, count = sceneObjects.Filtered.Count; i < count; ++i)
			{
				if (drawRect.yMax < minScrollY)
				{
					drawRect.y += drawRect.height;
					continue;
				}
				else if (drawRect.y > maxScrollY)
				{
					break;
				}

				SceneObjectInfo sceneObject = sceneObjects.Filtered[i];

				drawRect.xMin  = 0.0f;
				drawRect.width = _objectStat.Width;

				if (sceneObject.IsSelected == true)
				{
					Rect selectionRect = drawRect;
					selectionRect.xMin = 0.0f;
					selectionRect.xMax = window.width;
					selectionRect.yMin = Mathf.Max(minScrollY, selectionRect.yMin);
					selectionRect.yMax = Mathf.Min(selectionRect.yMax, maxScrollY);
					TextureUtility.DrawTexture(selectionRect, FusionInspector.SelectionColor);

					selectionRect.width = scrollRect.x;
					selectionRect.y += scrollRect.y - _scrollPosition.y;
					GUIHighlights.Add(selectionRect, FusionInspector.SelectionColor);
				}
				else if (mousePosition.x >= 0.0f && mousePosition.x <= content.width && mousePosition.y >= minScrollY && mousePosition.y <= maxScrollY && mousePosition.y > drawRect.yMin && mousePosition.y < drawRect.yMax)
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

				GUIColor.Set(sceneObject.IsActive == true ? FusionInspector.ActiveTextColor : FusionInspector.InactiveTextColor);
				_objectContent.text = sceneObject.Name;
				drawRect.height += 1.0f; // GUI.Button dead zone fix
				if (GUI.Button(drawRect, _objectContent, GUIStyles.ObjectField) == true)
				{
					if (Event.current.button == 1)
					{
						GenericMenu menu = new GenericMenu();

						if (_selection.Count > 1 && _selection.Contains(sceneObject) == true)
						{
							List<string> names = new List<string>();
							List<string> paths = new List<string>();

							for (int j = 0; j < _selection.Count; ++j)
							{
								names.Add(_selection[j].Name);
								paths.Add(GetHierarchyPath(_selection[j].Transform));
							}

							menu.AddItem(new GUIContent("Copy Names"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, string.Join("\n", names));
							menu.AddItem(new GUIContent("Copy Paths"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, string.Join("\n", paths));
						}
						else
						{
							string objectName = sceneObject.Name;
							menu.AddItem(new GUIContent($"Filter Name: {objectName}"), sceneObjects.ObjectNameFilter == objectName, () => { sceneObjects.ObjectNameFilter = sceneObjects.ObjectNameFilter == objectName ? default : objectName; });

							PlayerRef stateAuthority = sceneObject.StateAuthority.Value;
							PlayerRef inputAuthority = sceneObject.InputAuthority.Value;

							if (stateAuthority.IsNone == false)
							{
								menu.AddItem(new GUIContent($"Filter State Authority: {sceneObject.StateAuthority.GetLabel()}"), sceneObjects.StateAuthorityFilter == stateAuthority, () => { sceneObjects.StateAuthorityFilter = sceneObjects.StateAuthorityFilter == stateAuthority ? PlayerRef.None : stateAuthority; });
							}

							if (inputAuthority.IsNone == false)
							{
								menu.AddItem(new GUIContent($"Filter Input Authority: {sceneObject.InputAuthority.GetLabel()}"), sceneObjects.InputAuthorityFilter == inputAuthority, () => { sceneObjects.InputAuthorityFilter = sceneObjects.InputAuthorityFilter == inputAuthority ? PlayerRef.None : inputAuthority; });
							}

							int interestMode = sceneObject.InterestMode;
							menu.AddItem(new GUIContent($"Filter Interest: {_interestModeContents[interestMode].text}"), sceneObjects.InterestModeFilter == interestMode, () => { sceneObjects.InterestModeFilter = sceneObjects.InterestModeFilter == interestMode ? -1 : interestMode; });

							menu.AddSeparator("");
							menu.AddItem(new GUIContent("Copy Name"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, sceneObject.Name);
							menu.AddItem(new GUIContent("Copy Path"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, GetHierarchyPath(sceneObject.Transform));
						}

						menu.ShowAsContext();
					}
					else
					{
						if (_selection.Count == 1 && _selection.Contains(sceneObject) == true && EditorApplication.timeSinceStartup < (_selectionTime + FusionInspector.SelectionDelay))
						{
							_selectionTime = default;

							Selection.SetActiveObjectWithContext(sceneObject.GameObject, sceneObject.GameObject);
						}
						else
						{
							db.Components.ClearSelectedStateChanges();

							if (Event.current.shift == true)
							{
								minSelection = Mathf.Min(minSelection, i);
								maxSelection = Mathf.Max(maxSelection, i);

								for (int j = minSelection; j <= maxSelection; ++j)
								{
									sceneObjects.Filtered[j].IsSelected = true;
								}

								EditorGUIUtility.PingObject(sceneObject.GameObject);
							}
							else if (Event.current.control == true)
							{
								sceneObject.IsSelected = !sceneObject.IsSelected;
								if (sceneObject.IsSelected == true)
								{
									EditorGUIUtility.PingObject(sceneObject.GameObject);
								}
							}
							else
							{
								for (int j = 0; j < _selection.Count; ++j)
								{
									_selection[j].IsSelected = false;
								}

								if (_selection.Count != 1 || _selection.Contains(sceneObject) == false)
								{
									sceneObject.IsSelected = true;
									EditorGUIUtility.PingObject(sceneObject.GameObject);
								}
							}

							_selectionTime = EditorApplication.timeSinceStartup;
						}
					}
				}
				drawRect.height -= 1.0f; // GUI.Button dead zone fix
				GUIColor.Reset();

				if ((sceneObject.HasStateAuthority == true || sceneObject.HasInputAuthority == true) && drawRect.yMin >= minScrollY && drawRect.yMax <= maxScrollY)
				{
					Rect dotRect = drawRect;
					dotRect.xMin  = drawRect.xMax - _localDotWidth - 3.0f;
					dotRect.width = _localDotWidth;

					if (sceneObject.HasStateAuthority == true)
					{
						GUIColor.Set(Color.green);
						EditorGUI.LabelField(dotRect, _localStateAuthorityContent, GUIStyles.Dot);
						GUIColor.Reset();
					}

					dotRect.x -= _localDotWidth - 4.0f;

					if (sceneObject.HasInputAuthority == true)
					{
						GUIColor.Set(Color.yellow);
						EditorGUI.LabelField(dotRect, _localInputAuthorityContent, GUIStyles.Dot);
						GUIColor.Reset();
					}
				}

				if (showComponents == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _componentsStat.Width;

					if (sceneObject.ComponentCount.Value > 0)
					{
						_componentsContent.text = sceneObject.ComponentCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _componentsContent, GUIStyles.RightLabel) == true)
						{
							if (sceneObject.Components.Length > 0)
							{
								GenericMenu menu = new GenericMenu();
								for (int componentIndex = 0; componentIndex < sceneObject.Components.Length; ++componentIndex)
								{
									ComponentInfo component = sceneObject.Components[componentIndex];
									menu.AddItem(new GUIContent($"ID:[{ObjectUtility.GetObjectId(sceneObject.NetworkObject.NetworkedBehaviours[componentIndex])}] {component.TypeName}"), false, (script) =>
									{
										MonoScript monoScript = script as MonoScript;
										if (monoScript != null)
										{
											AssetDatabase.OpenAsset(monoScript);
										}
									}, component.Script);
								}
								menu.ShowAsContext();
							}
						}
						drawRect.xMax += 2.0f;
					}
				}

				if (showIsMasterClientObject            == true) { FusionInspector.DrawIcon(_isMasterClientObjectStat,            ref drawRect, sceneObject.IsMasterClientObject);            }
				if (showAllowStateAuthorityOverride     == true) { FusionInspector.DrawIcon(_allowStateAuthorityOverrideStat,     ref drawRect, sceneObject.AllowStateAuthorityOverride);     }
				if (showDestroyWhenStateAuthorityLeaves == true) { FusionInspector.DrawIcon(_destroyWhenStateAuthorityLeavesStat, ref drawRect, sceneObject.DestroyWhenStateAuthorityLeaves); }

				if (showInterestMode == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _interestModeStat.Width;

					EditorGUI.LabelField(drawRect, _interestModeContents[(int)sceneObject.InterestMode], GUIStyles.RightLabel);

					if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && drawRect.Contains(Event.current.mousePosition) == true)
					{
						int interestMode = sceneObject.InterestMode;

						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent($"Filter Interest: {_interestModeContents[interestMode].text}"), sceneObjects.InterestModeFilter == interestMode, () => { sceneObjects.InterestModeFilter = sceneObjects.InterestModeFilter == interestMode ? -1 : interestMode; });
						menu.ShowAsContext();

						Event.current.Use();
					}
				}

				if (showIsInSimulation    == true) { FusionInspector.DrawIcon(_isInSimulationStat,    ref drawRect, sceneObject.IsInSimulation);    }
				if (showHasStateAuthority == true) { FusionInspector.DrawIcon(_hasStateAuthorityStat, ref drawRect, sceneObject.HasStateAuthority); }
				if (showHasInputAuthority == true) { FusionInspector.DrawIcon(_hasInputAuthorityStat, ref drawRect, sceneObject.HasInputAuthority); }

				if (showNetworkId == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _networkIdStat.Width;

					EditorGUI.LabelField(drawRect, sceneObject.NetworkId.GetLabel(), GUIStyles.RightLabel);
				}

				if (showStateAuthority == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _stateAuthorityStat.Width;

					if (sceneObject.StateAuthority.Value != PlayerRef.None)
					{
						if (sceneObject.HasStateAuthority == true)
						{
							GUIColor.Set(Color.green);
							EditorGUI.LabelField(drawRect, sceneObject.StateAuthority.GetLabel(), GUIStyles.RightLabel);
							GUIColor.Reset();
						}
						else
						{
							EditorGUI.LabelField(drawRect, sceneObject.StateAuthority.GetLabel(), GUIStyles.RightLabel);
						}

						if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && drawRect.Contains(Event.current.mousePosition) == true)
						{
							PlayerRef stateAuthority = sceneObject.StateAuthority.Value;

							GenericMenu menu = new GenericMenu();
							menu.AddItem(new GUIContent($"Filter State Authority: {sceneObject.StateAuthority.GetLabel()}"), sceneObjects.StateAuthorityFilter == stateAuthority, () => { sceneObjects.StateAuthorityFilter = sceneObjects.StateAuthorityFilter == stateAuthority ? PlayerRef.None : stateAuthority; });
							menu.ShowAsContext();

							Event.current.Use();
						}
					}
				}

				if (showInputAuthority == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _inputAuthorityStat.Width;

					if (sceneObject.InputAuthority.Value != PlayerRef.None)
					{
						if (sceneObject.HasInputAuthority == true)
						{
							GUIColor.Set(Color.green);
							EditorGUI.LabelField(drawRect, sceneObject.InputAuthority.GetLabel(), GUIStyles.RightLabel);
							GUIColor.Reset();
						}
						else
						{
							EditorGUI.LabelField(drawRect, sceneObject.InputAuthority.GetLabel(), GUIStyles.RightLabel);
						}

						if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && drawRect.Contains(Event.current.mousePosition) == true)
						{
							PlayerRef inputAuthority = sceneObject.InputAuthority.Value;

							GenericMenu menu = new GenericMenu();
							menu.AddItem(new GUIContent($"Filter Input Authority: {sceneObject.InputAuthority.GetLabel()}"), sceneObjects.InputAuthorityFilter == inputAuthority, () => { sceneObjects.InputAuthorityFilter = sceneObjects.InputAuthorityFilter == inputAuthority ? PlayerRef.None : inputAuthority; });
							menu.ShowAsContext();

							Event.current.Use();
						}
					}
				}

				if (showTotalStateChanges == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _totalStateChangesStat.Width;

					if (sceneObject.TotalStateChanges.Value > 0)
					{
						EditorGUI.LabelField(drawRect, sceneObject.TotalStateChanges.GetLabel(), GUIStyles.RightLabel);
					}
				}

				if (showAverageStateChanges == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _averageStateChangesStat.Width;

					if (sceneObject.AverageStateChanges.Value > 0)
					{
						EditorGUI.LabelField(drawRect, sceneObject.AverageStateChanges.GetLabel(), GUIStyles.RightLabel);
					}
				}

				if (showStateSize == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _stateSizeStat.Width;

					EditorGUI.LabelField(drawRect, sceneObject.StateSize.GetLabel(), GUIStyles.RightLabel);
				}

				if (showDistance == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _distanceStat.Width;

					EditorGUI.LabelField(drawRect, sceneObject.Distance.GetLabel(), GUIStyles.RightLabel);
				}

				FusionInspector.RestoreLinePadding(ref drawRect);

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			GUIHighlights.DrawAll();

			float scrollHeight = Mathf.Min(scrollRect.height, scrollViewRect.height);

			GUISeparators.AddY(scrollRect.yMin + scrollHeight + 2.0f);
			GUISeparators.AddY(scrollRect.yMin + scrollHeight + 2.0f + FusionInspector.LineHeight);

			DrawTotalGUI(window, new Rect(content.x, scrollRect.yMin + scrollHeight + 2.0f, content.width, FusionInspector.LineHeight), sceneObjects,
				showComponents, showIsMasterClientObject, showAllowStateAuthorityOverride, showDestroyWhenStateAuthorityLeaves, showInterestMode,
				showIsInSimulation, showHasStateAuthority, showHasInputAuthority, showNetworkId, showStateAuthority, showInputAuthority,
				showTotalStateChanges, showAverageStateChanges, showStateSize);

			GUISeparators.DrawAll(new Rect(0.0f, 0.0f, window.width, window.height), headerRect.yMin, headerRect.height, scrollRect.yMin, scrollHeight + 2.0f);
		}

		private void DrawTotalGUI(Rect window, Rect totalRect, SceneObjects sceneObjects,
			bool showComponents, bool showIsMasterClientObject, bool showAllowStateAuthorityOverride, bool showDestroyWhenStateAuthorityLeaves, bool showInterestMode,
			bool showIsInSimulation, bool showHasStateAuthority, bool showHasInputAuthority, bool showNetworkId, bool showStateAuthority, bool showInputAuthority,
			bool showTotalStateChanges, bool showAverageStateChanges, bool showStateSize)
		{
			int totalComponents          = 0;
			int totalStateSize           = 0;
			int totalStateChanges        = 0;
			int totalAverageStateChanges = 0;

			for (int i = 0, count = sceneObjects.Filtered.Count; i < count; ++i)
			{
				SceneObjectInfo sceneObject = sceneObjects.Filtered[i];

				totalComponents          += Mathf.Max(0, sceneObject.ComponentCount.Value);
				totalStateChanges        += Mathf.Max(0, sceneObject.TotalStateChanges.Value);
				totalAverageStateChanges += Mathf.Max(0, sceneObject.AverageStateChanges.Value);

				if (sceneObject.StateSize.Value > 0)
				{
					totalStateSize += sceneObject.StateSize.Value;
				}
			}

			_totalComponents.Value          = totalComponents;
			_totalStateSize.Value           = totalStateSize;
			_totalStateChangesSum.Value     = totalStateChanges;
			_totalStateChangesAverage.Value = totalAverageStateChanges;

			TextureUtility.DrawTexture(new Rect(0.0f, totalRect.yMin, window.width, totalRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref totalRect);

			Rect drawRect = totalRect;
			drawRect.width = _objectStat.Width;

			Rect totalLabelRect = drawRect;
			totalLabelRect.xMin += GUIStyles.ObjectField.padding.left + (totalLabelRect.height - GUIStyles.ObjectField.padding.vertical) - GUIStyles.LeftBoldLabel.padding.left + 2.0f;

			EditorGUI.LabelField(totalLabelRect, _totalContent, GUIStyles.LeftBoldLabel);

			if (showComponents == true)
			{
				drawRect.xMin += drawRect.width;
				drawRect.width = _componentsStat.Width;

				drawRect.xMax -= 2.0f;
				EditorGUI.LabelField(drawRect, _totalComponents.GetLabel(), GUIStyles.RightBoldLabel);
				drawRect.xMax += 2.0f;
			}

			if (showIsMasterClientObject            == true) { drawRect.xMin += drawRect.width; drawRect.width = _isMasterClientObjectStat.Width;            }
			if (showAllowStateAuthorityOverride     == true) { drawRect.xMin += drawRect.width; drawRect.width = _allowStateAuthorityOverrideStat.Width;     }
			if (showDestroyWhenStateAuthorityLeaves == true) { drawRect.xMin += drawRect.width; drawRect.width = _destroyWhenStateAuthorityLeavesStat.Width; }
			if (showInterestMode                    == true) { drawRect.xMin += drawRect.width; drawRect.width = _interestModeStat.Width;                    }
			if (showIsInSimulation                  == true) { drawRect.xMin += drawRect.width; drawRect.width = _isInSimulationStat.Width;                  }
			if (showHasStateAuthority               == true) { drawRect.xMin += drawRect.width; drawRect.width = _hasStateAuthorityStat.Width;               }
			if (showHasInputAuthority               == true) { drawRect.xMin += drawRect.width; drawRect.width = _hasInputAuthorityStat.Width;               }
			if (showNetworkId                       == true) { drawRect.xMin += drawRect.width; drawRect.width = _networkIdStat.Width;                       }
			if (showStateAuthority                  == true) { drawRect.xMin += drawRect.width; drawRect.width = _stateAuthorityStat.Width;                  }
			if (showInputAuthority                  == true) { drawRect.xMin += drawRect.width; drawRect.width = _inputAuthorityStat.Width;                  }
			if (showTotalStateChanges == true)
			{
				drawRect.xMin += drawRect.width;
				drawRect.width = _totalStateChangesStat.Width;

				if (_totalStateChangesSum.Value > 0)
				{
					EditorGUI.LabelField(drawRect, _totalStateChangesSum.GetLabel(), GUIStyles.RightBoldLabel);
				}
			}

			if (showAverageStateChanges == true)
			{
				drawRect.xMin += drawRect.width;
				drawRect.width = _averageStateChangesStat.Width;

				if (_totalStateChangesAverage.Value > 0)
				{
					EditorGUI.LabelField(drawRect, _totalStateChangesAverage.GetLabel(), GUIStyles.RightBoldLabel);
				}
			}

			if (showStateSize == true)
			{
				drawRect.xMin += drawRect.width;
				drawRect.width = _stateSizeStat.Width;

				EditorGUI.LabelField(drawRect, _totalStateSize.GetLabel(), GUIStyles.RightBoldLabel);
			}

			FusionInspector.RestoreLinePadding(ref totalRect);
		}

		private void DrawFieldsGUI(ref Rect sectionRect, SceneObjects sceneObjects, ESceneObjectField visibleFields, ESceneObjectField toggleableFields)
		{
			if (sceneObjects.VisibleFieldsFoldout == false)
				return;

			float xOffset = 3.0f;
			float yOffset = 6.0f;

			Rect innerRect = sectionRect;
			innerRect.xMin += xOffset;
			innerRect.xMax -= xOffset;
			innerRect.height = EditorGUIUtility.singleLineHeight;

			float totalWidth      = _fieldsWidth1 + _fieldsWidth2 + _fieldsWidth3;
			float widthMultiplier = Mathf.Min(1.0f, innerRect.width / totalWidth);

			//----------------------------------------------------------------

			Rect configRect = innerRect;
			configRect.xMin = innerRect.xMin;
			configRect.xMax = innerRect.xMin + _fieldsWidth1 * widthMultiplier;

			configRect.y += yOffset;

			DrawCategoryToggle(ref configRect, "Configuration", ESceneObjectField.Configuration);
			configRect.y += yOffset;

			DrawToggle(ref configRect, "Components (NB)",                     _componentsStat.Content.tooltip,                      ESceneObjectField.Components);
			DrawToggle(ref configRect, "Is Master Client Object",             _isMasterClientObjectStat.Content.tooltip,            ESceneObjectField.IsMasterClientObject);
			DrawToggle(ref configRect, "Allow State Authority Override",      _allowStateAuthorityOverrideStat.Content.tooltip,     ESceneObjectField.AllowStateAuthorityOverride);
			DrawToggle(ref configRect, "Destroy When State Authority Leaves", _destroyWhenStateAuthorityLeavesStat.Content.tooltip, ESceneObjectField.DestroyWhenStateAuthorityLeaves);
			DrawToggle(ref configRect, "Object Interest Mode",                _interestModeStat.Content.tooltip,                    ESceneObjectField.InterestMode);

			configRect.y += yOffset;

			//----------------------------------------------------------------

			Rect runtimeRect = innerRect;
			runtimeRect.xMin = configRect.xMax + xOffset;
			runtimeRect.xMax = runtimeRect.xMin + _fieldsWidth2 * widthMultiplier;

			runtimeRect.y += yOffset;

			DrawCategoryToggle(ref runtimeRect, "Runtime", ESceneObjectField.Runtime);
			runtimeRect.y += yOffset;

			DrawToggle(ref runtimeRect, "Is In Simulation",    _isInSimulationStat.Content.tooltip,      ESceneObjectField.IsInSimulation);
			DrawToggle(ref runtimeRect, "Has State Authority", _hasStateAuthorityStat.Content.tooltip,   ESceneObjectField.HasStateAuthority);
			DrawToggle(ref runtimeRect, "Has Input Authority", _hasInputAuthorityStat.Content.tooltip,   ESceneObjectField.HasInputAuthority);
			DrawToggle(ref runtimeRect, "Network Id",          _networkIdStat.Content.tooltip,           ESceneObjectField.NetworkId);
			DrawToggle(ref runtimeRect, "State Authority",     _stateAuthorityStat.Content.tooltip,      ESceneObjectField.StateAuthority);
			DrawToggle(ref runtimeRect, "Input Authority",     _inputAuthorityStat.Content.tooltip,      ESceneObjectField.InputAuthority);
			DrawToggle(ref runtimeRect, "Σ State Changes",     _totalStateChangesStat.Content.tooltip,   ESceneObjectField.TotalStateChanges);
			DrawToggle(ref runtimeRect, "Ø State Changes",     _averageStateChangesStat.Content.tooltip, ESceneObjectField.AverageStateChanges);

			runtimeRect.y += yOffset;

			//----------------------------------------------------------------

			Rect otherRect = innerRect;
			otherRect.xMin = runtimeRect.xMax + xOffset;
			otherRect.xMax = otherRect.xMin + _fieldsWidth3 * widthMultiplier;

			otherRect.y += yOffset;

			DrawCategoryToggle(ref otherRect, "Other", ESceneObjectField.Other);
			otherRect.y += yOffset;

			DrawToggle(ref otherRect, "State Size", _stateSizeStat.Content.tooltip, ESceneObjectField.StateSize);
			DrawToggle(ref otherRect, "Distance",   _distanceStat.Content.tooltip,  ESceneObjectField.Distance);

			otherRect.y += yOffset;

			//----------------------------------------------------------------

			sectionRect.yMin = Mathf.Max(configRect.yMin, runtimeRect.yMin, otherRect.yMin);

			return;

			void DrawCategoryToggle(ref Rect rect, string label, ESceneObjectField categoryFields)
			{
				ESceneObjectField enabledFields = sceneObjects.VisibleFields & categoryFields;

				bool allEnabled = enabledFields == categoryFields;

				EditorGUI.BeginDisabledGroup((toggleableFields & categoryFields) == ESceneObjectField.None);

				EditorGUI.showMixedValue = allEnabled == false && enabledFields != ESceneObjectField.None;
				bool toggleState = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width - FusionInspector.CategorySpacing * widthMultiplier, rect.height), label, allEnabled, EditorStyles.boldLabel);
				EditorGUI.showMixedValue = false;

				EditorGUI.EndDisabledGroup();

				if (toggleState != allEnabled)
				{
					sceneObjects.SetVisibleFields(categoryFields, toggleState);
				}

				rect.y += rect.height;

				TextureUtility.DrawTexture(new Rect(rect.x, rect.y + 2.0f, rect.width - FusionInspector.CategorySpacing * widthMultiplier, FusionInspector.SeparatorSize), FusionInspector.SeparatorColor);
			}

			void DrawToggle(ref Rect rect, string label, string tooltip, ESceneObjectField field)
			{
				_toggleContent.text    = label;
				_toggleContent.tooltip = tooltip;

				EditorGUI.BeginDisabledGroup(toggleableFields.Has(field) == false);

				bool previousState = sceneObjects.VisibleFields.Has(field);
				bool currentState  = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width - FusionInspector.CategorySpacing * widthMultiplier, rect.height), _toggleContent, previousState);

				EditorGUI.EndDisabledGroup();

				if (currentState != previousState)
				{
					sceneObjects.ToggleVisibleField(field);
				}

				rect.y += rect.height;
			}
		}

		private void DrawComponentFilterGUI(ref Rect sectionRect, SceneObjects sceneObjects, Components components)
		{
			sceneObjects.ComponentFilterInclude.Clear();
			sceneObjects.ComponentFilterExclude.Clear();

			if (sceneObjects.ComponentFilterFoldout == false)
			{
				for (int i = 0, count = components.All.Count; i < count; ++i)
				{
					ComponentInfo component = components.All[i];

					if (component.SceneObjects.FilterMode == EFilterMode.Include)
					{
						sceneObjects.ComponentFilterInclude.Add(component.Type);
					}
					else if (component.SceneObjects.FilterMode == EFilterMode.Exclude)
					{
						sceneObjects.ComponentFilterExclude.Add(component.Type);
					}
				}

				return;
			}

			_tempComponents.Clear();
			_tempComponents.AddRange(components.All);

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

				ComponentInfo.ObjectContext context = component.SceneObjects;

				if (EditorGUI.ToggleLeft(componentRect, context.ObjectFilter.GetLabel(component.TypeName, context.AllObjectCount.Value, context.FilteredObjectCount.Value), false, context.FilteredObjectCount.Value > 0 ? GUIStyles.ActiveToggle : GUIStyles.InactiveToggle) == true)
				{
					context.FilterMode = (EFilterMode)(((int)context.FilterMode + 1) % FusionInspector.FILTER_MODES);
				}

				if (context.FilterMode == EFilterMode.Include)
				{
					sceneObjects.ComponentFilterInclude.Add(component.Type);

					EditorGUI.LabelField(componentRect, GUISymbols.CHECKMARK);
				}
				else if (context.FilterMode == EFilterMode.Exclude)
				{
					sceneObjects.ComponentFilterExclude.Add(component.Type);

					EditorGUI.LabelField(componentRect, GUISymbols.CROSS);
				}
			}

			_tempComponents.Clear();
		}

		private void DrawFilterBarGUI(Rect window, ref Rect region, SceneObjects sceneObjects)
		{
			bool hasStateAuthorityFilter = sceneObjects.StateAuthorityFilter.IsNone == false;
			bool hasInputAuthorityFilter = sceneObjects.InputAuthorityFilter.IsNone == false;
			bool hasInterestModeFilter   = sceneObjects.InterestModeFilter >= 0;

			if (hasStateAuthorityFilter == false && hasInputAuthorityFilter == false && hasInterestModeFilter == false)
				return;

			if (hasStateAuthorityFilter == true && _stateAuthorityFilterPlayer != sceneObjects.StateAuthorityFilter)
			{
				_stateAuthorityFilterPlayer = sceneObjects.StateAuthorityFilter;
				_authorityFilterLabel.Value = _stateAuthorityFilterPlayer;
				_stateAuthorityFilterContent.text = $"State Authority: {_authorityFilterLabel.GetLabel()}";
			}

			if (hasInputAuthorityFilter == true && _inputAuthorityFilterPlayer != sceneObjects.InputAuthorityFilter)
			{
				_inputAuthorityFilterPlayer = sceneObjects.InputAuthorityFilter;
				_authorityFilterLabel.Value = _inputAuthorityFilterPlayer;
				_inputAuthorityFilterContent.text = $"Input Authority: {_authorityFilterLabel.GetLabel()}";
			}

			if (hasInterestModeFilter == true && _interestModeFilterMode != sceneObjects.InterestModeFilter)
			{
				_interestModeFilterMode = sceneObjects.InterestModeFilter;
				_interestModeFilterContent.text = $"Interest: {_interestModeContents[_interestModeFilterMode].text}";
			}

			Rect barRect = region;
			barRect.height = FusionInspector.LineHeight;

			TextureUtility.DrawTexture(new Rect(0.0f, barRect.yMin, window.width, barRect.height), FusionInspector.HeaderColor);

			region.yMin += barRect.height;

			if (hasStateAuthorityFilter == true && DrawFilterChip(ref barRect, _stateAuthorityFilterContent) == true)
			{
				sceneObjects.StateAuthorityFilter = PlayerRef.None;
			}

			if (hasInputAuthorityFilter == true && DrawFilterChip(ref barRect, _inputAuthorityFilterContent) == true)
			{
				sceneObjects.InputAuthorityFilter = PlayerRef.None;
			}

			if (hasInterestModeFilter == true && DrawFilterChip(ref barRect, _interestModeFilterContent) == true)
			{
				sceneObjects.InterestModeFilter = -1;
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

		private void DrawSelectionGUI(Rect window, ref Rect region, FusionInspectorDB db)
		{
			SceneObjects sceneObjects = db.SceneObjects;
			if (sceneObjects.All.Count <= 0)
				return;

			_tempScripts.Clear();
			for (int i = 0, count = sceneObjects.Filtered.Count; i < count; ++i)
			{
				SceneObjectInfo sceneObject = sceneObjects.Filtered[i];
				if (sceneObject.IsSelected == true)
				{
					for (int j = 0, countJ = sceneObject.Components.Length; j < countJ; ++j)
					{
						_tempScripts.Add(sceneObject.Components[j]);
					}
				}
			}

			if (_tempScripts.Count <= 0)
				return;

			Rect content = region;
			content.xMax = content.xMin + FusionInspector.LeftWindowOffset + _objectStat.Width;

			GUISeparators.Clear();
			GUISeparators.AddX(content.xMax);

			float widthOffset = 0.0f;
			int   scrollViewItems = _tempScripts.Count;

			bool isInPlayMode = Application.isPlaying;
			if (isInPlayMode == true)
			{
				++scrollViewItems;
			}

			Rect scrollViewRect = new Rect(0.0f, 0.0f, content.width, FusionInspector.LineHeight * scrollViewItems);
			if (scrollViewRect.height > (content.height - FusionInspector.HeaderHeight - 1.0f))
			{
				widthOffset = FusionInspector.RightWindowOffset;
			}

			scrollViewRect.width -= widthOffset;

			Rect headerRect = content;
			headerRect.xMin += FusionInspector.LeftWindowOffset;
			headerRect.width  = _objectStat.Width - widthOffset;
			headerRect.height = FusionInspector.HeaderHeight;

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, content.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			Rect typeRect = headerRect;
			typeRect.xMin += _sortWidth + 2.0f;
			GUI.Label(typeRect, "Type", _objectStat.Style);

			if (isInPlayMode == true)
			{
				GUI.Label(typeRect, "Σ Changes", _totalStateChangesStat.Style);
			}
			else
			{
				GUI.Label(typeRect, "State", _stateSizeStat.Style);
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			content.yMin += headerRect.height + 0.5f;
			content.yMax -= 0.5f;

			_scriptScrollPosition = GUI.BeginScrollView(content, _scriptScrollPosition, scrollViewRect);

			float minScrollY = _scriptScrollPosition.y;
			float maxScrollY = _scriptScrollPosition.y + content.height;
			Rect  drawRect   = new Rect(FusionInspector.LeftWindowOffset, 0.0f, _objectStat.Width - widthOffset, FusionInspector.LineHeight);

			if (isInPlayMode == true)
			{
				_totalSelectionChanges.Value = 0;

				foreach (ComponentInfo component in _tempScripts)
				{
					_totalSelectionChanges.Value += component.SelectedStateChanges.Value;
				}

				GUI.Label(new Rect(drawRect.x + 18.0f, drawRect.y, drawRect.width, drawRect.height), "Total", GUIStyles.ObjectField);
				GUI.Label(drawRect, _totalSelectionChanges.GetLabel(), GUIStyles.RightLabel);
				drawRect.y += drawRect.height;
			}

			foreach (ComponentInfo component in _tempScripts)
			{
				if (drawRect.yMax < minScrollY)
				{
					drawRect.y += drawRect.height;
					continue;
				}
				else if (drawRect.y > maxScrollY)
				{
					break;
				}

				_scriptContent.text = component.TypeName;
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
					else
					{
						if (_scriptSelection == component && EditorApplication.timeSinceStartup < (_scriptSelectionTime + FusionInspector.SelectionDelay))
						{
							AssetDatabase.OpenAsset(component.Script);
							_scriptSelection     = default;
							_scriptSelectionTime = default;
						}
						else
						{
							EditorGUIUtility.PingObject(component.Script);
							_scriptSelection     = component;
							_scriptSelectionTime = EditorApplication.timeSinceStartup;
						}
					}
				}

				if (isInPlayMode == true)
				{
					GUI.Label(drawRect, component.SelectedStateChanges.GetLabel(), GUIStyles.RightLabel);
				}
				else
				{
					GUI.Label(drawRect, component.StateSize.GetLabel(), GUIStyles.RightLabel);
				}

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			GUISeparators.DrawAll(new Rect(0.0f, 0.0f, content.width, region.height), headerRect.yMin, headerRect.height, headerRect.yMax, region.height - headerRect.height);
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

		private static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			FusionInspector.DrawHeader(stat, ref rect, ref filter);
		}

		private static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref ESceneObjectSortMode sortMode, ESceneObjectSortMode primarySortMode, ESceneObjectSortMode secondarySortMode)
		{
			int intSortMode = (int)sortMode;
			FusionInspector.DrawHeader(stat, icon, ref rect, ref intSortMode, (int)primarySortMode, (int)secondarySortMode);
			sortMode = (ESceneObjectSortMode)intSortMode;
		}

		private static void ToggleSortMode(ref ESceneObjectSortMode sortMode, ESceneObjectSortMode primaryMode, ESceneObjectSortMode secondaryMode)
		{
			sortMode = sortMode == primaryMode ? secondaryMode : primaryMode;
		}
	}
}
