using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HRGolfHoleTrigger : BaseTrigger
{
    #region Init & Subscribe
    bool bHasBall;
    bool bInit;
    private void OnEnable()
    {
        if (bInit) return;
        bInit = true;

        InInventory = GetComponent<BaseInventory>();

        if (InInventory)
            InInventory.WeaponChangedDelegate += HandleInventorySlotChanged;
    }

    private void OnDisable()
    {
        UnsubscribeDelegates();
    }


    public void UnsubscribeDelegates()
    {
        if (!bInit) return;
        bInit = false;
        if (InInventory)
            InInventory.WeaponChangedDelegate -= HandleInventorySlotChanged;
    }
    #endregion

    // Optional
    public BaseScriptingEvent EventGolfHoleEntered;
    public BaseScriptingEvent EventGolfHoleExited;

    public BaseInventory InInventory;
    public static uint GolfBallID = 1709;

    #region Trigger Callbacks
    private void HandleInventorySlotChanged(BaseInventory InInventory, int Index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
    {
        if (!NewWeapon || NewWeapon.ItemID != HRGolfHoleTrigger.GolfBallID)
        {
            if(OldWeapon && OldWeapon.ItemID == HRGolfHoleTrigger.GolfBallID)
            {
                InvokeHoleExited();
            }
            return;
        }

        if(NewWeapon.ItemID == HRGolfHoleTrigger.GolfBallID)
        {
            InvokeHoleEntered();
        }
    }
    void InvokeHoleExited()
    {
        if (!bHasBall) return;
        bHasBall = false;
        HRGolfManager.Instance.OnHoleExited();
        EventGolfHoleExited.FireEvents();
    }
    void InvokeHoleEntered()
    {
        if (bHasBall) return;
        bHasBall = true;
        HRGolfManager.Instance.OnHoleEntered();
        EventGolfHoleEntered.FireEvents();
    }
    #endregion
}