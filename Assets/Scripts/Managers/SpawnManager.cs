using AYellowpaper.SerializedCollections;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

struct Spawndata
{
    public Spawndata(int id, Vector3 pos)
    {
        this.id = id;
        this.pos = pos;
    }
    public int id;
    public Vector3 pos;
}

public class SpawnManager : NetworkBehaviour
{
    Dictionary<int,  Spawndata> spawnData = new(); // 서버 데이터 저장용
    Dictionary<int, Neutral> spawnedNeutral = new();
    [SerializeField] private Collider plane;
    [SerializeField] private int intialSpawnCount = 1000;

    [SerializeField] private int idCounter = 0;
    [SerializeField] private Neutral neutral;

    public override void OnStartServer()
    {
        base.OnStartServer();

        SpawnIntialNeutral();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        SpawnNeutralReq();
    }

    private Vector3 GetRandomPosInCollider()
    {
        Vector3 originPosition = plane.transform.position;
        // 콜라이더의 사이즈를 가져오는 bound.size 사용
        float range_X = plane.bounds.size.x;
        float range_Z = plane.bounds.size.z;

        range_X = Random.Range((range_X / 2) * -1, range_X / 2);
        range_Z = Random.Range((range_Z / 2) * -1, range_Z / 2);
        Vector3 RandomPostion = new Vector3(range_X, 1f, range_Z);

        Vector3 respawnPosition = originPosition + RandomPostion;
        return respawnPosition;
    }


    [Server]
    private void SpawnIntialNeutral()
    {
        for (int i = 0; i < intialSpawnCount; i++)
        {
            int id = idCounter++;
            var pos = GetRandomPosInCollider();
            Spawndata data = new Spawndata(id, pos);
            spawnData.Add(id, data);
        }
        SpawnIntialNeutralAck(spawnData);
    }


    [ServerRpc(RequireOwnership = false)]
    private void SpawnNeutralReq()
    {
        SpawnNeutral();
    }


    [Server]
    private void SpawnNeutral()
    {
        SpawnIntialNeutralAck(spawnData);
    }


    [ObserversRpc]
    [Client]
    private void SpawnIntialNeutralAck(Dictionary<int, Spawndata> dict)
    {
        foreach(var data in dict)
        {
            if(!spawnedNeutral.ContainsKey(data.Key))
            {
                Neutral n = Instantiate(neutral);
                n.Id = data.Key;
                n.transform.position = data.Value.pos;
                spawnedNeutral.Add(data.Key, n);
            }
        }
    }
}
