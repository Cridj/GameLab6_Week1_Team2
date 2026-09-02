using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SpawnData
{
    public PoolType type;

    [Range(0, 100)]
    public float weight;
}

[CreateAssetMenu(fileName = "StageSpawnData", menuName = "Scriptable Objects/StageSpawnData")]
public class StageSpawnData : ScriptableObject
{
    public Vector3 playerInitialPos;

    [Header("Initial")]
    public float initialSpawnRadius;
    public int initialNeutralAmount;

    [Header("Spawn Target")]
    public List<SpawnData> spawnDataList = new List<SpawnData>();

    [Space]
    public float nearPlayerSpawnRadius;
    public float spawnInterval = 0.5f;


    [Header("Max Amount")]
    public int maxNeutralAmount;
    public int maxMedicAmount;
    public int maxPoliceAmount;

}