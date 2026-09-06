using AYellowpaper.SerializedCollections;
using UnityEngine;

[System.Serializable]
public struct CustomizeInfo
{
    public string nickName;
    public Color bodyColor;
    public Color shoesColor;
    public int hatType;
    public int faceType;
}

public class GameInstance : MonoBehaviour
{
    public static GameInstance Instance { get; private set; }
    public CustomizeInfo CustomizeInfo;


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
}