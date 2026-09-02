using UnityEngine;

public class Managers : MonoBehaviour
{
    private static Managers instance = null;
    
    [SerializeField] private FadeManager fadeManager;
    [SerializeField] private SoundManager soundManager;

    public FadeManager Fade => fadeManager;
    public SoundManager Sound => soundManager;

    public static Managers Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectsByType<Managers>(FindObjectsSortMode.None)[0];
            }
            return instance;
        }
    }

    void Awake()
    {
        if (null == instance)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        fadeManager.Init();
    }
}
