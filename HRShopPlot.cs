using EmployeeSystem.Gathering;
using Mirror;
using Pathfinding;
using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Ceras.SerializedType]
public class HRShopPlot : Mirror.NetworkBehaviour, IHRSaveable
{
    public delegate void FShopPlotSaleSignature(HRShopManager InShopManager, HRSaleInfo InSaleInfo);
    public FShopPlotSaleSignature ItemSoldDelegate;
    public delegate void ShopPlotSignature(HRShopPlot ShopPlot);
    public ShopPlotSignature OnShopPlotUnlocked;

    public delegate void PlotItemsLoadedSignature(HRShopPlot ShopPlot);
    public PlotItemsLoadedSignature OnPlotChunksLoaded;

    public BaseIDComponent IDComponent;
    [Header("Static Data")]
    // What kinds of star ratings there are.
    public GameObject OverrideShopPrefab;
    public HRStarRatingInfo[] StarRatingInfos;
    public HRDemandDatabase CustomerDemandDB;

    [Header("Base Price for Modifying Sales Toward Peformance")]
    public AnimationCurve StarterItemPrice;

    [Header("Spawn Rate Modifier vs. Star Rating")]
    public AnimationCurve CustomerSpawnRateCurveModifier;
    public float BasePlotCustomerSpawnCooldown = 280f;

    [Header("Group Spawning")]
    public HRGroupSpawnInfo[] GroupSpawnInfo;

    public int BaseWaitingCustomerMax;

    [Header("Traffic Rates")]
    [SerializeField]
    public HRTimeZone RushHour;
    public HRTimeZone[] HighZones;
    public HRTimeZone[] MediumZones;
    public HRTimeZone[] LowZones;
    public HRTimeZone[] DeadZones;

    // Daily modifier on top of shop degradation modifier.
    public float DegradationModifier = 1.0f;
    [Tooltip("Spawn time modifier for immediate customer after opening. Set to -1 if disabled.")]
    public float InitialCustomerSpawnTimeModifier = -1f;
    public int MinGroupSize = 1;


    [Header("Instance Data")]
    public string PlotName = "Untitled Shop Plot";
    public HREmployeeSystem EmployeeSystem;
    public HRStoreValue ShopBounds;
    public Transform SellerSpawnPoint;
    public Transform TravellingMerchantPoint;
    public GameObject SellerNPC;
    public BaseDialogueStarter dialogueStarter;
    public GameObject LeaveVFX;

    public GameObject parentSpawnerContainer;
    public HRCustomerSpawner[] CustomerSpawners;
    private List<HRCustomerAI> SpawnedCustomers = new List<HRCustomerAI>();
    // This is the starting floor tile of the shop plot.
    public HRFloorTile StartingTile;

    //public Mirror.SyncList<uint> ShopNetIDs = new Mirror.SyncList<uint>();
    public List<HRShopManager> Shops = new List<HRShopManager>();
    public List<HRShopManager> ShopsInPlot { get => Shops; }

    [Mirror.SyncVar, Ceras.SerializedField]
    public int NumShopsToAdd = 1;
    public int MaxShopsInPlot = 4;

    public float CustomerSpawnCooldown = 120.0f;
    int MaxPlotCustomers = 30;
    int MinPlotCustomers = 2;
    public int CurrentPlotCustomers = 0;

    // Quest character spawn variables
    public HRQuestCustomers QuestCustomers
    {
        get { return ((HRGameInstance)BaseGameInstance.Get).QuestCustomers; }
    }
    int LocalQuestCharacterCount = 0;
    float QuestCharacterSpawnProbability = 0f;
    bool bQuestCharacterSpawned = false;
    bool bAllSpawned = false;
    public Dictionary<HeroPlayerCharacter, int> QuestCharacterDict = new Dictionary<HeroPlayerCharacter, int>();

    public HRCustomerDatabase CustomerDB;

    public int MaxCustomersInShop = 3;

    public AnimationCurve ShopClosedCustomerFlowModifier;

    public bool bShouldSpawnPlotCustomers = false;

    public TextAsset navMeshAsset;
    [HideInInspector]
    public string NavMeshSettingsPath;

    [Ceras.SerializedField]
    public byte[] savedNavMesh;
    [HideInInspector]
    public Pathfinding.RecastGraph.RecastGraphSetting GraphSettings;
    public Pathfinding.RecastGraph graphRef;

    [Header("Plot Info")]
    [SyncVar, Ceras.SerializedField]
    public bool Unlocked;
    public bool StartUnlocked = false;
    public byte PlotID = 0;
    public GatheringArea AssociatedArea = GatheringArea.BambooForest;
    public GatherUnlockData[] UnlockableResources;
    public Dictionary<short, float> GatheringMultipliers = new Dictionary<short, float>();
    public int Cost = 500;

    public AnimationCurve UnlockedNumberShopsCurve;

    // This will be used by another thread so don't mutate is elsewhere
    [ClearOnReload(true)]
    public static List<HRShopPlot> AutoSavingPlots = new List<HRShopPlot>();

    private bool WaitingSectorsToLoad = false;
    public HashSet<string> WaitingSectorsToLoadList = new HashSet<string>();
    [HideInInspector] public bool PlotChunksLoaded = false;

    public ShopDataGrid ShopGrid;
    public HRShopPlotFarmManager PlotFarmManager;

    private Bounds tempBounds = new Bounds();
    private const float Reciprocal45 = 1f / 45f;

    // returns the number of shops that can be built on this plot
    public int GetMaxShops()
    {
        return MaxShopsInPlot;
    }

    public bool HasShopsRunning()
    {
        for (int i = 0; i < Shops.Count; ++i)
        {
            if (Shops[i] && Shops[i].bShopRunning)
            {
                return true;
            }
        }
        return false;
    }

    public int GetNumShops()
    {
        if (Shops != null)
        {
            return Shops.Count;
        }
        else
        {
            return 0;
        }
    }

    public override void Awake()
    {
        base.Awake();

        if (ShopBounds != null)
        {
            ShopBounds.PlayerAddedDelegate += HandlePlayerAddedDelegate;

            BoxCollider collider = ShopBounds.GetComponent<BoxCollider>();
            ShopGrid = new ShopDataGrid();

            byte cellSize = 1;
            byte sizeX = (byte)collider.size.x;
            byte sizeY = (byte)collider.size.y;
            byte sizeZ = (byte)collider.size.z;
            Vector3 offset = Vector3.zero;

            // Adjust the cell position so they line up with the shop bounds
            if (sizeX % 2 != 0)
            {
                offset -= new Vector3(cellSize * .5f, 0, 0);
                sizeX++;
            }
            if (sizeZ % 2 != 0)
            {
                offset -= new Vector3(0, 0, cellSize * .5f);
                sizeZ++;
            }

            ShopGrid.CreateShopGrid(sizeX, sizeY, sizeZ, cellSize, transform.position + offset - new Vector3(collider.size.x, 0, collider.size.z) / 2f);
        }
    }

    private IEnumerator WaitForShopEntity()
    {
        yield return new WaitUntil(() => HRShopEntityManager.Get != null);

        HRShopEntityManager.Get.RegisterShopPlot(this, true);
    }

