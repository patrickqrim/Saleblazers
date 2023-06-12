// Copyright (c) 2014 Make Code Now! LLC

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

/// \ingroup Stream
/// Chunk is the loadable/streamable version of a SECTR_Sector. The
/// Chunk manages loading and unloading that data, usually at
/// the request of a Loader component.
/// 
/// Chunk stores the data needed to load (and unload) a Sector
/// that has been exported into a separate scene file. Loading will
/// happen asynchronously if the user has any Unity 5 or a Pro Unity 4 Pro license,
/// synchronously otherwise.
/// 
/// Chunk uses a reference counted loading scheme, so multiple
/// clients may safely request loading the same Chunk, provided that
/// they equally match their Load requests with their Unload requests.
/// Data for the Sector will be loaded when the reference count goes up
/// from 0, and unloaded when it returns to 0.
[RequireComponent(typeof(SECTR_Sector))]
[AddComponentMenu("Procedural Worlds/SECTR/Stream/SECTR Chunk")]
public class SECTR_Chunk : MonoBehaviour
{
    float RefCheckCooldown = 6.0f;
    float CurrentCheckCooldown = 0.0f;

    public delegate void SECTR_ChunkSignature(SECTR_Chunk chunk);
    public SECTR_ChunkSignature OnChunkLoadedDelegate;
    public SECTR_ChunkSignature OnChunkUnloadedDelegate;

    #region Private Members
    private AsyncOperation asyncLoadOp;
    [SerializeField, HideInInspector]
    private LoadState loadState = LoadState.Unloaded;
    private int refCount = 0;
    private GameObject chunkRoot = null;
    private GameObject chunkSector = null;
    private bool recenterChunk = false;
    [SerializeField, HideInInspector]
    private SECTR_Sector cachedSector = null;
    public GameObject proxy = null;
    bool bHasProxy;
    private bool quitting = false;
    public bool bGenerateProxy = true;

    private static SECTR_Chunk chunkActivating = null;
    private static LinkedList<SECTR_Chunk> activationQueue = new LinkedList<SECTR_Chunk>();
    private static bool requestedDeferredUnload = false;

    private NextAction nextAction;
    #endregion

    #region Public Interface
    public enum LoadState
    {
        Unloaded,
        Loading,
        Loaded,
        Unloading,
        Active,
    }

    // #ASBeginChange by Salvador Galindo for importing, exporting, and reverting using scene view loader
    public enum NextAction
    {
        None,
        Import,
        Export,
        Revert
    }
    // #ASEndChange

    [SECTR_ToolTip("The path of the scene to load")]
    public string ScenePath;
    [SECTR_ToolTip("The unique name of the root object in the exported Sector.")]
    public string NodeName;
    [SECTR_ToolTip("Exports the Chunk in a way that allows it to be shared by multiple Sectors, but may take more CPU to load.")]
    public bool ExportForReuse = false;
    [SECTR_ToolTip("Position of the proxy mesh.")]
    public Vector3 ProxyMeshPosition;
    [SECTR_ToolTip("A mesh to display when this Chunk is unloaded. Will be hidden when loaded.")]
    [HideInInspector] public Mesh ProxyMesh;
    public ProxyData ProxyData;

    public NextAction NextChunkAction => nextAction;
    public int ReferenceCount => refCount;

    // #ASBeginChange Added by Salvador Galindo to override sectr loader
    public bool FreezeState => bFreezeState;

    [SerializeField, SECTR_ToolTip("If true, will override SECTR Loader loading and unloading.")]
    public bool bFreezeState;

    public bool UniversalLoad => bUniversalLoad;
    [SerializeField, SECTR_ToolTip("If this chunk should load and unload for all players.")]
    private bool bUniversalLoad = false;
    // #ASEndChange

    /// Returns the Sector associated with this Chunk.
    public SECTR_Sector Sector
    {
        get { return cachedSector; }
    }

