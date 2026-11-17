using System.Linq;
using UnityEngine;

public class Camera : MonoBehaviour
{
    // 플레이어 오브젝트를 저장할 변수
    public Vector3 i;
    public Transform player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        
    }

    // Update is called once per frame
    void Update()
    {
        

    }
    void LateUpdate()
    {
        transform.position = player.position + new Vector3(0, 2.2f, -2);
        transform.rotation = Quaternion.Euler(45, 0, 0);
    }
}
