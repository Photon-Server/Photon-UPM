using Fusion.XR.Shared.Core;
using UnityEngine;

namespace Fusion.XR.Shared.Base
{
    public class HardwareHand : BaseLateralizedHardwareRigPart, IHardwareHand
    {
        #region IHardwareHand
        public Transform IndexTipFollowerTransform { get => indexTipFollowerTransform; set => indexTipFollowerTransform = value; }
        public Transform WristFollowerTransform { get => wristFollowerTransform; set => wristFollowerTransform = value; }
        #endregion
        public override RigPartKind Kind => RigPartKind.Hand;

        public Transform indexTipFollowerTransform;
        public Transform wristFollowerTransform;

        public virtual Pose WorldThumbTipPose => RigPartPose;
        public virtual Pose WorldIndexTipPose => RigPartPose;
        public virtual Pose WorldIndexBasePose => RigPartPose;
        public virtual Pose WorldMiddleTipPose => RigPartPose;
        public virtual Pose WorldRingTipPose => RigPartPose;
        public virtual Pose WorldLittleTipPose => RigPartPose;
        public virtual Pose WorldWristPose => RigPartPose;
        public virtual Pose WorldPalmPose => RigPartPose;

        protected override void LateUpdate()
        {
            base.LateUpdate();
            PositionHandBoneFollowers();
        }

        protected override void Update()
        {
            base.Update();
            PositionHandBoneFollowers();
        }

        protected virtual void PositionHandBoneFollowers()
        {
            if (indexTipFollowerTransform != null)
            {
                var indexTipPose = WorldIndexTipPose;
                indexTipFollowerTransform.SetPositionAndRotation(indexTipPose.position, indexTipPose.rotation);
            }
            if (wristFollowerTransform != null)
            {
                var wristPose = WorldWristPose;
                wristFollowerTransform.SetPositionAndRotation(wristPose.position, wristPose.rotation);
            }
        }
    }
} 

