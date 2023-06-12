using PixelCrushers;
using PixelCrushers.QuestMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseInventoryTrigger : BaseTrigger
{
    private bool initialized;
    public enum PlayerInteractMode { All, LocalOnly };
    [Tooltip("How this trigger should interact with players.")]
    public PlayerInteractMode PlayerTriggerInteractMode = PlayerInteractMode.All;

    [System.Serializable]
    public class BaseInventoryTriggerEvent
    {
        public delegate void BaseInventoryTriggerEventSignature(BaseInventoryTriggerEvent InTrigger, bool bComplete);
        public BaseInventoryTriggerEventSignature OnEventsComplete;

        [System.Serializable]
        public struct BaseInventoryContentsTriggerEvent
        {
            [SerializeField]
            public bool runOnlyOnHost;
            public int ItemID;
            public int ItemCount;
            public bool RequireFewerInstead;
            [HideInInspector]
            public BaseChecklistItemUI ChecklistItem;
            [HideInInspector]
            public int StartingAmount;
            public string ChecklistText;
            public bool bComplete;

            public BaseScriptingEvent InventoryContentsTriggerEvent;
        }

        [System.Serializable]
        public struct BaseInventorySpecificEvent
        {
            // If we are listening for adding/removing specific weapons.
            [SerializeField]
            public bool runOnlyOnHost;
            public bool TriggerWhenRemovedInsteadOfAdded;
            [HideInInspector]
            public BaseChecklistItemUI ChecklistItem;
            public string ChecklistText;
            public BaseWeapon[] SpecificWeapons;
            public bool bComplete;

            public BaseScriptingEvent InventorySpecificTriggerEvent;
        }

        public List<BaseInventory> InventoriesToBindTo;
        public List<BaseInventory> PlayerInventories;
        public List<BaseItemPlacingManager> PlacingManagers;
        public bool bComplete;

        // When money goes from below this threshold to above it, it will trigger these.
        public BaseInventoryContentsTriggerEvent[] InventoryContentsTriggerEvent;
        public BaseInventorySpecificEvent[] InventorySpecificGameObjectTriggerEvent;
        public BaseScriptingEvent CompletedEvent;
        public BaseScriptingEvent InventoryFullTriggerEvent;
        public BaseScriptingEvent InventoryEmptyTriggerEvent;

        public bool BindPlayersToEvent = true;
        public bool bWaitForPlayerPostStart = false;
        public enum BaseInventoryType { Main, Hotkey, Equipment };
        [Tooltip("Inventory types to look for.")]
        public BaseInventoryType[] DefaultPlayerInventoryTypesToBindTo;

        public bool bIsGlobalQuest;

        bool bInitialized = false;
        public void Initialize()
        {
            if (!bInitialized)
            { 
                if (InventoriesToBindTo != null)
                {
                    for (int i = 0; i < InventoriesToBindTo.Count; ++i)
                    {
                        if (InventoriesToBindTo[i])
                        {
                            InventoriesToBindTo[i].WeaponChangedDelegate -= HandleInventoryChanged;
                            InventoriesToBindTo[i].WeaponChangedDelegate += HandleInventoryChanged;
                            InventoriesToBindTo[i].WeaponStackCountChangedDelegate -= HandleStackCountChanged;
                            InventoriesToBindTo[i].WeaponStackCountChangedDelegate += HandleStackCountChanged;
                            CheckInventoryContentsTrigger();
                        }
                    }
                }
                
                bInitialized = true;
            }
        }

        // In case and item gets placed down directly from a container, i.e. putting the research table down from the crafting bench
        public void HandleWeaponPlaced(BaseItemPlacingManager placingManager, GameObject placeableObject)
        {
            if (placeableObject)
            {
                BaseWeapon weapon = placeableObject.GetComponent<BaseWeapon>();
                if (weapon)
                {
                    int itemID = weapon.ItemID;
                    for (int i = 0; i < InventoryContentsTriggerEvent.Length; ++i)
                    {
                        if ((InventoryContentsTriggerEvent[i].ItemID < 0 || InventoryContentsTriggerEvent[i].ItemID == itemID) && InventoryContentsTriggerEvent[i].ItemCount == 1)
                        {
                            if (!InventoryContentsTriggerEvent[i].bComplete)
                            {
                                InventoryContentsTriggerEvent[i].InventoryContentsTriggerEvent.OneShotEvent?.Invoke();
                            }
                            InventoryContentsTriggerEvent[i].InventoryContentsTriggerEvent.RepeatedEvent?.Invoke();
                            InventoryContentsTriggerEvent[i].bComplete = true;
                            if (InventoryContentsTriggerEvent[i].ChecklistItem)
                            {
                                InventoryContentsTriggerEvent[i].ChecklistItem.SetComplete(true);
                            }
                        }
                    }
                    for (int i = 0; i < InventorySpecificGameObjectTriggerEvent.Length; ++i)
                    {
                        if (InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons.Length == 1 
                            && InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons[0] == weapon)
                        {
                            if (!InventorySpecificGameObjectTriggerEvent[i].bComplete)
                            {
                                InventorySpecificGameObjectTriggerEvent[i].InventorySpecificTriggerEvent.OneShotEvent?.Invoke();
                            }
                            InventorySpecificGameObjectTriggerEvent[i].InventorySpecificTriggerEvent.RepeatedEvent?.Invoke();
                            InventorySpecificGameObjectTriggerEvent[i].bComplete = true;
                            if (InventorySpecificGameObjectTriggerEvent[i].ChecklistItem)
                            {
                                InventorySpecificGameObjectTriggerEvent[i].ChecklistItem.SetComplete(true);
                            }
                        }
                    }
                }
            }
            if (CheckAllEventsComplete())
            {
                bComplete = true;
                CompletedEvent.FireEvents();
                OnEventsComplete?.Invoke(this, true);
            }
            else
            {
                bComplete = false;
            }
        }


        public void HandleStackCountChanged(BaseInventory InInventory, BaseWeapon Weapon, int OldAmount, int NewAmount)
        {
            var GameInstance = HRGameInstance.Get as HRGameInstance;

            if (!GameInstance || !GameInstance.GetLocalHeroPlayerCharacter())
            {
                return;
            }

            //Send messages (handled by quest counters)
            if (Weapon)
            {
                // Return if weapon is not in ID list
                bool bIsInList = false;
                foreach (BaseInventoryContentsTriggerEvent e in InventoryContentsTriggerEvent)
                {
                    if (Weapon.ItemID == e.ItemID)
                    {
                        bIsInList = true;
                        break;
                    }
                }
                if (!bIsInList) return;

                if (bIsGlobalQuest)
                {
                    MessageSystem.SendMessage(this, HRQuestMessages.ItemCountChangedGlobal, Weapon.ItemID.ToString(), GetItemCountInInventories(Weapon.ItemID));
                }
                else
                {
                    MessageSystem.SendMessage(this, HRQuestMessages.ItemCountChanged, Weapon.ItemID.ToString(), GetItemCountInInventories(Weapon.ItemID));
                }
            }

            CheckInventoryContentsTrigger();

            for (int i = 0; i < InventorySpecificGameObjectTriggerEvent.Length; ++i)
            {
                if (InventorySpecificGameObjectTriggerEvent[i].runOnlyOnHost
                    && !HRNetworkManager.IsHost())
                {
                    continue;
                }

                bool bContainsAllSpecificGameObjects = true;
                int SpecificObjectCount = 0;
                // Check to see if the inventory contains all the gameobjects
                for (int j = 0; j < InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons.Length; ++j)
                {
                    if (!InventorySpecificGameObjectTriggerEvent[i].TriggerWhenRemovedInsteadOfAdded)
                    {
                        if (InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons[j] == null || !CheckInventoryBound(InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons[j].OwningInventory))
                        {
                            bContainsAllSpecificGameObjects = false;
                        }
                        else
                        {
                            SpecificObjectCount++;
                        }
                    }
                    else
                    {
                        if (InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons[j] != null && CheckInventoryBound(InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons[j].OwningInventory))
                        {
                            bContainsAllSpecificGameObjects = false;
                        }
                        else
                        {
                            SpecificObjectCount++;
                        }
                    }
                }

                // Deprecated
                if (InventorySpecificGameObjectTriggerEvent[i].ChecklistItem && InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons.Length > 1)
                {
                    InventorySpecificGameObjectTriggerEvent[i].ChecklistItem.SetProgress((float)SpecificObjectCount / InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons.Length);
                }

                if (bContainsAllSpecificGameObjects)
                {
                    if (!InventorySpecificGameObjectTriggerEvent[i].bComplete)
                    {
                        InventorySpecificGameObjectTriggerEvent[i].InventorySpecificTriggerEvent.OneShotEvent.Invoke();
                    }

                    InventorySpecificGameObjectTriggerEvent[i].bComplete = true;
                    InventorySpecificGameObjectTriggerEvent[i].InventorySpecificTriggerEvent.RepeatedEvent.Invoke();

                    // Deprecated
                    if (InventorySpecificGameObjectTriggerEvent[i].ChecklistItem)
                    {
                        InventorySpecificGameObjectTriggerEvent[i].ChecklistItem.SetComplete(true);
                    }
                }
                else
                {
                    if (InventorySpecificGameObjectTriggerEvent[i].bComplete)
                    {
                        OnEventsComplete?.Invoke(this, false);
                    }
                    InventorySpecificGameObjectTriggerEvent[i].bComplete = false;

                    // Deprecated
                    if (InventorySpecificGameObjectTriggerEvent[i].ChecklistItem)
                    {
                        InventorySpecificGameObjectTriggerEvent[i].ChecklistItem.SetComplete(false);
                    }
                }
            }

            if (CheckAllEventsComplete())
            {
                bComplete = true;
                CompletedEvent.FireEvents();
                OnEventsComplete?.Invoke(this, true);
            }
            else
            {
                bComplete = false;
            }
        }


        public void HandleInventoryChanged(BaseInventory InInventory, int Index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
        {
            var GameInstance = HRGameInstance.Get as HRGameInstance;

            if (!GameInstance || !GameInstance.GetLocalHeroPlayerCharacter())
            {
                return;
            }

            //Send messages (handled by quest counters)
            if (NewWeapon)
            {
                // Return if weapon is not in ID list
                bool bIsInList = false;
                foreach (BaseInventoryContentsTriggerEvent e in InventoryContentsTriggerEvent)
                {
                    if (NewWeapon.ItemID == e.ItemID)
                    {
                        bIsInList = true;
                        break;
                    }
                }
                if (!bIsInList) return;

                if (bIsGlobalQuest)
                {
                    MessageSystem.SendMessage(this, HRQuestMessages.ItemCountChangedGlobal, NewWeapon.ItemID.ToString(), GetItemCountInInventories(NewWeapon.ItemID));
                }
                else
                {
                    MessageSystem.SendMessage(this, HRQuestMessages.ItemCountChanged, NewWeapon.ItemID.ToString(), GetItemCountInInventories(NewWeapon.ItemID));
                }
            }
            if (OldWeapon)
            {
                // Return if weapon is not in ID list
                bool bIsInList = false;
                foreach (BaseInventoryContentsTriggerEvent e in InventoryContentsTriggerEvent)
                {
                    if (OldWeapon.ItemID == e.ItemID)
                    {
                        bIsInList = true;
                        break;
                    }
                }
                if (!bIsInList) return;

                if (bIsGlobalQuest)
                {
                    MessageSystem.SendMessage(this, HRQuestMessages.ItemCountChangedGlobal, OldWeapon.ItemID.ToString(), GetItemCountInInventories(OldWeapon.ItemID));
                }
                else
                {
                    MessageSystem.SendMessage(this, HRQuestMessages.ItemCountChanged, OldWeapon.ItemID.ToString(), GetItemCountInInventories(OldWeapon.ItemID));
                }
            }

            CheckInventoryContentsTrigger();

            for (int i = 0; i < InventorySpecificGameObjectTriggerEvent.Length; ++i)
            {
                if(InventorySpecificGameObjectTriggerEvent[i].runOnlyOnHost
                    && !HRNetworkManager.IsHost())
                {
                    continue;
                }

                bool bContainsAllSpecificGameObjects = true;
                int SpecificObjectCount = 0;
                // Check to see if the inventory contains all the gameobjects
                for (int j = 0; j < InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons.Length; ++j)
                {
                    if (!InventorySpecificGameObjectTriggerEvent[i].TriggerWhenRemovedInsteadOfAdded)
                    {
                        if (InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons[j] == null || !CheckInventoryBound(InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons[j].OwningInventory))
                        {
                            bContainsAllSpecificGameObjects = false;
                        }
                        else
                        {
                            SpecificObjectCount++;
                        }
                    }
                    else
                    {
                        if (InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons[j] != null && CheckInventoryBound(InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons[j].OwningInventory))
                        {
                            bContainsAllSpecificGameObjects = false;
                        }
                        else
                        {
                            SpecificObjectCount++;
                        }
                    }
                }

                // Deprecated
                if (InventorySpecificGameObjectTriggerEvent[i].ChecklistItem && InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons.Length > 1)
                {
                    InventorySpecificGameObjectTriggerEvent[i].ChecklistItem.SetProgress((float)SpecificObjectCount / InventorySpecificGameObjectTriggerEvent[i].SpecificWeapons.Length);
                }
                
                if (bContainsAllSpecificGameObjects)
                {
                    if (!InventorySpecificGameObjectTriggerEvent[i].bComplete)
                    {
                        InventorySpecificGameObjectTriggerEvent[i].InventorySpecificTriggerEvent.OneShotEvent.Invoke();
                    }

                    InventorySpecificGameObjectTriggerEvent[i].bComplete = true;
                    InventorySpecificGameObjectTriggerEvent[i].InventorySpecificTriggerEvent.RepeatedEvent.Invoke();

                    // Deprecated
                    if (InventorySpecificGameObjectTriggerEvent[i].ChecklistItem)
                    {
                        InventorySpecificGameObjectTriggerEvent[i].ChecklistItem.SetComplete(true);
                    }
                }
                else
                {
                    if (InventorySpecificGameObjectTriggerEvent[i].bComplete)
                    {
                        OnEventsComplete?.Invoke(this, false);
                    }
                    InventorySpecificGameObjectTriggerEvent[i].bComplete = false;

                    // Deprecated
                    if (InventorySpecificGameObjectTriggerEvent[i].ChecklistItem)
                    {
                        InventorySpecificGameObjectTriggerEvent[i].ChecklistItem.SetComplete(false);
                    }
                }
            }

            if (CheckAllEventsComplete())
            {
                bComplete = true;
                CompletedEvent.FireEvents();
                OnEventsComplete?.Invoke(this, true);
            }
            else
            {
                bComplete = false;
            }
        }

        bool CheckAllEventsComplete()
        {
            for (int i = 0; i < InventoryContentsTriggerEvent.Length; ++i)
            {
                if (!InventoryContentsTriggerEvent[i].bComplete)
                {
                    return false;
                }
            }
            for (int i = 0; i < InventorySpecificGameObjectTriggerEvent.Length; ++i)
            {
                if (!InventorySpecificGameObjectTriggerEvent[i].bComplete)
                {
                    return false;
                }
            }
            return true;
        }

        public void ResetEvents()
        {
            for (int i = 0; i < InventoryContentsTriggerEvent.Length; ++i)
            {
                InventoryContentsTriggerEvent[i].bComplete = false;
            }
            for (int i = 0; i < InventorySpecificGameObjectTriggerEvent.Length; ++i)
            {
                InventorySpecificGameObjectTriggerEvent[i].bComplete = false;
            }
            bComplete = false;
        }

        public void CheckInventoryContentsTrigger()
        {
            for (int i = 0; i < InventoryContentsTriggerEvent.Length; ++i)
            {
                CheckInventoryContentTrigger(i);
            }

            if (CheckInventoriesFull())
            {
                InventoryFullTriggerEvent.FireEvents();
            }
            else if (CheckInventoriesEmpty())
            {
                InventoryEmptyTriggerEvent.FireEvents();
            }
        }

        void CheckInventoryContentTrigger(int Index)
        {
            if(InventoryContentsTriggerEvent[Index].runOnlyOnHost
                && !HRNetworkManager.IsHost())
            {
                return;
            }

            int NumItems = CheckInventoriesTotal(Index);

            if (InventoryContentsTriggerEvent[Index].RequireFewerInstead)
            {
                if (!bInitialized)
                {
                    InventoryContentsTriggerEvent[Index].StartingAmount = NumItems;
                }
                if (InventoryContentsTriggerEvent[Index].ChecklistItem && bInitialized)
                {
                    InventoryContentsTriggerEvent[Index].ChecklistItem.SetProgress(1 - ((float)NumItems/InventoryContentsTriggerEvent[Index].StartingAmount));
                }
                if (NumItems < InventoryContentsTriggerEvent[Index].ItemCount)
                {
                    InventoryContentsTriggerEvent[Index].bComplete = true;
                    if (InventoryContentsTriggerEvent[Index].ChecklistItem)
                    {
                        InventoryContentsTriggerEvent[Index].ChecklistItem.SetComplete(true);
                    }
                }
                else
                {
                    if (InventoryContentsTriggerEvent[Index].bComplete)
                    {
                        OnEventsComplete?.Invoke(this, false);
                        if (InventoryContentsTriggerEvent[Index].ChecklistItem)
                        {
                            InventoryContentsTriggerEvent[Index].ChecklistItem.SetComplete(false);
                        }
                    }
                    InventoryContentsTriggerEvent[Index].bComplete = false;
                }
            }
            else
            {
                // Deprecated
                if (InventoryContentsTriggerEvent[Index].ChecklistItem && InventoryContentsTriggerEvent[Index].ItemCount > 0)
                {
                    InventoryContentsTriggerEvent[Index].ChecklistItem.SetProgress((float)NumItems / InventoryContentsTriggerEvent[Index].ItemCount);
                }
                if (NumItems >= InventoryContentsTriggerEvent[Index].ItemCount)
                {
                    if (!InventoryContentsTriggerEvent[Index].bComplete)
                    {
                        InventoryContentsTriggerEvent[Index].InventoryContentsTriggerEvent.OneShotEvent.Invoke();
                    }

                    InventoryContentsTriggerEvent[Index].bComplete = true;
                    InventoryContentsTriggerEvent[Index].InventoryContentsTriggerEvent.RepeatedEvent.Invoke();

                    // Deprecated
                    if (InventoryContentsTriggerEvent[Index].ChecklistItem)
                    {
                        InventoryContentsTriggerEvent[Index].ChecklistItem.SetComplete(true);
                    }
                }
                else 
                {
                    if (InventoryContentsTriggerEvent[Index].bComplete)
                    {
                        OnEventsComplete?.Invoke(this, false);

                        // Deprecated
                        if (InventoryContentsTriggerEvent[Index].ChecklistItem)
                        {
                            InventoryContentsTriggerEvent[Index].ChecklistItem.SetComplete(false);
                        }
                    }
                    InventoryContentsTriggerEvent[Index].bComplete = false;
                }
            }
        }

        bool CheckInventoryBound(BaseInventory InInventory)
        {
            for (int i = 0; i < InventoriesToBindTo.Count; ++i)
            {
                if (InInventory == InventoriesToBindTo[i])
                {
                    return true;
                }
            }
            return false;
        }

        bool CheckInventoriesFull()
        {
            for (int i = 0; i < InventoriesToBindTo.Count; ++i)
            {
                if (InventoriesToBindTo[i])
                {
                    if (InventoriesToBindTo[i].NumItems < InventoriesToBindTo[i].UnlockedSlots)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        bool CheckInventoriesEmpty()
        {
            for (int i = 0; i < InventoriesToBindTo.Count; ++i)
            {
                if (InventoriesToBindTo[i])
                {
                    if (InventoriesToBindTo[i].NumItems > 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        int CheckInventoriesTotal(int Index)
        {
            int NumItems = 0;
            for (int k = 0; k < InventoriesToBindTo.Count; ++k)
            {
                if (InventoriesToBindTo[k] && InventoriesToBindTo[k].InventorySlots != null)
                {
                    for (int i = 0; i < InventoriesToBindTo[k].InventorySlots.Count; ++i)
                    {
                        if (InventoriesToBindTo[k].InventorySlots[i].SlotWeapon)
                        {
                            if (InventoryContentsTriggerEvent[Index].ItemID < 0 || InventoriesToBindTo[k].InventorySlots[i].SlotWeapon.ItemID == InventoryContentsTriggerEvent[Index].ItemID ||
                                InventoriesToBindTo[k].InventorySlots[i].SlotWeapon.ItemID == -1)
                            {
                                NumItems += InventoriesToBindTo[k].InventorySlots[i].SlotWeapon.StackCount;
                            }
                        }
                    }
                }
            }
            return NumItems;
        }

        //Calculate number of this item in inventory
        public int GetItemCountInInventories(int ItemID)
        {
            int NumItems = 0;
            for (int k = 0; k < InventoriesToBindTo.Count; ++k)
            {
                if (InventoriesToBindTo[k] && InventoriesToBindTo[k].InventorySlots != null)
                {
                    if (InventoriesToBindTo[k].OwningWeaponManager && InventoriesToBindTo[k].OwningWeaponManager.OwningPlayerCharacter)
                    {
                        // ONLY COUNT INVENTORIES THAT BELONG TO LOCAL PLAYER
                        HRPlayerController localController = (HRPlayerController)BaseGameInstance.Get.GetLocalPlayerController();
                        HeroPlayerCharacter character = (HeroPlayerCharacter)InventoriesToBindTo[k].OwningWeaponManager.OwningPlayerCharacter;
                        HRPlayerController inventoryController = (HRPlayerController)character.PlayerController;
                        if (localController == inventoryController || bIsGlobalQuest)
                        {
                            for (int i = 0; i < InventoriesToBindTo[k].InventorySlots.Count; ++i)
                            {
                                if (InventoriesToBindTo[k].InventorySlots[i].SlotWeapon)
                                {
                                    if (InventoriesToBindTo[k].InventorySlots[i].SlotWeapon.ItemID == ItemID)
                                    {
                                        NumItems += InventoriesToBindTo[k].InventorySlots[i].SlotWeapon.StackCount;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return NumItems;
        }

        public int GetNumPickupsNotTrackedInInventories(int ItemID)
        {
            int NumPickupsTracked = 0;
            int NumItems = 0;

            for (int k = 0; k < InventoriesToBindTo.Count; ++k)
            {
                if (InventoriesToBindTo[k] && InventoriesToBindTo[k].InventorySlots != null)
                {
                    for (int i = 0; i < InventoriesToBindTo[k].InventorySlots.Count; ++i)
                    {
                        if (InventoriesToBindTo[k].InventorySlots[i].SlotWeapon)
                        {
                            if (InventoriesToBindTo[k].InventorySlots[i].SlotWeapon.ItemID == ItemID)
                            {
                                BaseWeapon weapon = InventoriesToBindTo[k].InventorySlots[i].SlotWeapon;
                                NumPickupsTracked += weapon.numPickupsTracked;
                                NumItems += weapon.StackCount;

                                weapon.SetNumPickupsTracked(weapon.StackCount);
                            }
                        }
                    }
                }
            }
            return NumItems - NumPickupsTracked;
        }
    }

    public struct PlayerPawnInventoryData
    {
        public BaseInventory MainInventory;
        public BaseInventory HotkeyInventory;
        public BaseInventory EquipmentInventory;
    }

    public bool bOrMode;
    public BaseInventoryTriggerEvent[] InventoryTriggerEvents;
    public BaseScriptingEvent AllTriggersCompleteEvent;

    // TODO: Make this not a sync dictionary later
    private Dictionary<BaseScripts.BasePawn, PlayerPawnInventoryData> PlayerPawnInventories = new Dictionary<BaseScripts.BasePawn, PlayerPawnInventoryData>();


    void HandleEventsComplete(BaseInventoryTriggerEvent InTrigger, bool bComplete)
    {
        if (bComplete)
        {
            if (CheckAllTriggersComplete() || bOrMode)
            {
                OnCompleteDelegate?.Invoke(this, true);
                AllTriggersCompleteEvent.FireEvents();
            }
        }
    }

    bool CheckAllTriggersComplete()
    {
        for (int i = 0; i < InventoryTriggerEvents.Length; ++i)
        {
            if (!InventoryTriggerEvents[i].bComplete)
            {
                return false;
            }
        }
        return true;
    }

    void ResetAllEvents()
    {
        for (int i = 0; i < InventoryTriggerEvents.Length; ++i)
        {
            InventoryTriggerEvents[i].ResetEvents();
        }
    }

    public override void SetEnabled(bool bEnabled, bool bRemoveFromList = false)
    {
        // Checklist is deprecated

        /*BaseChecklistUI ChecklistUI = null;
        if (Checklist)
        {
            ChecklistUI = Checklist.GetComponent<BaseChecklistUI>();
        }
        if (ChecklistUI == null)
        {
            ChecklistUI = ((HRPlayerController)BaseGameInstance.Get.GetFirstPawn()?.PlayerController)?.PlayerUI?.ChecklistUI;
            if(ChecklistUI == null)
            {
                return;
            }
        }*/

        // Should be true for every event. Change exception cases here if necessary.
        foreach (var InventoryTriggerEvent in InventoryTriggerEvents)
        {
            InventoryTriggerEvent.BindPlayersToEvent = true;
        }

        for (int i = 0; i < InventoryTriggerEvents.Length; ++i)
        {
            if (bEnabled)
            {
                if(InventoryTriggerEvents[i] != null)
                {
                    InventoryTriggerEvents[i].Initialize();
                    InventoryTriggerEvents[i].OnEventsComplete += HandleEventsComplete;
                }
            }
        }

        if (bEnabled && !initialized)
        {
            HRGameInstance.Get.StartCoroutine(WaitForOwningPlayer());
            initialized = true;
        }
    }

    void OnPlayersJoinOrLeave()
    {
        // Use this function to manage players

        foreach(var InventoryTriggerEvent in InventoryTriggerEvents)
        {
            if (InventoryTriggerEvent.InventoriesToBindTo == null)
            {
                InventoryTriggerEvent.InventoriesToBindTo = new List<BaseInventory>(3);
            }

            if (InventoryTriggerEvent.PlayerInventories == null)
            {
                InventoryTriggerEvent.PlayerInventories = new List<BaseInventory>(3);
            }

            InventoryTriggerEvent.PlayerInventories.Clear();
        }

        // Add player inventories
        foreach (var InventoryTriggerEvent in InventoryTriggerEvents)
        {
            if (!InventoryTriggerEvent.BindPlayersToEvent) { continue; }

            foreach (var Key in PlayerPawnInventories.Keys)
            {
                if (Key == null) { continue; }

                var data = PlayerPawnInventories[Key];

                bool UseMain = false, UseEquipment = false, UseHotkey = false;

                for (int i = 0; i < InventoryTriggerEvent.DefaultPlayerInventoryTypesToBindTo.Length; ++i)
                {
                    switch (InventoryTriggerEvent.DefaultPlayerInventoryTypesToBindTo[i])
                    {
                        case BaseInventoryTriggerEvent.BaseInventoryType.Main:
                            UseMain = true;
                            break;
                        case BaseInventoryTriggerEvent.BaseInventoryType.Equipment:
                            UseEquipment = true;
                            break;
                        case BaseInventoryTriggerEvent.BaseInventoryType.Hotkey:
                            UseHotkey = true;
                            break;
                    }
                }

                if ((PlayerTriggerInteractMode == PlayerInteractMode.All
                    || (PlayerTriggerInteractMode == PlayerInteractMode.LocalOnly && Key == HRNetworkManager.Get.LocalPlayerController.PlayerPawn)))
                {
                    if (data.MainInventory && UseMain)
                    {
                        if (!InventoryTriggerEvent.PlayerInventories.Contains(data.MainInventory))
                            InventoryTriggerEvent.PlayerInventories.Add(data.MainInventory);
                        if(!InventoryTriggerEvent.InventoriesToBindTo.Contains(data.MainInventory))
                            InventoryTriggerEvent.InventoriesToBindTo.Add(data.MainInventory);
                        data.MainInventory.WeaponChangedDelegate -= InventoryTriggerEvent.HandleInventoryChanged;
                        data.MainInventory.WeaponChangedDelegate += InventoryTriggerEvent.HandleInventoryChanged;
                        data.MainInventory.WeaponStackCountChangedDelegate -= InventoryTriggerEvent.HandleStackCountChanged;
                        data.MainInventory.WeaponStackCountChangedDelegate += InventoryTriggerEvent.HandleStackCountChanged;
                    }
                    if (data.EquipmentInventory && UseEquipment)
                    {
                        if (!InventoryTriggerEvent.PlayerInventories.Contains(data.EquipmentInventory))
                            InventoryTriggerEvent.PlayerInventories.Add(data.EquipmentInventory);
                        if (!InventoryTriggerEvent.InventoriesToBindTo.Contains(data.EquipmentInventory))
                            InventoryTriggerEvent.InventoriesToBindTo.Add(data.EquipmentInventory);
                        data.EquipmentInventory.WeaponChangedDelegate -= InventoryTriggerEvent.HandleInventoryChanged;
                        data.EquipmentInventory.WeaponChangedDelegate += InventoryTriggerEvent.HandleInventoryChanged;
                        data.EquipmentInventory.WeaponStackCountChangedDelegate -= InventoryTriggerEvent.HandleStackCountChanged;
                        data.EquipmentInventory.WeaponStackCountChangedDelegate += InventoryTriggerEvent.HandleStackCountChanged;
                    }
                    if (data.HotkeyInventory && UseHotkey)
                    {
                        if (!InventoryTriggerEvent.PlayerInventories.Contains(data.HotkeyInventory))
                            InventoryTriggerEvent.PlayerInventories.Add(data.HotkeyInventory);
                        if (!InventoryTriggerEvent.InventoriesToBindTo.Contains(data.HotkeyInventory))
                            InventoryTriggerEvent.InventoriesToBindTo.Add(data.HotkeyInventory);
                        data.HotkeyInventory.WeaponChangedDelegate -= InventoryTriggerEvent.HandleInventoryChanged;
                        data.HotkeyInventory.WeaponChangedDelegate += InventoryTriggerEvent.HandleInventoryChanged;
                        data.HotkeyInventory.WeaponStackCountChangedDelegate -= InventoryTriggerEvent.HandleStackCountChanged;
                        data.HotkeyInventory.WeaponStackCountChangedDelegate += InventoryTriggerEvent.HandleStackCountChanged;
                    }
                    if (InventoryTriggerEvent.PlacingManagers != null && !InventoryTriggerEvent.PlacingManagers.Contains(((HeroPlayerCharacter)Key).ItemPlacingManager))
                    {
                        InventoryTriggerEvent.PlacingManagers.Add(((HeroPlayerCharacter)Key).ItemPlacingManager);
                        ((HeroPlayerCharacter)Key).ItemPlacingManager.PlacedItemDelegate += InventoryTriggerEvent.HandleWeaponPlaced;
                    }
                }
            }

            //InventoryTriggerEvent.CheckInventoryContentsTrigger();
        }
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        if (initialized)
        {
            return;
        }

        // Add missing player inventories to the list. 
        if (this.isActiveAndEnabled)
        {
            HRGameInstance.Get.StartCoroutine(WaitForOwningPlayer());
            initialized = true;
        }
    }


    private IEnumerator WaitForOwningPlayer()
    {
        // We have to wait for the LocalPlayerController reference to be assigned
        yield return new WaitUntil(() => HRNetworkManager.Get.LocalPlayerController != null &&
            HRNetworkManager.Get.LocalPlayerController.PlayerPawn != null);

        if(HRSaveSystem.Get.bIsLoadingPlayer)
            yield return new WaitUntil(() => !HRSaveSystem.Get.bIsLoadingPlayer);

        AddPlayerToTriggerList(HRNetworkManager.Get.LocalPlayerController.PlayerPawn);
    }


    public void AddPlayerToTriggerList(BaseScripts.BasePawn InPawn)
    {
        if (HRNetworkManager.IsHost() || (PlayerTriggerInteractMode == PlayerInteractMode.LocalOnly && InPawn == HRNetworkManager.Get.LocalPlayerController.PlayerPawn))
        {
            AddPlayerToTriggerList_Implementation(InPawn);
        }
        else
        {
            if(netIdentity)
            {
                AddPlayerToTriggerList_Command(InPawn);
            }
        }
    }


    public void AddPlayerToTriggerList_Implementation(BaseScripts.BasePawn InPawn)
    {
        if (!InPawn)
        {
            return;
        }

        if (!PlayerPawnInventories.ContainsKey(InPawn))
        {
            var data = new PlayerPawnInventoryData();
            var inventories = InPawn.GetComponentInChildren<BasePlayerInventoryManager>();

            if(inventories)
            {
                data.MainInventory = inventories.PlayerInventory;
                data.EquipmentInventory = inventories.PlayerEquipmentInventory;
                data.HotkeyInventory = inventories.PlayerHotKeyInventory;

                PlayerPawnInventories.Add(InPawn, data);

                OnPlayersJoinOrLeave();
            }
            else
            {
                Debug.LogError("Error 104: There is no inventories in AddPlayerToTriggerList_Implementation for BaseInventoryTrigger.");
            }
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    public void AddPlayerToTriggerList_Command(BaseScripts.BasePawn InPawn)
    {
        // Add to player list
        AddPlayerToTriggerList_Implementation(InPawn);

        // Add to player list on all clients
        AddPlayerToTriggerList_ClientRpc(InPawn);

        // Attempt to add all existing players, as pulling from the Player List does not work for clients
        foreach (var Player in PlayerPawnInventories.Keys)
        {
            AddPlayerToTriggerList_ClientRpc(Player);
        }
    }


    [Mirror.ClientRpc]
    public void AddPlayerToTriggerList_ClientRpc(BaseScripts.BasePawn InPawn)
    {
        AddPlayerToTriggerList_Implementation(InPawn);
    }


    public override void OnStopClient()
    {
        base.OnStopClient();

        // Remove all inventories related to disconnected players
        var OwningPlayer = HRNetworkManager.Get.LocalPlayerController;

        // Owning Player is not null here so we can remove directly
        if (OwningPlayer
            && OwningPlayer.PlayerPawn != null)
        {
            RemovePlayerFromTriggerList(OwningPlayer.PlayerPawn);
        }
        else
        {
            RemoveAllNulls();
        }
    }


    public void RemovePlayerFromTriggerList(BaseScripts.BasePawn InPawn)
    {
        if (HRNetworkManager.IsHost())
        {
            RemovePlayerFromTriggerList_Implementation(InPawn);
        }
        else
        {
            RemovePlayerFromTriggerList_Command(InPawn);
        }
    }


    public void RemovePlayerFromTriggerList_Implementation(BaseScripts.BasePawn InPawn)
    {
        if (PlayerPawnInventories.ContainsKey(InPawn))
        {
            PlayerPawnInventories.Remove(InPawn);

            OnPlayersJoinOrLeave();
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    public void RemovePlayerFromTriggerList_Command(BaseScripts.BasePawn InPawn)
    {
        RemovePlayerFromTriggerList_Implementation(InPawn);
        RemovePlayerFromTriggerList_ClientRpc(InPawn);
    }


    [Mirror.ClientRpc]
    public void RemovePlayerFromTriggerList_ClientRpc(BaseScripts.BasePawn InPawn)
    {
        RemovePlayerFromTriggerList_Implementation(InPawn);
    }


    public void RemoveAllNulls()
    {
        if (HRNetworkManager.IsHost())
        {
            RemoveAllNulls_Implementation();
        }
        else
        {
            RemoveAllNulls_Command();
        }
    }


    public void RemoveAllNulls_Implementation()
    {
        var PawnsToRemove = new List<BaseScripts.BasePawn>();

        foreach(var key in PlayerPawnInventories.Keys)
        {
            if(key == null)
            {
                PawnsToRemove.Add(key);
            }
        }

        foreach(var Pawn in PawnsToRemove)
        {
            PlayerPawnInventories.Remove(Pawn);
        }

        OnPlayersJoinOrLeave();
    }


    [Mirror.Command(ignoreAuthority = true)]
    public void RemoveAllNulls_Command()
    {
        RemoveAllNulls_Implementation();
    }


    [Mirror.ClientRpc]
    public void RemoveAllNulls_ClientRpc()
    {
        RemoveAllNulls_Implementation();
    }



    public override void OnDestroy()
    {
        base.OnDestroy();

        if(InventoryTriggerEvents != null)
        {
            foreach (var InventoryTriggerEvent in InventoryTriggerEvents)
            {
                if(InventoryTriggerEvent != null)
                {
                    if (InventoryTriggerEvent.InventoriesToBindTo != null)
                    {
                        for (int i = 0; i < InventoryTriggerEvent.InventoriesToBindTo.Count; ++i)
                        {
                            if (InventoryTriggerEvent.InventoriesToBindTo[i] != null)
                            {
                                //InventoryTriggerEvent.InventoriesToBindTo[i].WeaponChangedDelegate -= InventoryTriggerEvent.HandleInventoryChanged;
                                //InventoryTriggerEvent.InventoriesToBindTo[i].WeaponStackCountChangedDelegate -= InventoryTriggerEvent.HandleStackCountChanged;
                            }
                        }
                    }

                    if(InventoryTriggerEvent.PlacingManagers != null)
                    {
                        for (int i = 0; i < InventoryTriggerEvent.PlacingManagers.Count; ++i)
                        {
                            if (InventoryTriggerEvent.PlacingManagers[i])
                            {
                                InventoryTriggerEvent.PlacingManagers[i].PlacedItemDelegate -= InventoryTriggerEvent.HandleWeaponPlaced;
                            }
                        }

                        InventoryTriggerEvent.PlacingManagers.Clear();
                    }


                    if(InventoryTriggerEvent.PlayerInventories != null)
                    {
                        InventoryTriggerEvent.PlayerInventories.Clear();
                    }
                }
            }
        }
    }
}