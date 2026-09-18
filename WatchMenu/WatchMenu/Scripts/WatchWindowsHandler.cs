using Fusion.XR.Shared.Core;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  This component should be located on rig(s) with a watch menu that needs to open a window.
///  WatchWindowsHandler receives requests to open windows from the `RadialMenuButtonWindows` component on buttons intended to open a window.
///  It spawns the windows if not yet created and centralizes all user-opened windows in a list
///  Then it manages their activation with `ToggleWindow()`, and keeps their position aligned with the user.
/// </summary>

namespace Fusion.Addons.WatchMenu
{
    public class WatchWindowsHandler : MonoBehaviour, IWatchScreenListener
    {
        public List<WatchScreen> watchScreens = new List<WatchScreen>();

        [System.Serializable]
        public struct WindowDescription
        {
            public string windowName;
            public WatchWindow windowPrefab;
            public bool instantiateHiddenAtStart;
        }

        [System.Serializable]
        public class RegisteredWindows
        {
            public WindowDescription windowDescription;
            public WatchWindow windowInstance;
        }

        public List<WindowDescription> windowsDescriptions = new List<WindowDescription>();
        public List<RegisteredWindows> registeredWindowList = new List<RegisteredWindows>();

        // Related network object if we place the watch on a network rig
        NetworkObject _networkObject;

        [Header("Various settings")]
        public Vector3 windowSpawnPositionOffsetRelativeToHeadset = new Vector3(0.15f, -0.1f, 0.5f);
        [SerializeField] bool flipWindow = false;
        [SerializeField] string defaultWindowText = "";

        public bool IsLocalUserWindowHandler => _networkObject == null || _networkObject.HasStateAuthority;

        IHardwareRig _hardwareRig;
        string _desiredWatchText = null;

        Dictionary<string, RegisteredWindows> _registeredWindows = new Dictionary<string, RegisteredWindows>();
        bool _rigPositionChecked = false;
        Vector3 _lastRigPosition = Vector3.zero;
        bool _initialInstantiationChecked = false;

        private void Awake()
        {
            foreach(var screen in GetComponentsInChildren<WatchScreen>(true))
            {
                RegisterWatchScreen(screen);
            }

            _networkObject = GetComponentInParent<NetworkObject>();

            foreach (var info in windowsDescriptions) RegisterWindow(info);
        }

        #region IWatchScreenListener
        public void RegisterWatchScreen(WatchScreen screen)
        {
            if (watchScreens.Contains(screen) == false) watchScreens.Add(screen);
            if(screen != null && string.IsNullOrEmpty(_desiredWatchText) == false)
            {
                screen.UpdateWatchText(_desiredWatchText);
            }
        }

        public void UnregisterWatchScreen(WatchScreen screen)
        {
            if (watchScreens.Contains(screen)) watchScreens.Remove(screen);
        }
        #endregion 

        private void Start()
        {
            UpdateWatchText(defaultWindowText);
        }

        public void RegisterWindow(WindowDescription description)
        {
            if (_registeredWindows.ContainsKey(description.windowName))
            {
                Debug.LogError($"Window {description.windowName} already known (with prefab {_registeredWindows[description.windowName].windowDescription.windowPrefab})." +
                    $" Cancelling new registration with prefab {description.windowPrefab}");
                return;
            }
            _registeredWindows[description.windowName] = new RegisteredWindows { windowDescription = description, windowInstance = null };
            registeredWindowList.Add(_registeredWindows[description.windowName]);
        }

        private WatchWindow InstantiateWindowByName(string name, bool startOpen = false)
        {
            if (_registeredWindows.ContainsKey(name))
            {
                InstantiateWindow(ref _registeredWindows[name].windowInstance, _registeredWindows[name].windowDescription.windowPrefab, startOpen);
            }
            else
            {
                Debug.LogError("Unregistered window " + name);
            }
            return null;
        }

        public WatchWindow InstantiateWindow(ref WatchWindow window, WatchWindow windowPrefab, bool startOpen = false)
        {
            if (window == null && windowPrefab != null)
            {
                var windowSpawnPosition = _hardwareRig.Headset.gameObject.transform.TransformPoint(windowSpawnPositionOffsetRelativeToHeadset);
                window = Instantiate(windowPrefab, windowSpawnPosition, Quaternion.identity);
                if (startOpen == false)
                {
                    window.gameObject.SetActive(false);
                }
                window.watchWindowsHandler = this;
            }
            return window;
        }

