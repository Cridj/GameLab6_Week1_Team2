using UnityEngine;
using UnityEngine.AI;

public class NavChase : MonoBehaviour
{
    private Transform target = null;
    private NavMeshAgent agent;

    [SerializeField] private float detectionRange;

    public float infectionChaseDistance = 2f;

    void OnEnable()
    {
        if (target != null) return;
        target = GameObject.FindGameObjectWithTag("Player").transform;

        agent = GetComponent<NavMeshAgent>();

    }

    void Update()
    {
        if (target == null) return;

        Vector3 pos1 = transform.position;
        Vector3 pos2 = target.position;
        pos1.y = 0;
        pos2.y = 0;

        float dist = Vector3.Distance(pos1, pos2);
        if (dist <= detectionRange)
        {
            agent.SetDestination(target.position);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
