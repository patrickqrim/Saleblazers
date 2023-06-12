using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaseScripts;
using System;

public class HRFarmingPlot : Mirror.NetworkBehaviour
{
    public BaseWeapon OwningWeapon;
    public BaseInventory InInventory;
    public BaseInteractable InInteractable;
    public BaseContainer InContainer;

    [NonSerialized]
    public HRShopPlotFarmManager ShopPlotFarmManager;

    HRPlantComponent PlantComponent;
    HRSeedComponent SeedComponent;

    public override void OnStartServer()
    {
        OnStartImplementation();
    }

    public override void OnStartClient()
    {
        if (HRNetworkManager.IsHost()) return;

        OnStartImplementation();
    }

    public void OnStartImplementation()
    {
        if (InInteractable)
            InInteractable.TapInteractionDelegate += CheckIfHoldingSeed;

        if (HRNetworkManager.IsHost())
        {
            if (InInventory)
            {
                InInventory.SlotChangedDelegate += HandleInventorySlotChanged;
                for (int i = 0; i < InInventory.InventorySlots.Count; ++i)
                {
                    if (InInventory.InventorySlots[i].SlotWeapon)
                    {
                        PlantComponent = InInventory.InventorySlots[i].SlotWeapon.GetComponent<HRPlantComponent>();
                        if (PlantComponent)
                        {
                            OnPlantAdded();
                        }
                    }
                }
            }
            if (OwningWeapon)
            {
                OwningWeapon.OnUpdatePlotDelegate += HandlePlaceInPlot;
            }
        }
    }

    public override void OnStopClient()
    {
        if (HRNetworkManager.IsHost())
        {
            if (InInventory)
                InInventory.SlotChangedDelegate -= HandleInventorySlotChanged;
            if (OwningWeapon)
            {
                OwningWeapon.OnUpdatePlotDelegate -= HandlePlaceInPlot;
            }
        }
        if (InInteractable)
            InInteractable.TapInteractionDelegate -= CheckIfHoldingSeed;
    }

    //TODO: does this get triggered when loading a save?
    private void HandlePlaceInPlot(BaseWeapon InWeapon, HRShopPlot InPlot)
    {
        if (InPlot)
        {
            ShopPlotFarmManager = InPlot.PlotFarmManager;
            //Need to do this b/c PlaceInPlot is called after InventoryChanged when loading
            OnPlantAdded();
            OnSeedAdded();
        }
    }

    public float GetShopPlotGrowthModifier()
    {
        if (ShopPlotFarmManager)
        {
            return ShopPlotFarmManager.ShopPlotGrowModifier;
        }
        return 1;
    }

    private void HandleInventorySlotChanged(BaseInventory InInventory, int SlotChanged)
    {
        BaseInventorySlot slot = InInventory.InventorySlots[SlotChanged];
        if (slot.SlotWeapon)
        {
            SeedComponent = slot.SlotWeapon.GetComponent<HRSeedComponent>();
            if (SeedComponent)
            {
                OnSeedAdded();
                return;
            }
            PlantComponent = slot.SlotWeapon.GetComponent<HRPlantComponent>();
            if (PlantComponent)
            {
                OnPlantAdded();
                //Debug.Log(plant.OwningWeapon.ItemID.ToString());
                return;
            }
        }
        else
        {
            SetInteractableEnabled(true);
        }
    }

    private void SetInteractableEnabled(bool bEnabled)
    {
        if (HRNetworkManager.IsHost())
        {
            SetInteractableEnabled_Implementation(bEnabled);
            SetInteractableEnabled_ClientRpc(bEnabled);
        }
    }

    [Mirror.ClientRpc]
    private void SetInteractableEnabled_ClientRpc(bool bEnabled)
    {
        SetInteractableEnabled_Implementation(bEnabled);
    }

    private void SetInteractableEnabled_Implementation(bool bEnabled)
    {
        if (InInteractable)
        {
            InInteractable.gameObject.SetActive(bEnabled);
        }
    }

    private void OnSeedAdded()
    {
        if (SeedComponent)
        {
            SeedComponent.SetCanPlant(true, this);

            if (SeedComponent.bPlantImmediately)
            {
                SetInteractableEnabled(false);
                SeedComponent.Plant();
            }
        }
    }

    private void OnPlantAdded()
    {
        if (PlantComponent)
        {
            PlantComponent.SetFarmingPlot(this);
            SetInteractableEnabled(false);
            PlantComponent.InitializeColliders();
        }
    }

    private void CheckIfHoldingSeed(BaseInteractionManager InInteractionManager)
    {
        if (InInventory.IsInventoryFull())
        {
            return;
        }
        var PlayerCharacter = InInteractionManager.GetInteractorSourceGameObject().GetComponent<HeroPlayerCharacter>();
        if (PlayerCharacter && PlayerCharacter.WeaponManager && PlayerCharacter.WeaponManager.CurrentWeapon)
        {
            var HoldingItem = PlayerCharacter.WeaponManager.CurrentWeapon.gameObject;
            var OwningSeedComponent = HoldingItem.GetComponent<HRSeedComponent>();
            if (OwningSeedComponent)
            {
                PlayerCharacter.ItemPlacingManager.PlaceHeldItemWithoutGhost(HoldingItem.GetComponent<BaseItemPlaceable>(), InContainer);
            }
        }
    }

    public bool CheckIfHoveringPlotWithSeed(GameObject PlacingObject)
    {
        return PlacingObject && PlacingObject.GetComponent<HRSeedComponent>();
    }
}
