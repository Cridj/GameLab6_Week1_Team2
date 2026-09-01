using UnityEngine;

[System.Serializable]
public class MutationData
{
    public string skillName;
    public string skillDescription;
    public string skillCooldown;
    public string skillCost;
}


public class MutationInfo : MonoBehaviour
{
    public MutationData[] MutationData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
