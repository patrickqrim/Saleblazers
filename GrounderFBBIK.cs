using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK
{

    /// <summary>
    /// Grounding for FBBIK characters.
    /// </summary>
    [HelpURL("https://www.youtube.com/watch?v=9MiZiaJorws&index=6&list=PLVxSIA1OaTOu8Nos3CalXbJ2DrKnntMv6")]
    [AddComponentMenu("Scripts/RootMotion.FinalIK/Grounder/Grounder Full Body Biped")]
    public class GrounderFBBIK : Grounder
    {

        // Open a video tutorial video
        [ContextMenu("TUTORIAL VIDEO")]
        void OpenTutorial()
        {
            Application.OpenURL("https://www.youtube.com/watch?v=9MiZiaJorws&index=6&list=PLVxSIA1OaTOu8Nos3CalXbJ2DrKnntMv6");
        }

        // Open the User Manual URL
        [ContextMenu("User Manual")]
        protected override void OpenUserManual()
        {
            Application.OpenURL("http://www.root-motion.com/finalikdox/html/page9.html");
        }

        // Open the Script Reference URL
        [ContextMenu("Scrpt Reference")]
        protected override void OpenScriptReference()
        {
            Application.OpenURL("http://www.root-motion.com/finalikdox/html/class_root_motion_1_1_final_i_k_1_1_grounder_f_b_b_i_k.html");
        }

        #region Main Interface

        /// <summary>
        /// Contains the bending weights for an effector.
        /// </summary>
        [System.Serializable]
        public class SpineEffector
        {
            /// <summary>
            /// The type of the effector.
            /// </summary>
            [Tooltip("The type of the effector.")]
            public FullBodyBipedEffector effectorType;
            /// <summary>
            /// The weight of horizontal bend offset towards the slope..
            /// </summary>
            [Tooltip("The weight of horizontal bend offset towards the slope.")]
            public float horizontalWeight = 1f;
            /// <summary>
            /// The vertical bend offset weight.
            /// </summary>
            [Tooltip("The vertical bend offset weight.")]
            public float verticalWeight;

            public SpineEffector() { }

            /// <summary>
            /// Initializes a new instance of the <see cref="RootMotion.FinalIK.GrounderFBBIK+SpineEffector"/> class.
            /// </summary>
            /// <param name="effectorType">Effector type.</param>
            /// <param name="horizontalWeight">Horizontal weight.</param>
            /// <param name="verticalWeight">Vertical weight.</param>
            public SpineEffector(FullBodyBipedEffector effectorType, float horizontalWeight, float verticalWeight)
            {
                this.effectorType = effectorType;
                this.horizontalWeight = horizontalWeight;
                this.verticalWeight = verticalWeight;
            }
        }

        /// <summary>
        /// Reference to the FBBIK componet.
        /// </summary>
        [Tooltip("Reference to the FBBIK componet.")]
        public FullBodyBipedIK ik;
        /// <summary>
        /// The amount of spine bending towards upward slopes.
        /// </summary>
        [Tooltip("The amount of spine bending towards upward slopes.")]
        public float spineBend = 2f;
        /// <summary>
        /// The interpolation speed of spine bending.
        /// </summary>
        [Tooltip("The interpolation speed of spine bending.")]
        public float spineSpeed = 3f;
        /// <summary>
        /// The spine bending effectors.
        /// </summary>
        public SpineEffector[] spine = new SpineEffector[0];

        #endregion Main Interface

        public override void ResetPosition()
        {
            solver.Reset();
            spineOffset = Vector3.zero;
        }

        private Transform[] feet = new Transform[2];
        private Vector3 spineOffset;
        private bool firstSolve;

        //#ASBeginChange

        // POSITION VARIABLES
        private Vector3[] savedLegPositions = new Vector3[2];
        private Vector3[] targetLegPositions = new Vector3[2];
        private float leftFootOffset;
        private float rightFootOffset;
        public bool leftDown, rightDown;
        private bool bBegin;
        private bool[] bSave;
        private const float OFFSET_THRESHOLD = 0.3f;
        private const float VELOCITY_TERM = 0.02f;

        // ROTATION VARIABLES
        private Quaternion[] savedLegRotations = new Quaternion[2];
        //private Quaternion[] targetLegRotations = new Quaternion[2];
        //private float leftRotation, rightRotation;
        //private float leftROffset, rightROffset;
        private const float ROTATION_THRESHOLD = 60.0f;
        private const float R_VELOCITY_TERM = 2.4f;
        private const float SAVE_ROTATION_THRESHOLD = 3.0f;

        private const float SLERP_RATE = 7.0f;
        public bool leftRDown, rightRDown;
        private float lastTime, deltaTime;
        private bool bRotationBegin;

        //#ASEndChange


        // Can we initiate the Grounding?
        private bool IsReadyToInitiate()
        {
            if (ik == null) return false;
            if (!ik.solver.initiated) return false;
            return true;
        }

        // Initiate once we have a FBBIK component
        void Update()
        {
            firstSolve = true;
            weight = Mathf.Clamp(weight, 0f, 1f);
            if (weight <= 0f) return;

            if (initiated) return;
            if (!IsReadyToInitiate()) return;

            Initiate();
        }

        void FixedUpdate()
        {
            firstSolve = true;
        }

        void LateUpdate()
        {
            firstSolve = true;
        }

        private void Initiate()
        {
            // Set maintainRotationWeight to 1 for both dadlimbs so their rotation will be maintained as animated
            ik.solver.leftLegMapping.maintainRotationWeight = 1f;
            ik.solver.rightLegMapping.maintainRotationWeight = 1f;

            // Gathering both foot bones from the FBBIK
            feet = new Transform[2];
            feet[0] = ik.solver.leftFootEffector.bone;
            feet[1] = ik.solver.rightFootEffector.bone;

            // Add to the FBBIK OnPreUpdate delegate to know when it solves
            ik.solver.OnPreUpdate += OnSolverUpdate;
            ik.solver.OnPostUpdate += OnPostSolverUpdate;

            // Initiate Grounding
            solver.Initiate(ik.references.root, feet);
            solver.Update();

            initiated = true;

            //#ASBeginChange

            // Initialize leg position variables
            savedLegPositions[0] = solver.legs[0].transform.position;
            savedLegPositions[1] = solver.legs[1].transform.position;
            leftDown = true;
            rightDown = true;
            targetLegPositions[0] = solver.legs[0].transform.position;
            targetLegPositions[1] = solver.legs[1].transform.position;
            bBegin = true;
            bSave = new bool[2];

            // Initialize leg rotation variables
            savedLegRotations[0] = solver.legs[0].transform.rotation;
            savedLegRotations[1] = solver.legs[1].transform.rotation;
            leftRDown = true;
            rightRDown = true;
            //targetLegRotations[0] = solver.legs[0].transform.rotation;
            //targetLegRotations[1] = solver.legs[1].transform.rotation;
            //leftRotation = solver.legs[0].transform.rotation.eulerAngles.y;
            //rightRotation = solver.legs[1].transform.rotation.eulerAngles.y;
            lastTime = Time.time;
            bRotationBegin = true;

            //#ASEndChange
        }

        public void UpdateFootstep(int footstepIdx)
        {
            // Null check for ai that doesn't have solver. This needs to not call at all for pawns that do not need footstep logic. This still throws null during load.
            // Do we need this for all characters? OPTIMIZE 204
            if (solver == null || solver.legs == null || footstepIdx >= solver.legs.Length || 
                solver.legs[footstepIdx] == null || footstepIdx < 0 || footstepIdx >= solver.legs.Length) return;

            bSave[footstepIdx] = true;
            if (footstepIdx == 0)
            {
                leftDown = true;
            }
            else if (footstepIdx == 1)
            {
                rightDown = true;
            }

            bBegin = false;
        }

        // Called before updating the main IK solver
        private void OnSolverUpdate()
        {
            if (!firstSolve) return;
            firstSolve = false;
            if (!enabled) return;
            if (weight <= 0f) return;

            if (OnPreGrounder != null) OnPreGrounder();


            //#ASBeginChange: Save foot position and manually set position back to saved position while distance threshold is not crossed

            // Set leg positions to target positions
            if (leftDown && !bBegin)
            {
                solver.legs[0].SetFootPosition(targetLegPositions[0]);
            }
            if (rightDown && !bBegin)
            {
                solver.legs[1].SetFootPosition(targetLegPositions[1]);
            }

            targetLegPositions[0] = solver.legs[0].transform.position;
            targetLegPositions[1] = solver.legs[1].transform.position;

            if (bSave[0])
            {
                savedLegPositions[0] = targetLegPositions[0];
                bSave[0] = false;
            }
            if (bSave[1])
            {
                savedLegPositions[1] = targetLegPositions[1];
                bSave[1] = false;
            }

            leftFootOffset = Vector3.Distance(savedLegPositions[0], targetLegPositions[0]);
            rightFootOffset = Vector3.Distance(savedLegPositions[1], targetLegPositions[1]);

            // Subtract from threshold for higher velocities
            float frameThreshold = OFFSET_THRESHOLD - VELOCITY_TERM * Mathf.Max(solver.legs[0].velocity.magnitude, solver.legs[1].velocity.magnitude);

            // Checks if position thresholds are crossed
            if (leftFootOffset > frameThreshold && leftDown)
            {
                leftDown = false;
                solver.legs[0].SetFootPosition(targetLegPositions[0]);
            }
            else if (leftDown && !bBegin)
            {
                solver.legs[0].SetFootPosition(savedLegPositions[0]);
            }

            if (rightFootOffset > frameThreshold && rightDown)
            {
                rightDown = false;
                solver.legs[1].SetFootPosition(targetLegPositions[1]);
            }
            else if (rightDown && !bBegin)
            {
                solver.legs[1].SetFootPosition(savedLegPositions[1]);
            }


            // BEGIN PLANT FOOT ROTATION CODE
            if (bRotationBegin)
            {
                savedLegRotations[0] = solver.legs[0].transform.rotation;
                savedLegRotations[1] = solver.legs[1].transform.rotation;
                //targetLegRotations[0] = solver.legs[0].transform.rotation;
                //targetLegRotations[1] = solver.legs[1].transform.rotation;
                //leftRotation = solver.legs[0].transform.rotation.eulerAngles.y;
                //rightRotation = solver.legs[1].transform.rotation.eulerAngles.y;
                lastTime = Time.time;
                bRotationBegin = false;
            }

            // Calculate target rotation

            // ATTEMPT 2
            //leftROffset = solver.legs[0].transform.rotation.eulerAngles.y - leftRotation;
            //rightROffset = solver.legs[1].transform.rotation.eulerAngles.y - rightRotation;
            //leftRotation = solver.legs[0].transform.rotation.eulerAngles.y;
            //rightRotation = solver.legs[1].transform.rotation.eulerAngles.y;
            //Vector3 leftEuler = targetLegRotations[0].eulerAngles;
            //Vector3 rightEuler = targetLegRotations[1].eulerAngles;
            //leftEuler.y = (leftEuler.y + leftROffset) >= 0 ? (leftEuler.y + leftROffset) % 360 : (leftEuler.y + leftROffset) + 360;
            //rightEuler.y = (rightEuler.y + rightROffset) >= 0 ? (rightEuler.y + rightROffset) % 360 : (rightEuler.y + rightROffset) + 360;
            //targetLegRotations[0] = Quaternion.Euler(leftEuler);
            //targetLegRotations[1] = Quaternion.Euler(rightEuler);
            // ATTEMPT 1
            //offsetRotations[0] = solver.legs[0].transform.rotation * Quaternion.Inverse(leftRotation);
            //offsetRotations[1] = solver.legs[1].transform.rotation * Quaternion.Inverse(rightRotation);
            //float testAngle = System.Math.Abs(solver.legs[0].transform.rotation.eulerAngles.y - leftRotation.eulerAngles.y);
            //testAngle = testAngle > 180 ? 360 - testAngle : testAngle;
            //Debug.Log(testAngle.ToString());
            //targetLegRotations[0] = offsetRotations[0] * targetLegRotations[0];
            //targetLegRotations[1] = offsetRotations[1] * targetLegRotations[1];

            //Debug.Log(targetLegRotations[0].eulerAngles.y.ToString() + "   " + solver.legs[0].transform.rotation.eulerAngles.y.ToString());

            deltaTime = Time.time - lastTime;
            lastTime = Time.time;

            float leftAngle = System.Math.Abs(savedLegRotations[0].eulerAngles.y - solver.legs[0].transform.rotation.eulerAngles.y);
            leftAngle = leftAngle > 180 ? 360 - leftAngle : leftAngle;
            //Debug.Log(leftAngle.ToString());
            float rightAngle = System.Math.Abs(savedLegRotations[1].eulerAngles.y - solver.legs[1].transform.rotation.eulerAngles.y);
            rightAngle = rightAngle > 180 ? 360 - rightAngle : rightAngle;


            // Save rotation if BELOW a threshold
            //if (leftAngle < SAVE_ROTATION_THRESHOLD && !leftRDown)
            //{
            //    leftRDown = true;
            //    savedLegRotations[0] = solver.legs[0].transform.rotation;
            //}
            //if (rightAngle < SAVE_ROTATION_THRESHOLD && !rightRDown)
            //{
            //    rightRDown = true;
            //    savedLegRotations[1] = solver.legs[1].transform.rotation;
            //}

            // Per frame rotation threshold
            float frameRThreshold = ROTATION_THRESHOLD - R_VELOCITY_TERM * Mathf.Max(solver.legs[0].velocity.magnitude, solver.legs[1].velocity.magnitude);

            // Check if rotation thresholds are crossed
            if (leftAngle > frameRThreshold)
            {
                leftRDown = false;
            }
            if (!leftRDown)
            {
                savedLegRotations[0] = Quaternion.Slerp(savedLegRotations[0], solver.legs[0].transform.rotation, deltaTime * SLERP_RATE);
                if (leftAngle < SAVE_ROTATION_THRESHOLD)  // snap and save if < epsilon
                {
                    savedLegRotations[0] = solver.legs[0].transform.rotation;
                    leftRDown = true;
                }
            }
            // Only take y component of saved rotation
            Vector3 leftEuler = solver.legs[0].transform.rotation.eulerAngles;
            leftEuler.y = savedLegRotations[0].eulerAngles.y;
            solver.legs[0].transform.rotation = Quaternion.Euler(leftEuler);

            if (rightAngle > frameRThreshold)
            {
                rightRDown = false;
            }
            if (!rightRDown)
            {
                savedLegRotations[1] = Quaternion.Slerp(savedLegRotations[1], solver.legs[1].transform.rotation, deltaTime * SLERP_RATE);
                if (rightAngle < SAVE_ROTATION_THRESHOLD)  // snap and save if < epsilon
                {
                    savedLegRotations[1] = solver.legs[1].transform.rotation;
                    rightRDown = true;
                }
            }
            // Only take y component of saved rotation
            Vector3 rightEuler = solver.legs[1].transform.rotation.eulerAngles;
            rightEuler.y = savedLegRotations[1].eulerAngles.y;
            solver.legs[1].transform.rotation = Quaternion.Euler(rightEuler);

            //if (rightAngle > ROTATION_THRESHOLD && rightRDown)
            //{
            //    rightRDown = false;
            //    targetLegRotations[1] = solver.legs[1].transform.rotation;
            //}
            //else if (rightRDown)
            //{
            //    solver.legs[1].transform.rotation = savedLegRotations[1];
            //}

            //Debug.Log("BEFORE " + solver.legs[0].transform.rotation.eulerAngles.y.ToString());

            // UPDATE call actually updates the grounding solver
            solver.Update();

            //Debug.Log(targetLegRotations[0].eulerAngles.y.ToString() + "   " + solver.legs[0].transform.rotation.eulerAngles.y.ToString());

            //#ASEndChange


            // Set pelvis position
            ik.references.pelvis.position += solver.pelvis.IKOffset * weight;

            // Set effector positionOffsets for the feet
            SetLegIK(ik.solver.leftFootEffector, solver.legs[0]);
            SetLegIK(ik.solver.rightFootEffector, solver.legs[1]);

            // Bending the spine
            if (spineBend != 0f)
            {
                spineSpeed = Mathf.Clamp(spineSpeed, 0f, spineSpeed);

                Vector3 spineOffseTarget = GetSpineOffsetTarget() * weight;
                spineOffset = Vector3.Lerp(spineOffset, spineOffseTarget * spineBend, Time.deltaTime * spineSpeed);
                Vector3 verticalOffset = ik.references.root.up * spineOffset.magnitude;

                for (int i = 0; i < spine.Length; i++)
                {
                    ik.solver.GetEffector(spine[i].effectorType).positionOffset += (spineOffset * spine[i].horizontalWeight) + (verticalOffset * spine[i].verticalWeight);
                }
            }


            if (OnPostGrounder != null) OnPostGrounder();

        }

        // Set the effector positionOffset for the foot
        private void SetLegIK(IKEffector effector, Grounding.Leg leg)
        {
            effector.positionOffset += (leg.IKPosition - effector.bone.position) * weight;

            effector.bone.rotation = Quaternion.Slerp(Quaternion.identity, leg.rotationOffset, weight) * effector.bone.rotation;
        }

        //#ASBeginChange: Draw the saved positions/target positions for debugging
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (targetLegPositions != null && targetLegPositions[0] != null)
            {
                Gizmos.DrawWireSphere(targetLegPositions[0], 0.1f);
            }
            Gizmos.color = Color.blue;
            if (targetLegPositions != null && targetLegPositions[1] != null)
            {
                Gizmos.DrawWireSphere(targetLegPositions[1], 0.1f);
            }
            Gizmos.color = Color.magenta;
            if (savedLegPositions != null && savedLegPositions[0] != null)
            {
                Gizmos.DrawWireSphere(savedLegPositions[0], 0.1f);
            }
            Gizmos.color = Color.cyan;
            if (savedLegPositions != null && savedLegPositions[1] != null)
            {
                Gizmos.DrawWireSphere(savedLegPositions[1], 0.1f);
            }
        }

        //#ASEndChange

        // Auto-assign ik
        void OnDrawGizmosSelected()
        {
            if (ik == null) ik = GetComponent<FullBodyBipedIK>();
            if (ik == null) ik = GetComponentInParent<FullBodyBipedIK>();
            if (ik == null) ik = GetComponentInChildren<FullBodyBipedIK>();
        }

        private void OnPostSolverUpdate()
        {
            if (OnPostIK != null) OnPostIK();
        }

        // Cleaning up the delegate
        void OnDestroy()
        {
            if (initiated && ik != null)
            {
                ik.solver.OnPreUpdate -= OnSolverUpdate;
                ik.solver.OnPostUpdate -= OnPostSolverUpdate;
            }
        }
    }
}
