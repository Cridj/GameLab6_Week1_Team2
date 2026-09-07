using AYellowpaper.SerializedCollections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public GameObject local, remote;
    public FollowerManager remoteFollower;
    public PlayerController myController;

    private readonly Queue<int> pendingFollowerCounts = new();
    private readonly SyncVar<int> followerCount = new();
    private readonly List<Vector3> serverFollowerPositions = new();
    private readonly List<Vector3> serverPositionHistory = new();
    [SerializeField] private float serverPathPointDistance = 0.5f;
    [SerializeField] private float serverPathSendInterval = 0.1f;
    private float serverPathSendTimer;
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
    [SerializeField] private GameObject[] trails;

    private void Awake()
    {
        followerCount.OnChange += OnFollowerCountChanged;
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

        var followerManager = GetComponentInChildren<FollowerManager>();
        followerManager.customInfo = customInfo;
        StartCoroutine(followerManager.ApplyCustomize());
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
        ApplyFollowerCount(followerCount.Value);
        StartCoroutine(ProcessPendingWhenReady());
    }

    private void Update()
    {
        if (!IsServerInitialized)
            return;

        if (!serverFollowerStateInitialized || serverFollowerPositions.Count == 0)
            return;

        serverPathSendTimer -= Time.deltaTime;
        if (serverPathSendTimer <= 0f &&
            (serverPositionHistory.Count == 0 ||
             Vector3.Distance(transform.position, serverPositionHistory[0]) >= serverPathPointDistance))
        {
            serverPathSendTimer = serverPathSendInterval;
            serverPositionHistory.Insert(0, transform.position);
            AddPathPointObserversRpc(transform.position);
            TrimServerPositionHistory();
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

    }

    [ServerRpc]
    private void JoinGameReq(int clientId, CustomizeInfo customInfo)
    {
        GameManager.Instance.OnJoinPlayer(clientId, this, customInfo);
    }

    private void OnFollowerCountChanged(int previous, int next, bool asServer)
    {
        if (asServer || IsOwner)
            return;

        ApplyFollowerCount(next);
    }

    private void ApplyFollowerCount(int count)
    {
        if (IsOwner)
            return;

        FollowerManager followerManager = FindFollowerManager();
        if (followerManager == null)
        {
            pendingFollowerCounts.Enqueue(count);
            return;
        }

        followerManager.SetFollowerCount(count);
        transform.localScale = Utils.CalculateScale(count);
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

        while (pendingFollowerCounts.Count > 0)
        {
            int count = pendingFollowerCounts.Dequeue();
            FollowerManager followerManager = FindFollowerManager();
            if (followerManager == null)
                yield break;

            followerManager.SetFollowerCount(count);
            transform.localScale = Utils.CalculateScale(count);
            yield return null;
        }
    }


    public void OnDie(string ownerName)
    {
        //TODO 공격자 정보 UI에 표시해주기
        OnDieReq(NetworkObject.OwnerId);
        diePanel.SetActive(true);
        mainCam.transform.SetParent(transform);
        UIPanel.transform.SetParent(transform);
        myController.playerUI.UpdateDieText(ownerName);
        Destroy(local.gameObject);
        StartCoroutine(OnDieReturnMain());
    }

    private IEnumerator OnDieReturnMain()
    {
        while(true)
        {
            yield return null;
            if(Input.GetKeyDown(KeyCode.Space))
            {
                Managers.Instance.Fade.FadeOut(() =>
                {
                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainLobby");
                });
            }
        }
    }


    [ServerRpc]
    private void OnDieReq(int clientId)
    {
        GameManager.Instance.OnDiePlayer(clientId);
    }

    [ServerRpc]
    private void OnLeftPlayerReq(int clientId)
    {
        GameManager.Instance.OnLeftPlayer(clientId);
    }

    //private void OnApplicationQuit()
    //{
    //    OnLeftPlayerReq(OwnerId);
    //}


    public void IncreaseScale(int cnt)
    {
        Vector3 scale = Utils.CalculateScale(cnt);
        transform.localScale = scale;
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
        followerCount.Value = serverFollowerPositions.Count;
    }

    [ObserversRpc(ExcludeServer = true)]
    private void AddPathPointObserversRpc(Vector3 position)
    {
        FollowerManager followerManager = FindFollowerManager();
        if (followerManager != null)
            followerManager.AddAuthoritativePathPoint(position);
    }

    [Server]
    public void SendInitialFollowerState(NetworkConnection connection)
    {
        InitialFollowerStateTargetRpc(
            connection,
            followerCount.Value,
            serverPositionHistory.ToArray());
    }

    [TargetRpc]
    private void InitialFollowerStateTargetRpc(
        NetworkConnection connection,
        int count,
        Vector3[] path)
    {
        if (IsOwner)
            return;

        FollowerManager followerManager = FindFollowerManager();
        if (followerManager == null)
        {
            pendingFollowerCounts.Enqueue(count);
            return;
        }

        followerManager.SetAuthoritativePath(path);
        followerManager.SetFollowerCount(count);
        transform.localScale = Utils.CalculateScale(count);
    }

    private void TrimServerPositionHistory()
    {
        int requiredCount = Mathf.CeilToInt(
            GetFirstFollowerGap() + serverFollowerPositions.Count * GetFollowerGap()) + 10;
        if (serverPositionHistory.Count > requiredCount)
            serverPositionHistory.RemoveRange(requiredCount, serverPositionHistory.Count - requiredCount);
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
