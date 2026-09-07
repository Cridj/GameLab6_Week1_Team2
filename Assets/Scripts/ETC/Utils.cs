using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class Utils 
{
    private static readonly Dictionary<float, WaitForSeconds> WaitDictionary = new Dictionary<float, WaitForSeconds>();
    public static WaitForSeconds GetWait(float time)
    {
        if (WaitDictionary.TryGetValue(time, out var wait)) return wait;
        WaitDictionary[time] = new WaitForSeconds(time);
        return WaitDictionary[time];
    }

    public static Vector3 CalculateScale(int cnt)
    {
        return Vector3.one * ((float)cnt / 40 + 1);
    }
}
