using Fusion;
using Fusion.Addons.AnchorsAddon.Colocalization;
using UnityEngine;

public class MoveRequesterSpawner : NetworkBehaviour, IRLRoomManager.IIRLRoomManagerListener
{
    IRLRoomManager roomManager;

    public Transform spawnCenter;
    public float minDistance = 0.1f;
    public float maxDistance = 0.5f;
    public NetworkObject requesterPrefab;

    private void Awake()
    {
        if (spawnCenter == null) spawnCenter = transform;
        roomManager = FindAnyObjectByType<IRLRoomManager>();
        roomManager.RegisterListener(this);
        foreach(var room in roomManager.knowRooms)
        {
            OnRoomCreate(room.roomId);
        }
    }

    #region (IRLRoomManager.IIRLRoomManagerListener)
    public void OnRoomCreate(string roomId)
    {
        if (roomManager.localNetworkIRLRoomMember?.RoomId.ToString() == roomId)
        {
            var offset = new Vector3(Random.Range(minDistance, maxDistance), 0, Random.Range(minDistance, maxDistance));
            var pos = spawnCenter.position + offset;
            var requesterObj = Runner.Spawn(requesterPrefab, pos, Quaternion.identity);
            var requester = requesterObj.GetComponentInChildren<NetworkIRLRoomMoveRequester>();
            requester.ChangeRoomId(roomId);
        }
    }

    public void OnRoomDelete(string roomId)
    {
    }
    #endregion
}
