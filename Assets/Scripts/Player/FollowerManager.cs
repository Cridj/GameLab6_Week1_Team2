using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

public class FollowerManager : MonoBehaviour
{
    public GameObject followerPrefab;
    public List<GameObject> followers = new List<GameObject>();

    [SerializeField]
    public List<Vector3> positionHistory = new List<Vector3>();

    public GameObject infected;
    [SerializeField]
    public float Gap = 1f;
    public int maxFollowerPerLine = 100;
    [SerializeField] private float followerYOffset = 0f;
    [SerializeField] private float serverSmoothTime = 0.04f;
    [SerializeField] private float historyRecordDistance = 0.2f;
    [SerializeField] private float gapSamplesPerScale = 3f;
    [SerializeField] private float firstFollowerGap = 1f;
    private int followersCnt = 0;
    [SerializeField] private float followerSpeed = 35f;

    public Action<int> onSpawn;
    public bool isRemote = false;
    public bool isInitialized = false;
    public NetworkPlayer player;
    private bool useServerPositions;
    private bool useAuthoritativePath;
    private readonly List<Vector3> serverTargetPositions = new List<Vector3>();
    private readonly List<Vector3> serverSmoothVelocities = new List<Vector3>();
    public CustomizeInfo customInfo;

    public int FollowerCnt
    {
        get { return followersCnt; }
        set
        {
            followersCnt = value;
        }
    }


    void Start()
    {
        if (followerSpeed <= 0f)
            followerSpeed = 35f;

        if (!isRemote)
        {
            positionHistory.Insert(0, transform.position);
            GameManager.Instance.myFollower = this;
        }
        else
        {
            positionHistory.Insert(0, transform.position);
        }
    }

    void Update()
    {
        if (useServerPositions)
        {
            UpdateServerFollowers();
            return;
        }

        if (!isRemote || useAuthoritativePath)
        {
            // 로컬 플레이어만 자기 위치를 기록하고. 원격 플레이어는 서버 경로 RPC에서 받아와서 사용하기
            if (!useAuthoritativePath)
            {
                float dist = Vector3.Distance(transform.position, positionHistory[0]);
                if (dist >= historyRecordDistance)
                {
                    positionHistory.Insert(0, transform.position);
                    TrimPositionHistory();
                }
            }

            int index = 1;
            foreach (var follower in followers)
            {
                int followerIndex = index - 1;
                int historyIndex = Mathf.Min(
                    followerIndex == 0
                        ? Mathf.RoundToInt(firstFollowerGap)
                        : Mathf.RoundToInt(firstFollowerGap + followerIndex * Gap),
                    positionHistory.Count - 1);
                Vector3 point = positionHistory[historyIndex];
                point.y = followerYOffset;
                follower.transform.position = Vector3.MoveTowards(
                    follower.transform.position,
                    point,
                    followerSpeed * Time.deltaTime);
                LookAtFollowerTarget(index - 1);
                index++;
            }
        }
        else
        {
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isRemote)
            return;
        if (other.CompareTag("Neutral"))
        {
            SpawnFollower(other);
        }
        else if(other.CompareTag("Follower"))
        {
            if (!isRemote)
            {
                if(!followers.Contains(other.gameObject))
                {
                    var follower = other.GetComponent<Follower>();
                    if(follower != null)
                    {
                        player.OnDie(follower.ownerName);
                        foreach (var fol in followers)
                            Destroy(fol);
                    }
                }
            }
        }
    }


    private void SpawnFollower(Collider other)
    {
        Neutral neutral = other.GetComponent<Neutral>();
        MakeFollower();

        onSpawn?.Invoke(neutral.Id);
        GameManager.Instance.IncreaseFollower();

        Destroy(other.gameObject);
    }

    public float GetFollowerYOffset()
    {
        return followerYOffset;
    }

    public float GetFollowerSpeed()
    {
        return followerSpeed;
    }

    public float GetHistoryRecordDistance()
    {
        return historyRecordDistance;
    }

    public float GetGapForFollowerCount(int count)
    {
        return Mathf.Max(1f, Utils.CalculateScale(count).x * gapSamplesPerScale);
    }

    public float GetFirstFollowerGap()
    {
        return Mathf.Max(0f, firstFollowerGap);
    }

    public void ApplyServerPositions(List<Vector3> positions) // 클라에서 매프레임 서버 포지션 받아서 처리하던 구 코드 Deprecated
    {
        if (positions == null)
            positions = new List<Vector3>();

        while (followers.Count < positions.Count)
        {
            GameObject follower = Instantiate(infected);
            follower.transform.position = positions[followers.Count];
            follower.transform.rotation = Quaternion.identity;
            followers.Add(follower);
            serverSmoothVelocities.Add(Vector3.zero);

            Animator animator = follower.GetComponentInChildren<Animator>();
            if (animator != null)
                animator.Play("Hopak");
        }

        while (followers.Count > positions.Count)
        {
            int lastIndex = followers.Count - 1;
            GameObject follower = followers[lastIndex];
            followers.RemoveAt(lastIndex);

            if (follower != null)
                Destroy(follower);

            if (lastIndex < serverSmoothVelocities.Count)
                serverSmoothVelocities.RemoveAt(lastIndex);
        }

        while (serverSmoothVelocities.Count < followers.Count)
            serverSmoothVelocities.Add(Vector3.zero);

        while (serverSmoothVelocities.Count > followers.Count)
            serverSmoothVelocities.RemoveAt(serverSmoothVelocities.Count - 1);

        serverTargetPositions.Clear();
        serverTargetPositions.AddRange(positions);

        FollowerCnt = followers.Count;
        SyncGapWithScale();
        isInitialized = true;
        useServerPositions = true;

        Vector3 followerScale = Utils.CalculateScale(FollowerCnt);
        foreach (GameObject follower in followers)
        {
            if (follower != null)
                follower.transform.localScale = followerScale;
        }

        if (player != null)
        {
            player.IncreaseScale(FollowerCnt);
        }
    }

