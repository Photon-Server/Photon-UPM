#if FUSION_2_1_OR_NEWER
using Photon.Client;
#else
using ExitGames.Client.Photon; 
# endif
using Fusion;
using Fusion.Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fusion.Addons.ConnectionManagerAddon
{
    /// <summary>
    /// Load network preferences from settings. If fusionBootstrap/connectionManager are not set, should be place on the gameobject of the object handling connection
    /// </summary>
    [DefaultExecutionOrder(-100_000)]
    public class NetworkPreferences : MonoBehaviour
    {
        [SerializeField] FusionBootstrap fusionBootstrap;
        [SerializeField] ConnectionManager connectionManager;

        const string PHOTON_SETTINGS_PROTOCOL_PREF = "PHOTON_SETTINGS_PROTOCOL_PREF";
        const string PHOTON_SETTINGS_REGION_PREF = "PHOTON_SETTINGS_REGION_PREF";
        const string PHOTON_SETTINGS_SERVER_PREF = "PHOTON_SETTINGS_SERVER_PREF";
        const string PHOTON_SETTINGS_PORT_PREF = "PHOTON_SETTINGS_PORT_PREF";
        const string PHOTON_SETTINGS_ROOMNAME_PREF = "PHOTON_SETTINGS_ROOMNAME_PREF";

        string _initialRegion;
        string _initialServer;
        string _initialRoom;
        int _initialPort;
        ConnectionProtocol _initialProtocol;

        protected bool _isServerOverriden = false;
        protected bool _isRegionOverriden = false;
        protected bool _isPortOverriden = false;
        protected bool _isProtocolOverriden = false;
        protected bool _isRoomNameOverriden = false;

        public bool IsConfigOverriden => IsServerOverriden || IsServerOverriden || IsRegionOverriden || IsPortOverriden || IsPortOverriden || IsProtocolOverriden || IsRoomNameOverriden;
        public bool IsServerOverriden => _isServerOverriden;
        public bool IsRegionOverriden => _isRegionOverriden;
        public bool IsPortOverriden => _isPortOverriden;
        public bool IsProtocolOverriden => _isProtocolOverriden;
        public bool IsRoomNameOverriden => _isRoomNameOverriden;

        // Last values detected (default, or modified)
        public static string LastRegion;
        public static string LastServer;
        public static string LastRoom;
        public static int LastPort;
        public static ConnectionProtocol LastProtocol;
        
        // Last default values detected
        public static string LastInitialRegion;
        public static string LastInitialServer;
        public static string LastInitialRoom;
        public static int LastInitialPort;
        public static ConnectionProtocol LastInitialProtocol;

        #region Preference loading logic
        private void Awake()
        {
            if (fusionBootstrap == null) fusionBootstrap = GetComponent<FusionBootstrap>();
            if (connectionManager == null) connectionManager = GetComponent<ConnectionManager>();
            BackupSettings();
            LoadPreferences();
        }

        private void OnDestroy()
        {
            // We restore PhotonAppSettings
            ReapplyBackedSettings();
        }

        void LoadPreferences()
        {
            _isServerOverriden = false;
            _isRegionOverriden = false;
            _isPortOverriden = false;
            _isProtocolOverriden = false;
            _isRoomNameOverriden = false;

            if (PlayerPrefs.HasKey(PHOTON_SETTINGS_PROTOCOL_PREF))
            {
                ConnectionProtocol protocol = (ConnectionProtocol)PlayerPrefs.GetInt(PHOTON_SETTINGS_PROTOCOL_PREF);
                PhotonAppSettings.Global.AppSettings.Protocol = protocol;
                LastProtocol = protocol;
                _isProtocolOverriden = true;
            }
            if (PlayerPrefs.HasKey(PHOTON_SETTINGS_REGION_PREF))
            {
                string region = PlayerPrefs.GetString(PHOTON_SETTINGS_REGION_PREF);
                PhotonAppSettings.Global.AppSettings.FixedRegion = region;
                LastRegion = region;
                _isRegionOverriden = true;
            }
            if (PlayerPrefs.HasKey(PHOTON_SETTINGS_SERVER_PREF))
            {
                string server = PlayerPrefs.GetString(PHOTON_SETTINGS_SERVER_PREF);
                PhotonAppSettings.Global.AppSettings.Server = server;
                LastServer = server;
                _isServerOverriden = true;
            }
            if (PlayerPrefs.HasKey(PHOTON_SETTINGS_PORT_PREF))
            {
                int port = PlayerPrefs.GetInt(PHOTON_SETTINGS_PORT_PREF);
                PhotonAppSettings.Global.AppSettings.Port = (ushort)port;
                LastPort = port;
                _isPortOverriden = true;
            }
            if (PlayerPrefs.HasKey(PHOTON_SETTINGS_ROOMNAME_PREF))
            {
                var roomName = PlayerPrefs.GetString(PHOTON_SETTINGS_ROOMNAME_PREF);
                _isRoomNameOverriden = true;
                if (connectionManager)
                {
                    connectionManager.roomName = roomName;
                }
                if (fusionBootstrap)
                {
                    fusionBootstrap.DefaultRoomName = roomName;
                }
                LastRoom = roomName;
            }
            Debug.Log($"[NP] _isServerOverriden:{_isServerOverriden}/_isRegionOverriden:{_isRegionOverriden}/_isPortOverriden:{_isPortOverriden}/_isProtocolOverriden:{_isProtocolOverriden}/_isRoomNameOverriden:{_isRoomNameOverriden}");

        }

        void BackupSettings()
        {
            // PhotonAppSettings
            _initialProtocol = PhotonAppSettings.Global.AppSettings.Protocol;
            _initialRegion = PhotonAppSettings.Global.AppSettings.FixedRegion;
            _initialServer = PhotonAppSettings.Global.AppSettings.Server;
            _initialPort = PhotonAppSettings.Global.AppSettings.Port;

            // Connection handling
            if (connectionManager)
            {
                _initialRoom = connectionManager.roomName;
            }
            if (fusionBootstrap)
            {
                _initialRoom = fusionBootstrap.DefaultRoomName;
            }

            LastProtocol = _initialProtocol;
            LastRegion = _initialRegion;
            LastServer = _initialServer;
            LastPort = _initialPort;
            Debug.Log($"[NP] BackupSettings port "+ _initialPort);
            LastRoom = _initialRoom; 
            LastInitialProtocol = _initialProtocol;
            LastInitialRegion = _initialRegion;
            LastInitialServer = _initialServer;
            LastInitialPort = _initialPort;
            LastInitialRoom = _initialRoom;
        }

        [ContextMenu("ReapplyBackedSettings")]
        public void ReapplyBackedSettings()
        {
            // PhotonAppSettings
            PhotonAppSettings.Global.AppSettings.Protocol = _initialProtocol;
            PhotonAppSettings.Global.AppSettings.FixedRegion = _initialRegion;
            PhotonAppSettings.Global.AppSettings.Server = _initialServer;
            PhotonAppSettings.Global.AppSettings.Port = (ushort)_initialPort;
            Debug.Log($"[NP] ReapplyBackedSettings port " + _initialPort);

            // Connection handling is set in the scene, no need to restore it if the scene is reloaded. Doing it anyway in case of other usages
            if (connectionManager)
            {
                connectionManager.roomName = _initialRoom;
            }
            if (fusionBootstrap)
            {
                fusionBootstrap.DefaultRoomName = _initialRoom;
            }
        }

        public static void DeleteSetting(string settingKey)
        {
            PlayerPrefs.DeleteKey(settingKey);
            PlayerPrefs.Save();
        }
        #endregion

        public void ReloadScene()
        {
            if (fusionBootstrap)
            {
                // Fusion bootstrap's ShutdownAll also reloads initial scene
                fusionBootstrap.ShutdownAll();
            } 
            else
            {
                Scene scene = SceneManager.GetActiveScene();
                Debug.Log("Active scene:" + scene.name);
                try
                {
                    if (gameObject != null)
                    {
                        var runner = NetworkRunner.GetRunnerForGameObject(gameObject);
                        if (runner)
                        {
                            runner.Shutdown();
                            Destroy(runner.gameObject);
                        }
                        Destroy(gameObject);
                    }
                }
                catch { 
                
                }

                SceneManager.LoadScene(scene.name);
            }

        }

        public void ChangeProtocol(ConnectionProtocol protocol, bool reloadScene = true)
        {
            PhotonAppSettings.Global.AppSettings.Protocol = protocol;
            if (protocol == _initialProtocol)
            {
                DeleteProtocolPreference(reloadScene:false);
            }
            else
            {
                LastProtocol = protocol;
                PlayerPrefs.SetInt(PHOTON_SETTINGS_PROTOCOL_PREF, (int)protocol);
                PlayerPrefs.Save();
            }
            if (reloadScene) ReloadScene();
        }

        [ContextMenu("ChangeProtocolToTcp")]
        public void ChangeProtocolToTcp(bool reloadScene = true)
        {
            ChangeProtocol(ConnectionProtocol.Tcp, reloadScene);
        }

        [ContextMenu("ChangeProtocolToUdp")]
        public void ChangeProtocolToUdp(bool reloadScene = true)
        {
            ChangeProtocol(ConnectionProtocol.Udp, reloadScene);
        }

        [ContextMenu("RestoreProtocol")]
        public void DeleteProtocolPreference(bool reloadScene = true)
        {
            DeleteSetting(PHOTON_SETTINGS_PROTOCOL_PREF);
            PhotonAppSettings.Global.AppSettings.Protocol = LastInitialProtocol;
            LastProtocol = _initialProtocol;
            if (reloadScene) ReloadScene();
        }

        public void ChangeRegion(string region,bool reloadScene = true)
        {
            PhotonAppSettings.Global.AppSettings.FixedRegion = region;
            if(region == _initialRegion)
            {
                DeleteRegionPreference(reloadScene: false);
            }
            else
            {
                PlayerPrefs.SetString(PHOTON_SETTINGS_REGION_PREF, region);
                PlayerPrefs.Save();
                LastRegion = region;
            }
            if (reloadScene) ReloadScene();
        }

        [ContextMenu("RestoreRegion")]
        public void DeleteRegionPreference(bool reloadScene = true)
        {
            DeleteSetting(PHOTON_SETTINGS_REGION_PREF);
            PhotonAppSettings.Global.AppSettings.FixedRegion = _initialRegion;
            LastRegion = _initialRegion;
            if (reloadScene) ReloadScene();
        }

        public void ChangeServer(string server, bool reloadScene = true)
        {
            PhotonAppSettings.Global.AppSettings.Server = server;
            if(server == _initialServer)
            {
                DeleteServerPreference(reloadScene: false);
            }
            else
            {
                PlayerPrefs.SetString(PHOTON_SETTINGS_SERVER_PREF, server);
                PlayerPrefs.Save();
                LastServer = server;
            }
            if (reloadScene) ReloadScene();
        }

        [ContextMenu("RestoreServer")]
        public void DeleteServerPreference(bool reloadScene = true)
        {
            DeleteSetting(PHOTON_SETTINGS_SERVER_PREF);
            PhotonAppSettings.Global.AppSettings.Server = _initialServer;
            LastServer = _initialServer;
            if (reloadScene) ReloadScene();
        }

        public void ChangePort(int port, bool reloadScene = true)
        {
            PhotonAppSettings.Global.AppSettings.Port = (ushort)port;
            if(port == _initialPort)
            {
                DeletePortPreference(reloadScene: false);
            }
            else
            {
                PlayerPrefs.SetInt(PHOTON_SETTINGS_PORT_PREF, port);
                PlayerPrefs.Save();
                LastPort = port;
                Debug.Log($"[NP] ChangePort. LastPort " + LastPort);
            }
            if (reloadScene) ReloadScene();
        }

        [ContextMenu("RestorePort")]
        public void DeletePortPreference(bool reloadScene = true)
        {
            DeleteSetting(PHOTON_SETTINGS_PORT_PREF);
            PhotonAppSettings.Global.AppSettings.Port = (ushort)_initialPort;
            LastPort = _initialPort;
            Debug.Log($"[NP] DeletePortPreference. LastPort " + _initialPort);
            if (reloadScene) ReloadScene();
        }

        public void ChangeRoomName(string roomName, bool reloadScene = true)
        {
            if (connectionManager)
            {
                connectionManager.roomName = roomName;
            }
            if (fusionBootstrap)
            {
                fusionBootstrap.DefaultRoomName = roomName;
            }
            if (roomName == _initialRoom)
            {
                DeleteRoomNamePreference(reloadScene: false);
            }
            else
            {
                Debug.Log("[NP] !! ChangeRoomName !!");

                PlayerPrefs.SetString(PHOTON_SETTINGS_ROOMNAME_PREF, roomName);
                PlayerPrefs.Save();
                LastRoom = roomName;
            }
            if (reloadScene) ReloadScene();
        }


        public string GetRoomName()
        {
            // For Legacy purposes. Prefer LastRoom static property
            return LastRoom;
        }

        [ContextMenu("RestoreRoomName")]
        public void DeleteRoomNamePreference(bool reloadScene = true)
        {
            DeleteSetting(PHOTON_SETTINGS_ROOMNAME_PREF);
            if (connectionManager)
            {
                connectionManager.roomName = _initialRoom;
            }
            if (fusionBootstrap)
            {
                fusionBootstrap.DefaultRoomName = _initialRoom;
            }
            LastRoom = _initialRoom;
            if (reloadScene) ReloadScene();
        }

        public static void ResetAllPreferences()
        {
            DeleteSetting(PHOTON_SETTINGS_REGION_PREF);
            DeleteSetting(PHOTON_SETTINGS_PROTOCOL_PREF);
            DeleteSetting(PHOTON_SETTINGS_SERVER_PREF);
            DeleteSetting(PHOTON_SETTINGS_PORT_PREF);
            DeleteSetting(PHOTON_SETTINGS_ROOMNAME_PREF);
            LastProtocol = LastInitialProtocol;
            LastRegion = LastInitialRegion;
            LastServer = LastInitialServer;
            LastPort = LastInitialPort;
            LastRoom = LastInitialRoom;
        }
    }
}
