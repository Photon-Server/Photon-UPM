using Fusion.XR.Shared.Core.Interaction;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.XR.Shared.Core.Touch
{
    public class TouchingSetup : MonoBehaviour
    {

        IHardwareRig hardwareRig;
        List <IRigPart> touchingRigParts = new List<IRigPart> ();

        [Tooltip("If true, for IHardwareHand, the touching won't be place on the hand palm but on the index, if an index follower is set on the hand, or if a IndexTipMarker is found")]
        public bool useIndexWhenAvailableForTouching = true;

        [Tooltip("If true, don't display warning when automaticaly setting a collider is needed")]
        public bool removeColliderAutosetupWarning = false;

        [Header("Debug")]
        [SerializeField]
        bool logCreation = false;
        private void Awake()
        {
            hardwareRig = GetComponent<IHardwareRig>();
        }

        void Update()
        {
            ToucherVerification();
        }

        void ToucherVerification()
        {
            foreach (var rigPart in hardwareRig.RigParts)
            {
                if (rigPart is IHardwareController || rigPart is IHardwareHand)
                {
                    // Skip the check if already setup
                    if(touchingRigParts.Contains(rigPart)) continue;

                    touchingRigParts.Add(rigPart);


                    var root = rigPart.transform;
                    bool indexFound = false;
                    if (useIndexWhenAvailableForTouching)
                    {
                        if (rigPart is IHardwareHand hand)
                        {
                            if (hand.IndexTipFollowerTransform != null)
                            {
                                root = hand.IndexTipFollowerTransform;
                                indexFound = true;
                            }
                        }
                        if (indexFound == false)
                        {
                            var indexTipMark = rigPart.gameObject.GetComponentInChildren<IInteractionTip>(true);
                            if (indexTipMark != null)
                            {
                                root = indexTipMark.transform;
                                indexFound = true;
                            }
                        }
                    }

                    var toucher = root.GetComponentInChildren<Toucher>();
                    if (toucher == null)
                    {
                        if (logCreation) Debug.Log($"[TouchingSetup] Creating toucher for {rigPart.gameObject.name} under {root.name}");
                        toucher = root.gameObject.AddComponent<Toucher>();
                    }
                    var rigidBody = toucher.GetComponent<Rigidbody>();
                    if (rigidBody == null)
                    {
                        if (logCreation) Debug.Log($"[TouchingSetup] Creating toucrigibody for {rigPart.gameObject.name} under {root.name}");
                        rigidBody = toucher.gameObject.AddComponent<Rigidbody>();
                        rigidBody.isKinematic = true;
                    }

                    Collider collider = null;
                    foreach (var existingCollider in rigidBody.GetComponentsInChildren<Collider>())
                    {
                        if (existingCollider.enabled == false)
                        {
                            continue;
                        }
                        Transform t = existingCollider.transform;
                        while (t != null)
                        {
                            var r = t.GetComponent<Rigidbody>();
                            if (r != null && r != rigidBody)
                            {
                                // the collider has another parent rigidbody, we can not use it
                                if (logCreation) Debug.Log($"[{GetType().Name}] The collider {existingCollider.name} has another parent rigidbody ({t.name}), we can not use it for {rigPart.gameObject.name}");
                                break;
                            }
                            if (r == rigidBody)
                            {
                                // the collider has no other parent rigidbody, we can use it
                                collider = existingCollider;
                                break;
                            }
                            t = t.parent;
                        }
                        if (collider != null)
                        {
                            break;
                        }
                    }

                    if (collider == null){
                        if (useIndexWhenAvailableForTouching && indexFound)
                        {
                            // Set an index collider
                            if (collider == null)
                            {
                                if (logCreation) Debug.Log($"[TouchingSetup] Creating touching sphere collider for {rigPart.gameObject.name} under {root.name}");
                                var sphereColliderGO = new GameObject("InteractionCollider");
                                sphereColliderGO.transform.parent = root;
                                sphereColliderGO.transform.localPosition = Vector3.zero;
                                sphereColliderGO.transform.localRotation = Quaternion.identity;
                                var sphereCollider = sphereColliderGO.AddComponent<SphereCollider>();
                                sphereCollider.transform.localScale = Vector3.one * 0.04f;
                                sphereCollider.radius = 0.2f;
                                sphereCollider.isTrigger = true;
                                if (removeColliderAutosetupWarning == false)
                                    Debug.LogWarning($"A default index collider has been added for grabbing under {root.name}. Please create on in the scene to have desired positionning, or set removeColliderAutosetupWarning to true.");
                            }
                        }
                        else
                        {
                            // Set a palm collider
                            if (collider == null)
                            {
                                if (logCreation) Debug.Log($"[TouchingSetup] Creating touching box collider for {rigPart.gameObject.name} under {root.name}");
                                var boxColliderGO = new GameObject("InteractionCollider");
                                boxColliderGO.transform.parent = rigPart.transform;
                                boxColliderGO.transform.localPosition = Vector3.zero;
                                boxColliderGO.transform.localRotation = Quaternion.identity;
                                var boxCollider = boxColliderGO.AddComponent<BoxCollider>();
                                boxCollider.transform.localScale = Vector3.one * 0.1f;
                                boxCollider.isTrigger = true;
                                if (removeColliderAutosetupWarning == false)
                                    Debug.LogWarning($"A default box collider has been added for grabbing under {root.name}. Please create on in the scene to have desired positionning, or set removeColliderAutosetupWarning to true.");
                            }
                        }
                    }
                }
            }
        }
    }
}
