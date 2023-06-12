using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using BaseScripts;
using DG.Tweening;

public class BaseItemPlacingManager : Mirror.NetworkBehaviour
{
    public delegate void BaseItemPlacingSignature(BaseItemPlacingManager InManager);
    public BaseItemPlacingSignature StartPlacingDelegate;

    public delegate void BaseItemPlacingGOSignature(BaseItemPlacingManager InManager, GameObject InPlaceable);
    public BaseItemPlacingGOSignature PlacedItemDelegate;

    public delegate void BaseItemConstructedSignature(BaseItemPlacingManager InManager, GameObject ConstructedItem);
    public BaseItemConstructedSignature ConstructedItemDelegate;

    public Color[] ItemRarityTint;

    public bool bJustPlaced = false;

    // The layer mask to use to determine if you can place an object down.
    public LayerMask BuildingLayerMask;
    public LayerMask PlaceLayerMask;
    // The layer mask to use to determine if there is an obstacle (e.g. wall) in the way
    public LayerMask ObstacleLayerMask;
    // Material that ghost objects use.
    public Material ValidGhostMaterial;
    public Material InvalidGhostMaterial;
    public Material ContainerGhostMaterial;

    private BaseContainer _currentContainer;
    public BaseContainer CurrentContainer
    {
        get
        {
            return _currentContainer;
        }
        set
        {
            _currentContainer = value;
            HasCurrentContainer = value;
        }
    }
    [HideInInspector] public bool HasCurrentContainer;
    public BaseItemPlaceable CurrentPlaceableGameObject;

    public HRBuildingComponent CurrentGhostBuildingComponent;
    public HRMaterialCheckComponent CurrentGhostMaterialCheckComponent;
    public BaseGhostItem CurrentGhostItemRef;
    public BoxCollider CurrentPlaceCollider;

    [HideInInspector]
    public GameObject CurrentGhostObject;

    [HideInInspector]
    public Quaternion LastSavedRotation;

    public BaseItemPlacementCollision CurrentPlaceCollision;
    List<Renderer> CurrentGhostRenderers = new List<Renderer>();

    BasePlayerCamera PlayerCamera;
    BasePlayerCharacter OwningPlayerCharacter;
    BaseWeaponManager OwningWeaponManager;
    BasePlayerCharacter OtherPlayerCharacter;

    // The y offset applied to placed objects so things dont fall through the map
    public float PlacementYOffset = 0.05f;
    // How far away from the player the object can be placed.
    public float MaxPlaceDistance = 1.25f;

    public float GridSize = 0.25f;
    public float GridSeesawBuffer = 0.2f;

    // Used to track if we should move to the next grid.
    private bool bInitializedCurrentGrid = false;
    private Vector2 CurrentGrid;

    [HideInInspector]
    public bool bIsPlacing = false;
    bool bIsValidToPlace = false;
    bool bSwitching = false;
    bool hasHadValidPlacementSpot = true;
    bool bIsDragPlacement;


    //public GrassControl _grassControl;

    public GameObject PlaceItemParticle;
    public GameObject PlaceInstancedItemParticle;

    public LineRenderer PlaceLineRenderer;

    public AudioClip PlaceErrorSound;
    public AudioClip RotateSound;
    public AudioClip MoveTickSound;

    private Vector3 OriginalGhostScale;

    private Vector3 LastRecordedPosition;

    public float RotateDegreeAmount = 45.0f;

    GameObject GhostMeshGO = null;
    Vector3 LastGhostMeshPos = new Vector3(0, 0, 0);
    Quaternion LastGhostMeshRot = Quaternion.identity;
    GameObject GhostMeshGOTemp;

    public FInteractionContextDelegate PlaceItemContextDelegate;
    public FInteractionContextDelegate RotateItemContextDelegate;

    public BaseInteractionContextData RotateContextInfo = BaseInteractionContextData.Default;
    public BaseInteractionContextData PlaceContextInfo = BaseInteractionContextData.Default;

    private bool _midPlacingObject = false;

    Vector3 SnapHitPosition = Vector3.zero;

    List<HRBuildingComponent> BuildingsWithEnabledInteractables = new List<HRBuildingComponent>();

    HRBuildingComponent OtherBuildingToSnapTo = null;
    int OtherSocketIndexToSnapTo = -1;

    bool bObstacle = false;

    BaseContainer TempHoveredContainer;

    public GameObject SocketVisualizerObject;
    GameObject SocketVisualizerInstance;

    string containerFailText;

    public BaseQueueSystem LastQueueSystem;
    bool bSpecialDestroyOnPickup = false;

    private IEnumerator SwapRoutine;

    public int bPlacedItem; //TEST
    public Collider SavedBoxCollider;


    Transform test;
    public float GetGridSize()
    {
        if (CurrentPlaceCollision && CurrentPlaceCollision.bOverrideGridSize)
        {
            return CurrentPlaceCollision.OverrideGridSpacing;
        }

        return GridSize;
    }
    public GameObject GetSocketVisualizer()
    {
        if (!SocketVisualizerInstance && SocketVisualizerObject)
        {
            SocketVisualizerInstance = Instantiate(SocketVisualizerObject);
            SocketVisualizerInstance.SetActive(false);
        }
        return SocketVisualizerInstance;
    }
    public void SetSpecialDestroyOnPickupMode(bool bInEnabled)
    {
        bSpecialDestroyOnPickup = bInEnabled;
    }

    public bool CanSpecialDestroy()
    {
        return bSpecialDestroyOnPickup;
    }
    public void InitializeBaseItemPlacingManager(BasePlayerCharacter InCharacter, BasePlayerCamera InPlayerCamera)
    {
        PlayerCamera = InPlayerCamera;
        OwningPlayerCharacter = InCharacter;
        // Bad to mix HR

        if (InCharacter is HeroPlayerCharacter)
        {
            OwningWeaponManager = (InCharacter as HeroPlayerCharacter).WeaponManager;
            OwningWeaponManager.HotkeySlotSelectedDelegate += OnWeaponChanged;
        }
    }

    private void OnWeaponChanged(BaseWeaponManager InManager, int OldSlot, int NewSlot)
    {
        if (!bIsValidToPlace)
        {
            if (SwapRoutine != null)
            {
                StopCoroutine(SwapRoutine);
            }

            SwapRoutine = WeaponSwapDelay();
            StartCoroutine(SwapRoutine);
        }
    }

    private IEnumerator WeaponSwapDelay()
    {
        bSwitching = true;
        bIsValidToPlace = false;

        UpdateMaterialBasedOnValid();

        yield return new WaitForSeconds(0.1f);

        bSwitching = false;
    }

    // Start is called before the first frame update
    public override void Awake()
    {
        base.Awake();

        this.enabled = false;

        if (ValidGhostMaterial)
        {
            ValidGhostMaterial = Instantiate<Material>(ValidGhostMaterial);
        }
        else
        {
            Debug.LogWarning("No ValidGhostMaterial assigned!");
        }
        if (InvalidGhostMaterial)
        {
            InvalidGhostMaterial = Instantiate<Material>(InvalidGhostMaterial);
        }
        else
        {
            Debug.LogWarning("No InvalidGhostMaterial assigned!");
        }
        if (ContainerGhostMaterial)
        {
            ContainerGhostMaterial = Instantiate<Material>(ContainerGhostMaterial);
        }
        else
        {
            Debug.LogWarning("No ContainerGhostMaterial assigned!");
        }

        //if (_grassControl == null) _grassControl = FindObjectOfType<GrassControl>();
    }

    public override void Start()
    {
        // Bad but works.
        AstarPath.OnGraphsUpdated -= HandleGraphsUpdated;
        AstarPath.OnGraphsUpdated += HandleGraphsUpdated;
    }

    float GetRoundedNumber(float InFloat)
    {
        return Mathf.Round(InFloat / GetGridSize()) * GetGridSize();
    }

    void ClearBuildingToSnapTo()
    {
        if (OtherBuildingToSnapTo)
        {
            if (OtherBuildingToSnapTo.OwningPlaceable?.OwningWeapon?.OwningInteractable)
            {
                OtherBuildingToSnapTo.OwningPlaceable.OwningWeapon.OwningInteractable.SetInteractionCollisionEnabled(false);
            }

            OtherBuildingToSnapTo = null;
        }

        OtherSocketIndexToSnapTo = -1;

        GetSocketVisualizer().SetActive(false);
    }
    Vector3 GetRoundedPosition(RaycastHit hit)
    {
        Vector3 RoundedPosition = hit.point;

        if (CurrentPlaceCollision)
        {
            float DotResult = Vector3.Dot(hit.normal, new Vector3(0, 1, 0));
            if (DotResult < 0.05f && DotResult > -0.05f)
            {
                // Horizontal offset.
                Vector3 OffsetVector = hit.normal;
                OffsetVector.y = 0.0f;
                RoundedPosition += OffsetVector * CurrentPlaceCollision.HorizontalPlacementOffset;
            }

            // offset ghost object from walls, snap rotation to normal for bCanStickToWall objects
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("WallTiles"))
            {
                Vector3 offset = hit.normal;
                offset.y = 0.0f;
                float offsetAmount = 0.5f * CurrentPlaceCollider.size.z *
                    CurrentGhostObject.transform.localScale.z;
                if (CurrentPlaceableGameObject.bCanStickToWall)
                {
                    CurrentGhostObject.transform.rotation =
                        Quaternion.LookRotation(hit.normal, Vector3.up);
                }
                else
                {
                    // using max extent to ensure object doesn't clip into wall; might need a better method
                    offsetAmount = Mathf.Max(CurrentPlaceCollider.bounds.extents.x,
                        CurrentPlaceCollider.bounds.extents.z);
                }

                RoundedPosition = hit.point + offset * offsetAmount;
            }
        }

        RoundedPosition.y += PlacementYOffset;

