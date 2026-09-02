using UnityEngine;

public class Infection : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponentInChildren<FollowerManager>().MakeFollower();
            Destroy(gameObject);
        }
    }

}
