using UnityEngine;
using UnityEngine.Splines;
using Unity.Cinemachine;

public class CarAnimation : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] public float maxSpeed = 10f;
    [SerializeField] private float fadeOutDistance = 5f;
    [SerializeField] private AnimationCurve speedFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Position & Rotation")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private float rotationSmoothing = 1f;

    [Header("Animation Settings")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private bool loopAnimation = false;

    [Header("Cinemachine Camera")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private bool useCameraTransition = true;
    [SerializeField] private int cameraTransitionStartPercentage = 0;
    [SerializeField] private int cameraTransitionEndPercentage = 100;

    [Header("End Animation Actions")]
    [SerializeField] private AudioSource audioSourceToDeactivate;
    [SerializeField] private GameObject playerToActivate;
    [SerializeField] private bool deactivateCameraOnEnd = true;

    [Header("Beginning - Position Damping")]
    [SerializeField] private Vector3 beginningDamping = Vector3.one;

    [Header("Beginning - Follow Offset")]
    [SerializeField] private Vector3 beginningOffset = new Vector3(8f, 5f, 0f);

    [Header("Final - Position Damping")]
    [SerializeField] private Vector3 finalDamping = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Final - Follow Offset")]
    [SerializeField] private Vector3 finalOffset = new Vector3(8f, 5f, 0f);

    private float currentDistance;
    private float totalSplineLength;
    private Quaternion targetRotation = Quaternion.identity;
    private bool isAnimating;
    private float delayTimer;
    private CinemachineFollow cinemachineFollow;

    private const float SPLINE_END_THRESHOLD = 0.1f;
    private const float HALF_PI = Mathf.PI / 2f;
    private const int SPLINE_INDEX = 0;

    private void Start()
    {
        if (!ValidateSetup())
            return;

        totalSplineLength = splineContainer.CalculateLength();
        InitializeCamera();

        if (autoStart)
            delayTimer = startDelay;
    }

    private bool ValidateSetup()
    {
        if (splineContainer == null)
        {
            Debug.LogError("SplineContainer is not assigned!");
            enabled = false;
            return false;
        }

        splineContainer.gameObject.GetComponent<MeshRenderer>().enabled = false;
        return true;
    }

    private void InitializeCamera()
    {
        if (virtualCamera == null || !useCameraTransition)
            return;

        cinemachineFollow = virtualCamera.GetComponent<CinemachineFollow>();
        if (cinemachineFollow != null)
            cinemachineFollow.FollowOffset = beginningOffset;
    }

    private void Update()
    {
        if (HandleStartDelay())
            return;

        if (!isAnimating || splineContainer == null)
            return;

        float distanceFromEnd = totalSplineLength - currentDistance;
        float currentSpeed = CalculateSpeed(distanceFromEnd);
        currentDistance += currentSpeed * Time.deltaTime;

        if (currentDistance >= totalSplineLength - SPLINE_END_THRESHOLD)
            HandleAnimationEnd();
        else
            UpdatePosition();
    }

    private bool HandleStartDelay()
    {
        if (isAnimating || delayTimer <= 0)
            return false;

        delayTimer -= Time.deltaTime;
        if (delayTimer <= 0)
            isAnimating = true;

        return true;
    }

    private float CalculateSpeed(float distanceFromEnd)
    {
        if (distanceFromEnd >= fadeOutDistance)
            return maxSpeed;

        float fadeProgress = 1f - (distanceFromEnd / fadeOutDistance);
        return maxSpeed * speedFalloff.Evaluate(fadeProgress);
    }

    public void HandleAnimationEnd()
    {
        if (loopAnimation)
        {
            currentDistance = 0f;
            return;
        }

        currentDistance = totalSplineLength;
        isAnimating = false;
        OnAnimationEnd();
    }
    public void TeleportToEnd()
    {
        currentDistance = totalSplineLength;

        float t = currentDistance / totalSplineLength;
        Vector3 splinePosition = splineContainer.EvaluatePosition(SPLINE_INDEX, t);
        Vector3 splineTangent = splineContainer.EvaluateTangent(SPLINE_INDEX, t);

        Vector3 newPosition = transform.position;
        newPosition.x = splinePosition.x + positionOffset.x;
        newPosition.z = splinePosition.z + positionOffset.z;
        transform.position = newPosition;

        targetRotation = CalculateRotation(splineTangent);
        transform.rotation = targetRotation;

        OnAnimationEnd();
    }

    private void UpdatePosition()
    {
        float t = currentDistance / totalSplineLength;
        Vector3 splinePosition = splineContainer.EvaluatePosition(SPLINE_INDEX, t);
        Vector3 splineTangent = splineContainer.EvaluateTangent(SPLINE_INDEX, t);

        Vector3 newPosition = transform.position;
        newPosition.x = splinePosition.x + positionOffset.x;
        newPosition.z = splinePosition.z + positionOffset.z;
        transform.position = newPosition;

        targetRotation = CalculateRotation(splineTangent);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);

        UpdateCameraTransition(t);
    }

    private Quaternion CalculateRotation(Vector3 direction)
    {
        direction.Normalize();
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        return Quaternion.Euler(rotationOffset.x, angle + rotationOffset.y, rotationOffset.z);
    }

    private void UpdateCameraTransition(float progress)
    {
        if (cinemachineFollow == null || !useCameraTransition)
            return;

        float progressPercentage = progress * 100f;

        if (progressPercentage < cameraTransitionStartPercentage || progressPercentage > cameraTransitionEndPercentage)
            return;

        float transitionRange = cameraTransitionEndPercentage - cameraTransitionStartPercentage;
        float transitionProgress = (progressPercentage - cameraTransitionStartPercentage) / transitionRange;
        float easeProgress = Mathf.Sin(transitionProgress * HALF_PI);

        Vector3 offsetLerp = Vector3.Lerp(beginningOffset, finalOffset, easeProgress);
        cinemachineFollow.FollowOffset = offsetLerp;
    }

    private void OnAnimationEnd()
    {
        if (audioSourceToDeactivate != null)
            audioSourceToDeactivate.enabled = false;

        if (playerToActivate != null)
            playerToActivate.SetActive(true);

        if (deactivateCameraOnEnd && virtualCamera != null)
            virtualCamera.enabled = false;
    }

    public void RestartAnimation()
    {
        currentDistance = 0f;
        delayTimer = startDelay;
        isAnimating = startDelay <= 0;
    }

    public void StartAnimation(float delay = 0f)
    {
        currentDistance = 0f;
        delayTimer = delay;
        isAnimating = delay <= 0;
    }

    public void SetSpeedFalloff(AnimationCurve curve) => speedFalloff = curve;

    public bool IsAnimating() => isAnimating;

    public float GetProgress() => totalSplineLength > 0 ? currentDistance / totalSplineLength : 0f;
}