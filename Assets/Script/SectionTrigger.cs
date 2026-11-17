using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class SectionTrigger : MonoBehaviour
{
    public GameObject map; //생성시킬 맵 오브젝트
    public Vector3 mapPosition; //맵의 위치를 저장할 변수
    public List<GameObject> mapCount = new();
    public Vector3 playerPosition;
    //public int mapIndex = 10;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float yOffset;


    void Start()
    {
        Vector3 playerPosition = transform.position;
        //MapInfo mapInfo = map.GetComponent<MapInfo>();
        mapPosition = playerPosition +new Vector3(0, yOffset, 95);//mapPosition에 플레이어 위치랑 바닥위치 고려해서 삽입
        
        GameObject go = Instantiate(map, mapPosition, map.transform.rotation);
        mapCount.Add(go);
    }


    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Trigger"))
        {

            GameObject go = Instantiate(map, mapPosition + new Vector3(0, 0, 200), map.transform.rotation);
            
            mapCount.Add(go);
            if(mapCount.Count >4)//3개이상 맵이 생성되면 이전맵 제거 
            {
                Destroy(mapCount[0]);//이전맵제거
                mapCount.RemoveAt(0);//이전인덱스제거
            }

            mapPosition = go.transform.position; //새로 생성된 맵의 위치를 저장

            

        }
        
    }
    
}
