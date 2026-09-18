#if FUSION_2_1_OR_NEWER
using Photon.Client;
#else
using ExitGames.Client.Photon; 
# endif
using Fusion;
using Fusion.Addons.ConnectionManagerAddon;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Fusion.XR.Shared.Core;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// NetworkSettingsManager handles and centralizes the network parameters.
/// It provides methods to : 
///     - change the network settings of the NetworkPreferences component
///     - restart the network connection 
///     - save or restore network settings in the user's preferences (server parameters, region, room name, etc)
/// It implements the INetworkRunnerCallbacks to be notified at each connection state.
/// </summary>
public class NetworkSettingsManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner runner;

    public string ServerIPAdress => NetworkPreferences.LastServer;
    public string ServerPort => $"{NetworkPreferences.LastPort}";
    public ConnectionProtocol ConnectionProtocol => NetworkPreferences.LastProtocol;
    public bool IsFixedRegion => string.IsNullOrEmpty(NetworkPreferences.LastRegion) == false;
    public string Region => NetworkPreferences.LastRegion;
    public string RoomName => NetworkPreferences.LastRoom;

    public string connectionStatus = "";
    [SerializeField] TextMeshProUGUI connectionStatusTMP;

    public NetworkPreferences networkPreferences = null;

    [SerializeField] string defaultRegion = "EU";

    [SerializeField] IFeedbackHandler feedback;

    [SerializeField] bool displayConnectionPopupMessages = true;

    string memorizedRegion = "";
    bool callBackRegistered = false;


    private void Awake()
    {
        if (networkPreferences == null)
        {
            networkPreferences = FindAnyObjectByType<NetworkPreferences>();
        }
        if (networkPreferences == null)
        {
            Debug.LogError("No NetworkPreferences. Required next to the FusionBootstrap or ConnexionManager");
        }

        if (feedback == null)
        {
            feedback = GetComponent<IFeedbackHandler>();
        }

        var connectionText = "Connecting ...";
        if (networkPreferences && networkPreferences.IsConfigOverriden)
        {
            if (networkPreferences.IsServerOverriden == false)
            {
                connectionText = $"Connecting (custom network {(PhotonAppSettings.Global.AppSettings.Protocol == ConnectionProtocol.Udp ? "UDP":"TCP")} config) ...";
            }
            else
            {
                connectionText = $"Connecting (custom network {(PhotonAppSettings.Global.AppSettings.Protocol == ConnectionProtocol.Udp ? "UDP" : "TCP")} config, server: {PhotonAppSettings.Global.AppSettings.Server})...";
            }
        }
        else
        {
            connectionText = "Connecting ...";
        }
        if (connectionStatusTMP )
        {
            connectionStatusTMP.text = connectionText;
        }

        if (displayConnectionPopupMessages)
            Fusion.XRShared.Tools.PopupMessageHandler.Instance?.ShowPermanentMessage(connectionText);
    }

    private void Update()
    {
        FindRunner();
    }

    protected virtual void FindRunner()
    {
        if(callBackRegistered) return;

        if (runner == null)
        {
            // Try to find a runner
            foreach (var r in GameObject.FindObjectsByType<NetworkRunner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r.State != NetworkRunner.States.Shutdown)
                {
                    runner = r;
                }
            }
        }

        if (runner != null)
        {
            runner.AddCallbacks(this);
            callBackRegistered = true;
        }
    }

    #region network settings change
    public void ProtocolDropDownValueChanged(int value)
    {
        switch (value)
        {
            case 0:
                if(networkPreferences != null)
                {
                    networkPreferences.ChangeProtocol(ConnectionProtocol.Udp, reloadScene: false);
                }
                break;

            case 1:
                if (networkPreferences != null)
                {
                    networkPreferences.ChangeProtocol(ConnectionProtocol.Tcp, reloadScene: false);
                }
                break;
        }
    }

    public void ToggleFixedRegionChanged(bool value)
    {
        if (value == false)
        {
            if(networkPreferences != null)
            {
                networkPreferences.ChangeRegion("", reloadScene: false);
            }
        }
        else
        {
            if(string.IsNullOrEmpty(memorizedRegion))
            {
                memorizedRegion = defaultRegion;
            }
            if(networkPreferences != null)
            {
                networkPreferences.ChangeRegion(memorizedRegion, reloadScene: false);
            }
        }
    }

    public void ChangeRegion(int value)
    {
        string region = defaultRegion;
        switch (value)
        {
            case 0:
                region = "EU";
                break;

            case 1:
                region = "US";
                break;

            case 2:
                region = "ASIA";
                break;

            case 3:
                region = "AU";
                break;

            case 4:
                region = "IN";
                break;

            case 5:
                region = "JP";
                break;
        }
        memorizedRegion  = region;
        if(networkPreferences != null)
        {
            networkPreferences.ChangeRegion(region, reloadScene: false);
        }
    }

    public void ChangeServerIpAddress(string serverIp)
    {
        if (networkPreferences != null)
        {
            networkPreferences.ChangeServer(serverIp, reloadScene: false);
        }
    }

    public void ChangeServerPort(string port)
    {
        if (port == "")
        {
            if (networkPreferences != null)
            {
                networkPreferences.DeletePortPreference(reloadScene: false);
            }
            return;
        }
        port = Regex.Replace(port, "[^0-9]", "");
        try
        {
            if(networkPreferences != null)
            {
                networkPreferences.ChangePort(Int32.Parse(port), reloadScene: false);
            }
        }
        catch (Exception e) {
            Debug.LogError($"Unable to change port ( {port}not parsable as an int?): "+e.Message);
        }
    }

    public void ChangeRoomName(string roomName)
    {
        Debug.LogError($"[NP] Request to changeRoomName " + roomName);
        if(networkPreferences != null)
        {
            networkPreferences.ChangeRoomName(roomName, reloadScene: false);
        }
    }

    public bool IsFixedRegionEnabled()
    {
        return IsFixedRegion;
    }

    public void Reconnect()
    {
        if(networkPreferences != null)
        {
            networkPreferences.ReloadScene();
        }
        else
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }
    }

    public void RestoreDefaultSettings()
    {
        Debug.LogError($"RestoreDefaultSettings {networkPreferences}");
        if(networkPreferences != null)
        {
            networkPreferences.DeleteRegionPreference(reloadScene: false);
            networkPreferences.DeleteProtocolPreference(reloadScene: false);
            networkPreferences.DeleteServerPreference(reloadScene: false);
            networkPreferences.DeletePortPreference(reloadScene: false);
            networkPreferences.DeleteRoomNamePreference(reloadScene: false);
        }
        else
        {
            // NetworkPreferences has already been destroyed
            Debug.LogError($"[NP] NetworkPreferences has already been destroyed");
            NetworkPreferences.ResetAllPreferences();
        }
    }
    #endregion

    private void PlayAudioFeedback(string audioType)
    {
        if (feedback != null && feedback.IsAudioFeedbackIsPlaying() == false)
        {
            feedback.PlayAudioFeedback(audioType);
        }
    }

    protected virtual void DebugLog(string debug, bool permanentError = false)
    {
        connectionStatus = debug;
        if (permanentError)
        {
            Debug.LogError(debug);
        }
        else
        {
            Debug.Log(debug);
        }
        if(connectionStatusTMP != null)
        {
            connectionStatusTMP.text = debug;
        }

    }
    #region INetworkRunnerCallbacks
    public virtual void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        PlayAudioFeedback("playerJoined");

        if (player == runner.LocalPlayer)
        {
            DebugLog("You have joined !");
            if (displayConnectionPopupMessages)
                Fusion.XRShared.Tools.PopupMessageHandler.Instance?.ShowMessage("Connected", displayDuration:1.5f);
        }
        else
        {
            DebugLog("A player joined !");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        PlayAudioFeedback("playerLeft");
        DebugLog("A player left !");
    }


    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        DebugLog($"Shutdown : {shutdownReason} ", permanentError: true);
        PlayAudioFeedback("shutdown");
        if (displayConnectionPopupMessages)
        {
            if (shutdownReason == ShutdownReason.Error)
            {
                Fusion.XRShared.Tools.PopupMessageHandler.Instance?.ShowPermanentMessage("Disconnected (Error).");
            }
            else
            {
                Fusion.XRShared.Tools.PopupMessageHandler.Instance?.ShowPermanentMessage("Disconnected.");
            }
        }
            
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        DebugLog("Connected to the server");
        PlayAudioFeedback("connectedToServer");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        DebugLog($"Disconnected From Server: {runner.SessionInfo} ({reason})", permanentError: true);
        PlayAudioFeedback("disconnectedFromServer");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        DebugLog($"Connect Failed : {reason} ", permanentError: true);
        PlayAudioFeedback("connectFailed");
        if (displayConnectionPopupMessages)
            Fusion.XRShared.Tools.PopupMessageHandler.Instance?.ShowPermanentMessage("Failed to connect");
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
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
#if FUSION_2_1_OR_NEWER
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
#endif
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

#endregion
}
