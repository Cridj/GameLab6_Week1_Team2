using FishNet.Object;
using UnityEngine;

public class HopakSync : NetworkBehaviour
{
    public HopakAnimation remoteAnim, localAnim;
    public FollowerManager remoteFollower, localFollower;

    public override void OnStartClient()
    {
        localAnim.rightAction += (speed, type) =>
        {
            SyncAnimationAck(speed, type);
        };
        localAnim.leftAction += (speed, type) =>
        {
            SyncAnimationAck(speed, type);
        };

        localFollower.onSpawn += (id) =>
        {
            SpawnNeutralReq(id);
        };

        SyncOtherFollowersReq();
    }

    [ServerRpc]
    private void SyncOtherFollowersReq()
    {
    }


    [ServerRpc]
    private void SpawnNeutralReq(int id)
    {
        SyncNeutral(id);
    }

    [Server]
    private void SyncNeutral(int id)
    {
        GameManager.Instance.DespawnNeutral(id);
        NetworkPlayer networkPlayer = GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
            networkPlayer.AddFollowerOnServer();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void SyncSpawnNeutralAck(int id)
    {
    }


    [ServerRpc]
    private void SyncAnimationAck(float speed, bool type)
    {
        SyncAnimationReq(speed, type);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void SyncAnimationReq(float speed, bool type)
    {
        remoteAnim.PlayAnimation(type, speed);
    }
}
