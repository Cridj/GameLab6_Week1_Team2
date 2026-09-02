using UnityEngine;

public class Infection : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("JuniorHopak"))
        {
            if (other.TryGetComponent(out HopackCarrier hopackCarrier))
            {
                hopackCarrier.followerManager.MakeFollower();
                PoolType type = PoolManager.Instance.GetPoolType(gameObject);
                PoolManager.Instance.Return(type, gameObject);

            }
        }
    }

}
