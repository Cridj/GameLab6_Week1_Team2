using UnityEngine;
using UnityEngine.SceneManagement;

public class MainLobby : MonoBehaviour
{
    public void StartGame()
    {
        Managers.Instance.Fade.FadeOut(() =>
        {
            SceneManager.LoadSceneAsync("PlayerTest");
        });
    }
}
