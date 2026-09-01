using UnityEngine;

public class EnemyFire : MonoBehaviour
{
    [SerializeField] private float fireInterval = 1f;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] private float fireRanage = 3f;
    [SerializeField] private float projectileSpeed = 1f;
    private float lastfireTime = -999f;

    private Transform target = null;

    void OnEnable()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (target == null) return;
        if (Time.time - lastfireTime < fireInterval) return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= fireRanage && projectilePrefab != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            GameObject projectileInstance = Instantiate(projectilePrefab, transform.position + new Vector3(1, 0, 0), Quaternion.identity);
            EnemyProjectile projectile = projectileInstance.GetComponent<EnemyProjectile>();
            projectile.rb.linearVelocity = dir * projectileSpeed;
            lastfireTime = Time.time;
        }
    }

}
