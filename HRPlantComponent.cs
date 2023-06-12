using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaseScripts;
using Mirror;

[System.Serializable]
public struct HRGrowthPhase
{
    public int DaysToNextPhase;
    public GameObject PhaseMeshObject;
    public GameObject WiltMeshObject;
    public GameObject ParticlePrefab;
    //public BoxCollider PlantCollider;
    public AudioClip SoundClip;
    public Vector3 PhaseScale;
    public float HarvestModifier;
}

[Ceras.SerializedType]
public class HRPlantComponent : Mirror.NetworkBehaviour, IHRSaveable, IBasePool
{
    private const int WET_ATTRIBUTE_ID = 8;

    public delegate void HRPlantComponentSignature(HRPlantComponent InPlantComponent, int OldPhase, int NewPhase);
    public HRPlantComponentSignature OnPlantPhaseChangedDelegate;

    public BaseWeapon OwningWeapon;
    public HRAttributeManager attributeManager;
    public HRElementalDamageManager ElementalDamageManager;
    bool bHasElementalDamageManager;
    //[Tooltip("The maximum number of days the plant can go without water.")]
    //public int MaxDaysWithoutWater = 1;
    public int InitialGrowthPhase;
    public bool bShouldWilt = true;
    public int MaxHoursWiltedUntilDie = 24;
    public float HourlyConsumptionRate = 0.04f;
    public float InGameTimeToGrowFully = 15f;
    public AudioSource SoundSource;
    public bool bStartGrown = true;
    public bool bIsTree;
    public bool bUseScale;
    public bool bNeedsWater = false;
    public bool bIsWatered = false;
    public HRGrowthPhase[] GrowthPhases;
    public HRNeedManager PlantNeeds;
    public BaseReplaceOnDestroy ReplaceOnDestroyRef;

    public GameObject NurturingParticlePrefab;
    public float HoldTimeToNurture = 10;

    public GameObject SeedDropPrefab;
    public float SeedDropMinImpulse = 1;
    public float SeedDropMaxImpulse = 4;

    //int NumDaysPassed = 0;

    [SyncVar(hook = nameof(SetGrowthPhase_Hook))]
    public int CurrentGrowthPhase = 0;

    [System.NonSerialized, Ceras.SerializedField] public bool bIsGrowing;
    [System.NonSerialized, Ceras.SerializedField] public HRDateAndTimeStruct SavedDateAndTime = new HRDateAndTimeStruct();
    int SavedHour = 0;
    bool bIsDirty = false;
    bool bIsCurrentlyWilting;

    private float WateringLevelPercentage = 0.25f;


    private bool bInitialized;
    private float WetStatusTimer = 0f;
    public float SecondsDelayBetweenWatering = 1f;

    public float WoodcuttingExpModifier = 1f;
    public float FarmingExpModifier = 1f;

    public AnimationCurve MasteryStartingAmount;

    public bool bEnableInteractableWhenPlanted = true;
    public bool bEnableMeshCollisionWhenPlanted = true;

    // delegate for when the plant is watered
    public delegate void HRWateredSignature();
    public HRWateredSignature WateredDelegate;

    // delegate for when the plant is done growing
    public delegate void HRDoneGrowingSignature();
    public HRDoneGrowingSignature DoneGrowingDelegate;

    // delegate for when the plant is harvested
    public delegate void HRHarvestedSignature();
    public HRHarvestedSignature HarvestedDelegate;

    HRDayManager DayManager;

    HRFarmingPlot OwningFarmingPlot;

#if UNITY_EDITOR
    void OnValidate()
    {
        if(GrowthPhases != null)
        {
            if (bStartGrown)
            {
                InitialGrowthPhase = GrowthPhases.Length - 1;
            }
            InitialGrowthPhase = Mathf.Clamp(InitialGrowthPhase, 0, GrowthPhases.Length - 1);
            if (CurrentGrowthPhase != InitialGrowthPhase)
            {
                OnGrowthPhaseUpdate(CurrentGrowthPhase, InitialGrowthPhase, false);
                CurrentGrowthPhase = InitialGrowthPhase;
            }
        }
    }
#endif
    public void HandleAddedToPool(BaseObjectPoolingComponent PoolingComponent)
    {

    }
    public void HandlePoolInstantiate(BaseObjectPoolingComponent PoolingComponent)
    {
        InitializePlant();
    }
    public void HandleReturnToPool(BaseObjectPoolingComponent PoolingComponent)
    {
        UnbindDelegates();
        bInitialized = false;
    }

