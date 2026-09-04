using FishNet.Object;
using UnityEngine;

public class HopakSync : NetworkBehaviour
{
    public HopakAnimation remote, local;


    public override void OnStartClient()
    {
        local.rightAction += (speed, type) =>
        {
            SyncAnimationAck(speed, type);
        };
        local.leftAction += (speed, type) =>
        {
            SyncAnimationAck(speed, type);
        };
    }


    [ServerRpc]
    private void SyncAnimationAck(float speed, bool type)
    {
        SyncAnimationReq(speed, type);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void SyncAnimationReq(float speed, bool type)
    {
        remote.PlayAnimation(type, speed);
    }
}
