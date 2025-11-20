using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class SplinePathFollower : MonoBehaviour
{
    [SerializeField] public SplineContainer splineContainer;
    [SerializeField] public GameObject prefabToSpawn;
    [SerializeField] public int objectCount = 5;
    [SerializeField] public float spacingBetweenObjects = 1f;
    [SerializeField] public float speed = 5f;
    [SerializeField] public float startOffset = 0f;

    private List<SplineFollowerComponent> followers = new List<SplineFollowerComponent>();
    private float splineLength;

#if UNITY_EDITOR
    public void EditorSpawnPreview()
    {
        EditorClearPreview();

        if (splineContainer == null || prefabToSpawn == null)
        {
            UnityEditor.EditorUtility.DisplayDialog("Error", "SplineContainer and Prefab To Spawn must be assigned!", "OK");
            return;
        }

        float splineLen = splineContainer.CalculateLength();
        Transform previewParent = EditorGetOrCreatePreviewParent();

        for (int i = 0; i < objectCount; i++)
        {
            GameObject newObject = UnityEditor.PrefabUtility.InstantiatePrefab(prefabToSpawn, previewParent) as GameObject;
            newObject.name = $"{prefabToSpawn.name}_Preview_{i}";

            float startDistance = startOffset + (i * spacingBetweenObjects);
            float t = Mathf.Clamp01(startDistance / splineLen);

            var spline = splineContainer.Spline;
            Vector3 position = SplineUtility.EvaluatePosition(spline, t);
            Vector3 tangent = SplineUtility.EvaluateTangent(spline, t);

            newObject.transform.position = splineContainer.transform.TransformPoint(position);

            if (tangent.magnitude > 0.001f)
            {
                Vector3 worldTangent = splineContainer.transform.TransformDirection(tangent).normalized;
                Vector3 worldUp = Vector3.up;

                Vector3 right = Vector3.Cross(worldUp, worldTangent).normalized;
                if (right.magnitude < 0.001f)
                    right = Vector3.right;

                Vector3 up = Vector3.Cross(worldTangent, right).normalized;

                newObject.transform.rotation = Quaternion.LookRotation(worldTangent, up);
            }
        }
    }

    public void EditorClearPreview()
    {
        Transform previewParent = transform.Find("_PreviewParent");
        if (previewParent != null)
        {
            DestroyImmediate(previewParent.gameObject);
        }
    }

    private Transform EditorGetOrCreatePreviewParent()
    {
        Transform existing = transform.Find("_PreviewParent");
        if (existing != null)
        {
            return existing;
        }

        GameObject previewParent = new GameObject("_PreviewParent");
        previewParent.transform.SetParent(transform);
        previewParent.transform.localPosition = Vector3.zero;
        previewParent.transform.localRotation = Quaternion.identity;

        return previewParent.transform;
    }
#endif

    private void Start()
    {
        if (splineContainer == null || prefabToSpawn == null)
        {
            Debug.LogError("SplineContainer and prefab must be assigned!");
            return;
        }

        splineLength = splineContainer.CalculateLength();

        Transform previewParent = transform.Find("_PreviewParent");
        if (previewParent != null && previewParent.childCount > 0)
        {
            foreach (Transform child in previewParent)
            {
                SplineFollowerComponent follower = child.GetComponent<SplineFollowerComponent>();
                if (follower == null)
                    follower = child.gameObject.AddComponent<SplineFollowerComponent>();

                int index = child.GetSiblingIndex();
                float startDistance = startOffset + (index * spacingBetweenObjects);
                float stopDistance = splineLength - ((objectCount - 1 - index) * spacingBetweenObjects);
                follower.Initialize(splineContainer, startDistance, speed, splineLength, stopDistance);
                followers.Add(follower);
            }
        }
        else
        {
            for (int i = 0; i < objectCount; i++)
            {
                GameObject newObject = Instantiate(prefabToSpawn, transform);
                SplineFollowerComponent follower = newObject.AddComponent<SplineFollowerComponent>();

                float startDistance = startOffset + (i * spacingBetweenObjects);
                float stopDistance = splineLength - ((objectCount - 1 - i) * spacingBetweenObjects);
                follower.Initialize(splineContainer, startDistance, speed, splineLength, stopDistance);

                followers.Add(follower);
            }
        }

        UpdateFollowerPositions();
    }

    private void UpdateFollowerPositions()
    {
        foreach (var follower in followers)
        {
            follower.UpdatePosition();
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.G))
        {
            foreach (var follower in followers)
            {
                follower.MoveAlongSpline();
            }
        }
    }
}

public class SplineFollowerComponent : MonoBehaviour
{
    private SplineContainer splineContainer;
    private float currentDistance;
    private float speed;
    private float splineLength;
    private float stopDistance;
    private bool hasReachedEnd = false;

    public void Initialize(SplineContainer container, float startDistance, float moveSpeed, float length, float stop)
    {
        splineContainer = container;
        currentDistance = startDistance;
        speed = moveSpeed;
        splineLength = length;
        stopDistance = stop;
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
        if (splineContainer == null)
            return;

        float t = currentDistance / splineLength;

        var spline = splineContainer.Spline;
        Vector3 position = SplineUtility.EvaluatePosition(spline, t);
        Vector3 tangent = SplineUtility.EvaluateTangent(spline, t);

        transform.position = splineContainer.transform.TransformPoint(position);

        if (tangent.magnitude > 0.001f)
        {
            Vector3 worldTangent = splineContainer.transform.TransformDirection(tangent).normalized;
            Vector3 worldUp = Vector3.up;

            Vector3 right = Vector3.Cross(worldUp, worldTangent).normalized;
            if (right.magnitude < 0.001f)
                right = Vector3.right;

            Vector3 up = Vector3.Cross(worldTangent, right).normalized;

            transform.rotation = Quaternion.LookRotation(worldTangent, up);
        }
    }
}