    public void InitializePlant()
    {
        if (bInitialized || GrowthPhases == null) return;

        if (PlantNeeds && !bIsGrowing)
        {
            PlantNeeds.enabled = !bStartGrown;
            PlantNeeds.gameObject.SetActive(!bStartGrown);
        }

        BindDelegates();

        if (bStartGrown)
        {
            //this should only really be true for trees
            InitialGrowthPhase = GrowthPhases.Length - 1;
        }
        else
        {
            InitialGrowthPhase = 0;
        }

        InitialGrowthPhase = Mathf.Clamp(InitialGrowthPhase, 0, GrowthPhases.Length - 1);
        OnGrowthPhaseUpdate(CurrentGrowthPhase, InitialGrowthPhase, false);

        if (HRNetworkManager.IsHost())
        {
            CurrentGrowthPhase = InitialGrowthPhase;

            if(!bStartGrown)
            {
                StartGrowing();
            }
        }

        InitializeColliders();

        bInitialized = true;
        if (bStartGrown)
        {
            this.enabled = false;
        }
    }

    public void SetFarmingPlot(HRFarmingPlot InFarmingPlot)
    {
        OwningFarmingPlot = InFarmingPlot;
        if (OwningFarmingPlot?.ShopPlotFarmManager)
        {
            OwningFarmingPlot.ShopPlotFarmManager.OnGrowModifierUpdated -= SetGrowRate;
            OwningFarmingPlot.ShopPlotFarmManager.OnGrowModifierUpdated += SetGrowRate;
            SetGrowRate();
        }
    }

    public void InitializeColliders()
    {
        if (OwningWeapon)
        {
            if (OwningWeapon.OwningInteractable)
            {
                OwningWeapon.OwningInteractable.SetInteractionCollisionEnabled(bEnableInteractableWhenPlanted);
            }
            OwningWeapon.SetMeshCollisionEnabled(bEnableMeshCollisionWhenPlanted);
        }
    }

    private void Start()
    {
        bHasElementalDamageManager = ElementalDamageManager != null;

        if (HRNetworkManager.IsHost())
        {
            DayManager = ((HRGameManager)BaseGameManager.Get).DayManager;
            if (DayManager)
            {
                DayManager.HourChangedDelegate += HandleHourChanged;
                DayManager.MinuteChangedDelegate += HandleMinuteChanged;
            }
        }

        InitializePlant();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        OnStartImplementation();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (HRNetworkManager.IsHost()) return;
        OnStartImplementation();

        if (!this.isActiveAndEnabled) return;

        OnGrowthPhaseUpdate(InitialGrowthPhase, CurrentGrowthPhase, false);
    }

    public void OnStartImplementation()
    {
        if (attributeManager)
        {
            attributeManager.OnAttributeAddedDelegate += HandleAddAttribute;
        }
    }

    private void SetGrowRate()
    {
        float realMinutesToGrow = InGameTimeToGrowFully * (EnviroSkyLite.instance.GameTime.cycleLengthInMinutes / 24f);
        float growRate = -1 * (PlantNeeds.NeedDatabase.GetNeedData(HRENeed.PlantStatus).MaxValue / (realMinutesToGrow * 60f));
        if (OwningFarmingPlot)
        {
            growRate *= OwningFarmingPlot.GetShopPlotGrowthModifier();
        }
        PlantNeeds.SetNeedBurnRateValue(HRENeed.PlantStatus, growRate); // set plant growth rate
    }

    private void Nurture(BaseInteractionManager InInteractionManager)
    {
        float baseNurture = .2f;
        if (InInteractionManager)
        {
            GameObject interactorSource = InInteractionManager.GetInteractorSourceGameObject();
            if (interactorSource)
            {
                var PlayerCharacter = interactorSource.GetComponent<HeroPlayerCharacter>();
                if (PlayerCharacter)
                {
                    if (PlayerCharacter.SkillSystem) //adding exp to farming when you nurture a crop
                    {
                        PlayerCharacter.SkillSystem.AddToSkill(HRSkillSystem.EPlayerSkill.Farming, baseNurture * 10f);
                    }
                }
            }
        }

        if (PlantNeeds && PlantNeeds.NeedDatabase)
        {
            AddHP(HRENeed.CropQuality, baseNurture * PlantNeeds.NeedDatabase.GetNeedData(HRENeed.CropQuality).MaxValue);
        }

        PlayNurtureEffect();
    }

