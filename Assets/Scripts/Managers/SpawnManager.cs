using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    public StageSpawnData curStageSpawnData;

    private Transform target;

    private float timeSinceLastSpawned;

    private int totalWeight = 0;

    public int NeutralCnt { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Init(StageSpawnData stageSpawnData)
    {
        curStageSpawnData = stageSpawnData;
        timeSinceLastSpawned = 0f;

        totalWeight = 0;
        foreach (SpawnData spawnData in stageSpawnData.spawnDataList)
        {
            totalWeight += spawnData.weight;
        }

        NeutralCnt = 0;

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
        NeutralCnt += curStageSpawnData.initialNeutralAmount;
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

        PoolType type = GetSpawnType();
        if (type == PoolType.Neutral) NeutralCnt++;
        GameObject obj = PoolManager.Instance.Get(type);
        obj.transform.position = spawnPos;
        obj.transform.rotation = Quaternion.identity;


    }

    PoolType GetSpawnType()
    {
        foreach (var spawnData in curStageSpawnData.spawnDataList)
        {
            int randomWeight = Random.Range(0, totalWeight);
            if (randomWeight < spawnData.weight)
            {
                return spawnData.type;
            }

        }

        return curStageSpawnData.spawnDataList[0].type;
    }


}
