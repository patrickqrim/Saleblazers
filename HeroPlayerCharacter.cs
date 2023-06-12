using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using BaseScripts;
using DG.Tweening;
using Animancer;
using Sirenix.OdinInspector;
using System.Linq;
using Mirror;
using JBooth.MicroSplat;
using PixelCrushers.DialogueSystem;

[System.Serializable]
public class HeroPlayerCharacterFallDamageData
{
    [SerializeField]
    private float minHeightFallDamage = 5.0f;
    [SerializeField]
    private float healthLostPerUnitY = 0.5f;

    public float MinHeightFallDamage
        => this.minHeightFallDamage;

    public float HealthLostPerUnitY => this.healthLostPerUnitY;
}

public struct HighlightEffectSettings
{
    public bool bEnabled;
    public float width;
    public Color color;
}

public class HeroPlayerCharacter : BasePlayerCharacter, IChannelTarget, IPlayerCrimeDataHolder, IAttributeManagerHolder, IConveyorAffector, IHRRandomSpawnableHandler
{
    // temp
    public TMPro.TMP_Text PlayerNameText;
    public TMPro.TMP_Text CharacterNameText;

    public delegate void HeroPlayerStateChangeSignature(HeroPlayerCharacter InPlayerCharacter, InteractState PrevState, InteractState NewState);
    public HeroPlayerStateChangeSignature OnStateChangeDelegate;

    public delegate void HeroPlayerHitstunSignature(HeroPlayerCharacter InPlayerCharacter, float InHitstunTime);
    public HeroPlayerHitstunSignature OnHitstunDelegate;

    public delegate void HeroPlayerDestroyedSignature(HeroPlayerCharacter InPlayerCharacter);
    public HeroPlayerDestroyedSignature OnCharacterDestroyedDelegate;
    public HeroPlayerDestroyedSignature OnClientStartDelegate;
    public HeroPlayerDestroyedSignature OnPlayerReadyDelegate;

    public delegate void HeroPlayerCharacterSitSignature(HeroPlayerCharacter InPlayerCharacter, HRSeatComponent InSeat, bool bSitting);
    public HeroPlayerCharacterSitSignature OnSitDelegate;

    public delegate void OnFishingBobberCastSignature();
    public OnFishingBobberCastSignature OnFishingBobberCastDelegate;

    public delegate void OnFishingMinigameSignature();
    public OnFishingMinigameSignature OnFishingMinigameStartDelegate;

    public delegate void OnFishingItemCatchSignature(BaseWeapon Weapon);
    public OnFishingItemCatchSignature OnFishingItemCatchDelegate;

    public delegate void HeroPlayerCharacterDamagedCharacterSignature(HeroPlayerCharacter InCharacter, HeroPlayerCharacter TargetCharacter, float Damage);
    public HeroPlayerCharacterDamagedCharacterSignature OnDamagedCharacter;
    public HeroPlayerCharacterDamagedCharacterSignature OnKilledCharacter;

    public delegate void HeroPlayerCharacterDamagedWeaponSignature(HeroPlayerCharacter InCharacter, BaseWeapon TargetWeapon, float Damage);
    public HeroPlayerCharacterDamagedWeaponSignature OnDamagedWeapon;
    public HeroPlayerCharacterDamagedWeaponSignature OnDestroyedWeapon;

    public IPlayerCrimeDataHolder.HRPlayerBeingFinedSignature OnPlayerBeingFinedDelegate;

    public delegate void HeroPlayerCharacterChannelSignature(AHeroChannel channel, bool bStarted);
    public HeroPlayerCharacterChannelSignature OnChannelDelegate;

    public BaseInteractionContextData RotateItemContextInfo = BaseInteractionContextData.Default;
    public BaseInteractionContextData ThrowItemContextInfo = BaseInteractionContextData.Default;
    public BaseInteractionContextData BlockItemContextInfo = BaseInteractionContextData.Default;

    public bool bHasHealthInsurance = true;
    public GameObject HealthInsuranceFX;
    public GameObject NoHealthInsuranceFX;
    public AudioClip HealthInsuranceAudioClip;

    public float HealthInsuranceCostPercent = 0.2f;
    [HideInInspector]
    [Mirror.SyncVar]
    public int LastHealthInsuranceCost = 0;

    public GameObject SubtleTeleportFXPrefab;

    // None = can't click on anything
    // Free = can click on any object
    // Build = in build mode can build/modify tiles
    // Dying - Is waiting for a revive from another player/being revived.
    // Dead - The player is dead, failed to be revived by another player.
    public enum InteractState { None, Free, Paused, Stunned, Sleeping, Dying, Dead, Talking };

    public enum Faction { None, Player, Law, Enemy, Cowboy, Animal, PreyAnimal, PredatorAnimal, NightEnemy, Ronin, Customer, Shopkeeper };

    public HRInteractionManager InteractionManager;
    public BasePlayerInventoryManager InventoryManager;
    public HRWeaponManager WeaponManager;
    public BaseEquipmentComponent EquipmentComponent;
    public BaseMusicManager AudioManager;
    public BaseEquipmentSFXHandler EquipmentSFXHandler;

    public bool bCanSwitchWeapons { get; set; }
    public bool bInitialLoad { get; private set; }

    [SerializeField]
    private HRWallet wallet;

    [ReadOnly]
    [Mirror.SyncVar(hook = nameof(HandleControllerAssigned_Hook))]
    public HRPlayerController HRPC;

    [HideInInspector] public HRPlayerController HRPCBeforeSpectate;

    [HideInInspector]
    public InteractState _localInteractState = InteractState.Free;
    private InteractState _previousInteractState = InteractState.Free;

    // The current interact state
    [Mirror.SyncVar(hook = nameof(HandleInteractStateChanged))]
    private InteractState _networkedInteractState = InteractState.Free;

    public BaseItemPlacingManager ItemPlacingManager;

    public BasePauseReceiver PauseReceiver;
    private bool bIsPaused = false;

    private BaseMusicLayer PrePauseMusicLayer;
    private float PrePauseVolume;
    private float PrePauseZoomAmount;

    public BaseHP HP;
    [HideInInspector] public bool HasHP;
    public BaseArmorManager Armor;
    public BaseDamageReceiver DamageReceiver;
    public BaseDestroyHPListener destroyListener;
    public BaseStatusEffectsManager statusEffectsManager;

    public HRHealthBarComponent StaminaHealthBarComponent;

    public BaseRagdollManager Ragdoll;

    public bool bCanBeBowled = true;
    bool bOriginalCanBeBowled;

    public bool bUseRagdollCollisions = false;

    public BaseMovementDash DashComponent;
    public HRStaminaComponent StaminaComponent;
    public HRXPComponent XPComponent;
    public BaseHP HungerHP;
    public BaseHP ThirstHP;

    public BaseFactionDataAsset OriginalFactionDataAsset;
    [System.NonSerialized]
    public BaseFactionDataAsset FactionDataAsset;

    public Faction CurrentFaction => FactionDataAsset ? FactionDataAsset.OwnerFaction : Faction.None;

    public HRNameComponent NameComponent;

    public HRFearComponent FearComponent;
    public HRWitnessComponent WitnessComponent;

    public HRCharacterAnimScript AnimScript;

    public HRLockPickMinigameSystem LockPickingMinigameSystem;

    public bool bCanBeHitstunned = true;

    public HRAttributeManager AttributeManager;

    public HRCraftingComponent PlayerCraftingComponent;

    public GameObject TalkingSprite;
    public GameObject RadioSprite;

    public float SprintStaminaRate = 5.0f;
    public float DashStaminaCost = 10.0f;
    public float DashStaminaRate = 2.0f;
    public float JumpStaminaCost = 5.0f;
    public float SwimStaminaRate = 2.0f;
    public float GlideStaminaRate = 5.0f;

    public float SprintXPRate = 5.0f;
    public AnimationCurve MovementSpeedAtLevelCurve;
    public float DashXPRate = 100f;
    public float JumpXPRate = 100f;

    public bool bAllowGiveUp = true;
    public BasePCVVolume OwningPCVVolume;

    public bool bPlayCombatIdle = true;
    public bool bPlayCharacterVoice = true;

    [Header("Block Variables")]
    public float maxBlockCooldown = 1f;
    public float lastTimeBlocked = -1000f;
    public bool bIsBlockPressed = false;


    [Header("EXP Variables")]
    public XPSoak xpSoak;
    public float XPToGiveWhenKilled = 0.5f;

    public float modifierMeleeExp = .25f;
    public float modifierGunExp = .5f;
    public float modifierThrownExp = .25f;
    public float modifierWoodcuttingExp = .2f;
    public float modifierMiningExp = .2f;
    public float modifierFishingExp = .1f;
    public float modifierLockpickingExp = .1f;

    [SerializeField] private float XPFromFishing = 10f;
    [SerializeField] private float XPFromFarming = 10f;
    [SerializeField] private float XPFromLockpicking = 10f;
    [SerializeField] private float XPFromCooking = 100f;

    public HRShopkeeper ShopkeeperRef;
    private bool bInventoryBound = false;

    [Header("Revenge"), Tooltip("Chance it will be added to revengng target's queue")]
    [Range(0f, 1f)]
    // Revinging Enemy Properties
    public float RevengeTargetChance = 0.3f;
    [Range(0f, 1f), Tooltip("If chosen to revenge by Revenge Chance, how likely will it revenge among all other revenging enemies")]
    // Revinging Enemy Properties
    public float RevengeWeight = 1f;
    [Min(1)]
    public int MinRevengeAmount = 1;
    public int MaxRevengeAmount = 3;
    public float MinRevengeTimeAfter = 60f;
    public float MaxRevengeTimeAfter = 120f;
    public string DefaultRevengerName = "my brothers";

    // Revenging Target Properties
    public HREncounterSpawner RevengeEncounterSpawner;
    BaseProbabilityTable<HREncounterDynamicEnemyData> RevengingEnemies;
    HREncounterDynamicEnemyData CurrentRevengingEnemy;
    bool bIsRevengeTarget;
    float CurrentRevengingTimer;
    float CurrentRevengeTime = -1;


    [HideInInspector]
    public bool onJumpPad;

    [Header("Damage Modifiers")]
    // Tracks how much damage should be modified
    // Used for AI in prefab
    [Mirror.SyncVar, HideInInspector, System.NonSerialized]
    public float levelDamageModifier = 1.0f;
    public float damageModifier = 1.0f;

    // Used for AI in prefab
    [Mirror.SyncVar, HideInInspector, System.NonSerialized]
    public float levelDefenseModifier = 1.0f;
    public float defenseModifier = 1.0f;

    public bool IsTryingToBlock()
    {
        if (bIsPlayer)
        {
            return bIsBlockPressed;
        }
        else
        {
            if (WeaponManager && WeaponManager.CurrentWeapon && WeaponManager.CurrentWeapon.WeaponBlockerComponent)
            {
                return WeaponManager.CurrentWeapon.WeaponBlockerComponent.IsBlocking(false, true);
            }

            return false;
        }
    }

    [Header("Kill List Info")]
    [SerializeField]
    public int characterDatabaseIndex = -1;
    [SerializeField]
    private List<int> killedCharacterIndices = new List<int>();

    [Header("Drowning Variables")]

    [SerializeField, Min(0.0f)]
    private float healthLostPerSecondDrowning = 1.0f;
    [SerializeField, Min(1.0f)]
    private float drowningNotificationTimeDifference = 1.5f;

    [Header("Hunger and Thirst Variables")]
    [SerializeField, Min(0.0f)]
    private float healthLostPerTickHungerAndThirst = 10.0f;
    [SerializeField, Min(0.0f)]
    private float thirstAndHungerTickRate = 3.0f;

    [HideInInspector]
    public HRSeatComponent CurrentSeat;
    public bool bCanSit = true;

    public BaseFollowerManager FollowerManager;

    public BaseBehaviorManager BehaviorManager;

    public BaseMovementSpeedFX MovementFX;

    [SerializeField]
    private HROverheadPlayerInformation PlayerInformationPrefab;
    private HROverheadPlayerInformation PlayerInformationInstance;
    [SerializeField]
    private HRDyingUI dyingUIPrefab;
    [SerializeField]
    private GameObject RevivePrompt;

    [SerializeField]
    private HeroPlayerCharacterFallDamageData fallDamageData;
    [SerializeField]
    private HeroReviveData heroReviveData;
    [SerializeField]
    private GameZoneListener gameZoneListener;
    [SerializeField]
    public HRSkillTreeInstance playerSkillTree;
    [SerializeField]
    public HRSkillTreeInstance playerMasteryTree;
    [SerializeField]
    public BaseCustomizationSystem customizationSystem;

    [Mirror.SyncVar(hook = nameof(ReviveInteractableEnabled_Hook))]
    private bool reviveInteractableEnabled = false;

    // The holster implementation for the hero.
    private HeroHolster heroHolster;
    // Listener for a character wave.
    private HeroCharacterWaveListener waveListener;
    // The hero revive implementation.
    private HeroDeathReviveDelay heroDeathReviveDelayController;
    // The hero revive controller -> used to revive other players.
    private HeroReviveController heroReviveController;
    // The Current Dying UI that's being displayed.
    private HRDyingUI currentDyingUI = null;
    // The bomb channel controller.
    private HeroBombChannel _bombChannel;

    private HeroAIOutpostTarget _outpostTargetAI;
    // Handles the current player's respawn.
    private HeroRespawn _respawnController = null;

    public GameObject PlayerRespawnFXPrefab;

    private HRAttribute _currentWetAttribute = null;
    private bool _movingOnConveyor = false;

    // The localized username variable.
    private string currentUserName = "AIRSTRAFER";
    public string PlayerName => currentUserName;
    private Mirror.SyncDictionary<GameObject, HRPlayerCrimeSystemData> _crimeSystemData
        = new Mirror.SyncDictionary<GameObject, HRPlayerCrimeSystemData>();

    private IPlayerCrimeDataHolder.HRPlayerCrimeSystemDataChangedSignature _dataChangedEventDelegate;

    private float crimeMusicCooldown;

    private bool bIsBeingFined;

    [Header("Crafting Progression Parameters")]
    public AnimationCurve ItemRarityProgression;
    public int MaxLevel = 10;

    [Header("Cosmetics")]
    public bool bCanWave = true;
    public bool bFaceTargetOnWave = true;
    [System.NonSerialized]
    public bool bIsWaving;
    [Header("Hit Reactions Animations")]
    public bool bHasUniqueHitReactions;
    [ShowIf("bHasUniqueHitReactions")]
    public ClipState.Transition defaultStaggerHitAnim;

    [Header("Throwing")]
    [Range(0.01f, 500f)]
    public float ThrowStrength = 5.0f;
    [Range(0, 10)]
    public float ThrowStartTime = 0.65f;
    [Range(0, 10)]
    public float ThrowCooldown = 0.65f;
    public float ThrowStartupTime = .6f;
    private IEnumerator ThrowRoutine;

    public BaseDialogueStarter InteractDialoguePrefab;
    public BaseDialogueStarter InteractDialogueInstance;
    [Header("Barks")]
    [SerializeField] private HRBarkData onDeathBark;

    [SerializeField]
    private bool bLockAngleOnStart = false;
    private bool bLightControlMode = true;

    private int OriginalLayer;

    public GameObject BodyPoofFX;

    public bool bShouldUseGrounder = false;

    private bool bWaitingForGround;
    private float waitForGroundTimer;

    [SerializeField]
    private GameObject sleepingFXPrefab;
    private GameObject sleepingFXInstance;

    [HideInInspector]
    public bool INSTANTCOMPUTATIONS = false;

    public BaseMotionSmoother MeshSmootherRef;

    public BaseJiggleScript MeshJiggleScript;

    public WeatherListener weatherListener;



    bool bServerInitialized = false;

    #region properties

    public InteractState CurrentInteractState => _localInteractState;
    public InteractState PreviousInteractState => _previousInteractState;

    public HRWallet Wallet => wallet;

    public bool IsControlledByPlayer => PlayerController && IsPossessedByPlayer;
    private bool bLocalPlayer = false;

    public HRDyingUI DyingUI => this.currentDyingUI;

    public HRAttributeManager AttributeManagerProperty => AttributeManager;

    public URPWater.URPWaterDynamicEffects DynamicEffects;

    public string UserName
    {
        get
        {
            if (this.HRPC)
            {
                return HRPC.PlayerUsername;
            }
            else if (!string.IsNullOrEmpty(currentUserName) && this.transform.CompareTag("Player"))
            {
                return currentUserName;
            }
            else if (NameComponent && NameComponent.HasName)
            {
                return NameComponent.Name;
            }
            else if (Interactable)
            {
                return Interactable.InteractionName;
            }
            else
            {
                return this.name;
            }
        }
    }

    public bool MovingOnConveyor => _movingOnConveyor;

    HRBaseAI CachedAI;
    bool bTryCachedAI;

    public HRBaseAI AIRef
    {
        get
        {
            if (!bTryCachedAI)
            {
                CachedAI = GetComponent<HRBaseAI>();
                bTryCachedAI = true;
            }

            return CachedAI;
        }
    }
    public HRCustomerAI CustomerAI
    {
        get
        {
            if (!bTryCachedAI)
            {
                CachedAI = GetComponent<HRBaseAI>();
                bTryCachedAI = true;
            }

            return CachedAI as HRCustomerAI;
        }
    }

    public HRHostileAI HostileAI
    {
        get
        {
            if (!bTryCachedAI)
            {
                CachedAI = GetComponent<HRBaseAI>();
                bTryCachedAI = true;
            }

            return CachedAI as HRHostileAI;
        }
    }

    public Pathfinding.RichAI _OwningRichAI;

    public HRSkillSystem SkillSystem
    {
        get
        {
            HRPlayerController controller = PlayerController as HRPlayerController;
            return controller?.SkillSystem;
        }
    }

    public HeroCharacterWaveListener WaveAnimationListener
    {
        get
        {
            this.waveListener = this.waveListener ?? new HeroCharacterWaveListener(this);
            return this.waveListener;
        }
    }

    public HeroDeathReviveDelay HeroDeathReviveDelayController
    {
        get
        {
            this.heroDeathReviveDelayController = this.heroDeathReviveDelayController ?? new HeroDeathReviveDelay(this, this.heroReviveData);
            return this.heroDeathReviveDelayController;
        }
    }

    public HeroReviveController HeroReviveController
    {
        get
        {
            this.heroReviveController = this.heroReviveController ?? new HeroReviveController(this, HeroChannelInputType.TYPE_HOLD);
            return this.heroReviveController;
        }
    }

    public HeroHolster HeroHolsterController
    {
        get
        {
            this.heroHolster = this.heroHolster ?? new HeroHolster(this);
            return this.heroHolster;
        }
    }

    public HRSleepManager SleepManager
    {
        get
        {
            return ((HRGameInstance)BaseGameInstance.Get).GetSleepManager();
        }
    }

    public HeroRespawn RespawnController
    {
        get
        {
            _respawnController = _respawnController ?? new HeroRespawn(this);
            return _respawnController;
        }
    }

    public Vector3 RespawnPosition
    {
        get
        {
            if (RespawnController.bUseTemporaryRespawnPosition)
            {
                return RespawnController.TemporaryRespawnPosition;
            }
            if (RespawnController.HasReachedACheckpoint)
            {
                return RespawnController.LatestCheckpoint.transform.position;
            }
            if (RespawnController.BedReference)
            {
                return RespawnController.BedReference.SleepPosition;
            }
            return _defaultSpawnPosition;
        }
    }

    private BaseTimeMusicManager TimeMusicManager
        => ((HRGameManager)BaseGameManager.Get).TimeMusicManager;

    private BaseTimeAmbientManager TimeAmbientManager
    => ((HRGameManager)BaseGameManager.Get).TimeAmbientManager;

    public bool IsPaused => bIsPaused;

    public GameZoneListener GZoneListener => gameZoneListener;

    public HeroAIOutpostTarget OutpostTargetAI
    {
        get
        {
            _outpostTargetAI = _outpostTargetAI ?? new HeroAIOutpostTarget(this);
            return _outpostTargetAI;
        }
    }

    public HeroBombChannel BombChannelController
    {
        get
        {
            _bombChannel = _bombChannel ?? new HeroBombChannel(this, HeroChannelInputType.TYPE_HOLD);
            return _bombChannel;
        }
    }

    public HRPoliceSystem CurrentPoliceSystem
    {
        get
        {
            return GZoneListener?.CurrentGameZone?.PoliceSystem;
        }
    }

    public HRPoliceSystem LastPoliceSystem
    {
        get
        {
            return GZoneListener?.LastGameZone?.PoliceSystem;
        }
    }

    public bool IsFined
    {
        get
        {
            HRPlayerCrimeSystemData crimeSystemData;
            if (GetCurrentCrimeSystemData(HRCrimeSystemUtils.GetCurrentCrimeSystem(this), out crimeSystemData))
            {
                return crimeSystemData.fined;
            }
            return false;
        }
    }

    public bool IsBeingFined
    {
        get
        {
            return bIsBeingFined;
        }
        set
        {
            bIsBeingFined = value;
            OnPlayerBeingFinedDelegate?.Invoke(this, bIsBeingFined);
        }
    }

    public float CrimeMusicCooldown = 30f;

    public BaseCustomizationSystem CustomizationSystem => customizationSystem;

    public HRBarkComponent BarkComponent;

