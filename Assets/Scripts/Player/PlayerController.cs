using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private PlayerInput playerInput;
    private CharacterController cc;
    private HopakAnimation hopakAnim;

    [SerializeField]
    private bool comboAvailable = true;
    [SerializeField] private int comboCnt = 0;


    [SerializeField] private float comboDuration = defaultDuration;

    private bool leftPressed = false;

    [SerializeField]
    [Header("Combo timeout until the next combo")]
    private float comboTimeout = 0.3f;
    [SerializeField]
    private const float defaultDuration = 0.4f;

    [SerializeField]
    [Header("Mouse Seneitivity")]
    private float mouseSensitivity = 1f;
    [SerializeField]
    [Header("Current speed [Debug]")]
    private float speed;

    [SerializeField]
    [Header("deceleration smooth curve")]
    private AnimationCurve decelerationCurve;

    [SerializeField]
    [Header("Duration time to stop")]
    float stopDuration = 1f;

    [SerializeField] float decelerationTime;

    [SerializeField]
    [Header("Increase speed per each combo")]
    private float increseSpeedPerCombo;


    [SerializeField]
    [Header("Rotate speed with use keyboard arrows")]
    [Range(0.1f, 2f)]
    private float rotSpeed = 1f;

    [SerializeField]
    [Header("Decrease combo duration per combo")]
    [Range(0.98f, 0.999f)] private float comboDurationDecayRate = 0.99f;
    private float curRotateInput;
    bool isDecelerating;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        hopakAnim = GetComponent<HopakAnimation>();
        playerInput.actions["Left"].performed += OnLeft;
        playerInput.actions["Right"].performed += OnRight;
        playerInput.actions["Rotate"].performed += OnRotate;
        playerInput.actions["Turn"].performed += OnTurn;
        playerInput.actions["Turn"].canceled += OnTurnEnd;

    }

    void Update()
    {
        Decelerating();
        transform.Rotate(0, curRotateInput * rotSpeed, 0);
        cc.Move(transform.forward * speed * Time.deltaTime);
    }

    private void Decelerating()
    {
        if (isDecelerating)
        {
            if (comboCnt != 0)
            {
                isDecelerating = false;
            }
            else
            {
                decelerationTime += Time.deltaTime;

                float t = Mathf.Clamp01(decelerationTime / stopDuration);
                speed *= decelerationCurve.Evaluate(t);

                if (t >= 1f)
                {
                    speed = 0f;
                    isDecelerating = false;
                    return;
                }
            }
        }
    }


    private void BreakCombo()
    {
        decelerationTime = 0f;
        isDecelerating = true;
        comboCnt = 0;
        comboDuration = defaultDuration;
    }

    private IEnumerator WaitCombo()
    {
        comboAvailable = false;
        yield return new WaitForSeconds(comboDuration);
        comboAvailable = true;
        StartCoroutine(WaitComboTimeout(comboCnt));
    }

    private IEnumerator WaitComboTimeout(int prevComboCnt)
    {
        float timeout = 0f;
        while (true)
        {
            if (prevComboCnt < comboCnt)
                yield break;
            if (timeout > comboTimeout)
            {
                BreakCombo();
                yield break;
            }
            timeout += Time.deltaTime;
            yield return null;
        }
    }


    private void IncreaseSpeed()
    {
        speed = Mathf.Clamp(speed + increseSpeedPerCombo, 3f, float.MaxValue);
    }

    #region Input

    private void OnLeft(CallbackContext context)
    {
        if (leftPressed || !comboAvailable)
        {
            BreakCombo();
            return;
        }

        InCreaseCombo(true);
    }

    private void OnRight(CallbackContext context)
    {
        if (!leftPressed || !comboAvailable)
        {
            BreakCombo();
            return;
        }

        InCreaseCombo(false);
    }

    private void InCreaseCombo(bool left)
    {
        comboCnt++;
        leftPressed = left;
        IncreaseSpeed();
        hopakAnim.PlayAnimation(leftPressed, comboDuration);
        comboDuration *= comboDurationDecayRate;
        StartCoroutine(WaitCombo());
    }


    private void OnRotate(CallbackContext context)
    {
        Vector2 mouse = context.ReadValue<Vector2>();
        float mouseX = mouse.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(0, mouseX, 0);
    }


    private void OnTurn(CallbackContext context)
    {
        curRotateInput = context.ReadValue<Vector2>().x;
    }

    private void OnTurnEnd(CallbackContext context)
    {
        curRotateInput = 0f;
    }
    #endregion
}
