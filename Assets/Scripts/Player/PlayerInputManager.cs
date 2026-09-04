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
        playerInput.actions["Turn"].performed += OnTurn;
        playerInput.actions["Turn"].canceled += OnTurnEnd;
        playerInput.actions["Jump"].performed += OnJump;
        playerInput.actions["Sprint"].performed += OnSprint;
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
        playerInput.actions["Turn"].performed -= OnTurn;
        playerInput.actions["Turn"].canceled -= OnTurnEnd;
        playerInput.actions["Jump"].performed -= OnJump;
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

    private void OnTurn(CallbackContext context)
    {
        if (inputList.TryGetValue("Turn", out Action<CallbackContext> action))
            action?.Invoke(context);
    }

    private void OnTurnEnd(CallbackContext context)
    {
        if (inputList.TryGetValue("Turn", out Action<CallbackContext> action))
            action?.Invoke(context);
    }

    private void OnSprint(CallbackContext context)
    {
        if (inputList.TryGetValue("Sprint", out Action<CallbackContext> action))
            action?.Invoke(context);
    }

    private void OnJump(CallbackContext context)
    {
        if (inputList.TryGetValue("Jump", out Action<CallbackContext> action))
            action?.Invoke(context);
    }
    #endregion
}
