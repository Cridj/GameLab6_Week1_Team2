using UnityEngine;
using UnityEngine.SceneManagement;

public class MainLobby : MonoBehaviour
{
    public void StartGame()
    {
        Managers.Instance.Fade.FadeOut(() =>
        {
            Managers.Instance.Sound.StopBgm();
            SceneManager.LoadSceneAsync("GameScene");
        });
    }

    private void Start()
    {
        Managers.Instance.Sound.PlayBgm("로비소리_느린버전");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
