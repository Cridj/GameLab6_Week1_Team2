using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private GameObject fadeImagePrefab;

    public void Init()
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
        if (fadeImage == null)
        {
            Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
            if (canvas == null)
                canvas = new GameObject().AddComponent<Canvas>();
            fadeImage = Instantiate(fadeImagePrefab, canvas.transform).GetComponent<Image>();
        }


        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 1);
        fadeImage.DOColor(new Color(0, 0, 0, 0), 1.5f);
    }

    [ContextMenu("FadeOut")]
    public void FadeOut(Action callback)
    {
        if (fadeImage == null)
        {
            Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
            if (canvas == null)
                canvas = new GameObject().AddComponent<Canvas>();
            fadeImage = Instantiate(fadeImagePrefab, canvas.transform).GetComponent<Image>();
        }
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.DOColor(new Color(0, 0, 0, 1), 1.5f);
        StartCoroutine(CallbackReceiver(callback));
    }

    IEnumerator CallbackReceiver(Action callback)
    {
        yield return new WaitForSeconds(1.5f);
        callback?.Invoke();
    }
}
