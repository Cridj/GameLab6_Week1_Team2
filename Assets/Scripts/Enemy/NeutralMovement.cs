using UnityEngine;
using UnityEngine.AI;

public class NeutralMovement : MonoBehaviour
{
    [SerializeField] private float updateInterval = 3f;
    [SerializeField] private float wanderingRadius = 5f;
    private NavMeshAgent agent;
    private float timeSinceLastUpdate;

    void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();
        timeSinceLastUpdate = updateInterval;
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;

        if (agent.isOnNavMesh && timeSinceLastUpdate >= updateInterval)
        {
            Vector3 randomPosition = GetRandomPositionOnNavMesh();
            agent.SetDestination(randomPosition);
            timeSinceLastUpdate = 0f;
        }
    }

    Vector3 GetRandomPositionOnNavMesh()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderingRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderingRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        else
        {
            return transform.position;
        }
    }

}
