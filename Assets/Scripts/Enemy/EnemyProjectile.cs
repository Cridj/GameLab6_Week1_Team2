using System.Collections;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] int damage = 1;
    [SerializeField] float lifetime = 3f;
    public Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        rb.linearVelocity = Vector3.zero;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        StartCoroutine(ReturnThis());
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage);
                PoolManager.Instance.Return(PoolType.Projectile, gameObject);
            }
        }
    }

    IEnumerator ReturnThis()
    {
        yield return new WaitForSeconds(lifetime);
        PoolManager.Instance.Return(PoolType.Projectile, gameObject);
    }
}
