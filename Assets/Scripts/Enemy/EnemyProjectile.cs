using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] int damage = 1;
    public Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
        }

    }
}
