using System.Collections.Generic;
using UnityEngine;


public enum CommonAbilityType
{
    Sprint, Jump, Snip, Restoration, HopakJunior, Growing
}

public enum HiddenAbilityType
{
    Windmill, Mario, Artillery, Gambler
}

public class PlayerAbility : MonoBehaviour
{
    [Header("Common Abilities")]
    public Dictionary<CommonAbilityType, int> commonAbilities = new Dictionary<CommonAbilityType, int>();

    [Header("Hidden  Abilities")]
    [SerializeField]
    public Dictionary<HiddenAbilityType, int> hiddenAbilities = new Dictionary<HiddenAbilityType, int>();


    private void Awake()
    {
        // Initialize common abilities 임시
        commonAbilities.Add(CommonAbilityType.Growing, 1);
        commonAbilities.Add(CommonAbilityType.Sprint, 1);
        commonAbilities.Add(CommonAbilityType.HopakJunior, 4);
        commonAbilities.Add(CommonAbilityType.Jump, 1);
        //commonAbilities.Add(CommonAbilityType.Snip, 1);


        // Initialize hidden abilities 임시
        //hiddenAbilities.Add(HiddenAbilityType.Windmill, 1);
    }


}