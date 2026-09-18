using Fusion;
using UnityEngine;

namespace Fusion.Addons.AnchorsAddon.Colocalization.Minimap
{
    /// <summary>
    /// Move an element to follow another one, by changing the target local position to match their relative position to their own referentials
    /// This allows to create minimap effects
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    public class LocalPoseProxy : MonoBehaviour
    {
        public const int EXECUTION_ORDER = NetworkIRLRoomAssociatedPart.EXECUTION_ORDER + 20;
        public Transform source;
        public Transform sourceReferential;
        public Transform target;
        public Transform targetReferential;
        public bool swapLogicIfTargetNotStateAuthority = false;
        public bool forceLogicSwap = false;
        public bool useWorldPositionWhenSwappedWithNoReferential = true;
        public bool adaptScale = false;

        public NetworkObject targetNetworkObject;
        public bool disableLateUpdateAdaptation = false;
        public Transform ObjectToFollow => SwapLogic == false ? source : target;
        public Transform ObjectToFollowReferential => SwapLogic == false ? sourceReferential : targetReferential;
        public Transform ObjectToMove => SwapLogic == false ? target : source;
        public Transform ObjectToMoveReferential => SwapLogic == false ? targetReferential : sourceReferential;
        bool SwapLogic => forceLogicSwap || (swapLogicIfTargetNotStateAuthority && targetNetworkObject != null && targetNetworkObject.IsValid && targetNetworkObject.HasStateAuthority == false);
        bool _isTargetAnalysed = false;

        private void LateUpdate()
        {
            if (disableLateUpdateAdaptation == false)
            {
                AdaptPosition();
            }
        }

        public void AdaptPosition()
        {
            if (source == null || target == null) return;
            if (_isTargetAnalysed == false)
            {
                _isTargetAnalysed = true;
                targetNetworkObject = target.GetComponent<NetworkObject>();
            }
            Vector3 localPosition;
            Quaternion localRotation;
            if (ObjectToFollowReferential)
            {
                localPosition = ObjectToFollowReferential.InverseTransformPoint(ObjectToFollow.position);
                localRotation = Quaternion.Inverse(ObjectToFollowReferential.rotation) * ObjectToFollow.rotation;
            }
            else
            {
                localPosition = ObjectToFollow.position;
                localRotation = ObjectToFollow.rotation;
            }

            if (ObjectToMoveReferential)
            {
                ObjectToMove.rotation = ObjectToMoveReferential.rotation * localRotation;
                ObjectToMove.position = ObjectToMoveReferential.TransformPoint(localPosition);
            }
            else if (SwapLogic && useWorldPositionWhenSwappedWithNoReferential)
            {
                ObjectToMove.rotation = localRotation;
                ObjectToMove.position = localPosition;
            }
            else
            {
                ObjectToMove.localRotation = localRotation;
                ObjectToMove.localPosition = localPosition;
            }

            if (adaptScale)
            {
                ObjectToMove.localScale = ObjectToFollow.localScale;
            }
        }
    }
}

