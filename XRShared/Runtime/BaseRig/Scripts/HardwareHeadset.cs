using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Core.Interaction;
using UnityEngine;

namespace Fusion.XR.Shared.Base
{
    public class HardwareHeadset : BaseHardwareRigPart, IHardwareHeadset, IFadeable
    {
        [Tooltip("Automatically setup if not defined")]
        public Camera headsetCamera = null;
        public override RigPartKind Kind => RigPartKind.Headset;

        public ICameraFader Fader { get; set; }

        bool _cameraSearched = false;

        public Camera HeadsetCamera { 
            get {
                if (headsetCamera == null && _cameraSearched == false)
                {
                    _cameraSearched = true;
                    headsetCamera = GetComponentInChildren<Camera>();
                }
                return headsetCamera;
            } 
        }


        protected override void Awake()
        {
            base.Awake();
            Fader = GetComponentInChildren<ICameraFader>(true);
        }
    }
}