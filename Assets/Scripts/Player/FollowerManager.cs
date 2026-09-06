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
    public int maxFollowerPerLine = 10;
    public float gapBetweenLine = 1f;
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
    private readonly List<Vector3> serverTargetPositions = new List<Vector3>();
    private readonly List<Vector3> serverSmoothVelocities = new List<Vector3>();

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
            positionHistory.Insert(0, transform.position);
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

        if (!isRemote)
        {
            // 일정 거리마다 position 기록
            float dist = Vector3.Distance(transform.position, positionHistory[0]);
            if (dist >= historyRecordDistance)
            {
                positionHistory.Insert(0, transform.position);
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
                    player.OnDie();
                    foreach(var fol in followers)
                    {
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

    public void ApplyServerPositions(List<Vector3> positions)
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
        GameObject follower;
        if (followers.Count == 0)
        {
            Vector3 spawnPos = transform.position;/* - transform.forward * 0.3f;*/
            spawnPos.y += followerYOffset;
            follower = Instantiate(infected);
            follower.transform.position = spawnPos;
            follower.transform.rotation = Quaternion.identity;
            followers.Add(follower);

            FollowerCnt++;
        }

        else if (followers.Count < maxFollowerPerLine)
        {
            Vector3 spawnPos = followers[followers.Count - 1].transform.position - transform.forward * 1.5f;
            follower = Instantiate(infected);
            follower.transform.position = spawnPos;
            follower.transform.rotation = Quaternion.identity;
            followers.Add(follower);

            FollowerCnt++;
        }

        else
        {
            int lineNum = FollowerCnt / maxFollowerPerLine;
            int x = FollowerCnt % maxFollowerPerLine;

            GameObject targetFollower = followers[x];
            follower = Instantiate(infected);
            follower.transform.parent = targetFollower.transform;

            if (lineNum % 2 == 0)
                //follower.transform.localPosition += targetFollower.transform.localRotation * Vector3.right * gapBetweenLine * (lineNum / 2);
                follower.transform.localPosition = new Vector3(gapBetweenLine * (lineNum / 2), 0, 0);

            else
                //follower.transform.localPosition -= targetFollower.transform.right * gapBetweenLine * ((lineNum + 1) / 2);
                follower.transform.localPosition = new Vector3(-gapBetweenLine * ((lineNum + 1) / 2), 0, 0);


            FollowerCnt++;
        }

        SyncGapWithScale();



        follower.GetComponentInChildren<Animator>().Play("Hopak");


        foreach(var fol in followers)
        {
            fol.transform.localScale = Utils.CalculateScale(FollowerCnt);
        }

        if (player != null)
        {
            player.IncreaseScale(followersCnt);
        }
    }

    private void SyncGapWithScale()
    {
        Gap = GetGapForFollowerCount(FollowerCnt);
    }
}