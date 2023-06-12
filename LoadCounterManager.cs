using UnityEngine;
using System.Collections.Generic;
using Mesh = UnityEngine.Mesh;
# if UNITY_EDITOR
using VisualDesignCafe.Nature.Materials.Editor.Sections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
# endif

// THIS CLASS IS CURRENTLY NOT USED!! Could be useful in the future?
public static class LoadCounterManager
{
    [ClearOnReload(true)]
    public static Dictionary<uint, int> loadCounters = new Dictionary<uint, int>();

    public static int GetLoadCounter(uint assetKey)
    {
        if (!loadCounters.ContainsKey(assetKey))
        {
            return 0;
        }
        return loadCounters[assetKey];
    }

    public static int IncrementLoadCounter(uint assetKey)
    {
        if (!loadCounters.ContainsKey(assetKey))
        {
            loadCounters.Add(assetKey, 0);
        }

        if (assetKey == 2136554873)
            Debug.Log(assetKey.ToString() + ": " + (loadCounters[assetKey] + 1).ToString());
        return ++loadCounters[assetKey];
    }
    public static int DecrementLoadCounter(uint assetKey)
    {
        if (!loadCounters.ContainsKey(assetKey))
        {
            loadCounters.Add(assetKey, 0);
        }
        int newCount = --loadCounters[assetKey];
        if (newCount == 0)
        {
            loadCounters.Remove(assetKey);
        }

        if (assetKey == 2136554873)
            Debug.Log(assetKey.ToString() + ": " + newCount.ToString());
        return newCount;
    }
}
