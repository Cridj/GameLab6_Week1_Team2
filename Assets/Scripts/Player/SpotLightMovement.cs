using System.Runtime.CompilerServices;
using UnityEngine;

public class SpotLightMovement : MonoBehaviour
{
    [SerializeField]
    private GameObject leftSpotLight;
    private GameObject rightSpotLight;
    private GameObject frontSpotLight;
    private GameObject backSpotLight;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ToCenter();
    }


    private void ToCenter()
    {
        int spot = 0;
        leftSpotLight.transform.
    }

    /*
     * Spotlight 소환하기.(Instantiate) Unity에 강좌가 있던 것 같다.
     * Instantiate (SpotLight Prefab으로 만들어서) -> 가운데 지점으로 이동. 가운데 지점에 이동하는 순간 비활성화.
     * 가장 중요한 건 채보.. 
     * 
    
     
     */

}
