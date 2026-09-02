using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SpawnData
{
    public GameObject prefab;

    [Range(0, 100)]
    public float weight;
}

[CreateAssetMenu(fileName = "StageSpawnData", menuName = "Scriptable Objects/StageSpawnData")]
public class StageSpawnData : ScriptableObject
{
    public Vector3 playerInitialPos;
    public float nearPlayerSpawnRadius;

    [Header("Initial")]
    public float initialSpawnRadius;
    public int initialNeutralAmount;

    [Header("Max Amount")]
    public int maxNeutralAmount;
    public int maxMedicAmount;
    public int maxPoliceAmount;


    [Header("Spawn Target")]
    public List<SpawnData> spawnDataList = new List<SpawnData>();

    [Space]
    public float spawnInterval = 0.5f;

}
