using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;
using static UnityEngine.Rendering.DebugUI;

public class ShopManager : MonoBehaviour
{
    public Dictionary<string, int> mutationCnt = new();

    private string mutationName;
    private string mutationDescription;
    private string mutationCooldown;
    private string mutationCost;

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
            dnaAmountText.text = dnaAmount.ToString();
        }
    }

    private void OnEnable()
    {
        distributionMutation();
        distributionRareMutation();
        //dnaAmount.text = GameInstance.Instance.curDNA.ToString();

        DnaAmount = 5000;
    }

    // Update is called once per frame
    void Update()
    {

    }


    //보유 변이 유전자 업데이트
    private void updateDNA()
    {

    }


    [ContextMenu("distributionMutationTest")]
    private void distributionMutation()
    {
        
        foreach(var card in commonCard) // 리롤용 재생성
        {
            card.gameObject.SetActive(true);
        }
        List<MutationData> indexList = mutationInfo.MutationData.ToList();

        int count = 3; // 무작위로 몇 개 뽑을 것인지 정함

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
                purchaseMutation(data);
                card.gameObject.SetActive(false);  
            });

            //if(data.MutationName == "돌진 해금")
            //{
            //    for(int j = 0; j < mutationInfo.UnlockMutationData.Count; j++)
            //    {
            //        if (mutationInfo.UnlockMutationData[j].MutationName == "더 많은 돌진")
            //        {
            //            mutationInfo.MutationData.Add(mutationInfo.UnlockMutationData[j]);
            //            mutationInfo.UnlockMutationData.RemoveAt(j);
            //        }
            //    }
            //}
            // ~ list[index] 사용해서 하고 싶은 일 하기 ~
            indexList.RemoveAt(index); // 인덱스를 없애서 중복 없이 뽑을 수 있도록 한다
        }
    }

    [ContextMenu("distributionRareMutationTest")]

    private void distributionRareMutation()
    {
        List<RareMutationData> indexList = mutationInfo.RareMutationData;

        int index = UnityEngine.Random.Range(0, indexList.Count);
        rareCard.cardName.text = indexList[index].RareMutationName;
        rareCard.cardDescription.text = indexList[index].RareMutationDescription;
        rareCard.cardCost.text = indexList[index].RareMutationCost.ToString();

        int cardCostInt = indexList[index].RareMutationCost;

        rareCard.buyButton.onClick.AddListener(() =>
        {
            purchaseRareMutation(indexList[index]);
            rareCard.gameObject.SetActive(false);
        });
    }


    //유전자 변이 점수 차감(결제, 리롤 시)
    private void dnaDecrease(int cost = 1)
    {
        DnaAmount -= cost;
    }



    public void purchaseMutation(MutationData data)
    {
        if (data.MutationCost > DnaAmount)
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

        //if (data.MutationName == "'퉤 해금")
        //    mutationInfo.MutationData.RemoveAt(index);
        //if (data.MutationName == "폴짝 해금")
        //    mutationInfo.MutationData.RemoveAt(index);
  

        if (!mutationCnt.ContainsKey(data.MutationName))
        {
            mutationCnt[data.MutationName] = 1;
        }
        else
        {
            mutationCnt[data.MutationName]++;
        }


        dnaDecrease(data.MutationCost);
    }


    private void purchaseRareMutation(RareMutationData data2)
    {
        if (data2.RareMutationCost > DnaAmount)
        {
            return;
        }

        if (!mutationCnt.ContainsKey(data2.RareMutationName))
        {
            mutationCnt[data2.RareMutationName] = 1;
        }
        else
        {
            mutationCnt[data2.RareMutationName]++;
        }

        //mutationInfo.MutationData.RemoveAt(index);
        dnaDecrease(data2.RareMutationCost);
    }


    public void reRoll()
    {
        if (reRollCost > DnaAmount)
        {
            return;
        }
        distributionMutation();
        reRollCostText.text = reRollCost.ToString();
        dnaDecrease(reRollCost);
        reRollCost *= 2;
    }

}
