using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class JumperCable : MonoBehaviour
{
    [Header("Connection Points")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Cable Settings")]
    [Range(10, 100)]
    public int segmentCount = 50;

    [Range(0f, 2f)]
    public float cableSag = 0.5f;

    [Header("Twirl/Spiral Settings")]
    [Range(0f, 10f)]
    public float twirlFrequency = 3f;

    [Range(0f, 0.2f)]
    public float twirlAmplitude = 0.05f;

    [Range(0f, 5f)]
    public float twirlSpeed = 1f;

    [Header("Visual Settings")]
    [Range(0.01f, 0.1f)]
    public float cableWidth = 0.03f;

    public Color cableColor = Color.red;
    public Material cableMaterial;

    private LineRenderer lineRenderer;
    private Vector3[] cablePoints;
    private float timeOffset;

    void Start()
    {
        SetupLineRenderer();
        InitializeCablePoints();
    }

    void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segmentCount;
        lineRenderer.startWidth = cableWidth;
        lineRenderer.endWidth = cableWidth;

        if (cableMaterial != null)
        {
            lineRenderer.material = cableMaterial;
        }
        else
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        lineRenderer.startColor = cableColor;
        lineRenderer.endColor = cableColor;

        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;
    }

    void InitializeCablePoints()
    {
        cablePoints = new Vector3[segmentCount];
    }

    void Update()
    {
        if (startPoint == null || endPoint == null)
            return;

        timeOffset += Time.deltaTime * twirlSpeed;

        UpdateCablePoints();

        lineRenderer.SetPositions(cablePoints);
    }

    void UpdateCablePoints()
    {
        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;
        Vector3 cableDirection = end - start;
        float cableLength = cableDirection.magnitude;

        // Calculate local coordinate system for twirl
        Vector3 cableRight;
        Vector3 cableUp;

        if (cableLength > 0.001f)
        {
            Vector3 cableDir = cableDirection / cableLength;

            // Choose a reference vector that isn't parallel to the cable
            Vector3 reference = Mathf.Abs(Vector3.Dot(cableDir, Vector3.up)) < 0.99f
                ? Vector3.up
                : Vector3.right;

            cableRight = Vector3.Cross(cableDir, reference).normalized;
            cableUp = Vector3.Cross(cableRight, cableDir).normalized;
        }
        else
        {
            cableRight = Vector3.right;
            cableUp = Vector3.forward;
        }

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);

            // Base position along the cable
            Vector3 point = Vector3.Lerp(start, end, t);

            // Sag effect - maximum in the middle
            float sagMultiplier = Mathf.Sin(t * Mathf.PI);
            point += Vector3.down * cableSag * sagMultiplier;

            // Twirl/spiral effect - also stronger in the middle
            float twirlStrength = sagMultiplier;
            float angle = (t * twirlFrequency * Mathf.PI * 2f) + timeOffset;

            Vector3 twirlOffset = (cableRight * Mathf.Cos(angle) + cableUp * Mathf.Sin(angle))
                                  * twirlAmplitude * twirlStrength;

            point += twirlOffset;

            cablePoints[i] = point;
        }
    }

    public void RefreshVisuals()
    {
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = cableWidth;
            lineRenderer.endWidth = cableWidth;
            lineRenderer.startColor = cableColor;
            lineRenderer.endColor = cableColor;
        }
    }

    public void SetConnectionPoints(Transform start, Transform end)
    {
        startPoint = start;
        endPoint = end;
    }

    void OnDrawGizmos()
    {
        if (startPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, 0.05f);
        }

        if (endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPoint.position, 0.05f);
        }

        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = cableColor;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
        }
    }
}