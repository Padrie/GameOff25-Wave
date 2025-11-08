using UnityEngine;

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
    public AudioSource doorSound;
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;

    [Header("Auto-Open Triggers")]
    public BoxCollider frontTrigger;
    public BoxCollider backTrigger;
    public string playerTag = "Player";
    public float knockSpeedMultiplier = 2.5f;

    bool isOpen;
    bool isMoving;
    Quaternion closedRot, openRot, targetRot;
    float currentSpeed;

    void Awake()
    {
        closedRot = transform.rotation;
        openRot = closedRot * Quaternion.Euler(0f, toggleOpenOrientation ? -EndRotation : EndRotation, 0f);
        isOpen = Quaternion.Angle(transform.rotation, openRot) < Quaternion.Angle(transform.rotation, closedRot);
        targetRot = isOpen ? openRot : closedRot;
        currentSpeed = Speed;
        SetupTrigger(frontTrigger, false);
        SetupTrigger(backTrigger, true);
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

        if (doorSound)
        {
            doorSound.clip = isOpen ? doorOpenSound : doorCloseSound;
            if (doorSound.clip) doorSound.Play();
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

        if (doorSound)
        {
            doorSound.clip = doorOpenSound;
            if (doorSound.clip) doorSound.Play();
        }
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
