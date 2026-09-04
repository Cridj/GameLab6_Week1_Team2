using FishNet.Object;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public GameObject local, remote;
    public override void OnStartClient()
    {
        base.OnStartClient();


        if (IsOwner)
        {
            Destroy(remote);
            local.SetActive(true);
        }
        else
        {
            Destroy(local);
            remote.SetActive(true);
        }
    }
}
