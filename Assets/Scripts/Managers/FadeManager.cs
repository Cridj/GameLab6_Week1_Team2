using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    private static FadeManager instance = null;
    [SerializeField] private Image fadeImage;
    [SerializeField] private GameObject fadeImagePrefab;

    public static FadeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectsByType<FadeManager>(FindObjectsSortMode.None)[0];
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
        if(fadeImage == null)
        {
            Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
            if (canvas == null)
                canvas = new GameObject().AddComponent<Canvas>();
            fadeImage = Instantiate(fadeImagePrefab, canvas.transform).GetComponent<Image>();
        }
        FadeIn();
    }

    [ContextMenu("FadeIn")]
    public void FadeIn()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 1);
        fadeImage.CrossFadeAlpha(0, 1.5f, false);
    }

    [ContextMenu("FadeOut")]
    public void FadeOut()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.CrossFadeAlpha(1, 1.5f, false);
    }
}
