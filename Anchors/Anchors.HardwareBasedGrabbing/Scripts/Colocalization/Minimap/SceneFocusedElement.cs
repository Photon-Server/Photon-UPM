using Fusion.XR.Shared.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.AnchorsAddon.Colocalization.Minimap
{
    /// <summary>
    /// Indicate that this object should be displayed on the minimap, with minimapPrefab being spawned to represent it
    /// </summary>
    /// 
    public class SceneFocusedElement : MonoBehaviour
    {
        public GameObject minimapPrefab;

        [Header("Usage in common room")]
        public bool addToCommonRoom = true;
        public List<Transform> commonRoomAdditionnalTransforms = new List<Transform>();
        public bool updateCommonRoomOnMove = true;
        public Transform boundingBoxTransform = null;
        public bool doNotAddFocusedElementTransform = false;

        Vector3 _lastPosition;
        Quaternion _lastRotation;

        private void Start()
        {
            IRLRoomsMinimap.SharedInstance.RegisterSceneFocusedElement(this);
            if (addToCommonRoom)
            {
                if(doNotAddFocusedElementTransform == false && commonRoomAdditionnalTransforms.Contains(transform) == false)
                {
                    commonRoomAdditionnalTransforms.Add(transform);
                }

                if (boundingBoxTransform)
                {
                    AddBoundingBoxPoint(new Vector3(-0.5f, 0.5f, 0.5f));
                    AddBoundingBoxPoint(new Vector3(-0.5f, 0.5f, -0.5f));
                    AddBoundingBoxPoint(new Vector3(0.5f, 0.5f, -0.5f));
                    AddBoundingBoxPoint(new Vector3(0.5f, 0.5f, 0.5f));
                    AddBoundingBoxPoint(new Vector3(-0.5f, -0.5f, 0.5f));
                    AddBoundingBoxPoint(new Vector3(-0.5f, -0.5f, -0.5f));
                    AddBoundingBoxPoint(new Vector3(0.5f, -0.5f, -0.5f));
                    AddBoundingBoxPoint(new Vector3(0.5f, -0.5f, 0.5f));
                }

                foreach (var commonRoomAdditionnalTransform in commonRoomAdditionnalTransforms)
                {
                    MixedRealityRemoteUsersRoom.SharedInstance.RegisterRemoteUsersRoomAdditionalTransforms(commonRoomAdditionnalTransform);
                }
            }
        }

        void AddBoundingBoxPoint(Vector3 localPoint)
        {
            var gameObject = new GameObject("Edge");
            gameObject.transform.parent = boundingBoxTransform;
            gameObject.transform.localPosition = localPoint;
            gameObject.transform.localRotation = Quaternion.identity;
            commonRoomAdditionnalTransforms.Add(gameObject.transform);
        }

        private void OnDestroy()
        {
            IRLRoomsMinimap.SharedInstance?.UnregisterSceneFocusedElement(this);
            foreach (var commonRoomAdditionnalTransform in commonRoomAdditionnalTransforms)
            {
                MixedRealityRemoteUsersRoom.SharedInstance?.UnregisterRemoteUsersRoomAdditionalTransforms(commonRoomAdditionnalTransform);
            }
        }

        private void Update()
        {
            if (updateCommonRoomOnMove && (_lastPosition != transform.position || Quaternion.Angle(_lastRotation, transform.rotation) > 1))
            {
                _lastPosition = transform.position;
                _lastRotation = transform.rotation;
                MixedRealityRemoteUsersRoom.SharedInstance.RequestUpdateRemoteUsersRoom();
            }
        }
    }
}

