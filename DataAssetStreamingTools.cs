#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Util;
using UnityEditor.VersionControl;
using System.IO;
using VisualDesignCafe.Nature.Materials.Editor.Sections;
using Mesh = UnityEngine.Mesh;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using Mono.CSharp;
using ch.sycoforge.Decal.Projectors.Geometry;
using System.Linq;
using Apache.Arrow;

namespace AssetTools
{
    public class DataAssetStreamingTools : BaseEditorWindowTool
    {
        public HRItemDatabase MasterItemDB;
        public int itemIdx;

        [MenuItem("Airstrafe Tools/Asset Tools/Data Asset Streaming Tools")]
        public static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            DataAssetStreamingTools window = (DataAssetStreamingTools)EditorWindow.GetWindow(typeof(DataAssetStreamingTools));
            window.minSize = new Vector2(400, 250);

            window.Show();
        }
        private void OnEnable()
        {
        }

        void SetupWeapon(int singleIdx = -1)
        {
            // Debugging trackers
            List<int> failedOnCheckOut = Enumerable.Range(1500, MasterItemDB.ItemArray.Length).ToList<int>();
            List<int> prefabDoesntExist = new List<int>();
            List<int> failedOnSetup = new List<int>();
            List<int> successfulSetup = new List<int>();

            // Checkout the mesh and material addressable asset groups, and ERROR if they can't be checked out
            AssetList addressableGroups = new AssetList();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            Asset meshGroupAsset = Provider.GetAssetByPath(AssetDatabase.GetAssetPath(settings.FindGroup("Meshes")));
            if (!Provider.CheckoutIsValid(meshGroupAsset))
            {
                Debug.LogError($"ERROR: Could not check out meshes addressable group");
                return;
            }
            addressableGroups.Add(meshGroupAsset);
            Asset materialGroupAsset = Provider.GetAssetByPath(AssetDatabase.GetAssetPath(settings.FindGroup("Materials")));
            if (!Provider.CheckoutIsValid(materialGroupAsset))
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

            // Add prefab(s) to asset list
            AssetList assetList = new AssetList();
            List<int> idxList = new List<int>();
            int[] itemIdxs = { singleIdx };
            if (singleIdx == -1)
            {
                itemIdxs = Enumerable.Range(1500, MasterItemDB.ItemArray.Length).ToArray();
            }
            for (int a = 0; a < itemIdxs.Length; a++)
            {
                itemIdx = itemIdxs[a];
                if (itemIdx != -1 && (itemIdx < 0 || itemIdx >= MasterItemDB.ItemArray.Length || MasterItemDB.ItemArray[itemIdx] == null)) continue;
                string itemName = MasterItemDB.ItemArray[itemIdx].ItemName;
                GameObject prefab = MasterItemDB.ItemArray[itemIdx].ItemPrefab;
                if (prefab == null || itemName == "")
                {
                    Debug.LogError("Item " + itemIdx.ToString() + ": no item name, or prefab doesn't exist");
                    failedOnCheckOut.Remove(itemIdx);
                    prefabDoesntExist.Add(itemIdx);
                    continue;
                }
                string path = AssetDatabase.GetAssetPath(prefab);
                Asset asset = Provider.GetAssetByPath(path);
                if (!Provider.CheckoutIsValid(asset) && !Provider.IsOpenForEdit(asset))
                {
                    Debug.Log("Item " + itemIdx.ToString() + ": Perforce checkout issue");
                    continue;
                }
                assetList.Add(asset);
                idxList.Add(itemIdx);
                // Debugging trackers
                failedOnCheckOut.Remove(itemIdx);
                failedOnSetup.Add(itemIdx);
            }
            // Checkout prefab asset list
            if (Provider.GetLatestIsValid(assetList))
            {
                Task GetLatestTask = Provider.GetLatest(assetList);
                GetLatestTask.Wait();
            }
            if (Provider.ResolveIsValid(assetList))
            {
                Task ResolveTask = Provider.Resolve(assetList, ResolveMethod.UseTheirs);
                ResolveTask.Wait();
            }
            checkoutOperation = Provider.Checkout(assetList, CheckoutMode.Both);
            checkoutOperation.Wait();

            // BEGIN SETTING UP PREFABS
            AssetDatabase.StartAssetEditing();
            int h = 0;
            try
            {
                for (h = 0; h < assetList.Count; h++)
                {
                    GameObject currentPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(assetList[h].assetPath, typeof(GameObject));
                    LODGroup LODs = currentPrefab?.GetComponent<LODGroup>();
                    // Only do if there is an LOD group at all
                    if (LODs)
                    {
                        StaticWeaponDataAsset dataAsset = currentPrefab.GetComponent<StaticWeaponDataAsset>();
                        if (dataAsset == null) continue;
                        if (dataAsset.LODs == null || dataAsset.LODs.Length == 0)
                            dataAsset.LODs = new Renderer[LODs.lodCount];
                        if (dataAsset.WeaponMeshAssetRefs == null || dataAsset.WeaponMeshAssetRefs.Length == 0)
                            dataAsset.WeaponMeshAssetRefs = new AssetReference[LODs.lodCount];

                        for (int i = 0; i < LODs.lodCount; i++)
                        {
                            // NOTE: lastIdx is the LAST PREFAB WITH A MESH FILTER
                            Renderer[] renderers = LODs.GetLODs()[i].renderers;
                            int lastIdx = renderers.Length - 1;
                            if (lastIdx < 0)
                            {
                                continue;
                            }
                            for (int j = lastIdx; j >= 0; j--)
                            {
                                if (renderers[j]?.GetComponent<MeshFilter>())
                                {
                                    lastIdx = j;
                                    break;
                                }
                            }
                            if(!renderers[lastIdx].GetComponent<MeshFilter>())
                            {
                                Debug.LogError("ERROR: LOD " + i.ToString() + " HAS NO MESH RENDERER!!");
                                continue;
                            }
                            if (lastIdx < 0)
                            {
                                Debug.LogError("ERROR: LOD " + i.ToString() + " HAS NO RENDERER!!");
                                continue;
                            }
                            if (renderers != null && renderers[lastIdx] != null)
                            {
                                // Add to LOD list in data asset
                                dataAsset.LODs[i] = renderers[lastIdx];

                                // Create mesh addressable
                                Mesh mesh = DataAssetProcessor.GetRendererMesh(renderers[lastIdx]);
                                if (mesh)
                                {
                                    AddressableAssetGroup meshGroup = settings.FindGroup("Meshes");
                                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh));
                                    var entry = settings.CreateOrMoveEntry(guid, meshGroup);
                                    entry.labels.Add(MasterItemDB.ItemArray[idxList[h]].ItemName + "_mesh" + i.ToString());
                                    entry.address = meshGroup.name + "/"+MasterItemDB.ItemArray[idxList[h]].ItemName +"/"+ guid;
                                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                                    // Add to mesh addressable list in data asset
                                    dataAsset.WeaponMeshAssetRefs[i] = new AssetReference(entry.guid);
                                    try
                                    {
                                        dataAsset.WeaponMeshAssetRefs[i].SetEditorSubObject(mesh);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError("ERROR WITH SETTING SUBOBJECT OF ADDRESSABLE: " + e.ToString());
                                    }
                                }
                                // Set mesh to null
                                DataAssetProcessor.SetRendererMesh(renderers[lastIdx], null);

                                // Create material addressable (only for i = 0 because *we assume* material is same for all LODs)
                                if (i == 0)
                                {
                                    Material material = renderers[lastIdx].sharedMaterial;
                                    if (material)
                                    {
                                        AddressableAssetGroup materialGroup = settings.FindGroup("Materials");
                                        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material));
                                        var entry = settings.CreateOrMoveEntry(guid, materialGroup);
                                        entry.labels.Add(MasterItemDB.ItemArray[idxList[h]].ItemName + "_material");
                                        entry.address = materialGroup.name + "/" + MasterItemDB.ItemArray[idxList[h]].ItemName +"/"+ guid;
                                        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                                        // Set material addressable in data asset
                                        dataAsset.WeaponMaterialAssetRef = new AssetReference(entry.guid);
                                    }
                                }
                                // Set material to null
                                renderers[lastIdx].sharedMaterial = null;
                            }
                        }
                        AssetDatabase.SaveAssets();
                        AssetDatabase.ImportAsset(assetList[h].assetPath);
                        // Debugging trackers
                        failedOnSetup.Remove(idxList[h]);
                        successfulSetup.Add(idxList[h]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Item " + idxList[h].ToString() + ": SERIOUS ERROR, TOOL FAILED AND WAS INTERRUPTED: " + e.ToString());
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();

            // Debugging trackers
            Debug.Log("Prefab doesn't exist: " + string.Join(", ", prefabDoesntExist));
            Debug.Log("Failed on checkout: " + string.Join(", ", failedOnCheckOut));
            Debug.Log("Failed on setup: " + string.Join(", ", failedOnSetup));
            Debug.Log("Successful setup: " + string.Join(", ", successfulSetup));
        }

        void OnGUI()
        {
            MasterItemDB = (HRItemDatabase)EditorGUILayout.ObjectField("Master Item DB", MasterItemDB, typeof(HRItemDatabase), allowSceneObjects: false);

            GUILayout.Label(new GUIContent("Set Up Weapons", "Sets up streaming data assets for all weapons"), EditorStyles.largeLabel);

            // SET UP WEAPON AT INDEX BUTTON
            itemIdx = EditorGUILayout.IntField(new GUIContent("Item Index", ""), itemIdx);
            if (GUILayout.Button(new GUIContent("Set up weapon at index", ""), GUILayout.Height(30)))
            {
                SetupWeapon(singleIdx : itemIdx);
            }

            // SET UP ALL WEAPONS BUTTON
            if (GUILayout.Button(new GUIContent("Set up ALL weapons", ""), GUILayout.Height(30)))
            {
                SetupWeapon();
            }
        }
    }
}
#endif