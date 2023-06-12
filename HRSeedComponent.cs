using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaseScripts;

public class HRSeedComponent : Mirror.NetworkBehaviour
{
    public BaseWeapon OwningWeapon;

    public bool bPlantImmediately;
    public int DaysToSprout = 0;
    public int BaseDaysToSprout = 0;
    public HRPlantComponent PlantPrefab;
    public BaseProjectilePhysics ProjectilePhysics;
    [SerializeField]
    private bool plantOnAnyGround;

    public static readonly string[] PlantableGroundNames = { "GrassPhysicMaterial", "DirtPhysicMaterial" }; // unused

    [Mirror.SyncVar]
    private bool bCanPlant;

    [Header("DEBUG")]
    public bool bStartGrown;

    public bool CanPlantSeed => bCanPlant;
    private bool bSprouting = false;

    HRFarmingPlot OwningFarmingPlot;
    
    HRDateStruct SavedDate;
    int SavedHour;
    float SavedMinute;

    public AnimationCurve MasteryStartingPlantQuality;
    private HeroPlayerCharacter PC;

    HRPlantComponent PlantInstance;

    // delegate for when the seed is planted
    public delegate void HRPlantedSignature();
    public HRPlantedSignature PlantedDelegate;
    public override void Awake()
    {
        base.Awake();
        if (ProjectilePhysics)
        {
            ProjectilePhysics.LandedDelegate += HandleLanded;
        }
        
        if (OwningWeapon && OwningWeapon.OwningInteractable)
        {
            OwningWeapon.OwningInteractable.TapInteractionDelegate += CheckPlantInteraction;
        }
    }

    public override void Start()
    {
        base.Start();

        if (HRNetworkManager.IsHost())
        {
            HRDayManager DayManager = ((HRGameManager)BaseGameManager.Get).DayManager;
            if (DayManager)
            {
                DayManager.HourChangedDelegate += HandleHourChanged;

                // Resolves issues with large amounts of time
                SavedDate = DayManager.CalendarManager.CurrentDate;
                SavedHour = DayManager.TimeHour;
                SavedMinute = DayManager.TimeMinutes;
            }
        }
    }

