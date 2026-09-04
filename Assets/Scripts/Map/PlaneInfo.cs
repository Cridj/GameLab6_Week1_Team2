using System.Collections;
using UnityEngine;

public class PlaneInfo : MonoBehaviour
{
    public Vector2Int chunk;
    public Vector2Int index;

    BoxCollider col;
    public int spawnPitPerPlane = 100;
    public int spawnObstaclePerPlane = 10;

    public GameObject pitPrefab;
    public GameObject dopPitPrefab;

    public GameObject obstaclePrefab;

    public float dropDelay = 0.5f;

    private void Awake()
    {
        col = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        if (GameInstance.Instance.curStageLevel == 2)
        {
            SpawnRandomObstacle();
        }
        if (GameInstance.Instance.curStageLevel == 3)
        {
            SpawnRandomObstacle();
            SpawnRandomPitfall();
        }
        if (GameInstance.Instance.curStageLevel == 4)
        {
            SpawnRandomObstacle();
            SpawnRandomPitfall();
            StartCoroutine(DropPit());
        }
    }

    Vector3 GetRandomPosInCollider()
    {
        Vector3 originPosition = col.transform.position;
        // 콜라이더의 사이즈를 가져오는 bound.size 사용
        float range_X = col.bounds.size.x;
        float range_Z = col.bounds.size.z;

        range_X = Random.Range((range_X / 2) * -1, range_X / 2);
        range_Z = Random.Range((range_Z / 2) * -1, range_Z / 2);
        Vector3 RandomPostion = new Vector3(range_X, 0f, range_Z);

        Vector3 respawnPosition = originPosition + RandomPostion;
        return respawnPosition;
    }

    [ContextMenu("Spawn Pit Test")]
    public void SpawnRandomPitfall()
    {
        for(int i = 0; i < spawnPitPerPlane; i++)
        {
            var pos = GetRandomPosInCollider();

            Instantiate(pitPrefab, pos, Quaternion.identity);
        }
    }

    [ContextMenu("Spawn Obstacle Test")]
    public void SpawnRandomObstacle()
    {
        for (int i = 0; i < spawnObstaclePerPlane; i++)
        {
            var pos = GetRandomPosInCollider();

            Instantiate(obstaclePrefab, pos, Quaternion.identity);
        }
    }

    IEnumerator DropPit()
    {
        while(true)
        {
            var pos = GetRandomPosInCollider();
            GameObject go = Instantiate(dopPitPrefab, new Vector3(pos.x, 100f, pos.z), Quaternion.identity);
            yield return new WaitForSeconds(dropDelay);
            Destroy(go, 10f);
        }
    }
}
