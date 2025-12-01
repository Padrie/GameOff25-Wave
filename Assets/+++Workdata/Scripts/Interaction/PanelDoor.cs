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

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public float volume = 0.5f;

    public bool isOpen;
    private bool isMoving;
    private Quaternion targetRotation;
    private float currentSpeed;
    private Quaternion closedRotation;
    private RaycastHit lastHit;
    private bool currentOpenDirection;

    void Awake()
    {
        closedRotation = transform.rotation;
        Debug.Log("Closed rotation saved: " + closedRotation.eulerAngles);
    }

    void Update()
    {
        if (isMoving)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, currentSpeed * Time.deltaTime);

            float remainingAngle = Quaternion.Angle(transform.rotation, targetRotation);

            if (remainingAngle <= rotationThreshold)
            {
                transform.rotation = targetRotation;
                isMoving = false;
                Debug.Log("Finished rotating");
            }
        }
    }

    public void UpdateHitInfo(RaycastHit hit)
    {
        lastHit = hit;
    }

    protected override void OnInteracted()
    {
        if (isMoving) return;
        Toggle();
    }

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
        PlaySound(isOpen ? openSound : closeSound);
    }

    private void SetRotationTarget()
    {
        if (!isOpen)
        {
            targetRotation = closedRotation;
            currentSpeed = closeSpeed;
            Debug.Log("Closing to: " + targetRotation.eulerAngles);
        }
        else
        {
            float angle = currentOpenDirection ? -openAngle : openAngle;

            Vector3 eulerOffset = rotationAxis switch
            {
                RotationAxis.X => new Vector3(angle, 0f, 0f),
                RotationAxis.Y => new Vector3(0f, angle, 0f),
                RotationAxis.Z => new Vector3(0f, 0f, angle),
                _ => new Vector3(0f, angle, 0f)
            };

            targetRotation = closedRotation * Quaternion.Euler(eulerOffset);
            currentSpeed = openSpeed;
            Debug.Log("Opening to: " + targetRotation.eulerAngles + " (axis: " + rotationAxis + ", angle: " + angle + ")");
        }

        isMoving = true;
        Debug.Log("isMoving set to true, currentSpeed: " + currentSpeed);
    }

    private void PlaySound(AudioClip clip)
    {
        if (!clip) return;
        AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }
}