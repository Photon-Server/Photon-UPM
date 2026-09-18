using Fusion;
using Fusion.Sockets;
#if FUSION_WEAVER
using Fusion.XR.Shared.Core;
#endif
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if CINEMACHINE_AVAILABLE
using Unity.Cinemachine;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

#if FUSION_WEAVER
public class UsersTracker : MonoBehaviour, INetworkRunnerCallbacks
{
    public List<GameObject> trackedObjects = new List<GameObject>();
#if CINEMACHINE_AVAILABLE
    public CinemachineTargetGroup cinemachineTargetGroup;
    public CinemachinePanTilt panTilt;
#endif


    bool runnerSetup = false;

    [SerializeField] bool trackLocalPlayer = true;
    public bool trackHeadsets = true;
    public bool trackControllers = false;
    public bool trackHands = false;
    public float rigPartRadiusForScaleOne = 0.5f;

#if CINEMACHINE_AVAILABLE
    [Header("Camera control")]
    [SerializeField] float panFactor = 0.1f;
    [SerializeField] float tiltFactor = 0.01f;
    [SerializeField] float panMouseFactor = 0.1f;
    [SerializeField] float tiltMouseFactor = 0.01f;
#endif

    private void Awake()
    {
#if CINEMACHINE_AVAILABLE
        if (cinemachineTargetGroup == null)
        {
            cinemachineTargetGroup = GetComponentInChildren<CinemachineTargetGroup>(true);
        }
#endif
        EnableTouch();
    }
    protected virtual void Update()
    {
        ConfigureRunner();
        CheckTouch();

    }

    void EnableTouch()
    {
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
    }

    bool wasPressed = false;
    Vector2 lastMouseDelta;
    void CheckTouch()
    {
        foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
        {
            if(touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
#if CINEMACHINE_AVAILABLE
                if (panTilt == null) panTilt = FindAnyObjectByType<CinemachinePanTilt>();
                if (panTilt)
                {
                    panTilt.PanAxis.Value += touch.delta.x * panFactor;
                    panTilt.TiltAxis.Value -= touch.delta.y * tiltFactor;
                }
#endif
            }
        }

        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            var delta = Mouse.current.delta.ReadValue();
            if (wasPressed)
            {
#if CINEMACHINE_AVAILABLE
                var move = delta - lastMouseDelta;
                if (panTilt == null) panTilt = FindAnyObjectByType<CinemachinePanTilt>();
                if (panTilt)
                {
                    panTilt.PanAxis.Value += delta.x * panMouseFactor;
                    panTilt.TiltAxis.Value -= delta.y * tiltMouseFactor;
                }
#endif
            }
            wasPressed = true;
            lastMouseDelta = delta;
        } 
        else
        {
            wasPressed = false;
        }
    }


    protected virtual void AddNetworkRig(NetworkRig rig)
    {
        Debug.Log("AddNetworkRig "  +rig);
        if (rig.HasStateAuthority && trackLocalPlayer == false)
        {
            return;
        }
        foreach(var rigPart in rig.RigParts)
        {
            Debug.Log("AddNetworkRig - rigPart " + rigPart);
            bool shouldTrack = false;
            if (rigPart is IHeadset && trackHeadsets) shouldTrack = true;
            if (rigPart is IController && trackControllers) shouldTrack = true;
            if (rigPart is IHand && trackHands) shouldTrack = true;

            if (shouldTrack && rigPart.transform.GetComponentInChildren<TrackerInterest>() == null)
            {
                var trackerInterest = rigPart.gameObject.AddComponent<TrackerInterest>();
                trackerInterest.radiusForScaleOne = 0.5f;
                trackerInterest.adaptRadiusToScale = true;
                trackerInterest.unregisterWhenRenderersDisabled = true;
            }
        }
    }

    public void AddTrackedObject(GameObject o, float weight = 1, float radius = 1.5f)
    {
        Debug.Log("AddTrackedObject " + o);
        if (o != null && trackedObjects.Contains(o) == false)
        {
            trackedObjects.Add(o);
#if CINEMACHINE_AVAILABLE
            if (cinemachineTargetGroup)
            {
                cinemachineTargetGroup.AddMember(o.transform, weight, radius);
            }
#endif
        }
    }

    public void UpdateTrackedObject(GameObject o, float weight = 1, float radius = 1.5f)
    {
        if (o != null && trackedObjects.Contains(o))
        {
#if CINEMACHINE_AVAILABLE
            if (cinemachineTargetGroup)
            {
                var memberIndex = cinemachineTargetGroup.FindMember(o.transform);
                if (memberIndex != -1)
                {
                    var member = cinemachineTargetGroup.Targets[memberIndex];
                    member.Weight = weight;
                    member.Radius = radius;
                }
            }
#endif
        }
        else
        {
            AddTrackedObject(o, weight, radius);
        }
    }

    public void RemoveTrackObject(GameObject o)
    {
        Debug.Log("RemoveTrackObject " + o);
        if (o && trackedObjects.Contains(o))
        {
            trackedObjects.Remove(o);
#if CINEMACHINE_AVAILABLE
            if (cinemachineTargetGroup)
            {
                cinemachineTargetGroup.RemoveMember(o.transform);
            }
#endif
        }
    }

    void ConfigureRunner() {
        if (runnerSetup) return;
        var runner = NetworkRunner.GetRunnerForGameObject(gameObject);
        if (runner)
        {
            Debug.Log("Configuring runner callbacks ...");
            runnerSetup = true;
            runner.AddCallbacks(this);
            UpdateNetworkRigs();
        }
    }

    void UpdateNetworkRigs()
    {
        var rigs = FindObjectsByType<NetworkRig>(FindObjectsSortMode.None);
        foreach (var rig in rigs)
        {
            AddNetworkRig(rig);
        }
    }

    private void OnDestroy()
    {
        var runner = NetworkRunner.GetRunnerForGameObject(gameObject);
        if (runner && runnerSetup)
        {
            runner.RemoveCallbacks(this);
        }
    }
#region INetworkRunnerCallbacks

    public async void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("OnPlayerJoined");
        // We wait for the rig to spawn
        await AsyncTask.Delay(1_000);
        UpdateNetworkRigs();
    }
#endregion

#region INetworkRunnerCallbacks (unused)
    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }


    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

#if FUSION_2_1_OR_NEWER
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
#endif

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

#if !FUSION_2_1_OR_NEWER
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }
#endif




    #endregion

#if !CINEMACHINE_AVAILABLE
    private void OnValidate()
    {
        Debug.LogError("The Cinemachine package is required for UsersTracker");
    }
#endif

}

#else
public class UsersTracker : MonoBehaviour { }

#endif
