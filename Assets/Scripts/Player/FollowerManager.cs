using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class FollowerManager : MonoBehaviour
{
    public GameObject followerPrefab;
    public List<GameObject> followers = new List<GameObject>();
    public GameObject infected;
    public float Gap = 1f;
    public int maxFollowerPerLine = 100;
    [SerializeField] private float followerYOffset = 0f;
    public Action<int> onSpawn;
    public bool isRemote;
    public bool isInitialized;
    public NetworkPlayer player;
    public CustomizeInfo customInfo;
    private bool hasDied;

    public int FollowerCnt { get; private set; }
    private int MaximumSpawnedCount => Mathf.Max(1, maxFollowerPerLine);

    private void Start()
    {
        if (!isRemote && player != null && player.IsOwner && GameManager.Instance != null)
            GameManager.Instance.myFollower = this;
    }

    private void LateUpdate()
    {
        if (hasDied || player == null || !player.IsClientInitialized || followers.Count == 0)
            return;

        FollowerPath path = player.Path;
        FollowerPath.Cursor cursor = path.Begin(player.transform.position, player.transform.forward);
        float firstDistance = player.GetFirstFollowerDistance(FollowerCnt);
        Gap = player.GetFollowerSpacing(FollowerCnt);

        for (int i = 0; i < followers.Count; i++)
        {
            GameObject follower = followers[i];
            if (follower == null)
                continue;

            Vector3 forward;
            Vector3 position = path.Behind(ref cursor, firstDistance + i * Gap, out forward);
            position.y = followerYOffset;
            follower.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasDied || isRemote || player == null || !player.IsClientInitialized || !player.IsOwner)
            return;

        if (other.CompareTag("Neutral"))
        {
            SpawnFollower(other);
        }
        else if (other.CompareTag("Follower") && !followers.Contains(other.gameObject))
        {
            Follower follower = other.GetComponent<Follower>();
            if (follower != null)
            {
                hasDied = true;
                player.OnDie(follower.ownerName);
                ClearFollowers();
            }
        }
    }

    private void SpawnFollower(Collider other)
    {
        Neutral neutral = other.GetComponent<Neutral>();
        if (neutral == null)
            return;

        MakeFollower();
        onSpawn?.Invoke(neutral.Id);
        GameManager.Instance.IncreaseFollower();
        Destroy(other.gameObject);
    }

    public void MakeFollower()
    {
        SetFollowerCount(FollowerCnt + 1);
        if (!isRemote && player != null && player.IsOwner && player.myController != null)
        {
            player.myController.maxSpeed = Mathf.Clamp(player.myController.maxSpeed + 0.1f, 0f, 30f);
        }
    }

    private bool AddFollowerObject()
    {
        if (infected == null || followers.Count >= MaximumSpawnedCount)
            return false;

        Vector3 position = followers.Count == 0
            ? (player != null ? player.transform.position : transform.position)
            : followers[followers.Count - 1].transform.position;
        position.y = followerYOffset;
        GameObject follower = Instantiate(infected, position, Quaternion.identity);
        followers.Add(follower);

        Animator animator = follower.GetComponentInChildren<Animator>();
        if (animator != null)
            animator.Play("Hopak");

        ApplyFollowerCustomize(follower);
        return true;
    }

    private void ApplyFollowerCustomize(GameObject instance)
    {
        Follower follower = instance.GetComponent<Follower>();
        if (follower == null)
            return;
        follower.ApplyCustomizeInfo(customInfo);
        follower.ownerName = customInfo.nickName;
    }

    public IEnumerator ApplyCustomize()
    {
        yield return new WaitForSeconds(0.1f);
        foreach (GameObject follower in followers)
        {
            if (follower != null)
                ApplyFollowerCustomize(follower);
        }
    }

    public void SetFollowerCount(int targetCount)
    {
        targetCount = Mathf.Max(0, targetCount);
        int targetSpawnCount = Mathf.Min(targetCount, MaximumSpawnedCount);
        if (isInitialized && targetCount == FollowerCnt && targetSpawnCount == followers.Count)
            return;

        while (followers.Count < targetSpawnCount)
        {
            if (!AddFollowerObject())
                break;
        }

        while (followers.Count > targetSpawnCount)
        {
            int lastIndex = followers.Count - 1;
            GameObject follower = followers[lastIndex];
            followers.RemoveAt(lastIndex);
            if (follower != null)
                Destroy(follower);
        }

        FollowerCnt = targetCount;
        isInitialized = true;
        RefreshFollowerScale();
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
        {
            player.IncreaseScale(FollowerCnt);
            Gap = player.GetFollowerSpacing(FollowerCnt);
        }
    }

    public void StopFollowing()
    {
        hasDied = true;
        StopAllCoroutines();
        ClearFollowers();
    }

    private void ClearFollowers()
    {
        foreach (GameObject follower in followers)
        {
            if (follower != null)
                Destroy(follower);
        }
        followers.Clear();
        FollowerCnt = 0;
    }

    private void OnDestroy()
    {
        ClearFollowers();
    }
}
