namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	internal sealed class PrefabsInspector
	{
		private Vector2                _scrollPosition;
		private double                 _selectionTime;
		private List<PrefabInfo>       _selection = new List<PrefabInfo>();
		private object                 _scriptSelection;
		private double                 _scriptSelectionTime;
		private Vector2                _scriptScrollPosition;
		private HashSet<ComponentInfo> _tempScripts = new HashSet<ComponentInfo>();
		private List<ComponentInfo>    _tempComponents = new List<ComponentInfo>();
		private IntLabel               _totalComponents = new IntLabel();
		private MemoryLabel            _totalStateSize = new MemoryLabel(false);

		private GUINameStat _objectStat;
		private GUISortStat _componentsStat;
		private GUIIconStat _isSpawnableStat;
		private GUIIconStat _isMasterClientObjectStat;
		private GUIIconStat _allowStateAuthorityOverrideStat;
		private GUIIconStat _destroyWhenStateAuthorityLeavesStat;
		private GUISortStat _interestModeStat;
		private GUISortStat _stateSizeStat;

		private GUIContent   _objectContent;
		private GUIContent   _componentsContent;
		private GUIContent   _componentsIconContent;
		private GUIContent[] _interestModeContents;
		private GUIContent   _stateSizeContent;
		private GUIContent   _filterContent;
		private GUIContent   _fieldsContent;
		private GUIContent   _scriptContent;
		private GUIContent   _toggleContent;
		private GUIContent   _totalContent;
		private GUIContent   _interestModeFilterContent;
		private GUIContent   _removeFilterContent;

		private int _interestModeFilterMode = -1;

		private float _sortWidth;
		private float _fieldsWidth1;
		private float _fieldsWidth2;

		public PrefabsInspector()
		{
			Texture2D prefabIcon = EditorGUIUtility.IconContent("d_Prefab Icon").image as Texture2D;
			Texture2D scriptIcon = EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D;
			Texture2D filterIcon = EditorGUIUtility.IconContent("d_FilterByType@2x").image as Texture2D;
			Texture2D fieldsIcon = EditorGUIUtility.IconContent("d__Menu@2x").image as Texture2D;

			float headerHeight   = FusionInspector.HeaderHeight;
			float stateSizeWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent("8888 B")).x;

			_objectStat = new GUINameStat(GUIStyles.LeftBoldLabel, "Object", "Name of the object", (int)EPrefabSortMode.NameAscending, (int)EPrefabSortMode.NameDescending);

			_componentsStat                      = new GUISortStat(GUIStyles.RightBoldLabel, "     ", "# of NetworkBehaviour components on the object", (int)EPrefabSortMode.ComponentsAscending, (int)EPrefabSortMode.ComponentsDescending);
			_isSpawnableStat                     = new GUIIconStat(GUIStyles.Icon, "✿", "✔", $"Is Spawnable", headerHeight);
			_isMasterClientObjectStat            = new GUIIconStat(GUIStyles.Icon, "❖", "✔", $"Is Master Client Object (Shared Mode)", headerHeight);
			_allowStateAuthorityOverrideStat     = new GUIIconStat(GUIStyles.Icon, "✪", "✔", $"Allow State Authority Override (Shared Mode)", headerHeight);
			_destroyWhenStateAuthorityLeavesStat = new GUIIconStat(GUIStyles.Icon, "☢", "✔", $"Destroy When State Authority Leaves (Shared Mode)", headerHeight);
			_interestModeStat                    = new GUISortStat(GUIStyles.RightBoldLabel, "Interest", "Object Interest Mode", (int)EPrefabSortMode.InterestModeAscending, (int)EPrefabSortMode.InterestModeDescending);
			_stateSizeStat                       = new GUISortStat(GUIStyles.RightBoldLabel, "State", "Size of the networked state of the object in Bytes", (int)EPrefabSortMode.StateSizeAscending, (int)EPrefabSortMode.StateSizeDescending, stateSizeWidth);

			_objectContent         = new GUIContent(prefabIcon);
			_componentsContent     = new GUIContent("", "# of NetworkBehaviour components on the object");
			_componentsIconContent = new GUIContent(scriptIcon, "# of NetworkBehaviour components on the object");
			_interestModeContents  = new GUIContent[] { new GUIContent("Area"), new GUIContent("Global"), new GUIContent("Explicit") };
			_stateSizeContent      = new GUIContent("", "Size of the networked state of the object in Bytes");
			_filterContent         = new GUIContent(filterIcon, "Filter by component type");
			_fieldsContent         = new GUIContent(fieldsIcon, "Visible fields");
			_scriptContent         = new GUIContent(scriptIcon);
			_toggleContent         = new GUIContent();
			_totalContent          = new GUIContent("Total", "Totals for all listed prefabs");

			_interestModeFilterContent = new GUIContent();
			_removeFilterContent       = new GUIContent(GUISymbols.CROSS, "Remove filter");

			_sortWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"{GUISymbols.ARROW_UP} ")).x - GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"")).x;

			_componentsStat.Width   += _sortWidth;
			_interestModeStat.Width += _sortWidth;
			_stateSizeStat.Width    += _sortWidth;

			_fieldsWidth1 = FusionInspector.GetCategoryWidth("Configuration", "Components (NB)", "Is Spawnable", "Is Master Client Object", "Allow State Authority Override", "Destroy When State Authority Leaves", "Object Interest Mode");
			_fieldsWidth2 = FusionInspector.GetCategoryWidth("Other", "State Size");
		}

		public void DrawGUI(Rect window, Rect region, FusionInspectorDB db)
		{
			EPrefabField visibleFields    = db.Prefabs.VisibleFields | EPrefabField.Object;
			EPrefabField toggleableFields = ~EPrefabField.Object;

			db.Prefabs.VisibleFields = visibleFields;

			DrawPrefabsGUI(window, ref region, db, visibleFields, toggleableFields);

			region.yMin += Mathf.Clamp(region.height, 0.0f, 2.0f);

			DrawSelectionGUI(window, ref region, db);
		}

		private void DrawPrefabsGUI(Rect window, ref Rect region, FusionInspectorDB db, EPrefabField visibleFields, EPrefabField toggleableFields)
		{
			Prefabs prefabs = db.Prefabs;
			if (prefabs.All.Count <= 0)
				return;

			int minSelection = int.MaxValue;
			int maxSelection = int.MinValue;

			_selection.Clear();
			for (int i = 0, count = prefabs.Filtered.Count; i < count; ++i)
			{
				PrefabInfo prefab = prefabs.Filtered[i];
				if (prefab.IsSelected == true)
				{
					_selection.Add(prefab);

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

			bool showComponents                      = visibleFields.Has(EPrefabField.Components);
			bool showIsSpawnable                     = visibleFields.Has(EPrefabField.IsSpawnable);
			bool showIsMasterClientObject            = visibleFields.Has(EPrefabField.IsMasterClientObject);
			bool showAllowStateAuthorityOverride     = visibleFields.Has(EPrefabField.AllowStateAuthorityOverride);
			bool showDestroyWhenStateAuthorityLeaves = visibleFields.Has(EPrefabField.DestroyWhenStateAuthorityLeaves);
			bool showInterestMode                    = visibleFields.Has(EPrefabField.InterestMode);
			bool showStateSize                       = visibleFields.Has(EPrefabField.StateSize);

			GUISeparators.Clear();

			content.xMin += FusionInspector.LeftWindowOffset;
			content.xMax -= FusionInspector.RightWindowOffset;

			_objectStat.Width = content.width;

			if (showComponents                      == true) { _objectStat.Width -= _componentsStat.Width;                      }
			if (showIsSpawnable                     == true) { _objectStat.Width -= _isSpawnableStat.Width;                     } else { prefabs.IsSpawnableFilter                     = default; }
			if (showIsMasterClientObject            == true) { _objectStat.Width -= _isMasterClientObjectStat.Width;            } else { prefabs.IsMasterClientObjectFilter            = default; }
			if (showAllowStateAuthorityOverride     == true) { _objectStat.Width -= _allowStateAuthorityOverrideStat.Width;     } else { prefabs.AllowStateAuthorityOverrideFilter     = default; }
			if (showDestroyWhenStateAuthorityLeaves == true) { _objectStat.Width -= _destroyWhenStateAuthorityLeavesStat.Width; } else { prefabs.DestroyWhenStateAuthorityLeavesFilter = default; }
			if (showInterestMode                    == true) { _objectStat.Width -= _interestModeStat.Width;                    }
			if (showStateSize                       == true) { _objectStat.Width -= _stateSizeStat.Width;                       }

			Rect headerRect = content;
			headerRect.width  = _objectStat.Width;
			headerRect.height = FusionInspector.HeaderHeight;

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, window.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			Rect componentFilterRect = headerRect;
			componentFilterRect.xMin = componentFilterRect.xMax - FusionInspector.HeaderHeight;
			GUIColor.Set(FusionInspector.FilterColors[Mathf.Min(prefabs.ComponentFilterInclude.Count + prefabs.ComponentFilterExclude.Count, 1)]);
			if (GUI.Button(componentFilterRect, _filterContent, GUIStyles.Icon) == true)
			{
				if (Event.current.button == 1)
				{
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Reset"), false, () =>
					{
						for (int i = 0, count = db.Components.All.Count; i < count; ++i)
						{
							db.Components.All[i].Prefabs.FilterMode = EFilterMode.None;
						}
					});
					menu.ShowAsContext();
				}
				else
				{
					prefabs.ComponentFilterFoldout = !prefabs.ComponentFilterFoldout;
					prefabs.VisibleFieldsFoldout = false;
				}
			}
			GUIColor.Reset();

			GUIContent nameContent = _objectStat.GetContent(prefabs.All.Count, prefabs.Filtered.Count, _selection.Count, (int)prefabs.SortMode);

			Rect nameRect = headerRect;
			nameRect.xMin += _objectStat.IsSorted((int)prefabs.SortMode) ? 0.0f : _sortWidth;

			Rect nameFilterRect = headerRect;
			nameFilterRect.xMax = componentFilterRect.xMin;
			FusionInspector.ApplyFilterRect(ref nameFilterRect, nameRect.xMin + _objectStat.Style.CalcSize(nameContent).x);
			prefabs.ObjectNameFilter = FusionInspector.DrawFilterField(nameFilterRect, prefabs.ObjectNameFilter);

			nameRect.xMax = nameFilterRect.xMin;
			if (GUI.Button(nameRect, nameContent, _objectStat.Style) == true)
			{
				ToggleSortMode(ref prefabs.SortMode, EPrefabSortMode.NameAscending, EPrefabSortMode.NameDescending);
			}

			if (showComponents                      == true) { DrawHeader(_componentsStat, _componentsIconContent, ref headerRect, ref prefabs.SortMode, EPrefabSortMode.ComponentsDescending, EPrefabSortMode.ComponentsAscending); }
			if (showIsSpawnable                     == true) { DrawHeader(_isSpawnableStat,                        ref headerRect, ref prefabs.IsSpawnableFilter);                     }
			if (showIsMasterClientObject            == true) { DrawHeader(_isMasterClientObjectStat,               ref headerRect, ref prefabs.IsMasterClientObjectFilter);            }
			if (showAllowStateAuthorityOverride     == true) { DrawHeader(_allowStateAuthorityOverrideStat,        ref headerRect, ref prefabs.AllowStateAuthorityOverrideFilter);     }
			if (showDestroyWhenStateAuthorityLeaves == true) { DrawHeader(_destroyWhenStateAuthorityLeavesStat,    ref headerRect, ref prefabs.DestroyWhenStateAuthorityLeavesFilter); }
			if (showInterestMode                    == true) { DrawHeader(_interestModeStat, null,                 ref headerRect, ref prefabs.SortMode, EPrefabSortMode.InterestModeAscending, EPrefabSortMode.InterestModeDescending); }
			if (showStateSize                       == true) { DrawHeader(_stateSizeStat, null,                    ref headerRect, ref prefabs.SortMode, EPrefabSortMode.StateSizeDescending,   EPrefabSortMode.StateSizeAscending);     }

			headerRect.xMin += headerRect.width;
			headerRect.width = FusionInspector.RightWindowOffset;
			if (GUI.Button(headerRect, _fieldsContent, GUIStyles.Icon) == true)
			{
				prefabs.VisibleFieldsFoldout = !prefabs.VisibleFieldsFoldout;
				prefabs.ComponentFilterFoldout = false;
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			content.yMin += headerRect.height;

			DrawFieldsGUI(ref content, prefabs, visibleFields, toggleableFields);
			DrawComponentFilterGUI(ref content, prefabs, db.Components);
			DrawFilterBarGUI(window, ref content, prefabs);

			GUISeparators.AddY(content.y);

			// One line is reserved for the always visible Total row below the list.
			content.yMax -= FusionInspector.LineHeight + 2.0f;

			Rect scrollRect     = new Rect(content.x, content.y, content.width + FusionInspector.RightWindowOffset, content.height);
			Rect scrollViewRect = new Rect(0.0f, 0.0f, content.width, FusionInspector.LineHeight * prefabs.Filtered.Count);

			GUISeparators.AddX(headerRect.xMin);

			_scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, scrollViewRect);

			float   minScrollY    = _scrollPosition.y;
			float   maxScrollY    = _scrollPosition.y + content.height;
			Rect    drawRect      = new Rect(0.0f, 0.0f, 0.0f, FusionInspector.LineHeight);
			Vector2 mousePosition = Event.current.mousePosition;

			for (int i = 0, count = prefabs.Filtered.Count; i < count; ++i)
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

				PrefabInfo prefab = prefabs.Filtered[i];

				drawRect.xMin  = 0.0f;
				drawRect.width = _objectStat.Width;

				if (prefab.IsSelected == true)
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

				GUIColor.Set(prefab.IsActive == true ? FusionInspector.ActiveTextColor : FusionInspector.InactiveTextColor);
				_objectContent.text = prefab.Name;
				drawRect.height += 1.0f; // GUI.Button dead zone fix
				if (GUI.Button(drawRect, _objectContent, GUIStyles.ObjectField) == true)
				{
					if (Event.current.button == 1)
					{
						GenericMenu menu = new GenericMenu();

						if (_selection.Count > 1 && _selection.Contains(prefab) == true)
						{
							List<string> names     = new List<string>();
							List<string> paths     = new List<string>();
							List<string> fullPaths = new List<string>();

							for (int j = 0; j < _selection.Count; ++j)
							{
								string selectionPath = AssetDatabase.GetAssetPath(_selection[j].GameObject);

								names.Add(_selection[j].Name);
								paths.Add(selectionPath);
								fullPaths.Add(string.IsNullOrEmpty(selectionPath) == false ? System.IO.Path.GetFullPath(selectionPath) : "");
							}

							menu.AddItem(new GUIContent("Copy Names"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, string.Join("\n", names));
							menu.AddItem(new GUIContent("Copy Paths"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, string.Join("\n", paths));
							menu.AddItem(new GUIContent("Copy Full Paths"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, string.Join("\n", fullPaths));
						}
						else
						{
							string assetPath = AssetDatabase.GetAssetPath(prefab.GameObject);

							int interestMode = prefab.InterestMode;
							menu.AddItem(new GUIContent($"Filter Interest: {_interestModeContents[interestMode].text}"), prefabs.InterestModeFilter == interestMode, () => { prefabs.InterestModeFilter = prefabs.InterestModeFilter == interestMode ? -1 : interestMode; });

							menu.AddSeparator("");
							menu.AddItem(new GUIContent("Copy Name"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, prefab.Name);
							menu.AddItem(new GUIContent("Copy Path"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, assetPath);
							menu.AddItem(new GUIContent("Copy Full Path"), false, (data) => { EditorGUIUtility.systemCopyBuffer = (string)data; }, string.IsNullOrEmpty(assetPath) == false ? System.IO.Path.GetFullPath(assetPath) : "");
							menu.AddSeparator("");
							menu.AddItem(new GUIContent(Application.platform == RuntimePlatform.OSXEditor ? "Reveal in Finder" : "Show in Explorer"), false, (data) => { EditorUtility.RevealInFinder((string)data); }, assetPath);
						}

						menu.ShowAsContext();
					}
					else
					{
						if (_selection.Count == 1 && _selection.Contains(prefab) == true && EditorApplication.timeSinceStartup < (_selectionTime + FusionInspector.SelectionDelay))
						{
							_selectionTime = default;

							Selection.SetActiveObjectWithContext(prefab.GameObject, prefab.GameObject);
						}
						else
						{
							if (Event.current.shift == true)
							{
								minSelection = Mathf.Min(minSelection, i);
								maxSelection = Mathf.Max(maxSelection, i);

								for (int j = minSelection; j <= maxSelection; ++j)
								{
									prefabs.Filtered[j].IsSelected = true;
								}

								EditorGUIUtility.PingObject(prefab.GameObject);
							}
							else if (Event.current.control == true)
							{
								prefab.IsSelected = !prefab.IsSelected;
								if (prefab.IsSelected == true)
								{
									EditorGUIUtility.PingObject(prefab.GameObject);
								}
							}
							else
							{
								for (int j = 0; j < _selection.Count; ++j)
								{
									_selection[j].IsSelected = false;
								}

								if (_selection.Count != 1 || _selection.Contains(prefab) == false)
								{
									prefab.IsSelected = true;
									EditorGUIUtility.PingObject(prefab.GameObject);
								}
							}

							_selectionTime = EditorApplication.timeSinceStartup;
						}
					}
				}
				drawRect.height -= 1.0f; // GUI.Button dead zone fix
				GUIColor.Reset();

				if (showComponents == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _componentsStat.Width;

					if (prefab.ComponentCount.Value > 0)
					{
						_componentsContent.text = prefab.ComponentCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _componentsContent, GUIStyles.RightLabel) == true)
						{
							if (prefab.Components.Length > 0)
							{
								GenericMenu menu = new GenericMenu();
								for (int componentIndex = 0; componentIndex < prefab.Components.Length; ++componentIndex)
								{
									ComponentInfo component = prefab.Components[componentIndex];
									menu.AddItem(new GUIContent($"ID:[{ObjectUtility.GetObjectId(prefab.NetworkObject.NetworkedBehaviours[componentIndex])}] {component.TypeName}"), false, (script) =>
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

				if (showIsSpawnable                     == true) { FusionInspector.DrawIcon(_isSpawnableStat,                     ref drawRect, prefab.IsSpawnable);                     }
				if (showIsMasterClientObject            == true) { FusionInspector.DrawIcon(_isMasterClientObjectStat,            ref drawRect, prefab.IsMasterClientObject);            }
				if (showAllowStateAuthorityOverride     == true) { FusionInspector.DrawIcon(_allowStateAuthorityOverrideStat,     ref drawRect, prefab.AllowStateAuthorityOverride);     }
				if (showDestroyWhenStateAuthorityLeaves == true) { FusionInspector.DrawIcon(_destroyWhenStateAuthorityLeavesStat, ref drawRect, prefab.DestroyWhenStateAuthorityLeaves); }

				if (showInterestMode == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _interestModeStat.Width;

					EditorGUI.LabelField(drawRect, _interestModeContents[(int)prefab.InterestMode], GUIStyles.RightLabel);

					if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && drawRect.Contains(Event.current.mousePosition) == true)
					{
						int interestMode = prefab.InterestMode;

						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent($"Filter Interest: {_interestModeContents[interestMode].text}"), prefabs.InterestModeFilter == interestMode, () => { prefabs.InterestModeFilter = prefabs.InterestModeFilter == interestMode ? -1 : interestMode; });
						menu.ShowAsContext();

						Event.current.Use();
					}
				}

				if (showStateSize == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _stateSizeStat.Width;

					EditorGUI.LabelField(drawRect, prefab.StateSize.GetLabel(), GUIStyles.RightLabel);
				}

				FusionInspector.RestoreLinePadding(ref drawRect);

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			GUIHighlights.DrawAll();

			float scrollHeight = Mathf.Min(scrollRect.height, scrollViewRect.height);

			GUISeparators.AddY(scrollRect.yMin + scrollHeight + 2.0f);
			GUISeparators.AddY(scrollRect.yMin + scrollHeight + 2.0f + FusionInspector.LineHeight);

			DrawTotalGUI(window, new Rect(content.x, scrollRect.yMin + scrollHeight + 2.0f, content.width, FusionInspector.LineHeight), prefabs, showComponents, showIsSpawnable, showIsMasterClientObject, showAllowStateAuthorityOverride, showDestroyWhenStateAuthorityLeaves, showInterestMode, showStateSize);

			GUISeparators.DrawAll(new Rect(0.0f, 0.0f, window.width, window.height), headerRect.yMin, headerRect.height, scrollRect.yMin, scrollHeight + 2.0f);
		}

		private void DrawTotalGUI(Rect window, Rect totalRect, Prefabs prefabs, bool showComponents, bool showIsSpawnable, bool showIsMasterClientObject, bool showAllowStateAuthorityOverride, bool showDestroyWhenStateAuthorityLeaves, bool showInterestMode, bool showStateSize)
		{
			int totalComponents = 0;
			int totalStateSize  = 0;

			for (int i = 0, count = prefabs.Filtered.Count; i < count; ++i)
			{
				PrefabInfo prefab = prefabs.Filtered[i];

				totalComponents += Mathf.Max(0, prefab.ComponentCount.Value);

				if (prefab.StateSize.Value > 0)
				{
					totalStateSize += prefab.StateSize.Value;
				}
			}

			_totalComponents.Value = totalComponents;
			_totalStateSize.Value  = totalStateSize;

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

			if (showIsSpawnable                     == true) { drawRect.xMin += drawRect.width; drawRect.width = _isSpawnableStat.Width;                     }
			if (showIsMasterClientObject            == true) { drawRect.xMin += drawRect.width; drawRect.width = _isMasterClientObjectStat.Width;            }
			if (showAllowStateAuthorityOverride     == true) { drawRect.xMin += drawRect.width; drawRect.width = _allowStateAuthorityOverrideStat.Width;     }
			if (showDestroyWhenStateAuthorityLeaves == true) { drawRect.xMin += drawRect.width; drawRect.width = _destroyWhenStateAuthorityLeavesStat.Width; }
			if (showInterestMode                    == true) { drawRect.xMin += drawRect.width; drawRect.width = _interestModeStat.Width;                    }

			if (showStateSize == true)
			{
				drawRect.xMin += drawRect.width;
				drawRect.width = _stateSizeStat.Width;

				EditorGUI.LabelField(drawRect, _totalStateSize.GetLabel(), GUIStyles.RightBoldLabel);
			}

			FusionInspector.RestoreLinePadding(ref totalRect);
		}

		private void DrawFieldsGUI(ref Rect sectionRect, Prefabs prefabs, EPrefabField visibleFields, EPrefabField toggleableFields)
		{
			if (prefabs.VisibleFieldsFoldout == false)
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

			DrawCategoryToggle(ref configRect, "Configuration", EPrefabField.Configuration);
			configRect.y += yOffset;

			DrawToggle(ref configRect, "Components (NB)",                     _componentsStat.Content.tooltip,                      EPrefabField.Components);
			DrawToggle(ref configRect, "Is Spawnable",                        _isSpawnableStat.Content.tooltip,                     EPrefabField.IsSpawnable);
			DrawToggle(ref configRect, "Is Master Client Object",             _isMasterClientObjectStat.Content.tooltip,            EPrefabField.IsMasterClientObject);
			DrawToggle(ref configRect, "Allow State Authority Override",      _allowStateAuthorityOverrideStat.Content.tooltip,     EPrefabField.AllowStateAuthorityOverride);
			DrawToggle(ref configRect, "Destroy When State Authority Leaves", _destroyWhenStateAuthorityLeavesStat.Content.tooltip, EPrefabField.DestroyWhenStateAuthorityLeaves);
			DrawToggle(ref configRect, "Object Interest Mode",                _interestModeStat.Content.tooltip,                    EPrefabField.InterestMode);

			configRect.y += yOffset;

			//----------------------------------------------------------------

			Rect otherRect = innerRect;
			otherRect.xMin = configRect.xMax + xOffset;
			otherRect.xMax = otherRect.xMin + _fieldsWidth2 * widthMultiplier;

			otherRect.y += yOffset;

			DrawCategoryToggle(ref otherRect, "Other", EPrefabField.Other);
			otherRect.y += yOffset;

			DrawToggle(ref otherRect, "State Size", _stateSizeStat.Content.tooltip, EPrefabField.StateSize);

			otherRect.y += yOffset;

			//----------------------------------------------------------------

			sectionRect.yMin = Mathf.Max(configRect.yMin, otherRect.yMin);

			return;

			void DrawCategoryToggle(ref Rect rect, string label, EPrefabField categoryFields)
			{
				EPrefabField enabledFields = prefabs.VisibleFields & categoryFields;

				bool allEnabled = enabledFields == categoryFields;

				EditorGUI.BeginDisabledGroup((toggleableFields & categoryFields) == EPrefabField.None);

				EditorGUI.showMixedValue = allEnabled == false && enabledFields != EPrefabField.None;
				bool toggleState = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width - FusionInspector.CategorySpacing * widthMultiplier, rect.height), label, allEnabled, EditorStyles.boldLabel);
				EditorGUI.showMixedValue = false;

				EditorGUI.EndDisabledGroup();

				if (toggleState != allEnabled)
				{
					prefabs.SetVisibleFields(categoryFields, toggleState);
				}

				rect.y += rect.height;

				TextureUtility.DrawTexture(new Rect(rect.x, rect.y + 2.0f, rect.width - FusionInspector.CategorySpacing * widthMultiplier, FusionInspector.SeparatorSize), FusionInspector.SeparatorColor);
			}

			void DrawToggle(ref Rect rect, string label, string tooltip, EPrefabField field)
			{
				_toggleContent.text    = label;
				_toggleContent.tooltip = tooltip;

				EditorGUI.BeginDisabledGroup(toggleableFields.Has(field) == false);

				bool previousState = prefabs.VisibleFields.Has(field);
				bool currentState  = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width - FusionInspector.CategorySpacing * widthMultiplier, rect.height), _toggleContent, previousState);

				EditorGUI.EndDisabledGroup();

				if (currentState != previousState)
				{
					prefabs.ToggleVisibleField(field);
				}

				rect.y += rect.height;
			}
		}

		private void DrawComponentFilterGUI(ref Rect sectionRect, Prefabs prefabs, Components components)
		{
			prefabs.ComponentFilterInclude.Clear();
			prefabs.ComponentFilterExclude.Clear();

			if (prefabs.ComponentFilterFoldout == false)
			{
				for (int i = 0, count = components.All.Count; i < count; ++i)
				{
					ComponentInfo component = components.All[i];

					if (component.Prefabs.FilterMode == EFilterMode.Include)
					{
						prefabs.ComponentFilterInclude.Add(component.Type);
					}
					else if (component.Prefabs.FilterMode == EFilterMode.Exclude)
					{
						prefabs.ComponentFilterExclude.Add(component.Type);
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

				ComponentInfo.ObjectContext context = component.Prefabs;

				if (EditorGUI.ToggleLeft(componentRect, context.ObjectFilter.GetLabel(component.TypeName, context.AllObjectCount.Value, context.FilteredObjectCount.Value), false, context.FilteredObjectCount.Value > 0 ? GUIStyles.ActiveToggle : GUIStyles.InactiveToggle) == true)
				{
					context.FilterMode = (EFilterMode)(((int)context.FilterMode + 1) % FusionInspector.FILTER_MODES);
				}

				if (context.FilterMode == EFilterMode.Include)
				{
					prefabs.ComponentFilterInclude.Add(component.Type);

					EditorGUI.LabelField(componentRect, GUISymbols.CHECKMARK);
				}
				else if (context.FilterMode == EFilterMode.Exclude)
				{
					prefabs.ComponentFilterExclude.Add(component.Type);

					EditorGUI.LabelField(componentRect, GUISymbols.CROSS);
				}
			}

			_tempComponents.Clear();
		}

		private void DrawFilterBarGUI(Rect window, ref Rect region, Prefabs prefabs)
		{
			bool hasInterestModeFilter = prefabs.InterestModeFilter >= 0;

			if (hasInterestModeFilter == false)
				return;

			if (_interestModeFilterMode != prefabs.InterestModeFilter)
			{
				_interestModeFilterMode = prefabs.InterestModeFilter;
				_interestModeFilterContent.text = $"Interest: {_interestModeContents[_interestModeFilterMode].text}";
			}

			Rect barRect = region;
			barRect.height = FusionInspector.LineHeight;

			TextureUtility.DrawTexture(new Rect(0.0f, barRect.yMin, window.width, barRect.height), FusionInspector.HeaderColor);

			region.yMin += barRect.height;

			if (DrawFilterChip(ref barRect, _interestModeFilterContent) == true)
			{
				prefabs.InterestModeFilter = -1;
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
			Prefabs prefabs = db.Prefabs;
			if (prefabs.All.Count <= 0)
				return;

			_tempScripts.Clear();
			for (int i = 0, count = prefabs.Filtered.Count; i < count; ++i)
			{
				PrefabInfo prefabInfo = prefabs.Filtered[i];
				if (prefabInfo.IsSelected == true)
				{
					for (int j = 0, countJ = prefabInfo.Components.Length; j < countJ; ++j)
					{
						_tempScripts.Add(prefabInfo.Components[j]);
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

			Rect scrollViewRect = new Rect(0.0f, 0.0f, content.width, FusionInspector.LineHeight * _tempScripts.Count);
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
			GUI.Label(typeRect, "State", _stateSizeStat.Style);

			FusionInspector.RestoreLinePadding(ref headerRect);

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			content.yMin += headerRect.height + 0.5f;
			content.yMax -= 0.5f;

			_scriptScrollPosition = GUI.BeginScrollView(content, _scriptScrollPosition, scrollViewRect);

			float minScrollY = _scriptScrollPosition.y;
			float maxScrollY = _scriptScrollPosition.y + content.height;
			Rect  drawRect   = new Rect(FusionInspector.LeftWindowOffset, 0.0f, _objectStat.Width - widthOffset, FusionInspector.LineHeight);

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

				GUI.Label(drawRect, component.StateSize.GetLabel(), GUIStyles.RightLabel);

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			GUISeparators.DrawAll(new Rect(0.0f, 0.0f, content.width, region.height), headerRect.yMin, headerRect.height, headerRect.yMax, region.height - headerRect.height);
		}

		private static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			FusionInspector.DrawHeader(stat, ref rect, ref filter);
		}

		private static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref EPrefabSortMode sortMode, EPrefabSortMode primarySortMode, EPrefabSortMode secondarySortMode)
		{
			int intSortMode = (int)sortMode;
			FusionInspector.DrawHeader(stat, icon, ref rect, ref intSortMode, (int)primarySortMode, (int)secondarySortMode);
			sortMode = (EPrefabSortMode)intSortMode;
		}

		private static void ToggleSortMode(ref EPrefabSortMode sortMode, EPrefabSortMode primaryMode, EPrefabSortMode secondaryMode)
		{
			sortMode = sortMode == primaryMode ? secondaryMode : primaryMode;
		}
	}
}
