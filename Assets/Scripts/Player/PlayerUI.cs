using DG.Tweening;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Image comboGuide;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private float textPunchScale = 1.3f;
    [SerializeField] private TextMeshProUGUI currentInfection;
    [SerializeField] private Transform rankRoot;
    [SerializeField] private UI_Rank[] rank;
    [SerializeField] private TextMeshProUGUI dieText;
    [SerializeField] private Image dashFilled;
    private float desireFilled;

     


    private Color guideOriginColor;
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
