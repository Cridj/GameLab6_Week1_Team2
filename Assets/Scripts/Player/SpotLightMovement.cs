using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class SpotLightMovement : MonoBehaviour
{
    [SerializeField] private GameObject leftSpotLight;

    [SerializeField] private GameObject rightSpotLight;

    [SerializeField] private GameObject frontSpotLight;

    [SerializeField] private GameObject backSpotLight;

    [SerializeField] private GameObject centerSpotLight;

    [SerializeField] private float moveSpeed = 2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        LeftToCenter();
        RightToCenter();
    }


    private void LeftToCenter()
    {
        if (leftSpotLight.transform.position.x >= centerSpotLight.transform.position.x)
        {
            leftSpotLight.SetActive(false);
            return;
        }

        Vector3 position = leftSpotLight.transform.position;
        position.x += moveSpeed * Time.deltaTime;
        leftSpotLight.transform.position = position;
    }


    private void RightToCenter()
    {
        if (rightSpotLight.transform.position.x <= centerSpotLight.transform.position.x)
        {
            rightSpotLight.SetActive(false);
            return;
        }
        Vector3 position = rightSpotLight.transform.position;
        position.x -= moveSpeed * Time.deltaTime;
        rightSpotLight.transform.position = position;
    }


    /*
     * Spotlight 소환하기.(Instantiate) Unity에 강좌가 있던 것 같다.
     * Instantiate (SpotLight Prefab으로 만들어서) -> 가운데 지점으로 이동. 가운데 지점에 이동하는 순간 비활성화.
     * 가장 중요한 건 채보.. 
     * 
    
     
     */

}
