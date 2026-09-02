using System.Collections;
using UnityEngine;

public class ObstaclePillar : MonoBehaviour
{
    private MeshCollider meshCollider;
    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        StartCoroutine(ActiveCollider());
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Crash");
            //Todo 충돌 후 효과
        }


    }

    IEnumerator ActiveCollider()
    {
        yield return new WaitForSeconds(0.5f);
        meshCollider.enabled = true;

    }
}
