using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EasyPeasyFirstPersonController;

public class Door : MonoBehaviour, IInteractableWithHit
{
    [Header("Rotation")]
    public float openAngle = 90f;
    public float openSpeed = 180f;
    public float closeSpeed = 180f;
    public float rotationThreshold = 0.5f;

    [Header("Paired Door")]
    [SerializeField] private List<Door> pairedDoors = new List<Door>();

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip kickSound;
    public float volume = 1f;
    public float spatialBlend = 1f;
    public float forcedVolumeMultiplier = 1f;
    public float normalVolumeMultiplier = 0.1f;

    [Header("Triggers")]
    public BoxCollider frontTrigger;
    public BoxCollider backTrigger;
    public string playerTag = "Player";
    public float kickSpeedMultiplier = 2.5f;

    private bool isOpen;
    private bool isMoving;
    private Quaternion targetRotation;
    private float currentSpeed;
    private Quaternion closedRotation;
    private RaycastHit lastHit;
    private bool currentOpenDirection;

    void Awake()
    {
        closedRotation = transform.rotation;
        SetupTriggers();
    }

    void Update()
    {
        if (isMoving)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, currentSpeed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, targetRotation) <= rotationThreshold)
            {
                transform.rotation = targetRotation;
                isMoving = false;
            }
        }
    }

    public void UpdateHitInfo(RaycastHit hit)
    {
        lastHit = hit;
    }

    public void Interact()
    {
        if (isMoving) return;
        Toggle();
    }

    public void OnHoverEnter() { }
    public void OnHoverExit() { }

    private bool DetermineDirectionFromHit()
    {
        float dot = Vector3.Dot(transform.forward, lastHit.normal);
        return dot < 0;
    }

    public void Toggle()
    {
        if (!isOpen)
        {
            currentOpenDirection = DetermineDirectionFromHit();
            isOpen = true;
        }
        else
        {
            isOpen = false;
        }

        SetRotationTarget();
        PlaySound(isOpen ? openSound : closeSound, false);

        if (pairedDoors.Count > 0)
            TogglePairedDoors();
    }

    public void Kick(bool direction)
    {
        if (!IsClosed()) return;

        currentOpenDirection = direction;
        isOpen = true;
        SetRotationTarget();
        PlaySound(kickSound, true);
        currentSpeed = openSpeed * kickSpeedMultiplier;

        if (pairedDoors.Count > 0)
            KickPairedDoors(direction);
    }

    private void SetRotationTarget()
    {
        if (!isOpen)
        {
            targetRotation = closedRotation;
            currentSpeed = closeSpeed;
        }
        else
        {
            float angle = currentOpenDirection ? -openAngle : openAngle;
            targetRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
            currentSpeed = openSpeed;
        }

        isMoving = true;
    }

    private void TogglePairedDoors()
    {
        foreach (Door paired in pairedDoors)
        {
            if (paired == null || paired == this) continue;

            paired.isOpen = this.isOpen;
            paired.currentOpenDirection = this.currentOpenDirection;
            paired.SetRotationTarget();

            AudioClip sound = this.isOpen ? paired.openSound : paired.closeSound;
            if (sound) paired.PlaySound(sound, false);
        }
    }

    private void KickPairedDoors(bool direction)
    {
        foreach (Door paired in pairedDoors)
        {
            if (paired == null || paired == this || !paired.IsClosed()) continue;

            paired.currentOpenDirection = direction;
            paired.isOpen = true;
            paired.SetRotationTarget();
            paired.currentSpeed = paired.openSpeed * paired.kickSpeedMultiplier;
            paired.PlaySound(paired.kickSound, true);
        }
    }

    private void PlaySound(AudioClip clip, bool forced)
    {
        if (!clip) return;

        GameObject audioObj = new GameObject("DoorAudio");
        audioObj.transform.position = transform.position;

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume * (forced ? forcedVolumeMultiplier : normalVolumeMultiplier);
        source.pitch = forced ? 1f : Random.Range(0.6f, 0.8f);
        source.spatialBlend = spatialBlend;
        source.Play();

        StartCoroutine(DestroyAfterDelay(audioObj, clip.length + 0.5f));
    }

    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj) Destroy(obj);
    }

    private bool IsClosed()
    {
        return Quaternion.Angle(transform.rotation, closedRotation) <= rotationThreshold + 0.001f;
    }

    private void SetupTriggers()
    {
        SetupTrigger(frontTrigger, false);
        SetupTrigger(backTrigger, true);
    }

    private void SetupTrigger(BoxCollider collider, bool direction)
    {
        if (!collider) return;
        collider.isTrigger = true;

        DoorTrigger trigger = collider.GetComponent<DoorTrigger>();
        if (!trigger) trigger = collider.gameObject.AddComponent<DoorTrigger>();
        trigger.Initialize(this, direction, playerTag);
    }

    public Quaternion GetOpenRotation()
    {
        float angle = currentOpenDirection ? -openAngle : openAngle;
        return closedRotation * Quaternion.Euler(0f, angle, 0f);
    }

    public Quaternion GetClosedRotation() => closedRotation;
}

public class DoorTrigger : MonoBehaviour
{
    private Door door;
    private bool direction;
    private string playerTag;
    private FirstPersonController playerController;

    void Awake()
    {
        playerController = FindFirstObjectByType<FirstPersonController>();
    }

    public void Initialize(Door door, bool direction, string playerTag)
    {
        this.door = door;
        this.direction = direction;
        this.playerTag = playerTag;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!door || !other.CompareTag(playerTag) || !playerController) return;
        if (playerController.isSprinting)
            door.Kick(direction);
    }
}