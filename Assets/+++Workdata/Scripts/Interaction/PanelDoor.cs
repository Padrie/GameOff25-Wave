using UnityEngine;

public class PanelDoor : InteractableObject, IInteractableWithHit
{
    [Header("Rotation")]
    public float openAngle = 120f;
    public float openSpeed = 180f;
    public float closeSpeed = 180f;
    public float rotationThreshold = 0.5f;
    public enum RotationAxis { X, Y, Z }
    public RotationAxis rotationAxis = RotationAxis.Y;
    public bool reverseDirection = false;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public float volume = 0.5f;

    public bool isOpen;
    private bool isMoving;
    private Quaternion targetRotation;
    private float currentSpeed;
    private Quaternion closedRotation;

    void Awake()
    {
        closedRotation = transform.rotation;
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
        // No longer needed, but kept for interface compatibility
    }

    protected override void OnInteracted()
    {
        if (isMoving) return;
        Toggle();
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        SetRotationTarget();
        PlaySound(isOpen ? openSound : closeSound);
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
            float angle = reverseDirection ? -openAngle : openAngle;

            Vector3 eulerOffset = rotationAxis switch
            {
                RotationAxis.X => new Vector3(angle, 0f, 0f),
                RotationAxis.Y => new Vector3(0f, angle, 0f),
                RotationAxis.Z => new Vector3(0f, 0f, angle),
                _ => new Vector3(0f, angle, 0f)
            };

            targetRotation = closedRotation * Quaternion.Euler(eulerOffset);
            currentSpeed = openSpeed;
        }

        isMoving = true;
    }

    private void PlaySound(AudioClip clip)
    {
        if (!clip) return;
        AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }
}