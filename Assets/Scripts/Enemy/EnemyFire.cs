using System.Collections;
using UnityEngine;

public class EnemyFire : MonoBehaviour
{
    [SerializeField] private float fireInterval = 1f;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] private float fireRanage = 3f;
    [SerializeField] private float projectileSpeed = 1f;
    [SerializeField] private float fireDelay = 1f;
    private float lastfireTime = -999f;

    private Transform target = null;
    private bool canFire = true;

    private Vector3 targetDir;
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
        targetDir = (targetPos2D - curPos2D).normalized;

        float dist = Vector3.Distance(curPos2D, targetPos2D);
        if (dist <= fireRanage && projectilePrefab != null)
        {
            if (canFire)
            {
                StartCoroutine(Fire());
            }
        }

    }

    IEnumerator Fire()
    {
        canFire = false;

        yield return new WaitForSeconds(fireDelay);

        GameObject projectileInstance = PoolManager.Instance.Get(PoolType.Projectile);
        projectileInstance.transform.position = transform.position;
        projectileInstance.transform.rotation = Quaternion.identity; ;
        EnemyProjectile projectile = projectileInstance.GetComponent<EnemyProjectile>();
        projectile.rb.linearVelocity = targetDir * projectileSpeed;
        lastfireTime = Time.time;

        canFire = true;
    }

}
