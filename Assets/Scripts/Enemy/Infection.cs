using UnityEngine;

public class Infection : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("find player");
            HopackCarrier hopackCarrier = other.GetComponent<HopackCarrier>();
            if (hopackCarrier != null)
            {
                hopackCarrier.followerManager.MakeFollower();
                Destroy(gameObject);
            }

        }
    }

}
