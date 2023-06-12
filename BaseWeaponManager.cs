using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BaseScripts;
using UnityEngine.EventSystems;
using Mirror;
using System;

public static class BaseWeaponManagerReaderWriter
{
    public static void WriteBaseWeaponManager(this Mirror.NetworkWriter writer, BaseWeaponManager InWeaponManager)
    {
        if (InWeaponManager)
        {
            writer.Write<uint>(InWeaponManager.netId);
        }
        else
        {
            writer.Write<uint>(0);
        }
    }

    public static BaseWeaponManager ReadBaseWeaponManager(this Mirror.NetworkReader reader)
    {
        Mirror.NetworkIdentity outNetworkIdentity = null;

        if (Mirror.NetworkIdentity.spawned.TryGetValue(reader.Read<uint>(), out outNetworkIdentity))
        {
            if(outNetworkIdentity)
            {
                return outNetworkIdentity.GetComponent<BaseWeaponManager>();
            }
        }

        return null;
    }
}

// Weapon is a generic term for something that can be picked up.
// WeaponManager handles picking up, using, swapping, and storing weapons.
public class BaseWeaponManager : Mirror.NetworkBehaviour, IBasePool
{
    // Used to listen to swing events, dash events, etc. of animations
    public BaseCombatListener CombatListener;

    public enum WeaponUseMode { PLACING, USING };

    [System.Serializable]
    public struct BaseWeaponColliderData
    {
        public WeaponColliderType ColliderType;
        public Collider ColliderObj;
    }

    public bool bShouldLunge = true;

    // 0 = placing
    // 1 = attack
    [System.NonSerialized]
    public WeaponUseMode CurrentUseMode = WeaponUseMode.USING;

    [SyncVar(hook = "InCombatChanged_Hook")]
    public bool inCombat = false;

    public delegate void FBaseWeaponManagerAddedWeaponSignature(BaseWeaponManager InManager, BaseWeapon WeaponAdded);
    public FBaseWeaponManagerAddedWeaponSignature AddedWeaponDelegate;
    public FBaseWeaponManagerAddedWeaponSignature EquipWeaponDelegate;
    public FBaseWeaponManagerAddedWeaponSignature UnequipWeaponDelegate;
    public FBaseWeaponManagerAddedWeaponSignature WeaponUseDelegate;

    public delegate void FBaseWeaponManagerUseModeChangedSignature(BaseWeaponManager InManager, WeaponUseMode OldUseMode, WeaponUseMode NewUseMode);
    public FBaseWeaponManagerUseModeChangedSignature WeaponUseModeChangedDelegate;

    public delegate void FBaseWeaponManagerDamageModifierChangedSignature(float OldModifier, float NewModifier);
    public FBaseWeaponManagerDamageModifierChangedSignature WeaponDamageModifierChangedDelegate;

    public delegate void FBaseWeaponManagerRemovedWeaponSignature(BaseWeaponManager InManager, BaseWeapon WeaponRemoved);
    public FBaseWeaponManagerRemovedWeaponSignature RemovedWeaponDelegate;

    public delegate void FBaseWeaponManagerHotkeySelectChangeDelegate(BaseWeaponManager InManager, int OldSlot, int NewSlot);
    public FBaseWeaponManagerHotkeySelectChangeDelegate HotkeySlotSelectedDelegate;

    public delegate void FBaseWeaponManagerInCombatSignature(BaseWeaponManager InManager, bool bInCombat);
    public FBaseWeaponManagerInCombatSignature InCombatChangedDelegate;

    [Mirror.SyncVar(hook = "HandleHotkeySlotChanged_Hook")]
    public int CurrentSelectedHotKeySlot = 0;

    // Hot key slots, 1-6 keys
    public BaseInventory HotKeyInventory;
    // Main inventory, opened using inventory button
    public BaseInventory MainInventory;

    public BaseInventory DragInventory;

    public BaseInventory EquipmentInventory;

    [System.NonSerialized]
    // The weapon we are currently holding.
    public BaseWeapon CurrentWeapon;

    [Tooltip("Need to change this to change default unarmed")]
    public int DefaultEmptyWeaponID = 830;

    public bool bUseRigWeaponColliders;
    public BaseWeaponColliderData[] AdditionalWeaponColliders;

    // The weapon prefab to equip if we are not actively holding any other weapons.
    // Typically used as an empty-handed fist weapon.
    [SyncVar(hook = "HandleEmptyWeaponChanged_Hook")]
    public uint DefaultEmptyWeaponRuntimeID;
    [System.NonSerialized]
    public BaseWeapon DefaultEmptyWeapon;

    public bool bSpawnDefaultWeapon = true;

    // The player character that owns this weapon manager.
    [HideInInspector]
    public BasePlayerCharacter OwningPlayerCharacter;

    public BaseItemPlacingManager ItemPlacingManager;

    public GameObject WeaponAttachSocket;

    public GameObject PickUpParticleEffect;

    public BaseObjectPoolingComponent FakeWeaponLerpEffect;

    BasePlayerInventoryManager InventoryManager;

    // This is so bad but I have no time to make this properly -- besides, most AI will need this anyway
    // What chance the AI will hipfire the weapon
    public float HipfireChance = 0.4f;

    public BaseInteractionContextData UseItemContextInfo = BaseInteractionContextData.Default;
    public BaseInteractionContextData BlockItemContextInfo = BaseInteractionContextData.Default;

    public float SwingSpeedModifier = 1f;
    public float WeaponDamageModifier = 1f;
    float OriginalSwingSpeedModifier;

    public bool bDisableParry;

    public bool bIsSwinging;
    [System.NonSerialized, HideInInspector] public bool bCanThrow = true;
    [System.NonSerialized, HideInInspector] public bool bIsThrowing;
    public bool bFinalThowing;
    public int MeleeCurrentComboCount = 0;
    public bool bMeleeUsedFinalLightAttack = false;
    public float LastHitTime = 0.0f;

    public bool bShouldHideWeapon = false;
    public bool bSwingingCanNotRollCancel = false;
    private bool bSync = false;
    List<MonoBehaviour> HideWeaponRequesters = new List<MonoBehaviour>();

    float timeSpawned = float.MaxValue;
    const float justSpawnedTimer = 5f;

    private GameObject MainWeaponAttachSocket;
    private GameObject TempWeaponAttachSocket;

    const float DroppablePickupDelay = 0.005f;
    float LastPickupTimestamp;

