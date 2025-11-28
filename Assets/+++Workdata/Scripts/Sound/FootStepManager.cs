using EasyPeasyFirstPersonController;
using System;
using System.Collections;
using UnityEngine;

namespace FootstepSystem
{
    [Serializable]
    public struct FootstepClips
    {
        public AudioClip[] WalkClips;
        public AudioClip[] RunClips;
        public AudioClip[] JumpClips;
        public AudioClip[] LandClips;
    }

    [Serializable]
    public struct SurfaceAudio
    {
        public PhysicsMaterial SurfaceMaterial;
        public FootstepClips FootstepSounds;
    }

    public enum FootstepMode
    {
        Player,      // Distance-based system
        Enemy        // Animation event-based system
    }

    [RequireComponent(typeof(CharacterController))]
    public class FootStepManager : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Footstep Mode")]
        [SerializeField] private FootstepMode footstepMode = FootstepMode.Player;

        [Header("Surface Settings")]
        [SerializeField] private SurfaceAudio[] surfaceAudioSets;

        [Header("Audio Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float walkVolume = 1f;
        [SerializeField] private float runVolume = 1f;
        [SerializeField] private float crouchVolume = 0.6f;
        [SerializeField] private float spatialBlend = 1f;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheckOrigin;
        [SerializeField] private float groundCheckDistance = 1.5f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Player Settings (Distance-Based)")]
        [SerializeField] private float walkStepDistance = 2.0f;
        [SerializeField] private float runStepDistance = 1.3f;
        [SerializeField] private float minimumSpeed = 0.1f;

        [Header("Player State")]
        [SerializeField] private bool isCrouching;

        [Header("Enemy Settings (Animation-Based)")]
        [SerializeField] private StateMachine stateMachine;

        [Header("Debug")]
        [SerializeField] private bool debugConsole;

        #endregion

        #region Private Fields

        private CharacterController characterController;
        private FirstPersonController firstPersonController;
        private EnemySoundPerception enemySoundPerception;

        // Player-specific fields
        private Vector3 lastPosition;
        private Vector3 lastStepPosition;
        private Vector3 velocity;
        private float currentStepDistance;
        private Vector2 horizontalVelocity;

        // Shared fields
        private bool wasGrounded;
        private RaycastHit lastGroundHit;
        private PhysicsMaterial lastSurfaceMaterial;
        private Vector3 groundCheckPosition;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupComponents();

            if (footstepMode == FootstepMode.Player)
            {
                SetupPositions();
            }
        }

        private void Update()
        {
            if (!characterController) return;

            // Only use distance-based system for players
            if (footstepMode == FootstepMode.Player)
            {
                UpdatePlayerFootsteps();
            }

            // Both player and enemy need jump/land detection
            CheckJumpAndLanding();
        }

        #endregion

        #region Setup

        private void SetupComponents()
        {
            characterController = GetComponent<CharacterController>();

            if (footstepMode == FootstepMode.Player)
            {
                firstPersonController = GetComponent<FirstPersonController>();
                enemySoundPerception = FindFirstObjectByType<EnemySoundPerception>();
            }
            else if (footstepMode == FootstepMode.Enemy)
            {
                if (stateMachine == null)
                {
                    stateMachine = GetComponent<StateMachine>();
                }
            }
        }

        private void SetupPositions()
        {
            lastPosition = transform.position;
            lastStepPosition = transform.position;
        }

        #endregion

        #region Player Distance-Based System

        private void UpdatePlayerFootsteps()
        {
            CalculateVelocity();

            if (!IsGrounded(out RaycastHit hitInfo, out bool grounded)) return;

            if (!ShouldPlayFootsteps(grounded)) return;

            UpdateFootsteps(hitInfo);
        }

        private void CalculateVelocity()
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            velocity = (transform.position - lastPosition) / deltaTime;
            lastPosition = transform.position;
        }

        private bool ShouldPlayFootsteps(bool grounded)
        {
            if (!grounded) return false;

            horizontalVelocity.x = velocity.x;
            horizontalVelocity.y = velocity.z;

            return horizontalVelocity.magnitude > minimumSpeed;
        }

        private void UpdateFootsteps(RaycastHit hitInfo)
        {
            PhysicsMaterial currentMaterial = hitInfo.collider ? hitInfo.collider.sharedMaterial : null;

            if (SurfaceChanged(currentMaterial))
            {
                PlayFootstepOnSurfaceChange(hitInfo);
                return;
            }

            lastSurfaceMaterial = currentMaterial;

            if (MovedFarEnough())
            {
                PlayFootstepOnDistance(hitInfo);
            }
        }

        private bool SurfaceChanged(PhysicsMaterial currentMaterial)
        {
            if (currentMaterial == lastSurfaceMaterial) return false;

            return lastSurfaceMaterial != null;
        }

        private void PlayFootstepOnSurfaceChange(RaycastHit hitInfo)
        {
            bool isRunning = firstPersonController && firstPersonController.isSprinting;
            PlayFootstep(hitInfo, isRunning);

            lastStepPosition = transform.position;
            lastSurfaceMaterial = hitInfo.collider ? hitInfo.collider.sharedMaterial : null;

            if (debugConsole)
            {
                Debug.Log("Surface changed! New footstep sound played.");
            }
        }

        private bool MovedFarEnough()
        {
            bool isRunning = firstPersonController && firstPersonController.isSprinting;
            currentStepDistance = isRunning ? runStepDistance : walkStepDistance;

            float distanceMoved = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(lastStepPosition.x, 0, lastStepPosition.z)
            );

