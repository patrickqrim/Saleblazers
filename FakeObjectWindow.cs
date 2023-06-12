#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Util;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEditor.VersionControl;
using Sirenix.Utilities;
using DotLiquid.Tags;
using UnityEngine.AddressableAssets;

public class FakeObjectWindow : BaseEditorWindowTool
{
    bool groupEnabled, prefabSpawnable, adminControl;
    public GameObject[] TargetList;
    public Object[] FolderList;
    public GameObject[] EditorTargetList;
    SerializedObject so;
    SerializedObject eso;
    SerializedObject fo;
    public int listSize;
    public FakeGeneratorDatabase fakeEditorDatabase;
    FakeObjectWindow target;
    public HRFakeObjectType fakeType;

    Vector2 scrollPos;

    public GameObject CopyTarget;
    public bool ManualSelection = false;

    Dictionary<int, GameObject> FakeMap;
    Dictionary<int, GameObject> FakeSpawnableMap;
    Dictionary<int, GameObject> SpawnableNavMeshMap;
    public string FakeDataBasePath = "Assets/Data Assets/FakeGeneratorDB/FakeGeneratorData.asset";
    public string FakePrefabPath = "Assets/Prefabs/Fake Prefabs";
    public string FakeSpawnablePrefabPath = "Assets/Prefabs/Fake Prefabs/Spawnable Fakes";
    public string MasterItemDBPath = "Assets/Data Assets/MasterItemDB.asset";
    public string TempPath;

    public bool Dirty = false;

    protected EmployeeSystem.Generation.GeneticProfileIconsDB iconsDB;
    protected GUIStyle style;
    protected GUIStyle iconstyle;

