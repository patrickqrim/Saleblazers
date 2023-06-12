using BehaviorDesigner.Runtime.Tasks.Unity.UnityCharacterController;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BaseScripts
{
    public enum BaseMoveMode { GROUND, SWIMMING, NOCLIP, GLIDING, ZIPLINING };

    public enum BaseMoveType { WALKING, JOGGING, SPRINTING };

    public class BaseMovementComponent : Mirror.NetworkBehaviour, IBasePool, ILogicLOD
    {
        public BaseMovementConfig MovementConfig;

        public delegate void BaseMovementComponentSprintingSignature(BaseMovementComponent InMovementComponent, BaseMoveType NewMoveType);
        public BaseMovementComponentSprintingSignature OnSprintDelegate;

        public delegate void BaseMovementComponentFrozenSignature(BaseMovementComponent InMovementComponent, bool bMovementFrozen);
        public BaseMovementComponentFrozenSignature OnMovementFrozenDelegate;

        public delegate void BaseMovementComponentMovementSpeedChangeSignature(BaseMovementComponent InMovementComponent, float NewMovementSpeed);
        public BaseMovementComponentMovementSpeedChangeSignature OnMovementSpeedChangedDelegate;

        public delegate void BaseMovementComponentLandOnGroundSignature(
            BaseMovementComponent InMovementComponent, float gravityVelocity, Vector3 previousGroundLocation, Vector3 newGroundLocation);
        public BaseMovementComponentLandOnGroundSignature OnLandOnGroundDelegate;

        public delegate void BaseMovementComponentGroundChangedSignature(BaseMovementComponent InMovementComponent, Collider OldCollider, Collider NewCollider);
        public BaseMovementComponentGroundChangedSignature OnLastGroundedColliderChanged;

        public delegate void BaseMovementComponentAirSignature(
    BaseMovementComponent InMovementComponent, Vector3 GroundLocation, Collider GroundCollider);
        public BaseMovementComponentAirSignature OnAirDelegate;

        public delegate void BaseMovementComponentJumpSignature(
    BaseMovementComponent InMovementComponent, bool bJumped);
        public BaseMovementComponentJumpSignature OnJumpDelegate;
        public BaseMovementComponentJumpSignature OnCrouchDelegate;

        public delegate void BaseMovementComponentPositionChangedSignature(BaseMovementComponent movementComponent, Vector3 previousPosition, Vector3 newPosition);
        public BaseMovementComponentPositionChangedSignature OnPositionChangedDelegate;

        public delegate void BaseMovementComponentMoveModeSignature(BaseMovementComponent movementComponent, BaseMoveMode oldMoveMode, BaseMoveMode newMoveMode);
        public BaseMovementComponentMoveModeSignature OnMoveModeChangedDelegate;

        public delegate void BaseMovementComponentAcceptInput(BaseMovementComponent movementComponent, bool value);
        public BaseMovementComponentAcceptInput OnAcceptInputChangedDelegate;

        public delegate void BaseMovementComponentZiplineSignature(BaseMovementComponent movementComponent, HRZipline oldZipline, HRZipline newZipline);
        public BaseMovementComponentZiplineSignature ZiplineChangedDelegate;

        public BaseMoveType CurrentMoveSpeed = BaseMoveType.JOGGING;
        public BaseMoveMode CurrentMoveMode = BaseMoveMode.GROUND;

        [Mirror.SyncVar(hook = "MoveSpeedChanged_Hook")]
        public BaseMoveType CurrentMoveSpeed_Networked = BaseMoveType.JOGGING;

        [Mirror.SyncVar(hook = "MoveModeChanged_Hook")]
        public BaseMoveMode CurrentMoveMode_Networked = BaseMoveMode.GROUND;

        [HideInInspector] public Transform MovementTransform;
        public BaseRagdollManager RagdollManager;
        public BaseAIMovement OwningAIMovement;

        public bool bIsPlayer;
        bool bDidJumpInput = false;
        Vector3 JumpInputMovementVector;

        public bool bOnlySprint = false;

        public float MaxWalkSpeed = 1.8f;
        public float MaxJogSpeed = 5.0f;
        public float MaxSprintSpeed = 6.5f;

        public float SwimSpeedReduction = 0.5f;

        float CurrentAcceleration = 0.0f;
        Vector3 CurrentVelocity;

        [Tooltip("Acceleration rate for walking. When no input, slows down at twice this rate.")]
        public float WalkAccel = 1;
        [Tooltip("Acceleration rate for sprinting. When no input, slows down at twice this rate.")]
        public float SprintAccel = 2;
        [Tooltip("Artificial slowdown to movement. Does not apply to movement from input.")]
        public float Friction = 0.5f;
        bool bUsingFriction = true;
        bool bUseInput = true;
        bool bIgnoreAirFrictionModifier = false;
        public bool bCanSwim = true;

        private bool bCrouchingEnabled = true;
        float OriginalCapsuleHeight = 0.0f;
        float CrouchDifference = 0.0f;

        private float minSprintTime = 0.35f;
        float currSprintTime;
        public bool bReachedFullSprint;
        public GameObject fullSprintParticleEffect;

        [HideInInspector]
        public float MovementSpeedModifier = 1f;

        // This is to face the camera instead of velocity
        [SerializeField]
        private bool bShouldFaceCamera = false;

        // This is to require a non zero velocity to rotate the camera
        [SerializeField]
        private bool bFaceCameraRequiresVelocity = true;

        // If the player should face the direction of movement slowly.
        [SerializeField]
        private bool bShouldRotateTowardsMovement = true;
        private bool bOriginalRotateTowardsMovement;

        private bool bShouldRotate = true;

        [SerializeField]
        private bool bShouldRotateTowardsInput;
        private bool bOriginalRotateTowardsInput;

        // Used for targeting guns for example
        bool bShouldOverrideRotateLocation = false;
        Vector3 OverrideRotateLocation;

        // Different rotate states for different stances
        public float SprintingRotateRate = 20.0f;
        public float RunningRotateRate = 800.0f;
        public float WalkingRotateRate = 150.0f;
        public float StandingRotateRate = 800.0f;

        // Used for override in unrelated components such as for aiming
        float OverrideRotateSpeed = 800.0f;
        bool bUseOverrideRotateSpeed = false;

        public float JumpImpulse = 10.0f;

        public float JumpSpeed = 3f;
        public float JumpTime = .5f;
        //keep track how longer player was in air
        float currTimeInAir;

        [HideInInspector]
        public bool bOverrideJumpAttack;

        public bool IsThirdPerson = true;
        bool bFrozen = false;
        public bool bUseGravity = true;
        public float GravityStrength = 9.8f;
        [System.NonSerialized]
        public float GravityModifier = 1.0f;
        Vector3 MovementPlaneNormal = Vector3.up;
        Vector3 GravityDir = Vector3.up * -1;
        //private RaycastHit[] groundHits = new RaycastHit[4];

        // Saved variables
        Vector3 LastFrameLocalPosition;
        Vector3 LastFramePosition;
        Vector3 ForwardMoveVector;
        Vector3 RightMoveVector;
        Quaternion LastFrameLocalRotation;
        Quaternion LastFrameRotation;

        // This is not the same as Last Grounded Location as this is
        // updated during the hook function.
        private Vector3 prevPositionOnGround = Vector3.zero;

        public Vector3 PlayerInputVector; // Axis input. values range from -1 to 1

        [System.NonSerialized]
        public Vector3 PlayerInputMovementVector; // Movement to apply from input.
        Vector3 Velocity;
        [HideInInspector] public Vector3 RawVelocity;
        [HideInInspector] public Vector3 RawXZVelocity;
        Vector3 PrevPosition;
        Vector3 GravityVector;
        [HideInInspector] public Vector3 ExternalInputVector; // Movement to apply from external forces.
        [HideInInspector] public Vector3 AdditionalMovementVector; // Movement to apply from external forces.
        [HideInInspector] public Vector3 UnscaledAdditionalMovementVector; // Movement to apply from external forces.

        Vector3 VerticalInputVector;
        Vector3 HorizontalInputVector;

        // References
        public CharacterController CharacterController;
        public BasePlayerController PlayerController;
        public BaseScripts.BasePlayerCharacter OwningPlayerCharacter;
        private bool hasMotionSmoother;

        public BaseFootstepFX FootstepFX;
        public BaseMovementDash DashComponent;
        public bool bCanDashInSwim;
        [HideInInspector] public bool hasDashComponent;

        Vector3 OriginalScale;
        public GameObject StickingPoint;
        public bool bShouldUseStickingPoint = true;

        public LayerMask StickingPointLayerMask;
        public LayerMask GroundedLayers;
        private Vector3 GroundedCheckOffset;

        [SerializeField]
        private Vector3 LastAIMoveVector = new Vector3(0, 0, 0);

        [Header("Debug Test Variables")]
        public bool bSimulateBackwardInput;

        [Header("Slope Variables")]

        [SerializeField, Min(0)]
        private float maxSlopeSpeedGain = 0.01f;
        [SerializeField, Range(0, 360)]
        private float maxSlopeClimbAngle = 50.0f;
        [SerializeField, Min(0.01f)]
        private float slopeCastDistance = 0.01f;
        [SerializeField, Min(4), Tooltip("Number of Rays to cast around the movement component")]
        private int numberOfSlopeRays = 4;
        [Tooltip("The percentage of the rays that emit from player to determine if they are in a hole.")]
        [SerializeField, Range(0.0f, 100.0f)]
        private float raysHolePercentage = 90.0f;
        [SerializeField]
        private LayerMask slopeLayerMask;

        private Vector3 _conveyorVelocity = Vector3.zero;
        private bool _applyingSlopeForces = false;

        public Vector3 ConveyorVelocity => _conveyorVelocity;

        public BaseRootMotionComponent RootMotionComponent;
        [HideInInspector] public bool hasRootMotionComponent;

        [SerializeField]
        public float RebuildGridInterval = 1f;
        private float CurrentUpdateTime = 0.0f;

        // The last time that the player slid down a slope.
        float LastTimeSlidJump = -100;

        // How long a player must wait after sliding down a slope.
        float JumpCooldownAfterSliding = 0.1f;

        [Header("Randomized Audio")]

        public bool bUseRandomizedAudio;

        public LoopingAudioTrigger LoopingAudioTrigger;

        public List<AudioClip> WalkClips;
        public List<AudioClip> JogClips;
        public List<AudioClip> SprintClips;

        Pathfinding.RichAI PathfindingAI;

        public AudioSource ZipliningAudioSource;

        public BaseMotionSmoother MotionSmoother => motionSmoother;
        [SerializeField]
        private BaseMotionSmoother motionSmoother;

        public void SetPathfindingAI(Pathfinding.RichAI InPathfindingAI)
        {
            PathfindingAI = InPathfindingAI;
            bIsPathfindingAI = InPathfindingAI != null;
        }

        bool bIsPathfindingAI = false;
        [HideInInspector] public bool AISimpleMovement = false;

        bool bStartHasRun = false;

        [Mirror.SyncVar(hook = "HandleAttachedZiplineChanged_Hook")]
        public HRZipline AttachedZipline_Server = null;

        private HRZipline AttachedZipline_Local = null;

        bool bCanSprint = true;

        bool bHasController = false;

        bool bHasNetworkControl = false;
        [System.NonSerialized]
        public bool bHasAIMovement = false;

        bool movedThisFramed = false;

        float StartGlideTime;

        public float GliderHorizontalMovementModifier = 1.75f;
        public AnimationCurve GliderHorizontalMovementCurve;

        public float GliderGravityModifier = 0.1f;
        public AnimationCurve GliderGravityCurve;

        float LowestDiveDepth;

        [System.NonSerialized]
        public bool bIsDiving;

        public GameObject SlideJumpPrefab;

        private void DoAcceleration(Vector3 InDirection, float TargetSpeed, float InAcceleration)
        {
            float CurrentSpeed = Vector3.Dot(CurrentVelocity, InDirection);
            float SpeedDiff = TargetSpeed - CurrentSpeed;
            if (SpeedDiff <= 0)
            {
                return;
            }

            float AccelerationSpeed = Mathf.Min(CurrentAcceleration * Time.deltaTime * TargetSpeed, SpeedDiff);
            CurrentVelocity.x += AccelerationSpeed * InDirection.x;
            CurrentVelocity.z += AccelerationSpeed * InDirection.z;
        }

        public override void Start()
        {
            base.Start();
            bStartHasRun = true;

            if (DashComponent)
            {
                DashComponent.OnDashDelegate += HandleDash;
            }
        }

        void HandleDash(BaseMovementDash InDash, bool bIsDashing, bool bRecoveringFromKnockdown = false)
        {
            if (CurrentMoveMode == BaseMoveMode.ZIPLINING)
            {
                DetachZipline();
            }
        }

        public HRZipline CurrentNetworkedZipline()
        {
            return AttachedZipline_Server;
        }

        Transform ZiplinePointToTravelTo = null;

        public void AttachZipline(HRZipline InZipline)
        {
            if (InZipline && InZipline.ZiplinePointA && InZipline.ZiplinePointB)
            {
                AttachedZipline_Local = InZipline;

                SetAttachedZipline(AttachedZipline_Local);

                if (PlayerController && PlayerController.GetCamera())
                {
                    // Snap to rotate towards the camera direction
                    Transform TargetTransform = InZipline.GetTargetTransform(OwningPlayerCharacter.Position, PlayerController.GetCamera().transform.forward);
                    if (TargetTransform)
                    {
                        if (InZipline.bOnlyGoFromAToB && TargetTransform == InZipline.ZiplinePointA)
                        {
                            return;
                        }

                        RotateTowards(Vector3.ProjectOnPlane(TargetTransform.position - this.transform.position, Vector3.up), true);
                        ZiplinePointToTravelTo = TargetTransform;
                    }

                    //transform.position = GetClosestPointOnFiniteLine(OwningPlayerCharacter.Position, InZipline.ZiplinePointA.transform.position, InZipline.ZiplinePointB.transform.position) + new Vector3(0, -(CharacterController.height + 0.25f), 0);
                    //if(OwningPlayerCharacter)
                    //{
                    //    OwningPlayerCharacter.UpdateTransformCache();
                    //}
                }

                SetNetworkedIsGrounded(false);
                SetMoveMode(BaseScripts.BaseMoveMode.ZIPLINING);
                SetMoveSpeed(BaseMoveType.JOGGING);
            }
        }

        public void DetachZipline()
        {
            if (AttachedZipline_Local)
            {
                AttachedZipline_Local = null;

                SetAttachedZipline(null);

                SetMoveMode(BaseScripts.BaseMoveMode.GROUND);
            }

            ZiplinePointToTravelTo = null;
        }

        void SetAttachedZipline(HRZipline InZipline)
        {
            if (HRNetworkManager.IsHost())
            {
                SetAttachedZipline_Server(InZipline);
            }
            else
            {
                SetAttachedZipline_Command(InZipline);
            }
        }

        [Mirror.Server]
        void SetAttachedZipline_Server(HRZipline InZipline)
        {
            if (netIdentity)
            {
                if (InZipline && InZipline.netIdentity)
                {
                    AttachedZipline_Server = InZipline;
                }
                else
                {
                    if (InZipline != null)
                    {
                        Debug.LogError("Need to save " + InZipline.gameObject.scene.name + " because of Zipline.");
                    }

                    AttachedZipline_Server = null;
                }
            }
        }

        [Mirror.Command(ignoreAuthority = true)]
        void SetAttachedZipline_Command(HRZipline InZipline)
        {
            SetAttachedZipline_Server(InZipline);
        }

        void HandleAttachedZiplineChanged_Hook(HRZipline OldZipline, HRZipline NewZipline)
        {
            ZiplineChangedDelegate?.Invoke(this, OldZipline, NewZipline);
        }

        // Use this for initialization
        public override void Awake()
        {
            base.Awake();

            MovementTransform = transform;

            bHasAIMovement = OwningAIMovement != null;
            hasMotionSmoother = motionSmoother != null;
            hasDashComponent = DashComponent;
            hasRootMotionComponent = RootMotionComponent;

            if (hasMotionSmoother) { motionSmoother.movementComponentControlled = bIsPlayer ? false : true; motionSmoother.bIsPlayer = bIsPlayer; }

            CurrentAcceleration = CurrentMoveSpeed == BaseMoveType.WALKING ? WalkAccel : SprintAccel;
            CharacterController = GetComponent<CharacterController>();
            if (CharacterController == null)
            {
                Debug.Log("ERROR: NO CHARACTERCONTROLLER ON " + this.name);
            }
            else
            {
                bHasController = true;
                LastFrameLocalPosition = MovementTransform.localPosition;
                LastFrameLocalRotation = MovementTransform.localRotation;
                LastFramePosition = MovementTransform.position;
                LastFrameRotation = MovementTransform.rotation;
            }

            OriginalScale = CharacterController.transform.localScale;

            if (bShouldUseStickingPoint)
            {
                StickingPoint = new GameObject(this.gameObject.name + " Stick Point");
            }

            bOriginalRotateTowardsMovement = bShouldRotateTowardsMovement;
            bOriginalRotateTowardsInput = bShouldRotateTowardsInput;

            GroundedCheckOffset = new Vector3(0, (CharacterController.radius), 0);
        }
        public void SetShouldFaceCamera(bool bInShouldFaceCamera)
        {
            bShouldFaceCamera = bInShouldFaceCamera;
        }
        public bool GetShouldFaceCamera()
        {
            return bShouldFaceCamera;
        }
        public void SetFaceCameraNeedsVelocity(bool bNeedsVelocity)
        {
            bFaceCameraRequiresVelocity = bNeedsVelocity;
        }

        public bool IsInDeepWater()
        {
            return bInDeepWater;
        }

        List<BaseWater> CurrentDeepWaters = new List<BaseWater>();
        bool bInDeepWater = false;

        public void HandleDeepWaterEntered(BaseWater InWater, bool bEntered, bool bForceRemove = true)
        {
            if (!bCanSwim && bEntered)
            {
                return;
            }

            bInDeepWater = bEntered;
            if (bEntered)
            {
                if (!CurrentDeepWaters.Contains(InWater))
                {
                    CurrentDeepWaters.Add(InWater);
                }
                SetMoveMode(BaseMoveMode.SWIMMING);

                if (bHasAIMovement && !OwningAIMovement.bHasRichAI)
                {
                    if (InWater.DeepWaterArea)
                    {
                        OwningAIMovement.SetPathfindingArea(InWater.DeepWaterArea);
                        bIsPathfindingAI = true;
                    }
                }
            }
            else
            {
                var Hero = this.GetComponent<HeroPlayerCharacter>();

                if (Hero && bForceRemove)
                {
                    //InWater.RemoveCharacter(Hero);
                }

                CurrentDeepWaters.Remove(InWater);
                if (CurrentDeepWaters.Count == 0)
                {
                    // Left all waters. Change back to rgounded mode
                    SetMoveMode(BaseMoveMode.GROUND);
                }

                if (bHasAIMovement && !OwningAIMovement.bHasRichAI)
                {
                    if (!InWater.DeepWaterArea || !InWater.DeepWaterArea.CurrentShape.IsWithinShape(transform.position))
                    {
                        OwningAIMovement.SetPathfindingArea(null);
                        bIsPathfindingAI = false;
                    }
                }
            }
        }

        public void RemoveCharacterFromDeepWater()
        {
            var Hero = this.GetComponent<HeroPlayerCharacter>();

            for (int i = CurrentDeepWaters.Count - 1; i >= 0; i--)
            {
                var water = CurrentDeepWaters[i];

                if (Hero)
                {
                    //water.RemoveCharacter(Hero);
                }
            }

            CurrentDeepWaters.Clear();
            SetMoveMode(BaseMoveMode.GROUND);
            SetForce(new Vector3(0, 0, 0));
        }

        public void SetRootMotionComponent(BaseRootMotionComponent InRootMotionComponent)
        {
            RootMotionComponent = InRootMotionComponent;
        }

        public void SetCanSwim(bool value)
        {
            if (HRNetworkManager.IsHost())
            {
                SetCanSwim_Implementation(value);
                SetCanSwim_ClientRpc(value);
            }
            else
            {
                if (hasAuthority)
                {
                    SetCanSwim_Implementation(value);
                    SetCanSwim_Command(value);
                }
            }
        }


        public void SetCanSwimAfterDelay(bool value, float duration = 0.15f)
        {
            StartCoroutine(SetSwimAfterDelay(value, duration));
        }

        private IEnumerator SetSwimAfterDelay(bool value, float delay)
        {
            yield return new WaitForSeconds(delay);

            SetCanSwim(value);
        }


        [Mirror.Command]
        public void SetCanSwim_Command(bool value)
        {
            SetCanSwim_Implementation(value);
            SetCanSwim_ClientRpc(value);
        }

        [Mirror.ClientRpc]
        public void SetCanSwim_ClientRpc(bool value)
        {
            SetCanSwim_Implementation(value);
        }


        public void SetCanSwim_Implementation(bool value)
        {
            if (bCanSwim == value) return;

            bCanSwim = value;

            if (bCanSwim)
            {
                BaseWater.CheckInAnyWater(gameObject, true);
            }
            else
            {
                RemoveCharacterFromDeepWater();
            }
        }

        public bool IsCrouching()
        {
            if (HRNetworkManager.HasControl(netIdentity))
            {
                return bLocalCrouching;
            }
            else
            {
                return bNetworkedCrouching;
            }
        }

        public void ToggleCrouch(bool bNeedsControl = true)
        {
            if (CanCrouch(!IsCrouching()))
            {
                Crouch(!IsCrouching(), bNeedsControl);
            }
        }

        public bool CanCrouch(bool bCrouch)
        {
            if (bCrouch)
            {
                if (!GetIsSwimming() && GetIsGrounded())
                {
                    return true;
                }
            }
            else
            {
                // todo use a raycast or something to see if you can uncrouch
                return true;
            }

            return false;
        }

        public BaseMoveType GetMoveType(bool bHasControl)
        {
            if (bHasControl)
            {
                return CurrentMoveSpeed;
            }
            else
            {
                return CurrentMoveSpeed_Networked;
            }
        }
        public void Crouch(bool bCrouch, bool bForce = false, bool bNeedsControl = true)
        {
            if (!bCrouchingEnabled)
            {
                return;
            }

            if (IsCrouching() == bCrouch)
            {
                return;
            }

            bool bHasControl = HRNetworkManager.HasControl(netIdentity);
            if (!bNeedsControl || bHasControl)
            {
                if (!bForce || CanCrouch(bCrouch))
                {
                    if (bCrouch)
                    {
                        SetMoveSpeed(BaseMoveType.WALKING);
                    }
                    else
                    {
                        if (GetMoveType(bHasControl) == BaseMoveType.WALKING)
                        {
                            SetMoveSpeed(BaseMoveType.JOGGING);
                        }
                    }

                    Crouch_Implementation(bCrouch);

                    if (HRNetworkManager.IsHost())
                    {
                        Crouch_Server(bCrouch);
                    }
                    else
                    {
                        Crouch_Command(bCrouch);
                    }
                }
            }
        }

        void SetOriginalCapsuleHeight()
        {
            if (OriginalCapsuleHeight == 0.0f)
            {
                OriginalCapsuleHeight = CharacterController.height;
                CrouchDifference = CharacterController.height / 3;
            }
        }

        [Mirror.Command]
        void Crouch_Command(bool bInCrouching)
        {
            Crouch_Server(bInCrouching);
        }

        [Mirror.Server]
        void Crouch_Server(bool bInCrouching)
        {
            bNetworkedCrouching = bInCrouching;
        }

        void Crouch_Implementation(bool bInCrouching)
        {
            // Delegate for crouching
            SetOriginalCapsuleHeight();

            CharacterController.height = bInCrouching ? OriginalCapsuleHeight - CrouchDifference : OriginalCapsuleHeight;
            if (OwningPlayerCharacter && OwningPlayerCharacter.PlayerMesh)
            {
                // Move the mesh down or up to compensate
                OwningPlayerCharacter.PlayerMesh.transform.localPosition = new Vector3(OwningPlayerCharacter.PlayerMesh.transform.localPosition.x, bInCrouching ? OwningPlayerCharacter.PlayerMesh.OriginalMeshHeight - CrouchDifference : OwningPlayerCharacter.PlayerMesh.OriginalMeshHeight, OwningPlayerCharacter.PlayerMesh.transform.localPosition.z);
            }

            bLocalCrouching = bInCrouching;
            OnCrouchDelegate?.Invoke(this, bInCrouching);
        }

        private void HandleCrouchChanged_Hook(bool bOldCrouched, bool bNewCrouched)
        {
            if (!HRNetworkManager.HasControl(netIdentity))
            {
                Crouch_Implementation(bNewCrouched);
            }
        }

        void SpawnSlideJumpFX(Vector3 InPosition)
        {
            if (HRNetworkManager.IsHost())
            {
                SpawnSlideJumpFX_Server(InPosition);
            }
            else
            {
                if (netIdentity)
                {
                    SpawnSlideJumpFX_Command(InPosition);
                }
            }
        }

        [Mirror.Command(ignoreAuthority = true)]
        void SpawnSlideJumpFX_Command(Vector3 InPosition)
        {
            SpawnSlideJumpFX_ClientRpc(InPosition);
        }

        [Mirror.ClientRpc]
        void SpawnSlideJumpFX_ClientRpc(Vector3 InPosition)
        {
            SpawnSlideJumpFX_Implementation(InPosition);
        }

        [Mirror.Server]
        void SpawnSlideJumpFX_Server(Vector3 InPosition)
        {
            SpawnSlideJumpFX_ClientRpc(InPosition);
        }

        void SpawnSlideJumpFX_Implementation(Vector3 InPosition)
        {
            if (SlideJumpPrefab)
            {
                BaseObjectPoolManager.Get.InstantiateFromPool(SlideJumpPrefab, false, true, InPosition, false);
            }
        }

        bool RecentlyJumpSlid()
        {
            return Time.timeSinceLevelLoad - LastTimeSlidJump < JumpCooldownAfterSliding;
        }

        float JumpForwardFactor = 0.05f;
        float SlideJumpFactor = 0.1f;

        public bool Jump(bool bForce = false, float JumpMultiplier = 1, float ForwardForce = 0, bool bSuspendInput = false, bool bSuspend = true, bool ignoreGrounded = false, bool bIgnoreJumpButtonEval = false, float OverrideJumpTime = -1f, bool ignoreFallDamage = false)
        {
            if ((!bAcceptPlayerInput || bLockPlayerInput) && !bForce)
            {
                // Maybe need to filter whether it was a player requested jump or not.
                return false;
            }

            bool bSlidingJump = IsSliding();
            Vector3 SlideJumpNormal = bSlidingJump ? (LastGroundingHit.normal + (PlayerInputMovementVector.normalized / 3)).normalized : Vector3.up;

            if (bSlidingJump && RecentlyJumpSlid())
            {
                return false;
            }

            if (bSlidingJump || ignoreGrounded || GetIsGrounded() || CurrentMoveMode == BaseMoveMode.SWIMMING || CurrentMoveMode == BaseMoveMode.ZIPLINING)
            {
                if (bIsDiving)
                {
                    return false;
                }

                //if (CurrentDeepWaters.Count > 0)
                //{
                //    CurrentDeepWaters[0].RemoveCharacter(OwningPlayerCharacter);
                //}

                float t = 0;

                DetachZipline();
                Crouch(false);

                if (DashComponent && DashComponent.bIsDashing)
                {
                    JumpMultiplier *= 1.2f;
                }

                if (bSlidingJump)
                {
                    SetNetworkedIsSliding(HRNetworkManager.HasControl(this.netIdentity), false);
                    LastTimeSlidJump = Time.timeSinceLevelLoad;
                    JumpMultiplier *= 1.5f;
                    //ClearVerticalVelocity();

                    SpawnSlideJumpFX(this.transform.position);
                }

                float Speed = RawVelocity.magnitude;
                AddForce((transform.forward * ((ForwardForce + Speed) * JumpForwardFactor)) + new Vector3(0, JumpImpulse * JumpMultiplier, 0) + (bSlidingJump ? ((SlideJumpNormal * JumpImpulse) * SlideJumpFactor) : Vector3.zero));

                bDidJumpInput = true;
                JumpInputMovementVector = PlayerInputMovementVector;

                OnJumpDelegate?.Invoke(this, true);

                if (ignoreFallDamage)
                {
                    HeroPlayerCharacter hpc = OwningPlayerCharacter as HeroPlayerCharacter;
                    hpc.bIgnoreFallDamageOnce = true;
                }

                return true;
            }

            return false;
        }

        //Jump attack is valid if spacebar was inputed or if the character was in the air for 0.2 seconds long
        public bool CanDoJumpAttack()
        {
            if (CompareTag("Player"))
            {
                if (bDidJumpInput || currTimeInAir > 0.45f || bOverrideJumpAttack)
                {
                    return true;
                }
            }
            //be much harsher on AI onAir buffer
            else
            {
                if (bDidJumpInput || currTimeInAir > 0.55f || bOverrideJumpAttack)
                {
                    return true;
                }
            }


            return false;
        }

        public bool IsSprinting()
        {
            return CurrentMoveSpeed == BaseMoveType.SPRINTING;
        }

        void MoveSpeedChanged_Hook(BaseMoveType OldMoveType, BaseMoveType NewMoveType)
        {
            if (!HRNetworkManager.HasControl(netIdentity))
            {
                CurrentMoveSpeed = NewMoveType;
                OnSprintDelegate?.Invoke(this, CurrentMoveSpeed);
            }
        }

        void MoveModeChanged_Hook(BaseMoveMode OldMoveMode, BaseMoveMode NewMoveMode)
        {
            if (!HRNetworkManager.HasControl(netIdentity))
            {
                CurrentMoveMode = NewMoveMode;
                OnMoveModeChangedDelegate?.Invoke(this, OldMoveMode, NewMoveMode);
            }
        }

        public void ApplyConveyorVelocity(Vector3 velocity)
        {
            _conveyorVelocity = velocity;
        }

        public void SetMoveMode(BaseMoveMode InMoveMode)
        {
            if (InMoveMode != CurrentMoveMode)
            {
                BaseMoveMode OldMoveMode = CurrentMoveMode;
                CurrentMoveMode = InMoveMode;

                if (OldMoveMode == BaseMoveMode.GLIDING)
                {
                    prevPositionOnGround = OwningPlayerCharacter.Position;
                    HeroPlayerCharacter hpc = OwningPlayerCharacter as HeroPlayerCharacter;
                    if (hpc && hpc.AnimScript && hpc.AnimScript.IKManager)
                    {
                        hpc.AnimScript.IKManager.SetBipedIKEnabled(true);
                    }
                }
                else if (OldMoveMode == BaseMoveMode.SWIMMING)
                {
                    HeroPlayerCharacter hpc = OwningPlayerCharacter as HeroPlayerCharacter;
                    if (hpc && hpc.AnimScript && hpc.AnimScript.IKManager)
                    {
                        hpc.AnimScript.IKManager.SetBipedIKEnabled(true);
                    }

                    ToggleDiving(false);
                }
                else if (OldMoveMode == BaseMoveMode.GROUND)
                {
                    SetNetworkedIsSliding(HRNetworkManager.HasControl(this.netIdentity), false);
                }

                OnMoveModeChangedDelegate?.Invoke(this, OldMoveMode, InMoveMode);
            }

            if (InMoveMode != BaseMoveMode.GLIDING)
            {
                ResetGravityModifier();
            }

            if (InMoveMode != BaseMoveMode.ZIPLINING)
            {
                if (ZipliningAudioSource)
                {
                    ZipliningAudioSource.enabled = false;
                }
            }

            if (InMoveMode == BaseMoveMode.SWIMMING)
            {
                // prevent fall damage calculation
                prevPositionOnGround = OwningPlayerCharacter.Position;

                if (!bCanDashInSwim && DashComponent && DashComponent.bIsDashing)
                {
                    DashComponent.StopDashing();
                    DashComponent.SetDashEnabled(false);
                }

                Crouch(false);

                HeroPlayerCharacter hpc = OwningPlayerCharacter as HeroPlayerCharacter;

                if (hpc && hpc.AnimScript && hpc.AnimScript.IKManager)
                {
                    hpc.AnimScript.IKManager.SetBipedIKEnabled(false);
                }


            }
            else if (InMoveMode == BaseMoveMode.GLIDING)
            {
                if (DashComponent && DashComponent.bIsDashing)
                {
                    DashComponent.StopDashing();
                }
                SetMoveSpeed(BaseMoveType.JOGGING);
                // Set last 
                StartGlideTime = Time.time;

                prevPositionOnGround = OwningPlayerCharacter.Position;

                SetVelocity(new Vector3(Velocity.x, 0, Velocity.z));
                // Gravity fall speed lowered
                /*
                if (GliderGravityCurve != null && GliderGravityCurve.length > 0)
                {
                    HeroPlayerCharacter hrpc = OwningPlayerCharacter as HeroPlayerCharacter;
                    float staminaGlider = hrpc.StaminaComponent.CurrentHP / hrpc.StaminaComponent.MaxHP;
                    SetGravityModifier(GliderGravityCurve.Evaluate(staminaGlider));
                    Debug.Log("Curve: " + GliderGravityCurve.Evaluate(staminaGlider));
                    //SetGravityModifier(GliderGravityCurve.Evaluate(Mathf.Clamp((Time.timeSinceLevelLoad - StartGlideTime)/5,0,1)));
                }
                else
                {
                    SetGravityModifier(GliderGravityModifier);
                }
                */
                SetGravityModifier(GliderGravityModifier);
                Crouch(false);

                if (HRNetworkManager.Get
               && HRNetworkManager.bIsServer)
                {
                    SetIsGrounded_Server(false);
                }
                else
                {
                    SetIsGrounded_Command(false);
                }

                HeroPlayerCharacter hpc = OwningPlayerCharacter as HeroPlayerCharacter;
                if (hpc && hpc.AnimScript && hpc.AnimScript.IKManager)
                {
                    hpc.AnimScript.IKManager.SetBipedIKEnabled(false);
                }
            }
            else if (InMoveMode == BaseMoveMode.ZIPLINING)
            {
                if (ZipliningAudioSource)
                {
                    ZipliningAudioSource.enabled = true;
                }

                SetMoveSpeed(BaseMoveType.JOGGING);

                prevPositionOnGround = OwningPlayerCharacter.Position;
            }
            else
            {
                if (DashComponent)
                    DashComponent.SetDashEnabled(true);
            }

            if (HRNetworkManager.HasControl(netIdentity))
            {
                if (HRNetworkManager.IsHost())
                {
                    CurrentMoveMode_Networked = InMoveMode;
                }
                else
                {
                    SetMoveMode_Command(InMoveMode);
                }
            }
        }

        public void SetVelocity(Vector3 InVelocity)
        {
            // This is not correct
            Velocity = InVelocity;
            RawVelocity = InVelocity;
        }

        public void SetCanSprint(bool bInCanSprint)
        {
            bCanSprint = bInCanSprint;
        }

        public bool CanSprint()
        {
            return bCanSprint;
        }

        [Mirror.Command]
        private void SetMoveMode_Command(BaseMoveMode InMoveMode)
        {
            CurrentMoveMode_Networked = InMoveMode;
        }

        bool bMoveSpeedCanChange = true;

        public void SetMoveSpeedCanChange(bool bCanChange)
        {
            bMoveSpeedCanChange = bCanChange;
        }

        public void SetMoveSpeed(BaseMoveType InMoveType, bool bIgnoreAuthority = false)
        {
            if (!bMoveSpeedCanChange)
            {
                return;
            }

            // For airstrafer customers
            if (bOnlySprint)
            {
                InMoveType = BaseMoveType.JOGGING;
            }

            if (InMoveType == BaseMoveType.SPRINTING && !CanSprint())
            {
                return;
            }

            // This is so the player must commit to a move speed before jumping
            if ((!GetIsGrounded() && CurrentMoveMode != BaseMoveMode.ZIPLINING) && !(GetIsSwimming()) && (InMoveType != BaseMoveType.WALKING && CurrentMoveSpeed != BaseMoveType.WALKING))
            {
                return;
            }

            if (CurrentMoveSpeed == InMoveType)
            {
                return;
            }
            CurrentMoveSpeed = InMoveType;

            if (bIgnoreAuthority || HRNetworkManager.HasControl(netIdentity))
            {
                if (HRNetworkManager.IsHost())
                {
                    CurrentMoveSpeed_Networked = InMoveType;
                }
                else
                {
                    SetMoveSpeed_Command(InMoveType);
                }
            }

            if (InMoveType != BaseMoveType.WALKING)
            {
                Crouch(false);
            }

            OnSprintDelegate?.Invoke(this, InMoveType);
        }

        [Mirror.Command]
        private void SetMoveSpeed_Command(BaseMoveType InMoveType)
        {
            CurrentMoveSpeed_Networked = InMoveType;
        }

        public void SetOverrideRotateSpeed(bool bInUseOverrideRotateSpeed)
        {
            bUseOverrideRotateSpeed = bInUseOverrideRotateSpeed;
        }

        public void SetOverrideRotateSpeed(bool bUseOverrideRotateSpeed, float InRotateSpeed = 400.0f)
        {
            OverrideRotateSpeed = InRotateSpeed;
            SetOverrideRotateSpeed(bUseOverrideRotateSpeed);
        }

        public float GetRotateSpeed()
        {
            if (bUseOverrideRotateSpeed)
            {
                return OverrideRotateSpeed;
            }

            if (RawVelocity.sqrMagnitude > 0.1f)
            {
                if (CurrentMoveSpeed == BaseMoveType.SPRINTING)
                {
                    return SprintingRotateRate;
                }
                else
                {
                    return CurrentMoveSpeed == BaseMoveType.WALKING ? WalkingRotateRate : RunningRotateRate;
                }
            }
            else
            {
                return StandingRotateRate;
            }
        }

        public Vector3 GetLastAIMoveVector()
        {
            return LastAIMoveVector;
        }

        // Returns the desired player input movement axis unit
        public Vector3 GetPlayerInputVector()
        {
            if (PlayerInputVector == ZeroVector)
            {
                return ZeroVector;
            }
            else
            {
                return PlayerInputVector.normalized;
            }
        }

        public Vector3 GetForwardVector()
        {
            return ForwardMoveVector;
        }

        public Vector3 GetRightVector()
        {
            return RightMoveVector;
        }

        public void ClearVelocity()
        {
            RawVelocity = ZeroVector;
            Velocity = ZeroVector;
            ExternalInputVector = ZeroVector;
            PlayerInputVector = ZeroVector;
            PlayerInputMovementVector = ZeroVector;
            ForwardMoveVector = ZeroVector;
            RightMoveVector = ZeroVector;
            HorizontalInputVector = ZeroVector;
            VerticalInputVector = ZeroVector;
            RightAxis = 0f;
            ForwardAxis = 0f;
            GravityVector = ZeroVector;

            PrevPosition = OwningPlayerCharacter.Position;
            LastFrameLocalPosition = CharacterController.transform.localPosition;

            if (bUseInput)
            {
                UpdateInputVector();
            }
        }

        public void ClearVerticalVelocity()
        {
            RawVelocity = new Vector3(RawVelocity.x, 0, RawVelocity.z);
            Velocity = new Vector3(Velocity.x, 0, Velocity.z);
            ExternalInputVector = new Vector3(ExternalInputVector.x, 0, ExternalInputVector.z);
            PlayerInputMovementVector = new Vector3(PlayerInputMovementVector.x, 0, PlayerInputMovementVector.z);
            ForwardMoveVector = new Vector3(ForwardMoveVector.x, 0, ForwardMoveVector.z);
            RightMoveVector = new Vector3(RightMoveVector.x, 0, RightMoveVector.z);
            HorizontalInputVector = new Vector3(HorizontalInputVector.x, 0, HorizontalInputVector.z);
            VerticalInputVector = new Vector3(VerticalInputVector.x, 0, VerticalInputVector.z);
            GravityVector = new Vector3(GravityVector.x, 0, GravityVector.z);

            PrevPosition = OwningPlayerCharacter.Position;
            LastFrameLocalPosition = CharacterController.transform.localPosition;

            if (bUseInput)
            {
                UpdateInputVector();
            }
        }

        public Vector3 GetVelocity()
        {
            if (this.isActiveAndEnabled)
            {
                return Velocity;
            }
            else
            {
                return ZeroVector;
            }

        }

        public Vector3 GetPlayerInputVelocity()
        {
            return PlayerInputMovementVector;
        }


        public void SetShouldRotate(bool bInShouldRotate)
        {
            bShouldRotate = bInShouldRotate;
        }

        // if this is locked then dont change player input value
        bool bLockPlayerInput = false;
        // TODO: create a list of player input blockers
        public void SetUsePlayerInput(bool bUsePlayerInput)
        {
            if (!bLockPlayerInput)
                bAcceptPlayerInput = bUsePlayerInput;

            OnAcceptInputChangedDelegate?.Invoke(this, bUsePlayerInput);
        }
        public void LockUsePlayerInput(bool bLockUsePlayerInput)
        {
            bLockPlayerInput = bLockUsePlayerInput;
        }

        public void SetupMovementComponent(BasePlayerController InPlayerController)
        {
            PlayerController = InPlayerController;
        }

        public void ResetLatestOnGroundPosition(Vector3 onGroundPosition)
        {
            prevPositionOnGround = onGroundPosition;
        }

        bool bAcceptPlayerInput = true;

        float ForwardAxis = 0.0f;
        float RightAxis = 0.0f;

        // Called before update if set up correctly in script execution order.
        public void AddForwardMovement(float AxisAmount)
        {
            if (AxisAmount != 0f)
            {
                ForwardMoveVector = Vector3.Dot(PlayerController.GetCamera().transform.forward, Vector3.up) > 0 ?
                -PlayerController.GetCamera().transform.up : PlayerController.GetCamera().transform.forward;

                // Planar projection on a flat plane
                ForwardMoveVector = Vector3.ProjectOnPlane(ForwardMoveVector, Vector3.up);
                ForwardMoveVector.Normalize();

                VerticalInputVector = ForwardMoveVector * AxisAmount;
                ForwardAxis = AxisAmount;
            }
            else
            {
                VerticalInputVector = ZeroVector;
                ForwardMoveVector = ZeroVector;
                ForwardAxis = 0.0f;
            }
            UpdateInputVector();
        }

        // Called before update if set up correctly in script execution order.
        public void AddRightMovement(float AxisAmount)
        {
            if (AxisAmount != 0f)
            {
                RightMoveVector = PlayerController.GetCamera().transform.right;
                HorizontalInputVector = RightMoveVector * AxisAmount;
                RightAxis = AxisAmount;
            }
            else
            {
                HorizontalInputVector = ZeroVector;
                RightMoveVector = ZeroVector;
                RightAxis = 0.0f;
            }
            UpdateInputVector();
        }

        public void UpdateInputVector()
        {
            Vector3 WorldDirection = VerticalInputVector + HorizontalInputVector;
            Vector3 ProjectedVector = Vector3.ProjectOnPlane(WorldDirection, MovementPlaneNormal);
            PlayerInputVector = ProjectedVector.normalized;
        }

        public void Move(Vector3 InDirection, Transform targetTransform, bool hasControl = true)
        {
            if (bIsPlayer)
            {
                if (InDirection != ZeroVector && CurrentMoveMode != BaseMoveMode.NOCLIP)
                {
                    CollisionFlags Flags = CharacterController.Move(InDirection);
                    if (Flags == CollisionFlags.Below)
                    {
                        GravityAccel = 1;
                    }
                }
            }
            else
            {
                if (bHasAIMovement && OwningAIMovement.bHasRichAI && !GetIsGrounded(hasControl))
                {
                    if (ExternalInputVector == Vector3.zero && !RootMotionComponent.GetUsingRootMotion()) // maybe cache?
                    {
                        Vector3 projected = targetTransform.position + InDirection;
                        Vector3 targetClamp = OwningAIMovement._OwningRichAI.ClampToNavmesh(projected, out bool positionChanged);
                        targetTransform.position = positionChanged ? Vector3.Lerp(projected, targetClamp, .8f) : projected;
                    }
                    else
                    {
                        // If there is an external input, we need to cast the character movement otherwise AI can go through walls.
                        Vector3 projectedDirection = InDirection;
                        Vector3 targetClampDirection = OwningAIMovement._OwningRichAI.ClampToNavmesh(projectedDirection, out bool positionChanged);
                        CharacterController.Move(positionChanged ? Vector3.Lerp(projectedDirection, targetClampDirection, .8f) : projectedDirection);
                    }
                }
                else
                {
                    if (ExternalInputVector == Vector3.zero && !RootMotionComponent.GetUsingRootMotion()) // maybe cache?
                    {
                        targetTransform.position += InDirection;
                    }
                    else
                    {
                        // If there is an external input, we need to cast the character movement otherwise AI can go through walls.
                        CharacterController.Move(InDirection);
                    }
                }
            }
        }

        public void FreezeMovement(bool bFrozen, bool bUseGravity = false)
        {
            this.bFrozen = bFrozen;

            if (!bUseGravity)
            {
                this.enabled = !bFrozen;
            }

            OnMovementFrozenDelegate?.Invoke(this, bFrozen);
        }

        public void ToggleGlider()
        {
            if (!GetIsGrounded() && !IsSliding())
            {
                if (CurrentMoveMode == BaseMoveMode.GROUND)
                {
                    SetMoveMode(BaseMoveMode.GLIDING);
                }
                else if (CurrentMoveMode == BaseMoveMode.GLIDING)
                {
                    SetMoveMode(BaseMoveMode.GROUND);
                }
            }
        }

        /// <summary>
        /// Applies a force to this character.
        /// </summary>
        /// <param name="InForce">Force vector to apply</param>
        public void AddForce(Vector3 InForce)
        {
            ExternalInputVector += InForce;
        }

        public void SetForce(Vector3 InForce)
        {
            ExternalInputVector = InForce;
        }

        public Vector3 GetForce()
        {
            return ExternalInputVector;
        }

        public void SetFriction(bool bEnabled)
        {
            bUsingFriction = bEnabled;
        }

        float FrictionModifier = 1.0f;

        public void SetFrictionModifier(float InModifier)
        {
            FrictionModifier = InModifier;
        }

        public void ResetFrictionModifier()
        {
            FrictionModifier = 1.0f;
        }

        public void SetUseAirFriction(bool bEnabled)
        {
            bIgnoreAirFrictionModifier = bEnabled;
        }

        public void SetUseGravity(bool bEnabled)
        {
            if (bUseGravity != bEnabled)
                bUseGravity = bEnabled;
        }

        public void SetUseInput(bool bEnabled)
        {
            bUseInput = bEnabled;
        }

        public void SetOverrideRotateTarget(bool bInShouldOverrideRotateTarget)
        {
            bShouldOverrideRotateLocation = bInShouldOverrideRotateTarget;
        }

        public void SetRotateTowardsMovement(bool bRotate)
        {
            if (bRotate)
            {
                bShouldRotateTowardsMovement = bOriginalRotateTowardsMovement;
                bShouldRotateTowardsInput = bOriginalRotateTowardsInput;
            }
            else
            {
                bShouldRotateTowardsMovement = false;
                bShouldRotateTowardsInput = false;
            }
        }

        public void SetOverrideRotateTarget(bool bInShouldOverrideRotateTarget, Vector3 InLocation)
        {
            SetOverrideRotateTarget(bInShouldOverrideRotateTarget);
            UpdateOverrideRotateTarget(InLocation);
        }

        public void UpdateOverrideRotateTarget(Vector3 InLocation)
        {
            OverrideRotateLocation = InLocation;
        }

        public void SetGravityModifier(float InModifier)
        {
            GravityModifier = InModifier;
        }

        public void ResetGravityModifier()
        {
            GravityModifier = 1.0f;
        }

        public void ResetGravityAcceleration()
        {
            GravityAccel = 0f;
        }

        [Mirror.SyncVar(hook = nameof(HandleGroundChanged_Hook))]
        private bool bNetworkedGrounded = true;
        private bool bLocalGrounded = true;

        [Mirror.SyncVar(hook = nameof(HandleSlidingChanged_Hook))]
        private bool bNetworkedSliding = false;
        private bool bLocalSliding = false;

        [Mirror.SyncVar(hook = nameof(HandleCrouchChanged_Hook))]
        private bool bNetworkedCrouching = false;
        private bool bLocalCrouching = false;

        public ParticleSystem SlidingEffectPrefab;
        public ParticleSystem SlidingEffectInstance;
        public AudioSource SlidingEffectAudioSource;

        public bool GetIsGrounded(bool hasControl = false)
        {
            if (hasControl || HRNetworkManager.HasControl(this.netIdentity))
            {
                return bLocalGrounded;
            }

            return bNetworkedGrounded;
        }

        float GravityAccel = 0.0f;

        /// <summary>
        /// Sets the movement component as grounded.
        /// </summary>
        /// <param name="bIsGrounded">Set is grounded.</param>
        public void SetNetworkedIsGrounded(bool bIsGrounded)
        {
            bool bOldGrounded = bLocalGrounded;
            bLocalGrounded = bIsGrounded;

            if (HRNetworkManager.HasControl(netIdentity))
            {
                if (bIsGrounded)
                {
                    if (CurrentMoveMode == BaseMoveMode.GLIDING)
                    {
                        SetMoveMode(BaseMoveMode.GROUND);
                    }

                }

                LandedOnGround(bOldGrounded, bIsGrounded);
            }

            if (HRNetworkManager.Get
                && HRNetworkManager.bIsServer)
            {
                SetIsGrounded_Server(bIsGrounded);
            }
            else
            {
                SetIsGrounded_Command(bIsGrounded);
            }
        }

        [Mirror.Command(ignoreAuthority = true)]
        private void SetIsGrounded_Command(bool bIsGrounded)
        {
            SetIsGrounded_Server(bIsGrounded);
        }
        [Mirror.Server]
        private void SetIsGrounded_Server(bool bIsGrounded)
        {
            bNetworkedGrounded = bIsGrounded;
        }

        /// <summary>
        /// Called whenveer the on ground variable is changed.
        /// </summary>
        /// <param name="oldGround">The old on ground value.</param>
        /// <param name="newGround">The new on ground value.</param>
        private void HandleGroundChanged_Hook(bool oldGround, bool newGround)
        {
            if (HRNetworkManager.HasControl(this.netIdentity))
            {
                return;
            }

            LandedOnGround(oldGround, newGround);
        }

        void LandedOnGround(bool oldGround, bool newGround)
        {
            if (newGround && !oldGround)
            {
                OnLandOnGroundDelegate?.Invoke(this, Mathf.Abs(GravityVector.y), prevPositionOnGround,
                    this.transform.position);

                bDidJumpInput = false;
                JumpInputMovementVector = Vector3.zero;
            }
            else if (oldGround && !newGround)
            {
                OnAirDelegate?.Invoke(this, prevPositionOnGround, LastGroundedCollider);
                prevPositionOnGround = this.transform.position;
            }
        }

        public bool IsSliding()
        {
            if (HRNetworkManager.HasControl(this.netIdentity))
            {
                return bLocalSliding;
            }
            else
            {
                return bNetworkedSliding;
            }
        }

        public void SetNetworkedIsSliding(bool bHasControl, bool bIsSliding)
        {
            if (!bHasControl || bIsSliding == bLocalSliding) return;

            if (DashComponent)
            {
                if (bIsSliding)
                {
                    if (bIsSliding)
                    {
                        if (!bLocalSliding)
                        {
                            SetFrictionModifier(DashComponent.FrictionModifier);
                        }
                    }
                    else
                    {
                        ResetFrictionModifier();
                    }
                }

                bLocalSliding = bIsSliding;

                if (HRNetworkManager.IsHost())
                {
                    bNetworkedSliding = bIsSliding;
                    SetFrictionModifier(DashComponent.FrictionModifier);
                    if (bIsSliding)
                    {
                        if (!bLocalSliding)
                        {
                            SetFrictionModifier(DashComponent.FrictionModifier);
                        }
                    }
                    else
                    {
                        if (bLocalSliding)
                        {
                            if (!DashComponent.bIsDashing)
                            {
                                ResetFrictionModifier();
                            }
                        }
                    }
                }

                bLocalSliding = bIsSliding;

                if (HRNetworkManager.IsHost())
                {
                    bNetworkedSliding = bIsSliding;
                }
                else
                {
                    ResetFrictionModifier();
                }
            }

            bLocalSliding = bIsSliding;

            if (HRNetworkManager.IsHost())
            {
                bNetworkedSliding = bIsSliding;
            }
            else
            {
                SetIsSliding_Command(bIsSliding);
            }
        }

        [Mirror.Command(ignoreAuthority = true)]
        private void SetIsSliding_Command(bool bIsSliding)
        {
            bNetworkedSliding = bIsSliding;
        }

        private void HandleSlidingChanged_Hook(bool oldSliding, bool newSliding)
        {
            if (SlidingEffectPrefab)
            {
                if (newSliding)
                {
                    if (!SlidingEffectInstance)
                    {
                        SlidingEffectInstance = Instantiate(SlidingEffectPrefab);
                        SlidingEffectInstance.transform.SetParent(OwningPlayerCharacter != null && OwningPlayerCharacter.PlayerMesh != null ? OwningPlayerCharacter.PlayerMesh.transform : this.transform);
                        SlidingEffectInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                        SlidingEffectAudioSource = SlidingEffectInstance.GetComponent<AudioSource>();
                    }

                    SlidingEffectInstance.Play();
                    SlidingEffectAudioSource.Play();
                }
                else
                {
                    if (SlidingEffectInstance)
                    {
                        SlidingEffectInstance.Stop();
                        SlidingEffectAudioSource.Stop();
                    }
                }
            }
        }

        public bool GetIsSwimming()
        {
            return CurrentMoveMode == BaseMoveMode.SWIMMING;
        }
        private void ApplyGravity(float deltaTime, bool hasControl)
        {
            if (CurrentMoveMode == BaseMoveMode.SWIMMING || CurrentMoveMode == BaseMoveMode.NOCLIP || CurrentMoveMode == BaseMoveMode.ZIPLINING)
            {
                GravityAccel = 0.0f;
                GravityVector = ZeroVector;
                return;
            }

            if (GetIsGrounded(hasControl))
            {
                GravityAccel = 0.0f;
                GravityVector = GravityDir * GravityStrength * GravityModifier * deltaTime;
            }
            else
            {
                if (CurrentMoveMode != BaseMoveMode.GLIDING)
                {
                    GravityAccel += GravityStrength;
                }
                else
                {
                    //HRStaminaComponent stamina = (OwningPlayerCharacter as HeroPlayerCharacter).StaminaComponent;
                    //SetGravityModifier(stamina.MaxHP != 0f ? GliderGravityCurve.Evaluate(1 - (stamina.CurrentHP / stamina.MaxHP)) : GliderGravityModifier);
                    GravityAccel = GravityStrength;
                }

                GravityVector = GravityDir * GravityAccel * GravityModifier * deltaTime;
            }
        }

        [HideInInspector]
        public bool bSimulatedMovement;
        [HideInInspector]
        public Vector3 simulatedVectorAmount;

        public float GetAcceleration()
        {
            return CurrentAcceleration;
        }

        private void ApplyAxisInput(float deltaTime, Vector3 currentPosition)
        {
            //debugger tool, used to test networking in parrallel sync if someone needs both client and host to move
            if (bSimulateBackwardInput)
            {
                PlayerInputVector = new Vector3(0, 0, -1f);
            }

            float MoveSpeed = GetMoveSpeed(CurrentMoveSpeed);

            //if characters needs to be moved without using any inputs from player
            //example of this when using a jump attack and we want the character to simulate moving forward
            if (bSimulatedMovement)
            {
                if (!LogicLODEnabled)
                {
                    float moveAcccel = GetMoveAcceleration();
                    PlayerInputVector = simulatedVectorAmount;
                    CurrentAcceleration = Mathf.MoveTowards(CurrentAcceleration, moveAcccel, deltaTime * moveAcccel);
                    PlayerInputMovementVector += PlayerInputVector * CurrentAcceleration * deltaTime;
                    PlayerInputMovementVector = Vector3.ClampMagnitude(PlayerInputMovementVector, MoveSpeed);
                }
                else // LOD
                {
                    PlayerInputMovementVector = PlayerInputVector * MoveSpeed; // When in lod form, no need for acceleration
                }

                return;
            }

            if (bAcceptPlayerInput)
            {
                if (!LogicLODEnabled)
                {
                    if (CurrentMoveMode == BaseMoveMode.GROUND && !GetIsGrounded())
                    {
                        if (PlayerInputMovementVector == ZeroVector)
                        {
                            PlayerInputMovementVector = JumpInputMovementVector;
                        }

                        JumpInputMovementVector = PlayerInputMovementVector;
                    }

                    if (PlayerInputVector != ZeroVector || CurrentAcceleration != 0)
                    {
                        if (PlayerInputVector != ZeroVector)
                        {
                            float moveAcccel = GetMoveAcceleration();
                            CurrentAcceleration = Mathf.MoveTowards(CurrentAcceleration, PlayerInputVector != ZeroVector ? moveAcccel : 0, deltaTime * moveAcccel);
                            PlayerInputMovementVector += PlayerInputVector * CurrentAcceleration * deltaTime;
                            PlayerInputMovementVector = Vector3.ClampMagnitude(PlayerInputMovementVector, MoveSpeed);
                        }
                        else
                        {
                            CurrentAcceleration = 0.0f;
                            PlayerInputMovementVector = ZeroVector;
                        }

                    }
                    else
                    {
                        PlayerInputMovementVector = ZeroVector;
                    }
                }
                else // LOD
                {
                    if (PlayerInputVector != ZeroVector)
                    {
                        PlayerInputMovementVector = PlayerInputVector * MoveSpeed; // When in lod form, no need for acceleration
                    }
                }
            }
            else
            {
                PlayerInputMovementVector = ZeroVector;
            }

            // Increase horizontal speed when gliding
            if (CurrentMoveMode == BaseMoveMode.GLIDING)
            {
                // Force glider to always go forward at max sped
                if (PlayerInputMovementVector == Vector3.zero)
                {
                    PlayerInputMovementVector = this.transform.forward * MoveSpeed;
                }
                else
                {
                    PlayerInputMovementVector = PlayerInputMovementVector.normalized * MoveSpeed;
                }

                if (GliderHorizontalMovementCurve != null && GliderHorizontalMovementCurve.length > 0)
                {
                    HeroPlayerCharacter hrpc = OwningPlayerCharacter as HeroPlayerCharacter;

                    PlayerInputMovementVector *= hrpc.StaminaComponent.MaxHP != 0f ?
                        GliderHorizontalMovementCurve.Evaluate(1 - (hrpc.StaminaComponent.CurrentHP / hrpc.StaminaComponent.MaxHP)) :
                        GliderHorizontalMovementModifier;
                }
                else
                {
                    PlayerInputMovementVector *= GliderHorizontalMovementModifier;
                }
                SetGravityModifier(GliderGravityCurve.Evaluate(Mathf.Clamp((Time.time - StartGlideTime) / 5, 0, 1)));
            }
            else if (FootstepFX?.CollidingWater)
            {
                // This is horrible but to slow player in deep water
                if (OwningPlayerCharacter.PlayerMesh.AnimScript.FootstepFX.CollidingWater.GetWaterSurfacePosition(currentPosition).y - currentPosition.y > 0.6f)
                {
                    PlayerInputMovementVector *= 0.65f;
                }
            }
        }

        private void ApplyFriction(float deltaTime, bool hasControl)
        {
            float CachedFriction = Friction * FrictionModifier * deltaTime;
            // LockPlayerInput is so that when you get hit (staggered) it will use regular ground friction otherwise people will get launched
            if (!bLockPlayerInput && !GetIsSwimming() && (bDidJumpInput || (!GetIsGrounded(hasControl) && !bIgnoreAirFrictionModifier))) // TODO: cache this later maybe
            {
                CachedFriction *= 0.01f;
            }

            if (CurrentAcceleration != 0)
            {
                CurrentAcceleration = Mathf.MoveTowards(CurrentAcceleration, 0.0f, CachedFriction);
            }

            Vector3 normExternalVector = ZeroVector;
            if (ExternalInputVector.x != 0 || ExternalInputVector.z != 0)
            {
                normExternalVector = new Vector3(ExternalInputVector.x, 0f, ExternalInputVector.z).normalized;
            }

            // Apply friction to external input vector
            if (ExternalInputVector.x != 0f)
            {
                ExternalInputVector.x = Mathf.MoveTowards(ExternalInputVector.x, 0f, Mathf.Abs(normExternalVector.x) * CachedFriction);
            }
            if (ExternalInputVector.z != 0f)
            {
                ExternalInputVector.z = Mathf.MoveTowards(ExternalInputVector.z, 0f, Mathf.Abs(normExternalVector.z) * CachedFriction);
            }

            if (bUseGravity)
            {
                if (ExternalInputVector.y > 0)
                {
                    ExternalInputVector.y = Mathf.MoveTowards(ExternalInputVector.y, 0, GravityAccel);
                }
                else
                {
                    ExternalInputVector.y = Mathf.MoveTowards(ExternalInputVector.y, 0, CachedFriction);
                }
            }
        }

        public Vector3 GetRawVelocity()
        {
            return RawVelocity;
        }
        public Vector3 GetRawXZVelocity()
        {
            return RawXZVelocity;
        }

        float LastTimeMoved = 0.0f;
        float TimeBeforeMovementRooted = 0.5f;

        Vector3 ZeroVector = Vector3.zero;

        const float DesiredDeltaTime = 0.02f;
        float ActualDeltaTime;
        float LastStep = -1;
        float DesiredDiveDepth;

        public float GetRandomDiveDepth()
        {
            Vector3 SurfacePosition = CurrentDeepWaters[0].GetWaterSurfacePosition(MovementTransform.position);
            SurfacePosition.y = SurfacePosition.y - OwningPlayerCharacter.CharacterSwimmingHeight;

            if (!OwningPlayerCharacter || !OwningPlayerCharacter.bCanDive || CurrentDeepWaters.Count <= 0)
            {
                return SurfacePosition.y;
            }

            return Random.Range(LowestDiveDepth, SurfacePosition.y);
        }
        public void ApplyDiveDepth(ref List<Vector3> Path)
        {
            if (!OwningPlayerCharacter || !OwningPlayerCharacter.bCanDive || CurrentDeepWaters.Count <= 0)
            {
                ToggleDiving(false);
                return;
            }

            if (Path.Count <= 0) return;

            Vector3 SurfacePosition = CurrentDeepWaters[0].GetWaterSurfacePosition(MovementTransform.position);
            SurfacePosition.y = SurfacePosition.y - OwningPlayerCharacter.CharacterSwimmingHeight;

            DesiredDiveDepth = Mathf.Clamp(Path[Path.Count - 1].y, LowestDiveDepth, SurfacePosition.y);

            float YDiffSegmented = (DesiredDiveDepth - MovementTransform.position.y) / (Path.Count - 1);

            for (int i = 1; i < Path.Count; i++)
            {
                Vector3 PathPoint = Path[i];

                PathPoint.y += YDiffSegmented * i;

                Path[i] = PathPoint;

                if (PathPoint.y < SurfacePosition.y)
                {
                    ToggleDiving(true);
                }
            }
        }

        public void ToggleDiving(bool bDiving)
        {
            if (OwningPlayerCharacter && !OwningPlayerCharacter.bCanDive) return;

            if (bIsDiving != bDiving)
            {
                if (bDiving)
                {
                    if (CurrentMoveMode != BaseMoveMode.SWIMMING) return;
                }

                bIsDiving = bDiving;
            }
        }

        RaycastHit LastGroundingHit;

        Vector3 finalposition;

        float DeltaTime;
        Vector3 TransformPosition;

        private void DesiredUpdate(float deltaTime)
        {
            DeltaTime = deltaTime;
            TransformPosition = MovementTransform.position;
            finalposition = TransformPosition;

            if (bHasAIMovement)
            {
                OwningAIMovement.DesiredUpdate(DeltaTime, bHasNetworkControl, finalposition);
            }

            if (bHasNetworkControl)
            {
                //To keep track how long the player was in air
                if (!GetIsGrounded(bHasNetworkControl))
                {
                    currTimeInAir += DeltaTime;
                }
                else
                {
                    currTimeInAir = 0;
                }
            }

            if (bFrozen)
            {
                if (bUseGravity)
                {
                    ApplyGravity(DeltaTime, bHasNetworkControl);
                }

                Move((bUseGravity ? GravityVector : ZeroVector) * DeltaTime, MovementTransform, bHasNetworkControl);
                return;
            }

            Vector3 lastUpdatedPosition = LastFrameLocalPosition;
            bool bPerformedRotation = false;
            bool bCanUpdateRotationManually = CanUpdateRotationManually(bHasNetworkControl);
            if (bHasNetworkControl)
            {
                if (bUseGravity)
                {
                    ApplyGravity(DeltaTime, bHasNetworkControl);
                }

                ApplyAxisInput(DeltaTime, finalposition);

                if (bUsingFriction)
                {
                    ApplyFriction(DeltaTime, bHasNetworkControl);
                }

                // Prevent moving until you have rotated towards your direction
                if (!bCanUpdateRotationManually)
                {
                    if (PlayerInputMovementVector.sqrMagnitude > 0)
                    {
                        //RotateTowards(bShouldOverrideRotateLocation ? OverrideRotateLocation - this.transform.position : PlayerInputMovementVector);
                        //bPerformedRotation = true;

                        // Check to see if the facing is similar to the input movement -- if they're not similar at all then don't do any movement.
                        if (Velocity.sqrMagnitude == 0 && (Vector3.Dot(PlayerInputMovementVector, CharacterController ? MovementTransform.forward : ZeroVector) < 0.95f) && LastTimeMoved - Time.timeSinceLevelLoad > TimeBeforeMovementRooted)
                        {
                            PlayerInputMovementVector = new Vector3(0, 0, 0);
                        }
                        else
                        {
                            LastTimeMoved = Time.timeSinceLevelLoad;

                            if (IsSprinting())
                            {
                                //Debug.Log("Sprint");
                                if (!bReachedFullSprint)
                                {
                                    if (currSprintTime < minSprintTime)
                                    {
                                        currSprintTime += DeltaTime;
                                    }
                                    //sprinted the min amount of time needed
                                    else
                                    {
                                        bReachedFullSprint = true;
                                        if (fullSprintParticleEffect && OwningPlayerCharacter && OwningPlayerCharacter == BaseGameInstance.Get.GetLocalPlayerPawn())
                                        {
                                            BaseObjectPoolManager.Get.InstantiateFromPool(fullSprintParticleEffect, Parent: OwningPlayerCharacter.PlayerMesh.RigRootTransform);
                                        }
                                    }
                                }
                            }
                            else//still moving but not sprinting
                            {
                                //Debug.Log("No Sprint");
                                bReachedFullSprint = false;
                                currSprintTime = 0;
                            }
                        }
                    }
                    //not moving
                    else
                    {
                        bReachedFullSprint = false;
                        currSprintTime = 0;
                    }
                }

                bool bShouldMove = true;

                if (CurrentMoveMode == BaseMoveMode.GROUND || CurrentMoveMode == BaseMoveMode.GLIDING)
                {
                    if (bIsPlayer || !bIsPathfindingAI)
                    {
                        bool bWasGrounded = GetIsGrounded(bHasNetworkControl);
                        //Move(((bUseInput ? ExternalInputVector + PlayerInputMovementVector : ExternalInputVector) + (bUseGravity ? GravityVector * (bWasGrounded ? 100 : 1.0f) : ZeroVector) + AdditionalMovementVector) * deltaTime, cachedTransform);
                        finalposition += ((bUseInput ? ExternalInputVector + PlayerInputMovementVector : ExternalInputVector) + (bUseGravity ? GravityVector * (bWasGrounded ? 50.0f : 1.0f) : ZeroVector) + AdditionalMovementVector) * DeltaTime + UnscaledAdditionalMovementVector;

                        bool newIsGrounded = GetIsGrounded(bHasNetworkControl);
                        if (!bWasGrounded && newIsGrounded)
                        {
                            ExternalInputVector.y = 0.0f;
                        }
                    }
                    else
                    {
                        //finalposition += ((ExternalInputVector + (bUseGravity ? (GravityVector * (GetIsGrounded() ? 0 : 1f)) : ZeroVector) + AdditionalMovementVector) * DeltaTime ) + UnscaledAdditionalMovementVector;

                        if (ExternalInputVector != Vector3.zero)
                        {

                            finalposition += (((ExternalInputVector) + ((bUseGravity && !LogicLODEnabled) ? (GravityVector * (GetIsGrounded(bHasNetworkControl) ? 0 : 1f)) : ZeroVector)) * DeltaTime);
                        }
                        else
                        {
                            finalposition += (((AdditionalMovementVector + ExternalInputVector) + ((bUseGravity && !LogicLODEnabled) ? (GravityVector * (GetIsGrounded(bHasNetworkControl) ? 0 : 1f)) : ZeroVector)) * DeltaTime) + UnscaledAdditionalMovementVector;
                        }

                    }

                    int FarHit = -1;

                    if (bHasController)
                    {
                        if (bIsPlayer)
                        {
                            bool isGrounded = false;

                            if (!RecentlyJumpSlid())
                            {
                                float ClosestHit = float.MaxValue;

                                PhysicsUtil.SphereCastNonAlloc(MovementTransform.position + GroundedCheckOffset, CharacterController.radius * 0.75f, Vector3.down, CharacterController.height * .5f, GroundedLayers);
                                if (PhysicsUtil.BufferLength > 0)
                                {
                                    bool floorfound = false;

                                    for (int i = PhysicsUtil.BufferLength - 1; i >= 0; i--)
                                    {
                                        //if (PhysicsUtil.RaycastBuffer[i].collider == null) continue;

                                        if (!isGrounded && PhysicsUtil.RaycastBuffer[i].point != Vector3.zero)
                                        {
                                            Collider hitCollider = PhysicsUtil.RaycastBuffer[i].collider;
                                            if (hitCollider.transform.root != MovementTransform.root)
                                            {
                                                float CurrSqrDistance = (PhysicsUtil.RaycastBuffer[i].point - MovementTransform.position).sqrMagnitude;

                                                if (CurrSqrDistance < .03f && PhysicsUtil.RaycastBuffer[i].distance < ClosestHit)
                                                {
                                                    LastGroundingHit = PhysicsUtil.RaycastBuffer[i];
                                                    ClosestHit = PhysicsUtil.RaycastBuffer[i].distance;
                                                }
                                                else if (CurrSqrDistance < 0.5f)
                                                {
                                                    FarHit = i;
                                                }

                                                if (OwningPlayerCharacter.IsInShopPlot && !floorfound && CheckFloorTile(hitCollider))
                                                {
                                                    floorfound = true;
                                                    HRFloorTile floorTile = hitCollider.GetComponentInParent<HRFloorTile>();
                                                    OwningAIMovement.SetFloorTile(floorTile, !floorTile);
                                                }
                                            }
                                        }

                                    }

                                    if (ClosestHit != float.MaxValue) // Optimize check for grounding
                                    {
                                        isGrounded = true;
                                    }


                                    if (!floorfound && isGrounded)
                                    {
                                        OwningAIMovement.SetFloorTile(null, true);
                                    }

                                    if (!isGrounded)
                                    {
                                        // This is for if nothing is "grounded" but we want to slide on something that was farther than the very strict 0.15 threshold.
                                        if (FarHit != -1)
                                        {
                                            LastGroundingHit = PhysicsUtil.RaycastBuffer[FarHit];
                                            isGrounded = true;
                                        }
                                    }

                                    PhysicsUtil.ClearRaycastBuffer();
                                }
                            }

                            GroundMovementUpdatePostRaycast(isGrounded);
                        }
                        else
                        {
                            float magnitude = Mathf.Clamp(MovementTransform.position.y - finalposition.y, 1.0f, 99);
                            PhysicsUtil.ScheduleRaycast(HandleGroundRaycastResult, finalposition + (Vector3.up * magnitude), Vector3.down, magnitude + 0.1f, GroundedLayers);
                        }
                    }
                }
                else if (CurrentMoveMode == BaseMoveMode.SWIMMING)
                {
                    // Move to the top of the first water
                    if (CurrentDeepWaters.Count > 0 && CurrentDeepWaters[0])
                    {
                        if (!bIsPlayer && bIsPathfindingAI)
                        {
                            finalposition += (AdditionalMovementVector * DeltaTime);
                        }

                        Vector3 TargetPosition = CurrentDeepWaters[0].GetWaterSurfacePosition(finalposition);
                        float YPosition = MovementTransform.position.y;
                        float SurfacePosition = TargetPosition.y - OwningPlayerCharacter.CharacterSwimmingHeight;

                        if (bIsDiving)
                        {
                            float DivingDirection;    // > 0 - Up, < 0 - Down, 0 - Flat
                            if (bIsPlayer)
                            {
                                if (PlayerInputMovementVector == ZeroVector || !OwningPlayerCharacter.PlayerController)
                                {
                                    DivingDirection = 0;
                                }
                                else
                                {
                                    float LookAngle = Mathf.Clamp(OwningPlayerCharacter.PlayerController.PlayerCamForward.y, -1, 1);
                                    float MovingForward = Vector3.Dot(PlayerInputMovementVector, MovementTransform.forward);

                                    DivingDirection = LookAngle * MovingForward;
                                }
                            }
                            else
                            {
                                DivingDirection = DesiredDiveDepth - MovementTransform.position.y;
                            }

                            PhysicsUtil.RaycastNonAlloc(TransformPosition, Vector3.down, OwningPlayerCharacter.MaxDiveDepth, GroundedLayers, QueryTriggerInteraction.Ignore);
                            if (PhysicsUtil.RayCastHitValid)
                            {
                                LowestDiveDepth = PhysicsUtil.RaycastBuffer[0].point.y;
                            }
                            else
                            {
                                LowestDiveDepth = YPosition;
                            }

                            TargetPosition.y = Mathf.Clamp(YPosition + DivingDirection * OwningPlayerCharacter.DivingSpeed * DeltaTime, LowestDiveDepth, SurfacePosition);

                            if (TargetPosition.y >= SurfacePosition)
                            {
                                ToggleDiving(false);
                            }
                        }
                        else
                        {
                            TargetPosition.y = SurfacePosition;
                        }

                        if (!bIsPlayer && bHasAIMovement && OwningAIMovement.bHasRichAI)
                        {
                            PathfindingAI.ClosestSwimPosition = TargetPosition;
                        }

                        finalposition = TargetPosition;

                        //Move(((bUseInput ? ExternalInputVector + PlayerInputMovementVector : ExternalInputVector)) * deltaTime, cachedTransform);
                        finalposition += ((bUseInput ? ExternalInputVector + PlayerInputMovementVector : ExternalInputVector)) * DeltaTime + UnscaledAdditionalMovementVector;

                        // This is set so that there is no fall damage when swimming
                        prevPositionOnGround = finalposition;

                        if (GetIsGrounded(bHasNetworkControl) != false)
                        {
                            SetNetworkedIsGrounded(false);
                        }
                    }
                    else
                    {
                        SetMoveMode(BaseMoveMode.GROUND);
                    }
                }
                else if (CurrentMoveMode == BaseMoveMode.NOCLIP)
                {
                    if (PlayerController)
                    {
                        Vector3 TotalMoveVector = (ForwardAxis * PlayerController.GetCamera().transform.forward + RightAxis * PlayerController.GetCamera().transform.right) * GetMoveSpeed(CurrentMoveSpeed) * 5.0f * DeltaTime;
                        finalposition += TotalMoveVector;
                        MovementTransform.position = finalposition;
                    }
                }
                else if (CurrentMoveMode == BaseMoveMode.ZIPLINING)
                {
                    // Move towards zipline
                    if (AttachedZipline_Local)
                    {
                        // Move towards the zip line point that is not the player forward
                        Vector3 ZiplineMovementVector = AttachedZipline_Local.GetMovementVector(ZiplinePointToTravelTo);

                        // Move towards the other side at the zipline speed

                        //Move(ZiplineMovementVector * MaxSprintSpeed * 3.0f * deltaTime, cachedTransform);

                        finalposition += ZiplineMovementVector * MaxSprintSpeed * 2.0f * DeltaTime;
                        prevPositionOnGround = finalposition;

                        if (AttachedZipline_Local.IsPastPoint(MovementTransform, ZiplinePointToTravelTo))
                        {
                            DetachZipline();
                        }
                    }
                    else
                    {
                        DetachZipline();
                    }
                }

                // The final move
                if(bShouldMove)
                {
                    Move(finalposition - TransformPosition, MovementTransform, bHasNetworkControl);
                }
            }

            movedThisFramed = true; // Set true when the fixed update is called

            /*
            if ((StickingPoint != null && StickingPoint.transform.parent == PrevStickingPointParent) || !bShouldUseStickingPoint)
            {
                if (CharacterController.transform.parent)
                {
                    Velocity = (CharacterController.transform.parent.TransformPoint(CharacterController.transform.localPosition)
                        - CharacterController.transform.parent.TransformPoint(LastFrameLocalPosition)) / deltaTime;
                }
                else
                {
                    Velocity = (CharacterController.transform.localPosition - LastFrameLocalPosition) / deltaTime;
                }
            }
            */

            if (FootstepFX)
            {
                FootstepFX.bPlayParticleEffects = (RawVelocity.sqrMagnitude >= (MaxWalkSpeed * MaxWalkSpeed));
            }

            if (bHasNetworkControl)
            {
                if (bShouldRotate && CurrentMoveMode != BaseMoveMode.ZIPLINING)
                {
                    float RotateSpeed = GetRotateSpeed();
                    if (bCanUpdateRotationManually)
                    {
                        RotateTowards(Vector3.ProjectOnPlane(OverrideRotateLocation - OwningPlayerCharacter.Position, MovementPlaneNormal), RotateSpeed, DeltaTime, MovementTransform);
                    }
                    else if (!bPerformedRotation)
                    {
                        bool bShouldNotRotateTowardsCamera = !bShouldFaceCamera || bFrozen
                            || !IsUsingPlayerInput() || (!PlayerController || ((PlayerController && (!PlayerController.PlayerPawn || !PlayerController.PlayerPawn.PlayerCamera))) || (!DashComponent || (DashComponent && DashComponent.bIsDashing))
                            || (IsSprinting() || CurrentMoveMode == BaseMoveMode.GLIDING || (bFaceCameraRequiresVelocity && PlayerInputVector == ZeroVector)));

                        if (bShouldNotRotateTowardsCamera)
                        {
                            if (bShouldRotateTowardsMovement)
                            {
                                RotateTowards(RawXZVelocity, RotateSpeed, DeltaTime, MovementTransform);
                            }
                            else if (bShouldRotateTowardsInput)
                            {
                                RotateTowards(Vector3.ProjectOnPlane(PlayerInputVector, MovementPlaneNormal), RotateSpeed, DeltaTime, MovementTransform);
                            }
                        }
                        else
                        {
                            // Face forward
                            if (PlayerController)
                            {
                                RotateTowards(Vector3.ProjectOnPlane(PlayerController.PlayerPawn.PlayerCamera.transform.forward, MovementPlaneNormal), 50.0f, DeltaTime, MovementTransform);
                            }
                        }
                    }

                    // Replicate value for lean or other potential uses
                    ReplicatePlayerInputVector();
                }

                // Disabled for now
                //if (this.transform.position.y < -50.0f && this.gameObject.tag != "Player")
                //{
                //    BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject.Destroy.DestroyGameObject(this.gameObject, 0f);
                //}
            }



            if (bHasController)
            {
                LastFrameLocalPosition = MovementTransform.localPosition;
                LastFrameLocalRotation = MovementTransform.localRotation;
                LastFramePosition = MovementTransform.position;
                LastFrameRotation = MovementTransform.rotation;
            }


            // Calls a position changed delegate.
            if (lastUpdatedPosition != LastFrameLocalPosition)
            {
                OnPositionChangedDelegate?.Invoke(this, lastUpdatedPosition, LastFrameLocalPosition);
            }

            if (bNeedToSetRotation)
            {
                ConsumeSetRotation();
            }

            AdditionalMovementVector = Vector3.zero;
            UnscaledAdditionalMovementVector = AdditionalMovementVector;
            OwningPlayerCharacter.Position = LastFramePosition;
        }

        void HandleGroundRaycastResult(bool bHit, RaycastHit HitStruct)
        {
            if (this == null) return;

            if (bHit)
            {
                LastGroundingHit = HitStruct;
                LastGroundedCollider = HitStruct.collider;

                finalposition.y = HitStruct.point.y;

                if (OwningPlayerCharacter.IsInShopPlot) // If the ai is in the shop plot, start checking for floor tiles
                {
                    Collider hitCollider = HitStruct.collider;
                    if (CheckFloorTile(hitCollider))
                    {
                        HRFloorTile floorTile = hitCollider.GetComponentInParent<HRFloorTile>();
                        OwningAIMovement.SetFloorTile(floorTile, !floorTile);
                    }
                    else
                    {
                        OwningAIMovement.SetFloorTile(null, true);
                    }
                }
            }

            GroundMovementUpdatePostRaycast(bHit);
        }

        void GroundMovementUpdatePostRaycast(bool isGrounded)
        {
            if (isGrounded)
            {
                //Only check CharacterController.isGrounded because AI may not be calling Move() to trigger a hit
                if (bHasController && !LogicLODEnabled) // Only do this if they within LOD
                {
                    bool bIsSliding = ApplySlopeForces(LastGroundingHit, DeltaTime);

                    if (bIsSliding)
                    {
                        isGrounded = false;
                        prevPositionOnGround = finalposition;

                        //// If we are moving between 15 and 80 degrees of the inversed normal, reset velocity (to simulate surfing)
                        //float SurfAngle = Vector3.Angle(-LastGroundingHit.normal, (finalposition - transform.position).normalized);
                        //if (SurfAngle > 15 && SurfAngle < 80)
                        //{
                        //    ResetGravityAcceleration();
                        //}
                    }

                    SetNetworkedIsSliding(bHasNetworkControl, bIsSliding);
                }
            }

            if (isGrounded)
            {
                LastGroundedCollider = LastGroundingHit.collider; // Maybe this needs to be networked somehow?
            }

            if (GetIsGrounded(bHasNetworkControl) != isGrounded)
            {
                if (isGrounded && (CurrentMoveMode == BaseMoveMode.GLIDING))
                {
                    prevPositionOnGround = finalposition;
                }

                if (isGrounded)
                {
                    if (!bIsPlayer || LastGroundingHit.collider != null)
                    {
                        SetNetworkedIsGrounded(true);
                    }
                }
                else
                {
                    SetNetworkedIsGrounded(false);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!bIsPlayer) return;

            bHasNetworkControl = bHasNetworkControl || HRNetworkManager.HasControl(netIdentity);

            DesiredUpdate(Time.fixedDeltaTime);
        }

        private void Update()
        {
            if (this == null)
            {
                return;
            }

            bHasNetworkControl = bHasNetworkControl || HRNetworkManager.HasControl(netIdentity);

            if (!bFrozen)
            {
                if (movedThisFramed)
                {
                    Vector3 lastRawVelocity = (OwningPlayerCharacter.Position - PrevPosition) / Time.deltaTime;
                    float VelocityMagnitude = Velocity.sqrMagnitude;
                    float RawVelocityMagnitude = lastRawVelocity.sqrMagnitude;
                    if (Mathf.Max(VelocityMagnitude, RawVelocityMagnitude) == VelocityMagnitude)
                    {
                        //SetVelocity(Velocity);
                        RawVelocity = Velocity;
                        RawXZVelocity = new Vector3(RawVelocity.x, 0, RawVelocity.z);
                    }
                    else
                    {
                        //SetVelocity(lastRawVelocity);
                        RawVelocity = lastRawVelocity;
                        RawXZVelocity = new Vector3(RawVelocity.x, 0, RawVelocity.z);
                    }

                    PrevPosition = OwningPlayerCharacter.Position;
                    movedThisFramed = false;
                }

                CurrentUpdateTime += Time.deltaTime;

                if (movedThisFramed && CurrentUpdateTime >= RebuildGridInterval)
                {
                    CurrentUpdateTime = 0;
                    Mirror.NetworkServer.RebuildObservers(netIdentity, false);
                    if (OwningPlayerCharacter.hasPoolingComponent)
                    {
                        OwningPlayerCharacter.CurrentPoolingComponent.UpdateCellPosition(OwningPlayerCharacter.Position);
                    }
                }

                if (bHasNetworkControl && CurrentMoveSpeed == BaseMoveType.SPRINTING && PlayerController)
                {
                    if (PlayerController.PlayerPawn && PlayerController.PlayerPawn.PlayerCamera && Vector3.Angle(PlayerInputVector, Vector3.ProjectOnPlane(PlayerController.PlayerPawn.PlayerCamera.transform.forward, Vector3.up)) > 55.0f)
                    {
                        SetMoveSpeed(BaseMoveType.JOGGING);
                    }
                }
            }

            if (!bIsPlayer)
            {
                ActualDeltaTime = Time.time - LastStep;
                if (ActualDeltaTime >= DesiredDeltaTime)
                {
                    DesiredUpdate(ActualDeltaTime);
                    LastStep = Time.time;
                }

            }

            //else
            //{
            //    if(PlayerController)
            //    {
            //        // Compare input vector direction with camera movement to simulate airstrafing
            //        if(ExternalInputVector != ZeroVector)
            //        {
            //            // Check if movement of camera matches input movement -- if so, modify external input vector.
            //            if(Vector3.Dot(PlayerInputMovementVector, PlayerController.PlayerPawn.PlayerCamera.transform.right) * PlayerController.PlayerPawn.PlayerCamera.GetUserRotationDirection() > 0)
            //            {
            //                ExternalInputVector = Vector3.ProjectOnPlane(PlayerController.PlayerPawn.PlayerCamera.transform.forward, Vector3.up) * ExternalInputVector.magnitude;
            //            }
            //        }
            //    }
            //}
        }

        public bool CanUpdateRotationManually(bool bHasControl)
        {
            if (GetMoveType(bHasControl) == BaseMoveType.SPRINTING)
            {
                return false;
            }
            else
            {
                return bShouldOverrideRotateLocation;
            }
        }

        public float GetMoveAcceleration()
        {
            return GetMoveAcceleration(CurrentMoveSpeed);
        }

        public float GetMoveAcceleration(BaseMoveType InMoveType)
        {
            float baseAccel = 0.0f;
            switch (InMoveType)
            {
                case BaseMoveType.WALKING:
                    baseAccel = WalkAccel;
                    break;
                case BaseMoveType.JOGGING:
                    baseAccel = SprintAccel;
                    break;
                case BaseMoveType.SPRINTING:
                    baseAccel = SprintAccel;
                    break;
            }
            return baseAccel;
        }

        public float GetMoveSpeed()
        {
            return GetMoveSpeed(CurrentMoveSpeed);
        }

        public float GetMoveSpeed(BaseMoveType InMoveType)
        {
            float BaseSpeed = 0.0f;

            switch (InMoveType)
            {
                case BaseMoveType.WALKING:
                    BaseSpeed = MaxWalkSpeed;
                    if (bUseRandomizedAudio && LoopingAudioTrigger && WalkClips.Count > 0)
                    {
                        LoopingAudioTrigger.StartRandomLoop(WalkClips);
                    }
                    break;
                case BaseMoveType.JOGGING:
                    BaseSpeed = MaxJogSpeed;
                    if (bUseRandomizedAudio && LoopingAudioTrigger && JogClips.Count > 0)
                    {
                        LoopingAudioTrigger.StartRandomLoop(JogClips);
                    }
                    break;
                case BaseMoveType.SPRINTING:
                    BaseSpeed = MaxSprintSpeed;
                    if (bUseRandomizedAudio && LoopingAudioTrigger && SprintClips.Count > 0)
                    {
                        LoopingAudioTrigger.StartRandomLoop(SprintClips);
                    }
                    break;
            }

            return BaseSpeed *= MovementSpeedModifier * (CurrentMoveMode == BaseMoveMode.SWIMMING ? SwimSpeedReduction : 1.0f);
        }

        float LastTimeInputNetworked;
        float NetworkUpdateInterval = 0.5f;
        public void ReplicatePlayerInputVector()
        {
            if (bHasNetworkIdentity)
            {
                if (Time.timeSinceLevelLoad - LastTimeInputNetworked > NetworkUpdateInterval)
                {
                    LastTimeInputNetworked = Time.timeSinceLevelLoad;

                    if (HRNetworkManager.IsHost())
                    {
                        if (isServer && Mirror.NetworkServer.connections.Count > 0)
                        {
                            if (bIsPlayer)
                            {
                                SendPlayerInputVector_RPC(PlayerInputVector);
                            }
                            else
                            {
                                // Send desired AI update as the player vector
                                SendPlayerInputVector_RPC(AdditionalMovementVector);
                            }
                        }
                    }
                    else if(bIsPlayer)
                    {
                        Client_SendPlayerInputVector(PlayerInputVector);
                    }
                }

            }
        }

        // Ask server to send your updated vector
        // Will only work if client calls this, not listen server.
        [Mirror.Command(channel = 1)]
        public void Client_SendPlayerInputVector(Vector3 InVector)
        {
            UpdateNetworkedPlayerInputVector(InVector);
        }

        // Will only work if server calls this.
        // Tell all clients to set player input, wonder if there's a way to exclude the client that sent it.
        [Mirror.ClientRpc(channel = 1)]
        public void SendPlayerInputVector_RPC(Vector3 InVector)
        {
            UpdateNetworkedPlayerInputVector(InVector);
        }

        public void UpdateNetworkedPlayerInputVector(Vector3 InVector)
        {
            // Only want to update player input for non authority objects
            if (!hasAuthority)
            {
                PlayerInputVector = InVector;
            }
        }

        public void RotateTowards(Vector3 InVector, float InRotateSpeed, float deltaTime, Transform targetTransform)
        {
            if (InVector == ZeroVector) return;

            Vector3 ProjectedVelocity = InVector;
            // Keep the scale tho
            // Rotate towards the current vector
            // Quaternion PrevScale = CharacterController.transform.rotation;
            if (InRotateSpeed < 0)
            {
                targetTransform.rotation = Quaternion.LookRotation(ProjectedVelocity.normalized, MovementPlaneNormal);
            }
            else
            {

                targetTransform.rotation = Quaternion.Slerp(targetTransform.rotation,
                                                            Quaternion.LookRotation(ProjectedVelocity.normalized, MovementPlaneNormal),
                                                            InRotateSpeed * deltaTime);
            }
        }

        public void RotateTowards(Vector3 InVector, bool bInstant = false)
        {
            RotateTowards(InVector, !bInstant ? GetRotateSpeed() : -1, Time.deltaTime, transform);
        }

        [HideInInspector]
        public Collider LastGroundedCollider = null;

        public Vector3 GetLastGroundedPosition()
        {
            return prevPositionOnGround;
        }

        bool CheckFloorTile(Collider col)
        {
            return (bHasAIMovement && col.CompareTag(Constants.Tags.BUILDINGPIECE) && col.gameObject.layer == Constants.Layers.FLOORTILE);
        }

        #region slope_implementation

        bool CanWalkOnSlope(float InSlope)
        {
            return !(InSlope > maxSlopeClimbAngle && InSlope < 85.0f);
        }

        float LastSlopeAngle = 0.0f;
        private bool ApplySlopeForces(RaycastHit hit, float deltaTime)
        {
            LastSlopeAngle = 0.0f;
            if ((CurrentMoveMode == BaseMoveMode.SWIMMING
                || _applyingSlopeForces) || hit.transform == null)
            {
                return false;
            }

            // Applies the force in the direction of the slope.
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            LastSlopeAngle = angle;

            Vector3 slopeCrossProduct = Vector3.Cross(hit.normal, Vector3.up);
            Vector3 slopeDirection = Vector3.Cross(hit.normal, slopeCrossProduct).normalized;
            if (slopeDirection != ZeroVector
                && !CanWalkOnSlope(angle))
            {
                if (GetIsGrounded())
                {
                    if (CheckClimbOutOfHole(transform.position + CharacterController.center * 2,
                        slopeCastDistance, hit.collider.gameObject, numberOfSlopeRays, slopeLayerMask))
                    {
                        return false;
                    }
                }

                Vector3 normalOutwards = hit.normal;
                normalOutwards.y = 0.0f;
                // Applying slope forces must be here
                // as if it doesn't exist than it will recursively
                // call this function.
                _applyingSlopeForces = true;
                CharacterController.Move(normalOutwards.normalized * deltaTime);

                Vector3 slopeForce = slopeDirection;
                if (!ShouldApplySlopeForce(
                    CharacterController.center * 2 + transform.position,
                    slopeForce, slopeForce.magnitude * deltaTime,
                    slopeLayerMask, hit.collider.gameObject))
                {
                    _applyingSlopeForces = false;
                    return false;
                }

                AddForce(slopeForce * maxSlopeSpeedGain * deltaTime);
                _applyingSlopeForces = false;

                return true;
            }

            return false;
        }

        private bool ShouldApplySlopeForce(Vector3 position, Vector3 slopeForce, float magnitude, LayerMask layers, GameObject filter)
        {
            Collider outCollider;
            if (HitCollider(
                new Ray(position, slopeForce.normalized), magnitude, layers, filter, out outCollider))
            {
                return false;
            }
            return true;
        }

        private bool CheckClimbOutOfHole(Vector3 position, float radius, GameObject filter, int numberOfRays, LayerMask layers)
        {
            int validRays = 0;
            int numberOfRaysInFront = 0;
            int validRaysInFront = 0;

            Vector3 forwardDirectionNoY = transform.forward;
            forwardDirectionNoY.y = 0.0f;

            for (int currentRay = 0; currentRay < numberOfRays; currentRay++)
            {
                Ray calculatedRay = CalculateSlopeRay(position, currentRay, numberOfRays);
                float dotProductDirection = Vector3.Dot(forwardDirectionNoY, calculatedRay.direction);
                bool isRayInFront = dotProductDirection > 0.0f;
                if (isRayInFront)
                {
                    numberOfRaysInFront++;
                }

                Collider outCollider;
                if (HitCollider(calculatedRay, radius, layers, filter, out outCollider))
                {
                    if (isRayInFront)
                    {
                        validRaysInFront++;
                    }
                    validRays++;
                }
            }

            if (numberOfRaysInFront <= 0)
            {
                return false;
            }

            float validRaysPercentage = (float)validRays / (float)numberOfRays;
            float validRaysInFrontPercentage = (float)validRaysInFront / (float)numberOfRaysInFront;
            // Checks if its in a hole and if there is a certain number of hits
            // in front of the player.
            return (validRaysPercentage * 100.0f) >= raysHolePercentage
                && validRaysInFrontPercentage >= 0.9f;
        }

        private Ray CalculateSlopeRay(Vector3 origin, int currentRay, int totalRays)
        {
            float theta = (Mathf.PI * 0.5f) / (float)totalRays;
            float angle = theta * currentRay;
            Vector3 direction = new Vector3(
                 Mathf.Cos(angle),
                0.0f,
                Mathf.Sin(angle));
            return new Ray(origin, direction.normalized);
        }

        private bool HitCollider(Ray ray, float distance, LayerMask mask, GameObject filter, out Collider collider)
        {
            PhysicsUtil.RaycastAllNonAlloc(ray.origin, ray.direction, distance, mask);

            for (int i = 0; i < PhysicsUtil.BufferLength; i++)
            {
                RaycastHit currentHit = PhysicsUtil.RaycastBuffer[i];
                if (IsValidColliderCandidate(
                    currentHit.collider)
                    && currentHit.transform.gameObject != filter)
                {
                    collider = currentHit.collider;
                    PhysicsUtil.ClearRaycastBuffer();
                    return true;
                }
            }
            collider = null;
            PhysicsUtil.ClearRaycastBuffer();
            return false;
        }

        #endregion

        private bool IsValidColliderCandidate(Collider hit)
        {
            return hit
                && !hit.transform.IsChildOf(transform);
        }

        bool bNeedToSetRotation = false;
        Quaternion NextFrameRotationToSet;

        public void SetRotation(Quaternion InRotation)
        {
            bNeedToSetRotation = true;
            NextFrameRotationToSet = InRotation;
        }

        public bool TryGetRaycastInfo(out RaycastHit RaycastInfo)
        {
            if (bHasAIMovement && OwningAIMovement.bHasRichAI && bIsPathfindingAI)
            {
                return PathfindingAI.TryGetRaycastInfo(out RaycastInfo);
            }

            RaycastInfo = new RaycastHit();
            return false;
        }
        private void ConsumeSetRotation()
        {
            bNeedToSetRotation = false;
            transform.rotation = NextFrameRotationToSet;
        }

        public static void SetGlobalScale(Transform transform, Vector3 globalScale)
        {
            transform.localScale = Vector3.one;
            transform.localScale = new Vector3(globalScale.x / transform.lossyScale.x, globalScale.y / transform.lossyScale.y, globalScale.z / transform.lossyScale.z);
        }

        public bool IsUsingPlayerInput()
        {
            return bAcceptPlayerInput;
        }
        public void HandleAddedToPool(BaseObjectPoolingComponent PoolingComponent)
        {

        }
        public void HandlePoolInstantiate(BaseObjectPoolingComponent PoolingComponent)
        {

        }
        public void HandleReturnToPool(BaseObjectPoolingComponent PoolingComponent)
        {
            FreezeMovement(false);

            if (!PoolingComponent.bReserved)
            {
                SetMoveMode(BaseMoveMode.GROUND);
            }
        }

        public bool IsFrozen()
        {
            return bFrozen;
        }

        [System.NonSerialized] public bool LogicLODEnabled;
        public void EnableLOD(LogicLOD logicLOD)
        {
            LogicLODEnabled = true;
        }

        public void DisableLOD(LogicLOD logicLOD)
        {
            LogicLODEnabled = false;
        }

        private void OnDrawGizmos()
        {
            if (LogicLODEnabled)
            {
                Gizmos.DrawSphere(transform.position + Vector3.up * 3, 1);
            }
        }
    }
}