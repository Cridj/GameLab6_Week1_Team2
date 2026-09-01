using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Projectile hit player");
            //TakeDamage
            Destroy(gameObject);
        }

    }
}
