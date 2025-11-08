using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float speed = 10f;
    [SerializeField, Min(0)] float acceleration = 10f;
    [Space(25)]
    [SerializeField] float jumpHeight = 10f;
    [SerializeField, Range(0f, 1f)] float airControlFactor = 1f;

    [Header("Raycast")]
    [SerializeField] LayerMask environmentMask;
    [SerializeField] public float groundedRayLength = 1.5f;
    [SerializeField] public float headRayLength = 1.5f;

    RaycastHit groundedRaycastHit;
    RaycastHit headRaycastHit;

    public float inputX;
    public float inputZ;

    public Vector3 velocity;

    Camera cam;
    Rigidbody rb;
    Vector3 playerInput;
    Vector3 cameraInput;

    public bool isGrounded = false;
    bool isCrouching = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        cam = Camera.main;
    }

    private void Update()
    {
        HandleInput();
        HandleCrouch();
        HandleJump();

        isGrounded = IsGrounded();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    public void HandleInput()
    {
        inputX = Mathf.Lerp(inputX, Input.GetAxisRaw("Horizontal"), acceleration * Time.deltaTime);
        inputZ = Mathf.Lerp(inputZ, Input.GetAxisRaw("Vertical"), acceleration * Time.deltaTime);

        playerInput = new Vector3(inputX, 0f, inputZ);
    }

    public void HandleMovement()
    {
        #region Get Camera Rotation
        Vector3 camForward = cam.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cam.transform.right;
        camRight.y = 0f;
        camRight.Normalize();
        #endregion

        velocity = (camForward * playerInput.z + camRight * playerInput.x) * speed;

        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
    }

    public void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(new Vector3(0, jumpHeight, 0), ForceMode.Impulse);
        }
    }

    public void HandleCrouch()
    {
        float crouchFactor = 0.5f;

        if (Input.GetKeyDown(KeyCode.LeftControl) && !isCrouching && isGrounded)
        {
            transform.localScale = new Vector3(1, crouchFactor, 1);
            transform.position = new Vector3(transform.position.x, transform.position.y - crouchFactor, transform.position.z);
            isCrouching = true;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl) && isCrouching && isGrounded)
        {
            transform.localScale = new Vector3(1, 1, 1);
            transform.position = new Vector3(transform.position.x, transform.position.y + crouchFactor, transform.position.z);
            isCrouching = false;
        }
    }

    Vector3[] rayOffsets = new Vector3[]
    {
            Vector3.zero,
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f)
    };

    bool IsGrounded()
    {

        foreach (var offset in rayOffsets)
        {
            if (Physics.Raycast(transform.position + offset, Vector3.down, out RaycastHit hit, groundedRayLength, environmentMask))
            {
                return true;
            }
        }

        return false;
    }

    bool IsGrounded(out Vector3 normal)
    {
        normal = Vector3.zero;

        foreach (var offset in rayOffsets)
        {
            if (Physics.Raycast(transform.position + offset, Vector3.down, out RaycastHit hit, groundedRayLength, environmentMask))
            {
                normal = hit.normal;
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        foreach (var offset in rayOffsets)
        {
            Gizmos.DrawLine(transform.position + offset, transform.position + offset + Vector3.down * groundedRayLength);
        }
    }
}
