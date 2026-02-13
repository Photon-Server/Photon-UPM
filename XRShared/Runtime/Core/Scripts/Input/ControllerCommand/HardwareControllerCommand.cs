using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Fusion.XR.Shared.Core
{
    public class HardwareControllerCommand : MonoBehaviour, IHandCommandProvider
    {
#if ENABLE_INPUT_SYSTEM
        [Header("Hand command actions")]
        public bool updateHandCommandWithAction = true;
        public InputActionProperty thumbAction = new InputActionProperty(new InputAction());
        public InputActionProperty gripAction = new InputActionProperty(new InputAction());
        public InputActionProperty triggerAction = new InputActionProperty(new InputAction());
        public InputActionProperty indexAction = new InputActionProperty(new InputAction());
#endif

        IHardwareController hardwareController;

        [HideInInspector]
        public List<IHandCommandHandler> commandHandlers = new List<IHandCommandHandler>();

        HandCommand _handCommand;

        #region IHandCommandProvider
        public HandCommand HandCommand
        {
            get
            {
                return _handCommand;
            }
            set
            {
                _handCommand = value;
            }
        }
        public void RegisterCommandHandler(IHandCommandHandler handler) {
            if (commandHandlers.Contains(handler) == false)
            {
                commandHandlers.Add(handler);
            }
        }

        public void UnregisterCommandHandler(IHandCommandHandler handler)
        {
            if (commandHandlers.Contains(handler))
            {
                commandHandlers.Remove(handler);
            }
        }

        #endregion

        protected virtual void Awake()
        {
            hardwareController = GetComponentInParent<IHardwareController>();
            if (hardwareController == null) throw new System.Exception("Should be placed under a IHardwareController hierarchy");
#if ENABLE_INPUT_SYSTEM
            thumbAction.EnableWithDefaultXRBindings(side: hardwareController.Side, new List<string> { "thumbstickTouched", "primaryTouched", "secondaryTouched" });
            gripAction.EnableWithDefaultXRBindings(side: hardwareController.Side, new List<string> { "grip" });
            triggerAction.EnableWithDefaultXRBindings(side: hardwareController.Side, new List<string> { "trigger" });
            indexAction.EnableWithDefaultXRBindings(side: hardwareController.Side, new List<string> { "triggerTouched" });
#else
            Debug.LogError("Missing com.unity.inputsystem package");
#endif
            foreach (var handler in GetComponentsInChildren<IHandCommandHandler>())
            {
                RegisterCommandHandler(handler);
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            // update hand pose
            if (updateHandCommandWithAction)
            {
                _handCommand.thumbTouchedCommand = thumbAction.action.ReadValue<float>();
                _handCommand.indexTouchedCommand = indexAction.action.ReadValue<float>();
                _handCommand.gripCommand = gripAction.action.ReadValue<float>();
                _handCommand.triggerCommand = triggerAction.action.ReadValue<float>();
                _handCommand.pinchCommand = 0;

            }
#else
            Debug.LogError("Missing com.unity.inputsystem package");
#endif
            ApplyHandComand(_handCommand);
        }

        void ApplyHandComand(HandCommand command)
        {
            foreach (var h in commandHandlers) h.SetHandCommand(command);
        }
    }

}
