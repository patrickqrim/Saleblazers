using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseIKManager : MonoBehaviour
{
    public Animancer.AnimancerComponent OwningAnimancerComponent;
    public RootMotion.SolverManager FinalIKRef;
    public RootMotion.FinalIK.GrounderFBBIK FullBodyGrounderIKRef;
    public BaseFootstepListener FootstepListener;

    float LookAtWeight = 0.0f;

    float LookAtBlend = 0.0f;

    Vector3 PreviousLookAtLocation;
    Vector3 LerpLookAtLocation;

    // Start is called before the first frame update
    void Start()
    {
        if (FootstepListener)
        {
            FootstepListener.FootstepLandDelegate += UpdateFootstep;
        }
    }

    public void UpdateFootstep(BaseFootstepListener listener, BaseFootstepListener.FootstepFoot footstepFoot)
    {
        if (FullBodyGrounderIKRef)
        {
            FullBodyGrounderIKRef.UpdateFootstep((int)footstepFoot);
        }
    }

    bool bUpdateEnabled = false;

    public bool bUsingCane = false;
    public float CaneLength = 0.8f;
    public float CaneForwardOffset = 0.2f;
    public float CaneSidewaysOffset = 0.0f;
    public float MinCane = 0.1f;
    public float MaxCane = 0.5f;

    // Allowed meaning if IK is inherently enabled, but disabled for special occasions like ragdoll.
    bool bIKAllowed = false;

    public bool IsIKAllowed()
    {
        return bIKAllowed;
    }

    public void SetBipedIKEnabled(bool bInEnabled, bool bChangeIKAllowed = false)
    {
        if (FinalIKRef)
        {
            FinalIKRef.enabled = bInEnabled;
        }

        if (FullBodyGrounderIKRef)
        {
            FullBodyGrounderIKRef.enabled = bInEnabled;
        }

        if (bChangeIKAllowed)
        {
            bIKAllowed = bInEnabled;
        }
    }
    private void OnAnimatorIK(int LayerIndex)
    {
        if(!OwningAnimancerComponent)
        {
            return;
        }

        if (bUpdateEnabled)
        {
            bool bNeedsToRun = false;

            if ((bIsLookingAtTarget && LookAtTarget) || bIsLookingAtLocation)
            {
                if (Mathf.Approximately(LookAtWeight, 1.0f))
                {
                    LookAtWeight = 1.0f;
                }
                else
                {
                    LookAtWeight = Mathf.Lerp(LookAtWeight, 1.0f, Time.deltaTime * 12.0f);
                    bNeedsToRun = true;
                }
            }
            else
            {
                if (Mathf.Approximately(LookAtWeight, 0.0f))
                {
                    LookAtWeight = 0.0f;
                }
                else
                {
                    LookAtWeight = Mathf.Lerp(LookAtWeight, 0.0f, Time.deltaTime * 5.0f);
                    bNeedsToRun = true;
                }
            }

            Vector3 FinalLookAt;

            if (bIsLookingAtTarget && LookAtTarget)
            {
                FinalLookAt = LookAtTarget != null ? LookAtTarget.position : PreviousLookAtLocation;
            }
            else if (bIsLookingAtLocation)
            {
                FinalLookAt = LookAtLocation;
            }
            else
            {
                FinalLookAt = PreviousLookAtLocation;
            }

            // Interp look at locations
            if (Mathf.Approximately(LookAtBlend, 1.0f))
            {
                LerpLookAtLocation = FinalLookAt;
                LookAtBlend = 1.0f;
            }
            else
            {
                LookAtBlend = Mathf.Lerp(LookAtBlend, 1.0f, Time.deltaTime * 48.0f);
                LerpLookAtLocation = Vector3.Lerp(PreviousLookAtLocation, FinalLookAt, LookAtBlend);
                bNeedsToRun = true;
            }

            if (!bNeedsToRun)
            {
                bUpdateEnabled = false;

                if (LookAtWeight == 0.0f)
                {
                    OwningAnimancerComponent.Layers[0].ApplyAnimatorIK = false;
                }
            }
        }

        OwningAnimancerComponent.Animator.SetLookAtWeight(LookAtWeight, 0.25f, 0.9f, 0.9f, 0.55f);
        //Debug.Log("LookAtBlend: " + LookAtBlend);

        if (LookAtTarget || bIsLookingAtLocation)
        {
            OwningAnimancerComponent.Animator.SetLookAtPosition(LookAtBlend == 1.0f ? (LookAtTarget != null ? LookAtTarget.transform.position : LerpLookAtLocation) : LerpLookAtLocation);
            //Debug.Log("PrevLookAt: " + PreviousLookAtLocation + ", CurrentLookAt: " + LerpLookAtLocation);
        }
        else
        {
            OwningAnimancerComponent.Animator.SetLookAtPosition(LerpLookAtLocation);
            //Debug.Log("PrevLookAt: " + PreviousLookAtLocation + ", CurrentLookAt: " + LerpLookAtLocation);
        }

        if(bUsingCane)
        {

            OwningAnimancerComponent.Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
            OwningAnimancerComponent.Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

            Vector3 OGHandPosition = OwningAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            // The plane is the character's right vector
            Plane test = new Plane(OwningAnimancerComponent.transform.right, OGHandPosition + OwningAnimancerComponent.transform.right * CaneSidewaysOffset);
            Vector3 CaneHandLocation = test.ClosestPointOnPlane(OwningAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.RightFoot).position);

            // Move the hand upwards of how long the cane is.
            CaneHandLocation += new Vector3(0.0f, CaneLength, 0.0f);

            // Move the hand forward according to the plane.
            CaneHandLocation += OwningAnimancerComponent.transform.forward * CaneForwardOffset;
            
            Vector3 MinPoint = OwningAnimancerComponent.transform.position + OwningAnimancerComponent.transform.forward * MinCane;
            Vector3 MaxPoint = OwningAnimancerComponent.transform.position + OwningAnimancerComponent.transform.forward * MaxCane;

            float CaneHandLocationDistance = Vector2.Distance(new Vector2(CaneHandLocation.x, CaneHandLocation.z), new Vector2(MinPoint.x, MinPoint.z));

            // Now constrain the cane so it cannot go this amoutn in front of the base player mesh.
            if (Vector3.Dot(CaneHandLocation - MinPoint, MaxPoint - MinPoint) < 0)
            {
                //If cane hand went behind the min point clamp to min
                Vector3 ClampedLocation = test.ClosestPointOnPlane(MinPoint);
                CaneHandLocation = new Vector3(ClampedLocation.x, CaneHandLocation.y, ClampedLocation.z);

            }
            else if (Vector3.Dot(MaxPoint - CaneHandLocation, MaxPoint - MinPoint) < 0)
            {
                // If cane went in front of the max point clamp to max
                Vector3 ClampedLocation = test.ClosestPointOnPlane(MaxPoint);
                CaneHandLocation = new Vector3(ClampedLocation.x, CaneHandLocation.y, ClampedLocation.z);
            }

            OwningAnimancerComponent.Animator.SetIKPosition(AvatarIKGoal.LeftHand,
                CaneHandLocation);
            
            OwningAnimancerComponent.Animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.LookRotation(OwningAnimancerComponent.transform.forward));
        }

        //OwningAnimancerComponent.Animator.SetLookAtPosition(LookAtBlend == 1.0f ? LookAtTarget.transform.position : LerpLookAtLocation);
    }

    bool bIsLookingAtTarget = false;
    Transform LookAtTarget;

    bool bIsLookingAtLocation = false;
    Vector3 LookAtLocation;

    public void ClearLookAt()
    {
        if (LookAtTarget)
        {
            SetLookAtTarget(null);
        }

        if(bIsLookingAtLocation)
        {
            bIsLookingAtLocation = false;
        }
    }

    public void SetLookAtLocation(Vector3 InLocation)
    {
        if (bIsLookingAtLocation)
        {
            PreviousLookAtLocation = LerpLookAtLocation;
        }

        LookAtLocation = InLocation;
        bIsLookingAtLocation = true;

        LookAtBlend = 0.0f;
        bUpdateEnabled = true;
    }

    Transform TempTransform;

    public void SetLookAtTarget(Transform InTarget)
    {
        if(InTarget == LookAtTarget)
        {
            return;
        }

        if(InTarget && (InTarget.CompareTag("Player") || InTarget.CompareTag("AI")))
        {
            HeroPlayerCharacter PC = InTarget.GetComponent<HeroPlayerCharacter>();
            if(PC)
            {
                TempTransform = PC.PlayerMesh?.PlayerRig?.AnimancerComponent?.Animator?.GetBoneTransform(HumanBodyBones.Head);

                if(TempTransform)
                {
                    InTarget = TempTransform;
                }
            }
        }

        if (LookAtTarget)
        {
            PreviousLookAtLocation = LookAtBlend == 1.0f ? LookAtTarget.transform.position : LerpLookAtLocation;
        }

        LookAtBlend = 0.0f;

        if (InTarget)
        {
            bIsLookingAtTarget = true;
            LookAtTarget = InTarget;
            OwningAnimancerComponent.Layers[0].ApplyAnimatorIK = true;
            bUpdateEnabled = true;
        }
        else
        {
            bIsLookingAtTarget = false;
            LookAtTarget = null;
            // Todo: fade weight to 0 and make sure hands/feet aren't ik'd before turning it off fully

            if(!bIsLookingAtLocation)
            {
                bUpdateEnabled = true;
            }
        }
    }
}