    /// Add a reference to this Chunk. If this is the first reference,
    /// the data associated with the SectorChunk will be loaded.
    /// If you call AddReference, make sure to eventually call RemoveReference.
    public void AddReference()
    {
        if (bFreezeState)
        {
            return;
        }
        //Debug.Log("Added Reference to Chunk " + gameObject.name);
        if (refCount == 0)
        {
            //Debug.Log("Loading Chunk " + gameObject.name);
            _Load();
        }
        ++refCount;
        if (ReferenceChange != null)
        {
            ReferenceChange(this, LoadState.Loading);
        }
    }

    bool bUnloadRequested = false;
    float UnloadRequestTimestamp = 0.0f;
    const float DelayBeforeUnloading = 4.0f;

    /// Add a reference to this Chunk. If this is the first reference,
    /// the data associated with the SectorChunk will be loaded.
    public void RemoveReference()
    {
        if (bFreezeState)
        {
            //Debug.Log(gameObject.name + " bFreezeState is true. Overriding remove chunk reference.");
            return;
        }
        //Debug.Log("Removed Reference from Chunk " + gameObject.name);
        if (ReferenceChange != null)
        {
            ReferenceChange(this, LoadState.Unloading);
        }
        --refCount;
        if (refCount <= 0)
        {
            //Debug.Log("Unloading Chunk " + gameObject.name);
            //_Unload();
            // We will unoad in update so that we can set the time diff
            UnloadRequestTimestamp = Time.timeSinceLevelLoad;
            bUnloadRequested = true;
            SetEnabled(true);
            // Guard against underflows.
            refCount = 0;
        }
    }

    public bool SetProxyActive(bool bNewActive)
    {
        if(!proxy && bNewActive)
        {
            _CreateProxy();
        }

        if(proxy && (proxy.activeSelf != bNewActive))
        {
            proxy.SetActive(bNewActive);
            return true;
        }
        else if(!bNewActive && !proxy)
        {
            return true;
        }

        return false;
    }

    // #ASBeginChange by Salvador Galindo to set load state when manually loaded through the inspector
    public void SetLoadState(LoadState newState)
    {
        loadState = newState;
        Changed?.Invoke(this, newState);
    }

    /// Determines whether the Chunk data is currently loaded.
    /// <returns>True if this instance is loaded; otherwise false.</returns>
    public bool IsLoaded()
    {
        return loadState == LoadState.Active;
    }

    /// Determines whether the Chunk data is currently unloaded.
    /// <returns>True if this instance is unloaded; otherwise false.</returns>
    public bool IsUnloaded()
    {
        return loadState == LoadState.Unloaded;
    }

    /// Returns the progress of the load, perhaps for use in an in-game display.
    /// <returns>The progress as a float between 0 and 1.</returns>
    public float LoadProgress()
    {
        switch (loadState)
        {
            case LoadState.Loading:
                return asyncLoadOp != null ? asyncLoadOp.progress * 0.8f : 0.5f;
            case LoadState.Loaded:
                return 0.9f;
            case LoadState.Active:
                return 1f;
            case LoadState.Unloaded:
            case LoadState.Unloading:
            default:
                return 0f;
        }
    }

#if UNITY_EDITOR
    // #ASBeginChange by Salvador Galindo for scene view streaming
    public void SetNextAction(NextAction action)
    {

    }

    public SaveResolution TerrainProxyResolution => terrainProxyResolution;
    public SaveFormat TerrainProxySaveFormat => terrainProxySaveFormat;
    public MicroSplatTerrainEditor.BakingResolutions TerrainProxyTextureResolution => terrainProxyTextureResolution;
    public MicroSplatTerrainEditor.BakingPasses TerrainProxyTextureType => terrainProxyTextureType;

