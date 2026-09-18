using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.XR.Shared.Utils
{
    public static class TransformManipulations
    {
        #region Object position computation to respect offset constraints to a point
        /// <summary>
        /// Return the position and rotation of a referential referenceTransform
        ///  so that a positioned object, already placed properly, will have the desired position offset/rotation offset,
        ///  if the function results are applied to the referential
        /// </summary>
        /// <param name="referenceTransform">actual transform we would move with the results to have the desired effect</param>
        /// <param name="positionedObjectPosition">world position of the object (already placed properly)</param>
        /// <param name="positionedObjectRotation">world rotation of the object (already placed properly)</param>
        /// <param name="desiredPositionOffset">desired position offset</param>
        /// <param name="desiredRotationOffset">desired rotation offset</param>
        /// <param name="acceptLossyScale">[Parented referential] Force accepting less accurate results when referential is parented and forcedScale is not set</param>
        /// <param name="forcedScale">[Parented referential] Override the referential scale (useful for accurate results when referential transform is parented)</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Raise an exception if the referential has a parent, and 1) forcedScale is not set, 2) acceptLossyScale is not true</exception>
        public static (Vector3 newReferencePosition, Quaternion newReferencerotation) ReferentialPositionToRespectOffsetsOfPositionedObject(
            Transform referenceTransform,
            Vector3 positionedObjectPosition, Quaternion positionedObjectRotation,
            Vector3 desiredPositionOffset, Quaternion desiredRotationOffset,
            bool acceptLossyScale = false,
            Vector3? forcedScale = null
            )
        {
            var newReferentialRotation = positionedObjectRotation * Quaternion.Inverse(desiredRotationOffset);
            // We do not apply the rotation to the transform right now, so to use the rotated transform, we can't rely on it and have to use a matrix to emulate in advance the new transform position
            Vector3 scale;
            if (forcedScale != null)
            {
                scale = forcedScale.GetValueOrDefault();
            }
            else if (referenceTransform.parent != null)
            {
                if (acceptLossyScale)
                {
                    scale = referenceTransform.lossyScale;
                }
                else
                {
                    throw new System.Exception("[ReferentialPositionToRespectChildPositionOffset] Lossy scale not accepted while the reference transform has a parent");
                }
            }
            else
            {
                scale = referenceTransform.localScale;
            }
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransform.position, newReferentialRotation, scale);
            // If the transform was already rotated, it would be equivalent to Equivalent to:
            //     var offsetInRotatedReference = referenceTransform.TransformPoint(positionOffset);
            var offsetedInRotatedReference = referenceTransformMatrix.MultiplyPoint(desiredPositionOffset);
            var newReferentialPosition = positionedObjectPosition - (offsetedInRotatedReference - referenceTransform.transform.position);
            // TODO: debug code (to test result), remove
            var movedReferenceTransformMatrix = Matrix4x4.TRS(newReferentialPosition, newReferentialRotation, referenceTransform.localScale);
            var appliedOffsetInFixedRef = movedReferenceTransformMatrix.MultiplyPoint(desiredPositionOffset);
            return (newReferentialPosition, newReferentialRotation);
        }


        /// <summary>
        /// Return the position and rotation of a referential referenceTransform
        ///  so that a positioned object, already placed properly, will have the desired position offset/rotation offset,
        ///  if the function results are applied to the referential.
        ///  
        /// desiredPositionOffset and desiredRotationOffset must be passed as offset without scale (can be obtained with UnscaledOffset)
        /// </summary>
        /// <param name="referenceTransform">actual transform we would move with the results to have the desired effect</param>
        /// <param name="positionedObjectPosition">world position of the object (already placed properly)</param>
        /// <param name="positionedObjectRotation">world rotation of the object (already placed properly)</param>
        /// <param name="desiredPositionOffset">desired position offset without scale (can be obtained with UnscaledOffset)</param>
        /// <param name="desiredRotationOffset">desired rotation offset</param>
        /// <returns></returns>
        public static (Vector3 newReferencePosition, Quaternion newReferencerotation) ReferentialPositionToRespectOffsetsOfPositionedObjectWithUnscaledOffsets(
            Transform referenceTransform,
            Vector3 positionedObjectPosition, Quaternion positionedObjectRotation,
            Vector3 desiredPositionOffset, Quaternion desiredRotationOffset
        )
        {
            return ReferentialPositionToRespectOffsetsOfPositionedObject(referenceTransform, positionedObjectPosition, positionedObjectRotation, desiredPositionOffset, desiredRotationOffset, forcedScale: Vector3.one);
        }

        /// <summary>
        /// Return the position and rotation of a virtual referential referenceTransform defined by its position and rotation
        ///  so that a positioned object, already placed properly, will have the desired position offset/rotation offset,
        ///  if the function results are applied to the referential.
        ///  
        /// desiredPositionOffset and desiredRotationOffset must be passed as offset without scale (can be obtained with UnscaledOffset)
        /// </summary>
        /// <param name="referenceTransform">actual transform we would move with the results to have the desired effect</param>
        /// <param name="positionedObjectPosition">world position of the object (already placed properly)</param>
        /// <param name="positionedObjectRotation">world rotation of the object (already placed properly)</param>
        /// <param name="desiredPositionOffset">desired position offset without scale (can be obtained with UnscaledOffset)</param>
        /// <param name="desiredRotationOffset">desired rotation offset</param>
        /// <returns></returns>
        public static (Vector3 newReferencePosition, Quaternion newReferencerotation) ReferentialPositionToRespectOffsetsOfPositionedObjectWithUnscaledOffsets(
            Vector3 referenceTransformPosition, Quaternion referenceTransformRotation,
            Vector3 positionedObjectPosition, Quaternion positionedObjectRotation,
            Vector3 desiredPositionOffset, Quaternion desiredRotationOffset
        )
        {
            var newReferentialRotation = positionedObjectRotation * Quaternion.Inverse(desiredRotationOffset);
            // We do not apply the rotation to the transform right now, so to use the rotated transform, we can't rely on it and have to use a matrix to emulate in advance the new transform position
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransformPosition, newReferentialRotation, Vector3.one);

            // If the transform was already rotated, it would be equivalent to Equivalent to:
            //     var offsetInRotatedReference = referenceTransform.TransformPoint(positionOffset);
            var offsetedInRotatedReference = referenceTransformMatrix.MultiplyPoint(desiredPositionOffset);
            var newReferentialPosition = positionedObjectPosition - (offsetedInRotatedReference - referenceTransformPosition);
            return (newReferentialPosition, newReferentialRotation);
        }
        #endregion

        #region Relative offset without taking scale into account
        /// <summary>
        /// Return a transform position/rotation offset relative to another transform
        /// For the position, equivalent to "offsetPosition = referenceTransform.InverseTransformPoint(transformToOffset.position)" when the referenceTransform scale is Vector3.one, as well as the ones of its parents
        /// </summary>
        public static (Vector3 offset, Quaternion rotationOffset) UnscaledOffset(Transform referenceTransform, Transform transformToOffset)
        {
            // Equivalent to "offset = referenceTransform.InverseTransformPoint(transformToOffset.position)" when the referenceTransform scale is Vector3.one, as well as the one of its parents
            return UnscaledOffset(referenceTransform.position, referenceTransform.rotation, transformToOffset);
        }

        /// <summary>
        /// Return a transform position/rotation offset relative to another virtual transform, with referenceTransform.position=referenceTransformPosition, referenceTransform.rotation=referenceTransformRotation, referenceTransform.scale=Vector3.one (as well as its parents' scales)
        /// For the position, equivalent to "offsetPosition = referenceTransform.InverseTransformPoint(transformToOffset.position)" when the referenceTransform scale is Vector3.one, as well as the ones of its parents
        /// </summary>
        /// <returns></returns>
        public static (Vector3 offset, Quaternion rotationOffset) UnscaledOffset(Vector3 referenceTransformPosition, Quaternion referenceTransformRotation, Transform transformToOffset)
        {
            return UnscaledOffset(referenceTransformPosition, referenceTransformRotation, transformToOffset.position, transformToOffset.rotation);
        }

        /// <summary>
        /// Return a transform position/rotation offset relative to another virtual transform, with referenceTransform.position=referenceTransformPosition, referenceTransform.rotation=referenceTransformRotation, referenceTransform.scale=Vector3.one (as well as its parents' scales)
        /// For the position, equivalent to "offsetPosition = referenceTransform.InverseTransformPoint(transformToOffset.position)" when the referenceTransform scale is Vector3.one, as well as the ones of its parents
        /// </summary>
        /// <returns></returns>
        public static (Vector3 offset, Quaternion rotationOffset) UnscaledOffset(Vector3 referenceTransformPosition, Quaternion referenceTransformRotation, Vector3 transformToOffsetPosition, Quaternion transformToOffsetRotation)
        {
            var offset = UnscaledOffset(referenceTransformPosition, referenceTransformRotation, transformToOffsetPosition);
            var rotationOffset = Quaternion.Inverse(referenceTransformRotation) * transformToOffsetRotation;
            return (offset, rotationOffset);
        }

        /// <summary>
        /// Return a position offset relative to another virtual transform, with referenceTransform.position=referenceTransformPosition, referenceTransform.rotation=referenceTransformRotation, referenceTransform.scale=Vector3.one (as well as its parents' scales)
        /// For the position, equivalent to "offsetPosition = referenceTransform.InverseTransformPoint(transformToOffset.position)" when the referenceTransform scale is Vector3.one, as well as the ones of its parents
        /// </summary>
        public static Vector3 UnscaledOffset(Vector3 referenceTransformPosition, Quaternion referenceTransformRotation, Vector3 positionToOffset)
        {
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransformPosition, referenceTransformRotation, Vector3.one);
            var offset = referenceTransformMatrix.inverse.MultiplyPoint(positionToOffset);

            return offset;
        }

        /// <summary>
        /// Return an offseted position/rotation relatively to a referenceTransform
        /// </summary>
        public static (Vector3 position, Quaternion rotation) ApplyUnscaledOffset(Transform referenceTransform, Vector3 offset, Quaternion rotationOffset)
        {
            return ApplyUnscaledOffset(referenceTransform.position, referenceTransform.rotation, offset, rotationOffset);
        }

        /// <summary>
        /// Return an offseted position/rotation relatively to a virtual referenceTrasnform, with referenceTransform.position=referenceTransformPosition, with referenceTransform.rotation=referenceTransformRotation, referenceTransform.scale=Vector3.one (as well as its parents' scales)
        /// </summary>
        public static (Vector3 position, Quaternion rotation) ApplyUnscaledOffset(Vector3 referenceTransformPosition, Quaternion referenceTransformRotation, Vector3 offset, Quaternion rotationOffset)
        {
            var rotation = referenceTransformRotation * rotationOffset;
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransformPosition, referenceTransformRotation, Vector3.one);
            var position = referenceTransformMatrix.MultiplyPoint(offset);
            return (position, rotation);
        }

        /// <summary>
        /// Return an offseted positionrelatively to a virtual referenceTransform, with referenceTransform.position=referenceTransformPosition, with referenceTransform.rotation=referenceTransformRotation, referenceTransform.scale=Vector3.one (as well as its parents' scales)
        /// </summary>
        public static Vector3 ApplyUnscaledOffset(Vector3 referenceTransformPosition, Quaternion referenceTransformRotation, Vector3 offset)
        {
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransformPosition, referenceTransformRotation, Vector3.one);
            var position = referenceTransformMatrix.MultiplyPoint(offset);
            return position;
        }

        #endregion

        #region Mixed reality relocalization 
        // Returns the new rig position, so that the headset position relatively to the current point becomes the new headset position relatively to the target
        //  (in AR, the localAnchor reallife position will have the targetAnchor corrdinates after the teleport)
        static public (Vector3 newRigPosition, Quaternion newRigRotation) DetermineNewRigPositionToMovePositionToTargetPosition(
            Vector3 currentPointPosition, Quaternion currentPointRotation,
            Vector3 targetPosition, Quaternion targetRotation,
            Transform rigTransform, Transform headsetTransform,
            bool ignoreYAxisMove = true, bool keepUpDirection = true)
        {
            return DetermineNewRigPositionToMovePositionToTargetPosition(
                currentPointPosition, currentPointRotation,
                targetPosition, targetRotation,
                rigTransform.position, rigTransform.rotation,
                headsetTransform.position, headsetTransform.rotation,
                ignoreYAxisMove, keepUpDirection);
        }

        // Returns the new rig position, so that the headset position relatively to the current point becomes the new headset position relatively to the target
        //  (in AR, the localAnchor reallife position will have the targetAnchor corrdinates after the teleport)
        static public (Vector3 newRigPosition, Quaternion newRigRotation) DetermineNewRigPositionToMovePositionToTargetPosition(
            Vector3 currentPointPosition, Quaternion currentPointRotation,
            Vector3 targetPosition, Quaternion targetRotation,
            Vector3 rigTransformPosition, Quaternion rigTransformRotation,
            Vector3 headsetTransformPosition, Quaternion headsetTransformRotation,
            bool ignoreYAxisMove = true, bool keepUpDirection = true)
        {
            Matrix4x4 currentPointTransformMatrix = Matrix4x4.TRS(currentPointPosition, currentPointRotation, Vector3.one);
            Matrix4x4 targetTransformMatrix = Matrix4x4.TRS(targetPosition, targetRotation, Vector3.one);

            // Equivalent of localAnchorTransform.InverseTransformPoint(hardwareRig.headset.transform.position)
            Vector3 headsetPositionInCandidateReferential = currentPointTransformMatrix.inverse.MultiplyPoint3x4(headsetTransformPosition);
            var headsetRotationInCandidateReferential = Quaternion.Inverse(currentPointRotation) * headsetTransformRotation;

            var newHeadsetPosition = targetTransformMatrix.MultiplyPoint(headsetPositionInCandidateReferential);
            var newHeadsetRotation = targetRotation * headsetRotationInCandidateReferential;

            var rigTransformMatrix = Matrix4x4.TRS(rigTransformPosition, rigTransformRotation, Vector3.one);
            var headsetLocalPosition = rigTransformMatrix.inverse.MultiplyPoint(headsetTransformPosition);

            var headsetLocalRotation = Quaternion.Inverse(rigTransformRotation) * headsetTransformRotation;
            (var newRigPosition, var newRigRotation) = TransformManipulations.ReferentialPositionToRespectOffsetsOfPositionedObjectWithUnscaledOffsets(rigTransformPosition, rigTransformRotation, newHeadsetPosition, newHeadsetRotation, headsetLocalPosition, headsetLocalRotation);
            // We don't adapt on the vertical axis
            if (ignoreYAxisMove)
            {
                newRigPosition.y = rigTransformPosition.y;
            }
            if (keepUpDirection)
            {
                var forward = newRigRotation * Vector3.forward;
                var rigTransformUp = rigTransformRotation * Vector3.up;
                if (Vector3.Cross(forward, rigTransformUp).magnitude < 0.01f)
                {
                    // forward is colinear to up, look rotation won't work
                    Debug.LogError("[NewRigPositionToMoveAnchorToTarget] keepUpDirection: forward is colinear to up, look rotation won't work. Applying fix");
                    forward = Vector3.Cross(newRigRotation * Vector3.right, rigTransformUp);
                }

                // Make sure forward is perpendicular to the current rig up
                forward = Vector3.ProjectOnPlane(forward, rigTransformUp);

                newRigRotation = Quaternion.LookRotation(forward, rigTransformUp);
            }

            return (newRigPosition, newRigRotation);
        }
        #endregion
    }



}
