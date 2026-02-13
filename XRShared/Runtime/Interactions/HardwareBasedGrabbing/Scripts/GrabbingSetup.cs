using Fusion.XR.Shared.Core.Interaction;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.XR.Shared.Core.HardwareBasedGrabbing
{
    public class GrabbingSetup : MonoBehaviour
    {

        IHardwareRig hardwareRig;
        List <IRigPart> grabbingRigParts = new List<IRigPart> ();

        [Tooltip("If true, for IHardwareHand, the grabbing won't be place on the hand palm but on the index, if an index follower is set on the hand")]
        public bool useIndexFollowerWhenAvailableForGrabbing = true;

        [Tooltip("If true, for IHardwareController, we don't want to use the index for grabbing (the palm is more natural), so we'll ignore collider with a IInteractionTip parent (we're on an index only collider), and if nothing else is found, we'll add a dedicated collider")]
        public bool ignoreInteractionTipColliderForController = true;

        [Tooltip("If true, don't display warning when automaticaly setting a collider is needed")]
        public bool removeColliderAutosetupWarning = false;

        [Header("Debug")]
        [SerializeField]
        bool logCreation = false;

        private void Awake()
        {
            hardwareRig = GetComponent<IHardwareRig>();
        }

        // Update is called once per frame
        void Update()
        {
            GrabberVerification();
        }

        void GrabberVerification()
        {
            foreach (var rigPart in hardwareRig.RigParts)
            {
                if (rigPart is IHardwareController || rigPart is IHardwareHand)
                {
                    if(grabbingRigParts.Contains(rigPart)) continue;
                    grabbingRigParts.Add(rigPart);


                    var root = rigPart.transform;
                    bool indexFound = false;
                    if (useIndexFollowerWhenAvailableForGrabbing && rigPart is IHardwareHand hand)
                    {
                        if (hand.IndexTipFollowerTransform != null)
                        {
                            root = hand.IndexTipFollowerTransform;
                            indexFound = true;
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

                    var grabber = root.gameObject.GetComponentInChildren<Grabber>();

                    if (grabber == null)
                    {
                        if (logCreation) Debug.Log($"[GrabbingSetup] Creating grabber for {rigPart.gameObject.name} under {root.name}");
                        grabber = root.gameObject.AddComponent<Grabber>();
                    }

                    var rigidBody = grabber.GetComponent<Rigidbody>();
                    if (rigidBody == null)
                    {
                        if (logCreation) Debug.Log($"[GrabbingSetup] Creating Rigidbody for {rigPart.gameObject.name} under {grabber.name}");
                        rigidBody = grabber.gameObject.AddComponent<Rigidbody>();
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
                        while(t != null)
                        {
                            if (ignoreInteractionTipColliderForController)
                            {
                                var indexTip = t.GetComponent<IInteractionTip>();
                                if (indexTip != null)
                                {
                                    if(logCreation) Debug.Log($"[{GetType().Name}] The collider {existingCollider.name} has an index tip ({t.name}). For controllers, we don't want to use the index for grabbing (the palm is more natural), so we'll ignore this collider, and if nothing else is found, we'll add one). we can not use it for {rigPart.gameObject.name}");
                                    // The collider has an index tip. For controllers, we don't want to use the index for grabbing (the palm is more natural), so we'll ignore this collider, and if nothing else is found, we'll add one)
                                    break;
                                }
                            }
                            var r = t.GetComponent<Rigidbody>();
                            if(r != null && r != rigidBody)
                            {
                                if (logCreation) Debug.Log($"[{GetType().Name}] The collider {existingCollider.name} has another parent rigidbody ({t.name}), we can not use it for {rigPart.gameObject.name}");
                                // The collider has another parent rigidbody, we can not use it
                                break;
                            }
                            if (r == rigidBody)
                            {
                                // The collider has no other parent rigidbody, we can use it
                                if (logCreation) Debug.Log($"[{GetType().Name}] The collider {existingCollider.name} has no other parent rigidbody ({t.name} contains the source rigidbody). We can use it for {rigPart.gameObject.name}");
                                collider = existingCollider;
                                break;
                            }
                            t = t.parent;
                        }
                        if(collider != null)
                        {
                            break;
                        }
                    }


                    if (useIndexFollowerWhenAvailableForGrabbing && indexFound)
                    {
                        // Set an index collider
                        if (collider == null)
                        {
                            if (logCreation) Debug.Log($"[GrabbingSetup] Creating SphereCollider for {rigPart.gameObject.name} under {grabber.name}");
                            var sphereColliderGO = new GameObject("InteractionCollider");
                            sphereColliderGO.transform.parent = root;
                            sphereColliderGO.transform.localPosition = Vector3.zero;
                            sphereColliderGO.transform.localRotation = Quaternion.identity;
                            var sphereCollider = sphereColliderGO.AddComponent<SphereCollider>();
                            sphereCollider.transform.localScale = Vector3.one * 0.04f;
                            sphereCollider.radius = 0.2f;
                            sphereCollider.isTrigger = true;
                            if (removeColliderAutosetupWarning == false)
                                Debug.LogWarning($"A default index collider has been added for grabbing under the indexTipFollowerTransform {root}. Please create on in the scene to have desired positionning, or set removeColliderAutosetupWarning to true.");
                        }
                    }
                    else
                    {
                        // Set a palm collider
                        if (collider == null)
                        {
                            if (logCreation) Debug.Log($"[GrabbingSetup] Creating BoxCollider for {rigPart.gameObject.name} under {grabber.name}");
                            var boxColliderGO = new GameObject("InteractionCollider");
                            boxColliderGO.transform.parent = root.transform;
                            boxColliderGO.transform.localPosition = Vector3.zero;
                            boxColliderGO.transform.localRotation = Quaternion.identity;
                            var boxCollider = boxColliderGO.AddComponent<BoxCollider>();
                            boxCollider.transform.localScale = Vector3.one * 0.1f;
                            boxCollider.isTrigger = true;
                            if (removeColliderAutosetupWarning == false)
                                Debug.LogWarning($"A default box collider has been added for grabbing under the palm {root}. Please create on in the scene to have desired positionning.");
                        }
                    }
                }
            }
        }
    }
}
