using UnityEngine;
using UnityEngine.UI;

public class HeartUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    [SerializeField] GameObject heartPrefab;

    void OnEnable()
    {
        playerHealth.OnHealthChanged += SetHeart;
    }

    public void SetHeart(int amount)
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < amount; i++)
            Instantiate(heartPrefab, transform);
    }

}
