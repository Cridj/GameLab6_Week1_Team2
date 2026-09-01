using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [SerializeField] private float contactInterval = 11f;
    [SerializeField] private float contactDamage = 5f;
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
        Debug.Log("Dealt Contact Damage");
    }
}
