using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Image comboGuide;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI speedText;

    [SerializeField] private float textPunchScale = 1.3f;
    private Color guideOriginColor;
    private void Start()
    {
        guideOriginColor = comboGuide.color;
    }

    public void SetSpeed(float speed)
    {
        speedText.text = speed.ToString("0") + " km/h";
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
        else if (cnt == 150)
        {
            comboText.color = Color.blue;
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
