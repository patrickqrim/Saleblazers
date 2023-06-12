using ch.sycoforge.Decal.Projectors.Geometry;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.VersionControl;
using VisualDesignCafe.Nature.Materials.Editor.Sections;
# endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Mesh = UnityEngine.Mesh;

public abstract class BaseWeaponDataAsset : BaseDataAsset
{
    // Asset reference fields
    /// <summary> References to the mesh renderers of each LOD for the weapon </summary>
    public Renderer[] LODs;
    /// <summary> List of meshes of each LOD for the weapon </summary>
    public AssetReference[] WeaponMeshAssetRefs;
    /// <summary> Material for the weapon (all LODs use the same material) </summary>
    public AssetReference WeaponMaterialAssetRef;

    // Store the loaded assets so we can unload (release) them
    public List<AsyncOperationHandle<UnityEngine.Mesh>> LoadedMeshes;
    public AsyncOperationHandle<UnityEngine.Material> LoadedMaterial;

    // Store GUIDs for load counters
    [HideInInspector]
    public uint MaterialGUID;
    [HideInInspector]
    public uint[] MeshGUIDs;

    // Booleans to keep track of whether each asset is currently loaded/unloaded

    [HideInInspector]
    public bool bMaterialLoaded;
    [HideInInspector]
    public bool[] bMeshesLoaded;

    public override void LoadAllAssets()
    {
        base.LoadAllAssets();  // reset, just in case
        if (LODs == null || LODs.Length == 0) return;
        bool hasWeaponMaterialAssetRef = !string.IsNullOrEmpty(WeaponMaterialAssetRef.AssetGUID); 
        totalStillUnloaded += (hasWeaponMaterialAssetRef ? 1 : 0);

        bool[] hasWeaponMeshAssetRefs = new bool[WeaponMeshAssetRefs.Length];
        MeshGUIDs = new uint[WeaponMeshAssetRefs.Length];
        bMeshesLoaded = new bool[WeaponMeshAssetRefs.Length];

        for (int i = 0; i < WeaponMeshAssetRefs.Length; i++)
        {
            hasWeaponMeshAssetRefs[i] = !string.IsNullOrEmpty(WeaponMeshAssetRefs[i].AssetGUID);
            totalStillUnloaded += (hasWeaponMeshAssetRefs[i] ? 1 : 0);
        }
        if (CheckAllLoaded()) { return; }

        // Load the material
        if (hasWeaponMaterialAssetRef)
        {
            var LoadedAsset = LoadAssetAsync<UnityEngine.Material>(WeaponMaterialAssetRef);
            MaterialGUID = ConversionUtil.ConvertStringToUInt(WeaponMaterialAssetRef.AssetGUID);
            LoadedAsset.Completed += operation =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    totalStillUnloaded--;
                    for (int i = 0; i < LODs.Length; i++)
                    {
                        if (LODs[i])
                        {
                            LODs[i].sharedMaterial = operation.Result;
                        }

                    }
                    CheckAllLoaded();
                }
                else
                {
                    Debug.LogError($"Failed to load material");
                }
            };
            LoadedMaterial = LoadedAsset;

        }


        // Load the meshes
        LoadedMeshes = new List<AsyncOperationHandle<UnityEngine.Mesh>>();
        if (WeaponMeshAssetRefs != null)
        {
            for (int j = 0; j < WeaponMeshAssetRefs.Length; j++)
            {
                if (hasWeaponMeshAssetRefs[j])
                {
                    int index = j;
                    var LoadedAsset = LoadAssetAsync<Mesh>(WeaponMeshAssetRefs[index]);
                    MeshGUIDs[index] = ConversionUtil.ConvertStringToUInt(WeaponMeshAssetRefs[index].AssetGUID);
                    LoadedAsset.Completed += operation =>
                    {
                        if (operation.Status == AsyncOperationStatus.Succeeded)
                        {
                            totalStillUnloaded--;

                            DataAssetProcessor.SetRendererMesh(LODs[index], operation.Result);
                            //LoadCounterManager.IncrementLoadCounter(MeshGUIDs[index]);


                            CheckAllLoaded();
                        }
                        else
                        {
                            Debug.LogError($"Failed to load mesh " + index.ToString());
                        }
                    };
                    LoadedMeshes.Add(LoadedAsset);
                }

            }
        }
    }


    public override void UnloadAllAssets()
    {
        if (LoadedMeshes != null)
        {
            for (int i = 0; i < LoadedMeshes.Count; i++)
            {
                // Null out material/meshes here because we want to do it for each LOD
                LODs[i].sharedMaterial = null;
                DataAssetProcessor.SetRendererMesh(LODs[i], null);

                // Unload meshes
                if (LoadedMeshes[i].IsValid())
                    Addressables.Release(LoadedMeshes[i].Result);

                // TODO: cancel the handles?

                // Unload meshes
                //LoadCounterManager.DecrementLoadCounter(MeshGUIDs[i]);
                //if (LoadCounterManager.DecrementLoadCounter(MeshGUIDs[i]) <= 0)
                //{
                //    Addressables.Release(sharedMesh);
                //}

            }
            LoadedMeshes = null;
        }
#if UNITY_EDITOR
        else
        {
            Debug.Log(gameObject.name + ": LoadedMeshes is null or invalid.");
        }
#endif

        if (LoadedMaterial.IsValid()) {
            // Unload material
            Addressables.Release(LoadedMaterial.Result);

            // Unload material
            //LoadCounterManager.DecrementLoadCounter(MaterialGUID);
            //if (LoadCounterManager.GetLoadCounter(MaterialGUID) <= 0)
            //{
            //    Addressables.Release(sharedMaterial); 
            //}
        }
#if UNITY_EDITOR
        else
        {
            Debug.Log(gameObject.name + ": LoadedMaterial is null or invalid.");
        }
#endif

        // When all unloaded, do:
        OnAllAssetsUnloaded();
    }

