using UnityEngine;

public class NeutralMovement : MonoBehaviour
{
    [SerializeField] private float updateInterval = 3f;
    [SerializeField] private float wanderingRadius = 5f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private Animator animator;

    private float timeSinceLastUpdate;
    private Vector3 targetPosition;

    void OnEnable()
    {
        timeSinceLastUpdate = updateInterval;
        targetPosition = GetRandomPosition();
    }

    void Update()
    {

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0;
        dir = dir.normalized;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }

        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= updateInterval)
        {
            targetPosition = GetRandomPosition();
            animator.Play("Hopak_Walk");
            timeSinceLastUpdate = 0f;
        }

    }

    Vector3 GetRandomPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderingRadius;
        Vector3 pos = new Vector3(randomCircle.x + transform.position.x, 1, randomCircle.y + transform.position.z);
        return pos;

    }

}
