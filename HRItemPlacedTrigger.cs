using PixelCrushers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HRItemPlacedTrigger : BaseTrigger
{
    [System.Serializable]  
    public class ItemPlacedEvent
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

    public List<ItemPlacedEvent> OnItemPlacedEvents;


    void OnEnable()
    {
        
    }


    public void OnPiecePlaced(BaseWeapon Target, bool bPlaced)
    {
        
    }

    private void OnDisable()
    {
        
    }
}
