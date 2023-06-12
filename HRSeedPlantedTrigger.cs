using PixelCrushers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HRSeedPlantedTrigger : BaseTrigger
{
    [System.Serializable]  
    public class SeedPlantedEvent
    {
        public int BuildingToPlaceID;
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

    public List<SeedPlantedEvent> OnCropPlotPlacedEvents;

    void OnEnable()
    {
        if (HRGameManager.Get)
        {
            (HRGameManager.Get as HRGameManager).OnBuildingPlaced += OnPiecePlaced;
        }
        else
        {
            Debug.LogError("Not doing it!");
        }
    }

    public void OnInventorySlotChanged(BaseInventory InInventory, int SlotChanged)
    {
        BaseInventorySlot slot = InInventory.InventorySlots[SlotChanged];
        if (slot.SlotWeapon)
        {
            HRPlantComponent plant = slot.SlotWeapon.GetComponent<HRPlantComponent>();
            if (plant)
            {
                foreach (var Event in OnCropPlotPlacedEvents)
                {
                    //Event.Invoke(Target.ItemID);
                }
            }
        }
    }
    
    //temp to avoid syntax errors
    public void OnPiecePlaced(BaseWeapon Target, bool bPlaced)
    {
        if (Target)
        {
            MessageSystem.SendMessage(this, HRQuestMessages.BuildingItemPlaced, Target.ItemID.ToString(), 1);
            foreach (var Event in OnCropPlotPlacedEvents)
            {
                Event.Invoke(Target.ItemID);
            }
        }
    }
    private void OnDisable()
    {
        if (HRGameManager.Get)
        {
            (HRGameManager.Get as HRGameManager).OnBuildingPlaced -= OnPiecePlaced;
        }
    }
}
