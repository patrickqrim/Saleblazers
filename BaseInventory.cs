using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public static class BaseInventoryReaderWriter
{
    public static void WriteBaseInventory(this Mirror.NetworkWriter writer, BaseInventory InBaseInventory)
    {
        if (InBaseInventory)
        {
            writer.Write<uint>(InBaseInventory.netId);
            writer.Write<int>(InBaseInventory.ComponentIndex);
        }
        else
        {
            writer.Write<uint>(0);
            writer.Write<int>(-1);
        }
    }

    public static BaseInventory ReadBaseInventory(this Mirror.NetworkReader reader)
    {
        Mirror.NetworkIdentity outNetworkIdentity = null;
        uint netID = reader.Read<uint>();
        int componentIndex = reader.Read<int>();

        if (componentIndex != -1 && Mirror.NetworkIdentity.spawned.TryGetValue(netID, out outNetworkIdentity))
        {
            return outNetworkIdentity.NetworkBehaviours[componentIndex] as BaseInventory;
        }
        else
        {
            return null;
        }
    }
}

[System.Serializable]
public struct BaseStartingWeaponData
{
    public BaseWeapon WeaponPrefab;
    public int Count;
    public BaseRarity Rarity;
}

// Used for number of items to generate in loot box
public static class PoissonRandomGenerator
{
    private static readonly System.Random random = new System.Random();

    public static int GeneratePoisson(double lambda)
    {
        double L = System.Math.Exp(-lambda);
        int k = 0;
        double p = 1;

        do
        {
            k++;
            double u = random.NextDouble();
            p *= u;
        } while (p > L);

        return k - 1;
    }
}

[Ceras.SerializedType]
public class BaseInventory : Mirror.NetworkBehaviour, IHRSaveable
{
    // Whether or not we should cache every item to a dictionary entry. This is typically only necessary for players, e.g. checking to see if they have a lockpick or not.
    public bool bShouldCacheItemIDDictionary = false;
    public bool bShouldDropContentsOnPickup = true;

    public Dictionary<int, List<int>> ItemIDToSlotCache;
    public enum InventoryHighlightType { Hotkey, Hover };

    public BaseWeapon OwningWeapon;
    public BaseWeaponManager OwningWeaponManager;

    // Where items will appear when being dropped.
    public GameObject DropTransform;
    // The name of the inventory. Examples: Backpack, Wooden Crate
    public string InventoryName;
    // How many slots the inventory can have total.
    [Mirror.SyncVar(hook = "MaxSlotsChanged")]
    public int MaxSlots;
    public int LocalMaxSlots;
    public int ExpectedSlots;
    // How many slots that can currently be used.
    public int UnlockedSlots;
    public bool bIsClientInstanced;
    // How many items are in the inventory.
    [HideInInspector]
    public int NumItems = 0;

    // -1 is unlimited.
    public int MaxStackLimit = 100000;

    [HideInInspector]
    public BaseInventoryUI InventoryUI;

    // The individual inventory slots.
    [HideInInspector]
    public SyncListBaseInventorySlot InventorySlots = new SyncListBaseInventorySlot();

    [SerializeField]
    private HRItemFilter ItemFilter;

    public BaseItemBreakRepair ItemBreakRepair;

    public bool bDropItemsOnDestroy = false;
    public bool bRandomizeContents = true;
    public bool bRandomizeOnOpen = true;
    [Tooltip("If set to true, this inventory will not allow the player to insert any items into it.")]
    public bool bDoNotAllowInput = false;

    // Delegate for UI to update that a slot has changed.
    public delegate void FInventorySlotChangeDelegate(BaseInventory InInventory, int ChangedSlot);
    public FInventorySlotChangeDelegate SlotChangedDelegate;

    // Delegate for UI to update that max slots have changed
    public delegate void FInventoryMaxSlotChangeDelegate(BaseInventory InInventory, int MaxSlots);
    public FInventorySlotChangeDelegate MaxSlotChangedDelegate;

    // Delegate for UI to update that a slot has been highlighted.
    public delegate void FInventoryHighlightChangeDelegate(BaseInventory Inventory, int ChangedSlot, InventoryHighlightType HighlightType);
    public FInventoryHighlightChangeDelegate SlotHighlighedDelegate;

    // Delegate that weapon is added/removed.
    public delegate void FInventorySlotWeaponChangeDelegate(BaseInventory InInventory, int Index,
        BaseWeapon OldWeapon, BaseWeapon NewWeapon);
    public FInventorySlotWeaponChangeDelegate WeaponChangedDelegate;

    public delegate void FInventorySlotWeaponStackChangedDelegate(BaseInventory InInventory, BaseWeapon Weapon, int OldAmount, int NewAmount);
    public FInventorySlotWeaponStackChangedDelegate WeaponStackCountChangedDelegate;

    public delegate void FInventoryManuallyClosedSignature(BaseInventory InInventory);
    // Delegate reserved for when the inventory UI is manually closed by clicking the X button
    public FInventoryManuallyClosedSignature InventoryUIManuallyClosedDelegate;

    public delegate void FInventoryInitializeSignature(BaseInventory InInvetory);
    public FInventoryInitializeSignature OnInventoryInitializedDelegate;
    public FInventoryInitializeSignature OnInventoryStartClient;

    public delegate void FWeaponSelectedSignature(BaseWeapon PickedWeapon);
    public FWeaponSelectedSignature OnItemPickedForCustomerDelegate;

    public delegate void ManagerDragInventorySlotSignature(BasePlayerInventoryManager Manager, BaseInventory Inventory, int SlotIndex, bool bDragging);
    public ManagerDragInventorySlotSignature OnManagerDragInventorySlotDelegate;

    public BaseWeapon[] StartingInventoryItems;
    public BaseStartingWeaponData[] StartingInventoryWeapons;
    public bool bHideStartingInventoryItems = true;
    bool bInitializedStartingInventory = false;
    [HideInInspector]
    public bool bInitializedInventory = false;
    [Tooltip("If an item can be dragged out of inventory into the world. (For example, disabled for shop inventories)")]
    public bool bCanPlaceFromInventory = true;
    public bool bItemInteractableState = false;

    public bool useVariantLootTables = false;
    [ShowIf("useVariantLootTables")]
    [Tooltip("If -1, then will chose random loot table or the RandomizedLotTable")]
    public int TargetLootTable = 0;
    [ShowIf("useVariantLootTables")]
    public HRItemLootTableDB[] VariantLootTables;
    [HideIf("useVariantLootTables")]
    public HRItemLootTableDB RandomizedLootTable;
    public int MinRandomItemsToSpawn;
    public int MaxRandomItemsToSpawn;
    public bool bNested = false;
    public bool bUsePoisson = false;  // will be uniform by default if false
    public int poissonLambda = 1;

    public bool bOverrideShowOnAdd = false;

    // Can move this to another managing component instead of being in itself
    public bool bShouldCacheSaveComponent = false;
    public List<HRSaveComponent> SaveCache;

    public delegate void FInventoryActiveSignature(BaseInventory InInventory, bool bActive);
    public FInventoryActiveSignature InventoryActiveDelegate;

    [System.NonSerialized, Ceras.SerializedField]
    public bool bContentsBeenRandomized;

    // So bad but this is to use for placing spawned items
    public Vector3 Temp_PlacingPosition;
    public Quaternion Temp_PlacingRotation;

    private bool bIsDirty;

    private Dictionary<int, BaseWeapon> LoadedWeaponCache = new Dictionary<int, BaseWeapon>();
    private Dictionary<int, int> LoadedWeaponCountCache = new Dictionary<int, int>();

    Queue<int> MoveToPlayerInventoryQueue;

    public GameObject SpawnItemIntoInventory(GameObject PrefabToSpawn)
    {
        return SpawnItemIntoInventory(PrefabToSpawn, 1);
    }

    public GameObject SpawnItemIntoInventory(GameObject PrefabToSpawn, int amount)
    {
        GameObject SpawnedGO = Instantiate(PrefabToSpawn);

        if (SpawnedGO)
        {
            SpawnedGO.transform.position = this.transform.position;
            if (Mirror.NetworkServer.active)
            {
                SpawnedGO.GetComponent<Mirror.NetworkIdentity>().bSpawnImmediately = true;
                Mirror.NetworkServer.Spawn(SpawnedGO);
            }
            BaseWeapon weapon = SpawnedGO.GetComponent<BaseWeapon>();
            if (weapon)
            {
                weapon.StackCount = amount;
            }
        }

        return SpawnedGO;
    }

