using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[Ceras.SerializedType]
public class HRGolfManager : NetworkBehaviour, IHRSaveable
{
    public static HRGolfManager Instance;
    //public HRGolfHoleTrigger[] GolfHoles;

    [System.NonSerialized, Ceras.SerializedField]
    public bool bGameOver;
    public HRCageTrigger cageTriggerCache;
    public int NumTargetFilledHoles = 18;
    private int NumFilledHoles;
    public BaseScriptingEvent ScriptingEvent;

    #region Singleton Callbacks
    public void OnHoleEntered()
    {
        NumFilledHoles++;
        RefreshState();
    }

    public void OnHoleExited()
    {
        NumFilledHoles--;
        RefreshState();
    }

    void RefreshState()
    {
        if (NumFilledHoles == NumTargetFilledHoles)
        {
            if (!bGameOver)
            {
                bGameOver = true;
                if (cageTriggerCache != null)
                {
                    cageTriggerCache.InvokeTrigger();
                }
                ScriptingEvent.FireEvents();
            }
            //foreach(var h in GolfHoles)
            //{
            //    h.UnsubscribeDelegates();
            //}
        }
    }
    #endregion

    #region Singleton
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(this);
    }
    #endregion

    #region Saving
    public void HandleSaveComponentInitialize(HRSaveComponent InSaveComponent, int ComponentID, int AuxIndex)
    {

    }
    public void HandlePreSave()
    {
    }
    public void HandleLoaded()
    {
    }
    public void HandleSaved()
    {

    }
    public void HandleReset()
    {

    }
    public bool IsSaveDirty()
    {
        return true;
    }
    #endregion
}