    private void PlayNurtureEffect()
    {
        if (HRNetworkManager.IsHost())
        {
            PlayNurtureEffect_Implementation();
            if (netIdentity)
            {
                PlayNurtureEffect_ClientRpc();
            }
        }
    }

    [Mirror.ClientRpc]
    private void PlayNurtureEffect_ClientRpc()
    {
        if (!HRNetworkManager.IsHost())
            PlayNurtureEffect_Implementation();
    }

    private void PlayNurtureEffect_Implementation()
    {
        if (NurturingParticlePrefab)
            Instantiate(NurturingParticlePrefab, this.transform);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        UnbindDelegates();
    }

    private void BindDelegates()
    {
        if (PlantNeeds)
        {
            if(PlantNeeds.Initialized)
            {
                SetGrowRate();
            }
            else
            {
                PlantNeeds.OnNeedsInitializedDelegate += SetGrowRate;
            }
        }

        if (ElementalDamageManager != null)
        {
            ElementalDamageManager.OnStatusAppliedDelegate += OnStatusApplied;
        }

        if (OwningWeapon)
        {
            //Dont nurture trees
            if (OwningWeapon.OwningInteractable && !bIsTree)
            {
                OwningWeapon.OwningInteractable.TapInteractionDelegate += Nurture;
            }
            if (OwningWeapon.AttributeManager)
            {
                OwningWeapon.AttributeManager.OnAttributeAddedDelegate += HandleAttributeAdded;
            }
        }


    }

    private void UnbindDelegates()
    {
        if (PlantNeeds)
        {
            PlantNeeds.OnNeedsInitializedDelegate -= SetGrowRate;
        }

        if (ElementalDamageManager != null)
        {
            ElementalDamageManager.OnStatusAppliedDelegate -= OnStatusApplied;
        }

        if (OwningWeapon)
        {
            //Dont nurture trees
            if (OwningWeapon.OwningInteractable && !bIsTree)
            {
                OwningWeapon.OwningInteractable.TapInteractionDelegate -= Nurture;
            }
            if (OwningWeapon.AttributeManager)
            {
                OwningWeapon.AttributeManager.OnAttributeAddedDelegate -= HandleAttributeAdded;
            }
        }
    }

    private void HandleAttributeAdded(HRAttributeManager InAttributeManager, int AttributeID = -1)
    {
        if (AttributeID == WET_ATTRIBUTE_ID)
        {
            // TODO: Add cooldown
            int count = InAttributeManager.GetCountOfAttributesWithID(WET_ATTRIBUTE_ID);
            AddHP(HRENeed.WaterLevel, WateringLevelPercentage * PlantNeeds.NeedDatabase.GetNeedData(HRENeed.WaterLevel).MaxValue);
        }
    }

    public void SetWilting(bool bIsWilting, bool bPlayEffects = true)
    {
        if (HRNetworkManager.IsHost())
            SetWilting_Server(bIsWilting, bPlayEffects);
    }

    private void SetWilting_Server(bool bIsWilting, bool bPlayEffects = true)
    {
        bIsCurrentlyWilting = bIsWilting;
        ChangeWiltMesh_Implementation(bIsWilting);

        if(HRNetworkManager.IsHost() && Mirror.NetworkServer.active)
        {
            ChangeWiltMesh_ClientRpc(bIsWilting);
        }

        if (bIsWilting)
        {
            StopGrowing();
        }
        else
        {
            StartGrowing();
        }
    }

    [Mirror.ClientRpc]
    private void ChangeWiltMesh_ClientRpc(bool IsWilting)
    {
        if (!HRNetworkManager.IsHost())
            ChangeWiltMesh_Implementation(IsWilting);
    }
    private void ChangeWiltMesh_Implementation(bool IsWilting)
    {
        if(GrowthPhases != null)
        {
            if(GrowthPhases[CurrentGrowthPhase].PhaseMeshObject)
            {
                GrowthPhases[CurrentGrowthPhase].PhaseMeshObject.SetActive(!IsWilting);
            }

            if(GrowthPhases[CurrentGrowthPhase].WiltMeshObject)
            {
                GrowthPhases[CurrentGrowthPhase].WiltMeshObject?.SetActive(IsWilting);
            }
        }
    }

