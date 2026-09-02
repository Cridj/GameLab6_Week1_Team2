using UnityEngine;

public class PlaneInfo : MonoBehaviour
{
    public Vector2Int chunk;
    public Vector2Int index;

    BoxCollider col;
    public int spawnPitPerPlane = 100;
    public GameObject pitPrefab;

    private void Awake()
    {
        col = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        if(GameInstance.Instance.curStageLevel == 3)
        {
            SpawnRandomPitfall();
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
}