    private void UpdateServerFollowers()
    {
        float smoothTime = Mathf.Max(0.001f, serverSmoothTime);

        for (int i = 0; i < followers.Count; i++)
        {
            if (followers[i] == null || i >= serverTargetPositions.Count)
                continue;

            Vector3 velocity = serverSmoothVelocities[i];
            followers[i].transform.position = Vector3.SmoothDamp(
                followers[i].transform.position,
                serverTargetPositions[i],
                ref velocity,
                smoothTime,
                Mathf.Infinity,
                Time.deltaTime);
            serverSmoothVelocities[i] = velocity;

            LookAtFollowerTarget(i);
        }
    }

    private void LookAtFollowerTarget(int followerIndex)
    {
        if (followerIndex < 0 || followerIndex >= followers.Count || followers[followerIndex] == null)
            return;

        Transform target = followerIndex == 0
            ? transform
            : followers[followerIndex - 1].transform;

        Vector3 direction = target.position - followers[followerIndex].transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            float yRotation = Quaternion.LookRotation(direction).eulerAngles.y;
            followers[followerIndex].transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }


    public void MakeFollower()
    {
        MakeFollower(true);
    }

    private void MakeFollower(bool refreshScale) // 한마리 한마리 추가
    {
        GameObject follower;
        if (followers.Count == 0)
        {
            Vector3 spawnPos = transform.position;/* - transform.forward * 0.3f;*/
            spawnPos.y += followerYOffset;
            follower = Instantiate(infected);
            follower.transform.position = spawnPos;
            follower.transform.rotation = Quaternion.identity;
            followers.Add(follower);

            InitFollower(follower, refreshScale);
        }
        else if (followers.Count < maxFollowerPerLine)
        {
            Vector3 spawnPos = followers[followers.Count - 1].transform.position - transform.forward * 1.5f;
            follower = Instantiate(infected);
            follower.transform.position = spawnPos;
            follower.transform.rotation = Quaternion.identity;
            followers.Add(follower);

            InitFollower(follower, refreshScale);
        }
        FollowerCnt++;
        if (!isRemote) // 자기자신의 플레이어는 팔로워 늘어날때마다 스피드업
        {
            Mathf.Clamp(player.myController.maxSpeed += 0.1f, 0f, 15f);
        }
        SyncGapWithScale();
    }

    private void InitFollower(GameObject follower, bool refreshScale)
    {
        follower.GetComponentInChildren<Animator>().Play("Hopak");
        var fol = follower.GetComponent<Follower>();
        fol.ApplyCustomizeInfo(customInfo);
        fol.ownerName = customInfo.nickName;
        if (refreshScale)
            RefreshFollowerScale();
    }

    public IEnumerator ApplyCustomize()
    {
        yield return new WaitForSeconds(0.1f);

        foreach (var fol in followers)
        {
            var follower = fol.GetComponent<Follower>();
            if(follower != null)
            {
                follower.ApplyCustomizeInfo(customInfo);
            }
        }
    }

    public void SetFollowerCount(int targetCount)
    {
        targetCount = Mathf.Max(0, targetCount);

        while (followers.Count < targetCount)
            MakeFollower(false);

        while (followers.Count > targetCount)
        {
            int lastIndex = followers.Count - 1;
            GameObject follower = followers[lastIndex];
            followers.RemoveAt(lastIndex);
            if (follower != null)
                Destroy(follower);
        }

        FollowerCnt = followers.Count;
        SyncGapWithScale();
        RefreshFollowerScale();
        TrimPositionHistory();
    }

    public void AddAuthoritativePathPoint(Vector3 position)
    {
        useAuthoritativePath = true;
        positionHistory.Insert(0, position);
        TrimPositionHistory();
    }

    public void SetAuthoritativePath(IReadOnlyList<Vector3> path)
    {
        useAuthoritativePath = true;
        positionHistory.Clear();

        if (path != null)
        {
            for (int i = 0; i < path.Count; i++)
                positionHistory.Add(path[i]);
        }

        if (positionHistory.Count == 0)
            positionHistory.Add(transform.position);

        TrimPositionHistory();
    }

    private void RefreshFollowerScale()
    {
        Vector3 scale = Utils.CalculateScale(FollowerCnt);
        foreach (GameObject follower in followers)
        {
            if (follower != null)
                follower.transform.localScale = scale;
        }

        if (player != null)
            player.IncreaseScale(FollowerCnt);
    }

    private void TrimPositionHistory()
    {
        int requiredCount = Mathf.CeilToInt(firstFollowerGap + followers.Count * Gap) + 10;
        requiredCount = Mathf.Max(requiredCount, 2);
        if (positionHistory.Count > requiredCount)
            positionHistory.RemoveRange(requiredCount, positionHistory.Count - requiredCount);
    }

    private void SyncGapWithScale()
    {
        Gap = GetGapForFollowerCount(FollowerCnt);
    }
}
