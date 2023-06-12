using PixelCrushers.DialogueSystem;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Mirror;

[System.Serializable]
public class HRBubbleUI
{
    public HRBubbleUIType Type;
    public BaseAnimatedWorldUI UIObject;
    public HRBubbleAnimData[] Anims;

    public AnimationClip GetClip(HRBubbleUIStatus InStatus)
    {
        for (int i = 0; i < Anims.Length; ++i)
        {
            if (Anims[i].Status == InStatus)
            {
                return Anims[i].Clip;
            }
        }
        return null;
    }
}

[System.Serializable]
public struct HRBubbleAnimData
{
    public HRBubbleUIStatus Status;
    public AnimationClip Clip;
}

public enum HRCustomerState { NONE, GOINGTOPLOT, WANDERINGPLOT, GOINGTOSHOP, WANDERINGSHOP, GOINGTODISPLAYCONTAINER, BROWSING, LOOKINGFORCASHREGISTER, WAITINGINLINE, LEAVINGSHOP, GOINGHOME };

public enum HRBubbleUIType { Greet, WaitingOutside, Help, NoRegister, NoDisplay, EmptyDisplay };

public enum HRBubbleUIStatus { Start, Idle, LowStart, LowIdle, Success, Failure };

public class HRCustomerAI : HRBaseAI
{
    public static T GetItemFromList<T>(List<BaseWeapon> InWeaponList, bool bFilterItemType = false, HRRoomItemType ItemType = HRRoomItemType.DISPLAY, string[] additionalAttributes = null, float InMaxDistance = -1, Transform OriginObject = null)
    {
        // todo use pooled list because this is used very often
        List<T> ItemList = new List<T>();

        if (InWeaponList != null)
        {
            for (int i = InWeaponList.Count - 1; i >= 0; --i)
            {
                if (InWeaponList[i])
                {
                    T Item = InWeaponList[i].GetComponent<T>();
                    if (Item != null)
                    {
                        if (InWeaponList[i].OwningInventory == null)
                        {
                            HRRoomItem RoomItem = InWeaponList[i].RoomItemRef;

                            bool bPassedFilter = false;
                            if (bFilterItemType)
                            {
                                if (RoomItem && RoomItem.bFree && RoomItem.RoomItemType == ItemType)
                                {
                                    bool success = true;
                                    if (additionalAttributes != null)
                                    {
                                        for (int k = 0; k < additionalAttributes.Length; k++)
                                        {
                                            if (!RoomItem.AdditionalAttributes.Contains(additionalAttributes[k]))
                                            {
                                                success = false;
                                            }
                                        }
                                    }

                                    bPassedFilter = success;
                                }
                            }
                            else
                            {
                                if (RoomItem && !RoomItem.bFree)
                                {
                                    continue;
                                }
                                else
                                {
                                    bPassedFilter = true;
                                }
                            }

                            if (bPassedFilter)
                            {
                                if (InMaxDistance == -1 || OriginObject == null ||
                                    (Vector3.Distance(InWeaponList[i].transform.position, OriginObject.transform.position) <= InMaxDistance))
                                {
                                    ItemList.Add(Item);
                                }
                            }
                        }
                    }
                }
                else
                {
                    InWeaponList.RemoveAt(i);
                }
            }
        }

        if (ItemList.Count > 0)
        {
            return ItemList[Random.Range(0, ItemList.Count)];
        }

        return default(T);
    }

    bool bClientMode = false;

    public struct HRShopOrder
    {
        public int ItemID;
        public int Amount;

        public HRShopOrder(int InItemID, int InAmount)
        {
            ItemID = InItemID;
            Amount = InAmount;
        }
    }

    public delegate void CustomerAISignature(HRCustomerAI InCustomer);
    public CustomerAISignature OnDestroyDelegate;
    public CustomerAISignature OnStunDelegate;
    public CustomerAISignature OnCustomerServedDelegate;
    public CustomerAISignature OnCustomerRemovedFromRegisterDelegate;

    public delegate void CustomerAIInteractionDelegate(HRCustomerAI InCustomer, BaseScripts.BaseInteractionManager Interactor);
    public CustomerAIInteractionDelegate OnTapInteractionDelegate;

    public delegate void CustomerAIBoolSignature(HRCustomerAI InCustomer, bool bUnhappy);
    public CustomerAIBoolSignature OnLeaveShopDelegate;
    public CustomerAIBoolSignature OnLeavePlotDelegate;
    public CustomerAIBoolSignature OnShoppingBasketChangedDelegate;

    public delegate void CustomerAIQueueSignature(HRCustomerAI InCustomer, BaseQueueSpot InSpot);
    public CustomerAIQueueSignature OnSpotRemovedDelegate;

    public delegate void CustomerAIBudgetSignature(HRCustomerAI InCustomer, float InBudget);
    public CustomerAIBudgetSignature BudgetChangedDelegate;

    [Header("Core Values")]
    public string CustomerType;
    public HeroPlayerCharacter OwningPlayerCharacter;
    public Transform BTParent;
    public BaseBehaviorTree MainCustomerBTPrefab;
    public BaseBehaviorTree CustomerBTPrefab;
    public BaseBehaviorTree MainCustomerBT;
    public BaseBehaviorTree CustomerBT;
    [Tooltip("Behavior tree used for retail stores (i.e. browsing items and bringing them to the register to buy)")]
    public BaseBehaviorTree RetailBT;
    [Tooltip("Behavior tree used for when the customer needs to place an order (e.g. fast food, workshop, etc)")]
    public BaseBehaviorTree OrderBT;
    [Tooltip("Behavior tree used for when the customer needs to be seated in a restaurant.")]
    public BaseBehaviorTree RestaurantBT;
    public BaseBehaviorTree MeleeBT;
    public BaseBehaviorTree RangeBT;
    public BaseAIController OwningAIController;
    [Tooltip("Database that determines how this customer responds to stimuli that affect the rating.")]
    public HRShopRatingModifierDB ShopRatingModifierDB;
    [Tooltip("Database that overrides the generic dialogue for reviews, giving customers more personality.")]
    public HRShopRatingDialogueDB OverrideDialogueDB;

    // Temp way of displaying what the customer is doing.
    public TMPro.TMP_Text CustomerStateText;

    // Use SetTargetShopManager instead of setting this.
    [Mirror.SyncVar(hook = nameof(HandleShopManagerChanged))]
    public HRShopManager TargetShopManager;

    [HideInInspector]
    public HRShopManager LastTargetShopManager;

    public GameObject ShoppingBasketGameObject;

    public AnimationCurve HygieneToTrashDropChance;

    // X axis = Budget, Y axis = number of items the customer will pick up
    public AnimationCurve BudgetToNumItemsCurve;

    // Does this customer have a shopping basket?
    [SerializeField, Mirror.SyncVar(hook = nameof(HandleShoppingBasketChanged_Hook))]
    bool bHasShoppingBasket = false;

    // Use SetTargetShopPlot instead of setting this.
    [HideInInspector, Mirror.SyncVar]
    public HRShopPlot TargetShopPlot;

    [Header("Needs Values")]
    public HRNeedManager CustomerNeeds;
    public float DamageWhenStarving = 2;
    public float StarvingDamageInterval = 1;
    public float DamageWhenThirsty = 2;
    public float ThirstyDamageInterval = 1;
    [Range(0f, 1f)]
    public float TiredMovementSpeedModifier = 0.4f;
    public GameObject BladderZeroMessPrefab;
    public bool bCanDropBladderMess = true;
    private bool bBoundNeeds = false;
    private bool bSleeping = false;
    public float MaxNeedsItemSearchingRadius = 10f;
    public float NeedsItemSearchingTick = 2f;

    [Header("Animation")]
    public string customBrowseAnimation;
    public string customBrowseFailAnimation;
    public string customBrowseSuccessAnimation;
    public string customWalkAndHoldAnimation;
    public string customHoldIdleAnimation;
    public string[] customWaitIdleAnimations;

    [Header("Shopping Values")]
    public float MaxBudget = 100.0f;
    public float MaxBudgetMultiplier = 4.0f;
    public float LeavingBudgetThreshold = 50.0f; // The customer will leave once they drop below this threshold after buying.

    public int MaxPlotWanderTimes = 0; // How many times the customer will wander the plot after visiting a shop before leaving the plot.

    public float StartingBudgetSize = 0;
    int BudgetReloads = 0;

    [Tooltip("Minimum amount of items that can be in Shopping Cart.")]
    public int ShoppingCartMin = 1;

    [Tooltip("Amount of items that can be in Shopping Cart.")]
    public int ShoppingCartMax = 1;

    public HRDemandDatabase CustomerDemandDB;
    public HREmotionUI EmotionComponent;

    // Runtime
    [Mirror.SyncVar]
    public bool bDoneShopping = false;
    private int ShoppingListSize;
    private int NumItems_Server;
    [Mirror.SyncVar]
    private string CurrentWantedCategoryID = "some items";

    [HideInInspector]
    [Mirror.SyncVar]
    public HRCashRegister TargetRegister;

    [HideInInspector] public SyncList<BaseWeapon> ShoppingCart = new SyncList<BaseWeapon>();
    [HideInInspector] public List<int> ShoppingCartIDs = new List<int>();
    [HideInInspector] public Dictionary<int, int> ShoppingCartQuantity = new Dictionary<int, int>();
    public delegate void OnShoppingCartUpdatedSignature(HRCustomerAI HRCustomer);
    public OnShoppingCartUpdatedSignature OnShoppingCartUpdatedDelegate;

    // Budgeting
    public float CurrentBudget
    {
        get
        {
            return Budget;
        }
    }
    [Mirror.SyncVar(hook = "HandleBudgetChanged_Hook")]
    private float Budget = 0f; // How much customer is willing to spend
    private float CurrentHeldBudget; // How much we are carrying in the budget at the moment

    public float MinBudgetDropFraction = 0.035f;
    public float MaxBudgetDropFraction = 0.05f;
    private int MoneyItemID = 1173;
    private HRItemCategorySO CurrentWantedCategory;



    [Header("Patience Controls")]
    [Tooltip("Happiness percentage level (0-1) at which the Customer will go to the register even if they haven't found all their desired items.")]
    [Range(0f, 1.0f)]
    public float PatienceToLeaveThreshold = 0.5f;
    public bool bUseImpatienceTimer = false;
    [Tooltip("Happiness percentage (0-1) level at which the Customer will display what they're looking for.")]
    [Range(0f, 1.0f)]
    public float ImpatienceThreshold = 0.7f;
    [Tooltip("Amount of time until Customer displays what they're looking for.")]
    public float ImpatienceTime = 5f;
    float ImpatienceTimer = -1f;
    [Tooltip("Percentage of lost patience to restore when a customer is admitted into the shop.")]
    [Range(0f, 1.0f)]
    public float MaxPatienceToRestore = 0.5f;

    [Range(0f, 1f)]
    public float LowHappinessWarningThreshold;

    public float HappinessBoostWhenOrderReceived = 0.25f;

    public float UnhappinessFromRagdoll;
    [HideInInspector, System.NonSerialized]
    public bool bSearching = false;
    bool bHasAsked = false;
    public bool bDialogueIsOpen { get; private set; }

    [Header("Wandering")]
    public int MinWanderTimesBeforeBuying = 1;
    public int MaxWanderTimesBeforeBuying = 3;

    [Header("Special State Parameters")]
    [Range(0, 3)]
    public int MinSpecialStates = 2;
    [Range(0, 3)]
    public int MaxSpecialStates = 2;
    public enum SpecialCustomerState { Greet, Search, Discuss }
    public List<SpecialCustomerState> CustomerStates;

    [Header("Customer AI Modes")]
    public bool bUseSpecialModes = true;
    public bool bCanGreet;
    public bool bCanSearch;
    public bool bCanDiscuss;
    public bool bCanPersuade;
    private bool bCanInteract = true;
    private bool bCanOverworldGreet = false;

    [Header("Greeting State Parameters")]
    public AnimationCurve GreetProbabilityCurve;
    public AnimationCurve GreetShopkeepingXPCurve;
    //TODO: would be great if this wasn't hard coded :P all player skills go to 100
    const int MaxShopkeepingLevel = 100;
    [Range(1, MaxShopkeepingLevel)]
    public int CustomerPersuasionLevel = 1;
    private bool bPersuaded = false;
    public float BaseGreetMinigameDifficulty = 5.0f;
    public float TimeBeforeLeavingGreet = 15.0f;
    private float CurrentGreetWaitTime = 0.0f;
    [SerializeField]
    [Tooltip("How close this customer needs to be to the greet point to enter the greet state.")]
    private float GreetProximity;
    [SerializeField]
    [Tooltip("How close this customer needs to be to the shop for    the minigame to activate")]
    private float MaxDistanceToShopThreshold = 500;
    [HideInInspector]
    public bool bGreetMinigameActive = false;
    private bool bUseGreetTimer = false;
    private bool bQueued = false;
    public bool bGreet { get; private set; }
    public HRGreetingDialogueStarter GreetingDialogueStarter;

    public float PositiveInteractionRatingModifier = 2;

    [Header("Searching State Parameters")]
    [Tooltip("Probability of requesting an item."), Range(0f, 1f)]
    public float searchModeProbability = 0.15f;
    public bool bOverrideSearchModeProbability = false;
    public float overrideSearchModeProbability = 1f;
    public bool bAcceptAnything = false;
    public bool bUseSearchingTimer = false;
    private bool bFollowing = false;
    private bool bGiving = false;
    private string searchWeaponPickedResult;
    private float SearchWeaponPrice = 0;
    private float SearchWeaponBonusPrice = 0;
    public float MinBrowseTime = 0.0f;
    public float MaxBrowseTime = 0.0f;
    [Tooltip("Price that the customer will pay if the item they get matches the descriptor they were looking for.")]
    public float ItemFoundPriceModifier = 2f;
    [Range(0, 1)] public float WaveChance = 0.5f;
    [Range(0, 1)]
    public float SearchingPatienceExcellentPercent = 0.30f;
    [Range(0, 1)]
    public float SearchingPatienceSuccessPercent = 0.20f;
    [Range(0, 1)]
    public float SearchingPatienceFailurePercent = 0.15f;
    //A timer used for when the customer is waiting to be talked for the first time.
    float SearchingTimer;
    //A timer used when we want customers to leave if they don't get their requested item in time
    float WaitingForRequestedItemTimer;
    public HRSearchingDialogueStarter SearchingDialogueStarter;
    private HeroPlayerCharacter InteractedPlayer;
    private BaseScripts.BaseInteractionManager PlayerInteractionManager;
    private BaseInventory CurrentInventory;
    private BaseInventoryUI CurrentInventoryUI;

    public List<float> lastSeenBeautyValues = new List<float>();

    [Header("Aggro State Parameters")]
    [Range(0, 1)]
    public float ChanceToAggro = 0.0f;

    [Header("Barter Parameters")]
    public int MaximumBarterPercent = 800;

    [Header("Show Icon Toggles")]
    public bool bShowWaitingIcon = true;
    public bool bShowGreetIcon = true;
    public bool bShowSearchIcon = true;
    public bool bShowDecideIcon = true;
    public bool bShowLeaveIcon = true;

    [Header("Waiting In Queue Parameters")]
    [SerializeField] private float MinTimeToWait = 3.0f;
    [SerializeField] public float MaxTimeToWait = 10.0f;
    private float CurrentQueueDelay;
    private bool bInQueue = false;

    [Header("Mess Controls")]
    public HRMessGenerator MessGenerator;
    [Tooltip("Ignores attractiveness bonuses, usually associated with messes.")]
    public bool bIgnoreAttractivity = false;

    public float LikeScorePatienceThreshold = 0.5f;
    public float LikeScoreThreshold = 4.0f;

    [HideInInspector]
    public float DemandAttractivityModifier = 0.0f;
    List<HRItemAttractivity> AttractivityItemsInRange = new List<HRItemAttractivity>();

    private bool bIsPaused = false;

    [Mirror.SyncVar(hook = "CustomerStateChanged_Hook")]
    public HRCustomerState CurrentVisibleState = HRCustomerState.NONE;

    [Mirror.SyncVar(hook = "HandleShowNeeds_Hook")]
    public bool bShowNeedsUI;

    public BaseScripts.BaseInteractable Interactable
    {
        get
        {
            return Player.Interactable;
        }
    }

    [HideInInspector]
    public BaseQueueSpot SpotInQueue = null;

    public bool bWantsToShop = true;
    public bool bLeavingShop;

    public HRBarterDialogueStarter BarterDialogueStarter;
    public float CurrentTotalPrice => currentTotalPrice;

    [Mirror.SyncVar]
    private float currentTotalPrice;

    public HRCharacterPathFollower CharacterPathFollower;
    public HRCombatComponent CombatComponent;

    [Space]

    bool bWaitingOutside;

    [HideInInspector]
    public int MaxArrivalHour = 22;
    public int MinPatiencePeriod = 2;
    public int MaxPatiencePeriod = 4;
    public float MinWaitTimePerMovement = 0.5f;
    public float MaxWaitTimePerMovement = 1.5f;
    [HideInInspector]
    public int HoursWaited;
    private int CurrentPatiencePeriod;
    private HRDayManager DayManager;

    public HROrdererComponent OrdererComponent;
    public float ChanceToRetail = 0.5f;
    public float ChanceToOrder = 0.5f;
    public float ChanceToRestaurant = 0.5f;

    public HRFloatingUIComponent HappinessSliderUI;
    public UnityEngine.UI.Image HappinessSlider;
    public bool bShowingHappiness;
    public bool bShouldShowHappiness;
    public Color MaxHappinessColor;
    public Color MinHappinessColor;

    HRBubbleUI CurrentBubbleUI;
    [FoldoutGroup("Bubble UI")]
    public HRBubbleUI[] BubbleUIDatas;

    //public GameObject InteractDialogue;

    [Header("Customer Minigames")]
    public HRMinigameSystem GreetMinigame;

    public HROffscreenCustomerSpawner OffscreenSpawner { get; set; }

    public bool Initialized { get; private set; }

    public override HeroPlayerCharacter Player => OwningPlayerCharacter;

    private GameObject targetGameObject;
    public override GameObject TargetGameObject => targetGameObject;
    private List<GameObject> targetList = new List<GameObject>();
    public override List<GameObject> TargetList => targetList;
    private HRHostileState currentHostileState;
    public override HRHostileState CurrentHostileState => currentHostileState;

    private Vector3 lastSeenLocation;
    public override Vector3 LastSeenLocation => lastSeenLocation;

    [Header("Combat Parameters")]
    public BaseEnemyStarterWeaponDataAsset StartingWeaponData;

    public override BaseWeapon CurrentWeaponInstance => currentWeaponInstance;
    protected BaseWeapon currentWeaponInstance;

    public BaseEnemyStarterWeaponDataAsset StartingSideWeaponData;

    public BaseSenseComponent[] Senses;

    [SerializeField, FoldoutGroup("Barks")]
    private HRBarkData onAggroBark;
    [SerializeField, FoldoutGroup("Barks")]
    private HRBarkData onCombatBark;
    [SerializeField, FoldoutGroup("Barks")]
    private HRBarkData onTargetDefeatedBark;

    public override HRBarkData OnAggroBark => onAggroBark;
    public override HRBarkData OnCombatBark => onCombatBark;
    public override HRBarkData OnTargetDefeatedBark => onTargetDefeatedBark;

    private Coroutine combatBarkCoroutine;

    [Mirror.SyncVar(hook = "HandleCustomerInteractableChanged_Hook")]
    public bool bCustomerInteractableEnabled = true;

    float LastTimeVisitedShop;
    public float ShopVisitCooldown = 10.0f; // How long the customer waits before entering another shop

    public BaseEQSChecker EQSChecker;
    HashSet<HRRoomItem> OldTickNeedsAffectingItems;
    HashSet<HRRoomItem> NewTickNeedsAffectingItems;
    HashSet<HRRoomItem> OneshotNeedsAffectedItems;
    float NeedsItemSearchingTimer = -1;

    [HideInInspector]
    public bool bItemFound;

    private BaseWeapon searchWeaponPicked;

    public AudioClip EnterShopAudioClip;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (StartingWeaponData)
        {
            StartingWeaponData.PoolWeaponsOnce();
        }

