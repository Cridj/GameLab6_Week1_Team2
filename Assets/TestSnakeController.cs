using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TestSnakeController : MonoBehaviour
{
    public float moveSpeed = 5;
    public float steerSpeed = 180;
    public float bodySpeed = 5;
    public int gap = 10;

    public GameObject bodyPrefab;


    private List<GameObject> bodyParts = new List<GameObject>();
    private List<Vector3> positionHistory = new List<Vector3>();

    void Start()
    {
        GrowSnake();
        GrowSnake();
        GrowSnake();
        GrowSnake();
    }

    void Update()
    {
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        float steerDirection = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * steerDirection * steerSpeed * Time.deltaTime);

        if (positionHistory.Count > 0)
        {
            float dist = Vector3.Distance(transform.position, positionHistory[0]);
            if (dist >= 0.2)
                positionHistory.Insert(0, transform.position);

        }
        else
        {
            positionHistory.Insert(0, transform.position);
        }

        int index = 1;
        foreach (var body in bodyParts)
        {
            Vector3 point = positionHistory[Mathf.Min(index * gap, positionHistory.Count - 1)];
            Vector3 moveDirection = point - body.transform.position;
            body.transform.position += moveDirection * bodySpeed * Time.deltaTime;
            body.transform.LookAt(point);
            index++;
        }

    }

    void GrowSnake()
    {
        GameObject body = null;
        if (bodyParts.Count == 0)
        {
            Vector3 spawnPos = transform.position - transform.forward * 2;
            body = Instantiate(bodyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Vector3 spawnPos = bodyParts[bodyParts.Count - 1].transform.position - transform.forward * 2;
            body = Instantiate(bodyPrefab, spawnPos, Quaternion.identity);
        }

        if (body != null)
            bodyParts.Add(body);
    }
}
