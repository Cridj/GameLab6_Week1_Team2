using DG.Tweening;
using System.Collections;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Image comboGuide;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private float textPunchScale = 1.3f;
    [SerializeField] private TextMeshProUGUI currentInfection;
    [SerializeField] private Transform rankRoot;
    [SerializeField] private UI_Rank[] rank;
    [SerializeField] private TextMeshProUGUI dieText;
    [SerializeField] private Image dashFilled;
    [SerializeField] private TextMeshProUGUI killlogTextPrefab;
    [SerializeField] private Transform killlogRoot;
    private float desireFilled;
    private Coroutine scrollCoroutine;

     


    private Color guideOriginColor;
    private void Awake()
    {
        if (scrollRect == null && killlogRoot != null)
            scrollRect = killlogRoot.GetComponentInParent<ScrollRect>(true);
        if (scrollRect != null && scrollRect.content == null)
            scrollRect.content = killlogRoot as RectTransform;
    }

    private void Start()
    {
        guideOriginColor = comboGuide.color;
    }

    public void SetSpeed(float speed)
    {
        speedText.text = speed.ToString("0.0") + " km/h";
    }

    public void UpdateFollowerUI(int cnt)
    {
        currentInfection.text = cnt.ToString();
        currentInfection.transform.DOPunchScale(Vector3.one * 0.4f, 0.1f).OnComplete(()=> currentInfection.transform.localScale = Vector3.one);
    }

    public void AddKillLog(string target, string instigator)
    {
        var text = Instantiate(killlogTextPrefab, killlogRoot);

        int ran = Random.Range(0, 3);

        if(ran == 0)
            text.text = $"{target}님이 {instigator}님에게 무참히 살해당하였습니다.";
        else if(ran == 1)
            text.text = $"{instigator}님이 {target}님에게 정의를 실현하였습니다.";
        else
            text.text = $"{instigator}님이 {target}님을 무자비하게 짓밟았습니다.";
        text.transform.SetAsLastSibling();
        RefreshCanvas();
    }
    public void RefreshCanvas()
    {
        if (!isActiveAndEnabled)
            return;
        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);
        scrollCoroutine = StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom()
    {
        yield return null;
        if (killlogRoot is RectTransform content)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 0f;
        }
        scrollCoroutine = null;
    }

    public void AddDisconnectedLog(string target)
    {
        var text = Instantiate(killlogTextPrefab, killlogRoot);
        text.text = $"{target}님이 우주로 떠났습니다.";
        text.transform.SetAsLastSibling();
        RefreshCanvas();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        scrollCoroutine = null;
        if (currentInfection != null)
            DOTween.Kill(currentInfection.transform);
        if (comboGuide != null)
        {
            DOTween.Kill(comboGuide);
            DOTween.Kill(comboGuide.transform);
        }
        if (comboText != null)
            DOTween.Kill(comboText.transform);
    }
    private void ShowComboGuide(float duration = 0.4f, float timeout = 0.5f)
    {
        DOTween.Kill(comboGuide.transform);
        comboGuide.color = new Color(guideOriginColor.a, guideOriginColor.g, guideOriginColor.b, 0f);
        comboGuide.DOColor(new Color(guideOriginColor.a, guideOriginColor.g, guideOriginColor.b, 1f), duration);
        comboGuide.transform.localScale = Vector3.one * 1.3f;
        comboGuide.transform.DOScale(0.5f, duration).OnComplete(() => comboGuide.transform.DOScale(Vector3.one * 0.3f, timeout));
        comboGuide.gameObject.SetActive(true);
    }

    public void UpdateDieText(string ownerName)
    {
        dieText.text = "당신은 " + ownerName + "에게 무참히 살해당했습니다.";
    }


    public void UpdateLeaderboard(RankingInfo[] info)
    {
        var sort = info
            .OrderByDescending(data => int.TryParse(data.score, out int score) ? score : 0)
            .ThenBy(data => data.name)
            .ToArray();

        int count = Mathf.Min(rank.Length, sort.Length);
        for (int i = 0; i < count; i++)
        {
            rank[i].nickName.text = sort[i].name;
            rank[i].score.text = sort[i].score;
        }
    }

    public void UpdateDashGauge(float filled)
    {
        desireFilled = filled;
    }

    private void Update()
    {
        dashFilled.fillAmount = Mathf.Lerp(dashFilled.fillAmount, desireFilled, 10f * Time.deltaTime);
    }


    private void UpdateComboText(int cnt, float duration)
    {
        comboText.text = cnt.ToString();
        comboText.transform.DOPunchScale(Vector3.one * textPunchScale, duration);
        if (cnt == 1)
        {
            comboText.color = Color.black;
        }
        if (cnt == 10)
        {
            comboText.color = Color.red;
        }
        else if (cnt == 30)
        {
            comboText.color = Color.yellow;
        }
        else if (cnt == 80)
        {
            comboText.color = Color.green;
        }
        else if (cnt == 130)
        {
            comboText.color = Color.blue;
        }
        else if(cnt == 200)
        {
            comboText.color = Color.magenta;
        }
    }

    public void ComboUpdate(float comboDuration, int comboCnt, float comboTimeout)
    {
        ShowComboGuide(comboDuration, comboTimeout);
        UpdateComboText(comboCnt, comboDuration);
    }

    public void ComboBreak()
    {
        comboText.text = "0";
        comboGuide.gameObject.SetActive(false);
    }
}
