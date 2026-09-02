using UnityEngine;

public class InfectionManager : MonoBehaviour
{
    public static InfectionManager Instance { get; private set; }

    public int infectedCount = 0;

    public float followDistanceDiff = 2f;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        infectedCount = 0;
    }

    public float GetFollowDistance()
    {
        return followDistanceDiff * infectedCount;
    }

}
