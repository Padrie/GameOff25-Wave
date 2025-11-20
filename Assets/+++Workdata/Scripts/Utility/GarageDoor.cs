using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class GarageDoor : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private SplineContainer doorSpline;
    [SerializeField] private SplineContainer guideSpline;
    [SerializeField] private float openAmount = 0f;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isAnimating = false;
    private float targetOpenAmount = 0f;
    private BezierKnot[] originalKnots;
    private float doorSplineLength;
    private float guideSplineLength;

    void Start()
    {
        if (doorSpline == null)
        {
            doorSpline = GetComponent<SplineContainer>();
        }

        if (guideSpline == null)
        {
            CreateGuideSpline();
        }

        CacheOriginalKnots();
    }

    void CreateGuideSpline()
    {
        GameObject guideObj = new GameObject("GuideSpline");
        guideObj.transform.parent = transform;
        guideObj.transform.localPosition = Vector3.zero;
        guideObj.transform.localRotation = Quaternion.identity;

        guideSpline = guideObj.AddComponent<SplineContainer>();
        guideSpline.Spline = new Spline();

        for (int i = 0; i < doorSpline.Spline.Count; i++)
        {
            guideSpline.Spline.Add(doorSpline.Spline[i]);
        }

        Debug.Log($"Created guide spline with {guideSpline.Spline.Count} knots");
    }

    void CacheOriginalKnots()
    {
        if (doorSpline == null) return;

        doorSplineLength = doorSpline.Spline.GetLength();
        guideSplineLength = guideSpline.Spline.GetLength();

        int knotCount = doorSpline.Spline.Count;
        originalKnots = new BezierKnot[knotCount];

        for (int i = 0; i < knotCount; i++)
        {
            originalKnots[i] = doorSpline.Spline[i];
        }

        Debug.Log($"Cached {originalKnots.Length} knots");
    }

    void Update()
    {
        if (isAnimating)
        {
            float step = animationSpeed * Time.deltaTime;
            openAmount = Mathf.MoveTowards(openAmount, targetOpenAmount, step);

            if (Mathf.Approximately(openAmount, targetOpenAmount))
            {
                isAnimating = false;
            }
        }

        UpdateDoorSpline();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Toggle();
        }
    }

    void UpdateDoorSpline()
    {
        if (doorSpline == null || guideSpline == null || originalKnots == null) return;

        float easedOpen = easingCurve.Evaluate(openAmount);

        for (int i = 0; i < originalKnots.Length; i++)
        {
            float tOnDoor = (float)i / (originalKnots.Length - 1);

            float distanceAlongDoor = tOnDoor * doorSplineLength;
            float offsetDistance = easedOpen * doorSplineLength * 0.8f;
            float newDistanceOnGuide = distanceAlongDoor + offsetDistance;
            float tOnGuide = Mathf.Clamp01(newDistanceOnGuide / guideSplineLength);

            float3 newPosition = guideSpline.EvaluatePosition(tOnGuide);

            BezierKnot knot = originalKnots[i];
            knot.Position = newPosition;
            doorSpline.Spline[i] = knot;
        }
    }

    public void Open()
    {
        targetOpenAmount = 1f;
        isAnimating = true;
    }

    public void Close()
    {
        targetOpenAmount = 0f;
        isAnimating = true;
    }

    public void Toggle()
    {
        if (targetOpenAmount > 0.5f)
            Close();
        else
            Open();
    }
}