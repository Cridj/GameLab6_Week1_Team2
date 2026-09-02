using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public class RewardPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI infectionReward;
    [SerializeField] private TextMeshProUGUI highSpeedReward;
    [SerializeField] private TextMeshProUGUI infectionRatioReward;
    [SerializeField] private TextMeshProUGUI finalReward;

    [SerializeField] private CanvasGroup mainCanvasGroup;
    [SerializeField] private CanvasGroup infectionCanvasGroup;
    [SerializeField] private CanvasGroup plusCanvanGroup;
    [SerializeField] private CanvasGroup highSpeedCanvasGroup;
    [SerializeField] private CanvasGroup mulCanvanGroup;
    [SerializeField] private CanvasGroup infectionRatioCanvasGroup;
    [SerializeField] private CanvasGroup lineCanvanGroup;
    [SerializeField] private CanvasGroup finalRewardCanvasGroup;


    [SerializeField] private float waitTime = 0.5f;
    [SerializeField] private FollowerManager followerManager;
    [SerializeField] private PlayerController playerController;

    private void OnEnable()
    {
        mainCanvasGroup.alpha = 0f;
        infectionCanvasGroup.alpha = 0f;
        plusCanvanGroup.alpha = 0f;
        highSpeedCanvasGroup.alpha = 0f;
        mulCanvanGroup.alpha = 0f;
        infectionRatioCanvasGroup.alpha = 0f;
        lineCanvanGroup.alpha = 0f;
        finalRewardCanvasGroup.alpha = 0f;
        StartCoroutine(OnReward());
    }

    [ContextMenu("RewardTest")]
    public void RewardTest()
    {
        mainCanvasGroup.alpha = 0f;
        infectionCanvasGroup.alpha = 0f;
        plusCanvanGroup.alpha = 0f;
        highSpeedCanvasGroup.alpha = 0f;
        mulCanvanGroup.alpha = 0f;
        infectionRatioCanvasGroup.alpha = 0f;
        lineCanvanGroup.alpha = 0f;
        finalRewardCanvasGroup.alpha = 0f;
        StartCoroutine(OnReward());
    }

    public void GetRewards()
    {

    }

    private IEnumerator OnReward()
    {
        DOTween.To(() => mainCanvasGroup.alpha, x => mainCanvasGroup.alpha = x, 1f, 0.5f);
        yield return new WaitForSeconds(waitTime);

        infectionCanvasGroup.alpha = 1f;
        StartCoroutine(Count(followerManager.FollowerCnt, 0, infectionReward));

        yield return new WaitForSeconds(waitTime);

        DOTween.To(() => plusCanvanGroup.alpha, x => plusCanvanGroup.alpha = x, 1f, 0.5f);
        yield return new WaitForSeconds(0.8f);

        highSpeedCanvasGroup.alpha = 1f;
        StartCoroutine(Count(playerController.maxSpeed, 0, highSpeedReward));

        yield return new WaitForSeconds(waitTime);

        DOTween.To(() => mulCanvanGroup.alpha, x => mulCanvanGroup.alpha = x, 1f, 0.5f);
        yield return new WaitForSeconds(0.8f);

        infectionRatioCanvasGroup.alpha = 1f;
        StartCoroutine(Count(1.05f, 0, infectionRatioReward, true));

        yield return new WaitForSeconds(waitTime);


        DOTween.To(() => lineCanvanGroup.alpha, x => lineCanvanGroup.alpha = x, 1f, 0.5f);
        yield return new WaitForSeconds(0.8f);

        finalRewardCanvasGroup.alpha = 1f;
        float total = (followerManager.FollowerCnt + playerController.maxSpeed) * 1.05f;

        StartCoroutine(Count(Mathf.FloorToInt(total), 0, finalReward));
    }

    IEnumerator Count(float target, float current, TextMeshProUGUI text, bool useDecimal = false)

    {

        float duration = 0.5f; // 카운팅에 걸리는 시간 설정. 

        float offset = (target - current) / duration;



        while (current < target)

        {

            current += offset * Time.deltaTime;

            if(useDecimal)
                text.text = current.ToString("F2");
            else
                text.text = ((int)current).ToString("F0");

            yield return null;

        }



        current = target;

        if (useDecimal)
            text.text = current.ToString("F2");
        else
            text.text = ((int)current).ToString("F0");
    }
}
