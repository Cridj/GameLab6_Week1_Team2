using FishNet;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameScene : MonoBehaviour
{
    public bool isServer = false;
    private NetworkManager networkManager;
    private bool returningToMain;
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
    private void Update()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToMain()
    {
        if (returningToMain)
            return;
        returningToMain = true;
        Managers.Instance.Fade.FadeOut(() => StartCoroutine(DisconnectAndReturn()));
    }

    private IEnumerator DisconnectAndReturn()
    {
        if (networkManager != null)
        {
            Transport transport = networkManager.TransportManager.Transport;
            if (transport.GetConnectionState(false) != LocalConnectionState.Stopped)
            {
                networkManager.ClientManager.StopConnection();
                while (transport != null && transport.GetConnectionState(false) != LocalConnectionState.Stopped)
                    yield return null;
            }
            if (networkManager != null && transport != null
                && transport.GetConnectionState(true) != LocalConnectionState.Stopped)
            {
                networkManager.ServerManager.StopConnection(true);
                while (transport != null && transport.GetConnectionState(true) != LocalConnectionState.Stopped)
                    yield return null;
            }
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        yield return SceneManager.LoadSceneAsync("MainLobby");
    }
}
