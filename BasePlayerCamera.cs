using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace BaseScripts
{
    // Representation of the player's camera. Should be placed with another camera.
    public class BasePlayerCamera : MonoBehaviour
    {
        public Ray GetAimRay()
        {
            if(IsMouseRotating)
            {
                Ray CameraRay = new Ray(this.transform.position, this.transform.forward);
                return CameraRay;
            }
            else
            {
                return GetCamera().ScreenPointToRay(Input.mousePosition);
            }
        }

        public Camera GetCamera()
        {
            // Cache Camera.main since it is notoriously slow
            if (!CurrentCamera)
            {
                CurrentCamera = Camera.main;
            }
            return CurrentCamera;
        }
        public Transform GetCameraTransform()
        {
            if (!CurrentCameraTransform)
            {
                CurrentCameraTransform = GetCamera().transform;
            }
            return CurrentCameraTransform;
        }


        public struct BasePlayerCameraSettings
        {
            // How far back the camera can be
            public bool bShouldOverrideWorldRotation;
            public Vector3 OverrideWorldRotation;

            public bool bShouldOverrideRotationAmounts;
            public float MaxRotationAmount;
            public float MinRotationAmount;
        }

        public delegate void BasePlayerCameraNoMouseSignature(bool bNoShow);
        public BasePlayerCameraNoMouseSignature OnNoMouseDelegate;

        public delegate void BasePlayerCameraZoomTickSignature(float InTick);
        public BasePlayerCameraZoomTickSignature OnZoomTickDelegate;

        public Cinemachine.CinemachineVirtualCamera VirtualCamera;

        // Used for when taken over by an override volume.
        [System.NonSerialized]
        public Cinemachine.CinemachineVirtualCamera OverrideVirtualCamera;

        // Where the camera wants to be.
        public GameObject CameraTargetGameObject;

        public bool bMouseControlsCamera = true;
        public bool bCanRotate = true;

        public BasePlayerCameraSettings DefaultCamSettings;
        // How far back the camera can be
        public float MaxZoomAmount = 10.0f;
        public float MaxPlayerZoomAmount = 10.0f;
        // How close the camera can be
        public float MinZoomAmount = 1.0f;

        float CameraSensitivity = 0.2f;

        // How much has been zoomed
        [HideInInspector]
        public float CurrentZoomAmount = 0.0f;

        // How much to zoom per tick
        public float ZoomTickInterval = 1.0f;

        bool bLockZoom;

        // If this camera should detach. This is useful for when we want to use a follow camera instead of letting it
        // be parented straight to the player.
        public bool ShouldDetachCamera = true;
        // Root of everything that will be detached.
        public GameObject CameraRoot;
        Vector3 CameraRootRelativeLocation;
        Vector3 OriginalCameraRootRelativeLocation;
        GameObject CameraRootParent;

        public bool bShouldUseNewRotationAtAll = true;
        public bool bUseCinematicRotation = false;
        Quaternion OriginalRootWorldRotation;
        public Vector3 TEMPCINEMATICROTATION;
        bool bShouldOverrideWorldRotation;
        Vector3 OverrideWorldRotation;

        public Vector2 CameraOffset = new Vector2(0, 0);
        public Vector2 BaseCameraOffset = new Vector2(0, 0);
        private Vector2 AimPosition = new Vector2(0, 0);
        private Vector3 MousePosition = new Vector3(0, 0, 0);
        private bool IsMouseRotating = true;

        public bool bUseDefaultRotation = false;

        public bool bUsePositionOffset = true;

        public bool bInverseMouseX;
        public bool bInverseMouseY;

        public Vector3 MaxPositionOffset = new Vector3(0, 0, 0);
        public Vector3 MinPositionOffset = new Vector3(0, 0.219f, 0);
        public float DefaultRotationAmount = 50.0f;
        public float MaxRotationAmount = 80.0f;
        public float TEMPMAXCINEMATICROTATIONAMOUNT = 30.0f;
        public float MinRotationAmount = 10.0f;

        public SimpleFollower FollowerComponent;
        public BaseFreeCamera FreeCam;
        [SerializeField]
        private float mouseControlsCameraInterpSpeed = 9001.0f;
        [SerializeField]
        private float mouseControlsCameraRotationInterpSpeed = 9001.0f;

        int CurrentPriority = -1;

        public float FixedPlayerZoom = 8.0f;

        float PlayerOriginalZoom = 0.0f;

        float OriginalMinRotation;
        float OriginalMaxRotation;
        float OriginalMinZoom;
        float OriginalMaxZoom;

        public float MaxRotationClamp = 0.5f;

        [Header("Clamping")]
        public bool bClampToEdges = true;
        bool bOriginalClampToEdges = false;
        public bool bLockZoomWhenClamped = true;
        public float TopEdgeOffset = 0;
        public float BottomEdgeOffset = 0;
        public float LeftEdgeOffset = 0;
        public float RightEdgeOffset = 0;
        public LayerMask ClampLayerMask;

        [Tooltip("Layer Mask used when clamping between camera and player.")]
        public LayerMask ForwardClampLayerMask;
        public bool bClampToObstacleBounds;

        Camera CurrentCamera = null;
        Transform CurrentCameraTransform = null;

        Transform TransformToFollow = null;

        bool bUseOverrideFollowTransform = false;
        Transform OverrideFollowTransform = null;

        Vector3 ZoomDirectionLocal = Vector3.up;

        public void SetMouseControlsCamera(bool bInControlsCamera)
        {
            return;

            bMouseControlsCamera = bInControlsCamera;
        }

        // Use this for initialization
        void Awake()
        {
            SetMouseRotating(true);

            OriginalMinRotation = MinRotationAmount;
            OriginalMaxRotation = MaxRotationAmount;
            OriginalMinZoom = MinZoomAmount;
            OriginalMaxZoom = MaxZoomAmount;

            if (CameraRoot)
            {
                TransformToFollow = CameraRoot.transform.parent;
                CameraRootParent = CameraRoot.transform.parent.gameObject;
                CameraRootRelativeLocation = CameraRoot.transform.localPosition;

                if (FollowerComponent)
                {
                    FollowerComponent.OriginalRootObj = CameraRootParent;
                    FollowerComponent.OriginalRootRelativeLocation = CameraRootRelativeLocation;
                }

                OriginalCameraRootRelativeLocation = CameraRootRelativeLocation;
                OriginalRootWorldRotation = CameraRoot.transform.rotation;
                //OriginalFOV = VirtualCamera.m_Lens.FieldOfView;
                TargetFOV = GetFOV();

                ZoomDirectionLocal = CameraTargetGameObject.transform.localPosition;
                ZoomDirectionLocal.Normalize();

                CurrentZoomAmount = FixedPlayerZoom;

                ClampZoom();

                CameraRoot.transform.SetParent(null);
            }

            // Detach camera.
            if (ShouldDetachCamera)
            {
                transform.parent = null;
            }

            bOriginalClampToEdges = bClampToEdges;
            bClampToEdges = false;

            if (PixelCrushers.DialogueSystem.DialogueManager.instance)
            {
                PixelCrushers.DialogueSystem.DialogueManager.instance.RequestPauseDelegate += HandleConversationRequestPause;
            }

            PlayerOriginalZoom = CurrentZoomAmount;

            ResetCameraToPlayer();

            HRNetworkManager.Get.ClientDisconnectedDelegate += OnClientDisconnected;
        }


        private void OnClientDisconnected()
        {
            if (BaseGameInstance.Get.LobbyManager.ConnectingInvasion)
            {
                return;
            }

            SetMouseRotating(false);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public float GetFOV()
        {
            return OriginalFOV + BaseAdditiveFOV;
        }

        float OriginalFOV = 70f;
        float BaseAdditiveFOV;

        float TargetFOV;

        public void ResetFOV()
        {
            TargetFOV = GetFOV();
            CurrentFOVMultiplier = 1.0f;
        }

        public void SetOriginalFOV(float InOriginalFOV)
        {
            OriginalFOV = InOriginalFOV;
            SetFOV(GetFOV(), true);
        }

        public void AddBaseAdditiveFOV(float InAdditivePOV)
        {
            SetBaseAdditiveFOV(BaseAdditiveFOV + InAdditivePOV);
        }
        public void SetBaseAdditiveFOV(float InAdditiveFOV)
        {
            BaseAdditiveFOV = InAdditiveFOV;
            SetFOV(GetFOV());
        }

        public float GetPercentOfTargetFOV()
        {
            return TargetFOV / VirtualCamera.m_Lens.FieldOfView;
        }

        public void SetFOV(float InFOV, bool bInstant = false)
        {
            TargetFOV = InFOV;

            if(bInstant)
            {
                VirtualCamera.m_Lens.FieldOfView = InFOV;
            }
        }

        public void SetFOVMultiplier(float InMultiplier, bool bInstant = false)
        {
            SetFOV(GetFOV() * InMultiplier, bInstant);
            CurrentFOVMultiplier = InMultiplier;
        }

        public float GetFOVMultiplier()
        {
            return CurrentFOVMultiplier;
        }

        float CurrentFOVMultiplier = 1.0f;

        bool bRevertCinematicTEMP = false;

        public void SetMinMaxRotation(float InMin, float InMax)
        {
            MinRotationAmount = InMin;
            MaxRotationAmount = InMax;

            if (bShouldUseNewRotationAtAll)
            {
                bShouldUseNewRotationAtAll = false;
                bRevertCinematicTEMP = true;
            }
        }

        public void SetMinMaxZoom(float InMin, float InMax)
        {
            MinZoomAmount = InMin;
            MaxZoomAmount = InMax;
        }

        public void SetCameraSensitivity(float NewSens)
        {
            CameraSensitivity = Mathf.Clamp(NewSens, 0.01f, 1f);
        }

        public void ResetMinMaxRotation()
        {
            MinRotationAmount = OriginalMinRotation;
            MaxRotationAmount = OriginalMaxRotation;

            if (bRevertCinematicTEMP)
            {
                bShouldUseNewRotationAtAll = true;
                bRevertCinematicTEMP = false;
            }
        }

        public void ResetMinMaxZoom()
        {
            MinZoomAmount = OriginalMinZoom;
            MaxZoomAmount = OriginalMaxZoom;

            SetZoomAmount(Mathf.Clamp(CurrentZoomAmount, MinZoomAmount, MaxZoomAmount));
        }

        void HandleConversationRequestPause(bool bPaused)
        {
            if (bOriginalClampToEdges)
            {
                bClampToEdges = !bPaused;
            }
        }

        public void SetPlayerOriginalZoom(float InZoom)
        {
            PlayerOriginalZoom = InZoom;
        }

        public void ResetCameraToPlayer()
        {
            if (CurrentCamera)
            {
                CurrentCamera.transform.DORewind();
            }

            bUseOverrideFollowTransform = false;
            OverrideFollowTransform = null;
            CameraOffset = new Vector2(0, 0);
            if (CameraRoot && CameraRootParent)
            {
                //CameraRoot.transform.parent = CameraRootParent.transform;
                CameraRootRelativeLocation = OriginalCameraRootRelativeLocation;
                CameraRoot.transform.position = CameraRootParent.transform.TransformPoint(CameraRootRelativeLocation);
                CurrentPriority = -1;
                bUsePositionOffset = true;
                bUseDefaultRotation = false;

                if (FollowerComponent)
                {
                    FollowerComponent.OnTargetReachedDelegate += HandleTargetReached;
                    FollowerComponent.InterpSpeed = FollowerComponent.OriginalInterpSpeed;
                    FollowerComponent.RotationInterpSpeed = FollowerComponent.OriginalRotationSpeed;
                }

                SetZoomAmount(PlayerOriginalZoom);
            }

            
        }

        void HandleTargetReached(SimpleFollower InFollower, Vector3 InPosition)
        {
            InFollower.OnTargetReachedDelegate -= HandleTargetReached;
            if (bOriginalClampToEdges)
            {
                bClampToEdges = true;
            }
        }

        public void SetCameraRoot(Transform newRoot)
        {
            if (newRoot)
            {
                CameraRootParent = newRoot.gameObject;
                ResetCameraToPlayer();
            }
        }

        public void MoveCameraRoot(Transform InTransform, int Priority = -1, bool bUseWorldPosition = true, bool bInUseDefaultRotation = false, bool bInUseOverrideFollow = false)
        {
            if (!InTransform)
            {
                return;
            }

            // Going from the player, so set the zoom
            if (TransformToFollow == CameraRootParent && InTransform != null)
            {
                PlayerOriginalZoom = CurrentZoomAmount;
            }

            TransformToFollow = InTransform;
            OverrideFollowTransform = InTransform;
            bUseOverrideFollowTransform = bInUseOverrideFollow;
            //CameraRoot.transform.parent = InTransform;

            if (!bUseWorldPosition)
            {
                CameraRootRelativeLocation = new Vector3(0, 0, 0);
                CameraRoot.transform.position = InTransform.transform.position;
                bUsePositionOffset = false;
            }

            if (Priority == -1)
            {

            }
            else if (Priority > CurrentPriority)
            {
                CurrentPriority = Priority;
            }

            bUseDefaultRotation = bInUseDefaultRotation;
            bClampToEdges = false;
        }

        public void ZoomInTick()
        {
            ZoomTick(true);
        }

        public void ZoomOutTick()
        {
            ZoomTick(false);
        }

        public void ZoomTick(bool bZoomIn)
        {
            // Check to see if the player mouse is above UI first. Do not scroll if above UI.
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            int ZoomFactor = bZoomIn ? -1 : 1;
            float TargetZoom = Mathf.Min(CurrentZoomAmount + ZoomFactor * ZoomTickInterval, MaxPlayerZoomAmount);

            if (TargetZoom < MinZoomAmount || TargetZoom > MaxZoomAmount)
            {
                ClampZoom();
            }
            else
            {
                SetZoomAmount(TargetZoom);
            }

            OnZoomTickDelegate?.Invoke(TargetZoom);
        }

        void ClampZoom()
        {
            SetZoomAmount(Mathf.Clamp(CurrentZoomAmount, MinZoomAmount, MaxZoomAmount));
        }

        public void LockZoom(bool bLocked)
        {
            bLockZoom = bLocked;
        }

        public void SetZoomAmount(float InZoomAmount)
        {
            if (!bLockZoom && CameraTargetGameObject)
            {
                CameraTargetGameObject.transform.localPosition = ZoomDirectionLocal * InZoomAmount;
                CurrentZoomAmount = InZoomAmount;
            }
        }

        int LastUserRotationDirection = 0;
        public Vector3 UserRotation;

        // Update is called once per frame
        void Update()
        {
            if(!CameraRoot || !CameraRootParent)
            {
                return;
            }

            if (OverrideFollowTransform && bUseOverrideFollowTransform)
            {
                CameraRootRelativeLocation = OverrideFollowTransform.localPosition;
            }

            CameraRoot.transform.position = CameraRootParent.transform.TransformPoint(CameraRootRelativeLocation);

            //Vector3 NewRotation = (Quaternion.Euler(UserRotation)).eulerAngles; //(OriginalRootWorldRotation * Quaternion.Euler(UserRotation)).eulerAngles;
            if (CameraRoot)
            {
                Quaternion QuatRot = Quaternion.Euler(UserRotation);
                // Compare camera root rotation with the new user rotation to get direction.
                if (CameraRoot.transform.rotation == QuatRot)
                {
                    LastUserRotationDirection = 0;
                }
                else if(Vector3.Dot(UserRotation + CameraRoot.transform.rotation.eulerAngles, CameraRoot.transform.right) > 0)
                {
                    LastUserRotationDirection = 1;
                }
                else
                {
                    LastUserRotationDirection = -1;
                }

                CameraRoot.transform.rotation = QuatRot;
            }

            // Yiming: Jank fix for cursor state being set to Locked EVEN THOUGH Cursor.visible is true when loading from tutorial to open world cut scene. 
            Cursor.lockState = Cursor.visible? CursorLockMode.None: CursorLockMode.Locked;

            Vector3 NewLocation;
            if (VirtualCamera == null || BaseGameManager.Get == null || BaseGameManager.Get.CinemachineBrain.ActiveVirtualCamera == null || BaseGameManager.Get.CinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject != VirtualCamera.VirtualCameraGameObject)
            {
                if (!Cursor.visible && (BaseGameManager.Get == null || BaseGameManager.Get.CinemachineBrain == null || BaseGameManager.Get.CinemachineBrain.ActiveVirtualCamera == null))
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    SetAimX(0);
                    SetAimY(0);
                    
                    NewLocation = bUsePositionOffset ? Vector3.Lerp(MinPositionOffset, MaxPositionOffset, 1) : new Vector3(0, 0, 0);
                    CameraRoot.transform.position += NewLocation;
                }
                return;
            }

            // Make these into real inputs later instead of hard coding
            if (bMouseControlsCamera || (IsMouseRotating))
            {
                if (bCanRotate)
                {
                    float YawMovement = AimPosition.x * CameraSensitivity;
                    float PitchMovement = -AimPosition.y * CameraSensitivity;

                    if(bInverseMouseX)
                    {
                        YawMovement = -YawMovement;
                    }

                    if(bInverseMouseY)
                    {
                        PitchMovement = -PitchMovement;
                    }

                    // Bad use cursor manager so we can manage instances of this.
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;

                    UserRotation.x = Mathf.Clamp((UserRotation.x + PitchMovement), MinRotationAmount, MaxRotationAmount);
                    UserRotation.y += YawMovement;// Mathf.Clamp(UserRotation.y + YawMovement, -65.0f, 65.0f);

                    FollowerComponent.InterpSpeed = mouseControlsCameraInterpSpeed;
                    FollowerComponent.RotationInterpSpeed = mouseControlsCameraRotationInterpSpeed;
                }
            }
            else
            {
                if (Cursor.visible == false)
                {
                    FollowerComponent.InterpSpeed = FollowerComponent.OriginalInterpSpeed;
                    FollowerComponent.RotationInterpSpeed = FollowerComponent.OriginalRotationSpeed;

                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
            }

            float ZoomPercentage = (CurrentZoomAmount - MinZoomAmount) / (MaxZoomAmount - MinZoomAmount);

            //if (!bUseDefaultRotation)
            //{
            //    NewRotation.x = Mathf.Lerp(MinRotationAmount, MaxRotationAmount, ZoomPercentage / MaxRotationClamp);
            //}
            //else
            //{
            //    NewRotation.x = MaxRotationAmount;
            //}

            NewLocation = bUsePositionOffset ? Vector3.Lerp(MinPositionOffset, MaxPositionOffset, ZoomPercentage) : new Vector3(0, 0, 0);
            CameraRoot.transform.position += ((CameraRoot.transform.right * (NewLocation.x))+ (CameraRoot.transform.up * ((NewLocation.y + CameraOffset.y))));

            if (TargetFOV != VirtualCamera.m_Lens.FieldOfView)
            {
                VirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(VirtualCamera.m_Lens.FieldOfView, TargetFOV, Time.deltaTime * 2.5f);
                if (Mathf.Abs((TargetFOV - VirtualCamera.m_Lens.FieldOfView)) <= 0.05f)
                {
                    VirtualCamera.m_Lens.FieldOfView = TargetFOV;
                }
            }

            if (bClampToEdges)
            {
                ClampMovement();
            }

            // Clear items requesting mouse lock
            for (int i = ItemsRequestingNoMouseRotate.Count - 1; i >= 0; --i)
            {
                if(ItemsRequestingNoMouseRotate[i])
                {
                    if(ItemsRequestingNoMouseRotate[i].activeInHierarchy == false)
                    {
                        RemoveNoMouseRequest(ItemsRequestingNoMouseRotate[i]);
                    }
                }
                else
                {
                    ItemsRequestingNoMouseRotate.RemoveAt(i);
                    RemoveNoMouseRequest(null);
                }
            }

            //if (bClampToObstacleBounds)
            //{
            //    ClampToObstacleBounds();
            //}

            //Bug: heavy attack charge up screenshake doesn't play in non F5 mode
            //BaseScreenShakeManager.LateScreenShake();
        }

        public int GetUserRotationDirection()
        {
            return LastUserRotationDirection;
        }

        void ClampMovement()
        {
            if (BaseGameManager.Get.CinemachineBrain.ActiveVirtualCamera == null || BaseGameManager.Get.CinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject != VirtualCamera.VirtualCameraGameObject)
            {
                return;
            }

            if (!bClampToEdges || !FollowerComponent)
            {
                return;
            }
        }


        public void SetAimX(float x)
        {
            AimPosition.x = x;
        }


        public void SetAimY(float y)
        {
            AimPosition.y = y;
        }

        private void RemoveNullRequests()
        {
            for (int i = ItemsRequestingNoMouseRotate.Count - 1; i >= 0; --i)
            {
                if (!ItemsRequestingNoMouseRotate[i])
                {
                    ItemsRequestingNoMouseRotate.RemoveAt(i);
                }
            }
            if (ItemsRequestingNoMouseRotate.Count == 0)
            {
                SetMouseRotating(true);
            }
        }    

        public void AddNoMouseRequest(GameObject InGameObject, bool bShouldSetCursorImmediately = false)
        {
            RemoveNullRequests();
            // Check for duplicates
            if(ItemsRequestingNoMouseRotate.Contains(InGameObject))
            {
                if(bShouldSetCursorImmediately)
                {
                    SetCursorImmediately(false);
                }
                return;
            }

            ItemsRequestingNoMouseRotate.Add(InGameObject);

            SetMouseRotating(false, bShouldSetCursorImmediately);
        }

        public void RemoveNoMouseRequest(GameObject InGameObject, bool bShouldSetCursorImmediately = false)
        {
            for(int i = ItemsRequestingNoMouseRotate.Count - 1; i >= 0; --i)
            {
                if(ItemsRequestingNoMouseRotate[i] == null || ItemsRequestingNoMouseRotate[i] == InGameObject)
                {
                    ItemsRequestingNoMouseRotate.RemoveAt(i);
                }
            }

            if(ItemsRequestingNoMouseRotate.Count == 0)
            {
                SetMouseRotating(true, bShouldSetCursorImmediately);
            }
        }

        public void ClearNoMouseRequests()
        {
            ItemsRequestingNoMouseRotate.Clear();
            SetMouseRotating(true);
        }

        private void SetMouseRotating(bool IsPressed, bool bShouldSetCursorImmediately = false)
        {
            IsMouseRotating = IsPressed;

            if(bShouldSetCursorImmediately)
            {
                SetCursorImmediately(IsPressed);
            }

            OnNoMouseDelegate?.Invoke(IsMouseRotating);
        }

        void SetCursorImmediately(bool bPressed)
        {
            if (bPressed)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
        public bool IsMouseShown()
        {
            return !IsMouseRotating;
        }

        // Things that don't want the camera to be controlled by the mouse
        public List<GameObject> ItemsRequestingNoMouseRotate = new List<GameObject>();


        private void OnDestroy()
        {
            ClearNoMouseRequests();
            
            if (HRNetworkManager.Get)
            {
                HRNetworkManager.Get.ClientDisconnectedDelegate -= OnClientDisconnected;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}