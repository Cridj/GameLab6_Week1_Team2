using FishNet.Object;
using UnityEngine;

public class HopakSync : NetworkBehaviour
{
    public HopakAnimation remoteAnim, localAnim;
    public FollowerManager remoteFollower, localFollower;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
            return;
        ReleaseCallbacks();
        if (localAnim != null)
        {
            localAnim.rightAction += OnLocalAnimation;
            localAnim.leftAction += OnLocalAnimation;
        }
        if (localFollower != null)
            localFollower.onSpawn += OnLocalSpawn;
    }

    public override void OnStopClient()
    {
        ReleaseCallbacks();
        base.OnStopClient();
    }

    private void OnDestroy()
    {
        ReleaseCallbacks();
    }

    private void ReleaseCallbacks()
    {
        if (localAnim != null)
        {
            localAnim.rightAction -= OnLocalAnimation;
            localAnim.leftAction -= OnLocalAnimation;
        }
        if (localFollower != null)
            localFollower.onSpawn -= OnLocalSpawn;
    }

    private void OnLocalAnimation(float speed, bool type)
    {
        if (IsClientInitialized && IsOwner)
            SyncAnimationAck(speed, type);
    }

    private void OnLocalSpawn(int id)
    {
        if (IsClientInitialized && IsOwner)
            SpawnNeutralReq(id);
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
        if (remoteAnim != null && remoteAnim.isActiveAndEnabled)
            remoteAnim.PlayAnimation(type, speed);
    }
}
