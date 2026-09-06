using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using AYellowpaper.SerializedCollections;
using UnityEngine.SceneManagement;
using TMPro;


public class UI_Customize : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private TMP_InputField nicknameInputField;

    [Header("Palette Images")]
    [SerializeField] private RawImage paletteImage;
    [SerializeField] private RawImage hueImage;
    [SerializeField] private TextMeshProUGUI hatText;
    [SerializeField] private TextMeshProUGUI faceText;

    [Header("Color")]
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Renderer[] hopakRenderers;
    [SerializeField] private Renderer[] shoesRenderers;

    [SerializeField, Range(0f, 360f)] private float baseHue;
    [SerializeField, Min(16)] private int textureSize = 256;
    [SerializeField, Min(16)] private int hueTextureWidth = 360;
    [SerializeField, Min(1)] private int hueTextureHeight = 16;

    [Header("Events")]
    [SerializeField] private UnityEvent<Color> onColorChanged;
    [SerializeField] private UnityEvent<float> onHueChanged;

    [SerializeField] private int curFace, curHat;

    [SerializeField] private SerializedDictionary<int, GameObject> hatDict;
    [SerializeField] private SerializedDictionary<int, GameObject> faceDict;

    [SerializeField] private GameObject currentHat, currentFace;

    private Texture2D paletteTexture;
    private Texture2D hueTexture;

    public Color selectedColor;

    public float SelectedHue { get; private set; }

    private void Start()
    {
        SelectedHue = baseHue;
        GenerateHuePalette();
        GeneratePalette();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdatePaletteValue(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdatePaletteValue(eventData);
    }

    private void SetHat()
    {
        foreach (var hat in hatDict.Values)
        {
            if (hat != null)
            {
                hat.SetActive(false);
            }
        }

        if (hatDict.TryGetValue(curHat, out var go))
            if (go != null) go.SetActive(true);
        hatText.text = curHat.ToString();
        GameInstance.Instance.CustomizeInfo.hatType = curHat;
    }

    private void SetFace()
    {
        foreach (var face in faceDict.Values)
        {
            if (face != null)
            {
                face.SetActive(false);
            }
        }

        if (faceDict.TryGetValue(curFace, out var go))
            if (go != null) go.SetActive(true);
        faceText.text = curFace.ToString();
        GameInstance.Instance.CustomizeInfo.faceType = curFace;
    }

    public void OnNextHat()
    {
        curHat = (curHat + 1) % hatDict.Count;
        SetHat();
    }

    public void OnPrevHat()
    {
        curHat = (curHat - 1 + hatDict.Count) % hatDict.Count;
        SetHat();
    }
    public void OnNextFace()
    {
        curFace = (curFace + 1) % faceDict.Count;
        SetFace();
    }

    public void OnPrevFace()
    {
        curFace = (curFace - 1 + faceDict.Count) % faceDict.Count;
        SetFace();
    }

    private void Update()
    {
        if (EventSystem.current == null)
            return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began ||
                touch.phase == TouchPhase.Moved ||
                touch.phase == TouchPhase.Stationary)
            {
                ProcessPointerPosition(touch.position, touch.fingerId);
            }

            return;
        }

        if (Input.GetMouseButton(0))
            ProcessPointerPosition(Input.mousePosition, -1);
    }

    private void ProcessPointerPosition(Vector2 position, int pointerId)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = position,
            pointerId = pointerId
        };

        UpdatePaletteValue(eventData);
    }

    private Camera GetCanvasCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void UpdatePaletteValue(PointerEventData eventData)
    {
        if (TryUpdateHue(eventData))
            return;

        UpdateColor(eventData);
    }

    public void MoveToNextScene()
    {
        //TODO 닉네임 인풋필드 비어있으면 안넘어가게
        Managers.Instance.Fade.FadeOut(() =>
        {
            GameInstance.Instance.CustomizeInfo.nickName = nicknameInputField.text;
            SceneManager.LoadSceneAsync("MultiScene");
        });
    }

    private bool TryUpdateHue(PointerEventData eventData)
    {
        if (hueImage == null || hueTexture == null)
            return false;

        RectTransform rectTransform = hueImage.rectTransform;
        Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : GetCanvasCamera();
        if (!RectTransformUtility.RectangleContainsScreenPoint(
                rectTransform, eventData.position, eventCamera))
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventCamera, out Vector2 localPoint))
            return false;

        float normalizedHue = Mathf.InverseLerp(
            rectTransform.rect.xMin, rectTransform.rect.xMax, localPoint.x);
        SetHue(normalizedHue * 360f);
        return true;
    }

    public void GenerateHuePalette()
    {
        if (hueImage == null)
            return;

        if (hueTexture != null)
            Destroy(hueTexture);

        int width = Mathf.Max(16, hueTextureWidth);
        int height = Mathf.Max(1, hueTextureHeight);
        hueTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Runtime Hue Palette",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] colors = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            Color color = Color.HSVToRGB((float)x / (width - 1), 1f, 1f);
            for (int y = 0; y < height; y++)
                colors[y * width + x] = color;
        }

        hueTexture.SetPixels(colors);
        hueTexture.Apply();
        hueImage.texture = hueTexture;
    }

    public void GeneratePalette(float hue)
    {
        baseHue = Mathf.Repeat(hue, 360f);
        GeneratePalette();
    }

    public void GeneratePalette()
    {
        if (paletteImage == null)
            return;

        if (paletteTexture != null)
            Destroy(paletteTexture);

        int size = Mathf.Max(16, textureSize);
        paletteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        paletteTexture.name = "Runtime Palette";
        paletteTexture.wrapMode = TextureWrapMode.Clamp;
        paletteTexture.filterMode = FilterMode.Bilinear;

        Color[] colors = new Color[size * size];
        float normalizedHue = baseHue / 360f;

        for (int y = 0; y < size; y++)
        {
            float value = (float)y / (size - 1);

            for (int x = 0; x < size; x++)
            {
                float saturation = (float)x / (size - 1);
                colors[y * size + x] = Color.HSVToRGB(normalizedHue, saturation, value);
            }
        }

        paletteTexture.SetPixels(colors);
        paletteTexture.Apply();
        paletteImage.texture = paletteTexture;
    }






    public void SetHue(float hue)
    {
        SelectedHue = Mathf.Repeat(hue, 360f);
        GeneratePalette(SelectedHue);
        onHueChanged?.Invoke(SelectedHue);
    }

    private void UpdateColor(PointerEventData eventData)
    {
        if (paletteImage == null || paletteTexture == null)
            return;

        RectTransform rectTransform = paletteImage.rectTransform;
        Camera eventCamera = eventData.pressEventCamera != null
            ? eventData.pressEventCamera
            : GetCanvasCamera();
        if (!RectTransformUtility.RectangleContainsScreenPoint(
                rectTransform, eventData.position, eventCamera))
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float uvX = Mathf.InverseLerp(
            rectTransform.rect.xMin,
            rectTransform.rect.xMax,
            localPoint.x);
        float uvY = Mathf.InverseLerp(
            rectTransform.rect.yMin,
            rectTransform.rect.yMax,
            localPoint.y);

        int x = Mathf.Clamp(
            Mathf.RoundToInt(uvX * (paletteTexture.width - 1)),
            0,
            paletteTexture.width - 1);
        int y = Mathf.Clamp(
            Mathf.RoundToInt(uvY * (paletteTexture.height - 1)),
            0,
            paletteTexture.height - 1);

        selectedColor = paletteTexture.GetPixel(x, y);

        if (targetGraphic != null)
        {
            targetGraphic.color = selectedColor;
        }



        onColorChanged?.Invoke(selectedColor);
    }


    public void ChangeColorAndSave(string type)
    {
        if (type == "Body")
        {
            foreach (var renderer in hopakRenderers)
            {
                renderer.material.color = selectedColor;
                GameInstance.Instance.CustomizeInfo.bodyColor = selectedColor;
            }
        }
        else
        {
            foreach (var renderer in shoesRenderers)
            {
                renderer.material.color = selectedColor;
                GameInstance.Instance.CustomizeInfo.shoesColor = selectedColor;
            }
        }
    }

    public void OpenConfirmPanel()
    {
        nicknameInputField.text = "";
    }
    

    private void OnDestroy()
    {
        if (paletteTexture != null)
            Destroy(paletteTexture);

        if (hueTexture != null)
            Destroy(hueTexture);
    }
}
