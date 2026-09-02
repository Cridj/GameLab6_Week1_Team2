using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [SerializeField] private float contactInterval = 2f;
    [SerializeField] private int contactDamage = 1;
    private float lastHitTime = -999f;

    void OnTriggerStay(Collider collider)
    {
        if (Time.time - lastHitTime < contactInterval) return;

        if (collider.CompareTag("Player"))
        {
            if (collider.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(contactDamage);
                lastHitTime = Time.time;
            }
        }
    }
}
