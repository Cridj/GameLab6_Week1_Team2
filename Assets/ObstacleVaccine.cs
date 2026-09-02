using UnityEngine;

public class ObstacleVaccine : MonoBehaviour
{
    [SerializeField] private int vaccineDamage = 1;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(vaccineDamage);
                Debug.Log("vaccine damage");
            }
        }
    }
}
