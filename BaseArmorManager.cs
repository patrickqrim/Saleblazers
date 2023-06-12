using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseArmorManager : BaseHP
{
    public BaseWeaponManager WeaponManager;
    public List<BaseArmorComponent> ActiveArmorComponents = new List<BaseArmorComponent>();

    public delegate void OnArmorHPChangedSignature(BaseArmorManager InArmorManager, float OldHP, float NewHP, bool bPlayEffects);
    public OnArmorHPChangedSignature OnArmorHPChangedDelegate;
    public OnArmorHPChangedSignature OnArmorMaxHPChangedDelegate;

    public float MaxArmorHP { get; private set; }
    public float CurrentArmorHP { get; private set; }

    public HRHealthBarComponent HealthBar;
    public HRHealthBarComponent ArmorBar;
    public BaseMeshDamageFlash MeshDamageFlash;
    private bool bRegistered = false;

    public override void Awake()
    {
        base.Awake();

        if (HRNetworkManager.IsHost())
        {
            if (WeaponManager && WeaponManager.EquipmentInventory)
            {
                WeaponManager.EquipmentInventory.WeaponChangedDelegate += UpdateArmorComponent;
            }
        }
    }


    void Start()
    {
        if (HRNetworkManager.IsHost())
        {
            float armorHP = 0;

            float max = 0;
            float current = 0;

            foreach (var Armor in ActiveArmorComponents)
            {
                Armor.OnHPChangedDelegate += OnArmorHPChanged;
                armorHP += Armor.MaxHP;

                max += Armor.MaxHP;
                current += Armor.CurrentHP;
            }

            this.ServerMaxHP = max;
            this.ServerHP = current;
            if (ActiveArmorComponents.Count > 0 && !bRegistered && armorHP > 0)
            {
                bRegistered = true;
                HealthBar.LinkedHealthBars.Add(ArmorBar);
                ArmorBar.LinkedHealthBars.Add(HealthBar);
            }
        }
    }

    private void UpdateArmorComponent(BaseInventory Inventory, int Index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
    {
        if (NewWeapon)
        {
            var Armor = NewWeapon.GetComponent<BaseArmorComponent>();

            if (Armor && !ActiveArmorComponents.Contains(Armor))
            {
                Armor.OnHPChangedDelegate += OnArmorHPChanged;
                ActiveArmorComponents.Add(Armor);

                float OldCurr = CurrentArmorHP;
                float OldMax = MaxArmorHP;

                SetMaxHP(MaxHP + Armor.MaxHP, true);
                SetHP(CurrentHP + Armor.CurrentHP, this.gameObject);

                if (ActiveArmorComponents.Count > 0 && !bRegistered && Armor.MaxHP > 0)
                {
                    bRegistered = true;
                    HealthBar.LinkedHealthBars.Add(ArmorBar);
                    ArmorBar.LinkedHealthBars.Add(HealthBar);
                }

                //OnArmorHPChangedDelegate?.Invoke(this, OldCurr, CurrentArmorHP, false);
                //OnArmorMaxHPChangedDelegate?.Invoke(this, OldMax, MaxArmorHP, false);
            }
        }
        if (OldWeapon)
        {
            var Armor = OldWeapon.GetComponent<BaseArmorComponent>();

            if (Armor && ActiveArmorComponents.Contains(Armor))
            {
                Armor.OnHPChangedDelegate -= OnArmorHPChanged;
                ActiveArmorComponents.Remove(Armor);

                SetMaxHP(MaxHP - Armor.MaxHP, true);
                SetHP(CurrentHP - Armor.CurrentHP, this.gameObject);

                if (ActiveArmorComponents.Count == 0 && bRegistered)
                {
                    bRegistered = false;
                    HealthBar.LinkedHealthBars.Remove(ArmorBar);
                    ArmorBar.LinkedHealthBars.Remove(HealthBar);
                }

                //OnArmorHPChangedDelegate?.Invoke(this, OldCurr, CurrentArmorHP, false);
                //OnArmorMaxHPChangedDelegate?.Invoke(this, OldMax, MaxArmorHP, false);
            }
        }
    }


    public bool DamageArmor(AppliedDamageData damageData, float amount)
    {
        foreach (var Armor in ActiveArmorComponents)
        {
            if (Armor.DoesHitArmor(damageData.damageType) & Armor.CurrentHP > 0)
            {
                float ModifiedAmount = Armor.ModifyDamage(damageData.damageType, amount);

                if (damageData.ignoreAuthority)
                {
                    Armor.RemoveHP_IgnoreAuthority(ModifiedAmount,
                        damageData.instigator, damageData.forceUpdate, false,
                        damageData.displayText, damageData.overrideTextFX, damageData.bUseDamageLocation, damageData.damageLocation);
                }
                else
                {
                    Armor.RemoveHP(ModifiedAmount,
                        damageData.instigator, damageData.forceUpdate, false,
                        damageData.displayText, damageData.overrideTextFX, damageData.bUseDamageLocation, damageData.damageLocation);
                }

                return true;
            }
        }

        return false;
    }


    private void OnArmorHPChanged(BaseHP InHP, GameObject Instigator, float OldHP, float NewHP, bool bPlayEffects)
    {
        // Show the health bar
        if (OldHP != NewHP)
        {
            float Old = CurrentHP;
            SetHP(CurrentHP - (OldHP - NewHP), this.gameObject);

            OnArmorHPChangedDelegate?.Invoke(this, Old, CurrentArmorHP, true);
        }
    }



    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!WeaponManager || !WeaponManager.EquipmentInventory)
        {
            return;
        }

        /*for (int i = 0; i < WeaponManager.EquipmentInventory.InventorySlots.Count; i++)
        {
            var slot = WeaponManager.EquipmentInventory.InventorySlots[i];

            if (slot.SlotWeapon != null)
            {
                UpdateArmorComponent(WeaponManager.EquipmentInventory, i, null, slot.SlotWeapon);
            }
        }*/
    }
}