    [SerializeField]
    SaveResolution terrainProxyResolution = SaveResolution.Quarter;
    [SerializeField]
    SaveFormat terrainProxySaveFormat = SaveFormat.Triangles;
    [SerializeField]
    MicroSplatTerrainEditor.BakingResolutions terrainProxyTextureResolution = MicroSplatTerrainEditor.BakingResolutions.k1024;
    [SerializeField]
    MicroSplatTerrainEditor.BakingPasses terrainProxyTextureType = MicroSplatTerrainEditor.BakingPasses.Albedo;

    public void GenerateTerrainLODMeshAndMaterial(string filePath, SaveResolution terrainResolution, SaveFormat terrainSaveFormat, MicroSplatTerrainEditor.BakingResolutions textureResolution, MicroSplatTerrainEditor.BakingPasses textureType)
    {
        GenerateTerrainLOD(filePath, terrainResolution, terrainSaveFormat);
        GenerateTerrainMaterialLOD(filePath, textureResolution, textureType);
    }

    public void GenerateTerrainLOD(string filePath, SaveResolution terrainResolution, SaveFormat saveFormat)
    {
        string path = filePath + "/" + ScenePath + "_ProxyMesh.obj";
        Terrain chunkTerrain = GetComponentInChildren<Terrain>();
        ProxyMesh = BaseTerrainLODGenerator.GenerateTerrainLOD(path, chunkTerrain, terrainResolution, saveFormat);
        ProxyMeshPosition = chunkTerrain.GetPosition();
    }

    public void GenerateTerrainMaterialLOD(string filePath, MicroSplatTerrainEditor.BakingResolutions textureResolution, MicroSplatTerrainEditor.BakingPasses textureType)
    {
        UnityEditor.VersionControl.AssetList database = new UnityEditor.VersionControl.AssetList();
        database.Add(UnityEditor.VersionControl.Provider.GetAssetByPath("Assets/renderbake.shader"));
        string path = filePath + ScenePath + "_ProxyData.asset";
        database.Add(UnityEditor.VersionControl.Provider.GetAssetByPath(path));

        UnityEditor.VersionControl.Task statusTask = UnityEditor.VersionControl.Provider.Status(database);
        statusTask.Wait();

        if (!UnityEditor.VersionControl.Provider.CheckoutIsValid(statusTask.assetList) && ((statusTask.assetList[0] != null && !UnityEditor.VersionControl.Provider.IsOpenForEdit(statusTask.assetList[0])) || (statusTask.assetList[1] != null && !UnityEditor.VersionControl.Provider.IsOpenForEdit(statusTask.assetList[1]))))
        {
            Debug.LogError("Could not check out Assets/renderbake.shader");
            return;
        }

        UnityEditor.VersionControl.Task databaseOperation = UnityEditor.VersionControl.Provider.Checkout(database, UnityEditor.VersionControl.CheckoutMode.Both);
        databaseOperation.Wait();

        string texturePath = filePath + "/" + ScenePath + "_ProxyTexture.png";
        string materialPath = filePath + "/" + ScenePath + "_ProxyMaterial.mat";
        Terrain chunkTerrain = GetComponentInChildren<Terrain>();
        BaseTerrainLODGenerator.GenerateTerrainLODTexture(texturePath, chunkTerrain, textureType, textureResolution);
        BaseTerrainLODGenerator.GenerateTerrainLODMaterial(materialPath, UnityEditor.AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D);
        ProxyData.ProxyMaterials = new Material[] { UnityEditor.AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material)) as Material };

        GameObject destroyTarget = GameObject.Find("_DELETETHIS_");
        while (destroyTarget)
        {
            DestroyImmediate(destroyTarget);
            destroyTarget = GameObject.Find("_DELETETHIS_");
        }
    }
    // #ASEndChange
