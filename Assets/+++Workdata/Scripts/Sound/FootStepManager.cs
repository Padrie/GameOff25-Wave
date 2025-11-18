using UnityEngine;
using System;
using System.Collections;
//using SteamAudio;
using Vector3 = UnityEngine.Vector3;


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
        [Range(0f, 1f)] public float MasterVolume = 1f;
        public float WalkVolume = 1f;
        public float RunVolume = 1f;
        public float CrouchVolume = 0.6f;
        public float AudioSourceLifetime = 2f;
        public float _spatialBlend = 1f;

        [Header("Steam Audio Settings")]
        public bool applyHRTF = true;
        public bool applyReflections = true;
        public bool applyPathing = false;
        public bool directBinaural = true;

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

        [Header("Debug")]
        public bool debugConsole;

        private CharacterController characterController;
        private Vector3 lastPosition;
        private Vector3 lastStepPosition;
        private Vector3 velocity;
        private bool wasGrounded;
        private RaycastHit lastGroundHit;
        private PhysicsMaterial lastSurfaceMaterial;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            lastPosition = transform.position;
            lastStepPosition = transform.position;
        }

        private void Update()
        {
            if (!characterController)
                return;

            bool isGrounded = Physics.Raycast(
                GroundCheckOrigin ? GroundCheckOrigin.position : transform.position + Vector3.up * 0.1f,
                Vector3.down, out var hitInfo,
                GroundCheckDistance, GroundLayerMask);

            velocity = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPosition = transform.position;
            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

            HandleJumpAndLand(isGrounded, hitInfo);

            if (!isGrounded || horizontalSpeed <= MinimumSpeed)
                return;

            bool isRunning = horizontalSpeed >= RunSpeedThreshold && !IsCrouching;

            PhysicsMaterial currentMaterial = hitInfo.collider ? hitInfo.collider.sharedMaterial : null;
            bool surfaceChanged = currentMaterial != lastSurfaceMaterial;

            if (surfaceChanged && lastSurfaceMaterial != null)
            {
                PlayFootstep(hitInfo, isRunning);
                lastStepPosition = transform.position;
                lastSurfaceMaterial = currentMaterial;

                if (debugConsole)
                {
                    Debug.Log($"Surface changed! New sound played.");
                }
                return;
            }

            lastSurfaceMaterial = currentMaterial;

            float distanceMoved = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(lastStepPosition.x, 0, lastStepPosition.z));

            float stepDistance = isRunning ? RunStepDistance : WalkStepDistance;

            if (distanceMoved >= stepDistance)
            {
                PlayFootstep(hitInfo, isRunning);
                lastStepPosition = transform.position;
                if (debugConsole)
                {
                    Debug.Log($"Footstep played.");
                }
            }
        }

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

        private void PlayRandomClip(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0) return;
            var clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip != null)
            {
                GameObject audioObject = new GameObject("FootstepAudio_" + clip.name);
                audioObject.transform.position = transform.position;

                AudioSource audioSource = audioObject.AddComponent<AudioSource>();
                audioSource.clip = clip;
                audioSource.volume = volume;
                audioSource.spatialBlend = _spatialBlend;

                //SteamAudioSource steamAudio = audioObject.AddComponent<SteamAudioSource>();
                //steamAudio.directBinaural = directBinaural;
                //steamAudio.reflections = applyReflections;
                //steamAudio.pathing = applyPathing;
                //steamAudio.distanceAttenuation = true;

                audioSource.Play();

                StartCoroutine(DestroyAudioSource(audioObject, clip.length + 0.5f));
            }
        }

        private IEnumerator DestroyAudioSource(GameObject audioObject, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (audioObject != null)
                Destroy(audioObject);
        }
    }
}