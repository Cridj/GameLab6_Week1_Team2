using DG.Tweening;
using GameLab.Rhythm;
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

    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private SpotlightRhythmGame rhythmGame;
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
    [Header("Mouse Sensitivity")]
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


    // 감속 관련 변수인 듯 
    [SerializeField]
    [Header("Decrease combo duration per combo")]
    [Range(0.98f, 0.999f)] private float comboDurationDecayRate = 0.99f;

    [SerializeField]
    [Header("Jump Power")]
    private float jumpPower = 7.5f;

    [SerializeField]
    private GameObject hopakPlayer;
    [SerializeField]
    private float sprintDuration = 5f;
    private float curRotateInput;
    bool isDecelerating;
    private Vector3 jumpDir;
    private float speedModifier = 1f;

    [SerializeField] private float sprintCooldown = 15f;

    private bool isStarted = false;


    void Start()
    {
        Cursor.visible = false;
        Init();
    }


    public void GameOver()
    {
        playerInput.actions["Left"].performed -= OnLeft;
        playerInput.actions["Right"].performed -= OnRight;
        playerInput.actions["Rotate"].performed -= OnRotate;
        playerInput.actions["Turn"].performed -= OnTurn;
        playerInput.actions["Turn"].canceled -= OnTurnEnd;
        playerInput.actions["Jump"].performed -= OnJump;
        playerInput.actions["Sprint"].performed -= OnSprint;

        if (rhythmGame != null)
        {
            rhythmGame.Judged -= OnRhythmJudged;
        }
    }

    public void Init()
    {
        //Component initialization
        hopakAnim = GetComponent<HopakAnimation>();

        if (rhythmGame == null)
        {
            rhythmGame = FindFirstObjectByType<SpotlightRhythmGame>();
        }

        //Input action binding
        playerInput.actions["Left"].performed += OnLeft;
        playerInput.actions["Right"].performed += OnRight;
        playerInput.actions["Rotate"].performed += OnRotate;
        playerInput.actions["Turn"].performed += OnTurn;
        playerInput.actions["Turn"].canceled += OnTurnEnd;

        playerInput.actions["Jump"].performed += OnJump;
        playerInput.actions["Sprint"].performed += OnSprint;

        if (rhythmGame != null)
        {
            rhythmGame.Judged -= OnRhythmJudged;
            rhythmGame.Judged += OnRhythmJudged;
        }

        isStarted = true;
    }

    void Update()
    {
        if (!isStarted)
            return;
        Decelerating();
        transform.Rotate(0, curRotateInput * rotSpeed, 0);

        if (rhythmGame != null && rhythmGame.IsRunning)
        {
            playerUI.UpdateExpectedKey(rhythmGame.ExpectedLane);
        }
    }

    private void Decelerating()
    {
        if (isDecelerating)
        {
            // 손을 떼고 있는 모든 순간 어느정도 감속을 하기 때문에, 콤보가 끊겨서 감속하는 건지 원래 감속하는 건지 구분.
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
        playerUI.ComboBreak();
    }

    //private IEnumerator WaitCombo()
    //{
    //    comboAvailable = false;
    //    yield return new WaitForSeconds(comboDuration);
    //    comboAvailable = true;
    //    StartCoroutine(WaitComboTimeout(comboCnt));
    //}

    //private IEnumerator WaitComboTimeout(int prevComboCnt)
    //{
    //    float timeout = 0f;
    //    while (true)
    //    {
    //        if (prevComboCnt < comboCnt)
    //            yield break;
    //        if (timeout > comboTimeout)
    //        {
    //            BreakCombo();
    //            yield break;
    //        }
    //        timeout += Time.deltaTime;
    //        yield return null;
    //    }
    //}

    private IEnumerator Windmill(float duration)
    {
        Debug.Log("Windmill!");
        hopakAnim.PlayWindmill(duration);

        yield return new WaitForSeconds(duration);
    }

    #region Input

    private void OnLeft(CallbackContext context)
    {
        if (rhythmGame != null && rhythmGame.IsRunning)
        {
            rhythmGame.PressLeft();
            return;
        }

        if (leftPressed || !comboAvailable)
        {
            BreakCombo();
            return;
        }

        InCreaseCombo(true);
    }

    private void OnRight(CallbackContext context)
    {
        if (rhythmGame != null && rhythmGame.IsRunning)
        {
            rhythmGame.PressRight();
            return;
        }

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
        hopakAnim.PlayAnimation(leftPressed, comboDuration);
        //comboDuration *= comboDurationDecayRate;
        //StartCoroutine(WaitCombo()); 
    }

    private void OnRhythmJudged(RhythmJudgementResult result)
    {
        switch (result.Judgement)
        {
            case RhythmJudgement.Perfect:
            case RhythmJudgement.Great:
            case RhythmJudgement.Good:
                InCreaseCombo(result.ExpectedLane == RhythmLane.Left);
                break;

            default:
                BreakCombo();
                break;
        }

        playerUI.UpdateRhythmResult(result);
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

    }

    private bool isJumping = false;
    private void OnJump(CallbackContext context)
    {
        isJumping = true;
        hopakPlayer.transform.DOLocalJump(Vector3.zero, 2.5f, 1, 0.8f).OnComplete(()=> isJumping = false);
    }
    #endregion

}