    [MenuItem("Airstrafe Tools/Fake Item Generator")]
    public static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        FakeObjectWindow window = (FakeObjectWindow)EditorWindow.GetWindow(typeof(FakeObjectWindow));
        window.fakeEditorDatabase = (FakeGeneratorDatabase)AssetDatabase.LoadAssetAtPath(window.FakeDataBasePath, typeof(FakeGeneratorDatabase));
        window.FakeMap = window.fakeEditorDatabase.GetFakeMap();
        window.FakeSpawnableMap = window.fakeEditorDatabase.GetFakeSpawnableMap();
        window.SpawnableNavMeshMap = window.fakeEditorDatabase.GetNavMeshSpawnableFakes();
        window.minSize = new Vector2(400, 250);
        window.target = window;
        window.Show();
    }

    private void OnEnable()
    {
        ScriptableObject target = this;
        so = new SerializedObject(target);
        eso = new SerializedObject(target);
        fo = new SerializedObject(target);

        iconsDB = (EmployeeSystem.Generation.GeneticProfileIconsDB)AssetDatabase.LoadAssetAtPath("Assets/Source/BudgetHero/Employees/EmployeeGeneration/GeneticProfileIconsDB.asset", typeof(EmployeeSystem.Generation.GeneticProfileIconsDB));

        style = new GUIStyle();
        style.richText = true;
        style.alignment = TextAnchor.MiddleLeft;
        style.fixedHeight = 20;
        style.stretchWidth = false;
        style.normal.textColor = Color.white;

        iconstyle = new GUIStyle();
        iconstyle.stretchWidth = false;
        iconstyle.alignment = TextAnchor.MiddleLeft;
        iconstyle.fixedHeight = 20;
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        /*
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        myString = EditorGUILayout.TextField("Text Field", myString);

        */

        GUILayout.Label("Fake Item Generator v4.23 ", EditorStyles.centeredGreyMiniLabel);

        groupEnabled = EditorGUILayout.BeginToggleGroup(new GUIContent("Configuration Settings", "Do not try to edit this normally"), groupEnabled);
        fakeEditorDatabase = (FakeGeneratorDatabase)EditorGUILayout.ObjectField(new GUIContent("Configuration", "Database used to identify what assets are needed to fake objects"), fakeEditorDatabase, typeof(FakeGeneratorDatabase), true);
        if (fakeEditorDatabase == null)
        {
            EditorGUILayout.HelpBox("Database is NULL", MessageType.Error);
        }
        EditorGUILayout.EndToggleGroup();

        EditorGUILayout.BeginHorizontal();
        ManualSelection = EditorGUILayout.Toggle(new GUIContent("Manual Selection", "If true, you need to drag into the list manually"), ManualSelection);
        EditorGUILayout.EndHorizontal();

        /*
         listSize = EditorGUILayout.IntField("Rock collection size", listSize);
         if (list != null && listSize != list.Length)
             list = new GameObject[listSize];
         for (int i = 0; i < listSize; i++)
         {
             list[i] = EditorGUILayout.ObjectField("Rock " + i.ToString(), list[i], typeof(GameObject), false) as GameObject;
         }
        */

        GUILayout.Label("Faking", EditorStyles.largeLabel);

        so.Update();
        SerializedProperty stringsProperty = so.FindProperty("TargetList");

        EditorGUILayout.PropertyField(stringsProperty, true); // True means show children
        so.ApplyModifiedProperties(); // Remember to apply modified properties

        if (!ManualSelection && !adminControl)
        {
            TargetList = Selection.gameObjects;
        }

        if (TargetList == null || TargetList.Length == 0) { EditorGUILayout.HelpBox("Target is NULL, add a target!", MessageType.Warning); }
        if (TargetList != null && TargetList.Length > 0)
        {
            foreach (GameObject g in TargetList)
            {
                if (TargetList[0] != null && TargetList[0].GetComponent<BaseWeapon>() && fakeEditorDatabase && (TargetList[0].GetComponent<BaseWeapon>().ItemID == -1 || fakeEditorDatabase.itemDatabase.ItemArray[TargetList[0].GetComponent<BaseWeapon>().ItemID].FakeType == HRFakeObjectType.None))
                {
                    if (TargetList[0].GetComponent<BaseWeapon>().ItemID == -1)
                    {
                        EditorGUILayout.HelpBox("This does not have a valid ItemID. Check the MasterItemDB", MessageType.Error);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("This object is not marked to be fake-able, generation will not work. Check the MasterItemDB", MessageType.Error);
                    }

                }
            }

        }

        float buttonSize = 30;
        if (GUILayout.Button(new GUIContent("FAKE Object and children", "Updates all children and target"), GUILayout.Height(buttonSize)))
        {
            GenerateFakesForChildren(TargetList);
            TargetList = CleanUpList(TargetList);
            Debug.Log("Replace Completed");
        }
        if (GUILayout.Button(new GUIContent("REGENERATE Object and children", "Updates all children and target"), GUILayout.Height(buttonSize)))
        {
            RegenerateFakesForChildren(TargetList);
            TargetList = CleanUpList(TargetList);
            Debug.Log("Regeneration Completed");
        }

        /*
        if (GUILayout.Button(new GUIContent("UPDATE Objects in children", "Updates all children and target"), GUILayout.Height(buttonSize)))
        {
            List<GameObject> realList = RegenerateFakesForChildren(TargetList);
            GenerateFakesForChildren(realList.ToArray());
            TargetList = CleanUpList(TargetList);
            Debug.Log("Update Complete");
        }
        */

        GUILayout.Space(buttonSize);

        prefabSpawnable = EditorGUILayout.Toggle(new GUIContent("Spawnable Prefab", "If true, these objects are set to be spawnable by the enemy spawners"), prefabSpawnable);
        if (prefabSpawnable && TargetList != null)
        {
            foreach (GameObject g in TargetList)
            {
                if (TargetList[0] != null && !TargetList[0].GetComponent<BaseObjectPoolingComponent>())
                {
                    EditorGUILayout.HelpBox(TargetList[0] + " is not spawnable, cannot update it. Add base pooling component", MessageType.Error);
                }
            }

        }


        if (GUILayout.Button(new GUIContent(prefabSpawnable ? "UPDATE FAKE Prefab" : "Update FAKE Prefab [Use with caution]", prefabSpawnable ? "Updates targets only" : "Updates all children and target"), GUILayout.Height(buttonSize)))
        {
            GenerateFakesForChildren(TargetList, true);
            TargetList = CleanUpList(TargetList);
            Debug.Log("Update Complete");
        }

        if (prefabSpawnable)
        {
            EditorGUILayout.LabelField("*To Update, REGENERATE the prefabs, then REPLACE them afterwards", EditorStyles.centeredGreyMiniLabel);
        }

        GUILayout.Space(buttonSize);

        ShowPing();

        GUILayout.Space(buttonSize);

        GUILayout.Label("Editor Tools", EditorStyles.largeLabel);

        eso.Update();
        SerializedProperty listProperty = eso.FindProperty("EditorTargetList");

        EditorGUILayout.PropertyField(listProperty, true); // True means show children
        eso.ApplyModifiedProperties(); // Remember to apply modified properties

        bool setFakeTypable = true;
        if (EditorTargetList != null)
        {
            for (int i = 0; i < EditorTargetList.Length; i++)
            {
                if (EditorTargetList[i] != null && EditorTargetList[i].GetComponent<BaseWeapon>() == null)
                {
                    EditorGUILayout.HelpBox("This object [" + EditorTargetList[i].name + "] cannot be edited", MessageType.Error);
                }
                else if (EditorTargetList[i] != null && EditorTargetList[i].GetComponent<BaseWeapon>() != null)
                {
                    GUI.enabled = false;
                    EditorGUILayout.BeginHorizontal();
                    if (EditorTargetList[i].GetComponent<BaseWeapon>().ItemID >= 0)
                    {
                        EditorGUILayout.LabelField(EditorTargetList[i].name + "'s Fake Type: ");
                        EditorGUILayout.LabelField("|" + fakeEditorDatabase.itemDatabase.ItemArray[EditorTargetList[i].GetComponent<BaseWeapon>().ItemID].FakeType, EditorStyles.boldLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(EditorTargetList[i].name + "'s Is Invalid Prefab, no ItemID assigned to it: ");
                        setFakeTypable = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    GUI.enabled = true;
                }
            }

        }

        GUILayout.Space(buttonSize);
        if (setFakeTypable)
        {
            fakeType = (HRFakeObjectType)EditorGUILayout.EnumPopup("Target Fake Type", fakeType);

            if (GUILayout.Button(new GUIContent("Set Fake Type", ""), GUILayout.Height(buttonSize)))
            {

                var asset = Provider.GetAssetByPath(MasterItemDBPath.ToString());

                if (Provider.CheckoutIsValid(asset) || Provider.IsOpenForEdit(asset))
                {
                    Task task = Provider.GetLatest(asset);
                    task.Wait();
                    task = Provider.Checkout(asset, CheckoutMode.Both);
                    task.Wait();

                    foreach (GameObject g in EditorTargetList)
                    {
                        if (g == null || g.GetComponent<BaseWeapon>() == null) return;
                        int ItemID = g.GetComponent<BaseWeapon>().ItemID;
                        fakeEditorDatabase.itemDatabase.ItemArray[ItemID].Data.FakeType = fakeType;
                        EditorUtility.SetDirty(fakeEditorDatabase.itemDatabase);
                        AssetDatabase.SaveAssetIfDirty(fakeEditorDatabase.itemDatabase);
                        Debug.Log("Turned " + g.name + " fake type to " + fakeType);
                        Ping("MasterItemDB is checked out by you. Remember to push on perforce!", MessageType.Info);
                    }
                }
                else
                {
                    Ping("MasterItemDB is checked out. Cannot modify it to set fake type for these object");
                }
            }
        }

        if (TargetList != null)
        {
            foreach (GameObject g in TargetList)
            {
                if (g != null && g.GetComponent<BaseWeapon>() && fakeEditorDatabase && g.GetComponent<BaseWeapon>().ItemID != -1 && fakeEditorDatabase.itemDatabase.ItemArray[g.GetComponent<BaseWeapon>().ItemID].FakeType == HRFakeObjectType.None)
                {
                    EditorGUILayout.HelpBox(g.name + " is not marked to be fake-able, generation will not work. Check the MasterItemDB", MessageType.Error);
                }
            }
        }

        GUILayout.Space(buttonSize);
        /*
        GUILayout.Label("Mesh Manipulation", EditorStyles.largeLabel);

        CopyTarget = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Copy Target", "Used to copy into another object"), CopyTarget, typeof(GameObject), true);
        if (GUILayout.Button(new GUIContent("Create Mesh Object Copy [Use with caution]", "Copies children of the target and converts them to meshes only into new object"), GUILayout.Height(buttonSize)))
        {
            GenerateMeshes(new GameObject[] { CopyTarget });
            Debug.Log("Copy Complete");
        }
        */


        if (fakeEditorDatabase != null)
        {
            adminControl = EditorGUILayout.BeginToggleGroup(new GUIContent("Admin", "Do not try to edit this normally"), adminControl);

            if (adminControl)
            {
                FolderList = Selection.objects;

                fo.Update();
                SerializedProperty folderProperty = fo.FindProperty("FolderList");

                EditorGUILayout.PropertyField(folderProperty, true); // True means show children
                fo.ApplyModifiedProperties(); // Remember to apply modified properties

                bool validFolders = true;
                for (int i = 0; i < FolderList.Length; i++)
                {
                    if (!AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(FolderList[i])))
                    {
                        validFolders = false;
                    }
                }

                if (GUILayout.Button(new GUIContent("Rename Fakes", ""), GUILayout.Height(buttonSize)))
                {
                    foreach (var selectedObject in Selection.objects)
                    {
                        GameObject g = selectedObject as GameObject;
                        if (g)
                        {
                            if (g.GetComponent<BaseReplace>())
                            {
                                bool spawnable = g.GetComponent<Mirror.NetworkIdentity>();
                                string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                                string directoryPath = System.IO.Path.GetDirectoryName(assetPath);
                                string oldFileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

                                BaseReplace br = g.GetComponent<BaseReplace>();
                                string name = (spawnable ? "FOS_" : "FO_") + br.ReplacePrefab.name + "_Fake" + (spawnable ? "_Spawnable" : "");
                                string newFilePath = directoryPath + "/" + name + System.IO.Path.GetExtension(assetPath);


                                AssetDatabase.RenameAsset(assetPath, name);
                                AssetDatabase.Refresh();
                            }
                        }

                    }
                }


                if (validFolders)
                {
                    if (GUILayout.Button(new GUIContent((prefabSpawnable ? "Get Spawnable Fakes" : "Fetch Fakes"), ""), GUILayout.Height(buttonSize)))
                    {
                        List<Object> Folders = new List<Object>();
                        Folders.AddRange(FolderList);
                        List<GameObject> Prefabs = new List<GameObject>();
                        for (int i = 0; i < Folders.Count; i++)
                        {
                            string path = AssetDatabase.GetAssetPath(Folders[i]);
                            string[] paths = System.IO.Directory.GetFiles(path);
                            string[] folderPaths = System.IO.Directory.GetDirectories(path);

                            if (!path.Contains("Spawnable Fakes"))
                            {
                                for (int j = 0; j < folderPaths.Length; j++)
                                {
                                    if (prefabSpawnable && folderPaths[j].Contains("Spawnable Fakes"))
                                    {
                                        paths = System.IO.Directory.GetFiles(folderPaths[j]);
                                        break;
                                    }
                                    else if (!folderPaths[j].Contains("Spawnable Fakes"))
                                    {
                                        string[] extraPaths = System.IO.Directory.GetFiles(folderPaths[j]);
                                        paths = paths.Concat(extraPaths).ToArray();
                                    }
                                }
                            }


                            for (int j = 0; j < paths.Length; j++)
                            {
                                if (!paths[j].Contains(".meta"))
                                {
                                    Prefabs.Add(AssetDatabase.LoadAssetAtPath(paths[j], typeof(GameObject)) as GameObject);
                                }
                                else if (AssetDatabase.IsValidFolder(paths[j]))
                                {
                                    Folders.Add(AssetDatabase.LoadAssetAtPath(paths[j], typeof(object)));
                                }
                            }
                            //Prefabs.AddRange(System.Array.ConvertAll(objects, item => item as GameObject));
                        }
                        GameObject[] targets = Prefabs.ToArray();

                        //RegenerateFakesForChildren(targets);
                        TargetList = targets;

                        Debug.Log(targets);
                        //GenerateFakesForChildren(targets);
                    }

                    if (GUILayout.Button(new GUIContent((prefabSpawnable ? "Apply Spawnable Fakes" : "Fetch Fakes"), ""), GUILayout.Height(buttonSize)))
                    {
                        List<Object> Folders = new List<Object>();
                        Folders.AddRange(FolderList);
                        List<GameObject> Prefabs = new List<GameObject>();
                        for (int i = 0; i < Folders.Count; i++)
                        {
                            string path = AssetDatabase.GetAssetPath(Folders[i]);
                            string[] paths = System.IO.Directory.GetFiles(path);
                            string[] folderPaths = System.IO.Directory.GetDirectories(path);

                            if (!path.Contains("Spawnable Fakes"))
                            {
                                for (int j = 0; j < folderPaths.Length; j++)
                                {
                                    if (prefabSpawnable && folderPaths[j].Contains("Spawnable Fakes"))
                                    {
                                        paths = System.IO.Directory.GetFiles(folderPaths[j]);
                                        break;
                                    }
                                    else if (!folderPaths[j].Contains("Spawnable Fakes"))
                                    {
                                        string[] extraPaths = System.IO.Directory.GetFiles(folderPaths[j]);
                                        paths = paths.Concat(extraPaths).ToArray();
                                    }
                                }
                            }


                            for (int j = 0; j < paths.Length; j++)
                            {
                                if (!paths[j].Contains(".meta"))
                                {
                                    Prefabs.Add(AssetDatabase.LoadAssetAtPath(paths[j], typeof(GameObject)) as GameObject);
                                }
                                else if (AssetDatabase.IsValidFolder(paths[j]))
                                {
                                    Folders.Add(AssetDatabase.LoadAssetAtPath(paths[j], typeof(object)));
                                }
                            }
                            //Prefabs.AddRange(System.Array.ConvertAll(objects, item => item as GameObject));
                        }
                        GameObject[] targets = Prefabs.ToArray();

                        TargetList = targets;

                        Debug.Log(targets);

                        Dictionary<int, GameObject> spawnableDictionary = new Dictionary<int, GameObject>();
                        spawnableDictionary = fakeEditorDatabase.GetFakeSpawnableMap();
                        foreach (GameObject g in TargetList)
                        {
                            Debug.Log(g.name);
                            if (!spawnableDictionary.ContainsKey(g.GetComponent<BaseReplace>().ReplacePrefab.GetComponent<BaseWeapon>().ItemID))
                            {
                                fakeEditorDatabase.AddFakeSpawnableEntry(new FakeData(g.GetComponent<BaseReplace>().ReplacePrefab.GetComponent<BaseWeapon>().ItemID, g));
                            }
                        }
                        //GenerateFakesForChildren(targets);
                    }

                }
            }

            EditorGUILayout.EndToggleGroup();
        }


        if (Dirty)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Dirty = false;
        }
        GUILayout.Label("Report to Kenny Doan for bugs, or more features!", EditorStyles.centeredGreyMiniLabel);
        //EditorGUILayout.BeginHorizontal();
        //Icons(iconsDB.geneticIcon, iconstyle, TotalTick % 10);
        //EditorGUILayout.EndHorizontal();
        //FillIcons(iconsDB.geneticIcon, iconsDB.mutationIcon, iconstyle, (int)(leftoverUsuableGenes / IconsPerGene), (int)(maximumUsableGenes / IconsPerGene), "");
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Creates a fake version of the objects in this object and children if it is possible
    /// </summary>
    /// <param name="targetGameObject"></param>
    private List<GameObject> GenerateFakesForChildren(GameObject targetGameObject, bool updatePrefab = false)
    {
        List<GameObject> returnList = new List<GameObject>();

        bool willUpdatePrefab = false;
        string assetPath = "";
        if (updatePrefab == true && PrefabUtility.GetPrefabInstanceStatus(targetGameObject) == PrefabInstanceStatus.NotAPrefab && targetGameObject.GetComponent<BaseReplace>()) { willUpdatePrefab = true; }
        else if (updatePrefab == true)
        {
            if (targetGameObject.GetComponent<BaseReplace>())
            {
                Debug.LogError("Target is NOT A PREFAB!");
                Ping("Target is NOT A PREFAB!");
                return null;
            }
            else if (targetGameObject.GetComponent<BaseWeapon>())
            {
                if ((prefabSpawnable && FakeSpawnableMap.ContainsKey(targetGameObject.GetComponent<BaseWeapon>().ItemID)) || (!prefabSpawnable && FakeMap.ContainsKey(targetGameObject.GetComponent<BaseWeapon>().ItemID)))
                {
                    Debug.Log(PrefabUtility.GetOutermostPrefabInstanceRoot(prefabSpawnable ? FakeSpawnableMap[targetGameObject.GetComponent<BaseWeapon>().ItemID] : FakeMap[targetGameObject.GetComponent<BaseWeapon>().ItemID]));
                    targetGameObject = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabSpawnable ? FakeSpawnableMap[targetGameObject.GetComponent<BaseWeapon>().ItemID] : FakeMap[targetGameObject.GetComponent<BaseWeapon>().ItemID]);
                    updatePrefab = true;
                    willUpdatePrefab = true;
                }
                else
                {
                    Debug.LogError("Target is NOT A FAKE PREFAB TO UPDATE!");
                    Ping("Target is NOT A FAKE PREFAB TO UPDATE!");
                }

                //Debug.Log(PrefabUtility.GetOutermostPrefabInstanceRoot(targetGameObject));


                //return null;
            }
        }
        else if (updatePrefab == true && PrefabUtility.GetPrefabInstanceStatus(targetGameObject) == PrefabInstanceStatus.NotAPrefab)
        {
            Debug.LogError("Cannot normal fake a prefab! Use it on a scene object");
            Ping("Cannot normal fake a prefab! Use it on a scene object");
            return null;
        }
        if (willUpdatePrefab)
        {
            assetPath = AssetDatabase.GetAssetPath(targetGameObject);
            TempPath = assetPath;
            targetGameObject = PrefabUtility.LoadPrefabContents(assetPath);
        }

        List<Transform> targets = new List<Transform>() { targetGameObject.transform };
        while (targets.Count > 0)
        {
            int children = targets[0].transform.childCount;
            int progress = 0;

            for (int i = children - 1; i >= 0; --i)
            {
                if (targets[0].transform.GetChild(i).GetComponent<FakeMyChildren>()) { progress++; targets.Add(targets[0].transform.GetChild(i)); continue; }
                if (targets[0].transform.GetChild(i).GetComponent<DontFakeMe>()) { progress++; continue; }
                GameObject g = GenerateFake(targets[0].transform.GetChild(i).gameObject, updatePrefab);
                if (g != null)
                {
                    Debug.Log("Faked: " + targets[0].transform.GetChild(i).name);
                    EditorUtility.DisplayProgressBar("Generating Fakes", "Faking - " + targets[0].transform.GetChild(i).name, ((float)progress / children));
                    returnList.Add(g);
                }
                progress++;
            }
            targets.RemoveAt(0);
        }

        if (willUpdatePrefab)
        {
            if (returnList.Count == 0) { targetGameObject = GenerateFake(targetGameObject, updatePrefab); }
            bool successfullPrefabUpdate = false;
            //PrefabUtility.SavePrefabAsset(targetGameObject);

            PrefabUtility.SaveAsPrefabAsset(targetGameObject, assetPath, out successfullPrefabUpdate);
            //PrefabUtility.UnloadPrefabContents(targetGameObject);
            //if (returnList.Count == 0) { returnList.Add(targetGameObject); }
            if (successfullPrefabUpdate) { Debug.Log("Successful update for prefab"); }
            else { Debug.Log("Failed update for prefab"); }
        }
        else if (!targetGameObject.GetComponent<DontFakeMe>())
        {
            GameObject fake = GenerateFake(targetGameObject, updatePrefab);
            if (fake != null)
            {
                returnList.Add(fake);
            }
        }
        EditorUtility.ClearProgressBar();
        return returnList;
    }
    private List<GameObject> GenerateFakesForChildren(GameObject[] targetGameObjects, bool updatePrefab = false)
    {
        FakeMap = fakeEditorDatabase.GetFakeMap();
        FakeSpawnableMap = fakeEditorDatabase.GetFakeSpawnableMap();
        SpawnableNavMeshMap = fakeEditorDatabase.GetNavMeshSpawnableFakes();
        List<GameObject> returnList = new List<GameObject>();
        foreach (GameObject g in targetGameObjects)
        {
            if (g != null && g.GetComponent<DontFakeMe>()) { continue; }
            List<GameObject> list = GenerateFakesForChildren(g, updatePrefab);
            if (list != null && list.Count > 0)
            {
                returnList.AddRange(list);
            }
        }
        return returnList;
    }
    private GameObject GenerateFake(GameObject targetGameObject, bool updatePrefab = false)
    {
        //Debug.Log(PrefabUtility.GetPrefabInstanceStatus(targetGameObject) + " | " + PrefabUtility.GetPrefabAssetType(targetGameObject) + " | " + targetGameObject.name); return null;
        //if(updatePrefab && PrefabUtility.GetPrefabInstanceStatus(targetGameObject))
        bool valid = false;
        if (targetGameObject.GetComponent<BaseWeapon>()) valid = true;
        if (updatePrefab && targetGameObject.GetComponent<BaseReplace>()) valid = true;
        if (!valid) return null;


        int prefabID = (updatePrefab && targetGameObject.GetComponent<BaseReplace>()) ? targetGameObject.GetComponent<BaseReplace>().ReplacePrefab.GetComponent<BaseWeapon>().ItemID : targetGameObject.GetComponent<BaseWeapon>().ItemID;
        if (prefabID < 0 || fakeEditorDatabase.itemDatabase.ItemArray[prefabID] == null || fakeEditorDatabase.itemDatabase.ItemArray[prefabID].FakeType == HRFakeObjectType.None) { return null; }

        GameObject OriginalPrefab = fakeEditorDatabase.itemDatabase.ItemArray[prefabID].ItemPrefab;
        if (OriginalPrefab == null) { Debug.LogError("Master Item DB entry " + prefabID + " is missing a prefab!!!"); return null; }


        if (!updatePrefab && ((prefabSpawnable && FakeSpawnableMap.ContainsKey(prefabID)) || (!prefabSpawnable && FakeMap.ContainsKey(prefabID))))
        {
            GameObject fakeItem = (GameObject)PrefabUtility.InstantiatePrefab(prefabSpawnable ? FakeSpawnableMap[prefabID] : FakeMap[prefabID], targetGameObject.scene);

            CopyID(targetGameObject, fakeItem, updatePrefab);
            CopyTransforms(targetGameObject, fakeItem);

            ReplaceOnHitInteract hitInteract = fakeItem.GetComponent<ReplaceOnHitInteract>();
            if (hitInteract)
            {
                CopySecurity(targetGameObject, hitInteract);
            }

            BaseReplace baseFake = fakeItem.GetComponent<BaseReplace>();
            if (baseFake)
            {
                CreateLight(targetGameObject, baseFake);

                ApplyLootData(targetGameObject, baseFake);
            }
            DestroyImmediate(targetGameObject);
            return fakeItem;
        }
        else if ((prefabSpawnable && !FakeSpawnableMap.ContainsKey(prefabID)) || (!prefabSpawnable && !FakeMap.ContainsKey(prefabID)))
        {
            var asset = Provider.GetAssetByPath(FakeDataBasePath.ToString());

            if (Provider.CheckoutIsValid(asset) || Provider.IsOpenForEdit(asset))
            {
                Task task = Provider.GetLatest(asset);
                task.Wait();
                task = Provider.Checkout(asset, CheckoutMode.Both);
                task.Wait();
            }
            else
            {
                Ping("Fake Generator Database is checked out. Cannot add a new fake for " + targetGameObject.name);
                return null;
            }
        }

        GameObject ItemPrefab = (PrefabUtility.InstantiatePrefab(OriginalPrefab) as GameObject);
        //ItemPrefab.GetComponent<BaseWeaponDataAsset>()?.EDITOR_ApplyDataAssets();

        string fakeTypeString = "";
        GameObject TemplatePrefab = null;
        HRFakeObjectType fakeType = fakeEditorDatabase.itemDatabase.ItemArray[prefabID].FakeType;
        switch (fakeType)
        {
            case HRFakeObjectType.OnHit:
                fakeTypeString = "_On_Hit";
                TemplatePrefab = prefabSpawnable ? fakeEditorDatabase.OnHitPrefabTemplate_Spawnable : fakeEditorDatabase.OnHitPrefabTemplate;
                break;
            case HRFakeObjectType.OnHitAndInteractable:
                fakeTypeString = "_On_Interactable_Hit";
                TemplatePrefab = prefabSpawnable ? fakeEditorDatabase.OnHitandInteractablePrefabTemplate_Spawnable : fakeEditorDatabase.OnHitandInteractablePrefabTemplate;
                break;
            case HRFakeObjectType.OnHitAndPickupable:
                fakeTypeString = "_On_Pickupable_Hit";
                TemplatePrefab = prefabSpawnable ? fakeEditorDatabase.OnHitandInteractablePrefabTemplate_Spawnable : fakeEditorDatabase.OnHitandInteractablePrefabTemplate;
                break;
            case HRFakeObjectType.OnHitInteractPickup:
                fakeTypeString = "_On_InteractPickup_Hit";
                TemplatePrefab = prefabSpawnable ? fakeEditorDatabase.OnHitandInteractablePrefabTemplate_Spawnable : fakeEditorDatabase.OnHitandInteractablePrefabTemplate;
                break;
            case HRFakeObjectType.MeshOnly:
                fakeTypeString = "_MeshOnly";
                TemplatePrefab = fakeEditorDatabase.FakeMeshPrefabTemplate;
                break;
        }

        // Create the fake object base
        UnityEngine.SceneManagement.Scene scene = targetGameObject.scene;
        GameObject newWeapon = (GameObject)PrefabUtility.InstantiatePrefab(TemplatePrefab, scene.name != null ? scene : EditorSceneManager.GetActiveScene()); 
        newWeapon.name = ItemPrefab.name.Replace("PF", (prefabSpawnable ? "FOS" : "FO")) + "_Fake" + (prefabSpawnable ? "_Spawnable" : "");

        if (updatePrefab)
        {
            targetGameObject = ItemPrefab;
        }

        //Mirror.NetworkServer.Spawn(newWeapon); // Remove this in final

        // Go through and retrieve all the mesh lods and create the game objects for them
        LODGroup prefabLODGroup = ItemPrefab.GetComponentInChildren<LODGroup>();
        if (prefabLODGroup == null) { return null; }

        // Copy the LODS from the prefab
        LODGroup targetLOD = newWeapon.GetComponentInChildren<LODGroup>();

        // Set up default for mesh GO
        GameObject meshGOTemplate = new GameObject();
        meshGOTemplate.AddComponent(typeof(MeshFilter));
        meshGOTemplate.AddComponent(typeof(MeshRenderer));
        GameObject billboardGOTemplate = new GameObject();
        billboardGOTemplate.AddComponent(typeof(BillboardRenderer));

        // Copy the get the LOD of the prefab and copy it
        LOD[] NewLods = prefabLODGroup.GetLODs();
        Transform targetMeshTransform = newWeapon.transform.Find("Mesh");
        for (int i = 0; i < NewLods.Length; i++)
        {
            for (int j = 0; j < NewLods[i].renderers.Length; j++)
            {
                Renderer[] renderers = NewLods[i].renderers;
                GameObject meshGO = null;
                if (renderers[j] == null || renderers[j].gameObject.activeSelf == false) { continue; }
                Transform target = targetMeshTransform;
                if (!renderers[j].GetComponent<BillboardRenderer>()) // If there is no billboard, then do normal mesh renderer
                {
                    if (renderers[j].gameObject.name != "Mesh")
                    {
                        meshGO = Instantiate(meshGOTemplate, targetMeshTransform);
                        target = meshGO.transform;
                        if (renderers[j].GetComponent<Trees.TreeBillboard>())
                        {
                            meshGO.AddComponent<Trees.TreeBillboard>();
                            meshGO.GetComponent<Trees.TreeBillboard>().BillboardData = renderers[j].GetComponent<Trees.TreeBillboard>().BillboardData;
                            meshGO.GetComponent<Trees.TreeBillboard>().TargetMesh = target.GetComponent<MeshRenderer>();
                        }
                    }
                    target.GetComponent<MeshFilter>().sharedMesh = renderers[j].GetComponent<MeshFilter>() ? renderers[j].GetComponent<MeshFilter>().sharedMesh : renderers[j].GetComponent<SkinnedMeshRenderer>().sharedMesh;
                    target.GetComponent<MeshRenderer>().sharedMaterials = renderers[j].sharedMaterials;
                    target.GetComponent<MeshRenderer>().shadowCastingMode = renderers[j].shadowCastingMode;
                    target.GetComponent<MeshRenderer>().staticShadowCaster = renderers[j].staticShadowCaster;
                    target.GetComponent<MeshRenderer>().allowOcclusionWhenDynamic = renderers[j].allowOcclusionWhenDynamic;
                    target.GetComponent<MeshRenderer>().lightProbeUsage = renderers[j].lightProbeUsage;
                    target.GetComponent<MeshRenderer>().renderingLayerMask = renderers[j].renderingLayerMask;
                    target.GetComponent<MeshRenderer>().receiveShadows = renderers[j].receiveShadows;

                }
                else
                {
                    meshGO = Instantiate(billboardGOTemplate, targetMeshTransform);
                    meshGO.GetComponent<BillboardRenderer>().billboard = renderers[j].GetComponent<BillboardRenderer>().billboard;
                    target = meshGO.transform;
                }

                if (target != targetMeshTransform)
                {

                    target = meshGO.transform;
                }
                target.name = renderers[j].gameObject.name;
                target.gameObject.layer = renderers[j].gameObject.layer;
                target.gameObject.tag = renderers[j].gameObject.tag;
                target.localPosition = renderers[j].transform.localPosition;
                target.localScale = renderers[j].transform.localScale;
                target.localRotation = renderers[j].transform.localRotation;
                renderers[j] = target.GetComponent<Renderer>();

            }
        }
        targetLOD.animateCrossFading = true;
        targetLOD.SetLODs(NewLods); // Set the lods into our target object
        targetLOD.fadeMode = LODFadeMode.CrossFade;
        targetLOD.size = prefabLODGroup.size;
        //targetLOD.RecalculateBounds();

        // If its a normal fake, otherwise a mesh fake
        BaseReplace baseReplace = newWeapon.GetComponent<BaseReplace>();
        if (baseReplace)
        {
            baseReplace.ReplacePrefab = OriginalPrefab;
        }
        else if (newWeapon.GetComponent<FakeMesh>())
        {
            newWeapon.GetComponent<FakeMesh>().PrefabItem = OriginalPrefab;
            newWeapon.GetComponent<FakeMesh>().SavedComponentID = OriginalPrefab.GetComponent<BaseIDComponent>().GetUniqueID();
        }

        // SETUP DATA ASSET
        StaticWeaponDataAsset fakeDataAsset = newWeapon.GetComponent<StaticWeaponDataAsset>();
        if (fakeDataAsset == null)
        {
            Debug.LogError("FAKE ITEM DOESN'T HAVE DATA ASSET COMPONENT");
            return null;
        }

        // Set up LODs
        fakeDataAsset.LODs = new Renderer[NewLods.Length];
        for (int i = 0; i < NewLods.Length; i++)
        {
            Renderer[] renderers = NewLods[i].renderers;
            // NOTE: lastIdx is the LAST PREFAB WITH A MESH FILTER
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
            fakeDataAsset.LODs[i] = renderers[lastIdx];

            // Null out mesh and material
            DataAssetProcessor.SetRendererMesh(renderers[lastIdx], null);
            renderers[lastIdx].sharedMaterial = null;
        }

        // Set up Addressables
        StaticWeaponDataAsset originalDataAsset = OriginalPrefab.GetComponent<StaticWeaponDataAsset>();
        if (fakeDataAsset.WeaponMeshAssetRefs == null || fakeDataAsset.WeaponMeshAssetRefs.Length == 0)
            fakeDataAsset.WeaponMeshAssetRefs = new AssetReference[NewLods.Length];
        for (int i = 0; i < NewLods.Length; i++)
        {
            fakeDataAsset.WeaponMeshAssetRefs[i] = originalDataAsset.WeaponMeshAssetRefs[i];
        }
        fakeDataAsset.WeaponMaterialAssetRef = originalDataAsset.WeaponMaterialAssetRef;


        // Copy eqs checker
        /* Deprecated, eqs should be in the prefab to begin with
        if (newWeapon.GetComponent<BaseReplace>() && ItemPrefab.GetComponent<BaseEQSChecker>() != null)
        {
            if (newWeapon.GetComponent<BaseEQSChecker>() == null)
            {
                newWeapon.AddComponent<BaseEQSChecker>();
            }
            newWeapon.GetComponent<BaseEQSChecker>().bIsFakeChecker = true;
        }
        */

        // Copy the baseDamage reciever properties
        /*
        if (newWeapon.GetComponent<BaseReplace>() && ItemPrefab.GetComponent<BaseDamageReceiver>() != null)
        {
            newWeapon.GetComponent<BaseDamageReceiver>().MeshDamageFlash = null; // No flashies for fakes
            newWeapon.GetComponent<BaseDamageReceiver>().ItemEXPType = ItemPrefab.GetComponent<BaseDamageReceiver>().ItemEXPType;
            newWeapon.GetComponent<BaseDamageReceiver>().DamageTier = ItemPrefab.GetComponent<BaseDamageReceiver>().DamageTier;
            newWeapon.GetComponent<BaseDamageReceiver>().bAllowLowHPSecondChance = ItemPrefab.GetComponent<BaseDamageReceiver>().bAllowLowHPSecondChance;
            newWeapon.GetComponent<BaseDamageReceiver>().bRoundToInteger = ItemPrefab.GetComponent<BaseDamageReceiver>().bRoundToInteger;
        }
        */

        HRLightComponent lightComponent = OriginalPrefab.GetComponent<HRLightComponent>();

        if (baseReplace && lightComponent)
        {
            CreateLight(OriginalPrefab, baseReplace);
        }

        ApplyLootData(OriginalPrefab, baseReplace);

        // Generate the colliders and copy properties
        AddCollidersTo(targetMeshTransform.Find("Collider"), OriginalPrefab.GetComponent<BaseWeapon>().MeshColliders);

        if (newWeapon.GetComponent<ReplaceOnHitInteract>() && OriginalPrefab.GetComponent<BaseWeapon>().OwningInteractable)
        {
            ReplaceOnHitInteract hitInteract = newWeapon.GetComponent<ReplaceOnHitInteract>();
            BaseScripts.BaseInteractable refInteract = OriginalPrefab.GetComponent<BaseWeapon>().OwningInteractable;
            List<Collider> colliderList = AddCollidersTo(hitInteract.interactable.transform, OriginalPrefab.GetComponent<BaseWeapon>().OwningInteractable.InteractionCollisions);
            hitInteract.interactable.InteractionCollisions = colliderList.ToArray();
            //ComponentUtil.CopyComponent<BaseScripts.BaseInteractable>(refInteract, hitInteract.interactable.gameObject);
            hitInteract.interactable.InteractionCollisionStartEnabled = refInteract.InteractionCollisionStartEnabled;
            hitInteract.interactable.bIsToggleInteraction = refInteract.bIsToggleInteraction;
            hitInteract.interactable.bUseHoldInteraction = refInteract.bUseHoldInteraction;
            hitInteract.interactable.bUseTapHoldInteraction = refInteract.bUseTapHoldInteraction;
            hitInteract.interactable.HoldInteractionDuration = refInteract.HoldInteractionDuration;
            hitInteract.interactable.bShowUIOnHover = refInteract.bShowUIOnHover;
            hitInteract.interactable.InteractionWidgetOffset = refInteract.InteractionWidgetOffset;
            hitInteract.interactable.TapInteractionDescription = refInteract.TapInteractionDescription;
            hitInteract.interactable.HoldInteractionDescription = refInteract.HoldInteractionDescription;
            hitInteract.interactable.LocalTapVFX = refInteract.LocalTapVFX;
            hitInteract.interactable.SetInteractionName(OriginalPrefab.GetComponent<BaseWeapon>().ItemName);
            hitInteract.interactable.AttributeManager = null;
            hitInteract.interactable.HealthComponentToShow = null;

            // Set subsubsribing booleans
            if (fakeType == HRFakeObjectType.OnHitAndInteractable) { hitInteract.IsTap = true; }
            if (fakeType == HRFakeObjectType.OnHitAndPickupable) { hitInteract.IsHold = true; }
            if (fakeType == HRFakeObjectType.OnHitInteractPickup) { hitInteract.IsTap = true; hitInteract.IsHold = true; }

            CopySecurity(OriginalPrefab, hitInteract);
        }

        if (prefabSpawnable)
        {
            // Copy navmesh cut if applicable. If the target has it or not
            if (SpawnableNavMeshMap.ContainsKey(prefabID) && OriginalPrefab.GetComponent<Pathfinding.NavmeshCut>() != null)
            {
                if (newWeapon.GetComponent<Pathfinding.NavmeshCut>() == null)
                {
                    newWeapon.AddComponent<Pathfinding.NavmeshCut>();
                }
                Utils.CopyComponent<Pathfinding.NavmeshCut>(OriginalPrefab.GetComponent<Pathfinding.NavmeshCut>(), newWeapon);
                newWeapon.GetComponent<Pathfinding.NavmeshCut>().hideFlags = HideFlags.None;
            }

            /* Deprecated, BaseObjectPoolingComponent should be in the prefab to begin with
            if (newWeapon.GetComponent<BaseObjectPoolingComponent>() == null)
            {
                newWeapon.AddComponent<BaseObjectPoolingComponent>().hideFlags = HideFlags.None;
            }
            newWeapon.GetComponent<Mirror.NetworkIdentity>()?.CreateNetworkBehavioursCache();
            */
        }

        // Copy baseIDComponent if applicable
        CopyID(targetGameObject, newWeapon, updatePrefab);

        // Copy world info
        CopyTransforms(targetGameObject, newWeapon);

        string finalString = "/" + newWeapon.name + ".prefab";
        string path = updatePrefab ? TempPath : (prefabSpawnable ? FakeSpawnablePrefabPath : FakePrefabPath) + finalString;

        if (!updatePrefab) { DestroyImmediate(targetGameObject); }
        DestroyImmediate(meshGOTemplate);
        DestroyImmediate(billboardGOTemplate);


        bool successfulDatabaseUpdate = false;
        //GameObject newPrefab = PrefabUtility.prefa( FakePrefabPath + "/" + newWeapon.name + ".asset", newWeapon);

        //PrefabUtility.CreatePrefab(path,newWeapon);
        Vector3 pos = newWeapon.transform.position;
        Vector3 angle = newWeapon.transform.localEulerAngles;

        newWeapon.transform.position = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;

        GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(newWeapon, path, out successfulDatabaseUpdate);

        newWeapon.transform.position = pos;
        newWeapon.transform.localEulerAngles = angle;

        //GameObject asset = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
        if (!prefabSpawnable)
        {
            if (!FakeMap.ContainsKey(prefabID) || FakeMap[prefabID] == null)
            {
                FakeMap.Add(prefabID, newPrefab);
                fakeEditorDatabase.AddFakeEntry(new FakeData(prefabID, newPrefab));
                EditorUtility.SetDirty(fakeEditorDatabase);
            }
            Dirty = true;
        }
        else
        {
            if (!FakeSpawnableMap.ContainsKey(prefabID) || FakeSpawnableMap[prefabID] == null)
            {
                FakeSpawnableMap.Add(prefabID, newPrefab);
                fakeEditorDatabase.AddFakeSpawnableEntry(new FakeData(prefabID, newPrefab));
                EditorUtility.SetDirty(fakeEditorDatabase);
            }
            Dirty = true;
        }

        DestroyImmediate(ItemPrefab);

        return newWeapon;
    }

    public void CopyTransforms(GameObject reference, GameObject target)
    {
        CopyTransformsBase(reference, target);
        target.transform.parent = reference.transform.parent;
        target.transform.SetSiblingIndex(reference.transform.GetSiblingIndex());
    }

    public void CopyTransformsBase(GameObject reference, GameObject target)
    {
        target.transform.position = reference.transform.position;
        target.transform.rotation = reference.transform.rotation;
        target.transform.localScale = reference.transform.localScale;
    }

    public void CopyID(GameObject reference, GameObject target, bool updatePrefab)
    {
        if (target.GetComponent<BaseIDComponent>())
        {
            target.GetComponent<BaseIDComponent>().SetUniqueID(reference.GetComponent<BaseIDComponent>().GetUniqueID(false), bAddToGameManager: false);
            target.GetComponent<BaseIDComponent>().PlacedInLevel = prefabSpawnable ? false : true;
            target.GetComponent<BaseIDComponent>().AutoGenerateUniqueID = true;
        }
    }

    public void CopySecurity(GameObject targetGameObject, ReplaceOnHitInteract hitInteract)
    {
        BaseLockedItem lockScript = targetGameObject.GetComponent<BaseLockedItem>();
        if (lockScript)
        {
            BaseWeapon weapon = targetGameObject.GetComponent<BaseWeapon>();
            if (weapon && weapon.bCanPickup == false)
            {
                hitInteract.SecurityLevel = 99;
                hitInteract.Locked = true;
            }
            else
            {
                hitInteract.SecurityLevel = lockScript.SecurityLevel;
                hitInteract.Locked = lockScript.IsLockedFromField;
            }

        }
    }
    public void CopySecurity(GameObject targetGameObject, BaseLockedItem lockScript)
    {
        ReplaceOnHitInteract hitInteract = targetGameObject.GetComponent<ReplaceOnHitInteract>();
        if (hitInteract)
        {
            BaseWeapon weapon = lockScript.GetComponent<BaseWeapon>();
            if (weapon && hitInteract.SecurityLevel == 99 && hitInteract.Locked)
            {
                lockScript.EDITOR_SetSecurityLevel(99);
                weapon.bCanPickup = false;
            }
            else
            {
                lockScript.EDITOR_SetSecurityLevel(hitInteract.SecurityLevel);
            }
            lockScript.EDITOR_SetLocked(hitInteract.Locked);
        }
    }

    public void CreateLight(GameObject targetGameObject, BaseReplace baseReplace)
    {
        HRLightComponent lightComponent = targetGameObject.GetComponent<HRLightComponent>();
        if (lightComponent)
        {
            Light l = lightComponent.lightLOD.targetLight;

            if (lightComponent.lightLOD == null)
            {
                Ping("THIS OBJECT DOES NOT HAVE A LIGHT LOD IN IT'S HRLIIGHTCOMPONENT");
            }

            Light fakeLight = null;

            if (IntegerMask.ReadDigit(baseReplace.data, ((int)FakeDataDigits.LIGHTS)) == 0)
            {
                GameObject lightObject = ((GameObject)PrefabUtility.InstantiatePrefab(fakeEditorDatabase.LightPrefab));
                lightObject.name = "Light";
                lightObject.transform.parent = baseReplace.transform.Find("Mesh");
                lightObject.transform.CopyLocalProperties(l.gameObject.transform);

                fakeLight = lightObject.GetComponent<Light>();
                //fakeLight = lightObject.AddComponent<Light>();
            }
            else
            {
                fakeLight = baseReplace.transform.Find("Mesh").Find("Light").GetComponent<Light>();
            }

            bool enabledLight = lightComponent.lightLOD.gameObject.activeSelf;

            fakeLight.CopyProperties(l);

            /*
            fakeLight.tag = l.tag;
            fakeLight.gameObject.layer = l.gameObject.layer;
            fakeLight.flare = l.flare;
            fakeLight.cullingMask = l.cullingMask;
            fakeLight.cookie = l.cookie;
            fakeLight.cookieSize = l.cookieSize;
            fakeLight.color = l.color;
            fakeLight.colorTemperature = l.colorTemperature;
            fakeLight.boundingSphereOverride = l.boundingSphereOverride;
            fakeLight.bounceIntensity = l.bounceIntensity;
            fakeLight.areaSize = l.areaSize;
            fakeLight.layerShadowCullDistances = l.layerShadowCullDistances;
            fakeLight.intensity = l.intensity;
            fakeLight.innerSpotAngle = l.innerSpotAngle;
            fakeLight.lightmapBakeType = l.lightmapBakeType;
            fakeLight.lightShadowCasterMode = l.lightShadowCasterMode;
            fakeLight.renderingLayerMask = l.renderingLayerMask;
            fakeLight.renderMode = l.renderMode;
            fakeLight.shape = l.shape;
            fakeLight.range = l.range;
            fakeLight.useColorTemperature = l.useColorTemperature;
            fakeLight.type = l.type;
            fakeLight.shadows = l.shadows;
            */

            fakeLight.gameObject.SetActive(enabledLight);

            baseReplace.data = IntegerMask.ReplaceDigit(baseReplace.data, ((int)FakeDataDigits.LIGHTS), enabledLight ? 2 : 1);
        }
    }

    public void ApplyLootData(GameObject referenceGameObject, BaseReplace baseReplace)
    {
        BaseInventory inventoryComponent = referenceGameObject.GetComponent<BaseInventory>();
        if (baseReplace && inventoryComponent && inventoryComponent.useVariantLootTables)
        {
            string addedString = inventoryComponent.TargetLootTable <= 9 ? "0" : "";
            string lootData = addedString + inventoryComponent.TargetLootTable.ToString();
            baseReplace.data = IntegerMask.ReplaceDigit(baseReplace.data, ((int)FakeDataDigits.LOOTVARIANTFIRSTDIGIT), int.Parse(lootData[0].ToString()));
            baseReplace.data = IntegerMask.ReplaceDigit(baseReplace.data, ((int)FakeDataDigits.LOOTVARIANTSECONDDIGIT), int.Parse(lootData[1].ToString()));
        }
    }

    public void ApplyLootDataFromFake(GameObject referenceGameObject, BaseReplace baseReplace)
    {
        BaseInventory inventoryComponent = referenceGameObject.GetComponent<BaseInventory>();
        if (baseReplace && inventoryComponent && inventoryComponent.useVariantLootTables)
        {
            int firstDigit = IntegerMask.ReadDigit(baseReplace.data, ((int)FakeDataDigits.LOOTVARIANTFIRSTDIGIT));
            int secondDigit = IntegerMask.ReadDigit(baseReplace.data, ((int)FakeDataDigits.LOOTVARIANTSECONDDIGIT));
            int value = (firstDigit * 10) + secondDigit;
            if (value > 0)
            {
                inventoryComponent.TargetLootTable = value;
            }
        }
    }

    /// <summary>
    /// Returns the fake prefab back to real object. Assuming no overrides during scene edit
    /// </summary>
    /// <param name="targetGameObject"></param>
    private List<GameObject> RegenerateFakesForChildren(GameObject targetGameObject, bool updatePrefab = false)
    {
        List<GameObject> returnList = new List<GameObject>();

        bool willUpdatePrefab = false;
        string assetPath = "";
        if (updatePrefab == true && PrefabUtility.GetPrefabInstanceStatus(targetGameObject) == PrefabInstanceStatus.NotAPrefab && targetGameObject.GetComponent<BaseWeapon>()) { willUpdatePrefab = true; }
        else if (updatePrefab == true) { Debug.LogError("Target is NOT A PREFAB!"); Ping("Target is NOT A PREFAB!"); return null; }
        else if (updatePrefab == true && PrefabUtility.GetPrefabInstanceStatus(targetGameObject) == PrefabInstanceStatus.NotAPrefab)
        {
            Debug.LogError("Cannot normal regenerate a prefab! Use it on a scene object");
            Ping("Cannot normal regenerate a prefab! Use it on a scene object");
            return null;
        }
        if (willUpdatePrefab)
        {
            assetPath = AssetDatabase.GetAssetPath(targetGameObject);
            TempPath = assetPath;
            targetGameObject = PrefabUtility.LoadPrefabContents(assetPath);
        }

        List<Transform> targets = new List<Transform>() { targetGameObject.transform };
        while (targets.Count > 0)
        {
            int children = targets[0].transform.childCount;
            int progress = 0;

            for (int i = children - 1; i >= 0; --i)
            {
                if (targets[0].transform.GetChild(i).GetComponent<FakeMyChildren>()) { progress++; targets.Add(targets[0].transform.GetChild(i)); continue; }
                if (targets[0].transform.GetChild(i).GetComponent<DontFakeMe>()) { progress++; continue; }
                GameObject g = RegenerateFake(targets[0].transform.GetChild(i).gameObject, updatePrefab);
                if (g != null)
                {
                    EditorUtility.DisplayProgressBar("Generating Fakes", "Regenerating - " + targets[0].name, ((float)progress / children));
                    returnList.Add(g);
                }
                progress++;
            }
            targets.RemoveAt(0);
        }
        if (willUpdatePrefab)
        {
            if (returnList.Count == 0) { targetGameObject = RegenerateFake(targetGameObject, updatePrefab); }
            bool successfullPrefabUpdate = false;
            PrefabUtility.SaveAsPrefabAsset(targetGameObject, assetPath, out successfullPrefabUpdate);
            PrefabUtility.UnloadPrefabContents(targetGameObject);
            if (successfullPrefabUpdate) { Debug.Log("Successful update for prefab"); }
            else { Debug.Log("Failed update for prefab"); }

        }
        else if (!targetGameObject.GetComponent<DontFakeMe>())
        {
            GameObject real = RegenerateFake(targetGameObject, updatePrefab);
            if (real != null)
            {
                returnList.Add(real);
            }
        }
        EditorUtility.ClearProgressBar();
        return returnList;
    }
    private List<GameObject> RegenerateFakesForChildren(GameObject[] targetGameObjects, bool updatePrefab = false)
    {
        List<GameObject> returnList = new List<GameObject>();
        foreach (GameObject g in targetGameObjects)
        {
            List<GameObject> list = RegenerateFakesForChildren(g, updatePrefab);
            if (list != null && list.Count > 0)
            {
                if (g != null && g.GetComponent<DontFakeMe>()) { continue; }
                returnList.AddRange(list);
            }
        }
        return returnList;
    }
    private GameObject RegenerateFake(GameObject targetGameObject, bool updatePrefab = false)
    {
        if (!(targetGameObject.GetComponent<BaseReplace>() || targetGameObject.GetComponent<FakeMesh>()))
        {
            if (targetGameObject.GetComponent<BaseWeapon>())
            {
                return targetGameObject;
            }
            else
            {
                return null;
            }
        }

        GameObject ItemPrefab = null;
        BaseReplace baseReplace = targetGameObject.GetComponent<BaseReplace>();
        if (baseReplace)
        {
            ItemPrefab = targetGameObject.GetComponent<BaseReplace>().ReplacePrefab;
        }
        else if (targetGameObject.GetComponent<FakeMesh>())
        {
            ItemPrefab = targetGameObject.GetComponent<FakeMesh>().PrefabItem;
        }
        if (ItemPrefab == null)
        {
            Debug.LogError("Fake Prefabs NOT SET UP CORRECTLY");
            return targetGameObject;
        }


        UnityEngine.SceneManagement.Scene scene = targetGameObject.scene;
        GameObject newWeapon = (GameObject)PrefabUtility.InstantiatePrefab(ItemPrefab, scene);

        if (newWeapon.GetComponent<BaseIDComponent>())
        {
            string UniqueID = "";
            if (targetGameObject.GetComponent<BaseIDComponent>())
            {
                UniqueID = targetGameObject.GetComponent<BaseIDComponent>().GetUniqueID(false);
            }
            else if (targetGameObject.GetComponent<FakeMesh>())
            {
                UniqueID = targetGameObject.GetComponent<FakeMesh>().SavedComponentID;
            }
            newWeapon.GetComponent<BaseIDComponent>().SetUniqueID(UniqueID);
            newWeapon.GetComponent<BaseIDComponent>().AutoGenerateUniqueID = true;
        }

        // Update the security
        BaseLockedItem lockScript = newWeapon.GetComponent<BaseLockedItem>();
        if (lockScript)
        {
            CopySecurity(targetGameObject, lockScript);
        }

        if (baseReplace)
        {
            ApplyLootDataFromFake(newWeapon, baseReplace);
        }


        newWeapon.transform.position = targetGameObject.transform.position;
        newWeapon.transform.rotation = targetGameObject.transform.rotation;
        newWeapon.transform.localScale = targetGameObject.transform.localScale;
        newWeapon.transform.parent = targetGameObject.transform.parent;
        newWeapon.transform.SetSiblingIndex(targetGameObject.transform.GetSiblingIndex());


        DestroyImmediate(targetGameObject);


        return newWeapon;
    }

    public List<Collider> AddCollidersTo(Transform target, Collider[] references)
    {
        List<Collider> returnList = new List<Collider>();
        Transform colliderTransform = target;
        foreach (Collider collider in references)
        {
            if (!collider.enabled) continue; //Ignore if disabled, assume its not going to be used for collisions

            if (collider is BoxCollider)
            {
                BoxCollider col = colliderTransform.gameObject.AddComponent<BoxCollider>();
                col.size = (collider as BoxCollider).size;
                col.center = (collider as BoxCollider).center;
                col.isTrigger = (collider as BoxCollider).isTrigger;
                returnList.Add(col);
            }
            else if (collider is CapsuleCollider)
            {
                CapsuleCollider col = colliderTransform.gameObject.AddComponent<CapsuleCollider>();
                col.radius = (collider as CapsuleCollider).radius;
                col.height = (collider as CapsuleCollider).height;
                col.center = (collider as CapsuleCollider).center;
                col.isTrigger = (collider as CapsuleCollider).isTrigger;
                returnList.Add(col);
            }
            else if (collider is MeshCollider)
            {
                MeshCollider col = colliderTransform.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = (collider as MeshCollider).sharedMesh;
                col.convex = (collider as MeshCollider).convex;
                col.material = (collider as MeshCollider).material;
                col.isTrigger = (collider as MeshCollider).isTrigger;
                returnList.Add(col);
            }
            colliderTransform.localScale = collider.transform.localScale;
            colliderTransform.localPosition = collider.transform.localPosition;
            colliderTransform.localRotation = collider.transform.localRotation;
            colliderTransform.gameObject.layer = collider.gameObject.layer;
            colliderTransform.gameObject.tag = collider.gameObject.tag;
            colliderTransform.tag = "Fake";
        }

        return returnList;
    }

    private List<GameObject> GenerateMeshesForChildren(GameObject targetGameObject, GameObject parent)
    {
        List<GameObject> returnList = new List<GameObject>();

        List<Transform> targets = new List<Transform>() { targetGameObject.transform };
        while (targets.Count > 0)
        {
            int children = targets[0].transform.childCount;
            int progress = 0;

            for (int i = children - 1; i >= 0; --i)
            {
                if (targets[0].transform.GetChild(i).GetComponent<FakeMyChildren>()) { progress++; targets.Add(targets[0].transform.GetChild(i)); continue; }
                GameObject g = GenerateMesh(targets[0].transform.GetChild(i).gameObject);
                if (g != null)
                {
                    EditorUtility.DisplayProgressBar("Generating Meshes Only", "Meshing - " + targets[0].transform.GetChild(i).name, ((float)progress / children));
                    g.transform.parent = parent.transform;
                    g.transform.SetSiblingIndex(targets[0].transform.GetChild(i).GetSiblingIndex());
                    returnList.Add(g);
                }
                progress++;
            }
            targets.RemoveAt(0);
        }

        EditorUtility.ClearProgressBar();
        return returnList;
    }
    private List<GameObject> GenerateMeshes(GameObject[] targetGameObjects)
    {
        List<GameObject> returnList = new List<GameObject>();
        foreach (GameObject g in targetGameObjects)
        {
            GameObject parent = new GameObject();
            List<GameObject> list = GenerateMeshesForChildren(g, parent);
            if (list != null && list.Count > 0)
            {
                returnList.AddRange(list);
            }
            parent.transform.SetSiblingIndex(g.transform.GetSiblingIndex());
            parent.name = g.name + "_COPY";

            parent.transform.localPosition = g.transform.localPosition;
            parent.transform.localRotation = g.transform.localRotation;
            parent.transform.localScale = g.transform.localScale;
        }
        return returnList;
    }
    private GameObject GenerateMesh(GameObject targetGameObject)
    {
        if (!targetGameObject.GetComponent<BaseWeapon>() && !targetGameObject.GetComponent<FakeMesh>() && !targetGameObject.GetComponent<BaseReplace>()) return null;
        if (targetGameObject.GetComponent<LODGroup>() == null) return null;

        int prefabID = -1;
        if (targetGameObject.GetComponent<BaseWeapon>())
        {
            prefabID = targetGameObject.GetComponent<BaseWeapon>().ItemID;
        }
        if (targetGameObject.GetComponent<FakeMesh>())
        {
            prefabID = targetGameObject.GetComponent<FakeMesh>().PrefabItem.GetComponent<BaseWeapon>().ItemID;
        }
        if (targetGameObject.GetComponent<BaseReplace>())
        {
            prefabID = targetGameObject.GetComponent<BaseReplace>().ReplacePrefab.GetComponent<BaseWeapon>().ItemID;
        }

        if (prefabID < 0 || fakeEditorDatabase.itemDatabase.ItemArray[prefabID] == null) { return null; }

        GameObject ItemPrefab = fakeEditorDatabase.itemDatabase.ItemArray[prefabID].ItemPrefab;

        // Create the fake object base
        GameObject newWeapon = (GameObject)Instantiate(fakeEditorDatabase.MeshOnlyPrefab);


        //Mirror.NetworkServer.Spawn(newWeapon); // Remove this in final

        // Go through and retrieve all the mesh lods and create the game objects for them
        LODGroup prefabLODGroup = null;
        if (ItemPrefab == null)
        {
            prefabLODGroup = ItemPrefab.GetComponentInChildren<LODGroup>();
            newWeapon.name = ItemPrefab.name + "_Mesh";
        }
        else
        {
            prefabLODGroup = targetGameObject.GetComponentInChildren<LODGroup>();
            newWeapon.name = targetGameObject.name + "_Mesh";
        }

        if (prefabLODGroup == null) { return null; }

        // Copy the get the LOD of the prefab and copy it
        LOD[] NewLods = prefabLODGroup.GetLODs();
        Transform targetMeshTransform = newWeapon.transform;

        Renderer[] renderers = prefabLODGroup.GetLODs()[0].renderers;
        if (renderers[0].GetComponent<MeshRenderer>())
        {
            newWeapon.GetComponent<MeshFilter>().sharedMesh = renderers[0].GetComponent<MeshFilter>().sharedMesh;
            newWeapon.GetComponent<MeshRenderer>().sharedMaterials = renderers[0].sharedMaterials;
        }
        newWeapon.layer = renderers[0].gameObject.layer;
        newWeapon.tag = renderers[0].gameObject.tag;

        newWeapon.transform.localPosition = targetGameObject.transform.localPosition;
        newWeapon.transform.localRotation = targetGameObject.transform.localRotation;
        newWeapon.transform.localScale = targetGameObject.transform.localScale;

        return newWeapon;
    }
    private List<GameObject> RemoveColliders(GameObject[] targetGameObjects)
    {
        List<GameObject> returnList = new List<GameObject>();
        foreach (GameObject g in targetGameObjects)
        {
            RemoveCollider(g);
        }
        return returnList;
    }
    private GameObject RemoveCollider(GameObject targetGameObject)
    {
        Collider[] colliders = targetGameObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            DestroyImmediate(col);
        }
        return targetGameObject;
    }

    public GameObject[] CleanUpList(GameObject[] list)
    {
        List<GameObject> returnList = new List<GameObject>();
        foreach (GameObject g in list)
        {
            if (g != null)
            {
                returnList.Add(g);
            }
        }
        return returnList.ToArray();
    }

    public void Icons(Texture2D icon, GUIStyle style, int amount)
    {
        GUILayout.BeginHorizontal();
        for (int i = 0; i < amount; i++)
        {
            //style.alignment = TextAnchor.MiddleLeft;
            //if (stretchFirst) { style.stretchWidth = true; stretchFirst = false; } else { style.stretchWidth = false; }
            GUILayout.Label(new GUIContent("", icon), style);
        }
        GUILayout.EndHorizontal();
    }

    public void FillIcons(Texture2D fillicon, Texture2D emptyicon, GUIStyle iconStyle, int inAmount, int totalAmount, string endText = "", Texture2D overfillIcon = null)
    {
        GUILayout.BeginHorizontal();
        //iconStyle.fixedHeight = 20;
        //iconStyle.alignment = TextAnchor.MiddleLeft;
        for (int i = 0; i < totalAmount; i++)
        {
            if (i < inAmount)
            {
                GUILayout.Label(new GUIContent("", fillicon), iconStyle);
            }
            else
            {
                GUILayout.Label(new GUIContent("", emptyicon), iconStyle);
            }

        }

        if (overfillIcon != null && inAmount > totalAmount)
        {
            for (int i = 0; i < inAmount - totalAmount; i++)
            {
                GUILayout.Label(new GUIContent("", overfillIcon), iconStyle);
            }
        }

        if (!string.IsNullOrEmpty(endText))
        {
            GUILayout.Label(new GUIContent(endText), this.style);
        }
        GUILayout.EndHorizontal();
    }

}
#endif