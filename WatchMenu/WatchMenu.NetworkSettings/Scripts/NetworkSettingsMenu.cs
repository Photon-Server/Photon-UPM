#if FUSION_2_1_OR_NEWER
using Photon.Client;
#else
using ExitGames.Client.Photon;

#endif
using Fusion.Addons.WatchMenu;
using Fusion.XR.Shared.Core;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NetworkSettingsMenu handles the settings related to network parameters.
/// It subscribes to UI events to call NetworkSettingsManager utility methods when the user interacts with the UI.
/// UI elements are updated based on the NetworkSettingsManager.
/// </summary>

public class NetworkSettingsMenu : WatchWindow
{
    [Header("Set automatically")]
    NetworkSettingsManager networkSettingsManager;

    [Header("Audio Feedback")]
    public bool playSoundWhenTouched = true;
    [SerializeField] IFeedbackHandler feedback;
    [SerializeField] string audioType;

    [Header("UI elements")]
    [SerializeField] TMP_Text statusText;

    [Header("New UI elements - Column 1")]
    [SerializeField] TMP_InputField serverAddressInputField;
    [SerializeField] TMP_InputField serverPortInputField;
    [SerializeField] Dropdown protocolDropDown;

    [Header("New UI elements - Column 2")]
    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] Toggle toggleFixedRegionButton;
    [SerializeField] Dropdown regionDropDown;

    [Header("New UI elements - Action")]
    [SerializeField] Button restoreSettingsButton;
    [SerializeField] Button reconnectButton;

    float delayBeforeClosing = 0.3f;
  
    private void Awake()
    {
        AddListenersOnUiElements();

        if (feedback == null)
        {
            feedback = GetComponentInParent<IFeedbackHandler>();
        }
    }

    private void OnDestroy()
    {
        RemoveListenersOnUiElements();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        FindNetworkSettingsManager();
        UpdateUI();
    }


    void FindNetworkSettingsManager()
    {
        if (networkSettingsManager == null)
        {
            networkSettingsManager = FindAnyObjectByType<NetworkSettingsManager>();
        }
        if (networkSettingsManager == null)
        {
            Debug.LogError("networkSettingsManager not found");
        }
    }


    #region Listeners

    private void AddListenersOnUiElements()
    {
        // Column 1
        if (serverAddressInputField == null) Debug.LogError("serverAddressInputField not defined");
        serverAddressInputField.onValueChanged.AddListener(OnServerAddressInputFieldChanged);

        if (serverPortInputField == null) Debug.LogError("serverPortInputField not defined");
        serverPortInputField.onValueChanged.AddListener(OnServerPortInputFieldChanged);

        if (protocolDropDown == null) Debug.LogError("protocolDropDown not defined");
        protocolDropDown.onValueChanged.AddListener(OnProtocolDropDownValueChanged);

        // Column 2
        if (roomNameInputField == null) Debug.LogError("roomNameInputField not defined");
        roomNameInputField.onValueChanged.AddListener(OnRoomNameInputFieldInputFieldChanged);

        if (toggleFixedRegionButton == null) Debug.LogError("toggleFixedRegionButton not defined");
        toggleFixedRegionButton.onValueChanged.AddListener(OnToggleFixedRegionChanged);

        if (regionDropDown == null) Debug.LogError("regionDropDown not defined");
        regionDropDown.onValueChanged.AddListener(OnRegionDropDownChanged);

        // Action
        if (restoreSettingsButton == null) Debug.LogError("saveSettingsButton not defined");
        restoreSettingsButton.onClick.AddListener(OnRestoreNetworkSettingsButtonClick);

        if (reconnectButton == null) Debug.LogError("reconnectButton not defined");
        reconnectButton.onClick.AddListener(OnReconnectButtonClick);
    }

    private void RemoveListenersOnUiElements()
    {
        // Column 1
        serverAddressInputField.onValueChanged.RemoveListener(OnServerAddressInputFieldChanged);
        serverAddressInputField.onValueChanged.RemoveListener(OnServerPortInputFieldChanged);
        protocolDropDown.onValueChanged.RemoveListener(OnProtocolDropDownValueChanged);


        // Column 2
        roomNameInputField.onValueChanged.RemoveListener(OnRoomNameInputFieldInputFieldChanged);
        toggleFixedRegionButton.onValueChanged.RemoveListener(OnToggleFixedRegionChanged);
        regionDropDown.onValueChanged.RemoveListener(OnRegionDropDownChanged);

        // Action
        restoreSettingsButton.onClick.RemoveListener(OnRestoreNetworkSettingsButtonClick);
        reconnectButton.onClick.RemoveListener(OnReconnectButtonClick);
    }

    #endregion

    #region UI interactions

    private void OnServerAddressInputFieldChanged(string serverIp)
    {
        networkSettingsManager.ChangeServerIpAddress(serverIp);
        UpdateUI();
        PlayAudioFeedback();
    }
    private void OnServerPortInputFieldChanged(string port)
    {
        networkSettingsManager.ChangeServerPort(port);
        UpdateUI();
        PlayAudioFeedback();
    }

    private void OnProtocolDropDownValueChanged(int value)
    {
        networkSettingsManager.ProtocolDropDownValueChanged(value);
        UpdateUI();
        PlayAudioFeedback();
    }

    private void OnRoomNameInputFieldInputFieldChanged(string roomName)
    {
        networkSettingsManager.ChangeRoomName(roomName);
        UpdateUI();
        PlayAudioFeedback();
    }

    [ContextMenu("OnToggleFixedRegionChanged")]
    public void OnToggleFixedRegionChanged(bool value)
    {
        networkSettingsManager.ToggleFixedRegionChanged(value);
        PlayAudioFeedback();
        UpdateUI();
    }

    private void OnRegionDropDownChanged(int region)
    {
        if (toggleFixedRegionButton.isOn)
        {
            networkSettingsManager.ChangeRegion(region);
            UpdateUI();
            PlayAudioFeedback();
        }
    }

    private void OnRestoreNetworkSettingsButtonClick()
    {
        networkSettingsManager.RestoreDefaultSettings();
        UpdateUI();
        PlayAudioFeedback();
    }

    private void OnReconnectButtonClick()
    {
        networkSettingsManager.Reconnect();
        UpdateUI();
        PlayAudioFeedback();
    }



    // CloseSettingsMenu is called by the close button in the settings menu prefab
    public void CloseSettingsMenu()
    {
        PlayAudioFeedback();
        StartCoroutine(CloseAfterDelay(delayBeforeClosing));
    }

    IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    #endregion

    #region UI Update

    public void UpdateUI()
    {
        serverAddressInputField.text = networkSettingsManager.ServerIPAdress;

        var serverPort = networkSettingsManager.ServerPort;
        if (serverPort == "0")
            serverPort = "";

        serverPortInputField.text = serverPort;
        if(networkSettingsManager.RoomName != null)
        {
            // networkSettingsManager.RoomName might be null during disconnect phase
            roomNameInputField.text = networkSettingsManager.RoomName;
        }

        if (networkSettingsManager.ConnectionProtocol == ConnectionProtocol.Udp)
            protocolDropDown.value = 0;
        else
            protocolDropDown.value = 1;

        // update toggles (required at start after userpref restoration)
        Debug.LogError($"region: {Fusion.Addons.ConnectionManagerAddon.NetworkPreferences.LastRegion}");
        toggleFixedRegionButton.SetIsOnWithoutNotify(networkSettingsManager.IsFixedRegionEnabled());

        switch (networkSettingsManager.Region)
        {
            case "EU":
                regionDropDown.value = 0;
                break;

            case "US":
                regionDropDown.value = 1;
                break;

            case "ASIA":
                regionDropDown.value = 2;
                break;

            case "AU":
                regionDropDown.value = 3;
                break;

            case "IN":
                regionDropDown.value = 4;
                break;

            case "JP":
                regionDropDown.value = 5;
                break;
            default:
                regionDropDown.value = 0;
                break;
        }

        regionDropDown.interactable = networkSettingsManager.IsFixedRegionEnabled();


        statusText.text = networkSettingsManager.connectionStatus;

    }
    
    #endregion

    #region others


    private void PlayAudioFeedback()
    {
        if (playSoundWhenTouched && feedback != null && feedback.IsAudioFeedbackIsPlaying() == false)
            feedback.PlayAudioFeedback(audioType);
    }

    #endregion
}
