using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Unit")]
    [SerializeField] GameObject neutralPrefab;
    [SerializeField] GameObject medicPrefab;
    [SerializeField] GameObject policePrefab;

    public StageSpawnData curStageSpawnData;

    private Transform target;

    private float timeSinceLastSpawned;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Init(curStageSpawnData);
        target = GameObject.FindGameObjectWithTag("Player").transform;
        InitialSpawn();
    }

    void Init(StageSpawnData stageSpawnData)
    {
        curStageSpawnData = stageSpawnData;
        timeSinceLastSpawned = 0f;
    }

    [ContextMenu("Initial Spawn")]
    void InitialSpawn()
    {
        Vector3 spawnPos = curStageSpawnData.playerInitialPos;
        for (int i = 0; i < curStageSpawnData.initialNeutralAmount; i++)
        {
            spawnPos += Random.insideUnitSphere * curStageSpawnData.initialSpawnRadius;
            spawnPos.y = 1;

            Instantiate(curStageSpawnData.spawnDataList[0].prefab, spawnPos, Quaternion.identity);
        }
    }

    void Update()
    {
        timeSinceLastSpawned += Time.deltaTime;
        if (timeSinceLastSpawned >= curStageSpawnData.spawnInterval)
        {
            Spawn();
            timeSinceLastSpawned = 0f;
        }

    }

    void Spawn()
    {
        if (target == null) return;
        Vector3 spawnPos = target.position;

        spawnPos += Random.insideUnitSphere * curStageSpawnData.nearPlayerSpawnRadius;
        spawnPos.y = 1;

        Instantiate(GetSpawnPrefab(), spawnPos, Quaternion.identity);

    }

    GameObject GetSpawnPrefab()
    {
        float cumulative = 0f;

        foreach (var spawnData in curStageSpawnData.spawnDataList)
        {
            cumulative += spawnData.weight;
            int randomVal = Random.Range(1, 100);

            if (randomVal <= cumulative)
                return spawnData.prefab;

        }

        return curStageSpawnData.spawnDataList[0].prefab;
    }


}
