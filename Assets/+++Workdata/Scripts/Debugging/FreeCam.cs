using UnityEngine;

public class FreeCam : MonoBehaviour
{
    public MonoBehaviour playerController;
    public MonoBehaviour footstepManager;
    public CharacterController controller;
    public KeyCode toggleKey = KeyCode.V;
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;

    private bool freeCamActive = false;
    private float pitch = 0f;
    private float yaw = 0f;

    public Transform playerCamera;

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        yaw = transform.eulerAngles.y;
        pitch = playerCamera != null ? playerCamera.localEulerAngles.x : 0f;

        if (pitch > 180f) pitch -= 360f;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleFreeCam();

        if (!freeCamActive) return;

        MouseLook();
        Move();
    }

    void ToggleFreeCam()
    {
        freeCamActive = !freeCamActive;

        if (freeCamActive)
        {
            yaw = transform.eulerAngles.y;
            if (playerCamera != null)
            {
                pitch = playerCamera.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
            }
        }
        else
        {
            SyncRotationToFPC();
        }

        if (playerController != null)
            playerController.enabled = !freeCamActive;

        if (footstepManager != null)
            footstepManager.enabled = !freeCamActive;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void SyncRotationToFPC()
    {
        var fpcType = playerController.GetType();

        var rotXField = fpcType.GetField("rotX", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rotYField = fpcType.GetField("rotY", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var xVelocityField = fpcType.GetField("xVelocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var yVelocityField = fpcType.GetField("yVelocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (rotXField != null) rotXField.SetValue(playerController, yaw);
        if (rotYField != null) rotYField.SetValue(playerController, pitch);
        if (xVelocityField != null) xVelocityField.SetValue(playerController, yaw);
        if (yVelocityField != null) yVelocityField.SetValue(playerController, pitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void MouseLook()
    {
        yaw += Input.GetAxis("Mouse X") * lookSpeed;
        pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        playerCamera.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = playerCamera != null ? playerCamera.forward : transform.forward;
        Vector3 right = playerCamera != null ? playerCamera.right : transform.right;

        forward.y = 0f;
        forward.Normalize();
        right.y = 0f;
        right.Normalize();

        Vector3 dir = forward * v + right * h;

        if (Input.GetKey(KeyCode.Space))
            dir += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl))
            dir += Vector3.down;

        controller.Move(dir * moveSpeed * Time.deltaTime);
    }
}