using FishNet.Object;
using System.Collections;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;


public enum GameState
{
    Playing, Sprint, Jumping, Idle
}

public class PlayerController : NetworkBehaviour
{

    //Instance
    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private PlayerInputManager playerInputManager;
    [SerializeField] private CharacterController cc;
    [SerializeField] private HopakAnimation hopakAnim;


    //private field
    private bool comboAvailable = true;
    private bool leftPressed = false;

    [SerializeField] private int comboCnt = 0;
    [SerializeField] private float defaultDuration = 0.4f;
    [SerializeField] private float comboDuration;


    [SerializeField] private float comboTimeout = 0.3f;
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
    private GameObject hopakPlayer;
    [SerializeField]
    private float sprintDuration = 5f;
    private float curRotateInput;
    bool isDecelerating;
    private int bonusHeart;
    private Vector3 jumpDir;
    private float speedModifier = 1f;

    [SerializeField] private float sprintCooldown = 15f;


    private GameState CurrentState;
    void Start()
    {
        Cursor.visible = false;

    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
        {
            gameObject.name = "Loacl Player";
        }
        else
        {
            gameObject.name = "Remote Player";
            Destroy(this);
        }

        Init();
    }

    public void GameOver() => CurrentState = GameState.Idle;

    #region Initialize 

    public void Init()
    {
        //Component initialization
        hopakAnim = GetComponent<HopakAnimation>();

        SubscribeInput();
        CurrentState = GameState.Playing;
    }

    private void SubscribeInput()
    {
        playerInputManager.Subscribe("Left", Left);
        playerInputManager.Subscribe("Right", Right);
        playerInputManager.Subscribe("Rotate", Rotate);
        playerInputManager.Subscribe("Sprint", Sprint);
    }

    #endregion

    void Update()
    {
        if (CurrentState == GameState.Idle)
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
    private void IncreaseSpeed()
    {
        speed = Mathf.Clamp(speed + increseSpeedPerCombo, 3f, float.MaxValue);
        maxSpeed = Mathf.Max(maxSpeed, speed);
        playerUI.SetSpeed(speed * speedModifier);
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

    #region Coroutine
    private IEnumerator WaitCombo() // 다음콤보 타이밍까지 대기
    {
        comboAvailable = false;
        yield return new WaitForSeconds(comboDuration);
        comboAvailable = true;
        StartCoroutine(WaitComboTimeout(comboCnt));
    }

    private IEnumerator WaitComboTimeout(int prevComboCnt) // 콤보 유예시간동안 대기
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
    private IEnumerator OnSprint(float duration)
    {
        CurrentState = GameState.Sprint;
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

        if(CurrentState != GameState.Idle)
            CurrentState = GameState.Playing;
    }

    #endregion


    #region Action Event
    private void Right(CallbackContext context)
    {
        if (CurrentState == GameState.Idle)
            return;
        if (!leftPressed || !comboAvailable)
        {
            BreakCombo();
            return;
        }

        InCreaseCombo(false);
    }
    private void Left(CallbackContext context)
    {
        if (CurrentState == GameState.Idle)
            return;
        if (leftPressed || !comboAvailable)
        {
            BreakCombo();
            return;
        }

        InCreaseCombo(true);
    }
    private void Rotate(CallbackContext context)
    {
        if (CurrentState == GameState.Idle)
            return;
        Vector2 mouse = context.ReadValue<Vector2>();
        float mouseX = mouse.x * mouseSensitivity * Time.deltaTime;
        transform.parent.Rotate(0, mouseX, 0);
    }
    private void Sprint(CallbackContext context)
    {
        if (CurrentState != GameState.Idle && CurrentState != GameState.Sprint)
            return;
        StartCoroutine(OnSprint(sprintDuration));
    }
    #endregion
}