#endif

    public delegate void LoadCallback(SECTR_Chunk source, LoadState loadState);

    /// Event handler for load/unload callbacks.
    public event LoadCallback Changed;
    public event LoadCallback ReferenceChange;
    #endregion

    #region Unity Interface
    void Awake()
    {
        SECTR_LightmapRef.InitRefCounts();
        SetEnabled(false);
        bHasProxy = proxy != null;
    }

    //#ASBeginChange by Michael to set up sector cache
    public void SetupSector()
    {
        if (!cachedSector)
        {
            cachedSector = GetComponent<SECTR_Sector>();
        }
    }

    void OnEnable()
    {
        SetupSector();

        if (cachedSector.Frozen)
        {
            _CreateProxy();
        }

        // #ASBeginChange by Salvador Galindo - optimization to disable chunk gameobject when not loaded.
        if (cachedSector.Frozen && !bFreezeState && Sector.SectorType == Sectr_Type.MANUAL)
        {
            gameObject.SetActive(false);
        }
        // #ASEndChange
    }
    //#ASEndChange

    // #ASBeginChange by Salvador Galindo - Commenting out any functionality on disable because we are disabling chunks when unloaded now so this is redundant
    //   void OnDisable()
    //{
    //	if(!quitting && asyncLoadOp != null && !asyncLoadOp.isDone)
    //	{
    //		Debug.LogError("Chunk unloaded with async operation active. " +
    //					   "Do not disable chunks until async operations are complete or Unity will likely crash.");
    //	}

    //	if(loadState != LoadState.Unloaded)
    //	{
    //		_FindChunkRoot();
    //		if(chunkRoot)
    //		{
    //			_DestroyChunk(false, true);
    //		}
    //	}
    //	cachedSector = null;
    //}
    // #ASEndChange

    void OnApplicationQuit()
    {
        quitting = true;
    }

    public void OnSceneLoaded()
    {
        chunkActivating = null;
        if (activationQueue.Count > 0)
        {
            activationQueue.RemoveFirst();
        }
        if (loadState == LoadState.Loading)
        {
            if(refCount > 0)
            {
                loadState = LoadState.Loaded;
                Changed?.Invoke(this, loadState);
            }
            else
            {
                _Unload();
            }
        }

        UpdateState();
    }

    public void OnSceneUnloaded()
    {
        if (!HRNetworkManager.IsHost() && bUniversalLoad)
        {
            OnChunkUnloaded(true, false);
        }
        else
        {
            OnChunkUnloaded(true, false);
        }

        if (loadState == LoadState.Unloading)
        {
            loadState = LoadState.Unloaded;
        }

        UpdateState();
    }

    private void OnChunkUnloaded(bool createProxy, bool fromDisable)
    {
        if (cachedSector && (cachedSector.TopTerrain || cachedSector.BottomTerrain || cachedSector.RightTerrain || cachedSector.LeftTerrain))
        {
            cachedSector.DisonnectTerrainNeighbors();
        }

        chunkRoot = null;
        chunkSector = null;
        recenterChunk = false;
        if (asyncLoadOp != null)
        {
            if (chunkActivating == this)
            {
                chunkActivating = null;
            }
            if (activationQueue.Contains(this))
            {
                activationQueue.Remove(this);
            }
            asyncLoadOp = null;
        }
        if (fromDisable || quitting || this == null)
        {
            _UnloadResources();
        }
        else if (!requestedDeferredUnload)
        {
            requestedDeferredUnload = true;
            StartCoroutine("_DeferredUnload");
        }
        loadState = LoadState.Unloaded;
        if (Changed != null)
        {
            Changed(this, loadState);
        }
        if (createProxy && ProxyMesh)
        {
            _CreateProxy();
        }
        OnChunkUnloadedDelegate?.Invoke(this);

        // #ASBeginChange by Salvador Galindo - optimization disables chunk game object when not loaded.
        if (Application.isPlaying && Sector && Sector.SectorType == Sectr_Type.MANUAL)
        {
            gameObject.SetActive(false);
        }
        // #ASEndChange
    }

    //#ASBeginChange by Michael Duan adding chunk ID loading
