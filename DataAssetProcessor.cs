using UnityEngine;
using System.Collections.Generic;
using Mesh = UnityEngine.Mesh;
#if UNITY_EDITOR
using VisualDesignCafe.Nature.Materials.Editor.Sections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AddressableAssets;

[InitializeOnLoad]
#endif
public static class DataAssetProcessor
{
    public static void SetRendererMesh(Renderer renderer, Mesh mesh)
    {
        if (renderer == null) return;
        if (renderer is SkinnedMeshRenderer r)
        {
            if(r != null)
            {
                r.sharedMesh = mesh;
            }
        }
        else
        {
            renderer.GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }

    public static Mesh GetRendererMesh(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer r)
        {
            return r.sharedMesh;
        }
        else
        {
            return renderer.GetComponent<MeshFilter>().sharedMesh;
        }
    }

#if UNITY_EDITOR
    static DataAssetProcessor()
    {
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
    }

    static void OnPrefabStageOpened(PrefabStage prefabStage)
    {
        PrefabStage.prefabSaving += HandlePrefabSaving;
        PrefabStage.prefabSaved += HandlePrefabSaved;
    }

    static void OnPrefabStageClosing(PrefabStage prefabStage)
    {
        PrefabStage.prefabSaving -= HandlePrefabSaving;
        PrefabStage.prefabSaved -= HandlePrefabSaved;
    }

    static void HandlePrefabSaving(GameObject savedObject)
    {
        if (savedObject)
        {
            BaseDataAsset dataAsset = savedObject.GetComponent<BaseDataAsset>();
            if (dataAsset)
            {
                dataAsset.OnSave();
            }
        }
    }

    static void HandlePrefabSaved(GameObject savedObject)
    {
        if (savedObject)
        {
            BaseDataAsset dataAsset = savedObject.GetComponent<BaseDataAsset>();
            if (dataAsset)
            {
                dataAsset.LoadAllAssets();
            }
        }
    }
    #endif
}