    [Server]
    public void SetGrowthPhase(int InGrowthPhase, bool bPlayEffects = true)
    {
        if(ReplaceOnDestroyRef)
        {
            ReplaceOnDestroyRef.SetEnabled(InGrowthPhase >= GrowthPhases.Length - 1);
        }

        //Return if not host
        if (!HRNetworkManager.IsHost()) return;
        //Return if phase has not changed
        if (CurrentGrowthPhase == InGrowthPhase) return;
        //Return if phase is invalid
        if (GrowthPhases.Length <= 0 || InGrowthPhase < 0 || InGrowthPhase >= GrowthPhases.Length) return;
        int PrevGrowthPhase = CurrentGrowthPhase;
        CurrentGrowthPhase = InGrowthPhase;
        OnGrowthPhaseUpdate(PrevGrowthPhase, CurrentGrowthPhase, bPlayEffects);
    }

    [Client]
    public void SetGrowthPhase_Hook(int InOldPhase, int InNewPhase)
    {
        OnGrowthPhaseUpdate(InOldPhase, InNewPhase, BaseGameManager.Get && !((HRGameManager)BaseGameManager.Get).bJustLoaded);
    }

    private void OnGrowthPhaseUpdate(int InOldPhase, int InNewPhase, bool bPlayEffects = true)
    {
        //Debug.Log($"OnGrowthPhaseUpdate - Old: {InOldPhase} New: {InNewPhase}");

        SetPhaseObjects(InOldPhase, InNewPhase);

        if (bPlayEffects)
            PlayAllEffects();

        if (InNewPhase >= GrowthPhases.Length - 1)
        {
            StopGrowing();
        }
        else
        {
            bIsDirty = true;

            //HRDayManager dayManager = ((HRGameManager)BaseGameManager.Get).DayManager;
//          SavedDate = dayManager.CalendarManager.CurrentDate;
//          SavedHour = dayManager.TimeHour;
//          SavedMinute = dayManager.TimeMinutes;
        }

        OnPlantPhaseChangedDelegate?.Invoke(this, InOldPhase, InNewPhase);
    }

    public bool IsFullyGrown()
    {
        return CurrentGrowthPhase == GrowthPhases.Length - 1;
    }

    public void SetPhaseObjects(int OldPhase, int NewPhase)
    {
        OldPhase = Mathf.Clamp(OldPhase, 0, GrowthPhases.Length - 1);
        NewPhase = Mathf.Clamp(NewPhase, 0, GrowthPhases.Length - 1);

        if (GrowthPhases[NewPhase].PhaseMeshObject)
        {
            GrowthPhases[OldPhase].PhaseMeshObject?.SetActive(false);
            GrowthPhases[NewPhase].PhaseMeshObject?.SetActive(true);
        }

        if (!bUseScale) return;
        SetPhaseScale(NewPhase);
    }

    public void PlayAllEffects()
    {
        if (CurrentGrowthPhase >= 0 && CurrentGrowthPhase < GrowthPhases.Length)
        {
            if (GrowthPhases[CurrentGrowthPhase].ParticlePrefab)
                SpawnPhaseParticle(GrowthPhases[CurrentGrowthPhase].ParticlePrefab);
            if (OwningWeapon && OwningWeapon.WeaponJiggle)
            {
                OwningWeapon.WeaponJiggle.JiggleForSeconds(1.0f);
            }
            if (GrowthPhases[CurrentGrowthPhase].SoundClip)
                PlayPhaseSoundFX(GrowthPhases[CurrentGrowthPhase].SoundClip);
        }
    }

    public void SetPhaseScale(int InIndex)
    {
        if (!OwningWeapon) return;
        OwningWeapon.MeshRendererGameObject.transform.localScale = GrowthPhases[InIndex].PhaseScale;
        OwningWeapon.MeshColliderGameObject.transform.localScale = GrowthPhases[InIndex].PhaseScale;
        if (!OwningWeapon.WeaponJiggle) return;
        OwningWeapon.WeaponJiggle.OriginalLocalScale = GrowthPhases[InIndex].PhaseScale;
    }

//     void GetWateringCanWaterLevel(BaseHP InHPComponent, GameObject Instigator, float PreviousHP, float NewHP, bool bFireEffects) // TO FIX
//     {
//         var InteractingBullet = Instigator?.GetComponent<BaseBulletComponent>();
// 
//         if (InteractingBullet)
//         {
//             if (InteractingBullet.OwningGun && (InteractingBullet.OwningGun as HRWateringCan))
//             {
//                 var WaterCan = (InteractingBullet.OwningGun as HRWateringCan);
//                 if (WaterCan)
//                     WateringLevelPercentage = WaterCan.WateringLevelPercentageToAdd;
//             }
//         }
//     }