            return distanceMoved >= currentStepDistance;
        }

        private void PlayFootstepOnDistance(RaycastHit hitInfo)
        {
            bool isRunning = firstPersonController && firstPersonController.isSprinting;
            PlayFootstep(hitInfo, isRunning);

            if (isRunning)
            {
                AlertEnemyToSound();
            }

            lastStepPosition = transform.position;
        }

        private void AlertEnemyToSound()
        {
            if (enemySoundPerception != null)
            {
                enemySoundPerception.CalculateSoundDistance(
                    transform.position,
                    SoundStrength.Normal
                );
            }
        }

        #endregion

        #region Enemy Animation Event System

        /// <summary>
        /// Call this from animation events for walking footsteps
        /// </summary>
        public void OnFootstepWalk()
        {
            if (footstepMode != FootstepMode.Enemy) return;

            if (debugConsole)
            {
                Debug.Log("Walk footstep animation event triggered");
            }

            PlayFootstepAtCurrentPosition(false);
        }

        /// <summary>
        /// Call this from animation events for running footsteps
        /// </summary>
        public void OnFootstepRun()
        {
            if (footstepMode != FootstepMode.Enemy) return;

            if (debugConsole)
            {
                Debug.Log("Run footstep animation event triggered");
            }

            PlayFootstepAtCurrentPosition(true);
        }

        /// <summary>
        /// Generic footstep event that auto-detects if enemy is running
        /// </summary>
        public void OnFootstep()
        {
            if (footstepMode != FootstepMode.Enemy) return;

            bool isRunning = IsEnemyRunning();

            if (debugConsole)
            {
                Debug.Log($"Footstep animation event triggered (Running: {isRunning})");
            }

            PlayFootstepAtCurrentPosition(isRunning);
        }

        private void PlayFootstepAtCurrentPosition(bool isRunning)
        {
            if (!IsGrounded(out RaycastHit hitInfo, out bool grounded)) return;
            if (!grounded) return;

            PlayFootstep(hitInfo, isRunning);
        }

        private bool IsEnemyRunning()
        {
            if (stateMachine == null || stateMachine.currentState == null)
                return false;

            // Adjust this logic based on your state machine implementation
            return stateMachine.currentState.ToString().Contains("Chase");
        }

        #endregion

        #region Shared Ground Detection

        private bool IsGrounded(out RaycastHit hitInfo, out bool grounded)
        {
            groundCheckPosition = groundCheckOrigin
                ? groundCheckOrigin.position
                : transform.position + Vector3.up * 0.1f;

            grounded = Physics.Raycast(
                groundCheckPosition,
                Vector3.down,
                out hitInfo,
                groundCheckDistance,
                groundLayerMask
            );

            return true;
        }

        #endregion

        #region Jump & Landing

        private void CheckJumpAndLanding()
        {
            if (!IsGrounded(out RaycastHit hitInfo, out bool grounded)) return;

            // Jumped
            if (wasGrounded && !grounded)
            {
                if (FindSurface(lastGroundHit, out SurfaceAudio surface))
                {
                    PlayRandomSound(surface.FootstepSounds.JumpClips, masterVolume);
                }
            }
            // Landed
            else if (!wasGrounded && grounded)
            {
                if (FindSurface(hitInfo, out SurfaceAudio surface))
                {
                    PlayRandomSound(surface.FootstepSounds.LandClips, masterVolume);
                }
            }

            if (grounded)
            {
                lastGroundHit = hitInfo;
            }

            wasGrounded = grounded;
        }

        #endregion

        #region Audio Playback

        private void PlayFootstep(RaycastHit hitInfo, bool isRunning)
        {
            if (!FindSurface(hitInfo, out SurfaceAudio surface)) return;

            AudioClip[] clips = isRunning
                ? surface.FootstepSounds.RunClips
                : surface.FootstepSounds.WalkClips;

            float volumeMultiplier = GetVolumeMultiplier(isRunning);
            float finalVolume = Mathf.Clamp01(masterVolume * volumeMultiplier);

            PlayRandomSound(clips, finalVolume);
        }

        private float GetVolumeMultiplier(bool isRunning)
        {
            if (isRunning) return runVolume;
            if (isCrouching) return crouchVolume;
            return walkVolume;
        }

        private bool FindSurface(RaycastHit hit, out SurfaceAudio surface)
        {
            PhysicsMaterial material = hit.collider ? hit.collider.sharedMaterial : null;

            foreach (SurfaceAudio surfaceData in surfaceAudioSets)
            {
                if (surfaceData.SurfaceMaterial == material)
                {
                    surface = surfaceData;
                    return true;
                }
            }

            surface = default;
            return false;
        }

        private void PlayRandomSound(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0) return;

            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip == null) return;

            PlaySound(clip, volume);
        }

        private void PlaySound(AudioClip clip, float volume)
        {
            GameObject audioObject = new GameObject($"FootstepAudio_{clip.name}");
            audioObject.transform.position = transform.position;

            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.spatialBlend = spatialBlend;
            audioSource.Play();

            StartCoroutine(DestroyWhenFinished(audioObject, clip.length));
        }

        private IEnumerator DestroyWhenFinished(GameObject audioObject, float clipLength)
        {
            yield return new WaitForSeconds(clipLength + 0.5f);

            if (audioObject != null)
            {
                Destroy(audioObject);
            }
        }

        #endregion

        #region Public API

        public void SetCrouchState(bool crouching)
        {
            isCrouching = crouching;
        }

        public void SetFootstepMode(FootstepMode mode)
        {
            footstepMode = mode;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Vector3 origin = groundCheckOrigin
                ? groundCheckOrigin.position
                : transform.position + Vector3.up * 0.1f;

            Gizmos.color = wasGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
            Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, 0.1f);
        }

        #endregion
    }
}