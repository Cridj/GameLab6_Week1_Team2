using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "StageSpawnData", menuName = "Scriptable Objects/StageSpawnData")]
public class StageSpawnData : ScriptableObject
{
    public Vector3 playerInitialPos;

    [Header("Initial")]
    public float initialSpawnRadius;
    public int initialNeutralAmount;

    [Space]
    public float nearPlayerSpawnRadius;
    public float spawnInterval = 0.5f;


    [Header("Max Amount")]
    public int maxNeutralAmount;
    public int maxMedicAmount;
    public int maxPoliceAmount;

}