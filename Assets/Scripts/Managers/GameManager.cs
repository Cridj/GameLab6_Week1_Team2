using AYellowpaper.SerializedCollections;
using DG.Tweening;
using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct Spawndata
{
    public Spawndata(int id, Vector3 pos)
    {
        this.id = id;
        this.pos = pos;
    }
    public int id;
    public Vector3 pos;
}

public class PlayerData
{
    public int clientId;
    public string name;
    public int followerCount;
    public CustomizeInfo customInfo;
}

public struct RankingInfo
{
    public string name;
    public string score;
}
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    Dictionary<int, Spawndata> spawnData = new(); // 서버 데이터 저장용
    Dictionary<int, Neutral> spawnedNeutral = new();
    [SerializeField] private Collider plane;
    [SerializeField] private int intialSpawnCount = 2000;

    [SerializeField] private int neutralIdCounter = 0;
    [SerializeField] private Neutral neutral;

    [SerializeField] private int neutralMaxCount = 4000;

    [SerializeField] private float spawnInterval = 0.2f;

    [SerializeField] private Transform spawnRoot;
    [SerializeField] public FollowerManager myFollower;
    public PlayerController myPlayer;

    [SerializeField] private Dictionary<int, PlayerData> curruntOnlinePlayers = new();
    [SerializeField] private Dictionary<int, NetworkPlayer> networkPlayers = new(); // Server Only
    [SerializeField] private List<PlayerData> currentRanking = new();
    private RankingInfo[] pendingLeaderboard;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {

    }

    private void Update()
    {
        if (pendingLeaderboard != null && myPlayer != null && myPlayer.playerUI != null)
        {
            myPlayer.playerUI.UpdateLeaderboard(pendingLeaderboard);
            pendingLeaderboard = null;
        }
    }


    public override void OnStartServer()
    {
        base.OnStartServer();

        SpawnIntialNeutral();
        StartCoroutine(SpawnObjectAtInterval());
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        SpawnNeutralReq();
        InitLeaderboardReq();

    }


    [Server]
    public void OnDiePlayer(int clientId)
    {
        if(curruntOnlinePlayers.TryGetValue(clientId, out var data))
        {
            if (networkPlayers.TryGetValue(data.clientId, out var player))
            {
                //Despawn(player, DespawnType.Destroy); // 굳이 서버에서 디스폰 해야하나?
                //게임 나갈때 디스폰하면 될듯

                if (curruntOnlinePlayers.TryGetValue(clientId, out var playerData))
                {
                    curruntOnlinePlayers.Remove(clientId);
                    if (currentRanking.Contains(playerData))
                        currentRanking.Remove(playerData);
                    UpdateLeaderboardAck(CreateRankingInfo());
                }
                BroadcastPlayerDie(data);
            }
        }
    }

    [ServerRpc]
    public void OnLeftPlayer(int clientId)
    {
        if (curruntOnlinePlayers.TryGetValue(clientId, out var playerData))
        {
            curruntOnlinePlayers.Remove(clientId);
            if (currentRanking.Contains(playerData))
                currentRanking.Remove(playerData);
            UpdateLeaderboardAck(CreateRankingInfo());
        }
    }

    [Client]
    public void AddPlayer(NetworkPlayer player, int clientId) // 네트워크플레이어 리스트에 추가 - 클라 전용 별로 의미없는듯
    {
        networkPlayers.Add(clientId, player);
    }


    [ObserversRpc(ExcludeOwner = true)]
    public void BroadcastPlayerDie(PlayerData data) // 해당 플레이어 삭제 <<- 로컬에서 발생해서 서버엔 영향 X
    {
        if(NetworkObject.OwnerId != data.clientId)
        {
            if (networkPlayers.TryGetValue(data.clientId, out var player))
            {
                if (player.OwnerId == LocalConnection.ClientId)
                    return;
                var followerManager = player.GetComponentInChildren<FollowerManager>();
                if(followerManager != null)
                {
                    foreach(var fol in followerManager.followers)
                    {
                        Destroy(fol.gameObject);
                    }
                }
                Destroy(player.gameObject);
                networkPlayers.Remove(data.clientId);
            }
        }
    }


    [Server]
    public void OnJoinPlayer(int clientId, NetworkPlayer player, CustomizeInfo customInfo)
    {
        if (curruntOnlinePlayers.TryGetValue(clientId, out PlayerData playerData))
        {
            playerData.name = customInfo.nickName;
        }
        else
        {
            playerData = new PlayerData
            {
                clientId = clientId,
                name = customInfo.nickName,
                followerCount = 0,
                customInfo = customInfo
            };
            curruntOnlinePlayers.Add(clientId, playerData);
        }

        networkPlayers[clientId] = player;
        foreach (NetworkPlayer existingPlayer in networkPlayers.Values)
            existingPlayer.SendInitialFollowerState(player.Owner);

        UpdateLeaderboardAck(CreateRankingInfo());
        ApplyCustomizeInfoAck(clientId, customInfo);
        ApplyAnotherCustomizeAck(networkPlayers[clientId].Owner, curruntOnlinePlayers);
    }

    [TargetRpc]
    private void ApplyAnotherCustomizeAck(NetworkConnection connection, Dictionary<int, PlayerData> data)
    {
        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);

        foreach(var player in players)
        {
            if(data.TryGetValue(player.OwnerId, out var playerData))
            {
                player.ApplyCustomizeInfo(playerData.customInfo);
            }
        }
    }

    [ObserversRpc]
    private void ApplyCustomizeInfoAck(int clientId, CustomizeInfo customInfo)
    {
        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach(var p in players)
        {
            if(p.NetworkObject.OwnerId == clientId)
            {
                p.ApplyCustomizeInfo(customInfo);
            }
        }
    }

    public void IncreaseFollower()
    {
        int clientId = NetworkObject.LocalConnection != null
            ? NetworkObject.LocalConnection.ClientId
            : NetworkObject.OwnerId;
        IncreaseFollowerReq(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseFollowerReq(int clientId)
    {
        if (curruntOnlinePlayers.ContainsKey(clientId))
        {
            curruntOnlinePlayers[clientId].followerCount++;

            UpdateLeaderboardAck(CreateRankingInfo());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InitLeaderboardReq(NetworkConnection caller = null)
    {
        InitLeaderboardTargetRpc(caller, CreateRankingInfo());
    }

    [TargetRpc]
    private void InitLeaderboardTargetRpc(NetworkConnection connection, RankingInfo[] info)
    {
        pendingLeaderboard = info;
    }


    [ObserversRpc]
    private void UpdateLeaderboardAck(RankingInfo[] info)
    {
        pendingLeaderboard = info;
        if (myPlayer != null && myPlayer.playerUI != null)
        {
            myPlayer.playerUI.UpdateLeaderboard(info);
            pendingLeaderboard = null;
        }
    }

    private RankingInfo[] CreateRankingInfo()
    {
        RankingInfo[] info = new RankingInfo[10];
        IEnumerable<PlayerData> sortedPlayers = curruntOnlinePlayers.Values
            .OrderByDescending(data => data.followerCount)
            .ThenBy(data => data.name);

        int index = 0;
        foreach (PlayerData data in sortedPlayers)
        {
            if (index >= info.Length)
                break;

            info[index].name = data.name;
            info[index].score = data.followerCount.ToString();
            index++;
        }

        return info;
    }


    [Server]
    private IEnumerator SpawnObjectAtInterval()
    {
        while (true)
        {
            yield return Utils.GetWait(0.5f);

            if (spawnData.Count < neutralMaxCount)
            {
                int id = neutralIdCounter++;
                var pos = GetRandomPosInCollider();
                Spawndata data = new Spawndata(id, pos);
                spawnData.Add(id, data);
                SpawnNeutralAck(id, data);
            }
        }
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
            int id = neutralIdCounter++;
            var pos = GetRandomPosInCollider();
            Spawndata data = new Spawndata(id, pos);
            spawnData.Add(id, data);
        }
        // 접속한 클라이언트가 SpawnNeutralReq를 보낼 때 그 클라이언트에게만 전달함
    }


    [ServerRpc(RequireOwnership = false)]
    private void SpawnNeutralReq(NetworkConnection caller = null)
    {
        SpawnNeutral(caller);
    }


    [Server]
    private void SpawnNeutral(NetworkConnection connection)
    {
        SpawnIntialNeutralAck(connection, spawnData);
    }

    [Server]
    public void DespawnNeutral(int id)
    {
        spawnData.Remove(id);
        DespawnNeutralAck(id);
    }


    [ObserversRpc]
    private void DespawnNeutralAck(int id)
    {
        if (spawnedNeutral.TryGetValue(id, out Neutral neutral))
        {
            if (neutral != null)
                Destroy(neutral.gameObject);
            spawnedNeutral.Remove(id);
        }
    }


    [TargetRpc]
    [Client]
    private void SpawnIntialNeutralAck(NetworkConnection connection, Dictionary<int, Spawndata> dict)
    {
        foreach (var data in dict)
        {
            if (!spawnedNeutral.ContainsKey(data.Key))
            {
                Neutral n = Instantiate(neutral);
                n.transform.SetParent(spawnRoot);
                n.Id = data.Key;
                n.transform.position = data.Value.pos;
                spawnedNeutral.Add(data.Key, n);
            }
        }
    }


    [ObserversRpc]
    private void SpawnNeutralAck(int id, Spawndata data)
    {
        if (!spawnedNeutral.ContainsKey(id))
        {
            Neutral n = Instantiate(neutral);
            n.transform.SetParent(spawnRoot);
            n.Id = id;
            n.transform.position = data.pos;
            spawnedNeutral.Add(id, n);
        }
    }
}
