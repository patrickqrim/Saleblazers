using PixelCrushers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HRBuildingPlacedTrigger : BaseTrigger
{
    [System.Serializable]
    public class BuildingPlacedEvent
    {
        public int BuildingToPlaceID;
        public int AmountToPlace;
        public BaseScriptingEvent ScriptingEvent;

        private int TimesCaught = 0;

        public void Invoke(int ID)
        {
            if (BuildingToPlaceID == -1 || BuildingToPlaceID == ID)
            {
                TimesCaught++;
            }

            if (TimesCaught >= AmountToPlace)
            {
                ScriptingEvent.FireEvents();
            }
        }
    }

    public List<BuildingPlacedEvent> OnBuildingPlacedEvents;


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
        if (Target && bPlaced)
        {
            MessageSystem.SendMessage(this, HRQuestMessages.BuildingItemPlaced, Target.ItemID.ToString(), 1);
            foreach (var Event in OnBuildingPlacedEvents)
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
