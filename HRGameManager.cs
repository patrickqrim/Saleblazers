using BehaviorDesigner.Runtime;
using GPUInstancer;
using Mirror;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class HRGameManager : BaseGameManager
{
    public FBaseGameManagerStartSignature GameManagerPreStartedDelegate;

    public delegate void HREnemySignature(HREnemyAI InEnemy);
    public HREnemySignature OnEnemyDefeatedDelegate;

    public delegate void HRCraftingComponentSignature(HRCraftingComponent InComponent, int ItemID);
    public HRCraftingComponentSignature OnItemCraftedDelegate;

    public delegate void HRBuildingPlacedSignature(BaseWeapon Target, bool bPlaced);
    public HRBuildingPlacedSignature OnBuildingPlaced;

    public delegate void HRBuildingPlacedLocalSignature(BaseWeapon Target, bool bPlaced);
    public HRBuildingPlacedLocalSignature OnBuildingPlacedLocal;

    public delegate void HRItemDisplayedSignature(BaseWeapon Target, HRDisplayContainer Display);
    public HRItemDisplayedSignature OnItemDisplayed;

    public delegate void HRItemPlacedInPlotSignature(HRShopManager Target, BaseWeapon InWeapon);
    public HRItemPlacedInPlotSignature OnItemPlacedInPlot;

    public delegate void OnSkillActiveSyncSignature(HRSkillNode TargetSkill, int Root, bool bActive);
    public OnSkillActiveSyncSignature OnShopSkillActiveSyncDelegate;

    public delegate void OnMessCleanedSignature(HRMessDecal InMessDecal);
    public OnMessCleanedSignature OnMessCleanedDelegate;

    // If we should save the persistent inventory.
    public bool bUsePersistentPlayerInventory = true;
    // If we should save this level.
    public bool bSaveLevelData = true;
    public bool bSaveDialogueData = true;

    [System.NonSerialized]
    public HRShopManager PlayerControlledShop;

    public bool bSaveInstanceOnLeaveTEMP = true;
    public HRGameMapInitializationDatabase MasterInitializationDatabase;
    public PixelCrushers.DialogueSystem.ExtraDatabases LevelExtraDatabases;
    public UnityEvent GameStartEvents;
    public List<WallTileShowTrigger> WallTileShowTriggers = new List<WallTileShowTrigger>();
    public BaseVictoryConditionManager VictoryConditionManager;
    public HRPlayerHomeManager PlayerHomeManager;
    public HRGestureWheelUI GestureWheelUI;

    [SerializeField]
    private HRDayManager dayManager;
    [SerializeField]
    private HRSleepManager sleepManager;
    [SerializeField]
    private GameZoneManager gameZoneManager;
    [SerializeField]
    private GameOutpostManager outpostManager;
    [SerializeField]
    private HREnemySpawnerManager enemySpawnerManager;
    [SerializeField]
    private BaseTimeMusicManager timeMusicManager;
    [SerializeField]
    private BaseTimeAmbientManager timeAmbientManager;
    [SerializeField]
    private BaseMusicManager ambientManager;
    [SerializeField]
    private HREndScreenManager endScreenManager;
    [SerializeField]
    private HRRaidSystem raidManager;
    [SerializeField]
    private HRBankSystem bankSystem;
    //dont make private
    public HRLightningManager lightningManager;
    

    public bool bIsMainMission = false;
    public bool bAutoStartDayManager = true;

    public Light TimeModifiedDirectionalLight;

    // This will be filled out if this is the player's persistent shop.
    public HRShopManager PersistentPlayerShop;

    public bool bUseChecklist;

    public bl_MiniMap MiniMapRef;

    public HREndDayUI EndDayUI;

    public HRCalendarUI CalendarUI;
    public BasePopupUI InvasionWait;
    public HRInvasionQueueUI InvasionQueueUI;
    public BasePopupUI InvasionInfo;
    public HRNewsTicker NewsTicker;

    // Gets the player character.
    public static HeroPlayerCharacter GetFirstHRPlayerCharacter()
    {
        BaseScripts.BasePawn FirstPawn = BaseGameInstance.Get.GetFirstPawn();
        if (FirstPawn)
        {
            return (HeroPlayerCharacter)FirstPawn;
        }
        else
        {
            return null;
        }
    }

    public GameZoneManager ZoneManager => this.gameZoneManager;

    public GameOutpostManager OutpostManager => this.outpostManager;

    public HRDayManager DayManager => this.dayManager;

    public HRSleepManager SleepManager => this.sleepManager;

    public BaseTimeMusicManager TimeMusicManager => timeMusicManager;

    public BaseTimeAmbientManager TimeAmbientManager => timeAmbientManager;

    public BaseMusicManager AmbientManager => ambientManager;

    public HREnemySpawnerManager EnemySpawnerManager => enemySpawnerManager;

    public HREndScreenManager EndScreenManager => endScreenManager;

    public HRDialogueSystem DialogueSystem;

    public HRQuestManager QuestManager;
    public HRRaidSystem RaidManager => raidManager;
    public HRBankSystem BankSystem => bankSystem;

    public HRGlobalPlayerSkillTreeInstance GlobalSkillTreeInstance;

    public BaseTerrainDetectorData DefaultTerrainDetectorDB;

    [HideInInspector]
    public TerrainDetectorManager TerrainDetectorManager;

    public HRShopBuildingVisualizer ShopBuildingVisualizer;

    public HRMinigame BarterMinigame;

    // Temp class. May be removed/reworked.
    public HRInvasionManager InvasionManager;

    public JBooth.MicroSplat.TraxManager TraxManager;

    public BaseAtmosphericFogManager AtmosphericFogManager;

    public HRWorldMapManager WorldMapManager;

    public HREncounterManager EncounterManager;

    //Yiming: used for scene object GPUI regsitration
    const int GPU_INSTANCER_THRESHOLD = 100;
    public HRItemDatabase MasterItemDB;
    public HRStaticPrefabDatabase MasterStaticPrefabDB;
    public GPUInstancerPrefabManager GPUIPrefabManager;
    public Dictionary<string, GPUInstancerPrefabPrototype> DefinedGPUIPrototypes = new Dictionary<string, GPUInstancer.GPUInstancerPrefabPrototype>();
    public Dictionary<string, HashSet<GPUInstancerPrefab>> ActiveGPUInstancesList = new Dictionary<string, HashSet<GPUInstancerPrefab>>();
    public Dictionary<string, HashSet<GPUInstancerPrefab>> GPUInstanceGroups = new Dictionary<string, HashSet<GPUInstancerPrefab>>();

    [System.NonSerialized]
    public List<BaseWeapon> NavMeshObstaclesEnablingList = new List<BaseWeapon>();
    [System.NonSerialized]
    public List<BehaviorTree> TickBehaviorTrees = new List<BehaviorTree>();
    [System.NonSerialized]
    public List<BaseBehaviorTree> DelayedDisablingBT = new List<BaseBehaviorTree>();

    public HRWeatherManager WeatherManager;

    public void RefreshAllIDComponents()
    {
        foreach (BaseIDComponent IDComponent in Object.FindObjectsOfType<BaseIDComponent>())
        {
            IDComponent.ForceGenerateID();
        }
    }

    public void EnableWallTileShowTriggers(bool bEnabled)
    {
        for (int i = 0; i < WallTileShowTriggers.Count; ++i)
        {
            if (WallTileShowTriggers[i])
            {
                WallTileShowTriggers[i].HandleOnPause(null, !bEnabled);
                if (bEnabled)
                {
                    WallTileShowTriggers[i].SetWallTilesVisibility(true);
                }
            }
        }
    }

    int PreloadedSceneCount = 0;
    int SceneLoaded = 0;
    bool bHookedSceneLoad = false;

    override public void Awake()
    {
        base.Awake();

        // Clear first gear games scene queues because we could trigger some that don't get cleaned up due to this.
        FirstGearGames.FlexSceneManager.FlexSceneManager.ClearProcessSceneQueue();

        if (bSaveLevelData && !bHookedSceneLoad)
        {
            PreloadedSceneCount = SceneManager.sceneCount;
            SceneLoaded = 0;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            bHookedSceneLoad = true;
        }

        if (MasterInitializationDatabase)
        {
            MasterInitializationDatabase.HandleGameManagerAwake(gameObject.scene.name, LevelExtraDatabases);
        }

        if (SpatialGridManager.Instance)
        {
            SpatialGridManager.Instance.UpdatePreLoadedScenes();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (MasterInitializationDatabase)
        {
            MasterInitializationDatabase.HandleGameManagerOnStartServer(gameObject.scene.name);
        }
    }

    void HandleSceneLoaded(Scene LoadedScene, LoadSceneMode LoadedSceneMode)
    {
        if (!Mirror.NetworkServer.active || HRNetworkManager.bIsServer == true)
        {
            SceneLoaded++;

            if (SceneLoaded >= PreloadedSceneCount)
            {
                HRGameInstance GameInstance = ((HRGameInstance)BaseGameInstance.Get);

                GameInstance.SaveSystem.bSavePersistentPlayerData = bUsePersistentPlayerInventory;
                GameInstance.SaveSystem.bSaveLevelData = bSaveLevelData;

                SceneManager.sceneLoaded -= HandleSceneLoaded;
                GameInstance.SaveSystem.HandleNewLevelLoaded();
            }
        }
    }

    public override void LateUpdate()
    {
        base.LateUpdate();

        int Count = NavMeshObstaclesEnablingList.Count;

        if (Count > 0)
        {
            for (int i = 0; i < Count; ++i)
            {
                NavMeshObstaclesEnablingList[i].NavObstaclesEnabled();
            }

            NavMeshObstaclesEnablingList.Clear();
        }

        Count = TickBehaviorTrees.Count;

        if (Count > 0)
        {
            for (int i = 0; i < Count; ++i)
            {
                BehaviorManager.instance.Tick(TickBehaviorTrees[i]);
            }

            TickBehaviorTrees.Clear();
        }

        Count = DelayedDisablingBT.Count;

        if (Count > 0)
        {
            for (int i = 0; i < Count; ++i)
            {
                DelayedDisablingBT[i].DelayedDisable();
            }

            DelayedDisablingBT.Clear();
        }
    }
    public override void OnDestroy()
    {
        if (TerrainDetectorManager != null)
        {
            TerrainDetectorManager.Shutdown();
            TerrainDetectorManager = null;
        }

        // Yiming: Flush unfinisehd load navmesh operations
        if(AstarPath.active)
        {
            AstarPath.active.FlushWorkItems();
        }
    }

    public void InitializeGameManager()
    {
        TerrainDetectorManager = new TerrainDetectorManager();
        TerrainDetectorManager.FallbackTerrainDetectorDB = DefaultTerrainDetectorDB;

        timeLoaded = Time.timeSinceLevelLoad;
        BaseWorldTravelPoint SpawnTravelPoint = null;

        // Set this so that the grass doesn't glow.
        UnityEngine.RenderSettings.reflectionIntensity = 0.0f;

        BaseGameInstance GameInstance = BaseGameInstance.Get;
        if (GameInstance)
        {
            GameInstance.InitializeSpawns();
        }
        else
        {
            Debug.Log("Somehow the game instance does not exist. Maybe you forgot to drag it in?");
        }

        GameManagerPreStartedDelegate?.Invoke(this);
        BaseGameInstance.Get.GameManagerPreStartedDelegate?.Invoke(this);

        if (BaseIDManager.Get == null)
        {
            BaseIDManager.Get = IDManager;
        }

        // Check to see if all databases are loaded. If so, we can call the start events.
        if (!LevelExtraDatabases || LevelExtraDatabases.bFinishedAddingDatabases || LevelExtraDatabases.databases.Length == 0)
        {
            GameStarted();
        }
        else
        {
            LevelExtraDatabases.ExtraDatabasesLoadedDelegate += HandleExtraDialogueDatabasesLoaded;
        }

        StartDayManager();

        if (SpawnTravelPoint)
        {
            SpawnTravelPoint.ArrivedToTravelPoint();
        }

        if (BaseLightProbeUpdater.Get)
        {
            BaseLightProbeUpdater.Get.ApplyHarmonics();
        }

        if (BarterMinigame)
        {
            (HRGameInstance.Get as HRGameInstance).BarteringSystem.BarterGame = BarterMinigame;
        }
    }

    public void InitializeInstancerPrefab(GPUInstancerPrefab InstancerPrefab, GameObject Prefab)
    {
        if (GPUIPrefabManager && InstancerPrefab && InstancerPrefab.state == PrefabInstancingState.None)
        {
            string PrefabName = Prefab.name;

            GPUInstancerPrefabPrototype NewPrototype;

            HashSet<GPUInstancerPrefab> InstanceGroup;
            if (!GPUInstanceGroups.TryGetValue(PrefabName, out InstanceGroup))
            {
                InstanceGroup = new HashSet<GPUInstancerPrefab>();
                GPUInstanceGroups.Add(PrefabName, InstanceGroup);
            }

            InstanceGroup.Add(InstancerPrefab);

            if (InstanceGroup.Count < GPU_INSTANCER_THRESHOLD) return;

            if (!DefinedGPUIPrototypes.TryGetValue(PrefabName, out NewPrototype))
            {
                NewPrototype = GPUIPrefabManager.DefineGameObjectAsPrefabPrototypeAtRuntime(Prefab, false);
                DefinedGPUIPrototypes[PrefabName] = NewPrototype;
            }

            HashSet<GPUInstancerPrefab> ActiveInstances;
            if (!ActiveGPUInstancesList.TryGetValue(PrefabName, out ActiveInstances))
            {
                ActiveInstances = new HashSet<GPUInstancerPrefab>();
                ActiveGPUInstancesList.Add(PrefabName, ActiveInstances);
            }

            ActiveInstances.UnionWith(InstanceGroup);

            //GPUIPrefabManager.AddInstancesToPrefabPrototypeAtRuntime(NewPrototype, InstanceGroup);
            InstanceGroup.Clear();
        }
    }

    public void InitializeInstancerPrefab(GPUInstancerPrefab InstancerPrefab, int ItemID)
    {
        if (GPUIPrefabManager && ItemID >= 0 && ItemID < MasterItemDB.ItemArray.Length && MasterItemDB.ItemArray[ItemID].ItemPrefab)
        {
            InitializeInstancerPrefab(InstancerPrefab, MasterItemDB.ItemArray[ItemID].ItemPrefab);
        }
    }

    public void ToggleGPUInstancingState(GPUInstancerPrefab InstancerPrefab, int ItemID, bool bIsOn)
    {
        if (GPUIPrefabManager && ItemID >= 0 && ItemID < MasterItemDB.ItemArray.Length && MasterItemDB.ItemArray[ItemID].ItemPrefab)
        {
            ToggleGPUInstancingState(InstancerPrefab, MasterItemDB.ItemArray[ItemID].ItemPrefab, bIsOn);
        }
    }

    public void ToggleGPUInstancingState(GPUInstancerPrefab InstancerPrefab, GameObject Prefab, bool bIsOn)
    {
        if (!GPUIPrefabManager || !InstancerPrefab) return;

        if (!InstancerPrefab.prefabPrototype)
        {
            if (bIsOn)
            {
                InitializeInstancerPrefab(InstancerPrefab, Prefab);
            }
            else
            {
                return;
            }
        }

        if (InstancerPrefab.state == GPUInstancer.PrefabInstancingState.None) return;

        string PrefabName = Prefab.name;

        if (bIsOn && InstancerPrefab.state == GPUInstancer.PrefabInstancingState.Disabled)
        {
            HashSet<GPUInstancerPrefab> ActiveInstances;
            if (!ActiveGPUInstancesList.TryGetValue(PrefabName, out ActiveInstances))
            {
                ActiveInstances = new HashSet<GPUInstancerPrefab>();
                ActiveGPUInstancesList.Add(PrefabName, ActiveInstances);
            }
            ActiveInstances.Add(InstancerPrefab);

            GPUIPrefabManager.EnableInstancingForInstance(InstancerPrefab);
        }
        else if(!bIsOn && InstancerPrefab.state == GPUInstancer.PrefabInstancingState.Instanced)
        {
            if (ActiveGPUInstancesList.ContainsKey(PrefabName))
            {
                ActiveGPUInstancesList[PrefabName].Remove(InstancerPrefab);

                if (ActiveGPUInstancesList[PrefabName].Count <= 0)
                {
                    Dictionary<GPUInstancer.GPUInstancerPrototype, List<GPUInstancer.GPUInstancerPrefab>> RegisteredPrefabs = GPUIPrefabManager.GetRegisteredPrefabsRuntimeData();
                    if (RegisteredPrefabs.TryGetValue(InstancerPrefab.prefabPrototype, out List<GPUInstancer.GPUInstancerPrefab> RegisteredPrefab))
                    {
                        GPUIPrefabManager.RemovePrefabInstances(InstancerPrefab.prefabPrototype, RegisteredPrefab);
                        return;
                    }
                }
            }

            GPUIPrefabManager.DisableIntancingForInstance(InstancerPrefab);
        }
    }

    public void RemoveGPUInstance(GPUInstancer.GPUInstancerPrefab InstancerPrefab, int ItemID)
    {
        if (GPUIPrefabManager && ItemID >= 0 && ItemID < MasterItemDB.ItemArray.Length && MasterItemDB.ItemArray[ItemID].ItemPrefab)
        {
            RemoveGPUInstance(InstancerPrefab, MasterItemDB.ItemArray[ItemID].ItemPrefab);
        }
    }

    public void RemoveGPUInstance(GPUInstancer.GPUInstancerPrefab InstancerPrefab, GameObject Prefab)
    {
        string PrefabName = Prefab.name;

        if (GPUIPrefabManager && GPUInstanceGroups.TryGetValue(PrefabName, out HashSet<GPUInstancerPrefab> GroupInstances))
        {
            GroupInstances.Remove(InstancerPrefab);
        }

        if (ActiveGPUInstancesList.TryGetValue(PrefabName, out HashSet<GPUInstancerPrefab> ActiveInstances))
        {
            if(ActiveInstances.Contains(InstancerPrefab))
            {
                ActiveInstances.Remove(InstancerPrefab);
                GPUIPrefabManager.RemovePrefabInstance(InstancerPrefab);
            }
        }
    }

    override public void Start()
    {
        //if (!BoltNetwork.IsRunning)
        //{
            InitializeGameManager();
        //}
    }

    public static void TickBTNextFrame(BehaviorTree BT)
    {
        if(Get && Get is HRGameManager GameManager)
        {
            GameManager.TickBehaviorTrees.Add(BT);
        }
    }

    public void StartDayManager()
    {
        DayManager.HandleNewLevelLoaded();
        DayManager.SetTimePassEnabled(DayManager.bTimePassEnabled);
    }

    public override void HandleLevelUnload()
    {
        base.HandleLevelUnload();

        if (PersistentPlayerShop)
        {
            PersistentPlayerShop.StopShop();
        }

        DayManager.SetTimePassEnabled(false);

        // Unload teh current dialogue as well.
        PixelCrushers.DialogueSystem.DialogueManager.StopConversation();
        
        HRGameInstance GameInstance = BaseGameInstance.Get as HRGameInstance;
        //GameInstance.SaveSystem.bUnloadingLevel = true;

        if (bSaveInstanceOnLeaveTEMP && (bUsePersistentPlayerInventory || bSaveLevelData || bSaveDialogueData))
        {
            GameInstance.SaveSystem.HandleLevelUnload();
        }

        LevelExtraDatabases.RemoveDatabases(true);
    }

    void HandleExtraDialogueDatabasesLoaded(PixelCrushers.DialogueSystem.ExtraDatabases InDatabase)
    {
        GameStarted();
    }

    public void GameStarted()
    {
        HRGameInstance GameInstance = (HRGameInstance)BaseGameInstance.Get;
        if (GameInstance.DistrictDB)
        {
            int DistrictIndex = GameInstance.DistrictDB.GetDistrictInfoIndexByScene(SceneManager.GetActiveScene().path);
            if (DistrictIndex != -1)
            {
                GameInstance.CurrentDistrictIndex = DistrictIndex;
            }
        }
        bGameManagerStarted = true;
        GameManagerStartedDelegate?.Invoke(this);
        BaseGameInstance.Get.GameManagerStartedDelegate?.Invoke(this);
        GameStartEvents.Invoke();

        if(bHandleFades && !BaseWorldStreamManager.Get)
            BaseGameInstance.Get.FadeManager.Fade(true, 7.0f, Color.black, true);

        HRDialogueSystemSave.Get.BindToLevelExtraDatabases(HRSaveSystem.Get.CurrentFileInstance);
    }

    // TEMP STUFF BAD
    int TotalActiveWallTriggers = 0;
    public void AddWallTriggerCount(int InCount)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        int OldCount = TotalActiveWallTriggers;
        TotalActiveWallTriggers += InCount;

        if(OldCount == 0 || (OldCount != 0 && TotalActiveWallTriggers == 0))
        {
            HeroPlayerCharacter PC = GetFirstHRPlayerCharacter();
            if (PC)
            {
                PC.PlayerCamera.bUseCinematicRotation = TotalActiveWallTriggers == 0 ? true : false;
            }
        }
    }

    public void OnBuildingPiecePlacedLocal(BaseWeapon Target)
    {
        if (Target)
        {
            OnBuildingPlacedLocal?.Invoke(Target, true);
        }
    }

    [Mirror.Server]
    public void OnBuildingPiecePlaced(BaseWeapon Target)
    {
        OnBuildingPiecePlaced_Server(Target);
    }

    private void OnBuildingPiecePlaced_Implementation(BaseWeapon Target)
    {
        OnBuildingPlaced?.Invoke(Target, true);
    }

    [Mirror.Server]
    private void OnBuildingPiecePlaced_Server(BaseWeapon Target)
    {
        OnBuildingPiecePlaced_Implementation(Target);
        OnBuildingPiecePlaced_ClientRpc(Target);
    }

    [Mirror.ClientRpc]
    public void OnBuildingPiecePlaced_ClientRpc(BaseWeapon Target)
    {
        if (HRNetworkManager.IsHost()) return;

        OnBuildingPiecePlaced_Implementation(Target);
    }

    public void OnBuildingPieceDestroyed(BaseWeapon Target)
    {
        if (HRNetworkManager.IsHost())
        {
            OnBuildingPieceDestroyed_Implementation(Target);
        }
        else
        {
            OnBuildingPieceDestroyed_Command(Target);
        }
    }


    private void OnBuildingPieceDestroyed_Implementation(BaseWeapon Target)
    {
        // Execute event
        OnBuildingPlaced?.Invoke(Target, false);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void OnBuildingPieceDestroyed_Command(BaseWeapon Target)
    {
        OnBuildingPieceDestroyed_Implementation(Target);
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void SaveClientData_Command(string Key, byte Data)
    {
         //TODO
    }
    [Mirror.Command(ignoreAuthority = true)]
    public void LoadClientData_Command(string Key, byte Data)
    {
        //TODO
    }

    public void UnlockAchievementForAllClients(string achievementID, string data)
    {
        if (HRNetworkManager.IsHost())
        {
            UnlockAchievementForAllClients_Implementation(achievementID, data);
            UnlockAchievementForAllClients_ClientRpc(achievementID, data);
        }
        else
        {
            UnlockAchievementForAllClients_Command(achievementID, data);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void UnlockAchievementForAllClients_Command(string achievementID, string data)
    {
        UnlockAchievementForAllClients_Implementation(achievementID, data);
        UnlockAchievementForAllClients_ClientRpc(achievementID, data);
    }

    [Mirror.ClientRpc]
    private void UnlockAchievementForAllClients_ClientRpc(string achievementID, string data)
    {
        if (!HRNetworkManager.IsHost())
        {
            UnlockAchievementForAllClients_Implementation(achievementID, data);
        }
    }

    private void UnlockAchievementForAllClients_Implementation(string achievementID, string data)
    {
        HRAchievementManager.Instance?.UnlockAchievement(achievementID);
    }

    private void OnApplicationQuit()
    {
        if (HRNetworkManager.Get)
        {
            // Stops the host if the player quits the application.
            if (Mirror.NetworkServer.active && Mirror.NetworkClient.isConnected)
            {
                HRNetworkManager.Get.StopHost();
            }
            // Otherwise stops the client if the player quits the application.
            else if (Mirror.NetworkClient.isConnected)
            {
                HRNetworkManager.Get.StopClient();
            }
        }
    }
}