    private void OnStatusApplied(DamageStatus Status)
    {
        switch (Status)
        {
            // If plant is wet, it has been watered today and will grow.
            case DamageStatus.WET:
                AddHP(HRENeed.WaterLevel, WateringLevelPercentage * PlantNeeds.NeedDatabase.GetNeedData(HRENeed.WaterLevel).MaxValue);
                //DaysNotWatered = 0;
                break;
            // TEST FUNCTION: If plant is burning and it has been watered today, pretend it hasn't.
            case DamageStatus.BURNING:

//                 if(bNeedsWater && DaysNotWatered == 0)
//                 {
//                     DaysNotWatered = 1;
//                 }

                break;
        }
    }

    private void DecreaseWaterLevel()
    {
        if (!PlantNeeds)
        {
            return;
        }
        if (bIsCurrentlyWilting)
        {
            PlantNeeds.GetNeedComponent(HRENeed.WaterLevel).SetCurrentHP(0);
        }
        else
        {
            AddHP(HRENeed.WaterLevel, -1 * HourlyConsumptionRate * PlantNeeds.NeedDatabase.GetNeedData(HRENeed.WaterLevel).MaxValue);
        }
    }

    void AddHP(HRENeed Need, float Value)
    {
        //NeedHP.AddHP(amountToAdd, Instigator);
        if (HRNetworkManager.IsHost())
        {
            AddHP_Implementation(Need, Value);
        }
        else
            AddHP_Command(Need, Value);
    }

    [Mirror.Command(ignoreAuthority = true)]
    void AddHP_Command(HRENeed Need, float Value)
    {
        AddHP_Implementation(Need, Value);
    }

    void AddHP_Implementation(HRENeed Need, float Value)
    {
        PlantNeeds.ModifyNeedValue(Need, Value);
    }


    [Mirror.Command(ignoreAuthority = true)]
    void StartGrowing_Command()
    {
        StartGrowing_Implementation();
    }

    [Mirror.Command(ignoreAuthority = true)]
    void StopGrowing_Command()
    {
        StopGrowing_Implementation();
    }

    void StartGrowing_Implementation()
    {
        if (IsFullyGrown()) return;
        if (PlantNeeds)
        {
            //Debug.Log("FARMING: Start Growing");
            PlantNeeds.SetNeedActive(HRENeed.PlantStatus, true);
            bIsGrowing = true;
        }
    }

    void StopGrowing_Implementation()
    {
        if (PlantNeeds)
        {
            if (bIsGrowing)
            {
                DoneGrowingDelegate?.Invoke();
            }
            //Debug.Log($"FARMING: Stop Growing. Need Active: {PlantNeeds.GetNeedActive(HRENeed.PlantStatus)}");
            PlantNeeds.SetNeedActive(HRENeed.PlantStatus, false);
            bIsGrowing = false;
        }
        if (bIsTree)
        {
            //Disable grown trees so that they don't tick anymore
            if (Application.isPlaying)
                this.enabled = false;
        }
    }

    public void StartGrowing()
    {
        if (HRNetworkManager.IsHost() || netIdentity == null)
        {
            StartGrowing_Implementation();
        }
        else
        {
            StartGrowing_Command();
        }
    }

    public void StopGrowing()
    {
        if (HRNetworkManager.IsHost() || netIdentity == null)
        {
            StopGrowing_Implementation();
        }
        else
        {
            StopGrowing_Command();
        }
    }

    public void SpawnPhaseParticle(GameObject ParticlePrefab)
    {
        if (ParticlePrefab)
        {
            var SpawnedParticlesGO = Instantiate(ParticlePrefab, this.transform);
            var SpawnedParticles = SpawnedParticlesGO.GetComponent<ParticleSystem>();
            if (SpawnedParticles)
                SpawnedParticles.GetComponent<ParticleSystem>().Play();
        }
    }