        if (StartingSideWeaponData)
        {
            StartingSideWeaponData.PoolWeaponsOnce();
        }
    }

    public void Initialize(bool CanGreet = true, bool CanSearch = true, bool CanDiscuss = true, bool Randomize = false, bool OverworldGreet = false)
    {
        CustomerStates = new List<SpecialCustomerState>();
        List<SpecialCustomerState> AvailableStates = new List<SpecialCustomerState>();

        //if (!bCanOverworldGreet) bCanGreet = true;

        if (bUseSpecialModes)
        {
            CanSearch = CustomerDemandDB ? CustomerDemandDB.LikedCategories.Count > 0 : false;
            if (bCanSearch && CanSearch)
            {
                float randomRoll = Random.Range(0f, 1f);
                CanSearch = bOverrideSearchModeProbability ? randomRoll <= overrideSearchModeProbability : randomRoll <= searchModeProbability;
            }
            if (Randomize)
            {
                if (CanGreet && bCanPersuade && !OverworldGreet) { AvailableStates.Add(SpecialCustomerState.Greet); }
                if (CanSearch && bCanSearch) AvailableStates.Add(SpecialCustomerState.Search);
                if (CanDiscuss && bCanDiscuss) { AvailableStates.Add(SpecialCustomerState.Discuss); }

                int NumStates = Random.Range(MinSpecialStates, MaxSpecialStates);

                for (int i = 0; i < NumStates; i++)
                {
                    if (AvailableStates.Count == 0) { break; }
                    int Index = Random.Range(0, AvailableStates.Count);
                    CustomerStates.Add(AvailableStates[Index]);
                    AvailableStates.RemoveAt(Index);
                }
            }
            else
            {
                if (CanGreet && bCanPersuade && !OverworldGreet) { CustomerStates.Add(SpecialCustomerState.Greet); }
                if (CanSearch && bCanSearch) { CustomerStates.Add(SpecialCustomerState.Search); }
                if (CanDiscuss && bCanDiscuss) { CustomerStates.Add(SpecialCustomerState.Discuss); }
            }
        }

        InteractedPlayer = null;
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bGreet", CustomerStates.Contains(SpecialCustomerState.Greet));
        OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bSearch", CustomerStates.Contains(SpecialCustomerState.Search));
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bSearch", CustomerStates.Contains(SpecialCustomerState.Search));

        Initialized = true;
        bPersuaded = false;
        bGreet = false;
        bQueued = false;
        bCanInteract = true;
        bDoneShopping = false;
        bCanOverworldGreet = OverworldGreet;
        ShopToPersuadeTo = null;

        if (!bBoundNeeds)
        {
            CustomerNeeds.OnNeedUpdatedDelegate += OnNeedsUpdated;
            bBoundNeeds = true;
        }
        CurrentGreetWaitTime = 0.0f;
        OwningPlayerCharacter.WeaponManager.enabled = true;

        if (!bShowingHappiness)
        {
            SetHappinessMeterActive(false);
        }
    }

    private void HandleShopManagerChanged(HRShopManager oldShop, HRShopManager newShop)
    {
        Debug.Log("ShopManager Changed: " + (newShop ? newShop.gameObject.name : "null"));
    }

    HRShoppingBasketStation CurrentBasketStation = null;

    [Mirror.Server]
    public void SetShoppingCartBasket_Server(HRShoppingBasketStation InBasketStation)
    {
        if (InBasketStation)
        {
            if (CurrentBasketStation)
            {
                return;
            }

            CurrentBasketStation = InBasketStation;
            bHasShoppingBasket = true;
        }
        else
        {
            if (CurrentBasketStation)
            {
                // Return
                ReturnShoppingBasket();
            }
        }
    }

    void ReturnShoppingBasket()
    {
        if (CurrentBasketStation)
        {
            CurrentBasketStation.ReturnBasket_Server(this);
        }

        CurrentBasketStation = null;

        bHasShoppingBasket = false;
    }

    private void HandleShoppingBasketChanged_Hook(bool bOldHasShoppingBasket, bool bNewHasShoppingBasket)
    {
        UpdateShoppingBasketVisuals(bNewHasShoppingBasket);

        UpdateShoppingCartMinMax();

        OnShoppingBasketChangedDelegate?.Invoke(this, bNewHasShoppingBasket);
    }

    public bool HasShoppingBasket()
    {
        return bHasShoppingBasket;
    }

    void UpdateShoppingBasketVisuals(bool bInShow)
    {
        if (ShoppingBasketGameObject)
        {
            ShoppingBasketGameObject.SetActive(bInShow);
        }
    }

    public void SetCustomerState(HRCustomerState NewState)
    {
        if (HRNetworkManager.IsHost())
        {
            SetCustomerState_Server(NewState);
        }
        else
        {
            SetCustomerState_Command(NewState);
        }
    }

    [Mirror.Server]
    public void SetCustomerState_Server(HRCustomerState NewState)
    {
        CurrentVisibleState = NewState;
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void SetCustomerState_Command(HRCustomerState NewState)
    {
        SetCustomerState_Server(NewState);
    }

    public void CustomerStateChanged_Hook(HRCustomerState OldState, HRCustomerState NewState)
    {
        if (CustomerStateText)
        {
            if (BaseGameInstance.Get.bDebugMode)
            {
                CustomerStateText.gameObject.SetActive(true);
            }
            CustomerStateText.text = NewState.ToString();
        }
    }

    public void HandleBudgetChanged_Hook(float OldBudget, float NewBudget)
    {
        BudgetChangedDelegate?.Invoke(this, NewBudget);
    }

    public float GetRemainingBudget()
    {
        return Budget;
    }

    public BaseQueueSystem GetShopWaitingLine(HRShopManager InShopManager = null)
    {
        HRShopManager ShopManagerToUse = InShopManager;
        if (!ShopManagerToUse)
        {
            ShopManagerToUse = TargetShopManager;
        }

        if (ShopManagerToUse)
        {
            return ShopManagerToUse.GetShopWaitingLine(this);
        }
        else
        {
            return null;
        }
    }

    public void GiveTip(float TipAmount, string TipText)
    {
        if (HRNetworkManager.IsHost())
        {
            GiveTip_Server(TipAmount, TipText);
        }
        else
        {
            GiveTip_Command(TipAmount, TipText);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void GiveTip_Command(float TipAmount, string TipText)
    {
        GiveTip_Server(TipAmount, TipText);
    }

    [Mirror.Server]
    private void GiveTip_Server(float TipAmount, string TipText)
    {
        if (TargetShopManager)
        {
            TargetShopManager.ReceiveTip(this, TipAmount, TipText);
        }
    }

    void SetWaitingOutside(bool bWaiting)
    {
        if (bWaitingOutside != bWaiting)
        {
            bWaitingOutside = bWaiting;
            OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bWaitingOutside", bWaiting);
        }
    }

    public void WaitOutsideShop(HRShopManager InShopManager, BaseQueueSystem InQueueSystem = null)
    {
        BaseQueueSystem TargetQueueSystem = InQueueSystem != null ? InQueueSystem : GetShopWaitingLine(InShopManager);

        if (TargetQueueSystem)
        {
            SetCustomerState(HRCustomerState.GOINGTOSHOP);
            SetTargetShopManager(InShopManager);

            OwningAIController.BehaviorManager.SetAllVariableValues("ShopManager", InShopManager);
            OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "QueueSystem", TargetQueueSystem);
            // Will eventually be a random point outside the bounds of the waiting queue
            OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "OutsideArea", TargetQueueSystem != null ? TargetQueueSystem.transform.position : InShopManager.GetRandomSignPosition());

            SetWaitingOutside(true);
            if (!DayManager)
            {
                DayManager = ((HRGameManager)BaseGameManager.Get).DayManager;
            }
            if (DayManager)
            {
                HoursWaited = 0;
                DayManager.HourChangedDelegate += HandleHourChanged;
            }

            CurrentPatiencePeriod = Random.Range(MinPatiencePeriod, MaxPatiencePeriod + 1);
            ImpatienceTimer = ImpatienceTime;
            ShowHappinessSlider();
            bCanInteract = true;
        }
        else
        {
            // Does not have any queue to wait at.
        }
    }

    public void SetMoveLayer(AIMoveLayer InLayer, int ShopIndex = -1)
    {
        if (OwningAIController.AIMovement)
        {
            OwningAIController.AIMovement.SetMovementLayer(InLayer, ShopIndex);
        }
    }
    public void EnterPlot(HRShopPlot InPlot)
    {
        SetCustomerState(HRCustomerState.GOINGTOPLOT);
        SetTargetShopPlot(InPlot);
        Initialize(true, true, true);

        SetNeedsUIEnabled(true);
    }

    // TODO: call this when arriving at the plot
    public void ArrivedAtPlot(HRShopPlot InPlot)
    {
        SetMoveLayer(AIMoveLayer.PLOT);

        if (CustomerNeeds)
        {
            CustomerNeeds.SetIsTicking(true);
        }

        SetCustomerState(HRCustomerState.WANDERINGPLOT);
        SetHappinessDecayEnabled(true);

        if (InPlot)
        {
            InPlot.AddCustomer(this);
        }
    }

    // TODO: call this when arriving at the shop sign
    public void ArrivedAtShop(HRShopManager InShopManager)
    {
        int ShopIndex = -1;
        if (InShopManager.OwningShopPlot)
        {
            ShopIndex = InShopManager.OwningShopPlot.GetShopIndex(InShopManager);
        }

        SetMoveLayer(AIMoveLayer.SHOP, ShopIndex);
    }

    private void LeavePlot_Implementation(bool bUnhappy, bool bLeaveShop, bool bDropShoppingCart, bool bReturningToPool)
    {
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bLeavingPlot", true);
        bDoneShopping = true;
        bQueued = false;

        if (bLeaveShop)
        {
            LeaveShop(bUnhappy, bDropShoppingCart, bReturningToPool);
        }

        SetWaitingOutside(false);

        SetCustomerState(HRCustomerState.GOINGHOME);
        if (CustomerNeeds)
        {
            CustomerNeeds.SetIsTicking(false);
            SetNeedsUIEnabled(false);
        }
        OnLeavePlotDelegate?.Invoke(this, bUnhappy);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void LeavePlot_Command(bool bUnhappy, bool bLeaveShop, bool bDropShoppingCart, bool bReturningToPool)
    {
        LeavePlot_Implementation(bUnhappy, bLeaveShop, bDropShoppingCart, bReturningToPool);
    }

    public void LeavePlot(bool bUnhappy = true, bool bLeaveShop = true, bool bDropShoppingCart = true, bool bReturningToPool = false)
    {
        if (HRNetworkManager.IsHost())
        {
            LeavePlot_Implementation(bUnhappy, bLeaveShop, bDropShoppingCart, bReturningToPool);
        }
        else
        {
            LeavePlot_Command(bUnhappy, bLeaveShop, bDropShoppingCart, bReturningToPool);
        }
    }

    public void EnterShop(HRShopManager InShopManager)
    {
        if (HRNetworkManager.IsHost())
        {
            EnterShop_Server(InShopManager);
        }
        else
        {
            EnterShop_Command(InShopManager);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void EnterShop_Command(HRShopManager InShopManager)
    {
        EnterShop_Server(InShopManager);
    }

    int GetMinimumCartSize()
    {
        if (HasShoppingBasket() && BudgetToNumItemsCurve != null)
        {
            return (int)BudgetToNumItemsCurve.Evaluate(CurrentBudget);
        }
        else
        {
            if (TargetShopManager.GetShopEntity())
            {
                return TargetShopManager.GetShopEntity().GetMinimumCartSize();
            }
        }

        return 1;
    }

    int GetMaximumCartSize()
    {
        if (HasShoppingBasket() && BudgetToNumItemsCurve != null)
        {
            return (int)BudgetToNumItemsCurve.Evaluate(CurrentBudget);
        }
        else
        {
            if (TargetShopManager.GetShopEntity())
            {
                return TargetShopManager.GetShopEntity().GetMaximumCartSize();
            }
        }

        return 1;
    }

    void UpdateShoppingCartMinMax()
    {
        ShoppingCartMin = GetMinimumCartSize();
        ShoppingCartMax = GetMaximumCartSize();
    }

    private void EnterShop_Server(HRShopManager InShopManager)
    {
        LastTimeVisitedShop = Time.timeSinceLevelLoad;

        if (InShopManager && InShopManager.OwningShopPlot && InShopManager.OwningShopPlot != TargetShopPlot)
        {
            SetTargetShopPlot(InShopManager.OwningShopPlot);
        }
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "ShopManager", InShopManager);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "QueueSystem", null);

        SetWaitingOutside(false);
        SetTargetShopManager(InShopManager);

        UpdateShoppingCartMinMax();

        if (DayManager)
        {
            DayManager.HourChangedDelegate -= HandleHourChanged;
        }
        this.enabled = true;

        int OrderingStationCount = InShopManager.GetRoomItemCount(HRRoomItemType.ORDERING);
        int RestaurantHostStationCount = InShopManager.GetRoomItemCount(HRRoomItemType.RESTAURANT);
        float OrderWeight = 0f;
        float RestaurantWeight = 0f;
        float RetailWeight = ChanceToRetail;

        float RandomRoll = Random.Range(0f, 1f);
        if (OrderingStationCount > 0)
        {
            OrderWeight = ChanceToOrder;
        }
        if (RestaurantHostStationCount > 0)
        {
            RestaurantWeight = ChanceToRestaurant;
        }
        float TotalWeight = RetailWeight + OrderWeight + RestaurantWeight;
        RetailWeight = RetailWeight / TotalWeight;
        OrderWeight = OrderWeight / TotalWeight;
        RestaurantWeight = RestaurantWeight / TotalWeight;
        float[] Weights = { OrderWeight, RestaurantWeight, RetailWeight };

        float WeightRoll = Random.Range(0f, 1f);
        float Cummulative = 0f;
        int ModeIndex = 0;
        for (int i = 0; i < Weights.Length; ++i)
        {
            Cummulative += Weights[i];
            if (WeightRoll < Cummulative)
            {
                ModeIndex = i;
                break;
            }
        }

        bInShop = true;

        if (ModeIndex == 0)
        {
            OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bOrderMode", true);
        }
        else if (ModeIndex == 1)
        {
            OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bRestaurantMode", true);
        }
        else
        {
            OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bOrderMode", false);
            OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bRestaurantMode", false);

            OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "WanderTimes", Random.Range(MinWanderTimesBeforeBuying, MaxWanderTimesBeforeBuying + 1));
            OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "WanderTimes", Random.Range(MinWanderTimesBeforeBuying, MaxWanderTimesBeforeBuying + 1));

        }

        if (!bIsPaused)
        {
            OwningAIController.BehaviorManager.RestartBehavior(MainCustomerBT);
        }
        ImpatienceTimer = ImpatienceTime;
        ShowHappinessSlider();

        //if (HappinessSliderIcon)
        //{
        //    SetHappinessIconColor(0, Color.clear);
        //}

        MessGenerator?.SetDropDecalsEnabled(true);

        AddPlotWanderTimes(InShopManager.GetShopEntity().GetWanderPlotAmount());

        RatingInfo.OnPlayerEnterShop();
    }

    [Mirror.ClientRpc]
    public void EnterShop_ClientRpc()
    {
        if (EnterShopAudioClip)
        {
            ((HRGameInstance)BaseGameInstance.Get).MusicManager?.PlayClipAtPoint(EnterShopAudioClip, this.transform.position);
        }
    }

    bool bInShop = false;

    public void WanderShop(HRShopManager InShopManager)
    {
        EnterShop(InShopManager);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bForceWander", true);
    }


    public void SetEmotion(HREmotion Emotion, float duration, bool permanent = false)
    {
        // Remove for now because too ugly
        return;

        if (HRNetworkManager.IsHost())
        {
            SetEmotion_Implementation(Emotion, duration, permanent);
            if (HRNetworkManager.bIsServer && Mirror.NetworkServer.active)
            {
                SetEmotion_ClientRpc(Emotion, duration, permanent);
            }
        }
        else
        {
            SetEmotion_Command(Emotion, duration, permanent);
        }
    }


    public void SetEmotion_Implementation(HREmotion Emotion, float duration, bool permanent)
    {
        EmotionComponent.SetEmotion(Emotion, duration, permanent);
    }

    [Mirror.ClientRpc]
    public void SetEmotion_ClientRpc(HREmotion Emotion, float duration, bool permanent)
    {
        SetEmotion_Implementation(Emotion, duration, permanent);
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void SetEmotion_Command(HREmotion Emotion, float duration, bool permanent)
    {
        SetEmotion_ClientRpc(Emotion, duration, permanent);
    }

    [Mirror.Server]
    public void AddPlotWanderTimes(int InTimes)
    {
        MaxPlotWanderTimes += InTimes;
    }

    [Mirror.Server]
    public void LeaveShop_Server(bool bUnhappy, bool bDropShoppingCart, bool bReturningToPool)
    {
        LastTimeVisitedShop = Time.timeSinceLevelLoad;

        if (bLeavingShop && !bInShop)
        {
            RemoveSpotInQueue();
            return;
        }

        var CachedTargetShopManager = TargetShopManager;
        bInShop = false;

        if (bDropShoppingCart)
        {
            DropShoppingCart();
        }

        if (bUnhappy)
        {
            SetHappinessMeterActive(false);

            SetEmotion(HREmotion.Angry, 3.0f);
        }
        else
        {
            HideHappinessSlider();

            SetEmotion(HREmotion.Happy, 3.0f);
        }

        if (TargetRegister != null)
        {
            TargetRegister.PlayFailureFX();
        }

        if (DayManager)
        {
            DayManager.HourChangedDelegate -= HandleHourChanged;
        }

        bLeavingShop = true;
        RemoveSpotInQueue();
        SetWaitingOutside(true);
        OwningAIController.BehaviorManager.SetAllVariableValues("ShopManager", null);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "QueueSystem", null);
        if (OwningAIController.AIMovement.PlayerFollow && OwningAIController.AIMovement.PlayerFollow.OwningManager)
        {
            OwningAIController.AIMovement.PlayerFollow.OwningManager.RemoveFollower(OwningAIController.AIMovement.PlayerFollow);
        }

        if (bQueued)
        {
            TargetShopManager.OnCustomerCountChange -= OnCustomersInShopChanged;
        }

        SetTargetShopManager(null);

        if (OrdererComponent)
        {
            OrdererComponent.ClearOrders();
        }

        SetCustomerState(HRCustomerState.LEAVINGSHOP);
        OnLeaveShopDelegate?.Invoke(this, bUnhappy);
        OnLeaveShop_ClientRpc(bUnhappy);

        if (CurrentBudget < LeavingBudgetThreshold || CustomerNeeds.GetNeedComponent(HRENeed.Patience).CurrentHP <= 0 || MaxPlotWanderTimes <= 0)
        {
            LeavePlot(false, false);
        }


        if (!bReturningToPool)
        {
            RatingInfo.OnPlayerLeaveShop();

            if (CachedTargetShopManager)
            {
                bool underTheRug = (CachedTargetShopManager.ShopPoliciesManager.GetUnderTheRugNodeActive() && (RatingInfo.HasStimulus("NoItemFound") && !RatingInfo.HasStimulus("PurchaseFailed")));

                if (RatingInfo.HasStimulus() && underTheRug == false)
                {
                    var RatingData = RatingInfo.GetRatingData();

                    if (RatingEffects)
                    {
                        PlayRatingEffects_ClientRpc(RatingData.Rating);
                    }

                    CachedTargetShopManager.AddStarRating(RatingData);
                }
            }

            RatingInfo.ResetRating();
            if (!bUnhappy)
                OwningPlayerCharacter.CharacterVoice.PlayRandomAudioFromGroup("Happy");
        }

        if (bSearching)
        {
            StopSearching();
        }

        MessGenerator?.SetDropDecalsEnabled(false);

        bItemFound = false;
        //RatingInfo.ResetRating();
    }


    [Mirror.ClientRpc]
    public void PlayRatingEffects_ClientRpc(float Rating)
    {
        string BodyType = OwningPlayerCharacter.CharacterVoice.VoiceType;
        var RatingEffect = RatingEffects.GetRatingEffect(Rating);

        // Play SFX
        var Audio = RatingEffect.Sound.GetClip(BodyType);

        if (Audio != null)
        {
            OwningPlayerCharacter.AudioManager.PlaySFX(Audio);
        }

        // Play VFX
        if (RatingEffect.Effect != null)
        {
            Instantiate(RatingEffect.Effect, transform.position, transform.rotation);
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    public void LeaveShop_Command(bool bUnhappy, bool bDropShoppingCart, bool bReturningToPool)
    {
        LeaveShop_Server(bUnhappy, bDropShoppingCart, bReturningToPool);
    }

    public void LeaveShop(bool bUnhappy, bool bDropShoppingCart = true, bool bReturningToPool = false)
    {
        if (HRNetworkManager.IsHost())
        {
            ReturnShoppingBasket();

            LeaveShop_Server(bUnhappy, bDropShoppingCart, bReturningToPool);
        }
        else
        {
            if (netIdentity)
            {
                LeaveShop_Command(bUnhappy, bDropShoppingCart, bReturningToPool);
            }
        }
    }

    [Mirror.ClientRpc]
    private void OnLeaveShop_ClientRpc(bool bUnhappy)
    {
        if (!HRNetworkManager.IsHost())
        {
            OnLeaveShopDelegate?.Invoke(this, bUnhappy);
        }
    }

    public void SetMoveSpeed(BaseScripts.BaseMoveType MoveSpeed)
    {
        OwningAIController.AIMovement.OwningMovementComponent.SetMoveSpeed(MoveSpeed);
        if (RetailBT)
        {
            RetailBT.SetVariableValue("DefaultWanderSpeed", MoveSpeed);
        }
    }

    private void HandleRigChanged(BaseScripts.BasePlayerMesh playerMesh)
    {
        if (playerMesh)
        {
            if (OwningPlayerCharacter.AnimScript)
            {
                OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "Animator", OwningPlayerCharacter.AnimScript.AnimancerComponent.Animator);
            }
            if (HappinessSliderUI)
            {
                HappinessSliderUI.WorldPosition = playerMesh.PlayerRig.HeadTransform;
            }
        }
        else
        {
            if (OwningPlayerCharacter.AnimScript)
            {
                OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "Animator", null);
            }
            if (HappinessSliderUI)
            {
                HappinessSliderUI.WorldPosition = null;
            }
        }
    }

    bool bInitialized = false;
    public override void Awake()
    {
        base.Awake();

        if (CustomerStateText)
        {
            CustomerStateText.gameObject.SetActive(false);
        }

        if (bInitialized)
        {
            return;
        }

        bInitialized = true;

        if (OwningPlayerCharacter && OwningPlayerCharacter.PlayerMesh)
        {
            OwningPlayerCharacter.PlayerMesh.OnRigChangedDelegate += HandleRigChanged;
        }

        if (HRNetworkManager.IsHost() && CustomerNeeds && EQSChecker)
        {
            OneshotNeedsAffectedItems = new HashSet<HRRoomItem>();
            OldTickNeedsAffectingItems = new HashSet<HRRoomItem>();
            NewTickNeedsAffectingItems = new HashSet<HRRoomItem>();
            NeedsItemSearchingTimer = NeedsItemSearchingTick;
        }

        if (!HRNetworkManager.IsHost() || this.netIdentity == null)
        {
            bClientMode = true;
            //this.enabled = false;
            return;
        }
        if (Player)
        {
            Player.OnStateChangeDelegate += HandleInteractStateChanged;
        }
        if (OwningAIController && OwningAIController.AIMovement)
        {
            if (OwningAIController.AIMovement)
            {
                OwningAIController.AIMovement.MovementEndedDelegate += HandleMovementEnded;
                OwningAIController.AIMovement.MovementStartedDelegate += HandleMovementStarted;
            }
            if (OwningAIController.BehaviorManager)
            {
                OwningAIController.BehaviorManager.AddBehaviorTree(MainBT, BaseBehaviorTreeTickRateType.OverrideAll);
                OwningAIController.BehaviorManager.AddBehaviorTree(MeleeBT, BaseBehaviorTreeTickRateType.OverrideAll);
                OwningAIController.BehaviorManager.AddBehaviorTree(RangeBT, BaseBehaviorTreeTickRateType.OverrideAll);
                OwningAIController.BehaviorManager.AddBehaviorTree(RetailBT);
                OwningAIController.BehaviorManager.AddBehaviorTree(OrderBT);
                OwningAIController.BehaviorManager.AddBehaviorTree(RestaurantBT);
            }
        }

        if (OwningPlayerCharacter)
        {
            OwningPlayerCharacter.OnStateChangeDelegate += HandleStateChange;
            OwningPlayerCharacter.Ragdoll.OnRagdollDelegate += HandleRagdoll;
            if (OwningPlayerCharacter.HP)
                OwningPlayerCharacter.HP.OnHPChangedDelegate += HandleHPChanged;
            if (OwningPlayerCharacter.FearComponent)
            {
                OwningPlayerCharacter.FearComponent.OnFearThresholdReachedDelegate += HandleStartedFleeing;
                OwningPlayerCharacter.FearComponent.OnHPChangedInstigatorDelegate += HandleFearChanged;
            }
        }

        if (OwningAIController.AIMovement.PlayerFollow)
        {
            OwningAIController.AIMovement.PlayerFollow.OnFollowingPlayerDelegate += HandleFollowing;
            OwningAIController.AIMovement.PlayerFollow.OnOwnerTapInteractionDelegate += HandlePlayerFollowOwnerTap;
        }

        RatingInfo = new HRShopRatingRuntime(this);
    }

    private IEnumerator CombatBarkCoroutine(float InitialDelay)
    {
        yield return new WaitForSeconds(InitialDelay);
        while (Player.BarkComponent && currentHostileState == HRHostileState.Attacking)
        {
            Player.BarkComponent.PlayBark(OnCombatBark);
            yield return null;
        }
    }

    public void InitializeCustomerSpecificBTs()
    {
        if (!MainCustomerBT && MainCustomerBTPrefab)
        {
            MainCustomerBT = BaseObjectPoolManager.Get.InstantiateFromPool(MainCustomerBTPrefab.gameObject, Parent: BTParent).GetComponent<BaseBehaviorTree>();

            if (MainCustomerBT)
            {
                OwningAIController.BehaviorManager.AddBehaviorTree(MainCustomerBT);

                OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "AIPlayerCharacter", OwningPlayerCharacter);
                OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "AIMovement", OwningAIController.AIMovement);
                OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "CustomerAI", this);
                OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "OwningGameObject", this.gameObject);
                if (EmotionComponent)
                {
                    OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "EmotionComponent", EmotionComponent.gameObject);
                }
                if (MainBT)
                {
                    OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "AttackBT", MainBT.gameObject);

                    if (MeleeBT)
                    {
                        OwningAIController.BehaviorManager.SetVariableValue(MainBT, "MeleeBT", MeleeBT.gameObject);
                    }
                    if (RangeBT)
                    {
                        OwningAIController.BehaviorManager.SetVariableValue(MainBT, "RangeBT", RangeBT.gameObject);
                    }
                }
                if (CustomerBT)
                {
                    OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "CustomerBT", CustomerBT.gameObject);
                }
            }
        }

        if (!CustomerBT && CustomerBTPrefab)
        {
            CustomerBT = BaseObjectPoolManager.Get.InstantiateFromPool(CustomerBTPrefab.gameObject, Parent: BTParent).GetComponent<BaseBehaviorTree>();

            if (CustomerBT)
            {
                OwningAIController.BehaviorManager.AddBehaviorTree(CustomerBT);

                OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "AIMovement", OwningAIController.AIMovement);
                OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "CustomerAI", this);
                OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "OwningGameObject", this.gameObject);
                OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "GreetProximity", GreetProximity);

                if (RetailBT)
                {
                    OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "RetailBT", RetailBT.gameObject);
                    OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "MinContainerWaitTime", MinBrowseTime);
                    OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "MaxContainerWaitTime", MaxBrowseTime);
                }

                OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "MinContainerWaitTime", MinBrowseTime);
                OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "MaxContainerWaitTime", MaxBrowseTime);

                if (OrderBT)
                {
                    OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "OrderBT", OrderBT.gameObject);
                }
                if (RestaurantBT)
                {
                    OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "RestaurantBT", RestaurantBT.gameObject);
                }

                if (MainCustomerBT)
                {
                    OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "CustomerBT", CustomerBT.gameObject);
                }
            }
        }

        OwningAIController.BehaviorManager.SetAllVariableValues("MinWaitTimePerMovement", MinWaitTimePerMovement);
        OwningAIController.BehaviorManager.SetAllVariableValues("MaxWaitTimePerMovement", MaxWaitTimePerMovement);
    }

    void RefreshNeedsAffectingItems()
    {
        if (TargetShopPlot)
        {
            NewTickNeedsAffectingItems.Clear();

            // Find Nearby Weapons
            EQSData.DistanceTest.MaxThreshold = MaxNeedsItemSearchingRadius;
            EQSData.DistanceTest.bRequireSort = false;

            EQSChecker.AddOverrideTest(0, EQSData.DistanceTest);
            List<EQSValidNeighbor> ValidNeighbors = EQSChecker.SingleQuery(0);

            for (int i = 0; i < ValidNeighbors.Count; ++i)
            {
                EQSValidNeighbor ValidNeighbor = ValidNeighbors[i];

                HRRoomItem RoomItem = ValidNeighbor.Checker.GetComponentFromCache<HRRoomItem>();
                if (RoomItem != null && RoomItem.AffectingRange > 0 && ValidNeighbor.SqrDistanceToSelf <= RoomItem.AffectingRange * RoomItem.AffectingRange)
                {
                    // Apply one shot if not applied before
                    if (RoomItem.OneshotNeedsModifiers != null && RoomItem.OneshotNeedsModifiers.Length > 0 && !OneshotNeedsAffectedItems.Contains(RoomItem))
                    {
                        OneshotNeedsAffectedItems.Add(RoomItem);
                        CustomerNeeds.ApplyNeedModifier(RoomItem.OneshotNeedsModifiers, false, false, true);
                    }

                    if (RoomItem.TickNeedsModifiers != null && RoomItem.TickNeedsModifiers.Length > 0 && !OldTickNeedsAffectingItems.Contains(RoomItem))
                    {
                        NewTickNeedsAffectingItems.Add(RoomItem);
                        CustomerNeeds.ApplyNeedModifier(RoomItem.TickNeedsModifiers, true, false, true);
                    }
                }
            }
        }

        // Clear out-of-range affecting needs
        foreach (HRRoomItem RoomItem in OldTickNeedsAffectingItems)
        {
            if (RoomItem != null && !NewTickNeedsAffectingItems.Contains(RoomItem))
            {
                CustomerNeeds.ApplyNeedModifier(RoomItem.TickNeedsModifiers, true, true, true);
            }
        }

        OldTickNeedsAffectingItems.Clear();
        OldTickNeedsAffectingItems.UnionWith(NewTickNeedsAffectingItems);
    }

    private void Update()
    {
        if (bClientMode) { return; }

        if (bInShop)
        {
            if (GetCurrentPatience() == 0)
            {
                LeavePlot();
            }
        }

        if (bQueued)
        {
            CurrentQueueDelay -= Time.deltaTime;

            if (CurrentQueueDelay <= 0)
            {
                PlayRandomIdleAnimation();
                CurrentQueueDelay = Random.Range(MinTimeToWait, MaxTimeToWait);
            }
        }

        if (!bIsPaused && Initialized)
        {
            if (bUseGreetTimer)
            {
                CurrentGreetWaitTime += Time.deltaTime;

                if (CurrentGreetWaitTime >= TimeBeforeLeavingGreet)
                {
                    OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bGreet", false);
                    bUseGreetTimer = false;
                    bDoneShopping = true;

                    SetBubbleUI(HRBubbleUIType.Greet, HRBubbleUIStatus.Failure);
                    LeaveShop(true, true, true);

                    SetInteractableActive(false);
                }
            }

            if (bUseSearchingTimer)
            {
                SearchingTimer -= Time.deltaTime;

                if (SearchingTimer <= 0)
                {
                    OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bIdle", false);
                    OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bIdle", false);
                }
            }
        }

        if (targetGameObject)
        {
            if (CheckSenses())
            {
                SetHostileState(HRHostileState.Attacking);
            }
            else if (currentHostileState == HRHostileState.Attacking)
            {
                SetHostileState(HRHostileState.Searching);
            }
        }

        if (NeedsItemSearchingTimer >= 0)
        {
            NeedsItemSearchingTimer += Time.deltaTime;

            if (NeedsItemSearchingTimer >= NeedsItemSearchingTick)
            {
                RefreshNeedsAffectingItems();
                NeedsItemSearchingTimer = 0f;
            }
        }
    }

    void HandleShopLaunched(HRShopManager InShopManager)
    {
        // Let customer in line
        //if (InShopManager)
        //{
        //    InShopManager.AdmitCustomerIntoShop(this);
        //}
    }

    public void SetTargetShopManager(HRShopManager InShopManager)
    {
        if (TargetShopManager)
        {
            TargetShopManager.ShopLaunchedDelegate -= HandleShopLaunched;
        }

        LastTargetShopManager = TargetShopManager;
        TargetShopManager = InShopManager;

        if (TargetShopManager)
        {
            TargetShopManager.OnCustomerCountChange -= OnCustomersInShopChanged;
            TargetShopManager.ShopLaunchedDelegate -= HandleShopLaunched;
            TargetShopManager.ShopLaunchedDelegate += HandleShopLaunched;
        }
    }

    public void SetTargetShopPlot(HRShopPlot InShopPlot)
    {
        if (!MainCustomerBT || !CustomerBT)
        {
            InitializeCustomerSpecificBTs();
        }

        TargetShopPlot = InShopPlot;

        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "ShopPlot", InShopPlot);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "ShopPlot", InShopPlot);

        if (InShopPlot && InShopPlot.StartingTile)
        {
            OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "PlotEntrance", InShopPlot.StartingTile.transform.position);
        }

        if (MessGenerator)
        {
            MessGenerator.SetEnabled(true);
        }
    }
    private bool CheckSenses()
    {
        for (int i = 0; i < Senses.Length; ++i)
        {
            if (Senses[i].SenseType == BaseSenseComponent.Sense.Sight)
            {
                if (targetGameObject && ((currentHostileState == HRHostileState.Attacking && BaseLineOfSight.CheckDistance(Senses[i].transform, targetGameObject.transform, 3f))
                    || ((BaseLineOfSight)Senses[i]).CheckInLineOfSight(targetGameObject.transform)))
                {
                    SetLastSeenLocation(targetGameObject.transform.position);
                    return true;
                }
            }
        }
        return false;
    }

    private void SetLastSeenLocation(Vector3 InLocation)
    {
        lastSeenLocation = InLocation;
        OwningAIController.BehaviorManager.SetAllVariableValues("LastSeenLocation", InLocation);
    }

    void HandleFollowing(BasePlayerFollow InFollow, BaseScripts.BaseInteractionManager InteractionManager, bool bFollowing)
    {
        if (MessGenerator)
        {
            MessGenerator.SetEnabled(!bFollowing);
        }
    }

    void HandlePlayerFollowOwnerTap(BasePlayerFollow InPlayerFollow, BaseScripts.BaseInteractable InInteractable)
    {
        if (InInteractable)
        {
            HRChair Chair = InInteractable.GetComponentInParent<HRChair>();
            if (Chair && Chair.bChairFree)
            {
                Chair.SeatComponent.TrySit(OwningPlayerCharacter);
                Chair.SeatComponent.OnQueuedSitterCanceledDelegate += HandleSitCanceled;
                Chair.SeatComponent.OnSeatTakenDelegate += HandleSeatTaken;
                InPlayerFollow.RemoveFromOwningManager();
            }
        }
    }

    void HandleSitCanceled(HRSeatComponent SeatComponent, HeroPlayerCharacter PC, bool bTaken)
    {
        if (PC == OwningPlayerCharacter)
        {
            SeatComponent.OnQueuedSitterCanceledDelegate -= HandleSitCanceled;
            SeatComponent.OnSeatTakenDelegate -= HandleSeatTaken;
            if (OwningAIController.AIMovement.PlayerFollow)
            {
                OwningAIController.AIMovement.PlayerFollow.enabled = true;
            }
        }
    }
    [HideInInspector, System.NonSerialized]
    public HRSeatComponent CurrentSeatComponent = null;
    public void HandleSeatTaken(HRSeatComponent SeatComponent, HeroPlayerCharacter PC, bool bTaken)
    {
        if (PC == OwningPlayerCharacter)
        {
            SeatComponent.OnQueuedSitterCanceledDelegate -= HandleSitCanceled;
            SeatComponent.OnSeatTakenDelegate -= HandleSeatTaken;
            if (OwningAIController.AIMovement.PlayerFollow)
            {
                OwningAIController.AIMovement.PlayerFollow.enabled = false;
            }

            if (HRNetworkManager.IsHost())
            {
                if (TargetShopManager && TargetShopManager.ShopPoliciesManager.GetFurniturePerkActive())
                {
                    //TODO: do we need to set needs active?
                    CustomerNeeds.ApplyNeedModifier(TargetShopManager.ShopPoliciesManager.FurniturePerkNode.NeedModifiers, true, !bTaken);
                }
                CustomerNeeds.SetNeedActive(HRENeed.Patience, !bTaken);
                CurrentSeatComponent = bTaken ? SeatComponent : null;
            }
        }
    }

    void HandleDebugMode(bool bDebugEnabled)
    {
        if (CustomerStateText)
        {
            CustomerStateText.gameObject.SetActive(bDebugEnabled);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (BaseGameInstance.Get)
        {
            BaseGameInstance.Get.DebugDelegate += HandleDebugMode;
        }

        if (Player.Interactable)
        {
            bOriginalInteractableEnabled = Player.Interactable.enabled;
        }

        if (bClientMode)
        {
            Interactable.TapInteractionDelegate -= HandleTapInteraction;
            Interactable.TapInteractionDelegate += HandleTapInteraction;
            return;
        }

        if (OwningPlayerCharacter && OwningPlayerCharacter.PauseReceiver)
        {
            OwningPlayerCharacter.PauseReceiver.OnPauseDelegate += HandlePause;
            HandlePause(OwningPlayerCharacter.PauseReceiver, OwningPlayerCharacter.PauseReceiver.bIsPaused);
        }

        if (CustomerNeeds)
        {
            if (CustomerNeeds.Initialized)
            {
                OnNeedsInitialized();
            }
            else
            {
                CustomerNeeds.OnNeedsInitializedDelegate += OnNeedsInitialized;
            }
        }

        DayManager = ((HRGameManager)BaseGameManager.Get).DayManager;


        if (StartingWeaponData)
        {
            BaseStarterWeaponStruct chosenWeaponStruct = StartingWeaponData.GetRandomWeapon();
            if (chosenWeaponStruct.weapon)
            {
                currentWeaponInstance = InstantiateWeaponPrefab(chosenWeaponStruct.weapon, chosenWeaponStruct.weaponRarity);
            }
            else
            {
            }
        }

        if (StartingSideWeaponData)
        {
            for (int i = 0; i < StartingSideWeaponData.starterWeapons.Length; i++)
            {
                InstantiateWeaponPrefab(StartingSideWeaponData.starterWeapons[i].weapon, StartingSideWeaponData.starterWeapons[i].weaponRarity);
            }
        }

        InitializeCustomerAI();

        if (Interactable)
        {
            Interactable.TapInteractionDelegate -= HandleTapInteraction;
            Interactable.TapInteractionDelegate += HandleTapInteraction;
        }

        if (OwningPlayerCharacter && OwningPlayerCharacter.WeaponManager)
        {
            OwningPlayerCharacter.WeaponManager.AddedWeaponDelegate += HandleWeaponAdded;
        }
    }

    //public bool bConsumeNextAddedItem = false;

    void HandleWeaponAdded(BaseWeaponManager InManager, BaseWeapon WeaponAdded)
    {
        //if(WeaponAdded)
        //{
        //    // Jank but ok for now. This is for the HRSampleDisplay functionality where you may want to pick something up and consume it.
        //    if (bConsumeNextAddedItem)
        //    {
        //        BaseWeaponConsumable ConsumableRef = WeaponAdded.GetComponent<BaseWeaponConsumable>();
        //        if(ConsumableRef)
        //        {
        //            InManager.SwitchToWeapon(WeaponAdded);
        //            ConsumableRef.StartConsuming();
        //        }

        //        bConsumeNextAddedItem = false;
        //    }
        //}
    }


    private void OnNeedsInitialized()
    {
        var patience = CustomerNeeds.GetNeedComponent(HRENeed.Patience);

        if (patience)
        {
            ImpatienceThreshold = patience.MaxHP * Mathf.Clamp(ImpatienceThreshold, 0f, 1.0f);
            PatienceToLeaveThreshold = patience.MaxHP * Mathf.Clamp(PatienceToLeaveThreshold, 0f, 1.0f);
        }
    }
    protected virtual BaseWeapon InstantiateWeaponPrefab(BaseWeapon InWeaponPrefab, BaseRarity WeaponRarity = BaseRarity.Common)
    {
        if (HRNetworkManager.IsHost())
        {
            BaseWeapon WeaponInstance = null;
            if (InWeaponPrefab)
            {
                BaseObjectPoolingComponent PooledObject = BaseObjectPoolManager.Get.InstantiateFromPool(InWeaponPrefab.gameObject);

                if (PooledObject)
                {
                    WeaponInstance = PooledObject.GetComponent<BaseWeapon>();

                    if (WeaponInstance)
                    {
                        StartCoroutine(PickupWeaponCoroutine(WeaponInstance));
                        WeaponInstance.SetItemRarity(WeaponRarity);
                    }
                }
            }
            return WeaponInstance;
        }

        return null;
    }

    /*
    private BaseWeapon InstantiateWeaponPrefab(BaseWeapon InWeaponPrefab)
    {
        if (HRNetworkManager.IsHost())
        {
            BaseWeapon WeaponInstance = null;
            if (InWeaponPrefab)
            {
                WeaponInstance = Instantiate(InWeaponPrefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<BaseWeapon>();

                Mirror.NetworkServer.Spawn(WeaponInstance.gameObject);

                if (WeaponInstance)
                {
                    StartCoroutine(PickupWeaponCoroutine(WeaponInstance));
                }
            }
            return WeaponInstance;
        }

        return null;
    }
    */
    IEnumerator PickupWeaponCoroutine(BaseWeapon WeaponInstance)
    {
        yield return new WaitForEndOfFrame();
        if (WeaponInstance)
        {
            Player.WeaponManager.AttemptPickupWeapon(WeaponInstance);
        }
    }

    void HandleTapInteraction(BaseScripts.BaseInteractionManager InteractionManager)
    {
        var InteractingPlayer = InteractionManager.GetComponent<HeroPlayerCharacter>();

        if (InteractingPlayer == null)
        {
            return;
        }


        if (!HRNetworkManager.IsHost())
        {
            HandleTapInteraction_Command(InteractionManager, InteractingPlayer);
        }
        else
        {
            HandleTapInteraction_Implementation(InteractionManager, InteractingPlayer);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void HandleTapInteraction_Command(BaseScripts.BaseInteractionManager InteractionManager, HeroPlayerCharacter InteractingPlayer)
    {
        HandleTapInteraction_Implementation(InteractionManager, InteractingPlayer);
    }


    private void HandleTapInteraction_Implementation(BaseScripts.BaseInteractionManager InteractionManager, HeroPlayerCharacter InteractingPlayer)
    {
        if (bSleeping)
        {
            AwakenCustomer();
        }
        else
        {
            AdmitCustomerInLine(InteractionManager, InteractingPlayer);

            if (OwningPlayerCharacter)
            {
                OwningPlayerCharacter.CharacterVoice?.PlayRandomAudioFromGroup("Greeting");
            }

            OnTapInteractionDelegate?.Invoke(this, InteractionManager);
        }
    }


    private void AwakenCustomer()
    {
        var NeedComponent = CustomerNeeds.GetNeedComponent(HRENeed.Comfort);

        if (NeedComponent && NeedComponent.CurrentHP <= 50.0f)
        {
            NeedComponent.SetCurrentHP(50.0f);
        }

        OwningPlayerCharacter.SetNewInteractState(HeroPlayerCharacter.InteractState.Free);
        OwningPlayerCharacter.AttributeManager.RemoveAllAttributesWithID(3, "comfortneed");
        OwningPlayerCharacter.AnimScript.SetFloat("Tired", 0);
        CustomerNeeds.SetNeedActive(HRENeed.Patience, true);
    }


    public void AdmitCustomerInLine(BaseScripts.BaseInteractionManager InteractionManager, HeroPlayerCharacter InteractingPlayer)
    {
        if (HRNetworkManager.IsHost())
        {
            AdmitCustomerInLine_Server(InteractionManager, InteractingPlayer);
        }
        else
        {
            if (netIdentity != null && !HRNetworkManager.IsOffline())
            {
                AdmitCustomerInLine_Command(InteractionManager, InteractingPlayer);
            }
        }
    }

    [Mirror.Server]
    public void AdmitCustomerInLine_Server(BaseScripts.BaseInteractionManager InteractionManager, HeroPlayerCharacter InteractingPlayer)
    {
        if (!bCanInteract)
        {
            return;
        }

        if (!Initialized)
        {
            if (TargetShopManager)
            {
                Initialize(true, TargetShopManager.CanSearch, TargetShopManager.CanDiscuss, false, true);
            }
            else
            {
                Debug.LogError("There is no TargetShopManager in AdmitCustomerInLine for " + this.name);
                return;
            }
        }

        // The specific connection
        var Connection = InteractingPlayer.connectionToClient;

        if (IsCustomerDoneShopping())
        {
            // Skip striaght to greet dialogue
            ExecuteGreetDialogue_Server(Connection, InteractingPlayer);
        }
        else
        {
            // Admit customer into shop
            if (((bWaitingOutside && CustomerStates.Contains(SpecialCustomerState.Greet)) || bCanOverworldGreet) && !bLeavingShop)
            {
                // Must run on host
                InteractedPlayer = InteractingPlayer;
                OwningAIController.AIMovement.OwningMovementComponent.FreezeMovement(false);
                //bCanInteract = false;
                bUseGreetTimer = false;

                //ExecuteGreetMinigame_TargetRpc(Connection, InteractingPlayer);
            }
            // This behavior can run exclusively on the host end.
            else if (bWaitingOutside && bQueued && !bLeavingShop && TargetShopManager && TargetShopManager.CanAdmitCustomerIntoShop())
            {
                if (TargetShopManager.AdmitCustomerIntoShop(this))
                {
                    SetBubbleUI(HRBubbleUIType.WaitingOutside, HRBubbleUIStatus.Success);

                    var patience = CustomerNeeds.GetNeedComponent(HRENeed.Patience);

                    if (patience)
                    {
                        float LostHappiness = GetMaxPatience() - patience.CurrentHP;
                        patience.AddHP(LostHappiness * MaxPatienceToRestore);
                    }

                    SetEmotion(HREmotion.Happy, 3.0f);

                    OwningPlayerCharacter.CharacterVoice.PlayRandomAudioFromGroup("Greeting");
                    bQueued = false;
                }
            }
            // Handle Help behavior
            else if (bSearching && (InteractedPlayer == null || InteractedPlayer == InteractingPlayer))
            {
                if (!bHasAsked)
                {
                    UpdateWantedCategory();
                }
                // Must run on host
                bUseSearchingTimer = false;
                bHasAsked = true;
                bDialogueIsOpen = true;

                // Behavior tree runs on the host only
                InteractedPlayer = InteractingPlayer;
                PlayerInteractionManager = InteractionManager;
                OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "Player", InteractionManager?.gameObject);
                OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "Player", InteractionManager?.gameObject);

                OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bIdle", true);
                OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bIdle", true);

                MainCustomerBT?.DisableBehaviorTree(false);
                RetailBT?.DisableBehaviorTree(false);
                CustomerBT?.DisableBehaviorTree(false);

                OwningAIController.AIMovement.StopMovement();
                OwningAIController.AIMovement.SetAIMovementEnabled(false);
                CustomerNeeds.SetNeedActiveAll(false);

                // Run on the client that interacted with this entity.
                ExecuteSearchDialogue(Connection, InteractingPlayer, CurrentWantedCategoryID);
            }
            else
            {
                ExecuteGreetDialogue_Server(Connection, InteractingPlayer);
            }
        }
    }

    [Mirror.Server]
    void ExecuteGreetDialogue_Server(Mirror.NetworkConnection InConnection, HeroPlayerCharacter InInteractingPlayer)
    {
        if (GreetingDialogueStarter != null)
        {
            // todo: networked version of this
            SetBehaviorTreeEnabled(false, true);
            OwningAIController?.AIMovement?.SetAIMovementEnabled(false, true);

            ExecuteGreetDialogue_TargetRpc(InConnection, InInteractingPlayer);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void AdmitCustomerInLine_Command(BaseScripts.BaseInteractionManager InteractionManager, HeroPlayerCharacter InteractingPlayer)
    {
        AdmitCustomerInLine_Server(InteractionManager, InteractingPlayer);
    }

    [Mirror.TargetRpc]
    void ExecuteGreetDialogue_TargetRpc(Mirror.NetworkConnection InTarget, HeroPlayerCharacter InInteractingPlayer)
    {
        ExecuteGreetDialogue(InTarget, InInteractingPlayer);
    }

    public void SetInteractableActive(bool state)
    {
        if (HRNetworkManager.IsHost())
        {
            SetInteractableActive_Server(state);
        }
        else
        {
            SetInteractableActive_Command(state);
        }
    }

    private void HandleCustomerInteractableChanged_Hook(bool oldInteractable, bool newInteractable)
    {
        Interactable.gameObject.SetActive(newInteractable);
    }


    [Mirror.Command(ignoreAuthority = true)]
    private void SetInteractableActive_Command(bool state)
    {
        SetInteractableActive_Server(state);
    }

    [Mirror.Server]
    private void SetInteractableActive_Server(bool state)
    {
        bCustomerInteractableEnabled = state;
    }

    void HandleHourChanged(HRDayManager DayManager, int OldTime, int NewTime)
    {
        /*if (bWaitingOutside)
        {
            HoursWaited++;
            if (HoursWaited >= CurrentPatiencePeriod || NewTime == MaxArrivalHour)
            {
                LeaveShop(true);
            }
        }*/
    }

    private void HandleMovementEnded(FBaseAIMovementData movementEndedData)
    {
        HandleMovementEnded(movementEndedData.movement, movementEndedData.reachedSuccess);
    }

    private void HandleMovementStarted(FBaseAIMovementData movementEndedData)
    {

    }

    void HandleMovementEnded(BaseAIMovement InMovement, bool bSuccess)
    {
        // Generate a mess
        if (bSuccess)
        {
            TryGenerateRandomMess();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.Label(transform.position + new Vector3(0, 1.5f, 0), "Attractivity Modifier: " + DemandAttractivityModifier);

        string CurrentTask = "be happy";

        if (CustomerNeeds)
        {
            var patience = CustomerNeeds.GetNeedComponent(HRENeed.Patience);

            if (patience)
            {
                Handles.Label(transform.position + new Vector3(0, 1.4f, 0), "Patience Decay: " +
                    CustomerNeeds.NeedDatabase.GetNeedData(HRENeed.Patience).BurnRate + "/s");
                Handles.Label(transform.position + new Vector3(0, 1.3f, 0), "Patience: " + patience.CurrentHP);

                if (patience.CurrentHP <= 0)
                {
                    CurrentTask = "leave the store because unhappy";
                }
            }
        }
        //else
        //{
        //    BehaviorDesigner.Runtime.SharedVariable ContainerVariable = OwningAIController.BehaviorManager.GetVariable(CustomerBT, "TargetContainer");
        //    if (ContainerVariable != null)
        //    {
        //        HRDisplayContainer TargetContainer = ContainerVariable.GetValue() as HRDisplayContainer;
        //        if (TargetContainer)
        //        {
        //            CurrentTask = "use display container: " + TargetContainer.name;
        //        }
        //    }
        //}

        Handles.Label(transform.position + new Vector3(0, 1.2f, 0), "Trying to " + CurrentTask);
        Handles.Label(transform.position + new Vector3(0, 1f, 0), "Budget: $" + Budget);
    }
#endif

    void SetHappinessFillAmount(float Amount, float ScaledAmount)
    {
        if (HRNetworkManager.IsHost())
        {
            if (netIdentity != null)
            {
                SetHappinessFillAmount_ClientRpc(Amount, ScaledAmount);
            }
            else
            {
                SetHappinessFillAmount_Implementation(Amount, ScaledAmount);
            }
        }
    }

    [Mirror.ClientRpc]
    void SetHappinessFillAmount_ClientRpc(float Amount, float ScaledAmount)
    {
        SetHappinessFillAmount_Implementation(Amount, ScaledAmount);
    }

    void SetHappinessFillAmount_Implementation(float Amount, float ScaledAmount)
    {
        if (HappinessSlider)
        {
            HappinessSlider.fillAmount = ScaledAmount;
            HappinessSlider.color = Color.Lerp(MinHappinessColor, MaxHappinessColor, ScaledAmount);
        }
    }


    bool bShowingHappinessWarning;

    private void SetNeedsUIEnabled(bool bEnabled)
    {
        if (HRNetworkManager.IsHost())
        {
            SetNeedsUIEnabled_Server(bEnabled);
        }
    }

    private void SetNeedsUIEnabled_Server(bool bEnabled)
    {
        bShowNeedsUI = bEnabled;
    }

    private void HandleShowNeeds_Hook(bool oldValue, bool newValue)
    {
        if (CustomerNeeds)
        {
            CustomerNeeds.SetNeedUIEnabled(newValue);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        HandleShowNeeds_Hook(false, bShowNeedsUI);
    }

    private void OnNeedsUpdated(HRENeed InNeed, float InOldHP, float InNewHP, BaseLiteHP InHPComponent)
    {
        if (InOldHP == InNewHP)
        {
            return;
        }

        switch (InNeed)
        {
            case HRENeed.Patience:

                if (InNewHP <= 0)
                {
                    InNewHP = 0;
                }

                if (InNewHP <= 0)
                {
                    LeavePlot(TargetShopManager != null);
                }

                if ((InNewHP <= PatienceToLeaveThreshold && ShoppingCart.Count > 0))
                {
                    bDoneShopping = true;

                    if (!bFollowing && PlayerInteractionManager)
                    {
                        AssignContainerInteractDelegate(false);
                    }

                    if (CurrentInventory != null)
                    {
                        UnregisterCurrentInventory(InteractedPlayer.connectionToClient);
                    }

                    if (CurrentInventoryUI != null)
                    {
                        UnregisterCurrentInventoryUI(InteractedPlayer.connectionToClient);
                    }
                }

                // Show warning
                if (CurrentBubbleUI != null && !bShowingHappinessWarning && InNewHP <= InHPComponent.MaxHP * LowHappinessWarningThreshold)
                {
                    bShowingHappinessWarning = true;
                    SetBubbleUI(CurrentBubbleUI.Type, HRBubbleUIStatus.LowStart);
                }

                break;
            case HRENeed.Hunger:
                if (InNewHP <= 0)
                {
                    OwningPlayerCharacter.ApplyHungerAttribute(DamageWhenStarving, StarvingDamageInterval);
                    RatingInfo.OnNeedRatingChange(HRENeed.Hunger, false);
                }
                else if (InOldHP <= 0f)
                {
                    OwningPlayerCharacter.RemoveHungerAttribute();
                }
                else if (InNewHP < 30.0f && TargetShopPlot)
                {
                    //CustomerNeeds.SetNeedValue(HRENeed.Patience, 0.0f);
                    //LeavePlot(TargetShopManager != null);
                }
                break;
            case HRENeed.Hygiene:
                break;
            case HRENeed.Thirst:
                if (InNewHP <= 0)
                {
                    OwningPlayerCharacter.ApplyDehydratedAttribute(DamageWhenThirsty, ThirstyDamageInterval);
                    RatingInfo.OnNeedRatingChange(HRENeed.Thirst, false);
                }
                else if (InOldHP <= 0f)
                {
                    OwningPlayerCharacter.RemoveDehydratedAttribute();
                }
                else if (InNewHP < 30.0f && TargetShopPlot)
                {
                    //CustomerNeeds.SetNeedValue(HRENeed.Patience, 0.0f);
                    //LeavePlot(TargetShopManager != null);
                }
                break;
            case HRENeed.Comfort:

                if (OwningPlayerCharacter.CurrentInteractState != HeroPlayerCharacter.InteractState.Sleeping)
                {
                    if (InNewHP <= 0f)
                    {
                        bSleeping = true;
                        OwningPlayerCharacter.SetNewInteractState(HeroPlayerCharacter.InteractState.Sleeping);
                        RatingInfo.OnNeedRatingChange(HRENeed.Comfort, false);
                        CustomerNeeds.SetNeedBurnRateValue(HRENeed.Comfort, -1);
                        CustomerNeeds.SetNeedActive(HRENeed.Patience, false);
                    }
                    else if (InNewHP <= 30f)
                    {
                        if (!OwningPlayerCharacter.AttributeManager.HasAttribute(3, "comfortneed"))
                        {
                            HRRawAttributeData attr = new HRRawAttributeData(3, true, false, "comfortneed", HRAttribute.EAttributeTriggers.None, (1 - TiredMovementSpeedModifier) * -100, -1, 0);
                            OwningPlayerCharacter.AttributeManager.AddAttribute(attr);
                            OwningPlayerCharacter.AnimScript.SetFloat("Tired", 1);
                        }
                    }
                    else if (InOldHP <= 30f)
                    {
                        OwningPlayerCharacter.AttributeManager.RemoveAllAttributesWithID(3, "comfortneed");
                        OwningPlayerCharacter.AnimScript.SetFloat("Tired", 0);
                    }
                }
                else
                {
                    if (InNewHP >= 100f)
                    {
                        AwakenCustomer();
                    }
                }
                break;
            case HRENeed.Entertainment:
                if (InNewHP <= 0f)
                {
                    //OwningPlayerCharacter.SetNewInteractState(HeroPlayerCharacter.InteractState.Sleeping);
                }
                else if (InNewHP <= 30f)
                {
                    if (!OwningPlayerCharacter.AttributeManager.HasAttribute(3, "entertainmentneed"))
                    {
                        HRRawAttributeData attr = new HRRawAttributeData(3, true, false, "entertainmentneed", HRAttribute.EAttributeTriggers.None, (1 - TiredMovementSpeedModifier) * -100, -1, 0);
                        OwningPlayerCharacter.AttributeManager.AddAttribute(attr);
                        OwningPlayerCharacter.AnimScript.SetFloat("Tired", 1);
                    }
                }
                else if (InOldHP <= 30f)
                {
                    OwningPlayerCharacter.AttributeManager.RemoveAllAttributesWithID(3, "entertainmentneed");
                    OwningPlayerCharacter.AnimScript.SetFloat("Tired", 0);
                }
                break;
            case HRENeed.Bladder:
                if (InNewHP <= 0)
                {
                    if (bCanDropBladderMess)
                    {
                        MessGenerator.SpawnDecal(BladderZeroMessPrefab);
                        RatingInfo.OnNeedRatingChange(HRENeed.Bladder, false);
                    }
                }
                else if (InNewHP >= 100)
                {
                    bCanDropBladderMess = true; // refill :)
                }
                break;

        }
    }

    private void HandleInteractStateChanged(HeroPlayerCharacter character, HeroPlayerCharacter.InteractState oldState, HeroPlayerCharacter.InteractState newState)
    {
        if (oldState != newState)
        {
            if (newState == HeroPlayerCharacter.InteractState.Sleeping)
            {
                OwningAIController.BehaviorManager.DisableAllBehaviors();
            }
            else if (oldState == HeroPlayerCharacter.InteractState.Sleeping && newState == HeroPlayerCharacter.InteractState.Free)
            {
                OwningAIController.BehaviorManager.EnableBehavior(CustomerBT);
            }
        }
    }

    public float GetMaxPatience()
    {
        var patience = CustomerNeeds.GetNeedComponent(HRENeed.Patience);

        if (patience)
        {
            return patience.MaxHP;
        }

        return 0;
    }

    BaseLiteHP CachedPatienceHP = null;
    public float GetCurrentPatience()
    {
        if (!CachedPatienceHP)
        {
            CachedPatienceHP = CustomerNeeds.GetNeedComponent(HRENeed.Patience);
        }

        if (CachedPatienceHP)
        {
            return CachedPatienceHP.CurrentHP;
        }

        return 0;
    }

    public void UpdateComfortNeedFromFloorTile(HRFloorTile InFloorTile)
    {
        HRShopManager shopManager = InFloorTile.OwningShop;
        float comfortMultiplier = 1;
        if (OwningPlayerCharacter.CurrentInteractState != HeroPlayerCharacter.InteractState.Sleeping)
        {
            if (InFloorTile.IsRoofed && shopManager.ShopPoliciesManager.GetShelteredEnvironmentNodeActive()) // reduce comfort loss when roofed
            {
                comfortMultiplier *= .5f;
            }
            CustomerNeeds.SetNeedBurnRateBaseValueMultiplier(HRENeed.Comfort, comfortMultiplier);
        }
    }

    public List<HRShopOrder> GetCurrentOrder()
    {
        if (OrdererComponent)
        {
            return OrdererComponent.GetCurrentOrders();
        }
        return null;
    }

    public void OnOrderPlaced()
    {
        if (OrdererComponent)
        {
            OwningAIController.BehaviorManager.SetVariableValue(OrderBT, "bOrderPlaced", true);
            OrdererComponent.OnOrderPlaced();
            OrdererComponent.OnOrderReceivedDelegate += HandleOrderReceived;
        }

        OnCustomerServedDelegate?.Invoke(this);
    }

    private void HandleOrderReceived(HROrdererComponent InOrderer)
    {
        if (InOrderer.OrderAmount <= 0)
        {
            bDoneShopping = true;
        }
        InOrderer.OnOrderReceivedDelegate -= HandleOrderReceived;
    }

    public void SetDoneShopping(bool bDone)
    {
        bDoneShopping = bDone;

        if (bDone)
        {
            bDoneShopping = true;
            ShoppingListSize = 0;
            NumItems_Server = 0;
            CustomerStates.Clear();
        }
    }

    public void HandleSuccessfulSale()
    {
        if (netIdentity != null)
        {
            SuccessfulSaleEffects_ClientRpc();
        }
        else
        {
            SuccessfulSaleEffects_Implementation();
        }

        ShoppingCart.Clear();

        RemoveSpotInQueue();


        LeaveShop(false);
    }

    void ConsumeWeapons(List<BaseWeapon> InWeapons)
    {
        if (InWeapons == null)
        {
            return;
        }

        for (int i = 0; i < InWeapons.Count; ++i)
        {
            ConsumeWeapon(InWeapons[i]);
        }
    }

    public void ConsumeWeapon(BaseWeapon InWeapon)
    {
        if (HRNetworkManager.IsHost())
        {
            ConsumeWeapon_Server(InWeapon);
        }
        else
        {
            ConsumeWeapon_Command(InWeapon);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    void ConsumeWeapon_Command(BaseWeapon InWeapon)
    {
        ConsumeWeapon_Server(InWeapon);
    }

    [Mirror.Server]
    public void ConsumeWeapon_Server(BaseWeapon InWeapon)
    {
        if (InWeapon)
        {
            HRClothingComponent ClothingComponent = InWeapon.GetComponent<HRClothingComponent>();
            if (ClothingComponent)
            {
                Player.EquipmentComponent.EquipItem(ClothingComponent.ClothingType, ClothingComponent);

                if (Player.WeaponManager.CurrentWeapon == InWeapon)
                {
                    Player.WeaponManager.SwitchToNextSlot();
                }
            }
            BaseWeaponConsumable InConsumable = InWeapon.GetComponent<BaseWeaponConsumable>();
            if (InConsumable)
            {
                if (Player.WeaponManager)
                {
                    if (Player.WeaponManager.CurrentWeapon != InWeapon)
                        Player.WeaponManager.SwitchToWeapon(InWeapon, true);

                    InConsumable.StartConsuming();
                }
            }
        }

    }

    [Mirror.ClientRpc]
    void SuccessfulSaleEffects_ClientRpc()
    {
        SuccessfulSaleEffects_Implementation();
    }

    void SuccessfulSaleEffects_Implementation()
    {
        // Make the emoji happy
        if (EmotionComponent)
        {
            SetEmotion_Implementation(HREmotion.Happy, -1f, true);
        }

        PixelCrushers.DialogueSystem.DialogueManager.Bark("GenericSuccessfulSaleBark", this.transform);
    }

    public void RemoveSpotInQueue()
    {
        if (SpotInQueue != null)
        {
            if (SpotInQueue.ParentSystem)
            {
                SpotInQueue.ParentSystem.RemoveSpotFromQueue(SpotInQueue);
            }
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void DropShoppingCart_Command()
    {
        DropShoppingCart_Implementation();
    }

    public void DropShoppingCart()
    {
        if (HRNetworkManager.IsHost())
        {
            DropShoppingCart_Implementation();
        }
        else
        {
            if (netIdentity)
            {
                DropShoppingCart_Command();
            }
        }
    }

    public bool CanEnterShop()
    {
        if (LastTimeVisitedShop == 0 || (Time.timeSinceLevelLoad - LastTimeVisitedShop > ShopVisitCooldown))
        {
            return true;
        }

        return false;
    }

    public void RemoveBudget(float InBudgetToRemove)
    {
        if (HRNetworkManager.IsHost())
        {
            RemoveBudget_Server(InBudgetToRemove);
        }
        else
        {
            RemoveBudget_Command(InBudgetToRemove);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void RemoveBudget_Command(float InBudgetToRemove)
    {
        RemoveBudget_Server(InBudgetToRemove);
    }

    [Mirror.Server]
    public void RemoveBudget_Server(float InBudgetToRemove)
    {
        SetBudget_Server(Budget - InBudgetToRemove);
    }

    [Mirror.Server]
    public void AddBudget_Server(float InBudgetToRemove)
    {
        SetBudget_Server(Budget + InBudgetToRemove);
    }

    public void RemoveItemFromCart(BaseWeapon InWeapon)
    {
        if (HRNetworkManager.IsHost())
        {
            RemoveItemFromCart_Server(InWeapon);
        }
        else
        {
            RemoveItemFromCart_Command(InWeapon);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void RemoveItemFromCart_Command(BaseWeapon InWeapon)
    {
        RemoveItemFromCart_Server(InWeapon);
    }

    [Mirror.Server]
    public void RemoveItemFromCart_Server(BaseWeapon InWeapon)
    {
        if (InWeapon && ShoppingCart.Contains(InWeapon))
            ShoppingCart.Remove(InWeapon);
    }

    public void ClearHeldBudget()
    {
        CurrentHeldBudget = 0;
    }


    public void DropShoppingCart_Implementation()
    {
        ClearHeldBudget();

        // Drop all their items on the floor
        for (int i = ShoppingCart.Count - 1; i >= 0; --i)
        {
            var Item = ShoppingCart[i];


            if (ShoppingCart[i])
            {
                ShoppingCart[i].OwningWeaponManager = null;
            }

            bool dropItem = true;
            if (TargetShopManager)
            {
                BaseContainer returnBox = TargetShopManager.OwningShopPlot.EmployeeSystem.GetClosestReturnBox(transform.position, ShoppingCart[i]);
                if (returnBox)
                {
                    OwningPlayerCharacter.WeaponManager.RemoveWeapon(ShoppingCart[i], false, -1, false, false, false);
                    returnBox.Inventory.InsertWeapon(ShoppingCart[i], returnBox.Inventory.GetFirstFreeSlot(ShoppingCart[i]));
                    dropItem = false;
                }
            }

            if (dropItem && Item.ProjectilePhysics)
            {
                OwningPlayerCharacter.WeaponManager.RemoveWeapon(ShoppingCart[i], true, -1, true, false, false);
                Item.ProjectilePhysics.SetEnabled(true);
                Item.ProjectilePhysics.BeginDropThrow(this.gameObject, dealDamage: false);
            }
        }
        currentTotalPrice = 0;
        ShoppingCart.Clear();
        ShoppingCartIDs.Clear();
        ShoppingCartQuantity.Clear();
    }

    public void CustomerUnhappy(bool bUnhappyFromSale)
    {
        if (HRNetworkManager.IsHost())
        {
            CustomerUnhappy_Server(bUnhappyFromSale);
        }
        else
        {
            CustomerUnhappy_Command(bUnhappyFromSale);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    void CustomerUnhappy_Command(bool bUnhappyFromSale)
    {
        CustomerUnhappy_Server(bUnhappyFromSale);
    }

    [Mirror.Server]
    void CustomerUnhappy_Server(bool bUnhappyFromSale)
    {
        //this.enabled = false;

        DropShoppingCart();

        if (netIdentity != null)
        {
            CustomerUnhappyEffects_ClientRpc();
        }
        else
        {
            CustomerUnhappyEffects_Implementation();
        }

        if (TargetShopManager && TargetShopManager.GetCustomersInShop().Contains(this))
        {
            if (!bUnhappyFromSale)
            {
                HRSaleInfo SaleInfo = new HRSaleInfo();
                SaleInfo.Buyer = gameObject;
                SaleInfo.SaleType = HRSaleType.FAILED;

                TargetShopManager.ItemSold(SaleInfo, 0, transform);
            }
            TargetShopManager.HandleUnhappyCustomer(this);
        }

        TryGenerateRandomMess();

        LeaveShop(bUnhappyFromSale);
    }

    [Mirror.ClientRpc]
    void CustomerUnhappyEffects_ClientRpc()
    {
        CustomerUnhappyEffects_Implementation();
    }

    void CustomerUnhappyEffects_Implementation()
    {
        /*if (EmotionComponent)
        {
            if (HappinessDecayPerSecond > StartingHappinessDecay + 0.1f)
            {
                SetEmotion_Implementation(HREmotion.Disgusted, -1f, true);
            }
            else
            {
                SetEmotion_Implementation(HREmotion.Angry, -1f, true);
            }
            if (CurrentBubbleUI != null)
            {
                SetBubbleUI_Implementation(CurrentBubbleUI.Type, HRBubbleUIStatus.Failure);
            }
        }*/

        if (TargetShopManager && TargetShopManager.GetCustomersInShop().Contains(this))
        {
            PixelCrushers.DialogueSystem.DialogueManager.Bark("GenericFailedSaleBark", this.transform);
        }
    }

    public void PlayVoice(string voiceGroup)
    {
        if (HRNetworkManager.IsHost())
        {
            PlayVoice_ClientRpc(voiceGroup);
        }
    }

    [Mirror.ClientRpc]
    private void PlayVoice_ClientRpc(string voiceGroup)
    {
        PlayVoice_Implementation(voiceGroup);
    }

    private void PlayVoice_Implementation(string voiceGroup)
    {
        if (OwningPlayerCharacter.CharacterVoice && !string.IsNullOrEmpty(voiceGroup))
        {
            OwningPlayerCharacter.CharacterVoice.PlayRandomAudioFromGroup(voiceGroup);
        }
    }

    public void PlayRandomIdleAnimation()
    {
        if (customWaitIdleAnimations.Length == 0)
        {
            return;
        }

        int index = Random.Range(0, customWaitIdleAnimations.Length);
        PlayEmoteAnimation(customWaitIdleAnimations[index]);
    }

    public void PlayBrowseDisplayAnimation()
    {
        PlayEmoteAnimation(customBrowseAnimation);
        PlayVoice("Browse");
    }

    public void PlayBrowseFailAnimation()
    {
        PlayEmoteAnimation(customBrowseFailAnimation);
        PlayVoice("BrowseFail");
    }

    public void PlayBrowseSuccessAnimation()
    {
        PlayEmoteAnimation(customBrowseSuccessAnimation);
        PlayVoice("BrowseSuccess");
    }

    public List<BaseWeapon> BrowseDisplayContainer(HRDisplayContainer InContainer)
    {
        // How do customers determine what to pick up: https://brinley.notion.site/Customer-Demand-b4802f9020ad4bcc9b225133e74a57ce- DEPRECATED

        // Wait for amount of time
        List<BaseWeapon> DesiredItems = GetDesiredItems(InContainer);

        bool bPickedUpItem = false;
        bool bPickedUpMoney = false;

        if (DesiredItems.Count > 0)
        {
            // Temp thing to make customers leave if they pick up money

            for (int i = 0; i < DesiredItems.Count; i++)
            {
                if (ShoppingCart.Count < ShoppingCartMax && DesiredItems[i].CanPickUp(OwningPlayerCharacter.WeaponManager))
                {
                    AddToCart(DesiredItems[i]);

                    //Hack to keep attributes on bought items
                    DesiredItems[i].IsHeldByCustomer = true;
                    InContainer.OwningContainer.Inventory.RemoveWeapon(DesiredItems[i], -1, true, false);
                    OwningPlayerCharacter.WeaponManager.AttemptPickupWeapon(DesiredItems[i]);
                    bPickedUpItem = true;

                    // 1173 is the money ID
                    if (DesiredItems[i].ItemID == MoneyItemID)
                    {
                        bPickedUpMoney = true;
                    }

                    if (bSearching)
                    {
                        StopSearching();
                    }
                }
            }

            BaseHP HPComponent = InContainer?.OwningContainer?.Inventory?.OwningWeapon?.HPComponent;
            if (HPComponent)
            {
                if (HPComponent.CurrentHP <= InContainer.CustomerBrowseSelfDamage)
                {
                    HPComponent.SetHP(1, InContainer.gameObject, true);
                }
                else
                {
                    HPComponent.RemoveHP(InContainer.CustomerBrowseSelfDamage, InContainer.gameObject);
                }
            }
        }

        if (bPickedUpItem)
        {
            PlayBrowseSuccessAnimation();

            // Sprint out of store if they picked up money
            if (bPickedUpMoney)
            {
                OwningPlayerCharacter?.MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.SPRINTING);
                LeavePlot(false, true, false);
            }
        }
        else
        {
            PlayBrowseFailAnimation();
        }

        return DesiredItems;
    }

    public void PlayWalkAndHoldAnimation()
    {
        Debug.Log("WALKING AND HOLDING ANIMATION");
        if (netIdentity != null)
        {
            PlayWalkandHoldAnimation_ClientRpc();
        }
        else
        {
            PlayWalkandHoldAnimation_Implementation();
        }
    }

    [Mirror.ClientRpc]
    void PlayWalkandHoldAnimation_ClientRpc()
    {
        PlayWalkandHoldAnimation_Implementation();
    }

    void PlayWalkandHoldAnimation_Implementation()
    {
        HRMasterAnimDatabase.HRAnimStruct AnimStruct;
        ((HRGameInstance)BaseGameInstance.Get).MasterAnimDB.GetAnimStruct(customHoldIdleAnimation, out AnimStruct);

        if (AnimStruct.ClipToPlay != null)
        {
            OwningPlayerCharacter.PlayAnimation(AnimStruct.ClipToPlay, 0.25f, AnimStruct.AnimLayer);
        }
    }

    public SyncList<BaseWeapon> GetShoppingCart(HeroPlayerCharacter Target)
    {
        return ShoppingCart;
    }

    public void HandleBarterStart(HRBarterDialogueStarter InBarterer)
    {
        if (HRNetworkManager.IsHost())
        {
            HandleBarterStart_Server(InBarterer);
        }
        else
        {
            HandleBarterStart_Command(InBarterer);
        }
    }

    [Mirror.Server]
    void HandleBarterStart_Server(HRBarterDialogueStarter InBarterer)
    {
        if (InBarterer.CustomerAI)
        {
            // Clear this budget so that the customer can freely pick stuff up again
            InBarterer.CustomerAI.ClearHeldBudget();

            // Pause the needs during minigame
            InBarterer.CustomerAI.CustomerNeeds.SetIsTicking(false);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    void HandleBarterStart_Command(HRBarterDialogueStarter InBarterer)
    {
        HandleBarterStart_Server(InBarterer);
    }

    public void HandleBarterStop(HRBarterDialogueStarter InBarterer)
    {
        if (HRNetworkManager.IsHost())
        {
            HandleBarterStop_Server(InBarterer);
        }
        else
        {
            HandleBarterStop_Command(InBarterer);
        }
    }

    [Mirror.Server]
    void HandleBarterStop_Server(HRBarterDialogueStarter InBarterer)
    {
        // Start needs back up after stopping bartering
        if (InBarterer && InBarterer.CustomerAI)
            InBarterer.CustomerAI.CustomerNeeds.SetIsTicking(true);
        else
            CustomerNeeds.SetIsTicking(true);
    }

    [Mirror.Command(ignoreAuthority = true)]
    void HandleBarterStop_Command(HRBarterDialogueStarter InBarterer)
    {
        HandleBarterStop_Server(InBarterer);
    }

    public bool TryGenerateRandomMess()
    {
        if (MessGenerator && (TargetShopPlot != null))
        {
            // Only want to drop messes if you are inside the shop which the graph mask guarantees
            if (OwningAIController != null && OwningAIController.AIMovement != null && OwningAIController.AIMovement._OwningSeeker != null && TargetShopPlot.graphRef != null)
            {
                if (OwningAIController.AIMovement._OwningSeeker.graphMask == Pathfinding.GraphMask.FromGraph(TargetShopPlot.graphRef))
                {
                    if (MessGenerator.isActiveAndEnabled)
                    {
                        MessGenerator.SetEnabled(true);
                    }

                    return MessGenerator.TryGenerateMess(GetMessGenerationProbability());
                }
            }
        }

        return false;
    }

    public float GetMessGenerationProbability()
    {
        float Probabilty = HygieneToTrashDropChance.Evaluate(CustomerNeeds.GetNeedComponent(HRENeed.Hygiene).CurrentHP);
        if (TargetShopManager)
        {
            Probabilty *= TargetShopManager.DropMessModifier;
        }
        return Probabilty;
    }

    public void AddToCart(BaseWeapon InItem)
    {
        if (HRNetworkManager.IsHost())
        {
            AddToCart_Server(InItem);
        }
        else
        {
            AddToCart_Command(InItem);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void AddToCart_Command(BaseWeapon InItem)
    {
        AddToCart_Server(InItem);
    }

    [Mirror.Server]
    private void AddToCart_Server(BaseWeapon InItem)
    {
        if (ShoppingCart.Contains(InItem))
        {
            return;
        }

        if (ShoppingCart.Count >= ShoppingCartMax)
        {
            return;
        }

        bItemFound = true;
        var TargetItem = InItem;

        ShoppingCart.Add(TargetItem);
        ShoppingCartIDs.Add(TargetItem.ItemID);

        if (ShoppingCartQuantity.ContainsKey(TargetItem.ItemID))
        {
            ShoppingCartQuantity[TargetItem.ItemID]++;
        }
        else
        {
            ShoppingCartQuantity.Add(TargetItem.ItemID, 1);
        }

        currentTotalPrice += InItem.ItemValue.GetValue();

        NumItems_Server++;

        UpdateDoneShopping();
    }

    public void RemoveFromCart(BaseWeapon InItem)
    {
        if (HRNetworkManager.IsHost())
        {
            RemoveFromCart_Server(InItem);
        }
        else
        {
            RemoveFromCart_Command(InItem);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void RemoveFromCart_Command(BaseWeapon InItem)
    {
        RemoveFromCart_Server(InItem);
    }

    [Mirror.Server]
    private void RemoveFromCart_Server(BaseWeapon InItem)
    {
        if (!ShoppingCart.Contains(InItem))
        {
            return;
        }
        ShoppingCart.Remove(InItem);
        currentTotalPrice -= InItem.ItemValue.GetValue();
    }

    public void UpdateDoneShopping(bool bForceEnd = false)
    {
        if (bForceEnd)
        {
            bDoneShopping = true;
        }
        else
        {
            if (NumItems_Server >= GetMaximumCartSize())
            {
                bDoneShopping = true;
            }
        }

        if (bDoneShopping)
        {
            ResetNoRegister();
        }
    }


    public void EndShoppingEarly()
    {
        if (HRNetworkManager.IsHost())
        {
            EndShoppingEarly_Implementation();
        }
        else
        {
            EndShoppingEarly_Command();
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    public void EndShoppingEarly_Command()
    {
        EndShoppingEarly_Implementation();
    }


    public void EndShoppingEarly_Implementation()
    {
        ShoppingListSize = 0;
        NumItems_Server = 0;
        SetDoneShopping(true);
        UpdateDoneShopping(true);
    }

    public void ShowHappinessSlider()
    {
        // Handled by Needs UI
        /*if (HRNetworkManager.IsHost())
        {
            if (HappinessSliderUI && bShouldShowHappiness)
            {
                bShowingHappiness = true;
                SetHappinessFillAmount(HappinessLevel / (GetMaxHappiness() != 0f ? GetMaxHappiness() : 1f), HappinessLevel / (GetMaxHappiness() != 0f ? MaxHappiness * 0.5f : 1f));
                if (netIdentity != null)
                {
                    ShowHappinessSlider_ClientRpc();
                }
                else
                {
                    ShowHappinessSlider_Implementation();
                }
            }
        }*/
    }

    [Mirror.ClientRpc]
    void ShowHappinessSlider_ClientRpc()
    {
        ShowHappinessSlider_Implementation();
    }

    void ShowHappinessSlider_Implementation()
    {
        if (HappinessSlider)
        {
            HappinessSliderUI.SetVisible(true);
        }
    }

    public void HideHappinessSlider()
    {
        // Handled by Needs UI
        /*if (HRNetworkManager.IsHost())
        {
            if (HappinessSlider)
            {
                bShowingHappiness = false;
                if (netIdentity != null)
                {
                    HideHappinessSlider_ClientRpc();
                }
                else
                {
                    HideHappinessSlider_Implementation();
                }
            }
        }*/
    }

    [Mirror.ClientRpc]
    void HideHappinessSlider_ClientRpc()
    {
        HideHappinessSlider_Implementation();
    }

    void HideHappinessSlider_Implementation()
    {
        if (HappinessSlider)
        {
            HappinessSliderUI.SetVisible(false);
            HappinessSliderUI.transform.SetParent(transform);
            HappinessSliderUI.transform.localScale = Vector3.one;
        }
    }


    public List<BaseWeapon> GetDesiredItems(HRDisplayContainer InContainer)
    {
        return GetDesiredItems(InContainer.OwningContainer.Inventory);
    }

    float GetDesiredScore(float InScore, List<HRItemCategorySO> WeaponCategoryList, ref List<HRItemCategorySO> CustomerCategoryList)
    {
        float FinalScore = 0;

        for (int j = 0; j < WeaponCategoryList.Count; ++j)
        {
            if (WeaponCategoryList[j] && CustomerCategoryList.Contains(WeaponCategoryList[j]))
            {
                FinalScore += InScore;
            }
        }

        return FinalScore;
    }

    public float GetTotalDesiredScore(BaseWeapon InWeapon)
    {
        if (InWeapon)
        {
            float FinalScore = 0;

            FinalScore += GetDesiredScore(CustomerDemandDB.PrimaryLikeWorth, InWeapon.ItemSizeAndType.GetItemCategories(), ref CustomerDemandDB.LikedCategories);
            FinalScore += GetDesiredScore(CustomerDemandDB.PrimaryDislikePointsWorth, InWeapon.ItemSizeAndType.GetItemCategories(), ref CustomerDemandDB.DislikedCategories);

            return FinalScore;
        }

        return 0;
    }

    private List<BaseWeapon> GetDesiredItems(BaseInventory InContainer)
    {
        // Reasoning: https://brinley.notion.site/Customer-Demand-b4802f9020ad4bcc9b225133e74a57ce - DEPRECATED
        List<BaseWeapon> DesiredItems = new List<BaseWeapon>();

        // Check to see if we even want to pick up the item
        //float CustomerLikeThreshold = (GetCurrentPatience() / GetMaxPatience()) >= LikeScorePatienceThreshold ? LikeScoreThreshold : 0;

        bool stocked = InContainer.NumItems > 0;

        for (int i = 0; i < InContainer.InventorySlots.Count; ++i)
        {
            BaseWeapon CurrentWeapon = InContainer.InventorySlots[i].SlotWeapon;
            if (CurrentWeapon)
            {
                /*
                float FinalScore = CustomerDemandDB.bIgnoreLikeAndDislike ? float.MaxValue : GetTotalDesiredScore(CurrentWeapon);

                if (FinalScore >= CustomerLikeThreshold)
                {
                    AddToDesiredItems(CurrentWeapon, ref DesiredItems);
                }
                */
                AddToDesiredItems(CurrentWeapon, ref DesiredItems);
            }
        }

        return DesiredItems;
    }

    public void AddToDesiredItems(BaseWeapon InWeapon, ref List<BaseWeapon> DesiredItems, int Index = -1)
    {
        CurrentHeldBudget += InWeapon.ItemValue.GetValue();
        DesiredItems.Add(InWeapon);
    }

    public void AttemptSecondaryItem(BaseWeapon InWeapon, ref List<BaseWeapon> DesiredItems)
    {
        // Get the demand for this item.
        float Probability = Random.Range(0.0f, 1.0f);
        // Add modifier from attractivity items nearby.
        float ProbabilityOfBuying = (1.0f - CustomerDemandDB.GetPurchasePercentageAt(InWeapon.ItemID)) + DemandAttractivityModifier;
        // Check if we have enough money and space to buy along with our primary list 
        if (Probability > ProbabilityOfBuying && Budget - CurrentHeldBudget >= InWeapon.ItemValue.GetValue() && (ShoppingCart.Count + ShoppingListSize) < ShoppingCartMax)
        {
            // We wanna buy it now.
            CurrentHeldBudget += InWeapon.ItemValue.GetValue();
            DesiredItems.Add(InWeapon);
        }
    }

    public bool IsCustomerDoneShopping()
    {
        return bDoneShopping;
    }

    // Probably should be in a separate component.
    public void HandleItemAttractivityAdded(HRItemAttractivity InAttractivity)
    {
        if (!AttractivityItemsInRange.Contains(InAttractivity) && !bIgnoreAttractivity)
        {
            AttractivityItemsInRange.Add(InAttractivity);

            CustomerNeeds.ModifyNeedBurnRateValue(HRENeed.Patience, -InAttractivity.HappinessDecayModifier);
            DemandAttractivityModifier += InAttractivity.DemandModifier;

            InAttractivity.RemovedDelegate += HandleAttractivityRemoved;
        }
    }

    void HandleAttractivityRemoved(HRItemAttractivity InAttractivity)
    {
        if (!bIgnoreAttractivity)
        {
            AttractivityItemsInRange.Remove(InAttractivity);

            CustomerNeeds.ModifyNeedBurnRateValue(HRENeed.Patience, InAttractivity.HappinessDecayModifier);
            DemandAttractivityModifier -= InAttractivity.DemandModifier;

            InAttractivity.RemovedDelegate -= HandleAttractivityRemoved;
        }
    }

    public void SetHappinessDecay(float decay)
    {
        CustomerNeeds.SetNeedBurnRateValue(HRENeed.Patience, decay);
    }

    private void OnDestroy()
    {
        if (bClientMode) { return; }

        _OnDestroy();
    }

    private void _OnDestroy(bool bDied = true)
    {
        if (OwningPlayerCharacter.HP.CurrentHP != 0 && OwningPlayerCharacter.FearComponent && OwningPlayerCharacter.FearComponent.bIsFleeing)
        {
            if (bDied)
            {
                if (LastTargetShopManager && LastTargetShopManager.GetCustomersInShop().Contains(this))
                {
                    HRSaleInfo SaleInfo = new HRSaleInfo();
                    SaleInfo.SaleType = HRSaleType.FAILED;
                    int LBPoints;
                    float XP;
                    HRShopEntityManager.Get.AddSale(LastTargetShopManager.ShopName, SaleInfo, out LBPoints, out XP);
                }
            }
        }

        if (OwningPlayerCharacter.PauseReceiver)
        {
            OwningPlayerCharacter.PauseReceiver.OnPauseDelegate -= HandlePause;
        }

        OwningPlayerCharacter.OnStateChangeDelegate -= HandleStateChange;
        if (EmotionComponent && EmotionComponent.FloatingUIComponent)
        {
            SetEmotion(HREmotion.None, -1);
            EmotionComponent.FloatingUIComponent.SetVisible(false);
            Destroy(EmotionComponent.FloatingUIComponent.gameObject);
        }

        if (HappinessSliderUI)
        {
            HappinessSliderUI.SetVisible(false);
            //Destroy(HappinessSliderUI.gameObject);
        }
        if (!bFollowing && PlayerInteractionManager)
        {
            AssignContainerInteractDelegate(false);
        }

        if (CurrentInventory != null)
        {
            UnregisterCurrentInventory(InteractedPlayer.connectionToClient);
        }

        if (CurrentInventoryUI != null)
        {
            UnregisterCurrentInventoryUI(InteractedPlayer.connectionToClient);
        }

        if (bBoundNeeds)
        {
            bBoundNeeds = false;
            CustomerNeeds.OnNeedUpdatedDelegate -= OnNeedsUpdated;
        }

        OnDestroyDelegate?.Invoke(this);
        OnDestroyedDelegate?.Invoke(this);
    }

    void HandlePause(BasePauseReceiver InPauseReceiver, bool bPaused)
    {
        if (bIsPaused != bPaused)
        {
            bIsPaused = bPaused;

            if (HRNetworkManager.IsHost())
            {
                SetBehaviorTreeEnabled_Server(!bPaused);
            }
        }
    }
    private bool CheckNonInteractState(HeroPlayerCharacter.InteractState state)
    {
        return state == HeroPlayerCharacter.InteractState.Stunned
            || state == HeroPlayerCharacter.InteractState.Dying
            || state == HeroPlayerCharacter.InteractState.Dead;
    }
    void HandleStateChange(HeroPlayerCharacter InPlayerCharacter, HeroPlayerCharacter.InteractState PrevState, HeroPlayerCharacter.InteractState NewState)
    {
        if (!bIsPaused && HRNetworkManager.IsHost())
        {
            if (CheckNonInteractState(NewState))
            {
                SetBehaviorTreeEnabled_Server(false, false);
                RemoveSpotInQueue();
                if (OwningAIController?.AIMovement)
                {
                    OwningAIController.AIMovement.bUseSplinePath = false;
                    if (OwningAIController.AIMovement.PathFollower)
                    {
                        OwningAIController.AIMovement.PathFollower.ResetFollower();
                    }
                }
            }
            else if (CheckNonInteractState(PrevState))
            {
                SetBehaviorTreeEnabled_Server(true);
            }

            if (PrevState == HeroPlayerCharacter.InteractState.Sleeping)
            {
                OwningPlayerCharacter.MovementComponent.FreezeMovement(false);
                //OwningPlayerCharacter.AnimScript.AnimancerComponent.Play(OwningPlayerCharacter.AnimScript.AnimancerComponent.Controller);

                if (bSleeping)
                {
                    bSleeping = false;
                    CustomerNeeds.SetNeedBurnRateValue(HRENeed.Comfort, -1);
                }
            }
        }
    }

    public void SetBehaviorTreeEnabled(bool bEnabled, bool bPause = true)
    {
        if (HRNetworkManager.IsHost())
        {
            SetBehaviorTreeEnabled_Server(bEnabled, bPause);
        }
        else
        {
            SetBehaviorTreeEnabled_Command(bEnabled, bPause);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    void SetBehaviorTreeEnabled_Command(bool bEnabled, bool bPause)
    {
        SetBehaviorTreeEnabled_Server(bEnabled, bPause);
    }

    void SetBehaviorTreeEnabled_Server(bool bEnabled, bool bPause = true)
    {
        if (bEnabled)
        {
            OwningAIController?.AIMovement?.SetAIMovementEnabled(true);
            if (OwningAIController?.AIMovement) { OwningAIController.AIMovement.SetDestination(OwningAIController.AIMovement.TargetDestination); }

            if (MainCustomerBT && (OwningPlayerCharacter && (!OwningPlayerCharacter.FearComponent || !OwningPlayerCharacter.FearComponent.bIsFleeing)))
            {
                OwningAIController.BehaviorManager.EnableBehavior(MainCustomerBT);
            }
        }
        else
        {
            OwningAIController?.AIMovement?.StopMovement();
            OwningAIController?.AIMovement?.SetAIMovementEnabled(false);

            if (MainCustomerBT)
            {
                OwningAIController.BehaviorManager.DisableBehavior(MainCustomerBT, bPause);
            }
        }
    }

    void HandleRagdoll(BaseRagdollManager InRagdollManager, bool bEnabled, GameObject Instigator)
    {
        if (Interactable)
        {
            SetInteractableActive(!bEnabled);
        }

        if (!bSleeping)
        {
            if (bEnabled)
            {
                //CustomerNeeds.ModifyNeedBurnRateValue(HRENeed.Patience, UnhappinessFromRagdoll);
            }
            else
            {
                //CustomerNeeds.ModifyNeedBurnRateValue(HRENeed.Patience, -UnhappinessFromRagdoll);
            }
        }
    }

    private void HandleHPChanged(BaseHP InHPComponent, GameObject Instigator, float PreviousHP, float NewHP, bool bFireEffects)
    {
        if (HRNetworkManager.IsHost())
        {
            if (bSleeping)
            {
                AwakenCustomer();
            }

            //CurrentHPChanged so that it triggers before Mineable.Harvest
            if ((NewHP > 0 && !OwningPlayerCharacter.HP.bOnHPZeroFired)) return;

            HRMineableComponent Mineable = OwningPlayerCharacter.GetComponent<HRMineableComponent>();
            if (Mineable)
            {
                Mineable.ClearRuntimeDrops();
                Mineable.AddRuntimeDrops(MoneyItemID, (uint)Random.Range(Budget * MinBudgetDropFraction, Budget * MaxBudgetDropFraction));
            }
        }
    }


    public void SetHappinessDecayEnabled(bool bEnabled)
    {
        if (!HRNetworkManager.IsHost())
        {
            SetHappinessDecayEnabled_Command(bEnabled);
        }
        else
        {
            CustomerNeeds.SetNeedActive(HRENeed.Patience, bEnabled);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SetHappinessDecayEnabled_Command(bool bEnabled)
    {
        CustomerNeeds.SetNeedActive(HRENeed.Patience, bEnabled);
    }

    public void StopWaitingAtRegister()
    {
        if (HRNetworkManager.IsHost())
        {
            StopWaitingAtRegister_Implementation();
        }
        else
        {
            StopWaitingAtRegister_Command();
        }
    }

    private void StopWaitingAtRegister_Implementation()
    {
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "TargetRegister", null);
        TargetRegister = null;
        OnCustomerRemovedFromRegisterDelegate?.Invoke(this);

        CustomerNeeds.SetIsTicking(true);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void StopWaitingAtRegister_Command()
    {
        if (HRNetworkManager.IsHost())
        {
            OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "TargetRegister", null);
            TargetRegister = null;
            OnCustomerRemovedFromRegisterDelegate?.Invoke(this);
        }
    }

    public void SetSpotInQueue(BaseQueueSpot InSpot)
    {
        if (InSpot != null)
        {
            InSpot.SetAIController(OwningAIController);
            SpotInQueue = InSpot;
            SpotInQueue.OnRemoveDelegate += HandleSpotRemoved;
        }
    }

    public void HandleSpotRemoved(BaseQueueSpot InSpot, bool bSuccess)
    {
        // Ha ha ha, CACHED Register
        var CachedRegister = TargetRegister;
        SpotInQueue.OnRemoveDelegate -= HandleSpotRemoved;

        if (SpotInQueue.ParentSystem.Head == SpotInQueue)
        {
            if (TargetRegister)
            {
                TargetRegister.RemoveWaitingCustomer();
            }
        }

        // This is false when the sign has been removed or destroyed.
        if (!bSuccess)
        {
            // The customer likely has not entered the shop so we should clear their stimuli if any exist so they don't post bad reviews.
            if (!CachedRegister)
            {
                RatingInfo.ResetRating();
            }

            DropShoppingCart();
            LeaveShop(false);
        }

        OnSpotRemovedDelegate?.Invoke(this, InSpot);
    }

    public void HandleStartedFleeing(HRFearComponent InFear, bool bStarted, bool bFighting)
    {
        if (bStarted)
        {
            if (OwningAIController)
            {
                OwningAIController.SetGenericGraph();

                if (OwningAIController.AIMovement)
                {
                    OwningAIController.AIMovement.bUseSplinePath = false;
                    if (OwningAIController.AIMovement.PathFollower)
                    {
                        OwningAIController.AIMovement.PathFollower.ResetFollower();
                    }
                }
            }

            // Cancel all conversations using this customer	
            if (HRNetworkManager.IsHost())
            {
                CancelAllConversations_Implementation();
                CancelAllConversations();
            }


        }

        if (MainCustomerBT)
        {
            if (bStarted)
            {
                ResetEmotion();

                if (InFear.LastInstigator)
                {
                    /*
                    HeroPlayerCharacter PC = InFear.LastInstigator.GetComponent<HeroPlayerCharacter>();
                    if (PC)
                    {
                        //if (bFighting && PC.CurrentFaction == HeroPlayerCharacter.Faction.Law && Player.CurrentFaction == HeroPlayerCharacter.Faction.None)
                        
                        if (bFighting && PC.FactionDataAsset && PC.FactionDataAsset.OwnerFaction == HeroPlayerCharacter.Faction.Law 
                            && Player.FactionDataAsset && Player.FactionDataAsset.OwnerFaction == HeroPlayerCharacter.Faction.Customer)
                        {
                            return; // Don't randomly attack police from fear component if customer has no faction
                        }
                        
                        PC.HP.OnHPZeroDelegate -= HandleTargetHPZero;
                        PC.HP.OnHPZeroDelegate += HandleTargetHPZero;
                        
                    }
                        */
                }

                CustomerUnhappy(false);
                SetTargetShopPlot(null);

                if (bFighting)
                {
                    if (OwningPlayerCharacter.AnimScript)
                    {
                        // Simple look at instigator, maybe have a timer to turn off ik
                        OwningPlayerCharacter.AnimScript.SetLookAtTarget(null);
                    }

                    OwningPlayerCharacter.CharacterVoice.PlayRandomAudioFromGroup("Angry");
                }
                else
                {
                    // Simple look at instigator, maybe have a timer to turn off ik
                    if (InFear.LastInstigator)
                    {
                        OwningPlayerCharacter.AnimScript.SetLookAtTarget(InFear.LastInstigator.transform);
                    }

                    OwningPlayerCharacter.CharacterVoice.PlayRandomAudioFromGroup("FleeingScared");
                }

                OwningAIController.BehaviorManager.DisableBehavior(CustomerBT);
                OwningAIController.BehaviorManager.DisableBehavior(RetailBT);
                OwningAIController.BehaviorManager.DisableBehavior(OrderBT);
                OwningAIController.BehaviorManager.DisableBehavior(RestaurantBT);

                HideHappinessSlider();

                if (bQueued && TargetShopManager)
                {
                    TargetShopManager.OnCustomerCountChange -= OnCustomersInShopChanged;
                }

                bQueued = false;
                bGreet = false;
                bCanInteract = false;
                SetWaitingOutside(false);
                bUseSearchingTimer = false;

                if (HRNetworkManager.IsHost())
                {
                    SetInteractableActive(false);
                }

                if (CurrentBubbleUI != null)
                {
                    CurrentBubbleUI.UIObject?.Hide();
                }


                DropShoppingCart();

                // Cancel searching
                SetSearching(false);

                if (CurrentInventory != null)
                {
                    UnregisterCurrentInventory(InteractedPlayer.connectionToClient);
                }
                if (CurrentInventoryUI != null)
                {
                    UnregisterCurrentInventoryUI(InteractedPlayer.connectionToClient);
                }


                if (InteractedPlayer != null)
                {
                    CancelMinigame(InteractedPlayer.connectionToClient);
                    RestoreDefaultUI(InteractedPlayer.connectionToClient);

                    if (bFollowing)
                    {
                        AssignContainerInteractDelegate(false);
                    }
                }

                if (OwningAIController.AIMovement.PlayerFollow && OwningAIController.AIMovement.PlayerFollow.OwningManager)
                {
                    OwningAIController.AIMovement.PlayerFollow.OwningManager.RemoveFollower(OwningAIController.AIMovement.PlayerFollow);
                }

                if (bStarted)
                {
                    RemoveSpotInQueue();
                    OwningAIController.AIMovement.bWalkWhenNearDestination = false;
                    OwningAIController.AIMovement.OwningMovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.JOGGING);
                    if (bFighting)
                    {
                        SetAttackTarget(InFear.TargetToFleeFrom);
                        BeginAttacking();
                        if (Player.BarkComponent)
                        {
                            Player.BarkComponent.PlayBark(onAggroBark);
                            if (!string.IsNullOrEmpty(OnCombatBark.ConversationName))
                            {
                                if (combatBarkCoroutine != null)
                                {
                                    StopCoroutine(combatBarkCoroutine);
                                }
                                combatBarkCoroutine = StartCoroutine(CombatBarkCoroutine(OnCombatBark.Cooldown));
                            }
                        }
                    }
                    else
                    {
                        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bFleeing", bStarted);
                        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "FleeTarget", InFear.TargetToFleeFrom);
                        OwningAIController.BehaviorManager.DisableBehavior(MainCustomerBT);
                    }
                }
            }
            else
            {
                OwningAIController.BehaviorManager.RestartBehavior(MainCustomerBT);

                if (!bFighting)
                {
                    if (CombatComponent)
                    {
                        CombatComponent.RemoveTarget();
                    }
                }

                bCanInteract = false;
            }
        }
    }
    public void CancelAllConversations()
    {
        if (HRNetworkManager.IsHost())
        {
            CancelAllConversations_ClientRpc();
        }
        else
        {
            CancelAllConversations_Command();
        }
    }
    [Mirror.Command(ignoreAuthority = true)]
    public void CancelAllConversations_Command()
    {
        CancelAllConversations_ClientRpc();
    }
    [Mirror.ClientRpc(excludeOwner = true)]
    public void CancelAllConversations_ClientRpc()
    {
        CancelAllConversations_Implementation();
    }
    public void CancelAllConversations_Implementation()
    {
        if (HRDialogueSystem.Get && HRDialogueSystem.Get.GetCurrentLocalConversation() != null)
        {
            if (HRDialogueSystem.Get.GetCurrentLocalConversation().Actor != null &&
                HRDialogueSystem.Get.GetCurrentLocalConversation().Actor == this.transform)
            {
                HRDialogueSystem.Get.EndExclusiveConversation(HRDialogueSystem.Get.GetCurrentLocalConversation().ConversationID);
            }
            else
            {
                Debug.LogError("haha, no actor somehow");
            }
        }
    }

    public override void SetAttackTarget(GameObject InTarget)
    {
        base.SetAttackTarget(InTarget);
        if (!InTarget)
        {
            if (OwningPlayerCharacter.FearComponent)
            {
                OwningPlayerCharacter.FearComponent.SetFighting(false);
            }
        }
        // This is not good. It should route through hostile AI or something.
        if (targetGameObject && targetGameObject != InTarget)
        {
            HeroPlayerCharacter PC = targetGameObject.GetComponent<HeroPlayerCharacter>();

            if (PC)
            {
                PC.HP.OnHPZeroDelegate -= HandleTargetHPZero;
                PC.OnDisableDelegate -= HandleTargetPawnDisabled;
            }

            if (CombatComponent)
            {
                CombatComponent.RemoveTarget();
            }
        }
        targetGameObject = InTarget;

        if (MainBT)
        {
            // Let AI roam free when attacking.
            if (InTarget)
            {
                OwningAIController.AIMovement.SetMovementLayer(AIMoveLayer.GENERIC);
                OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bAttacking", true);
            }

            MainBT.SetVariableValue("ObjectToAttack", InTarget);

        }
        if (InTarget && !TargetList.Contains(InTarget))
        {
            HeroPlayerCharacter PC = InTarget.GetComponent<HeroPlayerCharacter>();
            if (PC)
            {
                PC.HP.OnHPZeroDelegate -= HandleTargetHPZero;
                PC.HP.OnHPZeroDelegate += HandleTargetHPZero;

                PC.OnDisableDelegate -= HandleTargetPawnDisabled;
                PC.OnDisableDelegate += HandleTargetPawnDisabled;
            }
        }

    }

    public override void SetBTAttackTargetList(List<GameObject> AttackTargetList)
    {
        OwningAIController.BehaviorManager.SetAllVariableValues("TargetList", AttackTargetList);
    }

    public override void SetHostileState(HRHostileState NewState)
    {
        base.SetHostileState(NewState);
        HRHostileState oldState = currentHostileState;
        if (currentHostileState != NewState)
        {
            currentHostileState = NewState;
            switch (NewState)
            {
                case HRHostileState.Attacking:
                    break;
                case HRHostileState.Searching:
                    break;
                case HRHostileState.Alert:
                    //StopAttacking();
                    break;
                case HRHostileState.Inactive:
                    break;
            }
            OwningAIController.BehaviorManager.RestartBehavior(MainCustomerBT);
            OnHostileStateChangeDelegate?.Invoke(this, oldState, NewState);
        }
    }

    public override void RequestSetHostileState(HRHostileState NewState)
    {
        SetHostileState(NewState);
    }

    [Button("Convert To Enemy")]
    public void ConvertToEnemy()
    {
        SetAttackTarget(BaseGameInstance.Get.GetFirstPawn().gameObject);
        BeginAttacking();
    }

    void BeginAttacking()
    {
        OwningPlayerCharacter.WeaponManager.SwitchToWeapon(CurrentWeaponInstance);
        BaseWeapon CurrentWeapon = CurrentWeaponInstance;
        if (!CurrentWeapon)
        {
            for (int i = 0; i < OwningPlayerCharacter.WeaponManager.HotKeyInventory.InventorySlots.Count; ++i)
            {
                CurrentWeapon = OwningPlayerCharacter.WeaponManager.HotKeyInventory.InventorySlots[i].SlotWeapon;
                if (CurrentWeapon)
                {
                    OwningPlayerCharacter.WeaponManager.SwitchToSlot(i);
                    break;
                }
            }
        }
        if (CurrentWeapon)
        {
            BaseWeaponMode WeaponMode = CurrentWeapon.GetComponent<BaseWeaponMode>();
            if (WeaponMode)
            {
                OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "GunComponent", WeaponMode);
            }
            OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "WeaponComponent", CurrentWeapon);
            if (MainBT)
            {
                OwningAIController.BehaviorManager.SetVariableValue(MainBT, "WeaponComponent", CurrentWeapon);
            }
        }
        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bAttacking", true);
        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bFleeing", false);
        OwningAIController.BehaviorManager.RestartBehavior(MainCustomerBT);

        if (CombatComponent)
        {
            CombatComponent.AddTarget(targetGameObject);
        }

        RequestSetHostileState(HRHostileState.Attacking);
    }

    void StopAttacking()
    {
        OwningAIController.BehaviorManager.DisableBehavior(MainBT);
        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bAttacking", false);
        OwningAIController.BehaviorManager.RestartBehavior(MainCustomerBT);
    }

    public override bool AddTargetToList(GameObject InTarget)
    {
        if (base.AddTargetToList(InTarget))
        {
            HeroPlayerCharacter PC = InTarget.GetComponent<HeroPlayerCharacter>();
            if (PC)
            {
                PC.HP.OnHPZeroDelegate -= HandleTargetHPZero;
                PC.HP.OnHPZeroDelegate += HandleTargetHPZero;

                PC.OnDisableDelegate -= HandleTargetPawnDisabled;
                PC.OnDisableDelegate += HandleTargetPawnDisabled;
            }
            return true;
        }
        return false;
    }

    public override bool RemoveTargetFromList(GameObject InTarget)
    {
        if (base.RemoveTargetFromList(InTarget))
        {
            HeroPlayerCharacter PC = InTarget.GetComponent<HeroPlayerCharacter>();
            if (PC)
            {
                PC.HP.OnHPZeroDelegate -= HandleTargetHPZero;
                PC.OnDisableDelegate -= HandleTargetPawnDisabled;
            }
            return true;
        }
        return false;
    }

    void HandleTargetHPZero(BaseHP InHPComponent, GameObject Instigator, float PreviousHP, float NewHP, bool bFireEffects)
    {
        SetAttackTarget(null);
        RemoveTargetFromList(InHPComponent.gameObject);
        if (OwningAIController && OwningAIController.BehaviorManager)
        {
            OwningAIController.BehaviorManager.SetAllVariableValues("AttackTarget", null);
            OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bAttacking", false);
            OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bFleeing", true);
            OwningAIController.BehaviorManager.DisableAllBehaviors();
            OwningAIController.BehaviorManager.EnableBehavior(MainCustomerBT);

            if (Player.BarkComponent)
            {
                Player.BarkComponent.PlayBark(OnTargetDefeatedBark);
            }

            RequestSetHostileState(HRHostileState.Alert);
        }
        if (OwningPlayerCharacter && OwningPlayerCharacter.FearComponent)
        {
            OwningPlayerCharacter.FearComponent.SetFighting(false);
        }
    }

    private void HandleTargetPawnDisabled(BaseScripts.BasePawn pawn)
    {
        SetAttackTarget(null);
        RemoveTargetFromList(pawn.gameObject);

        if (OwningAIController && OwningAIController.BehaviorManager)
        {
            OwningAIController.BehaviorManager.SetAllVariableValues("AttackTarget", null);
            OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bAttacking", false);
            OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bFleeing", true);
            OwningAIController.BehaviorManager.DisableAllBehaviors();
            OwningAIController.BehaviorManager.EnableBehavior(MainCustomerBT);

            if (Player.BarkComponent)
            {
                Player.BarkComponent.PlayBark(OnTargetDefeatedBark);
            }

            RequestSetHostileState(HRHostileState.Alert);
        }
        if (OwningPlayerCharacter && OwningPlayerCharacter.FearComponent)
        {
            OwningPlayerCharacter.FearComponent.SetFighting(false);
        }
    }


    [Mirror.TargetRpc]
    void RestoreDefaultUI(Mirror.NetworkConnection Target)
    {
        ((HRGameInstance)BaseGameInstance.Get).PhoneManager.MainInventoryUI.SetPickButton(false);

        // Cancel discussion
        ((HRGameInstance)BaseGameInstance.Get).CustomerDecideUI.SetUIActive(false);
    }


    [Mirror.TargetRpc]
    private void CancelMinigame(Mirror.NetworkConnection Target)
    {
        // Cancel greeting
        //if (bGreetMinigameActive)
        //{
        //    GreetMinigame.MinigameComponent.OnResultDelegate -= OnGreetMinigameEnd;
        //}
    }

    void HandleFearChanged(BaseHP InHPComponent, GameObject Instigator, float PreviousHP, float NewHP, bool bFireEffects)
    {

    }

    [Mirror.Server]
    public void SetBudget_Server(float InNewWallet)
    {
        Budget = InNewWallet;
    }

    public override void HandlePoolInstantiate(BaseObjectPoolingComponent PoolingComponent)
    {
        base.HandlePoolInstantiate(PoolingComponent);

        InitializeCustomerAI();
    }

    public override void OnAfterSpawn(HRRandomEnemySpawner Spawner, ref SpawnedObjectInfo SpawnInfo)
    {
        if (!SpawnInfo.bSpawned || !SpawnInfo.bReserved)
        {
            if (MainCustomerBT)
            {
                AIController.BehaviorManager.RemoveBehaviorTree(MainCustomerBT);
                BaseObjectPoolManager.DestroyGameObject(MainCustomerBT.gameObject, 0f);
            }

            if (CustomerBT)
            {
                AIController.BehaviorManager.RemoveBehaviorTree(CustomerBT);
                BaseObjectPoolManager.DestroyGameObject(CustomerBT.gameObject.gameObject, 0f);
            }
        }

        base.OnAfterSpawn(Spawner, ref SpawnInfo);
    }

    void InitializeCustomerAI()
    {
        OwningAIController?.BehaviorManager.SetVariableValue(CustomerBT, "bIsInPlot", false);

        // Reset need HP
        CustomerNeeds.InitializeNeeds();

        // Set budget for random items.
        if (HRNetworkManager.IsHost())
        {
            float Roll = Random.Range(1.0f, MaxBudgetMultiplier);
            SetBudget_Server(Mathf.Floor(Roll * MaxBudget));
            StartingBudgetSize = Budget;
            BudgetReloads = 0;
        }

        if (CustomerNeeds)
        {
            CustomerNeeds.SetIsTicking(false);
        }

        if (Player && Player.AnimScript)
        {
            Player.AnimScript.SetFloat("Tired", 0f);
        }

        SetNeedsUIEnabled(false);

        if (RatingInfo != null)
        {
            RatingInfo.ResetRating();
        }
    }

    [Mirror.Server]
    public void IncrementBudgetReloads_Server()
    {
        BudgetReloads++;
    }

    public bool CanReloadBudget()
    {
        return BudgetReloads < 3;
    }

    public void ResetEmotion()
    {
        if (HRNetworkManager.IsHost())
        {
            if (netIdentity != null)
            {
                ResetEmotion_ClientRpc();
            }
            else
            {
                ResetEmotion_Implementation();
            }
        }
    }

    [Mirror.ClientRpc]
    void ResetEmotion_ClientRpc()
    {
        ResetEmotion_Implementation();
    }

    void ResetEmotion_Implementation()
    {
        if (EmotionComponent)
        {
            SetEmotion_Implementation(HREmotion.None, 0, false);
            EmotionComponent.SetEmotionEnabled(false);
            EmotionComponent.transform.SetParent(transform);
            EmotionComponent.transform.localScale = Vector3.one;
        }
    }

    public override void HandleReturnToPool(BaseObjectPoolingComponent PoolingComponent)
    {
        base.HandleReturnToPool(PoolingComponent);

        if (!HRNetworkManager.IsHost())
        {
            return;
        }

        SetMoveSpeed(BaseScripts.BaseMoveType.WALKING);

        ReturnShoppingBasket();

        LastTimeVisitedShop = 0;

        if (MessGenerator)
        {
            MessGenerator.ResetMessCount();
            MessGenerator.SetEnabled(true);
            MessGenerator?.SetDropDecalsEnabled(true);
        }

        bItemFound = false;
        bInShop = false;

        if (CustomerStateText)
        {
            CustomerStateText.gameObject.SetActive(false);
        }

        if (bQueued && TargetShopManager)
        {
            TargetShopManager.OnCustomerCountChange -= OnCustomersInShopChanged;
        }

        SetMoveLayer(AIMoveLayer.GENERIC);

        Initialized = false;
        bQueued = false;
        bLeavingShop = false;
        bHasAsked = false;
        bGreet = false;
        bCanInteract = true;
        CustomerNeeds.SetIsTicking(false);

        if (bBoundNeeds)
        {
            bBoundNeeds = false;
            CustomerNeeds.OnNeedUpdatedDelegate -= OnNeedsUpdated;
        }

        bUseSearchingTimer = false;
        bShowingHappinessWarning = false;

        if (HRNetworkManager.IsHost())
        {
            SetInteractableActive(true);
        }

        if (OwningPlayerCharacter.HP.CurrentHP != 0 && OwningPlayerCharacter.FearComponent && OwningPlayerCharacter.FearComponent.bIsFleeing)
        {
            if (LastTargetShopManager && LastTargetShopManager.GetCustomersInShop().Contains(this))
            {
                HRSaleInfo SaleInfo = new HRSaleInfo();
                SaleInfo.SaleType = HRSaleType.FAILED;
                int LBPoints;
                float XP;
                HRShopEntityManager.Get.AddSale(LastTargetShopManager.ShopName, SaleInfo, out LBPoints, out XP);
            }
            HRLeaderboardSystem.Get.AddDelayedLeaderboardPoints(((HRGameInstance)BaseGameInstance.Get).DistrictDB.DistrictInfos[((HRGameInstance)BaseGameInstance.Get).CurrentDistrictIndex].DistrictName, -1, this.Interactable.name + " witnessed a crime and escaped.");
        }

        /*if(OwningPlayerCharacter.HP.CurrentHP < OwningPlayerCharacter.HP.MaxHP)
        {
            OwningPlayerCharacter.HP.SetHP(OwningPlayerCharacter.HP.MaxHP, this.gameObject, true, false, true);
        }*/


        if (CurrentBubbleUI != null && CurrentBubbleUI.UIObject)
        {
            CurrentBubbleUI.UIObject.Hide();
        }

        if (EmotionComponent && EmotionComponent.FloatingUIComponent)
        {
            ResetEmotion();
            EmotionComponent.SetEmotionEnabled(false);
            EmotionComponent.FloatingUIComponent.SetVisible(false);
            //Destroy(EmotionComponent.FloatingUIComponent.gameObject);
        }
        if (HappinessSliderUI)
        {
            HappinessSliderUI.SetVisible(false);
            //Destroy(HappinessSliderUI.gameObject);
        }

        if (InteractedPlayer && bFollowing)
        {
            AssignContainerInteractDelegate(false);
        }

        if (OneshotNeedsAffectedItems != null)
        {
            OneshotNeedsAffectedItems.Clear();
        }

        if (OldTickNeedsAffectingItems != null)
        {
            OldTickNeedsAffectingItems.Clear();
        }

        if (CurrentInventory != null)
        {
            UnregisterCurrentInventory(InteractedPlayer.connectionToClient);
        }
        if (CurrentInventoryUI != null)
        {
            UnregisterCurrentInventoryUI(InteractedPlayer.connectionToClient);
        }


        if (DayManager)
        {
            DayManager.HourChangedDelegate -= HandleHourChanged;
        }

        SetWaitingOutside(false);

        if (OwningPlayerCharacter?.FearComponent)
        {
            OwningPlayerCharacter.FearComponent.SetFighting(false);
            OwningPlayerCharacter.FearComponent.SetFleeing(false);
        }

        ShoppingCart.Clear();
        ShoppingCartIDs.Clear();
        ShoppingCartQuantity.Clear();
        TargetRegister = null;
        LastTargetShopManager = null;
        LeavePlot(false, true, true, true);
        bDoneShopping = false;
        bLeavingShop = false;

        if (OwningPlayerCharacter.AnimScript)
        {
            OwningPlayerCharacter.AnimScript.SetLookAtTarget(null);
        }

        OwningPlayerCharacter.MovementComponent.SetMoveSpeed(BaseScripts.BaseMoveType.WALKING);
        OwningAIController.AIMovement.bWalkWhenNearDestination = true;

        if (CharacterPathFollower)
        {
            CharacterPathFollower.ResetFollower();
        }

        RemoveSpotInQueue();

        if (CombatComponent)
        {
            CombatComponent.RemoveTarget();
        }

        SetAttackTarget(null);
        SetHostileState(HRHostileState.Inactive);

        if (OwningAIController.AIMovement.PlayerFollow && OwningAIController.AIMovement.PlayerFollow.OwningManager)
        {
            OwningAIController.AIMovement.PlayerFollow.OwningManager.RemoveFollower(OwningAIController.AIMovement.PlayerFollow);
        }

        SetWaitingOutside(false);

        OwningAIController.BehaviorManager.SetAllVariableValues("ShopManager", null);
        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bAttacking", false);
        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "bFleeing", false);
        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "AttackTarget", null);
        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "QueueSystem", null);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "QueueSystem", null);
        OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bIdle", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bIdle", false);
        OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bFollowing", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bFollowing", false);
        OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bSearchForAlternateItem", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bSearchForAlternateItem", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bGreet", false);
        OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bSearch", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bSearch", false);
        OwningAIController.BehaviorManager.SetVariableValue(OrderBT, "bOrderPlaced", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bLeavingPlot", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bIsInPlot", false);
        OwningAIController.BehaviorManager.SetVariableValue(MainCustomerBT, "ShopPlot", null);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "ShopPlot", null);
        OwningAIController.BehaviorManager.DisableAllBehaviors(false);
        OwningAIController.SetGenericGraph();

        HideHappinessSlider();

        _OnDestroy();
    }

    #region Greet Behaviors

    public void OnCustomerEnterQueue()
    {
        bInQueue = true;
        CurrentQueueDelay = Random.Range(MinTimeToWait, MaxTimeToWait);
    }


    public void CustomerQueued(bool EndQueue = false)
    {
        if (!bQueued)
        {
            if (bWaitingOutside)
            {
                bQueued = true;

                if (TargetShopManager)
                {
                    if (TargetShopManager.AdmitCustomerIntoShop(this))
                    {

                    }
                    else
                    {
                        TargetShopManager.OnCustomerCountChange -= OnCustomersInShopChanged;
                        TargetShopManager.OnCustomerCountChange += OnCustomersInShopChanged;
                    }
                }
            }

            if (EndQueue)
            {
                bInQueue = false;
            }
        }
    }


    public void OnCustomersInShopChanged()
    {
        if (TargetShopManager && TargetShopManager.AdmitCustomerIntoShop(this))
        {
            TargetShopManager.OnCustomerCountChange -= OnCustomersInShopChanged;
        }
    }

    public void RemoveFromCurrentQueue()
    {
        bQueued = false;
        RemoveSpotInQueue();
    }

    public void BeginGreetState()
    {
        if (bShowGreetIcon && (CurrentBubbleUI == null || CurrentBubbleUI.Type != HRBubbleUIType.Greet) && !bCanOverworldGreet)
            SetBubbleUI(HRBubbleUIType.Greet, HRBubbleUIStatus.Start);

        TargetShopManager.OnCustomerGreet?.Invoke();

        SetInteractableActive(true);
        bGreet = true;
        CurrentGreetWaitTime = 0.0f;
        bUseGreetTimer = true;
        CustomerNeeds.SetNeedActive(HRENeed.Patience, false);
    }

    public void SetHappinessMeterActive(bool Active)
    {
        SetHappinessMeterActive_ClientRpc(Active);
        SetHappinessMeterActive_Implementation(Active);
    }


    [Mirror.ClientRpc]
    private void SetHappinessMeterActive_ClientRpc(bool Active)
    {
        SetHappinessMeterActive_Implementation(Active);
    }


    private void SetHappinessMeterActive_Implementation(bool Active)
    {
        if (HappinessSlider)
        {
            HappinessSlider.gameObject.SetActive(Active);
        }
    }

    public void ClearBubbleUI()
    {
        if (HRNetworkManager.IsHost())
        {
            ClearBubbleUI_ClientRpc();
        }
        else
        {
            ClearBubbleUI_Command();
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void ClearBubbleUI_Command()
    {
        ClearBubbleUI_ClientRpc();
    }

    [Mirror.ClientRpc]
    private void ClearBubbleUI_ClientRpc()
    {
        ClearBubbleUI_Implementation();
    }

    private void ClearBubbleUI_Implementation()
    {
        if (CurrentBubbleUI != null)
        {
            CurrentBubbleUI.UIObject.Hide();
            CurrentBubbleUI = null;
        }
    }

    public void ResetNoRegister()
    {
        if (CurrentBubbleUI != null && CurrentBubbleUI.Type == HRBubbleUIType.NoRegister)
        {
            SetBubbleUI(HRBubbleUIType.NoRegister, HRBubbleUIStatus.Success);
        }
    }

    public void ResetNoDisplay()
    {
        if (CurrentBubbleUI != null && CurrentBubbleUI.Type == HRBubbleUIType.NoDisplay)
        {
            SetBubbleUI(HRBubbleUIType.NoDisplay, HRBubbleUIStatus.Success);
        }
    }

    public void ResetEmptyDisplay()
    {
        if (CurrentBubbleUI != null && CurrentBubbleUI.Type == HRBubbleUIType.EmptyDisplay)
        {
            SetBubbleUI(HRBubbleUIType.EmptyDisplay, HRBubbleUIStatus.Success);
        }
    }

    public void ExpressNoDisplays()
    {
        // TODO: Change icon
        if (CurrentBubbleUI == null || CurrentBubbleUI.Type != HRBubbleUIType.NoDisplay)
        {
            SetBubbleUI(HRBubbleUIType.NoDisplay, HRBubbleUIStatus.Start);
        }
    }

    public void ExpressEmptyDisplay()
    {
        // TODO: Change icon
        if (CurrentBubbleUI == null || CurrentBubbleUI.Type != HRBubbleUIType.EmptyDisplay)
        {
            SetBubbleUI(HRBubbleUIType.EmptyDisplay, HRBubbleUIStatus.Start);
        }
    }

    public void ExpressNoRegister()
    {
        if (CurrentBubbleUI == null || CurrentBubbleUI.Type != HRBubbleUIType.NoRegister)
        {
            SetBubbleUI(HRBubbleUIType.NoRegister, HRBubbleUIStatus.Start);
        }
    }

    public void SetBubbleUI(HRBubbleUIType BubbleType, HRBubbleUIStatus Status)
    {
        if (!HRNetworkManager.IsHost())
        {
            SetBubbleUI_Command(BubbleType, Status);
        }
        else
        {
            SetCustomerBubbleUI_ClientRpc(BubbleType, Status);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void SetBubbleUI_Command(HRBubbleUIType BubbleType, HRBubbleUIStatus Status)
    {
        SetCustomerBubbleUI_ClientRpc(BubbleType, Status);
    }


    [Mirror.ClientRpc]
    private void SetCustomerBubbleUI_ClientRpc(HRBubbleUIType BubbleType, HRBubbleUIStatus Status)
    {
        SetBubbleUI_Implementation(BubbleType, Status);
    }

    private void SetBubbleUI_Implementation(HRBubbleUIType BubbleType, HRBubbleUIStatus Status)
    {
        HRBubbleUI BubbleData = GetBubbleUIData(BubbleType);
        if (BubbleData != null)
        {
            if (!BubbleData.UIObject)
            {
                return;
            }
            if (CurrentBubbleUI != null && CurrentBubbleUI.Type != BubbleData.Type)
            {
                CurrentBubbleUI.UIObject.Hide();
            }
            CurrentBubbleUI = BubbleData;
            AnimationClip anim = CurrentBubbleUI.GetClip(Status);
            switch (Status)
            {
                case HRBubbleUIStatus.Start:
                    CurrentBubbleUI.UIObject.Show();
                    if (anim)
                    {
                        if (bShowingHappinessWarning)
                        {
                            CurrentBubbleUI.UIObject.PlayAnimation(anim, ToLowIdle);
                        }
                        else
                        {
                            CurrentBubbleUI.UIObject.PlayAnimation(anim, ToBubbleIdle);
                        }
                    }
                    break;
                case HRBubbleUIStatus.LowStart:
                    CurrentBubbleUI.UIObject.Show();
                    if (anim)
                    {
                        CurrentBubbleUI.UIObject.PlayAnimation(anim, ToLowIdle);
                    }
                    break;
                case HRBubbleUIStatus.Success:
                    if (anim)
                    {
                        CurrentBubbleUI.UIObject.PlayAnimation(anim, ClearBubbleUI_Implementation);
                    }
                    break;
                case HRBubbleUIStatus.Failure:
                    if (anim)
                    {
                        CurrentBubbleUI.UIObject.PlayAnimation(anim, ClearBubbleUI_Implementation);
                    }
                    break;
            }
        }
    }

    private void ToBubbleIdle()
    {
        if (CurrentBubbleUI != null)
        {
            AnimationClip anim = CurrentBubbleUI.GetClip(HRBubbleUIStatus.Idle);
            if (anim)
            {
                CurrentBubbleUI.UIObject.PlayAnimation(anim);
            }
        }
    }

    private void ToLowIdle()
    {
        if (CurrentBubbleUI != null)
        {
            AnimationClip anim = CurrentBubbleUI.GetClip(HRBubbleUIStatus.LowIdle);
            if (anim)
            {
                CurrentBubbleUI.UIObject.PlayAnimation(anim);
            }
        }
    }

    private HRBubbleUI GetBubbleUIData(HRBubbleUIType BubbleType)
    {
        for (int i = 0; i < BubbleUIDatas.Length; ++i)
        {
            if (BubbleUIDatas[i].Type == BubbleType)
            {
                return BubbleUIDatas[i];
            }
        }
        return null;
    }

    HRShopManager ShopToPersuadeTo = null;

    private void ExecuteGreetDialogue(Mirror.NetworkConnection Target, HeroPlayerCharacter MinigamePlayer)
    {
        InteractedPlayer = MinigamePlayer;
        RegisterGreetingLua();
        //If customer is in or going to the plot, use specific dialogue if they havent already
        if (GetInPlot() && !GreetingDialogueStarter.bHasUsedSpecificDialogue && !GetAtRegister())
        {
            GreetingDialogueStarter.bUseSpecificDialogue = true;
        }
        GreetingDialogueStarter.StartGreetDialogue(MinigamePlayer.gameObject, false);


        ShopToPersuadeTo = FindShopToPersaudeTo();
    }


    public bool GetInShop()
    {
        return TargetShopManager != null;
    }

    public bool GetInPlot()
    {
        return TargetShopPlot != null;
    }

    public bool GetInPlotConfines()
    {
        if (!CustomerBT) return false;
        return (bool)CustomerBT.GetVariable("bIsInPlot").GetValue();
    }

    public bool GetAtRegister()
    {
        return TargetRegister != null;
    }

    public bool GetPersuaded()
    {
        return bPersuaded;
    }

    public bool HasClosestShopPlot()
    {
        if (this == null)
        {
            // Temp fix for survival build - Michael
            return true;
        }
        else if (((HRGameInstance)BaseGameInstance.Get)?.FindClosestShopPlot(this.transform.position) != null)
        {
            return true;
        }

        return false;
    }

    public bool ClosestShopHasSign()
    {
        HRShopPlot ShopPlotToEnter = TargetShopPlot;
        if (!ShopPlotToEnter)
        {
            // Find closest target shop plot
            ShopPlotToEnter = ((HRGameInstance)BaseGameInstance.Get)?.FindClosestShopPlot(this.transform.position);
        }

        if (ShopPlotToEnter)
        {
            HRShopManager ClosestShop = ShopPlotToEnter.GetClosestShop(this.transform.position);
            if (ClosestShop)
            {
                if (ClosestShop.GetShopWaitingLine(this) != null)
                {
                    return true;
                }
            }
        }

        return false;
    }


    public HRShopManager FindShopToPersaudeTo()
    {
        if (transform == null)
        {
            return null;
        }

        // Get closest shop
        HRShopManager ShopManagerToPersuadeTo = null;
        HRShopPlot ShopPlotToPersuadeTo = null;

        ShopPlotToPersuadeTo = ((HRGameInstance)BaseGameInstance.Get).FindClosestShopPlot(this.transform.position);

        if (ShopPlotToPersuadeTo)
        {
            ShopManagerToPersuadeTo = ShopPlotToPersuadeTo.GetClosestShop(this.transform.position);
        }

        if (!ShopManagerToPersuadeTo || (Vector3.Distance(ShopManagerToPersuadeTo.transform.position, transform.position) > MaxDistanceToShopThreshold))
        {
            return null;
        }

        return ShopManagerToPersuadeTo;
    }

    private void OnGreetDialogueEnd(int ConversationID)
    {
        bool bInMinigame = false;
        HRPlayerController HRPC = ((HRGameInstance)BaseGameInstance.Get)?.GetLocalPlayerController() as HRPlayerController;
        if (HRPC)
        {
            HeroInputComponent HRInputComponent = HRPC.GetInputComponent() as HeroInputComponent;
            if (HRInputComponent)
            {
                bInMinigame = HRInputComponent.GetIsInMinigame();
            }
        }

        if (bInMinigame)
        {
            return;
        }

        PixelCrushers.DialogueSystem.Wrappers.BaseDialogueUI.Get.OnConversationEndDelegate -= OnGreetDialogueEnd;
        FinishConversation();
    }

    void FinishConversation()
    {
        if (HRNetworkManager.IsHost())
        {
            FinishConversation_Server();
        }
        else
        {
            if (!HRNetworkManager.IsOffline())
            {
                FinishConversation_Command();
            }
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    void FinishConversation_Command()
    {
        FinishConversation_Server();
    }

    [Mirror.Server]
    public void FinishConversation_Server()
    {
        OwningAIController.AIMovement.OwningMovementComponent.FreezeMovement(false);
        OwningAIController.AIMovement.SetAIMovementEnabled(true);
        SetBehaviorTreeEnabled(true);
    }

    // This function is called whenever the player finishes interacting with the customer.
    public void HandleCustomerGreetingDialogueEnd(double Result)
    {
        int Mode = ((int)Result);
        UnregisterGreetingLua();

        switch (Mode)
        {
            case -1:
                //Positive interaction
                //SetBehaviorTreeEnabled(true);
                ModifyRating(PositiveInteractionRatingModifier);
                break;
            case 0:
                LeavePlot(false, bInShop, true);
                break;
            case 1:
                //Neutral interaction
                break;
            case 2:
                OnGreetMinigameEnd(1);
                break;
            case 3:
                OnGreetMinigameEnd(0);
                break;
            case 4:
                StartBartering();
                break;
            default:
                //SetBehaviorTreeEnabled(true);
                break;
        }
        SetBehaviorTreeEnabled(true);

        //Only use specific dialogue once?
        if (GreetingDialogueStarter && !GreetingDialogueStarter.bHasUsedSpecificDialogue)
        {
            if (GreetingDialogueStarter.bUseSpecificDialogue)
            {
                GreetingDialogueStarter.bUseSpecificDialogue = false;
                GreetingDialogueStarter.bHasUsedSpecificDialogue = true;
            }
        }
    }


    //private void OnGreetSuccessDialogueEnd(int ConversationID)
    //{
    //    PixelCrushers.DialogueSystem.Wrappers.BaseDialogueUI.Get.OnConversationEndDelegate -= OnGreetSuccessDialogueEnd;

    //    OnGreetMinigameEnd(1);
    //}


    //private void OnGreetFailDialogueEnd(int ConversationID)
    //{
    //    PixelCrushers.DialogueSystem.Wrappers.BaseDialogueUI.Get.OnConversationEndDelegate -= OnGreetFailDialogueEnd;

    //    OnGreetMinigameEnd(0);
    //}

    /*
    [Mirror.TargetRpc]
    private void ExecuteGreetMinigame_TargetRpc(Mirror.NetworkConnection Target, HeroPlayerCharacter MinigamePlayer)
    {
        ExecuteGreetMinigame(MinigamePlayer);
    }
    private void ExecuteGreetMinigame(HeroPlayerCharacter MinigamePlayer)
    {
        bGreetMinigameActive = true;
        bUseGreetTimer = false;
        GreetMinigame.MinigameComponent.OnResultDelegate -= OnGreetMinigameEnd;
        GreetMinigame.MinigameComponent.OnResultDelegate += OnGreetMinigameEnd;
        GreetMinigame.MinigameComponent.BindToInputComponent((HeroInputComponent)MinigamePlayer.InputComponent);
        GreetMinigame.PreStartMinigame(MinigamePlayer, true, BaseGreetMinigameDifficulty);
    }
    */

    public void StartBartering()
    {
        if (!TargetRegister) return;
        BaseScripts.BaseInteractionManager InteractionManager = ((HRGameInstance)BaseGameInstance.Get).GetLocalHeroPlayerCharacter()?.InteractionManager;
        if (!InteractionManager) return;
        TargetRegister.StartBartering(InteractionManager);
    }

    public bool RollGreet()
    {
        float probability = EvaluateGreetProbability();
        float roll = Random.value;
        return roll <= probability;
    }

    public float EvaluateGreetProbability()
    {
        if (!ShopToPersuadeTo) return 0f;
        int shopkeepingLevel = GetShopkeepingLevel();
        float probability = GreetProbabilityCurve.Evaluate(GetPersuasionPercent());
        Debug.Log($"Persuade - Shopkeeping Level: {shopkeepingLevel} Persuasion Lvl: {CustomerPersuasionLevel} Percent: {GetPersuasionPercent()} Probability: {probability}");
        return probability;
    }

    public float GetPersuasionPercent()
    {
        int shopkeepingLevel = GetShopkeepingLevel();
        int difference = Mathf.Clamp(CustomerPersuasionLevel - shopkeepingLevel, 0, MaxShopkeepingLevel);
        float percent = (float)difference / MaxShopkeepingLevel;
        return 1 - percent;
    }

    public int GetShopkeepingLevel()
    {
        return (int)((HRGameInstance)BaseGameInstance.Get).GetLocalHeroPlayerCharacter()?.SkillSystem.GetSkillLevel(HRSkillSystem.EPlayerSkill.Shopkeeping);
    }

    public int GetGreetProbabilityAsPercentage()
    {
        float greetProbability = EvaluateGreetProbability();
        return (int)(greetProbability * 100f);
    }

    [Mirror.Server]
    void OnGreetMinigameEnd_Server(int Result)
    {
        bPersuaded = true;
        bUseGreetTimer = false;
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bGreet", false);

        if (Result > 0)
        {
            SetBehaviorTreeEnabled(false);
        }

        if (Result == 1)
        {
            //Greet succeeded

            if (true)//bCanOverworldGreet)
            {
                //EnterShop(ShopToPersuadeTo);

                if (!TargetShopPlot)
                {
                    SetTargetShopPlot(((HRGameInstance)BaseGameInstance.Get)?.FindClosestShopPlot(this.transform.position));
                }
                else
                {
                    if (ShopToPersuadeTo)
                    {
                        WaitOutsideShop(ShopToPersuadeTo);
                    }
                }

                bCanOverworldGreet = false;
                CustomerNeeds.SetNeedActive(HRENeed.Patience, true);


                if (OwningPlayerCharacter && OwningPlayerCharacter.PlayerMesh && OffscreenSpawner)
                {
                    OwningPlayerCharacter.PlayerMesh.OnVisibilityChangedDelegate -= OffscreenSpawner.HandleCustomerVisibilityChanged;
                }
            }

            if (CustomerStates.Contains(SpecialCustomerState.Greet))
            {
                CustomerStates.Remove(SpecialCustomerState.Greet);
            }

            bCanInteract = true;
            bCanOverworldGreet = false;
            SetInteractableActive(true);

            SetEmotion(HREmotion.Happy, 3.0f);
            OwningPlayerCharacter.CharacterVoice.PlayRandomAudioFromGroup("Greeting");
            //Add shopkeeping XP based on the customer persuasion level? (value from 1 - 100)
            float GreetShopkeepingXP = GreetShopkeepingXPCurve.Evaluate(GetPersuasionPercent());
            Debug.Log("Adding Shopkeeping XP: " + GreetShopkeepingXP);
            OwningPlayerCharacter.AddXPForShopkeeping(GreetShopkeepingXP);
            SetBubbleUI(HRBubbleUIType.Greet, HRBubbleUIStatus.Success);
            SetBehaviorTreeEnabled(true);
        }
        else
        {
            if (Result == 0)
            {
                HideHappinessSlider();

                if (TargetShopManager)
                {
                    LeaveShop(true);
                    SetEmotion(HREmotion.Angry, 3.0f);
                    OwningPlayerCharacter.CharacterVoice.PlayRandomAudioFromGroup("Angry");
                }
                else if (TargetShopPlot)
                {
                    LeavePlot(false);
                }
                else
                {
                    SetBehaviorTreeEnabled(true);
                    OwningAIController?.AIMovement?.SetAIMovementEnabled(true);
                }
            }
            else if (Result < 0)
            {
                SetBehaviorTreeEnabled(true);
                OwningAIController?.AIMovement?.SetAIMovementEnabled(true);
            }

            CurrentGreetWaitTime = 0;
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    void OnGreetingMinigameEnd_Command(int Result)
    {
        OnGreetMinigameEnd_Server(Result);
    }

    private void OnGreetMinigameEnd(int Result)
    {
        if (HRNetworkManager.IsHost())
        {
            OnGreetMinigameEnd_Server(Result);
        }
        else
        {
            if (!HRNetworkManager.IsOffline())
            {
                OnGreetingMinigameEnd_Command(Result);
            }
        }
    }

    void SetLikedCategoriesVar()
    {
        //select a random liked catgeory
        int LikedIndex = Random.Range(0, CustomerDemandDB.LikedCategories.Count);
        string LikedName = "";
        if (LikedIndex >= 0 && LikedIndex < CustomerDemandDB.LikedCategories.Count - 1)
        {
            HRItemCategorySO Category = CustomerDemandDB.LikedCategories[LikedIndex];
            if (Category != null)
            {
                LikedName = Category.CategoryName;
            }
        }
        DialogueLua.SetVariable("CategoryPreference", LikedName);
    }

    private void RegisterGreetingLua()
    {
        Lua.RegisterFunction("GetPersuaded", this, SymbolExtensions.GetMethodInfo(() => GetPersuaded()));
        Lua.RegisterFunction("GetInShop", this, SymbolExtensions.GetMethodInfo(() => GetInShop()));
        Lua.RegisterFunction("GetInPlot", this, SymbolExtensions.GetMethodInfo(() => GetInPlot()));
        Lua.RegisterFunction(nameof(GetInPlotConfines), this, SymbolExtensions.GetMethodInfo(() => GetInPlotConfines()));
        Lua.RegisterFunction(nameof(GetAtRegister), this, SymbolExtensions.GetMethodInfo(() => GetAtRegister()));
        Lua.RegisterFunction("HasClosestShopPlot", this, SymbolExtensions.GetMethodInfo(() => HasClosestShopPlot()));
        Lua.RegisterFunction(nameof(StartBartering), this, SymbolExtensions.GetMethodInfo(() => StartBartering()));
        Lua.RegisterFunction("HandleCustomerGreetingDialogueEnd", this, SymbolExtensions.GetMethodInfo(() => HandleCustomerGreetingDialogueEnd((double)0)));
        Lua.RegisterFunction("ClosestShopHasSign", this, SymbolExtensions.GetMethodInfo(() => ClosestShopHasSign()));
        Lua.RegisterFunction(nameof(RollGreet), this, SymbolExtensions.GetMethodInfo(() => RollGreet()));
        Lua.RegisterFunction(nameof(GetGreetProbabilityAsPercentage), this, SymbolExtensions.GetMethodInfo(() => GetGreetProbabilityAsPercentage()));
        Lua.RegisterFunction(nameof(GetShopkeepingLevel), this, SymbolExtensions.GetMethodInfo(() => GetShopkeepingLevel()));
        Lua.RegisterFunction(nameof(IsCustomerDoneShopping), this, SymbolExtensions.GetMethodInfo(() => IsCustomerDoneShopping()));
        Lua.RegisterFunction(nameof(SetLikedCategoriesVar), this, SymbolExtensions.GetMethodInfo(() => SetLikedCategoriesVar()));

        RegisterDialogueLua();
    }

    private void UnregisterGreetingLua()
    {
        Lua.UnregisterFunction("GetPersuaded");
        Lua.UnregisterFunction("GetInShop");
        Lua.UnregisterFunction("GetInPlot");
        Lua.UnregisterFunction(nameof(GetInPlotConfines));
        Lua.UnregisterFunction(nameof(GetAtRegister));
        Lua.UnregisterFunction(nameof(StartBartering));
        Lua.UnregisterFunction("HasClosestShopPlot");
        Lua.UnregisterFunction("HandleCustomerGreetingDialogueEnd");
        Lua.UnregisterFunction("ClosestShopHasSign");
        Lua.UnregisterFunction(nameof(RollGreet));
        Lua.UnregisterFunction(nameof(GetGreetProbabilityAsPercentage));
        Lua.UnregisterFunction(nameof(GetShopkeepingLevel));
        Lua.UnregisterFunction(nameof(IsCustomerDoneShopping));
        Lua.UnregisterFunction(nameof(SetLikedCategoriesVar));

        DeregisterDialogueLua();
    }

    #endregion


    #region Search Behaviors


    [Mirror.TargetRpc]
    private void ExecuteSearchDialogue(Mirror.NetworkConnection Target, HeroPlayerCharacter MinigamePlayer, string shoppingTarget)
    {
        CurrentWantedCategoryID = shoppingTarget;
        RegisterSearchingLua();
        SearchingDialogueStarter.StartSearchDialogue(MinigamePlayer.gameObject, false);
    }

    private void StartSearchWeaponPickedDialogue(HRPlayerController picker, string result)
    {
        ExecuteSearchWeaponPickedDialogue(picker.PlayerPawn as HeroPlayerCharacter);
    }

    private void ExecuteSearchWeaponPickedDialogue(HeroPlayerCharacter giver)
    {
        RegisterSearchingLua();
        SearchingDialogueStarter.StartWeaponPickedDialogue(giver.gameObject, false);
        if (PixelCrushers.DialogueSystem.DialogueManager.isConversationActive)
        {
            HRDialogueSystem.Get.OnConversationEndedDelegate += HandleConversationFinished;
        }
    }

    private void HandleConversationFinished(HRDialogueSystem dialogueSystem, int conversationID)
    {
        if (HRNetworkManager.IsHost())
        {
            ConversationFinished_Implementation(conversationID);
        }
        else
        {
            ConversationFinished_Command(conversationID);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void ConversationFinished_Command(int conversationID)
    {
        ConversationFinished_Implementation(conversationID);
    }

    private void ConversationFinished_Implementation(int conversationID)
    {
        switch (searchWeaponPickedResult)
        {
            case HRSearchingDialogueStarter.ITEM_FOUND:
                StopSearching();
                LeaveShop(false, false);
                break;
            case HRSearchingDialogueStarter.NO_ITEM:
                HandleCustomerSearchingDialogueEnd(HRSearchingDialogueStarter.FAILURE_MODE);
                break;
        }
    }

    public void StartSearching()
    {
        SetSearching(true);
        CustomerStates.Remove(SpecialCustomerState.Search);
        TargetShopManager.OnCustomerAskForHelp?.Invoke();
    }

    public void StopSearching()
    {
        OwningAIController?.AIMovement?.SetAIMovementEnabled(true);
        if (TargetShopPlot)
        {
            CustomerBT?.EnableBehaviorTree();
        }
        else
        {
            RetailBT?.EnableBehaviorTree();
        }
        OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bIdle", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bIdle", false);

        OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bFollowing", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bIdle", false);

        OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bSearch", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bSearch", false);

        CustomerNeeds.SetNeedActiveAll(true);

        SetSearching(false);
        bFollowing = false;
    }

    public void SetSearching(bool bSearch)
    {
        if (bSearching != bSearch)
        {
            bSearching = bSearch;
            bHasAsked = false;

            if (bSearch)
            {
                if (bShowSearchIcon)
                    SetBubbleUI(HRBubbleUIType.Help, HRBubbleUIStatus.Start);
            }
            else
            {
                ClearBubbleUI();
            }
        }
    }


    // This function is called whenever the player finishes interacting with the customer.
    public void HandleCustomerSearchingDialogueEnd(string Result)
    {
        if (HRNetworkManager.IsHost())
        {
            HandleCustomerSearchingDialogueEnd_Implementation(BaseGameInstance.Get.GetLocalPlayerController() as HRPlayerController, Result);
        }
        else
        {
            HandleSearchingDialogueEnd_Command(BaseGameInstance.Get.GetLocalPlayerController() as HRPlayerController, Result);
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    public void HandleSearchingDialogueEnd_Command(HRPlayerController speaker, string Result)
    {
        HandleCustomerSearchingDialogueEnd_Implementation(speaker, Result);
    }

    public void HandleCustomerSearchingDialogueEnd_Implementation(HRPlayerController speaker, string Result)
    {
        //TW: commented out below cause sometimes it would be true :/
        //if (this == null)
        //{
        //    return;
        //}

        //Debug.Log($"{this.gameObject.name} is ENTERING MODE {Result}");
        switch (Result)
        {
            case HRSearchingDialogueStarter.CANCEL_MODE:
                if (RatingInfo != null)
                {
                    RatingInfo.OnCustomerRequestNotFound();
                }
                searchWeaponPickedResult = HRSearchingDialogueStarter.NO_ITEM;
                AfterWeaponPicked_TargetRpc(speaker.connectionToClient, searchWeaponPickedResult, 0 , 0);
                LeaveShop(true, true);  // THEY SHOULD LEAVE
                break;
            case HRSearchingDialogueStarter.FOLLOW_MODE:
                if (TargetShopPlot)
                {
                    CustomerBT.EnableBehaviorTree();
                }
                else
                {
                    RetailBT.EnableBehaviorTree();
                }
                OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bIdle", false);
                OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bIdle", false);

                OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bFollowing", true);
                OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bFollowing", true);

                ShowHappinessSlider();
                CustomerNeeds.SetNeedActive(HRENeed.Patience, true);
                bShouldShowHappiness = true;
                bHasAsked = false;
                bFollowing = true;

                AssignContainerInteractDelegate(true);
                break;

            case HRSearchingDialogueStarter.INVENTORY_MODE:
                if (this != null)
                {
                    WaitForPlayerItemMode_TargetRPC(speaker.connectionToClient);
                }
                break;
            case HRSearchingDialogueStarter.WAIT_MODE:
                OwningAIController?.AIMovement?.SetAIMovementEnabled(true);
                CustomerNeeds.SetNeedActiveAll(true);
                break;
            case HRSearchingDialogueStarter.FAILURE_MODE:
                SetEmotion(HREmotion.Angry, 3.0f);
                PlayRandomAudioFromGroup_ClientRpc("Angry");
                CustomerNeeds.ModifyNeedValue(HRENeed.Patience, -GetMaxPatience() * SearchingPatienceFailurePercent);
                OwningAIController?.AIMovement?.SetAIMovementEnabled(true);
                StopSearching();
                LeaveShop(true, true);
                break;
        }

        if (!bFollowing)
        {
            if (InteractedPlayer != null)
            {
                AssignContainerInteractDelegate(false);
            }
        }

        bDialogueIsOpen = false;

        if (InteractedPlayer != null)
        {
            UnregisterCurrentInventory(InteractedPlayer.connectionToClient);
            UnregisterCurrentInventoryUI(InteractedPlayer.connectionToClient);
            UnregisterSearchingLua(InteractedPlayer.connectionToClient);
        }
    }

    [Mirror.ClientRpc]
    private void PlayRandomAudioFromGroup_ClientRpc(string audioName)
    {
        PlayRandomAudioFromGroup_Implementation(audioName);
    }

    private void PlayRandomAudioFromGroup_Implementation(string audioName)
    {
        OwningPlayerCharacter?.CharacterVoice?.PlayRandomAudioFromGroup(audioName);
    }

    private void AssignContainerInteractDelegate(bool Assign)
    {
        if (InteractedPlayer != null && InteractedPlayer.connectionToClient != null)
        {
            AssignContainerInteractDelegate_TargetRpc(InteractedPlayer.connectionToClient, InteractedPlayer, Assign);
        }
    }


    [Mirror.TargetRpc]
    private void AssignContainerInteractDelegate_TargetRpc(Mirror.NetworkConnection Target, HeroPlayerCharacter Player, bool Assign)
    {
        bFollowing = Assign;

        if (Assign)
        {
            Player.InteractionManager.OnTapInteractionDelegate += OnContainerInteract;
        }
        else
        {
            Player.InteractionManager.OnTapInteractionDelegate -= OnContainerInteract;
        }
    }

    [Mirror.TargetRpc]
    private void WaitForPlayerItemMode_TargetRPC(Mirror.NetworkConnection Target)
    {
        StartCoroutine(WaitForPlayerItemModeRoutine());
    }

    private IEnumerator WaitForPlayerItemModeRoutine()
    {
        // Wait until the Dialogue Manager is active
        yield return new WaitUntil(() => !DialogueManager.isConversationActive);
        yield return null;

        OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bFollowing", false);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bFollowing", false);

        var PlayerController = ((HRPlayerController)BaseGameInstance.Get.GetFirstPawn().PlayerController);
        var PlayerUI = PlayerController.PlayerUI;

        // Must be manually called because of animation issues
        PlayerUI.RequestCutsceneMode(false, this);

        // Open player inventory
        HRPhoneManager PhoneManager = ((HRGameInstance)BaseGameInstance.Get).PhoneManager;

        if (PhoneManager)
        {
            PhoneManager.ShowPhone(0);
        }

        //TODO: not sure how this is meant to be used, but setting it to false caused some issues
        PlayerWaitBegin(true);

        // Listen for OnWeaponChanged on the player Inventory
        BindInventorySearchEvents(false);
        BindInventorySearchEvents(true);
    }

    void BindInventorySearchEvents(bool bBind)
    {
        HRPhoneManager PhoneManager = ((HRGameInstance)BaseGameInstance.Get).PhoneManager;
        if (bBind)
        {
            PhoneManager.MainInventoryUI.OwningInventory.OnItemPickedForCustomerDelegate += OnWeaponPicked;
            PhoneManager.HotkeyInventoryUI.OwningInventory.OnItemPickedForCustomerDelegate += OnWeaponPicked;
            PhoneManager.OnShowDelegate += OnInventorySearchCancelled;
        }
        else
        {
            PhoneManager.MainInventoryUI.OwningInventory.OnItemPickedForCustomerDelegate -= OnWeaponPicked;
            PhoneManager.HotkeyInventoryUI.OwningInventory.OnItemPickedForCustomerDelegate -= OnWeaponPicked;
            PhoneManager.OnShowDelegate -= OnInventorySearchCancelled;
        }
        PhoneManager.MainInventoryUI.SetPickButton(bBind);
        PhoneManager.HotkeyInventoryUI.SetPickButton(bBind);
    }

    private void OnInventorySearchCancelled(HRPhoneManager PhoneManager, bool bShown)
    {
        if (bShown) return;
        BindInventorySearchEvents(false);
    }

    public void PlayerWaitBegin(bool Waiting)
    {
        if (HRNetworkManager.IsHost())
        {
            PlayerWaitBegin_Implementation(Waiting);
        }
        else
        {
            PlayerWaitBegin_Command(Waiting);
        }
    }


    public void PlayerWaitBegin_Implementation(bool Waiting)
    {
        OwningAIController.BehaviorManager.SetVariableValue(RetailBT, "bIdle", Waiting);
        OwningAIController.BehaviorManager.SetVariableValue(CustomerBT, "bIdle", Waiting);

        if (Waiting)
        {
            bFollowing = false;
            bGiving = true;
        }
        else
        {
            // Player has abandoned customer
            if (bSearching)
            {
                CustomerNeeds.ModifyNeedValue(HRENeed.Patience, -GetMaxPatience() * SearchingPatienceFailurePercent);
                StopSearching();

                SetEmotion(HREmotion.Angry, 3.0f);
                OwningPlayerCharacter.CharacterVoice.PlayRandomAudioFromGroup("Angry");
            }

            bGiving = false;
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void PlayerWaitBegin_Command(bool Waiting)
    {
        PlayerWaitBegin_Implementation(Waiting);
    }

    public void OnContainerInteract(BaseScripts.BaseInteractable InInteractable)
    {
        if (!bFollowing) return;

        // Make sure the player is interacting with a display in this shop
        var Inventory = InInteractable.GetComponentInParent<BaseInventory>();

        if (Inventory)
        {
            var ShopInventoryObject = CheckIfShopInventory(Inventory);

            if (bFollowing && ShopInventoryObject != null)
            {
                if (CurrentInventory != null)
                {
                    // Degregister old callbacks
                    UnregisterCurrentInventory(InteractedPlayer.connectionToClient);
                }

                CurrentInventory = Inventory;
                CurrentInventoryUI = Inventory.InventoryUI;

                CurrentInventory.OnItemPickedForCustomerDelegate += OnWeaponPicked;

                CurrentInventoryUI.SetPickButton(true);
            }
        }
    }


    [Mirror.TargetRpc]
    private void UnregisterCurrentInventory(Mirror.NetworkConnection Target)
    {
        if (CurrentInventory == null) return;

        CurrentInventory.OnItemPickedForCustomerDelegate -= OnWeaponPicked;
        CurrentInventory = null;
    }


    [Mirror.TargetRpc]
    private void UnregisterCurrentInventoryUI(Mirror.NetworkConnection Target)
    {
        if (CurrentInventoryUI == null) return;

        CurrentInventoryUI.SetPickButton(false);
        CurrentInventoryUI = null;
    }

    private void OnWeaponPicked(BaseWeapon Weapon)
    {
        BindInventorySearchEvents(false);
        searchWeaponPicked = Weapon;
        if (Weapon && Weapon.OwningWeaponManager && Weapon.OwningWeaponManager.OwningPlayerCharacter)
        {
            if (HRNetworkManager.IsHost())
            {
                OnWeaponPicked_Implementation(Weapon.OwningWeaponManager.OwningPlayerCharacter as HeroPlayerCharacter, Weapon);
            }
            else
            {
                OnWeaponPicked_Command(Weapon.OwningWeaponManager.OwningPlayerCharacter as HeroPlayerCharacter, Weapon);
            }
        }

        HRPhoneManager PhoneManager = ((HRGameInstance)BaseGameInstance.Get).PhoneManager;
        PhoneManager.HidePhone();
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void OnWeaponPicked_Command(HeroPlayerCharacter seller, BaseWeapon Weapon)
    {
        OnWeaponPicked_Implementation(seller, Weapon);
    }

    private void OnWeaponPicked_Implementation(HeroPlayerCharacter seller, BaseWeapon Weapon)
    {
        if (this == null)
        {
            return;
        }
        if (!Weapon) return;
        if (!bGiving) return;

        BaseWeaponManager weaponSeller = Weapon.OwningWeaponManager;
        if (WillTakeItem(Weapon))
        {
            //Set our barter system variables
            ((HRGameInstance)HRGameInstance.Get).BarteringSystem.InitializeBarter(BarterDialogueStarter, Weapon, TargetShopManager);
            //Get price using barter system
            float WeaponPrice = ((HRGameInstance)HRGameInstance.Get).BarteringSystem.GetSuggestivePrice() * ItemFoundPriceModifier;
            SearchWeaponPrice = ((HRGameInstance)HRGameInstance.Get).BarteringSystem.GetSuggestivePrice();
            SearchWeaponBonusPrice = WeaponPrice - SearchWeaponPrice;

            CustomerNeeds.ModifyNeedValue(HRENeed.Patience, GetMaxPatience() * SearchingPatienceSuccessPercent);
            HRSaleInfo saleInfo = new HRSaleInfo(TargetShopManager, HRSaleType.EXCELLENT, Weapon, 1, WeaponPrice, InSalePercent: 1, gameObject, seller?.gameObject, false);

            if (TargetShopManager)
            {
                TargetShopManager.ItemSold(saleInfo, 1, this.transform);
            }

            PlayRandomAudioFromGroup_ClientRpc("Happy");
            if (RatingInfo != null)
            {
                RatingInfo.OnCustomerRequestFound();
            }
            searchWeaponPickedResult = HRSearchingDialogueStarter.ITEM_FOUND;
        }
        else // TODO 304 - Right now if they don't like the item, they will not take it and end. In the future they should probably let the player know this isn't exactly what they were looking for.
        {
            if (RatingInfo != null)
            {
                RatingInfo.OnCustomerRequestNotFound();
            }
            searchWeaponPickedResult = HRSearchingDialogueStarter.NO_ITEM;
        }

        if (weaponSeller && weaponSeller.OwningPlayerCharacter && weaponSeller.OwningPlayerCharacter.connectionToClient != null)
        {
            AfterWeaponPicked_TargetRpc(weaponSeller.OwningPlayerCharacter.connectionToClient, searchWeaponPickedResult, SearchWeaponPrice, SearchWeaponBonusPrice);
        }
    }

    [Mirror.TargetRpc]
    private void AfterWeaponPicked_TargetRpc(Mirror.NetworkConnection target, string result, float InSearchWeaponPrice, float InSearchWeaponBonusPrice)
    {
        searchWeaponPickedResult = result;
        SearchWeaponPrice = InSearchWeaponPrice;
        SearchWeaponBonusPrice = InSearchWeaponBonusPrice;
        StartSearchWeaponPickedDialogue(BaseGameInstance.Get.GetLocalPlayerController() as HRPlayerController, searchWeaponPickedResult);
    }


    public bool WillTakeItem(BaseWeapon Weapon, bool HandleInventoryOperation = true)
    {
        if (bAcceptAnything || Weapon.ItemSizeAndType.GetItemCategories().Contains(CurrentWantedCategory))
        {
            //Return if our requester only accepts rare items
            if (TargetShopManager && TargetShopManager.ShopPoliciesManager.GetRareRequestNodeActive() && !TargetShopManager.ShopPoliciesManager.CheckRareRequestConditional(Weapon, BarterDialogueStarter))
            {
                return false;
            }

            if (HandleInventoryOperation)
            {
                BaseWeapon removedWeapon = null;
                if (Weapon.OwningWeaponManager != null)
                {
                    removedWeapon = Weapon.OwningWeaponManager.RemoveWeapon(Weapon, true, 1, false);
                }

                OwningPlayerCharacter.WeaponManager.AddWeapon(removedWeapon);
                Weapon.OwningInteractable.enabled = false;
            }

            return true;
        }

        return false;
    }

    private HRRoomItem CheckIfShopInventory(BaseInventory Inventory)
    {
        foreach (var Item in Inventory.InventorySlots)
        {
            if (Item.SlotWeapon != null)
            {
                // Check if the item is in the shop
                if (TargetShopManager != null && TargetShopManager.ShopRooms != null)
                {
                    foreach (var Room in TargetShopManager.ShopRooms)
                    {
                        foreach (var RoomItem in Room.RoomItemArrayList)
                        {
                            foreach (var ItemInst in RoomItem.AllItemsList)
                            {
                                if (ItemInst.OwningWeapon && ItemInst.OwningWeapon.gameObject == Item.SlotWeapon.OwningInventory.gameObject)
                                    return ItemInst;
                            }
                        }
                    }
                }

            }
        }

        return null;
    }


    public string GetCurrentWantedCategoryName()
    {
        return ((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemDescriptorDB.GetCategoryNameFromID(CurrentWantedCategoryID);
    }

    private string GetCurrentSearchWeaponPrice()
    {
        return ((int)SearchWeaponPrice).ToString();
    }

    private string GetCurrentSearchWeaponBonusPrice()
    {
        return ((int)SearchWeaponBonusPrice).ToString();
    }


    public string GetCurrentSearchWeaponResult()
    {
        return searchWeaponPickedResult;
    }

    public string GetCurrentSearchWeaponCategories()
    {
        string result = "";
        if (searchWeaponPicked)
        {
            for (int i = 0; i < searchWeaponPicked.ItemSizeAndType.GetItemCategories().Count; ++i)
            {
                result += searchWeaponPicked.ItemSizeAndType.GetItemCategories()[i].CategoryName;
                if (i < searchWeaponPicked.ItemSizeAndType.GetItemCategories().Count - 1)
                {
                    result += ", ";
                }
            }
        }

        return result;
    }

    private void UpdateWantedCategory()
    {
        if (TargetShopManager && TargetShopManager.ShopPoliciesManager.GetSpecializedRequestNodeActive())
        {
            int Index = Random.Range(0, TargetShopManager.ShopSpecialtiesManager.SpecialtyCategories.Count);
            string CategoryID = TargetShopManager.ShopSpecialtiesManager.SpecialtyCategories[Index];
            //If our selected category is none, stop searching
            CurrentWantedCategory = ((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemDescriptorDB.GetCategoryFromID(CategoryID);
            if (CurrentWantedCategory && !((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemDescriptorDB.IsNoneCategory(CurrentWantedCategory))
            {
                CurrentWantedCategoryID = CurrentWantedCategory.CategoryID;
                return;
            }
        }
        // Use the demand DB if any data exists
        else if (CustomerDemandDB.LikedCategories.Count > 0)
        {
            int Index = Random.Range(0, CustomerDemandDB.LikedCategories.Count);
            CurrentWantedCategory = CustomerDemandDB.LikedCategories[Index];
            CurrentWantedCategoryID = CurrentWantedCategory ? CustomerDemandDB.LikedCategories[Index].CategoryID : "";
            return;
        }
        //If we fail to find a valid category, stop searching
        StopSearching();
    }

    bool GetRareRequestNodeActive()
    {
        if (!TargetShopManager) return false;
        if (!TargetShopManager.ShopPoliciesManager) return false;
        return TargetShopManager.ShopPoliciesManager.GetRareRequestNodeActive();
    }


    private void RegisterSearchingLua()
    {
        Lua.RegisterFunction("GetCurrentWantedCategory", this, SymbolExtensions.GetMethodInfo(() => GetCurrentWantedCategoryName()));
        Lua.RegisterFunction("HandleCustomerSearchingDialogueEnd", this, SymbolExtensions.GetMethodInfo(() => HandleCustomerSearchingDialogueEnd(string.Empty)));
        Lua.RegisterFunction("GetCurrentSearchWeaponResult", this, SymbolExtensions.GetMethodInfo(() => GetCurrentSearchWeaponResult()));
        Lua.RegisterFunction("GetCurrentSearchWeaponDescriptors", this, SymbolExtensions.GetMethodInfo(() => GetCurrentSearchWeaponCategories()));
        Lua.RegisterFunction(nameof(GetCurrentSearchWeaponPrice), this, SymbolExtensions.GetMethodInfo(() => GetCurrentSearchWeaponPrice()));
        Lua.RegisterFunction(nameof(GetCurrentSearchWeaponBonusPrice), this, SymbolExtensions.GetMethodInfo(() => GetCurrentSearchWeaponBonusPrice()));
        Lua.RegisterFunction(nameof(GetRareRequestNodeActive), this, SymbolExtensions.GetMethodInfo(() => GetRareRequestNodeActive()));
        RegisterDialogueLua();
    }

    [Mirror.TargetRpc]
    private void UnregisterSearchingLua(Mirror.NetworkConnection Target)
    {
        Lua.UnregisterFunction("GetCurrentWantedCategory");
        Lua.UnregisterFunction("HandleCustomerSearchingDialogueEnd");
        Lua.UnregisterFunction("GetCurrentSearchWeaponResult");
        Lua.UnregisterFunction("GetCurrentSearchWeaponDescriptors");
        Lua.UnregisterFunction(nameof(GetCurrentSearchWeaponPrice));
        Lua.UnregisterFunction(nameof(GetCurrentSearchWeaponBonusPrice));
        Lua.UnregisterFunction(nameof(GetRareRequestNodeActive));
        DeregisterDialogueLua();
    }


    #endregion

    public List<BaseWeapon> GetShoppingCartAsList()
    {
        var result = new List<BaseWeapon>();

        foreach (var Weapon in ShoppingCart)
        {
            if (Weapon != null)
            {
                result.Add(Weapon);
            }
        }

        return result;
    }


    #region Rating Functions

    private HRShopRatingRuntime RatingInfo;
    public HRShopRatingRuntime GetRatingInfo() { return RatingInfo; }

    [Header("RATING POLISH")]
    public HRShopRatingEffectsDB RatingEffects;


    public void CalculateSaleEndRating(HRSaleInfo InSaleInfo)
    {
        RatingInfo.CalculateShopPurchaseRating(InSaleInfo);
    }


    public void GetCategoryOverlap(BaseWeapon InWeapon, List<string> Neutral, List<string> Liked, List<string> Disliked)
    {
        var WeaponCategories = InWeapon.ItemSizeAndType.GetItemCategories();

        foreach (var category in WeaponCategories)
        {
            if (category != null)
            {
                Neutral.Add(category.CategoryID);
            }
        }

        var likedCategories = CustomerDemandDB.LikedCategories;
        var dislikedCategories = CustomerDemandDB.DislikedCategories;


        foreach (var category in likedCategories)
        {
            if (Neutral.Contains(category.CategoryID))
            {
                Liked.Add(category.CategoryID);
                Neutral.Remove(category.CategoryID);
            }
        }

        foreach (var category in dislikedCategories)
        {
            if (Neutral.Contains(category.CategoryID))
            {
                Disliked.Add(category.CategoryID);
                Neutral.Remove(category.CategoryID);
            }
        }
    }


    public void ModifyRating(float Amount)
    {
        if (RatingInfo != null)
        {
            RatingInfo.ModifyRatingConversation(Amount);
        }
    }

    public void OnCustomerKickedOut(float OverrideAmount = -1)
    {
        if (RatingInfo != null)
        {
            RatingInfo.OnCustomerKickedOut(this, OverrideAmount);
        }
    }

    #endregion


    #region Lua functions

    public void RegisterDialogueLua()
    {
        //Debug.LogError("REGISTERING WITH LUA");
        Lua.RegisterFunction("IsCustomerType", this, SymbolExtensions.GetMethodInfo(() => IsCustomerType(string.Empty)));
        Lua.RegisterFunction("HasHeldItemDescriptor", this, SymbolExtensions.GetMethodInfo(() => HasHeldItemCategory(string.Empty)));
        Lua.RegisterFunction("HasDesiredDescriptor", this, SymbolExtensions.GetMethodInfo(() => IsSearchCategory(string.Empty)));
        Lua.RegisterFunction("GetCurrentHeldItemName", this, SymbolExtensions.GetMethodInfo(() => GetCurrentHeldItemName()));
    }


    public void DeregisterDialogueLua()
    {
        //Debug.LogError("deREGISTERING WITH LUA");
        Lua.UnregisterFunction("IsCustomerType");
        Lua.UnregisterFunction("HasHeldItemDescriptor");
        Lua.UnregisterFunction("HasDesiredDescriptor");
        Lua.UnregisterFunction("GetCurrentHeldItemName");
    }


    public bool IsCustomerType(string Customer)
    {
        return Interactable.InteractionName == Customer || Interactable.InteractionName.Contains(Customer);
    }


    public bool HasHeldItemCategory(string CategoryID)
    {
        if (!OwningPlayerCharacter.WeaponManager || !OwningPlayerCharacter.WeaponManager.CurrentWeapon)
        {
            return false;
        }
        else
        {
            var weapon = OwningPlayerCharacter.WeaponManager.CurrentWeapon;

            if (weapon.ItemSizeAndType.GetItemCategories().Find(category => category?.CategoryID == CategoryID))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }


    public bool IsSearchCategory(string CategoryID)
    {
        if (!CurrentWantedCategory) return false;
        return CurrentWantedCategory.CategoryID == CategoryID;
    }


    public string GetCurrentHeldItemName()
    {
        if (!OwningPlayerCharacter.WeaponManager || !OwningPlayerCharacter.WeaponManager.CurrentWeapon)
        {
            return "";
        }
        else
        {
            return OwningPlayerCharacter.WeaponManager.CurrentWeapon.ItemName;
        }
    }

    #endregion
}
