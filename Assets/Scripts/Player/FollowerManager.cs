using UnityEngine;
using System.Collections.Generic;

public class FollowerManager : MonoBehaviour
{
    public GameObject followerPrefab;
    private List<GameObject> followers = new List<GameObject>();
    private List<Vector3> positionHistory = new List<Vector3>();
    public int Gap = 1;
    public int maxFollowerPerLine = 10;
    public float gapBetweenLine = 1f;
    private int followersCnt = 0;

    public PlayerController playerController;

    void Start()
    {
        positionHistory.Insert(0, transform.position);
    }

    void Update()
    {
        // 일정 거리마다 position 기록
        float dist = Vector3.Distance(transform.position, positionHistory[0]);
        if (dist >= 1.5f)
        {
            positionHistory.Insert(0, transform.position);
        }

        int index = 1;
        foreach (var follower in followers)
        {
            Vector3 point = positionHistory[Mathf.Min(index * Gap, positionHistory.Count - 1)];
            point.y = 1;
            Vector3 moveDirection = point - follower.transform.position;
            follower.transform.position += moveDirection * 10f * Time.deltaTime;
            follower.transform.LookAt(point);
            index++;
        }
    }

    public void MakeFollower()
    {
        GameObject follower;
        if (followers.Count == 0)
        {
            Vector3 spawnPos = transform.position - transform.forward * 1.5f;
            follower = Instantiate(followerPrefab, spawnPos, Quaternion.identity);
            followers.Add(follower);

            followersCnt++;
        }
        else if (followers.Count < maxFollowerPerLine)
        {
            Vector3 spawnPos = followers[followers.Count - 1].transform.position - transform.forward * 1.5f;
            follower = Instantiate(followerPrefab, spawnPos, Quaternion.identity);
            followers.Add(follower);

            followersCnt++;
        }
        else
        {
            int lineNum = followersCnt / maxFollowerPerLine;
            int x = followersCnt % maxFollowerPerLine;

            GameObject targetFollower = followers[x];
            follower = Instantiate(followerPrefab, targetFollower.transform);

            if (lineNum % 2 == 0)
                //follower.transform.localPosition += targetFollower.transform.localRotation * Vector3.right * gapBetweenLine * (lineNum / 2);
                follower.transform.localPosition = new Vector3(gapBetweenLine * (lineNum / 2), 0, 0);

            else
                //follower.transform.localPosition -= targetFollower.transform.right * gapBetweenLine * ((lineNum + 1) / 2);
                follower.transform.localPosition = new Vector3(-gapBetweenLine * ((lineNum + 1) / 2), 0, 0);

            followersCnt++;
        }

        follower.GetComponentInChildren<Animator>().Play("Hopak");

    }
}
