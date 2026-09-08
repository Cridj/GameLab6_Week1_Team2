using AYellowpaper.SerializedCollections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class NetworkPlayer : NetworkBehaviour
{
    public GameObject local, remote;
    public FollowerManager remoteFollower;
    public PlayerController myController;

    private readonly SyncVar<int> followerCount = new();
    private readonly SyncVar<bool> deathState = new();
    private readonly FollowerPath serverPath = new();
    private readonly FollowerPath clientPath = new();
    private readonly List<Vector3> pendingPathPoints = new(16);
    [SerializeField] private float serverPathPointDistance = 0.1f;
    [SerializeField] private Renderer minimapIcon;
    [SerializeField] private float serverPathSendInterval = 0.1f;
    [Header("Follower spacing (world units)")]
    [Min(0.05f)] [SerializeField] private float firstFollowerBodyDistance = 0.75f;
    [Min(0.05f)] [SerializeField] private float followerBodyDistance = 0.5f;
    [Min(0f)] [SerializeField] private float followerClearance = 0.05f;
    private double serverPathSendTimer;
    private uint serverPathSequence;
    private uint clientPathSequence;
    private bool clientPathInitialized;
    private bool pathSnapshotRequested;
    private bool isDead;
    private int maximumSpawnedFollowers = 50;

    public FollowerPath Path => IsServerInitialized ? serverPath : clientPath;
    public int FollowerCount => Mathf.Max(0, followerCount.Value);
    public int FollowerSpawnLimit => Mathf.Max(1, maximumSpawnedFollowers);

    public float GetFollowerSpacing(int count)
    {
        return Mathf.Max(0.05f, followerBodyDistance) * Utils.CalculateScale(count).x
            + Mathf.Max(0f, followerClearance);
    }

    public float GetFirstFollowerDistance(int count)
    {
        return Mathf.Max(0.05f, firstFollowerBodyDistance) * Utils.CalculateScale(count).x
            + Mathf.Max(0f, followerClearance);
    }

    [SerializeField] private SerializedDictionary<int, GameObject[]> hatDict;
    [SerializeField] private SerializedDictionary<int, GameObject[]> faceDict;
    [SerializeField] private Renderer[] hopakRenderers;
    [SerializeField] private Renderer[] shoesRenderers;
    public CustomizeInfo customInfo;
    [SerializeField] private GameObject diePanel, UIPanel;
    [SerializeField] private GameObject mainCam;
    [SerializeField] private GameObject[] trails;

    private void Awake()
    {
        followerCount.OnChange += OnFollowerCountChanged;
        deathState.OnChange += OnDeathStateChanged;
        desireScale = transform.localScale;
    }

    public void OnSprint(bool sprint)
    {
        if (isDead)
            return;
        foreach(var trail in trails)
        {
            if (trail != null)
                trail.SetActive(sprint);
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
            {
                if (IsOwner)
                    renderer.material.color = Color.green;
                else
                    renderer.material.color = Color.red;
            }
        }
        if(minimapIcon != null)
        {
            if (IsOwner)
                minimapIcon.material.color = Color.red;
            else
                minimapIcon.material.color = Color.black;
        }
        FollowerManager followerManager = FindFollowerManager();
        if (followerManager != null)
        {
            followerManager.customInfo = customInfo;
            StartCoroutine(followerManager.ApplyCustomize());
        }
    }
    

    private void SetHat()
    {
        foreach (var hat in hatDict.Values)
            foreach (var obj in hat)
                if (obj != null)
                    obj.SetActive(false);

        if (hatDict.TryGetValue(customInfo.hatType, out var go))
            if (go != null)
                foreach (var obj in go)
                    if (obj != null)
                        obj.SetActive(true);
    }

    private void SetFace()
    {
        foreach (var face in faceDict.Values)
            foreach (var obj in face)
                if (obj != null)
                    obj.SetActive(false);
        if (faceDict.TryGetValue(customInfo.faceType, out var go))
            if (go != null)
                foreach (var obj in go)
                    if (obj != null)
                        obj.SetActive(true);
    }



    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            remote.SetActive(false);
            local.SetActive(true);
            remoteFollower = local.GetComponentInChildren<FollowerManager>(true);
            JoinGameReq(NetworkObject.OwnerId, GameInstance.Instance.CustomizeInfo);
            GameManager.Instance.myPlayer = myController;
        }
        else
        {
            local.SetActive(false);
            remote.SetActive(true);
            remoteFollower = remote.GetComponentInChildren<FollowerManager>(true);
        }

        GameManager.Instance.AddPlayer(this, OwnerId);
        if (deathState.Value)
            ApplyDeathState();
        else
            ApplyFollowerCount(followerCount.Value);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        FollowerManager settings = FindFollowerManager();
        maximumSpawnedFollowers = settings == null ? 50 : Mathf.Max(1, settings.maxFollowerPerLine);
        serverPath.Clear();
        pendingPathPoints.Clear();
        serverPathSequence = 0;
        serverPathSendTimer = 0d;
        isDead = false;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        serverPath.Append(transform.position - forward.normalized * GetRetainedPathDistance());
        serverPath.Append(transform.position);
        serverPathSequence = (uint)serverPath.Count;
        TimeManager.OnPostTick += RecordServerPath;
    }

    public override void OnStopServer()
    {
        TimeManager.OnPostTick -= RecordServerPath;
        base.OnStopServer();
    }

    public override void OnStopClient()
    {
        if (myController != null)
            myController.GameOver();
        StopFollowers();
        if (GameManager.Instance != null)
            GameManager.Instance.RemovePlayer(this, OwnerId);
        base.OnStopClient();
    }

    private void OnDestroy()
    {
        followerCount.OnChange -= OnFollowerCountChanged;
        deathState.OnChange -= OnDeathStateChanged;
    }
    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);
        SendInitialFollowerState(connection);
    }

    private void RecordServerPath()
    {
        if (isDead)
            return;

        serverPathSendTimer -= TimeManager.TickDelta;
        Vector3 position = transform.position;
        position.y = 0f;
        float distance = Vector3.Distance(position, serverPath.Newest);
        bool sendNow = serverPathSendTimer <= 0d;
        if (distance >= Mathf.Max(0.05f, serverPathPointDistance) || (sendNow && distance > 0.001f))
        {
            if (serverPath.Append(position))
            {
                serverPathSequence++;
                pendingPathPoints.Add(position);
                serverPath.Trim(GetRetainedPathDistance());
            }
        }

        if (sendNow || pendingPathPoints.Count >= 16)
        {
            serverPathSendTimer = Mathf.Max(0.05f, serverPathSendInterval);
            if (pendingPathPoints.Count > 0)
            {
                uint firstSequence = serverPathSequence - (uint)pendingPathPoints.Count + 1;
                AddPathPointsObserversRpc(firstSequence, pendingPathPoints.ToArray());
                pendingPathPoints.Clear();
            }
        }
    }

    [ServerRpc]
    private void JoinGameReq(int clientId, CustomizeInfo customInfo)
    {
        GameManager.Instance.OnJoinPlayer(clientId, this, customInfo);
    }

    private void OnFollowerCountChanged(int previous, int next, bool asServer)
    {
        if (asServer || !OnStartClientCalled)
            return;

        ApplyFollowerCount(next);
    }

    private void ApplyFollowerCount(int count)
    {
        if (IsOwner || isDead)
            return;

        FollowerManager followerManager = FindFollowerManager();
        if (followerManager != null)
            followerManager.SetFollowerCount(count);
    }


    public void OnDie(string ownerName)
    {
        if (!IsOwner || isDead)
            return;
        mainCam.transform.SetParent(transform);
        UIPanel.transform.SetParent(transform);
        diePanel.SetActive(true);
        myController.playerUI.UpdateDieText(ownerName);
        ApplyDeathState();
        OnDieReq(ownerName);
        StartCoroutine(OnDieReturnMain());
    }

    private void OnDeathStateChanged(bool previous, bool next, bool asServer)
    {
        if (!asServer && OnStartClientCalled && next)
            ApplyDeathState();
    }

    private void ApplyDeathState()
    {
        isDead = true;
        if (myController != null)
            myController.GameOver();
        StopFollowers();
        foreach (Collider collider in GetComponents<Collider>())
            collider.enabled = false;
        foreach (GameObject trail in trails)
        {
            if (trail != null)
                trail.SetActive(false);
        }
        if (local != null)
            local.SetActive(false);
        if (remote != null)
            remote.SetActive(false);
    }

    private void StopFollowers()
    {
        foreach (FollowerManager manager in GetComponentsInChildren<FollowerManager>(true))
            manager.StopFollowing();
    }

    private IEnumerator OnDieReturnMain()
    {
        while(true)
        {
            yield return null;
            if(Input.GetKeyDown(KeyCode.Space))
            {
                GameScene scene = FindFirstObjectByType<GameScene>();
                if (scene != null)
                    scene.ReturnToMain();
                yield break;
            }
        }
    }


    [ServerRpc]
    private void OnDieReq(string instigator)
    {
        if (deathState.Value)
            return;
        deathState.Value = true;
        ApplyDeathState();
        GameManager.Instance.OnDiePlayer(OwnerId, instigator);
    }

    private Vector3 desireScale = Vector3.one;

    public void IncreaseScale(int cnt)
    {
        desireScale = Utils.CalculateScale(Mathf.Max(0, cnt));
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, desireScale, 10f * Time.deltaTime);
    }

    [Server]
    public void AddFollowerOnServer()
    {
        if (!isDead)
            followerCount.Value++;
    }

    [ObserversRpc(ExcludeServer = true)]
    private void AddPathPointsObserversRpc(uint firstSequence, Vector3[] positions)
    {
        if (isDead || positions == null)
            return;
        if (!clientPathInitialized || firstSequence > clientPathSequence + 1)
        {
            if (!pathSnapshotRequested)
            {
                pathSnapshotRequested = true;
                RequestPathSnapshotServerRpc();
            }
            return;
        }

        for (int i = 0; i < positions.Length; i++)
        {
            uint sequence = firstSequence + (uint)i;
            if (sequence <= clientPathSequence)
                continue;
            clientPath.Append(positions[i]);
            clientPathSequence = sequence;
        }
        clientPath.Trim(GetRetainedPathDistance());
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPathSnapshotServerRpc(NetworkConnection caller = null)
    {
        if (caller != null && NetworkObject.Observers.Contains(caller))
            SendInitialFollowerState(caller);
    }

    [Server]
    public void SendInitialFollowerState(NetworkConnection connection)
    {
        InitialFollowerStateTargetRpc(
            connection,
            serverPathSequence,
            followerCount.Value,
            serverPath.CopyPoints());
    }

    [TargetRpc]
    private void InitialFollowerStateTargetRpc(
        NetworkConnection connection,
        uint sequence,
        int count,
        Vector3[] path)
    {
        if (!clientPathInitialized || sequence >= clientPathSequence)
        {
            clientPath.Load(path);
            clientPathSequence = sequence;
            clientPathInitialized = true;
        }
        pathSnapshotRequested = false;
        ApplyFollowerCount(Mathf.Max(count, followerCount.Value));
    }

    private float GetRetainedPathDistance()
    {
        FollowerManager settings = FindFollowerManager();
        int limit = settings == null ? maximumSpawnedFollowers : Mathf.Max(1, settings.maxFollowerPerLine);
        int scaleCount = Mathf.Max(limit, followerCount.Value);
        if (settings != null)
            scaleCount = Mathf.Max(scaleCount, settings.FollowerCnt);
        return GetFirstFollowerDistance(scaleCount) + (limit - 1) * GetFollowerSpacing(scaleCount) + 20f;
    }

    private FollowerManager FindFollowerManager()
    {
        if (IsClientInitialized)
        {
            GameObject visual = IsOwner ? local : remote;
            if (visual == null || isDead)
                return null;
            if (remoteFollower == null || !remoteFollower.transform.IsChildOf(visual.transform))
                remoteFollower = visual.GetComponentInChildren<FollowerManager>(true);
            return remoteFollower;
        }

        if (remoteFollower != null)
            return remoteFollower;

        FollowerManager[] managers = GetComponentsInChildren<FollowerManager>(true);
        if (managers.Length == 0)
            return null;

        remoteFollower = managers[0];
        return remoteFollower;
    }

    public void StartSprint()
    {
        StartSprintReq(OwnerId);
    }

    [ServerRpc]
    private void StartSprintReq(int clientId)
    {
        GameManager.Instance.OnStartSprint(clientId);
    }

    public void StopSprint()
    {
        StopSprintReq(OwnerId);
    }

    [ServerRpc]
    private void StopSprintReq(int clientId)
    {
        GameManager.Instance.OnStopSprint(clientId);
    }
}
