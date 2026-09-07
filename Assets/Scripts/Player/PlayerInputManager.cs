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
    private InputAction leftAction;
    private InputAction rightAction;
    private InputAction rotateAction;
    private InputAction sprintAction;
    private bool hasStarted;

    private void Start()
    {
        hasStarted = true;
        Init();
    }

    private void OnEnable()
    {
        if (hasStarted)
            Init();
    }

    private void OnDisable()
    {
        ReleaseInputActions();
    }

    private void OnDestroy()
    {
        ReleaseInputActions();
        inputList.Clear();
    }

    private void Init()
    {
        ReleaseInputActions();
        if (playerInput == null || playerInput.actions == null)
            return;

        leftAction = playerInput.actions.FindAction("Left", true);
        rightAction = playerInput.actions.FindAction("Right", true);
        rotateAction = playerInput.actions.FindAction("Rotate", true);
        sprintAction = playerInput.actions.FindAction("Sprint", true);
        leftAction.performed += OnLeft;
        rightAction.performed += OnRight;
        rotateAction.performed += OnRotate;
        sprintAction.performed += OnSprint;
        sprintAction.canceled += OnSprintEnd;
    }

    public void Subscribe(string key, Action<CallbackContext> action)
    {
        if (action == null)
            return;
        inputList.TryGetValue(key, out var value);
        value -= action;
        inputList[key] = value + action;
    }

    public void Unsubscribe(string key, Action<CallbackContext> action)
    {
        if (!inputList.TryGetValue(key, out var value))
            return;
        value -= action;
        if (value == null)
            inputList.Remove(key);
        else
            inputList[key] = value;
    }

    private void ReleaseInputActions()
    {
        if (leftAction != null)
            leftAction.performed -= OnLeft;
        if (rightAction != null)
            rightAction.performed -= OnRight;
        if (rotateAction != null)
            rotateAction.performed -= OnRotate;
        if (sprintAction != null)
        {
            sprintAction.performed -= OnSprint;
            sprintAction.canceled -= OnSprintEnd;
        }
        leftAction = null;
        rightAction = null;
        rotateAction = null;
        sprintAction = null;
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
