using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class PlayerInputManager : MonoBehaviour
{
    [SerializeField]
    private PlayerInput playerInput;

    public Dictionary<string, Action<CallbackContext>> inputList = new();


    private void Start()
    {
        Init();
    }

    private void Init()
    {
        playerInput.actions["Left"].performed += OnLeft;
        playerInput.actions["Right"].performed += OnRight;
        playerInput.actions["Rotate"].performed += OnRotate;
        playerInput.actions["Sprint"].performed += OnSprint;
        playerInput.actions["Sprint"].canceled += OnSprintEnd;
    }

    public void Subscribe(string key, Action<CallbackContext> action)
    {
        if(inputList.TryGetValue(key, out var value))
        {
            value += action;
        }
        else
        {
            inputList.Add(key, action);
        }
    }

    private void ReleaseInputActions()
    {
        playerInput.actions["Left"].performed -= OnLeft;
        playerInput.actions["Right"].performed -= OnRight;
        playerInput.actions["Rotate"].performed -= OnRotate;
        playerInput.actions["Sprint"].performed -= OnSprint;
    }

    #region Input

    private void OnLeft(CallbackContext context)
    {
        if(inputList.TryGetValue("Left", out Action<CallbackContext> action))
            action?.Invoke(context);
    }

    private void OnRight(CallbackContext context)
    {
        if (inputList.TryGetValue("Right", out Action<CallbackContext> action))
            action?.Invoke(context);
    }

    private void OnRotate(CallbackContext context)
    {
        if (inputList.TryGetValue("Rotate", out Action<CallbackContext> action))
            action?.Invoke(context);
    }
    private void OnSprint(CallbackContext context)
    {
        if (inputList.TryGetValue("Sprint", out Action<CallbackContext> action))
            action?.Invoke(context);
    }
    private void OnSprintEnd(CallbackContext context)
    {
        if (inputList.TryGetValue("SprintEnd", out Action<CallbackContext> action))
            action?.Invoke(context);
    }
    #endregion
}
