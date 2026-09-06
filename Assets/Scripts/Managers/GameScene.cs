using FishNet;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

public class GameScene : MonoBehaviour
{
    public bool isServer = false;
    private NetworkManager networkManager;
    void Start()
    {
        if (networkManager == null)
        {
            networkManager = InstanceFinder.NetworkManager; // 자동 탐색 예시
        }
        if (isServer)
        {
            networkManager.ServerManager.StartConnection();

        }
        else
        {
            networkManager.ClientManager.StartConnection();
        }
    }
}
