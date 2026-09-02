using AYellowpaper.SerializedCollections;
using System.Collections;
using UnityEngine;

public class GameScene : MonoBehaviour
{
    public SerializedDictionary<int, GameObject> introPanels = new SerializedDictionary<int, GameObject>();

    [SerializeField] private GameObject IngamePanel;
    [SerializeField] private PlayerController pc;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private bool isStarted = false;
    [SerializeField] private bool isFinished = false;
    [SerializeField] private float remainTime = 60f;


    private void Start()
    {
        Managers.Instance.Fade.FadeIn();
        if (introPanels.TryGetValue(GameInstance.Instance.curStageLevel, out GameObject panel))
        {
            panel.SetActive(true);
        }
        StartCoroutine(OnStart());
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

                StartCoroutine(StartGame());
                yield break;
            }
            yield return null;
        }
    }

    IEnumerator StartGame()
    {
        yield return new WaitForSeconds(remainTime);
        isFinished = true;
        spawnManager.enabled = false;
        pc.enabled = false;
        
    }
}