        return RoundedPosition;
    }


    Vector3 GetPivotSnapOffset(Transform otherBuilding, Vector3 pivotPos, float customAxisOffset = 0)
    {
        Bounds b = otherBuilding.GetComponent<Collider>().bounds;
        float yOffset = 0;
        float axisOffsetAmount = customAxisOffset == 0 ? b.size.x / 2 : customAxisOffset;
        Vector3 centeredPos = b.center;
        centeredPos.y = otherBuilding.position.y;
        float xDiff = Mathf.Abs(centeredPos.x - pivotPos.x);
        float zDiff = Mathf.Abs(centeredPos.z - pivotPos.z);

        int negation = (xDiff > zDiff && centeredPos.x - pivotPos.x < 0) || (xDiff < zDiff && centeredPos.z - pivotPos.z < 0) ? 1 : -1;
        return xDiff > zDiff ? new Vector3(pivotPos.x + negation * axisOffsetAmount, pivotPos.y + yOffset, centeredPos.z) : new Vector3(centeredPos.x, pivotPos.y + yOffset, pivotPos.z + negation * axisOffsetAmount);
    }
    // Update is called once per frame
    void Update()
    {
        if (bIsPlacing && PlayerCamera && CurrentGhostObject && CurrentPlaceableGameObject)
        {
            // Figure out where the item can be placed.
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                SnapHitPosition = this.transform.position;

                Vector3 CameraPosition = PlayerCamera.GetCamera().transform.position;
                float distPlayer = Vector2.Distance(new Vector2(OwningPlayerCharacter.Position.x, OwningPlayerCharacter.Position.z), new Vector2(CameraPosition.x, CameraPosition.z));
                Ray CameraToScreenRaycast = PlayerCamera.GetAimRay();

                RaycastHit BuildingSnapHit = new RaycastHit();

                RaycastHit SphereHit = new RaycastHit();
                bool bValidSphereHit = false;

                RaycastHit SweepHit = new RaycastHit();
                bool bValidSweepHit = false;

                RaycastHit RaycastHit = new RaycastHit();
                bool bValidRaycastHit = Physics.Raycast(CameraPosition + CameraToScreenRaycast.direction.normalized * distPlayer, CameraToScreenRaycast.direction, out RaycastHit, 3000.0f, PlaceLayerMask);

                HRBuildingSocket FromSocket = new HRBuildingSocket();
                HRBuildingSocket ToSocket = new HRBuildingSocket();

                if (CurrentPlaceableGameObject.BuildingComponent && !Input.GetKey(KeyCode.LeftShift))
                {
                    HRBuildingComponent HitBuildingComponent = null;

                    // Rigidbody cast

                    bool bHitPhysicsCast = false;

                    if (CurrentPlaceCollider)
                    {
                        Vector3 PivotToBoxCenter = CurrentPlaceCollider.transform.TransformPoint(CurrentPlaceCollider.center) - CurrentPlaceCollider.transform.position;

                        if (Physics.BoxCast(CameraPosition + PivotToBoxCenter, CurrentPlaceCollider.size, CameraToScreenRaycast.direction, out SweepHit, CurrentPlaceCollider.transform.rotation, 3000.0f, BuildingLayerMask))
                        {
                            bHitPhysicsCast = true;
                            bValidSweepHit = true;
                        }
                    }

                    if (Physics.SphereCast(CameraPosition + CameraToScreenRaycast.direction * 0.25f, 0.25f, CameraToScreenRaycast.direction, out SphereHit, 3000.0f, BuildingLayerMask))
                    {
                        bHitPhysicsCast = true;
                        bValidSphereHit = true;
                    }

                    //if (HitBuildingComponent && OtherBuildingToSnapTo != HitBuildingComponent)
                    //{
                    //    CurrentGhostBuildingComponent.transform.position = CameraToScreenHit.point;
                    //}

                    bool bValidBuildingSnap = false;
                    bool bUsedSweep = false;

                    if (bHitPhysicsCast || bValidRaycastHit)
                    {
                        if (bValidSweepHit && !bValidSphereHit)
                        {
                            Vector3 SweepHitPosition = CameraPosition + CameraToScreenRaycast.direction * SweepHit.distance;

                            if (SweepHitPosition.y < SweepHit.point.y && Vector3.Dot(PlayerCamera.GetCamera().transform.forward, (SweepHit.point - OwningPlayerCharacter.PlayerCamera.CameraRoot.transform.position).normalized) > 0.2f)
                            {
                                BuildingSnapHit = SweepHit;
                                HitBuildingComponent = BuildingSnapHit.collider.GetComponent<HRBuildingComponent>();


                                if (HitBuildingComponent)
                                {
                                    bUsedSweep = true;
                                }
                            }
                        }

                        if (!HitBuildingComponent)
                        {
                            if (bValidSphereHit)
                            {
                                BuildingSnapHit = SphereHit;
                                HitBuildingComponent = BuildingSnapHit.collider.GetComponent<HRBuildingComponent>();
                            }

                            if (!HitBuildingComponent)
                            {
                                if (bValidSweepHit)
                                {
                                    Vector3 SweepHitPosition = CameraPosition + CameraToScreenRaycast.direction * SweepHit.distance;

                                    if (SweepHitPosition.y < SweepHit.point.y && Vector3.Dot(PlayerCamera.GetCamera().transform.forward, (SweepHit.point - OwningPlayerCharacter.PlayerCamera.CameraRoot.transform.position).normalized) > 0.2f)
                                    {
                                        BuildingSnapHit = SweepHit;
                                        HitBuildingComponent = BuildingSnapHit.collider.GetComponent<HRBuildingComponent>();

                                        if (HitBuildingComponent)
                                        {
                                            bUsedSweep = true;
                                        }
                                    }
                                }
                            }
                        }

                        if (!HitBuildingComponent)
                        {
                            if (bValidRaycastHit)
                            {
                                BuildingSnapHit = RaycastHit;
                                HitBuildingComponent = BuildingSnapHit.collider.GetComponent<HRBuildingComponent>();
                            }
                        }
                    }
                    // Get the closest building to the target location.
                    if (HitBuildingComponent)
                    {

                        int OutSocketIndex = -1;
                        // See if it is far enough from the origin

                        // If sweep but no sphere, use sweep (used when placing underneath an object)
                        // If sweep and sphere, use sphere
                        // If no sweep and no sphere, use raycast

                        if (Vector3.Dot(PlayerCamera.GetCamera().transform.forward, (BuildingSnapHit.point - OwningPlayerCharacter.PlayerCamera.CameraRoot.transform.position).normalized) > 0.2f)
                        {
                            //int OutSocketIndex = -1;
                            if (HRBuildingComponent.GetSnapPoint(BuildingSnapHit.point, CurrentGhostBuildingComponent, out FromSocket, HitBuildingComponent
                                , out ToSocket, out OutSocketIndex))
                            {
                                bool bShouldPerformCheck = true;

                                if (bUsedSweep)
                                {
                                    SetGhostObjectPosition(CameraPosition + CameraToScreenRaycast.direction * BuildingSnapHit.distance);
                                    //CurrentGhostObject.transform.position = CameraPosition + CameraToScreenRaycast.direction * BuildingSnapHit.distance;
                                }
                                else if (OtherBuildingToSnapTo != HitBuildingComponent || (OtherBuildingToSnapTo == HitBuildingComponent && OtherSocketIndexToSnapTo != OutSocketIndex))
                                {
                                    OtherSocketIndexToSnapTo = OutSocketIndex;
                                    // If the snap point is different from the original one, reset the position
                                    // Debug.Log("Different building!");
                                    //SetGhostObjectPosition(HitBuildingComponent.transform.TransformPoint(ToSocket.LocalPosition));
                                    //CurrentGhostObject.transform.position = HitBuildingComponent.transform.TransformPoint(ToSocket.LocalPosition);
                                    bShouldPerformCheck = (HRBuildingComponent.GetSnapPoint(BuildingSnapHit.point, CurrentGhostBuildingComponent, out FromSocket, HitBuildingComponent
                                        , out ToSocket, out OutSocketIndex));

                                    if (CurrentPlaceableGameObject.BuildingComponent.ShouldAutoRotate() && OtherBuildingToSnapTo && OtherBuildingToSnapTo.ShouldAutoRotate())
                                    {
                                        if (ToSocket.LocalPosition.x == 0)
                                        {
                                            if (ToSocket.LocalPosition.z == 1.5)
                                            {
                                                Vector3 eulerRotation = new Vector3(HitBuildingComponent.transform.eulerAngles.x, HitBuildingComponent.transform.eulerAngles.y, HitBuildingComponent.transform.eulerAngles.z);


                                                CurrentGhostObject.transform.rotation = Quaternion.Euler(eulerRotation);

                                            }
                                            else if (ToSocket.LocalPosition.z == 0)
                                            {
                                                Vector3 eulerRotation = new Vector3(HitBuildingComponent.transform.eulerAngles.x, HitBuildingComponent.transform.eulerAngles.y + 180f, HitBuildingComponent.transform.eulerAngles.z);


                                                CurrentGhostObject.transform.rotation = Quaternion.Euler(eulerRotation);

                                            }
                                        }

                                        else if (ToSocket.LocalPosition.z == 0.75)
                                        {
                                            if (ToSocket.LocalPosition.x == 0.75)
                                            {
                                                Vector3 eulerRotation = new Vector3(HitBuildingComponent.transform.eulerAngles.x, HitBuildingComponent.transform.eulerAngles.y + 90f, HitBuildingComponent.transform.eulerAngles.z);


                                                CurrentGhostObject.transform.rotation = Quaternion.Euler(eulerRotation);
                                            }
                                            else if (ToSocket.LocalPosition.x == -0.75)
                                            {
                                                Vector3 eulerRotation = new Vector3(HitBuildingComponent.transform.eulerAngles.x, HitBuildingComponent.transform.eulerAngles.y - 90f, HitBuildingComponent.transform.eulerAngles.z);


                                                CurrentGhostObject.transform.rotation = Quaternion.Euler(eulerRotation);

                                            }
                                        }
                                    }

                                    //ugly code done
                                }

                                if (bShouldPerformCheck)
                                {
                                    float DistanceToSnapHit = Vector3.Distance(BuildingSnapHit.point, HitBuildingComponent.transform.TransformPoint(ToSocket.LocalPosition));
                                    if (DistanceToSnapHit <= 1.5f)
                                    {
                                        bValidBuildingSnap = true;
                                        SnapHitPosition = BuildingSnapHit.point;
                                    }
                                    else
                                    {
                                        bValidBuildingSnap = false;
                                    }
                                }
                            }

                            // Set enabled so we can remove it.
                            if (HitBuildingComponent?.OwningPlaceable?.OwningWeapon?.OwningInteractable)
                            {
                                HitBuildingComponent.OwningPlaceable.OwningWeapon.OwningInteractable.SetInteractionCollisionEnabled(true);
                            }

                            BuildingsWithEnabledInteractables.Add(HitBuildingComponent);
                        }

                        if (bValidBuildingSnap && HitBuildingComponent)
                        {
                            //super temp and intentionally ugly
                            //this code in intended to be removed when we have center pivot
                            //this is for snapping a ghost builidng into the correct rotation when hovering

                            OtherBuildingToSnapTo = HitBuildingComponent;
                            SnapHitPosition = PlayerCamera.GetCamera().transform.position + CameraToScreenRaycast.direction * BuildingSnapHit.distance;
                        }
                    }
                    else
                    {
                        ClearBuildingToSnapTo();
                    }
                }
                else
                {
                    ClearBuildingToSnapTo();
                }

                bool bHasBuildingToSnap = OtherBuildingToSnapTo != null;//  OtherBuildingToSnapTo && Vector3.Distance(CurrentGhostObject.transform.position, SnapHitPosition) < 0.25f;

                if (!bHasBuildingToSnap && bValidRaycastHit && hasHadValidPlacementSpot)
                {
                    // Check overlap at this point?
                    // Check distance
                    // Round to the nearest XZ amount.

                    BuildingSnapHit = RaycastHit;

                    Vector3 RoundedPosition = GetRoundedPosition(RaycastHit);


                    //if (bInitializedCurrentGrid)
                    //{
                    //    // Check to see if we have moved sufficiently iwnside the next grid to avoid seesawing between two different grids.
                    //    if (true)//CurrentPlaceCollision && !CurrentPlaceCollision.bSnapToGrid) we don't want snapping ever anymore
                    //    {
                    //        CurrentGrid.x = RoundedPosition.x;
                    //        CurrentGrid.y = RoundedPosition.z;
                    //    }
                    //    else
                    //    {
                    //        if (Mathf.Abs(RoundedPosition.x - CurrentGrid.x) >= GetGridSize() || Mathf.Abs(RoundedPosition.z - CurrentGrid.y) >= GetGridSize() || RoundedPosition.x % GetGridSize() > GridSeesawBuffer || RoundedPosition.z % GetGridSize() > GridSeesawBuffer)
                    //        {
                    //            RoundedPosition.z = GetRoundedNumber(RoundedPosition.z);
                    //            RoundedPosition.x = GetRoundedNumber(RoundedPosition.x);

                    //            CurrentGrid.x = RoundedPosition.x;
                    //            CurrentGrid.y = RoundedPosition.z;

                    //        }
                    //        else
                    //        {
                    //            RoundedPosition.x = CurrentGrid.x;
                    //            RoundedPosition.z = CurrentGrid.y;
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    if (CurrentPlaceCollision && false)// CurrentPlaceCollision.bSnapToGrid)
                    //    {
                    //        RoundedPosition.z = GetRoundedNumber(RoundedPosition.z);
                    //        RoundedPosition.x = GetRoundedNumber(RoundedPosition.x);
                    //    }

                    //    CurrentGrid.x = RoundedPosition.x;
                    //    CurrentGrid.y = RoundedPosition.z;

                    //    bInitializedCurrentGrid = true;
                    //}

                    //RoundedPosition.y += PlacementYOffset;

                    bool bIsCurrentlyValidToPlace = false;
                    bool bIsCloseEnough = false;

                    if (OwningPlayerCharacter)
                    {
                        bIsCloseEnough = Vector3.Distance(RoundedPosition, OwningPlayerCharacter.gameObject.transform.position) <= MaxPlaceDistance;
                    }
                    else
                    {
                        bIsCloseEnough = true;
                    }

                    if (bIsCloseEnough)
                    {
                        if (CurrentPlaceCollision && !CurrentGhostBuildingComponent)
                        {
                            bIsCurrentlyValidToPlace = !CurrentPlaceCollision.IsColliding();
                        }
                        else
                        {
                            bIsCurrentlyValidToPlace = true;
                        }

                        if (bSwitching)
                        {
                            bIsCurrentlyValidToPlace = false;
                        }

                        bIsValidToPlace = bIsCurrentlyValidToPlace;
                        UpdateMaterialBasedOnValid();
                        SetGhostObjectVisibility(true);
                    }
                    else
                    {
                        // Not close enough.
                        bIsValidToPlace = false;
                        UpdateMaterialBasedOnValid();
                    }

                    if (bIsValidToPlace)
                    {
                        // The below is used to raycast between the placer and the destination. Was useful for top down, no longer as needed in third/first person.
                        //Vector3 vectorTo = CurrentGhostObject.transform.position - transform.position;
                        //RaycastHit hit;

                        //if (!CurrentGhostBuildingComponent && Physics.Raycast(transform.position, vectorTo.normalized, out hit, vectorTo.magnitude, ObstacleLayerMask))
                        //{
                        //    bIsValidToPlace = false;
                        //    bObstacle = true;
                        //    UpdateMaterialBasedOnValid();
                        //}
                        //else
                        //{
                        //    bObstacle = false;
                        //}
                    }
                    else
                    {
                        bObstacle = false;
                    }

                    SetGhostObjectPosition(RoundedPosition);
                    // If it's a building object, do another cast to see if we can just snap;
                }
                else
                {
                    hasHadValidPlacementSpot = true;
                    if (OtherBuildingToSnapTo && OtherBuildingToSnapTo.OwningPlaceable && OtherBuildingToSnapTo.OwningPlaceable.OwningWeapon && OtherBuildingToSnapTo.OwningPlaceable.OwningWeapon.OwningInteractable)
                    {
                        bIsValidToPlace = true;

                        //CurrentGhostObject.transform.position = SnapHitPosition;


                        //GhostMeshGO.transform.position = SnapHitPosition;



                        GetSocketVisualizer().SetActive(true);
                        var snapPivotWorldPos = OtherBuildingToSnapTo.transform.TransformPoint(ToSocket.LocalPosition);
                        GetSocketVisualizer().transform.position = snapPivotWorldPos;
                        OtherBuildingToSnapTo.OwningPlaceable.OwningWeapon.OwningInteractable.SetInteractionCollisionEnabled(true);

                        SnapGhostObjectToSelectedPivot(snapPivotWorldPos);
                        //Based on current ToSocket, select closet FromSocket on the GhostObject, that's in the same directional alignment as ToSocket.
                        //Ex. Up/down pair with up/down, left/right direction pair with left/right
                        //if (HRBuildingComponent.GetSnapPoint(OtherBuildingToSnapTo, ToSocket, CurrentGhostBuildingComponent, out FromSocket))
                        //{
                        //    Vector3 FinalLocation = HRBuildingComponent.SnapBuildingToBuilding(FromSocket, ToSocket, CurrentGhostBuildingComponent, OtherBuildingToSnapTo);

                        //    Debug.Log("Here");
                        //    if (test == null)
                        //    {
                        //        test.position = CurrentGhostBuildingComponent.transform.TransformPoint(FromSocket.LocalPosition);

                        //        Debug.Log("Fuck bro: " + CurrentGhostBuildingComponent.transform.position + " : " + CurrentGhostObject.transform.position);
                        //    }
                        //    SetGhostObjectPosition(FinalLocation);
                        //    //CurrentGhostObject.transform.position = FinalLocation;
                        //    //GhostMeshGO.transform.position = FinalLocation;

                        //}

                        //SnappedToGrid(CurrentGhostObject.transform.position);
                        UpdateMaterialBasedOnValid();

                    }
                }
            }
            else
            {
                SetGhostObjectVisibility(false);
                bIsValidToPlace = false;

                ClearBuildingToSnapTo();
            }

            // Check to see if it is over a container. If so, remove visibility and swap icon
            // Janky but works for now
            TempHoveredContainer = null;

            bool IsHovering = CheckIsHoveringOverContainer();

            if (TempHoveredContainer)
            {
                var FarmingPlotComponent = TempHoveredContainer.GetComponent<HRFarmingPlot>();
                if (!FarmingPlotComponent || (FarmingPlotComponent && FarmingPlotComponent.CheckIfHoveringPlotWithSeed(CurrentPlaceableGameObject.gameObject)))
                {
                    bIsValidToPlace = IsHovering && TempHoveredContainer.gameObject != CurrentPlaceableGameObject.gameObject;

                    UpdateMaterialBasedOnValid();
                    CurrentGhostObject.transform.localScale = OriginalGhostScale * 0.8f;
                    //SetGhostObjectVisibility(false);
                }
                else
                {
                    CurrentGhostObject.transform.localScale = OriginalGhostScale;
                }
            }
            else
            {
                CurrentGhostObject.transform.localScale = OriginalGhostScale;
            }

            if (!GhostMeshGOTemp)
            {
                GhostMeshGOTemp = new GameObject();
            }

            if (!GhostMeshGO)
            {
                // This is mostly for Dynamic Zipline is because it has no owning weapon.
                GhostMeshGO = CurrentPlaceableGameObject?.MeshRendererGameObject;

                if (CurrentGhostObject)
                {
                    GhostMeshGOTemp.transform.SetParent(CurrentGhostObject.transform, false);
                }

                GhostMeshGOTemp.transform.position = GhostMeshGO.transform.position;
                GhostMeshGOTemp.transform.rotation = GhostMeshGO.transform.rotation;
            }
            // Bad should prob detach this
            if (GhostMeshGO)
            {
                GhostMeshGO.transform.position = Vector3.Lerp(LastGhostMeshPos, GhostMeshGOTemp.transform.position, Time.deltaTime * 100.0f);
            }
            else
            {
                Debug.LogError("There is no GhostMeshGO in the Update function of BaseItemPlacingManager.");
            }
            //GhostMeshGO.transform.rotation = Quaternion.Slerp(LastGhostMeshRot, GhostMeshGOTemp.transform.rotation, Time.deltaTime * 400.0f);
            LastGhostMeshPos = GhostMeshGO.transform.position;
            LastGhostMeshRot = GhostMeshGO.transform.rotation;
            // Put the ghost object there.

            if (PlaceLineRenderer)
            {
                if (OwningPlayerCharacter && ((HeroPlayerCharacter)OwningPlayerCharacter).AnimScript && GhostMeshGOTemp)
                {
                    if (((HeroPlayerCharacter)OwningPlayerCharacter).AnimScript.GetBoneTransform(HumanBodyBones.RightHand))
                    {
                        PlaceLineRenderer.SetPositions(new Vector3[] { ((HeroPlayerCharacter)OwningPlayerCharacter).AnimScript.GetBoneTransform(HumanBodyBones.RightHand).position, GhostMeshGOTemp.transform.position });
                    }
                }
            }
        }
        else
        {
            this.enabled = false;
        }
    }

    void SnapGhostObjectToSelectedPivot(Vector3 snapPivotWorldPos)
    {
        //Uncommented for testing
        //if (CurrentGhostBuildingComponent.bForceCenter)
        //{
        //    SetGhostObjectPosition(GetPivotSnapOffset(OtherBuildingToSnapTo.transform, snapPivotWorldPos), true);
        //    return;
        //}

        //Use pivot to socket snap approach
        SetGhostObjectPosition(GetPivotSnapOffset(OtherBuildingToSnapTo.transform, snapPivotWorldPos, 0.2f), true);

        //Get closest pivot and snap the pivot to snapPivotWorldPos (pivot from toBuilding)
        Vector3 pivot = CurrentGhostBuildingComponent.GetClosestPivotWorldPosition(snapPivotWorldPos);

        Vector3 offset = snapPivotWorldPos - pivot;
        SetGhostObjectPosition(CurrentGhostObject.transform.position + offset, false);
    }
    //Kevin: Allows for centuring object to position even if object pivot isn't at center
    void SetGhostObjectPosition(Vector3 pos, bool forceCenter)
    {
        if (!forceCenter)
        {
            CurrentGhostObject.transform.position = pos;
            LastRecordedPosition = pos;
            return;
        }

        // Calculate the offset based on the pivot position
        Vector3 pivotOffset = CurrentGhostObject.transform.position - CurrentGhostObject.GetComponentInChildren<Collider>().bounds.center;

        // Calculate the new position to center the object
        Vector3 newPosition = pos + pivotOffset;
        newPosition.y = pos.y;
        // Move the object to the new centered position
        CurrentGhostObject.transform.position = newPosition;
        LastRecordedPosition = newPosition;
    }
    void SetGhostObjectPosition(Vector3 pos)
    {
        SetGhostObjectPosition(pos, CurrentGhostBuildingComponent && CurrentGhostBuildingComponent.bForceCenter);
    }

    public bool CheckIsHoveringOverContainer()
    {
        return IsHoveringOverFreeContainer(out TempHoveredContainer);
    }

    void SetGhostObjectVisibility(bool bVisible)
    {
        if (CurrentGhostObject)
        {
            if (PlaceLineRenderer)
            {
                PlaceLineRenderer.gameObject.SetActive(bVisible);
            }

            CurrentGhostObject.SetActive(bVisible);
            if (!bVisible && CurrentPlaceCollision)
            {
                CurrentPlaceCollision.RemoveAllCollisions();
            }
        }
    }

    public void StartPlacingObject(BaseItemPlaceable InPlaceable, bool bForce = false, bool bIsDragPlacement = false)
    {
        bool bCurrentlyPlacing = bIsPlacing;
        this.bIsDragPlacement = bIsDragPlacement;

        if (!InPlaceable.bPlacingEnabled)
        {
            if (bIsPlacing)
            {
                StopPlacingObject();
            }
            return;
        }

        if (_midPlacingObject)
        {
            return;
        }

        // Can only handle one placeable for now.
        if (bIsPlacing && !bForce)
        {
            StopPlacingObject(false);
        }
        // TODO: Make addressable

        CurrentGhostObject = InstantiateGhostObject(InPlaceable.OwningWeapon ? InPlaceable.OwningWeapon.gameObject : InPlaceable.gameObject, InPlaceable.PlacedRotation);

        if (CurrentGhostObject)
        {
            CurrentGhostObject.transform.localScale = InPlaceable.transform.lossyScale;
            OriginalGhostScale = CurrentGhostObject.transform.localScale;
        }

        CurrentPlaceableGameObject = InPlaceable;
        bIsPlacing = true;

        if (bIsValidToPlace)
            bIsValidToPlace = true;

        StartPlacingDelegate?.Invoke(this);
        UpdateMaterialBasedOnValid();

        this.enabled = true;

        //PlaceItemContextDelegate?.Invoke(PlaceContextInfo, true);
        //RotateItemContextDelegate?.Invoke(RotateContextInfo, true);

        bool bShouldTurnOnBuildingVisualizer = CanSpecialDestroy();

        if (((HRGameManager)BaseGameManager.Get) && ((HRGameManager)BaseGameManager.Get).ShopBuildingVisualizer)
        {
            ((HRGameManager)BaseGameManager.Get).ShopBuildingVisualizer.SetEnabled(bShouldTurnOnBuildingVisualizer);
        }
    }

    public void SetPlaceable_Server(GameObject g, bool bCreatedNew, Vector3 position, Quaternion rotation)
    {
        CurrentPlaceableGameObject = g.GetComponent<BaseItemPlaceable>();

        Vector3 Position = position;
        Quaternion Rotation = rotation;
        Collider ghostCollider = CurrentPlaceableGameObject.gameObject.GetComponentInChildren<Collider>();
        Vector3 Bounds;

        if (ghostCollider)
        {
            Bounds = ghostCollider.bounds.size;
        }
        else
        {
            Bounds = Vector3.one;
        }

        PlaceWeapon_Implementation(g, CurrentContainer ? CurrentContainer.gameObject : null, Position, Rotation, Bounds, false);
        PlaceWeapon_ClientRpc(g, CurrentContainer ? CurrentContainer.gameObject : null, Position, Rotation, Bounds, false);
    }

    public void RefreshGhostGameObject()
    {
        if (CurrentPlaceableGameObject)
        {
            if (!GhostMeshGOTemp)
            {
                GhostMeshGOTemp = new GameObject();
            }

            GhostMeshGOTemp.transform.SetParent(null, false);

            Destroy(CurrentGhostObject);
            StartPlacingObject(CurrentPlaceableGameObject);
        }
    }

    public void StopPlacingObject(bool bResetPlaceable = true)
    {
        if (GhostMeshGOTemp)
        {
            GhostMeshGOTemp.transform.SetParent(null, false);
        }

        if (((HRGameManager)BaseGameManager.Get) && ((HRGameManager)BaseGameManager.Get).ShopBuildingVisualizer)
        {
            ((HRGameManager)BaseGameManager.Get).ShopBuildingVisualizer.SetEnabled(false);
        }

        if (CurrentPlaceableGameObject && CurrentGhostObject)
        {
            CurrentPlaceableGameObject.StopPlacingObject(CurrentGhostObject.transform.position, CurrentGhostObject.transform.rotation);
        }

        if (CurrentGhostObject)
        {
            Destroy(CurrentGhostObject);
        }

        CurrentPlaceableGameObject = null;

        bIsPlacing = false;

        if (bResetPlaceable)
            bIsValidToPlace = false;

        CurrentPlaceCollision = null;
        bInitializedCurrentGrid = false;
        CurrentContainer = null;

        if (this)
        {
            this.enabled = false;
        }

        PlaceItemContextDelegate?.Invoke(PlaceContextInfo, false);
        //RotateItemContextDelegate?.Invoke(RotateContextInfo, false);

        ClearBuildingToSnapTo();

        ClearBuildingInteractables();
    }

    // All currently interactable buildings need to become uninteractable again.
    void ClearBuildingInteractables()
    {
        if (BuildingsWithEnabledInteractables.Count > 0)
        {
            var NewBuildingsWithEnabledInteractables = new List<HRBuildingComponent>();
            for (int i = 0; i < BuildingsWithEnabledInteractables.Count; ++i)
            {
                if (BuildingsWithEnabledInteractables[i])
                {
                    if (BuildingsWithEnabledInteractables[i].bDisableInteractableOnBuild)
                    {
                        if (BuildingsWithEnabledInteractables[i].OwningPlaceable &&
                            BuildingsWithEnabledInteractables[i].OwningPlaceable.OwningWeapon &&
                            BuildingsWithEnabledInteractables[i].OwningPlaceable.OwningWeapon.OwningInteractable)
                        {
                            BuildingsWithEnabledInteractables[i].OwningPlaceable.OwningWeapon.OwningInteractable.SetInteractionCollisionEnabled(false);
                        }
                    }
                    else
                    {
                        BuildingsWithEnabledInteractables[i].OwningPlaceable.OwningWeapon.OwningInteractable.SetInteractionCollisionEnabled(true);
                        NewBuildingsWithEnabledInteractables.Add(BuildingsWithEnabledInteractables[i]);
                    }
                }
            }

            BuildingsWithEnabledInteractables.Clear();
            BuildingsWithEnabledInteractables = NewBuildingsWithEnabledInteractables;
        }
    }

    public bool IsHoveringOverFreeContainer(out BaseContainer HoveredContainer)
    {
        HoveredContainer = CurrentContainer;

        // If we are placing a prefab, we are likely placing a wall. Just return false.
        if ((CurrentPlaceableGameObject && CurrentPlaceableGameObject.gameObject.scene == null) || IsCurrentObjectABuildingComponent())
        {
            CurrentContainer = null;
            HoveredContainer = null;
            return false;
        }

        // bad, now mixing hero and base classes also uses too many get components
        HeroPlayerCharacter HPC = (HeroPlayerCharacter)OwningPlayerCharacter;
        if ((!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) && HPC)
        {
            if (HPC.InteractionManager)
            {
                BaseInteractable Clickable = HPC.InteractionManager.GetClickableInteractable();
                if (Clickable && Clickable.gameObject != CurrentPlaceableGameObject && (!CurrentContainer || (CurrentContainer && CurrentContainer.gameObject != Clickable.gameObject)))
                {
                    HoveredContainer = Clickable.GetComponentInParent<BaseContainer>();
                    if (HoveredContainer && HoveredContainer.IsFree)
                    {
                        if (CurrentPlaceableGameObject && !HoveredContainer.CanInsert(CurrentPlaceableGameObject.OwningWeapon))
                        {
                            CurrentContainer = null;
                            //TODO: Localization
                            containerFailText = "Cannot nest containers!";
                            return false;
                        }
                        // Allow placing in container
                        CurrentContainer = HoveredContainer;
                        return true;
                    }
                    else
                    {
                        CurrentContainer = null;
                        if (HoveredContainer && HoveredContainer.Inventory)
                        {
                            if (HoveredContainer.Inventory.bDoNotAllowInput)
                            {
                                //TODO: Localization
                                containerFailText = "Container locked!";
                            }
                            else
                            {
                                //TODO: Localization
                                containerFailText = "Container full!";
                            }
                        }
                    }
                }
            }
        }

        CurrentContainer = null;
        return false;
    }

    public bool CanPlaceObject()
    {
        if (bIsPlacing)
        {
            // Check to see if we can even place the object first.
            if (bIsValidToPlace)
            {
                return true;
            }
            else
            {
                // Play error sound
                ((HRGameInstance)BaseGameInstance.Get)?.MusicManager?.PlayClipAtPoint(PlaceErrorSound, transform.position);
                //AudioSource.PlayClipAtPoint(PlaceErrorSound, transform.position);

                // Too far?
                bool bTooFar = Vector3.Distance(CurrentGhostObject.transform.position, OwningPlayerCharacter.gameObject.transform.position) > MaxPlaceDistance;

                if (bTooFar)
                {
                    //TODO: Localization
                    ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("Too far away!", CurrentGhostObject.transform.position);
                }
                else if (TempHoveredContainer)
                {
                    ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add(containerFailText, CurrentGhostObject.transform.position);
                }
                else
                {
                    if (!CurrentGhostBuildingComponent)
                    {
                        if (bObstacle)
                        {
                            //TODO: Localization
                            ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("There's an obstacle in the way!", CurrentGhostObject.transform.position);
                        }
                        else
                        {
                            //TODO: Localization
                            ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("Overlapping another object!", CurrentGhostObject.transform.position);
                        }
                    }
                    else
                    {
                        // Buildings can be placed through walls and overlapping other objects.
                        return true;
                    }
                }


                return false;
            }
        }

        return false;
    }

    public void PlaceObject(bool placeWholeStack = false, bool fromDrag = false)
    {
        if (LastQueueSystem)
        {
            LastQueueSystem.SetVisualizerVisibility(false);
            LastQueueSystem = null;
        }

        if (CurrentPlaceableGameObject)
        {
            if (CurrentGhostItemRef && !CurrentGhostItemRef.bCanPlace)
            {
                if ((HRGameManager.Get as HRGameManager).InvasionManager.IsInvader())
                    ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("Cannot build if invading!", CurrentGhostObject.transform.position);
                else
                    ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("Cannot build!", CurrentGhostObject.transform.position);
                return;
            }

            BaseItemPlaceable CachedItem = CurrentPlaceableGameObject;

            _midPlacingObject = true;

            bJustPlaced = true;

            if (CurrentPlaceCollision)
            {
                CurrentPlaceCollision.SetCollisionEnabled(false);
            }

            int Remainder = CachedItem.OwningWeapon ? CachedItem.OwningWeapon.StackCount : 1;
            // Get the current ghost object and put the original gameobject there
            // Add offset in case the object falls through.
            if (CachedItem.gameObject.scene.name != null)
            {
                Remainder = PlaceHeldItem(CachedItem, placeWholeStack, fromDrag: fromDrag);
            }
            else
            {
                // We are placing an instanced object.
                PlaceInstancedItem(CachedItem);
            }

            _midPlacingObject = false;

            //TODO: holy guacamole this feels bad to do. Need to clean up how placing is handled
            //Ensures that we swap to the current weapon when done placing items from the drag inventory
            //Needs to happen when _midPlacingObject is false after PlaceHeldItem is complete
            if (Remainder == 0)
            {
                OwningWeaponManager.SwitchToWeapon(OwningWeaponManager.CurrentWeapon);
            }

        }
    }

    private void PlaceInstancedItem(BaseItemPlaceable InPlaceable)
    {
        // Spawn the thing at the location
        if (CanPlaceObject() && InPlaceable.OwningWeapon)
        {
            Vector3 Position = CurrentGhostObject.transform.position;
            Quaternion Rotation = CurrentGhostObject.transform.rotation;
            bool RemoveFoliage = InPlaceable.willRemoveFoliage;
            hasHadValidPlacementSpot = false;
            bIsValidToPlace = false;

            PlayBuildAnimation();

            HRPlayerController controller = (HRPlayerController)BaseGameInstance.Get.GetLocalPlayerController();

            if (HRNetworkManager.IsHost())
            {
                PlaceInstancedItem_Server(controller, InPlaceable.OwningWeapon.ItemID, Position, Rotation, RemoveFoliage,
                    OtherBuildingToSnapTo && OtherBuildingToSnapTo.OwningPlaceable ? OtherBuildingToSnapTo.OwningPlaceable.gameObject : null);
            }
            else
            {
                PlaceInstancedItem_Command(controller, InPlaceable.OwningWeapon.ItemID, Position, Rotation, RemoveFoliage,
                    OtherBuildingToSnapTo && OtherBuildingToSnapTo.OwningPlaceable ? OtherBuildingToSnapTo.OwningPlaceable.gameObject : null);
            }

            ClearBuildingToSnapTo();
        }
    }

    [Mirror.Command]
    private void PlaceInstancedItem_Command(HRPlayerController controller, int InItemID, Vector3 Position, Quaternion Rotation, bool RemoveFoliage, GameObject OtherBuildingGameObject)
    {
        PlaceInstancedItem_Server(controller, InItemID, Position, Rotation, RemoveFoliage, OtherBuildingGameObject);
    }

    [Mirror.Server]
    private void PlaceInstancedItem_Server(HRPlayerController controller, int InItemID, Vector3 Position, Quaternion Rotation, bool RemoveFoliage, GameObject OtherBuildingGameObject)
    {
        GameObject NewItem = Instantiate(((HRGameInstance)BaseGameInstance.Get).ItemDB.ItemArray[InItemID].ItemPrefab, Position, Rotation);

        if (NewItem)
        {
            BaseWeapon NewWeapon = NewItem.GetComponent<BaseWeapon>();
            if (NewWeapon)
            {
                HRBuildingComponent NewBuilding = NewWeapon.MeshColliderGameObject.GetComponent<HRBuildingComponent>();
                if (NewBuilding)
                {
                    Mirror.NetworkServer.Spawn(NewItem);

                    (HRGameManager.Get as HRGameManager).OnBuildingPiecePlaced(NewWeapon);

                    PlaceInstancedItem_TargetRpc(controller.connectionToClient, NewWeapon);

                    if (OtherBuildingGameObject)
                    {
                        HRBuildingComponent SnappedBuilding = OtherBuildingGameObject.GetComponent<BaseWeapon>().MeshColliderGameObject.GetComponent<HRBuildingComponent>();
                        if (SnappedBuilding)
                        {
                            NewBuilding.HandleSnappedToBuilding(SnappedBuilding);
                        }
                    }
                }
            }
        }

        BaseWeapon SpawnedWeapon = NewItem.GetComponent<BaseWeapon>();
        Bounds ItemBounds = new Bounds();

        if (SpawnedWeapon)
        {
            ItemBounds = SpawnedWeapon.MeshColliders[0].bounds;
            SpawnedWeapon.AddIDToScene();
            // If this has no projectils physics (like a wall), update chunk here.
            // Otherwise this is done in the projectile physics when it lands.

            if (!SpawnedWeapon.ProjectilePhysics)
            {
                SpawnedWeapon.UpdateChunk();
            }
        }

        PlaceInstancedItem_ClientRpc(NewItem, ItemBounds.extents, ItemBounds.center, InItemID, Position, Rotation, RemoveFoliage);
    }

    [Mirror.ClientRpc]
    private void PlaceInstancedItem_ClientRpc(GameObject NewItem, Vector3 Extents, Vector3 Center, int InItemID, Vector3 Position, Quaternion Rotation, bool RemoveFoliage)
    {
        PlaceInstancedItem_Implementation(NewItem, Extents, Center, InItemID, Position, Rotation, RemoveFoliage);
    }

    [Mirror.TargetRpc]
    private void PlaceInstancedItem_TargetRpc(Mirror.NetworkConnection target, BaseWeapon NewWeapon)
    {
        (HRGameManager.Get as HRGameManager).OnBuildingPiecePlacedLocal(NewWeapon);
    }

    public void PlayPlaceParticlesAndAnimation()
    {
        if (HRNetworkManager.IsHost())
        {
            PlayBuildAnimation();
            PlayInstancedItemParticles(CurrentGhostObject.transform.position, CurrentGhostObject.transform.rotation);
            //PlayPlaceParticlesAndAnimation_ClientRpc(CurrentGhostObject.transform.position, CurrentGhostObject.transform.rotation);
        }
        else
        {
            //PlayPlaceParticlesAndAnimation_Command(CurrentGhostObject.transform.position, CurrentGhostObject.transform.rotation);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void PlayPlaceParticlesAndAnimation_Command(Vector3 GhostObjPos, Quaternion GhostObjRot)
    {
        PlayPlaceParticlesAndAnimation_ClientRpc(CurrentGhostObject.transform.position, CurrentGhostObject.transform.rotation);
    }

    [Mirror.ClientRpc]
    public void PlayPlaceParticlesAndAnimation_ClientRpc(Vector3 GhostObjPos, Quaternion GhostObjRot)
    {
        PlayPlaceParticlesAndAnimation_Implementation(CurrentGhostObject.transform.position, CurrentGhostObject.transform.rotation);
    }

    public void PlayPlaceParticlesAndAnimation_Implementation(Vector3 GhostObjPos, Quaternion GhostObjRot)
    {
        if (!HRNetworkManager.HasControl(netIdentity))
        {
            PlayBuildAnimation();
        }

        PlayInstancedItemParticles(CurrentGhostObject.transform.position, CurrentGhostObject.transform.rotation);
    }

    public void PlayBuildAnimation()
    {
        if (OwningPlayerCharacter)
        {
            if (OwningPlayerCharacter.PlayerController && OwningPlayerCharacter.PlayerController.isLocalPlayer)
            {
                BaseScreenShakeManager.DoScreenShake(OwningPlayerCharacter.PlayerCamera.CameraTargetGameObject.transform, 0.3f, 0.05f, 10, Ease.InOutElastic);
                //OwningPlayerCharacter.PlayerCamera.CameraTargetGameObject.transform.DORewind();
                //OwningPlayerCharacter.PlayerCamera.CameraTargetGameObject.transform.DOShakePosition(0.3f, 0.05f, 10).SetEase(Ease.InOutElastic);
            }

            HRMasterAnimDatabase.HRAnimStruct AnimStruct;
            if (((HRGameInstance)BaseGameInstance.Get).MasterAnimDB.GetAnimStruct("BuildAnimation", out AnimStruct))
            {
                if (OtherPlayerCharacter == OwningPlayerCharacter)
                    OtherPlayerCharacter.PlayAnimation(AnimStruct.ClipToPlay, 0.5f, AnimStruct.AnimLayer);
                else
                    OwningPlayerCharacter.PlayAnimation(AnimStruct.ClipToPlay, 0.5f, AnimStruct.AnimLayer);
            }
        }
    }

    private void PlayInstancedItemParticles(Vector3 pos, Quaternion rot)
    {
        BaseObjectPoolManager.Get.InstantiateFromPool(PlaceInstancedItemParticle, false, true, pos, false, rot);
    }

    private void PlaceInstancedItem_Implementation(GameObject NewItem, Vector3 Extents, Vector3 Center, int InItemID, Vector3 Position, Quaternion Rotation, bool RemoveFoliage)
    {
        // Play an animation on the player
        // TODO move this so it's not a clientrpc for the owning player
        if (!HRNetworkManager.HasControl(netIdentity))
        {
            PlayBuildAnimation();
        }

        ConstructedItemDelegate?.Invoke(this, NewItem);

        PlayInstancedItemParticles(Position, Rotation);
    }

    public void PlaceHeldItemWithoutGhost(BaseItemPlaceable InPlaceable, BaseContainer PassedContainer, bool placeWholeStack = false)
    {
        PlaceHeldItem(InPlaceable, placeWholeStack, PassedContainer);
    }

    public void PlaceOnFoot(BaseItemPlaceable InPlaceable, bool placeWholeStack = false)
    {
        PlaceHeldItem(InPlaceable, placeWholeStack, null, true);
    }

    private void TurnColliderOn(BaseWeapon InWeapon, Vector3 position)
    {
        //(InWeapon.MeshColliders[0] as BoxCollider).enabled = true;
    }

    //TODO: this should probably mostly be moved to server no?
    private int PlaceHeldItem(BaseItemPlaceable InPlaceable, bool placeWholeStack, BaseContainer PassedContainer = null, bool bPlaceOnFoot = false, bool fromDrag = false)
    {
        BaseWeapon PlaceableWeapon = InPlaceable.OwningWeapon;
        int remainder = PlaceableWeapon != null ? PlaceableWeapon.StackCount : 1;
        BaseContainer CachedContainer;
        if (!PassedContainer)
            CachedContainer = CurrentContainer;
        else
            CachedContainer = PassedContainer;

        if (CachedContainer && !CachedContainer.Inventory.RunFilterCheck(InPlaceable.OwningWeapon))
        {
            return remainder;
        }

        if (PlaceableWeapon)
        {
            if (CurrentContainer && PlaceableWeapon.StackCount > CurrentContainer.Inventory.MaxStackLimit)
            {
                placeWholeStack = false;
            }
        }

        int amountToPlace = placeWholeStack ? -1 : 1;

        if (PlaceableWeapon)
        {

            int previousStackCount = PlaceableWeapon.StackCount;
            if ((PlaceableWeapon.OwningWeaponManager)
            && (PlaceableWeapon.OwningInventory && PlaceableWeapon.OwningInventory.gameObject == PlaceableWeapon.OwningWeaponManager.gameObject))
            {
                PlaceableWeapon.OwningWeaponManager.RemoveWeapon(InPlaceable.OwningWeapon, false, amountToPlace, CachedContainer == null, CachedContainer == null, true);
            }
            else
            {
                if (InPlaceable && PlaceableWeapon && PlaceableWeapon.OwningInventory && OwningWeaponManager)
                {
                    bool removed = OwningWeaponManager.TryRemovingFromInventory(PlaceableWeapon.OwningInventory,
                        PlaceableWeapon, false, amountToPlace, CachedContainer == null, true, true);
                    if (removed)
                    {
                        remainder = placeWholeStack ? 0 : PlaceableWeapon.StackCount - amountToPlace;
                    }
                    if (PlaceableWeapon.OwningWeaponManager)
                    {
                        PlaceableWeapon.SetOwningWeaponManager(null);
                        PlaceableWeapon.HandleEquip(false, null, false);
                    }
                }
            }

            if (InPlaceable.OwningWeapon && InPlaceable.OwningWeapon.MeshColliders.Length > 0)
            {
                InPlaceable.OwningWeapon.MeshColliders[0].enabled = false;
                SavedBoxCollider = InPlaceable.OwningWeapon.MeshColliders[0];
            }

            bPlacedItem = 3;
            //InPlaceable.GetComponentInParent<BaseWeapon>().OnWeaponPlaced -= TurnColliderOn;
            //InPlaceable.GetComponentInParent<BaseWeapon>().OnWeaponPlaced += TurnColliderOn;

            // Has a possibility of changing due to 
            // the decrease in the current stack size.
            if (CurrentPlaceableGameObject
                && CurrentPlaceableGameObject != InPlaceable)
            {
                InPlaceable = CurrentPlaceableGameObject;
                InPlaceable.OwningWeapon = InPlaceable?.GetComponent<BaseWeapon>();
            }

            bool createdNew = false;

            Vector3 PlacingPosition;
            Transform TransformRotationToUse;
            if (CurrentGhostObject)
            {
                PlacingPosition = CurrentGhostObject.transform.position;
                TransformRotationToUse = CurrentGhostObject.transform;
            }
            else
            {
                TransformRotationToUse = InPlaceable.transform;

                Vector3 CameraPosition = PlayerCamera.GetCamera().transform.position;
                float distPlayer = Vector2.Distance(new Vector2(OwningPlayerCharacter.transform.position.x, OwningPlayerCharacter.transform.position.z), new Vector2(CameraPosition.x, CameraPosition.z));
                Ray PlacingRay = PlayerCamera.GetAimRay();

                if (bPlaceOnFoot)
                {
                    PlacingRay = new Ray(OwningPlayerCharacter.transform.position, -OwningPlayerCharacter.transform.up);
                    CameraPosition = OwningPlayerCharacter.transform.position;
                }

                RaycastHit RaycastHit = new RaycastHit();

                bool IsHit;

                if (bPlaceOnFoot)
                {
                    IsHit = Physics.Raycast(OwningPlayerCharacter.transform.position, -OwningPlayerCharacter.transform.up, out RaycastHit, 3000.0f, PlaceLayerMask);
                }
                else
                {
                    IsHit = Physics.Raycast(CameraPosition + PlacingRay.direction.normalized * distPlayer, PlacingRay.direction, out RaycastHit, 3000.0f, PlaceLayerMask);
                }

                PlacingPosition = RaycastHit.point;
            }

            if (CachedContainer && !IsCurrentObjectABuildingComponent())
            {
                int TargetSlot = -1;
                HRDisplayContainer DisplayContainer = CachedContainer.GetComponent<HRDisplayContainer>();
                if (DisplayContainer)
                {
                    TargetSlot = DisplayContainer.GetClosestDisplaySlot(PlacingPosition);
                }

                // Add a new instance of the prefab if this item is stacked and we are not inserting the whole stack

                if (placeWholeStack || previousStackCount == 1 && InPlaceable.OwningWeapon.StackCount == 1)
                {
                    CachedContainer.Inventory.AddWeapon(InPlaceable.OwningWeapon, TargetSlot, bAutoFill: true);
                }
                else
                {
                    if (HRNetworkManager.IsHost())
                    {
                        SpawnWeaponToInventory_Implementation(InPlaceable.OwningWeapon.ItemID, CachedContainer.Inventory, TargetSlot, PlacingPosition, TransformRotationToUse.eulerAngles);
                    }
                    else
                    {
                        SpawnWeaponToInventory_Command(InPlaceable.OwningWeapon.ItemID, CachedContainer.Inventory, TargetSlot, PlacingPosition, TransformRotationToUse.eulerAngles);
                    }

                    return remainder;
                }
            }

            Vector3 Position = PlacingPosition;
            Quaternion Rotation = TransformRotationToUse.rotation;
            Collider ghostCollider = InPlaceable.gameObject.GetComponentInChildren<Collider>();
            Vector3 Bounds;
            if (ghostCollider)
            {
                Bounds = ghostCollider.bounds.size;
                //Debug.Log($"Immediate Bounds for {ghostCollider.gameObject.name}: {ghostCollider.bounds}");
            }
            else
            {
                Bounds = Vector3.one;
            }

            if (InPlaceable.OwningWeapon)
            {
                if (InPlaceable.OwningWeapon.StackCount > 1)
                {
                    createdNew = true;
                }

                if (!createdNew)
                {
                    InPlaceable.OwningWeapon.OwningWeaponManager = null;
                }

                // Physic check the last position when moving an item this way
                if (fromDrag) { PlaceableWeapon.PhysicsCheckNearbyObjects(PlaceableWeapon.transform.position, PlaceableWeapon.transform.rotation); }
            }

            if (HRNetworkManager.IsHost())
            {
                PlaceWeapon_Implementation(placeWholeStack || InPlaceable.OwningWeapon.StackCount == 1 ? InPlaceable.gameObject : null, CachedContainer ? CachedContainer.gameObject : null, Position, Rotation, Bounds, !createdNew);
                PlaceWeapon_ClientRpc(placeWholeStack || InPlaceable.OwningWeapon.StackCount == 1 ? InPlaceable.gameObject : null, CachedContainer ? CachedContainer.gameObject : null, Position, Rotation, Bounds, !createdNew);
            }
            else
            {
                PlaceWeapon_Command(placeWholeStack || InPlaceable.OwningWeapon.StackCount == 1 ? InPlaceable.gameObject : null, CachedContainer ? CachedContainer.gameObject : null, Position, Rotation, Bounds, !createdNew);
            }

        }
        else
        {
            // This is only for ziplines
            PlacedItemDelegate?.Invoke(this, InPlaceable?.gameObject);
            StopPlacingObject();
        }
        return remainder;
    }

    [Mirror.Command]
    public void SpawnWeaponToInventory_Command(int ItemID, BaseInventory Inventory, int TargetSlot, Vector3 pos, Vector3 rot)
    {
        SpawnWeaponToInventory_Implementation(ItemID, Inventory, TargetSlot, pos, rot);
    }

    public void SpawnWeaponToInventory_Implementation(int ItemID, BaseInventory Inventory, int TargetSlot, Vector3 pos, Vector3 rot)
    {
        var prefab = Inventory.SpawnItemIntoInventory(((HRGameInstance)HRGameInstance.Get).ItemDB.ItemArray[ItemID].ItemPrefab);
        Inventory.AddWeapon(prefab.GetComponent<BaseWeapon>(), TargetSlot, bAutoFill: true);

        var InPlaceable = prefab.GetComponent<BaseItemPlaceable>();

        Vector3 Position = pos;
        Quaternion Rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        Collider ghostCollider = InPlaceable.gameObject.GetComponentInChildren<Collider>();
        Vector3 Bounds;

        if (ghostCollider)
        {
            Bounds = ghostCollider.bounds.size;
            //Debug.Log($"Immediate Bounds for {ghostCollider.gameObject.name}: {ghostCollider.bounds}");
        }
        else
        {
            Bounds = Vector3.one;
        }

        if (HRNetworkManager.IsHost())
        {
            PlaceWeapon_Implementation(InPlaceable.gameObject, Inventory.gameObject, Position, Rotation, Bounds, false);
            PlaceWeapon_ClientRpc(InPlaceable.gameObject, Inventory.gameObject, Position, Rotation, Bounds, false);
        }
        else
        {
            PlaceWeapon_Command(InPlaceable.gameObject, Inventory.gameObject, Position, Rotation, Bounds, false);
        }
    }

    [Mirror.Command]
    public void PlaceWeapon_Command(GameObject InPlaceable, GameObject TargetContainer, Vector3 Location, Quaternion Rotation, Vector3 Bounds, bool bRemoveWeaponManager)
    {
        PlaceWeapon_Implementation(InPlaceable ? InPlaceable.gameObject : null, TargetContainer ? TargetContainer.gameObject : null, Location, Rotation, Bounds, bRemoveWeaponManager);
        PlaceWeapon_ClientRpc(InPlaceable ? InPlaceable.gameObject : null, TargetContainer ? TargetContainer.gameObject : null, Location, Rotation, Bounds, bRemoveWeaponManager);
    }

    [Mirror.ClientRpc]
    public void PlaceWeapon_ClientRpc(GameObject InPlaceable, GameObject TargetContainer, Vector3 Location, Quaternion Rotation, Vector3 Bounds, bool bRemoveWeaponManager)
    {
        if (HRNetworkManager.IsHost()) return;

        if (InPlaceable)
        {
            PlaceWeapon_Implementation(InPlaceable.gameObject, TargetContainer ? TargetContainer.gameObject : null, Location, Rotation, Bounds, bRemoveWeaponManager);
        }
    }

    [Mirror.Client]
    public void PlaceWeapon_Implementation(GameObject InPlaceable, GameObject TargetContainer, Vector3 Location, Quaternion Rotation, Vector3 Bounds, bool bRemoveWeaponManager)
    {
        if (InPlaceable)
        {
            // TODO: addressable
            BaseItemPlaceable Placeable = InPlaceable.GetComponent<BaseItemPlaceable>();
            if (Placeable)
            {
                BaseWeapon WeaponToPlace = Placeable.GetComponent<BaseWeapon>();

                if (bRemoveWeaponManager)
                    WeaponToPlace.OwningWeaponManager = null;

                if (HRNetworkManager.IsHost())
                {
                    HRSenseTrigger.SendTriggerOnSight(Placeable.transform, HRSenseTrigger.DefaultRadius, true);
                }

                PlacedItemDelegate?.Invoke(this, Placeable.gameObject);

                //RemoveGrass(Location, Bounds);

                BaseContainer Container = TargetContainer ? TargetContainer.GetComponent<BaseContainer>() : null;

                if (!Container)
                {
                    Placeable.PlaceObject(Location, Rotation, false, true);// !Container, (Container != null && Container.bHandleShowHide));
                }

                // Call their event when they are placed down somewhere
                WeaponToPlace.OnWeaponPlaced?.Invoke(Placeable.GetComponent<BaseWeapon>(), Location);

                if (WeaponToPlace)
                {
                    WeaponToPlace.ToggleGPUInstancingState(true);

                    if (HRNetworkManager.IsHost() && TargetContainer == null && !Placeable.bCanStickToWall)
                    {
                        WeaponToPlace.ProjectilePhysics?.RaycastDownwards(false);
                    }
                }


                if (PlaceItemParticle && WeaponToPlace)
                {
                    if (PlaceItemParticle)
                    {
                        HRPoofVFXController PoofFX = FXPoolManager.Get.SpawnVFXFromPool(PlaceItemParticle, Location, Rotation)?.GetComponent<HRPoofVFXController>();
                        //HRPoofVFXController PoofFX = BaseObjectPoolManager.Get.InstantiateFromPool(PlaceItemParticle, false, true, Location, false, Rotation)?.GetComponent<HRPoofVFXController>();

                        if (PoofFX && WeaponToPlace.WeaponMeshRenderer)
                        {
                            PoofFX.SetMeshEmission(WeaponToPlace.WeaponMeshRenderer, WeaponToPlace.WeaponMeshRenderer.material);
                        }
                    }
                }
            }
        }
    }

    //public void RemoveGrass(Vector3 placementPosition, Vector3 placementBounds)
    //{
    //    //Collider objectCollider = placedObject.GetComponentInChildren<Collider>();
    //    //Debug.Log(placementBounds);
    //    _grassControl.RemoveGrass(placementPosition, placementBounds);
    //}

    // This is to apply tags etc. after graph scene update
    static void HandleGraphsUpdated(AstarPath script)
    {

    }

    public static void ReloadNavMeshTilesInBound(Bounds InBounds, float ExtentYModifier = 0.0f, bool bReloadTile = true, bool bUpdateGraph = true)
    {
        if (!AstarPath.active || AstarPath.active.data.recastGraph == null) return;

        if (bReloadTile)
        {
            Pathfinding.NavGraph[] AllGraphs = AstarPath.active.data.graphs;

            for (int i = 0; i < AllGraphs.Length; i++)
            {
                Pathfinding.RecastGraph graph = AllGraphs[i] as Pathfinding.RecastGraph;

                if (graph == null) continue;

                Pathfinding.IntRect TouchingTiles = graph.GetTouchingTiles(InBounds);
                if (TouchingTiles.xmin > 0 && TouchingTiles.ymin > 0)
                {
                    InBounds = graph.GetTileBounds(TouchingTiles);
                    float YToUse = ExtentYModifier != 0.0f ? ExtentYModifier : InBounds.extents.y;
                    InBounds.extents = new Vector3(InBounds.extents.x, YToUse, InBounds.extents.z);
                    graph.RebuildInBounds(InBounds);
                }
            }
        }

        if (bUpdateGraph)
        {
            AstarPath.active.UpdateGraphs(InBounds);
        }

        RebuildFloorTilesInBound(InBounds, ExtentYModifier);
    }

    public static void RebuildFloorTilesInBound(Bounds InBounds, float ExtentYModifier = 0.0f)
    {
        // Get all the floor tiles in these bounds and recalculate their stuff as well.
        Collider[] FloorTilesOverlapped = Physics.OverlapBox(InBounds.center - new Vector3(0, InBounds.extents.y, 0), (InBounds.extents * 2) + new Vector3(0, ExtentYModifier, 0), Quaternion.identity, LayerMask.NameToLayer("FloorTile"));

        bool bShouldUpdateGraph = false;

        for (int i = 0; i < FloorTilesOverlapped.Length; ++i)
        {
            Pathfinding.GraphUpdateScene GraphSceneUpdater = FloorTilesOverlapped[i].transform.GetComponent<Pathfinding.GraphUpdateScene>();
            if (GraphSceneUpdater && !AstarPath.UpdateScenes.Contains(GraphSceneUpdater))
            {
                AstarPath.UpdateScenes.Add(GraphSceneUpdater);
                bShouldUpdateGraph = true;
            }
        }

        if (bShouldUpdateGraph)
        {
            AstarPath.active.UpdateGraphs(new Bounds());
        }
    }

    public void CalculateAndPlaceGhostGameObject()
    {
        if (CurrentGhostObject && CurrentPlaceableGameObject)
        {
            // CurrentGhostObject.transform.position;
        }
    }

    public void RotateGhostGameObjectClockwise()
    {
        RotateGhostGameObjectClockwise(RotateDegreeAmount);
    }

    public void RotateGhostGameObjectCounterClockwise()
    {
        RotateGhostGameObjectClockwise(-RotateDegreeAmount);
    }


    public void RotateGhostGameObjectClockwise(float InAngle, bool centerRotation = true)
    {
        if (CurrentGhostObject && !EventSystem.current.IsPointerOverGameObject())
        {
            if (centerRotation)
            {
                //Transform t = new GameObject("fuck").transform;
                Vector3 pivot = CurrentGhostObject.GetComponentInChildren<Collider>().bounds.center;
                //t.position = pivot;

                //CurrentGhostObject.transform.SetParent(t, true);
                //t.Rotate(CurrentGhostObject.transform.up, InAngle);

                CurrentGhostObject.transform.RotateAround(pivot, CurrentGhostObject.transform.up, InAngle);
            }
            else
            {
                CurrentGhostObject.transform.Rotate(CurrentGhostObject.transform.up, InAngle);
            }

            LastSavedRotation = CurrentGhostObject.transform.rotation;

            ((HRGameInstance)BaseGameInstance.Get)?.MusicManager?.PlayClipAtPoint(RotateSound, transform.position);
            //AudioSource.PlayClipAtPoint(RotateSound, transform.position);
        }
    }

    public bool IsCurrentObjectABuildingComponent()
    {
        if (!CurrentPlaceableGameObject)
        {
            return false;
        }

        if (CurrentPlaceableGameObject.OwningWeapon == null)
        {
            return false;
        }

        if (CurrentPlaceableGameObject && CurrentPlaceableGameObject.OwningWeapon?.MeshColliderGameObject)
        {
            return CurrentPlaceableGameObject.OwningWeapon.MeshColliderGameObject.GetComponent<HRBuildingComponent>() != null;
        }

        return false;
    }

    public void UpdateMaterialBasedOnValid()
    {
        Material MaterialToUse = bIsValidToPlace ? ValidGhostMaterial : InvalidGhostMaterial;

        if (!IsCurrentObjectABuildingComponent())
        {
            if (CurrentContainer && CurrentContainer.Inventory && !CurrentContainer.Inventory.IsInventoryFull() && CurrentContainer.gameObject != CurrentPlaceableGameObject.gameObject)
            {
                var FarmingPlotComponent = CurrentContainer.GetComponent<HRFarmingPlot>();
                if (!FarmingPlotComponent || (FarmingPlotComponent && FarmingPlotComponent.CheckIfHoveringPlotWithSeed(CurrentPlaceableGameObject.gameObject)))
                {
                    MaterialToUse = ContainerGhostMaterial;
                }
            }
        }

        if (CurrentGhostItemRef && !CurrentGhostItemRef.bCanPlace)
        {
            MaterialToUse = InvalidGhostMaterial;
        }

        float BiggestScale = 0.0f;

        for (int i = 0; i < CurrentGhostRenderers.Count; ++i)
        {
            if (CurrentGhostRenderers[i])
            {
                CurrentGhostRenderers[i].material = MaterialToUse;

                if (CurrentGhostRenderers[i].transform.localScale.x > BiggestScale)
                {
                    BiggestScale = CurrentGhostRenderers[i].transform.localScale.x;
                }
            }
        }

        // Scale outline based on scale of object.
        MaterialToUse.SetFloat("_OutlineWidth", 1 / BiggestScale);
    }

    // Ghost mesh - instantiate a duplicate copy, delete everything that is not Mesh Renderer, rigid body, and colliders. 
    // Iterate through all mesh renderers and change their materials to the ghost material.
    private GameObject InstantiateGhostObject(GameObject InGameObject, Quaternion ObjectRotation)
    {
        if (InGameObject)
        {
            if (!GhostMeshGOTemp)
            {
                GhostMeshGOTemp = new GameObject();
            }

            // Should just have a separate gameobject that weeds all these out. And then have this as a fallback.

            // This is weird, but this is because it's marking it as a dummy so that if this object has a save component, it doesn't save it on awake.
            string OldTag = InGameObject.tag;
            InGameObject.tag = "Dummy";

            // Terrible, but getting displays to work for now after a load. TODO: make all items in playre inventory soemhow call their start/awakee functions when non active (non equipped)
            HRDisplayContainer DisplayContainer = InGameObject.GetComponent<HRDisplayContainer>();
            if (DisplayContainer)
            {
                DisplayContainer.InitializeDisplayCase();
            }

            BaseWeapon weapon = InGameObject.GetComponent<BaseWeapon>();
            if (weapon)
            {
                weapon.SetNavObstaclesEnabled(false);
            }

            // TURN GAMEOBJECT OFF AND REMOVE THE NETWORK IDENTITY, BIG HACK TIME BIG HACK TIME!!!!!
            //InGameObject.SetActive(false);
            GameObject NewGhostObject = Instantiate(InGameObject,
                InGameObject.transform.position,
                InGameObject.transform.rotation);
            //InGameObject.SetActive(true);
            InGameObject.tag = OldTag;


            Component[] AllComponents = NewGhostObject.GetComponentsInChildren<Component>(true);

            CurrentGhostRenderers.Clear();

            GhostMeshGO = null;
            CurrentGhostBuildingComponent = null;
            CurrentGhostMaterialCheckComponent = null;

            // Iterate backwards because we are potentially destroying it.
            for (int i = AllComponents.Length - 1; i >= 0; --i)
            {
                Renderer TempMeshRenderer = null;

                // Mark "from ghost object" for streamable data assets
                if (AllComponents[i] as BaseDataAsset)
                {
                    BaseDataAsset dataAsset = (BaseDataAsset)AllComponents[i];
                    dataAsset.fromGhostObject = true;
                    continue;
                }
                else if (AllComponents[i] as MeshRenderer || AllComponents[i] as SkinnedMeshRenderer)
                {
                    TempMeshRenderer = (Renderer)AllComponents[i];
                    if (TempMeshRenderer.name.Contains("Level"))
                    {
                        // Skip LOD renderers
                        TempMeshRenderer.gameObject.SetActive(false);
                        continue;
                    }
                    // If it is a mesh renderer, change all materials to the ghost material.
                    for (int j = 0; j < TempMeshRenderer.materials.Length; ++j)
                    {
                        // Todo: add these materials somehow to an array so that we can change this to red/green when placement isn't valid.
                        TempMeshRenderer.material = InvalidGhostMaterial;
                        CurrentGhostRenderers.Add(TempMeshRenderer);
                    }
                }
                else if (AllComponents[i] as BaseQueueSystem)
                {
                    LastQueueSystem = AllComponents[i] as BaseQueueSystem;
                    LastQueueSystem.gameObject.SetActive(false);
                    LastQueueSystem.SetVisualizerVisibility(true);
                }
                else if (AllComponents[i] as Collider)
                {
                    if (AllComponents[i] is MeshCollider)
                    {
                        MeshCollider CastedMeshCollider = (MeshCollider)AllComponents[i];
                        if (CastedMeshCollider && !CastedMeshCollider.convex)
                        {
                            Destroy(AllComponents[i]);
                            continue;
                        }
                    }

                    Collider CastedCollider = (Collider)AllComponents[i];
                    CastedCollider.isTrigger = true;
                }
                else if (AllComponents[i] as MeshFilter)
                {

                }
                else if (AllComponents[i] as Transform)
                {

                }
                else if (AllComponents[i] as Rigidbody)
                {

                }
                else if (AllComponents[i] as BaseItemPlacementCollision)
                {
                    if (!((BaseItemPlacementCollision)AllComponents[i]).bDummy)
                    {
                        CurrentPlaceCollision = (BaseItemPlacementCollision)AllComponents[i];
                        CurrentPlaceCollision.SetCollisionEnabled(true);  //TEST
                    }
                }
                else if (AllComponents[i] as BaseWeapon)
                {
                    BaseWeapon WeaponToUse = ((BaseWeapon)AllComponents[i]);
                    // Store the renderer GO to interpolate
                    GhostMeshGO = WeaponToUse.PlaceableComponent.MeshRendererGameObject;
                    //GhostMeshGO.transform.localRotation = InGameObject.GetComponent<BaseItemPlaceable>().OriginalMeshRenderRotation;

                    BaseItemPlaceable PlaceableComponent = InGameObject.GetComponent<BaseItemPlaceable>();

                    if (InGameObject.scene.name == null)
                    {
                        WeaponToUse.PlaceableComponent.InitializePlaceable();
                        PlaceableComponent = WeaponToUse.PlaceableComponent;
                    }
                    else
                    {
                        PlaceableComponent = InGameObject.GetComponent<BaseItemPlaceable>();
                    }

                    LastGhostMeshPos = GhostMeshGO.transform.position;
                    LastGhostMeshRot = GhostMeshGO.transform.rotation;
                    GhostMeshGO.transform.localRotation = PlaceableComponent.OriginalMeshRenderRotation;
                    GhostMeshGO.transform.localPosition = PlaceableComponent.OriginalMeshRenderLocation;

                    // THIS IS BAD USE TRNASFORMS
                    GhostMeshGOTemp.transform.SetParent(NewGhostObject.transform, false);

                    GhostMeshGOTemp.transform.position = GhostMeshGO.transform.position;
                    GhostMeshGOTemp.transform.rotation = GhostMeshGO.transform.rotation;

                    if (WeaponToUse)
                    {
                        WeaponToUse.SetMeshCollisionEnabled(true, true, true);
                    }

                    // Return the mesh location
                    if (ItemRarityTint.Length != 0)
                    {
                        ValidGhostMaterial.SetColor("HighlightColor", ItemRarityTint[(int)((BaseWeapon)AllComponents[i]).ItemRarity]);
                    }

                    if (WeaponToUse && WeaponToUse.MeshColliders != null && WeaponToUse.MeshColliders.Length > 0)
                    {
                        CurrentPlaceCollider = WeaponToUse.MeshColliders[0] as BoxCollider;
                    }
                    else
                    {
                        CurrentPlaceCollider = null;
                    }

                    Destroy(AllComponents[i]);
                }
                else if (AllComponents[i] as Transform)
                {
                    // Can't destroy transforms.
                }
                else if (AllComponents[i] as HRBuildingComponent)
                {
                    CurrentGhostBuildingComponent = AllComponents[i] as HRBuildingComponent;
                    CurrentGhostBuildingComponent.bIsGhostObject = true;
                }
                else if (AllComponents[i] as HRMaterialCheckComponent)
                {
                    CurrentGhostMaterialCheckComponent = AllComponents[i] as HRMaterialCheckComponent;
                }
                else
                {
                    // Delete it.
                    Destroy(AllComponents[i]);
                }
            }

            if (GhostMeshGO)
            {
                GhostMeshGO.SetActive(true);
            }

            if (CurrentGhostBuildingComponent)
            {
                CurrentPlaceCollision.SetCollisionEnabled(false);
            }

            if (CurrentGhostMaterialCheckComponent)
            {
                var GhostMesh = NewGhostObject.GetComponentInChildren<MeshFilter>();
                if (GhostMesh)
                    GhostMesh.mesh = null;
            }

            Vector3 TargetRotation = ObjectRotation.eulerAngles;
            TargetRotation.x = 0;
            TargetRotation.z = 0;
            TargetRotation.y = ((int)TargetRotation.y / (int)RotateDegreeAmount) * RotateDegreeAmount;

            // Zero the rotation since we don't want it to be weird and diagonal.
            NewGhostObject.transform.rotation = Quaternion.Euler(TargetRotation);
            NewGhostObject.SetActive(false);

            CurrentGhostItemRef = NewGhostObject.AddComponent<BaseGhostItem>();

            return NewGhostObject;
        }

        return null;
    }

    public void DragPickup(BaseWeapon pickupWeapon)
    {
        if (bIsPlacing) return;
        if (CurrentPlaceableGameObject) return;
        if (pickupWeapon == null) return;
        if (!pickupWeapon.bCanPickup) return;
        if (pickupWeapon.LockedItemRef && pickupWeapon.LockedItemRef.IsLocked()) return;

        StartPlacingObject(pickupWeapon.PlaceableComponent, bIsDragPlacement: true);
    }

    public void DragDrop()
    {
        if (!bIsPlacing) return;
        if (!bIsDragPlacement) return;
        if ((!EventSystem.current || !EventSystem.current.IsPointerOverGameObject()) && CanPlaceObject())
        {
            PlaceObject(true, true);
        }
        StopPlacingObject();
        OwningWeaponManager.SwitchToWeapon(OwningWeaponManager.CurrentWeapon);
    }

    public override void OnDisable()
    {
        SetGhostObjectVisibility(false);
    }

    public void OnDrawGizmos()
    {
        if (OtherBuildingToSnapTo)
        {
            // Draw snap hit
            Gizmos.color = Color.white;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, CurrentPlaceableGameObject.transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;

            Gizmos.DrawWireCube(SnapHitPosition, new Vector3(1.0f, 1.0f, 1.0f));// (CurrentPlaceableGameObject.OwningWeapon.MeshRendererGameObject.transform.TransformVector((CurrentPlaceableGameObject.OwningWeapon.MeshColliders[0] as BoxCollider).size)));
            Gizmos.matrix = Matrix4x4.identity;

            HRBuildingSocket FromPivot;
            HRBuildingSocket ToSocket;

            HRBuildingComponent.SnapBuildingToBuilding(SnapHitPosition, CurrentGhostBuildingComponent, out FromPivot, OtherBuildingToSnapTo, out ToSocket);

            // Draw location where to snap 
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(CurrentGhostBuildingComponent.transform.TransformPoint(FromPivot.LocalPosition), 0.8f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(OtherBuildingToSnapTo.transform.TransformPoint(ToSocket.LocalPosition), 0.5f);
        }
    }
}