        public void UpdateWatchText(string text)
        {
            _desiredWatchText = text;
            foreach (var watchScreen in watchScreens)
            {
                watchScreen.UpdateWatchText(text);
            }
        }

        /// <summary>
        /// Register a window description if not yet know, then toggle it
        /// </summary>
        /// <param name="windowDescription"></param>
        public WatchWindow ToggleWindow(WindowDescription windowDescription)
        {
            if (_registeredWindows.ContainsKey(windowDescription.windowName) == false)
            {
                RegisterWindow(windowDescription);
            }
            return ToggleWindowByName(windowDescription.windowName);
        }

        public void DoToggleWindowByName(string name)
        {
            ToggleWindowByName(name);
        }

        public WatchWindow GetWindowByName(string windowName, bool startOpen = false)
        {
            if (_registeredWindows.ContainsKey(windowName))
            {
                if (IsLocalUserWindowHandler)
                {
                    if (_registeredWindows[windowName].windowInstance == null)
                    {
                        InstantiateWindowByName(windowName, startOpen);
                    }
                    if (_registeredWindows[windowName].windowInstance == null)
                    {
                        Debug.LogError("Unable to instanciate window " + windowName);
                    }
                    else
                    {
                        return _registeredWindows[windowName].windowInstance;
                    }
                }
            }
            else
            {
                Debug.LogError($"[WatchWindowsHandler {this.name}] Unregistered window " + windowName);
            }
            return null;
        }

        public WatchWindow ToggleWindowByName(string windowName)
        {
            var window = GetWindowByName(windowName);
            if (window)
            {
                return ToggleWindow(window);
            }
            return null;
        }

        WatchWindow ToggleWindow(WatchWindow window)
        {
            if (window)
            {
                DisplayWindow(window, shouldDisplay: !window.gameObject.activeSelf);

                return window;
            }
            return null;
        }

        public WatchWindow DisplayWindowByName(string windowName, bool shouldDisplay = true)
        {
            var menu = GetWindowByName(windowName, startOpen: shouldDisplay);
            if (menu)
            {
                DisplayWindow(menu);
            }
            return menu;
        }

        public void DisplayWindow(WatchWindow window, bool shouldDisplay = true)
        {
            window.gameObject.SetActive(shouldDisplay);
            PositionWindow(window);
        }

        public void PositionWindow(WatchWindow window)
        {
            if (window && window.gameObject.activeSelf)
            {
                var headsetTransform = _hardwareRig.Headset.gameObject.transform;

                var windowPosition = headsetTransform.TransformPoint(windowSpawnPositionOffsetRelativeToHeadset);
                Quaternion windowRotation;
                if (flipWindow)
                {
                    windowRotation = Quaternion.Euler(0, headsetTransform.eulerAngles.y, 0);
                }
                else
                {
                    windowRotation = Quaternion.Euler(0, 180 + headsetTransform.eulerAngles.y, 0);
                }
                window.transform.position = windowPosition;
                window.transform.rotation = windowRotation;
                window.transform.localScale = _hardwareRig.transform.localScale;
            }
        }

        private void Update()
        {
            if (_hardwareRig == null)
            {
                _hardwareRig = HardwareRigsRegistry.GetHardwareRig();
            }
            if (_hardwareRig == null) return;

            if(_initialInstantiationChecked == false && IsLocalUserWindowHandler)
            {
                _initialInstantiationChecked = true;
                foreach (var registeredWindow in _registeredWindows.Values)
                {
                    if (registeredWindow.windowDescription.instantiateHiddenAtStart && registeredWindow.windowInstance == null) 
                    { 
                        InstantiateWindowByName(registeredWindow.windowDescription.windowName); 
                    }
                }
            }

            // We close the window in case of large rig teleportation
            if (_rigPositionChecked && Vector3.Distance(_lastRigPosition, _hardwareRig.transform.position) > 0.5f)
            {
                foreach (var registeredWindow in _registeredWindows.Values)
                {
                    if (registeredWindow.windowInstance != null && registeredWindow.windowInstance.gameObject.activeSelf && registeredWindow.windowInstance.closeOnLargeRigMove)
                    {
                        ToggleWindowByName(registeredWindow.windowDescription.windowName);
                    }                        
                }
            }
            _lastRigPosition = _hardwareRig.transform.position;
            _rigPositionChecked = true;
        }
    }
}
