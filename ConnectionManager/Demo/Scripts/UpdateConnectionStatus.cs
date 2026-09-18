using Fusion;
using Fusion.Addons.ConnectionManagerAddon;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UpdateConnectionStatus : MonoBehaviour, INetworkRunnerCallbacks
{
    protected NetworkRunner runner;
    private AudioSource audioSource;

    public AudioClip connectedToServer;
    public AudioClip disconnectedFromServer;
    public AudioClip shutdown;
    public AudioClip connectFailed;
    public AudioClip localUserSpawned;
    public AudioClip playerJoined;
    public AudioClip playerLeft;

    public TextMeshProUGUI sessionStatus;
       
    protected virtual void Start()
    {
        FindRunner();
        runner.AddCallbacks(this);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        var connectionManager = runner.GetComponent<ConnectionManager>();
        connectionManager.onWillConnect.AddListener(OnWillConnect);
    }

    protected virtual void FindRunner()
    {
        // Find the associated runner, if not defined
        if (runner == null) runner = GetComponentInParent<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("Should be stored under a NetworkRunner to be discoverable");
            return;
        }
    }

    protected virtual void DebugLog(string debug, bool permanentError = false)
    {
        sessionStatus.text = debug;
        if (permanentError)
        {
            Debug.LogError(debug);
        }
        else
        {
            Debug.Log(debug);
        }
    }

    void OnWillConnect()
    {
        DebugLog("Starting connection. Please wait...");
    }

    #region INetworkRunnerCallbacks
    public virtual void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (playerJoined)
        {
            audioSource.PlayOneShot(playerJoined);
        }

        if (player == runner.LocalPlayer)
        {
            DebugLog("You have joined !");
        }
        else
            DebugLog("A player joined !");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (playerLeft)
        {
            audioSource.PlayOneShot(playerLeft);
        }
        DebugLog("A player left !");
    }


    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdown)
        {
            audioSource.PlayOneShot(shutdown);
        }
        DebugLog($"Shutdown : { shutdownReason} ", permanentError: true);
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        if (connectedToServer)
        {
            audioSource.PlayOneShot(connectedToServer);
        }
        DebugLog("Connected to the server");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (disconnectedFromServer)
        {
            audioSource.PlayOneShot(disconnectedFromServer);
        }
        DebugLog($"Disconnected From Server: {runner.SessionInfo} ({reason})", permanentError: true);
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        if (connectFailed)
        {
            audioSource.PlayOneShot(connectFailed);
        }
        DebugLog($"Connect Failed : { reason} ", permanentError: true);
    }
    #endregion

    #region INetworkRunnerCallbacks (unused)
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
#if !FUSION_2_1_OR_NEWER
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }
#endif
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
#if FUSION_2_1_OR_NEWER
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
#endif
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    #endregion
}