    public void PlayPhaseSoundFX(AudioClip SoundClip)
    {
        if (SoundClip)
        {
            if(SoundSource)
            {
                if(SoundSource.isActiveAndEnabled)
                {
                    SoundSource.PlayOneShot(SoundClip);
                }
            }
            else
            {
                ((HRGameInstance)BaseGameInstance.Get)?.MusicManager?.PlaySFX(SoundClip);
            }
        }
    }

    public float GetHarvestModifier()
    {
        if (bIsCurrentlyWilting)
        {
            return 0;
        }
        return GrowthPhases[CurrentGrowthPhase].HarvestModifier;
    }

    private float GetCropYieldMultipler()
    {
        if (!PlantNeeds)
        {
            return 1f;
        }
        float CurrentCropYield = PlantNeeds.GetNeedComponent(HRENeed.CropYield).CurrentHP;
        if (CurrentCropYield < 0)
            return 0;
        else if (CurrentCropYield >= 91)
            return 3f;
        else if (CurrentCropYield >= 75)
            return 2.5f;
        else if (CurrentCropYield >= 50)
            return 2f;
        else if (CurrentCropYield >= 26)
            return 1.5f;
        else
            return 1f;
    }

    public void HandleAddAttribute(HRAttributeManager InAttributeManager, int AttributeID)
    {
        //if a wet attribute was added 
        //set the water level need to 100%
        //prob will be better if increases gradually instead
        if (AttributeID == WET_ATTRIBUTE_ID)
        {
            AddHP(HRENeed.WaterLevel, PlantNeeds.NeedDatabase.GetNeedData(HRENeed.WaterLevel).MaxValue);
            if (!bIsWatered)
            {
                WateredDelegate?.Invoke();
                bIsWatered = true;
            }
        }
    }

    private void HandleHourChanged(HRDayManager DayManager, int OldHour, int NewHour)
    {
        if (bIsCurrentlyWilting)
        {
            SavedHour++;
        }

        DecreaseWaterLevel();
    }

    private void HandleMinuteChanged(HRDayManager InManager, float OldTime, float NewTime)
    {
        if (bIsGrowing)
        {
            SetGrowthPhase(GetGrowthPhaseFromPlantNeeds());
        }

        if (bShouldWilt)
        {
            if (PlantNeeds && PlantNeeds.GetNeedComponent(HRENeed.WaterLevel).CurrentHP <= 0)
            {
                SetWilting(true, false);

                if (SavedHour >= MaxHoursWiltedUntilDie)
                {
                    GetComponent<BaseDestroyHPListener>().DestroyObject(null);
                }
            }
            else
            {
                SetWilting(false, false);
                SavedHour = 0;
            }
        }
    }

    int GetGrowthPhaseFromPlantNeeds()
    {
        var CurrentPlantStatus = PlantNeeds.GetNeedComponent(HRENeed.PlantStatus);
        if (CurrentPlantStatus.CurrentHP == 1 * CurrentPlantStatus.MaxHP)
            return 3;
        else if (CurrentPlantStatus.CurrentHP >= 0.5f * CurrentPlantStatus.MaxHP)
            return 2;
        else if (CurrentPlantStatus.CurrentHP >= 0.1f * CurrentPlantStatus.MaxHP)
            return 1;
        else
        {
            return 0;
        }
    }

