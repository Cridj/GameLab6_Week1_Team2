using AYellowpaper.SerializedCollections;
using UnityEngine;

public class Follower : MonoBehaviour
{
    [SerializeField] private SerializedDictionary<int, GameObject[]> hatDict;
    [SerializeField] private SerializedDictionary<int, GameObject[]> faceDict;
    [SerializeField] private Renderer[] hopakRenderers;
    [SerializeField] private Renderer[] shoesRenderers;
    public string ownerName;
    [SerializeField] private Renderer minimapIcon;

    public void ApplyCustomizeInfo(CustomizeInfo customInfo, bool isRemote)
    {
        SetHat(customInfo);
        SetFace(customInfo);
        foreach (var renderer in hopakRenderers)
        {
            if (renderer != null)
                renderer.material.color = customInfo.bodyColor;
        }
        foreach (var renderer in shoesRenderers)
        {
            if (renderer != null)
                renderer.material.color = customInfo.shoesColor;
        }

        if (isRemote)
            minimapIcon.material.color = Color.black;
        else
            minimapIcon.material.color = Color.red;
    }


    private void SetHat(CustomizeInfo customInfo)
    {
        foreach (var hat in hatDict.Values)
            foreach (var obj in hat)
                if (obj != null)
                    obj.SetActive(false);

        if (hatDict.TryGetValue(customInfo.hatType, out var go))
            if (go != null)
                foreach (var obj in go)
                    if (obj != null)
                        obj.SetActive(true);
    }

    private void SetFace(CustomizeInfo customInfo)
    {
        foreach (var face in faceDict.Values)
            foreach (var obj in face)
                if (obj != null)
                    obj.SetActive(false);
        if (faceDict.TryGetValue(customInfo.faceType, out var go))
            if (go != null)
                foreach (var obj in go)
                    if (obj != null)
                        obj.SetActive(true);
    }

}
