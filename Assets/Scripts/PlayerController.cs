using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private PlayerInput playerInput;
    private CharacterController cc;

    [SerializeField]
    private bool comboAvailable = true;
    private bool CombeAvailable
    {
        get
        {
            return comboAvailable;
        }         
        set
        {
            if(value == true)
            {

            }
            comboAvailable = value;
        }
    }
    [SerializeField] private int comboCnt = 0;
    [SerializeField] private float comboTimeout = 0.3f;
    [SerializeField] private const float defaultDuration = 0.4f;

    [SerializeField] private float comboDuration = defaultDuration;
    [SerializeField] private float mouseSensitivity = 1f;
    private bool leftPressed = false;


    [SerializeField] private float speed;
    [SerializeField] private AnimationCurve decelerationCurve;
    [SerializeField] float stopDuration = 1f;
    [SerializeField] float decelerationTime;
    [SerializeField] private float increseSpeedPerCombo;
    bool isDecelerating;

    void Update()
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
        cc.Move(transform.forward * speed * Time.deltaTime);
    }


    void Start()
    {
        cc = GetComponent<CharacterController>();
        playerInput.actions["Left"].performed += OnLeft;
        playerInput.actions["Right"].performed += OnRight;
        playerInput.actions["Rotate"].performed += OnRotate;        
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
        CombeAvailable = false;
        yield return new WaitForSeconds(comboDuration);
        CombeAvailable = true;
        StartCoroutine(WaitComboTimeout(comboCnt));
    }

    private IEnumerator WaitComboTimeout(int prevComboCnt)
    {
        float timeout = 0f;
        while (true)
        {
            if (prevComboCnt < comboCnt)
                yield break;
            if(timeout > comboTimeout)
            {
                BreakCombo();
                yield break;
            }
            timeout += Time.deltaTime;
            yield return null;
        }
    }

    private void OnLeft(InputAction.CallbackContext context)
    {
        if(leftPressed || !CombeAvailable)
        {
            BreakCombo();
            return;
        }
        comboCnt++;
        leftPressed = true;
        IncreaseSpeed();

        comboDuration *= 0.99f;
        StartCoroutine(WaitCombo());
    }

    private void OnRight(InputAction.CallbackContext context)
    {
        if (!leftPressed || !CombeAvailable)
        {
            BreakCombo();
            return;
        }
        comboCnt++;
        leftPressed = false;
        IncreaseSpeed();

        comboDuration *= 0.99f;
        StartCoroutine(WaitCombo());
    }

    private void IncreaseSpeed()
    {
        speed = Mathf.Clamp(speed + increseSpeedPerCombo, 3f, float.MaxValue);
    }

    private void OnRotate(InputAction.CallbackContext context)
    {
        Debug.Log(context.ReadValue<Vector2>());
        Vector2 mouse = context.ReadValue<Vector2>();
        float mouseX = mouse.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(0, mouseX, 0);
    }
}
