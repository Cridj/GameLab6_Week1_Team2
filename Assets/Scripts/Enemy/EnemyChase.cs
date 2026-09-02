using UnityEngine;

public class EnemyChase : MonoBehaviour
{

    [SerializeField] private Animator animator;
    [SerializeField] private float chaseSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float detectionRange;
    [SerializeField] private float stopDistance;

    private Transform target = null;
    private bool isMoving = false;


    void OnEnable()
    {
        if (target != null) return;
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (target == null) return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= detectionRange)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0;
            dir = dir.normalized;
            LookAtPlayer(dir);
            if (dist > stopDistance)
            {
                transform.position += chaseSpeed * Time.deltaTime * dir;
                if (!isMoving)
                {
                    isMoving = true;
                    animator.Play("Hopak_Run");
                }
            }
            else if (dist <= stopDistance)
            {
                isMoving = false;
                animator.Play("Default");
            }

        }
    }

    void LookAtPlayer(Vector3 dir)
    {
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

}