    bool bCanSwitchWeapons = true;
    public bool bJustSpawned
    {
        get
        {
            return Time.timeSinceLevelLoad - timeSpawned <= justSpawnedTimer;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        SpawnDefaultEmptyWeapon();
    }

    public override void Start()
    {
        base.Start();

        timeSpawned = Time.timeSinceLevelLoad;
    }

    void Update()
    {
        if (ItemPlacingManager)
        {
            if (ItemPlacingManager.bPlacedItem > 0)
            {
                ItemPlacingManager.bPlacedItem--;
                if (ItemPlacingManager.bPlacedItem == 0)
                {
                    BoxCollider CastedBoxCollider = (ItemPlacingManager.SavedBoxCollider as BoxCollider);
                    if(CastedBoxCollider)
                    {
                        CastedBoxCollider.enabled = true;
                        CastedBoxCollider.isTrigger = false;
                    }
                    else
                    {
                        Debug.LogError("Error: CastedBoxCollider didn't work in BaseWeaponManager.Update");
                    }
                }
            }
        }
    }

    public void SetInCombat(bool bCombat)
    {
        SetInCombat_Implementation(bCombat);
    }

    private void SetInCombat_Implementation(bool bCombat)
    {
        inCombat = bCombat;
    }

    public void SpawnDefaultEmptyWeapon()
    {
        if(bSpawnDefaultWeapon)
        {
            if (DefaultEmptyWeapon == null)
            {
                GameObject FistsGO = Instantiate(((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemArray[DefaultEmptyWeaponID].ItemPrefab);

                BaseWeapon FistsWeapon = FistsGO.GetComponent<BaseWeapon>();
                if (FistsWeapon)
                {
                    FistsWeapon.netIdentity.bSpawnImmediately = true;
                    NetworkServer.Spawn(FistsGO);
                    DefaultEmptyWeaponRuntimeID = FistsWeapon.netId;
                    DefaultEmptyWeapon = FistsWeapon;
                    FistsGO.transform.SetParent(null);
                    DontDestroyOnLoad(FistsGO);
                }
                else
                {
                    Debug.LogError("Fist weapon doesn't exist");
                }
            }
        }
    }

    public void HandleLODEnabled(BasePlayerRig playerRig, bool bEnabled)
    {
        if (bEnabled)
        {
            SetWeaponSocket(playerRig.gameObject);
        }
        else
        {
            ResetWeaponSocket();
        }
    }

    public void HandleRigBeingDestroyed(BasePlayerRig rig, bool bDestroyed)
    {
        if (rig)
        {
            rig.OnBeingDestroyed -= HandleRigBeingDestroyed;
        }

        if (CurrentWeapon)
        {
            CurrentWeapon.transform.SetParent(this.transform);
        }
    }

    void ResetWeaponSocket()
    {
        if (MainWeaponAttachSocket)
        {
            SetWeaponSocket(MainWeaponAttachSocket);
            TempWeaponAttachSocket = null;
        }
    }

    public void SetTempWeaponSocket(GameObject tempSocket)
    {
        if (tempSocket)
        {
            TempWeaponAttachSocket = tempSocket;
            SetWeaponSocket(tempSocket);
        }
    }

    public void SetMainWeaponSocket(GameObject weaponSocket)
    {
        MainWeaponAttachSocket = weaponSocket;
        if (MainWeaponAttachSocket != WeaponAttachSocket)
        {
            SetWeaponSocket(weaponSocket);
        }
    }

    public void SetWeaponSocket(GameObject newSocket)
    {
        if (!newSocket)
        {
            newSocket = this.gameObject;
        }
        if (newSocket != WeaponAttachSocket || (CurrentWeapon && CurrentWeapon.transform.parent != newSocket))
        {
            WeaponAttachSocket = newSocket;
            if (CurrentWeapon)
            {
                CurrentWeapon.SetWeaponToSocket();
            }
        }
    }

    public void InCombatChanged_Hook(bool bOldInCombat, bool bNewInCombat)
    {
        if (HRNetworkManager.HasControl(this.netIdentity) && OwningPlayerCharacter && OwningPlayerCharacter.PlayerCamera)
        {
            // Increase FOV
            float NewBaseAdditiveFOV = bNewInCombat ? 10.0f : 0.0f;

            OwningPlayerCharacter.PlayerCamera.SetBaseAdditiveFOV(NewBaseAdditiveFOV);
        }

        InCombatChangedDelegate?.Invoke(this, bNewInCombat);
    }

    public void RequestHideWeapon(bool bShouldHide, MonoBehaviour InComponent)
    {
        bool bContainsRequester = HideWeaponRequesters.Contains(InComponent);

        if (bShouldHide)
        {
            if (bContainsRequester)
            {
                return;
            }

            HideWeaponRequesters.Add(InComponent);

            if (bShouldHideWeapon == false)
            {
                Internal_HideWeapon(true);
            }
        }
        else
        {
            if (bContainsRequester)
            {
                HideWeaponRequesters.Remove(InComponent);
            }

            for (int i = HideWeaponRequesters.Count - 1; i >= 0; --i)
            {
                if (HideWeaponRequesters[i] == null)
                {
                    HideWeaponRequesters.RemoveAt(i);
                }
            }

            if (HideWeaponRequesters.Count == 0)
            {
                Internal_HideWeapon(false);
            }
        }
    }

    void Internal_HideWeapon(bool bHide)
    {
        // Show/hide current weapon
        HideWeaponMeshRenderer(CurrentWeapon, bHide);

        // Hide future weapons
        bShouldHideWeapon = bHide;
    }

    void HideWeaponMeshRenderer(BaseWeapon InWeapon, bool bHide)
    {
        if (InWeapon)
        {
            InWeapon.SetMeshRendererObjectEnabled(!bHide);
        }
    }

    public void HandleHotkeySlotChanged_Hook(int OldSlot, int NewSlot)
    {
        if (!netIdentity.hasAuthority || netIdentity.connectionToClient == null)
        {
            if (!this.OwningPlayerCharacter.hasAuthority)
            {
                SwitchToSlot(NewSlot);
            }
        }

        if (NewSlot == -1)
        {
            DoClearWeapon();
        }
    }

    public void HandleEmptyWeaponChanged_Hook(uint Old, uint New)
    {
        SearchForWeapon(New);
    }

    public void UpdateEmptyWeapon()
    {
        SearchForWeapon(DefaultEmptyWeaponRuntimeID);
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SearchForWeapon_Command()
    {
        SearchForWeapon_ClientRpc();
    }


    [Mirror.ClientRpc]
    private void SearchForWeapon_ClientRpc()
    {
        SearchForWeapon(DefaultEmptyWeaponRuntimeID);
    }

    private void SearchForWeapon(uint New)
    {
        if (DefaultEmptyWeapon != null)
        {
            return;
        }

        Mirror.NetworkIdentity outNetworkIdentity = null;

        if (Mirror.NetworkIdentity.spawned.TryGetValue(New, out outNetworkIdentity) && outNetworkIdentity)
        {
            BaseWeapon FistsWeapon = outNetworkIdentity.GetComponent<BaseWeapon>();

            if (FistsWeapon)
            {
                DefaultEmptyWeapon = FistsWeapon;

                if (CurrentWeapon == null)
                {
                    SwitchToWeapon(DefaultEmptyWeapon);
                }
            }
            else
            {
                Debug.LogError("Weapon failure");
            }

            //return outNetworkIdentity.GetComponent<BaseInventory>();
        }
        else
        {
            //Debug.LogError("Fist weapon not found in spawned NetworkIdentity list.");
        }
    }

    public void AddDamageModifier(float InModifier)
    {
        InModifier += WeaponDamageModifier;
        WeaponDamageModifierChangedDelegate?.Invoke(WeaponDamageModifier, InModifier);

        WeaponDamageModifier = InModifier;
    }

    public void SetDamageModifier(float InModifier)
    {
        WeaponDamageModifierChangedDelegate?.Invoke(WeaponDamageModifier, InModifier);

        WeaponDamageModifier = InModifier;
    }

    public void ResetDamageModifier()
    {
        WeaponDamageModifierChangedDelegate?.Invoke(WeaponDamageModifier, 1);

        WeaponDamageModifier = 1;
    }

    public void SetSwingSpeedModifier(float InModifier)
    {
        OriginalSwingSpeedModifier = SwingSpeedModifier;
        SwingSpeedModifier = InModifier;
    }

    public void ResetSwingSpeedModifier()
    {
        SwingSpeedModifier = OriginalSwingSpeedModifier;
    }

    public void SetUseMode(WeaponUseMode InUseMode, bool bRefreshCurrentWeapon = true)
    {
        if (InUseMode != CurrentUseMode)
        {
            WeaponUseMode OldUseMode = CurrentUseMode;

            CurrentUseMode = InUseMode;

            // Re-equip.
            if (bRefreshCurrentWeapon)
            {
                // Find in hotkey
                int HotkeySlot = -1;
                HotkeySlot = HotKeyInventory.FindSlotWithWeapon(CurrentWeapon);
                if (HotkeySlot != -1)
                {
                    // Swap to this slot.
                    SwitchToSlot(HotkeySlot);
                }
                else
                {
                    SwitchToWeapon(CurrentWeapon == null ? DefaultEmptyWeapon : CurrentWeapon);
                }
            }

            if ((!netIdentity || hasAuthority) && OwningPlayerCharacter.PlayerCamera)
            {
                OwningPlayerCharacter.PlayerCamera.SetMouseControlsCamera(InUseMode == WeaponUseMode.USING);
            }

            if (InUseMode == WeaponUseMode.USING)
            {
                ItemPlacingManager.StopPlacingObject();

                //OwningPlayerCharacter.PlayerCamera.ResetFOV();

                //OwningPlayerCharacter.PlayerCamera.ResetMinMaxRotation();
                //OwningPlayerCharacter.PlayerCamera.ResetMinMaxZoom();
                if (CurrentWeapon && CurrentWeapon.OwningInventory && CurrentWeapon.OwningInventory != HotKeyInventory)
                {
                    SwitchToSlot(CurrentSelectedHotKeySlot);
                }

                if (HotKeyInventory?.InventoryUI?.BaseRaycaster)
                {
                    HotKeyInventory.InventoryUI.BaseRaycaster.enabled = false;
                }

                if (MainInventory?.InventoryUI?.BaseRaycaster)
                {
                    MainInventory.InventoryUI.BaseRaycaster.enabled = false;
                }
            }
            else if (InUseMode == WeaponUseMode.PLACING)
            {
                if(HotKeyInventory?.InventoryUI?.BaseRaycaster)
                {
                    HotKeyInventory.InventoryUI.BaseRaycaster.enabled = true;
                }

                if (MainInventory?.InventoryUI?.BaseRaycaster)
                {
                    MainInventory.InventoryUI.BaseRaycaster.enabled = true;
                }
                //OwningPlayerCharacter.PlayerCamera.SetFOVMultiplier(1.1f);
                //OwningPlayerCharacter.PlayerCamera.SetMinMaxRotation(70.0f, 70.0f);
                //OwningPlayerCharacter.PlayerCamera.SetZoomAmount(Mathf.Clamp(OwningPlayerCharacter.PlayerCamera.CurrentZoomAmount, 7.0f, 12.0f));
                //OwningPlayerCharacter.PlayerCamera.SetMinMaxZoom(7.0f, 12.0f);
            }

            WeaponUseModeChangedDelegate?.Invoke(this, OldUseMode, InUseMode);

            // Change the context to be the flipped version.
            //if (InUseMode == WeaponUseMode.PLACING)
            //{
            //    SwitchToPlacementContextDelegate?.Invoke(SwitchToPlacementContextInfo, false);
            //    SwitchToUsingContextDelegate?.Invoke(SwitchToUsingContextInfo, true);
            //}
            //else
            //{
            //    SwitchToUsingContextDelegate?.Invoke(SwitchToUsingContextInfo, false);
            //    SwitchToPlacementContextDelegate?.Invoke(SwitchToPlacementContextInfo, true);
            //}
        }
    }

    public void CycleUseMode()
    {
        if (CurrentUseMode == WeaponUseMode.PLACING)
        {
            SetUseMode(WeaponUseMode.USING);
        }
        else if (CurrentUseMode == WeaponUseMode.USING)
        {
            SetUseMode(WeaponUseMode.PLACING);
        }
    }

    public void InitializeBaseWeaponManager(BaseItemPlacingManager InItemPlacingManager)
    {
        // Maybe move this to HRItemPlacingManager

        if (InItemPlacingManager)
        {
            ItemPlacingManager = InItemPlacingManager;
        }

        //SwitchToUsingContextDelegate?.Invoke(SwitchToPlacementContextInfo, true);
    }

    public virtual bool RaycastMouseInput(out RaycastHit hit)
    {
        return RaycastMouseInput(0.1f, ItemPlacingManager.PlaceLayerMask, out hit);
    }

    public virtual bool RaycastMouseInput(float WeaponRadius, LayerMask WeaponLayerMask, out RaycastHit hit)
    {
        if(OwningPlayerCharacter && OwningPlayerCharacter.PlayerCamera)
        {
            Ray WeaponRay = OwningPlayerCharacter.PlayerCamera.GetAimRay();
            if (Physics.SphereCast(WeaponRay, WeaponRadius, out hit, 1000.0f, WeaponLayerMask))
            {
                return true;
            }
        }

        hit = new RaycastHit();
        return false;
    }

    bool bPrimaryPressed = false;

    public void SecondaryInteract(bool bPressed)
    {
        if(CurrentWeapon)
        {
            DoInteract(bPressed, false, false);
        }
    }
    public void PrimaryInteract(bool bPressed, bool bForceAttack = false)
    {
        bPrimaryPressed = bPressed;
        // Don't do anything if there is UI under the mouse or if the game is paused.
        // TODO maybe trigger primary interact (false) on pause?
        if ((EventSystem.current.IsPointerOverGameObject() || BaseGameInstance.Get.PauseManager.GetStatus()))
        {
            bPrimaryPressed = false;
            return;
        }

        if (OwningPlayerCharacter && OwningPlayerCharacter.InputComponent && (OwningPlayerCharacter.InputComponent as HeroInputComponent).GetIsInMinigame())
            return;

        if (CurrentUseMode == WeaponUseMode.PLACING && !bForceAttack)
        {
            if (ItemPlacingManager)
            {
                if (ItemPlacingManager.bIsPlacing)
                {
                    // Place the weapon
                    // TODO this is very bad because AI can't place things in containers... need to condense this into a function
                    if (bPressed && ItemPlacingManager.CanPlaceObject())
                    {
                        BaseItemPlaceable currentPlacedItem = ItemPlacingManager.CurrentPlaceableGameObject;
                        var DragSlot = DragInventory.InventoryUI.InventorySlotGameObjects[0];

                        var DragWeapon = DragSlot.CurrentWeapon;
                        if (currentPlacedItem)
                        {
                            bool isPlacedItemSameAsWeapon = CurrentWeapon
                                && (CurrentWeapon.gameObject == currentPlacedItem.gameObject);

                            bool placeWholeStack = false;
                            if (DragSlot.isActiveAndEnabled && DragWeapon != null && ItemPlacingManager.CurrentPlaceableGameObject == DragWeapon.PlaceableComponent)
                                placeWholeStack = true;

                            ItemPlacingManager.PlaceObject(placeWholeStack);

                            if (CurrentWeapon != DefaultEmptyWeapon && CurrentWeapon && CurrentWeapon.OwningInventory && CurrentWeapon.OwningInventory.InventoryUI)
                            {
                                CurrentWeapon.OwningInventory.InventoryUI.InteractInventorySlots(CurrentWeapon);
                            }

                            if (isPlacedItemSameAsWeapon)
                            {
                                SetCurrentWeapon(CurrentWeapon);
                            }
                        }
                        //This is here so that we continue placing the next item in the stack when dropping stacks
                        if (!DragWeapon)
                        {
                            // Equip if there is a weapon
                            SwitchToWeapon(CurrentWeapon);
                        }
                        // Bad
                        ItemPlacingManager.bJustPlaced = false;
                    }
                    else if (CurrentWeapon)
                    {
                        CurrentWeapon.DropError();
                    }
                    else if (!CurrentWeapon)
                    {
                        if (bPressed)
                        {
                            ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("I'm not holding anything!", this.transform);
                        }
                    }
                }
            }
            else
            {
                if (CurrentWeapon)
                {
                    if (CurrentWeapon.bCanDrop)
                    {
                        if (CurrentWeapon.OwningWeaponManager)
                        {
                            CurrentWeapon.OwningWeaponManager.RemoveWeapon(CurrentWeapon);
                        }
                        else if (CurrentWeapon.OwningInventory)
                        {
                            CurrentWeapon.OwningInventory.RemoveWeapon(CurrentWeapon, -1);
                        }
                    }
                    else
                    {
                        ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("I can't place this.", this.transform);
                    }
                }
                else
                {
                    if (bPressed)
                    {
                        ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("I'm not holding anything!", this.transform);
                    }
                }
            }
        }
        else if (CurrentUseMode == WeaponUseMode.USING || (CurrentUseMode == WeaponUseMode.PLACING && bForceAttack))
        {

            // TODO dont use place layer mask maybe have weapon manager's own thing
            if (ItemPlacingManager)
            {
                // This is for ziplines mostly.
                if (bPressed && !CurrentWeapon && ItemPlacingManager.bIsPlacing && ItemPlacingManager.CanPlaceObject())
                {
                    ItemPlacingManager.PlaceObject();
                }

                // Switch to fists if for some reason it's not fists right now
                if (DefaultEmptyWeapon)
                {
                    if(!CurrentWeapon)
                    {
                        SwitchToWeapon(DefaultEmptyWeapon);
                    }
                }
                else if(DefaultEmptyWeaponRuntimeID > 0)
                {
                    SearchForWeapon(DefaultEmptyWeaponRuntimeID);
                }

                if (CurrentWeapon)
                {
                    DoInteract(bPressed, false);
                }
                else
                {
                    if (bPressed)
                    {
                        ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("I'm not holding anything!", this.transform);
                    }
                }
            }

            if (CurrentWeapon != DefaultEmptyWeapon && CurrentWeapon && CurrentWeapon.OwningInventory && CurrentWeapon.OwningInventory.InventoryUI)
            {
                CurrentWeapon.OwningInventory.InventoryUI.InteractInventorySlots(CurrentWeapon);
                //Debug.LogError("Vibrate slot");
            }
        }
    }

    private bool bPreviousOverObject;

    public override void ManagedUpdate(float DeltaTime)
    {
        base.ManagedUpdate(DeltaTime);

        if (OwningPlayerCharacter)
        {
            if(OwningPlayerCharacter.PlayerController)
            {
                if(OwningPlayerCharacter.PlayerController.isLocalPlayer)
                {
                    bool OverObject = EventSystem.current.IsPointerOverGameObject();

                    if (!OverObject)
                    {
                        if (CurrentWeapon && CurrentWeapon.PrimaryInteractionUpdateDelegate != null)
                        {
                            DoInteract(bPrimaryPressed, true, true);
                        }
                    }
                }
            }
            else
            {
                if (CurrentWeapon && CurrentWeapon.OwningWeaponManager == this && CurrentWeapon.AttachToSocket)
                {
                    if (WeaponAttachSocket && CurrentWeapon.transform.parent != WeaponAttachSocket.transform)
                    {
                        CurrentWeapon.SetWeaponToSocket();
                    }
                }
            }
        }
    }

    RaycastHit OutHit;

    public RaycastHit GetLastRaycastHit()
    {
        return OutHit;
    }

    void DoInteract(bool bPressed, bool bUpdate = false, bool bPrimary = true)
    {
        if (CurrentWeapon)
        {
            RaycastMouseInput(0.1f, CurrentWeapon.WeaponAimRayCollisionMask, out OutHit);

            if (CurrentWeapon == DefaultEmptyWeapon)
            {
                if (HRNetworkManager.IsHost())
                {
                    SearchForWeapon_ClientRpc();
                }
                else
                {
                    SearchForWeapon_Command();
                }
            }

            if (CurrentWeapon)
            {
                if (!bUpdate)
                {
                    if(bPrimary)
                    {
                        CurrentWeapon.PrimaryInteract(bPressed, OutHit);
                    }
                    else
                    {
                        CurrentWeapon.SecondaryInteract(bPressed, OutHit);
                    }

                    WeaponUseDelegate?.Invoke(this, CurrentWeapon);
                }
                else
                {
                    if(bPrimary)
                    {
                        CurrentWeapon.PrimaryInteractUpdate(bPressed, OutHit);
                    }
                    else
                    {
                        CurrentWeapon.SecondaryInteractUpdate(bPressed, OutHit);
                    }
                }
            }
        }
    }

    virtual public bool CanAddWeapon(BaseWeapon InWeapon)
    {
        if (HotKeyInventory && HotKeyInventory.GetFirstFreeSlot(InWeapon) != -1)
        {
            return true;
        }
        if (MainInventory && MainInventory.GetFirstFreeSlot(InWeapon) != -1)
        {
            return true;
        }
        return false;
    }

    public bool IsReadyToPickupDroppable()
    {
        return Time.timeSinceLevelLoad - LastPickupTimestamp > DroppablePickupDelay;
    }

    //Used for gathering droppables
    public void SetLastPickupTimestamp(float InLastPickupTimestamp)
    {
        LastPickupTimestamp = InLastPickupTimestamp;
    }

    virtual public bool AttemptPickupWeapon(BaseWeapon WeaponToPickUp)
    {
        if (!WeaponToPickUp.bCanPickup)
        {
            if (gameObject.CompareTag("Player"))
            {
                HRNotificationSystem NotificationSystem = ((HRGameInstance)BaseGameInstance.Get)?.FloatingNotificationSystem;

                if (NotificationSystem)
                {
                    NotificationSystem.AddNotification(new HRFloatingNotification(null, "I can't pick that up.", 2.0f, 5, BaseGameInstance.Get.GetFirstPawn().gameObject));
                }
            }

            return false;
        }

        // Don't do anything if already equipping weapon.
        if (CurrentWeapon == WeaponToPickUp)
        {
            //RemoveWeapon(CurrentWeapon);
            return false;
        }

        if (WeaponToPickUp.CanPickUp(this))
        {
            return DoPickup(WeaponToPickUp);
        }
        else
        {
            if (WeaponToPickUp.bNonRealPickup)
            {
                Destroy(WeaponToPickUp.gameObject);
                return false;
            }
        }

        if (gameObject.CompareTag("Player"))
        {
            HRNotificationSystem NotificationSystem = ((HRGameInstance)BaseGameInstance.Get)?.FloatingNotificationSystem;

            if (NotificationSystem)
            {
                NotificationSystem.AddNotification(new HRFloatingNotification(null, "I can't pick that up.", 2.0f, 5, BaseGameInstance.Get.GetFirstPawn().gameObject));
            }
        }

        return false;
    }


    /// <summary>
    /// Throws the current weapon at the given direction.
    /// </summary>
    /// <param name="InThrowAngle"></param>
    public void ThrowWeapon(GameObject InSource, Vector3 InThrowDirection)
    {
        if (CanThrow())
        {
            BaseWeapon CachedWeapon = CurrentWeapon;

            Vector3 PositionToUse = CachedWeapon.transform.position;
            Quaternion RotationToUse = CachedWeapon.transform.rotation;

            if (CachedWeapon.StackCount <= 1)
            {
                CachedWeapon.ProjectilePhysics?.BeginThrow(InSource, InThrowDirection, PositionToUse, RotationToUse);
            }
            else
            {
                var count = CachedWeapon.StackCount - 1;
                var ID = CachedWeapon.ItemID;
                var inventory = HotKeyInventory;
                var index = CurrentSelectedHotKeySlot;

                if (HRNetworkManager.IsHost())
                {
                    ThrowWeapon_Implementation(ID, index, count, inventory, CachedWeapon, InSource, PositionToUse, RotationToUse, InThrowDirection);
                }
                else
                {
                    ThrowWeapon_Command(ID, index, count, inventory, CachedWeapon, InSource, PositionToUse, RotationToUse, InThrowDirection);
                }
            }

            SwitchToSlot(CurrentSelectedHotKeySlot);
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    private void ThrowWeapon_Command(int ID, int index, int count, BaseInventory inventory, BaseWeapon weapon, GameObject thrower, Vector3 position, Quaternion rotation, Vector3 direction)
    {
        ThrowWeapon_Implementation(ID, index, count, inventory, weapon, thrower, position, rotation, direction);
    }


    private void ThrowWeapon_Implementation(int ID, int index, int count, BaseInventory inventory, BaseWeapon weapon, GameObject thrower, Vector3 position, Quaternion rotation, Vector3 direction)
    {
        if (weapon)
        {
            weapon.SetStackCount(count);
            BaseWeapon NewWeapon = Instantiate((HRGameInstance.Get as HRGameInstance).ItemDB.ItemArray[ID].ItemPrefab, position, rotation, null)?.GetComponent<BaseWeapon>();

            if (NewWeapon)
            {
                Mirror.NetworkServer.Spawn(NewWeapon.gameObject);
                NewWeapon.ProjectilePhysics.BeginThrow(thrower, direction, position, rotation);
            }
        }
    }


    public void AttemptThrow()
    {
        SwitchToWeapon(HotKeyInventory.InventorySlots[CurrentSelectedHotKeySlot].SlotWeapon);
    }


    public void EndThrow()
    {
        bIsThrowing = false;
        bFinalThowing = false;
    }


    public bool CanThrow()
    {
        if (!bCanThrow)
        {
            return false;
        }

        var CachedWeapon = CurrentWeapon;

        if (CachedWeapon && CachedWeapon != DefaultEmptyWeapon && (!CachedWeapon.WeaponMeleeComponent ||
               (!CachedWeapon.WeaponMeleeComponent.GetIsSwinging() && !CachedWeapon.WeaponMeleeComponent.IsChargingHeavyAttack())) &&
               (!CachedWeapon.GetComponent<BaseWeaponConsumable>() || !CachedWeapon.GetComponent<BaseWeaponConsumable>().IsConsuming))
        {
            return true;
        }

        return false;
    }

    bool DoPickup(BaseWeapon InWeapon)
    {
        // Pick up the weapon
        if (AddWeapon(InWeapon))
        {
            return true;
        }
        else
        {
            InWeapon.CancelPickupEffects();
        }

        return false;
    }

    public bool HasWeapon(BaseWeapon weapon)
    {
        if (!weapon.OwningInventory)
        {
            return false;
        }
        if (HotKeyInventory && weapon.OwningInventory == HotKeyInventory)
        {
            return true;
        }
        if (MainInventory && weapon.OwningInventory == MainInventory)
        {
            return true;
        }
        if (EquipmentInventory && weapon.OwningInventory == EquipmentInventory)
        {
            return true;
        }
        return false;
    }

    public void StartPlacingWeapon(BaseWeapon WeaponToPlace)
    {
        if (!WeaponToPlace) return;
        if (!ItemPlacingManager) return;
        if (CurrentUseMode != WeaponUseMode.PLACING) return;

        if(this.hasAuthority)
            ItemPlacingManager.StartPlacingObject(WeaponToPlace.PlaceableComponent);
    }

    public void SwitchToWeapon(BaseWeapon WeaponToSwitchTo, bool bEquip = true)
    {
        // Switch to fists weapon
        if (!WeaponToSwitchTo)
        {
            SearchForWeapon(DefaultEmptyWeaponRuntimeID);
            WeaponToSwitchTo = DefaultEmptyWeapon;
        }

        if(OwningPlayerCharacter != null) // Need this because of turrets
        {
            HRPlayerController HRPC = ((HRPlayerController)(OwningPlayerCharacter.PlayerController));
            if (HRPC && HRPC.PlayerUI && HRPC.PlayerUI.AttributeHoverUI && WeaponToSwitchTo)
            {
                HRPC.PlayerUI.AttributeHoverUI.RefreshCurrentAttributeHoverUI(WeaponToSwitchTo.AttributeManager);
            }
        }



        BaseWeapon OldWeapon = CurrentWeapon;

        if (CurrentWeapon && CurrentWeapon != WeaponToSwitchTo)
        {
            if (CurrentWeapon.OwningWeaponManager)
            {
                // Hide weapon since you are putting it in your hotkey.
                //TODO: Commented out below since it caused an issue on clients
                CurrentWeapon.HandleEquip(false, this/*, HasWeapon(CurrentWeapon)*/);
            }
        }

        SetCurrentWeapon(WeaponToSwitchTo);

        if (CurrentWeapon)
        {
            if (CurrentUseMode == WeaponUseMode.PLACING)
            {
                if (ItemPlacingManager && this.hasAuthority)
                {
                    if (DragInventory && DragInventory.IsInventoryEmpty())
                        ItemPlacingManager.StartPlacingObject(CurrentWeapon.PlaceableComponent);
                }
            }
            else if (CurrentUseMode == WeaponUseMode.USING)
            {

            }

            // Attach and show weapon.
            // TODO: Check if equipped
            if (OldWeapon != CurrentWeapon)
            {
                CurrentWeapon.HandleEquip(true, this);

                if (!bEquip)
                {
                    CurrentWeapon.gameObject.SetActive(false);
                }
            }
            // Todo: MOVE BACK If doesn't work

            // Set stance based on weapon type. For now, just do 2 hand.

            if (OwningPlayerCharacter && ((HeroPlayerCharacter)OwningPlayerCharacter) && ((HeroPlayerCharacter)OwningPlayerCharacter).PlayerAllyHighlightEffect)
            {
                ((HeroPlayerCharacter)OwningPlayerCharacter).PlayerAllyHighlightEffect.Refresh();
            }
        }
    }

    public void SwitchToNextSlot()
    {
        SwitchToSlot(Mathf.Clamp(CurrentSelectedHotKeySlot + 1, 0, HotKeyInventory.UnlockedSlots - 1));
    }

    public void SwitchToPreviousSlot()
    {
        SwitchToSlot(Mathf.Clamp(CurrentSelectedHotKeySlot - 1, 0, HotKeyInventory.UnlockedSlots - 1));
    }

    public void SwitchToSlot(int WeaponSlot)
    {
        int OldSlot = CurrentSelectedHotKeySlot;

        if (HRNetworkManager.IsHost())
        {
            SetCurrentSlot(WeaponSlot);
        }
        else
        {
            SwitchToSlot_Command(WeaponSlot);
        }

        if (WeaponSlot < HotKeyInventory.UnlockedSlots && WeaponSlot < HotKeyInventory.InventorySlots.Count
            && WeaponSlot >= 0)
        {
            SwitchToWeapon(HotKeyInventory.InventorySlots[WeaponSlot].SlotWeapon);
        }

        // Move the hotkey UI selector to the correct slot
        HotKeyInventory.HighlightSlot(WeaponSlot, BaseInventory.InventoryHighlightType.Hotkey);
        HotkeySlotSelectedDelegate?.Invoke(this, OldSlot, WeaponSlot);
    }

    public void SetCurrentSlot(int slot)
    {
        SetCurrentSlot_Implementation(slot);
    }

    private void SetCurrentSlot_Implementation(int slot)
    {
        CurrentSelectedHotKeySlot = slot;
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void SwitchToSlot_Command(int WeaponSlot)
    {
        SetCurrentSlot_Implementation(WeaponSlot);
    }

    public bool IsFull(bool bIgnoreStackCount = false, bool bCheckEquipment = false)
    {
        if (HotKeyInventory && !HotKeyInventory.IsInventoryFull(bIgnoreStackCount))
        {
            return false;
        }
        if (MainInventory && !MainInventory.IsInventoryFull(bIgnoreStackCount))
        {
            return false;
        }
        if (bCheckEquipment && EquipmentInventory && !EquipmentInventory.IsInventoryFull(bIgnoreStackCount))
        {
            return false;
        }

        return true;
    }

    public void SetCanSwitchWeapons(bool bCanSwitch)
    {
        bCanSwitchWeapons = bCanSwitch;
    }

    public bool CanSwitchWeapons()
    {
        return bCanSwitchWeapons;
    }

    public void KeyCodeSlotSelected(int KeyCodeSlot)
    {
        // The int that is inputted is equal to the keyboard numeric buttons. So, 1 should mean switching to slot 0.

        // Should probably move this to switchtoslot or something instead of this.
        // Do not switch if melee'ing
        if(!CanSwitchWeapons())
        {
            return;
        }

        if (CurrentWeapon && CurrentWeapon.WeaponMeleeComponent)
        {
            if (CurrentWeapon.WeaponMeleeComponent.GetIsSwinging())
            {
                return;
            }

            if (CurrentWeapon.WeaponMeleeComponent.IsChargingHeavyAttack())
            {
                return;
            }

            if (!GetCanCancelRoll())
            {
                return;
            }

            if (CurrentWeapon.WeaponBlockerComponent && CurrentWeapon.WeaponBlockerComponent.IsBlocking())
            {
                return;
            }
        }

        //Reset currently held item
        if (OwningPlayerCharacter)
        {
            HeroPlayerCharacter HPC = OwningPlayerCharacter as HeroPlayerCharacter;
            HPC.InventoryManager.ResetDragWeapon();
        }

        SwitchToSlot(KeyCodeSlot - 1);
    }

    private void SetCurrentWeapon(BaseWeapon NewWeapon)
    {
        if (NewWeapon)
        {
            if (CurrentUseMode == WeaponUseMode.USING)
            {
                //SwitchToUsingContextDelegate?.Invoke(UseItemContextInfo, true);
                //SwitchToUsingContextDelegate?.Invoke(BlockItemContextInfo, true);
            }
            else
            {
                //SwitchToUsingContextDelegate?.Invoke(UseItemContextInfo, false);
                //SwitchToUsingContextDelegate?.Invoke(BlockItemContextInfo, false);
            }

            if (bShouldHideWeapon)
            {
                HideWeaponMeshRenderer(CurrentWeapon, false);
                HideWeaponMeshRenderer(NewWeapon, true);
            }

            if (CurrentWeapon)
            {
                UnequipWeaponDelegate?.Invoke(this, CurrentWeapon);
            }

            //NewWeapon.transform.localPosition = NewWeapon.LocalSocketPositionOffset;

            EquipWeaponDelegate?.Invoke(this, NewWeapon);
        }
        else
        {
            //SwitchToUsingContextDelegate?.Invoke(UseItemContextInfo, false);
            //SwitchToUsingContextDelegate?.Invoke(BlockItemContextInfo, false);

            if (bShouldHideWeapon)
            {
                HideWeaponMeshRenderer(CurrentWeapon, false);
            }

            UnequipWeaponDelegate?.Invoke(this, CurrentWeapon);
        }

        if (NewWeapon == null)
        {
            if (CurrentWeapon && ItemPlacingManager && ItemPlacingManager.CurrentPlaceableGameObject && ItemPlacingManager.CurrentPlaceableGameObject.gameObject == CurrentWeapon.gameObject)
            {
                // Is in inventory. stoop placing the object
                ItemPlacingManager.StopPlacingObject();
            }
        }
        CurrentWeapon = NewWeapon;
        bSync = false;
    }

    #region clear_weapon

    private void ClearWeapon_Implementation()
    {
        if (CurrentWeapon)
        {
            CurrentWeapon.HandleEquip(false, this);
            SetCurrentWeapon(null);
        }
    }

    [ClientRpc]
    private void ClearWeapon_ClientRpc()
    {
        ClearWeapon_Implementation();
    }

    [Command(ignoreAuthority = true)]
    private void ClearWeapon_Command()
    {
        ClearWeapon_ClientRpc();
    }

    private void DoClearWeapon()
    {
        if (HRNetworkManager.IsHost())
        {
            ClearWeapon_ClientRpc();
        }
        else
        {
            ClearWeapon_Command();
        }
    }

    public void ClearWeapon()
    {
        SwitchToSlot(-1);
    }

    #endregion

    // This is for unity events.
    public bool AddWeapon(BaseWeapon WeaponToAdd)
    {
        if (WeaponToAdd)
        {
            return AddWeapon(WeaponToAdd, WeaponToAdd.StackCount, -1);
        }
        return false;
    }

    public bool AddWeaponStack(BaseWeapon Weapon, int Amount, int Index, bool bPlayFX = true)
    {
        if (Weapon && Amount > 0)
        {
            if (!AddWeapon(Weapon, Amount, Index, bPlayFX))
            {
                return false;
            }
        }
        return true;
    }

    public void FullInventoryNotification()
    {
        HRNotificationSystem NotificationSystem = ((HRGameInstance)BaseGameInstance.Get)?.FloatingNotificationSystem;

        ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("My inventory is full!", this.transform);
    }

    public void InvalidCraftNotification()
    {
        HRNotificationSystem NotificationSystem = ((HRGameInstance)BaseGameInstance.Get)?.FloatingNotificationSystem;

        ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("Input inventory is full!", this.transform);
    }

    // Adds the weapon to the inventory.
    public bool AddWeapon(BaseWeapon WeaponToAdd, int Amount, int Slot = -1, bool bPlayFX = true)
    {
        if (WeaponToAdd == null || Amount <= 0)
            return false;

        if (HRNetworkManager.IsHost())
        {
            if (WeaponToAdd.RequestingWeaponManager || WeaponToAdd.OwningWeaponManager)
            {
                return false;
            }
            return AddWeapon_Implementation(WeaponToAdd, Amount, Slot, bPlayFX);
        }
        else if (HRNetworkManager.HasControl(this.netIdentity))
        {
            RequestAddWeapon_Command(WeaponToAdd, Amount, Slot, bPlayFX);
        }

        return true;
    }

    [Command(ignoreAuthority = true)]
    private void RequestAddWeapon_Command(BaseWeapon WeaponToAdd, int Amount, int Slot, bool bPlayFX)
    {
        if (WeaponToAdd == null || WeaponToAdd.RequestingWeaponManager || WeaponToAdd.OwningWeaponManager)
        {
            return;
        }
        WeaponToAdd.RequestingWeaponManager = this;
        AddWeapon_TargetRpc(WeaponToAdd, Amount, Slot, bPlayFX);
    }

    [TargetRpc]
    private void AddWeapon_TargetRpc(BaseWeapon WeaponToAdd, int Amount, int Slot, bool bPlayFX)
    {
        AddWeapon_Implementation(WeaponToAdd, Amount, Slot, bPlayFX);
    }

    private bool AddWeapon_Implementation(BaseWeapon WeaponToAdd, int Amount, int Slot, bool bPlayFX)
    {
        if (!WeaponToAdd)
        {
            return false;
        }

        if(HRNetworkManager.IsHost())
        {
            WeaponToAdd.WeaponAttemptPickupDelegate_Server?.Invoke(WeaponToAdd, this);
        }

        WeaponToAdd.ResetAddWeaponRequest();

        WeaponToAdd.SetStackCount(Amount);
        int Remainder = Amount;
        //Tuples to store the slot index and how much to put there
        List<Tuple<int, int>> HotkeySlotAmountTuples = new List<Tuple<int, int>>();
        List<Tuple<int, int>> MainSlotAmountTuples = new List<Tuple<int, int>>();
        //First find all the stacks, prioritizing hotkey inventory
        Remainder = CalculateInventoryStackSlots(WeaponToAdd, Remainder, HotKeyInventory, ref HotkeySlotAmountTuples);
        Remainder = CalculateInventoryStackSlots(WeaponToAdd, Remainder, MainInventory, ref MainSlotAmountTuples);
        //If there's any remaining put into the first empty slot, prioritizing hotkey inventory
        Remainder = CalculateInventoryFreeSlot(WeaponToAdd, Remainder, HotKeyInventory, ref HotkeySlotAmountTuples);
        Remainder = CalculateInventoryFreeSlot(WeaponToAdd, Remainder, MainInventory, ref MainSlotAmountTuples);

        for (int i = 0; i < HotkeySlotAmountTuples.Count; i++)
        {
            HotKeyInventory.AddWeapon(WeaponToAdd, HotkeySlotAmountTuples[i].Item1, Amount: HotkeySlotAmountTuples[i].Item2);
        }

        for (int i = 0; i < MainSlotAmountTuples.Count; i++)
        {
            MainInventory.AddWeapon(WeaponToAdd, MainSlotAmountTuples[i].Item1, Amount: MainSlotAmountTuples[i].Item2);
        }
        int AmountAdded = Amount - Remainder;
        bool bAddedWeapon = AmountAdded > 0;

        if (bAddedWeapon)
        {
            if (bPlayFX)
            {
                if (gameObject.CompareTag("Player"))
                {
                    if (WeaponToAdd && WeaponToAdd.bShowScrollingNotification)
                    {
                        ShowWeaponNotification(WeaponToAdd, AmountAdded);
                    }
                }
            }
        }
        else
        {
            HRMoneyPickup moneyPickup = WeaponToAdd.GetComponentInChildren<HRMoneyPickup>();
            if (moneyPickup)
            {
                HeroPlayerCharacter playerCharacter = OwningPlayerCharacter as HeroPlayerCharacter;
                if (playerCharacter)
                {
                    moneyPickup.PickupMoney(playerCharacter.WeaponManager);
                }
                return true;
            }

            if (gameObject.CompareTag("Player"))
            {
                FullInventoryNotification();
            }
            return false;
        }

        return true;
    }

    int CalculateInventoryStackSlots(BaseWeapon WeaponToAdd, int RemainingAmount, BaseInventory Inventory, ref List<Tuple<int, int>> InventorySlotAmountTuples)
    {
        //Check for all stacks in the inventory
        if (!Inventory) return RemainingAmount;
        if (Inventory.GetSlotIndicesWithWeapon(WeaponToAdd.ItemID, out List<int> StackSlotIndices))
        {
            int SlotIndex;
            int SlotRemainder;
            int AmountToAdd;
            BaseWeapon SlotWeapon;
            for (int i = 0; i < StackSlotIndices.Count; i++)
            {
                SlotIndex = StackSlotIndices[i];
                SlotWeapon = Inventory.InventorySlots[SlotIndex].SlotWeapon;
                SlotRemainder = SlotWeapon.StackLimit - SlotWeapon.StackCount;
                AmountToAdd = Mathf.Min(SlotRemainder, RemainingAmount);
                if (Inventory.CanInsertWeaponIntoSlot(WeaponToAdd.ItemID, SlotIndex, InCustomStackCount: AmountToAdd))
                {
                    //Add to our list to keep track of slot index and amount
                    InventorySlotAmountTuples.Add(new Tuple<int, int>(SlotIndex, AmountToAdd));
                    RemainingAmount -= AmountToAdd;
                    if (RemainingAmount == 0)
                    {
                        break;
                    }
                }
            }
        }
        return RemainingAmount;
    }

    int CalculateInventoryFreeSlot(BaseWeapon WeaponToAdd, int Amount, BaseInventory Inventory, ref List<Tuple<int, int>> InventorySlotAmountTuples)
    {
        if (!Inventory) return Amount;
        //No stacks left, add to empty slot if there is one
        if (Amount != 0)
        {
            int FreeSlotIndex = Inventory.GetFirstFreeSlot(WeaponToAdd, true, InCustomStackCount: Amount);
            if (FreeSlotIndex != -1)
            {
                //Add to our list to keep track of slot index and amount
                InventorySlotAmountTuples.Add(new Tuple<int, int>(FreeSlotIndex, Amount));
                return 0;
            }
        }
        return Amount;
    }

    void ShowWeaponNotification(BaseWeapon WeaponToAdd, int Amount)
    {
        if (WeaponToAdd == null || Amount <= 0)
        {
            return;
        }

        HRNotificationSystem NotificationSystem = ((HRGameInstance)BaseGameInstance.Get)?.ScrollingNotificationSystem;
        if (NotificationSystem)
        {
            Sprite[] InSprites = new Sprite[]
            {
                WeaponToAdd.WeaponSprite,
                ((HRGameInstance)BaseGameInstance.Get).EncyclopediaSystem.GetShownFirst(WeaponToAdd.ItemID) ?
                        NotificationSystem.MaskOff : NotificationSystem.MaskOn
            };

            var Message = NotificationSystem.FindNotificationByType(WeaponToAdd.ItemName);

            if (Message != null && Message.Message != null && WeaponToAdd.StackLimit > 1)
            {
                int OldAmount = int.Parse(Message.Message.Substring(0, Message.Message.IndexOf('x')));

                Message.Message = (Amount + OldAmount).ToString() + "x " +
                    HRItemDatabase.GetLocalizedItemName(WeaponToAdd.ItemName.Replace(" ", WeaponToAdd.ItemName).ToLower(),
                    WeaponToAdd.ItemName);
                Message.OnNotificationInfoChangedDelegate?.Invoke(Message);
            }
            else
            {
                ((HRGameInstance)BaseGameInstance.Get).EncyclopediaSystem.SetShownFirst(WeaponToAdd.ItemID);

                NotificationSystem.AddNotification(
                    new HRScrollingNotification(null, 
                        Amount.ToString() + "x " + HRItemDatabase.GetLocalizedItemName(WeaponToAdd.ItemName.Replace(" ", WeaponToAdd.ItemName).ToLower(),WeaponToAdd.ItemName), 
                        InSprites, 4f, 10, WeaponToAdd.ItemName), 
                 bIgnorePriority: true, bForceInsertToQueue: true);
            }
        }
    }

    public bool ContainsWeaponID(int ItemID, int Amount, out int Remaining, bool bCheckEquipmentInventory = false)
    {
        Remaining = Amount;

        if (HotKeyInventory)
        {
            Remaining = HotKeyInventory.ContainsWeapon(ItemID, Remaining);
        }
        if (MainInventory && Remaining > 0)
        {
            Remaining = MainInventory.ContainsWeapon(ItemID, Remaining);
        }
        if (bCheckEquipmentInventory && EquipmentInventory && Remaining > 0)
        {
            Remaining = EquipmentInventory.ContainsWeapon(ItemID, Remaining);
        }

        return Remaining == 0;
    }


    public BaseWeapon GetWeaponByID(int ItemID, bool bCheckEquipmentInventory = false)
    {
        BaseWeapon weapon = null;

        if (HotKeyInventory)
        {
            if (weapon = HotKeyInventory.GetWeaponByID(ItemID)) return weapon;
        }
        if (MainInventory)
        {
            if (weapon = MainInventory.GetWeaponByID(ItemID)) return weapon;
        }
        if (bCheckEquipmentInventory && EquipmentInventory)
        {
            if (weapon = EquipmentInventory.GetWeaponByID(ItemID)) return weapon;
        }

        return null;
    }

    public int GetWeaponIDCount(int ItemID, bool bCheckEquipmentInventory = false)
    {
        int Total = 0;

        if (HotKeyInventory)
        {
            Total += HotKeyInventory.GetItemCount(ItemID);
        }
        if (MainInventory)
        {
            Total += MainInventory.GetItemCount(ItemID);
        }
        if (bCheckEquipmentInventory && EquipmentInventory)
        {
            Total += EquipmentInventory.GetItemCount(ItemID);
        }

        return Total;
    }

    public bool RemoveWeaponByID(int ItemID, int Amount, bool bRemoveFromEquipmentInventory = false, bool bChangeWeaponTransform = true, bool bDropWeapon = true, bool bDestroyWeapon = false)
    {
        bool bRemoveAll = Amount == -1;
        int Remaining = 0;

        if (HotKeyInventory)
        {
            HotKeyInventory.RemoveWeaponByID(ItemID, Amount, out Remaining, bChangeWeaponTransform, bDropWeapon, true, bDestroyWeapon);
        }
        if (MainInventory && (bRemoveAll || Remaining > 0))
        {
            MainInventory.RemoveWeaponByID(ItemID, bRemoveAll ? Amount : Remaining, out Remaining, bChangeWeaponTransform, bDropWeapon, false, bDestroyWeapon);
        }
        if (bRemoveFromEquipmentInventory && EquipmentInventory && (bRemoveAll || Remaining > 0))
        {
            EquipmentInventory.RemoveWeaponByID(ItemID, bRemoveAll ? Amount : Remaining, out Remaining, bChangeWeaponTransform, bDropWeapon, false, bDestroyWeapon);
        }

        return bRemoveAll || Remaining == 0;
    }

    /// <summary> Adds stacks to a weapon if possible, will make new item if it does not exist already </summary>
    public bool AddWeaponByID(int ItemID, int Amount)
    {
        //Debug.Log("Adding");
        BaseWeapon weapon = GetWeaponByID(ItemID);
        if (weapon)
        {
            weapon.OwningInventory.AddWeapon(weapon, -1, false, true, Amount);
        }
        else
        {
            //Debug.Log("Add Spawn");
            SpawnItemInInventoryByID(ItemID, Amount);
        }
        return true;
    }

    [Command(ignoreAuthority = true)]
    private void SpawnItemInInventoryByID(int ItemID, int Amount)
    {
        GameObject newWeapon = Instantiate(((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemArray[ItemID].ItemPrefab);
        newWeapon.transform.position = transform.position + (transform.forward);
        NetworkServer.Spawn(newWeapon);
        bool added = AddWeapon(newWeapon.GetComponent<BaseWeapon>(), Amount);
        //Debug.Log("Added? "+added);
    }

    public BaseWeapon TryRemovingFromInventory(BaseInventory InInventory, BaseWeapon WeaponToRemove, bool bChangeWeaponTransform = true, int Amount = -1, bool bDropWeapon = true, bool bSpawnNew = true, bool bIsPlacing = false)
    {
        BaseWeapon weapon = null;
        if (InInventory)
        {
            if (ItemPlacingManager && ItemPlacingManager.CurrentGhostObject)
            {
                InInventory.Temp_PlacingPosition = ItemPlacingManager.CurrentGhostObject.transform.position;
                InInventory.Temp_PlacingRotation = ItemPlacingManager.CurrentGhostObject.transform.rotation;
            }

            weapon = InInventory.RemoveWeapon(WeaponToRemove, Amount, bChangeWeaponTransform, bDropWeapon, true, false, bSpawnNew, bIsPlacing);
        }

        return weapon;
    }

    // Removes the weapon from the inventory.
    public BaseWeapon RemoveWeapon(BaseWeapon WeaponToRemove, bool bChangeWeaponTransform = true, int Amount = -1, bool bDropWeapon = true, bool bSpawnNew = true, bool bIsPlacing = false)
    {
        BaseWeapon weapon = null;
        if (!WeaponToRemove)
        {
            return weapon;
        }

        weapon = TryRemovingFromInventory(HotKeyInventory, WeaponToRemove, bChangeWeaponTransform, Amount, bDropWeapon, bSpawnNew, bIsPlacing);
        if (!weapon)
        {
            weapon = TryRemovingFromInventory(MainInventory, WeaponToRemove, bChangeWeaponTransform, Amount, bDropWeapon, bSpawnNew, bIsPlacing);
        }
        else if (!weapon)
        {
            TryRemovingFromInventory(EquipmentInventory, WeaponToRemove, bChangeWeaponTransform, Amount, bDropWeapon, bSpawnNew, bIsPlacing);
        }

        return weapon;
    }

    /// <summary>
    /// Gets the weapon in the current hotkey slot.
    /// </summary>
    /// <param name="slot"></param>
    /// <returns>The Base weapon at a given slot.</returns>
    public BaseWeapon GetWeaponInHotkeySlot(int slot)
    {
        if (slot >= HotKeyInventory.InventorySlots.Count
            || slot < 0)
        {
            return null;
        }
        return HotKeyInventory.InventorySlots[slot].SlotWeapon;
    }
    public void GetAllWeapons(out List<int> ItemIDs, out List<int> ItemAmounts, bool bIncludeEquipmentInventory = false)
    {
        ItemIDs = new List<int>();
        ItemAmounts = new List<int>();
        if (HotKeyInventory)
        {
            for (int i = 0; i < HotKeyInventory.InventorySlots.Count; ++i)
            {
                if (HotKeyInventory.InventorySlots[i].SlotWeapon)
                {
                    ItemIDs.Add(HotKeyInventory.InventorySlots[i].SlotWeapon.ItemID);
                    ItemAmounts.Add(HotKeyInventory.InventorySlots[i].SlotWeapon.StackCount);
                }
            }
        }
        if (MainInventory)
        {
            for (int i = 0; i < MainInventory.InventorySlots.Count; ++i)
            {
                if (MainInventory.InventorySlots[i].SlotWeapon)
                {
                    ItemIDs.Add(MainInventory.InventorySlots[i].SlotWeapon.ItemID);
                    ItemAmounts.Add(MainInventory.InventorySlots[i].SlotWeapon.StackCount);
                }
            }
        }
        if (bIncludeEquipmentInventory && EquipmentInventory)
        {
            for (int i = 0; i < EquipmentInventory.InventorySlots.Count; ++i)
            {
                if (EquipmentInventory.InventorySlots[i].SlotWeapon)
                {
                    ItemIDs.Add(EquipmentInventory.InventorySlots[i].SlotWeapon.ItemID);
                    ItemAmounts.Add(EquipmentInventory.InventorySlots[i].SlotWeapon.StackCount);
                }
            }
        }
    }

    public bool DropCurrentWeapon()
    {
        if (CurrentWeapon)
        {
            // Instead check if a weapon is placeable
            if (CurrentWeapon.bCanDrop)
            {
                RemoveWeapon(CurrentWeapon);
                CurrentWeapon.ProjectilePhysics?.BeginDropThrow();
                return true;
            }
            else
            {
                CurrentWeapon.DropError();
            }
        }

        return false;
    }

    public void VoidDropCurrentWeapon()
    {
        DropCurrentWeapon();
    }

    public override void Awake()
    {
        RegisterType = ManagedRegisterType.Update;

        base.Awake();

        if (((HRGameInstance)BaseGameInstance.Get))
        {
            Mirror.NetworkClient.RegisterPrefab(((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemArray[DefaultEmptyWeaponID].ItemPrefab);
        }

        OwningPlayerCharacter = GetComponent<BasePlayerCharacter>();
        if (OwningPlayerCharacter)
        {
            HeroPlayerCharacter HPC = OwningPlayerCharacter as HeroPlayerCharacter;

            HPC.OnStateChangeDelegate += HandleInteractStateChanged;
        }
        else
        {
            Debug.Log("BaseWeaponManager missing OwningPlayerCharacter.");
        }

        if (MainInventory)
        {
            MainInventory.WeaponChangedDelegate += HandleInventoryWeaponChanged;
            MainInventory.bOverrideShowOnAdd = true;
        }


        if (HotKeyInventory)
        {
            HotKeyInventory.WeaponChangedDelegate += HandleInventoryWeaponChanged;
            HotKeyInventory.bOverrideShowOnAdd = true;
        }

        if (EquipmentInventory)
        {
            //EquipmentInventory.WeaponChangedDelegate += Evan;
        }

        SetWeaponSocket(WeaponAttachSocket);
    }

    /*private void Evan(BaseInventory Inventory, int Index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
    {
        if (NewWeapon)
        {
            Debug.LogError(NewWeapon.ItemName);
        }
        if(OldWeapon)
        {
            Debug.LogError("REMOVED: " + OldWeapon.ItemName);
        }
    }*/

    private void HandleInventoryWeaponChanged(BaseInventory Inventory, int Index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
    {
        // If weapon has not spawned yet, wait for it to be spawned in this client and try again
        if (Inventory.InventorySlots.Count > Index && !NewWeapon && Inventory.InventorySlots[Index].netId != 0)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(WaitForWeaponSpawnCoroutine(Inventory, Index, OldWeapon));
            }
            return;
        }

        if(!OldWeapon || !NewWeapon || OldWeapon != NewWeapon)
            InventoryChanged_Implementation(Inventory, Index, OldWeapon, NewWeapon);
    }

    //Called OnWeaponChanged when the weapon is added/taken/swapped
    //This is now also getting called when you start dragging an item
    private void InventoryChanged_Implementation(BaseInventory Inventory, int Index, BaseWeapon OldWeapon, BaseWeapon NewWeapon)
    {
        if (NewWeapon)
        {
            // This is redundant. But is necessary to generate nav mesh now instead of later when it is moved.
            NewWeapon.SetNavObstaclesEnabled(false);
        }

        bool bWeaponEquipped = true;
        

        //If we are removing the currently held weapon
        if (CurrentWeapon == OldWeapon || Index == -1)
        {
            if (!HRNetworkManager.IsHost() && CurrentWeapon != null && NewWeapon == null && NewWeapon != CurrentWeapon)
            {
                if (HotKeyInventory.InventorySlots[CurrentSelectedHotKeySlot].SlotWeapon != CurrentWeapon)
                {
                    //Unequip the currently held weapon
                    CurrentWeapon.HandleEquip(false, this, false);
                    bWeaponEquipped = false;
                }
            }
        }

        //TODO: shouldnt we also unequip on server if weapon is placed in main inventory?
        if (OldWeapon && (OldWeapon.OwningInventory == null || (OldWeapon.OwningInventory != MainInventory && OldWeapon.OwningInventory != HotKeyInventory)))
        {
            //TODO: not sure why client/server unequip need to be in different places
            if (OldWeapon.OwningWeaponManager && HRNetworkManager.IsHost())
            {
                // Hide weapon since you are putting it in your hotkey.
                OldWeapon.HandleEquip(false, this, false);
                bWeaponEquipped = false;
            }

            OldWeapon.OwningWeaponManager = null;

            if (!NewWeapon || OldWeapon != NewWeapon)
            {
                //Fire event to update attributes/recipes/EQS
                OldWeapon.SetWeaponManager(null);
            }
        }

        //Fire weapon removed delegate
        if (!bWeaponEquipped)
        {
            RemovedWeaponDelegate?.Invoke(this, OldWeapon);
        }

        // TODO: This is a jank fix for money drops triggering a weapon swap. In the future this can be a boolean on the weapon that auto-destroys on pickup and would also
        // bypass this too.
         if (NewWeapon == null || NewWeapon.ItemID != 787)
        {
            if (CurrentSelectedHotKeySlot >= 0 && CurrentSelectedHotKeySlot < HotKeyInventory.InventorySlots.Count &&
                (CurrentSelectedHotKeySlot == Index || (CurrentWeapon != HotKeyInventory.InventorySlots[CurrentSelectedHotKeySlot].SlotWeapon)))
            {
                //TODO: still want this?
                SwitchToSlot(CurrentSelectedHotKeySlot);
            }
        }

        if (NewWeapon)
        {
            //TODO: hack to tell if item is being picked up from the ground or not. not perfect and needs to be updated
            bool bIsMovedFromInventory = NewWeapon.PreviousOwningInventory == HotKeyInventory || NewWeapon.PreviousOwningInventory == MainInventory 
                || NewWeapon.PreviousOwningInventory == DragInventory || NewWeapon.PreviousOwningInventory == EquipmentInventory;
            if (NewWeapon.OwningWeaponManager != this)
            {
                NewWeapon.SetWeaponManager(this);
            }

            NewWeapon.transform.hasChanged = true;

            if (NewWeapon.OwningInteractable)
            {
                NewWeapon.OwningInteractable.transform.hasChanged = true;
                NewWeapon.OwningInteractable.SetInteractionCollisionEnabled(false);
            }

            if (AddedWeaponDelegate != null)
            {
                AddedWeaponDelegate(this, NewWeapon);
            }

            //PlayPickupFX(NewWeapon, NewWeapon.transform.position, NewWeapon.transform.rotation);

            NewWeapon.HandlePickup(!bIsMovedFromInventory);

            if (CurrentWeapon != NewWeapon)
            {
                NewWeapon.HandleEquip(false, this);
                NewWeapon.gameObject.SetActive(false);
            }
            else
            {
                 NewWeapon.gameObject.SetActive(true);
            }

        }

        if (((HeroPlayerCharacter)OwningPlayerCharacter).PlayerAllyHighlightEffect)
        {
            ((HeroPlayerCharacter)OwningPlayerCharacter).PlayerAllyHighlightEffect.Refresh();
        }
    }

    public void PlayPickupFX(BaseWeapon InWeapon, Vector3 position, Quaternion rotation)
    {
        if (PickUpParticleEffect && !bJustSpawned)
        {
            HRPoofVFXController PoofFX = FXPoolManager.Get.SpawnVFXFromPool(PickUpParticleEffect, position, rotation)?.GetComponent<HRPoofVFXController>();
            //HRPoofVFXController PoofFX = BaseObjectPoolManager.Get.InstantiateFromPool(PickUpParticleEffect, false, true, position, false, rotation).GetComponent<HRPoofVFXController>();

            if (PoofFX)
            {
                if(InWeapon.WeaponMeshRenderer)
                {
                    PoofFX.SetMeshEmission(InWeapon.WeaponMeshRenderer, InWeapon.WeaponMeshRenderer.material);
                }
            }
        }
    }

    public void PlayPickupLerpFX(BaseWeapon InWeapon)
    {
        if (FakeWeaponLerpEffect && !bJustSpawned)
        {
            var effect = BaseObjectPoolManager.Get.InstantiateFromPool(FakeWeaponLerpEffect.gameObject, false, true, InWeapon.transform.position, false, InWeapon.transform.rotation);
            effect.GetComponent<HRFakeWeaponLerpEffect>()?.PlayEffect(InWeapon, WeaponAttachSocket.transform.position);
        }
    }

    public bool GetCanCancelRoll()
    {
        if (bSwingingCanNotRollCancel)
        {
            if (CurrentWeapon)
            {
                if (CurrentWeapon.WeaponMeleeComponent && (CurrentWeapon.WeaponMeleeComponent.CanCancelHit() ||
                    CurrentWeapon.WeaponMeleeComponent.IsCooldownOver(MeleeCurrentComboCount, false)))
                {
                    bSwingingCanNotRollCancel = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        return true;
    }
    private IEnumerator WaitForWeaponSpawnCoroutine(BaseInventory Inventory, int Index, BaseWeapon OldWeapon)
    {
        Mirror.NetworkIdentity OutNetworkIdentity = null;
        while (Inventory && Inventory.InventorySlots[Index].netId != 0 && OutNetworkIdentity == null)
        {
            if (Mirror.NetworkIdentity.spawned.TryGetValue(Inventory.InventorySlots[Index].netId, out OutNetworkIdentity))
            {
                if (!OldWeapon || !Inventory.InventorySlots[Index].SlotWeapon || 
                    OldWeapon.ItemID != Inventory.InventorySlots[Index].SlotWeapon.ItemID)
                {
                    InventoryChanged_Implementation(Inventory, Index, OldWeapon, Inventory.InventorySlots[Index].SlotWeapon);
                }
                break;
            }
            yield return null;
        }
    }

    bool bClientStarted = false;
    public override void OnStartClient()
    {
        base.OnStartClient();

        bClientStarted = true;

        if (this.CurrentWeapon != null)
        {
            CurrentWeapon.SetWeaponToSocket();
        }
        else
        {
            if (this.isActiveAndEnabled)
                StartCoroutine(SetSocketOnceLoaded());
        }
    }

    private IEnumerator SetSocketOnceLoaded()
    {
        bSync = true;

        yield return new WaitUntil(() => !bSync);


        if (CurrentWeapon != null)
        {
            CurrentWeapon.SetWeaponToSocket();
        }
    }

    public IEnumerator PickupWeaponEndOfFrame(BaseWeapon InGift)
    {
        yield return new WaitForEndOfFrame();
        AttemptPickupWeapon(InGift);
    }

    public void HandleAddedToPool(BaseObjectPoolingComponent PoolingComponent)
    {

    }

    public void HandlePoolInstantiate(BaseObjectPoolingComponent PoolingComponent)
    {

    }

    public void HandleReturnToPool(BaseObjectPoolingComponent PoolingComponent)
    {
        if(!PoolingComponent.bReserved)
        {
            if (MainInventory)
            {
                MainInventory.ClearInventory();
            }

            if (HotKeyInventory)
            {
                HotKeyInventory.ClearInventory();
            }
        }

        StopAllCoroutines();
    }

    void HandleInteractStateChanged(HeroPlayerCharacter InPlayer, HeroPlayerCharacter.InteractState OldState, HeroPlayerCharacter.InteractState NewState)
    {

        if (CheckNonInteractState(NewState))
        {
            ResetMeleeComboCount();
            bMeleeUsedFinalLightAttack = false;
        }
    }

    public void ResetMeleeComboCount()
    {
        MeleeCurrentComboCount = 0;
    }

    private bool CheckNonInteractState(HeroPlayerCharacter.InteractState state)
    {
        return state == HeroPlayerCharacter.InteractState.Stunned
            || state == HeroPlayerCharacter.InteractState.Dying
            || state == HeroPlayerCharacter.InteractState.Dead;
    }


    public void OnDestroy()
    {
        if (MainInventory)
        {
            MainInventory.WeaponChangedDelegate -= HandleInventoryWeaponChanged;
        }


        if (HotKeyInventory)
        {
            HotKeyInventory.WeaponChangedDelegate -= HandleInventoryWeaponChanged;
        }

        if (HRNetworkManager.IsHost())
        {
            if (DefaultEmptyWeapon)
            {
                NetworkServer.Destroy(DefaultEmptyWeapon.gameObject);
            }
        }

        StopAllCoroutines();
    }
}