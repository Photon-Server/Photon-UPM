using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace Fusion.XRShared.Demo
{
    public class PrefabSpawner : NetworkBehaviour
    {
        public NetworkObject prefab;
        public NetworkObject currentInstance;
        public Transform spawnPosition;

        public float liberationDistance = .5f;
        public float cooldown = 0;
        float lastSpawn = -1;

        private void Awake()
        {
            if(spawnPosition == null)
            {
                spawnPosition = transform;
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (Object.HasStateAuthority && (currentInstance == null || Vector3.Distance(spawnPosition.position, currentInstance.transform.position) > liberationDistance))
            {
                Spawn();
            }
        }

        void Spawn()
        {
            if (cooldown != 0 && lastSpawn != -1 && (Time.time - lastSpawn) < cooldown)
            {
                return;
            }
            if (prefab == null) return;
            lastSpawn = Time.time;
            currentInstance = Runner.Spawn(prefab, spawnPosition.position, spawnPosition.rotation);
        }
    }

}
