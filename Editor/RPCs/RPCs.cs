namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using UnityEditor;

	[Serializable]
	internal sealed class RPCs
	{
		[NonSerialized]
		public List<RPCInfo>   All      = new List<RPCInfo>(512);
		[NonSerialized]
		public List<RPCInfo>   Filtered = new List<RPCInfo>(512);
		public ERPCView        View;
		public string          RPCNameFilter;
		public string          ObjectNameFilter;
		public PlayerRef       EventSourceFilter;
		public PlayerRef       EventTargetFilter;
		public PlayerRef       EventStateAuthorityFilter;
		public PlayerRef       EventInputAuthorityFilter;
		public PlayerRef       EventAnyPlayerFilter;
		public NetworkObject   EventObjectFilter;
		public string          EventObjectFilterName;
		public EFilterMode     IsReliableFilter;
		public EFilterMode     IsTickAlignedFilter;
		public EFilterMode     HasRpcTargetFilter;
		public EFilterMode     InvokeLocalFilter;
		public bool            ComponentFilterFoldout;
		public int             ComponentFilterVersion;
		public ERPCSortMode    SortMode = ERPCSortMode.NameHierarchyAscending;
		public ERPCField       OverviewVisibleFields;
		public bool            OverviewVisibleFieldsFoldout;
		public ERPCEventField  EventsVisibleFields;
		public bool            EventsVisibleFieldsFoldout;

		public readonly HashSet<ComponentInfo>                 ComponentFilterInclude = new HashSet<ComponentInfo>();
		public readonly HashSet<ComponentInfo>                 ComponentFilterExclude = new HashSet<ComponentInfo>();
		public readonly Dictionary<ComponentInfo, EFilterMode> ComponentFilter        = new Dictionary<ComponentInfo, EFilterMode>();

		private NetworkRunner  _processedRunner;
		private List<RPCEvent> _processedEvents;
		private int            _processedEventCount;

		private Dictionary<MethodBase, RPCInfo> _allByMethod = new Dictionary<MethodBase, RPCInfo>();

		public RPCs()
		{
			RPCEventTracker.Initialize();
		}

		public bool TryGetInfo(MethodBase method, out RPCInfo rpc)
		{
			return _allByMethod.TryGetValue(method, out rpc);
		}

		public void Synchronize(Components components)
		{
			if (All.Count > 0)
				return;

			_allByMethod.Clear();

			foreach (ComponentInfo component in components.All)
			{
				foreach (MethodInfo rpcMethod in component.RPCs)
				{
					RPCInfo rpcInfo = new RPCInfo(component, rpcMethod);
					_allByMethod[rpcMethod] = rpcInfo;
					All.Add(rpcInfo);
				}
			}
		}

		public void Refresh(Components components, NetworkRunner runner)
		{
			RPCEventTracker.TryGetEvents(runner, out List<RPCEvent> rpcEvents);

			if (runner != _processedRunner || rpcEvents != _processedEvents)
			{
				_processedRunner     = runner;
				_processedEvents     = rpcEvents;
				_processedEventCount = 0;

				foreach (RPCInfo rpc in All)
				{
					rpc.ClearCounts();
				}
			}

			if (rpcEvents != null)
			{
				for (int i = _processedEventCount, count = rpcEvents.Count; i < count; ++i)
				{
					RPCEvent rpcEvent = rpcEvents[i];
					if (rpcEvent.Method != null && _allByMethod.TryGetValue(rpcEvent.Method, out RPCInfo rpc) == true)
					{
						rpc.ProcessEvent(rpcEvent);
					}
				}

				_processedEventCount = rpcEvents.Count;
			}

			foreach (RPCInfo rpc in All)
			{
				rpc.UpdateCounts();
			}

			Filtered.Clear();

			Sort(All, SortMode);

			bool hasNameFilter          = string.IsNullOrWhiteSpace(RPCNameFilter) == false;
			bool hasIsReliableFilter    = IsReliableFilter    != EFilterMode.None;
			bool hasIsTickAlignedFilter = IsTickAlignedFilter != EFilterMode.None;
			bool hasHasRpcTargetFilter  = HasRpcTargetFilter  != EFilterMode.None;
			bool hasInvokeLocalFilter   = InvokeLocalFilter   != EFilterMode.None;

			bool hasAnyFilter  = false;
			hasAnyFilter |= hasNameFilter;
			hasAnyFilter |= hasIsReliableFilter;
			hasAnyFilter |= hasIsTickAlignedFilter;
			hasAnyFilter |= hasHasRpcTargetFilter;
			hasAnyFilter |= hasInvokeLocalFilter;

			foreach (RPCInfo rpc in All)
			{
				bool isFiltered = true;

				if (hasAnyFilter == true)
				{
					if (hasNameFilter          == true) isFiltered &= rpc.MethodName.Contains(RPCNameFilter, StringComparison.OrdinalIgnoreCase);
					if (hasIsReliableFilter    == true) isFiltered &= (IsReliableFilter    == EFilterMode.Include && rpc.IsReliable    == true) || (IsReliableFilter    == EFilterMode.Exclude && rpc.IsReliable    == false);
					if (hasIsTickAlignedFilter == true) isFiltered &= (IsTickAlignedFilter == EFilterMode.Include && rpc.IsTickAligned == true) || (IsTickAlignedFilter == EFilterMode.Exclude && rpc.IsTickAligned == false);
					if (hasHasRpcTargetFilter  == true) isFiltered &= (HasRpcTargetFilter  == EFilterMode.Include && rpc.HasRpcTarget  == true) || (HasRpcTargetFilter  == EFilterMode.Exclude && rpc.HasRpcTarget  == false);
					if (hasInvokeLocalFilter   == true) isFiltered &= (InvokeLocalFilter   == EFilterMode.Include && rpc.InvokeLocal   == true) || (InvokeLocalFilter   == EFilterMode.Exclude && rpc.InvokeLocal   == false);
				}

				if (isFiltered == true)
				{
					Filtered.Add(rpc);
				}
			}
		}

		public void LoadEditorPrefs()
		{
			ERPCField defaultOverviewFields = ERPCField.Name;
			defaultOverviewFields |= ERPCField.Sources;
			defaultOverviewFields |= ERPCField.Targets;
			defaultOverviewFields |= ERPCField.IsReliable;
			defaultOverviewFields |= ERPCField.IsTickAligned;
			defaultOverviewFields |= ERPCField.HasRpcTarget;
			defaultOverviewFields |= ERPCField.InvokeLocal;
			defaultOverviewFields |= ERPCField.Sent;
			defaultOverviewFields |= ERPCField.Received;
			OverviewVisibleFields = (ERPCField)EditorPrefs.GetInt($"{nameof(FusionInspector)}.{nameof(RPCs)}.{nameof(OverviewVisibleFields)}", (int)defaultOverviewFields);

			ERPCEventField defaultEventFields = ERPCEventField.RPC;
			defaultEventFields |= ERPCEventField.Type;
			defaultEventFields |= ERPCEventField.Object;
			defaultEventFields |= ERPCEventField.Frame;
			defaultEventFields |= ERPCEventField.Source;
			defaultEventFields |= ERPCEventField.Target;
			defaultEventFields |= ERPCEventField.Result;
			EventsVisibleFields = (ERPCEventField)EditorPrefs.GetInt($"{nameof(FusionInspector)}.{nameof(RPCs)}.{nameof(EventsVisibleFields)}", (int)defaultEventFields);
		}

		public void SetVisibleFields(ERPCField fields, bool enabled)
		{
			if (enabled == true)
			{
				OverviewVisibleFields |= fields;
			}
			else
			{
				OverviewVisibleFields &= ~fields;
			}

			EditorPrefs.SetInt($"{nameof(FusionInspector)}.{nameof(RPCs)}.{nameof(OverviewVisibleFields)}", (int)OverviewVisibleFields);
		}

		public void EventsToggleVisibleField(ERPCEventField field)
		{
			EventsVisibleFields ^= field;
			EditorPrefs.SetInt($"{nameof(FusionInspector)}.{nameof(RPCs)}.{nameof(EventsVisibleFields)}", (int)EventsVisibleFields);
		}

		public void EventsSetVisibleFields(ERPCEventField fields, bool enabled)
		{
			if (enabled == true)
			{
				EventsVisibleFields |= fields;
			}
			else
			{
				EventsVisibleFields &= ~fields;
			}

			EditorPrefs.SetInt($"{nameof(FusionInspector)}.{nameof(RPCs)}.{nameof(EventsVisibleFields)}", (int)EventsVisibleFields);
		}

		public void ToggleVisibleField(ERPCField field)
		{
			OverviewVisibleFields ^= field;
			EditorPrefs.SetInt($"{nameof(FusionInspector)}.{nameof(RPCs)}.{nameof(OverviewVisibleFields)}", (int)OverviewVisibleFields);
		}

		public static void Sort(List<RPCInfo> rpcs, ERPCSortMode sortMode)
		{
			switch (sortMode)
			{
				case ERPCSortMode.None:
					break;

				case ERPCSortMode.NameHierarchyAscending:  { ListUtility.InsertionSort(rpcs, (c1, c2) => string.CompareOrdinal(c1.MethodHierarchy, c2.MethodHierarchy)); break; }
				case ERPCSortMode.NameHierarchyDescending: { ListUtility.InsertionSort(rpcs, (c1, c2) => ListUtility.CompareHierarchyDescending(c1.MethodHierarchy, c2.MethodHierarchy)); break; }
				case ERPCSortMode.SentAscending:           { ListUtility.InsertionSort(rpcs, (c1, c2) => c1.Sent.Value.CompareTo(c2.Sent.Value)); break; }
				case ERPCSortMode.SentDescending:          { ListUtility.InsertionSort(rpcs, (c1, c2) => c2.Sent.Value.CompareTo(c1.Sent.Value)); break; }
				case ERPCSortMode.ReceivedAscending:       { ListUtility.InsertionSort(rpcs, (c1, c2) => c1.Received.Value.CompareTo(c2.Received.Value)); break; }
				case ERPCSortMode.ReceivedDescending:      { ListUtility.InsertionSort(rpcs, (c1, c2) => c2.Received.Value.CompareTo(c1.Received.Value)); break; }

				default:
					throw new NotSupportedException(sortMode.ToString());
			}
		}
	}
}
