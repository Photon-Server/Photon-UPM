using Fusion;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Rig;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Fusion.Addons.AnchorsAddon.ARFoundation
{
    public class ARFSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct PlaneSpawnDescription
        {
            public NetworkObject networkPrefab;
            public PlaneClassifications classifications;
            public bool shouldOverrideRotationOffset;
            public Vector3 rotationEulerOffset;
        }

        public List<PlaneSpawnDescription> planeSpawnDescriptions = new List<PlaneSpawnDescription>();
        public Vector3 rotationEulerOffset = new Vector3(-90, 180, 0);
        bool isSpawned = false;

        ARPlane arPlane;
        public static Dictionary<TrackableId, NetworkObject> spawnedObjects = new Dictionary<TrackableId, NetworkObject>();

        private void Awake()
        {
            arPlane = GetComponent<ARPlane>();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Debug.LogError($"[DebugARFSpawner] Start {name} {arPlane.classifications} {arPlane.trackableId}");
            TrySpawnNetworkObjects();
        }

        private void Update()
        {
            TrySpawnNetworkObjects();
        }

        async void TrySpawnNetworkObjects()
        {
            if (isSpawned)
                return;

            var runner = NetworkRunner.GetRunnerForGameObject(gameObject);
            if (runner == null || runner.IsConnectedToServer == false || runner.IsRunning == false)
                return;

            isSpawned = true;

            while (HardwareRigsRegistry.GetHardwareRig() == null || HardwareRigsRegistry.GetHardwareRig().LocalUserNetworkRig == null)
            {
                await Task.Delay(100);
            }

            foreach (var description in planeSpawnDescriptions)
            {
                if ((arPlane.classifications & description.classifications) != 0 && description.networkPrefab != null)
                {
                    var rotationEulerOffset = description.shouldOverrideRotationOffset ? description.rotationEulerOffset : this.rotationEulerOffset;

                    var trackableId = arPlane.trackableId;
                    var newScale = new Vector3(arPlane.extents.x * 2, arPlane.extents.y * 2, 0.05f); ;
                    var newPos = transform.position;
                    var newRot = transform.rotation * Quaternion.Euler(rotationEulerOffset);

                    if (spawnedObjects.ContainsKey(trackableId))
                    {
                        // Existing object

                        // We wait in case the object is just being spawned
                        while (spawnedObjects[trackableId] == null)
                        {
                            //Debug.LogError($"Waiting for {trackableId} spawn to finish...");
                            await Task.Delay(100);
                        }
                        var spawnedObject = spawnedObjects[trackableId];
                        // Update position and scale
                        // Debug.LogError($"Updating {trackableId} ({Vector3.Distance(spawnedObject.transform.position, newPos)}/{Quaternion.Angle(spawnedObject.transform.rotation, newRot)}/{Mathf.Abs(spawnedObject.transform.localScale.magnitude - newScale.magnitude)})...");
                        spawnedObject.transform.localScale = newScale;
                        spawnedObject.transform.rotation = newRot;
                        spawnedObject.transform.position = newPos;
                    }
                    else
                    {
                        // New object
                        // We first "book" the entry in the table (to avoid having 2 creations if the spawn last a bit)
                        spawnedObjects[trackableId] = null;
                        runner.Spawn(description.networkPrefab, newPos, newRot, onBeforeSpawned: (r, spawnedObject) =>
                        {
                            spawnedObject.transform.localScale = newScale;
                            spawnedObjects[trackableId] = spawnedObject;
                        });
                    }
                }
            }
        }
    }
}

