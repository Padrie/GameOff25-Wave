using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using DG.Tweening;

public class GarageDoor : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform segmentParent;
    [SerializeField] private float spacingBetweenObjects = 1f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float startOffset = 0f;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [SerializeField] private AudioClip doorOpenAudio;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float audioFadeDuration = 0.5f;
    [SerializeField][Range(0f, 1f)] private float maxAudioVolume = 1f;


    [SerializeField] private EnemySoundPerception _enemySoundPerception;


    private List<SplineFollowerComponent> followers = new List<SplineFollowerComponent>();
    private float splineLength;
    private bool isAudioPlaying;
    private const float TANGENT_MAGNITUDE_THRESHOLD = 0.001f;

    private void Start()
    {
        if (!ValidateSetup())
            return;

        splineContainer.gameObject.GetComponent<MeshRenderer>().enabled = false;
        splineLength = splineContainer.CalculateLength();

        InitializeFollowers();
        UpdateFollowerPositions();
    }


    private void Awake()
    {
        _enemySoundPerception = FindFirstObjectByType<EnemySoundPerception>();
    }

    private bool ValidateSetup()
    {
        if (splineContainer == null || segmentParent == null)
        {
            Debug.LogError("SplineContainer and Segment Parent must be assigned");
            return false;
        }

        if (audioSource == null)
        {
            Debug.LogError("AudioSource not assigned");
            return false;
        }

        audioSource.clip = doorOpenAudio;
        audioSource.spatialBlend = 1f;
        return true;
    }

    private void InitializeFollowers()
    {
        int objectCount = segmentParent.childCount;

        for (int i = 0; i < objectCount; i++)
        {
            Transform child = segmentParent.GetChild(i);
            SplineFollowerComponent follower = child.GetComponent<SplineFollowerComponent>() ??
                                              child.gameObject.AddComponent<SplineFollowerComponent>();

            float startDistance = startOffset + (i * spacingBetweenObjects);
            float stopDistance = splineLength - ((objectCount - 1 - i) * spacingBetweenObjects);

            follower.Initialize(splineContainer, startDistance, speed, splineLength, stopDistance, rotationOffset);
            followers.Add(follower);
        }
    }

    private void UpdateFollowerPositions()
    {
        foreach (var follower in followers)
            follower.UpdatePosition();

    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.G))
        {
            _enemySoundPerception.CalculateSoundDistance(splineContainer.transform.position, SoundStrength.VeryLoud);
            HandleDoorOpening();
        }
        else if (isAudioPlaying)
            StopAudio();
    }

    private void HandleDoorOpening()
    {
        if (followers.Count == 0)
        {
            return;
        }

        if (!isAudioPlaying)
        {
            StartAudio();
            isAudioPlaying = true;
        }

        foreach (var follower in followers)
            follower.MoveAlongSpline();
    }

    private void StartAudio()
    {
        if (audioSource == null || doorOpenAudio == null)
            return;

        DOTween.Kill(audioSource);

        if (audioSource.time > 0 && !audioSource.isPlaying)
            audioSource.UnPause();
        else if (!audioSource.isPlaying)
            audioSource.Play();

        audioSource.volume = 0f;
        audioSource.DOFade(maxAudioVolume, audioFadeDuration).SetEase(Ease.InOutQuad);
    }

    private void StopAudio()
    {
        if (audioSource == null || !audioSource.isPlaying)
            return;

        DOTween.Kill(audioSource);
        audioSource.DOFade(0f, audioFadeDuration).SetEase(Ease.InOutQuad)
            .OnComplete(() => audioSource.Pause());

        isAudioPlaying = false;
    }
}

public class SplineFollowerComponent : MonoBehaviour
{
    private SplineContainer splineContainer;
    private float currentDistance;
    private float speed;
    private float splineLength;
    private float stopDistance;
    private Vector3 rotationOffset;
    private bool hasReachedEnd;
    private const float TANGENT_THRESHOLD = 0.001f;

    public void Initialize(SplineContainer container, float startDistance, float moveSpeed, float length, float stop, Vector3 rotation)
    {
        splineContainer = container;
        currentDistance = startDistance;
        speed = moveSpeed;
        splineLength = length;
        stopDistance = stop;
        rotationOffset = rotation;
    }

    public void MoveAlongSpline()
    {
        if (hasReachedEnd || splineContainer == null)
            return;

        currentDistance += speed * Time.deltaTime;

        if (currentDistance >= stopDistance)
        {
            currentDistance = stopDistance;
            hasReachedEnd = true;
        }

        UpdatePosition();
    }

    public void UpdatePosition()
    {
        if (splineContainer == null || splineLength <= 0)
            return;

        float t = Mathf.Clamp01(currentDistance / splineLength);

        var spline = splineContainer.Spline;
        Vector3 position = SplineUtility.EvaluatePosition(spline, t);
        Vector3 tangent = SplineUtility.EvaluateTangent(spline, t);

        transform.position = splineContainer.transform.TransformPoint(position);
        ApplyRotation(tangent);
    }

    private void ApplyRotation(Vector3 tangent)
    {
        if (tangent.magnitude <= TANGENT_THRESHOLD)
            return;

        Vector3 worldTangent = splineContainer.transform.TransformDirection(tangent).normalized;
        Vector3 worldUp = Vector3.up;

        Vector3 right = Vector3.Cross(worldUp, worldTangent).normalized;
        if (right.magnitude < TANGENT_THRESHOLD)
            right = Vector3.right;

        Vector3 up = Vector3.Cross(worldTangent, right).normalized;

        Quaternion baseRotation = Quaternion.LookRotation(worldTangent, up);
        Quaternion offsetRotation = Quaternion.Euler(rotationOffset);
        transform.rotation = baseRotation * offsetRotation;
    }
}