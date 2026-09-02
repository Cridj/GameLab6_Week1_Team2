using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [SerializeField] private float contactInterval = 2f;
    [SerializeField] private float contactDamage = 1f;
    private float lastHitTime = -999f;

    void OnTriggerStay(Collider collider)
    {
        if (Time.time - lastHitTime < contactInterval) return;

        if (collider.CompareTag("Player"))
        {
            DealtDamage(contactDamage);
            lastHitTime = Time.time;
        }
    }

    void DealtDamage(float amount)
    {
        Debug.Log("Dealt Contact Damage!!!");
    }
}
