using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;
using static UnityEngine.ParticleSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private PlayerInput playerInput;
    private PlayerAbility playerAbility;

    [SerializeField] private PlayerUI playerUI;
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
    private TrailRenderer trail;

    [SerializeField]
    [Header("Mouse Seneitivity")]
    private float mouseSensitivity = 1f;
    [SerializeField]
    [Header("Current speed [Debug]")]
    public float speed;

    public float maxSpeed = 0f;

    public float Speed { get; private set; }

    [SerializeField]
    [Header("Deceleration smooth curve")]
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

    [SerializeField]
    [Header("Jump Power")]
    private float jumpPower = 7.5f;

    [SerializeField]
    private GameObject[] hopakJuniors;
    [SerializeField]
    private GameObject hopakPlayer;
    [SerializeField]
    private float sprintDuration = 5f;
    private float curRotateInput;
    bool isDecelerating;
    private int bonusHeart;
    private Vector3 jumpDir;
    private float speedModifier = 1f;

    [SerializeField] private float sprintCooldown = 15f;
    private bool isSprint = false;

    //Ability levels
    private int sprintLevel;
    private int jumpLevel;
    private int spitLevel;
    private bool isStarted = false;


    void Start()
    {
        Cursor.visible = false;
    }


    public void DisableInput()
    {
        playerInput.actions["Left"].performed -= OnLeft;
        playerInput.actions["Right"].performed -= OnRight;
        playerInput.actions["Rotate"].performed -= OnRotate;
        playerInput.actions["Turn"].performed -= OnTurn;
        playerInput.actions["Turn"].canceled -= OnTurnEnd;
        playerInput.actions["Jump"].performed -= OnJump;
        playerInput.actions["Sprint"].performed -= OnSprint;
    }

    public void Init()
    {
        //Component initialization
        cc = GetComponent<CharacterController>();
        hopakAnim = GetComponent<HopakAnimation>();
        playerAbility = GetComponent<PlayerAbility>();

        //Input action binding
        playerInput.actions["Left"].performed += OnLeft;
        playerInput.actions["Right"].performed += OnRight;
        playerInput.actions["Rotate"].performed += OnRotate;
        playerInput.actions["Turn"].performed += OnTurn;
        playerInput.actions["Turn"].canceled += OnTurnEnd;

        playerInput.actions["Jump"].performed += OnJump;
        playerInput.actions["Sprint"].performed += OnSprint;

        //ability initialization
        {
            if (GameInstance.Instance.commonAbilities.TryGetValue(CommonAbilityType.Growing, out int value)) // 거대화
            {
                foreach (var hopak in hopakJuniors)
                {
                    hopak.transform.localScale *= 1.3f * value;
                }
                hopakPlayer.transform.localScale *= 1.3f;
            }
            foreach (var ability in GameInstance.Instance.commonAbilities)
            {
                switch (ability.Key)
                {
                    case CommonAbilityType.HopakJunior: // 호팍 주니어
                        for (int i = 0; i < ability.Value; i++)
                        {
                            hopakJuniors[i].SetActive(true);
                            hopakJuniors[i].transform.DOPunchScale(Vector3.one * 0.8f, 1f);
                        }
                        break;
                    case CommonAbilityType.Restoration: // 바이러스 회복
                        bonusHeart = ability.Value;
                        break;
                    case CommonAbilityType.Sprint:
                        sprintLevel = ability.Value;
                        break;
                    case CommonAbilityType.Jump:
                        jumpLevel = ability.Value;
                        break;
                }
            }
        }
        isStarted = true;
    }

    void Update()
    {
        if (!isStarted)
            return;
        Decelerating();
        transform.Rotate(0, curRotateInput * rotSpeed, 0);

        jumpDir.y += Physics.gravity.y * Time.deltaTime;
        cc.Move(transform.forward * speedModifier * speed * Time.deltaTime);
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
                playerUI.SetSpeed(speed * speedModifier);

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
        playerUI.ComboBreak();
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
        maxSpeed = Mathf.Max(maxSpeed, speed);
        playerUI.SetSpeed(speed * speedModifier);
    }

    private IEnumerator Sprint(float duration)
    {
        isSprint = true;
        trail.enabled = true;
        speedModifier = 1.5f;
        if (GameInstance.Instance.hiddenAbilities.TryGetValue(HiddenAbilityType.Windmill, out var value))
        {
            Debug.Log("Windmill!");
            hopakAnim.PlayWindmill(duration);
            //TODO 윈드밀 특수효과 추가
        }
        else
        {
            Debug.Log("Sprint!");
        }

        yield return new WaitForSeconds(duration);



        speedModifier = 1f;
        trail.enabled = false;
        yield return new WaitForSeconds(sprintCooldown);
        isSprint = false;
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
        Managers.Instance.Sound.PlayComboSound();
        comboCnt++;
        playerUI.ComboUpdate(comboDuration, comboCnt, comboTimeout);
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

    private void OnSprint(CallbackContext context)
    {
        if (isSprint)
            return;
        if (sprintLevel > 0)
        {
            StartCoroutine(Sprint(sprintDuration));
        }
    }

    private void OnJump(CallbackContext context)
    {
        hopakPlayer.transform.DOLocalJump(Vector3.zero, 2.5f, 1, 0.8f);
    }
    #endregion

}
