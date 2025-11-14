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

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
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

        if (playerController != null)
            playerController.enabled = !freeCamActive;

        if (footstepManager != null)
            footstepManager.enabled = !freeCamActive;

        if (freeCamActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void MouseLook()
    {
        yaw += Input.GetAxis("Mouse X") * lookSpeed;
        pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 dir = transform.forward * v + transform.right * h;

        if (Input.GetKey(KeyCode.Space))
            dir += Vector3.up;

        if (Input.GetKey(KeyCode.LeftControl))
            dir += Vector3.down;

        controller.Move(dir * moveSpeed * Time.deltaTime);
    }
}
