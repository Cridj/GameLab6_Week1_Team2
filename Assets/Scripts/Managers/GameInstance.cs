using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameInstance : MonoBehaviour
{
    public static GameInstance Instance { get; private set; }
    public int curStageLevel = 1;
    public int curDNA = 0;
    public int curHeart = 0;

    public SerializedDictionary<CommonAbilityType, int> commonAbilities = new SerializedDictionary<CommonAbilityType, int>();

    public SerializedDictionary<HiddenAbilityType, int> hiddenAbilities = new SerializedDictionary<HiddenAbilityType, int>();

    public SerializedDictionary<int, StageSpawnData> stageSpawnData = new SerializedDictionary<int, StageSpawnData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public StageSpawnData GetCurrentSpawnData()
    {
        if(stageSpawnData.TryGetValue(curStageLevel, out StageSpawnData spawnData))
        {
            return spawnData;
        }
        return null;
    }
}