using FishNet.Object;
using System.Collections;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;


public enum PlayerState
{
    Playing, Sprint, Jumping, Idle
}

public class PlayerController : NetworkBehaviour
{

    //Instance
    public PlayerUI playerUI;
    [SerializeField] private PlayerInputManager playerInputManager;
    [SerializeField] private CharacterController cc;
    [SerializeField] private HopakAnimation hopakAnim;
    [SerializeField] private NetworkPlayer networkPlayer;


    //private field
    private bool comboAvailable = true;
    private bool leftPressed = false;

    [SerializeField] private int comboCnt = 0;
    [SerializeField] private float defaultDuration = 0.4f;
    [SerializeField] private float comboDuration;


    [SerializeField] private float comboTimeout = 0.3f;
    [SerializeField]

    [Header("Mouse Seneitivity")]
    private float mouseSensitivity = 1f;
    [SerializeField]
    [Header("Current speed [Debug]")]
    public float speed;

    public float maxSpeed = 0f;
    public float defaultSpeed = 5f;

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
    [Range(0.98f, 0.999f)] private float comboDurationDecayRate = 0.995f;

    [SerializeField] private float availableDashTime = 0f;
    [SerializeField] private float maxDashTime = 10f;

    [SerializeField]
    private GameObject hopakPlayer;
    [SerializeField]
    private float curRotateInput;
    bool isDecelerating;
    private float speedModifier = 1f;

    [SerializeField] GameObject[] trails;


    private PlayerState CurrentState = PlayerState.Idle;
    private bool inputSubscribed;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
        {
            gameObject.name = "Remote Player";
            enabled = false;
            return;
        }

        gameObject.name = "Local Player";
        Cursor.visible = false;
        Init();
    }

    public override void OnStopClient()
    {
        GameOver();
        base.OnStopClient();
    }

    private void OnDisable()
    {
        GameOver();
    }

    private void OnDestroy()
    {
        UnsubscribeInput();
    }

    public void GameOver()
    {
        CurrentState = PlayerState.Idle;
        speed = 0f;
        curRotateInput = 0f;
        speedModifier = 1f;
        UnsubscribeInput();
        StopAllCoroutines();
        foreach (GameObject trail in trails)
        {
            if (trail != null)
                trail.SetActive(false);
        }
    }

    #region Initialize 

    public void Init()
    {
        if (!IsOwner || !IsClientInitialized || !isActiveAndEnabled)
            return;
        //Component initialization
        hopakAnim = GetComponent<HopakAnimation>();
        if (networkPlayer == null)
            networkPlayer = GetComponentInParent<NetworkPlayer>();

        SubscribeInput();
        CurrentState = PlayerState.Playing;
    }

    private void SubscribeInput()
    {
        if (inputSubscribed || playerInputManager == null)
            return;
        playerInputManager.Subscribe("Left", Left);
        playerInputManager.Subscribe("Right", Right);
        playerInputManager.Subscribe("Rotate", Rotate);
        playerInputManager.Subscribe("Sprint", Sprint);
        playerInputManager.Subscribe("SprintEnd", SprintEnd);
        inputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (playerInputManager != null)
        {
            playerInputManager.Unsubscribe("Left", Left);
            playerInputManager.Unsubscribe("Right", Right);
            playerInputManager.Unsubscribe("Rotate", Rotate);
            playerInputManager.Unsubscribe("Sprint", Sprint);
            playerInputManager.Unsubscribe("SprintEnd", SprintEnd);
        }
        inputSubscribed = false;
    }

    #endregion

    void Update()
    {
        if (!IsOwner || !IsClientInitialized || CurrentState == PlayerState.Idle)
            return;
        if(CurrentState != PlayerState.Sprint)
        {
            Decelerating();
        }
        else if(CurrentState == PlayerState.Sprint)
        {
            if(availableDashTime < 0f)
            {
                availableDashTime = 0f;
                CurrentState = PlayerState.Playing;
                StopSprint();
            }
            else
            {
                availableDashTime -= Time.deltaTime;
                playerUI.UpdateDashGauge(availableDashTime / maxDashTime);
            }
        }
        transform.Rotate(0, curRotateInput * rotSpeed, 0);

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
                if(speed < defaultSpeed)
                    speed = defaultSpeed;
                playerUI.SetSpeed(speed * speedModifier);

                if (t >= 1f)
                {
                    speed = defaultSpeed;
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
    private void IncreaseSpeed() // TODO 대쉬 게이지 증가로 변경
    {
        speed = maxSpeed;
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

        if(CurrentState != PlayerState.Sprint && availableDashTime < maxDashTime)
        {
            availableDashTime += 0.05f + comboCnt / 1500f;
            if (availableDashTime > maxDashTime)
                availableDashTime = maxDashTime;

            playerUI.UpdateDashGauge(availableDashTime / maxDashTime);
        }
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

    #endregion


    #region Action Event
    private void Right(CallbackContext context)
    {
        if (CurrentState == PlayerState.Idle)
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
        if (CurrentState == PlayerState.Idle)
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
        if (CurrentState == PlayerState.Idle)
            return;
        Vector2 mouse = context.ReadValue<Vector2>();
        float mouseX = mouse.x * mouseSensitivity * Time.deltaTime;
        transform.parent.Rotate(0, mouseX, 0);
    }
    private void Sprint(CallbackContext context)
    {
        if (CurrentState == PlayerState.Idle)
            return;
        if (CurrentState == PlayerState.Sprint)
            return;
        CurrentState = PlayerState.Sprint;
        speedModifier = 2f;
        foreach(var trail in trails)
            trail.SetActive(true);
        networkPlayer.StartSprint();
    }
    private void SprintEnd(CallbackContext context)
    {
        if (CurrentState == PlayerState.Idle)
            return;
        StopSprint();
    }

    private void StopSprint()
    {
        CurrentState = PlayerState.Playing;
        speedModifier = 1f;
        foreach (var trail in trails)
            trail.SetActive(false);
        if (IsClientInitialized && IsOwner && networkPlayer != null)
            networkPlayer.StopSprint();
    }
    #endregion
}