#if UNITY_EDITOR
    public override void OnSave()
    {
        // Checkout the mesh and material addressable asset groups, and ERROR if they can't be checked out
        AssetList addressableGroups = new AssetList();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        Asset meshGroupAsset = Provider.GetAssetByPath(AssetDatabase.GetAssetPath(settings.FindGroup("Meshes")));
        if (!Provider.CheckoutIsValid(meshGroupAsset) && !Provider.IsOpenForEdit(meshGroupAsset))
        {
            Debug.LogError($"ERROR: Could not check out meshes addressable group");
            return;
        }
        addressableGroups.Add(meshGroupAsset);
        Asset materialGroupAsset = Provider.GetAssetByPath(AssetDatabase.GetAssetPath(settings.FindGroup("Materials")));
        if (!Provider.CheckoutIsValid(materialGroupAsset) && !Provider.IsOpenForEdit(materialGroupAsset))
        {
            Debug.LogError($"ERROR: Could not check out materials addressable group");
            return;
        }
        addressableGroups.Add(materialGroupAsset);
        if (Provider.GetLatestIsValid(addressableGroups))
        {
            Task GetLatestTask = Provider.GetLatest(addressableGroups);
            GetLatestTask.Wait();
        }
        if (Provider.ResolveIsValid(addressableGroups))
        {
            Task ResolveTask = Provider.Resolve(addressableGroups, ResolveMethod.UseTheirs);
            ResolveTask.Wait();
        }
        Task checkoutOperation = Provider.Checkout(addressableGroups, CheckoutMode.Both);
        checkoutOperation.Wait();

        bool bAnyChange = false;

        if (LODs != null)
        {
            if (LODs.Length > 0 && LODs[0] && WeaponMaterialAssetRef.AssetGUID != AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(LODs[0].sharedMaterial)))
            {
                Material material = LODs[0].sharedMaterial;
                AddressableAssetGroup materialGroup = settings.FindGroup("Materials");
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material));
                var entry = settings.CreateOrMoveEntry(guid, materialGroup);
                entry.labels.Add(guid.GetHashCode().ToString() + "_material");
                entry.address = AssetDatabase.GetAssetPath(material) + guid.GetHashCode().ToString();
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                // Set material addressable in data asset
                WeaponMaterialAssetRef = new AssetReference(entry.guid);

                bAnyChange = true;
            }

            // Check equality of meshes and create/set addressables if not equal
            for (int i = 0; i < LODs.Length; i++)
            {
                Mesh mesh = DataAssetProcessor.GetRendererMesh(LODs[i]);
                if (WeaponMeshAssetRefs[i].AssetGUID != AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh)))
                {
                    AddressableAssetGroup meshGroup = settings.FindGroup("Meshes");
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh));
                    var entry = settings.CreateOrMoveEntry(guid, meshGroup);
                    entry.labels.Add(guid.GetHashCode().ToString() + "_mesh" + i.ToString());
                    entry.address = AssetDatabase.GetAssetPath(mesh) + guid.GetHashCode().ToString();
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                    // Add to mesh addressable list in data asset
                    WeaponMeshAssetRefs[i] = new AssetReference(entry.guid);
                    try
                    {
                        WeaponMeshAssetRefs[i].SetEditorSubObject(mesh);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("ERROR WITH SETTING SUBOBJECT OF ADDRESSABLE: " + e.ToString());
                    }

                    bAnyChange = true;
                }
            }

            // Null out assets
            for (int i = 0; i < LODs.Length; i++)
            {
                if (LODs[i])
                {
                    LODs[i].sharedMaterial = null;
                    DataAssetProcessor.SetRendererMesh(LODs[i], null);
                }
            }
        }

        if (bAnyChange && Provider.SubmitIsValid(new ChangeSet(), addressableGroups))
        {
            Task SubmitTask = Provider.Submit(new ChangeSet(), addressableGroups, "--Prefab addressable changed", false);
            SubmitTask.Wait();
        }
        else
        {
            Task RevertTask = Provider.Revert(addressableGroups, UnityEditor.VersionControl.RevertMode.Normal);
            RevertTask.Wait();
        }

    }
    public void EDITOR_ApplyDataAssets()
    {
        if (LODs == null || LODs.Length == 0) return;
        bool hasWeaponMaterialAssetRef = !string.IsNullOrEmpty(WeaponMaterialAssetRef.AssetGUID); ;
        bool[] hasWeaponMeshAssetRefs = new bool[WeaponMeshAssetRefs.Length];
        MeshGUIDs = new uint[WeaponMeshAssetRefs.Length];
        for (int i = 0; i < WeaponMeshAssetRefs.Length; i++)
        {
            hasWeaponMeshAssetRefs[i] = !string.IsNullOrEmpty(WeaponMeshAssetRefs[i].AssetGUID);
        }
        // Load the material
        for (int i = 0; i < LODs.Length; i++)
        {
            if (LODs[i])
            {
                if (hasWeaponMeshAssetRefs[i])
                {
                    UnityEngine.Object[] objs = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(WeaponMeshAssetRefs[i].editorAsset));
                    foreach (var obj in objs)
                    {
                        if (obj is Mesh && obj.name == WeaponMeshAssetRefs[i].SubObjectName)
                        {
                            DataAssetProcessor.SetRendererMesh(LODs[i], obj as Mesh);
                        }
                    }

                }
                if (hasWeaponMaterialAssetRef)
                {
                    LODs[i].sharedMaterial = (Material)WeaponMaterialAssetRef.editorAsset;
                }
            }

        }


    }
#endif
}
