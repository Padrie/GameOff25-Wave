using UnityEngine;
using System;

namespace FootstepSystem
{
    [System.Serializable]
    public struct FootstepClips
    {
        public AudioClip[] WalkClips;
        public AudioClip[] RunClips;
        public AudioClip[] JumpClips;
        public AudioClip[] LandClips;
    }

    [System.Serializable]
    public struct SurfaceAudio
    {
        public PhysicsMaterial SurfaceMaterial;
        public FootstepClips FootstepSounds;
    }

    [RequireComponent(typeof(CharacterController))]
    public class FootStepManager : MonoBehaviour
    {
        [Header("Surface Settings")]
        public SurfaceAudio[] SurfaceAudioSets;

        [Header("Audio Settings")]
        public AudioSource AudioSource;
        [Range(0f, 1f)] public float MasterVolume = 1f;
        public float WalkVolume = 1f;
        public float RunVolume = 1f;
        public float CrouchVolume = 0.6f;

        [Header("Ground Detection")]
        public Transform GroundCheckOrigin;
        public float GroundCheckDistance = 1.5f;
        public LayerMask GroundLayerMask = ~0;

        [Header("Step Settings")]
        public float WalkStepDistance = 2.0f;
        public float RunStepDistance = 1.3f;
        public float RunSpeedThreshold = 3.5f;
        public float MinimumSpeed = 0.1f;

        [Header("Player State")]
        public bool IsCrouching;

        private CharacterController characterController;
        private Vector3 lastPosition;
        private Vector3 lastStepPosition;
        private Vector3 velocity;
        private bool wasGrounded;
        private RaycastHit lastGroundHit;

        private void Awake()
        {
            if (!AudioSource)
                AudioSource = GetComponent<AudioSource>();

            characterController = GetComponent<CharacterController>();
            lastPosition = transform.position;
            lastStepPosition = transform.position;
        }

        private void Update()
        {
            if (!AudioSource || !characterController)
                return;

            bool isGrounded = Physics.Raycast(
                GroundCheckOrigin ? GroundCheckOrigin.position : transform.position + Vector3.up * 0.1f,
                Vector3.down, out var hitInfo,
                GroundCheckDistance, GroundLayerMask,
                QueryTriggerInteraction.Ignore);

            velocity = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPosition = transform.position;
            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

            HandleJumpAndLand(isGrounded, hitInfo);

            if (!isGrounded || horizontalSpeed <= MinimumSpeed)
                return;

            float distanceMoved = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(lastStepPosition.x, 0, lastStepPosition.z));

            bool isRunning = horizontalSpeed >= RunSpeedThreshold && !IsCrouching;
            float stepDistance = isRunning ? RunStepDistance : WalkStepDistance;

            if (distanceMoved >= stepDistance)
            {
                PlayFootstep(hitInfo, isRunning);
                lastStepPosition = transform.position;
            }
        }

        //Handles jump and land sounds
        private void HandleJumpAndLand(bool isGrounded, RaycastHit hitInfo)
        {
            if (wasGrounded && !isGrounded)
            {
                if (TryGetSurface(lastGroundHit, out var surface))
                    PlayRandomClip(surface.FootstepSounds.JumpClips, MasterVolume);
            }
            else if (!wasGrounded && isGrounded)
            {
                if (TryGetSurface(hitInfo, out var surface))
                    PlayRandomClip(surface.FootstepSounds.LandClips, MasterVolume);
            }

            if (isGrounded)
                lastGroundHit = hitInfo;

            wasGrounded = isGrounded;
        }

        //Plays a walk or run footstep
        private void PlayFootstep(RaycastHit hitInfo, bool isRunning)
        {
            if (TryGetSurface(hitInfo, out var surface))
            {
                var clips = isRunning ? surface.FootstepSounds.RunClips : surface.FootstepSounds.WalkClips;
                float volume = MasterVolume * (isRunning ? RunVolume : (IsCrouching ? CrouchVolume : WalkVolume));
                PlayRandomClip(clips, Mathf.Clamp01(volume));
            }
        }

        private bool TryGetSurface(RaycastHit hit, out SurfaceAudio surface)
        {
            var material = hit.collider ? hit.collider.sharedMaterial : null;

            foreach (var surfaceData in SurfaceAudioSets)
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

        //Plays a random audio clip
        private void PlayRandomClip(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0) return;
            var clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip != null)
                AudioSource.PlayOneShot(clip, volume);
        }
    }
}
