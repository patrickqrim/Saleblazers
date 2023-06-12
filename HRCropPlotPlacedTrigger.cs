using PixelCrushers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HRCropPlotPlacedTrigger : BaseTrigger
{
    [System.Serializable]  
    public class CropPlotPlacedEvent
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

    public List<CropPlotPlacedEvent> OnCropPlotPlacedEvents;

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
