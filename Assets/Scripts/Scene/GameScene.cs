using AYellowpaper.SerializedCollections;
using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameScene : MonoBehaviour
{
    public SerializedDictionary<int, GameObject> introPanels = new SerializedDictionary<int, GameObject>();

    [SerializeField] private GameObject IngamePanel;
    [SerializeField] private GameObject DiePanel;
    [SerializeField] private RewardPanel rewardPanel;
    [SerializeField] private GameObject finishPanel;

    [SerializeField] private PlayerController pc;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private bool isStarted = false;
    [SerializeField] private bool isFinished = false;
    [SerializeField] private int gameTime = 60;
    private PlayerHealth health;




    [SerializeField] private TextMeshProUGUI timerText;


    private void Start()
    {
        Managers.Instance.Fade.FadeIn();
        if (introPanels.TryGetValue(GameInstance.Instance.curStageLevel, out GameObject panel))
        {
            panel.SetActive(true);
        }
        health = FindFirstObjectByType<PlayerHealth>();

        health.GameOver -= OnDie;
        health.GameOver += OnDie;

        StartCoroutine(OnStart());
    }

    private void OnDie()
    {
        isFinished = true;
        spawnManager.enabled = false;
        pc.enabled = false;
        IngamePanel.SetActive(false);
        DiePanel.SetActive(true);

        StartCoroutine(OnDieCoroutine());
    }

    private IEnumerator OnDieCoroutine()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Managers.Instance.Fade.FadeOut(() => 
                {
                    GameInstance.Instance.InitGame();
                    SceneManager.LoadSceneAsync("MainLobby");
                });
            }
            yield return null;
        }
    }

    IEnumerator OnStart()
    {
        yield return new WaitForSeconds(1.5f);

        while (true)
        {
            if (!isStarted && Input.GetKeyDown(KeyCode.Space))
            {
                isStarted = true;
                foreach (var panel in introPanels)
                {
                    panel.Value.SetActive(false);
                }
                IngamePanel.SetActive(true);
                pc.Init();
                var spawnData = GameInstance.Instance.GetCurrentSpawnData();
                if (spawnData != null)
                    spawnManager.Init(spawnData);
                else
                    Debug.LogWarning("스폰 데이터가 GameInstance에 할당 되어있지 않습니다.");
                health.Init(GameInstance.Instance.curHeart);
                StartCoroutine(StartGame());
                yield break;
            }
            yield return null;
        }
    }

    IEnumerator StartGame()
    {
        int timer = gameTime;
        while(true)
        {
            timer -= 1;

            if(timer <= 10)
            {
                timerText.text = timer.ToString("F0");
                timerText.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f);
            }
            else
            {
                timerText.text = timer.ToString("F0");

            }
            if (timer == 0)
            {
                timerText.text = "";
                break;
            }
            yield return new WaitForSeconds(1);
        }

        isFinished = true;
        spawnManager.enabled = false;
        pc.enabled = false;
        IngamePanel.SetActive(false);


        if(GameInstance.Instance.curStageLevel ==4) // 게임 클리어
        {
            health.GameEnd();
            finishPanel.gameObject.SetActive(true);

            yield return new WaitForSeconds(6f);
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    GameInstance.Instance.InitGame();
                    SceneManager.LoadSceneAsync("MainLobby");
                }
                yield return null;
            }
        }
        else
        {
            health.GameEnd();
            rewardPanel.gameObject.SetActive(true);

            yield return new WaitForSeconds(6f);
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    //TODO 상점으로 이동
                }
                yield return null;
            }
        }



    }

}
