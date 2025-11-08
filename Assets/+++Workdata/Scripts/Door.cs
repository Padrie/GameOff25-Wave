using UnityEngine;

public class Door : MonoBehaviour
{
    public bool toggleOpenOrientation;
    public float EndRotation = 90f;
    public float Speed = 180f;
    public float RotationThreshold = 0.5f;
    public KeyCode toggleDoor = KeyCode.E;
    public KeyCode toggleOpenDirection = KeyCode.T;

    public AudioSource doorSound;
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;

    bool isOpen;
    bool isMoving;
    Quaternion closedRot, openRot, targetRot;

    void Awake()
    {
        closedRot = transform.rotation;
        openRot = closedRot * Quaternion.Euler(0f, toggleOpenOrientation ? -EndRotation : EndRotation, 0f);
        isOpen = Quaternion.Angle(transform.rotation, openRot) < Quaternion.Angle(transform.rotation, closedRot);
        targetRot = isOpen ? openRot : closedRot;
    }

    void Update()
    {
        var desiredOpenRot = closedRot * Quaternion.Euler(0f, toggleOpenOrientation ? -EndRotation : EndRotation, 0f);
        if (desiredOpenRot != openRot) { openRot = desiredOpenRot; if (isOpen) targetRot = openRot; }

        if (isMoving)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, Speed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, targetRot) <= RotationThreshold)
            {
                transform.rotation = targetRot;
                isMoving = false;
            }
        }

        if (Input.GetKeyDown(toggleDoor))
        {
            ToggleDoor();
        }

        if (Input.GetKeyDown(toggleOpenDirection))
        {
            toggleOpenOrientation = !toggleOpenOrientation;
        }
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        targetRot = isOpen ? openRot : closedRot;
        isMoving = true;
        if (doorSound)
        {
            doorSound.clip = isOpen ? doorOpenSound : doorCloseSound;
            if (doorSound.clip) doorSound.Play();
        }
    }
}
