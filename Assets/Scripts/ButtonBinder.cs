using UnityEngine;
using System;
using UnityEngine.UI;

public class ButtonBinder : MonoBehaviour
{
    Action unlockDash;
   public Button button;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button.onClick.AddListener(() => unlockDash?.Invoke());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
