using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MutationData
{
    public CommonAbilityType type;
    public string MutationName;
    public string MutationDescription;
    public string MutationCooldown;
    public int MutationCost;
}

[System.Serializable]
public class RareMutationData
{
    public HiddenAbilityType type;
    public string RareMutationName;
    public string RareMutationDescription;
    public string RareMutationCooldown;
    public int RareMutationCost;
}

public class MutationInfo : MonoBehaviour
{
    public List<MutationData> MutationData;
    public List<MutationData> UnlockMutationData;


    public List<RareMutationData> RareMutationData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
