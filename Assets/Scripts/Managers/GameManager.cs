using AYellowpaper.SerializedCollections;
using DG.Tweening;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
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
    [SerializeField] [Min(0f)] private float deathSpawnScatterRadius = 0.35f;

    [SerializeField] private float spawnInterval = 0.2f;

    [SerializeField] private Transform spawnRoot;
    [SerializeField] public FollowerManager myFollower;
    public PlayerController myPlayer;

    [SerializeField] private Dictionary<int, PlayerData> curruntOnlinePlayers = new();
    [SerializeField] private Dictionary<int, NetworkPlayer> networkPlayers = new(); // Server Only
    [SerializeField] private List<PlayerData> currentRanking = new();
    private RankingInfo[] pendingLeaderboard;
    private readonly Dictionary<int, string> connectedPlayerNames = new();
    private readonly Queue<(string target, string instigator, bool disconnected)> pendingPlayerLogs = new();
    private ServerManager subscribedServerManager;
    [SerializeField] [Range(1, 600)] private int disconnectTimeoutSeconds = 10;

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
        FlushPendingUI();
    }

    private void FlushPendingUI()
    {
        if (myPlayer == null || myPlayer.playerUI == null || !myPlayer.playerUI.isActiveAndEnabled)
            return;

        if (pendingLeaderboard != null)
        {
            myPlayer.playerUI.UpdateLeaderboard(pendingLeaderboard);
            pendingLeaderboard = null;
        }

        while (pendingPlayerLogs.Count > 0)
        {
            var entry = pendingPlayerLogs.Dequeue();
            if (entry.disconnected)
                myPlayer.playerUI.AddDisconnectedLog(entry.target);
            else
                myPlayer.playerUI.AddKillLog(entry.target, entry.instigator);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        SpawnIntialNeutral();
        StartCoroutine(SpawnObjectAtInterval());
        subscribedServerManager = ServerManager;
        subscribedServerManager.SetRemoteClientTimeout(RemoteTimeoutType.Development,
            (ushort)Mathf.Clamp(disconnectTimeoutSeconds, 1, 600));
        subscribedServerManager.OnRemoteConnectionState += OnClientDisconnected;
    }

    public override void OnStopServer()
    {
        ReleaseServerCallbacks();
        StopAllCoroutines();
        connectedPlayerNames.Clear();
        curruntOnlinePlayers.Clear();
        currentRanking.Clear();
        networkPlayers.Clear();
        spawnData.Clear();
        base.OnStopServer();
    }

    public override void OnStopClient()
    {
        pendingLeaderboard = null;
        pendingPlayerLogs.Clear();
        myPlayer = null;
        myFollower = null;
        if (!IsServerInitialized)
            networkPlayers.Clear();
        foreach (Neutral instance in spawnedNeutral.Values)
        {
            if (instance != null)
                Destroy(instance.gameObject);
        }
        spawnedNeutral.Clear();
        base.OnStopClient();
    }

    private void OnDestroy()
    {
        ReleaseServerCallbacks();
        if (Instance == this)
            Instance = null;
    }

    private void ReleaseServerCallbacks()
    {
        if (subscribedServerManager != null)
            subscribedServerManager.OnRemoteConnectionState -= OnClientDisconnected;
        subscribedServerManager = null;
    }


    public override void OnStartClient()
    {
        base.OnStartClient();
        SpawnNeutralReq();
        InitLeaderboardReq();

    }


    [Server]
    public void OnDiePlayer(int clientId, string instigator)
    {
        if (!curruntOnlinePlayers.TryGetValue(clientId, out var data))
            return;

        curruntOnlinePlayers.Remove(clientId);
        currentRanking.Remove(data);
        SpawnNeutralWhenPlayerDie(clientId);
        UpdateLeaderboardAck(CreateRankingInfo());
        BroadcastPlayerDie(data.name, instigator);
    }

    [Server]
    private void OnClientDisconnected(NetworkConnection connection, RemoteConnectionStateArgs args) // 게임 접속 중 클라가 강종했을때 호출
    {
        if (args.ConnectionState != RemoteConnectionState.Stopped)
            return;

        int clientId = connection.ClientId;
        bool wasConnected = connectedPlayerNames.TryGetValue(clientId, out string playerName);
        connectedPlayerNames.Remove(clientId);
        networkPlayers.Remove(clientId);

        if (curruntOnlinePlayers.TryGetValue(clientId, out var playerData))
        {
            curruntOnlinePlayers.Remove(clientId);
            currentRanking.Remove(playerData);
            UpdateLeaderboardAck(CreateRankingInfo());
        }

        if (wasConnected)
            BroadcastClientDisconnected(playerName);
    }

    [ObserversRpc]
    private void BroadcastClientDisconnected(string playerName)
    {
        pendingPlayerLogs.Enqueue((playerName, null, true));
        FlushPendingUI();
    }

    [Client]
    public void AddPlayer(NetworkPlayer player, int clientId) // 네트워크플레이어 리스트에 추가 - 클라 전용 별로 의미없는듯
    {
        networkPlayers[clientId] = player;
    }

    public void RemovePlayer(NetworkPlayer player, int clientId)
    {
        if (networkPlayers.TryGetValue(clientId, out var registered) && registered == player)
            networkPlayers.Remove(clientId);
        if (myPlayer != null && myPlayer == player.myController)
        {
            myPlayer = null;
            myFollower = null;
        }
    }

    [ObserversRpc]
    public void BroadcastPlayerDie(string target, string instigator)
    {
        pendingPlayerLogs.Enqueue((target, instigator, false));
        FlushPendingUI();
    }


    [Server]
    public void OnJoinPlayer(int clientId, NetworkPlayer player, CustomizeInfo customInfo)
    {
        if (curruntOnlinePlayers.TryGetValue(clientId, out PlayerData playerData))
        {
            playerData.name = customInfo.nickName;
            playerData.customInfo = customInfo;
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

        connectedPlayerNames[clientId] = customInfo.nickName;
        networkPlayers[clientId] = player;
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
        FlushPendingUI();
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

    [Server]
    private void SpawnNeutralWhenPlayerDie(int clientId)
    {
        if (!networkPlayers.TryGetValue(clientId, out var player) || player == null)
            return;

        int count = player.FollowerCount;
        if (count == 0)
            return;

        int visibleCount = Mathf.Min(count, player.FollowerSpawnLimit);
        float firstDistance = player.GetFirstFollowerDistance(count);
        float lastDistance = firstDistance + (visibleCount - 1) * player.GetFollowerSpacing(count);
        float scatterRadius = Mathf.Max(0f, deathSpawnScatterRadius);
        float spawnHeight = (plane != null ? plane.transform.position.y : player.transform.position.y) + 1f;
        FollowerPath path = player.Path;
        FollowerPath.Cursor cursor = path.Begin(player.transform.position, player.transform.forward);
        List<Spawndata> batch = new(Mathf.Min(count, 128));

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? i / (float)(count - 1) : 0f;
            float distance = Mathf.Lerp(firstDistance, lastDistance, t);
            Vector3 position = path.Behind(ref cursor, distance, out _);
            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            position.x += offset.x;
            position.z += offset.y;
            position.y = spawnHeight;

            int id = neutralIdCounter++;
            Spawndata data = new Spawndata(id, position);
            spawnData.Add(id, data);
            batch.Add(data);

            if (batch.Count == 128 || i == count - 1)
            {
                SpawnNeutralBatchAck(batch.ToArray());
                batch.Clear();
            }
        }
    }

    [ObserversRpc]
    private void SpawnNeutralBatchAck(Spawndata[] batch)
    {
        foreach (Spawndata data in batch)
            SpawnNeutralLocal(data.id, data.pos);
    }

    [TargetRpc]
    [Client]
    private void SpawnIntialNeutralAck(NetworkConnection connection, Dictionary<int, Spawndata> dict)
    {
        foreach (var data in dict)
        {
            SpawnNeutralLocal(data.Key, data.Value.pos);
        }
    }


    [ObserversRpc]
    private void SpawnNeutralAck(int id, Spawndata data)
    {
        SpawnNeutralLocal(id, data.pos);
    }

    private void SpawnNeutralLocal(int id, Vector3 position)
    {
        if (spawnedNeutral.ContainsKey(id))
            return;
        Neutral instance = Instantiate(neutral, position, neutral.transform.rotation, spawnRoot);
        instance.Id = id;
        spawnedNeutral.Add(id, instance);
    }

    [Server]
    public void OnStartSprint(int clientId)
    {
        BroadcastSprint(clientId, true);
    }

    [ObserversRpc]
    private void BroadcastSprint(int clientId, bool sprint)
    {
        if(networkPlayers.TryGetValue(clientId, out var player) && player != null)
        {
            player.OnSprint(sprint);
        }
    }

    [Server]
    public void OnStopSprint(int clientId)
    {
        BroadcastSprint(clientId, false);
    }
}
