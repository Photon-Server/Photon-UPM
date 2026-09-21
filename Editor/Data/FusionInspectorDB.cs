namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using Unity.Profiling;
	using UnityEngine;
	using UnityEditor;
	using Fusion.Statistics;

	internal sealed class FusionInspectorDB : ScriptableObject
	{
		public int                 RTTMs;
		public int                 SmoothRTTMs;
		public int                 PlayerCount;
		public TimeSpan            SessionTime;
		public TimeSpan            CaptureTime;
		public float               InBandwidth;
		public float               OutBandwidth;
		public PlayerRef           LocalPlayer;
		public NetworkRunner       CurrentRunner;
		public NetworkRunner       PendingRunner;
		public List<NetworkRunner> Runners      = new List<NetworkRunner>();
		public Components          Components   = new Components();
		public SceneObjects        SceneObjects = new SceneObjects();
		public Prefabs             Prefabs      = new Prefabs();
		public RPCs                RPCs         = new RPCs();
		[NonSerialized]
		public List<RPCEvent>      RPCEvents;

		public int CurrentSection => _currentSection;

		[SerializeField]
		private int                 _currentSection;
		private int                 _lastFrameCount;
		private bool                _isPlaying;
		private float               _captureTime;
		private NetworkRunner       _sessionRunner;
		private Queue<int>          _rtts = new Queue<int>();
		private List<NetworkObject> _tempObjects = new List<NetworkObject>();
		private ProfilerMarker      _profilerMarker = new ProfilerMarker($"{nameof(FusionInspectorDB)}.{nameof(Update)}");

		public void Update()
		{
			bool synchronize = _lastFrameCount != Time.frameCount;

			_profilerMarker.Begin();

			UpdateRunners();
			UpdateSession(synchronize);
			UpdateComponents(synchronize);
			UpdateSceneObjects(synchronize);
			UpdatePrefabs(synchronize);
			UpdateRPCs(synchronize);

			if (synchronize == true)
			{
				Components.ProcessAccumulatedStateChanges();
			}

			_profilerMarker.End();

			_lastFrameCount = Time.frameCount;
		}

		public void ResetSession(bool refresh)
		{
			RTTMs        = default;
			SmoothRTTMs  = default;
			PlayerCount  = default;
			SessionTime  = default;
			CaptureTime  = default;
			InBandwidth  = default;
			OutBandwidth = default;

			_captureTime   = Time.unscaledTime;
			_sessionRunner = CurrentRunner;

			_rtts.Clear();

			foreach (ComponentInfo component in Components.All)
			{
				component.ResetSession();
			}

			foreach (SceneObjectInfo sceneObject in SceneObjects.All)
			{
				sceneObject.ResetSession(refresh);
			}

			foreach (RPCInfo rpc in RPCs.All)
			{
				rpc.ResetSession();
			}

			if (refresh == true)
			{
				RPCEventTracker.ClearEvents(CurrentRunner);
				RPCEvents = default;
			}
		}

		public void SetCurrentSection(int currentSection)
		{
			_currentSection = currentSection;

			EditorPrefs.SetInt($"{nameof(FusionInspectorDB)}.{nameof(_currentSection)}", currentSection);
		}

		private void Awake()
		{
			_currentSection = EditorPrefs.GetInt($"{nameof(FusionInspectorDB)}.{nameof(_currentSection)}", 0);

			Components.LoadEditorPrefs();
			SceneObjects.LoadEditorPrefs();
			Prefabs.LoadEditorPrefs();
			RPCs.LoadEditorPrefs();
		}

		private void UpdateRunners()
		{
			LocalPlayer = PlayerRef.None;

			Runners.Clear();

			List<NetworkRunner>.Enumerator runnersEnumerator = NetworkRunner.GetInstancesEnumerator();
			while (runnersEnumerator.MoveNext() == true)
			{
				NetworkRunner runner = runnersEnumerator.Current;
				if (runner.IsRunning == true)
				{
					Runners.Add(runnersEnumerator.Current);
				}
			}

			if (PendingRunner != default)
			{
				CurrentRunner = PendingRunner;
				PendingRunner = default;
			}

			if (Runners.Contains(CurrentRunner) == false)
			{
				CurrentRunner = Runners.Count > 0 ? Runners[0] : default;
			}

			if (CurrentRunner != null && CurrentRunner.IsRunning == true)
			{
				LocalPlayer = CurrentRunner.LocalPlayer;
			}
		}

		private void UpdateSession(bool synchronize)
		{
			if (_sessionRunner != CurrentRunner)
			{
				ResetSession(false);
			}

			if (_sessionRunner == null)
				return;

			RTTMs       = (int)(_sessionRunner.GetPlayerRtt(_sessionRunner.LocalPlayer) * 1000.0);
			PlayerCount = NetworkRunnerUtility.GetPlayerCount(_sessionRunner);
			SessionTime = default;
			CaptureTime = TimeSpan.FromSeconds(Time.unscaledTime - _captureTime);

			try
			{
				float tickRate = _sessionRunner.TickRate;
				if (tickRate > 0.1f)
				{
					SessionTime = TimeSpan.FromSeconds(_sessionRunner.Tick.Raw / tickRate);
				}
			}
			catch {}

			// Accumulations below run once per frame, otherwise the traffic totals and
			// the RTT smoothing window would scale with the OnGUI event rate.
			if (synchronize == false)
				return;

#if FUSION_2_1_OR_NEWER
			if (_sessionRunner.TryGetFusionStatistics(out FusionStatisticsManager statisticsManager) == true && statisticsManager.SimulationSnapshot != null)
			{
				if (statisticsManager.SimulationSnapshot.Stats.TryGetValue(FusionStatType.InBandwidth,  out float inBandwidth)  == true) { InBandwidth  += inBandwidth;  }
				if (statisticsManager.SimulationSnapshot.Stats.TryGetValue(FusionStatType.OutBandwidth, out float outBandwidth) == true) { OutBandwidth += outBandwidth; }
			}
#else
			if (_sessionRunner.TryGetFusionStatistics(out FusionStatisticsManager statisticsManager) == true && statisticsManager.CompleteSnapshot != null)
			{
				InBandwidth  += statisticsManager.CompleteSnapshot.InBandwidth;
				OutBandwidth += statisticsManager.CompleteSnapshot.OutBandwidth;
			}
#endif

			_rtts.Enqueue(RTTMs);
			if (_rtts.Count > 60)
			{
				_rtts.Dequeue();
			}

			int totalRTTMs = default;
			foreach (int rttMs in _rtts)
			{
				totalRTTMs += rttMs;
			}

			SmoothRTTMs = Mathf.RoundToInt((float)totalRTTMs / _rtts.Count);
		}

		private void UpdateComponents(bool synchronize)
		{
			if (synchronize == true)
			{
				Components.Synchronize();
			}

			Components.Refresh();
		}

		private void UpdateSceneObjects(bool synchronize)
		{
			if (Application.isPlaying == false)
			{
				SceneObjects.StateAuthorityFilter = PlayerRef.None;
				SceneObjects.InputAuthorityFilter = PlayerRef.None;
			}

			if (synchronize == true)
			{
				_tempObjects.Clear();

				if (CurrentRunner != null)
				{
					NetworkRunnerUtility.GetAllNetworkObjects(CurrentRunner, _tempObjects);
				}
				else
				{
					UnityEngine.Object[] objects = ObjectUtility.FindObjectsByType(typeof(NetworkObject), FindObjectsInactive.Include);
					for (int i = 0, count = objects.Length; i < count; ++i)
					{
						_tempObjects.Add((NetworkObject)objects[i]);
					}
				}

				Vector3 distanceOrigin    = Vector3.zero;
				bool    hasDistanceOrigin = false;

				if (Application.isPlaying == true)
				{
					if (LocalPlayer.IsRealPlayer == true && CurrentRunner != null)
					{
						if (CurrentRunner.TryGetPlayerObject(LocalPlayer, out NetworkObject playerObject) == true)
						{
							distanceOrigin    = playerObject.transform.position;
							hasDistanceOrigin = true;
						}
					}

					if (hasDistanceOrigin == false)
					{
						Camera camera = Camera.main;
						if (camera != null)
						{
							distanceOrigin    = camera.transform.position;
							hasDistanceOrigin = true;
						}
					}
				}

				if (hasDistanceOrigin == false)
				{
					SceneView lastActiveSceneView = SceneView.lastActiveSceneView;
					if (lastActiveSceneView != null)
					{
						Camera camera = lastActiveSceneView.camera;
						if (camera != null)
						{
							distanceOrigin    = camera.transform.position;
							hasDistanceOrigin = true;
						}
					}
				}

				SceneObjects.Synchronize(Components, _tempObjects, LocalPlayer, distanceOrigin, hasDistanceOrigin, Application.isPlaying);

				_tempObjects.Clear();
			}

			SceneObjects.Refresh(Components);
		}

		private void UpdatePrefabs(bool synchronize)
		{
			if (synchronize == true)
			{
				_tempObjects.Clear();

				NetworkPrefabTable prefabTable = NetworkProjectConfig.Global.PrefabTable;
				for (int i = 0, count = prefabTable.Prefabs.Count; i < count; ++i)
				{
					NetworkObject prefab = prefabTable.Load(NetworkPrefabId.FromIndex(i), true);
					if (ReferenceEquals(prefab, null) == false)
					{
						_tempObjects.Add(prefab);
					}
				}

				Prefabs.Synchronize(Components, _tempObjects);

				_tempObjects.Clear();
			}

			Prefabs.Refresh(Components);
		}

		private void UpdateRPCs(bool synchronize)
		{
			if (synchronize == true)
			{
				RPCs.Synchronize(Components);
			}

			if (CurrentRunner != null)
			{
				RPCEventTracker.TryGetEvents(CurrentRunner, out RPCEvents);
			}

			if (Application.isPlaying == true && _isPlaying == false)
			{
				RPCEvents = default;

				RPCs.EventSourceFilter         = PlayerRef.None;
				RPCs.EventTargetFilter         = PlayerRef.None;
				RPCs.EventStateAuthorityFilter = PlayerRef.None;
				RPCs.EventInputAuthorityFilter = PlayerRef.None;
				RPCs.EventAnyPlayerFilter      = PlayerRef.None;
				RPCs.EventObjectFilter         = default;
				RPCs.EventObjectFilterName     = default;
			}

			_isPlaying = Application.isPlaying;

			RPCs.Refresh(Components, CurrentRunner);
		}
	}
}
