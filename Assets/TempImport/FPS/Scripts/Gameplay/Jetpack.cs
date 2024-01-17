using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(AudioSource))]
    public class Jetpack : MonoBehaviour
    {
        [Header("References")] [Tooltip("Audio source for jetpack sfx")]
        public AudioSource AudioSource;

        [Tooltip("Particles for jetpack vfx")] public ParticleSystem[] JetpackVfx;

        [Header("Parameters")] [Tooltip("Whether the jetpack is unlocked at the begining or not")]
        public bool IsJetpackUnlockedAtStart = false;

        [Tooltip("The strength with which the jetpack pushes the player up")]
        public float JetpackAcceleration = 7f;

        [Range(0f, 1f)]
        [Tooltip(
            "This will affect how much using the jetpack will cancel the gravity value, to start going up faster. 0 is not at all, 1 is instant")]
        public float JetpackDownwardVelocityCancelingFactor = 1f;

        [Header("Durations")] [Tooltip("Time it takes to consume all the jetpack fuel")]
        public float ConsumeDuration = 1.5f;

        [Tooltip("Time it takes to completely refill the jetpack while on the ground")]
        public float RefillDurationGrounded = 2f;

        [Tooltip("Time it takes to completely refill the jetpack while in the air")]
        public float RefillDurationInTheAir = 5f;

        [Tooltip("Delay after last use before starting to refill")]
        public float RefillDelay = 1f;

        [Header("Audio")] [Tooltip("Sound played when using the jetpack")]
        public AudioClip JetpackSfx;

        bool m_CanUseJetpack;
        PlayerCharacterController m_PlayerCharacterController;
        PlayerInputHandler m_InputHandler;
        float m_LastTimeOfUse;

        // stored ratio for jetpack resource (1 is full, 0 is empty)
        public float CurrentFillRatio { get; private set; }
        public bool IsJetpackUnlocked { get; private set; }

        public bool IsPlayergrounded() => m_PlayerCharacterController.IsGrounded;

        public UnityAction<bool> OnUnlockJetpack;

        void Start()
        {
            IsJetpackUnlocked = IsJetpackUnlockedAtStart;

            m_PlayerCharacterController = GetComponent<PlayerCharacterController>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, Jetpack>(m_PlayerCharacterController,
                this, gameObject);

            m_InputHandler = GetComponent<PlayerInputHandler>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, Jetpack>(m_InputHandler, this, gameObject);

            CurrentFillRatio = 1f;

            AudioSource.clip = JetpackSfx;
            AudioSource.loop = true;
        }

        public bool TryUnlock()
        {
            if (IsJetpackUnlocked)
                return false;

            OnUnlockJetpack.Invoke(true);
            IsJetpackUnlocked = true;
            m_LastTimeOfUse = Time.time;
            return true;
        }
    }
}