    private void Update()
    {
        if (bHasElementalDamageManager && ElementalDamageManager.ActiveStatuses.ContainsKey(DamageStatus.WET))
        {
            WetStatusTimer += Time.deltaTime;
            if (WetStatusTimer >= SecondsDelayBetweenWatering)
            {
                WetStatusTimer = 0;
                ElementalDamageManager.RemoveStatus(DamageStatus.WET);
            }
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    public override void OnDestroy()
    {
        // only if done growing
        if (!bIsGrowing)
        {
            HarvestedDelegate?.Invoke();
        }

        base.OnDestroy();

        if (BaseGameManager.Get && HRNetworkManager.IsHost())
        {
            HRDayManager DayManager = ((HRGameManager)BaseGameManager.Get).DayManager;
            if (DayManager)
            {
                DayManager.HourChangedDelegate -= HandleHourChanged;
                DayManager.MinuteChangedDelegate -= HandleMinuteChanged;
            }
        }

        if(ElementalDamageManager)
        {
            ElementalDamageManager.OnStatusAppliedDelegate -= OnStatusApplied;
        }

        if (OwningWeapon && OwningWeapon.OwningInteractable)
            OwningWeapon.OwningInteractable.TapInteractionDelegate -= Nurture;

        if (PlantNeeds)
            PlantNeeds.OnNeedsInitializedDelegate -= SetGrowRate;

        if (attributeManager)
        {
            attributeManager.OnAttributeAddedDelegate -= HandleAddAttribute;
        }
    }

    private void HandleCalendarLoaded(HRCalendarManager calendarManager)
    {
        calendarManager.OnCalendarLoadedDelegate -= HandleCalendarLoaded;
        DayManager = ((HRGameManager)BaseGameManager.Get).DayManager;
        var Date = calendarManager.CurrentDate;
        var Hour = DayManager.TimeHour;
        int Minute = (int)DayManager.TimeMinutes;

        int MinutesPassed = HRCalendarManager.NumMinutesElapsed(SavedDateAndTime.Date, SavedDateAndTime.Hour, SavedDateAndTime.Minute, Date, Hour, Minute);
        float HoursPassed = MinutesPassed / 60f;

        if (PlantNeeds && PlantNeeds.SavedData != null)
        {
            PlantNeeds.enabled = true;
            PlantNeeds.gameObject.SetActive(true);
            if (!PlantNeeds.Initialized)
            {
                PlantNeeds.InitializeNeeds();
            }

            for (int j = 0; j < PlantNeeds.SavedData.Count && j < PlantNeeds.NeedHP.Count; j++)
            {
                if (PlantNeeds.NeedHP[j].bActive)
                {
                    float UpdatedHealth;
                    if (PlantNeeds.NeedHP[j].Need == HRENeed.PlantStatus)
                    {
                        /*float realMinutesToGrow = InGameTimeToGrowFully * (EnviroSkyLite.instance.GameTime.cycleLengthInMinutes / 24f);
                        Debug.LogError(this.transform.root.name + " " + (((HoursPassed / 24f) * EnviroSkyLite.instance.GameTime.cycleLengthInMinutes) / realMinutesToGrow) + " " +
                            PlantNeeds.NeedHP[j].NeedHP.CurrentHP);
                        AddHP(HRENeed.PlantStatus, ((HoursPassed / 24f) * EnviroSkyLite.instance.GameTime.cycleLengthInMinutes) / realMinutesToGrow);*/
                    }
                    if (PlantNeeds.NeedHP[j].Need == HRENeed.WaterLevel)
                    {
                        UpdatedHealth = -1 * HourlyConsumptionRate * PlantNeeds.NeedDatabase.GetNeedData(HRENeed.WaterLevel).MaxValue * HoursPassed;
                        AddHP(HRENeed.WaterLevel, UpdatedHealth);
                    }
                }
            }
            InitialGrowthPhase = GetGrowthPhaseFromPlantNeeds();
            SetGrowthPhase(InitialGrowthPhase, false);
            HandleMinuteChanged(DayManager, Minute, Minute);
        }
    }

    public void HandlePreSave() // before save
    {
        HRDayManager DayManager = ((HRGameManager)BaseGameManager.Get).DayManager;
        SavedDateAndTime.Date = DayManager.CalendarManager.CurrentDate;
        SavedDateAndTime.Hour = DayManager.TimeHour;
        SavedDateAndTime.Minute = (int)DayManager.TimeMinutes;
    }

    public void HandleSaved() // after save
    {
        //HRSaveSystem.Get.CurrentFileInstance.Load("Save This");
    }

    public void HandleLoaded() // on load
    {
        if (!bIsGrowing)
        {
            return;
        }
        HRCalendarManager calendarManager = ((HRGameManager)BaseGameManager.Get).DayManager.CalendarManager;
        if (calendarManager)
        {
            if (calendarManager.bLoaded)
            {
                HandleCalendarLoaded(calendarManager);
            }
            else
            {
                calendarManager.OnCalendarLoadedDelegate += HandleCalendarLoaded;
            }
        }
    }

    public void HandleReset()
    {
        // Sussy baka cheeky function
    }

    public void HandleSaveComponentInitialize(HRSaveComponent InSaveComponent, int ComponentID, int AuxID)
    {

    }

    public bool IsSaveDirty()
    {
        return bIsDirty;
    }
}
