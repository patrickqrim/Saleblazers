using PixelCrushers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HRFarmingTrigger : BaseTrigger
{
    // Store crop plot inventories to delete delegates later
    public List<BaseInventory> CropPlotInventories = new List<BaseInventory>();

    // Store plants to delete delegates later
    public List<HRPlantComponent> Plants = new List<HRPlantComponent>();

    // CROP PLOT EVENT
    [System.Serializable]
    public class CropPlotPlacedEvent
    {
        public int BuildingToPlaceID = 929;
        public int AmountToPlace;
        public BaseScriptingEvent ScriptingEvent;

        private int TimesCaught = 0;
        private const int NUCLEAR_STOP = 10000;

        public void Invoke(int ID)
        {
            if (BuildingToPlaceID == -1 || BuildingToPlaceID == ID)
            {
                TimesCaught++;
            }

            if (TimesCaught >= AmountToPlace && TimesCaught < NUCLEAR_STOP)
            {
                ScriptingEvent.FireEvents();
            }
        }
    }

    public List<CropPlotPlacedEvent> OnCropPlotPlacedEvents;


    // SEED PLANTED EVENT
    [System.Serializable]
    public class SeedPlantedEvent
    {
        public int AmountToPlant;
        public BaseScriptingEvent ScriptingEvent;

        private int TimesCaught = 0;
        public void Invoke()
        {
            TimesCaught++;
            if (TimesCaught >= AmountToPlant)
            {
                ScriptingEvent.FireEvents();
            }
        }
    }

    public List<SeedPlantedEvent> SeedPlantedEvents;


    // PLANT WATERED EVENT
    [System.Serializable]
    public class PlantWateredEvent
    {
        public int AmountToWater;
        public BaseScriptingEvent ScriptingEvent;

        private int TimesCaught = 0;
        public void Invoke()
        {
            TimesCaught++;
            if (TimesCaught >= AmountToWater)
            {
                ScriptingEvent.FireEvents();
            }
        }
    }

    public List<PlantWateredEvent> PlantWateredEvents;


    // DONE GROWING EVENT
    [System.Serializable]
    public class DoneGrowingEvent
    {
        public int AmountToGrow;
        public BaseScriptingEvent ScriptingEvent;

        private int TimesCaught = 0;
        public void Invoke()
        {
            TimesCaught++;
            if (TimesCaught >= AmountToGrow)
            {
                ScriptingEvent.FireEvents();
            }
        }
    }

    public List<DoneGrowingEvent> DoneGrowingEvents;
    

    // HARVESTED EVENT
    [System.Serializable]
    public class HarvestedEvent
    {
        public int AmountToHarvest;
        public BaseScriptingEvent ScriptingEvent;

        private int TimesCaught = 0; 
        public void Invoke()
        {
            TimesCaught++;
            if (TimesCaught >= AmountToHarvest)
            {
                ScriptingEvent.FireEvents();
            }
        }
    }

    public List<HarvestedEvent> HarvestedEvents;


    // BEGIN METHODS
    void OnEnable()
    {
        // TO CHECK IF CROP PLOT WAS PLACED
        if (HRGameManager.Get)
        {
            (HRGameManager.Get as HRGameManager).OnBuildingPlacedLocal -= OnPiecePlaced;
            (HRGameManager.Get as HRGameManager).OnBuildingPlacedLocal += OnPiecePlaced;
        }
        else
        {
            Debug.LogError("Not doing it!");
        }

        // For each crop plot inventory, delete delegates
        foreach (BaseInventory inventory in CropPlotInventories)
        {
            if (inventory)
            {
                inventory.SlotChangedDelegate -= OnInventorySlotChanged;
                inventory.SlotChangedDelegate += OnInventorySlotChanged;
            }
        }

        // For each plant, delete delegates
        foreach (HRPlantComponent plant in Plants)
        {
            if (plant)
            {
                plant.WateredDelegate -= OnPlantWatered;
                plant.DoneGrowingDelegate -= OnDoneGrowing;
                plant.WateredDelegate += OnPlantWatered;
                plant.DoneGrowingDelegate += OnDoneGrowing;
            }
        }

        // TO CHECK IF PLANT WAS ADDED TO HOTKEY INVENTORY
        //BaseInventory inventory = ((HeroPlayerCharacter)((HRGameInstance)BaseGameInstance.Get).GetLocalPlayerController().PlayerPawn).InventoryManager.PlayerHotKeyInventory;
        //inventory.SlotChangedDelegate += OnInventorySlotChanged;
    }

    public void OnPiecePlaced(BaseWeapon Target, bool bPlaced)
    {
        if (Target && bPlaced && Target.ItemID == 929)
        {
            //MessageSystem.SendMessage(this, HRQuestMessages.BuildingItemPlaced, Target.ItemID.ToString(), 1);
            foreach (var Event in OnCropPlotPlacedEvents)
            {
                // Add trigger to the crop plot inventories
                BaseInventory CropPlotInventory = Target.gameObject.GetComponent<BaseInventory>();
                if (!CropPlotInventories.Contains(CropPlotInventory))
                {
                    Event.Invoke(Target.ItemID);
                    
                    CropPlotInventory.SlotChangedDelegate -= OnInventorySlotChanged;
                    CropPlotInventory.SlotChangedDelegate += OnInventorySlotChanged;
                    // Store to delete later
                    CropPlotInventories.Add(CropPlotInventory);
                }
            }
        }
    }

    public void OnInventorySlotChanged(BaseInventory InInventory, int SlotChanged)
    {
        BaseInventorySlot slot = InInventory.InventorySlots[SlotChanged];
        if (slot.SlotWeapon)
        {
            // check if planted
            //HRSeedComponent seed = slot.SlotWeapon.GetComponent<HRSeedComponent>();
            //if (seed)
            //{
            //    foreach (var Event in SeedPlantedEvents)
            //    {
            //        Event.Invoke();
            //    }
            //}

            // Add watering, done growing, harvested triggers once seed becomes plant (happens immediately after seed is planted, fyi)
            HRPlantComponent plant = slot.SlotWeapon.GetComponent<HRPlantComponent>();
            if (plant && !Plants.Contains(plant))
            {
                foreach (var Event in SeedPlantedEvents)
                {
                    Event.Invoke();
                }
                plant.WateredDelegate -= OnPlantWatered;
                plant.DoneGrowingDelegate -= OnDoneGrowing;
                plant.HarvestedDelegate -= OnHarvested;
                plant.WateredDelegate += OnPlantWatered;
                plant.DoneGrowingDelegate += OnDoneGrowing;
                plant.HarvestedDelegate += OnHarvested;
                Plants.Add(plant);
            }
        }
    }

    public void OnPlantWatered()
    {
        foreach (var Event in PlantWateredEvents)
        {
            Event.Invoke();
        }
    }

    public void OnDoneGrowing()
    {
        foreach (var Event in DoneGrowingEvents)
        {
            Event.Invoke();
        }
    }

    public void OnHarvested()
    {
        foreach (var Event in HarvestedEvents)
        {
            Event.Invoke();
        }
    }

    private void OnDisable()
    {
        // Placing crop plots
        if (HRGameManager.Get)
        {
            (HRGameManager.Get as HRGameManager).OnBuildingPlacedLocal -= OnPiecePlaced;
        }

        // For each crop plot inventory, delete delegates
        //foreach (BaseInventory inventory in CropPlotInventories)
        //{
        //    if (inventory)
        //    {
        //        inventory.SlotChangedDelegate -= OnInventorySlotChanged;
        //    }
        //}

        //// For each plant, delete delegates
        //foreach (HRPlantComponent plant in Plants)
        //{
        //    if (plant)
        //    {
        //        plant.WateredDelegate -= OnPlantWatered;
        //        plant.DoneGrowingDelegate -= OnDoneGrowing;
        //    }
        //}
    }
}
