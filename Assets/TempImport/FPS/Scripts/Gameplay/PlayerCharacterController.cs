using Unity.FPS.Game;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(CapsuleCollider), typeof(PlayerInputHandler), typeof(AudioSource))]
    public class PlayerCharacterController : NetworkBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the main camera used for the player")]
        public Camera PlayerCamera;
        public Transform PlayerCameraTransform;
        public Transform WeaponParentSocket;

        [Tooltip("Audio source for footsteps, jump, etc...")]
        public AudioSource AudioSource;

        [Header("General")]
        [Tooltip("Force applied downward when in the air")]
        public float GravityDownForce = 20f;

        [Tooltip("Physic layers checked to consider the player grounded")]
        public LayerMask GroundCheckLayers = -1;

        [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
        public float GroundCheckDistance = 0.05f;

        [Header("Movement")]
        [Tooltip("Max movement speed when grounded (when not sprinting)")]
        public float MaxSpeedOnGround = 10f;

        [Tooltip("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
        public float MovementSharpnessOnGround = 15;

        [Tooltip("Max movement speed when crouching")] [Range(0, 1)]
        public float MaxSpeedCrouchedRatio = 0.5f;

        [Tooltip("Max movement speed when not grounded")]
        public float MaxSpeedInAir = 10f;

        [Tooltip("Acceleration speed when in the air")]
        public float AccelerationSpeedInAir = 25f;

        [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
        public float SprintSpeedModifier = 2f;

        [Tooltip("Height at which the player dies instantly when falling off the map")]
        public float KillHeight = -50f;

        [Tooltip("Max slope angle the character can walk on")]
        public float SlopeLimit = 45f;

        [Header("Rotation")] [Tooltip("Rotation speed for moving the camera")]
        public float RotationSpeed = 200f;

        [Range(0.1f, 1f)] [Tooltip("Rotation speed multiplier when aiming")]
        public float AimingRotationMultiplier = 0.4f;

        [Header("Jump")] [Tooltip("Force applied upward when jumping")]
        public float JumpForce = 9f;

        [Header("Stance")] [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
        public float CameraHeightRatio = 0.9f;

        [Tooltip("Height of character when standing")]
        public float CapsuleHeightStanding = 2f;

        [Tooltip("Height of character when crouching")]
        public float CapsuleHeightCrouching = 1f;

        [Tooltip("Speed of crouching transitions")]
        public float CrouchingSharpness = 10f;

        [Header("Audio")] [Tooltip("Amount of footstep sounds played when moving one meter")]
        public float FootstepSfxFrequency = 1f;

        [Tooltip("Amount of footstep sounds played when moving one meter while sprinting")]
        public float FootstepSfxFrequencyWhileSprinting = 1f;

        [Tooltip("Sound played for footsteps")]
        public AudioClip FootstepSfx;

        [Tooltip("Sound played when jumping")] public AudioClip JumpSfx;
        [Tooltip("Sound played when landing")] public AudioClip LandSfx;

        [Tooltip("Sound played when taking damage froma fall")]
        public AudioClip FallDamageSfx;

        [Header("Fall Damage")]
        [Tooltip("Whether the player will recieve damage when hitting the ground at high speed")]
        public bool RecievesFallDamage;

        [Tooltip("Minimun fall speed for recieving fall damage")]
        public float MinSpeedForFallDamage = 10f;

        [Tooltip("Fall speed for recieving th emaximum amount of fall damage")]
        public float MaxSpeedForFallDamage = 30f;

        [Tooltip("Damage recieved when falling at the mimimum speed")]
        public float FallDamageAtMinSpeed = 10f;

        [Tooltip("Damage recieved when falling at the maximum speed")]
        public float FallDamageAtMaxSpeed = 50f;

        public UnityAction<bool> OnStanceChanged;

        public Vector3 CharacterVelocity { get; set; }
        public bool IsGrounded { get; private set; }
        public bool HasJumpedThisFrame { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsCrouching { get; private set; }

        public float RotationMultiplier
        {
            get
            {
                if (m_WeaponsManager.IsAiming)
                {
                    return AimingRotationMultiplier;
                }

                return 1f;
            }
        }

        Health m_Health;
        PlayerInputHandler m_InputHandler;
        CapsuleCollider m_CapsuleCollider;
        Rigidbody m_Rigidbody;
        PlayerWeaponsManager m_WeaponsManager;
        Actor m_Actor;
        Vector3 m_GroundNormal;
        Vector3 m_CharacterVelocity;
        Vector3 m_LatestImpactSpeed;
        float m_LastTimeJumped = 0f;
        float m_CameraVerticalAngle = 0f;
        float m_FootstepDistanceCounter;
        float m_TargetCharacterHeight;

        const float k_JumpGroundingPreventionTime = 0.2f;
        const float k_GroundCheckDistanceInAir = 0.07f;

        void Awake()
        {
            ActorsManager actorsManager = FindFirstObjectByType<ActorsManager>();
            if (actorsManager != null)
                actorsManager.SetPlayer(gameObject);
        }

        void Start()
        {
            // fetch components on the same gameObject
            m_CapsuleCollider = GetComponent<CapsuleCollider>();
            DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerCharacterController>(m_CapsuleCollider,
                this, gameObject);
            
            m_Rigidbody = GetComponent<Rigidbody>();
            DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerCharacterController>(m_Rigidbody,
                this, gameObject);

            m_InputHandler = GetComponent<PlayerInputHandler>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerCharacterController>(m_InputHandler,
                this, gameObject);

            m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerWeaponsManager, PlayerCharacterController>(
                m_WeaponsManager, this, gameObject);

            m_Health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerCharacterController>(m_Health, this, gameObject);

            m_Actor = GetComponent<Actor>();
            DebugUtility.HandleErrorIfNullGetComponent<Actor, PlayerCharacterController>(m_Actor, this, gameObject);

            m_Health.OnDie += OnDie;

            // force the crouch state to false when starting
            SetCrouchingState(false, true);
            UpdateCharacterHeight(true);
        }

        [ClientRpc]
        public void SetActive_ClientRpc(bool pIsActive)
        {
            enabled = pIsActive;
        }
        
        void Update()
        {
            // check for Y kill
            if (!IsDead && transform.position.y < KillHeight)
            {
                m_Health.Kill();
            }

            HasJumpedThisFrame = false;

            bool wasGrounded = IsGrounded;
            GroundCheck();

            // landing
            if (IsGrounded && !wasGrounded)
            {
                // Fall damage
                float fallSpeed = -Mathf.Min(CharacterVelocity.y, m_LatestImpactSpeed.y);
                float fallSpeedRatio = (fallSpeed - MinSpeedForFallDamage) /
                                       (MaxSpeedForFallDamage - MinSpeedForFallDamage);
                if (RecievesFallDamage && fallSpeedRatio > 0f)
                {
                    float dmgFromFall = Mathf.Lerp(FallDamageAtMinSpeed, FallDamageAtMaxSpeed, fallSpeedRatio);
                    m_Health.TakeDamage(dmgFromFall, null);

                    // fall damage SFX
                    AudioSource.PlayOneShot(FallDamageSfx);
                }
                else
                {
                    // land SFX
                    AudioSource.PlayOneShot(LandSfx);
                }
            }

            // crouching
            if (m_InputHandler.GetCrouchInputDown())
            {
                SetCrouchingState(!IsCrouching, false);
            }

            UpdateCharacterHeight(false);

            HandleCharacterMovement();
        }

        void OnDie()
        {
            IsDead = true;

            // Tell the weapons manager to switch to a non-existing weapon in order to lower the weapon
            m_WeaponsManager.SwitchToWeaponIndex(-1, true);

            EventManager.Broadcast(Events.PlayerDeathEvent);
        }

        void GroundCheck()
        {
            // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
            float chosenGroundCheckDistance = IsGrounded ? (m_Rigidbody.velocity.magnitude * Time.deltaTime + GroundCheckDistance) : k_GroundCheckDistanceInAir;

            // reset values before the ground check
            IsGrounded = false;
            m_GroundNormal = Vector3.up;

            // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after trying to jump
            if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
            {
                // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
                if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_CapsuleCollider.height),
                    m_CapsuleCollider.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundCheckLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    // storing the upward direction for the surface found
                    m_GroundNormal = hit.normal;

                    // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                    // and if the slope angle is lower than the character controller's limit
                    if (Vector3.Dot(hit.normal, transform.up) > 0f && IsNormalUnderSlopeLimit(m_GroundNormal))
                    {
                        IsGrounded = true;

                        // handle snapping to the ground
                        //m_Rigidbody.MovePosition(transform.position - Vector3.up * (hit.distance));
                    }
                }
                else
                {
                    // If the capsule cast doesn't hit anything, perform a raycast to check if the character is close to the ground
                    RaycastHit rayHit;
                    if (Physics.Raycast(transform.position, Vector3.down, out rayHit, k_GroundCheckDistanceInAir, GroundCheckLayers, QueryTriggerInteraction.Ignore))
                    {
                        IsGrounded = true;
                        m_GroundNormal = rayHit.normal;

                        // adjust the character's position to be exactly on the ground
                        //m_Rigidbody.MovePosition(rayHit.point);
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void HorizontalRotation_ServerRpc(float pHorizontalLook)
        {
            transform.Rotate(
                new Vector3(0f, (pHorizontalLook * RotationSpeed * RotationMultiplier),
                    0f), Space.Self);
        }

        [ServerRpc(RequireOwnership = false)]
        private void VerticalRotation_ServerRpc(float pVerticalLook)
        {
            m_CameraVerticalAngle += pVerticalLook * RotationSpeed * RotationMultiplier;

            m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

            PlayerCameraTransform.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);
            WeaponParentSocket.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);
        }

        void HandleCharacterMovement()
        {
            // horizontal character rotation
            {
                var horizontalLook = m_InputHandler.GetLookInputsHorizontal();
                HorizontalRotation_ServerRpc(horizontalLook);
            }

            // vertical camera rotation
            {
                var verticalLook = m_InputHandler.GetLookInputsVertical();
                VerticalRotation_ServerRpc(verticalLook);
            }

            // character movement handling
            {
                var movement = m_InputHandler.GetMoveInput();
                HandleMovement_ServerRpc(m_InputHandler.GetSprintInputHeld(), movement);
            }

            // character Jump
            {
                if (m_InputHandler.GetJumpInputDown())
                    Jump_ServerRpc();
            }
        }

        [ServerRpc]
        private void HandleMovement_ServerRpc(bool pIsSprinting, Vector3 pMoveInput)
        {
            if (pIsSprinting)
            {
                pIsSprinting = SetCrouchingState(false, false);
            }

            float speedModifier = pIsSprinting ? SprintSpeedModifier : 1f;

            // converts move input to a worldspace vector based on our character's transform orientation
            Vector3 worldspaceMoveInput = transform.TransformVector(pMoveInput);

            // handle grounded movement
            if (IsGrounded)
            {
                // calculate the desired velocity from inputs, max speed, and current slope
                Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;
                // reduce speed if crouching by crouch speed ratio
                if (IsCrouching)
                    targetVelocity *= MaxSpeedCrouchedRatio;
                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) *
                                 targetVelocity.magnitude;

                // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
                //m_Rigidbody.velocity = Vector3.Lerp(m_Rigidbody.velocity, targetVelocity, MovementSharpnessOnGround * Time.deltaTime);
                m_Rigidbody.velocity = targetVelocity;


                // footsteps sound
                float chosenFootstepSfxFrequency =
                    (pIsSprinting ? FootstepSfxFrequencyWhileSprinting : FootstepSfxFrequency);
                if (m_FootstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
                {
                    m_FootstepDistanceCounter = 0f;
                    AudioSource.PlayOneShot(FootstepSfx);
                }

                // keep track of distance traveled for footsteps sound
                m_FootstepDistanceCounter += m_Rigidbody.velocity.magnitude * Time.deltaTime;
            }
            // handle air movement
            else
            {
                // add air acceleration
                m_Rigidbody.AddForce(worldspaceMoveInput * (AccelerationSpeedInAir * Time.deltaTime));

                // limit air speed to a maximum, but only horizontally
                Vector3 horizontalVelocity = new Vector3(m_Rigidbody.velocity.x, 0f, m_Rigidbody.velocity.z);
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
                m_Rigidbody.velocity = new Vector3(horizontalVelocity.x, m_Rigidbody.velocity.y, horizontalVelocity.z);

                // apply the gravity to the velocity
                m_Rigidbody.AddForce(Vector3.down * (GravityDownForce * Time.deltaTime));
            }
        }

        [ServerRpc]
        private void Jump_ServerRpc()
        {
            // jumping
            if (!IsGrounded) return;
            
            // force the crouch state to false
            if (!SetCrouchingState(false, false)) return;
            
            // start by canceling out the vertical component of our velocity
            m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, 0f, m_Rigidbody.velocity.z);

            // then, add the jumpForce value upwards
            m_Rigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);

            // play sound
            AudioSource.PlayOneShot(JumpSfx);

            // remember last time we jumped because we need to prevent snapping to ground for a short time
            m_LastTimeJumped = Time.time;
            HasJumpedThisFrame = true;

            // Force grounding to false
            IsGrounded = false;
            m_GroundNormal = Vector3.up;
        }

        // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
        bool IsNormalUnderSlopeLimit(Vector3 normal)
        {
            return Vector3.Angle(transform.up, normal) <= SlopeLimit;
        }

        // Gets the center point of the bottom hemisphere of the character controller capsule    
        Vector3 GetCapsuleBottomHemisphere()
        {
            return m_Rigidbody.position + (transform.up * m_CapsuleCollider.radius);
        }

        // Gets the center point of the top hemisphere of the character controller capsule    
        Vector3 GetCapsuleTopHemisphere(float atHeight)
        {
            return m_Rigidbody.position + (transform.up * (atHeight - m_CapsuleCollider.radius));
        }

        // Gets a reoriented direction that is tangent to a given slope
        public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, transform.up);
            return Vector3.Cross(slopeNormal, directionRight).normalized;
        }

        void UpdateCharacterHeight(bool force)
        {
            // Update height instantly
            if (force)
            {
                m_CapsuleCollider.height = m_TargetCharacterHeight;
                m_CapsuleCollider.center = Vector3.up * m_CapsuleCollider.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * CameraHeightRatio;
                m_Actor.AimPoint.transform.localPosition = m_CapsuleCollider.center;
            }
            // Update smooth height
            else if (m_CapsuleCollider.height != m_TargetCharacterHeight)
            {
                Debug.Log("UPDATE CHARACTER HEIGHT 2");
                // resize the capsule and adjust camera position
                m_CapsuleCollider.height = Mathf.Lerp(m_CapsuleCollider.height, m_TargetCharacterHeight, CrouchingSharpness * Time.deltaTime);
                m_CapsuleCollider.center = Vector3.up * m_CapsuleCollider.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.Lerp(PlayerCamera.transform.localPosition,
                    Vector3.up * m_TargetCharacterHeight * CameraHeightRatio, CrouchingSharpness * Time.deltaTime);
                m_Actor.AimPoint.transform.localPosition = m_CapsuleCollider.center;
            }
        }

        // returns false if there was an obstruction
        bool SetCrouchingState(bool crouched, bool ignoreObstructions)
        {
            // set appropriate heights
            if (crouched)
            {
                m_TargetCharacterHeight = CapsuleHeightCrouching;
            }
            else
            {
                // Detect obstructions
                if (!ignoreObstructions)
                {
                    // Calculate the height difference
                    float heightDifference = CapsuleHeightStanding - CapsuleHeightCrouching;

                    // Cast a capsule to check for obstructions
                    Collider[] standingOverlaps = Physics.OverlapCapsule(
                        GetCapsuleBottomHemisphere(),
                        GetCapsuleTopHemisphere(CapsuleHeightStanding),
                        m_CapsuleCollider.radius,
                        -1,
                        QueryTriggerInteraction.Ignore);

                    foreach (Collider c in standingOverlaps)
                    {
                        // Ignore the capsule collider itself
                        if (c != m_CapsuleCollider)
                        {
                            // If there is an obstruction, reduce the height to avoid it
                            m_TargetCharacterHeight -= heightDifference;
                            return false;
                        }
                    }
                }

                m_TargetCharacterHeight = CapsuleHeightStanding;
            }

            if (OnStanceChanged != null)
            {
                OnStanceChanged.Invoke(crouched);
            }

            IsCrouching = crouched;
            return true;
        }

    }
}