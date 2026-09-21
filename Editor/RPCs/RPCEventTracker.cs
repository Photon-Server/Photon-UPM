namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEditor;

	internal static class RPCEventTracker
	{
		private static readonly Dictionary<NetworkRunner, List<RPCEvent>> _events      = new Dictionary<NetworkRunner, List<RPCEvent>>();
		private static readonly Dictionary<NetworkBehaviour, string>      _objectNames = new Dictionary<NetworkBehaviour, string>();

		public static void Initialize()
		{
			NetworkRunner.DebugRpcEvent -= OnRpcEvent;
			NetworkRunner.DebugRpcEvent += OnRpcEvent;

			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		public static bool TryGetEvents(NetworkRunner runner, out List<RPCEvent> rpcEvents)
		{
			if (runner == null)
			{
				rpcEvents = default;
				return false;
			}

			return _events.TryGetValue(runner, out rpcEvents);
		}

		public static void ClearEvents(NetworkRunner runner)
		{
			if (runner == null)
				return;

			if (_events.ContainsKey(runner) == true)
			{
				_events[runner] = new List<RPCEvent>(1024);
			}
		}

		private static void OnRpcEvent(NetworkRunnerDebugRpcEvent debugRpcEvent)
		{
			if (_events.TryGetValue(debugRpcEvent.Runner, out List<RPCEvent> rpcEvents) == false)
			{
				rpcEvents = new List<RPCEvent>(1024);
				_events[debugRpcEvent.Runner] = rpcEvents;
			}

			NetworkBehaviour behaviour         = debugRpcEvent.Behaviour;
			NetworkObject    networkObject     = default;
			string           objectName        = default;
			PlayerRef        stateAuthority    = PlayerRef.None;
			PlayerRef        inputAuthority    = PlayerRef.None;
			bool             hasStateAuthority = false;
			bool             hasInputAuthority = false;

			if (ReferenceEquals(behaviour, null) == false)
			{
				if (_objectNames.TryGetValue(behaviour, out objectName) == false)
				{
					objectName = behaviour.name;
					_objectNames.Add(behaviour, objectName);
				}

				networkObject = behaviour.Object;
				if (ReferenceEquals(networkObject, null) == false)
				{
					stateAuthority    = networkObject.StateAuthority;
					inputAuthority    = networkObject.InputAuthority;
					hasStateAuthority = networkObject.HasStateAuthority;
					hasInputAuthority = networkObject.HasInputAuthority;
				}
			}

			rpcEvents.Add(new RPCEvent()
			{
				Type              = debugRpcEvent.EventType,
				Frame             = UnityEngine.Time.frameCount,
				Tick              = debugRpcEvent.Tick.Raw,
				Method            = debugRpcEvent.MethodBase,
				Behaviour         = behaviour,
				Object            = networkObject,
				ObjectName        = objectName,
				SourcePlayer      = debugRpcEvent.SourcePlayer,
				TargetPlayer      = debugRpcEvent.TargetPlayer.HasValue ? debugRpcEvent.TargetPlayer.Value : PlayerRef.None,
				LocalPlayer       = debugRpcEvent.Runner.LocalPlayer,
				InvokeInfo        = debugRpcEvent.LocalInvokeInfo,
				StateAuthority    = stateAuthority,
				InputAuthority    = inputAuthority,
				HasStateAuthority = hasStateAuthority,
				HasInputAuthority = hasInputAuthority,
			});
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode)
			{
				_events.Clear();
				_objectNames.Clear();
			}
		}
	}
}
