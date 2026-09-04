using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    public SerializedDictionary<string, int> mutationCnt = new();

    private int reRollCost = 100;

    public MutationInfo mutationInfo;
    public CommonCard[] commonCard;
    public RareCard rareCard;



    [SerializeField] private TextMeshProUGUI reRollCostText;
    [SerializeField] private TextMeshProUGUI dnaAmountText;
    [SerializeField] private int dnaAmount;
    public int DnaAmount
    {
        get { return dnaAmount; }
        set 
        { 
            dnaAmount = value;
            GameInstance.Instance.curDNA = value;
            dnaAmountText.text = dnaAmount.ToString();
        }
    }

    private void OnEnable()
    {
        Managers.Instance.Fade.FadeIn();
        UpdateCommonMutation();
        UpdateRareMutation();
        UpdateDNA();
    }

    private void Update()
    {
        Cursor.visible = true;
    }

    private void UpdateDNA()
    {
        DnaAmount = GameInstance.Instance.curDNA;
    }

    public void NextStage()
    {
        Managers.Instance.Fade.FadeOut(() =>
        {
            GameInstance.Instance.curStageLevel++;
            SceneManager.LoadSceneAsync("GameScene");
        });
    }

    [ContextMenu("UpdateCommonMutation")]
    private void UpdateCommonMutation()
    {
        
        foreach(var card in commonCard) // 리롤용 재생성
        {
            card.gameObject.SetActive(true);
        }
        List<MutationData> indexList = mutationInfo.MutationData.ToList();

        int count = 3; // 3개 뽑기

        for (int i = 0; i < count; i++)
        {
            int index = UnityEngine.Random.Range(0, indexList.Count);     // 무작위 인덱스 구함
            var data = indexList[index] ;
            commonCard[i].cardName.text = data.MutationName;
            commonCard[i].cardDescription.text = data.MutationDescription;
            commonCard[i].cardCost.text = data.MutationCost.ToString();

            commonCard[i].buyButton.onClick.RemoveAllListeners();
            var card = commonCard[i];
            commonCard[i].buyButton.onClick.AddListener(() =>
            {
                if(data.MutationCost <= GameInstance.Instance.curDNA)
                {
                    PurchaseMutation(data);
                    card.gameObject.SetActive(false);
                }
            });
            indexList.RemoveAt(index); // 인덱스를 없애서 중복 없이 뽑을 수 있도록 한다
        }
    }

    [ContextMenu("UpdateRareMutation")]

    private void UpdateRareMutation()
    {
        List<RareMutationData> indexList = mutationInfo.RareMutationData;

        int index = UnityEngine.Random.Range(0, indexList.Count);
        var data = indexList[index];
        rareCard.cardName.text = data.RareMutationName;
        rareCard.cardDescription.text = data.RareMutationDescription;
        rareCard.cardCost.text = data.RareMutationCost.ToString();

        int cardCostInt = data.RareMutationCost;

        rareCard.buyButton.onClick.AddListener(() =>
        {
            if (data.RareMutationCost <= GameInstance.Instance.curDNA)
            {
                PurchaseRareMutation(data);
                rareCard.gameObject.SetActive(false);
            }
        });
    }


    //유전자 변이 점수 차감(결제, 리롤 시)
    private void DnaDecrease(int cost = 1)
    {
        DnaAmount -= cost;
    }



    public void PurchaseRareMutation(RareMutationData data)
    {
        if (data.RareMutationCost > GameInstance.Instance.curDNA)
        {
            return;
        }


        //if (data.MutationName == "'퉤 해금")
        //    mutationInfo.MutationData.RemoveAt(index);
        //if (data.MutationName == "폴짝 해금")
        //    mutationInfo.MutationData.RemoveAt(index);


        if (!GameInstance.Instance.hiddenAbilities.ContainsKey(data.type))
        {
            GameInstance.Instance.hiddenAbilities[data.type] = 1;
        }
        else
        {
            GameInstance.Instance.hiddenAbilities[data.type]++;
        }

        DnaDecrease(data.RareMutationCost);
    }

    public void PurchaseMutation(MutationData data)
    {
        if (data.MutationCost > GameInstance.Instance.curDNA)
        {
            return;
        }
        if (data.MutationName == "돌진 해금")
        {
            for (int j = 0; j < mutationInfo.UnlockMutationData.Count; j++)
            {
                if (mutationInfo.UnlockMutationData[j].MutationName == "더 많은 돌진")
                {
                    mutationInfo.MutationData.Add(mutationInfo.UnlockMutationData[j]);
                    mutationInfo.UnlockMutationData.RemoveAt(j);
                }
            }
        }
        if(data.type == CommonAbilityType.Restoration)
        {
            GameInstance.Instance.curHeart++;
        }



        if (!GameInstance.Instance.commonAbilities.ContainsKey(data.type))
        {
            GameInstance.Instance.commonAbilities[data.type] = 1;
        }
        else
        {
            GameInstance.Instance.commonAbilities[data.type]++;
        }

        DnaDecrease(data.MutationCost);
    }



    public void Reroll()
    {
        if (reRollCost > DnaAmount)
        {
            return;
        }
        UpdateCommonMutation();
        DnaDecrease(reRollCost);
        reRollCost *= 2;
        reRollCostText.text = reRollCost.ToString();
    }

}
