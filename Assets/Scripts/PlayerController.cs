using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private PlayerInput playerInput;
    private Rigidbody rigid;


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


    void Start()
    {
        rigid = GetComponent<Rigidbody>();  
        playerInput.actions["Left"].performed += OnLeft;
        playerInput.actions["Right"].performed += OnRight;
        playerInput.actions["Rotate"].performed += OnRotate;        
    }

    private void InitVelocity()
    {
        rigid.linearVelocity = Vector3.zero;
        comboCnt = 0;
        comboTimeout = 0.3f;
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
                InitVelocity();
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
            InitVelocity();
            return;
        }
        comboCnt++;
        leftPressed = true;


        float velocity = Mathf.Clamp(comboCnt / 50f, 1, int.MaxValue);
        rigid.AddForce(transform.forward * velocity, ForceMode.Impulse);

        comboDuration *= 0.99f;
        StartCoroutine(WaitCombo());
    }

    private void OnRight(InputAction.CallbackContext context)
    {
        if (!leftPressed || !CombeAvailable)
        {
            InitVelocity();
            return;
        }
        comboCnt++;
        leftPressed = false;

        float velocity = Mathf.Clamp(comboCnt / 50f, 1, int.MaxValue);
        rigid.AddForce(transform.forward * velocity, ForceMode.Impulse);

        comboDuration *= 0.99f;
        StartCoroutine(WaitCombo());
    }

    private void OnRotate(InputAction.CallbackContext context)
    {
        Debug.Log(context.ReadValue<Vector2>());
        Vector2 mouse = context.ReadValue<Vector2>();
        float mouseX = mouse.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(0, mouseX, 0);
    }

    void Update()
    {
        
    }
}
