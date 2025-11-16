using UnityEngine;
using System.Collections;
using SteamAudio;

public class Door : MonoBehaviour
{
    [Header("Open/Close")]
    public bool toggleOpenOrientation;
    public float EndRotation = 90f;
    public float Speed = 180f;
    public float RotationThreshold = 0.5f;

    [Header("Input (optional)")]
    public KeyCode toggleDoor = KeyCode.E;
    public KeyCode toggleOpenDirection = KeyCode.T;

    [Header("Audio")]
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;
    [Range(0f, 1f)] public float volume = 1f;
    public float spatialBlend = 1f;
    public float audioSourceLifetime = 2f;

    [Header("Steam Audio Settings")]
    public bool applyHRTF = true;
    public bool applyReflections = true;
    public bool applyPathing = false;
    public bool directBinaural = true;

    [Header("Auto-Open Triggers")]
    public BoxCollider frontTrigger;
    public BoxCollider backTrigger;
    public string playerTag = "Player";
    public float knockSpeedMultiplier = 2.5f;

    bool isOpen;
    bool isMoving;
    Quaternion closedRot, openRot, targetRot;
    float currentSpeed;

    private CircularWaveSpawner _circularWaveSpawner;

    void Awake()
    {
        closedRot = transform.rotation;
        openRot = closedRot * Quaternion.Euler(0f, toggleOpenOrientation ? -EndRotation : EndRotation, 0f);
        isOpen = Quaternion.Angle(transform.rotation, openRot) < Quaternion.Angle(transform.rotation, closedRot);
        targetRot = isOpen ? openRot : closedRot;
        currentSpeed = Speed;
        SetupTrigger(frontTrigger, false);
        SetupTrigger(backTrigger, true);

        _circularWaveSpawner = FindFirstObjectByType<CircularWaveSpawner>();
    }

    void Update()
    {
        var desiredOpenRot = closedRot * Quaternion.Euler(0f, toggleOpenOrientation ? -EndRotation : EndRotation, 0f);
        if (desiredOpenRot != openRot) { openRot = desiredOpenRot; if (isOpen) targetRot = openRot; }

        if (isMoving)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, currentSpeed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, targetRot) <= RotationThreshold)
            {
                transform.rotation = targetRot;
                isMoving = false;
                currentSpeed = Speed;
            }
        }

        if (Input.GetKeyDown(toggleDoor)) ToggleDoor();
        if (Input.GetKeyDown(toggleOpenDirection)) toggleOpenOrientation = !toggleOpenOrientation;
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        targetRot = isOpen ? openRot : closedRot;
        isMoving = true;
        currentSpeed = Speed;

        AudioClip clipToPlay = isOpen ? doorOpenSound : doorCloseSound;
        if (clipToPlay != null)
        {
            PlayDoorSound(clipToPlay);
        }
    }

    public void TryKnockOpen(bool openOrientationForThisSide)
    {
        if (!IsActuallyClosed()) return;

        toggleOpenOrientation = openOrientationForThisSide;
        openRot = closedRot * Quaternion.Euler(0f, toggleOpenOrientation ? -EndRotation : EndRotation, 0f);
        isOpen = true;
        targetRot = openRot;
        isMoving = true;
        currentSpeed = Speed * knockSpeedMultiplier;

        if (doorOpenSound != null)
        {
            PlayDoorSound(doorOpenSound);
        }
        _circularWaveSpawner.SpawnWaveAt(gameObject.transform.position);
    }

    bool IsActuallyClosed()
    {
        return Quaternion.Angle(transform.rotation, closedRot) <= RotationThreshold + 0.001f;
    }

    void SetupTrigger(BoxCollider col, bool openOrientationForThisSide)
    {
        if (!col) return;
        col.isTrigger = true;
        var relay = col.gameObject.GetComponent<DoorAutoZone>();
        if (!relay) relay = col.gameObject.AddComponent<DoorAutoZone>();
        relay.Initialize(this, openOrientationForThisSide, playerTag);
    }

    private void PlayDoorSound(AudioClip clip)
    {
        if (clip == null) return;

        GameObject audioObject = new GameObject("DoorAudio_" + clip.name);
        audioObject.transform.position = transform.position;

        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = spatialBlend;

        SteamAudioSource steamAudio = audioObject.AddComponent<SteamAudioSource>();
        steamAudio.directBinaural = directBinaural;
        steamAudio.reflections = applyReflections;
        steamAudio.pathing = applyPathing;
        steamAudio.distanceAttenuation = true;

        audioSource.Play();

        StartCoroutine(DestroyAudioSource(audioObject, clip.length + 0.5f));
    }

    private IEnumerator DestroyAudioSource(GameObject audioObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioObject != null)
            Destroy(audioObject);
    }
}

public class DoorAutoZone : MonoBehaviour
{
    Door door;
    bool openOrientation;
    string playerTag;

    public void Initialize(Door door, bool openOrientation, string playerTag)
    {
        this.door = door;
        this.openOrientation = openOrientation;
        this.playerTag = playerTag;
        var bc = GetComponent<BoxCollider>();
        if (bc) bc.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!door) return;
        if (other.CompareTag(playerTag))
        {
            door.TryKnockOpen(openOrientation);
        }
    }
}