    private void HandleHourChanged(HRDayManager DayManager, int OldHour, int NewHour)
    {
        if (bSprouting)
        {
            HRDayManager dayManager = ((HRGameManager)BaseGameManager.Get).DayManager;
            HRCalendarManager CM = dayManager.CalendarManager;

            int NumDaysPassed = HRCalendarManager.NumDaysElapsed(SavedDate, SavedHour, (int)SavedMinute, CM.CurrentDate, dayManager.TimeHour, 0);

            if(NumDaysPassed >= DaysToSprout)
            {
                DayManager.HourChangedDelegate -= HandleHourChanged;
                Sprout();
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        //if(PlantPrefab && HRNetworkManager.IsHost())
        //{
        //    BaseObjectPoolManager.Get.RequestAddPooledPrefabs(PlantPrefab.gameObject, 1);
        //}
    }

    private bool CanPlantOnCollider(Collider InCollider)
    {
        if(!InCollider || this == false)
        {
            return false;
        }

        if(InCollider as TerrainCollider)
        {
            return CanPlantOnPhysicMaterial(TerrainDetectorManager.Get?.GetFootstepFXAtPosition(this.gameObject, this.transform.position)?.AssociatedPhysicMaterials[0]?.name);
        }
        else
        {
            return InCollider.material && InCollider.material
                && CanPlantOnPhysicMaterial(InCollider.material.name);
        }

    }
    private bool CanPlantOnPhysicMaterial(string MaterialName)
    {
        if(plantOnAnyGround)
        {
            return true;
        }

        if (MaterialName != "" && MaterialName != null)
        {
            for (int i = 0; i < PlantableGroundNames.Length; ++i)
            {
                if (PlantableGroundNames[i].Contains(MaterialName) || MaterialName.Contains(PlantableGroundNames[i]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void CheckPlantInteraction(BaseInteractionManager InInteractionManager)
    {
        if (bCanPlant)
        {
            Plant();
        }
    }

    private void HandleLanded(BaseProjectilePhysics InProjectilePhysics, RaycastHit HitInfo)
    {
        if (HitInfo.collider)
        {
            // TODO only plant on dirt/grass instead of rock/snow
            if (CanPlantOnCollider(HitInfo.collider))
            {
                BaseWeapon ProjectileOwningWeapon = InProjectilePhysics?.OwningWeapon;
                if(ProjectileOwningWeapon && ProjectileOwningWeapon.StackCount == 1)
                {
                    SetCanPlant(true, null);

                    if (bPlantImmediately && ProjectileOwningWeapon.PreviousOwningInventory)
                    {
                        InProjectilePhysics.Straighten(); // Make sure this object is upright
                        Plant();
                    }
                }
            }
        }
    }

    public void SetCanPlant(bool canPlant, HRFarmingPlot InFarmingPlot)
    {
        if(HRNetworkManager.Get
            && HRNetworkManager.bIsServer)
        {
            bCanPlant = canPlant;
            OwningFarmingPlot = InFarmingPlot;
            if (OwningFarmingPlot?.ShopPlotFarmManager)
            {
                OwningFarmingPlot.ShopPlotFarmManager.OnGrowModifierUpdated -= UpdateDaysToSprout;
                OwningFarmingPlot.ShopPlotFarmManager.OnGrowModifierUpdated += UpdateDaysToSprout;
                UpdateDaysToSprout();
            }
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    void Plant_Command()
    {
        Plant_Server();
    }

    public void Plant()
    {
        if (PlantPrefab && CanPlantSeed)
        {
            if (HRNetworkManager.IsHost())
            {
                Plant_Server();
            }
            else
            {
                Plant_Command();
            }
        }

        PlantedDelegate?.Invoke();
    }

    void Plant_Server()
    {
        if(DaysToSprout == 0)
        {
            Sprout();
        }
        else if(!bSprouting)
        {
            bSprouting = true;

            HRDayManager DayManager = ((HRGameManager)BaseGameManager.Get).DayManager;
            SavedDate = DayManager.CalendarManager.CurrentDate;
            SavedHour = DayManager.TimeHour;
            SavedMinute = DayManager.TimeMinutes;
        }
    }

    void UpdateDaysToSprout()
    {
        //TODO: I think at the moment DaysToSprout is always 0 anyway
        if (!OwningFarmingPlot) return;
        DaysToSprout = (int)(BaseDaysToSprout / OwningFarmingPlot.GetShopPlotGrowthModifier());
    }

    void Sprout()
    {
        // Don't pool until we find out why it keeps getting sent to dontdestroyonload. This prevents it rfom loading properly
        //BaseObjectPoolingComponent PlantPoolComponent = BaseObjectPoolManager.Get.InstantiateFromPool(PlantPrefab.gameObject, false, true, transform.position, true);
        HRPlantComponent PlantInstance = Instantiate<HRPlantComponent>(PlantPrefab, this.transform.position, this.transform.rotation, null);

        //if (PlantPoolComponent)
        //{
        //    BaseWeapon Weapon = PlantPoolComponent.GetComponent<BaseWeapon>();
        //    if (Weapon)
        //    {
        //        Weapon.transform.SetParent(null, true);
        //        Weapon.UpdateChunk();
        //    }
        //    else
        //    {
        //        if (PlantPoolComponent.gameObject.scene != this.gameObject.scene)
        //        {
        //            if (PlantPoolComponent.transform.root != PlantPoolComponent.transform)
        //            {
        //                PlantPoolComponent.transform.SetParent(null);
        //            }
        //            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(PlantPoolComponent.gameObject, this.gameObject.scene);
        //        }
        //    }

        //    PlantInstance = PlantPoolComponent.GetComponent<HRPlantComponent>();
        //}

        if (PlantInstance)
        {
            Mirror.NetworkServer.Spawn(PlantInstance.gameObject);

            InitializeNewPlant(PlantInstance);
            PlantInstance.InitializePlant();

            StopCoroutine(DestroySeedCoroutine());
            StartCoroutine(DestroySeedCoroutine());
        }
    }

    void InitializeNewPlant(HRPlantComponent InPlant)
    {
        if(InPlant)
        {
            float starterQuality = -1;

            if (ProjectilePhysics.Instigator)
            {
                PC = ProjectilePhysics.Instigator.GetComponent<HeroPlayerCharacter>();
                if (PC.SkillSystem)
                {
                    float farmingLvl = PC.SkillSystem.GetSkillLevel(HRSkillSystem.EPlayerSkill.Farming);
                    starterQuality = MasteryStartingPlantQuality.Evaluate(farmingLvl);
                }
            }

            if(OwningFarmingPlot)
            {
                var PlantWeapon = InPlant.GetComponent<BaseWeapon>();
                if (PlantWeapon)
                    OwningFarmingPlot.InInventory.AddWeapon(PlantWeapon);
            }

            InPlant.bStartGrown = bStartGrown;
            SproutPlant(InPlant, bStartGrown, starterQuality);
        }
    }

    [Mirror.ClientRpc]
    public void SproutPlant(HRPlantComponent InPlant, bool bInStartGrown, float CropQuality)
    {
        if (InPlant)
        {
            PlantInstance = InPlant;
            InPlant.bStartGrown = bInStartGrown;
            InPlant.transform.position = transform.position;

            if (HRNetworkManager.IsHost())
            {
                if (InPlant.PlantNeeds && CropQuality != -1)
                {
                    InPlant.PlantNeeds.SetNeedValue(HRENeed.CropQuality, CropQuality);
                }

                BaseObjectPoolingComponent PlantPoolingComponent = InPlant.GetComponent<BaseObjectPoolingComponent>();
                if (PlantPoolingComponent)
                {
                    PlantPoolingComponent.InitializePoolComponents();
                }
            }
        }
    }

    private IEnumerator DestroySeedCoroutine()
    {
        yield return new WaitForSeconds(0.05f); // scuffy fix because my delegates weren't working :(
        Mirror.NetworkServer.Destroy(this.gameObject);
    }
}