    public void SpawnItemIntoInventory(int itemID, int amount)
    {
        if (HRNetworkManager.IsHost())
        {
            SpawnItemIntoInventory_Implementation(itemID, amount);
        }
        else
        {
            SpawnItemIntoInventory_Command(itemID, amount);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SpawnItemIntoInventory_Command(int itemID, int amount)
    {
        SpawnItemIntoInventory_Implementation(itemID, amount);
    }

    private void SpawnItemIntoInventory_Implementation(int itemID, int amount)
    {
        if (itemID < 0)
        {
            return;
        }
        GameObject prefab = ((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemArray[itemID].ItemPrefab;
        if (prefab)
        {
            SpawnItemIntoInventory(prefab, amount);
        }
    }

    public bool CanRandomize()
    {
        return (RandomizedLootTable != null || CanUseVariantLootTables()) && OwningWeapon && bRandomizeContents;
    }

    public bool CanUseVariantLootTables()
    {
        return (useVariantLootTables && VariantLootTables != null && (TargetLootTable <= 0 || (VariantLootTables[TargetLootTable - 1])));
    }

    public HRItemLootTableDB GetRandomVariantLootTable()
    {
        if (TargetLootTable <= 0)
        {
            return VariantLootTables[Random.Range(0, VariantLootTables.Length)];
        }
        else
        {
            return VariantLootTables[TargetLootTable - 1];
        }
    }

    public void SetLootTableVariantData(int variantData)
    {
        if (HRNetworkManager.IsHost())
        {
            SetVariantLootTableDataImplementation(variantData);
            SetVariantLootTableDataClientRPC(variantData);
        }
        else
        {
            SetVariantLootTableDataCommand(variantData);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SetVariantLootTableDataCommand(int variantData)
    {
        SetVariantLootTableDataImplementation(variantData);
        SetVariantLootTableDataClientRPC(variantData);
    }
    [Mirror.ClientRpc]
    private void SetVariantLootTableDataClientRPC(int variantData)
    {
        if (!HRNetworkManager.IsHost())
        {
            SetVariantLootTableDataImplementation(variantData);
        }
    }
    private void SetVariantLootTableDataImplementation(int variantData)
    {
        if (useVariantLootTables == false) { Debug.LogError("Setting Variant Loot table for " + name + " but is not marked to use variant loot tables. Report this to Kenny! >:("); }
        TargetLootTable = variantData;
    }

    public void SpawnRandomizedItems(bool bForce = false)
    {
        if (!bForce && bContentsBeenRandomized) return;

        if (!HRNetworkManager.IsHost())
        {
            bContentsBeenRandomized = true;
            SpawnRandomizedItems_Command(bForce);
        }
        else
        {
            SpawnRandomizedItems_Server(bForce);
        }
    }

    [Mirror.Server]
    void SpawnRandomizedItems_Server(bool bForce)
    {
        if (!bForce && bContentsBeenRandomized)
        {
            return;
        }

        if (this && this.gameObject)
        {
            bContentsBeenRandomized = true;

            Random.InitState(System.DateTime.Now.Millisecond);

            int NumItemsToSpawn;
            if (bUsePoisson)
            {
                // Generate a random sample from the Poisson distribution within the range of x to y (inclusive)
                do
                {
                    NumItemsToSpawn = PoissonRandomGenerator.GeneratePoisson(poissonLambda);
                } while (NumItemsToSpawn < MaxRandomItemsToSpawn || NumItemsToSpawn > MaxRandomItemsToSpawn);
            }
            else
            {
                NumItemsToSpawn = Random.Range(MinRandomItemsToSpawn, MaxRandomItemsToSpawn + 1);
            }

            if (NumItemsToSpawn <= 0) return;

            HRItemLootTableDB.HRItemLootTableGroup LootTableGroup;

            HRItemLootTableDB targetLootTable = RandomizedLootTable;
            if (CanUseVariantLootTables())
            {
                targetLootTable = GetRandomVariantLootTable();
            }
            int GroupIndex = targetLootTable.RollRandomLootGroup(out LootTableGroup);
            if (GroupIndex == -1) return;

            if (OwningWeapon)
            {
                OwningWeapon.SetItemRarity(targetLootTable.LootTableItems[GroupIndex].Rarity);
            }

            for (int i = 0; i < NumItemsToSpawn; ++i)
            {
                HRItemLootTableDB.HRItemLootTableEntry LootTableEntry;

                if (!LootTableGroup.RollRandomLootItem(out LootTableEntry, NestedGroupIndex : (bNested ? 0 : -1))) continue;

                int randomID = LootTableEntry.ItemID;

                GameObject PrefabToSpawn = randomID != -1 ? ((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemArray[randomID].ItemPrefab : null;
                if (PrefabToSpawn)
                {
                    // Check if inventory has a free space for it, otherwise don't bother spawning it
                    if (GetFirstStackSlot(randomID, bIgnoreAllowInput : true) != -1)
                    {
                        var SpawnedGO = SpawnItemIntoInventory(PrefabToSpawn);
                        if (SpawnedGO)
                        {
                            BaseWeapon SpawnedWeapon = SpawnedGO.GetComponent<BaseWeapon>();
                            if (SpawnedWeapon)
                            {
                                if (!LootTableEntry.bDontRandomizeColorway)
                                {
                                    HRClothingComponent ClothingComponent = SpawnedWeapon.GetComponent<HRClothingComponent>();

                                    if (ClothingComponent != null)
                                    {
                                        ClothingComponent.RandomizeColorway();
                                    }
                                }

                                HRConsumableRecipeUnlock RecipeUnlock = SpawnedGO.GetComponent<HRConsumableRecipeUnlock>();

                                if (RecipeUnlock != null)
                                {
                                    RecipeUnlock.RollUnlocks(LootTableEntry.RecipeUnlockData);
                                }
                                else
                                {
                                    if (LootTableEntry.RolledRarities != null)
                                    {
                                        BaseRarity RolledRarity;
                                        if (LootTableEntry.RolledRarities.TryGetRandomEntry(out RolledRarity))
                                        {
                                            SpawnedWeapon.SetItemRarity(RolledRarity);
                                            SpawnedWeapon.bRandomizeRarityOnStart = false;
                                            SpawnedWeapon.bIgnoreZoneMaxRarityLimit = true;
                                        }
                                    }
                                }

                                if (LootTableEntry.ItemPrice > 0)
                                {
                                    SpawnedWeapon.ItemValue.SetUniqueValue(LootTableEntry.ItemPrice);
                                }

                                SpawnedWeapon.InitializeWeapon();
                                AddStartingWeapon(SpawnedWeapon, bIgnoreAllowInput: true);
                            }
                        }
                    }
                }
            }
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    void SpawnRandomizedItems_Command(bool bForce)
    {
        SpawnRandomizedItems_Server(bForce);
    }

    public override void Awake()
    {
        base.Awake();

        if (!bInitialized)
        {
            if (bShouldCacheItemIDDictionary)
            {
                ItemIDToSlotCache = new Dictionary<int, List<int>>();
            }

            LocalMaxSlots = MaxSlots;

            if (InventorySlots == null || InventorySlots.Count == 0)
            {
                if ((!Mirror.NetworkServer.active && !Mirror.NetworkClient.active) || HRNetworkManager.IsHost())
                    PopulateInventorySlots();
            }

            if (CanRandomize() && !bRandomizeOnOpen)
            {
                OwningWeapon.OnWeaponRandomizeDelegate += HandleWeaponRandomize;
            }

            InventorySlots.Callback += HandleInventorySlotsUpdated;

            if (bIsClientInstanced)
            {
                bDoNotAllowInput = true;
                if (OwningWeapon)
                {
                    OwningWeapon.SetCanPickUp(false);
                }
            }

            if (OwningWeapon)
            {
                OwningWeapon.WeaponAttemptPickupDelegate += HandleWeaponPickup;
            }

            bInitialized = true;
        }
    }
    
    public void InitializeCache()
    {
        bShouldCacheItemIDDictionary = true;
        ItemIDToSlotCache = new Dictionary<int, List<int>>();
        for (int i = 0; i < InventorySlots.Count; i++)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                AddItemToCacheID(InventorySlots[i].SlotWeapon, i);
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        bStartClient = true;
        //ForceUpdateInventory();
        bStartClient = false;

        bInitializedInventory = true;
        OnInventoryInitializedDelegate?.Invoke(this);
        OnInventoryStartClient?.Invoke(this);

        if(bIsClientInstanced && OwningWeapon && OwningWeapon.RarityBeam)
        {
            OwningWeapon.RarityBeam.SetVisibility(true);
        }
    }


    public void ForceUpdateInventory()
    {
        if (HRNetworkManager.IsHost())
        {
            BaseWeapon[] weapons = new BaseWeapon[InventorySlots.Count];
            for (int i = 0; i < InventorySlots.Count; ++i)
            {
                if (InventorySlots[i].SlotWeapon)
                {
                    weapons[i] = InventorySlots[i].SlotWeapon;
                }
            }

            ForceUpdateInventory_ClientRpc(weapons);
        }
        else
        {
            ForceUpdateInventory_Command();
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    private void ForceUpdateInventory_Command()
    {
        BaseWeapon[] weapons = new BaseWeapon[InventorySlots.Count];
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                weapons[i] = InventorySlots[i].SlotWeapon;
            }
        }

        ForceUpdateInventory_ClientRpc(weapons);
    }


    [Mirror.ClientRpc(excludeOwner = true)]
    private void ForceUpdateInventory_ClientRpc(BaseWeapon[] baseInventorySlots)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            Mirror.SyncList<BaseInventorySlot>.Operation op = new Mirror.SyncList<BaseInventorySlot>.Operation();
            var slotA = new BaseInventorySlot();
            var slotB = new BaseInventorySlot();
            slotB.SlotWeapon = baseInventorySlots[i];

            HandleInventorySlotsUpdated(op, i, slotA, slotB);
        }
    }


    bool bInitialized = false;

    // This flag is set to true when we are updating from the network.
    [System.NonSerialized]
    public bool bUpdatingInventory = true;
    [System.NonSerialized]
    public bool bStartClient = false;
    private bool bHasUpdatedInventory = false;

    public override void OnEnable()
    {
        base.OnEnable();
        InventoryActiveDelegate?.Invoke(this, true);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        InventoryActiveDelegate?.Invoke(this, false);
    }

    void AddItemToCacheID(BaseWeapon InWeapon, int InSlot)
    {
        if (!bShouldCacheItemIDDictionary || !InWeapon || ItemIDToSlotCache == null)
        {
            return;
        }

        if (!ItemIDToSlotCache.ContainsKey(InWeapon.ItemID))
        {
            ItemIDToSlotCache.Add(InWeapon.ItemID, new List<int>());
        }

        if (!ItemIDToSlotCache[InWeapon.ItemID].Contains(InSlot))
        {
            ItemIDToSlotCache[InWeapon.ItemID].Add(InSlot);
        }
    }

    void RemoveItemFromCacheID(BaseWeapon InWeapon, int InSlot)
    {
        if (!bShouldCacheItemIDDictionary || !InWeapon || ItemIDToSlotCache == null)
        {
            return;
        }

        if (ItemIDToSlotCache.ContainsKey(InWeapon.ItemID))
        {
            ItemIDToSlotCache[InWeapon.ItemID].Remove(InSlot);

            if (ItemIDToSlotCache[InWeapon.ItemID].Count == 0) { ItemIDToSlotCache.Remove(InWeapon.ItemID); } // kenny, remove the key if there is no slot that contains this weapon
        }


    }

    public BaseWeapon GetItemFromCache(int InItemID)
    {
        if (bShouldCacheItemIDDictionary && ItemIDToSlotCache != null)
        {
            List<int> ItemSlots;
            if (ItemIDToSlotCache.TryGetValue(InItemID, out ItemSlots))
            {
                if (ItemSlots.Count > 0 && ItemSlots[0] >= 0 && ItemSlots[0] < InventorySlots.Count)
                {
                    if (InventorySlots[ItemSlots[0]].SlotWeapon && InventorySlots[ItemSlots[0]].SlotWeapon.ItemID == InItemID)
                    {
                        return InventorySlots[ItemSlots[0]].SlotWeapon;
                    }
                }
            }
        }

        return null;
    }

    //Called when InvetorySlots SyncList is modified
    void HandleInventorySlotsUpdated(SyncListBaseInventorySlot.Operation op, int index, BaseInventorySlot oldItem, BaseInventorySlot newItem)
    {
        //Update save cache
        if (bShouldCacheSaveComponent)
        {
            if (newItem.SlotWeapon != null)
            {
                SaveCache.Add(newItem.SlotWeapon.SaveComponentRef);
            }
            else
            {
                if (oldItem.SlotWeapon)
                {
                    SaveCache.Remove(oldItem.SlotWeapon.SaveComponentRef);
                }
            }
        }

        // If not the server
        bUpdatingInventory = true;

        if (newItem.SlotWeapon)
        {
            if (oldItem.SlotWeapon != newItem.SlotWeapon)
            {
                AddItemToCacheID(newItem.SlotWeapon, index);

                if (oldItem.SlotWeapon != null)
                {
                    RemoveItemFromCacheID(oldItem.SlotWeapon, index);
                }
            }

            if (HRNetworkManager.IsHost())
            {
                newItem.SlotWeapon.OwningInventory = this;
            }
        }
        else if (oldItem.SlotWeapon)
        {
            //Remove old weapon from inventory (drop)
            RemoveItemFromCacheID(oldItem.SlotWeapon, index);

            oldItem.SlotWeapon.PreviousOwningInventory = this;

            if (oldItem.SlotWeapon.OwningInventory == null)
            {
                oldItem.SlotWeapon.WeaponDropDelegate?.Invoke(oldItem.SlotWeapon, null);
            }
        }

        // Ensure that the slot count is correct
        NumItems = 0;

        foreach (var Slot in InventorySlots)
        {
            if (!Slot.bLocked && Slot.SlotWeapon != null)
            {
                NumItems++;
            }
        }

        WeaponChangedDelegate?.Invoke(this, index, oldItem.SlotWeapon, newItem.netId == 0 ? null : newItem.SlotWeapon);
        SlotChangedDelegate?.Invoke(this, index);

        bUpdatingInventory = false;
        bHasUpdatedInventory = true;

        if (newItem.SlotWeapon && newItem.SlotWeapon.PreviousOwningInventory)
        {
            newItem.SlotWeapon.PreviousOwningInventory.ProcessTakeAllQueue();
        }
    }

    public int GetDroppableAmount()
    {
        int DroppableAmount = 0;

        for (int i = 0; i < InventorySlots.Count; i++)
        {
            if (InventorySlots[i].SlotWeapon && InventorySlots[i].SlotWeapon.bDropOnDeath && InventorySlots[i].SlotWeapon.bCanDrop)
            {
                DroppableAmount++;
            }
        }

        return DroppableAmount;
    }

    void HandleWeaponRandomize(BaseWeapon InWeapon)
    {
        SpawnRandomizedItems();
    }

    public void SetMaxSlots(int NewMaxSlots, bool bRefresh = false)
    {
        LocalMaxSlots = NewMaxSlots;

        // Currently clears the current inventory because of PopulateInventorySlots()
        if (HRNetworkManager.IsHost())
        {
            SetMaxSlots_Implementation(NewMaxSlots, bRefresh);
        }
        else
        {
            SetMaxSlots_Command(NewMaxSlots, bRefresh);
        }
    }

    public void SetMaxSlots_Implementation(int NewMaxSlots, bool bRefresh)
    {
        if (bRefresh)
        {
            UnlockedSlots = NewMaxSlots;
            ChangeMaxInventorySlots(MaxSlots, NewMaxSlots);
        }
        else
        {
            UpdateMaxSlots_Implementation(NewMaxSlots);
            PopulateInventorySlots();
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    public void SetMaxSlots_Command(int NewMaxSlots, bool bRefresh)
    {
        SetMaxSlots_Implementation(NewMaxSlots, bRefresh);
    }

    private void MaxSlotsChanged(int OldMaxSlots, int NewMaxSlots)
    {
        ExpectedSlots = NewMaxSlots;
        MaxSlotChangedDelegate?.Invoke(this, NewMaxSlots);
    }

    public void PopulateInventorySlots()
    {
        if (InventorySlots.Count > 0)
        {
            InventorySlots.Clear();
        }

        for (int i = 0; i < MaxSlots; ++i)
        {
            BaseInventorySlot NewSlot = new BaseInventorySlot(i >= UnlockedSlots, bCanPlaceFromInventory);
            NewSlot.InternalStackLimit = MaxStackLimit;

            InventorySlots.Add(NewSlot);
        }
    }

    public void ChangeMaxInventorySlots(int OldMax, int NewMax)
    {
        if (InventorySlots.Count > NewMax)
        {
            for (int i = InventorySlots.Count; i > NewMax; --i)
            {
                InventorySlots.RemoveAt(i - 1);
            }
        }
        else
        {
            for (int i = InventorySlots.Count; i < NewMax; ++i)
            {
                BaseInventorySlot NewSlot = new BaseInventorySlot(i >= UnlockedSlots, bCanPlaceFromInventory);
                NewSlot.InternalStackLimit = MaxStackLimit;

                InventorySlots.Add(NewSlot);
            }

            SetSlotsLocked(OldMax, NewMax - 1, false);
        }

        UpdateMaxSlots_Implementation(NewMax);
    }

    private void UpdateMaxSlots_Implementation(int slots)
    {
        MaxSlots = slots;
        ExpectedSlots = slots;
    }

    public void UpdateCanPlace(bool bCanPlace)
    {
        bCanPlaceFromInventory = bCanPlace;
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            BaseInventorySlot NewSlot = InventorySlots[i];
            NewSlot.bCanBePlaced = bCanPlace;

            InventorySlots[i] = NewSlot;
        }
    }

    protected virtual void Start()
    {
        if (bShouldCacheSaveComponent)
        {
            SaveCache = new List<HRSaveComponent>();
        }

        if (HRNetworkManager.IsHost())
        {
            InitializeStartingInventoryItems();
        }

        if (OwningWeapon)
        {
            if (OwningWeapon.AttributeManager)
                OwningWeapon.AttributeManager.AddStringAttribute("Slots", this.InventorySlots.Count.ToString());

            if (OwningWeapon.HPComponent)
            {
                OwningWeapon.HPComponent.OnHPZeroDelegate += HandleHPZero;
            }
        }
    }

    void HandleWeaponPickup(BaseWeapon InWeapon, BaseWeaponManager InWeaponManager)
    {
        // Drop all contents when something is picked up
        if(bShouldDropContentsOnPickup)
        {
            for (int i = InventorySlots.Count - 1; i >= 0; --i)
            {
                if (InventorySlots[i].SlotWeapon != null)
                {
                    InventorySlots[i].SlotWeapon.transform.position = this.transform.position;
                    RemoveWeapon(i, -1, false);
                }
            }
        }
    }

    void HandleHPZero(BaseHP InHPComponent, GameObject Instigator, float PreviousHP, float NewHP, bool bFireEffects)
    {
        // Close this container if it is open
        // terrible code, should not mix HR
        BaseScripts.BasePawn PlayerPawn = BaseGameInstance.Get.GetFirstPawn();
        if (PlayerPawn)
        {
            if (PlayerPawn is HeroPlayerCharacter && (PlayerPawn as HeroPlayerCharacter).InventoryManager)
            {
                BaseContainer CurrentContainer = (PlayerPawn as HeroPlayerCharacter).InventoryManager.CurrentContainer;

                if (CurrentContainer)
                {
                    if (CurrentContainer.Inventory == this)
                    {
                        (PlayerPawn as HeroPlayerCharacter).InventoryManager.ContainerInteract(CurrentContainer);
                    }
                }
            }
        }
    }

    public void InitializeStartingInventoryItems()
    {
        if (bInitializedStartingInventory || StartingInventoryItems == null)
        {
            return;
        }

        for (int i = 0; i < StartingInventoryItems.Length; ++i)
        {
            if (StartingInventoryItems[i])
            {
                bInitializedStartingInventory = true;

                // Item has not been instantiated
                if (StartingInventoryItems[i].gameObject.scene.name == null)
                {
                    var SpawnedGO = SpawnItemIntoInventory(StartingInventoryItems[i].gameObject);

                    BaseWeapon SpawnedWeapon = SpawnedGO.GetComponent<BaseWeapon>();
                    if (SpawnedWeapon)
                    {
                        //SpawnedWeapon.bRandomizeRarityOnStart = false;
                        SpawnedWeapon.InitializeWeapon();
                        AddStartingWeapon(SpawnedWeapon);
                    }
                }
                else
                {
                    AddStartingWeapon(StartingInventoryItems[i]);
                }
            }
        }

        for (int i = 0; i < StartingInventoryWeapons.Length; ++i)
        {
            if (StartingInventoryWeapons[i].WeaponPrefab)
            {
                bInitializedStartingInventory = true;

                if (StartingInventoryWeapons[i].WeaponPrefab.gameObject.scene.name == null)
                {
                    GameObject SpawnedGO = SpawnItemIntoInventory(StartingInventoryWeapons[i].WeaponPrefab.gameObject);
                    BaseWeapon SpawnedWeapon = SpawnedGO.GetComponent<BaseWeapon>();
                    if (SpawnedWeapon)
                    {
                        //SpawnedWeapon.bRandomizeRarityOnStart = false;
                        SpawnedWeapon.InitializeWeapon();
                        SpawnedWeapon.SetStackCount(StartingInventoryWeapons[i].Count);
                        SpawnedWeapon.SetItemRarity(StartingInventoryWeapons[i].Rarity);
                        AddStartingWeapon(SpawnedWeapon);
                    }
                }
            }
        }
    }

    void AddStartingWeapon(BaseWeapon InWeapon, bool bIgnoreAllowInput = true)
    {
        if (!InWeapon)
        {
            return;
        }

        AddWeapon(InWeapon, -1, bIgnoreAllowInput: bIgnoreAllowInput);
        InWeapon.DisablePhysics();
    }

    //
    public bool InsertWeapon(BaseWeapon WeaponToInsert, int Index, bool bUseFilter = false, bool bAutoFill = false, int Amount = -1, bool bSwappingSlots = false)
    {
        if (!CanInsertWeapon(WeaponToInsert, Index, bUseFilter, bAutoFill, Amount, bSwappingSlots))
        {
            return false;
        }
        if (HRNetworkManager.IsHost())
        {
            return InsertWeapon_Implementation(WeaponToInsert, Index, bUseFilter, bAutoFill, Amount, bSwappingSlots);
        }
        else
        {
            InsertWeapon_Command(WeaponToInsert, Index, bUseFilter, bAutoFill, Amount, bSwappingSlots);
            return true;
        }
    }

    public void SpawnAndInsertWeapon(int WeaponID, int Index, bool bUseFilter, bool bAutoFill, int Amount)
    {
        if (HRNetworkManager.IsHost())
        {
            SpawnAndInsertWeapon_Implementation(WeaponID, Index, bUseFilter, bAutoFill, Amount);
        }
        else
        {
            SpawnAndInsertWeapon_Command(WeaponID, Index, bUseFilter, bAutoFill, Amount);
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    private void SpawnAndInsertWeapon_Command(int WeaponID, int Index, bool bUseFilter, bool bAutoFill, int Amount)
    {
        SpawnAndInsertWeapon_Implementation(WeaponID, GetFirstFreeSlot(), bUseFilter, bAutoFill, Amount);
    }

    private void SpawnAndInsertWeapon_Implementation(int WeaponID, int Index, bool bUseFilter, bool bAutoFill, int Amount)
    {
        if (WeaponID < 0 || WeaponID >= ((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemArray.Length)
        {
            Debug.LogError("ERROR: Invalid Weapon ID: " + WeaponID);
            return;
        }

        var Prefab = ((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemArray[WeaponID].ItemPrefab;

        if (Prefab)
        {
            BaseWeapon Weapon = Instantiate(Prefab, this.transform.position, Quaternion.identity).GetComponent<BaseWeapon>();

            if (HRNetworkManager.bIsServer)
            {
                Weapon.netIdentity.bSpawnImmediately = true;
                Mirror.NetworkServer.Spawn(Weapon.gameObject);
            }

            InsertWeapon(Weapon, Index, bUseFilter, bAutoFill, Amount);
            Weapon.SetStackCount(Amount);
        }
    }

    public void OnWeaponStackChanged(BaseWeapon Weapon, int OldAmount, int NewAmount)
    {
        WeaponStackCountChangedDelegate?.Invoke(this, Weapon, OldAmount, NewAmount);
    }

    public bool CanInsertWeapon(BaseWeapon WeaponToInsert, int Index, bool bUseFilter = false, bool bAutoFill = false, int Amount = -1, bool bSwappingSlots = false)
    {
        //Is weapon null?
        if (WeaponToInsert == null)
        {
            return true;
        }

        //Validate index
        if (Index < 0 || Index >= InventorySlots.Count)
        {
            return false;
        }
        //Locked?
        if (InventorySlots[Index].bLocked)
        {
            return false;
        }

        //Check if the slot is full already
        bool useInternalStackLimit = true;
        int stackLimit = InventorySlots[Index].GetStackLimit(useInternalStackLimit);
        int currentStackCount = 0;
        if (InventorySlots[Index].SlotWeapon && InventorySlots[Index].SlotWeapon.ItemID == WeaponToInsert.ItemID && !bSwappingSlots)
        {
            currentStackCount = InventorySlots[Index].SlotWeapon.StackCount;
        }

        //Add this to hard limit item movement based on stack count
        int proposedStackCount = ((Amount == -1) ? WeaponToInsert.StackCount : Amount) + currentStackCount;
        if (stackLimit != -1 && stackLimit < proposedStackCount)
        {
            return false;
        }

        //Run filter check
        if ((!bUpdatingInventory || !bHasUpdatedInventory) && bUseFilter)
        {
            if (!RunFilterCheck(WeaponToInsert))
            {
                return false;
            }
        }

        //If inventory is a container, check if container insert
        var ThisContainer = GetComponent<BaseContainer>();
        if (ThisContainer && !ThisContainer.CanInsert(WeaponToInsert, true))
        {
            return false;
        }

        return true;
    }

    private IEnumerator DelayedWeaponInsert(BaseWeapon WeaponToInsert, int Index, bool bUseFilter = false, bool bAutoFill = false, int Amount = -1)
    {
        yield return new WaitUntil(() => InventorySlots != null);
        yield return new WaitUntil(() => Index < InventorySlots.Count);

        InsertWeapon(WeaponToInsert, Index, bUseFilter, bAutoFill, Amount);
    }

    //Actually add the weapon to the slot
    public bool InsertWeapon_Implementation(BaseWeapon WeaponToInsert, int Index, bool bUseFilter = false, bool bAutoFill = false, int Amount = -1, bool bSwappingSlots = false)
    {
        if (InventorySlots == null || Index >= InventorySlots.Count)
        {
            if (InventorySlots == null)
            {
                Debug.LogError("ERROR: Inventory slots list is null.");
                StartCoroutine(DelayedWeaponInsert(WeaponToInsert, Index, bUseFilter, bAutoFill, Amount));
            }
            else
            {
                if (WeaponToInsert == null)
                {
                    Debug.LogError("ERROR: null weapon is out of bounds (Index " + Index + ", Count " + InventorySlots.Count + ")");
                }
                else
                {
                    Debug.LogError("ERROR: " + WeaponToInsert.ItemName + " is out of bounds (Index " + Index + ", Count " + InventorySlots.Count + ")");
                    StartCoroutine(DelayedWeaponInsert(WeaponToInsert, Index, bUseFilter, bAutoFill, Amount));
                }
            }
;
            return false;
        }

        BaseWeapon OldWeapon = InventorySlots[Index].SlotWeapon;

        bool bDestroyInsertedWeapon = false;
        bool bSameSlot = InventorySlots[Index].SlotWeapon == WeaponToInsert;
        int StackLimit = MaxStackLimit;

        if (WeaponToInsert)
        {
            WeaponToInsert.OwningInventory = this;

            int AmountToAdd = Amount;

            if ((AmountToAdd == -1 || AmountToAdd > WeaponToInsert.StackCount) && !bSameSlot)
            {
                AmountToAdd = WeaponToInsert.StackCount;
            }

            //Is slot empty or not the same as the item we want to add or not stackable
            if (InventorySlots[Index].SlotWeapon == null || (WeaponToInsert && (InventorySlots[Index].SlotWeapon.ItemID != WeaponToInsert.ItemID || WeaponToInsert.StackLimit == 1)))
            {
                //If we are adding the entire stack
                if (AmountToAdd == WeaponToInsert.StackCount)
                {
                    BaseInventorySlot TempSlot = InventorySlots[Index];
                    TempSlot.SetWeapon(WeaponToInsert);

                    InventorySlots[Index] = TempSlot;
                    //HideWeapon(WeaponToInsert, true);
                }
                //Otherwise, spawn a new weapon with the correct stack count and add it
                else
                {
                    GameObject WeaponPrefab = ((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemArray[WeaponToInsert.ItemID].ItemPrefab;
                    if (WeaponPrefab)
                    {
                        BaseWeapon NewWeaponInstance = Instantiate(WeaponPrefab, this.transform.position, Quaternion.identity).GetComponent<BaseWeapon>();
                        if (NewWeaponInstance)
                        {
                            WeaponToInsert.SetStackCount(WeaponToInsert.StackCount - AmountToAdd);
                            NewWeaponInstance.SetStackCount(AmountToAdd);

                            // TODO networking
                            if (HRNetworkManager.bIsServer)
                            {
                                NewWeaponInstance.netIdentity.bSpawnImmediately = true;
                                Mirror.NetworkServer.Spawn(NewWeaponInstance.gameObject);
                            }

                            BaseInventorySlot TempSlot = InventorySlots[Index];
                            TempSlot.SetWeapon(NewWeaponInstance);
                            InventorySlots[Index] = TempSlot;

                            NewWeaponInstance.InitializeWeapon();
                            WeaponToInsert = NewWeaponInstance;
                            NewWeaponInstance.HideWeapon(true);
                        }
                    }
                }
            }
            //There is something in slot that has the same ItemID as what we are adding and it is stackable
            else
            {
                //Only add the amount we are able
                //Maybe redundant of InsertInventorySlots_Implementation
                int AfterAddStackAmount = AmountToAdd;
                if (OldWeapon)
                {
                    AfterAddStackAmount += OldWeapon.StackCount;
                    StackLimit = Mathf.Min(StackLimit, OldWeapon.StackLimit);
                }
                int Remainder = (AfterAddStackAmount - StackLimit);
                if (StackLimit != -1 && StackLimit < AfterAddStackAmount)
                {
                    AmountToAdd = AmountToAdd - Remainder;
                }

                if (Remainder > 0)
                {
                    InventorySlots[Index].SlotWeapon.SetStackCount(InventorySlots[Index].SlotWeapon.StackCount + AmountToAdd);
                    WeaponToInsert.SetStackCount(Remainder);

                    //Add the excess to the first empty slot
                    if (bAutoFill)
                    {
                        int NewSlot = GetFirstFreeSlot(WeaponToInsert);
                        if (NewSlot != -1)
                        {
                            InsertWeapon(WeaponToInsert, NewSlot, false, true);
                        }
                    }
                }
                else
                {
                    // remove inserted amount from the stack
                    if (WeaponToInsert != InventorySlots[Index].SlotWeapon)
                    {
                        WeaponToInsert.SetStackCount(WeaponToInsert.StackCount - AmountToAdd, false);
                    }

                    if (OwningWeaponManager && OwningWeaponManager.OwningPlayerCharacter.bIsPlayer && Amount > 0)
                    {
                        InventorySlots[Index].SlotWeapon.AddToNumPickupsTracked(WeaponToInsert.numPickupsTracked);
                    }

                    InventorySlots[Index].SlotWeapon.SetStackCount(InventorySlots[Index].SlotWeapon.StackCount + AmountToAdd);

                    if (WeaponToInsert.StackCount == 0)
                    {
                        bDestroyInsertedWeapon = true;
                    }
                    else
                    {
                        WeaponToInsert = InventorySlots[Index].SlotWeapon;
                    }
                }
            }
        }
        else
        {
            //There is no weapon to add, so clear the slot
            BaseInventorySlot TempSlot = InventorySlots[Index];
            TempSlot.ClearSlot();
            InventorySlots[Index] = TempSlot;
        }
        //InvokeSlotChangedEventNetworked(Index, OldWeapon, WeaponToInsert); // This is probabl unecessary

        bIsDirty = true;

        //If we have some of our weapon remaining,
        if (bDestroyInsertedWeapon)
        {
            if (WeaponToInsert)
            {
                if (WeaponToInsert.PlaceableComponent.bIsInterpingMesh)
                {
                    WeaponToInsert.bDestroyOnInterpFinished = true;
                }
                else
                {
                    if (WeaponToInsert.SaveComponentRef)
                    {
                        WeaponToInsert.SaveComponentRef.SetRemoveFromSaveOnDestroy(true);
                    }
                    Mirror.NetworkServer.Destroy(WeaponToInsert.gameObject);
                }
            }
        }
        return true;
    }

    private void InvokeSlotChangedEventNetworked(int index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            InvokeSlotChangedEventNetworked_Server(index, OldWeapon, NewWeapon);
        }
        else
        {
            InvokeSlotChangedEventNetworked_Command(index, OldWeapon, NewWeapon);
        }
    }


    [Mirror.ClientRpc]
    private void InvokeSlotChangedEventNetworked_ClientRPC(int index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
    {
        InvokeSlotChangedEventNetworked_Implementation(index, OldWeapon, NewWeapon);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void InvokeSlotChangedEventNetworked_Command(int index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
    {
        InvokeSlotChangedEventNetworked_ClientRPC(index, OldWeapon, NewWeapon);
    }

    [Mirror.Server]
    private void InvokeSlotChangedEventNetworked_Server(int index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
    {
        if (this.netIdentity)
            InvokeSlotChangedEventNetworked_ClientRPC(index, OldWeapon, NewWeapon);
    }

    private void InvokeSlotChangedEventNetworked_Implementation(int index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
    {
        SlotChangedDelegate?.Invoke(this, index);
        WeaponChangedDelegate?.Invoke(this, index, OldWeapon, NewWeapon);
    }


    [Mirror.Command(ignoreAuthority = true)]
    public void InsertWeapon_Command(BaseWeapon WeaponToInsert, int Index, bool bUseFilter, bool bAutoFill, int Amount, bool bSwappingSlots)
    {
        InsertWeapon_Implementation(WeaponToInsert, Index, bUseFilter, bAutoFill, Amount, bSwappingSlots);
    }

    public bool RunFilterCheck(BaseWeapon weaponToCheck, Vector3 rejectionDisplayPos)
    {
        if (!weaponToCheck)
        {
            return true;
        }
        return ItemFilter.CheckSizeAndType(weaponToCheck.ItemSizeAndType, rejectionDisplayPos);
    }
    public bool RunFilterCheck(BaseWeapon weaponToCheck)
    {
        if (!weaponToCheck)
        {
            return true;
        }
        return ItemFilter.CheckSizeAndType(weaponToCheck.ItemSizeAndType);
    }

    public int FindSlotWithID(int ItemID)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                if (InventorySlots[i].SlotWeapon.ItemID == ItemID)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public int FindSlotWithID(int ItemID, BaseRarity Rarity)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                if (InventorySlots[i].SlotWeapon.ItemID == ItemID && InventorySlots[i].SlotWeapon.itemRarity == Rarity)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public bool AddWeapon(BaseWeapon WeaponToAdd, int Slot = -1, bool bNoStack = false, bool bAutoFill = false, int Amount = -1, bool bIgnoreAllowInput = false)
    {
        if (Amount == 0) return false;
        if (WeaponToAdd == null)
        {
            return false;
        }

        bool bAddedWeapon = false;

        if (Slot == -1)
        {
            int FreeSlot = -1;
            if (bNoStack)
            {
                FreeSlot = GetFirstFreeSlot(WeaponToAdd, bNoStack, bIgnoreAllowInput: bIgnoreAllowInput);
            }
            else
            {
                FreeSlot = GetFirstStackSlot(WeaponToAdd, bIgnoreAllowInput: bIgnoreAllowInput);
            }

            if (FreeSlot != -1)
            {
                bAddedWeapon = InsertWeapon(WeaponToAdd, FreeSlot, bAutoFill: bAutoFill, Amount: Amount);
            }
        }
        else
        {
            if (CanInsertWeaponIntoSlot(WeaponToAdd.ItemID, Slot, bIgnoreAllowInput: bIgnoreAllowInput))
            {
                bAddedWeapon = InsertWeapon(WeaponToAdd, Slot, bAutoFill: bAutoFill, Amount: Amount);
            }
        }

        // Invoke

        return bAddedWeapon;
    }

    [Mirror.Client]
    public void AddWeapon_Client(BaseWeapon WeaponToAdd, int Slot = -1, bool bNoStack = false, bool bAutoFill = false, int Amount = -1)
    {

    }

    [Mirror.Server]
    public void AddWeapon_Server()
    {

    }

    public bool ContainsWeapon(BaseWeapon WeaponToFind)
    {
        if (!bShouldCacheItemIDDictionary)
        {
            for (int i = 0; i < InventorySlots.Count; i++)
            {
                if (InventorySlots[i].SlotWeapon == WeaponToFind)
                {
                    return true;
                }
            }
            return false;
        }
        else
        {
            // Get from cache
            List<int> SlotsWithThisItem;
            if (ItemIDToSlotCache.TryGetValue(WeaponToFind.ItemID, out SlotsWithThisItem))
            {
                if (SlotsWithThisItem.Count > 0)
                    return true;
            }

            return false;
        }
    }

    public int ContainsWeapon(int ItemID, int Amount)
    {
        int Remaining = Amount;

        if (!bShouldCacheItemIDDictionary)
        {
            for (int i = 0; i < InventorySlots.Count; ++i)
            {
                if (InventorySlots[i].SlotWeapon && InventorySlots[i].SlotWeapon.ItemID == ItemID)
                {
                    Remaining -= InventorySlots[i].SlotWeapon.StackCount;
                    if (Remaining <= 0)
                    {
                        return 0;
                    }
                }
            }

            return Remaining;
        }
        else
        {
            // Get from cache
            List<int> SlotsWithThisItem;
            if (ItemIDToSlotCache.TryGetValue(ItemID, out SlotsWithThisItem))
            {
                for (int i = 0; i < SlotsWithThisItem.Count; ++i)
                {
                    if (InventorySlots[SlotsWithThisItem[i]].SlotWeapon)
                    {
                        Remaining -= InventorySlots[SlotsWithThisItem[i]].SlotWeapon.StackCount;
                        if (Remaining <= 0)
                        {
                            return 0;
                        }
                    }
                }
            }

            return Remaining;
        }
    }

    public BaseWeapon GetWeaponByID(int ItemID)
    {
        if (!bShouldCacheItemIDDictionary)
        {
            for (int i = 0; i < InventorySlots.Count; ++i)
            {
                if (InventorySlots[i].SlotWeapon && InventorySlots[i].SlotWeapon.ItemID == ItemID)
                {
                    return InventorySlots[i].SlotWeapon;
                }
            }

            return null;
        }
        else
        {
            // Get from cache
            List<int> SlotsWithThisItem;
            if (ItemIDToSlotCache.TryGetValue(ItemID, out SlotsWithThisItem))
            {
                for (int i = 0; i < SlotsWithThisItem.Count; ++i)
                {
                    if (InventorySlots[SlotsWithThisItem[i]].SlotWeapon)
                    {
                        return InventorySlots[SlotsWithThisItem[i]].SlotWeapon;
                    }
                }
            }

            return null;
        }
    }

    //Returns a list of all of the slots that have a weapon of this type
    public bool GetSlotIndicesWithWeapon(int ItemID, out List<int> SlotIndices)
    {
        SlotIndices = new List<int>();
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon && InventorySlots[i].SlotWeapon.ItemID == ItemID)
            {
                SlotIndices.Add(i);
            }
        }
        return SlotIndices.Count != 0;
    }

    //TODO: holy moly this is getting out of hand.
    //Rework a bit to consolidate with FirstStackSlot() and add struct for parameters or something
    public int GetFirstFreeSlot(BaseWeapon InWeapon, bool bNoStack = false, int InCustomStackCount = -1, bool bIgnoreAllowInput = false)
    {
        if (InWeapon)
        {
            return GetFirstFreeSlot(InWeapon.ItemID, bNoStack, false, InCustomStackCount, bIgnoreAllowInput);
        }

        return GetFirstFreeSlot(-1, bNoStack, false, InCustomStackCount, bIgnoreAllowInput);
    }

    public int GetFirstFreeSlot(int ItemID, bool bNoStack = false, bool checkReserved = false, int InCustomStackCount = -1, bool bIgnoreAllowInput = false)
    {
        for (int i = 0; i < UnlockedSlots; ++i)
        {
            if (CanInsertWeaponIntoSlot(ItemID, i, bNoStack, false, InCustomStackCount, bIgnoreAllowInput))
            {
                return i;
            }
        }

        return -1;
    }

    public int GetFirstStackSlot(BaseWeapon InWeapon, bool checkReserved = false, bool bIgnoreAllowInput = false)
    {
        return GetFirstStackSlot(InWeapon.ItemID, checkReserved, bIgnoreAllowInput);
    }

    public int GetFirstStackSlot(int ItemID, bool checkReserved = false, bool bIgnoreAllowInput = false)
    {
        if (InventorySlots == null)
        {
            return -1;
        }

        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon && InventorySlots[i].SlotWeapon.ItemID == ItemID && InventorySlots[i].SlotWeapon.StackCount < InventorySlots[i].GetStackLimit() && (!checkReserved || !InventorySlots[i].bReserved))
            {
                return i;
            }
        }
        return GetFirstFreeSlot(ItemID, checkReserved: checkReserved, bIgnoreAllowInput: bIgnoreAllowInput);
    }

    public int GetFirstStackSlot(int ItemID, int ItemCount, bool checkReserved = false, bool bIgnoreAllowInput = false)
    {
        if (InventorySlots == null)
        {
            return -1;
        }

        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon && InventorySlots[i].SlotWeapon.ItemID == ItemID && InventorySlots[i].SlotWeapon.StackCount + ItemCount < InventorySlots[i].GetStackLimit() && (!checkReserved || !InventorySlots[i].bReserved))
            {
                return i;
            }
        }
        return GetFirstFreeSlot(ItemID, checkReserved: checkReserved, bIgnoreAllowInput: bIgnoreAllowInput);
    }

    public bool CanInsertWeaponIntoSlot(int ItemID, int InSlot, bool bNoStack = false, bool checkReserved = false, int InCustomStackCount = -1, bool bIgnoreAllowInput = false)
    {
        if (bUpdatingInventory)
        {
            return true;
        }
        if (InventorySlots == null)
        {
            return false;
        }
        if (InSlot < 0 || InSlot >= InventorySlots.Count)
        {
            return false;
        }
        if (checkReserved && InventorySlots[InSlot].bReserved)
        {
            return false;
        }
        if (!bIgnoreAllowInput && bDoNotAllowInput)
        {
            return false;
        }
        if (InventorySlots[InSlot].SlotWeapon == null)
        {
            return true;
        }
        else
        {
            int StackCount = (InCustomStackCount == -1) ? InventorySlots[InSlot].SlotWeapon.StackCount : InCustomStackCount;
            if (!bNoStack && InventorySlots[InSlot].SlotWeapon.ItemID == ItemID && StackCount < InventorySlots[InSlot].GetStackLimit() && StackCount < InventorySlots[InSlot].SlotWeapon.StackLimit)
            {
                return true;
            }
        }

        return false;
    }

    // Only checks if the slot is basically open
    public bool IsOpenWeaponSlot(int InSlot)
    {
        return InventorySlots[InSlot].SlotWeapon == null;
    }

    public int GetFirstFreeSlot()
    {
        for (int i = 0; i < UnlockedSlots; ++i)
        {
            if (InventorySlots[i].SlotWeapon == null)
            {
                return i;
            }
        }

        return -1;
    }

    public int GetFirstOccupiedSlot()
    {
        for (int i = 0; i < UnlockedSlots; ++i)
        {
            if (InventorySlots[i].SlotWeapon != null)
            {
                return i;
            }
        }

        return -1;
    }

    public BaseWeapon GetFirstWeaponInInventory()
    {
        for (int i = 0; i < UnlockedSlots; ++i)
        {
            if (InventorySlots[i].SlotWeapon != null)
            {
                return InventorySlots[i].SlotWeapon;
            }
        }
        return null;
    }

    public BaseWeapon GetFirstWeaponInInventoryToInsertInto(BaseInventory inventory)
    {
        for (int i = 0; i < UnlockedSlots; ++i)
        {
            if (InventorySlots[i].SlotWeapon != null && inventory.RunFilterCheck(InventorySlots[i].SlotWeapon, default(Vector3)))
            {
                return InventorySlots[i].SlotWeapon;
            }
        }
        return null;
    }

    public BaseWeapon RemoveWeapon(BaseWeapon WeaponToRemove, int Amount, bool bChangeWeaponTransform = true, bool bDropWeapon = true, bool bFireSlotChangedDelegate = true, bool bDeleteWeapon = false, bool bSpawnNew = true, bool bPlacing = false)
    {
        if (bIsClientInstanced) return null;

        if (bUpdatingInventory)
        {
            int SlotWithWeapon = FindSlotWithWeapon(WeaponToRemove);
            if (SlotWithWeapon != -1)
            {
                return RemoveWeapon(SlotWithWeapon, Amount, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon, bSpawnNew, bPlacing);
            }
        }
        else
        {
            if (HRNetworkManager.IsHost())
            {
                return RemoveWeapon_Implementation(WeaponToRemove, Amount, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon, bSpawnNew, bPlacing);
            }
            else
            {
                RemoveWeapon_Command(WeaponToRemove.netIdentity, Amount, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon, bSpawnNew, Temp_PlacingPosition, Temp_PlacingRotation, bPlacing);
            }
        }

        return null;
    }

    public BaseWeapon RemoveWeapon_Implementation(BaseWeapon WeaponToRemove, int Amount, bool bChangeWeaponTransform = true, bool bDropWeapon = true, bool bFireSlotChangedDelegate = true, bool bDeleteWeapon = false, bool bSpawnNew = true, bool bIsPlacing = false)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon == WeaponToRemove)
            {
                return RemoveWeapon(i, Amount, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon, bSpawnNew, bIsPlacing);
            }
        }
        return null;
    }

    // Gross, but need a function for remove weapon of a specific rarity
    public BaseWeapon RemoveWeaponByID(int ItemID, BaseRarity Rarity, int Amount, out int RemainingCount, bool bChangeWeaponTransform = true, bool bDropWeapon = true, bool bFireSlotChangedDelegate = true, bool bDeleteWeapon = false)
    {
        bool bRemoveAll = Amount == -1;
        RemainingCount = Amount;
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                if (InventorySlots[i].SlotWeapon.ItemID == ItemID && InventorySlots[i].SlotWeapon.itemRarity == Rarity)
                {
                    if (!bRemoveAll && InventorySlots[i].SlotWeapon.StackCount >= RemainingCount)
                    {
                        RemainingCount = 0;
                        return RemoveWeapon(i, Amount, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon);
                    }
                    else
                    {
                        int SlotAmount = InventorySlots[i].SlotWeapon.StackCount;
                        if (RemoveWeapon(i, SlotAmount, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon))
                        {
                            RemainingCount -= Amount;
                        }
                    }
                }
            }
        }

        return null;
    }

    public BaseWeapon RemoveWeaponByID(int ItemID, int Amount, out int RemainingCount, bool bChangeWeaponTransform = true, bool bDropWeapon = true, bool bFireSlotChangedDelegate = true, bool bDeleteWeapon = false)
    {
        bool bRemoveAll = Amount == -1;
        RemainingCount = Amount;
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                if (InventorySlots[i].SlotWeapon.ItemID == ItemID)
                {
                    if (!bRemoveAll && InventorySlots[i].SlotWeapon.StackCount >= RemainingCount)
                    {
                        int amountToRemove = RemainingCount;
                        RemainingCount = 0;
                        return RemoveWeapon(i, amountToRemove, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon);
                    }
                    else
                    {
                        int SlotAmount = InventorySlots[i].SlotWeapon.StackCount;
                        if (RemoveWeapon(i, SlotAmount, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon))
                        {
                            RemainingCount -= SlotAmount;
                        }
                    }
                }
            }
        }

        return null;
    }

    public BaseWeapon RemoveWeapon(int Index, int Amount, bool bChangeWeaponTransform = true, bool bDropWeapon = true, bool bFireSlotChangedDelegate = true, bool bDeleteWeapon = false, bool bSpawnNew = true, bool bIsPlacing = true)
    {
        if (InventorySlots[Index].SlotWeapon)
        {
            BaseWeapon WeaponToRemove = InventorySlots[Index].SlotWeapon;

            if (OwningWeaponManager && OwningWeaponManager.OwningPlayerCharacter.bIsPlayer && Amount > 0)
            {
                if (WeaponToRemove.numPickupsTracked > Amount)
                {
                    WeaponToRemove.AddToNumPickupsTracked(-Amount);
                }
                else
                {
                    WeaponToRemove.SetNumPickupsTracked(Mathf.Min(Amount, WeaponToRemove.numPickupsTracked));
                }
            }

            if (HRNetworkManager.IsHost() || (bUpdatingInventory && (!netIdentity || netIdentity.hasAuthority)))
            {
                // TODO: fix this as this causes clients to crash sometimes
                // Error: SyncLists cannot be edited by a client.
                return RemoveWeapon_Implementation(Index, Amount, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon, bSpawnNew, bIsPlacing);
            }
            else
            {
                RemoveWeapon_Command(InventorySlots[Index].SlotWeapon.netIdentity, Amount, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon, bSpawnNew, Temp_PlacingPosition, Temp_PlacingRotation, bIsPlacing);
            }

            return WeaponToRemove;
        }

        return null;
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void RemoveWeapon_Command(Mirror.NetworkIdentity InWeaponIdentity, int Amount, bool bChangeWeaponTransform, bool bDropWeapon, bool bFireSlotChangedDelegate, bool bDeleteWeapon, bool bSpawnNew, Vector3 Position, Quaternion Rotation, bool bIsPlacing)
    {
        if (InWeaponIdentity)
        {
            Temp_PlacingPosition = Position;
            Temp_PlacingRotation = Rotation;
            RemoveWeapon_Implementation(InWeaponIdentity.GetComponent<BaseWeapon>(), Amount, bChangeWeaponTransform, bDropWeapon, bFireSlotChangedDelegate, bDeleteWeapon, bSpawnNew, bIsPlacing);
        }
    }

    public BaseWeapon RemoveWeapon_Implementation(int Index, int Amount, bool bChangeWeaponTransform = true, bool bDropWeapon = true, bool bFireSlotChangedDelegate = true, bool bDeleteWeapon = false, bool bSpawnNew = true, bool bIsPlacing = false)
    {
        if (InventorySlots[Index].SlotWeapon)
        {
            int AmountToRemove = Amount < 0 || Amount >= InventorySlots[Index].SlotWeapon.StackCount ? InventorySlots[Index].SlotWeapon.StackCount : Amount;

            BaseWeapon WeaponToRemove = InventorySlots[Index].SlotWeapon;
            BaseInventorySlot OldSlot = InventorySlots[Index];

            if (WeaponToRemove)
            {
                WeaponToRemove.PreviousOwningInventory = this;
                if (AmountToRemove != WeaponToRemove.StackCount)
                {
                    WeaponToRemove.SetStackCount(WeaponToRemove.StackCount - AmountToRemove);

                    if (bSpawnNew)
                    {
                        if (HRNetworkManager.bIsServer)
                        {
                            BaseWeapon NewWeapon;
                            HRItemDatabase itemDatabase = ((HRGameInstance)BaseGameInstance.Get).ItemDB;
                            if (itemDatabase
                                && WeaponToRemove.ItemID >= 0
                                && WeaponToRemove.ItemID < itemDatabase.ItemArray.Length)
                            {
                                NewWeapon = Instantiate(
                                    itemDatabase.ItemArray[WeaponToRemove.ItemID].ItemPrefab).GetComponent<BaseWeapon>();

                            }
                            else
                            {
                                Mirror.NetworkIdentity identity = WeaponToRemove.netIdentity;
                                GameObject correspondingPrefab;
                                if (Mirror.NetworkClient.GetPrefab(identity.assetId, out correspondingPrefab))
                                {
                                    NewWeapon = Instantiate(
                                        correspondingPrefab)?.GetComponent<BaseWeapon>();
                                }
                                else
                                {
                                    NewWeapon = null;
                                }
                            }

                            if (NewWeapon)
                            {
                                NewWeapon.transform.position = InventorySlots[Index].SlotWeapon.transform.position;
                                NewWeapon.transform.rotation = InventorySlots[Index].SlotWeapon.transform.rotation;
                                NewWeapon.PreviousOwningInventory = this;
                                NewWeapon.InitializeWeapon();
                                NewWeapon.OwningInventory = null;
                                NewWeapon.SetStackCount(AmountToRemove);

                                if(OwningWeaponManager && OwningWeaponManager.OwningPlayerCharacter.bIsPlayer)
                                {
                                    NewWeapon.SetNumPickupsTracked(Mathf.Min(AmountToRemove, WeaponToRemove.numPickupsTracked));
                                    if (WeaponToRemove.StackCount > WeaponToRemove.numPickupsTracked)
                                        WeaponToRemove.AddToNumPickupsTracked(-NewWeapon.numPickupsTracked);
                                }


                                WeaponToRemove = NewWeapon;
                                NewWeapon.HideWeapon(true);
                                NewWeapon.netIdentity.bSpawnImmediately = true;

                                Mirror.NetworkServer.Spawn(NewWeapon.gameObject);
                            }

                            // Make sure to update the placeable object if we are 
                            // currently placing an object.
                            if ((bDropWeapon || bIsPlacing) && !bDeleteWeapon && OwningWeaponManager)
                            {
                                //TODO: I removed this because it was causing issues placing items on clients. Not sure why it is used.
                                //if (OwningWeaponManager.ItemPlacingManager
                                //    && (OwningWeaponManager.ItemPlacingManager.bIsPlacing))
                                //{
                                //    OwningWeaponManager.ItemPlacingManager.CurrentPlaceableGameObject
                                //        = NewWeapon.PlaceableComponent;
                                //}
                                //else
                                {
                                    if (HRNetworkManager.IsHost() && OwningWeaponManager.ItemPlacingManager)
                                    {
                                        OwningWeaponManager.ItemPlacingManager.SetPlaceable_Server(NewWeapon.gameObject, true, Temp_PlacingPosition,
                                                                                        Temp_PlacingRotation);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    WeaponToRemove.OwningInventory = null;

                    BaseInventorySlot TempSlot = InventorySlots[Index];
                    TempSlot.ClearSlot();
                    UpdateSlot(TempSlot, Index);
                }

                if (bDropWeapon && !bDeleteWeapon)
                {
                    if (DropTransform && bChangeWeaponTransform && !bDeleteWeapon)
                    {
                        WeaponToRemove.transform.SetPositionAndRotation(DropTransform.transform.position, transform.rotation);
                    }

                    WeaponToRemove.DropWeapon(true);

                    if (!HRNetworkManager.IsOffline())
                    {
                        if (HRNetworkManager.IsHost() && Mirror.NetworkServer.active && netIdentity && isServer)
                        {
                            DropWeapon_ClientRpc(WeaponToRemove, WeaponToRemove.transform.position, transform.rotation, bChangeWeaponTransform);
                        }
                    }
                }

                SlotChangedDelegate?.Invoke(this, Index);
            }

            //TryToSaveInventoryToInstance();
            bIsDirty = true;

            if (bDeleteWeapon)
            {
                if (HRNetworkManager.IsHost())
                {
                    if (WeaponToRemove)
                    {
                        if (WeaponToRemove.SaveComponentRef)
                        {
                            WeaponToRemove.SaveComponentRef.SetRemoveFromSaveOnDestroy(true);
                        }
                        Mirror.NetworkServer.Destroy(WeaponToRemove.gameObject);
                    }
                }
            }
            return WeaponToRemove;
        }
        return null;
    }

    private void UpdateSlot(BaseInventorySlot slot, int index)
    {
        if (HRNetworkManager.hasSingleton
            && HRNetworkManager.bIsServer)
        {
            UpdateSlot_Implementation(slot, index);
        }
        else if (Mirror.NetworkServer.active)
        {
            UpdateSlot_Command(slot, index);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void UpdateSlot_Command(BaseInventorySlot slot, int index)
    {
        UpdateSlot_Implementation(slot, index);
    }

    private void UpdateSlot_Implementation(BaseInventorySlot slot, int index)
    {
        InventorySlots[index] = slot;
    }


    [Mirror.ClientRpc]
    public void DropWeapon_ClientRpc(BaseWeapon WeaponToDrop, Vector3 Position, Quaternion Rotation, bool bChangePositionAndRot)
    {
        if (WeaponToDrop)
        {
            WeaponToDrop.gameObject.SetActive(true);
            WeaponToDrop.transform.SetParent(null, true);
            if (bChangePositionAndRot)
                WeaponToDrop.transform.SetPositionAndRotation(Position, Rotation);

            if (!hasAuthority)
                WeaponToDrop.DropWeapon(true);
        }
    }

    public bool RemoveStack(int Index, bool bChangeWeaponTransform = true, bool bDropWeapon = true, bool bDeleteWeapons = false)
    {
        if (RemoveWeapon(Index, -1, bChangeWeaponTransform, bDropWeapon, false, bDeleteWeapons))
        {
            SlotChangedDelegate?.Invoke(this, Index);
            return true;
        }

        return false;
    }

    public bool RemoveFromStack(int Index, int Amount, bool bChangeWeaponTransform = true, bool bDropWeapon = true, bool bDeleteWeapons = false)
    {
        if (InventorySlots[Index].SlotWeapon)
        {
            int AmountToRemove = InventorySlots[Index].SlotWeapon.StackCount < Amount || Amount < 0 ? InventorySlots[Index].SlotWeapon.StackCount : Amount;
            bool bRemoved = RemoveWeapon(Index, Amount, bChangeWeaponTransform, bDropWeapon, false, bDeleteWeapons);

            SlotChangedDelegate?.Invoke(this, Index);

            return bRemoved;
        }

        return false;
    }

    public bool RemoveAmount(int ItemID, int Amount, bool bChangeWeaponTransform = true, bool bDropWeapon = true, bool bDeleteWeapons = false)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon && InventorySlots[i].SlotWeapon.ItemID == ItemID)
            {
                if (InventorySlots[i].SlotWeapon.StackCount > Amount)
                {
                    RemoveFromStack(i, Amount, bChangeWeaponTransform, bDropWeapon, bDeleteWeapons);
                    Amount -= Amount;
                }
                else
                {
                    Amount -= InventorySlots[i].SlotWeapon.StackCount;
                    RemoveFromStack(i, -1, bChangeWeaponTransform, bDropWeapon, bDeleteWeapons);
                }
            }
            if (Amount == 0)
            {
                return true;
            }
        }
        return false;
    }

    //Remove weapon from one inventory and add it to another
    public void InsertInventorySlots(BaseInventory FromInventory, int FromIndex, int ThisIndex, int NumItemsToInsert = -1, bool bAutoFill = false)
    {
        if (FromInventory.bIsClientInstanced)
        {
            BaseWeapon WeaponToInsert = FromInventory.InventorySlots[FromIndex].SlotWeapon;

            if (NumItemsToInsert == -1)
            {
                NumItemsToInsert = WeaponToInsert.StackCount;
            }
            else
            {
                NumItemsToInsert = Mathf.Min(WeaponToInsert.StackCount, NumItemsToInsert);
            }

            WeaponToInsert.SpawnInstancedCopy((InstancedCopy) =>
            {
                InstancedCopy.PreviousOwningInventory = FromInventory;
                AddWeapon(InstancedCopy, ThisIndex, false, bAutoFill, NumItemsToInsert);
            });
            FromInventory.InventoryUI.RefreshSlotUI(FromIndex);
            return;
        }

        if (HRNetworkManager.IsHost())
        {
            InsertInventorySlots_Implementation(FromInventory, FromIndex, ThisIndex, NumItemsToInsert, bAutoFill);
        }
        else
        {
            InsertInventorySlots_Command(FromInventory, FromIndex, ThisIndex, NumItemsToInsert, bAutoFill);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void InsertInventorySlots_Command(BaseInventory FromInventory, int FromIndex, int ThisIndex, int NumItemsToInsert, bool bAutoFill)
    {
        InsertInventorySlots_Implementation(FromInventory, FromIndex, ThisIndex, NumItemsToInsert, bAutoFill);
    }

    private void InsertInventorySlots_Implementation(BaseInventory FromInventory, int FromIndex, int ThisIndex, int NumItemsToInsert, bool bAutoFill = false)
    {
        if (bDoNotAllowInput)
        {
            return;
        }
        BaseWeapon WeaponToInsert = FromInventory.InventorySlots[FromIndex].SlotWeapon;
        if (!WeaponToInsert)
        {
            return;
        }

        int NumOfInventoryInserted = 0;
        if (NumItemsToInsert == -1)
        {
            NumOfInventoryInserted = WeaponToInsert.StackCount;
        }
        else
        {
            NumOfInventoryInserted = Mathf.Min(WeaponToInsert.StackCount, NumItemsToInsert);
        }
        //Add the minimum of the inserted weapon stack count and the space left in the destination stack
        //TODO: Keep this if we want to put as much as possible in a container
        if (!bAutoFill)
            NumOfInventoryInserted = Mathf.Min(NumOfInventoryInserted, InventorySlots[ThisIndex].GetStackLimit() - ((InventorySlots[ThisIndex].SlotWeapon) ? InventorySlots[ThisIndex].SlotWeapon.StackCount : 0));

        if (NumOfInventoryInserted == 0)
        {
            return;
        }
        if (!CanInsertWeapon(WeaponToInsert, ThisIndex, true, bAutoFill, NumOfInventoryInserted, false)) return;

        if (WeaponToInsert)
        {
            BaseWeapon removedWeapon = FromInventory.RemoveWeapon(FromIndex, NumOfInventoryInserted, false, false, true, false, true, false);
            removedWeapon.PreviousOwningInventory = FromInventory;
            AddWeapon(removedWeapon, ThisIndex, false, bAutoFill, NumOfInventoryInserted);

            FromInventory.SlotChangedDelegate?.Invoke(FromInventory, FromIndex);
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    private void InsertInventoryItemAfterInstantiate_Command(int ItemID, int ThisIndex, int NumOfInventoryInserted)
    {
        InsertInventoryItemAfterInstantiate(ItemID, ThisIndex, NumOfInventoryInserted);
    }
    private void InsertInventoryItemAfterInstantiate(int ItemID, int ThisIndex, int NumOfInventoryInserted)
    {
        var Database = ((HRGameInstance)BaseGameInstance.Get).ItemDB;
        var Instance = SpawnItemIntoInventory(Database.ItemArray[ItemID].ItemPrefab);

        var Weapon = Instance.GetComponent<BaseWeapon>();
        Weapon.SetStackCount(NumOfInventoryInserted);

        AddWeapon(Weapon, ThisIndex, false, false, NumOfInventoryInserted);
    }

    //Called when moving item from one slot to another
    public bool SwapInventorySlots(BaseInventory OtherInventory, int OtherIndex, int ThisIndex)
    {
        if (OtherInventory == null || OtherInventory.InventorySlots == null)
        {
            return false;
        }

        BaseInventorySlot OtherSlot = OtherInventory.InventorySlots[OtherIndex];
        BaseInventorySlot ThisSlot = InventorySlots[ThisIndex];

        if (bIsClientInstanced && ThisSlot.SlotWeapon)
        {
            ThisSlot.SlotWeapon.SpawnInstancedCopy((InstancedCopy) =>
            {
                InstancedCopy.PreviousOwningInventory = this;
                OtherInventory.InsertWeapon(InstancedCopy, OtherIndex, bSwappingSlots: true);
            });
            InventoryUI.RefreshSlotUI(ThisIndex);
            return false;
        }
        else if (OtherInventory.bIsClientInstanced && OtherSlot.SlotWeapon)
        {
            OtherSlot.SlotWeapon.SpawnInstancedCopy((InstancedCopy) =>
            {
                InstancedCopy.PreviousOwningInventory = OtherInventory;
                InsertWeapon(InstancedCopy, ThisIndex, bSwappingSlots: true);
            });
            OtherInventory.InventoryUI.RefreshSlotUI(OtherIndex);
            return false;
        }

        BaseWeapon ThisWeapon = ThisSlot.SlotWeapon;
        BaseWeapon OtherWeapon = OtherSlot.SlotWeapon;

        //Return if we don't allow player input and Other Weapon exists
        if (bDoNotAllowInput && OtherWeapon) return false;

        //Check if we can swap the other weapon into this inventory
        if (!CanInsertWeapon(OtherWeapon, ThisIndex, bUseFilter: true, bSwappingSlots: true)) return false;
        //Check if we can swap this weapon into the other inventory
        if (!OtherInventory.CanInsertWeapon(ThisWeapon, OtherIndex, bUseFilter: true, bSwappingSlots: true)) return false;

        //Make the swap
        if (ThisWeapon && OtherWeapon)
            Debug.LogError("BEFORE: " + ThisWeapon.ItemRarity + " " + OtherWeapon.ItemRarity);
        OtherInventory.InsertWeapon(ThisWeapon, OtherIndex, bUseFilter: true, bSwappingSlots: true);
        if (ThisWeapon && OtherWeapon)
            Debug.LogError("Middle: " + ThisWeapon.ItemRarity + " " + OtherWeapon.ItemRarity);
        InsertWeapon(OtherWeapon, ThisIndex, bUseFilter: true, bSwappingSlots: true);
        if (ThisWeapon && OtherWeapon)
            Debug.LogError("COMPLETE: " + ThisWeapon.ItemRarity + " " + OtherWeapon.ItemRarity);

        if (ItemBreakRepair)
        {
            if (OwningWeaponManager)
            {
                ItemBreakRepair.Interact(OwningWeaponManager.gameObject);
            }
            else
            {
                ItemBreakRepair.Interact(null);
            }
        }

        return true;
    }


    public void WeaponPicked(BaseWeapon Weapon)
    {
        OnItemPickedForCustomerDelegate?.Invoke(Weapon);
    }

    public void HighlightSlot(int SlotIndex, InventoryHighlightType HighlightType)
    {
        if (SlotHighlighedDelegate != null)
        {
            SlotHighlighedDelegate(this, SlotIndex, HighlightType);
        }
    }

    public void InventoryUIManuallyClosed()
    {
        InventoryUIManuallyClosedDelegate?.Invoke(this);
    }

    public void InventoryUIToggled(bool bOpen)
    {
        // Randomize contents on open if they haven't been already 
        if (CanRandomize() && bRandomizeOnOpen && bOpen)
        {
            HandleWeaponRandomize(OwningWeapon);
        }
    }

    void MoveSlotToPlayerInventoryAtIndex(int Index, HeroPlayerCharacter Player)
    {
        BaseWeapon SlotWeapon = InventorySlots[Index].SlotWeapon;
        if (!SlotWeapon) return;

        if (!SlotWeapon.bInstancedCopyTaken)
        {
            Player.InventoryManager.MoveWeaponToPlayerInventory(SlotWeapon, Index, this);
        }
    }

    public void PlayerTakeAll()
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (!InventorySlots[i].SlotWeapon) continue;

            if (MoveToPlayerInventoryQueue == null)
            {
                MoveToPlayerInventoryQueue = new Queue<int>();
            }

            MoveToPlayerInventoryQueue.Enqueue(i);
        }

        ProcessTakeAllQueue();
    }

    void ProcessTakeAllQueue()
    {
        if (MoveToPlayerInventoryQueue != null && MoveToPlayerInventoryQueue.Count > 0)
        {
            HeroPlayerCharacter Player = BaseGameInstance.Get.GetLocalPlayerPawn() as HeroPlayerCharacter;

            if (!Player) return;

            int Index = -1;

            while (MoveToPlayerInventoryQueue.Count > 0 && (Index < 0 || Index >= InventorySlots.Count || !InventorySlots[Index].SlotWeapon))
            {
                Index = MoveToPlayerInventoryQueue.Dequeue();
            }

            if (Index >= 0 && Index < InventorySlots.Count && InventorySlots[Index].SlotWeapon)
            {
                MoveSlotToPlayerInventoryAtIndex(Index, Player);
            }
        }
    }

    public bool IsInventoryFull(bool bIgnoreStackCount = false)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (!InventorySlots[i].SlotWeapon || (!bIgnoreStackCount && InventorySlots[i].SlotWeapon.StackCount < Mathf.Min(InventorySlots[i].GetStackLimit(), MaxStackLimit)))
            {
                return false;
            }
        }

        return true;
    }

    // Check if the inventory is full if one stack of an item can go in it
    public bool IsInventoryFull(int itemID)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (!InventorySlots[i].SlotWeapon || (InventorySlots[i].SlotWeapon.ItemID == itemID && InventorySlots[i].SlotWeapon.StackCount < Mathf.Min(InventorySlots[i].GetStackLimit(), MaxStackLimit)))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsInventoryEmpty()
    {
        return NumItems == 0;
    }

    public int FindSlotWithWeapon(BaseWeapon InWeapon)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                if (InventorySlots[i].SlotWeapon == InWeapon)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public int FindSlotWithCategory(List<HRItemCategorySO> categories)
    {
        if(categories != null)
        {
            for (int i = 0; i < InventorySlots.Count; ++i)
            {
                if (InventorySlots[i].SlotWeapon)
                {
                    foreach (var c in categories)
                    {
                        if (InventorySlots[i].SlotWeapon.ItemSizeAndType.IsInCategory(c))
                        {
                            return i;
                        }
                    }
                }
            }
        }

        return -1;
    }

    public int FindFirstSlotNotWithCategory(List<HRItemCategorySO> categories)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                bool bIsValid = true;

                foreach (var c in categories)
                {
                    if (InventorySlots[i].SlotWeapon.ItemSizeAndType.IsInCategory(c))
                    {
                        bIsValid = false;
                        break;
                    }
                }

                if (bIsValid)
                    return i;
            }
        }
        return -1;
    }

    public int FindFirstSlotNotWithCategory(string[] categoryIDs)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                bool bIsValid = true;

                foreach (var c in categoryIDs)
                {
                    if (InventorySlots[i].SlotWeapon.ItemSizeAndType.GetItemCategories().Find(category => category?.CategoryID == c))
                    {
                        bIsValid = false;
                        break;
                    }
                }

                if (bIsValid)
                    return i;
            }
        }
        return -1;
    }

    public int FindSlotWithSize(HRItemSize size)
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                if (InventorySlots[i].SlotWeapon.ItemSizeAndType.ItemSize == size)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public int FindSlotWithEmpty()
    {
        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon == null)
            {
                return i;
            }
        }
        return -1;
    }
    public int GetItemCount(int ItemID)
    {
        int Count = 0;
        if (InventorySlots != null)
        {
            for (int i = 0; i < InventorySlots.Count; ++i)
            {
                if (InventorySlots[i].SlotWeapon)
                {
                    if (InventorySlots[i].SlotWeapon.ItemID == ItemID)
                    {
                        Count += InventorySlots[i].SlotWeapon.StackCount;
                    }
                }
            }
        }

        return Count;
    }

    public int GetRecipeUnlockCount(HRRecipeUnlockData unlockData)
    {
        int Count = 0;
        if (InventorySlots != null)
        {
            for (int i = 0; i < InventorySlots.Count; ++i)
            {
                if (InventorySlots[i].SlotWeapon)
                {
                    HRConsumableRecipeUnlock recipeUnlock = InventorySlots[i].SlotWeapon.GetComponent<HRConsumableRecipeUnlock>();
                    if (recipeUnlock && recipeUnlock.RecipeUnlockData.BookName == unlockData.BookName)
                    {
                        Count += InventorySlots[i].SlotWeapon.StackCount;
                    }
                }
            }
        }

        return Count;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (OwningWeapon)
        {
            OwningWeapon.WeaponAttemptPickupDelegate -= HandleWeaponPickup;
        }

        if (HRNetworkManager.IsHost())
        {
            for (int i = 0; i < InventorySlots.Count; ++i)
            {
                if (InventorySlots[i].SlotWeapon != null && InventorySlots[i].SlotWeapon.gameObject != null)
                {
                    if (bDropItemsOnDestroy)
                    {
                        RemoveWeapon(InventorySlots[i].SlotWeapon, InventorySlots[i].SlotWeapon.StackCount);
                    }
                    else
                    {
                        Mirror.NetworkServer.Destroy(InventorySlots[i].SlotWeapon.gameObject);
                    }
                }
            }
        }

        if (((HRGameInstance)BaseGameInstance.Get))
        {
            HRPhoneManager PhoneManager = ((HRGameInstance)BaseGameInstance.Get).PhoneManager;
            if (PhoneManager)
            {
                if (PhoneManager.MainInventoryUI)
                {
                    if (PhoneManager.MainInventoryUI.OwningInventory == this)
                    {
                        PhoneManager.MainInventoryUI.RemoveCallbacks(this);
                    }
                }

                if (PhoneManager.HotkeyInventoryUI)
                {
                    if (PhoneManager.HotkeyInventoryUI.OwningInventory == this)
                    {
                        PhoneManager.HotkeyInventoryUI.RemoveCallbacks(this);
                    }
                }

                if (PhoneManager.PlayerEquipmentUI)
                {
                    if (PhoneManager.PlayerEquipmentUI.OwningInventory == this)
                    {
                        PhoneManager.PlayerEquipmentUI.RemoveCallbacks(this);
                    }
                }
            }
        }

        InventorySlots.Callback -= HandleInventorySlotsUpdated;
    }

    public void ClearInventory()
    {
        bool bIsDirty = false;
        if (InventorySlots != null && InventorySlots.Count > 0)
        {
            for (int i = 0; i < InventorySlots.Count; ++i)
            {
                if (RemoveStack(i, false, false, true))
                {
                    bIsDirty = true;
                }
            }
        }


        if (bIsDirty)
        {
            //TryToSaveInventoryToInstance();
            bIsDirty = true;
        }
    }

    // 2D array of saved item
    [System.NonSerialized, Ceras.SerializedField]
    public string[] SavedItemIDs;
    [System.NonSerialized, Ceras.SerializedField]
    public int[] SavedItemIDSlotTracker;
    [System.NonSerialized, Ceras.SerializedField]
    public int SavedMaxSlot;

    public void SaveInventory()
    {
        SaveInventory(out SavedItemIDs, out SavedItemIDSlotTracker);
    }

    public void SaveInventory(out string[] outItemIDs, out int[] outItemSlotTracker)
    {
        int NumTotalIndividualItems = 0;

        if (InventorySlots == null)
        {
            Debug.LogError("This shouldn't be null. " + this.gameObject);
            outItemIDs = new string[0];
            outItemSlotTracker = new int[0];
            return;
        }
        SavedItemIDSlotTracker = new int[InventorySlots.Count];

        for (int i = 0; i < InventorySlots.Count; ++i)
        {
            if (InventorySlots[i].SlotWeapon)
            {
                NumTotalIndividualItems += 1;
                SavedItemIDSlotTracker[i] = InventorySlots[i].SlotWeapon.StackCount;
            }
            else
            {
                SavedItemIDSlotTracker[i] = 0;
            }
        }

        SavedItemIDs = new string[NumTotalIndividualItems];

        int CurrentIndex = 0;
        for (int i = 0; i < InventorySlots.Count && CurrentIndex < NumTotalIndividualItems; ++i)
        {
            if (InventorySlots[i].SlotWeapon && InventorySlots[i].SlotWeapon.IDComponent)
            {
                SavedItemIDs[CurrentIndex] = InventorySlots[i].SlotWeapon.IDComponent.GetUniqueID();
                //Debug.Log($"Inventory: {gameObject.name} SavedId {CurrentIndex} {SavedItemIDs[CurrentIndex]}");

                CurrentIndex++;
            }
        }

        outItemIDs = SavedItemIDs;
        outItemSlotTracker = SavedItemIDSlotTracker;
        SavedMaxSlot = MaxSlots;
    }

    public void LoadInventory()
    {
        LoadInventory(SavedItemIDs, SavedItemIDSlotTracker, true, false);
    }

    public void LoadInventory(string[] InItemIDs, int[] InItemSlotTracker, bool bSpawnIfNotFound = false, bool bPlayerLoad = false)
    {
        if (SavedMaxSlot > 0)
        {
            SetMaxSlots(SavedMaxSlot, true);
        }

        if (InItemIDs != null && InItemSlotTracker != null)
        {
            //ClearInventory();

            // Prob should not mix base with HR
            HRGameInstance GameInstance = BaseGameInstance.Get as HRGameInstance;

            if (GameInstance && GameInstance.ItemDB)
            {
                int ItemIDIndexToUse = 0;
                for (int i = 0; i < InItemSlotTracker.Length; ++i)
                {
                    if (InventorySlots.Count <= i && ExpectedSlots <= i)
                    {
                        Debug.Log("What? Inventory is not right size when loading. Wtf");
                        continue;
                    }
                    if (InItemSlotTracker[i] > 0)
                    {
                        int NumItemsToInsert = InItemSlotTracker[i];
                        BaseIDComponent IDComponent;
                        if (InItemIDs[ItemIDIndexToUse] != null)
                        {
                            BaseIDManager.Get.IDMap.TryGetValue(InItemIDs[ItemIDIndexToUse], out IDComponent);
                            if (IDComponent)
                            {
                                BaseWeapon NewWeapon = IDComponent.GetComponent<BaseWeapon>();
                                if (NewWeapon)
                                {
                                    // Setting inactive because by default that's what should happen, and other stuff can set active if necessary after
                                    // like display cases
                                    NewWeapon.gameObject.SetActive(false);

                                    bool bAlreadyAdded = false;
                                    if (InventorySlots[i].SlotWeapon)
                                    {
                                        if (InventorySlots[i].SlotWeapon.netIdentity != NewWeapon.netIdentity)
                                        {
                                            // Destroy any existing weapon in this slot
                                            RemoveWeapon(InventorySlots[i].SlotWeapon, -1, false, false, false, true, false, false);
                                        }
                                        else
                                        {
                                            bAlreadyAdded = true;
                                        }
                                    }
                                    if (!bAlreadyAdded)
                                    {
                                        if (OwningWeaponManager && this == OwningWeaponManager.HotKeyInventory)
                                        {
                                            OwningWeaponManager.AddWeaponStack(NewWeapon, NumItemsToInsert, i, false);
                                        }
                                        else
                                        {
                                            //Debug.Log($"Inventory: {gameObject.name} Loading Slot: {i} with Stack Count: {NumItemsToInsert}");
                                            NewWeapon.SetStackCount(NumItemsToInsert);
                                            InsertWeapon(NewWeapon, i, false, false, NumItemsToInsert);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (bSpawnIfNotFound)
                                {
                                    int ObjectID = -1;
                                    if (bPlayerLoad)
                                    {
                                        ObjectID = HRSaveSystem.Get.GetCurrentInstanceID(HRSaveSystem.Get.PlayerFileInstance, InItemIDs[ItemIDIndexToUse], bPlayerLoad);
                                    }
                                    else
                                    {
                                        ObjectID = HRSaveSystem.Get.GetCurrentInstanceID(HRSaveSystem.Get.CurrentFileInstance, InItemIDs[ItemIDIndexToUse], bPlayerLoad);
                                        // very not good but theres not easy way to call this with bPlayerLoad = true from HandleLoaded function.
                                        // more specifically, this fixes nested inventories in PLAYER inventory not loading since those are loaded with bPlayerLoad = false
                                        if (ObjectID == -1)
                                        {
                                            ObjectID = HRSaveSystem.Get.GetCurrentInstanceID(HRSaveSystem.Get.PlayerFileInstance, InItemIDs[ItemIDIndexToUse], true);
                                        }
                                    }
                                    //Debug.Log($"Inventory: {gameObject.name} SPAWNING: " + InItemIDs[ItemIDIndexToUse] + " " + ObjectID + " " + i + " " + NumItemsToInsert);

                                    if (ObjectID >= 0 && ObjectID < (HRGameInstance.Get as HRGameInstance).ItemDB.ItemArray.Length)
                                    {
                                        if ((HRGameInstance.Get as HRGameInstance).ItemDB.ItemArray[ObjectID].ItemPrefab)
                                        {
                                            if (HRNetworkManager.IsHost())
                                            {
                                                InsertLoadedWeapon_Implementation(bPlayerLoad ? BaseGameInstance.Get.GetLocalPlayerPawn() : null,
                                                    InItemIDs[ItemIDIndexToUse], ObjectID, i, NumItemsToInsert);
                                            }
                                            else
                                            {
                                                Mirror.NetworkClient.RegisterPrefab((HRGameInstance.Get as HRGameInstance).ItemDB.ItemArray[ObjectID].ItemPrefab);
                                                InsertLoadedWeapon_Command(bPlayerLoad ? BaseGameInstance.Get.GetLocalPlayerPawn() : null,
                                                    InItemIDs[ItemIDIndexToUse], ObjectID, i, NumItemsToInsert);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        ItemIDIndexToUse++;

                    }
                    else
                    {
                        if (i < InventorySlots.Count && InventorySlots[i].SlotWeapon != null)
                        {
                            BaseWeapon RemovedWeapon = InventorySlots[i].SlotWeapon;
                            RemoveStack(i, false, false);
                            if (RemovedWeapon)
                            {
                                if (RemovedWeapon.OwningInteractable)
                                {
                                    RemovedWeapon.OwningInteractable.SetInteractionCollisionEnabled(true);
                                }

                                RemovedWeapon.SetMeshCollisionEnabled(true);
                            }
                        }
                    }
                }
            }
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    private void InsertLoadedWeapon_Command(BaseScripts.BasePawn Target, string SaveID, int ItemID, int Index, int NumItemsToInsert)
    {
        InsertLoadedWeapon_Implementation(Target, SaveID, ItemID, Index, NumItemsToInsert);
    }

    private void InsertLoadedWeapon_Implementation(BaseScripts.BasePawn Target, string SaveID, int ItemID, int Index, int NumItemsToInsert)
    {
        if (ItemID < 0 || ItemID >= ((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemArray.Length)
        {
            Debug.LogError("Attempting to insert out of range ItemID " + ItemID + " into inventory " + gameObject.name);
            return;
        }
        var ToSpawn = Instantiate((HRGameInstance.Get as HRGameInstance).ItemDB.ItemArray[ItemID].ItemPrefab,
            Target == null ? this.transform.position : Target.transform.position, Target == null ? this.transform.rotation : Target.transform.rotation);
        BaseWeapon weapon = ToSpawn.GetComponent<BaseWeapon>();
        weapon.SaveComponentRef.bManualSetup = true;

        if (Index >= InventorySlots.Count)
        {
            Debug.LogError("Attempting to insert weapon " + ItemID + " into invalid index: " + Index + ", " + gameObject.name + " max inventory slots: " + InventorySlots.Count);
            return;
        }

        var Weapon = ToSpawn.GetComponent<BaseWeapon>();
        Weapon.OwningInventory = this;

        if (Mirror.NetworkServer.active)
        {
            weapon.netIdentity.bSpawnImmediately = true;
            Mirror.NetworkServer.Spawn(ToSpawn);
        }

        if (Weapon.ProjectilePhysics)
            Weapon.ProjectilePhysics.SetEnabled(false);
        Weapon.SetStackCount(NumItemsToInsert);

        Weapon.SaveComponentRef.bLoadingFromPlayer = true;
        uint ID = Weapon.netId;

        //Debug.LogError("ON THE HOST, THE ID OF THE CLIENT'S ITEM IS: " + ID);
        ToSpawn.gameObject.SetActive(false);

        if (netIdentity)
        {
            HideLoadedWeapon_ClientRpc(ToSpawn);
        }

        if (Target == null || Target.connectionToClient != null)
        {
            bool bAlreadyAdded = false;
            if (InventorySlots[Index].SlotWeapon)
            {
                if (InventorySlots[Index].SlotWeapon.netIdentity != Weapon.netIdentity)
                {
                    RemoveWeapon(InventorySlots[Index].SlotWeapon, -1, false, false, false, true, false, false);
                }
                else
                {
                    bAlreadyAdded = true;
                }
            }
            if (Target != null && !bAlreadyAdded)
            {
                if (netIdentity)
                {
                    InsertLoadedWeapon_TargetRpc(Target.connectionToClient, ID, SaveID, Index, NumItemsToInsert);
                }
            }
            else
            {
                HRSaveSystem.Get.HandleInventoryWeaponLoaded(this, ID, SaveID, Index, NumItemsToInsert);
            }
        }
    }


    [Mirror.ClientRpc(excludeOwner = true)]
    private void HideLoadedWeapon_ClientRpc(GameObject Item)
    {
        if (Item)
            Item.SetActive(false);
    }


    [Mirror.TargetRpc]
    private void InsertLoadedWeapon_TargetRpc(Mirror.NetworkConnection Target, uint ServerItemID, string SaveID, int Index, int NumToInsert)
    {
        // This is going to be absolutely disgusting
        HRSaveSystem.Get.HandleInventoryWeaponLoaded(this, ServerItemID, SaveID, Index, NumToInsert);
    }


    public void AddItemToLoadCache(BaseWeapon Item, int slot, int stack)
    {
        if (!LoadedWeaponCache.ContainsKey(slot))
        {
            LoadedWeaponCache.Add(slot, Item);
            LoadedWeaponCountCache.Add(slot, stack);
        }
        else
        {
            LoadedWeaponCache[slot] = Item;
            LoadedWeaponCountCache[slot] = stack;
        }
    }


    public void RestoreCachedItems(int Count)
    {
        for (int i = 0; i < Count; ++i)
        {
            if (LoadedWeaponCache.ContainsKey(i))
            {
                InsertWeapon(LoadedWeaponCache[i], i, false, false, LoadedWeaponCountCache[i]);
                LoadedWeaponCache[i].SetStackCount(LoadedWeaponCountCache[i]);
                LoadedWeaponCache[i].HideWeapon(true);
            }
        }
    }

    /// <summary>
    /// Locks or unlocks inventory slots starting from StartIndex to EndIndex (inclusive).
    /// </summary>
    /// <param name="StartIndex">Starting index of slot to lock in InventorySlots array (inclusive).</param>
    /// <param name="EndIndex">Ending index of slot to lock in InventorySlots array (inclusive).</param>
    /// <param name="bLocked">Should be set to locked or not</param>
    public void SetSlotsLocked(int StartIndex, int EndIndex, bool bLocked)
    {
        if (StartIndex < 0 || StartIndex >= InventorySlots.Count || EndIndex < StartIndex || EndIndex >= InventorySlots.Count)
        {
            return;
        }
        for (int i = StartIndex; i <= EndIndex; i++)
        {
            BaseInventorySlot TempSlot = InventorySlots[i];
            TempSlot.bLocked = bLocked;
            InventorySlots[i] = TempSlot;
        }
    }

    public void HandlePreSave()
    {
        SaveInventory();
    }

    public void HandleLoaded()
    {
        LoadInventory();
    }

    public void HandleSaved()
    {

    }

    public void HandleReset()
    {

    }

    HRSaveComponent OwningSaveComponent = null;
    int OwningSaveID;
    int OwningAuxID;

    public void HandleSaveComponentInitialize(HRSaveComponent InSaveComponent, int ComponentID, int AuxID)
    {
        OwningSaveComponent = InSaveComponent;
        OwningSaveID = ComponentID;
        OwningAuxID = AuxID;
    }

    public void TryToSaveInventoryToInstance()
    {
        if (OwningSaveComponent)
        {
            OwningSaveComponent.SaveComponent(OwningSaveID, OwningAuxID);
        }
    }

    // TODO have ingventories save dynamically
    public bool IsSaveDirty()
    {
        return bIsDirty;
    }
}