    void LoadPlotNavMesh()
    {
        UpdateGraphRef();

        byte[] NavMeshToLoad;
        bool bLoadedFromSave = savedNavMesh != null && savedNavMesh.Length > 0;

        if (bLoadedFromSave)
        {
            NavMeshToLoad = savedNavMesh;
        }
        else
        {
            NavMeshToLoad = navMeshAsset.bytes;
        }

        if (NavMeshToLoad != null && NavMeshToLoad.Length > 0)
        {
            if (BaseWorldStreamManager.Get)
            {
                BaseWorldStreamManager.Get.QueueNavMeshToLoad(NavMeshToLoad, graphRef, (bSuccess) =>
                {
                    if (bSuccess) return;

                    if (bLoadedFromSave)
                    {
                        if (navMeshAsset != null)
                        {
                            BaseWorldStreamManager.Get.QueueNavMeshToLoad(navMeshAsset.bytes, graphRef);
                        }
                        else
                        {
                            Debug.LogError($"[{name}] Failed to load saved Shop Plot NavMesh. Couldn't fallback to default NavMesh Asset. Please bake one!");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[{name}] Failed to load default Shop Plot NavMesh. Please rebake!");
                    }
                });
            }
            else
            {
                AStarGraphSaver.LoadNavMesh(AStarGraphSaver.DeserializeTile(NavMeshToLoad), graphRef);
            }
        }
        else
        {
            Debug.LogError($"[HRShopPlot] {name} does not have a navmesh to load into A*");
        }
    }

    void UpdateGraphRef()
    {
        if (graphRef == null)
        {
            graphRef = (RecastGraph)AstarPath.active.data.AddGraph(typeof(RecastGraph));
            graphRef.InitializeGraphWithSettigns(GraphSettings, ShopBounds.GetComponent<BoxCollider>().bounds);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        QuestCharacterSpawnProbability = QuestCustomers.initProbability;

        //Debug.Log("Loaded " + PlotName + " : Unlocked:" + Unlocked);
        CustomerDB = Instantiate(CustomerDB);

        if (((HRGameManager)BaseGameManager.Get))
        {
            ((HRGameManager)BaseGameManager.Get).DayManager.HourChangedDelegate += HandleHourChanged;
        }

        RecalculateCustomerPercentageChances();

        if (HRNetworkManager.IsHost())
        {
            string ComponentIDKey = IDComponent.GetUniqueID();

            HRSaveComponent saveComponent = GetComponent<HRSaveComponent>();
            for (int i = 0; i < saveComponent.InitialSavedComponents.Length; i++)
            {
                for (int j = 0; j < saveComponent.InitialSavedComponents[i].SavedComponents.Length; j++)
                {
                    if (saveComponent.InitialSavedComponents[i].SavedComponents[j] == this)
                    {
                        ComponentIDKey = ComponentIDKey + i + j;
                        break;
                    }
                }
            }

            if (!HRSaveSystem.Get.CurrentFileInstance.KeyExists(ComponentIDKey))
            {
                LoadPlotNavMesh();
                InitializePlot();
            }
        }

        StartCoroutine(WaitForShopEntity());
    }

    private void HandleLocalPlayerPawnChanged(HRPlayerController Player)
    {
        LoadChunkLayers(true);
        HRNetworkManager.Get.ClientPawnAssignedDelegate -= HandleLocalPlayerPawnChanged;
    }

    public void InitializePlot()
    {
        if (StartUnlocked)
        {
            Unlocked = true;
        }

        if (Unlocked)
        {
            UnlockPlot_Implementation();
        }
        else
        {
            ToggleCustomerSpawner(false);
        }

        if (HRPlotManager.Get) { HRPlotManager.Get.RegisterPlot(this); }

        //Debug.Log("Loaded: Plot");

        if (BaseGameInstance.Get.GetLocalPlayerPawn())
        {
            LoadChunkLayers(true);
        }
        else
        {
            HRNetworkManager.Get.ClientPawnAssignedDelegate += HandleLocalPlayerPawnChanged;
        }

        //SellerNPC.GetComponent<BaseScripts.BaseMovementComponent>().FreezeMovement(true);
        SellerNPC.GetComponent<BaseScripts.BaseMovementComponent>().SetNetworkedIsGrounded(true);
        if (Unlocked) { TurnOffNPC(0); }
    }

    public void UnlockPlot()
    {
        if (HRNetworkManager.IsHost())
        {
            UnlockPlot_Implementation();
        }
        else
        {
            UnlockPlot_Command();
        }
    }
    [Command(ignoreAuthority = true)]
    private void UnlockPlot_Command() => UnlockPlot_Implementation();

    void UnlockPlot_Implementation()
    {
        HRPlotManager.Get.UnlockedGatheringAreas.Add(AssociatedArea);
        if (PlotID != 0) { HRPlotManager.Get.UnlockedPlots.Add(PlotID); }
        short[] resources = new short[UnlockableResources.Length];
        for (int i = 0; i < UnlockableResources.Length; i++)
        {
            short id = (short)(UnlockableResources[i].Unlock.GetComponent<BaseWeapon>().ItemID);
            HRPlotManager.Get.UnlockedResources.Add(id);
            resources[i] = id;

            if (!GatheringMultipliers.ContainsKey(id)) { GatheringMultipliers.Add(id, UnlockableResources[i].BonusMultiplier); }
            else { GatheringMultipliers[id] = UnlockableResources[i].BonusMultiplier; }
        }

        HRPlotManager.Get.RegisterUnlockedPlot(PlotID, AssociatedArea, resources);

        ToggleCustomerSpawner(true);
        Unlocked = true;

        // Create applicant data when the plot is bought
        EmployeeSystem.UpdateApplicants();
        OnShopPlotUnlocked?.Invoke(this);
    }

    public void BuyPlot()
    {
        UnlockPlot();
    }

    public void TurnOffNPC(float time = 2)
    {
        if (HRNetworkManager.IsHost())
        {
            TurnOffNPC_Implementation(time);
            TurnOffNPC_ClientRpc(time);
        }
        else
        {
            TurnOffNPC_Command(time);
        }
    }

    [Command(ignoreAuthority = true)]
    private void TurnOffNPC_Command(float time) { TurnOffNPC_Implementation(time); TurnOffNPC_ClientRpc(time); }
    [ClientRpc]
    private void TurnOffNPC_ClientRpc(float time) { if (!HRNetworkManager.IsHost()) { TurnOffNPC_Implementation(time); } }
    void TurnOffNPC_Implementation(float time)
    {
        if (time > 0)
        {
            StartCoroutine(WaitForTurnOffNPC(time));
        }
        else
        {
            SellerSpawnPoint.gameObject.SetActive(false);
        }
        //SellerNPC.SetActive(false);
    }
    IEnumerator WaitForTurnOffNPC(float time)
    {
        SellerNPC.GetComponent<HeroPlayerCharacter>().AnimScript.PlayAnimation("Waving");
        if (time > 0)
        {
            yield return new WaitForSeconds(time);
        }
        GameObject effect = Instantiate(LeaveVFX, SellerNPC.transform);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.parent = null;
        Destroy(effect, 3f);

        SellerSpawnPoint.gameObject.SetActive(false);
    }



    private void ToggleCustomerSpawner(bool enabled)
    {
        bShouldSpawnPlotCustomers = enabled ? true : false;
        foreach (HRCustomerSpawner spawner in CustomerSpawners)
        {
            spawner.enabled = enabled;
            if (spawner.GetComponent<Collider>()) { spawner.GetComponent<Collider>().enabled = false; }
        }
    }

    // Not networked.
    void HandlePlayerAddedDelegate(HeroPlayerCharacter InCharacter, bool bEntered)
    {
        if (InCharacter)
        {
            if (bEntered)
            {
                InCharacter.SetOwningShopPlot(this);
            }
            else
            {
                InCharacter.SetOwningShopPlot(null);
            }
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (graphRef != null && AstarPath.active != null && AstarPath.active.data != null) { AstarPath.active.data.RemoveGraph(graphRef); }
    }

    public override void OnEnable()
    {

    }

    // When the plot loads, we wait until all the associated chunks are loaded in
    private void OnChunkLoaded(BaseWorldStreamManager InWorldStreamManager, string ChunkName, bool bLoaded, bool bFinished)
    {
        if (WaitingSectorsToLoad && WaitingSectorsToLoadList.Contains(ChunkName))
        {
            WaitingSectorsToLoadList.Remove(ChunkName);
            if (WaitingSectorsToLoadList.Count <= 0)
            {
                CheckShopWeapons();

                WaitingSectorsToLoad = false;

                HRSaveSystem.Get.OnSceneFinishedLoaded -= OnChunkLoaded;
            }
        }
    }

    public void UnsubsribeShopWeapons()
    {
        BoxCollider shopCollider = ShopBounds.GetComponent<BoxCollider>();
        PhysicsUtil.OverlapBoxNonAlloc(shopCollider.transform.position + shopCollider.center, shopCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("PlaceableWeapon") | LayerMask.GetMask("NonPlaceableWeapon") | LayerMask.GetMask("FloorTiles"), QueryTriggerInteraction.Ignore);
        for (int i = 0; i < PhysicsUtil.BufferLength; i++)
        {
            BaseWeapon weapon = PhysicsUtil.ColliderBuffer[i].GetComponentInParent<BaseWeapon>();
            if (weapon)
            {
                ShopBounds.UnsubsribeShopItem(weapon);
            }

        }
        PhysicsUtil.ClearColliderArray();


        /*
        // Apply roofed parameter
        collidersInfo = PhysicsUtil.OverlapBoxNonAlloc(shopCollider.transform.position + shopCollider.center, shopCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("FloorTiles"), QueryTriggerInteraction.Ignore);

        for (int i = 0; i < collidersInfo.Item1; i++)
        {
            HRFloorTile floor = collidersInfo.Item2[i].GetComponentInParent<HRFloorTile>();
            if (floor)
            {
                floor.SetOwningShopPlot_Server(this);
                ApplyRoofed(floor, true);
            }
        }
        PhysicsUtil.ClearColliderArray();
        */

    }
    // This is called when all the items and baseweapons are loaded into the plot. We can run functions on the items 
    public void CheckShopWeapons()
    {
        if (ShopBounds == null)
        {
            if (this)
            {
                Debug.LogError("ERROR 104 [" + this.name + " |" + this + "] Contact Kenny Doan");
            }
            else
            {
                Debug.LogError("ERROR 104: HRShopPlot.CheckShopWeapons has a null ShopBounds. Contact Kenny Doan");
            }
            return;
        }

        Physics.SyncTransforms();

        BoxCollider shopCollider = ShopBounds.GetComponent<BoxCollider>();
        // Apply roofed parameter
        PhysicsUtil.OverlapBoxNonAlloc(shopCollider.transform.position + shopCollider.center, shopCollider.bounds.extents, Quaternion.identity, Constants.Layers.FLOORTILE, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < PhysicsUtil.BufferLength; i++)
        {
            HRBuildingComponent floorBuildingComponent = PhysicsUtil.ColliderBuffer[i].GetComponentInParent<HRBuildingComponent>();
            if (PhysicsUtil.ColliderBuffer[i].transform.parent)
            {
                if (PhysicsUtil.ColliderBuffer[i].transform.parent.parent)
                {
                    string name = PhysicsUtil.ColliderBuffer[i].transform.parent.parent.name;
                    Debug.Log(name);
                }
            }
            if (PhysicsUtil.ColliderBuffer[i].GetComponentInParent<BaseWeapon>())
            {
                string name = PhysicsUtil.ColliderBuffer[i].GetComponentInParent<BaseWeapon>().name; 
            }
            if (floorBuildingComponent)
            {
                if (floorBuildingComponent.OwningFloorTile)
                {
                    floorBuildingComponent.OwningFloorTile.SetOwningShopPlot_Server(this, GetShop(0));
                }
                else
                {
                    floorBuildingComponent.OwningPlaceable.OwningWeapon.OriginalBoundsCenter = floorBuildingComponent.OwningPlaceable.OwningWeapon.MeshColliders[0].bounds.center;
                }
                ApplyRoofed(floorBuildingComponent, true);
            }
        }

        PhysicsUtil.OverlapBoxNonAlloc(shopCollider.transform.position + shopCollider.center, shopCollider.bounds.extents, Quaternion.identity, Constants.Layers.PLACEABLEWEAPON | Constants.Layers.NONPLACEABLEWEAPON, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < PhysicsUtil.BufferLength; i++)
        {
            BaseWeapon weapon = PhysicsUtil.ColliderBuffer[i].GetComponentInParent<BaseWeapon>();
            //Debug.Log("Non-Floor");
            if (weapon)
            {
                ApplyWeaponBeauty(weapon, false);
                //weapon.RoomItemRef?.RaycastForShopPlot(true);
                //weapon.RoomItemRef?.OwningShop?.HandleWeaponAddedToStore(weapon, true);
            }
            else
            {
                //Debug.Log(c.name + " | " + c.transform.parent.name + " | " + c.transform.parent.parent.name + " | " + c.gameObject.tag);
            }

        }
        PhysicsUtil.ClearColliderArray();

        UpdateFloorTiles();

        StartCoroutine(OnPlotChunkLoadedHandler());
    }

    IEnumerator OnPlotChunkLoadedHandler()
    {
        yield return new WaitForSeconds(Time.deltaTime);
        OnPlotChunksLoaded?.Invoke(this);
        PlotChunksLoaded = true;
        yield return null;
    }


    public void ApplyWeaponBeauty_CommandToServer(BaseWeapon weapon, bool updateFloorTiles)
    {
        if (HRNetworkManager.IsHost())
        {
            ApplyWeaponBeauty(weapon, updateFloorTiles);
        }
        else
        {
            ApplyWeaponBeauty_Command(weapon.netIdentity, updateFloorTiles);
        }
    }
    [Command(ignoreAuthority = true)]
    private void ApplyWeaponBeauty_Command(NetworkIdentity weaponIdentity, bool updateFloorTiles)
    {
        ApplyWeaponBeauty(weaponIdentity.GetComponent<BaseWeapon>(), updateFloorTiles);
    }

    /// <summary>
    /// Adds beauty value from the shop grid based on weapon
    /// </summary>
    public void ApplyWeaponBeauty(BaseWeapon weapon, bool updateFloorTiles)
    {
        float beautyValue = ((HRGameManager)(HRGameManager.Get)).MasterItemDB.GetBeautyValue(weapon.ItemID);
        //Debug.Log(weapon.name);
        //weapon.ApplyBeautyValueToFloorTiles();

        if (beautyValue != 0)
        {
            byte range = ((HRGameManager)(HRGameManager.Get)).MasterItemDB.GetEffectingRange(weapon.ItemID);

            ApplyWeaponBeauty(beautyValue, weapon.transform.position, range, weapon.ItemID, updateFloorTiles);
        }
    }
    /// <summary>
    /// Adds beauty value from the shop grid based on a position and radius.
    /// </summary>
    public void ApplyWeaponBeauty(float beautyValue, Vector3 position, byte range, int ID, bool updateFloorTiles = true)
    {
        List<(BVector3, ShopGridData)> gridPoints = ShopGrid.GetPointsWithinRadiusSimple(position, range);

        for (int i = 0; i < gridPoints.Count; i++)
        {
            BVector3 coord = gridPoints[i].Item1;

            ShopGrid.AddEffectingItem(coord, (short)ID);

            byte effectingCount = ShopGrid.EffectingItems[coord][(short)ID];
            if (effectingCount <= 3)
            {
                float addedBeauty = (int)(beautyValue * ShopGrid.reciprocals[effectingCount]);
                ShopGrid.Set(coord.x, coord.y, coord.z, new ShopGridData((gridPoints[i].Item2.Beauty + addedBeauty), gridPoints[i].Item2.BitData));
            }

        }

        if (updateFloorTiles) { UpdateFloorTiles(new Bounds(position, Vector3.one * (range + 4f))); }
    }

    public void RemoveWeaponBeauty_CommandToServer(BaseWeapon weapon, bool updateFloorTiles = true)
    {
        if (HRNetworkManager.IsHost())
        {
            RemoveWeaponBeauty(weapon, updateFloorTiles);
        }
        else
        {
            RemoveWeaponBeauty_Command(weapon.netIdentity, updateFloorTiles);
        }
    }
    [Command(ignoreAuthority = true)]
    private void RemoveWeaponBeauty_Command(NetworkIdentity weaponIdentity, bool updateFloorTiles)
    {
        RemoveWeaponBeauty(weaponIdentity.GetComponent<BaseWeapon>(), updateFloorTiles);
    }
    /// <summary>
    /// Removes beauty value from the shop grid based on weapon
    /// </summary>
    public void RemoveWeaponBeauty(BaseWeapon weapon, bool updateFloorTiles = true)
    {
        if (weapon && ((HRGameManager)(HRGameManager.Get)))
        {
            float beautyValue = ((HRGameManager)(HRGameManager.Get)).MasterItemDB.GetBeautyValue(weapon.ItemID);

            if (beautyValue != 0)
            {
                byte range = ((HRGameManager)(HRGameManager.Get)).MasterItemDB.GetEffectingRange(weapon.ItemID);

                RemoveWeaponBeauty(beautyValue, weapon.transform.position, range, weapon.ItemID, updateFloorTiles);
            }
        }

    }

    /// <summary>
    /// Removes beauty value from the shop grid based on a position and radius.
    /// </summary>
    public void RemoveWeaponBeauty(float beautyValue, Vector3 position, byte range, int ID, bool updateFloorTiles = true)
    {
        List<(BVector3, ShopGridData)> gridPoints = ShopGrid.GetPointsWithinRadiusSimple(position, range);

        for (int i = 0; i < gridPoints.Count; i++)
        {
            BVector3 coord = gridPoints[i].Item1;

            if (ShopGrid.EffectingItems.ContainsKey(coord))
            {
                if (ShopGrid.EffectingItems[coord].ContainsKey((short)ID))
                {
                    byte effectingCount = ShopGrid.EffectingItems[coord][(short)ID];

                    if (effectingCount <= 3)
                    {
                        float addedBeauty = (int)(beautyValue * ShopGrid.reciprocals[effectingCount]);
                        ShopGrid.Set(coord.x, coord.y, coord.z, new ShopGridData((gridPoints[i].Item2.Beauty - addedBeauty), gridPoints[i].Item2.BitData));
                    }

                    ShopGrid.RemoveEffectingItem(coord, (short)ID);
                }
            }

        }

        if (updateFloorTiles) { UpdateFloorTiles(new Bounds(position, Vector3.one * (range + 4f))); }
    }

    public float GetBoundsReductionRatio(Transform target)
    {
        float angle = target.eulerAngles.y;

        // Convert angle to positive value (0 to 360 degrees)
        if (angle < 0)
        {
            angle = 360 + angle;
        }

        // Calculate the remainder of angle divided by 90
        float remainder = angle % 90;

        // Calculate the normalized value based on the remainder
        float normalizedValue = Mathf.Clamp01(1 - Mathf.Abs(remainder - 45) * Reciprocal45);

        //Debug.Log($"{normalizedValue}");

        return normalizedValue;
    }

    /// <summary>
    /// Sets whether the grid point can get wet from weather
    /// </summary>
    //public void ApplyRoofed(HRFloorTile floor, bool enabled, bool updateFloorTiles = false) => ApplyRoofed(GetFloorBounds(floor), enabled, updateFloorTiles);

    /// <summary> Sets whether the grid point can get wet from weather </summary>
    public void ApplyRoofed(HRBuildingComponent floorTile, bool enabled, bool updateFloorTiles = false)
    {
        ApplyRoofed(floorTile.OwningPlaceable.OwningWeapon, GetFloorBounds(floorTile.OwningPlaceable.OwningWeapon), enabled, updateFloorTiles);
    }
    public void ApplyRoofed(BaseWeapon tileWeapon, Bounds bounds, bool enabled, bool updateFloorTiles = false)
    {
        List<BVector3> TouchingCells = ShopGrid.GetCellsTouchingCollider(bounds);
        List<BVector3> CellUnder = ShopGrid.GetCellsUnder(TouchingCells);
        //Debug.Log("Bounds:" + enabled + " | " + bounds.center + " | " + bounds.extents);
        SetRoofedOnCells(CellUnder, enabled);

        if (updateFloorTiles && CellUnder.Count > 0)
        {
            Bounds bound = ShopGrid.GetBoundsOfCells(TouchingCells, true);
            RecalculateRoofs(bound, tileWeapon);
        }
    }
    //Return adjusted floor bounds of the floor.
    public Bounds GetFloorBounds(BaseWeapon tileWeapon)
    {
        Bounds bound = new Bounds(tileWeapon.OriginalBoundsCenter, tileWeapon.OriginalBoundsExtent * 2);
        float adjustedRatio = GetBoundsReductionRatio(tileWeapon.transform) * .5f;
        bound.extents *= (1 - adjustedRatio);
        return bound;
    }


    /// <summary> Applies roof bit on the list of cells </summary>
    private void SetRoofedOnCells(List<BVector3> CellUnder, bool roofed)
    {
        for (int i = 0; i < CellUnder.Count; i++)
        {
            if (roofed)
            {
                ShopGrid.Set(CellUnder[i], ShopGrid.SetShopGridBitData_OR(ShopGrid.Get(CellUnder[i]), GridBitData.ROOFED));
            }
            else
            {
                ShopGrid.Set(CellUnder[i], ShopGrid.SetShopGridBitData_AND(ShopGrid.Get(CellUnder[i]), ~GridBitData.ROOFED));
            }
        }
    }

    /// <summary>
    /// When a roof is removed from the shop grid, they can overlap with other roof bounds. In this case, we want to reapply the roof bool check for all nearby roofs
    /// </summary>
    /// <param name="bounds"></param>
    public void RecalculateRoofs(Bounds bounds, BaseWeapon ignoringTile)
    {
        // Get all nearby roofs and get their touching cells
        PhysicsUtil.OverlapBoxNonAlloc(bounds.center, bounds.extents, Quaternion.identity, LayerMask.GetMask("FloorTiles"), QueryTriggerInteraction.Ignore);
        HashSet<BVector3> TouchingCells = new HashSet<BVector3>();
        for (int i = 0; i < PhysicsUtil.BufferLength; i++)
        {
            HRFloorTile floor = PhysicsUtil.ColliderBuffer[i].GetComponentInParent<HRFloorTile>();
            if (floor && floor.netId != ignoringTile.netId)
            {
                TouchingCells.UnionWith(ShopGrid.GetCellsTouchingCollider(GetFloorBounds(ignoringTile)));
            }
        }

        // get all the cells underneath the cells.
        List<BVector3> CellUnder = ShopGrid.GetCellsUnder(TouchingCells.ToList());

        // Apply the roof bit on them again
        SetRoofedOnCells(CellUnder, true);

        // Reupdate the floor tiles
        for (int i = 0; i < PhysicsUtil.BufferLength; i++)
        {
            HRFloorTile floor = PhysicsUtil.ColliderBuffer[i].GetComponentInParent<HRFloorTile>();
            if (floor)
            {
                RecalculateFloor(floor);
            }
        }
        PhysicsUtil.ClearColliderArray();

    }
    /// <summary>
    /// Gets the average from the 4 spots of the floor tile. Originating from the center rounded down
    ///  ____ ____
    /// |    |    |
    /// |____|____|
    /// |    |    |
    /// |____|____|
    /// </summary>
    public void RecalculateFloor(HRFloorTile floor)
    {
        /*
        Bounds bounds = floor.OwningWeapon.MeshColliders[0].bounds;
        Vector3 boundCenter = bounds.center;
        Vector3 halfExtentsTopRight = new Vector3(bounds.extents.x, 0, bounds.extents.z) * .5f;
        Vector3 halfExtentsBottomRight = new Vector3(bounds.extents.x, 0, -bounds.extents.z) * .5f;
        ShopGridData gridData1 = ShopGrid.Get(boundCenter + halfExtentsTopRight); // Top Right
        ShopGridData gridData2 = ShopGrid.Get(boundCenter + halfExtentsBottomRight); // Bottom Right
        ShopGridData gridData3 = ShopGrid.Get(boundCenter - halfExtentsTopRight); // Bottom Left
        ShopGridData gridData4 = ShopGrid.Get(boundCenter - halfExtentsBottomRight); // Top Left
        floor.BeautyValue = (int)((gridData1.Beauty + gridData2.Beauty + gridData3.Beauty + gridData4.Beauty) * .25f);
        floor.IsRoofed = ShopGrid.GetBitData(gridData1, GridBitData.ROOFED);
        */
        if (floor.OwningShopPlot == null) { Debug.LogError("Error 104 - Floor's owning shop plot is null" + floor.name + " on " + (floor.transform.parent ? floor.transform.parent.name : "nil") + " | " + floor.transform.position); return; }
        floor.UpdateFloortileShopGrid();
        ShopGridData[] gridData = floor.GetShopGridData();
        if (gridData != null)
        {
            floor.BeautyValue = (int)((gridData[0].Beauty + gridData[1].Beauty + gridData[2].Beauty + gridData[3].Beauty) * .25f);
            floor.IsRoofed = ShopGrid.GetBitData(gridData[0], GridBitData.ROOFED);
        }
    }

    public void UpdateFloorTiles()
    {
        UpdateFloorTiles(ShopBounds.GetComponent<BoxCollider>().bounds);
    }

    public void UpdateFloorTiles(Bounds bounds)
    {
        PhysicsUtil.OverlapBoxNonAlloc(bounds.center, bounds.extents, Quaternion.identity, LayerMask.GetMask("FloorTiles"), QueryTriggerInteraction.Ignore);
        for (int i = 0; i < PhysicsUtil.BufferLength; i++)
        {
            HRFloorTile floor = PhysicsUtil.ColliderBuffer[i].GetComponentInParent<HRFloorTile>();
            if (floor)
            {
                RecalculateFloor(floor);
            }
        }
        PhysicsUtil.ClearColliderArray();
    }
    public HRShopManager GetClosestShop(Vector3 InPosition)
    {
        HRShopManager ClosestManager = null;
        if (Shops != null)
        {
            float ClosestDistance = float.MaxValue;
            for (int i = 0; i < Shops.Count; ++i)
            {
                if (!Shops[i])
                {
                    continue;
                }

                float CurrentDistance = Vector3.Distance(InPosition, Shops[i].transform.position);
                if (Shops[i] && CurrentDistance < ClosestDistance)
                {
                    ClosestManager = Shops[i];
                    ClosestDistance = CurrentDistance;
                }
            }
        }

        return ClosestManager;
    }

    public int GetShopIndex(HRShopManager InShopManager)
    {
        for (int i = 0; i < Shops.Count; ++i)
        {
            if (Shops[i] == InShopManager)
            {
                return i;
            }
        }

        return -1;
    }

    public HRShopManager GetShop(int InIndex)
    {
        if (Shops.Count > InIndex)
        {
            return Shops[InIndex];
        }

        return null;
    }

    public HRShopManager GetShop(string name)
    {
        name = name.ToLower().Trim();
        for (int i = 0; i < Shops.Count; ++i)
        {
            string shopName = Shops[i].ShopName.ToLower().Trim();
            if (shopName == name)
            {
                return Shops[i];
            }
        }
        return null;
    }

    public HRShopManager GetRandomShop()
    {
        if (Shops.Count == 0)
        {
            return null;
        }

        return Shops[Random.Range(0, Shops.Count)];
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        ((HRGameInstance)BaseGameInstance.Get)?.AddShopPlot(this);


        if (Unlocked)
        {
            SellerNPC.transform.parent.gameObject.SetActive(false);
        }

        // Sync up the client to this class
        if (!HRNetworkManager.IsHost())
        {
            if (((HRGameInstance)BaseGameInstance.Get).GetLocalPlayerController() == null)
            {
                HRNetworkManager.Get.LocalPlayerControllerChangedDelegate += SetUp;
            }
            else
            {
                SetUp((HRPlayerController)((HRGameInstance)BaseGameInstance.Get).GetLocalPlayerController());
            }
        }
        //

        //RequestTaskData_Command(connectionToClient);
    }

    public void SetUp(HRPlayerController controller)
    {
        HRNetworkManager.Get.LocalPlayerControllerChangedDelegate -= SetUp;
    }


    public override void OnStartServer()
    {
        base.OnStartServer();

        // Load shop info?
        if (Shops != null)
        {
            for (int i = 0; i < Shops.Count; ++i)
            {
                AddShop(Shops[i]);
            }
        }

        this.enabled = true;
    }

    void LoadShops()
    {

    }

    public void UnlockShop()
    {
        SetUnlockedShops(NumShopsToAdd + 1);
        AddNewShop();
    }

    public void SetUnlockedShops(int NumShops)
    {
        if (NumShopsToAdd != NumShops)
        {
            NumShopsToAdd = NumShops;

            for (int i = 0; i < (NumShops - Shops.Count); ++i)
            {
                AddNewShop();
            }
        }
    }

    public void AddShop(HRShopManager InShopManager)
    {
        if (InShopManager == null)
        {
            return;
        }

        InShopManager.ShopEntity.MaxCustomersAtOnce = MaxCustomersInShop;

        if (HRNetworkManager.IsHost())
        {
            InShopManager.ItemSoldDelegate += OnShopManagerMakeSale;
        }
    }

    HRShopManager AddNewShop()
    {
        GameObject ShopPrefab = ((HRGameInstance)BaseGameInstance.Get).ShopManagerPrefab;
        if (!ShopPrefab || (OverrideShopPrefab != null && ShopPrefab != OverrideShopPrefab))
        {
            ShopPrefab = OverrideShopPrefab;
        }

        GameObject ShopInstance = Instantiate(ShopPrefab);
        HRShopManager ShopManager = ShopInstance.GetComponent<HRShopManager>();
        //ShopManager.SaveComponent.IDComponent.ForceGenerateID(); // removed this because the UniqueID is overridden right after anyways

        if (ShopManager)
        {
            ShopManager.OwningShopPlot = this;
            ShopManager.InitializeShopForPlot_Server();
            ShopManager.LevelUpDelegate += HandleShopLevelUp;
        }

        if (this) { ShopInstance.transform.position = this.transform.position; }
        Mirror.NetworkServer.Spawn(ShopInstance);

        AddShop(ShopManager);

        return ShopManager;
    }

    void HandleShopLevelUp(HRShopManager InShopManager, int Level, float EXP, float NextEXP)
    {
        // Multiple shops support
        return;

        if (HRGameInstance.Get.CurrentOnlineSubsystem.IsDevBranch())
        {
            int TotalShopLevel = 0;

            if (Shops != null)
            {
                for (int i = 0; i < Shops.Count; ++i)
                {
                    if (Shops[i])
                    {
                        TotalShopLevel += Shops[i].GetShopEntity().GetShopLevel();
                    }
                }
            }

            SetUnlockedShops((int)UnlockedNumberShopsCurve.Evaluate(TotalShopLevel));
        }
    }

    private void OnShopManagerMakeSale(HRShopManager InShopManager, HRSaleInfo InSaleInfo)
    {
        ItemSoldDelegate?.Invoke(InShopManager, InSaleInfo);
    }


    void HandleHourChanged(HRDayManager InManager, int OldTime, int NewTime)
    {
        RecalculateCustomerPercentageChances();

        CurrentCustomerRate = InManager.GetCurrentTimeZone().SpawnRate;

        //if (CurrentCustomerRate <= 0)
        //{
        //    for (int i = SpawnedCustomers.Count - 1; i >= 0; --i)
        //    {
        //        SpawnedCustomers[i].LeavePlot(false);
        //    }
        //}
    }

    void RecalculateCustomerPercentageChances()
    {
        if (((HRGameManager)BaseGameManager.Get))
        {
            CustomerDB.CalculatePercentageChances(((HRGameManager)BaseGameManager.Get).DayManager.TimeHour);
        }
    }

    public HRCustomerSpawner GetCustomerSpawner()
    {
        if (CustomerSpawners != null && CustomerSpawners.Length > 0 && CustomerSpawners[0])
        {
            return CustomerSpawners[0];
        }

        return null;
    }

    private float CalculateQuestCharacterSpawnProbability()
    {
        //return 0;  // TEMP DEBUG
        if (!QuestCustomers) return 0;
        if (QuestCustomers.NumUnlocked - QuestCustomers.NumSpawned <= 0)
        {
            //Debug.Log("all quest customers have been spawned");
            bAllSpawned = true;
            return 0f;
        }
        if (QuestCustomers.questCharacterDebugMode)
        {
            return 1f;
        }
        if (bQuestCharacterSpawned || bAllSpawned && QuestCustomers.NumUnlocked - QuestCustomers.NumSpawned > 0)
        {
            bAllSpawned = false;
            return QuestCustomers.initProbability * (float)System.Math.Pow(QuestCustomers.customerPenalty, LocalQuestCharacterCount);
        }
        else
        {
            return System.Math.Min(QuestCustomers.maxProbability, QuestCharacterSpawnProbability * QuestCustomers.increaseRate);
        }
    }

    public HRCustomerAI SpawnPlotCustomer(string CustomerType = "")
    {
        if (StartingTile == null)
        {
            Debug.Log("There's no starting tile for this shop plot. Cannot spawn plot customer.");
            return null;
        }
        if (bShouldSpawnPlotCustomers == false)
        {
            return null;
        }

        /*
        if (HRGameManager.Get && (HRGameManager.Get as HRGameManager).DayManager.GetCurrentTimeZone().ZoneType == HRTimeZoneType.DEAD)
        {
            return null;
        }
        */


        if (CurrentPlotCustomers < MaxPlotCustomers)
        {
            HRCustomerSpawner SpawnerToUse = GetCustomerSpawner();
            if (SpawnerToUse)
            {
                // Use UnlockedCustomers from entity manager.
                HRCustomerAI SpawnedCustomer = null;
                string ID = "";

                QuestCharacterSpawnProbability = CalculateQuestCharacterSpawnProbability();
                //Debug.Log("P = " + QuestCharacterSpawnProbability.ToString());
                float randomFloat = Random.Range(0f, 1f);

                // QUEST CHARACTER
                if (randomFloat < QuestCharacterSpawnProbability)
                {
                    bQuestCharacterSpawned = true;  // set to true

                    int customerIdx = QuestCustomers.ChooseCustomer();
                    GameObject ValidCustomerPrefab = QuestCustomers.Spawn(customerIdx);
                    //Debug.Log("SPECIAL: " + customerIdx.ToString());
                    LocalQuestCharacterCount++;

                    if (ValidCustomerPrefab)
                    {
                        SpawnedCustomer = SpawnerToUse.SpawnCustomer(ValidCustomerPrefab, true);
                        // Set up initial visits
                        if (QuestCustomers.questCustomerList[customerIdx].initialVisits > 0)
                        {
                            SpawnedCustomer.bOverrideSearchModeProbability = true;
                            SpawnedCustomer.overrideSearchModeProbability = 0.25f * (QuestCustomers.questCustomerList[customerIdx].initialVisits + 1);
                            QuestCustomers.questCustomerList[customerIdx].initialVisits--;
                        }
                        else
                        {
                            SpawnedCustomer.bOverrideSearchModeProbability = false;
                        }
                    }

                    if (SpawnedCustomer)
                    {
                        SpawnedCustomer.EnterPlot(this);

                        CurrentPlotCustomers++;
                        SpawnedCustomer.OnLeavePlotDelegate -= HandleCustomerLeavePlot;
                        SpawnedCustomer.OnLeavePlotDelegate += HandleCustomerLeavePlot;

                        QuestCharacterDict.Add(SpawnedCustomer.OwningPlayerCharacter, customerIdx);
                        SpawnedCustomer.OwningPlayerCharacter.OnCharacterDestroyedDelegate -= HandleCustomerDestroyed;
                        SpawnedCustomer.OwningPlayerCharacter.OnCharacterDestroyedDelegate += HandleCustomerDestroyed;
                    }

                    return SpawnedCustomer;
                }
                // REGULAR CHARACTER
                else
                {
                    bQuestCharacterSpawned = false;  // set to false
                    //Debug.Log("REGULAR");

                    if (string.IsNullOrEmpty(CustomerType))
                    {
                        // This hasn't been initialized yet.
                        if (HRShopEntityManager.Get.UnlockedCustomers.Count != 0)
                        {
                            GameObject ValidCustomerPrefab = CustomerDB.GetCustomerFromUnlockedList(HRShopEntityManager.Get.UnlockedCustomers, out ID);
                            if (ValidCustomerPrefab)
                            {
                                SpawnedCustomer = SpawnerToUse.SpawnCustomer(ValidCustomerPrefab, true);
                            }
                            else
                            {
                                SpawnedCustomer = SpawnerToUse.SpawnCustomer(true);
                            }

                            if (SpawnedCustomer)
                            {
                                SpawnedCustomer.CustomerType = ID;
                                SpawnedCustomer.EnterPlot(this);

                                CurrentPlotCustomers++;
                                SpawnedCustomer.OnLeavePlotDelegate -= HandleCustomerLeavePlot;
                                SpawnedCustomer.OnLeavePlotDelegate += HandleCustomerLeavePlot;
                            }

                            return SpawnedCustomer;
                        }
                    }
                    else
                    {
                        int target = CustomerDB.GetCustomerDataIndex(CustomerType);

                        if (target < 0)
                        {
                            target = 0;
                        }

                        GameObject ValidCustomerPrefab = CustomerDB.Customers[target].CustomerPrefab;
                        if (ValidCustomerPrefab)
                        {
                            SpawnedCustomer = SpawnerToUse.SpawnCustomer(ValidCustomerPrefab, true);
                        }
                        else
                        {
                            SpawnedCustomer = SpawnerToUse.SpawnCustomer(true);
                        }

                        if (SpawnedCustomer)
                        {
                            SpawnedCustomer.CustomerType = CustomerDB.Customers[target].CustomerName;
                            SpawnedCustomer.EnterPlot(this);

                            CurrentPlotCustomers++;
                            SpawnedCustomer.OnLeavePlotDelegate -= HandleCustomerLeavePlot;
                            SpawnedCustomer.OnLeavePlotDelegate += HandleCustomerLeavePlot;
                        }

                        return SpawnedCustomer;
                    }
                }
            }
        }

        return null;
    }

    void HandleCustomerDestroyed(HeroPlayerCharacter InPlayerCharacter)
    {
        try
        {
            int customerIdx = QuestCharacterDict[InPlayerCharacter];
            QuestCharacterDict.Remove(InPlayerCharacter);
            QuestCustomers.Despawn(customerIdx);
            //Debug.Log("DESPAWNED " + customerIdx.ToString());
            LocalQuestCharacterCount--;
        }
        catch
        {
        }
    }

    void HandleCustomerLeavePlot(HRCustomerAI InCustomer, bool bUnhappy)
    {
        CurrentPlotCustomers--;
        InCustomer.OnLeavePlotDelegate -= HandleCustomerLeavePlot;

        if (SpawnedCustomers.Contains(InCustomer))
        {
            SpawnedCustomers.Remove(InCustomer);
        }
    }

    float CustomerSpawnTimer = 0.0f;
    float CurrentCustomerRate = 1;
    public void Update()
    {
        if (HRNetworkManager.IsHost())
        {
            if (bShouldSpawnPlotCustomers && (CurrentPlotCustomers < MinPlotCustomers))
            {
                if (CustomerDB)
                {
                    if (CustomerSpawnTimer <= 0.0f)
                    {
                        var customer = SpawnPlotCustomer();
                        AddCustomer(customer);

                        CustomerSpawnTimer = CustomerSpawnCooldown;
                    }
                    else
                    {
                        CustomerSpawnTimer -= (Time.deltaTime * CurrentCustomerRate);
                    }
                }
            }
        }
        else
        {
            this.enabled = false;
        }
    }


    public void AddCustomer(HRCustomerAI customer)
    {

        if (customer != null)
        {
            SpawnedCustomers.Add(customer);
        }
    }


    public int GetPlotCost()
    {
        return Cost;
    }

    public string GetPlotName()
    {
        return PlotName;
    }

    public string GetPlotArea()
    {
        return AssociatedArea.ToString();
    }

    public string GetResourcesString()
    {
        string str = "";
        for (int i = 0; i < UnlockableResources.Length; i++)
        {
            str += UnlockableResources[i].Unlock.GetComponent<BaseWeapon>().ItemName + "[x" + (int)(UnlockableResources[i].BonusMultiplier) + "]";
            if (UnlockableResources.Length > 1)
            {
                if (i != UnlockableResources.Length - 1)
                {
                    if (i == UnlockableResources.Length - 2)
                    {
                        str += " and ";
                    }
                    else
                    {
                        str += ", ";
                    }
                }
            }
        }

        return str;
    }


    public void LoadChunkLayers(bool load)
    {
        /*
        if (load && !HasShopsRunning())
        {
            // Dont load chunks if there are no shops running
            return;
        }
        */

        HashSet<SECTR_Sector> sectors = new HashSet<SECTR_Sector>();

        /*
        SECTR_Sector.FastGetContaining(ref sectors, ShopBounds.GetComponent<BoxCollider>().bounds, Sectr_Type.TERRAIN);
        foreach (SECTR_Sector sector in sectors)
        {
            if (load)
            {
                BaseWorldStreamManager.Get.LoadChunk(sector.name, ((HRGameInstance)BaseGameInstance.Get).GetLocalPlayerController());
                sector.Chunk.bFreezeState = true;
            }
            else
            {
                BaseWorldStreamManager.Get.UnloadChunk(sector.name, ((HRGameInstance)BaseGameInstance.Get).GetLocalPlayerController());
                sector.Chunk.bFreezeState = false;
            }
        }
        */

        if (ShopBounds == null) return;

        BoxCollider shopCollider = ShopBounds.GetComponent<BoxCollider>();
        BaseScripts.BasePlayerController controller = ((HRGameInstance)BaseGameInstance.Get).GetLocalPlayerController();
        BaseScripts.BasePawn localPawn = ((HRGameInstance)BaseGameInstance.Get).GetLocalPlayerPawn();
        SECTR_Sector.FastGetContaining(ref sectors, shopCollider.bounds, Sectr_Type.PROPS);
        foreach (SECTR_Sector sector in sectors)
        {
            if (load)
            {
                WaitingSectorsToLoadList.Add(sector.name);
                BaseWorldStreamManager.Get.LoadChunk(sector.name, controller);
                //sector.Chunk.bFreezeState = true; //commenting out for now so QA can keep testing multiplaeyr in open world
            }
            else
            {
                //sector.Chunk.bFreezeState = false;
                BaseWorldStreamManager.Get.UnloadChunk(sector.name, controller);
            }
        }

        SECTR_Sector.FastGetContaining(ref sectors, ShopBounds.GetComponent<BoxCollider>().bounds, Sectr_Type.TERRAIN);
        foreach (SECTR_Sector sector in sectors)
        {
            if (load && !BaseWorldStreamManager.Get.IsChunkLoadedForPlayer(sector.name, localPawn))
            {
                WaitingSectorsToLoadList.Add(sector.name);
            }
        }

        if (load)
        {
            if (WaitingSectorsToLoadList.Count > 0)
            {
                WaitingSectorsToLoad = true;
                HRSaveSystem.Get.OnSceneFinishedLoaded += OnChunkLoaded;
            }
            else
            {
                CheckShopWeapons();
            }
        }
    }

    public override void OnDestroy()
    {
        if (ShopBounds == null) return;

        if (HRNetworkManager.IsHost())
        {
            LoadChunkLayers(false);
        }

        if (HRShopEntityManager.Get)
        {
            HRShopEntityManager.Get.RegisterShopPlot(this, false);
        }

        for (int i = Shops.Count - 1; i >= 0; --i)
        {
            if (Shops[i])
            {
                NetworkServer.Destroy(Shops[i].gameObject);
            }
        }

        foreach (HRCustomerSpawner cs in CustomerSpawners)
        {
            cs.ClearCustomers();
        }

        base.OnDestroy();

    }

    [System.NonSerialized]
    public int OwningComponentID;
    [System.NonSerialized]
    public int OwningAuxIndex;

    public void HandleSaveComponentInitialize(HRSaveComponent InSaveComponent, int ComponentID, int AuxIndex)
    {
        OwningComponentID = ComponentID;
        OwningAuxIndex = AuxIndex;
    }

    public void HandlePreSave()
    {
        string PlotUniqueID = IDComponent.GetUniqueID();

        HRSaveSystem.Get.CurrentFileInstance.Save<int>(PlotUniqueID + "NumShops", GetNumShops());

        for (int i = Shops.Count - 1; i >= 0; --i)
        {
            if (Shops[i])
            {
                // Save the instance ID to reapply it on load
                string ShopManagerID = "Shop";
                string IDPostfix = "ID" + i.ToString();

                //Yiming: if already ends with "ID0" etc. don't append another one
                if (!ShopManagerID.EndsWith(IDPostfix))
                {
                    ShopManagerID += IDPostfix;
                }
                HRSaveSystem.Get.CurrentFileInstance.Save<string>(PlotUniqueID + "IDKey" + i.ToString(), ShopManagerID);
            }
        }

        if (!HRSaveSystem.bRunningAutoSave)
        {
            SavePlotNavMesh();
        }
        else
        {
            AutoSavingPlots.Add(this);
        }

    }

    public void SavePlotNavMesh()
    {
        if (graphRef != null)
        {
            byte[] bytes = AStarGraphSaver.SaveGraph(graphRef, null);
            savedNavMesh = bytes;
        }
    }

    public void HandleLoaded()
    {
        Debug.Log("HandleLoaded " + PlotName + " : Unlocked:" + Unlocked);
        if (Mirror.NetworkServer.active)
        {
            LoadShops();

            InitializePlot();
            LoadPlotNavMesh();
        }
    }

    public void HandleSaved()
    {
        savedNavMesh = null;
    }

    public void HandleReset()
    {

    }

    public bool IsSaveDirty()
    {
        return true;
    }

    [System.Serializable]
    public class GatherUnlockData
    {
        public GameObject Unlock;
        [Range(.5f, 4f)]
        public float BonusMultiplier = 1;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (ShopGrid == null)
            return;
        if (!HRGizmosManager.hasInstance || !HRGizmosManager.instance.showShopDebug)
            return;
        if (Vector3.SqrMagnitude(HRGizmosManager.instance.CameraPosition - transform.position) > 5000)
            return;

        Vector3 offset = ShopGrid.originPosition + (Vector3.one * ShopGrid.CellSize * .5f);
        Vector3 size = Vector3.one * ShopGrid.CellSize;
        Color reddy = Color.red;
        reddy.a = .2f;
        Color whity = Color.white;
        float whiteAlpha = .1f;
        whity.a = whiteAlpha;
        int y = (int)(ShopGrid.gridSizeY / 5f);
        for (byte i = 0; i < ShopGrid.gridSizeX; i++)
        {
            for (byte j = 0; j < y; j++)
            {
                for (byte k = 0; k < ShopGrid.gridSizeZ; k++)
                {
                    Vector3 position = offset + (new Vector3(i, j, k) * ShopGrid.CellSize);
                    float sqrMag = (HRGizmosManager.instance.CameraPosition - position).sqrMagnitude;
                    if (sqrMag < 100f)
                    {
                        ShopGridData data = ShopGrid.Get(i, j, k);
                        if ((data.BitData & GridBitData.ROOFED) != 0)
                        {
                            Gizmos.color = reddy;
                        }
                        else
                        {
                            whity.a = whiteAlpha * (1 - (sqrMag / 100f));
                            Gizmos.color = whity;
                        }
                        Gizmos.DrawWireCube(position, size);

                        if (HRGizmosManager.instance.showShopBeauty)
                        {
                            whity.a = whiteAlpha * (1 - (sqrMag / 100f));
                            Gizmos.color = whity;
                            Handles.Label(position, "" + data.Beauty);
                        }

                    }
                }
            }
        }
    }
#endif

    #region Lua Functions
    public void RegisterDialogueLua()
    {
        //Debug.LogError("REGISTERING WITH LUA");
        Lua.RegisterFunction("GetPlotCost", this, SymbolExtensions.GetMethodInfo(() => GetPlotCost()));
        Lua.RegisterFunction("GetPlotName", this, SymbolExtensions.GetMethodInfo(() => GetPlotName()));
        Lua.RegisterFunction("GetPlotArea", this, SymbolExtensions.GetMethodInfo(() => GetPlotArea()));
        Lua.RegisterFunction("GetResourcesString", this, SymbolExtensions.GetMethodInfo(() => GetResourcesString()));
        DialogueLua.SetVariable("PlotID", PlotID);
        HRDialogueSystem.Get.OnConversationEndedDelegate += Deregister;
    }

    private void Deregister(HRDialogueSystem DialogueSystem, int ConversationID)
    {
        DeregisterDialogueLua();
        HRDialogueSystem.Get.OnConversationEndedDelegate -= Deregister;
    }
    public void DeregisterDialogueLua()
    {
        //Debug.LogError("deREGISTERING WITH LUA");
        Lua.UnregisterFunction("GetPlotCost");
        Lua.UnregisterFunction("GetPlotName");
        Lua.UnregisterFunction("GetPlotArea");
        Lua.UnregisterFunction("GetResourcesString");
    }
    #endregion
}

public class ShopDataGrid
{
    ShopGridData[] ShopGrid;

    public byte CellSize;
    private float CellSizeRatio;
    public Vector3 originPosition;
    private Vector3 CellExtents;
    private Vector3 AdjustedCellExtents;

    public byte gridSizeX;
    public byte gridSizeY;
    public byte gridSizeZ;

    float floatGridSizeX;
    float floatGridSizeY;
    float floatGridSizeZ;

    public float[] reciprocals = new float[] { 1, 1, .5f, .25f, .125f, 0.0625f, 0.03125f };

    /// <summary>
    /// Data list containing the information of items that are effecting a cell. short - ItemID, btye - number of times it is effecting the spot
    /// </summary>
    public Dictionary<BVector3, Dictionary<short, byte>> EffectingItems = new Dictionary<BVector3, Dictionary<short, byte>>();

    public void CreateShopGrid(byte x, byte y, byte z, byte CellSize, Vector3 originPosition)
    {
        gridSizeX = x;
        gridSizeY = y;
        gridSizeZ = z;
        this.CellSize = CellSize;

        floatGridSizeX = gridSizeX * CellSize;
        floatGridSizeY = gridSizeY * CellSize;
        floatGridSizeZ = gridSizeZ * CellSize;

        CellSizeRatio = 1f / CellSize;
        this.originPosition = originPosition;
        CellExtents = new Vector3(CellSize * .5f, CellSize * .5f, CellSize * .5f);
        AdjustedCellExtents = CellExtents * .98f;
        ShopGrid = new ShopGridData[x * y * z];
    }

    public ShopGridData[] GetShopGridData()
    {
        return ShopGrid;
    }
    public ShopGridData Get(Vector3 worldPosition)
    {
        BVector3 coords = GetGridCoords(worldPosition);

        return Get(coords);
    }

    public BVector3 GetGridCoords(Vector3 worldPosition)
    {
        Vector3 diff = worldPosition - originPosition;
        if ((diff.y > floatGridSizeY || diff.y < 0))
        {
            if ((diff.x > floatGridSizeX || diff.x < 0) || (diff.z > floatGridSizeZ || diff.z < 0))
            {
                Debug.LogError("Out of Bounds check for ShopGrid 104: " + (diff.x + ">" + (floatGridSizeX)) + "," + (diff.y + ">" + (floatGridSizeY)) + "," + (diff.z + ">" + (floatGridSizeZ)));
                return new BVector3(0, 0, 0);
            }
            else if (diff.y > -1) // if the item is slightly below the ground, this should count as the first layer
            {
                diff += Vector3.up;
            }
        }


        byte x = (byte)(diff.x * CellSizeRatio);
        byte y = (byte)(diff.y * CellSizeRatio);
        byte z = (byte)(diff.z * CellSizeRatio);

        //Debug.Log($"{x}-{y}-{z}");

        return new BVector3(x, y, z);
    }

    public ShopGridData Get(BVector3 coord)
    {
        return Get(coord.x, coord.y, coord.z);
    }

    public ShopGridData Get(byte x, byte y, byte z)
    {
        return ShopGrid[x + y * gridSizeX + z * gridSizeX * gridSizeY];
    }

    public void Set(BVector3 coord, ShopGridData value)
    {
        Set(coord.x, coord.y, coord.z, value);
    }
    public void Set(byte x, byte y, byte z, ShopGridData value)
    {
        ShopGrid[x + y * gridSizeX + z * gridSizeX * gridSizeY] = value;
    }

    private bool GetPointsWithinRadiusAccurate(byte centerX, byte centerY, byte centerZ, byte x, byte y, byte z, byte radius)
    {
        byte dx = (byte)(x - centerX);
        byte dy = (byte)(y - centerY);
        byte dz = (byte)(z - centerZ);
        int distanceSquared = dx * dx + dy * dy + dz * dz;
        return distanceSquared <= radius * radius;
    }

    public List<ShopGridData> GetPointsWithinRadiusAccurate(byte centerX, byte centerY, byte centerZ, byte radius)
    {
        List<ShopGridData> points = new List<ShopGridData>();
        for (byte x = 0; x < gridSizeX; x++)
        {
            for (byte y = 0; y < gridSizeY; y++)
            {
                for (byte z = 0; z < gridSizeZ; z++)
                {
                    if (GetPointsWithinRadiusAccurate(centerX, centerY, centerZ, x, y, z, radius))
                    {
                        points.Add(Get(x, y, z));
                    }
                }
            }
        }
        return points;
    }

    private bool IsPointWithinRadiusSimple(byte centerX, byte centerY, byte centerZ, byte x, byte y, byte z, byte radius)
    {
        byte dx = (byte)(Mathf.Abs(x - centerX));
        byte dy = (byte)(Mathf.Abs(y - centerY));
        byte dz = (byte)(Mathf.Abs(z - centerZ));
        return dx + dy + dz <= radius;
    }

    public List<(BVector3, ShopGridData)> GetPointsWithinRadiusSimple(Vector3 worldPosition, byte radius)
    {
        BVector3 coords = GetGridCoords(worldPosition);
        return GetPointsWithinRadiusSimple(coords, radius);
    }

    public List<(BVector3, ShopGridData)> GetPointsWithinRadiusSimple(BVector3 coord, byte radius)
    {
        return GetPointsWithinRadiusSimple(coord.x, coord.y, coord.z, radius);
    }
    public List<(BVector3, ShopGridData)> GetPointsWithinRadiusSimple(byte centerX, byte centerY, byte centerZ, byte radius)
    {
        List<(BVector3, ShopGridData)> points = new List<(BVector3, ShopGridData)>();

        List<BVector3> cells = GetCellsNearCollider(GetCellWorldPosition(new BVector3(centerX, centerY, centerZ)), radius);

        for (int i = 0; i < cells.Count; i++)
        {
            if (IsPointWithinRadiusSimple(centerX, centerY, centerZ, cells[i].x, cells[i].y, cells[i].z, radius))
            {
                points.Add((cells[i], Get(cells[i])));
            }
        }

        /*
        // This can be optimized to not look at every point.
        for (byte x = 0; x < gridSizeX; x++)
        {
            for (byte y = 0; y < gridSizeY; y++)
            {
                for (byte z = 0; z < gridSizeZ; z++)
                {
                    if (IsPointWithinRadiusSimple(centerX, centerY, centerZ, x, y, z, radius))
                    {
                        points.Add((new BVector3(x, y, z), Get(x, y, z)));
                    }
                }
            }
        }
        */
        return points;
    }

    public void AddEffectingItem(BVector3 coord, short shortItemID)
    {
        if (EffectingItems.ContainsKey(coord))
        {
            if (EffectingItems[coord].ContainsKey(shortItemID))
            {
                EffectingItems[coord][shortItemID] += 1;
                int count = EffectingItems[coord][shortItemID];
            }
            else
            {
                EffectingItems[coord].Add(shortItemID, 1);
            }
        }
        else
        {
            EffectingItems.Add(coord, new Dictionary<short, byte> { { shortItemID, 1 } });
        }
    }

    public void RemoveEffectingItem(BVector3 coord, short shortItemID)
    {
        if (EffectingItems.ContainsKey(coord))
        {
            if (EffectingItems[coord].ContainsKey(shortItemID))
            {
                if (EffectingItems[coord][shortItemID] <= 1)
                {
                    EffectingItems[coord].Remove(shortItemID);
                }
                else
                {
                    EffectingItems[coord][shortItemID] -= 1;
                }
            }

        }
    }
    public List<BVector3> GetCellsTouchingCollider(Collider collider)
    {
        return GetCellsTouchingCollider(collider.bounds);
    }
    public List<BVector3> GetCellsTouchingCollider(Bounds bounds)
    {
        List<BVector3> cellsTouchingCollider = new List<BVector3>();

        List<BVector3> cells = GetCellsNearCollider(bounds); // Gets the only possible cells that could be touching this collider

        Bounds boundCheck = new Bounds();
        boundCheck.extents = AdjustedCellExtents;

        int count = cells.Count;
        for (int i = 0; i < count; i++)
        {
            boundCheck.center = GetCellWorldPosition(cells[i]) + CellExtents;
            if (boundCheck.Intersects(bounds))
            {
                cellsTouchingCollider.Add(cells[i]);
            }
        }

        return cellsTouchingCollider;
    }

    public Bounds GetBoundsOfCells(List<BVector3> cells, bool MaxOutY = false)
    {
        if (cells.Count <= 0) { Debug.LogError("Passed in null cells in GetBoundsOfCells"); return new Bounds(); }
        Bounds bounds = new Bounds(GetCellWorldPosition(cells[0]) + CellExtents, CellExtents * 2);
        for (int i = 1; i < cells.Count; i++)
        {
            bounds.Encapsulate(GetCellWorldPosition(cells[i]));
            bounds.Encapsulate(GetCellWorldPosition(cells[i]) + CellExtents * 2);
        }

        if (MaxOutY)
        {
            Vector3 horizontalExtent = new Vector3(bounds.extents.x, 0, bounds.extents.z);
            bounds.Encapsulate(bounds.center + horizontalExtent + Vector3.up * gridSizeY * CellSize); // top right
            bounds.Encapsulate(bounds.center - horizontalExtent - Vector3.up * gridSizeY * CellSize); // bottom left
        }
        return bounds;
    }

    public List<BVector3> GetCellsNearCollider(Collider collider)
    {
        return GetCellsNearCollider(collider.bounds);
    }
    public List<BVector3> GetCellsNearCollider(Bounds bounds)
    {
        return GetCellsNearCollider(bounds.center, bounds.extents.magnitude);
    }
    public List<BVector3> GetCellsNearCollider(Vector3 center, float radius)
    {
        List<BVector3> cellsNearCollider = new List<BVector3>();

        Vector3 localCenter = GetGridCoords(center).CastToVector3();

        int maxExtent = Mathf.CeilToInt(radius);
        Vector3Int minIndex = Vector3Int.FloorToInt(localCenter - Vector3.one * maxExtent);
        minIndex = new Vector3Int(Mathf.Clamp(minIndex.x, 0, gridSizeX), Mathf.Clamp(minIndex.y, 0, gridSizeY), Mathf.Clamp(minIndex.z, 0, gridSizeZ));
        Vector3Int maxIndex = Vector3Int.CeilToInt(localCenter + Vector3Int.one * maxExtent);
        maxIndex = new Vector3Int(Mathf.Clamp(maxIndex.x, 0, gridSizeX), Mathf.Clamp(maxIndex.y, 0, gridSizeY), Mathf.Clamp(maxIndex.z, 0, gridSizeZ));

        for (int x = minIndex.x; x < maxIndex.x; x++)
        {
            for (int y = minIndex.y; y < maxIndex.y; y++)
            {
                for (int z = minIndex.z; z < maxIndex.z; z++)
                {
                    cellsNearCollider.Add(new BVector3((byte)x, (byte)y, (byte)z));
                }
            }
        }

        return cellsNearCollider;
    }

    /// <summary>
    /// Gets the list of cells underneath given cells.
    /// </summary>
    public List<BVector3> GetCellsUnder(List<BVector3> cells)
    {
        HashSet<BVector3> cellsUnder = new HashSet<BVector3>();

        int count = cells.Count;
        for (int i = 0; i < count; i++)
        {
            byte y = cells[i].y;
            while (y > 0)
            {
                y--;
                BVector3 coord = new BVector3(cells[i].x, y, cells[i].z);
                if (!cellsUnder.Contains(coord))
                {
                    cellsUnder.Add(coord);
                }
                else // If there is a cell that is already added, this means the stack of cells is already calculated
                {
                    break;
                }
            }

        }
        return cellsUnder.ToList();
    }

    public Vector3 GetCellWorldPosition(BVector3 coord)
    {
        return originPosition + new Vector3(coord.x, coord.y, coord.z) * CellSize;
    }
    public bool GetBitData(ShopGridData data, GridBitData comparator)
    {
        return (data.BitData & comparator) != 0;
    }

    public ShopGridData SetShopGridBitData_AND(ShopGridData data, GridBitData bitdata)
    {
        return new ShopGridData(data.Beauty, data.BitData & bitdata);
    }
    public ShopGridData SetShopGridBitData_OR(ShopGridData data, GridBitData bitdata)
    {
        return new ShopGridData(data.Beauty, data.BitData | bitdata);
    }
    public ShopGridData SetShopGridBitData_OVERRIDE(ShopGridData data, GridBitData bitdata)
    {
        return new ShopGridData(data.Beauty, bitdata);
    }
}

[System.Serializable]
public struct ShopGridData
{
    public short Beauty;
    public GridBitData BitData;

    public ShopGridData(float Beauty, GridBitData BitData)
    {
        this.Beauty = (short)Beauty;
        this.BitData = BitData;
    }
    public ShopGridData(short Beauty, GridBitData BitData)
    {
        this.Beauty = Beauty;
        this.BitData = BitData;
    }
}

[System.Flags]
public enum GridBitData : byte
{
    ROOFED = 1 << 0,
    FLAG2 = 1 << 1,
    FLAG3 = 1 << 2,
    FLAG4 = 1 << 3,
    FLAG5 = 1 << 4,
    FLAG6 = 1 << 5,
    FLAG7 = 1 << 6,
    FLAG8 = 1 << 7
}

public struct BVector3
{
    public byte x;
    public byte y;
    public byte z;
    /// <summary>
    /// Vector3 made up of bytes
    /// </summary>
    public BVector3(byte x, byte y, byte z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}