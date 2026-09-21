namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	internal sealed class ComponentsInspector
	{
		private Vector2               _scrollPosition;
		private object                _selectionObject;
		private double                _selectionTime;
		private List<PrefabInfo>      _tempPrefabs = new List<PrefabInfo>();
		private List<SceneObjectInfo> _tempSceneObjects = new List<SceneObjectInfo>();

		private GUINameStat _componentStat;

		private GUIIconStat _hasSpawnedStat;
		private GUIIconStat _hasDespawnedStat;
		private GUIIconStat _hasFixedUpdateNetworkStat;
		private GUIIconStat _hasRenderStat;

		private GUIIconStat _hasAfterSpawnedStat;
		private GUIIconStat _hasAfterHostMigrationStat;
		private GUIIconStat _hasStateAuthorityChangedStat;
		private GUIIconStat _hasInputAuthorityGainedStat;
		private GUIIconStat _hasInputAuthorityLostStat;
		private GUIIconStat _hasSimulationEnterStat;
		private GUIIconStat _hasSimulationExitStat;
		private GUIIconStat _hasInterestEnterStat;
		private GUIIconStat _hasInterestExitStat;

		private GUIIconStat _hasBeforeUpdateStat;
		private GUIIconStat _hasBeforeCopyPreviousStateStat;
		private GUIIconStat _hasBeforeClientPredictionResetStat;
		private GUIIconStat _hasAfterClientPredictionResetStat;
		private GUIIconStat _hasBeforeSimulationStat;
		private GUIIconStat _hasBeforeAllTicksStat;
		private GUIIconStat _hasBeforeTickStat;
		private GUIIconStat _hasAfterTickStat;
		private GUIIconStat _hasAfterAllTicksStat;
		private GUIIconStat _hasAfterRenderStat;
		private GUIIconStat _hasAfterUpdateStat;

		private GUISortStat _sceneObjectsStat;
		private GUISortStat _prefabsStat;
		private GUISortStat _propertiesStat;
		private GUISortStat _rpcsStat;
		private GUISortStat _stateSizeStat;
		private GUISortStat _executionOrderStat;
		private GUISortStat _totalStateChangesStat;
		private GUISortStat _averageStateChangesStat;

		private GUIContent _sceneObjectsContent;
		private GUIContent _sceneObjectsIconContent;
		private GUIContent _prefabsContent;
		private GUIContent _prefabsIconContent;
		private GUIContent _propertiesContent;
		private GUIContent _rpcsContent;
		private GUIContent _stateSizeContent;
		private GUIContent _executionOrderContent;
		private GUIContent _scriptContent;
		private GUIContent _fieldsContent;
		private GUIContent _toggleContent;

		private float _sortWidth;
		private float _fieldsWidth1;
		private float _fieldsWidth2;
		private float _fieldsWidth3;
		private float _fieldsWidth4;

		public ComponentsInspector()
		{
			Texture2D gameObjectIcon = EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D;
			Texture2D prefabIcon     = EditorGUIUtility.IconContent("d_Prefab Icon").image as Texture2D;
			Texture2D scriptIcon     = EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D;
			Texture2D fieldsIcon     = EditorGUIUtility.IconContent("d__Menu@2x").image as Texture2D;

			float headerHeight   = FusionInspector.HeaderHeight;
			float stateSizeWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent("8888 B")).x;

			_componentStat = new GUINameStat(GUIStyles.LeftBoldLabel, "Type", "Name of the component", (int)EComponentSortMode.TypeHierarchyAscending, (int)EComponentSortMode.TypeHierarchyDescending);

			_hasSpawnedStat                     = new GUIIconStat(GUIStyles.Icon, "Ⓢ", "✔", $"Overrides {nameof(NetworkBehaviour.Spawned)}()", headerHeight);
			_hasDespawnedStat                   = new GUIIconStat(GUIStyles.Icon, "Ⓓ", "✔", $"Overrides {nameof(NetworkBehaviour.Despawned)}()", headerHeight);
			_hasFixedUpdateNetworkStat          = new GUIIconStat(GUIStyles.Icon, "Ⓕ", "✔", $"Overrides {nameof(NetworkBehaviour.FixedUpdateNetwork)}()", headerHeight);
			_hasRenderStat                      = new GUIIconStat(GUIStyles.Icon, "Ⓡ", "✔", $"Overrides {nameof(NetworkBehaviour.Render)}()", headerHeight);

			_hasAfterSpawnedStat                = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterSpawned)}", headerHeight);
			_hasAfterHostMigrationStat          = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterHostMigration)}", headerHeight);
			_hasStateAuthorityChangedStat       = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IStateAuthorityChanged)}", headerHeight);
			_hasInputAuthorityGainedStat        = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IInputAuthorityGained)}", headerHeight);
			_hasInputAuthorityLostStat          = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IInputAuthorityLost)}", headerHeight);
			_hasSimulationEnterStat             = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(ISimulationEnter)}", headerHeight);
			_hasSimulationExitStat              = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(ISimulationExit)}", headerHeight);
			_hasInterestEnterStat               = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IInterestEnter)}", headerHeight);
			_hasInterestExitStat                = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IInterestExit)}", headerHeight);

			_hasBeforeUpdateStat                = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeUpdate)}", headerHeight);
			_hasBeforeCopyPreviousStateStat     = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeCopyPreviousState)}", headerHeight);
			_hasBeforeClientPredictionResetStat = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeClientPredictionReset)}", headerHeight);
			_hasAfterClientPredictionResetStat  = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterClientPredictionReset)}", headerHeight);
			_hasBeforeSimulationStat            = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeSimulation)}", headerHeight);
			_hasBeforeAllTicksStat              = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeAllTicks)}", headerHeight);
			_hasBeforeTickStat                  = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IBeforeTick)}", headerHeight);
			_hasAfterTickStat                   = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterTick)}", headerHeight);
			_hasAfterAllTicksStat               = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterAllTicks)}", headerHeight);
			_hasAfterRenderStat                 = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterRender)}", headerHeight);
			_hasAfterUpdateStat                 = new GUIIconStat(GUIStyles.Icon, "Ⓘ", "✔", $"Implements {nameof(IAfterUpdate)}", headerHeight);

			_sceneObjectsStat        = new GUISortStat(GUIStyles.RightBoldLabel, "     ", "# of Scene Objects with the component", (int)EComponentSortMode.SceneObjectsAscending, (int)EComponentSortMode.SceneObjectsDescending);
			_prefabsStat             = new GUISortStat(GUIStyles.RightBoldLabel, "     ", "# of Prefabs with the component", (int)EComponentSortMode.PrefabsAscending, (int)EComponentSortMode.PrefabsDescending);
			_propertiesStat          = new GUISortStat(GUIStyles.RightBoldLabel, "Properties", "# of [Networked] properties declared in the component", (int)EComponentSortMode.PropertiesAscending, (int)EComponentSortMode.PropertiesDescending);
			_rpcsStat                = new GUISortStat(GUIStyles.RightBoldLabel, "RPCs", "# of RPCs declared in the component", (int)EComponentSortMode.RPCsAscending, (int)EComponentSortMode.RPCsDescending);
			_stateSizeStat           = new GUISortStat(GUIStyles.RightBoldLabel, "State", "Size of the networked state in Bytes", (int)EComponentSortMode.StateSizeAscending, (int)EComponentSortMode.StateSizeDescending, stateSizeWidth);
			_executionOrderStat      = new GUISortStat(GUIStyles.RightBoldLabel, "Execution", "Script execution order", (int)EComponentSortMode.ExecutionOrderAscending, (int)EComponentSortMode.ExecutionOrderDescending);
			_totalStateChangesStat   = new GUISortStat(GUIStyles.RightBoldLabel, "Σ Changes", "Total changes in networked state in Bytes", (int)EComponentSortMode.TotalStateChangesAscending, (int)EComponentSortMode.TotalStateChangesDescending);
			_averageStateChangesStat = new GUISortStat(GUIStyles.RightBoldLabel, "Ø Changes", "Average changes in networked state in Bytes per second", (int)EComponentSortMode.AverageStateChangesAscending, (int)EComponentSortMode.AverageStateChangesDescending);

			_sceneObjectsContent     = new GUIContent("", "# of Scene Objects with this component");
			_sceneObjectsIconContent = new GUIContent(gameObjectIcon, "# of Scene Objects with this component");
			_prefabsContent          = new GUIContent("", "# of Prefabs with this component");
			_prefabsIconContent      = new GUIContent(prefabIcon, "# of Prefabs with this component");
			_propertiesContent       = new GUIContent("", "# of [Networked] properties declared in the component");
			_rpcsContent             = new GUIContent("", "# of RPCs declared in this component");
			_stateSizeContent        = new GUIContent("", "Size of the networked state in Bytes");
			_executionOrderContent   = new GUIContent("", "Script execution order");
			_scriptContent           = new GUIContent(scriptIcon);
			_fieldsContent           = new GUIContent(fieldsIcon, "Visible fields");
			_toggleContent           = new GUIContent();

			_sortWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"{GUISymbols.ARROW_UP} ")).x - GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"")).x;

			_sceneObjectsStat.Width        += _sortWidth;
			_prefabsStat.Width             += _sortWidth;
			_propertiesStat.Width          += _sortWidth;
			_rpcsStat.Width                += _sortWidth;
			_stateSizeStat.Width           += _sortWidth;
			_executionOrderStat.Width      += _sortWidth;
			_totalStateChangesStat.Width   += _sortWidth;
			_averageStateChangesStat.Width += _sortWidth;

			_fieldsWidth1 = FusionInspector.GetCategoryWidth("Virtual Methods", "Spawned()", "Despawned()", "FixedUpdateNetwork()", "Render()");
			_fieldsWidth2 = FusionInspector.GetCategoryWidth("Interfaces (1)", "IAfterSpawned", "IAfterHostMigration", "IStateAuthorityChanged", "IInputAuthorityGained", "IInputAuthorityLost", "ISimulationEnter", "ISimulationExit", "IInterestEnter", "IInterestExit");
			_fieldsWidth3 = FusionInspector.GetCategoryWidth("Interfaces (2)", "IBeforeUpdate", "IBeforeCopyPreviousState", "IBeforeClientPredictionReset", "IAfterClientPredictionReset", "IBeforeSimulation", "IBeforeAllTicks", "IBeforeTick", "IAfterTick", "IAfterAllTicks", "IAfterRender", "IAfterUpdate");
			_fieldsWidth4 = FusionInspector.GetCategoryWidth("Other", "Scene Objects", "Prefabs", "Properties", "RPCs", "State Size", "Execution Order", "Σ State Changes", "Ø State Changes");
		}

		public void DrawGUI(Rect window, Rect region, FusionInspectorDB db)
		{
			GameMode        gameMode         = db.CurrentRunner != null ? db.CurrentRunner.GameMode : default;
			EComponentField visibleFields    = db.Components.GetVisibleFields() | EComponentField.Type;
			EComponentField toggleableFields = ~EComponentField.Type;

			bool showRuntimeInfo = gameMode != default;
			if (showRuntimeInfo == false)
			{
				visibleFields &= ~EComponentField.TotalStateChanges;
				visibleFields &= ~EComponentField.AverageStateChanges;

				toggleableFields &= ~EComponentField.TotalStateChanges;
				toggleableFields &= ~EComponentField.AverageStateChanges;
			}

			DrawComponentsGUI(window, region, db, visibleFields, toggleableFields);
		}

		private void DrawComponentsGUI(Rect window, Rect region, FusionInspectorDB db, EComponentField visibleFields, EComponentField toggleableFields)
		{
			Components components = db.Components;

			bool showHasSpawned                     = visibleFields.Has(EComponentField.HasSpawned);
			bool showHasDespawned                   = visibleFields.Has(EComponentField.HasDespawned);
			bool showHasFixedUpdateNetwork          = visibleFields.Has(EComponentField.HasFixedUpdateNetwork);
			bool showHasRender                      = visibleFields.Has(EComponentField.HasRender);

			bool showHasAfterSpawned                = visibleFields.Has(EComponentField.HasAfterSpawned);
			bool showHasAfterHostMigration          = visibleFields.Has(EComponentField.HasAfterHostMigration);
			bool showStateAuthorityChanged          = visibleFields.Has(EComponentField.HasStateAuthorityChanged);
			bool showInputAuthorityGained           = visibleFields.Has(EComponentField.HasInputAuthorityGained);
			bool showInputAuthorityLost             = visibleFields.Has(EComponentField.HasInputAuthorityLost);
			bool showHasSimulationEnter             = visibleFields.Has(EComponentField.HasSimulationEnter);
			bool showHasSimulationExit              = visibleFields.Has(EComponentField.HasSimulationExit);
			bool showHasInterestEnter               = visibleFields.Has(EComponentField.HasInterestEnter);
			bool showHasInterestExit                = visibleFields.Has(EComponentField.HasInterestExit);

			bool showHasBeforeUpdate                = visibleFields.Has(EComponentField.HasBeforeUpdate);
			bool showHasBeforeCopyPreviousState     = visibleFields.Has(EComponentField.HasBeforeCopyPreviousState);
			bool showHasBeforeClientPredictionReset = visibleFields.Has(EComponentField.HasBeforeClientPredictionReset);
			bool showHasAfterClientPredictionReset  = visibleFields.Has(EComponentField.HasAfterClientPredictionReset);
			bool showHasBeforeSimulation            = visibleFields.Has(EComponentField.HasBeforeSimulation);
			bool showHasBeforeAllTicks              = visibleFields.Has(EComponentField.HasBeforeAllTicks);
			bool showHasBeforeTick                  = visibleFields.Has(EComponentField.HasBeforeTick);
			bool showHasAfterTick                   = visibleFields.Has(EComponentField.HasAfterTick);
			bool showHasAfterAllTicks               = visibleFields.Has(EComponentField.HasAfterAllTicks);
			bool showHasAfterRender                 = visibleFields.Has(EComponentField.HasAfterRender);
			bool showHasAfterUpdate                 = visibleFields.Has(EComponentField.HasAfterUpdate);

			bool showSceneObjects                   = visibleFields.Has(EComponentField.SceneObjects);
			bool showPrefabs                        = visibleFields.Has(EComponentField.Prefabs);
			bool showProperties                     = visibleFields.Has(EComponentField.Properties);
			bool showRPCs                           = visibleFields.Has(EComponentField.RPCs);
			bool showStateSize                      = visibleFields.Has(EComponentField.StateSize);
			bool showExecutionOrder                 = visibleFields.Has(EComponentField.ExecutionOrder);
			bool showTotalStateChanges              = visibleFields.Has(EComponentField.TotalStateChanges);
			bool showAverageStateChanges            = visibleFields.Has(EComponentField.AverageStateChanges);

			GUISeparators.Clear();

			region.xMin += FusionInspector.LeftWindowOffset;
			region.xMax -= FusionInspector.RightWindowOffset;

			_componentStat.Width = region.width;

			if (showHasSpawned                     == true) { _componentStat.Width -= _hasSpawnedStat.Width;                     } else { components.HasSpawnedFilter                     = default; }
			if (showHasDespawned                   == true) { _componentStat.Width -= _hasDespawnedStat.Width;                   } else { components.HasDespawnedFilter                   = default; }
			if (showHasFixedUpdateNetwork          == true) { _componentStat.Width -= _hasFixedUpdateNetworkStat.Width;          } else { components.HasFixedUpdateNetworkFilter          = default; }
			if (showHasRender                      == true) { _componentStat.Width -= _hasRenderStat.Width;                      } else { components.HasRenderFilter                      = default; }

			if (showHasAfterSpawned                == true) { _componentStat.Width -= _hasAfterSpawnedStat.Width;                } else { components.HasAfterSpawnedFilter                = default; }
			if (showHasAfterHostMigration          == true) { _componentStat.Width -= _hasAfterHostMigrationStat.Width;          } else { components.HasAfterHostMigrationFilter          = default; }
			if (showStateAuthorityChanged          == true) { _componentStat.Width -= _hasStateAuthorityChangedStat.Width;       } else { components.HasStateAuthorityChangedFilter       = default; }
			if (showInputAuthorityGained           == true) { _componentStat.Width -= _hasInputAuthorityGainedStat.Width;        } else { components.HasInputAuthorityGainedFilter        = default; }
			if (showInputAuthorityLost             == true) { _componentStat.Width -= _hasInputAuthorityLostStat.Width;          } else { components.HasInputAuthorityLostFilter          = default; }
			if (showHasSimulationEnter             == true) { _componentStat.Width -= _hasSimulationEnterStat.Width;             } else { components.HasSimulationEnterFilter             = default; }
			if (showHasSimulationExit              == true) { _componentStat.Width -= _hasSimulationExitStat.Width;              } else { components.HasSimulationExitFilter              = default; }
			if (showHasInterestEnter               == true) { _componentStat.Width -= _hasInterestEnterStat.Width;               } else { components.HasInterestEnterFilter               = default; }
			if (showHasInterestExit                == true) { _componentStat.Width -= _hasInterestExitStat.Width;                } else { components.HasInterestExitFilter                = default; }

			if (showHasBeforeUpdate                == true) { _componentStat.Width -= _hasBeforeUpdateStat.Width;                } else { components.HasBeforeUpdateFilter                = default; }
			if (showHasBeforeCopyPreviousState     == true) { _componentStat.Width -= _hasBeforeCopyPreviousStateStat.Width;     } else { components.HasBeforeCopyPreviousStateFilter     = default; }
			if (showHasBeforeClientPredictionReset == true) { _componentStat.Width -= _hasBeforeClientPredictionResetStat.Width; } else { components.HasBeforeClientPredictionResetFilter = default; }
			if (showHasAfterClientPredictionReset  == true) { _componentStat.Width -= _hasAfterClientPredictionResetStat.Width;  } else { components.HasAfterClientPredictionResetFilter  = default; }
			if (showHasBeforeSimulation            == true) { _componentStat.Width -= _hasBeforeSimulationStat.Width;            } else { components.HasBeforeSimulationFilter            = default; }
			if (showHasBeforeAllTicks              == true) { _componentStat.Width -= _hasBeforeAllTicksStat.Width;              } else { components.HasBeforeAllTicksFilter              = default; }
			if (showHasBeforeTick                  == true) { _componentStat.Width -= _hasBeforeTickStat.Width;                  } else { components.HasBeforeTickFilter                  = default; }
			if (showHasAfterTick                   == true) { _componentStat.Width -= _hasAfterTickStat.Width;                   } else { components.HasAfterTickFilter                   = default; }
			if (showHasAfterAllTicks               == true) { _componentStat.Width -= _hasAfterAllTicksStat.Width;               } else { components.HasAfterAllTicksFilter               = default; }
			if (showHasAfterRender                 == true) { _componentStat.Width -= _hasAfterRenderStat.Width;                 } else { components.HasAfterRenderFilter                 = default; }
			if (showHasAfterUpdate                 == true) { _componentStat.Width -= _hasAfterUpdateStat.Width;                 } else { components.HasAfterUpdateFilter                 = default; }

			if (showSceneObjects                   == true) { _componentStat.Width -= _sceneObjectsStat.Width;        }
			if (showPrefabs                        == true) { _componentStat.Width -= _prefabsStat.Width;             }
			if (showProperties                     == true) { _componentStat.Width -= _propertiesStat.Width;          }
			if (showRPCs                           == true) { _componentStat.Width -= _rpcsStat.Width;                }
			if (showStateSize                      == true) { _componentStat.Width -= _stateSizeStat.Width;           }
			if (showExecutionOrder                 == true) { _componentStat.Width -= _executionOrderStat.Width;      }
			if (showTotalStateChanges              == true) { _componentStat.Width -= _totalStateChangesStat.Width;   }
			if (showAverageStateChanges            == true) { _componentStat.Width -= _averageStateChangesStat.Width; }

			Rect headerRect = region;
			headerRect.width  = _componentStat.Width;
			headerRect.height = FusionInspector.HeaderHeight;

			GUISeparators.AddY(headerRect.yMin);
			GUISeparators.AddY(headerRect.yMax);

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, window.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			GUIContent typeContent = _componentStat.GetContent(components.All.Count, components.Filtered.Count, default, (int)components.SortMode);

			Rect typeRect = headerRect;
			typeRect.xMin += _componentStat.IsSorted((int)components.SortMode) ? 0.0f : _sortWidth;

			Rect typeFilterRect = headerRect;
			typeFilterRect.xMax -= 4.0f;
			FusionInspector.ApplyFilterRect(ref typeFilterRect, typeRect.xMin + _componentStat.Style.CalcSize(typeContent).x);
			components.TypeNameFilter = FusionInspector.DrawFilterField(typeFilterRect, components.TypeNameFilter);

			typeRect.xMax = typeFilterRect.xMin;
			if (GUI.Button(typeRect, typeContent, _componentStat.Style) == true)
			{
				ToggleSortMode(ref components.SortMode, EComponentSortMode.TypeHierarchyAscending, EComponentSortMode.TypeHierarchyDescending);
			}

			if (showHasSpawned                     == true) { DrawHeader(_hasSpawnedStat,                     ref headerRect, ref components.HasSpawnedFilter);                     }
			if (showHasDespawned                   == true) { DrawHeader(_hasDespawnedStat,                   ref headerRect, ref components.HasDespawnedFilter);                   }
			if (showHasFixedUpdateNetwork          == true) { DrawHeader(_hasFixedUpdateNetworkStat,          ref headerRect, ref components.HasFixedUpdateNetworkFilter);          }
			if (showHasRender                      == true) { DrawHeader(_hasRenderStat,                      ref headerRect, ref components.HasRenderFilter);                      }

			if (showHasAfterSpawned                == true) { DrawHeader(_hasAfterSpawnedStat,                ref headerRect, ref components.HasAfterSpawnedFilter);                }
			if (showHasAfterHostMigration          == true) { DrawHeader(_hasAfterHostMigrationStat,          ref headerRect, ref components.HasAfterHostMigrationFilter);          }
			if (showStateAuthorityChanged          == true) { DrawHeader(_hasStateAuthorityChangedStat,       ref headerRect, ref components.HasStateAuthorityChangedFilter);       }
			if (showInputAuthorityGained           == true) { DrawHeader(_hasInputAuthorityGainedStat,        ref headerRect, ref components.HasInputAuthorityGainedFilter);        }
			if (showInputAuthorityLost             == true) { DrawHeader(_hasInputAuthorityLostStat,          ref headerRect, ref components.HasInputAuthorityLostFilter);          }
			if (showHasSimulationEnter             == true) { DrawHeader(_hasSimulationEnterStat,             ref headerRect, ref components.HasSimulationEnterFilter);             }
			if (showHasSimulationExit              == true) { DrawHeader(_hasSimulationExitStat,              ref headerRect, ref components.HasSimulationExitFilter);              }
			if (showHasInterestEnter               == true) { DrawHeader(_hasInterestEnterStat,               ref headerRect, ref components.HasInterestEnterFilter);               }
			if (showHasInterestExit                == true) { DrawHeader(_hasInterestExitStat,                ref headerRect, ref components.HasInterestExitFilter);                }

			if (showHasBeforeUpdate                == true) { DrawHeader(_hasBeforeUpdateStat,                ref headerRect, ref components.HasBeforeUpdateFilter);                }
			if (showHasBeforeCopyPreviousState     == true) { DrawHeader(_hasBeforeCopyPreviousStateStat,     ref headerRect, ref components.HasBeforeCopyPreviousStateFilter);     }
			if (showHasBeforeClientPredictionReset == true) { DrawHeader(_hasBeforeClientPredictionResetStat, ref headerRect, ref components.HasBeforeClientPredictionResetFilter); }
			if (showHasAfterClientPredictionReset  == true) { DrawHeader(_hasAfterClientPredictionResetStat,  ref headerRect, ref components.HasAfterClientPredictionResetFilter);  }
			if (showHasBeforeSimulation            == true) { DrawHeader(_hasBeforeSimulationStat,            ref headerRect, ref components.HasBeforeSimulationFilter);            }
			if (showHasBeforeAllTicks              == true) { DrawHeader(_hasBeforeAllTicksStat,              ref headerRect, ref components.HasBeforeAllTicksFilter);              }
			if (showHasBeforeTick                  == true) { DrawHeader(_hasBeforeTickStat,                  ref headerRect, ref components.HasBeforeTickFilter);                  }
			if (showHasAfterTick                   == true) { DrawHeader(_hasAfterTickStat,                   ref headerRect, ref components.HasAfterTickFilter);                   }
			if (showHasAfterAllTicks               == true) { DrawHeader(_hasAfterAllTicksStat,               ref headerRect, ref components.HasAfterAllTicksFilter);               }
			if (showHasAfterRender                 == true) { DrawHeader(_hasAfterRenderStat,                 ref headerRect, ref components.HasAfterRenderFilter);                 }
			if (showHasAfterUpdate                 == true) { DrawHeader(_hasAfterUpdateStat,                 ref headerRect, ref components.HasAfterUpdateFilter);                 }

			if (showSceneObjects                   == true) { DrawHeader(_sceneObjectsStat,        _sceneObjectsIconContent, ref headerRect, ref components.SortMode, EComponentSortMode.SceneObjectsDescending,        EComponentSortMode.SceneObjectsAscending);        }
			if (showPrefabs                        == true) { DrawHeader(_prefabsStat,             _prefabsIconContent,      ref headerRect, ref components.SortMode, EComponentSortMode.PrefabsDescending,             EComponentSortMode.PrefabsAscending);             }
			if (showProperties                     == true) { DrawHeader(_propertiesStat,          null,                     ref headerRect, ref components.SortMode, EComponentSortMode.PropertiesDescending,          EComponentSortMode.PropertiesAscending);          }
			if (showRPCs                           == true) { DrawHeader(_rpcsStat,                null,                     ref headerRect, ref components.SortMode, EComponentSortMode.RPCsDescending,                EComponentSortMode.RPCsAscending);                }
			if (showStateSize                      == true) { DrawHeader(_stateSizeStat,           null,                     ref headerRect, ref components.SortMode, EComponentSortMode.StateSizeDescending,           EComponentSortMode.StateSizeAscending);           }
			if (showExecutionOrder                 == true) { DrawHeader(_executionOrderStat,      null,                     ref headerRect, ref components.SortMode, EComponentSortMode.ExecutionOrderAscending,       EComponentSortMode.ExecutionOrderDescending);     }
			if (showTotalStateChanges              == true) { DrawHeader(_totalStateChangesStat,   null,                     ref headerRect, ref components.SortMode, EComponentSortMode.TotalStateChangesDescending,   EComponentSortMode.TotalStateChangesAscending);   }
			if (showAverageStateChanges            == true) { DrawHeader(_averageStateChangesStat, null,                     ref headerRect, ref components.SortMode, EComponentSortMode.AverageStateChangesDescending, EComponentSortMode.AverageStateChangesAscending); }

			headerRect.xMin += headerRect.width;
			headerRect.width = FusionInspector.RightWindowOffset;
			if (GUI.Button(headerRect, _fieldsContent, GUIStyles.Icon) == true)
			{
				components.VisibleFieldsFoldout = !components.VisibleFieldsFoldout;
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			region.yMin += headerRect.height;

			DrawFieldsGUI(ref region, components, visibleFields, toggleableFields);

			GUISeparators.AddY(region.y);

			Rect scrollRect     = new Rect(region.x, region.y, region.width + FusionInspector.RightWindowOffset, region.height);
			Rect scrollViewRect = new Rect(0.0f, 0.0f, region.width, FusionInspector.LineHeight * components.Filtered.Count);

			GUISeparators.AddX(headerRect.xMin);

			_scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, scrollViewRect);

			float   indent           = 16.0f;
			float   minScrollY       = _scrollPosition.y;
			float   maxScrollY       = _scrollPosition.y + region.height;
			Rect    drawRect         = new Rect(0.0f, 0.0f, 0.0f, FusionInspector.LineHeight);
			Vector2 mousePosition    = Event.current.mousePosition;
			bool    canShowHierarchy = components.All.Count == components.Filtered.Count && (components.SortMode == EComponentSortMode.TypeHierarchyAscending || components.SortMode == EComponentSortMode.TypeHierarchyDescending);

			for (int i = 0, count = components.Filtered.Count; i < count; ++i)
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

				ComponentInfo component = components.Filtered[i];

				drawRect.xMin  = 0.0f;
				drawRect.width = _componentStat.Width;

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

				int typeLevel = canShowHierarchy == true ? component.TypeLevel : 0;
				drawRect.xMin += typeLevel * indent;
				if (typeLevel > 0)
				{
					drawRect.xMin += GUIStyles.ObjectField.padding.left;
					EditorGUI.LabelField(new Rect(drawRect.x - indent, drawRect.y, indent, drawRect.height), GUISymbols.DERIVE, GUIStyles.Icon);
					drawRect.xMin -= GUIStyles.ObjectField.padding.left;
				}
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
					else
					{
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
				drawRect.xMin -= typeLevel * indent;

				if (showHasSpawned                     == true) { FusionInspector.DrawIcon(_hasSpawnedStat                     , ref drawRect, component.HasSpawned);                     }
				if (showHasDespawned                   == true) { FusionInspector.DrawIcon(_hasDespawnedStat                   , ref drawRect, component.HasDespawned);                   }
				if (showHasFixedUpdateNetwork          == true) { FusionInspector.DrawIcon(_hasFixedUpdateNetworkStat          , ref drawRect, component.HasFixedUpdateNetwork);          }
				if (showHasRender                      == true) { FusionInspector.DrawIcon(_hasRenderStat                      , ref drawRect, component.HasRender);                      }

				if (showHasAfterSpawned                == true) { FusionInspector.DrawIcon(_hasAfterSpawnedStat                , ref drawRect, component.HasAfterSpawned);                }
				if (showHasAfterHostMigration          == true) { FusionInspector.DrawIcon(_hasAfterHostMigrationStat          , ref drawRect, component.HasAfterHostMigration);          }
				if (showStateAuthorityChanged          == true) { FusionInspector.DrawIcon(_hasStateAuthorityChangedStat       , ref drawRect, component.HasStateAuthorityChanged);       }
				if (showInputAuthorityGained           == true) { FusionInspector.DrawIcon(_hasInputAuthorityGainedStat        , ref drawRect, component.HasInputAuthorityGained);        }
				if (showInputAuthorityLost             == true) { FusionInspector.DrawIcon(_hasInputAuthorityLostStat          , ref drawRect, component.HasInputAuthorityLost);          }
				if (showHasSimulationEnter             == true) { FusionInspector.DrawIcon(_hasSimulationEnterStat             , ref drawRect, component.HasSimulationEnter);             }
				if (showHasSimulationExit              == true) { FusionInspector.DrawIcon(_hasSimulationExitStat              , ref drawRect, component.HasSimulationExit);              }
				if (showHasInterestEnter               == true) { FusionInspector.DrawIcon(_hasInterestEnterStat               , ref drawRect, component.HasInterestEnter);               }
				if (showHasInterestExit                == true) { FusionInspector.DrawIcon(_hasInterestExitStat                , ref drawRect, component.HasInterestExit);                }

				if (showHasBeforeUpdate                == true) { FusionInspector.DrawIcon(_hasBeforeUpdateStat                , ref drawRect, component.HasBeforeUpdate);                }
				if (showHasBeforeCopyPreviousState     == true) { FusionInspector.DrawIcon(_hasBeforeCopyPreviousStateStat     , ref drawRect, component.HasBeforeCopyPreviousState);     }
				if (showHasBeforeClientPredictionReset == true) { FusionInspector.DrawIcon(_hasBeforeClientPredictionResetStat , ref drawRect, component.HasBeforeClientPredictionReset); }
				if (showHasAfterClientPredictionReset  == true) { FusionInspector.DrawIcon(_hasAfterClientPredictionResetStat  , ref drawRect, component.HasAfterClientPredictionReset);  }
				if (showHasBeforeSimulation            == true) { FusionInspector.DrawIcon(_hasBeforeSimulationStat            , ref drawRect, component.HasBeforeSimulation);            }
				if (showHasBeforeAllTicks              == true) { FusionInspector.DrawIcon(_hasBeforeAllTicksStat              , ref drawRect, component.HasBeforeAllTicks);              }
				if (showHasBeforeTick                  == true) { FusionInspector.DrawIcon(_hasBeforeTickStat                  , ref drawRect, component.HasBeforeTick);                  }
				if (showHasAfterTick                   == true) { FusionInspector.DrawIcon(_hasAfterTickStat                   , ref drawRect, component.HasAfterTick);                   }
				if (showHasAfterAllTicks               == true) { FusionInspector.DrawIcon(_hasAfterAllTicksStat               , ref drawRect, component.HasAfterAllTicks);               }
				if (showHasAfterRender                 == true) { FusionInspector.DrawIcon(_hasAfterRenderStat                 , ref drawRect, component.HasAfterRender);                 }
				if (showHasAfterUpdate                 == true) { FusionInspector.DrawIcon(_hasAfterUpdateStat                 , ref drawRect, component.HasAfterUpdate);                 }

				if (showSceneObjects == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _sceneObjectsStat.Width;

					if (component.SceneObjects.ScriptCount.Value > 0)
					{
						_sceneObjectsContent.text = component.SceneObjects.ScriptCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _sceneObjectsContent, GUIStyles.RightLabel) == true)
						{
							db.SceneObjects.GetObjects(_tempSceneObjects, component.Type, false);

							if (_tempSceneObjects.Count > 0)
							{
								GenericMenu menu = new GenericMenu();
								for (int objectIndex = 0; objectIndex < _tempSceneObjects.Count; ++objectIndex)
								{
									SceneObjectInfo sceneObject = _tempSceneObjects[objectIndex];
									menu.AddItem(new GUIContent($"ID:[{ObjectUtility.GetObjectId(sceneObject.GameObject)}] {sceneObject.Name}"), false, (gameObject) =>
									{
										GameObject go = gameObject as GameObject;
										if (go != null)
										{
											Selection.SetActiveObjectWithContext(go, go);
										}
									}, sceneObject.GameObject);
								}
								menu.ShowAsContext();
							}

							_tempSceneObjects.Clear();
						}
						drawRect.xMax += 2.0f;
					}
				}

				if (showPrefabs == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _prefabsStat.Width;

					if (component.Prefabs.ScriptCount.Value > 0)
					{
						_prefabsContent.text = component.Prefabs.ScriptCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _prefabsContent, GUIStyles.RightLabel) == true)
						{
							db.Prefabs.GetObjects(_tempPrefabs, component.Type, false);

							if (_tempPrefabs.Count > 0)
							{
								GenericMenu menu = new GenericMenu();
								for (int objectIndex = 0; objectIndex < _tempPrefabs.Count; ++objectIndex)
								{
									PrefabInfo prefab = _tempPrefabs[objectIndex];
									menu.AddItem(new GUIContent($"ID:[{ObjectUtility.GetObjectId(prefab.GameObject)}] {prefab.Name}"), false, (gameObject) =>
									{
										GameObject go = gameObject as GameObject;
										if (go != null)
										{
											Selection.SetActiveObjectWithContext(go, go);
										}
									}, prefab.GameObject);
								}
								menu.ShowAsContext();
							}

							_tempPrefabs.Clear();
						}
						drawRect.xMax += 2.0f;
					}
				}

				if (showProperties == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _propertiesStat.Width;

					if (component.PropertyCount.Value > 0)
					{
						_propertiesContent.text = component.PropertyCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _propertiesContent, GUIStyles.RightLabel) == true)
						{
							GenericMenu menu = new GenericMenu();
							foreach (System.Reflection.PropertyInfo property in component.Properties)
							{
								menu.AddItem(new GUIContent(ReflectionUtility.GetPropertyDeclaration(property)), false, (script) =>
								{
									MonoScript monoScript = script as MonoScript;
									if (monoScript != null)
									{
										int propertyLineNumber = ScriptUtility.GetLineNumber(property);
										AssetDatabase.OpenAsset(monoScript, propertyLineNumber);
									}
								}, component.Script);
							}
							menu.ShowAsContext();
						}
						drawRect.xMax += 2.0f;
					}
				}

				if (showRPCs == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _rpcsStat.Width;

					if (component.RPCCount.Value > 0)
					{
						_rpcsContent.text = component.RPCCount.GetLabel();
						drawRect.xMax -= 2.0f;
						if (GUI.Button(drawRect, _rpcsContent, GUIStyles.RightLabel) == true)
						{
							GenericMenu menu = new GenericMenu();
							foreach (System.Reflection.MethodInfo method in component.RPCs)
							{
								menu.AddItem(new GUIContent(ReflectionUtility.GetMethodDeclaration(method)), false, (script) =>
								{
									MonoScript monoScript = script as MonoScript;
									if (monoScript != null)
									{
										int methodLineNumber = ScriptUtility.GetLineNumber(method);
										AssetDatabase.OpenAsset(monoScript, methodLineNumber);
									}
								}, component.Script);
							}
							menu.ShowAsContext();
						}
						drawRect.xMax += 2.0f;
					}
				}

				if (showStateSize == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _stateSizeStat.Width;

					_stateSizeContent.text = component.StateSize.GetLabel();
					GUIColor.Set(component.StateSize.Value > 0 ? FusionInspector.NormalTextColor : FusionInspector.InactiveTextColor);
					EditorGUI.LabelField(drawRect, _stateSizeContent, GUIStyles.RightLabel);
					GUIColor.Reset();
				}

				if (showExecutionOrder)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _executionOrderStat.Width;

					if (component.ExecutionOrder.Value != 0)
					{
						_executionOrderContent.text = component.ExecutionOrder.GetLabel();
						EditorGUI.LabelField(drawRect, _executionOrderContent, GUIStyles.RightLabel);
					}
				}

				if (showTotalStateChanges == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _totalStateChangesStat.Width;

					if (component.TotalStateChanges.Value > 0)
					{
						EditorGUI.LabelField(drawRect, component.TotalStateChanges.GetLabel(), GUIStyles.RightLabel);
					}
				}

				if (showAverageStateChanges == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _averageStateChangesStat.Width;

					if (component.AverageStateChanges.Value > 0)
					{
						EditorGUI.LabelField(drawRect, component.AverageStateChanges.GetLabel(), GUIStyles.RightLabel);
					}
				}

				FusionInspector.RestoreLinePadding(ref drawRect);

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			GUIHighlights.DrawAll();

			float scrollHeight = Mathf.Min(scrollRect.height, scrollViewRect.height);

			GUISeparators.AddY(scrollRect.yMin + scrollHeight);

			GUISeparators.DrawAll(new Rect(0.0f, 0.0f, window.width, window.height), headerRect.yMin, headerRect.height, scrollRect.yMin, scrollHeight);
		}

		private void DrawFieldsGUI(ref Rect sectionRect, Components components, EComponentField visibleFields, EComponentField toggleableFields)
		{
			if (components.VisibleFieldsFoldout == false)
				return;

			float xOffset = 3.0f;
			float yOffset = 6.0f;

			Rect innerRect = sectionRect;
			innerRect.xMin += xOffset;
			innerRect.xMax -= xOffset;
			innerRect.height = EditorGUIUtility.singleLineHeight;

			float totalWidth      = _fieldsWidth1 + _fieldsWidth2 + _fieldsWidth3 + _fieldsWidth4;
			float widthMultiplier = Mathf.Min(1.0f, innerRect.width / totalWidth);

			//----------------------------------------------------------------

			Rect virtualMethodsRect = innerRect;
			virtualMethodsRect.xMin = innerRect.xMin;
			virtualMethodsRect.xMax = virtualMethodsRect.xMin + _fieldsWidth1 * widthMultiplier;

			virtualMethodsRect.y += yOffset;

			DrawCategoryToggle(ref virtualMethodsRect, "Virtual Methods", EComponentField.VirtualMethods);
			virtualMethodsRect.y += yOffset;

			DrawToggle(ref virtualMethodsRect, "Spawned()",            _hasSpawnedStat.Content.tooltip,            EComponentField.HasSpawned);
			DrawToggle(ref virtualMethodsRect, "Despawned()",          _hasDespawnedStat.Content.tooltip,          EComponentField.HasDespawned);
			DrawToggle(ref virtualMethodsRect, "FixedUpdateNetwork()", _hasFixedUpdateNetworkStat.Content.tooltip, EComponentField.HasFixedUpdateNetwork);
			DrawToggle(ref virtualMethodsRect, "Render()",             _hasRenderStat.Content.tooltip,             EComponentField.HasRender);

			virtualMethodsRect.y += yOffset;

			//----------------------------------------------------------------

			Rect interfaces1Rect = innerRect;
			interfaces1Rect.xMin = virtualMethodsRect.xMax + xOffset;
			interfaces1Rect.xMax = interfaces1Rect.xMin + _fieldsWidth2 * widthMultiplier;

			interfaces1Rect.y += yOffset;

			DrawCategoryToggle(ref interfaces1Rect, "Interfaces (1)", EComponentField.Interfaces1);
			interfaces1Rect.y += yOffset;

			DrawToggle(ref interfaces1Rect, "IAfterSpawned",                _hasAfterSpawnedStat.Content.tooltip,          EComponentField.HasAfterSpawned);
			DrawToggle(ref interfaces1Rect, "IAfterHostMigration",          _hasAfterHostMigrationStat.Content.tooltip,    EComponentField.HasAfterHostMigration);
			DrawToggle(ref interfaces1Rect, "IStateAuthorityChanged",       _hasStateAuthorityChangedStat.Content.tooltip, EComponentField.HasStateAuthorityChanged);
			DrawToggle(ref interfaces1Rect, "IInputAuthorityGained",        _hasInputAuthorityGainedStat.Content.tooltip,  EComponentField.HasInputAuthorityGained);
			DrawToggle(ref interfaces1Rect, "IInputAuthorityLost",          _hasInputAuthorityLostStat.Content.tooltip,    EComponentField.HasInputAuthorityLost);
			DrawToggle(ref interfaces1Rect, "ISimulationEnter",             _hasSimulationEnterStat.Content.tooltip,       EComponentField.HasSimulationEnter);
			DrawToggle(ref interfaces1Rect, "ISimulationExit",              _hasSimulationExitStat.Content.tooltip,        EComponentField.HasSimulationExit);
			DrawToggle(ref interfaces1Rect, "IInterestEnter",               _hasInterestEnterStat.Content.tooltip,         EComponentField.HasInterestEnter);
			DrawToggle(ref interfaces1Rect, "IInterestExit",                _hasInterestExitStat.Content.tooltip,          EComponentField.HasInterestExit);

			interfaces1Rect.y += yOffset;

			//----------------------------------------------------------------

			Rect interfaces2Rect = innerRect;
			interfaces2Rect.xMin = interfaces1Rect.xMax + xOffset;
			interfaces2Rect.xMax = interfaces2Rect.xMin + _fieldsWidth3 * widthMultiplier;

			interfaces2Rect.y += yOffset;

			DrawCategoryToggle(ref interfaces2Rect, "Interfaces (2)", EComponentField.Interfaces2);
			interfaces2Rect.y += yOffset;

			DrawToggle(ref interfaces2Rect, "IBeforeUpdate",                _hasBeforeUpdateStat.Content.tooltip,                EComponentField.HasBeforeUpdate);
			DrawToggle(ref interfaces2Rect, "IBeforeCopyPreviousState",     _hasBeforeCopyPreviousStateStat.Content.tooltip,     EComponentField.HasBeforeCopyPreviousState);
			DrawToggle(ref interfaces2Rect, "IBeforeClientPredictionReset", _hasBeforeClientPredictionResetStat.Content.tooltip, EComponentField.HasBeforeClientPredictionReset);
			DrawToggle(ref interfaces2Rect, "IAfterClientPredictionReset",  _hasAfterClientPredictionResetStat.Content.tooltip,  EComponentField.HasAfterClientPredictionReset);
			DrawToggle(ref interfaces2Rect, "IBeforeSimulation",            _hasBeforeSimulationStat.Content.tooltip,            EComponentField.HasBeforeSimulation);
			DrawToggle(ref interfaces2Rect, "IBeforeAllTicks",              _hasBeforeAllTicksStat.Content.tooltip,              EComponentField.HasBeforeAllTicks);
			DrawToggle(ref interfaces2Rect, "IBeforeTick",                  _hasBeforeTickStat.Content.tooltip,                  EComponentField.HasBeforeTick);
			DrawToggle(ref interfaces2Rect, "IAfterTick",                   _hasAfterTickStat.Content.tooltip,                   EComponentField.HasAfterTick);
			DrawToggle(ref interfaces2Rect, "IAfterAllTicks",               _hasAfterAllTicksStat.Content.tooltip,               EComponentField.HasAfterAllTicks);
			DrawToggle(ref interfaces2Rect, "IAfterRender",                 _hasAfterRenderStat.Content.tooltip,                 EComponentField.HasAfterRender);
			DrawToggle(ref interfaces2Rect, "IAfterUpdate",                 _hasAfterUpdateStat.Content.tooltip,                 EComponentField.HasAfterUpdate);

			interfaces2Rect.y += yOffset;

			//----------------------------------------------------------------

			Rect otherRect = innerRect;
			otherRect.xMin = interfaces2Rect.xMax + xOffset;
			otherRect.xMax = otherRect.xMin + _fieldsWidth4 * widthMultiplier;

			otherRect.y += yOffset;

			DrawCategoryToggle(ref otherRect, "Other", EComponentField.Other);
			otherRect.y += yOffset;

			DrawToggle(ref otherRect, "Scene Objects",   _sceneObjectsStat.Content.tooltip,        EComponentField.SceneObjects);
			DrawToggle(ref otherRect, "Prefabs",         _prefabsStat.Content.tooltip,             EComponentField.Prefabs);
			DrawToggle(ref otherRect, "Properties",      _propertiesStat.Content.tooltip,          EComponentField.Properties);
			DrawToggle(ref otherRect, "RPCs",            _rpcsStat.Content.tooltip,                EComponentField.RPCs);
			DrawToggle(ref otherRect, "State Size",      _stateSizeStat.Content.tooltip,           EComponentField.StateSize);
			DrawToggle(ref otherRect, "Execution Order", _executionOrderStat.Content.tooltip,      EComponentField.ExecutionOrder);
			DrawToggle(ref otherRect, "Σ State Changes", _totalStateChangesStat.Content.tooltip,   EComponentField.TotalStateChanges);
			DrawToggle(ref otherRect, "Ø State Changes", _averageStateChangesStat.Content.tooltip, EComponentField.AverageStateChanges);

			otherRect.y += yOffset;

			//----------------------------------------------------------------

			sectionRect.yMin = Mathf.Max(virtualMethodsRect.yMin, interfaces1Rect.yMin, interfaces2Rect.yMin, otherRect.yMin);

			return;

			void DrawCategoryToggle(ref Rect rect, string label, EComponentField categoryFields)
			{
				EComponentField enabledFields = components.GetVisibleFields() & categoryFields;

				bool allEnabled = enabledFields == categoryFields;

				EditorGUI.BeginDisabledGroup((toggleableFields & categoryFields) == EComponentField.None);

				EditorGUI.showMixedValue = allEnabled == false && enabledFields != EComponentField.None;
				bool toggleState = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width - FusionInspector.CategorySpacing * widthMultiplier, rect.height), label, allEnabled, EditorStyles.boldLabel);
				EditorGUI.showMixedValue = false;

				EditorGUI.EndDisabledGroup();

				if (toggleState != allEnabled)
				{
					components.SetVisibleFields(categoryFields, toggleState);
				}

				rect.y += rect.height;

				TextureUtility.DrawTexture(new Rect(rect.x, rect.y + 2.0f, rect.width - FusionInspector.CategorySpacing * widthMultiplier, FusionInspector.SeparatorSize), FusionInspector.SeparatorColor);
			}

			void DrawToggle(ref Rect rect, string label, string tooltip, EComponentField field)
			{
				_toggleContent.text    = label;
				_toggleContent.tooltip = tooltip;

				EditorGUI.BeginDisabledGroup(toggleableFields.Has(field) == false);

				bool previousState = components.GetVisibleFields().Has(field);
				bool currentState  = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width - FusionInspector.CategorySpacing * widthMultiplier, rect.height), _toggleContent, previousState);

				EditorGUI.EndDisabledGroup();

				if (currentState != previousState)
				{
					components.ToggleVisibleField(field);
				}

				rect.y += rect.height;
			}
		}

		private static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			FusionInspector.DrawHeader(stat, ref rect, ref filter);
		}

		private static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref EComponentSortMode sortMode, EComponentSortMode primarySortMode, EComponentSortMode secondarySortMode)
		{
			int intSortMode = (int)sortMode;
			FusionInspector.DrawHeader(stat, icon, ref rect, ref intSortMode, (int)primarySortMode, (int)secondarySortMode);
			sortMode = (EComponentSortMode)intSortMode;
		}

		private static void ToggleSortMode(ref EComponentSortMode sortMode, EComponentSortMode primaryMode, EComponentSortMode secondaryMode)
		{
			sortMode = sortMode == primaryMode ? secondaryMode : primaryMode;
		}
	}
}
