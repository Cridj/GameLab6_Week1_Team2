using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

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


    public void Init(StageSpawnData stageSpawnData)
    {
        curStageSpawnData = stageSpawnData;
        timeSinceLastSpawned = 0f;
        target = GameObject.FindGameObjectWithTag("Player").transform;
        InitialSpawn();
    }

    [ContextMenu("Initial Spawn")]
    void InitialSpawn()
    {
        Vector3 spawnPos = curStageSpawnData.playerInitialPos;
        for (int i = 0; i < curStageSpawnData.initialNeutralAmount; i++)
        {
            spawnPos += Random.insideUnitSphere * curStageSpawnData.initialSpawnRadius;
            spawnPos.y = 1;

            GameObject obj = PoolManager.Instance.Get(PoolType.Neutral);
            obj.transform.position = spawnPos;
            obj.transform.rotation = Quaternion.identity;
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

        GameObject obj = PoolManager.Instance.Get(GetSpawnType());
        obj.transform.position = spawnPos;
        obj.transform.rotation = Quaternion.identity;
    }

    PoolType GetSpawnType()
    {
        float cumulative = 0f;

        foreach (var spawnData in curStageSpawnData.spawnDataList)
        {
            cumulative += spawnData.weight;
            int randomVal = Random.Range(1, 100);

            if (randomVal <= cumulative)
                return spawnData.type;
        }

        return curStageSpawnData.spawnDataList[0].type;
    }
}
