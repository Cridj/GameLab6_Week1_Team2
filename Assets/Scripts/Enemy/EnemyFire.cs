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

        Vector3 curPos2D = transform.position;
        curPos2D.y = 0;
        Vector3 targetPos2D = target.position;
        targetPos2D.y = 0;
        float dist = Vector3.Distance(curPos2D, targetPos2D);
        if (dist <= fireRanage && projectilePrefab != null)
        {
            Vector3 dir = (targetPos2D - curPos2D).normalized;
            GameObject projectileInstance = PoolManager.Instance.Get(PoolType.Projectile);
            projectileInstance.transform.position = transform.position;
            projectileInstance.transform.rotation = Quaternion.identity; ;
            EnemyProjectile projectile = projectileInstance.GetComponent<EnemyProjectile>();
            projectile.rb.linearVelocity = dir * projectileSpeed;
            lastfireTime = Time.time;
        }
    }

}