#if UNITY_EDITOR
    public void HandleChunkImported(SECTR_Sector sector)
    {
        if (sector)
        {
            BaseSectorMember[] SectrMemberArray = sector.GetComponentsInChildren<BaseSectorMember>();
            for (int i = 0; i < SectrMemberArray.Length; ++i)
            {
                SectrMemberArray[i].HandleImported_Editor(this.ScenePath);
            }
        }
    }
#endif
    //#ASEndChange

    // #ASBeginChange Kenny Doan, manually update instead of constant check
    void UpdateState()
    {
        // Double check the ref count matches its state
        if(refCount == 0)
        {
            if (loadState != LoadState.Unloading && loadState != LoadState.Unloaded)
            {
                _Unload();
                return;
            }
        }
        else
        {
            if(loadState != LoadState.Loading && loadState != LoadState.Loaded)
            {
                _Load();
                return;
            }
        }

        switch (loadState)
        {
            case LoadState.Loading:
                // #ASBeginChange Salvador Galindo - using manual load state set instead
                _TrySceneActivation();
                //if(asyncLoadOp == null || asyncLoadOp.isDone)
                //{
                //	if(asyncLoadOp != null)
                //	{
                //		chunkActivating = null;
                //		activationQueue.RemoveFirst();
                //		asyncLoadOp = null;
                //	}
                //	loadState = LoadState.Loaded;
                //	if(Changed != null)
                //	{
                //		Changed(this, loadState);
                //	}
                //	// Run update again to try to parent the chunk right away.
                //	FixedUpdate();
                //}
                // #ASEndChange
                break;
            case LoadState.Loaded:
                // Fix for edge cases where proxy and chunk are loaded at the same time.
                SetProxyActive(false);

                // Unity takes a frame to create the objects, so fix them up here.
                _SetupChunk();
                OnChunkLoadedDelegate?.Invoke(this);
                break;
            case LoadState.Active:
                // Do nothing.
                break;
            case LoadState.Unloading:
                _TrySceneActivation();
                _FindChunkRoot();
                if (chunkRoot)
                {
                    _DestroyChunk(true, false);
                }
                break;
        }
    }
    // #ASEndChange
    #endregion

    void SetEnabled(bool bEnabled)
    {
        this.enabled = bEnabled;
    }

    // This is to force ref count == 0 unloading stuff
    private void Update()
    {
        if(refCount == 0)
        {
            if(CurrentCheckCooldown > RefCheckCooldown)
            {
                // Check if the scene is loaded 
                if(IsLoaded())
                {
                    if(bUnloadRequested && (Time.timeSinceLevelLoad - UnloadRequestTimestamp > DelayBeforeUnloading))
                    {
                        // Unload now!
                        _Unload();
                    }
                }
                else
                {
                    SetEnabled(false);
                }
                CurrentCheckCooldown = 0;
            }
            else
            {
                CurrentCheckCooldown += Time.deltaTime;
            }
        }
        else
        {
            if (CurrentCheckCooldown > RefCheckCooldown)
            {
                // Check if the scene is unloaded 
                if (IsUnloaded())
                {
                    // Load now!
                    _Load();
                }
                else
                {
                    SetEnabled(false);
                }
                CurrentCheckCooldown = 0;
            }
            else
            {
                CurrentCheckCooldown += Time.deltaTime;
            }
        }
    }

    #region Private Methods
    private void _Load()
    {
        // This is to set ref to 0
        bUnloadRequested = false;
        SetEnabled(true);

        if (ScenePath != null && (loadState == LoadState.Unloaded || loadState == LoadState.Unloading))
        {
            if (loadState == LoadState.Unloaded)
            {
                if (Application.isPlaying)
                {
                    if (!SECTR_Modules.HasPro())
                    {
                        SceneManager.LoadScene(ScenePath, LoadSceneMode.Additive);
                        _SetupChunk();
                    }
                    else if (HRNetworkManager.IsHost() || !bUniversalLoad)
                    {
                        // #ASBeginChange by Salvador Galindo - Handling optimization for disabling chunk gameobject when not loaded
                        gameObject.SetActive(true);
                        // #ASEndChange
                        // #ASBeginChange by Salvador Galindo - using Flex Scene Loader for networking instead
                        if (BaseWorldStreamManager.Get)
                        {
                            BaseWorldStreamManager.Get.LoadChunk(ScenePath, HRNetworkManager.Get.LocalPlayerController);
                        }
                        else
                        {
                            asyncLoadOp = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
                        }
                        // #ASEndChange
                        activationQueue.AddLast(this);
                    }
                }
                else if (Application.isEditor)
                {
                    asyncLoadOp = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
                    activationQueue.AddLast(this);
                    OnChunkLoadedDelegate?.Invoke(this);
                }

                chunkRoot = null;
                chunkSector = null;
                recenterChunk = false;
            }

            loadState = LoadState.Loading;
            if (Changed != null)
            {
                Changed(this, loadState);
            }
        }
    }

    private void _Unload()
    {
        bUnloadRequested = false;

        if (loadState != LoadState.Unloaded)
        {
            if (cachedSector)
            {
                cachedSector.Frozen = true;
            }

            if (chunkRoot)
            {
                _DestroyChunk(true, false);
            }
            else
            {
                loadState = LoadState.Unloading;
                if (Changed != null)
                {
                    Changed(this, loadState);
                }
                UpdateState();
            }
        }
    }

    private void _DestroyChunk(bool createProxy, bool fromDisable)
    {
        // #ASBeginChange by Salvador Galindo - using Flex Scene Loader for networking instead
        if (Application.isPlaying)
        {
            if (HRNetworkManager.IsHost() || !bUniversalLoad)
            {
                if (BaseWorldStreamManager.Get)
                {
                    BaseWorldStreamManager.Get.UnloadChunk(ScenePath, HRNetworkManager.Get.LocalPlayerController);
                }
                else
                {
                    SceneManager.UnloadSceneAsync(ScenePath);
                }
            }
        }
        else if (Application.isEditor)
        {
            // Added for unloading in editor
            //SECTR_StreamExport.ExportToChunk(mySector);
        }
        // #ASEndChange
    }

    private void _FindChunkRoot()
    {
        if (chunkRoot == null && !quitting)
        {
            SECTR_ChunkRef chunkRef = SECTR_ChunkRef.FindChunkRef(NodeName);
            if (chunkRef && chunkRef.RealSector)
            {
                recenterChunk = chunkRef.Recentered;
                if (recenterChunk)
                {
                    chunkRef.RealSector.parent = transform;
                    chunkRoot = chunkRef.RealSector.gameObject;
                    chunkSector = chunkRoot;
                    GameObject.Destroy(chunkRef.gameObject);
                }
                else
                {
                    chunkRoot = chunkRef.gameObject;
                    chunkSector = chunkRef.RealSector.gameObject;
                    GameObject.Destroy(chunkRef);
                }
            }
            else
            {
                //#ASBeginChange Yiming: Comment this out, it returns null anyways cuz we all have the chunk ref set up
                chunkRoot = null;//GameObject.Find(NodeName);
                                 //#ASEndChange
                chunkSector = chunkRoot;
                recenterChunk = false;
            }
        }
    }

    private void _SetupChunk()
    {
        _FindChunkRoot();
        if (chunkRoot)
        {
            // Activate the root if inactive (due to backwards compat or recentering
            if (!chunkRoot.activeSelf)
            {
                chunkRoot.SetActive(true);
            }

            // Recenter chunk under ourselves
            if (recenterChunk && !cachedSector.FloatingPointFix)
            {
                Transform chunkTransform = chunkRoot.transform;
                chunkTransform.localPosition = Vector3.zero;
                chunkTransform.localRotation = Quaternion.identity;
                chunkTransform.localScale = Vector3.one;
            }

            if (cachedSector.FloatingPointFix)
            {
                chunkRoot.transform.position = SECTR_FloatingPointFix.Instance.totalOffset;

                if (chunkRoot.transform.GetComponent<SECTR_FloatingPointFixMember>() == null)
                {
                    chunkRoot.AddComponent<SECTR_FloatingPointFixMember>();
                }

            }

            // Hook up the child proxy
            SECTR_Member rootMember = chunkSector.GetComponent<SECTR_Member>();
            if (!rootMember)
            {
                rootMember = chunkSector.gameObject.AddComponent<SECTR_Member>();
                rootMember.BoundsUpdateMode = SECTR_Member.BoundsUpdateModes.Static;
                rootMember.ForceUpdate(true);
            }
            else if (recenterChunk)
            {
                rootMember.ForceUpdate(true);
            }
            cachedSector.ChildProxy = rootMember;

            // Unfreeze our sector
            cachedSector.Frozen = false;

            // Remove the proxy if there is one
            DestroyProxy();

            loadState = LoadState.Active;
            if (Changed != null)
            {
                Changed(this, loadState);
            }
            Debug.Log("Successfully loaded chunk " + gameObject.name);
        }
    }

    public void DestroyProxy()
    {
        if (proxy)
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                GameObject.DestroyImmediate(proxy);
            }
            else
            {
                GameObject.Destroy(proxy);
            }
        }
    }

    public void CreateProxy()
    {
        _CreateProxy();
    }

    private void _CreateProxy()
    {
        if (proxy == null && !quitting && bGenerateProxy)
        {
            if (ProxyData)
            {
                if (ProxyData.ProxyMesh)
                {
                    proxy = Instantiate(ProxyData.ProxyMesh, this.transform);
                    proxy.transform.localPosition = Vector3.zero;
                    Transform container = proxy.transform.GetChild(0);
                    for (int i = 0; i < ProxyData.ProxyMaterials.Length; ++i)
                    {
                        if (i < container.childCount)
                        {
                            Transform child = container.GetChild(i);
                            if (child)
                            {
                                MeshRenderer rend = child.GetComponent<MeshRenderer>();
                                if (rend)
                                {
                                    rend.sharedMaterial = ProxyData.ProxyMaterials[i];
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (this)
                    {
                        Debug.LogError("There is a missing ProxyData in SECTR_Chunk._CreateProxy() in " + this.name);
                    }
                }

                if (proxy)
                {
                    proxy.isStatic = true;
                    proxy.transform.position = ProxyData.ProxyMesh ? Vector3.zero : ProxyMeshPosition;
                    proxy.transform.rotation = transform.rotation;
                    proxy.transform.localScale = transform.lossyScale;

                    if (!Application.isPlaying)
                    {
                        proxy.transform.SetParent(this.transform);
                    }
                }
            }

        }
    }

    private void _TrySceneActivation()
    {
        if (chunkActivating == null &&
            asyncLoadOp != null && !asyncLoadOp.allowSceneActivation && asyncLoadOp.progress >= 0.9f &&
            activationQueue.Count > 0 && activationQueue.First.Value == this)
        {
            chunkActivating = this;
            asyncLoadOp.allowSceneActivation = true;
        }
    }

    private void _UnloadResources()
    {
        //Resources.UnloadUnusedAssets();
        requestedDeferredUnload = false;
    }

    private IEnumerator _DeferredUnload()
    {
        yield return new WaitForEndOfFrame();
        _UnloadResources();
        yield return null;
    }

    private IEnumerator _UnloadScene(string scenePath)
    {
        yield return new WaitForEndOfFrame();
        SceneManager.UnloadSceneAsync(ScenePath);
    }
    #endregion
}