    [HideInInspector]
    public HRResearchBench CurrentResearchBench;
    #endregion
    public override void Initialize()
    {
        base.Initialize();

        if (BaseInGameSettings.Get)
        {
            bHoldToSprint = BaseInGameSettings.HoldToSprint;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        bServerInitialized = true;
        GenerateNewName();
    }

    public bool bPostStart = false;
    private IEnumerator PostStartCoroutine;

    public BaseDamageIndicator DamageIndicator;
    public BaseHitFeedbackManager HitmarkerManager;

    public GameObject DialogueBubble;

    private List<BasePlayerController> damagingPlayers = new List<BasePlayerController>();

    public HRWorldMapLocation CharacterMapLocation;

    // This is bad since only the player needs this, but this is where the customization camera is
    public Cinemachine.CinemachineVirtualCamera CustomizationCamera;

    public bool IsChanneling
    {
        get
        {
            return HeroReviveController.IsChanneling || BombChannelController.IsChanneling;
        }
    }

    public string Name
    {
        get
        {
            if (PlayerController)
            {
                return PlayerController.PlayerUsername;
            }
            else if (NameComponent && NameComponent.HasName)
            {
                return NameComponent.Name;
            }
            else
            {
                return gameObject.name;
            }
        }
    }

    public override void Awake()
    {
        if (FlashlightRef)
        {
            LocalFlashlightPosition = FlashlightRef.transform.localPosition;
            LocalFlashlightRotation = FlashlightRef.transform.localRotation;
        }

        OriginalLayer = this.gameObject.layer;
        if (OriginalLayer == 0)
        {
            // This is a bug with police. Just gonna set the layer to 14 which is AI for now. Bad bad bad bad bad
            OriginalLayer = 14;
        }

        base.Awake();
        bOriginalCanBeBowled = bCanBeBowled;
        OGHighlightSettings = new HighlightEffectSettings();
        HasHP = HP;
        if (PlayerAllyHighlightEffect)
        {
            OGHighlightSettings.bEnabled = PlayerAllyHighlightEffect.enabled;
            OGHighlightSettings.width = PlayerAllyHighlightEffect.outlineWidth;
            OGHighlightSettings.color = PlayerAllyHighlightEffect.outlineColor;
        }

        if (RevengeEncounterSpawner)
        {
            RevengingEnemies = new BaseProbabilityTable<HREncounterDynamicEnemyData>();
            CurrentRevengingEnemy.EnemyID = -1;
            bIsRevengeTarget = true;
        }

        ThrowCooldown = 0;
        ThrowStartupTime = 0;
        OnSpawn();
    }

    // This is so janky but I'm just gonna do this to ship for now.
    public Light FlashlightRef;
    Vector3 LocalFlashlightPosition;
    Quaternion LocalFlashlightRotation;
    public void SetFlashlightEnabled(bool bEnabled)
    {
        if (FlashlightRef)
        {
            FlashlightRef.gameObject.SetActive(bEnabled);
        }
    }
    protected override bool GetIsPossessedByPlayer()
    {
        return PlayerController || HRPC || HRPCBeforeSpectate;
    }

    public void WaitForGround(float MaxWaitTime)
    {
        if (MovementComponent)
        {
            MovementComponent.enabled = false;
        }
        waitForGroundTimer = MaxWaitTime;
        bWaitingForGround = true;
    }

    public override void OnSpawn()
    {
        if (!gameObject.CompareTag("Player"))
        {
            return;
        }
        LayerMask layerMask = LayerMask.GetMask("Default", "FloorTiles", "WallTiles", "PlaceableWeapon");
        if (!HasGroundBelow(100f, layerMask))
        {
            if (BaseWorldStreamManager.Get && BaseWorldStreamManager.Get.ChunkCount > 0)
            {
                WaitForGround(10000f);
            }
            else
            {
                WaitForGround(10f);
            }
        }
        else if (MovementComponent)
        {
            MovementComponent.enabled = true;
            OnGroundLoaded();
        }
        PlayerCamera.UserRotation.x = 0f;
        PlayerCamera.UserRotation.y = this.transform.eulerAngles.y;
        bCanSwitchWeapons = true;
    }

    protected override void OnGroundLoaded()
    {
        if (MovementComponent)
        {
            MovementComponent.enabled = true;
        }

        if (HRNetworkManager.IsHost())
        {
            BaseGameInstance.Get.LobbyManager.SetLobbyStarted(true);
        }
    }

    public void OnPlayerReady()
    {
        if (HRNetworkManager.IsHost())
        {
            OnPlayerReadyDelegate?.Invoke(this);
            bInitialLoad = true;
        }
        else
            OnPlayerReady_Command();
    }


    [Mirror.Command(ignoreAuthority = true)]
    private void OnPlayerReady_Command()
    {
        OnPlayerReadyDelegate?.Invoke(this);
        bInitialLoad = true;
    }

    public static bool IsPlayer(GameObject inGameObject, out HeroPlayerCharacter outPC)
    {
        outPC = null;
        if (HRNetworkManager.Get)
        {
            for (int i = 0; i < HRNetworkManager.Get.PlayerDataArray.Count; ++i)
            {
                if (HRNetworkManager.Get.PlayerDataArray[i].PlayerPawn && inGameObject == HRNetworkManager.Get.PlayerDataArray[i].PlayerPawn.gameObject)
                {
                    outPC = HRNetworkManager.Get.PlayerDataArray[i].PlayerPawn as HeroPlayerCharacter;
                    return true;
                }
            }
        }

        return false;
    }

    public void SetDamageModifier(float InAdditiveModifier)
    {
        if (HRNetworkManager.IsHost())
        {
            SetDamageModifier_Implementation(InAdditiveModifier);
        }
        else
        {
            SetDamageModifier_Command(InAdditiveModifier);
        }
    }

    public void AddDamageModifier(float InAdditiveModifier)
    {
        if (HRNetworkManager.IsHost())
        {
            SetDamageModifier_Implementation(levelDamageModifier + InAdditiveModifier);
        }
        else
        {
            AddDamageModifier_Command(InAdditiveModifier);
        }
    }

    [Mirror.Server]
    void SetDamageModifier_Implementation(float InAdditiveModifier)
    {
        levelDamageModifier = InAdditiveModifier;
    }

    [Mirror.Command]
    void AddDamageModifier_Command(float InAdditiveModifier)
    {
        SetDamageModifier_Implementation(levelDamageModifier + InAdditiveModifier);
    }

    [Mirror.Command]
    void SetDamageModifier_Command(float InAdditiveModifier)
    {
        SetDamageModifier_Implementation(InAdditiveModifier);
    }

    public float GetDamageModifier()
    {
        return damageModifier * levelDamageModifier;
    }

    public void ModifyDefense(float InModifier)
    {
        if (HRNetworkManager.IsHost())
        {
            ModifyDefense_Implementation(InModifier);
        }
        else
        {
            ModifyDefense_Command(InModifier);
        }
    }


    public void ModifyDefense_Implementation(float InModifier)
    {
        levelDefenseModifier *= InModifier;
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void ModifyDefense_Command(float InModifier)
    {
        ModifyDefense_Implementation(InModifier);
    }

    public float GetDefenseModifier()
    {
        return defenseModifier * levelDefenseModifier;
    }

    public void SetVisible(bool bVisible)
    {
        if (HRNetworkManager.IsHost())
        {
            SetVisible_ClientRpc(bVisible);
        }
        else
        {
            SetVisible_Command(bVisible);
        }
    }

    [Mirror.ClientRpc]
    private void SetVisible_ClientRpc(bool bVisible)
    {
        SetVisible_Implementation(bVisible);
    }

    [Mirror.Command]
    private void SetVisible_Command(bool bVisible)
    {
        SetVisible_ClientRpc(bVisible);
    }

    private void SetVisible_Implementation(bool bVisible)
    {
        if (destroyListener)
        {
            destroyListener.DestroyFX_Implementation(); // For ronin backstabber behavior
        }

        bool bIsLocalPlayer = (this == (HeroPlayerCharacter)(BaseGameInstance.Get.GetLocalPlayerPawn()));

        if (PlayerMesh.PlayerRig)
        {
            PlayerMesh.PlayerRig.gameObject.SetActive(bVisible);

            if (!bIsLocalPlayer)
            {
                SetHUDVisible_Implementation(bVisible);
            }
        }
    }

    public void SetHUDVisible(bool bVisible)
    {
        if (HRNetworkManager.IsHost())
        {
            SetHUDVisible_ClientRpc(bVisible);
        }
        else
        {
            SetHUDVisible_Command(bVisible);
        }
    }

    [Mirror.ClientRpc]
    private void SetHUDVisible_ClientRpc(bool bVisible)
    {
        SetHUDVisible_Implementation(bVisible);
    }

    [Mirror.Command]
    private void SetHUDVisible_Command(bool bVisible)
    {
        SetHUDVisible_ClientRpc(bVisible);
    }

    private void SetHUDVisible_Implementation(bool bVisible)
    {
        if (PlayerNameText && CharacterNameText)
        {
            SetNameTextVisibility(bVisible);
        }

        SetHealthBarVisibility(bVisible);
    }

    private void SetHealthBarVisibility(bool bShow)
    {
        if (Interactable && Interactable.HealthComponentToShow)
        {
            Interactable.HealthComponentToShow.bShowHealthBar = bShow;
        }
    }

    private void SetNameTextVisibility(bool bVisible)
    {
        PlayerNameText.gameObject.SetActive(bVisible);
        CharacterNameText.gameObject.SetActive(bVisible);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!HRNetworkManager.IsHost())
        {
            if (HRNetworkManager.Get.LocalPlayerController)
            {
                GetLayer_Command(HRNetworkManager.Get.LocalPlayerController);
            }
            else
            {
                HRNetworkManager.Get.LocalPlayerControllerChangedDelegate += HandleLocalPlayerControllerSet;
            }
        }
    }

    void HandleLocalPlayerControllerSet(HRPlayerController PlayerController)
    {
        HRNetworkManager.Get.LocalPlayerControllerChangedDelegate -= HandleLocalPlayerControllerSet;

        GetLayer_Command(PlayerController);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void GetLayer_Command(BasePlayerController Requester)
    {
        if (Requester)
        {
            SetLayer_TargetRpc(Requester.connectionToClient, gameObject.layer);
        }
    }

    public bool CheckSelfAuthority()
    {
        return hasAuthority;
    }

    private void HandleCharacterNameChanged(string OldName, string NewName)
    {
        UpdateCharacterName(NewName);
    }

    void UpdateCharacterName(string NewName)
    {
        if (CharacterNameText)
        {
            if (BaseGameInstance.Get)
            {
                CharacterNameText.text = ((HRGameInstance)BaseGameInstance.Get).ProcessText("<c=main>[" + XPComponent.CurrentXPLevel + "]</c> " + NewName);
            }
            else
            {
                CharacterNameText.text = NewName;
            }
        }
    }

    void UpdateCharacterName()
    {
        if (PlayerController)
        {
            UpdateCharacterName(PlayerController.CharacterUsername);
        }
    }

    void UpdatePlayerName(string InName)
    {
        if (PlayerNameText)
        {
            PlayerNameText.text = InName;
        }
    }

    private void HandleUsernameChanged(string OldName, string NewName)
    {
        UpdatePlayerName(NewName);
        this.currentUserName = NewName;
    }

    float SprintEfficiencyModifier = 0;
    [HideInInspector] public float MeleeSkillModifier = 0;
    [HideInInspector] public float RangedSkillModifier = 0;
    [HideInInspector] public float HarvestingSkillModifier = 0f;
    [HideInInspector] public float FarmingSkillModifier = 0; //farming quality 
    [HideInInspector] public float FishingSkillModifier = 0; //fish in pond, faster and wider cursor
    [HideInInspector] public float LockpickingSkillModifier = 0; //lockpicking minigame easier
    [HideInInspector] public float CookingSkillModifier = 0; //better food attributes

    [Header("SKILL CRIT")]
    public float HarvestingCritDamagePercent = 1.5f;
    public float HarvestingMaxCritPercent = 0.7f;
    public float HarvestingCritPercent => HarvestingMaxCritPercent * HarvestingSkillModifier;

    public float MeleeCritDamagePercent = 1.5f;
    public float MeleeMaxCritPercent = 0.7f;
    public float MeleeCritPercent => MeleeMaxCritPercent * MeleeSkillModifier;

    private float _drowningDamageTime = 0.0f;
    private float _drowningNotificationTime = 0.0f;
    private bool _currentlyDrowning = false;

    public void HandleFootstepFX(BaseFootstepFX InFX, BaseFootstepFXDB InDB, int InFootIndex, bool bJustLanded, bool bOnTraxGround)
    {
        if (InDB.bDealDamageWithNoShoes)
        {
            if (HRNetworkManager.HasControl(netIdentity))
            {
                // deal damage if no shoes on
                if (customizationSystem && !customizationSystem.IsEquipped(BaseClothingDatabase.BaseClothingType.Shoes))
                {
                    if (HP.CurrentHP > InDB.DamagePerStep)
                    {
                        DamageReceiver.ApplyDamage(InDB.DamagePerStep, this.gameObject, BaseDamageType.SHOCK, "", false, true);
                    }
                }
            }
        }
    }

    public void HandleFootstepFXLocal(BaseFootstepFX InFX, BaseFootstepFXDB InDB, int InFootIndex, bool bJustLanded, bool bOnTraxGround)
    {
        if (bJustLanded)
        {
            if (MovementComponent.IsSprinting())
            {
                BaseScreenShakeManager.DoScreenShake(PlayerCamera.CameraTargetGameObject.transform, 0.25f, 0.1f, 20);
                //PlayerCamera.CameraTargetGameObject.transform.DORewind();
                //PlayerCamera.CameraTargetGameObject.transform.DOShakePosition(0.25f, 0.1f, 20);
            }
            else
            {
                BaseScreenShakeManager.DoScreenShake(PlayerCamera.CameraTargetGameObject.transform, 0.15f, 0.1f, 4);
                //PlayerCamera.CameraTargetGameObject.transform.DORewind();
                //PlayerCamera.CameraTargetGameObject.transform.DOShakePosition(0.15f, 0.1f, 4);
            }
        }
        else if (MovementComponent.IsSprinting())
        {
            BaseScreenShakeManager.DoScreenShake(PlayerCamera.CameraTargetGameObject.transform, 0.15f, 0.1f, 4);
            //PlayerCamera.CameraTargetGameObject.transform.DORewind();
            //PlayerCamera.CameraTargetGameObject.transform.DOShakePosition(0.15f, 0.1f, 4);
        }

        if (MovementComponent.bIsPlayer)
        {
            if (TraxManager.bUseTrax && BaseGameManager.Get && ((HRGameManager)BaseGameManager.Get).TraxManager)
            {
                Camera TraxCam = ((HRGameManager)BaseGameManager.Get).TraxManager.cam;

                if (TraxCam && TraxCam.gameObject.activeSelf != bOnTraxGround)
                {
                    if (bOnTraxGround)
                    {
                        LastTraxActivateTime = Time.timeSinceLevelLoad;
                        TraxCam.gameObject.SetActive(true);
                    }
                    else
                    {
                        if (Time.timeSinceLevelLoad - LastTraxActivateTime >= 1f)
                        {
                            TraxCam.gameObject.SetActive(false);
                        }
                    }
                }
            }
            /*EquipmentSFXHandler.PlayArmorAudio(InFX, InDB, InFootIndex, bJustLanded, bOnTraxGround);*/
        }
    }

    float LastTraxActivateTime;

    public void GenerateNewName()
    {
        // Need this otherwise it will have a "Synclists can only be modified at the server" error
        if (bServerInitialized)
        {
            // Generate new name for this AI if it needs one
            if (NameComponent && NameComponent.HasName)
            {
                if (PlayerMesh && PlayerMesh.CustomizationComponent)
                {
                    NameComponent.GenerateNewName(PlayerMesh.CustomizationComponent.BodyType);
                }
            }
        }
    }

    public void HandlePreRigChanged(BasePlayerMesh InPlayerMesh)
    {
        if (FlashlightRef)
        {
            FlashlightRef.transform.SetParent(this.transform);
            FlashlightRef.transform.localPosition = LocalFlashlightPosition;
            FlashlightRef.transform.localRotation = LocalFlashlightRotation;
        }
    }

    public void HandleRigChanged(BasePlayerMesh InPlayerMesh)
    {
        GenerateNewName();

        if (InPlayerMesh.PlayerRig)
        {
            if (destroyListener)
            {
                destroyListener.EffectRootTransform = InPlayerMesh.PlayerRig.HipTransform;
            }

            if (IsPossessedByPlayer)
            {
                InPlayerMesh.PlayerRig.gameObject.SetLayersRecursive(LayerMask.NameToLayer("Player"));
            }
            if (AnimScript)
            {
                InPlayerMesh.PlayerRig.CharacterRef = this;
                if (MovementComponent)
                    MovementComponent.SetRootMotionComponent(InPlayerMesh.PlayerRig.RootMotionComponent);

                if (InPlayerMesh.PlayerRig.RootMotionComponent)
                {
                    InPlayerMesh.PlayerRig.RootMotionComponent.MovementComponent = MovementComponent;
                    InPlayerMesh.PlayerRig.bLetRootMotionDisableLODs = !this.transform.CompareTag("Player");
                }

                if (AnimScript.FootstepFX)
                {
                    AnimScript.FootstepFX.FootstepFXDelegate -= HandleFootstepFX;
                    AnimScript.FootstepFX.FootstepFXDelegate += HandleFootstepFX;
                }
            }

            if (MeshJiggleScript)
            {
                MeshJiggleScript.SetTargetJiggleOverride(InPlayerMesh.PlayerRig.transform);
            }

            // This is ugly and hardcoded but better than holding a bunch of refs just for this.
            BaseHP[] HPs = GetComponents<BaseHP>();
            for (int i = 0; i < HPs.Length; ++i)
            {
                HPs[i].DamageTextTransform = InPlayerMesh.PlayerRig.HipTransform;
            }

            UpdateHealthBarVisibility();

            if (Ragdoll)
            {
                Ragdoll.ReturnJointData();

                Ragdoll.PlayerCharacter = this;
                Ragdoll.PlayerAnimator = AnimScript.AnimancerComponent;
                if (InPlayerMesh.PlayerRig.RagdollData.RootPosition)
                {
                    Ragdoll.jointsSetup = false;
                    Ragdoll.RootPosition = InPlayerMesh.PlayerRig.RagdollData.RootPosition.gameObject;
                    Ragdoll.SetupRagdoll(InPlayerMesh.PlayerRig.RagdollData.RagdollBodyParts, true);
                }
            }

            if (WitnessComponent)
            {
                WitnessComponent.WitnessUI.WorldPosition = InPlayerMesh.PlayerRig.NeckTransform;
            }

            if (WeaponManager)
            {
                WeaponManager.CombatListener = InPlayerMesh.PlayerRig.CombatListener;
                if (WeaponManager.CurrentWeapon)
                {
                    HandleWeaponEquipped(WeaponManager, WeaponManager.CurrentWeapon);
                }
                WeaponManager.EquipWeaponDelegate += HandleWeaponEquipped;
                WeaponManager.UnequipWeaponDelegate += HandleWeaponUnequipped;
                //WeaponManager.CombatListener.OnSwingDelegate += HandleCombatSwing;

                if (WeaponManager.HotKeyInventory)
                {
                    WeaponManager.HotKeyInventory.DropTransform = InPlayerMesh.PlayerRig.HipTransform.gameObject;
                }
                if (WeaponManager.MainInventory)
                {
                    WeaponManager.MainInventory.DropTransform = InPlayerMesh.PlayerRig.HipTransform.gameObject;
                }
                if (WeaponManager.EquipmentInventory)
                {
                    WeaponManager.EquipmentInventory.DropTransform = InPlayerMesh.PlayerRig.HipTransform.gameObject;
                }

                if (WeaponManager.bUseRigWeaponColliders)
                {
                    WeaponManager.AdditionalWeaponColliders = InPlayerMesh.PlayerRig.BodyWeaponColliders;
                }

                if (InPlayerMesh && InPlayerMesh.PlayerRig && InPlayerMesh.PlayerRig.WeaponSocket)
                {
                    WeaponManager.SetMainWeaponSocket(InPlayerMesh.PlayerRig.WeaponSocket?.gameObject);
                }

                InPlayerMesh.PlayerRig.OnLODEnabled += WeaponManager.HandleLODEnabled;
                InPlayerMesh.PlayerRig.OnBeingDestroyed += WeaponManager.HandleRigBeingDestroyed;
                if (InPlayerMesh.PlayerRig.IsLODActivated())
                {
                    WeaponManager.HandleLODEnabled(InPlayerMesh.PlayerRig, true);
                }

                if (bShouldUseGrounder && InPlayerMesh?.AnimScript?.IKManager)
                {
                    // For now just turn on IK for players. Janky af.
                    if(bShouldUseGrounder)
                    {
                        InPlayerMesh.AnimScript.IKManager.SetBipedIKEnabled(true, true);
                    }
                }
            }

            if (CharacterVoice)
            {
                CharacterVoice.SetVoiceAudioSource(InPlayerMesh.PlayerRig.CharacterVoiceAudioSource);

                if (InPlayerMesh.PlayerRig.CharacterVoiceAudioSource)
                {
                    InPlayerMesh.PlayerRig.CharacterVoiceAudioSource.mute = !bPlayCharacterVoice;
                }

                CharacterVoice.OwningCharacter = this;
            }

            if (PlayerAllyHighlightEffect)
            {
                PlayerAllyHighlightEffect.Refresh();
            }

            if (FlashlightRef)
            {
                if (InPlayerMesh.PlayerRig.AnimancerComponent.Animator.isHuman)
                {
                    FlashlightRef.transform.SetParent(InPlayerMesh.PlayerRig.AnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips), true);
                }
            }
        }
    }

    void UpdateHealthBarVisibility()
    {
        Transform AttachTransform = PlayerMesh?.PlayerRig?.ChestTransform;

        if (!AttachTransform)
        {
            AttachTransform = this.transform;
        }

        HRHealthBarComponent[] Bars = GetComponents<HRHealthBarComponent>();
        if (PlayerController != null && bIsPlayer == true)
        {
            if (!PlayerController.isLocalPlayer)
            {
                // Show bars for other players.
                for (int i = 0; i < Bars.Length; ++i)
                {
                    Bars[i].enabled = true;
                    Bars[i].HealthBarPosition = AttachTransform;
                    Bars[i].bShowHealthBar = true;
                }
            }
            else
            {
                for (int i = 0; i < Bars.Length; ++i)
                {
                    Bars[i].enabled = false;
                    Bars[i].HealthBarPosition = AttachTransform;
                    Bars[i].bShowHealthBar = false;
                }
            }
        }
        else
        {
            // AI
            for (int i = 0; i < Bars.Length; ++i)
            {
                Bars[i].HealthBarPosition = AttachTransform;
            }
        }
    }

    void HandleWeaponUnequipped(BaseWeaponManager InManager, BaseWeapon WeaponAdded)
    {
        if (WeaponAdded)
        {
            if (WeaponAdded.WeaponMeleeComponent)
            {
                WeaponAdded.WeaponMeleeComponent.OnMeleeStartDelegate -= HandleMeleeStart;
            }
        }

        AddInteractionContext(ThrowItemContextInfo, false);
        AddInteractionContext(BlockItemContextInfo, false);
    }

    void HandleWeaponEquipped(BaseWeaponManager InManager, BaseWeapon WeaponAdded)
    {
        if (WeaponAdded)
        {
            if (WeaponAdded != WeaponManager.DefaultEmptyWeapon)
            {
                AddInteractionContext(ThrowItemContextInfo, true);
            }

            if (WeaponAdded.WeaponMeleeComponent)
            {
                WeaponAdded.WeaponMeleeComponent.OnMeleeStartDelegate -= HandleMeleeStart;
                WeaponAdded.WeaponMeleeComponent.OnMeleeStartDelegate += HandleMeleeStart;

                AddInteractionContext(BlockItemContextInfo, true);
            }
            else
            {
                AddInteractionContext(BlockItemContextInfo, false);
            }
        }
    }

    void HandleMeleeStart(BaseWeaponMelee InBaseWeaponMelee, bool bSwingStart, bool bInterrupted)
    {
        if (bSwingStart)
        {
            HandleCombatSwing(bSwingStart);
        }
    }

    // Use this for initialization
    public override void Start()
    {
        base.Start();

        if (!bHasHealthInsurance)
        {
            // Remove poof VFX and extend the duration of the destroy timer.
            if (destroyListener)
            {
                HealthInsuranceFX = destroyListener.DestroyParticleEffect;
                HealthInsuranceAudioClip = destroyListener.DestroyAudioClip;
                destroyListener.DestroyParticleEffect = NoHealthInsuranceFX;
                destroyListener.DestroyAudioClip = null;
            }
        }

        if (HungerHP)
        {
            HungerHP.OnHPZeroDelegate += HandleHungerZero;
        }

        if (ThirstHP)
        {
            ThirstHP.OnHPZeroDelegate += HandleThirstZero;
        }

        _outpostTargetAI = new HeroAIOutpostTarget(this);

        AudioManager = ((HRGameInstance)BaseGameInstance.Get).MusicManager;

        if (InteractionManager == null)
        {
            Debug.Log("ERROR: INTERACTION MANAGER IS NOT FOUND ON PLAYER CHARACTER " + this.name);
        }

        if (WeaponManager == null)
        {
            Debug.Log("ERROR: WEAPON MANAGER IS NOT FOUND ON PLAYER CHARACTER " + this.name);
        }
        else
        {
            WeaponManager.AddedWeaponDelegate += HandleWeaponAdded;
            WeaponManager.RemovedWeaponDelegate += HandleWeaponRemoved;
        }

        if (PauseReceiver)
        {
            PauseReceiver.OnPauseDelegate += HandlePause;
            ((HRGameInstance)(BaseGameInstance.Get)).DialoguePauser.DialogueStartedDelegate += HandleDialogueStarted;

            if (HRDialogueSystem.Get && HRDialogueSystem.Get.IsGlobalPlaying && PauseReceiver.bIsPaused)
            {
                HandleDialogueStarted(null, true);
            }

            if (BaseGameInstance.Get && BaseGameInstance.Get.PauseManager.GetStatus() != PauseReceiver.bIsPaused)
            {
                HandlePause(PauseReceiver, BaseGameInstance.Get.PauseManager.GetStatus());
            }
        }

        if (destroyListener)
        {
            destroyListener.OnDestroyDelegate += HandleDestroyed;
        }

        // Initializes the HRPC to the Wallet.
        if (HRPC != null
            && HRPC.PlayerUI != null)
        {
            HRPC.PlayerUI.YourWalletUI.Initialize(HRPC);
        }

        if (SleepManager)
        {
        }

        if (Wallet)
        {
            Wallet.WalletUIUpdateDelegate += HandleWalletChanged;
            HandleWalletChanged((int)Wallet.GetBalance(),
                (int)Wallet.GetBalance(), HRWallet.WalletBalanceChangeReason.None, "");
        }

        if (bCanWave)
            WaveAnimationListener.HookEvents();
        HeroDeathReviveDelayController.HookEvents();
        HeroReviveController.HookEvents();
        HeroReviveController.OnChannelDelegate += HandleChannel;
        BombChannelController.HookEvents();
        BombChannelController.OnChannelDelegate += HandleChannel;
        HeroHolsterController.HookEvents();
        RespawnController.HookEvents();

        if (InteractionManager)
        {
            InteractionManager.OnTapInteractionDelegate += HandleInteraction;
        }

        if (MovementComponent)
        {
            MovementComponent.OnLandOnGroundDelegate += HandleLandOnGround;
            MovementComponent.OnMoveModeChangedDelegate += HandleMoveModeChanged;
        }

        if (HP)
        {
            HP.OnHPChangedDelegate += HandleHPChanged;
            HP.OnHPChangedInstigatorDelegate += HandleHPChangedInstigator;
            HP.OnHPZeroDelegate += HandleHPZero;
        }

        if (XPComponent)
        {
            XPComponent.OnLevelChangedDelegate += HandleLevelChanged;
        }

        if (Ragdoll)
        {
            Ragdoll.OnRagdollDelegate += HandleRagdoll;
        }

        PostStartCoroutine = PostStartSetup();
        StartCoroutine(PostStartCoroutine);

        if (AnimScript)
        {
            IKManager = AnimScript.IKManager;
        }

        if (WeaponManager && WeaponManager.CombatListener)
        {
            WeaponManager.CombatListener.OnSwingDelegate += HandleCombatSwing;
        }

        if (DashComponent)
        {
            DashComponent.OnDashDelegate += HandleDashStarted;
        }

        if (this.CompareTag("Player"))
        {
            if (BaseGameManager.Get && BaseGameManager.Get.bGameManagerStarted)
            {
                InitializeOnGameManagerStarted();
            }
            else
            {
                BaseGameInstance.Get.GameManagerStartedDelegate += HandleGameManagerStarted;
            }
        }

        if (FollowerManager)
        {
            FollowerManager.OnFollowerAddedRemovedDelegate += HandleFollowerAddedRemoved;
        }

        if (this == BaseGameInstance.Get.GetLocalPlayerPawn())
        {
            if (PlayerMesh)
            {
                PlayerMesh.OnRigChangedDelegate += HideRigIfStillInLoading;
            }
            if (MovementComponent)
            {
                MovementComponent.OwningAIMovement.FloorTileChangedDelegate += HandleFloorTileChanged;
            }
        }


        InstantiateFactionData();

        // We need to cache this data, as it returns false during cleanup.
        bLocalPlayer = IsControlledByPlayer && this.hasAuthority;

        BaseWater.CheckInAnyWater(gameObject, true);
    }

    public void GiveHealthInsurance()
    {
        if (HRNetworkManager.IsHost())
        {
            GiveHealthInsurance_Implementation();
            GiveHealthInsurance_ClientRpc();
        }
        else
        {
            GiveHealthInsurance_Command();
        }
    }

    [Mirror.ClientRpc]
    private void GiveHealthInsurance_ClientRpc()
    {
        GiveHealthInsurance_Implementation();
    }

    [Mirror.Command]
    private void GiveHealthInsurance_Command()
    {
        GiveHealthInsurance_ClientRpc();
    }

    private void GiveHealthInsurance_Implementation()
    {
        bHasHealthInsurance = true;

        HRDeathBoxDropper DeathBoxDropper = GetComponent<HRDeathBoxDropper>();
        if (DeathBoxDropper != null)
        {
            DeathBoxDropper.DeathBoxPrefab = DeathBoxDropper.HealthInsuranceDeathBoxPrefab;
            MirrorUtils.RegisterPrefab(DeathBoxDropper.DeathBoxPrefab);
        }
        if (destroyListener)
        {
            destroyListener.DestroyParticleEffect = HealthInsuranceFX;
            destroyListener.DestroyAudioClip = HealthInsuranceAudioClip;
        }
    }

    public void ApplyDehydratedSwingSpeed(float InSwingSpeedChange)
    {
        if (!AttributeManager.HasAttribute(24, "dehydrated"))
        {
            HRRawAttributeData attr = new HRRawAttributeData(24, true, false, "dehydrated", HRAttribute.EAttributeTriggers.None, healthLostPerTickHungerAndThirst, -1, thirstAndHungerTickRate);
            AttributeManager.AddAttribute(attr);

            ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("Dehydrated!", this.transform);
        }
    }

    public void RemoveDehydratedSwingSpeed()
    {
        AttributeManager.RemoveAllAttributesWithID(24, "dehydrated");

        ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("Hydrated!", this.transform);
    }

    public void ApplyHungerAttribute(float DamageWhenStarving, float StarvingDamageInterval)
    {
        if (!AttributeManager.HasAttribute(23, "hungerneed"))
        {
            HRRawAttributeData attr = new HRRawAttributeData(23, true, false, "hungerneed", HRAttribute.EAttributeTriggers.None, DamageWhenStarving, -1, StarvingDamageInterval);
            AttributeManager.AddAttribute(attr);

            ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("Starving!", this.transform);
        }
    }

    public void RemoveHungerAttribute()
    {
        AttributeManager.RemoveAllAttributesWithID(23, "hungerneed");

        ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("No longer starving!", this.transform);
    }

    public void ApplyDehydratedAttribute(float DamageWhenDehydrated, float DehydratedDamageInterval)
    {
        if (!AttributeManager.HasAttribute(24, "thirstneed"))
        {
            HRRawAttributeData attr = new HRRawAttributeData(24, true, false, "thirstneed", HRAttribute.EAttributeTriggers.None, DamageWhenDehydrated, -1, DehydratedDamageInterval);
            AttributeManager.AddAttribute(attr);
        }
    }

    public void RemoveDehydratedAttribute()
    {
        AttributeManager.RemoveAllAttributesWithID(24, "thirstneed");
    }

    void HandleHungerZero(BaseHP InHPComponent, GameObject Instigator, float PreviousHP, float NewHP, bool bFireEffects)
    {
        if (InHPComponent && (NewHP == 0 && PreviousHP != 0))
        {
            InHPComponent.OnHPChangedDelegate -= HandleHungerHPChanged;
            InHPComponent.OnHPChangedDelegate += HandleHungerHPChanged;

            // Add hunger attribute
            ApplyHungerAttribute(healthLostPerTickHungerAndThirst, thirstAndHungerTickRate);
            HRNotificationSystem NotificationSystem = ((HRGameInstance)BaseGameInstance.Get)?.TooltipNotificationSystem;
            if (NotificationSystem)
            {
                string NotificationText = "You are starving! Eat food to regenerate your hunger.";
                float Duration = 5.0f;
                int Priority = 20;
                NotificationSystem.AddNotification(new HRTooltipNotification(null, NotificationText, Duration, Priority));
            }

        }
    }

    void HandleThirstZero(BaseHP InHPComponent, GameObject Instigator, float PreviousHP, float NewHP, bool bFireEffects)
    {
        if (InHPComponent && (NewHP == 0 && PreviousHP != 0))
        {
            InHPComponent.OnHPChangedDelegate -= HandleThirstHPChanged;
            InHPComponent.OnHPChangedDelegate += HandleThirstHPChanged;
            HRNotificationSystem NotificationSystem = ((HRGameInstance)BaseGameInstance.Get)?.TooltipNotificationSystem;
            if (NotificationSystem)
            {
                string NotificationText = "You are dehydrated! Drink something or find a water source to regenerate your thirst.";
                float Duration = 5.0f;
                int Priority = 20;
                NotificationSystem.AddNotification(new HRTooltipNotification(null, NotificationText, Duration, Priority));
            }
            // Add thirst attribute
            ApplyDehydratedSwingSpeed(0.85f);
        }
    }

    void HandleHungerHPChanged(BaseHP InHPComponent, GameObject Instigator, float PreviousHP, float NewHP, bool bFireEffects)
    {
        if (NewHP > 0)
        {
            // Remove hunger attribute
            if (InHPComponent)
            {
                InHPComponent.OnHPChangedDelegate -= HandleHungerHPChanged;
            }

            RemoveHungerAttribute();
        }
    }

    void HandleThirstHPChanged(BaseHP InHPComponent, GameObject Instigator, float PreviousHP, float NewHP, bool bFireEffects)
    {
        if (NewHP > 0)
        {
            // Remove thirst attribute
            if (InHPComponent)
            {
                InHPComponent.OnHPChangedDelegate -= HandleThirstHPChanged;

                RemoveDehydratedSwingSpeed();
            }
        }
    }

    private void InstantiateFactionData()
    {
        if (!OriginalFactionDataAsset)
        {
            OriginalFactionDataAsset = ((HRGameInstance)BaseGameInstance.Get).FallbackFactionAsset;
        }

        if (OriginalFactionDataAsset)
        {
            if (FactionDataAsset)
            {
                Destroy(FactionDataAsset);
            }

            FactionDataAsset = Instantiate(OriginalFactionDataAsset);
        }
    }

    private void InitializeOnGameManagerStarted()
    {
        InitializeSkills();

        if (gameZoneListener)
        {
            gameZoneListener.GameZoneSwitchedEvent += HandleGameZoneSwitched;
            GameZone forcedGameZone = gameZoneListener ? gameZoneListener.ForceGetGameZone() : null;
            HandleGameZoneSwitched(null, forcedGameZone, true);
        }

        HandleDebugMode(BaseGameInstance.Get.bDebugMode);
        BaseGameInstance.Get.DebugDelegate += HandleDebugMode;
    }

    void HandleGameManagerStarted(BaseGameManager GameManager)
    {
        GameManager.GameManagerStartedDelegate -= HandleGameManagerStarted;
        InitializeOnGameManagerStarted();
    }

    void InitializeSkills()
    {
        if (!SkillSystem)
        {
            return;
        }

        SkillSystem.OnSkillLevelChangedDelegate += HandleSkillLevelUp;
        //get intital skill levels here
        //TODO: are these even used anywhere?
        SprintEfficiencyModifier = SkillSystem.GetSkillEfficiency("Fitness"); ///???
        MeleeSkillModifier = SkillSystem.GetSkillEfficiency(HRSkillSystem.EPlayerSkill.Melee);
        RangedSkillModifier = SkillSystem.GetSkillEfficiency(HRSkillSystem.EPlayerSkill.Ranged);
        HarvestingSkillModifier = SkillSystem.GetSkillEfficiency(HRSkillSystem.EPlayerSkill.Harvesting);
        FishingSkillModifier = SkillSystem.GetSkillEfficiency(HRSkillSystem.EPlayerSkill.Fishing);
        FarmingSkillModifier = SkillSystem.GetSkillEfficiency(HRSkillSystem.EPlayerSkill.Farming);
        LockpickingSkillModifier = SkillSystem.GetSkillEfficiency(HRSkillSystem.EPlayerSkill.Lockpicking);
        CookingSkillModifier = SkillSystem.GetSkillEfficiency(HRSkillSystem.EPlayerSkill.Cooking);
        if (MovementComponent)
        {
            float MovementSpeedModifier = 1f + MovementSpeedAtLevelCurve.Evaluate(SkillSystem.GetSkillLevel("Fitness"));
            if (MovementSpeedModifier != 0f)
            {
                MovementComponent.MovementSpeedModifier = MovementSpeedModifier;
            }
        }
        if (this && HRNetworkManager.HasControl(netIdentity))
        {
            SkillSystem.SetSkillsXPTransform(this.transform);
        }
    }

    public void SetCurrentSeat(HRSeatComponent InSeat)
    {
        if (InSeat)
        {
            OnSitDelegate?.Invoke(this, InSeat, true);
        }
        else
        {
            OnSitDelegate?.Invoke(this, CurrentSeat, false);
        }
        CurrentSeat = InSeat;
    }

    private void HandleFollowerAddedRemoved(BaseFollowerManager InManager, BasePlayerFollow InFollower, bool bFollowing)
    {
        if (InManager.GetFollowerCount() > 0)
        {
            bCanSit = false;
            if (CurrentSeat)
            {
                CurrentSeat.Unseat(false);
            }
        }
        else
        {
            bCanSit = true;
        }
    }

    public void AddXPForMelee(float expDamage, float modifiers = 1f)
    {
        AddToSkill(HRSkillSystem.EPlayerSkill.Melee, expDamage * modifiers, xpSoak.xpSoakSO.GetPlayerSkill(HRSkillSystem.EPlayerSkill.Melee).XPShare, false, "", true);
    }
    public void AddXPForRanged(float RangedDamage = 1f, float modifiers = 1f)
    {
        AddToSkill(HRSkillSystem.EPlayerSkill.Ranged, RangedDamage * modifiers, xpSoak.xpSoakSO.GetPlayerSkill(HRSkillSystem.EPlayerSkill.Ranged).XPShare, false, "", true);
    }
    public void AddXPForFishing(float difficultyModifier)
    {
        //TODO: Add exp based on fish difficulty
        AddToSkill(HRSkillSystem.EPlayerSkill.Fishing, XPFromFishing * difficultyModifier, xpSoak.xpSoakSO.GetPlayerSkill(HRSkillSystem.EPlayerSkill.Fishing).XPShare, false, "", true, modifierFishingExp);
    }
    public void AddXPForWoodcutting(float exp, float WoodcuttingModifier = 1f)//, HRItemEXPType type)
    {
        AddToSkill(HRSkillSystem.EPlayerSkill.Harvesting, exp * WoodcuttingModifier, xpSoak.xpSoakSO.GetPlayerSkill(HRSkillSystem.EPlayerSkill.Harvesting).XPShare, false, "", true, modifierWoodcuttingExp);
    }
    public void AddXPForMining(float exp, float MiningModifier = 1f)//, HRItemEXPType type)
    {
        AddToSkill(HRSkillSystem.EPlayerSkill.Harvesting, exp * MiningModifier, xpSoak.xpSoakSO.GetPlayerSkill(HRSkillSystem.EPlayerSkill.Harvesting).XPShare, false, "", true, modifierMiningExp);
    }
    public void AddXPForFarming(float Farmingmodifier)//, HRItemEXPType type)
    {
        AddToSkill(HRSkillSystem.EPlayerSkill.Farming, XPFromFarming * Farmingmodifier, xpSoak.xpSoakSO.GetPlayerSkill(HRSkillSystem.EPlayerSkill.Farming).XPShare, false, "", true);
    }
    public void AddXPForLockpicking(int securityLevel)
    {
        AddToSkill(HRSkillSystem.EPlayerSkill.Lockpicking, XPFromLockpicking * securityLevel, xpSoak.xpSoakSO.GetPlayerSkill(HRSkillSystem.EPlayerSkill.Lockpicking).XPShare, false, "", true, modifierLockpickingExp);
    }
    public void AddXPForCooking()
    {
        AddToSkill(HRSkillSystem.EPlayerSkill.Cooking, XPFromCooking, xpSoak.xpSoakSO.GetPlayerSkill(HRSkillSystem.EPlayerSkill.Cooking).XPShare, false, "", true);
    }
    public void AddXPForShopkeeping(float saleModifier)//, HRItemEXPType type)
    {
        AddToSkill(HRSkillSystem.EPlayerSkill.Shopkeeping, saleModifier, xpSoak.xpSoakSO.GetPlayerSkill(HRSkillSystem.EPlayerSkill.Shopkeeping).XPShare, false, "", true);
    }

    public void AddXPFromSoak(HRSkillSystem.EPlayerSkill skill, float xp, bool XPShareEnabled, bool fromXPShare)
    {
        AddToSkill(skill, xp, XPShareEnabled, fromXPShare);
    }
    private void AddToSkill(HRSkillSystem.EPlayerSkill skill, float xp, bool XPShareEnabled, bool fromXPShare, string reason = "", bool immediate = false, float modifier = 1f)
    {
        if (!fromXPShare) // prevents skill-specific xp soak
        {
            SkillSystem?.AddToSkill(skill, xp, reason, immediate);
        }
        if (XPShareEnabled) 
        {
            xpSoak.XPSoakToPlayers(HRPC.gameObject, xp, skill);
        }
        //don't show exp for skills
        if (this && XPComponent)
        {
            XPComponent.AddHP_IgnoreAuthority(xp * modifier, null, false, false, false);
        }
    }

    void HandleSkillLevelUp(string InSkill, float Oldvalue, float NewValue)
    {
        if (!SkillSystem)
        {
            return;
        }
        if (InSkill == "Fitness")
        {
            SprintEfficiencyModifier = SkillSystem.GetSkillEfficiency(InSkill);
            if (MovementComponent)
            {
                float MovementSpeedModifier = 1f + MovementSpeedAtLevelCurve.Evaluate(NewValue);
                if (MovementSpeedModifier != 0f)
                {
                    MovementComponent.MovementSpeedModifier = MovementSpeedModifier;
                }
            }
        }
        HRSkillSystem.EPlayerSkill Skill = (HRSkillSystem.EPlayerSkill)System.Enum.Parse(typeof(HRSkillSystem.EPlayerSkill), InSkill);
        switch (Skill)
        {
            case HRSkillSystem.EPlayerSkill.Melee:
                MeleeSkillModifier = SkillSystem.GetSkillEfficiency(Skill);
                break;

            case HRSkillSystem.EPlayerSkill.Ranged:
                RangedSkillModifier = SkillSystem.GetSkillEfficiency(Skill);
                break;

            case HRSkillSystem.EPlayerSkill.Harvesting:
                HarvestingSkillModifier = SkillSystem.GetSkillEfficiency(Skill);
                break;
            case HRSkillSystem.EPlayerSkill.Fishing:
                FishingSkillModifier = SkillSystem.GetSkillEfficiency(Skill);
                break;

            case HRSkillSystem.EPlayerSkill.Farming:
                FarmingSkillModifier = SkillSystem.GetSkillEfficiency(Skill);
                break;

            case HRSkillSystem.EPlayerSkill.Lockpicking:
                LockpickingSkillModifier = SkillSystem.GetSkillEfficiency(Skill);
                break;

            case HRSkillSystem.EPlayerSkill.Cooking:
                CookingSkillModifier = SkillSystem.GetSkillEfficiency(Skill);
                break;
        }
    }

    void HandleDashStarted(BaseMovementDash InDashComponent, bool bStarted, bool bRecoveringFromKnockdown)
    {
        if (bStarted
            && InDashComponent.PlayerCharacter == this)
        {
            if (CharacterVoice)
            {
                CharacterVoice.PlayAudio("Dash");
            }

            if (this.CompareTag("Player") && HRNetworkManager.HasControl(netIdentity))
            {
                BaseScreenShakeManager.DoScreenShake(PlayerCamera.CameraTargetGameObject.transform, 0.3f, 0.1f, 40);
            }

            // Cancel

            if (MovementComponent && MovementComponent.CurrentMoveMode == BaseMoveMode.GLIDING)
            {
                MovementComponent.SetMoveMode(BaseMoveMode.GROUND);
            }
        }
    }

    void HandleCombatSwing(bool bStartSwing)
    {
        if (MovementComponent && MovementComponent.CurrentMoveMode == BaseMoveMode.ZIPLINING)
        {
            MovementComponent.DetachZipline();
        }
    }

    public void PlayVoiceAudio(string audioClipName)
    {
        if (CharacterVoice)
        {
            CharacterVoice.PlayAudio(audioClipName);
        }
    }

    private IEnumerator PostStartSetup()
    {
        if (ItemPlacingManager)
        {
            yield return new WaitForFixedUpdate();
            ItemPlacingManager.RefreshGhostGameObject();
        }
    }

    // This is the hook for it.
    private void HandleInteractStateChanged(InteractState prevInteractState, InteractState newInteractState)
    {
        InteractStateChanged(prevInteractState, newInteractState);
    }

    private void InteractStateChanged(InteractState prevInteractState, InteractState newInteractState)
    {
        bool bInvoke = false;
        if (prevInteractState != newInteractState)
        {
            if (newInteractState == InteractState.Stunned
                || newInteractState == InteractState.Dying
                || newInteractState == InteractState.Dead)
            {
                if (WeaponManager)
                {
                    if (WeaponManager.CurrentWeapon && WeaponManager.CurrentWeapon.WeaponBlockerComponent)
                    {
                        BaseWeaponBlocker WeaponBlocker = WeaponManager.CurrentWeapon.WeaponBlockerComponent;
                        if (WeaponBlocker)
                        {
                            SetLastTimeBlocked();
                            WeaponBlocker.SetBlocking(false);
                        }
                    }
                    WeaponManager.PrimaryInteract(false);
                }

                HandleBlock(false);

                SecondaryMouseEvent(false);
                if (DashComponent)
                {
                    DashComponent.StopCharging();
                }

                if (HRPC && HRPC.PlayerUI)
                {
                    if (HRPC.PlayerUI.CraftingUI && HRPC.PlayerUI.CraftingUI.CurrentCraftingComponent &&
                        HRPC.PlayerUI.CraftingUI.CurrentCraftingComponent.bIsCrafting)
                    {
                        HRPC.PlayerUI.CraftingUI.CurrentCraftingComponent.HideUI();
                    }

                    HRPC.PlayerUI.ToggleInventoryUI(false);

                    if (HRPC.PlayerUI.ContextMenuUI)
                        HRPC.PlayerUI.ContextMenuUI.SetEnabled(false);
                }
                if (AnimScript)
                {
                    AnimScript.SetFootstepsEnabled(false);
                }

                if (MovementComponent)
                {
                    MovementComponent.SetUsePlayerInput(false);
                    MovementComponent.LockUsePlayerInput(true);
                    MovementComponent.SetRotateTowardsMovement(false);
                    MovementComponent.SetShouldRotate(false);
                    if (MovementComponent.IsCrouching())
                    {
                        MovementComponent.Crouch(false, true, false);
                    }
                }

                if (InventoryManager)
                {
                    InventoryManager.CloseCurrentContainer();

                    // Not great
                    if (WeaponManager && WeaponManager.CurrentWeapon && !WeaponManager.CurrentWeapon.GetComponent<HRBuildingBlueprintWeapon>())
                    {
                        WeaponManager.SetUseMode(BaseWeaponManager.WeaponUseMode.USING);
                    }
                }

            }
            else if (newInteractState == InteractState.Free && !bIsPaused)
            {
                if (ItemPlacingManager)
                {
                    SetInputEnabled(true);
                }

                if (HRPC && HRPC.PlayerUI && HRPC.PlayerUI.ContextMenuUI)
                {
                    HRPC.PlayerUI.ContextMenuUI.SetEnabled(true);
                }

                if (PixelCrushers.DialogueSystem.DialogueManager.IsConversationActive)
                {
                    HandleDialogueStarted(null, true);
                }

                if (this.currentDyingUI)
                {
                    // Hide the F prompt
                    SetRevivePromptUIActive(false);
                    Destroy(this.currentDyingUI.gameObject);
                }

                if (AnimScript && prevInteractState != InteractState.Dead && AnimScript.FootstepListener && AnimScript.FootstepListener.bOGEnabled)
                {
                    AnimScript.SetFootstepsEnabled(true);
                }

                if (prevInteractState == InteractState.Sleeping)
                {
                    Ragdoll.GetUp();
                    ShowSleepingFX(false);

                    // Open eyes
                    if (PlayerMesh && PlayerMesh.PlayerRig && PlayerMesh.PlayerRig.FacialAnimationHandler)
                    {
                        PlayerMesh.PlayerRig.FacialAnimationHandler.SetEyeOpenValue(1f);
                    }
                }

                if (MovementComponent)
                {
                    MovementComponent.LockUsePlayerInput(false);
                    MovementComponent.SetUsePlayerInput(true);
                    MovementComponent.SetRotateTowardsMovement(true);
                    MovementComponent.SetShouldRotate(true);
                }
            }

            //Just for dead/dying state
            if (newInteractState == InteractState.Dying
                || newInteractState == InteractState.Dead)
            {
                if (!gameObject.CompareTag("Player"))
                {
                    this.gameObject.layer = LayerMask.NameToLayer("Ragdoll");
                    if (HRCrimeSystemUtils.IsCriminal(this))
                    {
                        HRCrimeSystemUtils.ForceResetAllCrimeStatuses(this);
                    }
                }
                else
                {
                    if (HRCrimeSystemUtils.IsCriminal((IPlayerCrimeDataHolder)PlayerController))
                    {
                        if (HRCrimeSystemUtils.TryResetAndPayAllCrimeStatuses(this))
                        {
                            PlayCrimeStinger(HRPoliceSystem.StingerType.PLAYER_DEATH);
                            StopCrimeMusic(CurrentPoliceSystem ? CurrentPoliceSystem : LastPoliceSystem);
                        }
                    }
                }

                if (AnimScript && AnimScript.AnimancerComponent)
                {
                    AnimScript.AnimancerComponent.Stop();
                    AnimScript.AnimancerComponent.Play(AnimScript.AnimancerComponent.Controller);
                }

                if (statusEffectsManager)
                {
                    //remove all types of status effects
                    statusEffectsManager.ResetAllStatusEffectsLocks();
                }
            }
            else
            {
                this.gameObject.layer = OriginalLayer;
            }

            //if (newInteractState == InteractState.Dead)
            //{
            // I don't think we need to stop music on death anymore.
            //if (bLocalPlayer)
            //{
            //    BaseMusicManager MusicManager = ((HRGameInstance)BaseGameInstance.Get).MusicManager;
            //    if (MusicManager && TimeMusicManager)
            //    {
            //        MusicManager.RequestStopAllMusic(1000);
            //        TimeMusicManager.SetCurrentMusicInfo(null);
            //    }
            //}


            //}

            if (newInteractState == InteractState.Sleeping)
            {
                if (!IsPossessedByPlayer)
                {
                    MovementComponent.FreezeMovement(true);
                    if (!bLocalPlayer)
                    {
                        BaseAIMovement AIMovement = GetComponent<BaseAIMovement>();
                        if (AIMovement)
                        {
                            AIMovement.StopMovement();
                        }
                    }
                    Ragdoll.StunForFall(Vector3.up, false, false);
                    if (AnimScript && AnimScript.AnimancerComponent)
                    {
                        AnimScript.AnimancerComponent.Stop();
                        AnimScript.AnimancerComponent.Play(AnimScript.AnimancerComponent.Controller);
                    }
                }

                ShowSleepingFX(true);

                // Close eyes
                if (PlayerMesh && PlayerMesh.PlayerRig && PlayerMesh.PlayerRig.FacialAnimationHandler)
                {
                    PlayerMesh.PlayerRig.FacialAnimationHandler.SetEyeOpenValue(0f);
                }
            }

            bInvoke = true;
        }

        _previousInteractState = prevInteractState;
        _localInteractState = newInteractState;

        if (bInvoke)
            OnStateChangeDelegate?.Invoke(this, prevInteractState, newInteractState);
    }

    public void ShowSleepingFX(bool bShow)
    {
        if (bShow)
        {
            if (sleepingFXPrefab && PlayerMesh && PlayerMesh.PlayerRig && PlayerMesh.PlayerRig.HeadTransform)
            {
                if (!sleepingFXInstance)
                {
                    sleepingFXInstance = Instantiate(sleepingFXPrefab);
                }
                if (sleepingFXInstance)
                {
                    sleepingFXInstance.SetActive(true);
                    sleepingFXInstance.transform.SetParent(PlayerMesh.PlayerRig.HeadTransform);
                    sleepingFXInstance.transform.localPosition = Vector3.zero;
                    sleepingFXInstance.transform.localRotation = Quaternion.identity;
                }
            }
        }
        else if (sleepingFXInstance)
        {
            Destroy(sleepingFXInstance);
        }
    }

    public void SetNewInteractState(InteractState NewInteractState)
    {
        if (CurrentInteractState != NewInteractState)
        {
            if (HRNetworkManager.HasControl(netIdentity))
            {
                _localInteractState = NewInteractState;
                if (HRNetworkManager.IsHost())
                {
                    _networkedInteractState = NewInteractState;
                }
                else
                    SetNewInteractState_Command(CurrentInteractState, NewInteractState);
            }
            else if (HRNetworkManager.Get
                && HRNetworkManager.bIsServer)
            {
                _networkedInteractState = NewInteractState;
            }
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SetNewInteractState_Command(InteractState prevState, InteractState newState)
    {
        _networkedInteractState = newState;
    }


    public bool IsBlocking()
    {
        if (WeaponManager.CurrentWeapon &&
            (WeaponManager.CurrentWeapon.WeaponBlockerComponent))
        {
            return WeaponManager.CurrentWeapon.WeaponBlockerComponent.IsBlocking();
        }

        return false;
    }

    bool bMovedSinceSprintTapped = false;

    void SetLastTimeBlocked()
    {
        lastTimeBlocked = Time.timeSinceLevelLoad;
    }

    void CheckForBlockInput()
    {
        bool bCachedTryingToBlock = IsTryingToBlock();

        if (bCachedTryingToBlock && (lastTimeBlocked > (Time.timeSinceLevelLoad - maxBlockCooldown)))
        {
            return;
        }

        // Only check if the pressed state is different from the block state
        if (WeaponManager.CurrentWeapon)
        {
            if (WeaponManager.CurrentWeapon.WeaponBlockerComponent && (bCachedTryingToBlock != WeaponManager.CurrentWeapon.WeaponBlockerComponent.IsBlocking(false, true)))
            {
                if (InteractionManager)
                {
                    InteractionManager.StopInteraction(InteractionManager.LastInteractable);
                    InteractionManager.StopTapHoldInteraction(InteractionManager.LastInteractable);
                }
                BaseWeaponBlocker WeaponBlocker = null;
                // Request to start blocking
                if (WeaponManager.CurrentWeapon)
                {
                    //check if block is in cooldown
                    WeaponBlocker = WeaponManager.CurrentWeapon.WeaponBlockerComponent;
                    if (WeaponBlocker)
                    {
                        if (bCachedTryingToBlock)
                        {
                            if (WeaponManager.CurrentWeapon.WeaponMeleeComponent && WeaponBlocker.CanBlock() && WeaponManager.CurrentUseMode != BaseWeaponManager.WeaponUseMode.PLACING)
                            {
                                SetLastTimeBlocked();
                                WeaponBlocker.SetBlocking(true);
                                //MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
                            }
                        }
                        else
                        {
                            SetLastTimeBlocked();
                            WeaponBlocker.SetBlocking(false);
                        }
                    }
                }

                /*
                if (WeaponBlocker)
                {
                    if (bCachedTryingToBlock)
                    {
                        if (IsBlocking())
                        {
                            if (MovementComponent.CurrentMoveSpeed != BaseMoveType.SPRINTING)
                            {
                                MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.WALKING);
                            }
                        }
                    }
                    else
                    {
                        // Probably need to have some sort of lock to make sure you can't unwalk from shooting
                        if (MovementComponent.CurrentMoveSpeed == BaseMoveType.WALKING)
                        {
                            MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
                        }
                    }
                }
                */
            }
        }
    }

    float CheckDestroyTime = 2f;
    float CheckDestroyTimer;

    // Update is called once per frame
    public override void ManagedUpdate(float DeltaTime)
    {
        base.ManagedUpdate(DeltaTime);

        bool isServerCached = isServer;

        if (isServerCached)
        {
            CheckDestroyTimer -= DeltaTime;

            if (CheckDestroyTimer <= 0 && BaseGameManager.HasInstance)
            {
                CheckDestroyTimer = CheckDestroyTime;

                if (transform.position.y <= BaseGameManager.Get.KillHeight && CurrentInteractState == InteractState.Free && DamageReceiver)
                {
                    DamageReceiver.ApplyDamage(9999999, gameObject, BaseDamageType.BLUNT, "ALL", true, true);
                    return;
                }
            }

            if (bIsRevengeTarget)
            {
                if (CurrentRevengingEnemy.EnemyID == -1)
                {
                    if (RevengingEnemies.Count > 0)
                    {
                        RevengingEnemies.TryGetRandomEntry(out CurrentRevengingEnemy, bRemoveAfter: true);
                    }
                }
                else
                {
                    if (CurrentRevengeTime == -1)
                    {
                        CurrentRevengingTimer = 0f;
                        CurrentRevengeTime = CurrentRevengingEnemy.SpawningTime;
                    }
                    else
                    {
                        CurrentRevengingTimer += DeltaTime;

                        if (CurrentRevengingTimer >= CurrentRevengeTime && CurrentRevengingEnemy.EnemyID != -1)
                        {
                            HREncounterSaveData RevengeEncounterData = new HREncounterSaveData();

                            RevengeEncounterData.EncounterID = 28;
                            RevengeEncounterData.DynamicEnemiesList = new List<HREncounterDynamicEnemyData>() { CurrentRevengingEnemy };

                            RevengeEncounterSpawner.EncountersCache.Add(RevengeEncounterData);

                            CurrentRevengeTime = -1;
                            CurrentRevengingEnemy.EnemyID = -1;
                        }
                    }
                }
            }
        }

        if (bWaitingForGround)
        {
            waitForGroundTimer -= DeltaTime;
            LayerMask layerMask = LayerMask.GetMask("Default", "FloorTiles", "WallTiles", "PlaceableWeapon");
            if (waitForGroundTimer <= 0f || HasGroundBelow(100f, layerMask))
            {
                OnGroundLoaded();
                bWaitingForGround = false;
                bPostStart = true;
            }
        }

        if (isOwned || (isServerCached && netIdentity.connectionToClient == null))
        {
            if (!bIsPaused && bIsPlayer)
                CheckForBlockInput();

            this.HeroReviveController?.Update(DeltaTime);
            this.BombChannelController?.Update(DeltaTime);

            // Check floor tile distance
            if (IsInShopPlot && onFloorTile == false && LastShopFloorTile != null)
            {
                float dist = (LastShopFloorTile.transform.position - MovementComponent.MovementTransform.position).sqrMagnitude;
                if (dist > 20f)
                {
                    LastShopFloorTile = null;
                    HandleEnteredShop(null);
                }
            }
        }

        if (DynamicEffects && DynamicEffects.gameObject.activeInHierarchy)
        {
            DynamicEffects.transform.position = this.transform.position + new Vector3(0, (DynamicEffects.CaptureSize / 2), 0);
        }

        switch (CurrentInteractState)
        {
            case InteractState.Free:
                {
                    // THIS IS SO GROSS REVISIT LATER
                    if (hasAuthority || (isServerCached && netIdentity.connectionToClient == null))
                    {
                        // Manually tick things using DoUpdate
                        if (InteractionManager)
                            InteractionManager.DoUpdate();
                        if (crimeMusicCooldown > 0f)
                            crimeMusicCooldown -= Time.deltaTime;
                    }
                    break;
                }

            case InteractState.None:
                break;
            case InteractState.Paused:
                break;
            case InteractState.Stunned:
                break;
            case InteractState.Dying:
                this.HeroDeathReviveDelayController?.Update(Time.deltaTime);
                break;
        }

        if (WeaponManager && WeaponManager.bIsThrowing)
        {
            ThrowStartupTime -= DeltaTime;
        }


        // Todo tick optimizations
        UpdateHitstun();
    }

    public void FixedUpdate()
    {
        if (isOwned || (isServer && netIdentity.connectionToClient == null))
        {
            //OutpostTargetAI.Update(Time.fixedDeltaTime);

            UpdateStamina(Time.fixedDeltaTime);
        }
    }

    private void UpdateStamina(float deltaTime)
    {
        // Consume stamina
        if (StaminaComponent)
        {
            float ResultingStaminaDrainHP = 0.0f;

            //Stamina drain for grounded movement
            if (MovementComponent)
            {
                if (MovementComponent.IsSprinting() && DashComponent && !DashComponent.bIsDashing)
                {
                    if (!MovementComponent.GetIsGrounded() || MovementComponent.GetPlayerInputVector() != Vector3.zero)
                    {
                        bMovedSinceSprintTapped = true;

                        if (StaminaComponent.CurrentHP > 0.0f)
                        {
                            // set hp instead of remove because authority
                            ResultingStaminaDrainHP += (SprintStaminaRate * SprintEfficiencyModifier) * deltaTime;
                            //AddToSkill("Fitness", SprintXPRate * deltaTime, "Sprinting", true);
                        }
                        else
                        {
                            if (MovementComponent.GetIsGrounded())
                            {
                                DisplayNoStaminaMessage();

                                MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
                            }
                        }
                    }
                    else
                    {
                        if (!bHoldToSprint)
                        {
                            if (bMovedSinceSprintTapped)
                            {
                                // If we haven't even moved since we tapped
                                MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);

                                bMovedSinceSprintTapped = false;
                            }
                        }
                    }
                }
            }

            // TODO: Add a better check for tutorial active state
            if (MovementComponent)
            {
                //swimming 
                if (MovementComponent.CurrentMoveMode == BaseMoveMode.SWIMMING)
                {
                    if (MovementComponent.GetPlayerInputVelocity() != Vector3.zero)
                    {
                        bMovedSinceSprintTapped = true;
                        ResultingStaminaDrainHP += (SwimStaminaRate * SprintEfficiencyModifier) * deltaTime;
                    }
                    else
                    {
                        if (!bHoldToSprint)
                        {
                            if (bMovedSinceSprintTapped)
                            {
                                // If we haven't even moved since we tapped
                                MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
                                bMovedSinceSprintTapped = false;
                            }
                        }
                    }

                }

                //gliding
                if (MovementComponent.CurrentMoveMode == BaseMoveMode.GLIDING)
                {
                    ResultingStaminaDrainHP += (GlideStaminaRate) * deltaTime;
                }
            }

            if (ResultingStaminaDrainHP > 0.0f)
            {
                Mathf.Clamp(ResultingStaminaDrainHP, 0.0f, StaminaComponent.MaxHP);
                //StaminaComponent.RemoveHP(ResultingStaminaDrainHP * SprintEfficiencyModifier, this.gameObject, false, false, false);
                StaminaComponent.SetHP(StaminaComponent.CurrentHP - ResultingStaminaDrainHP, this.gameObject, false, false, false);
                if (MovementComponent)
                {
                    if (MovementComponent.CurrentMoveMode == BaseMoveMode.GLIDING)
                    {
                        if (StaminaComponent.CurrentHP <= 0.0f)
                        {
                            MovementComponent.SetMoveMode(BaseMoveMode.GROUND);
                        }
                    }
                    else if (MovementComponent.CurrentMoveMode == BaseMoveMode.SWIMMING) //  && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tutorial")
                    {
                        if (StaminaComponent.CurrentHP <= 0.0f)
                        {
                            HandleDrowning(deltaTime);
                            return;
                        }
                    }
                }
            }

            if (_currentlyDrowning)
            {
                _drowningNotificationTime = 0.0f;
                _currentlyDrowning = false;
            }
        }
    }

    private void HandleDrowning(float deltaTime)
    {
        if (!_currentlyDrowning)
        {
            _drowningNotificationTime = drowningNotificationTimeDifference;
            _drowningDamageTime = 1.0f;
            _currentlyDrowning = true;
        }

        if (HP.CurrentHP <= 0.0f
            && IsAlive())
        {
            Kill();
            return;
        }

        if (DamageReceiver)
        {
            _drowningDamageTime += deltaTime;

            if (_drowningDamageTime >= 1)
            {
                AppliedDamageData appliedDamageData = DamageReceiver.CreateAppliedDamage(
                    false, healthLostPerSecondDrowning, this.gameObject, BaseDamageType.DROWNING, true);
                appliedDamageData.playEffects = true;
                DamageReceiver.ApplyDamage(appliedDamageData);
                _drowningDamageTime = 0.0f;
            }
        }

        _drowningNotificationTime += deltaTime;
        if (_drowningNotificationTime > drowningNotificationTimeDifference)
        {
            _drowningNotificationTime = 0.0f;
            ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("Drowning...", this.transform);
        }
    }

    public override void SyncPlayerController(BasePlayerController OldPlayerController, BasePlayerController InPlayerController)
    {
        base.SyncPlayerController(OldPlayerController, InPlayerController);

        if (InPlayerController)
        {
            if (OldPlayerController)
            {
                OldPlayerController.UsernameChangedDelegate -= HandleUsernameChanged;
                OldPlayerController.CharacterNameChangedDelegate -= HandleCharacterNameChanged;
            }

            SetNameTextVisibility(InPlayerController != BaseGameInstance.Get.GetLocalPlayerController());

            InPlayerController.UsernameChangedDelegate -= HandleUsernameChanged;
            InPlayerController.UsernameChangedDelegate += HandleUsernameChanged;

            InPlayerController.CharacterNameChangedDelegate -= HandleCharacterNameChanged;
            InPlayerController.CharacterNameChangedDelegate += HandleCharacterNameChanged;

            HandleUsernameChanged("", InPlayerController.PlayerUsername);
            HandleCharacterNameChanged("", InPlayerController.CharacterUsername);

            if (CharacterMapLocation)
            {
                CharacterMapLocation.gameObject.SetActive(true);
            }
        }
        else
        {
            if (OldPlayerController)
            {
                SetNameTextVisibility(OldPlayerController != BaseGameInstance.Get.GetLocalPlayerController());

                HandleUsernameChanged("", OldPlayerController.PlayerUsername);
                HandleCharacterNameChanged("", OldPlayerController.CharacterUsername);

                OldPlayerController.UsernameChangedDelegate -= HandleUsernameChanged;
                OldPlayerController.CharacterNameChangedDelegate -= HandleCharacterNameChanged;
            }

            if (CharacterMapLocation)
            {
                CharacterMapLocation.gameObject.SetActive(false);
            }
        }
    }

    public HighlightPlus.HighlightEffect PlayerAllyHighlightEffect;

    public HighlightEffectSettings OGHighlightSettings = new HighlightEffectSettings();


    [Mirror.Command(ignoreAuthority = true)]
    private void SetHRPC_Command(BasePlayerController InPlayerController)
    {
        HRPC = (HRPlayerController)(InPlayerController);

        foreach (var skill in HRPC.SkillSystem.Skills)
        {
            if (skill.SkillTree)
            {
                skill.SkillTree.OverrideTargetObject = this.gameObject;
            }
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SetHRPCNull_Command()
    {
        HRPC = null;
    }

    private void HandleControllerAssigned_Hook(HRPlayerController prevController, HRPlayerController newController)
    {
        if (HRPC && HRPC.hasAuthority)
        {
            HRPhoneManager PhoneManager = ((HRGameInstance)BaseGameInstance.Get).PhoneManager;
            if (PhoneManager)
            {
                if (PhoneManager.PlayerEquipmentUI)
                {
                    if (!PhoneManager.PlayerEquipmentUI.OwningInventory)
                    {
                        if (InventoryManager?.PlayerEquipmentInventory)
                        {
                            PhoneManager.PlayerEquipmentUI.SetEquipmentComponent(EquipmentComponent != null ? EquipmentComponent : transform.GetComponentInChildren<BaseEquipmentComponent>());
                        }
                    }
                }

                PhoneManager.OnShowDelegate += HandlePhoneShown;
                PhoneManager.HidePhone();
            }

            if (PlayerAllyHighlightEffect)
            {
                PlayerAllyHighlightEffect.enabled = !HRPC.hasAuthority;
                OGHighlightSettings.bEnabled = PlayerAllyHighlightEffect.enabled;
            }

            if (PlayerCamera)
            {
                PlayerCamera.ClearNoMouseRequests();
            }

            HRPC.PlayerUI.gameObject.SetActive(true);
            HRPC.PlayerUI.SetupHRPlayerCharacter(this);

            // Add context menu bindings
            // WeaponManager.SwitchToPlacementContextDelegate += HandleInteractionContextAdd;
            // WeaponManager.SwitchToUsingContextDelegate += HandleInteractionContextAdd;

            // so bad
            // HandleInteractionContextAdd(WeaponManager.BlockItemContextInfo, true);

            ItemPlacingManager.PlaceItemContextDelegate += HandleInteractionContextAdd;
            //ItemPlacingManager.RotateItemContextDelegate += HandleInteractionContextAdd;

            WeaponManager.WeaponUseModeChangedDelegate += HandleWeaponUseModeChanged;

            // TEMP adding tab context menu
            //BaseInteractionContextData TabData = new BaseInteractionContextData()
            //{
            //    PrimaryBinding = new KeybindDescription()
            //    {
            //        actionId = RewiredConsts.Action.Inventory,
            //        axisRange = Rewired.AxisRange.Positive,
            //        controllerType = Rewired.ControllerType.Keyboard,
            //    },
            //    SecondaryBinding = KeybindDescription.Default,
            //    InteractionDescription = BaseInteractionContextData.StringReference.FromGlobalKey("inventory"),
            //    OverrideBindingDescription = BaseInteractionContextData.StringReference.FromRaw("TAB/I"),
            //    Priority = -1,
            //};
            //HandleInteractionContextAdd(TabData, true);

            // TEMP adding dash context menu
            BaseInteractionContextData JumpData = new BaseInteractionContextData()
            {
                PrimaryBinding = new KeybindDescription()
                {
                    actionId = RewiredConsts.Action.Jump,
                    axisRange = Rewired.AxisRange.Positive,
                    controllerType = Rewired.ControllerType.Keyboard,
                },
                SecondaryBinding = KeybindDescription.Default,
                InteractionDescription = BaseInteractionContextData.StringReference.FromGlobalKey("jump"),
                Priority = -1,
            };
            HandleInteractionContextAdd(JumpData, true);

            // TEMP adding dash context menu
            BaseInteractionContextData DashData = new BaseInteractionContextData()
            {
                PrimaryBinding = new KeybindDescription()
                {
                    actionId = RewiredConsts.Action.Dash,
                    axisRange = Rewired.AxisRange.Positive,
                    controllerType = Rewired.ControllerType.Keyboard,
                },
                SecondaryBinding = KeybindDescription.Default,
                InteractionDescription = BaseInteractionContextData.StringReference.FromGlobalKey("dash"),
                Priority = -1,
            };
            HandleInteractionContextAdd(DashData, true);

            /*BaseInteractionContextData CamData = new BaseInteractionContextData();
            CamData.ButtonName = "TAB";
            CamData.InteractionDescription = new PixelCrushers.StringField("Rotate Camera [Hold]");
            CamData.Priority = -1;
            HandleInteractionContextAdd(CamData, true);*/

            ApplyTalkAndRadioContext();

            if (InteractionManager)
            {
                InteractionManager.OnHoverDelegate += HandleInteractionHover;
            }

            if (hasAuthority)
            {
                if (AnimScript.RootMotionComponent)
                {
                    AnimScript.RootMotionComponent.enabled = true;
                }
            }

            if (PlayerInformationPrefab)
            {
                if (HRPC.hasAuthority)
                {
                    //SpawnOverheadPlayerInfo();
                }
                else
                {

                }
            }

            if (HRPCBeforeSpectate && HRPCBeforeSpectate == HRPC)
            {
                HRPCBeforeSpectate = null;
            }
        }
    }

    public override void OnPossess(BasePlayerController InPlayerController)
    {
        base.OnPossess(InPlayerController);

        HRPlayerController InHRPC = (HRPlayerController)(InPlayerController);

        SetHRPC(InHRPC);

        foreach (var skill in InHRPC.SkillSystem.Skills)
        {
            if (skill.SkillTree)
            {
                skill.SkillTree.OverrideTargetObject = this.gameObject;
            }
        }

        // Setup movement
        if (MovementComponent)
        {
            MovementComponent.SetupMovementComponent(InPlayerController);
        }

        if (InteractionManager)
        {
            InteractionManager.Initialize(PlayerCamera, this.gameObject);
        }

        if (InventoryManager)
        {
            InventoryManager.ContainerOpenDelegate += HandleContainerInteract;
            InventoryManager.ContainerCloseDelegate += HandleContainerClose;
            InventoryManager.MiscContainerOpenDelegate += HandleMiscContainerInteract;
            InventoryManager.MiscContainerCloseDelegate += HandleMiscContainerInteract;
            InventoryManager.InventoryIconDragDelegate += HandleInventoryDragIcon;
            InventoryManager.ToolTipShowHideDelegate += HandleInventoryToolTipShowHide;
        }

        if (DamageIndicator)
        {
            DamageIndicator.Initialize(HP);
            DamageIndicator.gameObject.SetActive(true);
        }

        if (HitmarkerManager)
        {
            HitmarkerManager.Initialize(this);
            HitmarkerManager.gameObject.SetActive(true);
        }

        if (ItemPlacingManager)
        {
            ItemPlacingManager.InitializeBaseItemPlacingManager(this, PlayerCamera);
            WeaponManager.InitializeBaseWeaponManager(ItemPlacingManager);
        }

        LlamaSoftware.UNET.Chat.ChatPlayer chatPlayer = InPlayerController.ChatPlayer;
        if (chatPlayer && chatPlayer.chatSystem)
        {
            chatPlayer.chatSystem.OnChatOpenedDelegate += HandleChatOpened;
            chatPlayer.chatSystem.OnChatClosedDelegate += HandleChatClosed;
        }

        PlayerCamera.gameObject.SetActive(true);

        if (InHRPC)
        {
            InHRPC.PlayerUI.gameObject.SetActive(true);
        }

        if (HRNetworkManager.Get.AllClientsAssigned)
        {
            HandleAllClientsAssigned();
        }
        else
        {
            HRNetworkManager.Get.AllClientsAssignedDelegate += HandleAllExistingPlayersGet;
        }
    }

    public override void OnPossess_AllClient()
    {
        base.OnPossess_AllClient();
    }

    void HandleAllExistingPlayersGet()
    {
        HRNetworkManager.Get.AllClientsAssignedDelegate -= HandleAllExistingPlayersGet;
        HandleAllClientsAssigned();
    }

    void HandleAllClientsAssigned()
    {
        if (HRNetworkManager.Get != null && HRNetworkManager.Get.AllServerPlayers != null)
        {
            for (int i = 0; i < HRNetworkManager.Get.AllServerPlayers.Count; i++)
            {
                HRPlayerController Player = HRNetworkManager.Get.AllServerPlayers[i];
            }
        }
        else
        {
            Debug.LogError("HandleAllPlayersServerValid somehow has a null network manager or AllServerPlayers array");
        }
    }

    public override void OnUnpossess_AllClients(BasePlayerController InPlayerController)
    {
        base.OnUnpossess_AllClients(InPlayerController);
    }

    public void SpawnOverheadPlayerInfo(ulong PlayerID, bool bTitle, Transform Parent)
    {
        DestroyOverheadPlayerInfo();

        var info = Instantiate(PlayerInformationPrefab.gameObject, Parent).GetComponent<HROverheadPlayerInformation>();
        var floating = info.GetComponent<HRFloatingUIComponent>();
        floating.WorldPosition = this.PlayerMesh.transform;

        info.Initialize(PlayerID, !bTitle);
        PlayerInformationInstance = info;
    }


    public void DestroyOverheadPlayerInfo()
    {
        if (PlayerInformationInstance != null)
        {
            Destroy(PlayerInformationInstance.gameObject);
        }
    }

    void HandleChatOpened(LlamaSoftware.UNET.Chat.ChatSystem chatSystem)
    {
        if (chatSystem == null) return;

        Canvas ChatCanvas = chatSystem.GetComponentInParent<Canvas>();
        if (ChatCanvas) ChatCanvas.enabled = true;

        UnityEngine.UI.GraphicRaycaster ChatRaycaster = chatSystem.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>();
        if (ChatRaycaster) ChatRaycaster.enabled = true;

        chatSystem.inputField.onSelect.AddListener(HandleChatInputFieldFocused);
        chatSystem.inputField.onDeselect.AddListener(HandleChatInputFieldUnfocused);
    }

    void HandleChatClosed(LlamaSoftware.UNET.Chat.ChatSystem chatSystem)
    {
        chatSystem.inputField.onSelect.RemoveListener(HandleChatInputFieldFocused);
        chatSystem.inputField.onDeselect.RemoveListener(HandleChatInputFieldUnfocused);

        Canvas ChatCanvas = chatSystem.GetComponentInParent<Canvas>();

        if (ChatCanvas != null)
        {
            ChatCanvas.enabled = false;
        }

        UnityEngine.UI.GraphicRaycaster ChatRaycaster = chatSystem.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>();

        if (ChatRaycaster != null)
        {
            ChatRaycaster.enabled = false;
        }
    }

    void HandleChatInputFieldFocused(string callString)
    {
        if (InputComponent)
        {
            PlayerCamera.AddNoMouseRequest(this.gameObject);
            InputComponent.SetEnabled(false);
        }
    }

    void HandleChatInputFieldUnfocused(string callString)
    {
        if (InputComponent)
        {
            PlayerCamera.RemoveNoMouseRequest(this.gameObject);
            InputComponent.SetEnabled(true);
        }
    }

    void HandlePhoneShown(HRPhoneManager PhoneManager, bool bShown)
    {
        if (PlayerCamera)
        {
            if (bShown)
            {
                PlayerCamera.AddNoMouseRequest(PhoneManager.gameObject);
            }
            else
            {
                PlayerCamera.RemoveNoMouseRequest(PhoneManager.gameObject);
            }
        }
    }

    void ApplyTalkAndRadioContext(bool apply = true)
    {
        //BaseInteractionContextData localPushToTalkData = new BaseInteractionContextData()
        //{
        //    PrimaryBinding = new KeybindDescription()
        //    {
        //        actionId = RewiredConsts.Action.LocalPushToTalk,
        //        axisRange = Rewired.AxisRange.Positive,
        //        controllerType = Rewired.ControllerType.Keyboard
        //    },
        //    SecondaryBinding = new KeybindDescription()
        //    {
        //        actionId = -1
        //    },
        //    InteractionDescription = BaseInteractionContextData.StringReference.FromGlobalKey("localvoice"),
        //    Priority = -1,
        //};
        //HandleInteractionContextAdd(localPushToTalkData, apply);

        //BaseInteractionContextData globalPushToTalkData = new BaseInteractionContextData()
        //{
        //    PrimaryBinding = new KeybindDescription()
        //    {
        //        actionId = RewiredConsts.Action.GlobalPushToTalk,
        //        axisRange = Rewired.AxisRange.Positive,
        //        controllerType = Rewired.ControllerType.Keyboard
        //    },
        //    SecondaryBinding = new KeybindDescription()
        //    {
        //        actionId = -1
        //    },
        //    InteractionDescription = BaseInteractionContextData.StringReference.FromGlobalKey("globalvoice"),
        //    Priority = -1,
        //};
        //HandleInteractionContextAdd(globalPushToTalkData, apply);
    }

    void HandleInteractionHover(BaseInteractionManager InInteractionManager, BaseInteractable InInteractable, bool bStartHover)
    {
        // TEMP adding dash context menu
        //BaseInteractionContextData DashData = new BaseInteractionContextData();
        //DashData.ButtonName = "RMB";
        //DashData.InputToBindTo = "";
        //DashData.Priority = 13;

        //BaseWeapon WeaponHovered = InInteractable.GetComponentInParent<BaseWeapon>();
        //if (WeaponHovered && bStartHover)
        //{
        //    if (WeaponHovered.LockedItemRef != null && (WeaponHovered.LockedItemRef.IsLocked() || WeaponHovered.ItemOwnerID != ""))
        //    {
        //        DashData.InteractionDescription = new PixelCrushers.StringField("STEAL Item [Hold]");
        //    }
        //    else
        //    {
        //        DashData.InteractionDescription = new PixelCrushers.StringField("Pick Up Item [Hold]");
        //    }
        //}
        //else
        //{
        //    // TODO: Add hovered context for other various interactables (Ex: Emote)
        //    HandleInteractionContextAdd(DashData, false);
        //}
        //HandleInteractionContextAdd(DashData, bStartHover);
    }

    void HandleWeaponUseModeChanged(BaseWeaponManager InManager, BaseWeaponManager.WeaponUseMode OldUseMode, BaseWeaponManager.WeaponUseMode NewUseMode)
    {
        if (InteractionManager)
        {
            InteractionManager.bIsPlacingMode = NewUseMode == BaseWeaponManager.WeaponUseMode.PLACING;
        }

        if (OldUseMode != NewUseMode)
        {
            //if (!bLightControlMode)
            AddInteractionContext(RotateItemContextInfo, NewUseMode == BaseWeaponManager.WeaponUseMode.PLACING);
        }
    }

    public void AddInteractionContext(BaseInteractionContextData data, bool add)
    {
        HandleInteractionContextAdd(data, add);
    }
    void HandleInteractionContextAdd(BaseInteractionContextData InData, bool bAdd)
    {
        if (!HRPC || !HRPC.PlayerUI || !HRPC.PlayerUI.ContextMenuUI)
        {
            return;
        }

        if (bAdd)
        {
            HRPC.PlayerUI.ContextMenuUI.AddInteractionMenuItem(InData);
        }
        else
        {
            HRPC.PlayerUI.ContextMenuUI.RemoveInteractionMenuItem(InData);
        }
    }

    public override void OnUnpossess(BasePlayerController InPlayerController)
    {
        base.OnUnpossess(InPlayerController);

        InPlayerController.UsernameChangedDelegate -= HandleUsernameChanged;

        HandleBlock(false);

        if (HRPC)
        {
            HRPC.PlayerUI.gameObject.SetActive(false);

            // Remove context menu bindings
            //WeaponManager.SwitchToPlacementContextDelegate -= HandleInteractionContextAdd;
            //WeaponManager.SwitchToUsingContextDelegate -= HandleInteractionContextAdd;

            ItemPlacingManager.PlaceItemContextDelegate -= HandleInteractionContextAdd;

            HRPC.PlayerUI.ContextMenuUI.ClearMenu();

            if (hasAuthority)
            {
                if (AnimScript && AnimScript.RootMotionComponent)
                    AnimScript.RootMotionComponent.enabled = false;
            }

            HRPhoneManager PhoneManager = ((HRGameInstance)BaseGameInstance.Get).PhoneManager;
            if (PhoneManager)
            {
                PhoneManager.OnShowDelegate -= HandlePhoneShown;
            }
        }

        SetHRPC(null);

        if (InteractionManager)
        {
            InteractionManager.OnHoverDelegate -= HandleInteractionHover;
        }

        if (InventoryManager)
        {
            InventoryManager.ContainerOpenDelegate -= HandleContainerInteract;
            InventoryManager.ContainerCloseDelegate -= HandleContainerInteract;
            InventoryManager.MiscContainerOpenDelegate -= HandleMiscContainerInteract;
            InventoryManager.MiscContainerCloseDelegate -= HandleMiscContainerInteract;
            InventoryManager.InventoryIconDragDelegate -= HandleInventoryDragIcon;
            InventoryManager.ToolTipShowHideDelegate -= HandleInventoryToolTipShowHide;
        }

        if (ItemPlacingManager)
        {
            ItemPlacingManager.InitializeBaseItemPlacingManager(null, null);
        }

        if (DamageIndicator)
        {
            DamageIndicator.Unbind();
            DamageIndicator.gameObject.SetActive(false);
        }

        if (HitmarkerManager)
        {
            HitmarkerManager.Unbind();
            HitmarkerManager.gameObject.SetActive(false);
        }

        InteractionManager.SetInteractionEnabled(false);

        LlamaSoftware.UNET.Chat.ChatPlayer chatPlayer = InPlayerController.ChatPlayer;
        if (chatPlayer && chatPlayer.chatSystem)
        {
            chatPlayer.chatSystem.OnChatOpenedDelegate -= HandleChatOpened;
            chatPlayer.chatSystem.OnChatClosedDelegate -= HandleChatClosed;

            chatPlayer.chatSystem.inputField.onSelect.RemoveListener(HandleChatInputFieldFocused);
            chatPlayer.chatSystem.inputField.onDeselect.RemoveListener(HandleChatInputFieldUnfocused);
        }
    }

    private void SetHRPC(HRPlayerController playerController)
    {
        if (HRNetworkManager.IsHost())
        {
            HRPC = playerController;
        }
        else if (playerController)
        {
            SetHRPC_Command(playerController);
        }
        else
        {
            // Can't pass null as parameter in command function so need to use this dedicated command instead
            SetHRPCNull_Command();
        }
    }

    // Setup and bind inputs.
    public override void SetupInputComponent(BaseInputComponent inInputComponent)
    {
        base.SetupInputComponent(inInputComponent);

        if (InputComponent)
        {
            UnbindInputComponent();
        }

        HeroInputComponent CastedInputComponent = (HeroInputComponent)(InputComponent);

        if (LockPickingMinigameSystem)
        {
            LockPickingMinigameSystem.PreStartMinigame(this);
        }

        this.HeroHolsterController.ApplyInputEvents();

        if (CastedInputComponent)
        {
            CastedInputComponent.HorizontalMovementDelegate += HorizontalAxis;
            CastedInputComponent.VerticalMovementDelegate += VerticalAxis;

            CastedInputComponent.InteractButtonDelegate += Interact;
            CastedInputComponent.InteractBeamDelegate += InteractBeam;

            CastedInputComponent.DropButtonDelegate += HandleCycleWeaponMode;

            BindMouseInput();

            CastedInputComponent.ItemSlotSelectDelegate += ItemSlotSelectedEvent;

            if (!bInventoryBound)
            {
                CastedInputComponent.InventoryButtonDelegate += InventoryPressedEvent;
                bInventoryBound = true;
            }

            CastedInputComponent.WalkButtonDelegate += HandleWalk;
            CastedInputComponent.SprintButtonDelegate += HandleSprint;
            CastedInputComponent.ThrowButtonDelegate += HandleThrow;
            CastedInputComponent.BlockButtonDelegate += HandleBlock;

            CastedInputComponent.RotateClockwiseButtonDelegate += HandleRotateClockwise;
            CastedInputComponent.RotateCounterClockwiseButtonDelegate += HandleRotateCounterClockwise;

            CastedInputComponent.DashButtonDelegate += HandleDash;
            CastedInputComponent.JumpButtonDelegate += HandleJump;

            CastedInputComponent.CrouchButtonDelegate += HandleCrouch;

            CastedInputComponent.ShowInfoButtonDelegate += HandleShowInfo;

            CastedInputComponent.SpecialButtonDelegate += HandleSpecial;
            CastedInputComponent.GestureButtonDelegate += HandleGesture;

            CastedInputComponent.PauseButtonDelegate += HandlePauseButtonPressed;
            CastedInputComponent.MapButtonDelegate += HandleMapButtonPressed;

            CastedInputComponent.TalkButtonDelegate += HandleTalkButtonPressed;
            CastedInputComponent.RadioButtonDelegate += HandleRadioButtonPressed;

            CastedInputComponent.EscapeButtonDelegate += HandleEscape;

            CastedInputComponent.MouseHorizontalMovementDelegate += PlayerCamera.SetAimX;
            CastedInputComponent.MouseVerticalMovementDelegate += PlayerCamera.SetAimY;

            AnimScript.FootstepFX.FootstepFXDelegate += HandleFootstepFXLocal;

            DashComponent.StopCharging();

            MovementFX.SetActive(true);

            MovementComponent.OnLastGroundedColliderChanged += HandleLastGroundedColliderChanged_Local;

            if (DynamicEffects)
            {
                DynamicEffects.gameObject.SetActive(true);
                DynamicEffects.CaptureSize = 5.0f;
                DynamicEffects.transform.SetParent(null, true);
                DynamicEffects.transform.rotation = Quaternion.identity;
            }

            if (PlayerCamera)
            {
                PlayerCamera.OnZoomTickDelegate += HandleZoomTick;
            }
        }

        UpdateHealthBarVisibility();
    }

    void HandleZoomTick(float InZoomLevel)
    {
        if (InZoomLevel < 0.2f)
        {
            PlayerMesh?.PlayerRig?.SetVisibility(false, true);
            MovementComponent.SetFaceCameraNeedsVelocity(false);
        }
        else
        {
            PlayerMesh?.PlayerRig?.SetVisibility(true, true);
            MovementComponent.SetFaceCameraNeedsVelocity(true);
        }
    }

    public void SetFirstPersonMode(bool bEnabled)
    {
        if (PlayerMesh && PlayerMesh.PlayerRig)
        {
            PlayerMesh.PlayerRig.SetVisibility(!bEnabled, true);
        }

        MovementComponent.SetFaceCameraNeedsVelocity(!bEnabled);
    }

    void HandleLastGroundedColliderChanged_Local(BaseMovementComponent InMovementComponent, Collider OldCollider, Collider NewCollider)
    {
        // Check to see if we are on a shop ground.
        if (LastGroundedCollider != NewCollider)
        {
            LastGroundedCollider = NewCollider;
        }
    }

    public void HandleMoveModeChanged(BaseMovementComponent InMovementComponent, BaseMoveMode OldMoveMode, BaseMoveMode NewMoveMode)
    {
        if (StaminaComponent)
        {
            if (OldMoveMode == BaseMoveMode.SWIMMING || NewMoveMode == BaseMoveMode.SWIMMING)
            {
                if (NewMoveMode == BaseMoveMode.SWIMMING)
                {
                    StaminaComponent.SetShouldRegen(false);
                }
                else if (OldMoveMode == BaseMoveMode.SWIMMING && NewMoveMode != BaseMoveMode.SWIMMING)
                {
                    StaminaComponent.SetShouldRegen(true);
                }
            }

            if(bShouldUseGrounder)
            {
                if (NewMoveMode == BaseMoveMode.ZIPLINING || NewMoveMode == BaseMoveMode.SWIMMING)
                {
                    AnimScript?.IKManager?.SetBipedIKEnabled(false, true);
                }
                else
                {
                    AnimScript?.IKManager?.SetBipedIKEnabled(true, true);
                }
            }
        }
    }

    public void BindMouseInput()
    {
        HeroInputComponent CastedInputComponent = (HeroInputComponent)InputComponent;

        if (CastedInputComponent)
        {
            CastedInputComponent.PrimaryMouseDelegate += PrimaryMouseEvent;
            CastedInputComponent.PrimaryMouseUIDelegate += PrimaryMouseEventUI;
            CastedInputComponent.SecondaryMouseDelegate += SecondaryMouseEvent;

            CastedInputComponent.SecondaryMouseUIDelegate += SecondaryMouseEventUI;

            CastedInputComponent.MouseWheelDownDelegate += HandleMouseWheelDown;
            CastedInputComponent.MouseWheelUpDelegate += HandleMouseWheelUp;
        }
    }


    public void UnbindMouseInput()
    {
        HeroInputComponent CastedInputComponent = (HeroInputComponent)InputComponent;

        if (CastedInputComponent)
        {
            CastedInputComponent.PrimaryMouseDelegate -= PrimaryMouseEvent;
            CastedInputComponent.PrimaryMouseUIDelegate -= PrimaryMouseEventUI;
            CastedInputComponent.SecondaryMouseDelegate -= SecondaryMouseEvent;

            CastedInputComponent.SecondaryMouseUIDelegate -= SecondaryMouseEventUI;

            CastedInputComponent.MouseWheelDownDelegate -= HandleMouseWheelDown;
            CastedInputComponent.MouseWheelUpDelegate -= HandleMouseWheelUp;
        }
    }

    // Setup and bind inputs.
    public override void UnbindInputComponent()
    {
        //base.UnbindInputComponent();
        HeroInputComponent CastedInputComponent = (HeroInputComponent)InputComponent;

        this.HeroHolsterController.UnApplyInputEvents();

        if (CastedInputComponent)
        {
            CastedInputComponent.HorizontalMovementDelegate -= HorizontalAxis;
            CastedInputComponent.VerticalMovementDelegate -= VerticalAxis;

            CastedInputComponent.InteractButtonDelegate -= Interact;
            CastedInputComponent.InteractBeamDelegate -= InteractBeam;

            CastedInputComponent.DropButtonDelegate -= HandleCycleWeaponMode;

            UnbindMouseInput();

            CastedInputComponent.ItemSlotSelectDelegate -= ItemSlotSelectedEvent;

            if (bInventoryBound)
            {
                CastedInputComponent.InventoryButtonDelegate -= InventoryPressedEvent;
                bInventoryBound = false;
            }

            CastedInputComponent.MouseWheelDownDelegate -= HandleMouseWheelDown;
            CastedInputComponent.MouseWheelUpDelegate -= HandleMouseWheelUp;

            CastedInputComponent.WalkButtonDelegate -= HandleWalk;
            CastedInputComponent.SprintButtonDelegate -= HandleSprint;
            CastedInputComponent.ThrowButtonDelegate -= HandleThrow;
            CastedInputComponent.BlockButtonDelegate -= HandleBlock;

            CastedInputComponent.RotateClockwiseButtonDelegate -= HandleRotateClockwise;
            CastedInputComponent.RotateCounterClockwiseButtonDelegate -= HandleRotateCounterClockwise;

            CastedInputComponent.DashButtonDelegate -= HandleDash;
            CastedInputComponent.JumpButtonDelegate -= HandleJump;

            CastedInputComponent.CrouchButtonDelegate -= HandleCrouch;

            CastedInputComponent.ShowInfoButtonDelegate -= HandleShowInfo;

            CastedInputComponent.SpecialButtonDelegate -= HandleSpecial;
            CastedInputComponent.GestureButtonDelegate -= HandleGesture;

            CastedInputComponent.PauseButtonDelegate -= HandlePauseButtonPressed;
            CastedInputComponent.MapButtonDelegate -= HandleMapButtonPressed;

            CastedInputComponent.TalkButtonDelegate -= HandleTalkButtonPressed;
            CastedInputComponent.RadioButtonDelegate -= HandleRadioButtonPressed;

            CastedInputComponent.EscapeButtonDelegate -= HandleEscape;

            AnimScript.FootstepFX.FootstepFXDelegate -= HandleFootstepFXLocal;

            CastedInputComponent.MouseHorizontalMovementDelegate -= PlayerCamera.SetAimX;
            CastedInputComponent.MouseVerticalMovementDelegate -= PlayerCamera.SetAimY;

            MovementComponent.OnLastGroundedColliderChanged -= HandleLastGroundedColliderChanged_Local;

            MovementFX.SetActive(false);

            if (DynamicEffects)
            {
                DynamicEffects.gameObject.SetActive(false);

                if (this.transform)
                {
                    DynamicEffects.transform.SetParent(this.transform, true);
                }
            }

            if (PlayerCamera)
            {
                PlayerCamera.OnZoomTickDelegate -= HandleZoomTick;
            }
        }

        if (MovementComponent)
        {
            MovementComponent.AddRightMovement(0);
            MovementComponent.AddForwardMovement(0);
            MovementComponent.ClearVelocity();
        }
    }

    public void SuspendMovement(bool Suspend)
    {
        if (Suspend)
        {
            UnbindInputComponent();
        }
        else
        {
            SetupInputComponent(InputComponent);
        }
    }

    public void SuspendWASD(bool Suspend)
    {
        HeroInputComponent CastedInputComponent = (HeroInputComponent)InputComponent;
        if (Suspend)
        {
            MovementComponent.ClearVelocity();
            CastedInputComponent.HorizontalMovementDelegate -= HorizontalAxis;
            CastedInputComponent.VerticalMovementDelegate -= VerticalAxis;
        }
        else
        {
            CastedInputComponent.HorizontalMovementDelegate += HorizontalAxis;
            CastedInputComponent.VerticalMovementDelegate += VerticalAxis;
        }
    }

    public void SuspendInventory(bool Suspend)
    {
        HeroInputComponent CastedInputComponent = (HeroInputComponent)InputComponent;
        if (Suspend)
        {
            if (bInventoryBound)
            {
                CastedInputComponent.InventoryButtonDelegate -= InventoryPressedEvent;
                bInventoryBound = false;
            }

            if (((HRGameInstance)BaseGameInstance.Get).PhoneManager.bIsShown)
            {
                ((HRGameInstance)BaseGameInstance.Get).PhoneManager.HidePhone();
            }
        }
        else
        {
            if (!bInventoryBound)
            {
                CastedInputComponent.InventoryButtonDelegate += InventoryPressedEvent;
                bInventoryBound = true;
            }
        }
    }

    private bool bPrimaryMousePressed;
    protected void PrimaryMouseEvent(bool bPressed)
    {
        if (CurrentInteractState == InteractState.Free)
        {
            if (WeaponManager.CurrentUseMode == BaseWeaponManager.WeaponUseMode.PLACING &&
                !(WeaponManager.CurrentWeapon && WeaponManager.CurrentWeapon.WeaponMeleeComponent &&
                (WeaponManager.CurrentWeapon.WeaponMeleeComponent.GetIsChargingHeavy() ||
                WeaponManager.CurrentWeapon.WeaponMeleeComponent.GetIsSwinging())) &&
                !(HRGameManager.Get as HRGameManager).InvasionManager.IsInvader(this))
            {
                if (bPressed)
                {
                    if (!bPrimaryMousePressed && InteractionManager.ClickableInteractableInRange)
                    {
                        BaseWeapon pickupWeapon = InteractionManager.ClickableInteractableInRange.OwningWeapon;
                        WeaponManager.ItemPlacingManager.DragPickup(pickupWeapon);
                    }
                }
                else if (bPrimaryMousePressed)
                {
                    WeaponManager.ItemPlacingManager.DragDrop();
                }
            }

            WeaponManager.PrimaryInteract(bPressed);
        }

        bPrimaryMousePressed = bPressed;

        /*if (bPressed)
        {
            // Try to interact with the inventory
            InventoryManager.TryInteractWithSlot();
        }
        else
        {

        }*/

    }

    protected void PrimaryMouseEventUI(bool bPressed)
    {
        if (bPressed)
        {
            // Try to interact with the inventory
            InventoryManager.TryInteractWithSlot(true);
        }
        else
        {

        }
    }

    protected void SecondaryMouseEvent(bool Pressed)
    {
        if (!bIsPaused)
        {
            if (!(WeaponManager.CurrentWeapon && WeaponManager.CurrentWeapon.WeaponMeleeComponent &&
                (WeaponManager.CurrentWeapon.WeaponMeleeComponent.GetIsChargingHeavy() ||
                WeaponManager.CurrentWeapon.WeaponMeleeComponent.GetIsSwinging())))
            {
                if (Pressed)
                {
                    // Don't do it if you are looking at the UI.
                    if (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject())
                    {
                        InteractionManager.OnInteractMousePressed();
                    }
                }
                else
                {
                    InteractionManager.OnInteractMouseReleased();
                }
            }
        }
    }

    protected void SecondaryMouseEventUI(bool Pressed)
    {
        if (Pressed)
        {
            // Try to interact with the inventory
            InventoryManager.TryInteractWithSlot(false);
        }
    }


    // Horizontal movement. Pass to character movement.
    protected void HorizontalAxis(float AxisAmount)
    {
        if (MovementComponent)
        {
            MovementComponent.AddRightMovement(AxisAmount);
            if (AxisAmount != 0 && CurrentSeat) 
            {
                if (CurrentSeat.bMoveToUnseat)
                {
                    CurrentSeat.Unseat(false, true, false);
                }
            }
        }
    }

    // Vertical movement. Pass to character movement.
    protected virtual void VerticalAxis(float AxisAmount)
    {
        if (MovementComponent)
        {
            MovementComponent.AddForwardMovement(AxisAmount);
            if (AxisAmount != 0 && CurrentSeat)
            {
                if (CurrentSeat.bMoveToUnseat)
                {
                    CurrentSeat.Unseat(false, true, false);
                }
            }
        }
    }

    protected virtual void Interact(bool IsButtonDown)
    {
        if (HRGameManager.Get && (HRGameManager.Get as HRGameManager).InvasionManager && (HRGameManager.Get as HRGameManager).InvasionManager.IsInvader(this.gameObject))
        {
            if (InteractionManager.ClickableInteractableInRange)
            {
                var weapon = InteractionManager.ClickableInteractableInRange.gameObject.GetComponentInParent<BaseWeapon>();

                if (weapon && !weapon.bClientItem && weapon.bInShopPlot)
                {
                    return;
                }
            }
            else if (InteractionManager.LastInteractable)
            {
                var weapon = InteractionManager.LastInteractable.gameObject.GetComponentInParent<BaseWeapon>();

                if (weapon && !weapon.bClientItem && weapon.bInShopPlot)
                {
                    return;
                }
            }
        }

        if (IsButtonDown)
        {
            // If interact button is pressed, we interact with it.
            InteractionManager.OnInteractButtonPressed();
        }
        else
        {
            InteractionManager.OnInteractButtonReleased();
        }
    }

    public virtual void InteractBeam(bool IsButtonDown)
    {
        if (IsButtonDown)
            InteractionManager.OnInteractMousePressed();
        else
            InteractionManager.OnInteractMouseReleased();
    }

    protected virtual void Drop(bool IsButtonDown)
    {
        //if (WeaponManager == null)
        //{
        //    return;
        //}

        //if (IsButtonDown)
        //{
        //    WeaponManager.DropCurrentWeapon();
        //}
        //else
        //{

        //}
    }

    protected virtual void ItemSlotSelectedEvent(int ItemSlot, bool IsButtonDown)
    {
        if (WeaponManager && IsButtonDown && bCanSwitchWeapons)
        {
            WeaponManager.KeyCodeSlotSelected(ItemSlot);
        }
    }

    protected virtual void InventoryPressedEvent(bool IsButtonDown)
    {
        if (!IsButtonDown || !HRPC) return;
        bool bShouldOpen = !HRPC.PlayerUI.bInventoryActive;

        if ((HRGameInstance.Get as HRGameInstance).DefaultCraftingUI &&
            (HRGameInstance.Get as HRGameInstance).DefaultCraftingUI.CurrentCraftingComponent &&
            (HRGameInstance.Get as HRGameInstance).DefaultCraftingUI.CurrentCraftingComponent.InUse)
        {
            (HRGameInstance.Get as HRGameInstance).DefaultCraftingUI.CurrentCraftingComponent.HideUI(false);
        }

        if ((HRGameInstance.Get as HRGameInstance).DefaultBuildingUI &&
            (HRGameInstance.Get as HRGameInstance).DefaultBuildingUI.BuildingUI &&
            (HRGameInstance.Get as HRGameInstance).DefaultBuildingUI.BuildingUI.CurrentCraftingComponent &&
            (HRGameInstance.Get as HRGameInstance).DefaultBuildingUI.BuildingUI.CurrentCraftingComponent.InUse)
        {
            (HRGameInstance.Get as HRGameInstance).DefaultBuildingUI.BuildingUI.CurrentCraftingComponent.HideUI(false);
        }

        if (((HRGameInstance)BaseGameInstance.Get).PhoneManager && !((HRGameInstance)BaseGameInstance.Get).PhoneManager.bIsShown && HRPC)
        {
            WeaponManager.PrimaryInteract(false);
            HandleBlock(false);

            HRPC.PlayerUI.ToggleInventoryUI(bShouldOpen);
        }

        if (bShouldOpen)
        {
            //Close open toggle
            if (BaseToggleUIManager.OpenToggles != null && BaseToggleUIManager.OpenToggles.Count > 0)
            {
                BaseToggleUIManager.OpenToggles.RemoveAll((t) => t == null);

                if (BaseToggleUIManager.OpenToggles.Count > 0)
                {
                    var current = BaseToggleUIManager.OpenToggles[BaseToggleUIManager.OpenToggles.Count - 1];
                    current.UIManuallyClosed();

                    if (BaseToggleUIManager.OpenToggles.Contains(current))
                    {
                        BaseToggleUIManager.OpenToggles.Remove(current);
                    }
                }
            }
        }
    }

    public bool IsAlive()
    {
        return IsAlive(_localInteractState);
    }

    public static bool IsAlive(InteractState interactState)
    {
        return interactState != InteractState.Dead
            && interactState != InteractState.Dying;
    }

    public bool CanMove()
    {
        return CanMove(_localInteractState);
    }

    public static bool CanMove(InteractState interactState)
    {
        return interactState != InteractState.Stunned
           && interactState != InteractState.Sleeping
           && interactState != InteractState.Dying
           && interactState != InteractState.Dead
           && interactState != InteractState.Talking;
    }

    public bool IsUnconscious()
    {
        return IsUnconscious(_localInteractState);
    }

    public static bool IsUnconscious(InteractState interactState)
    {
        return interactState == InteractState.Sleeping
            || interactState == InteractState.Dying
            || interactState == InteractState.Dead;
    }

    void HandleWalk(bool bPressed)
    {
        if (!CanMove())
        {
            return;
        }
        if (bPressed)
        {
            MovementComponent.SetMoveSpeed(BaseMoveType.WALKING);
        }
        else
        {
            MovementComponent.SetMoveSpeed(BaseMoveType.JOGGING);
        }
    }
    //after parry, reset block timer cooldown and give the option to instantly drop block

    public void ParriedResetCooldown()
    {
        SetLastTimeBlocked();
    }

    void HandleBlock(bool bPressed)
    {
        bIsBlockPressed = bPressed;

        // This is bad, but I am retroactively adding a block button as secondary interact.
        if (WeaponManager)
        {
            // This is a secondary interact now due to the 3rd parameter passed in. Super bad naming.
            WeaponManager.SecondaryInteract(bPressed);
        }
    }

    public bool bHoldToSprint = false;
    void HandleSprint(bool bPressed)
    {
        if (!CanMove())
        {
            return;
        }

        if (WeaponManager && WeaponManager.CurrentWeapon)
        {
            BaseWeaponBlocker WeaponBlocker = WeaponManager.CurrentWeapon.WeaponBlockerComponent;
            if (WeaponBlocker && WeaponBlocker.IsBlocking())
            {
                return;
            }
        }

        if (bHoldToSprint)
        {
            // If there is stamina, sprint
            if (bPressed)
            {
                if (StaminaComponent.CurrentHP > 0.0f)
                {
                    if (MovementComponent.CurrentMoveSpeed != BaseMoveType.SPRINTING && StaminaComponent.CurrentHP > 0.0f)
                    {
                        MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.SPRINTING);
                        if (CurrentSeat)
                        {
                            if (CurrentSeat.bMoveToUnseat)
                            {
                                CurrentSeat.Unseat(false, true, false);
                            }
                        }
                    }
                }
                else
                {
                    DisplayNoStaminaMessage();
                    MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
                }
            }
            else
            {
                MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
            }
        }
        else
        {
            // If there is stamina, sprint
            if (bPressed)
            {
                if (MovementComponent.CurrentMoveSpeed != BaseMoveType.SPRINTING)
                {
                    bMovedSinceSprintTapped = false;

                    if (StaminaComponent.CurrentHP > 0.0f)
                    {
                        if (MovementComponent.CurrentMoveSpeed != BaseMoveType.SPRINTING && StaminaComponent.CurrentHP > 0.0f)
                        {
                            MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.SPRINTING);
                        }
                    }
                    else
                    {
                        DisplayNoStaminaMessage();
                        MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
                    }
                }
                else
                {
                    MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
                }
            }
            else
            {
                if (MovementComponent.GetRawVelocity() == Vector3.zero)
                {
                    MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
                }
            }
        }
    }
    Animancer.AnimancerState throwState;

    public void HandleThrow(bool bPressed)
    {
        if (!CanMove() || !WeaponManager || !WeaponManager.CanThrow())
        {
            return;
        }

        if (bPressed && WeaponManager.bFinalThowing == false && WeaponManager.bIsThrowing == false)
        {
            //Debug.Log("Charge:" + WeaponManager.bFinalThowing + "|" + WeaponManager.bIsThrowing + "|" + Time.time);
            SetThrowMovementParams(true);

            WeaponManager.AttemptThrow();
            WeaponManager.bIsThrowing = true;

            //turn blocking off
            BaseWeaponBlocker WeaponBlocker = WeaponManager?.CurrentWeapon?.WeaponBlockerComponent;
            if (WeaponBlocker)
            {
                SetLastTimeBlocked();
                WeaponBlocker.SetBlocking(false);
            }


            BaseComboDataAsset comboData = null;

            comboData = ChargeThrowAnim(comboData);

            ThrowStartTime = comboData.throwStartTime;
            ThrowStartupTime = comboData.throwStartTime;

            if (ThrowStartTime == 0)
            {
                ThrowStartTime = 0.65f;
            }
            ThrowCooldown = comboData.throwCancelTime;
            if (ThrowCooldown == 0)
            {
                ThrowStartTime = 0.65f;
            }
            if (HRNetworkManager.IsHost())
            {
                ChargeThrowObject_ClientRpc(WeaponManager.CurrentWeapon);
            }
            else
            {
                ChargeThrowObject_Command(WeaponManager.CurrentWeapon);
            }
            SubscribeThrows_Implementation(true);
        }
        else if (!bPressed && WeaponManager.bFinalThowing == false && WeaponManager.bIsThrowing == true)
        {
            //Debug.Log("Throw:" + WeaponManager.bFinalThowing + "|" + WeaponManager.bIsThrowing + "|" + Time.time);
            WeaponManager.bFinalThowing = true;
            ThrowStartFX();

            float throwAnimSpeed = ThrowAnim();


            var throwDirection = transform.forward;

            if (this.gameObject.CompareTag("Player"))
            {
                if (bLockAngleOnStart)
                {
                    RaycastHit OutHit;
                    WeaponManager.RaycastMouseInput(0.1f, ItemPlacingManager.PlaceLayerMask, out OutHit);

                    if (OutHit.collider)
                    {
                        if (WeaponManager.WeaponAttachSocket)
                            throwDirection = (OutHit.point - WeaponManager.WeaponAttachSocket.transform.position).normalized;
                        else
                        {
                            throwDirection = (OutHit.point - WeaponManager.CurrentWeapon.transform.position).normalized;
                        }
                    }
                    else
                    {
                        if (PlayerCamera)
                        {
                            throwDirection = PlayerCamera.transform.forward;
                        }
                    }

                }
            }


            if (HRNetworkManager.IsHost())
            {
                ThrowObject_ClientRpc(WeaponManager.CurrentWeapon);
            }
            else
            {
                ThrowObject_Command(WeaponManager.CurrentWeapon);
            }

            if (ThrowRoutine != null)
            {
                StopCoroutine(ThrowRoutine);
            }

            float throwStrength = ThrowStrength * throwAnimSpeed;
            ThrowRoutine = ThrowWeaponRoutine(throwDirection, throwStrength);
            StartCoroutine(ThrowRoutine);
        }
    }

    public void ThrowStartFX()
    {
        PlayVoiceAudio("HeavyAttack");
    }

    public BaseComboDataAsset ChargeThrowAnim(BaseComboDataAsset comboData)
    {
        ClipState.Transition throwAnimToUse;

        if (WeaponManager.CurrentWeapon && WeaponManager.CurrentWeapon.WeaponMeleeComponent)
        {
            comboData = WeaponManager.CurrentWeapon.WeaponMeleeComponent.ComboAsset;
        }
        else
        {
            comboData = ((HRGameInstance)BaseGameInstance.Get).FallbackComboAssets
            [(int)WeaponManager.CurrentWeapon.ItemSizeAndType.ItemSize];
        }


        if (throwState != null)
        {
            StopAnimationState(throwState);
            //AnimScript.StopAnimation(throwAnimToUse);
        }

        throwAnimToUse = comboData.StartThrowingCharge;
        HRAnimLayers throwLayer = HRAnimLayers.UPPERBODY; // Use base and upper because feet not moving is not satisfying
        if (throwAnimToUse.Clip == null)
        {
            throwAnimToUse = comboData.HeavyAttackChargeAnimation;
        }
        throwState = AnimScript.PlayAnimation(throwAnimToUse, 0.5f, throwLayer, true);

        return comboData;
    }

    public float ThrowAnim()
    {
        if (throwState != null)
        {
            StopAnimationState(throwState);
            //AnimScript.StopAnimation(throwAnimToUse);
        }

        BaseComboDataAsset comboData;
        if (WeaponManager.CurrentWeapon && WeaponManager.CurrentWeapon.WeaponMeleeComponent)
        {
            comboData = WeaponManager.CurrentWeapon.WeaponMeleeComponent.ComboAsset;
        }
        else
        {
            comboData = ((HRGameInstance)BaseGameInstance.Get).FallbackComboAssets
            [(int)WeaponManager.CurrentWeapon.ItemSizeAndType.ItemSize];
        }
        ClipState.Transition throwAnimToUse = comboData.ThrowingAttack;
        HRAnimLayers throwLayer = HRAnimLayers.BASEANDUPPER; // Use base and upper because feet not moving is not satisfying
        if (throwAnimToUse.Clip == null)
        {
            throwAnimToUse = comboData.HeavyAttack.AttackAnimation;
        }
        float throwAnimSpeed = (ThrowStartupTime > 0 ? 1 : 1.25f);
        ThrowStartTime = (ThrowStartTime / throwAnimSpeed); // Start time is based on speed of animation. Slower if tap thrown
        throwState = AnimScript.PlayAnimation(throwAnimToUse, throwAnimToUse.FadeDuration, .5f, throwLayer, false, null, comboData.ThrowingAttack.Speed * throwAnimSpeed);
        return throwAnimSpeed;
    }

    //This function is used primarily for AI's in throwing their weapon
    public void DoThrow(bool bPressed, Transform targetObj)
    {
        if (!CanMove() || !WeaponManager.CanThrow() || WeaponManager.bIsThrowing)
        {
            return;
        }

        if (bPressed)
        {

        }
        else if (WeaponManager.bIsThrowing)
        {
            SetThrowMovementParams(true);

            WeaponManager.AttemptThrow();
            WeaponManager.bIsThrowing = true;


            ClipState.Transition throwAnimToUse;
            // TODO: Use a throw animation, should use a similar system to attack
            BaseComboDataAsset comboData;

            if (WeaponManager.CurrentWeapon && WeaponManager.CurrentWeapon.WeaponMeleeComponent)
            {
                comboData = WeaponManager.CurrentWeapon.WeaponMeleeComponent.ComboAsset;
            }
            else
            {
                comboData = ((HRGameInstance)BaseGameInstance.Get).FallbackComboAssets
                [(int)WeaponManager.CurrentWeapon.ItemSizeAndType.ItemSize];
            }


            if (throwState != null)
            {
                StopAnimationState(throwState);
                //AnimScript.StopAnimation(throwAnimToUse);
            }


            throwAnimToUse = comboData.ThrowingAttack;
            HRAnimLayers throwLayer = comboData.ThrowingLayer;
            if (throwAnimToUse.Clip == null)
            {
                throwAnimToUse = comboData.Attacks[0].LightAttack.AttackAnimation;
            }

            throwState = AnimScript.PlayAnimation(throwAnimToUse, 0.5f, throwLayer, false);
            ThrowStartFX();

            ThrowStartTime = comboData.throwStartTime;

            if (ThrowStartTime == 0)
            {
                ThrowStartTime = 0.65f;
            }
            ThrowCooldown = comboData.throwCancelTime;
            if (ThrowCooldown == 0)
            {
                ThrowStartTime = 0.65f;
            }

            SubscribeThrows_Implementation(true);

            if (HRNetworkManager.IsHost())
            {
                ThrowObject_ClientRpc(WeaponManager.CurrentWeapon);
            }
            else
            {
                ThrowObject_Command(WeaponManager.CurrentWeapon);
            }

            CurrentThrowTimer = 0.0f;

            float throwStrength = ThrowStrength * 1;
            ThrowRoutine = ThrowWeaponRoutineAtTransform(targetObj, throwStrength);
            StartCoroutine(ThrowRoutine);
        }
    }

    private float CurrentThrowTimer = 0.0f;

    private void SetThrowMovementParams(bool bThrowing)
    {
        if (bThrowing)
        {
            MovementComponent.SetFaceCameraNeedsVelocity(false);
            MovementComponent.SetMoveSpeed(BaseMoveType.JOGGING);
        }
        else
        {
            MovementComponent.SetFaceCameraNeedsVelocity(true);

            if (MovementComponent.CurrentMoveSpeed == BaseMoveType.WALKING)
                MovementComponent.SetMoveSpeed(BaseMoveType.JOGGING);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void ChargeThrowObject_Command(BaseWeapon Weapon)
    {
        ChargeThrowObject_ClientRpc(Weapon);
    }


    [Mirror.ClientRpc(excludeOwner = true)]
    private void ChargeThrowObject_ClientRpc(BaseWeapon Weapon)
    {
        if (!Weapon || !WeaponManager || !WeaponManager.CurrentWeapon)
        {
            // This weapon can be null if the thrower is far enough away, so don't do anything if it is
            return;
        }
        BaseComboDataAsset comboData = null;
        ChargeThrowAnim(comboData);

    }

    [Mirror.Command(ignoreAuthority = true)]
    private void ThrowObject_Command(BaseWeapon Weapon)
    {
        ThrowObject_ClientRpc(Weapon);
    }


    [Mirror.ClientRpc(excludeOwner = true)]
    private void ThrowObject_ClientRpc(BaseWeapon Weapon)
    {
        if (!Weapon || !WeaponManager || !WeaponManager.CurrentWeapon)
        {
            // This weapon can be null if the thrower is far enough away, so don't do anything if it is
            return;
        }
        ThrowAnim();
    }


    private void CancelThrow(BaseWeaponManager InManager, int OldSlot, int NewSlot)
    {
        if (OldSlot == NewSlot)
        {
            return;
        }

        if (ThrowRoutine != null)
        {
            StopCoroutine(ThrowRoutine);
        }

        if (InManager.HotKeyInventory.InventorySlots[OldSlot].SlotWeapon != null)
        {
            //var anim = ((HRGameInstance)BaseGameInstance.Get).FallbackComboAssets[(int)InManager.HotKeyInventory.InventorySlots[OldSlot].SlotWeapon.ItemSizeAndType.ItemSize];

            //AnimScript.StopAnimation(anim.Attacks[0].LightAttack.AttackAnimation);
            StopAnimationState(throwState);
            //AnimScript.StopAnimation(throwAnimToUse);
        }

        SetThrowMovementParams(false);
        WeaponManager.EndThrow();
        SubscribeThrows_Implementation(false);
    }

    private void CancelSprintThrow(BaseMovementComponent InMovementComponent, BaseMoveType NewMoveType)
    {
        if (NewMoveType != BaseMoveType.SPRINTING)
        {
            return;
        }

        if (ThrowRoutine != null)
        {
            StopCoroutine(ThrowRoutine);
        }

        StopAnimationState(throwState);
        //AnimScript.StopAnimation(throwAnimToUse);

        SetThrowMovementParams(false);
        WeaponManager.EndThrow();
        SubscribeThrows_Implementation(false);
    }

    private void CancelDashThrow(BaseMovementDash dashComp, bool bDash, bool bRecoveringFromKnockdown)
    {
        if (bDash)
        {
            if (ThrowRoutine != null)
            {
                StopCoroutine(ThrowRoutine);
            }

            StopAnimationState(throwState);
            //AnimScript.StopAnimation(throwAnimToUse);

            SetThrowMovementParams(false);
            WeaponManager.EndThrow();
            MovementComponent.SetMoveSpeed(BaseMoveType.JOGGING);
            SubscribeThrows_Implementation(false);
        }
    }
    private IEnumerator ThrowWeaponRoutine(Vector3 angle, float throwStrength)
    {
        //yield return new WaitForSeconds(ThrowStartupTime);
        yield return new WaitForSeconds(ThrowStartTime);

        if (WeaponManager)
        {
            if (bLockAngleOnStart)
            {
                WeaponManager.ThrowWeapon(this.gameObject, angle * throwStrength);
            }
            else
            {
                var throwDirection = transform.forward;

                if (ItemPlacingManager)
                {
                    RaycastHit OutHit;

                    WeaponManager.RaycastMouseInput(0.1f, ItemPlacingManager.PlaceLayerMask, out OutHit);

                    if (OutHit.collider)
                    {
                        Vector3 origin = WeaponManager.WeaponAttachSocket ? WeaponManager.WeaponAttachSocket.transform.position : WeaponManager.CurrentWeapon.transform.position;
                        Vector3 cameraToPlayer = origin - PlayerCamera.transform.position;
                        Vector3 playerToHit = OutHit.point - origin;

                        if (Vector3.Dot(cameraToPlayer, playerToHit) > 0.5f) // nothing we are aiming at should be less than 0.5f 
                        {
                            throwDirection = (OutHit.point - origin).normalized;
                        }
                        else if (PlayerCamera)
                        {
                            throwDirection = PlayerCamera.transform.forward;
                        }
                    }
                    else if (PlayerCamera)
                    {
                        throwDirection = PlayerCamera.transform.forward;
                    }
                }

                WeaponManager.ThrowWeapon(this.gameObject, throwDirection * throwStrength);
            }

            SetThrowMovementParams(false);

            yield return new WaitForSeconds(ThrowCooldown);
            ThrowCooldown = 0;

            WeaponManager.EndThrow();

            if (HRNetworkManager.IsHost())
            {
                SubscribeThrows_ClientRpc(false);
            }
            else
            {
                SubscribeThrows_Command(false);
            }
        }
    }

    private IEnumerator ThrowWeaponRoutineAtTransform(Transform targetObj, float throwStrength)
    {
        yield return new WaitForSeconds(ThrowStartTime);

        if (WeaponManager.WeaponAttachSocket)
        {
            Vector3 angleTowardsTarget;
            if (targetObj.TryGetComponent(out CharacterController controller))
            {
                angleTowardsTarget = controller.bounds.center - WeaponManager.WeaponAttachSocket.transform.position;
            }
            else
            {
                angleTowardsTarget = targetObj.position - WeaponManager.WeaponAttachSocket.transform.position;
            }

            WeaponManager.ThrowWeapon(this.gameObject, angleTowardsTarget.normalized * throwStrength);
        }


        SetThrowMovementParams(false);

        yield return new WaitForSeconds(ThrowCooldown);

        WeaponManager.EndThrow();

        if (HRNetworkManager.IsHost())
        {
            SubscribeThrows_ClientRpc(false);
        }
        else
        {
            SubscribeThrows_Command(false);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]

    private void SubscribeThrows_Command(bool bSubscribe)
    {
        SubscribeThrows_ClientRpc(bSubscribe);
    }

    [Mirror.ClientRpc(excludeOwner = true)]
    private void SubscribeThrows_ClientRpc(bool bSubscribe)
    {
        SubscribeThrows_Implementation(bSubscribe);
    }

    private void SubscribeThrows_Implementation(bool bSubscribe)
    {
        if (bSubscribe)
        {
            WeaponManager.HotkeySlotSelectedDelegate += CancelThrow;
            MovementComponent.OnSprintDelegate += CancelSprintThrow;
            DashComponent.OnDashDelegate += CancelDashThrow;
        }
        else
        {
            WeaponManager.HotkeySlotSelectedDelegate -= CancelThrow;
            MovementComponent.OnSprintDelegate -= CancelSprintThrow;
            DashComponent.OnDashDelegate -= CancelDashThrow;
        }
    }

    protected override void HandleInWaterChanged(bool prevInWater, bool newInWater)
    {
        if (AttributeManager)
        {
            //if (newInWater
            //    && !_currentWetAttribute)
            //{
            //    HRRawAttributeData attributeData = new HRRawAttributeData(
            //        8, true, true, "WetAttribute", "", 0.0f, -1.0f, 0.0f);
            //    _currentWetAttribute = AttributeManager.AddAttribute(attributeData);
            //}
            //else if (!newInWater
            //    && _currentWetAttribute)
            //{
            //    _currentWetAttribute.Duration = 5f;
            //    AttributeManager.RemoveAttribute(_currentWetAttribute);
            //    _currentWetAttribute = null;
            //}

            if (newInWater)
            {
                if (_currentWetAttribute)
                {
                    _currentWetAttribute.ResetDuration(-1f);
                }
                else
                {
                    HRRawAttributeData attributeData = new HRRawAttributeData(
                        HRWetAttribute.WetAttributeID, true, true, "WetAttribute", HRAttribute.EAttributeTriggers.None, 0f, -1f, 0f);
                    _currentWetAttribute = AttributeManager.AddAttribute(attributeData);
                }
            }
            else
            {
                if (_currentWetAttribute)
                {
                    _currentWetAttribute.ResetDuration(10f);
                }
            }
        }

        base.HandleInWaterChanged(prevInWater, newInWater);
    }

    public bool GetIsInWater()
    {
        return _currentWetAttribute != null;
    }

    void HandleMouseWheelUp(bool bPressed)
    {
        if (WeaponManager.ItemPlacingManager.bIsPlacing)
        {
            WeaponManager.ItemPlacingManager.RotateGhostGameObjectCounterClockwise();
        }
        else
        {
            PlayerCamera.ZoomInTick();
        }
    }

    void HandleMouseWheelDown(bool bPressed)
    {
        if (WeaponManager.ItemPlacingManager.bIsPlacing)
        {
            WeaponManager.ItemPlacingManager.RotateGhostGameObjectClockwise();
        }
        else
        {
            PlayerCamera.ZoomOutTick();
        }
    }

    void HandleRotateClockwise(bool bPressed)
    {
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            WeaponManager.ItemPlacingManager.RotateGhostGameObjectClockwise();
        }
        else
        {
            WeaponManager.ItemPlacingManager.RotateGhostGameObjectCounterClockwise();
        }
    }

    void HandleRotateCounterClockwise(bool bPressed)
    {
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            WeaponManager.ItemPlacingManager.RotateGhostGameObjectCounterClockwise();
        }
        else
        {
            WeaponManager.ItemPlacingManager.RotateGhostGameObjectClockwise();
        }
    }

    void HandleContainerClose(BaseContainer InContainer)
    {
        if (HRPC && HRPC.PlayerUI && HRPC.PlayerUI.ContainerInventoryUI)
        {
            HRPC.PlayerUI.ContainerInventoryUI.SetVisibility(false);
        }
    }

    void HandleContainerInteract(BaseContainer InContainer)
    {
        if (HRPC && HRPC.PlayerUI)
        {
            if (HRPC.PlayerUI.ContainerInventoryUI)
            {
                bool bWasContainerAlreadyOpen = HRPC.PlayerUI.ContainerInventoryUI.OwningInventory == InContainer.Inventory;
                //Debug.Log("@ InteractContainer:" + bWasContainerAlreadyOpen);

                HRPC.PlayerUI.SetupContainerUI(InContainer);

                if (!bWasContainerAlreadyOpen)
                {
                    HRPC.PlayerUI.ContainerInventoryUI.SetVisibility(true, true);
                }
                else
                {
                    HRPC.PlayerUI.ContainerInventoryUI.ToggleVisibility();
                }
            }
        }
    }

    void HandleMiscContainerInteract(BaseContainer InContainer)
    {
        HRGameInstance GameInstance = (HRGameInstance)(BaseGameInstance.Get);
        if (GameInstance)
        {
            if (GameInstance.MiscContainerUIManager)
            {
                if (GameInstance.MiscContainerUIManager.CurrentContainer != InContainer)
                {
                    GameInstance.MiscContainerUIManager.SetupUI(InContainer);
                    GameInstance.MiscContainerUIManager.SetVisibility(true, true);
                }
                else
                {
                    GameInstance.MiscContainerUIManager.ToggleVisibility();
                }
            }
        }
    }

    void HandleInventoryDragIcon(BaseInventorySlotUI InSlotUI, bool bVisible)
    {
        if (HRPC && HRPC.PlayerUI)
        {
            HRPC.PlayerUI.SetDragIconVisibility(bVisible);
        }
    }

    void HandleInventoryToolTipShowHide(BaseInventorySlotUI InSlotUI, bool bVisible)
    {
        HRItemDatabase itemDatabase = ((HRGameInstance)BaseGameInstance.Get).ItemDB;
        if (HRPC && HRPC.PlayerUI)
        {
            if (InSlotUI && InSlotUI.OwningInventory)
            {
                if (InSlotUI.CurrentWeapon && InSlotUI.bShowTooltipUI)
                {
                    if (bVisible)
                    {
                        if (InSlotUI.SlotUIType == BaseInventorySlotUIType.Storage || InSlotUI.SlotUIType == BaseInventorySlotUIType.ShopSelling)
                        {
                            //HRPC.PlayerUI.ToolTipUI.SetToolTipText(InSlotUI.CurrentWeapon.ItemName);
                            HRPC.PlayerUI.ToolTipUI.SetValueText(InSlotUI.CurrentWeapon.ItemValue.GetValue().ToString());

                            if (InSlotUI.CurrentWeapon.ItemID >= 0
                                && InSlotUI.CurrentWeapon.ItemID < itemDatabase.ItemArray.Length)
                            {
                                //HRPC.PlayerUI.ToolTipUI.SetToolTipDescriptionText(itemDatabase.ItemArray[InSlotUI.CurrentWeapon.ItemID].ItemDescription);
                            }
                        }
                        else if (InSlotUI.SlotUIType == BaseInventorySlotUIType.NPCShopSelling)
                        {
                            //HRPC.PlayerUI.SellingTipUI?.SetToolTipText(InSlotUI.CurrentWeapon.ItemName);
                            HRPC.PlayerUI.ToolTipUI.SetValueText(InSlotUI.CurrentWeapon.ItemValue.GetValue().ToString());

                            if (InSlotUI.CurrentWeapon.ItemID >= 0
                                && InSlotUI.CurrentWeapon.ItemID < itemDatabase.ItemArray.Length)
                            {
                                //HRPC.PlayerUI.SellingTipUI?.SetToolTipDescriptionText(itemDatabase.ItemArray[InSlotUI.CurrentWeapon.ItemID].ItemDescription);
                            }
                            HRPC.PlayerUI.SellingTipUI?.SetText(HRPC.PlayerUI.SellingTipUI?.PriceText, "<c=cash>$" + InSlotUI.CurrentWeapon.ItemValue.GetValue());
                        }
                        string AttributeString = "";

                        HRPC.PlayerUI.AttributeHoverUI.SetBackground((int)InSlotUI.CurrentWeapon.ItemRarity);

                        if (InSlotUI.CurrentWeapon.AttributeManager && HRPC.PlayerUI.AttributeHoverUI)
                        {
                            if (HRPC.WeaponManager.CurrentWeapon && HRPC.WeaponManager.CurrentWeapon.AttributeManager && HRPC.WeaponManager.CurrentWeapon != InSlotUI.CurrentWeapon)
                            {
                                HRPC.PlayerUI.AttributeHoverUI.RefreshAttributeHoverUIs(InSlotUI.CurrentWeapon.AttributeManager, HRPC.WeaponManager.CurrentWeapon.AttributeManager);
                            }
                            else
                            {
                                HRPC.PlayerUI.AttributeHoverUI.RefreshAttributeHoverUIs(InSlotUI.CurrentWeapon.AttributeManager, null);
                            }
                        }
                    }
                    else
                    {
                        if (HRPC.PlayerUI.AttributeHoverUI)
                        {
                            HRPC.PlayerUI.AttributeHoverUI.Clear();
                        }
                    }

                    if (InSlotUI.SlotUIType == BaseInventorySlotUIType.Storage || InSlotUI.SlotUIType == BaseInventorySlotUIType.ShopSelling)
                    {
                        HRPC.PlayerUI.ToolTipUI.SetToolTipActive(bVisible);
                    }
                    else if (InSlotUI.SlotUIType == BaseInventorySlotUIType.NPCShopSelling)
                    {
                        HRPC.PlayerUI.SellingTipUI?.SetToolTipActive(bVisible);
                    }
                }
                else
                {
                    HRPC.PlayerUI.ToolTipUI.SetToolTipActive(false);
                    HRPC.PlayerUI.SellingTipUI?.SetToolTipActive(false);
                    HRPC.PlayerUI.EncyclopediaToolTipUI?.SetToolTipActive(false);
                }

            }
            else
            {
                if (!InSlotUI)
                {
                    return;
                }

                HREncyclopediaItemUI EncyclopediaItemUI = InSlotUI.GetComponent<HREncyclopediaItemUI>();
                if (EncyclopediaItemUI)
                {
                    if (bVisible && InSlotUI.bShowTooltipUI)
                    {
                        string ItemName = EncyclopediaItemUI.EncyclopediaItemName;

                        var ItemIndex = (HRGameInstance.Get as HRGameInstance).ItemDB.GetItemIndexByName(ItemName);

                        if (ItemIndex != -1)
                        {
                            var ItemData = (HRGameInstance.Get as HRGameInstance).ItemDB.ItemArray[ItemIndex];

                            //HRPC.PlayerUI.EncyclopediaToolTipUI?.SetToolTipText(EncyclopediaItemUI.EncyclopediaItemName);
                            HRPC.PlayerUI.EncyclopediaToolTipUI?.SetText(HRPC.PlayerUI.EncyclopediaToolTipUI.DescriptionText,
                                HRItemDatabase.GetLocalizedItemName(ItemData.NameKey, ItemData.ItemName) + "\n\n<size=75%>" +
                                HRItemDatabase.GetLocalizedItemName(ItemData.DescriptionKey, ItemData.ItemDescription) + "</size>");
                            //HRPC.PlayerUI.EncyclopediaToolTipUI?.SetToolTipValueActive(false);
                            HRPC.PlayerUI.EncyclopediaToolTipUI?.SetText(HRPC.PlayerUI.EncyclopediaToolTipUI.ToolTipValueText,
                                ItemData.DefaultItemValue.ToString("N0"));
                        }

                        HRPC.PlayerUI.EncyclopediaToolTipUI?.SetToolTipActive(true);
                    }
                    else
                    {
                        HRPC.PlayerUI.EncyclopediaToolTipUI?.SetToolTipActive(false);
                    }
                }
                else
                {
                    HRRecipeBookItemUI RecipeBookItemUI = InSlotUI as HRRecipeBookItemUI;
                    if (RecipeBookItemUI)
                    {
                        if (bVisible && InSlotUI.bShowTooltipUI)
                        {
                            //HRPC.PlayerUI.EncyclopediaToolTipUI?.SetToolTipText(RecipeBookItemUI.ItemName);
                            HRPC.PlayerUI.EncyclopediaToolTipUI?.SetText(HRPC.PlayerUI.EncyclopediaToolTipUI.DescriptionText, RecipeBookItemUI.ItemDescription);
                            HRPC.PlayerUI.EncyclopediaToolTipUI?.SetToolTipActive(true);
                            //HRPC.PlayerUI.EncyclopediaToolTipUI?.SetToolTipValueActive(true);
                        }
                        else
                        {
                            HRPC.PlayerUI.EncyclopediaToolTipUI?.SetToolTipActive(false);
                        }

                    }
                    else
                    {
                        HRPC.PlayerUI.ToolTipUI.SetToolTipActive(false);
                        HRPC.PlayerUI.SellingTipUI?.SetToolTipActive(false);
                        HRPC.PlayerUI.EncyclopediaToolTipUI?.SetToolTipActive(false);
                    }
                }
            }
        }
    }

    public void HandleColorTooltipShowHide(HRPaintingComponent InPaintingComponent, bool bVisible)
    {
        /*
        if (bVisible)
        {
            HRPC.PlayerUI.ColorToolTipUI.SetToolTipText(InPaintingComponent.LastColorComponent.CustomMaterialDB.CustomItemInfos[InPaintingComponent.LastColorComponent.CustomItemInfoIndex].ItemName);
            HRPC.PlayerUI.ColorToolTipUI.SetMaterialNameText(InPaintingComponent.CurrentMaterialName);

            Texture2D MainTexture = null;
            string[] PropertyNames = InPaintingComponent.CurrentMaterial.GetTexturePropertyNames();
            if (PropertyNames.Length > 0)
            {
                MainTexture = InPaintingComponent.CurrentMaterial.GetTexture(PropertyNames[0]) as Texture2D;
                if (!MainTexture)
                {
                    MainTexture = InPaintingComponent.CurrentMaterial.GetTexture(PropertyNames[PropertyNames.Length - 1]) as Texture2D;
                }
            }
            HRPC.PlayerUI.ColorToolTipUI.SetToolTipActive(true);
            HRPC.PlayerUI.ColorToolTipUI.SetPreviewImage(MainTexture);
        }
        else
        {
            HRPC.PlayerUI.ColorToolTipUI.SetToolTipActive(false);
        }
        */
    }

    void HandlePause(BasePauseReceiver InPauseReceiver, bool bPaused)
    {
        HandlePause(null, bPaused, true);
    }

    void HandleDebugMode(bool bEnabled)
    {
        if (bEnabled)
        {
            if (PlayerCamera)
            {
                PlayerCamera.AddNoMouseRequest(gameObject);
            }
        }
        else
        {
            if (PlayerCamera)
            {
                PlayerCamera.RemoveNoMouseRequest(gameObject);
            }
        }
    }

    public void HandleDialogueStarted(HRDialoguePauser DialoguePauser, bool bStarted)
    {
        if (PlayerCamera)
        {
            // Kind of weird passing in the dialogue pauser but yeah
            if (bStarted)
            {
                PlayerCamera.AddNoMouseRequest(((HRGameInstance)BaseGameInstance.Get).DialoguePauser.gameObject);
                bCanSwitchWeapons = false;
            }
            else
            {
                PlayerCamera.RemoveNoMouseRequest(((HRGameInstance)BaseGameInstance.Get).DialoguePauser.gameObject);
                bCanSwitchWeapons = true;
            }
        }

        if (WeaponManager)
        {
            if (WeaponManager.CurrentWeapon && WeaponManager.CurrentWeapon.WeaponBlockerComponent)
            {
                BaseWeaponBlocker WeaponBlocker = WeaponManager.CurrentWeapon.WeaponBlockerComponent;
                if (WeaponBlocker && WeaponBlocker.IsBlocking())
                {
                    SetLastTimeBlocked();
                    WeaponBlocker.SetBlocking(false);
                    bIsBlockPressed = false;
                }
            }
        }

        HandlePause(null, bStarted, true);
        SetDialogueBubble(bStarted);
    }

    public void HandlePause(BasePauseReceiver InPauseReceiver, bool bPaused, bool bCutscene)
    {
        if (!HRNetworkManager.HasControl(netIdentity))
        {
            return;
        }
        bIsPaused = bPaused;
        if (bIsPaused)
        {
            if (InteractionManager.bClickedMouse)
            {
                InteractionManager.OnInteractMouseReleased();
            }

            bCanBeBowled = false;
        }
        else
        {
            bCanBeBowled = bOriginalCanBeBowled;
        }
        if (HP)
        {
            if (bIsPaused)
            {
                HP.SetInvincible(true);
            }
            else
            {
                if (PlayerController && HRConsoleCommands.bInvincibleCheat)
                {
                    // Do nothing bc invincible
                }
                else
                {
                    HP.ResetInvincible();
                }
            }
        }
        HRPlayerController hrpc = HRPC ?? (PlayerController as HRPlayerController);
        if (hrpc)
        {
            if (((HRGameInstance)BaseGameInstance.Get).MiscContainerUIManager)
            {
                ((HRGameInstance)BaseGameInstance.Get).MiscContainerUIManager.HandlePaused(bPaused);
            }
            if (bIsPaused)
            {
                // Check item placing manager because usually AI does not have it.
                if (ItemPlacingManager)
                {
                    SetFirstPersonMode(false);

                    PrimaryMouseEvent(false);
                    if (InteractionManager.LastInteractable)
                    {
                        if (!InteractionManager.LastInteractable.bIgnorePausing)
                        {
                            SecondaryMouseEvent(false);
                        }
                    }
                    else
                    {
                        SecondaryMouseEvent(false);
                    }

                    if (bCutscene && hrpc.PlayerUI)
                    {
                        hrpc.PlayerUI.RequestCutsceneMode(true, this);
                    }

                    ((HeroInputComponent)hrpc.InputComponent).SetMouseInputEnabled(false);
                    ((HeroInputComponent)hrpc.InputComponent).SetButtonInputEnabled(false);
                    InteractionManager.StopHover(InteractionManager.ClickableInteractableInRange);
                    hrpc.GetCursorManager().SetCursorType(BaseCursorManager.ECursorEnum.Regular);
                    ItemPlacingManager.enabled = false;
                    SetNewInteractState(InteractState.Paused);

                    // Zoom in
                    PrePauseZoomAmount = PlayerCamera.CurrentZoomAmount;
                    // Added to avoid zooming in any time a conversation finishes
                    PlayerCamera.SetPlayerOriginalZoom(PlayerCamera.CurrentZoomAmount);
                    //PlayerCamera.SetZoomAmount(3.0f);

                    // BAD TEMOP STUFF, setting background music to be lower volume
                    // Evan: Removing this because it conflicts
                    /*HRGameInstance GI = (HRGameInstance)BaseGameInstance.Get;
                    if (GI)
                    {
                        PrePauseMusicLayer = GI.MusicManager.CurrentMusicLayer;
                        if (PrePauseMusicLayer)
                        {
                            PrePauseVolume = PrePauseMusicLayer.CurrentTargetVolume;
                            PrePauseMusicLayer.SetLayerVolume(PrePauseVolume / 2.3f, 0.2f);
                        }
                    }*/
                }
                //if (MovementComponent)
                //{
                //    MovementComponent.FreezeMovement(true);
                //}
            }
            else
            {
                if (ItemPlacingManager)
                {
                    if (PlayerCamera)
                    {
                        HandleZoomTick(PlayerCamera.CurrentZoomAmount);
                    }

                    ((HeroInputComponent)hrpc.InputComponent).SetMouseInputEnabled(true);
                    ((HeroInputComponent)hrpc.InputComponent).SetButtonInputEnabled(true);
                    MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
                    ItemPlacingManager.enabled = true;
                    SetNewInteractState(InteractState.Free);

                    if (hrpc && hrpc.PlayerUI)
                    {
                        hrpc.PlayerUI.RequestCutsceneMode(false, this);
                    }

                    if (PlayerCamera)
                    {
                        // Todo fix the camear being set to 4 in the tutorial beginning instead of what it really is
                        PlayerCamera.ResetCameraToPlayer();
                        PlayerCamera.SetZoomAmount(PrePauseZoomAmount);
                    }

                    InteractionManager.bIsHolding = false;
                }
            }
        }
    }

    private void HandleShowInfo(bool bPressed)
    {
        if (bPressed)
        {
            SkillSystem?.ToggleStats();
        }
    }

    bool bQuitting = false;

    private void HandlePauseButtonPressed(bool bPressed)
    {
        if (!bPressed)
            return;
        if (DialogueManager.isConversationActive)
            return;

        if (BaseGameInstance.Get.PopupQueueUI.ActivePopups > 0 || BaseToggleUIManager.OpenToggles.Count > 0 || bQuitting ||
            (BaseGameInstance.Get as HRGameInstance).OpenMenusAux.Count > 0)
        {
            return;
        }
        else if (InventoryManager.CurrentContainer)
        {
            var Interactable = InventoryManager.CurrentContainer.Interactable;
            Interactable.TapInteraction(InteractionManager);
            Interactable.CancelToggle();
        }
        else
        {
            if (HRPC)
            {
                if (HRPC.PlayerUI.bInventoryActive)
                {
                    HRPC.PlayerUI.ToggleInventoryUI(false);
                    return;
                }

                if (!((HRGameInstance)BaseGameInstance.Get).PhoneManager.bIsShown)
                {
                    if (WeaponManager.CurrentWeapon && WeaponManager.CurrentWeapon.WeaponBlockerComponent)
                    {
                        BaseWeaponBlocker WeaponBlocker = WeaponManager.CurrentWeapon.WeaponBlockerComponent;
                        if (WeaponBlocker)
                        {
                            SetLastTimeBlocked();
                            WeaponBlocker.SetBlocking(false);
                        }

                        bIsBlockPressed = false;
                    }

                    WeaponManager.PrimaryInteract(false);
                    SecondaryMouseEvent(false);
                }

                bool isShown = ((HRGameInstance)BaseGameInstance.Get).PhoneManager.ToggleShow(0);

                (HRPC.InputComponent as HeroInputComponent).SetMouseInputEnabled(!isShown);
                (HRPC.InputComponent as HeroInputComponent).SetButtonInputEnabled(!isShown);
            }

            if (((HRGameInstance)BaseGameInstance.Get).PhoneManager.bIsShown)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                ((HRGameInstance)BaseGameInstance.Get).PhoneManager.OnShowDelegate += RestoreInput;
            }
        }
    }

    public void SetMapInputEnabled(bool bEnabled)
    {
        if (bEnabled)
        {
            if ((InputComponent as HeroInputComponent).MapButtonDelegate == null)
            {
                (InputComponent as HeroInputComponent).MapButtonDelegate += HandleMapButtonPressed;
            }
        }
        else
        {
            (InputComponent as HeroInputComponent).MapButtonDelegate -= HandleMapButtonPressed;
        }
    }

    void HandleMapButtonPressed(bool bPressed)
    {
        if (!bPressed) return;

        OpenMap();
    }
    public void OpenMap()
    {
        if (DialogueManager.isConversationActive) return;

        HRPhoneManager phoneManager = ((HRGameInstance)BaseGameInstance.Get).PhoneManager;

        if (phoneManager.bIsShown)
        {
            if (phoneManager.TryGetPhoneMapPanelIndex(out int mapPanelIndex) &&
                mapPanelIndex == phoneManager.NavBarPanelUI.CurrentIndex)
            {
                HandlePauseButtonPressed(true);
            }
            else
            {
                phoneManager.NavBarPanelUI.NavigateToIndex(mapPanelIndex);
            }
        }
        else
        {
            HandlePauseButtonPressed(true);
            // set separately to handle async show phone
            if (phoneManager.TryGetPhoneMapPanelIndex(out int mapPanelIndex))
            {
                phoneManager.SetIndexToNavigateTo(mapPanelIndex);
            }
        }
    }



    private void RestoreInput(HRPhoneManager PhoneManager, bool bShown)
    {
        if (!bShown)
        {
            if (PhoneManager)
            {
                PhoneManager.OnShowDelegate -= RestoreInput;
            }

            if (HRPC)
            {
                (HRPC.InputComponent as HeroInputComponent)?.SetMouseInputEnabled(true);
                (HRPC.InputComponent as HeroInputComponent)?.SetButtonInputEnabled(true);
            }
        }
    }

    private void HandleGesture(bool bPressed)
    {
        (HRGameManager.Get as HRGameManager).GestureWheelUI.gameObject.SetActive(bPressed);
        (HRGameManager.Get as HRGameManager).GestureWheelUI.OnGesturePressed(bPressed);
    }

    private void HandleSpecial(bool bPressed)
    {

    }


    private void UpdateCamera(BaseRagdollManager InRagdollManager, bool bEnabled, GameObject Instigator = null)
    {
        if (!bEnabled)
        {
            Ragdoll.OnRagdollDelegate -= UpdateCamera;
            PlayerCamera.bCanRotate = true;
        }
    }

    private void HandleTalkButtonPressed(bool bPressed)
    {
        if (BaseGameInstance.Get.bDebugMode)
        {
            if (bPressed)
            {
                // Fly
                MovementComponent.SetMoveMode(MovementComponent.CurrentMoveMode == BaseMoveMode.NOCLIP ? BaseMoveMode.GROUND : BaseMoveMode.NOCLIP);
            }
        }

        if (HRNetworkManager.IsHost())
        {
            if (netIdentity)
            {
                ShowTalkButton_ClientRpc(bPressed);
            }
            else
            {
                ShowTalkButton_Implementation(bPressed);
            }
        }
        else
        {
            ShowTalkButton_Command(bPressed);
        }
    }

    [Mirror.Command]
    private void ShowTalkButton_Command(bool bShow)
    {
        ShowTalkButton_ClientRpc(bShow);
    }

    [Mirror.ClientRpc]
    private void ShowTalkButton_ClientRpc(bool bShow)
    {
        ShowTalkButton_Implementation(bShow);
    }

    private void ShowTalkButton_Implementation(bool bShow)
    {
        TalkingSprite.SetActive(bShow);
    }

    private void HandleRadioButtonPressed(bool bPressed)
    {
        return;

        if (HRNetworkManager.IsHost())
        {
            if (netIdentity)
            {
                ShowRadioButton_ClientRpc(bPressed);
            }
            else
            {
                ShowRadioButton_Implementation(bPressed);
            }
        }
        else
        {
            ShowRadioButton_Command(bPressed);
        }
    }

    [Mirror.Command]
    private void ShowRadioButton_Command(bool bShow)
    {
        ShowRadioButton_ClientRpc(bShow);
    }

    [Mirror.ClientRpc]
    private void ShowRadioButton_ClientRpc(bool bShow)
    {
        ShowRadioButton_Implementation(bShow);
    }

    private void ShowRadioButton_Implementation(bool bShow)
    {
        RadioSprite.SetActive(bShow);
    }

    private void HandleInteraction(BaseInteractable interactable)
    {
        HRPlayerButtonHolder playerButtonHolder = interactable.GetComponentInParent<HRPlayerButtonHolder>();
        if (playerButtonHolder)
        {
            HandleButtonInteraction(playerButtonHolder);
        }
    }

    public void SetValuePlayerPVP(bool value)
    {
        if (HRNetworkManager.IsHost())
        {
            SetValuePlayerPVP_ClientRpc(value);
        }
        else
        {
            SetValuePlayerPVP_Command(value);
        }

    }

    [Mirror.ClientRpc]
    public void SetValuePlayerPVP_ClientRpc(bool value)
    {
        SetValuePlayerPVP_Implementation(value);
    }

    [Mirror.Command]
    public void SetValuePlayerPVP_Command(bool value)
    {
        SetValuePlayerPVP_ClientRpc(value);
    }

    public void SetValuePlayerPVP_Implementation(bool value)
    {
        if (FactionDataAsset)
        {
            FactionDataAsset.PVPEnabled = value;
        }
    }
    private void HandleButtonInteraction(HRPlayerButtonHolder holder)
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            HandleInteraction_Implementation(this.gameObject, holder.gameObject);
        }
        else
        {
            HandleButtonInteraction_Server(this.gameObject, holder.gameObject);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void HandleButtonInteraction_Server(GameObject player, GameObject holder)
    {
        HandleInteraction_Implementation(player, holder);
    }

    private void HandleInteraction_Implementation(GameObject characterObject, GameObject holderObject)
    {
        HRPlayerButtonHolder holder = holderObject?.GetComponent<HRPlayerButtonHolder>();
        if (holder)
        {
            HeroPlayerCharacter character = characterObject?.GetComponent<HeroPlayerCharacter>();
            holder?.HandleInteraction(character);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (PlayerController)
        {
            LlamaSoftware.UNET.Chat.ChatPlayer chatPlayer = PlayerController.ChatPlayer;
            if (chatPlayer && chatPlayer.chatSystem)
            {
                chatPlayer.chatSystem.OnChatOpenedDelegate -= HandleChatOpened;
                chatPlayer.chatSystem.OnChatClosedDelegate -= HandleChatClosed;

                chatPlayer.chatSystem.inputField.onSelect.RemoveListener(HandleChatInputFieldFocused);
                chatPlayer.chatSystem.inputField.onDeselect.RemoveListener(HandleChatInputFieldUnfocused);
            }
        }

        if (PlayerController && PlayerController == HRNetworkManager.Get.LocalPlayerController)
        {
            ((HRPlayerController)PlayerController).ClearAllCrimeSystemData();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (PauseReceiver)
        {
            PauseReceiver.OnPauseDelegate -= HandlePause;
        }

        if (Wallet)
        {
            Wallet.WalletUIUpdateDelegate -= HandleWalletChanged;
        }

        if (gameZoneListener)
        {
            gameZoneListener.GameZoneSwitchedEvent -= HandleGameZoneSwitched;
        }

        if (InteractionManager)
        {
            InteractionManager.OnTapInteractionDelegate -= HandleInteraction;
        }

        if (PlayerCamera)
        {
            Destroy(PlayerCamera.gameObject);
        }

        if (bCanWave && waveListener != null)
            waveListener.UnHookEvents();
        if(heroDeathReviveDelayController != null)
        {
            heroDeathReviveDelayController.UnHookEvents();
        }
        if(heroReviveController != null)
        {
            heroReviveController.UnHookEvents();
            heroReviveController.OnChannelDelegate -= HandleChannel;
        }
        if(_bombChannel != null)
        {
            _bombChannel.UnHookEvents();
            _bombChannel.OnChannelDelegate -= HandleChannel;
        }
        if(heroHolster != null)
        {
            heroHolster.UnHookEvents();
        }
        if(_respawnController != null)
        {
            _respawnController.UnHookEvents();
        }

        if (WeaponManager)
        {
            WeaponManager.AddedWeaponDelegate -= HandleWeaponAdded;
            WeaponManager.RemovedWeaponDelegate -= HandleWeaponRemoved;
        }

        if (MovementComponent)
        {
            MovementComponent.OnLandOnGroundDelegate -= HandleLandOnGround;
            MovementComponent.OnMoveModeChangedDelegate -= HandleMoveModeChanged;
        }

        if (HP)
        {
            HP.OnHPChangedDelegate -= HandleHPChanged;
            HP.OnHPChangedInstigatorDelegate -= HandleHPChangedInstigator;
        }

        if (bLocalPlayer)
        {
            if (BaseGameManager.Get && ((HRGameManager)BaseGameManager.Get).bShouldStopCombatLayerOnDeath)
            {
                ((HRGameInstance)BaseGameInstance.Get).MusicManager.RequestStopLayer(999, BaseMusicManager.BaseMusicLayerType.COMBAT, 0);
            }
        }

        if (BaseGameInstance.Get)
        {
            BaseGameInstance.Get.DebugDelegate -= HandleDebugMode;
        }


        OnCharacterDestroyedDelegate?.Invoke(this);

        UnbindInputComponent();

        if (DynamicEffects)
        {
            Destroy(DynamicEffects.gameObject);
        }

        BaseWater.CheckInAnyWater(gameObject, false);
    }

    private void HandleGameZoneSwitched(GameZone prev, GameZone next, bool bInitial)
    {
        // Only want non npc controlled players to add to
        // their crime system data as player controllers
        // hold the player's crime system data.
        HRCrimeSystem crimeSystem = next?.CrimeSystem;
        if (crimeSystem
            && !IsPossessedByPlayer)
        {
            NetworkAddCrimeSystemData(crimeSystem);
        }

        if (next != prev)
        {
            NetworkGameZoneSwitched(next?.gameObject);
        }
    }

    private void HandleChannel(AHeroChannel channel, bool bStarted)
    {
        OnChannelDelegate?.Invoke(channel, bStarted);
    }

    public void SetDialogueBubble(bool bIsOn)
    {
        if (bIsOn && HRDialogueSystem.Get.IsPlayeringLocalConversation) return;

        if (DialogueBubble)
        {
            DialogueBubble.SetActive(bIsOn);
        }
    }

    public void SpawnInteractDialogue(string ConversationTitle)
    {
        if (!Interactable || !DialogueManager.DatabaseManager.DefaultDatabase) return;

        if (HRNetworkManager.IsHost())
        {
            SpawnInteractDialogue_Server(ConversationTitle);
        }
        else
        {
            SpawnInteractDialogue_Command(ConversationTitle);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    void SpawnInteractDialogue_Command(string ConversationTitle)
    {
        SpawnInteractDialogue_Server(ConversationTitle);
    }

    void SpawnInteractDialogue_Server(string ConversationTitle)
    {
        SpawnInteractDialogue_ClientRpc(ConversationTitle);
    }

    [Mirror.ClientRpc]
    void SpawnInteractDialogue_ClientRpc(string ConversationTitle)
    {
        Conversation Convo = DialogueManager.DatabaseManager.MasterDatabase.GetConversation(ConversationTitle);

        if (Convo == null) return;

        if (!InteractDialogueInstance && InteractDialoguePrefab)
        {
            InteractDialogueInstance = Instantiate(InteractDialoguePrefab, transform).GetComponent<BaseDialogueStarter>();
        }

        if (InteractDialogueInstance)
        {
            InteractDialogueInstance.TriggeringInteractable = Interactable;
            InteractDialogueInstance.DialogueTrigger.selectedDatabase = DialogueManager.DatabaseManager.DefaultDatabase;
            InteractDialogueInstance.DialogueTrigger.conversation = Convo.Title;
            InteractDialogueInstance.DialogueActor = gameObject;

            if (Interactable)
            {
                Interactable.enabled = true;
                Interactable.SetInteractionCollisionEnabled(true);
            }
        }
    }

    private void NetworkGameZoneSwitched(GameObject nextGameZone)
    {
        if (!hasAuthority)
        {
            return;
        }

        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            NetworkGameZoneSwitched_Target(nextGameZone);
        }
        else
        {
            NetworkGameZoneSwitched_Server(nextGameZone);
        }
    }

    [Mirror.TargetRpc]
    private void NetworkGameZoneSwitched_Target(GameObject nextGameZone)
    {
        NetworkGameZoneSwitched_Implementation(nextGameZone);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkGameZoneSwitched_Server(GameObject nextGameZone)
    {
        NetworkGameZoneSwitched_Target(nextGameZone);
    }

    private void NetworkGameZoneSwitched_Implementation(GameObject nextGameZone)
    {
        GameZone next = nextGameZone?.GetComponent<GameZone>();
        bool sendDisplayAnimation = nextGameZone;
        string gameZoneText = next ? next.zoneName.value : "None";

        if (HRPC)
        {
            if (HRPC.isLocalPlayer && PlayerCamera)
            {
                PlayerCamera.GetCamera().useOcclusionCulling = next ? next.bUseOcclusionCulling : false;
            }

            if (HRPC.PlayerUI)
            {
                HRPC.PlayerUI.GameZoneUI.SetGameZoneText(gameZoneText, sendDisplayAnimation);
            }
        }

        if (TimeMusicManager && TimeMusicManager.isActiveAndEnabled && TimeMusicManager.bPlayOnStart)
        {
            TimeMusicManager.SetCurrentMusicInfo(next?.musicInfo);
        }

        if (TimeAmbientManager && TimeAmbientManager.isActiveAndEnabled && TimeAmbientManager.bPlayOnStart)
        {
            TimeAmbientManager.SetCurrentAmbientInfo(next?.ambientInfo);
        }

        JBooth.MicroSplat.TraxManager.bUseTrax = next ? next.UseTrax : true;
    }

    #region crime_system
    private void NetworkAddCrimeSystemData(HRCrimeSystem crimeSystem)
    {
        if (HRNetworkManager.IsHost())
        {
            NetworkAddCrimeSystemData_Server(crimeSystem?.gameObject);
        }
        else
        {
            NetworkAddCrimeSystemData_Command(crimeSystem?.gameObject);
        }
    }

    private void NetworkAddCrimeSystemData_Server(GameObject crimeSystemObject)
    {
        if (!crimeSystemObject)
        {
            return;
        }

        HRPlayerCrimeSystemData crimeSystemData = CreateCrimeSystemData();
        crimeSystemData.validCrimeSystem = true;
        if (!_crimeSystemData.ContainsKey(crimeSystemObject))
        {
            _crimeSystemData.Add(crimeSystemObject, crimeSystemData);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkAddCrimeSystemData_Command(GameObject crimeSystemObject)
    {
        NetworkAddCrimeSystemData_Server(crimeSystemObject);
    }

    private HRPlayerCrimeSystemData CreateCrimeSystemData()
    {
        HRPlayerCrimeSystemData crimeSystemData = new HRPlayerCrimeSystemData();
        crimeSystemData.currentCrimeLevel = 0;
        crimeSystemData.currentFineAmount = 0;
        crimeSystemData.fined = false;
        return crimeSystemData;
    }
    public bool GetCurrentCrimeSystemData(out HRPlayerCrimeSystemData systemData)
    {
        HRCrimeSystem crimeSystem = GZoneListener?.CurrentGameZone?.CrimeSystem;
        return GetCurrentCrimeSystemData(crimeSystem, out systemData);
    }

    public bool GetCurrentCrimeSystemData(HRCrimeSystem crimeSystem, out HRPlayerCrimeSystemData systemData)
    {
        if (IsControlledByPlayer)
        {
            HRPlayerController controller = PlayerController as HRPlayerController;
            return controller.GetCurrentCrimeSystemData(crimeSystem, out systemData);
        }

        if (crimeSystem)
        {
            if (_crimeSystemData.ContainsKey(crimeSystem.gameObject))
            {
                systemData = _crimeSystemData[crimeSystem.gameObject];
                return true;
            }

            NetworkAddCrimeSystemData(crimeSystem);
            HRPlayerCrimeSystemData createdSystemData = CreateCrimeSystemData();
            createdSystemData.validCrimeSystem = true;
            systemData = createdSystemData;
            return true;
        }

        systemData = default(HRPlayerCrimeSystemData);
        return false;
    }

    public void SendUpdatedCrimeSystemData(HRCrimeSystem crimeSystem, HRPlayerCrimeSystemData crimeSystemData)
    {
        if (!crimeSystem)
        {
            return;
        }

        if (HRNetworkManager.IsHost())
        {
            SendUpdatedCrimeSystemData_Server(crimeSystem.gameObject, crimeSystemData);
        }
        else
        {
            SendUpdatedCrimeSystemData_Command(crimeSystem.gameObject, crimeSystemData);
        }
    }

    private void SendUpdatedCrimeSystemData_Server(GameObject crimeSystem, HRPlayerCrimeSystemData crimeSystemData)
    {
        if (!_crimeSystemData.ContainsKey(crimeSystem))
        {
            _crimeSystemData.Add(crimeSystem, crimeSystemData);
        }
        else
        {
            _crimeSystemData[crimeSystem] = crimeSystemData;
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    private void SendUpdatedCrimeSystemData_Command(GameObject crimeSystem, HRPlayerCrimeSystemData crimeSystemData)
    {
        SendUpdatedCrimeSystemData_Server(crimeSystem, crimeSystemData);
    }

    public void HookCrimeSystemDataChangedFunc(IPlayerCrimeDataHolder.HRPlayerCrimeSystemDataChangedSignature signature)
    {
        if (IsControlledByPlayer)
        {
            HRPlayerController controller = PlayerController as HRPlayerController;
            controller?.HookCrimeSystemDataChangedFunc(signature);
            return;
        }
        _dataChangedEventDelegate += signature;
    }

    public void UnHookCrimeSystemDataChangedFunc(IPlayerCrimeDataHolder.HRPlayerCrimeSystemDataChangedSignature signature)
    {
        if (IsControlledByPlayer)
        {
            HRPlayerController controller = PlayerController as HRPlayerController;
            controller?.UnHookCrimeSystemDataChangedFunc(signature);
            return;
        }
        _dataChangedEventDelegate -= signature;
    }

    private void HandleValidCrimeSystemDataChanged(
    Mirror.SyncDictionary<GameObject, HRPlayerCrimeSystemData>.Operation operation,
    GameObject key, HRPlayerCrimeSystemData crimeSystemData)
    {
        HRCrimeSystem crimeSystem = key?.GetComponent<HRCrimeSystem>();
        switch (operation)
        {
            case Mirror.SyncDictionary<GameObject, HRPlayerCrimeSystemData>.Operation.OP_ADD:
                _dataChangedEventDelegate?.Invoke(
                    this, crimeSystem, crimeSystemData, crimeSystemData);
                break;
            case Mirror.SyncDictionary<GameObject, HRPlayerCrimeSystemData>.Operation.OP_SET:
                _dataChangedEventDelegate?.Invoke(
                    this, crimeSystem, _crimeSystemData[key], crimeSystemData);
                break;
        }
    }
    public void ClearAllCrimes()
    {
        if (IsControlledByPlayer)
        {
            HRPlayerController controller = PlayerController as HRPlayerController;
            controller.ClearAllCrimes();
            return;
        }
        HRCrimeSystem currentCrimeSystem = GZoneListener?.CurrentGameZone?.CrimeSystem;
        ClearAllCrimes(currentCrimeSystem);
    }

    public void ClearAllCrimes(HRCrimeSystem crimeSystem)
    {
        if (!crimeSystem)
        {
            return;
        }

        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            ClearAllCrimes_Implementation(crimeSystem.gameObject);
        }
        else
        {
            ClearAllCrimes_Server(crimeSystem.gameObject);
        }
    }

    private void ClearAllCrimes_Implementation(GameObject crimeSystem)
    {
        HRCrimeSystem crimeSystemComponent = crimeSystem?.GetComponent<HRCrimeSystem>();
        if (crimeSystemComponent)
        {
            HRPlayerCrimeInfo crimeInfo = crimeSystemComponent?.GetPlayerCrimeInfo(this);
            crimeInfo?.ClearAllCrimes();
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void ClearAllCrimes_Server(GameObject crimeSystem)
    {
        ClearAllCrimes_Implementation(crimeSystem);
    }

    public void PlayCrimeStinger(HRPoliceSystem.StingerType stingerType)
    {
        if (CurrentPoliceSystem)
        {
            PlayCrimeStinger(CurrentPoliceSystem, stingerType);
        }
        else if (LastPoliceSystem)
        {
            PlayCrimeStinger(LastPoliceSystem, stingerType);
        }
    }

    public void PlayCrimeStinger(HRPoliceSystem policeSystem, HRPoliceSystem.StingerType stingerType)
    {
        if (PlayerController)
        {
            HRPlayerController controller = PlayerController as HRPlayerController;
            if (controller)
            {
                controller.PlayCrimeStinger(policeSystem, stingerType);
            }
        }
    }

    public void PlayCrimeMusic(HRPoliceSystem policeSystem, HRPoliceSystem.MusicType musicType)
    {
        if (crimeMusicCooldown <= 0f || musicType == HRPoliceSystem.MusicType.TYPE_COMBAT)
        {
            if (PlayerController)
            {
                HRPlayerController controller = PlayerController as HRPlayerController;
                if (controller)
                {
                    controller.PlayCrimeMusic(policeSystem, musicType);
                    crimeMusicCooldown = CrimeMusicCooldown;
                }
            }
        }
    }

    public void StopCrimeMusic(HRPoliceSystem policeSystem)
    {
        if (PlayerController)
        {
            HRPlayerController controller = PlayerController as HRPlayerController;
            if (controller)
            {
                controller.StopCrimeMusic(policeSystem);
            }
        }
    }

    #endregion

    private void HandleWalletChanged(int oldAmount, int newAmount, HRWallet.WalletBalanceChangeReason reason, string objectText)
    {
        if (HRPC)
        {
            // Sets the contents of the wallet within the hero player controller.
            HRPC.SetWalletContentBalance(newAmount);
        }
    }

    public HRShopManager GetShopManager()
    {
        return CurrentShopManager;
    }

    Collider LastGroundedCollider = null;
    [HideInInspector] public HRShopManager CurrentShopManager = null;
    [HideInInspector] public HRFloorTile LastShopFloorTile = null;
    [HideInInspector] public bool onFloorTile = false;
    public bool bIgnoreFallDamageOnce = false;

    private void HandleLandOnGround(BaseMovementComponent InMovementComponent,
        float gravityVelocity, Vector3 previousGroundLocation, Vector3 newGroundLocation)
    {
        if (HRNetworkManager.HasControl(netIdentity))
        {
            if (bIgnoreFallDamageOnce)
            {
                bIgnoreFallDamageOnce = false;
                return;
            }

            // For now, don't allow ppl to take damage when in water
            if (HP && DamageReceiver && !bIsInWaterLocal)
            {
                float fallDistance = (previousGroundLocation - newGroundLocation).y;
                float distanceForFallDamage = fallDistance - fallDamageData.MinHeightFallDamage;
                distanceForFallDamage = Mathf.Max(distanceForFallDamage, 0.0f);
                float totalHealthLost = distanceForFallDamage * fallDamageData.HealthLostPerUnitY;

                if (totalHealthLost > 0.0f)
                {
                    DamageReceiver.ApplyDamage_Implementation(false, totalHealthLost, null, BaseDamageType.FALL);
                }
            }
        }
    }

    public void HandleFloorTileChanged(HRFloorTile tile)
    {
        if (tile == null)
        {
            onFloorTile = false;
        }
        else
        {
            onFloorTile = true;
            // Only want to update if it is a shop.
            if (tile && (tile.GetFloorType() == HRFloorType.SHOP
                || tile.GetFloorType() == HRFloorType.NEEDS
                    || tile.GetFloorType() == HRFloorType.STAFF))
            {
                LastShopFloorTile = tile;
            }

            HandleEnteredShop(LastShopFloorTile?.OwningShop);
        }

    }
    public void HandleEnteredShop(HRShopManager InShopManager)
    {
        if (CurrentShopManager == InShopManager) return;
        // Leave the shop. Turn off UI / music.
        if (CurrentShopManager)
        {
            CurrentShopManager.HandlePlayerExitedShop_Local(this);
        }

        if (InShopManager)
        {
            InShopManager.HandlePlayerEnteredShop_Local(this);
        }

        if (HRNetworkManager.IsHost())
        {
            HandleShopEntered_Server(InShopManager);
        }
        else
        {
            HandleShopEntered_Command(InShopManager);
            HandleShopEntered_Implementation(InShopManager);
        }
    }

    [Mirror.Command]
    public void HandleShopEntered_Command(HRShopManager InShopManager)
    {
        HandleShopEntered_Server(InShopManager);
    }

    [Mirror.Server]
    public void HandleShopEntered_Server(HRShopManager InShopManager)
    {
        //Run only on server
        if (CurrentShopManager)
        {
            if (CurrentShopManager.ShopPoliciesManager.GetHealthyLifestyleNodeActive())
                HP.SetShouldRegen(false);
        }
        if (InShopManager)
        {
            if (InShopManager.ShopPoliciesManager.GetHealthyLifestyleNodeActive())
                HP.SetShouldRegen(true);
        }
        HandleShopEntered_Implementation(InShopManager);
    }

    public void HandleShopEntered_Implementation(HRShopManager InShopManager)
    {
        //Run all this on server and client if called from one
        if (CurrentShopManager)
        {
            ShopkeeperRef?.UnPossessShop();
        }

        if (InShopManager)
        {
            ShopkeeperRef?.PossessShop(InShopManager);
        }
        CurrentShopManager = InShopManager;
    }

    private void HandleHPChanged(BaseHP InHP, GameObject Instigator, float PreviousHP, float NewHP, bool bPlayEffects)
    {
        if (NewHP < PreviousHP)
        {
            // This is so bad, but it's necessary to have FOV / music change happen when you get hit instead of when you get aggro.
            if (Instigator && HRNetworkManager.IsHost() && Instigator != gameObject && !IsPlayer(Instigator, out HeroPlayerCharacter outPC))
            {
                HeroPlayerCharacter AttackerCharacter = Instigator.GetComponent<HeroPlayerCharacter>();

                // Play music for player if they are getting hit by someone
                HandleCharacterCombat(AttackerCharacter);

                // Play music for player if they are hitting someone
                if (AttackerCharacter)
                {
                    AttackerCharacter.HandleCharacterCombat(this);
                }
            }
            //take off of seat if sitting and not a player
            if (CurrentSeat && !gameObject.CompareTag("Player"))
            {
                CurrentSeat.Unseat(false, true, false);
            }
        }
    }

    public void HandleCharacterCombat(HeroPlayerCharacter Instigator)
    {
        if ((this == null || this.PlayerController == null) && (Instigator == null || Instigator.PlayerController == null))
        {
            return;
        }

        if (!this.WeaponManager.inCombat)
        {
            this.WeaponManager.SetInCombat(true);

            if (this.PlayerController != null && Instigator)
            {
                HRCombatComponent Attacker = Instigator.GetComponent<HRCombatComponent>();
                if (Attacker)
                {
                    //plays music
                    // Check to see if attacker is alive.
                    if ((Attacker.OwningPlayerCharacter && Attacker.OwningPlayerCharacter.gameObject.activeInHierarchy && Attacker.OwningPlayerCharacter.HP.CurrentHP > 0)
                        || !Attacker.OwningPlayerCharacter)
                    {
                        if (Attacker.MusicInfo.AudioTracks.Length > 0 && !Attacker.bDontPlayCombatMusic)
                        {
                            if (this.connectionToClient != null)
                            {
                                this.PlaySpecificCombatMusic_TargetRpc(this.connectionToClient, Attacker.gameObject, false);
                            }
                            else
                            {
                                HRCombatManager.Get.PlaySpecificCombatMusic(Attacker.MusicInfo);
                            }
                        }
                    }
                }
            }
        }
    }

    private void HandleLevelChanged(HRXPComponent InComponent, int PreviousLevel, int NewLevel)
    {
        playerSkillTree.MaxSkillPoints = NewLevel;
        playerMasteryTree.MaxSkillPoints = NewLevel;

        if(PlayerController && HRAnalyticsManager.Get)
        {
            HRAnalyticsManager.Get.RecordPlayerLevel(PlayerController.CharacterUsername, NewLevel.ToString());
        }

        UpdateCharacterName();
    }


    private void HandleHPChangedInstigator(BaseHP InHP, GameObject Instigator, float PreviousHP, float NewHP, bool bPlayEffects)
    {
        bool bIsHost = HRNetworkManager.IsHost();

        if (NewHP < PreviousHP)
        {
            if (CharacterVoice && Instigator != this.gameObject)
            {
                CharacterVoice.PlayAudio("Damaged");
            }

            HeroPlayerCharacter instigatingCharacter = Instigator ? Instigator.GetComponent<HeroPlayerCharacter>() : null;

            if (this.gameObject.CompareTag("Player") && HRNetworkManager.IsPlayerPawn(this.gameObject))
            {
                if (CurrentInteractState != InteractState.Dying || CurrentInteractState != InteractState.Dead)
                    BaseScreenShakeManager.DoScreenShake(PlayerCamera.CameraTargetGameObject.transform, 0.3f, 0.1f, 30);
            }
            else if (Instigator && bIsHost)
            {
                HRCrimeSystemUtils.CheckWitnessesNearby(transform, Instigator.transform, HRSenseTrigger.DefaultRadius, LayerMask.GetMask("Senses"), 1, 20f, FactionDataAsset);
            }

            if (instigatingCharacter)
            {
                instigatingCharacter.OnDamagedCharacter?.Invoke(instigatingCharacter, this, PreviousHP - NewHP);

                if (bIsHost)
                {
                    if (instigatingCharacter.PlayerController)
                    {
                        if (damagingPlayers != null && !damagingPlayers.Contains(instigatingCharacter.PlayerController))
                        {
                            damagingPlayers.Add(instigatingCharacter.PlayerController);
                        }
                    }
                    damagingPlayers.Clear();

                    if (NewHP <= 0 && instigatingCharacter.bIsRevengeTarget && Random.Range(0f, 1f) < RevengeTargetChance)
                    {
                        if (instigatingCharacter.CurrentRevengingEnemy.EnemyID == CharacterID)
                        {
                            if (instigatingCharacter.CurrentRevengingEnemy.Amount < MaxRevengeAmount)
                            {
                                instigatingCharacter.CurrentRevengingEnemy.Amount++;
                            }

                            instigatingCharacter.CurrentRevengingEnemy.EnemyName = NameComponent && NameComponent.HasName ? NameComponent.Name : Interactable && Interactable.InteractionName != "Interactable" ? Interactable.InteractionName : DefaultRevengerName;
                        }
                        else
                        {
                            HREncounterDynamicEnemyData RevengingData;

                            if (instigatingCharacter.RevengingEnemies.TryGetValue(CharacterID, out RevengingData) && RevengingData.Amount < MaxRevengeAmount)
                            {
                                RevengingData.Amount += 1;
                                RevengingData.EnemyName = NameComponent && NameComponent.HasName ? NameComponent.Name : Interactable && Interactable.InteractionName != "Interactable" ? Interactable.InteractionName : DefaultRevengerName;
                                instigatingCharacter.RevengingEnemies.TrySetValue(CharacterID, RevengingData);
                            }
                            else
                            {
                                RevengingData = new HREncounterDynamicEnemyData(CharacterID, MinRevengeAmount, Random.Range(MinRevengeTimeAfter, MaxRevengeTimeAfter), NameComponent ? NameComponent.Name : "my friend");
                                instigatingCharacter.RevengingEnemies.Add(CharacterID, RevengingData, RevengeWeight);
                            }
                        }
                    }
                }
            }
        }
        else if (NewHP > PreviousHP)
        {
            if (CharacterVoice)
            {
                CharacterVoice.PlayAudio("Heal");
            }
        }
    }

    public string GetRichTextNameTag()
    {
        return "<color=" + GetFactionColorHTML() + ">" + UserName + "</color>";
    }

    public string GetFactionColorHTML()
    {
        if (FactionDataAsset)
        {
            return FactionDataAsset.GetFactionColorHTML();
        }
        return "#" + ColorUtility.ToHtmlStringRGB(Color.white);
    }

    public void ResetHungerThirst(bool bPartialRegen = true)
    {
        // Reset hunger and thirst
        if (HungerHP)
        {
            HungerHP.SetHP(Mathf.Max(bPartialRegen ? HungerHP.MaxHP / 3.0f : HungerHP.MaxHP, HungerHP.CurrentHP), this.gameObject);
        }

        if (ThirstHP)
        {
            ThirstHP.SetHP(Mathf.Max(bPartialRegen ? ThirstHP.MaxHP / 3.0f : ThirstHP.MaxHP, ThirstHP.CurrentHP), this.gameObject);
        }
    }

    private void HandleHPZero(BaseHP InHP, GameObject Instigator, float PreviousHP, float NewHP, bool bPlayEffects)
    {
        ResetHungerThirst();

        if (CurrentInteractState == InteractState.Dying
            || CurrentInteractState == InteractState.Dead)
        {
            return;
        }

        BaseBulletComponent bullet = Instigator?.GetComponent<BaseBulletComponent>();

        //if (bullet)
        //    Instigator = bullet.OriginalShooter;
        if (MovementComponent)
            MovementComponent.SetCanSwim(false);

        if (Ragdoll && PreviousHP != NewHP && bCanBeBowled)
        {
            if (Instigator && Instigator != this.gameObject)
            {
                Vector3 InstigatorLocation = Instigator != null ? Instigator.transform.position : transform.position;
                PlayerMesh?.transform.DORewind();
                Ragdoll.StunForFall((transform.position - InstigatorLocation).normalized * 750.0f, true);
                //ApplyHitStun(0.2f);
            }
            else
            {
                PlayerMesh?.transform.DORewind();
                Ragdoll.StunForFall(MovementComponent.GetRawVelocity() * 100.0f, true);
            }
        }

        if (CharacterVoice)
        {
            CharacterVoice.PlayAudio("Death");
        }

        HeroPlayerCharacter instigatingCharacter = Instigator ? Instigator.GetComponent<HeroPlayerCharacter>() : null;
        if (instigatingCharacter)
        {
            instigatingCharacter.OnKilledCharacter?.Invoke(instigatingCharacter, this, PreviousHP - NewHP);
        }

        if (HRNetworkManager.IsHost())
        {
            if (Instigator)
            {
                HRCrimeSystemUtils.CheckWitnessesNearby(transform, Instigator.transform, 10f, LayerMask.GetMask("Senses"), -1, 5, FactionDataAsset);
            }
            statusEffectsManager?.ResetAllStatusEffectsLocks();

            // Turn on interactable since ragdolls are interactable now due to sleeping customers
            SetInteractionCollisionEnabled_Server(true);
        }

        if (gameObject.CompareTag("Player"))
        {
            HRAchievementManager.Instance?.AddToTrackedDeaths(1);

            SetInDyingState();
            if (HRNetworkManager.IsHost() && instigatingCharacter)
            {
                if (BaseGameManager.Get.ChatSystem)
                {
                    string instigatorNameTag = instigatingCharacter.GetRichTextNameTag();
                    string nameTag = GetRichTextNameTag();
                    // TODO: distinction between downed and killed.
                    BaseGameManager.Get.ChatSystem.SendServerMessage(HRGameInstance.GetLocalizedGlobalString("chat_defeated").Replace
                        ("[Player2]", instigatorNameTag).Replace("[Player1]", nameTag));
                }
            }

            //take off of seat if sitting and not a player
            if (CurrentSeat)
            {
                CurrentSeat.Unseat(false, true, false);
            }
        }
        else
        {
            SetNewInteractState(HeroPlayerCharacter.InteractState.Dying);

            if (BarkComponent)
            {
                BarkComponent.PlayBark(onDeathBark);
            }

            this.gameObject.layer = LayerMask.NameToLayer("Ragdoll");
            //drop weapon on death
            /*
            if (WeaponManager)
            {
                WeaponManager.RemoveWeapon(WeaponManager.CurrentWeapon);
            }
            */

            if (instigatingCharacter != null)
            {
                if (instigatingCharacter.CheckSelfAuthority()) // Check to see if the character that killed this character is the client
                {
                    instigatingCharacter.UpdateKillList(this.characterDatabaseIndex);
                }
            }

            if (HRNetworkManager.IsHost())
            {
                for (int i = 0; i < damagingPlayers.Count; ++i)
                {
                    if (damagingPlayers[i] && damagingPlayers[i].PlayerPawn)
                    {
                        //give exp based on killing an npc
                        damagingPlayers[i].PlayerPawn.GetComponent<HRXPComponent>().AddHP_IgnoreAuthority(XPToGiveWhenKilled * InHP.MaxHP, null);
                    }
                }
            }
        }

        if (instigatingCharacter?.PlayerController)
        {
            ((HRPlayerController)instigatingCharacter.PlayerController).OnCharacterKilled(this);
        }
    }

    [Mirror.Server]
    void SetInteractionCollisionEnabled_Server(bool bEnabled)
    {
        SetInteractionCollisionEnabled_ClientRpc(bEnabled);
    }

    [Mirror.ClientRpc]
    void SetInteractionCollisionEnabled_ClientRpc(bool bEnabled)
    {
        SetInteractionCollision_Implementation(bEnabled);
    }

    void SetInteractionCollision_Implementation(bool bEnabled)
    {
        if (Interactable)
        {
            Interactable.SetInteractionCollisionEnabled(bEnabled);
        }
    }

    public void HandleCycleWeaponMode(bool IsButtonDown)
    {
        //if(IsButtonDown && WeaponManager)
        //{
        //    WeaponManager.CycleUseMode();
        //}
    }

    public void SetInputEnabled(bool bEnabled)
    {
        if (!HRPC || !HRPC.InputComponent)
        {
            return;
        }

        if (bEnabled)
        {
            HRPC.InputComponent.SetEnabled(true);
            MovementComponent.SetUsePlayerInput(true);
        }
        else
        {
            // Commented to resolve bug, may be necessary for other reasons
            //ItemPlacingManager.StopPlacingObject();

            if (HRPC && HRPC.InputComponent)
            {
                HRPC.InputComponent.SetEnabled(false);
                HRPC.GetCursorManager().SetCursorType(BaseCursorManager.ECursorEnum.Regular);
            }

            // Need to stop some input -- camera and movement
            MovementComponent.SetUsePlayerInput(false);
            PlayerCamera.SetAimX(0);
            PlayerCamera.SetAimY(0);

            InteractionManager.StopHover(InteractionManager.ClickableInteractableInRange);
        }
    }

    void HandleRagdoll(BaseRagdollManager InRagdollManager, bool bEnabled, GameObject Instigator = null)
    {
        if (bEnabled)
        {
            WeaponManager.RequestHideWeapon(false, this);
            if (IsPossessedByPlayer)
            {
                SetInputEnabled(false);
            }

            if (bShouldUseGrounder)
            {
                PlayerMesh?.AnimScript?.IKManager?.SetBipedIKEnabled(false);
            }
        }

        if (MovementComponent && MovementComponent.CharacterController)
        {
            MovementComponent.CharacterController.enabled = !bEnabled;

            MovementComponent.FreezeMovement(bEnabled);
        }

        if (!bEnabled)
        {
            if (IsPossessedByPlayer)
            {
                if (IsAlive())
                {
                    SetInputEnabled(true);

                    if (ItemPlacingManager && ItemPlacingManager.CurrentPlaceCollision)
                    {
                        ItemPlacingManager.CurrentPlaceCollision.RemoveAllCollisions();
                        ItemPlacingManager.CurrentPlaceCollision.ResetCollisionCheck();
                    }
                }

                if (bShouldUseGrounder)
                {
                    PlayerMesh?.AnimScript?.IKManager?.SetBipedIKEnabled(true);
                }
            }
        }

        if (!bEnabled)
        {
            SetHitstunMode(false);
        }
    }

    #region networked_revive_functions

    public void NetworkSetBeingRevived(bool enabled)
    {
        if (HRNetworkManager.bIsServer)
        {
            NetworkSetBeingRevived_Implementation(enabled);
            NetworkSetBeingRevived_ClientRPC(enabled);
        }
        else
        {
            NetworkSetBeingRevived_Command(enabled);
        }
    }

    [Mirror.ClientRpc]
    private void NetworkSetBeingRevived_ClientRPC(bool enabled)
    {
        NetworkSetBeingRevived_Implementation(enabled);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkSetBeingRevived_Command(bool enabled)
    {
        NetworkSetBeingRevived_Implementation(enabled);
        NetworkSetBeingRevived_ClientRPC(enabled);
    }

    private void NetworkSetBeingRevived_Implementation(bool enabled)
    {
        this.HeroDeathReviveDelayController.SetBeingRevived(enabled, true);
    }

    /// <summary>
    /// Sets the revive interactable enabled.
    /// </summary>
    /// <param name="enabled">Enabled</param>
    public void NetworkSetReviveInteractableEnabled(bool enabled)
    {
        if (HRNetworkManager.IsHost())
        {
            this.reviveInteractableEnabled = enabled;
        }
        else
        {
            this.ReviveInteractable_Server(enabled);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void ReviveInteractable_Server(bool enabled)
    {
        this.reviveInteractableEnabled = enabled;
    }

    private void ReviveInteractableEnabled_Hook(bool prevValue, bool newValue)
    {
        this.HeroDeathReviveDelayController?.SetReviveInteractableEnabled(newValue);
    }

    public void NetworkRevive(GameObject Reviver)
    {
        if (HRNetworkManager.IsHost())
        {
            NetworkRevive_ClientRPC();
        }
        else if (Reviver)
        {
            NetworkReviveByPlayer_Command(Reviver);
        }
        else
        {
            NetworkRevive_Command();
        }
    }

    [Mirror.ClientRpc]
    private void NetworkRevive_ClientRPC()
    {
        NetworkRevive_Implementation();
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkRevive_Command()
    {
        NetworkRevive_ClientRPC();
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkReviveByPlayer_Command(GameObject Reviver)
    {
        if (Reviver)
        {
            HeroPlayerCharacter reviverPC = Reviver.GetComponent<HeroPlayerCharacter>();
            if (reviverPC)
            {
                if (BaseGameManager.Get.ChatSystem)
                {
                    BaseGameManager.Get.ChatSystem.SendServerMessage(HRGameInstance.GetLocalizedGlobalString("chat_revive").Replace
                        ("[Player1]", GetRichTextNameTag()).Replace("[Player2]", reviverPC.GetRichTextNameTag()));
                }
            }
        }

        NetworkRevive_ClientRPC();
    }
    private void NetworkRevive_Implementation()
    {
        this.HeroDeathReviveDelayController.Revive();

        ResetHungerThirst();
    }

    public void NetworkSetChannelBarUIEnabled(HeroChannelType channelType, bool enabled)
    {
        if (HRNetworkManager.IsHost())
        {
            NetworkSetChannelBarUIEnabled_Target((int)channelType, enabled);
        }
        else
        {
            NetworkSetChannelBarUIEnabled_Command((int)channelType, enabled);
        }
    }

    [Mirror.TargetRpc]
    private void NetworkSetChannelBarUIEnabled_Target(int channelType, bool enabled)
    {
        NetworkSetChannelBarUIEnabled_Implementation(channelType, enabled);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkSetChannelBarUIEnabled_Command(int channelType, bool enabled)
    {
        NetworkSetChannelBarUIEnabled_Target(channelType, enabled);
    }

    private void NetworkSetChannelBarUIEnabled_Implementation(int channelType, bool enabled)
    {
        AHeroChannel channel = null;
        switch ((HeroChannelType)channelType)
        {
            case HeroChannelType.TYPE_REVIVE:
                channel = HeroReviveController;
                break;
            case HeroChannelType.TYPE_BOMB:
                channel = BombChannelController;
                break;
        }
        channel?.ReceiveChannelBarUIEnabled(enabled);
    }

    public void NetworkSetRevivedPlayerDisplay(string player)
    {
        if (HRNetworkManager.IsHost())
        {
            NetworkSetRevivedPlayerDisplay_Target(player);
        }
        else
        {
            NetworkSetRevivedPlayerDisplay_Server(player);
        }
    }

    [Mirror.TargetRpc]
    private void NetworkSetRevivedPlayerDisplay_Target(string player)
    {
        NetworkSetRevivedPlayerDisplay_Implementation(player);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkSetRevivedPlayerDisplay_Server(string player)
    {
        NetworkSetRevivedPlayerDisplay_Target(player);
    }

    private void NetworkSetRevivedPlayerDisplay_Implementation(string player)
    {
        this.HeroReviveController.ReceiveRevivedPlayerDisplay(player);
    }

    public void NetworkUpdateChannelUIBar(HeroChannelType channelBarType, float percentage, float timeLeft)
    {
        if (HRNetworkManager.IsHost())
        {
            NetworkUpdateChannelUIBar_Target((int)channelBarType, percentage, timeLeft);
        }
        else
        {
            NetworkUpdateChannelUIBar_Server((int)channelBarType, percentage, timeLeft);
        }
    }

    [Mirror.TargetRpc]
    private void NetworkUpdateChannelUIBar_Target(int channelType, float percentage, float timeLeft)
    {
        NetworkUpdateChannelUIBar_Implementation(channelType, percentage, timeLeft);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkUpdateChannelUIBar_Server(int channelType, float percentage, float timeLeft)
    {
        NetworkUpdateChannelUIBar_Target(channelType, percentage, timeLeft);
    }

    private void NetworkUpdateChannelUIBar_Implementation(int channelType, float percentage, float timeLeft)
    {
        AHeroChannel channelFound = null;
        switch ((HeroChannelType)channelType)
        {
            case HeroChannelType.TYPE_REVIVE:
                channelFound = HeroReviveController;
                break;
            case HeroChannelType.TYPE_BOMB:
                channelFound = BombChannelController;
                break;
        }

        if (channelFound != null)
        {
            channelFound.ReceiveUpdateChannelBarUI(percentage, timeLeft);
        }
    }

    #endregion

    #region death_functions

    private void SetInDyingState()
    {
        SetNewInteractState(InteractState.Dying);

        if (HRNetworkManager.IsHost())
        {
            SetInDyingState_Target();
        }
        else
        {
            SetInDyingState_Server();
        }
    }

    [Mirror.TargetRpc]
    private void SetInDyingState_Target()
    {
        SetInDyingState_Implementation();
    }

    [Mirror.Command]
    private void SetInDyingState_Server()
    {
        SetInDyingState_Target();
    }

    private void SetInDyingState_Implementation()
    {
        if (WeaponManager && !HRNetworkManager.IsPlayerPawn(this.gameObject))
        {
            //WeaponManager.DropCurrentWeapon();
        }

        if (InputComponent is HeroInputComponent heroInputComponent)
        {
            heroInputComponent.SetButtonInputEnabled(false);
        }

        // Ragdoll.SetRagdollEnabled(false, null, false);
        GenerateDyingUI();
    }

    /// <summary>
    /// Generates the dying UI.
    /// </summary>
    private void GenerateDyingUI()
    {
        if (this.dyingUIPrefab
            && !this.currentDyingUI)
        {
            GameObject instantiated = Instantiate<GameObject>(this.dyingUIPrefab.gameObject);
            instantiated.transform.position = Vector2.zero;
            this.currentDyingUI = instantiated.GetComponent<HRDyingUI>();
            this.currentDyingUI.SetCorrespondingPlayer(this);
        }
        else if (this.currentDyingUI)
        {
            this.currentDyingUI.SetVisible(true);
        }

        this.currentDyingUI.bAllowGivingUp = bAllowGiveUp;

        // Show the F prompt
        SetRevivePromptUIActive(true);
    }

    public void SendDyingUIDebugChanged(bool debug)
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            SendDyingUIDebugChanged_TargetRPC(debug);
        }
        else
        {
            SendDyingUIDebugChanged_Server(debug);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SendDyingUIDebugChanged_Server(bool debug)
    {
        SendDyingUIDebugChanged_TargetRPC(debug);
    }

    [Mirror.TargetRpc]
    private void SendDyingUIDebugChanged_TargetRPC(bool debug)
    {
        if (currentDyingUI)
        {
            currentDyingUI.ReceiveDebugChanged(debug);
        }
    }
    //When this is destroyed, it will be set to dead state

    private void HandleDestroyed(BaseDestroyHPListener listener)
    {
        this.SetNewInteractState(InteractState.Dead);
    }
    /// <summary>
    /// Kills the character.
    /// </summary>
    public void Kill(bool bForceSleep = false)
    {
        NetworkReviveReset();

        if (HRNetworkManager.IsHost())
        {
            Kill_Server(bForceSleep);
        }
        else
        {
            Kill_Command(bForceSleep);
        }
        this.SetNewInteractState(InteractState.Dead);
    }

    private void Kill_Server(bool ForceSleep)
    {
        CalculateHealthInsuranceCost();
        Kill_Target(ForceSleep);
        Kill_ClientRPC();
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void Kill_Command(bool ForceSleep)
    {
        Kill_Server(ForceSleep);
    }

    [Mirror.TargetRpc]
    private void Kill_Target(bool ForceSleep)
    {
        Kill_Target_Implementation(ForceSleep);
    }

    [Mirror.ClientRpc]
    private void Kill_ClientRPC()
    {
        Kill_ClientRPC_Implementation();
    }

    #region kill_implementation

    [Mirror.Server]
    public void CalculateHealthInsuranceCost()
    {
        //Remove some amount of the players money
        double balance = Wallet.GetBalance();
        //TODO: make this not a magic number
        double amountToRemove = balance * HealthInsuranceCostPercent;
        LastHealthInsuranceCost = (int)amountToRemove;
        Wallet.RemoveMoney(LastHealthInsuranceCost, HRWallet.WalletBalanceChangeReason.Deduction, "");
        Debug.Log($"Player died, removing ${LastHealthInsuranceCost}");
    }
    private void Kill_Target_Implementation(bool ForceSleep)
    {
        if (IsPossessedByPlayer)
        {
            Debug.Log("YOU DIED"); // Using this to debug chunks disappearing after respawning
            DestroyDyingUI();

            float fadeDuration = 1.0f;

            if (!ForceSleep)
            {
                BaseGameInstance.Get.FadeManager.Fade(false, fadeDuration, Color.black, true);
                BaseGameInstance.Get.FadeManager.FinishFadeDelegate += HandleFinishFade;
            }

            HeroInputComponent inputComponent = HRPC?.InputComponent as HeroInputComponent;
            inputComponent?.SetButtonInputEnabled(false);
            inputComponent?.SetMouseInputEnabled(false);

            SetInputEnabled(false);
        }
    }

    private void HandleFinishFade(BaseFadeManager fadeManager)
    {
        NetworkShowDeathUI(true);
        float fadeDuration = 0.5f;
        BaseGameInstance.Get.FadeManager.Fade(true, fadeDuration, Color.black, false);
        BaseGameInstance.Get.FadeManager.FinishFadeDelegate -= HandleFinishFade;
    }

    public void NetworkShowDeathUI(bool show)
    {
        NetworkShowDeathUI_Implementation(show);
    }

    private void NetworkShowDeathUI_Implementation(bool show)
    {
        DestroyDyingUI();

        HRPlayerController InPlayerController = HRPC ? HRPC : HRPCBeforeSpectate;

        if (InPlayerController && InPlayerController.PlayerUI && InPlayerController.PlayerUI.DeathUI)
        {
            InPlayerController.PlayerUI.DeathUI.Bind(this);
            InPlayerController.PlayerUI.DeathUI.Show(show);
        }
    }

    private void Kill_ClientRPC_Implementation()
    {
        if (HP)
        {
            HP.SetInvincible_Implementation(true);
        }

        if (Ragdoll)
        {
            Ragdoll.SetRagdollEnabled(true, this.gameObject, false, false);
            if (Ragdoll.bStunned)
            {
                Ragdoll.UnStun();
            }
        }
    }

    /// <summary>
    /// Destroys the Dying UI From the Client.
    /// </summary>
    public void DestroyDyingUI()
    {
        if (this.currentDyingUI)
        {
            // Hide the F prompt
            SetRevivePromptUIActive(false);
            Destroy(this.currentDyingUI.gameObject);
            this.currentDyingUI = null;
        }
    }


    private void SetRevivePromptUIActive(bool value)
    {
        return;

        if (HRNetworkManager.IsHost())
        {
            SetRevivePromptUIActive_Implementation(value);
            SetRevivePromptUIActive_ClientRpc(value);
        }
        else
        {
            SetRevivePromptUIActive_Command(value);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SetRevivePromptUIActive_Command(bool value)
    {
        SetRevivePromptUIActive_Implementation(value);
        SetRevivePromptUIActive_ClientRpc(value);
    }

    [Mirror.ClientRpc]
    private void SetRevivePromptUIActive_ClientRpc(bool value)
    {
        SetRevivePromptUIActive_Implementation(value);
    }


    private void SetRevivePromptUIActive_Implementation(bool value)
    {
        RevivePrompt.SetActive(value);
    }

    #endregion

    public void NetworkReviveReset()
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            NetworkReviveReset_ClientRPC();
        }
        else
        {
            NetworkReviveReset_Server();
        }
    }

    [Mirror.ClientRpc]
    private void NetworkReviveReset_ClientRPC()
    {
        NetworkReviveReset_Implementation();
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkReviveReset_Server()
    {
        NetworkReviveReset_ClientRPC();
    }

    private void NetworkReviveReset_Implementation()
    {
        HeroDeathReviveDelayController?.ReceiveReviveReset();
    }

    public void NetworkDyingUIReviveBarEnabled(bool enabled)
    {
        if (HRNetworkManager.IsHost())
        {
            NetworkDyingUIReviveBarEnabled_Target(enabled);
        }
        else
        {
            NetworkDyingUIReviveBarEnabled_Command(enabled);
        }
    }

    [Mirror.TargetRpc]
    private void NetworkDyingUIReviveBarEnabled_Target(bool enabled)
    {
        NetworkDyingUIReviveBarEnabled_Implementation(enabled);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkDyingUIReviveBarEnabled_Command(bool enabled)
    {
        NetworkDyingUIReviveBarEnabled_Target(enabled);
    }
    private void NetworkDyingUIReviveBarEnabled_Implementation(bool enabled)
    {
        this.HeroDeathReviveDelayController.ReceiveDyingUIReviveBarEnabled(enabled);
    }

    public void NetworkDyingUIUpdateProgressBar(float percentage, int seconds, bool reviveBar)
    {
        if (HRNetworkManager.IsHost())
        {
            NetworkDyingUIUpdateProgressBar_TargetRPC(percentage, seconds, reviveBar);
        }
        else
        {
            NetworkDyingUIUpdateProgressBar_Server(percentage, seconds, reviveBar);
        }
    }

    [Mirror.TargetRpc]
    private void NetworkDyingUIUpdateProgressBar_TargetRPC(float percentage, int seconds, bool reviveBar)
    {
        NetworkDyingUIUpdateProgressBar_Implementation(percentage, seconds, reviveBar);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkDyingUIUpdateProgressBar_Server(float percentage, int seconds, bool reviveBar)
    {
        NetworkDyingUIUpdateProgressBar_TargetRPC(percentage, seconds, reviveBar);
    }
    private void NetworkDyingUIUpdateProgressBar_Implementation(float percentage, int seconds, bool reviveBar)
    {
        this.HeroDeathReviveDelayController.ReceiveDyingUIUpdateReviveBar(percentage, seconds, reviveBar);
    }

    public void NetworkGetUp(bool force)
    {
        if (HRNetworkManager.IsHost())
        {
            NetworkGetUp_ClientRPC(force);
        }
    }

    [Mirror.ClientRpc]
    private void NetworkGetUp_ClientRPC(bool force)
    {
        NetworkGetUp_Implementation(force);
    }

    [Mirror.Command]
    private void NetworkGetUp_Command(bool force)
    {
        NetworkGetUp_ClientRPC(force);
    }

    // This should be changed to a syncvar isRagdolled and fire a hook for the actual getup
    private void NetworkGetUp_Implementation(bool force)
    {
        if (this.Ragdoll)
        {
            this.Ragdoll.GetUp(force);
        }
    }

    #endregion

    #region sleep_functions
    public void Sleep(HRBedComponent bedComponent, float sleepTimeSeconds = 4.0f)
    {
        SetNewInteractState(InteractState.Sleeping);

        if (HRNetworkManager.IsHost())
        {
            Sleep_Target(bedComponent?.gameObject, sleepTimeSeconds);
        }
        else
        {
            Sleep_Server(bedComponent?.gameObject, sleepTimeSeconds);
        }
    }

    [Mirror.TargetRpc]
    private void Sleep_Target(GameObject bed, float sleepTimeSeconds)
    {
        Sleep_Implementation(bed, sleepTimeSeconds);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void Sleep_Server(GameObject bed, float sleepTimeSeconds)
    {
        Sleep_Target(bed, sleepTimeSeconds);
    }
    private void Sleep_Implementation(GameObject bed, float sleepTimeSeconds)
    {
        // Set the player's state to sleep here
        SleepManager.Sleep(
            bed ? bed.GetComponent<HRBedComponent>() : null, sleepTimeSeconds);

        if (HungerHP)
        {
            HungerHP.SetHP(HungerHP.CurrentHP / 2, this.gameObject);
        }

        if (ThirstHP)
        {
            ThirstHP.SetHP(ThirstHP.CurrentHP / 2, this.gameObject);
        }
    }

    #endregion

    void HandleWeaponAdded(BaseWeaponManager InManager, BaseWeapon WeaponAdded)
    {

    }

    void HandleWeaponRemoved(BaseWeaponManager InManager, BaseWeapon WeaponAdded)
    {

    }

    public void DisplayNoStaminaMessage()
    {
        ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("No stamina!", this.transform);
    }

    void HandleJump(bool bPressed)
    {
        if (!onJumpPad)
        {
            if (statusEffectsManager)
            {
                if (statusEffectsManager.GetKnockdownStatus())
                {
                    //check if the character is on the ground and not stunned
                    if ((MovementComponent.GetIsGrounded() || MovementComponent.CurrentMoveMode == BaseMoveMode.SWIMMING) && !statusEffectsManager.GetStunStatus() && statusEffectsManager.GetCanRecoverFromKnockdown())
                    {
                        statusEffectsManager.SetIsNowRecoveringFromKnockdown(true);
                        AnimScript.HandleKnockdownNeutralGetUp();
                        return;
                    }
                }
            }

            if (!CanMove())
            {
                return;
            }

            if (MovementComponent && bPressed)
            {
                if (CurrentSeat)
                {
                    CurrentSeat.Unseat(false, true, false);
                }

                // If it's in the air
                if (MovementComponent.IsSliding() || MovementComponent.GetIsGrounded() || MovementComponent.CurrentMoveMode == BaseMoveMode.SWIMMING || MovementComponent.CurrentMoveMode == BaseMoveMode.ZIPLINING)
                {
                    if (MovementComponent.Jump(false, 1.0f, 0f, false, true, onJumpPad, onJumpPad))
                    {
                        //AddToSkill("Fitness", SprintXPRate * Time.deltaTime, "Sprinting", true);
                        //AddToSkill("Fitness", SprintXPRate * Time.deltaTime, "", true);

                        if (CharacterVoice)
                        {
                            CharacterVoice.PlayAudio("Jump");
                        }
                    }
                }
                else
                {
                    // Do glider if unlocked
                    if (((HRGameInstance)BaseGameInstance.Get).bGliderUnlocked)
                    {
                        MovementComponent.ToggleGlider();
                    }
                }
            }
        }
    }

    void HandleCrouch(bool bPressed)
    {
        if (MovementComponent)
        {
            MovementComponent.Crouch(bPressed);
            MovementComponent.ToggleDiving(bPressed);
        }
    }

    private bool CanDash()
    {
        //check to see if theres enough stamina to dash
        if (StaminaComponent)
        {
            if (StaminaComponent.CurrentHP < DashStaminaCost)
            {
                DisplayNoStaminaMessage();
                return false;
            }
        }

        //exception to ignore Stunned state if they're knocked down
        if (statusEffectsManager)
        {
            if (statusEffectsManager.GetKnockdownStatus())
            {
                //check if the character is on the ground and not stunned
                if ((MovementComponent.GetIsGrounded() || MovementComponent.CurrentMoveMode == BaseMoveMode.SWIMMING) && !statusEffectsManager.GetStunStatus() && statusEffectsManager.GetCanRecoverFromKnockdown())
                {

                    statusEffectsManager.SetIsNowRecoveringFromKnockdown(true);
                    return true;
                }
            }
        }

        if (!WeaponManager.GetCanCancelRoll())
        {
            return false;
        }
        return DashComponent && DashComponent.CanDash() && CanMove() && MovementComponent.CurrentMoveMode != BaseMoveMode.SWIMMING;
    }
    void HandleDash(bool bPressed)
    {
        if (bPressed)
        {
            if (CanDash())
            {
                if (CurrentSeat)
                {
                    CurrentSeat.Unseat(false, true, false);
                }

                if (DashComponent && DashComponent.bCanDash && MovementComponent)
                {
                    //check to see if this dash is a knockdown recovery
                    bool bIsRecoveringFromKnockdown = false;
                    if (statusEffectsManager)
                    {
                        bIsRecoveringFromKnockdown = statusEffectsManager.GetIsRecoveringFromKnockdown();
                    }
                    DashComponent.DashWithCharge((MovementComponent.GetPlayerInputVector() != Vector3.zero) ? MovementComponent.GetPlayerInputVector() : MovementComponent.transform.forward, null, bIsRecoveringFromKnockdown);

                    //turn blocking off
                    BaseWeaponBlocker WeaponBlocker = WeaponManager?.CurrentWeapon?.WeaponBlockerComponent;
                    if (WeaponBlocker)
                    {
                        SetLastTimeBlocked();
                        WeaponBlocker.SetBlocking(false);
                    }

                    if (StaminaComponent)
                    {
                        StaminaComponent.RemoveHP(DashStaminaCost * SprintEfficiencyModifier, this.gameObject, false, false, false);
                        //StaminaComponent.RemoveHP(DashStaminaCost * SprintEfficiencyModifier, this.gameObject, false, false, false);
                    }

                    // Cancel any held button weapons like consumables
                    WeaponManager.PrimaryInteract(false);
                }
            }
        }
    }

    void HandleEscape(bool bPressed)
    {
        if (bPressed)
        {
            bool bShouldCancelInteract = true;

            if (BaseGameInstance.Get.PopupQueueUI.ActivePopups > 0)
            {
                HRPlayerCustomizationUI CustomUI = ((HRGameInstance)BaseGameInstance.Get).PlayerCustomization;
                if (CustomUI.IsCustomizing())
                {
                    CustomUI.StopCustomization(false);
                }
                else
                {
                    BaseGameInstance.Get.PopupQueueUI.CurrentPopup.ClosePopup();
                    StartCoroutine(ClosingPopupDelay());

                    bShouldCancelInteract = false;
                }
            }
            else if (BaseToggleUIManager.OpenToggles.Count > 0)
            {
                BaseToggleUIManager.OpenToggles.RemoveAll((t) => t == null);

                if (BaseToggleUIManager.OpenToggles.Count > 0)
                {
                    var current = BaseToggleUIManager.OpenToggles[BaseToggleUIManager.OpenToggles.Count - 1];
                    current.UIManuallyClosed();

                    if (BaseToggleUIManager.OpenToggles.Contains(current))
                    {
                        BaseToggleUIManager.OpenToggles.Remove(current);
                    }

                    if (current == (BaseGameInstance.Get as HRGameInstance).DefaultCraftingUI)
                    {
                        (BaseGameInstance.Get as HRGameInstance).DefaultCraftingUI.CurrentCraftingComponent.HideUI();
                    }

                    StartCoroutine(ClosingPopupDelay());
                }
            }
            else if ((BaseGameInstance.Get as HRGameInstance).OpenMenusAux.Count > 0)
            {
                StartCoroutine(ClosingPopupDelay());
            }
            else if ((BaseGameInstance.Get as HRGameInstance).DefaultCraftingUI.CurrentCraftingComponent &&
                (BaseGameInstance.Get as HRGameInstance).DefaultCraftingUI.CurrentCraftingComponent.InUse)
            {
                (BaseGameInstance.Get as HRGameInstance).DefaultCraftingUI.CurrentCraftingComponent.HideUI();
                StartCoroutine(ClosingPopupDelay());
            }

            if (InteractionManager.LastInteractable != null && bShouldCancelInteract)
            {
                InteractionManager.LastInteractable.CancelToggle();
                InteractionManager.LastInteractable = null;
            }
        }
    }


    private IEnumerator ClosingPopupDelay()
    {
        bQuitting = true;

        yield return null;

        bQuitting = false;
    }

    float RemainingHitstun = 0.0f;
    bool bIsInHitstun = false;

    public bool IsInHitstun()
    {
        return bIsInHitstun;
    }

    public GameObject StunnedFX;
    public void ApplyHitStun(float HitstunSeconds, bool ignoreImmunity = false)
    {
        //dont apply hitstun if they're in grace period from reccovering form knockdown
        if (ignoreImmunity || (bCanBeHitstunned && statusEffectsManager && statusEffectsManager.bInGraceKnockdownPeriod))
        {
            if (HitstunSeconds > RemainingHitstun)
            {
                RemainingHitstun = HitstunSeconds;

                PlayerMesh.transform.DORewind();
                PlayerMesh.transform.DOShakePosition(0.3f, 0.1f, 30);
            }

            // Uncrouch when hit
            if (MovementComponent)
            {
                MovementComponent.Crouch(false);
            }

            OnHitstunDelegate?.Invoke(this, RemainingHitstun);
        }
    }

    public void SetOwningShopPlot(HRShopPlot InShopPlot)
    {
        OwningShopPlot = InShopPlot;

        if (ShopkeeperRef)
        {
            if (InShopPlot)
            {
                ShopkeeperRef.PossessShop(InShopPlot.GetShop(0));
            }
            else
            {
                ShopkeeperRef.UnPossessShop();
            }
        }

        // Show/hide shop plot UI if owning player
        if (PlayerController && PlayerController.isLocalPlayer)
        {
            if (HRPC && HRPC.PlayerUI)
            {
                if (HRPC.PlayerUI.ShopPlotUI)
                {
                    HRPC.PlayerUI.ShopPlotUI.Initialize(InShopPlot);
                }

                if (HRPC.PlayerUI.ShopManagerUI)
                {
                    if (InShopPlot)
                    {
                        if (InShopPlot.ShopsInPlot != null && InShopPlot.ShopsInPlot.Count > 0)
                        {
                            HRPC.PlayerUI.ShopManagerUI.Initialize(InShopPlot.ShopsInPlot[0], ShopkeeperRef);
                        }
                        else
                        {
                            HRPC.PlayerUI.ShopManagerUI.Initialize(null, ShopkeeperRef);
                        }
                    }
                    else
                    {
                        HRPC.PlayerUI.ShopManagerUI.Initialize(null, ShopkeeperRef);
                    }
                }
            }

        }
    }

    public void SetHitstunMode(bool bEnabled)
    {
        if (bIsInHitstun == bEnabled)
        {
            return;
        }

        bIsInHitstun = bEnabled;

        OnHitstunDelegate?.Invoke(this, RemainingHitstun);
        if (StunnedFX)
        {
            StunnedFX.gameObject.SetActive(bEnabled);
        }

        if (bEnabled == false)
        {
            DORewindOnPlayerMesh();
        }
    }

    public void SetFreezeMovement(bool bEnabled)
    {
        if (MovementComponent)
        {
            MovementComponent.FreezeMovement(bEnabled);
        }

        if (DashComponent)
        {
            DashComponent.enabled = !bEnabled;
        }
    }

    //Freezes animation in frame
    //Have to turn off leaning and IK to prevent falling and excessive lean
    public void SetFreezeAnimation(bool bEnabled)
    {
        if (AnimScript && AnimScript.AnimancerComponent)
        {
            if (bEnabled)
            {
                AnimScript.bMovementLeanEnabled = false;
                AnimScript.AnimancerComponent.Playable.PauseGraph();
                if (AnimScript.IKManager && AnimScript.IKManager.FinalIKRef)
                {
                    AnimScript.IKManager.FinalIKRef.enabled = false;
                }

            }
            else
            {
                AnimScript.bMovementLeanEnabled = AnimScript.bOriginalMovementLean;
                AnimScript.AnimancerComponent.Playable.UnpauseGraph();
                if (AnimScript.IKManager && AnimScript.IKManager.FinalIKRef)
                {
                    AnimScript.IKManager.FinalIKRef.enabled = true;
                }
            }
        }
    }

    //f
    public void DORewindOnPlayerMesh(bool includeDelay = true)
    {
        this.PlayerMesh.transform.DOKill(includeDelay);
    }

    public void HidePlayerRig(bool bHide, bool bPlaySpawnEffect = true)
    {
        if (HRNetworkManager.IsHost())
        {
            HidePlayerRig_Implementation(bHide, bPlaySpawnEffect);
            HidePlayerRig_ClientRpc(bHide, bPlaySpawnEffect);
        }
        else
        {
            HidePlayerRig_Implementation(bHide, bPlaySpawnEffect);
            HidePlayerRig_Command(bHide, bPlaySpawnEffect);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    void HidePlayerRig_Command(bool bHide, bool bPlaySpawnEffect)
    {
        HidePlayerRig_Implementation(bHide, bPlaySpawnEffect);
        HidePlayerRig_ClientRpc(bHide, bPlaySpawnEffect);
    }

    [Mirror.ClientRpc(excludeOwner = true)]
    void HidePlayerRig_ClientRpc(bool bHide, bool bPlaySpawnEffect)
    {
        HidePlayerRig_Implementation(bHide, bPlaySpawnEffect);
    }

    public void HidePlayerRig_Implementation(bool bHide, bool bPlaySpawnEffect)
    {
        if (bHide)
        {
            if (PlayerMesh && PlayerMesh.PlayerRig)
            {
                PlayerMesh.PlayerRig.gameObject.SetActive(false);
            }

            if (PlayerNameText)
                SetNameTextVisibility(false);
        }
        else
        {
            if (this != BaseGameInstance.Get.GetLocalPlayerPawn())
            {
                SetNameTextVisibility(true);
            }

            if (bPlaySpawnEffect && gameObject.activeSelf)
            {
                if (PlayerNameText)
                    StartCoroutine(UnHidePlayerRigCoroutine());
            }
            else
            {
                if (PlayerMesh && PlayerMesh.PlayerRig)
                    PlayerMesh.PlayerRig.gameObject.SetActive(true);
            }
        }
    }

    // Yiming: So that it visually looks like player character pops out from the spawn FX
    IEnumerator UnHidePlayerRigCoroutine()
    {
        Instantiate(PlayerRespawnFXPrefab, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(0.3f);

        if (PlayerMesh && PlayerMesh.PlayerRig)
        {
            PlayerMesh.PlayerRig.gameObject.SetActive(true);
        }
    }

    void HideRigIfStillInLoading(BasePlayerMesh InPlayerMesh)
    {
        if (BaseGameInstance.Get.LevelLoader.LoadingUI)
        {
            InPlayerMesh.PlayerRig.gameObject.SetActive(!BaseGameInstance.Get.LevelLoader.LoadingUI.bIsShown);
        }
    }

    void UpdateHitstun()
    {
        if (RemainingHitstun > 0.0f)
        {
            RemainingHitstun -= Time.deltaTime;
            SetHitstunMode(RemainingHitstun > 0.0f);
        }
    }

    public void StopAnimation(ClipState.Transition InAnimation)
    {
        AnimScript.StopAnimation(InAnimation);
    }

    public override void StopAnimationState(Animancer.AnimancerState AnimState)
    {
        if (AnimState != null && AnimState.IsPlaying)
        {
            //AnimState.Stop();

            AnimState.StartFade(0.0f, 0.15f);

            //if (!AnimState.Layer.IsAnyStatePlaying())
            //    AnimState.Layer.Weight = 0;
        }
    }

    bool bAnimationPaused;
    Animancer.AnimancerState PausedAnimation;
    public void PauseAnimationStateTimer(Animancer.AnimancerState AnimState, float delay, float timer)
    {
        if (AnimState != null && AnimState.IsPlaying)
        {
            StartCoroutine(PauseAnimation(AnimState, delay));
            StartCoroutine(UnpauseAnimation(AnimState, timer));
        }
    }
    IEnumerator PauseAnimation(Animancer.AnimancerState AnimState, float timer)
    {
        yield return new WaitForSeconds(timer);
        AnimState.IsPlaying = false;
    }
    IEnumerator UnpauseAnimation(Animancer.AnimancerState AnimState, float timer)
    {
        yield return new WaitForSeconds(timer);
        AnimState.IsPlaying = true;
    }

    public void PauseAllAnimationTimer(float timer)
    {
        AnimScript.AnimancerComponent.Playable.PauseGraph();
        StartCoroutine(UnpauseAllAnimation(timer));
    }

    IEnumerator UnpauseAllAnimation(float timer)
    {
        yield return new WaitForSeconds(timer);
        AnimScript.AnimancerComponent.Playable.UnpauseGraph();
    }
    public override AnimancerState PlayAnimation(ClipState.Transition InAnimation, float FadeInDuration, float FadeOutDuration, HRAnimLayers InBodyMask, bool bStayOnAnim = false, System.Action onEndCallback = null, float InAnimSpeed = 1.0f)
    {
        if (AnimScript)
        {
            return AnimScript.PlayAnimation(InAnimation, FadeInDuration, FadeOutDuration, InBodyMask, bStayOnAnim, onEndCallback, InAnimSpeed);
        }
        else
        {
            return null;
        }
    }

    public override void HandlePoolInstantiate(BaseObjectPoolingComponent PoolingComponent)
    {
        base.HandlePoolInstantiate(PoolingComponent);

        if (!PoolingComponent.bReserved)
        {
            if (HRNetworkManager.IsHost())
            {
                SpawnSubtleTeleportFX_Server();
            }

            SetNewInteractState(InteractState.Free);

            if (HP)
            {
                HP.SetHP(HP.MaxHP, null);
            }

            GenerateNewName();

            InstantiateFactionData();
        }

        if (PlayerMesh && !CustomizationSystem)
        {
            RegenerateRig();
        }

        MovementComponent.FreezeMovement(false);

        (WeaponManager as IBasePool).HandlePoolInstantiate(PoolingComponent);
        if (PauseReceiver)
        {
            PauseReceiver.InitializeReceiver();
        }
    }


    [Mirror.ClientRpc]
    void SpawnSubtleTeleportFX_ClientRpc()
    {
        SpawnSubtleTeleportFX_Implementation();
    }

    void SpawnSubtleTeleportFX_Implementation()
    {
        if (SubtleTeleportFXPrefab && bHasHealthInsurance)
        {
            BaseObjectPoolManager.Get.InstantiateFromPool(SubtleTeleportFXPrefab, false, true, this.transform.position, false, this.transform.rotation);
        }
    }

    [Mirror.Server]
    void SpawnSubtleTeleportFX_Server()
    {
        if (netIdentity)
        {
            SpawnSubtleTeleportFX_ClientRpc();
        }

        SpawnSubtleTeleportFX_Implementation();
    }

    public override void HandleReturnToPool(BaseObjectPoolingComponent PoolingComponent)
    {
        base.HandleReturnToPool(PoolingComponent);

        bool bIsPlayer = CompareTag("Player");
        if (!bIsPlayer)
        {
            // Turn on interactable since ragdolls are interactable now due to sleeping customers
            SetInteractionCollisionEnabled_Server(true);
        }

        if (!PoolingComponent.bReserved)
        {
            if (HRNetworkManager.IsHost())
            {
                SpawnSubtleTeleportFX_Server();
            }
        }

        if (PlayerMesh && !CustomizationSystem)
        {
            RegenerateRig(false);
        }

        SetHitstunMode(false);
        if (sleepingFXInstance)
        {
            Destroy(sleepingFXInstance);
        }

        // Empty weapon manaegr of items
        (WeaponManager as IBasePool).HandleReturnToPool(PoolingComponent);

        if (PauseReceiver)
        {
            PauseReceiver.UnbindFromPauseManager();
        }

        if (HRNetworkManager.IsHost())
        {
            if (Ragdoll)
            {
                if (netIdentity)
                {
                    Ragdoll.ResetRagdoll_ClientRpc();
                }
                else
                {
                    Ragdoll.ResetRagdoll();
                }
            }

            damagingPlayers.Clear();
        }

        //reset the jank fix in stopping ai sinking in the floor
        if (AnimScript)
        {
            AnimScript.DontCheckMovementLocks = 0;
        }

        if (AttributeManager)
        {
            AttributeManager.RemoveAllAttributes();
        }

        OnCharacterDestroyedDelegate?.Invoke(this);
    }

    public bool OnBeforeSpawn(HRRandomEnemySpawner Spawner, ref SpawnedObjectInfo SpawnInfo)
    {
        return true;
    }
    public void OnAfterSpawn(HRRandomEnemySpawner Spawner, ref SpawnedObjectInfo SpawnInfo)
    {
        if (!SpawnInfo.bSpawned)
        {
            EnemySpawnerPrefabsDB.EnemyPrefab PrefabData = Spawner.CurrentSpawningEnemiesList[SpawnInfo.EnemyIndex];

            if (PrefabData.InteractDialogues != null && PrefabData.InteractDialogues.Count > 0 && PrefabData.InteractDialogues.TryGetRandomEntry(out string RandomConvoTitle))
            {
                SpawnInteractDialogue(RandomConvoTitle);
            }
        }
    }
    public bool OnBeforeAddToCache(HRRandomEnemySpawner Spawner, ref SpawnedObjectInfo SpawnInfo)
    {
        SpawnInfo.bReserved = customizationSystem != null;

        return true;
    }
    public void OnSpawnedFakeObjectReplaced(HRRandomEnemySpawner Spawner, GameObject RealObjectRef)
    {

    }

    private void RegenerateRig(bool bGenerate = true)
    {
        if (HRNetworkManager.IsHost())
        {
            RegenerateRig_ClientRpc(bGenerate);
        }
    }

    [Mirror.ClientRpc]
    private void RegenerateRig_ClientRpc(bool bGenerate)
    {
        RegenerateRig_Implementation(bGenerate);
    }

    private void RegenerateRig_Implementation(bool bGenerate)
    {
        if (PlayerMesh)
        {
            if (bGenerate)
            {
                PlayerMesh.RegenerateRig();
            }
            else
            {
                PlayerMesh.ClearRig();
            }
        }
    }

    [Mirror.TargetRpc]
    private void SetLayer_TargetRpc(Mirror.NetworkConnection conn, int layer)
    {
        this.gameObject.layer = layer;
    }

    public void ResetLayer()
    {
        this.gameObject.layer = OriginalLayer;
    }

    #region paused_and_override
    private void NetworkOverrideTarget_Implementation(bool paused)
    {
        this.MovementComponent.SetOverrideRotateTarget(paused);
    }
    private void NetworkOverrideTarget_Implementation(bool paused, Vector3 newRotateTarget)
    {
        this.MovementComponent.SetOverrideRotateTarget(paused, newRotateTarget);
    }

    public void SetAIMovementPaused(bool paused)
    {
        if (!bHasNetworkIdentity)
        {
            return;
        }

        if (isServer)
        {
            SetAIMovementPaused_Server(paused);
        }
        else
        {
            SetAIMovementPaused_Command(paused);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    void SetAIMovementPaused_Command(bool paused)
    {
        SetAIMovementPaused_Server(paused);
    }

    void SetAIMovementPaused_Server(bool paused)
    {
        SetAIMovementPaused_Implementation(paused);
    }

    void SetAIMovementPaused_Implementation(bool paused)
    {
        if (!AIRef || (CustomerAI && CustomerAI.bGreetMinigameActive) || !AIRef.AIController || !AIRef.AIController.AIMovement || AIRef.AIController.AIMovement.bIsPaused == paused) return;

        AIRef.AIController.AIMovement.SetPaused(paused);
        if (paused)
        {
            AIRef.AIController.AIMovement.StopMovement();
        }
        else
        {
            AIRef.AIController.AIMovement.SetAIMovementEnabled(true);
        }
    }
    public void NetworkOverrideTarget(bool paused, Vector3? newRotationTarget = null)
    {
        if (netIdentity == null)
        {
            return;
        }

        if (isServer)
        {
            if (newRotationTarget.HasValue)
            {
                NetworkOverrideTarget_ClientRPC1(paused, newRotationTarget.Value);
            }
            else
            {
                NetworkOverrideTarget_ClientRPC0(paused);
            }
        }
        else
        {
            if (newRotationTarget.HasValue)
            {
                NetworkOverrideTarget_Server1(paused, newRotationTarget.Value);
            }
            else
            {
                NetworkOverrideTarget_Server0(paused);
            }
        }
    }

    [Mirror.ClientRpc]
    private void NetworkOverrideTarget_ClientRPC0(bool paused)
    {
        NetworkOverrideTarget_Implementation(paused);
    }

    [Mirror.ClientRpc]
    private void NetworkOverrideTarget_ClientRPC1(bool paused, Vector3 rotationTarget)
    {
        NetworkOverrideTarget_Implementation(paused, rotationTarget);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkOverrideTarget_Server0(bool paused)
    {
        NetworkOverrideTarget_ClientRPC0(paused);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkOverrideTarget_Server1(bool paused, Vector3 rotationTarget)
    {
        NetworkOverrideTarget_ClientRPC1(paused, rotationTarget);
    }

    #endregion

    public void NetworkRemoveStunnedFXInstance()
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            NetworkRemoveStunnedFXInstance_ClientRPC();
        }
        else
        {
            NetworkRemoveStunnedFXInstance_Server();
        }
    }

    [Mirror.ClientRpc]
    private void NetworkRemoveStunnedFXInstance_ClientRPC()
    {
        NetworkRemoveStunnedFXInstance_Implementation();
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkRemoveStunnedFXInstance_Server()
    {
        NetworkRemoveStunnedFXInstance_ClientRPC();
    }

    private void NetworkRemoveStunnedFXInstance_Implementation()
    {
        Ragdoll?.ReceiveDestroyStunnedFXInstance();
    }

    public void SendPosition(Vector3 position)
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            SendPosition_ClientRPC(position);
        }
        else
        {
            SendPosition_Server(position);
        }
    }

    [Mirror.ClientRpc]
    private void SendPosition_ClientRPC(Vector3 position)
    {
        SendPosition_Implementation(position);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SendPosition_Server(Vector3 position)
    {
        SendPosition_ClientRPC(position);
    }

    private void SendPosition_Implementation(Vector3 position)
    {
        this.transform.position = position;
    }

    #region respawn_funcs

    #region Set Respawn Bed
    public void SetRespawnBed(HRBedComponent bedComponent)
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            SetRespawnBed_ClientRPC(bedComponent?.gameObject);
        }
        else
        {
            SetRespawnBed_Server(bedComponent?.gameObject);
        }
    }

    [Mirror.ClientRpc]
    private void SetRespawnBed_ClientRPC(GameObject bedComponent)
    {
        SetRespawnBed_Implementation(
            bedComponent?.GetComponent<HRBedComponent>());
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SetRespawnBed_Server(GameObject bedComponent)
    {
        SetRespawnBed_ClientRPC(bedComponent);
    }

    private void SetRespawnBed_Implementation(HRBedComponent component)
    {
        RespawnController.ReceiveSetRespawnBed(component);
    }
    #endregion

    #region Respawn
    public void NetworkRespawn()
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            NetworkRespawn_Server();
        }
        else
        {
            NetworkRespawn_Command();
        }
    }

    [Mirror.ClientRpc]
    private void NetworkRespawn_ClientRPC()
    {
        NetworkRespawn_ClientRPC_Implementation(RespawnPosition);
    }

    [Mirror.TargetRpc]
    private void NetworkRespawn_TargetRPC()
    {
        NetworkRespawn_TargetRPC_Implementation();
    }


    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkRespawn_Command()
    {
        NetworkRespawn_Server();
    }

    [Mirror.Server]
    private void NetworkRespawn_Server()
    {
        NetworkRespawn_TargetRPC();
        NetworkRespawn_ClientRPC();

    }

    private void NetworkRespawn_TargetRPC_Implementation()
    {
        NetworkShowDeathUI_Implementation(false);
        HandleRespawn();
    }

    public void HandleRespawn()
    {
        if (PixelCrushers.DialogueSystem.DialogueManager.IsConversationActive)
        {
            HandleDialogueStarted(null, true);
        }
        else
        {
            if (HP)
            {
                //HP.StartRespawnInvincibilityTimer(1f);
            }
            SetNewInteractState(HeroPlayerCharacter.InteractState.Free);
        }
        if (MovementComponent)
        {
            MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
            MovementComponent.FreezeMovement(false);
        }

        if (BaseWorldStreamManager.Get)
        {
            BaseWorldStreamManager.Get.StartBulkLoading(RespawnPosition);
        }

        if (WeaponManager.inCombat)
        {
            WeaponManager.SetInCombat(false);
        }
    }

    private void NetworkRespawn_ClientRPC_Implementation(Vector3 InPlayerPosition)
    {
        HeroInputComponent inputComponent = HRPC?.InputComponent as HeroInputComponent;

        if (!PauseReceiver.bIsPaused)
        {
            inputComponent?.SetButtonInputEnabled(true);
            inputComponent?.SetMouseInputEnabled(true);
            SetInputEnabled(true);
        }

        RespawnController?.ReceiveRespawn(InPlayerPosition);
    }
    #endregion

    #region Fast Travel
    Vector3 travelPosition;
    public bool StartFastTravel(Vector3 targetPosition)
    {
        HRGameInstance hrGameInstance = BaseGameInstance.Get as HRGameInstance;
        if (hrGameInstance == null) return false;

        travelPosition = targetPosition;
        // start fade
        float fadeDuration = 1.0f;
        hrGameInstance.FadeManager.Fade(false, fadeDuration, Color.black, true);
        hrGameInstance.FadeManager.FinishFadeDelegate += HandleTravelFinishFade;

        // disable input
        HeroInputComponent inputComp = InputComponent as HeroInputComponent;
        if (inputComp)
        {
            inputComp.SetButtonInputEnabled(false);
            inputComp.SetMouseInputEnabled(false);
            inputComp.SetEnabled(false);
        }
        SetInputEnabled(false);

        if (HP)
        {
            HP.SetInvincible(true);
        }

        // close phone UI
        HRPhoneManager phoneManager = hrGameInstance.PhoneManager;
        if (phoneManager)
        {
            phoneManager.HidePhone();
        }
        return true;
    }

    private void HandleTravelFinishFade(BaseFadeManager fadeManager)
    {
        fadeManager.FinishFadeDelegate -= HandleTravelFinishFade;
        FastTravel(travelPosition + Vector3.up * 1.0f, out bool bHasChunkToLoad);

        if (!bHasChunkToLoad)
        {
            TravelFadeIn();
        }
    }

    private void TravelFadeIn()
    {
        float fadeDuration = 0.5f;
        BaseGameInstance.Get.FadeManager.Fade(true, fadeDuration, Color.black, false);
    }

    public void FastTravel(Vector3 targetPosition, out bool bHasChunkToLoad)
    {
        FastTravel_Local(targetPosition, out bHasChunkToLoad);
        if (HRNetworkManager.IsHost())
            FastTravel_Server(targetPosition);
        else
            FastTravel_Command(targetPosition);
    }

    [Mirror.Command]
    void FastTravel_Command(Vector3 targetPosition)
    {
        FastTravel_Server(targetPosition);
    }

    void FastTravel_Server(Vector3 targetPosition)
    {
        FastTravel_Implementation(targetPosition);
        FastTravel_ClientRpc(targetPosition);
    }

    [Mirror.ClientRpc]
    void FastTravel_ClientRpc(Vector3 targetPosition)
    {
        if (!HRNetworkManager.IsHost())
        {
            FastTravel_Implementation(targetPosition);
        }
    }

    void FastTravel_Implementation(Vector3 targetPosition)
    {
        transform.position = targetPosition;
        Position = targetPosition;
        Physics.SyncTransforms();
        if (PlayerCamera && PlayerCamera.FollowerComponent)
        {
            PlayerCamera.FollowerComponent.TeleportTo(targetPosition);
        }

        if (Ragdoll)
        {
            Ragdoll.ResetRagdoll();
        }

        if (MovementComponent)
        {
            if (MovementComponent.MotionSmoother)
            {
                MovementComponent.MotionSmoother.ResetMotionTransform();
            }
        }

        //TODO: vfx & sfx

        OnSpawn();
    }

    void FastTravel_Local(Vector3 targetPosition, out bool bHasChunkToLoad)
    {
        if (PixelCrushers.DialogueSystem.DialogueManager.IsConversationActive)
        {
            HandleDialogueStarted(null, true);
        }
        else
        {
            SetNewInteractState(HeroPlayerCharacter.InteractState.Free);
        }

        if (HP)
        {
            HP.SetInvincible(false);
        }

        if (HRPC)
        {
            HeroInputComponent inputComponent = HRPC.InputComponent as HeroInputComponent;
            if (inputComponent)
            {
                if (!PauseReceiver.bIsPaused)
                {
                    inputComponent.SetButtonInputEnabled(true);
                    inputComponent.SetMouseInputEnabled(true);
                    SetInputEnabled(true);
                }
            }
        }

        if (MovementComponent)
        {
            MovementComponent.SetMoveSpeed(BaseMoveType.JOGGING);
            MovementComponent.SetCanSwim(false);
            MovementComponent.SetCanSwimAfterDelay(true, 1.0f);
            MovementComponent.ResetLatestOnGroundPosition(targetPosition);
            MovementComponent.FreezeMovement(false);
        }

        if (AnimScript)
        {
            if (AnimScript.FootstepFX && AnimScript.FootstepFX.CollidingWater)
            {
                ExitedWater(AnimScript.FootstepFX.CollidingWater);
                AnimScript.FootstepFX.CollidingWater = null;
            }

            if (AnimScript.FootstepListener)
            {
                AnimScript.SetFootstepsEnabled(true);
            }
        }

        if (BaseWorldStreamManager.Get)
        {
            bHasChunkToLoad = BaseWorldStreamManager.Get.StartBulkLoading(targetPosition);
            if (bHasChunkToLoad)
            {
                BaseWorldStreamManager.Get.OnBulkLoadFinish += HandleBulkLoadingFinish;
                MovementComponent.FreezeMovement(true, false);
            }
        }
        else
        {
            bHasChunkToLoad = false;
        }
    }

    private void HandleBulkLoadingFinish(BaseWorldStreamManager manager)
    {
        BaseWorldStreamManager.Get.OnBulkLoadFinish -= HandleBulkLoadingFinish;
        MovementComponent.FreezeMovement(false, true);
        TravelFadeIn();
    }
    #endregion

    #endregion

    #region checkpoint_funcs

    public void SetLatestCheckpoint(HRPlayerCheckpoint checkpoint)
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            SetLatestCheckpoint_ClientRPC(checkpoint?.gameObject);
        }
        else
        {
            SetLatestCheckpoint_Server(checkpoint?.gameObject);
            SetLatestCheckpoint_Implementation(checkpoint);
        }
    }

    [Mirror.ClientRpc]
    private void SetLatestCheckpoint_ClientRPC(GameObject checkpoint)
    {
        SetLatestCheckpoint_Implementation(
            checkpoint?.GetComponent<HRPlayerCheckpoint>());
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SetLatestCheckpoint_Server(GameObject checkpoint)
    {
        SetLatestCheckpoint_ClientRPC(checkpoint);
    }

    private void SetLatestCheckpoint_Implementation(HRPlayerCheckpoint checkpoint)
    {
        RespawnController.ReceiveSetLatestCheckpoint(checkpoint);
    }

    #endregion

    public void NetworkSetTargetOutpostTent(GameOutpostTent outpost)
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            NetworkSetTargetOutpostTent_ClientRPC(outpost.gameObject);
        }
        else
        {
            NetworkSetTargetOutpostTent_Server(outpost.gameObject);
        }
    }

    [Mirror.ClientRpc]
    private void NetworkSetTargetOutpostTent_ClientRPC(GameObject outpost)
    {
        NetworkSetTargetOutpostTent_Implementation(outpost);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkSetTargetOutpostTent_Server(GameObject outpost)
    {
        NetworkSetTargetOutpostTent_ClientRPC(outpost);
    }

    private void NetworkSetTargetOutpostTent_Implementation(GameObject outpost)
    {
        GameOutpostTent outpostComponent = outpost?.GetComponent<GameOutpostTent>();

        if (outpostComponent)
        {
            OutpostTargetAI.ReceiveTargetOutpostTent(outpostComponent);
        }
    }

    public void HandleBeginChannel(AHeroChannel channel)
    {
        if (channel is HeroReviveController)
        {
            NetworkSetBeingRevived(true);
        }
    }

    public void HandleEndChannel(AHeroChannel channel, ChannelRemovalData removalData)
    {
        if (channel is HeroReviveController)
        {
            HeroReviveController.ReviveRemovalData reviveRemovalData = removalData as HeroReviveController.ReviveRemovalData;
            if (heroDeathReviveDelayController.BeingRevived
                && reviveRemovalData.updateStateOfTarget)
            {
                NetworkSetBeingRevived(false);
            }
        }
    }

    public void SetMovingOnConveyor(bool moving)
    {
        _movingOnConveyor = moving;
    }

    public void MoveOnConveyor(Vector3 velocity)
    {
        if (MovementComponent.CharacterController)
        {
            MovementComponent.ApplyConveyorVelocity(velocity);
            MovementComponent.CharacterController.Move(velocity);
        }
    }

    public GameObject GetOwner()
    {
        return this.gameObject;
    }

    public void Warp(Vector3 Position)
    {
        if (HRNetworkManager.IsHost())
        {
            OnWarp_ClientRpc();
        }
        else
        {
            OnWarp_Command();
        }

        BaseAIMovement AIMove = GetComponent<BaseAIMovement>();
        if (AIMove)
        {
            AIMove.Warp(Position);
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    private void OnWarp_Command()
    {
        OnWarp_ClientRpc();
    }


    [Mirror.ClientRpc]
    private void OnWarp_ClientRpc()
    {
        if (CurrentSeat)
        {
            CurrentSeat.Unseat(false, true, false);
        }

        BaseAIMovement AIMove = GetComponent<BaseAIMovement>();
        if (AIMove)
        {
            AIMove.StopMovement(true, true);
        }
    }

    private void UpdateKillList(int characterIndex)
    {
        if (characterIndex >= 0)
        {
            if (killedCharacterIndices.Count >= 5) // 5 is an arbitrary number to limit list size for raid makeup sampling
            {
                killedCharacterIndices.Remove(0);
            }
            killedCharacterIndices.Add(characterIndex);
        }
    }

    public List<int> KillList => killedCharacterIndices;

    [Mirror.TargetRpc]
    public void SendKillListToServer_TargetRpc()
    {
        ((HRGameManager)BaseGameManager.Get).RaidManager.FillRaid_Command(killedCharacterIndices, this);
    }

    //private class 


    // Ideally, this would be handled elsewhere, but sense the GameInstance isn't networked, it is impossible to send this data in any meaningful way
    #region Combat Music

    [Mirror.TargetRpc]
    public void ReplaceCombatMusic_TargetRpc(Mirror.NetworkConnection Target, string musicTrack)
    {
        var MusicManager = ((HRGameInstance)BaseGameInstance.Get).MusicManager;

        var active = MusicManager.CurrentMusicLayer.CancelTrack(musicTrack);

        if (active)
        {
            MusicManager.RequestPlayCurrentLayerPreviousMusic(MusicManager.CurrentMusicLayer.Priority);
        }
    }


    [Mirror.TargetRpc]
    public void PlayCombatMusic_TargetRpc(Mirror.NetworkConnection Target)
    {
        HRCombatManager.Get.PlayCombatMusic();
    }


    [Mirror.TargetRpc]
    public void PlaySpecificCombatMusic_TargetRpc(Mirror.NetworkConnection Target, GameObject InObject, bool Initialize)
    {
        if (!InObject)
        {
            return;
        }
        var CombatComponent = InObject.GetComponent<HRCombatComponent>();

        if (CombatComponent)
        {
            //if (Initialize) HRCombatManager.Get.PlayCombatMusic();

            HRCombatManager.Get.PlaySpecificCombatMusic(CombatComponent.MusicInfo);
        }
    }


    [Mirror.TargetRpc]
    public void StopCombatMusic_TargetRpc(Mirror.NetworkConnection Target)
    {
        HRCombatManager.Get.StopCombatMusic();
    }

    #endregion


    public override void Disconnect_TargetRpc(NetworkConnection NetworkConnect)
    {
        base.Disconnect_TargetRpc(NetworkConnect);
        HRNetworkManager.Get.StopClient();
    }
}

