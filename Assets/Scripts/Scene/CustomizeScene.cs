using UnityEngine;

public class CustomizeScene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Managers.Instance.Fade.FadeIn();
    }

}
