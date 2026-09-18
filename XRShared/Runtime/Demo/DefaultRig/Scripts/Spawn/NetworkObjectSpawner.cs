using Fusion;
using UnityEngine;

public class NetworkObjectSpawner : NetworkBehaviour
{
    [SerializeField] NetworkObject prefab;
    [SerializeField] float spawnDistance = 0.2f;
    public float cooldown = 0;

    [Header("Set automatically")]
    public Transform spawnPosition;

    [Networked]
    public NetworkObject CurrentInstance { get; set; }

    
    float lastSpawn = -1;


    private void Awake()
    {
        if (spawnPosition == null)
        {
            spawnPosition = transform;
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (Object != null && Object.HasStateAuthority && (CurrentInstance == null || Vector3.Distance(spawnPosition.position, CurrentInstance.transform.position) > spawnDistance))
        {
            Spawn();
        }
    }

    private void Spawn()
    {
        if (prefab == null) return;

        if (cooldown != 0 && lastSpawn != -1 && (Time.time - lastSpawn) < cooldown)
        {
            return;
        }

        lastSpawn = Time.time;
        CurrentInstance = Runner.Spawn(prefab, spawnPosition.position, spawnPosition.rotation);
    }
}






