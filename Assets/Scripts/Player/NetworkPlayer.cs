using AYellowpaper.SerializedCollections;
using FishNet.Connection;
using FishNet.Object;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public GameObject local, remote;
    public FollowerManager remoteFollower;
    public PlayerController myController;

    private Queue<(List<Vector3> positions, List<Vector3> history)> pendingInits = new();

    [SerializeField] private float followerSnapshotInterval = 0.1f;
    private readonly List<Vector3> serverFollowerPositions = new();
    private readonly List<Vector3> serverPositionHistory = new();
    private const int FollowerSnapshotChunkSize = 128;
    private int serverSnapshotId;
    private float followerSnapshotTimer;
    private bool serverFollowerStateInitialized;
    [SerializeField] private float followerCatchUpMultiplier = 1.5f;
    [SerializeField] private float followerCatchUpAcceleration = 8f;
    private Vector3 previousServerPlayerPosition;
    private bool hasPreviousServerPlayerPosition;

    [SerializeField] private SerializedDictionary<int, GameObject[]> hatDict;
    [SerializeField] private SerializedDictionary<int, GameObject[]> faceDict;
    [SerializeField] private Renderer[] hopakRenderers;
    [SerializeField] private Renderer[] shoesRenderers;
    public CustomizeInfo customInfo;
    [SerializeField] private GameObject diePanel, UIPanel;
    [SerializeField] private GameObject mainCam;

    private sealed class SnapshotChunkBuffer
    {
        public readonly Vector3[][] chunks;
        public int receivedChunks;

        public SnapshotChunkBuffer(int chunkCount)
        {
            chunks = new Vector3[chunkCount][];
        }
    }


    public void ApplyCustomizeInfo(CustomizeInfo customInfo)
    {
        this.customInfo = customInfo;
        SetHat();
        SetFace();
        foreach (var renderer in hopakRenderers)
        {
            if (renderer != null)
                renderer.material.color = customInfo.bodyColor;

        }
        foreach (var renderer in shoesRenderers)
        {
            if (renderer != null)
                renderer.material.color = customInfo.shoesColor;
        }
    }
    

    private void SetHat()
    {
        foreach (var hat in hatDict.Values)
        {
            foreach (var obj in hat)
            {
                if (obj != null)
                {

                    obj.SetActive(false);
                }
            }

        }

        if (hatDict.TryGetValue(customInfo.hatType, out var go))
        {
            if (go != null)
            {
                foreach (var obj in go)
                {
                    if (obj != null)
                    {

                        obj.SetActive(true);
                    }
                }
            }
        }
    }

    private void SetFace()
    {
        foreach (var face in faceDict.Values)
        {
            foreach (var obj in face)
            {
                if (obj != null)
                {

                    obj.SetActive(false);
                }
            }

        }

        if (faceDict.TryGetValue(customInfo.faceType, out var go))
        {
            if (go != null)
            {
                foreach (var obj in go)
                {
                    if (obj != null)
                    {

                        obj.SetActive(true);
                    }
                }
            }
        }
    }



    private readonly Dictionary<int, SnapshotChunkBuffer> pendingSnapshots = new();

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            Destroy(remote);
            local.SetActive(true);
            JoinGameReq(NetworkObject.OwnerId, GameInstance.Instance.CustomizeInfo);
            GameManager.Instance.myPlayer = myController;
        }
        else
        {
            Destroy(local);
            remote.SetActive(true);
        }

        GameManager.Instance.AddPlayer(this, OwnerId);
        StartCoroutine(ProcessPendingWhenReady());
    }

    private void Update()
    {
        if (!IsServerInitialized)
            return;

        if (!serverFollowerStateInitialized || serverFollowerPositions.Count == 0)
            return;

        if (serverPositionHistory.Count == 0 ||
            Vector3.Distance(transform.position, serverPositionHistory[0]) >= GetHistoryRecordDistance())
        {
            serverPositionHistory.Insert(0, transform.position);
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        float playerSpeed = hasPreviousServerPlayerPosition
            ? Vector3.Distance(transform.position, previousServerPlayerPosition) / deltaTime
            : GetFollowerSpeed();
        previousServerPlayerPosition = transform.position;
        hasPreviousServerPlayerPosition = true;

        for (int i = 0; i < serverFollowerPositions.Count; i++)
        {
            int historyIndex = Mathf.Min(
                i == 0
                    ? Mathf.RoundToInt(GetFirstFollowerGap())
                    : Mathf.RoundToInt(GetFirstFollowerGap() + i * GetFollowerGap()),
                serverPositionHistory.Count - 1);
            Vector3 target = serverPositionHistory[historyIndex];
            target.y = GetFollowerYOffset();
            float distance = Vector3.Distance(serverFollowerPositions[i], target);
            float followSpeed = Mathf.Max(
                GetFollowerSpeed(),
                playerSpeed * followerCatchUpMultiplier);
            followSpeed += distance * followerCatchUpAcceleration;
            serverFollowerPositions[i] = Vector3.MoveTowards(
                serverFollowerPositions[i],
                target,
                followSpeed * deltaTime);
        }

        followerSnapshotTimer -= Time.deltaTime;
        if (followerSnapshotTimer <= 0f)
        {
            followerSnapshotTimer = followerSnapshotInterval;
            SendFollowerSnapshot(serverFollowerPositions);
        }
    }

    [ServerRpc]
    private void JoinGameReq(int clientId, CustomizeInfo customInfo)
    {
        GameManager.Instance.OnJoinPlayer(clientId, this, customInfo);
    }

    public void IntialFollower(List<Vector3> followerPositions, List<Vector3> positionHistory)
    {
        var posCopy = new List<Vector3>(followerPositions ?? new List<Vector3>());
        var histCopy = new List<Vector3>(positionHistory ?? new List<Vector3>());

        FollowerManager followerManager = FindFollowerManager();
        if (followerManager == null)
        {
            pendingInits.Enqueue((posCopy, histCopy));
            Debug.Log($"[NetworkPlayer] IntialFollower queued (pending size:{pendingInits.Count}) for player {gameObject.name}");
            return;
        }

        followerManager.ApplyServerPositions(posCopy);
        transform.localScale = Utils.CalculateScale(posCopy.Count);
    }

    private IEnumerator ProcessPendingWhenReady()
    {
        float timeout = 5f;
        float elapsed = 0f;
        float interval = 0.1f;

        while (elapsed < timeout && remoteFollower == null)
        {
            if (remote != null)
            {
                remoteFollower = remote.GetComponentInChildren<FollowerManager>();
                if (remoteFollower != null) break;
            }

            if (remoteFollower == null)
                remoteFollower = GetComponentInChildren<FollowerManager>();

            if (remoteFollower != null) break;

            elapsed += interval;
            yield return new WaitForSeconds(interval);
        }

        if (remoteFollower == null)
        {
            Debug.LogWarning($"[NetworkPlayer] remoteFollower not found for {gameObject.name} after waiting.");
            yield break;
        }

        while (pendingInits.Count > 0)
        {
            var item = pendingInits.Dequeue();
            FollowerManager followerManager = FindFollowerManager();
            if (followerManager == null)
                yield break;

            followerManager.ApplyServerPositions(item.positions);
            transform.localScale = Utils.CalculateScale(item.positions.Count);
            Debug.Log($"[NetworkPlayer] Applied queued InitFollower for {gameObject.name} (remaining:{pendingInits.Count})");
            yield return null;
        }
    }


    public void OnDie()
    {
        OnDieReq(NetworkObject.OwnerId);
        diePanel.SetActive(true);
        mainCam.transform.SetParent(transform);
        Destroy(local.gameObject);
    }


    [ServerRpc]
    private void OnDieReq(int clientId)
    {
        GameManager.Instance.OnDiePlayer(clientId);
    }


    public void IncreaseScale(int cnt)
    {
        Vector3 scale = Utils.CalculateScale(cnt);
        transform.localScale = scale;
    }

    public void ApplyServerFollowerSnapshot(List<Vector3> positions)
    {
        FollowerManager followerManager = FindFollowerManager();
        if (followerManager == null)
        {
            pendingInits.Enqueue((new List<Vector3>(positions ?? new List<Vector3>()), new List<Vector3>()));
            return;
        }

        followerManager.ApplyServerPositions(positions);
        transform.localScale = Utils.CalculateScale(positions == null ? 0 : positions.Count);
    }

    [Server]
    public void AddFollowerOnServer()
    {
        if (!serverFollowerStateInitialized)
        {
            serverFollowerPositions.Clear();

            serverPositionHistory.Clear();
            if (serverPositionHistory.Count == 0)
                serverPositionHistory.Add(transform.position);

            serverFollowerStateInitialized = true;
        }
        Vector3 spawnPosition = transform.position;
        if (serverFollowerPositions.Count > 0)
            spawnPosition = serverFollowerPositions[serverFollowerPositions.Count - 1] - transform.forward * 1.5f;

        serverFollowerPositions.Add(spawnPosition);
        SendFollowerSnapshot(serverFollowerPositions);
    }

    private void SendFollowerSnapshot(List<Vector3> positions)
    {
        positions ??= new List<Vector3>();

        int snapshotId = ++serverSnapshotId;
        int chunkCount = Mathf.Max(1, Mathf.CeilToInt(positions.Count / (float)FollowerSnapshotChunkSize));

        for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            int startIndex = chunkIndex * FollowerSnapshotChunkSize;
            int count = Mathf.Min(FollowerSnapshotChunkSize, positions.Count - startIndex);
            Vector3[] chunk = count > 0
                ? positions.GetRange(startIndex, count).ToArray()
                : new Vector3[0];

            SendFollowerSnapshotChunk(snapshotId, chunkIndex, chunkCount, chunk);
        }
    }

    [ObserversRpc(ExcludeServer = true)]
    private void SendFollowerSnapshotChunk(
        int snapshotId,
        int chunkIndex,
        int chunkCount,
        Vector3[] positions)
    {
        if (chunkCount <= 0 || chunkIndex < 0 || chunkIndex >= chunkCount)
            return;

        if (!pendingSnapshots.TryGetValue(snapshotId, out SnapshotChunkBuffer buffer))
        {
            buffer = new SnapshotChunkBuffer(chunkCount);
            pendingSnapshots.Add(snapshotId, buffer);
        }

        if (buffer.chunks[chunkIndex] != null)
            return;

        buffer.chunks[chunkIndex] = positions ?? new Vector3[0];
        buffer.receivedChunks++;

        if (buffer.receivedChunks != chunkCount)
            return;

        List<Vector3> completeSnapshot = new List<Vector3>();
        for (int i = 0; i < buffer.chunks.Length; i++)
        {
            if (buffer.chunks[i] == null)
                return;

            completeSnapshot.AddRange(buffer.chunks[i]);
        }

        pendingSnapshots.Remove(snapshotId);
        ApplyServerFollowerSnapshot(completeSnapshot);
    }

    private FollowerManager FindFollowerManager()
    {
        if (remoteFollower != null)
            return remoteFollower;

        FollowerManager[] managers = GetComponentsInChildren<FollowerManager>(true);
        if (managers.Length == 0)
            return null;

        remoteFollower = managers[0];
        return remoteFollower;
    }

    private float GetFollowerGap()
    {
        FollowerManager manager = FindFollowerManager();
        return manager == null
            ? Mathf.Max(1f, Utils.CalculateScale(serverFollowerPositions.Count).x * 3f)
            : manager.GetGapForFollowerCount(serverFollowerPositions.Count);
    }

    private float GetFirstFollowerGap()
    {
        FollowerManager manager = FindFollowerManager();
        return manager == null ? 1f : manager.GetFirstFollowerGap();
    }

    private float GetFollowerSpeed()
    {
        FollowerManager manager = FindFollowerManager();
        return manager == null ? 35f : manager.GetFollowerSpeed();
    }

    private float GetFollowerYOffset()
    {
        FollowerManager manager = FindFollowerManager();
        return manager == null ? 1f : manager.GetFollowerYOffset();
    }

    private float GetHistoryRecordDistance()
    {
        FollowerManager manager = FindFollowerManager();
        return manager == null ? 0.2f : Mathf.Max(0.01f, manager.GetHistoryRecordDistance());
    }
}