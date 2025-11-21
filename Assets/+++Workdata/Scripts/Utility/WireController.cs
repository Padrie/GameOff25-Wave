using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class WireConnection
{
    [Header("Connection Points")]
    [Tooltip("The starting pole for this wire connection")]
    public Transform sourcePole;

    [Tooltip("The poles to connect to from the source")]
    public Transform[] targetPoles;

    [Header("Line Configuration")]
    [Tooltip("Number of lines per target pole")]
    [Range(1, 10)]
    public int lineCountPerPole = 1;

    [Tooltip("Offset variance between multiple lines per pole")]
    [Range(0f, 1f)]
    public float lineOffsetVariance = 0.2f;

    [Header("Wire Sag")]
    [Tooltip("Use sag for this connection")]
    public bool useSag = true;

    [Tooltip("Amount of sag in the wires")]
    public float sagAmount = 0.3f;

    [Tooltip("Number of segments for sag calculation")]
    [Range(5, 50)]
    public int sagSegments = 20;

    [Header("Visual")]
    [Tooltip("Height offset from pole base")]
    public float heightOffset = 0f;

    // Runtime data (not serialized)
    [System.NonSerialized]
    public GameObject wireContainer;
    [System.NonSerialized]
    public List<LineRenderer> lineRenderers = new List<LineRenderer>();
    [System.NonSerialized]
    public List<Vector3[]> originalPositions = new List<Vector3[]>();
    [System.NonSerialized]
    public List<float> randomOffsets = new List<float>();

    public bool IsValid()
    {
        return sourcePole != null && targetPoles != null && targetPoles.Length > 0;
    }

    public int GetValidPoleCount()
    {
        if (targetPoles == null) return 0;
        int count = 0;
        foreach (Transform pole in targetPoles)
        {
            if (pole != null) count++;
        }
        return count;
    }
}

[ExecuteAlways]
public class WireController : MonoBehaviour
{
    [Header("Wire Connections")]
    [Tooltip("Array of wire connections to manage")]
    public WireConnection[] wireConnections;

    [Header("Global Wire Appearance")]
    public Material wireMaterial;
    public float wireWidth = 0.05f;

    [Header("Wind Animation (Runtime Only)")]
    [Tooltip("Enable wind sway effect")]
    public bool enableWind = true;

    [Tooltip("Wind strength")]
    [Range(0f, 1f)]
    public float windStrength = 0.15f;

    [Tooltip("Wind speed")]
    [Range(0f, 5f)]
    public float windSpeed = 1f;

    [Tooltip("Wind direction (XZ plane)")]
    public Vector2 windDirection = new Vector2(1f, 0.5f);

    private GameObject mainContainer;
    private bool wiresCreated = false;
    private bool isPlayMode = false;

    private void OnEnable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += EditorUpdate;
#endif

        if (!Application.isPlaying && !wiresCreated)
        {
            CreateAllWires();
        }
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            isPlayMode = true;
            CreateAllWires();
        }
    }

#if UNITY_EDITOR
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ClearAllWires();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            isPlayMode = false;
            CreateAllWires();
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            isPlayMode = true;
        }
    }

    private void EditorUpdate()
    {
        if (!Application.isPlaying && HasChanged())
        {
            CreateAllWires();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    CreateAllWires();
                }
            };
        }
    }

    private bool HasChanged()
    {
        if (wireConnections == null || wireConnections.Length == 0) return false;
        if (!wiresCreated) return true;
        return false;
    }
#endif

    private void Update()
    {
        if (Application.isPlaying && enableWind && wiresCreated)
        {
            AnimateWind();
        }
    }

    [ContextMenu("Create All Wires")]
    public void CreateAllWires()
    {
        if (wireConnections == null || wireConnections.Length == 0)
        {
            return;
        }

        if (wireMaterial == null)
        {
            Debug.LogWarning("Wire material is not assigned!");
            return;
        }

        // Clear existing wires
        ClearAllWires();

        // Create main container
        mainContainer = new GameObject($"WireController_{gameObject.name}");
        mainContainer.transform.SetParent(transform);
        mainContainer.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            mainContainer.hideFlags = HideFlags.DontSave;
        }
#endif

        // Create wires for each connection
        for (int i = 0; i < wireConnections.Length; i++)
        {
            CreateConnectionWires(wireConnections[i], i);
        }

        wiresCreated = true;
    }

    [ContextMenu("Clear All Wires")]
    public void ClearAllWires()
    {
        if (mainContainer != null)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Destroy(mainContainer);
            }
            else
            {
                DestroyImmediate(mainContainer);
            }
#else
            Destroy(mainContainer);
#endif
        }

        mainContainer = null;

        // Clear runtime data from all connections
        if (wireConnections != null)
        {
            foreach (WireConnection connection in wireConnections)
            {
                connection.wireContainer = null;
                connection.lineRenderers.Clear();
                connection.originalPositions.Clear();
                connection.randomOffsets.Clear();
            }
        }

        wiresCreated = false;
    }

    private void CreateConnectionWires(WireConnection connection, int connectionIndex)
    {
        if (!connection.IsValid())
        {
            return;
        }

        int validPoleCount = connection.GetValidPoleCount();
        if (validPoleCount == 0)
        {
            return;
        }

        // Create container for this connection
        connection.wireContainer = new GameObject($"Connection_{connectionIndex}_{connection.sourcePole.name}");
        connection.wireContainer.transform.SetParent(mainContainer.transform);
        connection.wireContainer.transform.localPosition = Vector3.zero;

        // Clear previous runtime data
        connection.lineRenderers.Clear();
        connection.originalPositions.Clear();
        connection.randomOffsets.Clear();

        // Create wires for each valid target pole
        int wireIndex = 0;
        for (int i = 0; i < connection.targetPoles.Length; i++)
        {
            if (connection.targetPoles[i] != null)
            {
                // Create multiple lines for this pole
                for (int lineNum = 0; lineNum < connection.lineCountPerPole; lineNum++)
                {
                    CreateSingleWire(connection, connection.targetPoles[i], wireIndex, lineNum, connection.lineCountPerPole);
                    connection.randomOffsets.Add(Random.Range(0f, 100f));
                    wireIndex++;
                }
            }
        }
    }

    private void CreateSingleWire(WireConnection connection, Transform targetPole, int wireIndex, int lineNumber, int totalLinesForPole)
    {
        GameObject wireObj = new GameObject($"Wire_{wireIndex}_to_{targetPole.name}_line{lineNumber + 1}");
        wireObj.transform.SetParent(connection.wireContainer.transform);

        LineRenderer lr = wireObj.AddComponent<LineRenderer>();
        connection.lineRenderers.Add(lr);

        // Setup line renderer
        lr.material = wireMaterial;
        lr.startWidth = wireWidth;
        lr.endWidth = wireWidth;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // Calculate offsets for messy appearance
        Vector3 startOffset = GetLineOffset(lineNumber, totalLinesForPole, connection.lineOffsetVariance);
        Vector3 endOffset = GetLineOffset(lineNumber, totalLinesForPole, connection.lineOffsetVariance);

        Vector3 startPos = connection.sourcePole.position + Vector3.up * connection.heightOffset + startOffset;
        Vector3 endPos = targetPole.position + Vector3.up * connection.heightOffset + endOffset;

        // Create the wire with or without sag
        if (connection.useSag)
        {
            lr.positionCount = connection.sagSegments + 1;
            Vector3[] positions = new Vector3[connection.sagSegments + 1];

            for (int i = 0; i <= connection.sagSegments; i++)
            {
                float t = i / (float)connection.sagSegments;
                Vector3 point = Vector3.Lerp(startPos, endPos, t);

                // Add sag using parabolic curve
                float sag = connection.sagAmount * (1f - Mathf.Pow(2f * t - 1f, 2f));
                point.y -= sag;

                lr.SetPosition(i, point);
                positions[i] = point;
            }

            connection.originalPositions.Add(positions);
        }
        else
        {
            lr.positionCount = 2;
            Vector3[] positions = new Vector3[2];

            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);
            positions[0] = startPos;
            positions[1] = endPos;

            connection.originalPositions.Add(positions);
        }
    }

    private Vector3 GetLineOffset(int lineNumber, int totalLines, float variance)
    {
        if (totalLines <= 1 || variance == 0)
            return Vector3.zero;

        // Distribute lines evenly around the center
        float distribution = (lineNumber - (totalLines - 1) * 0.5f) / Mathf.Max(1, totalLines - 1);

        Vector3 offset = Vector3.zero;

        // Horizontal spread (X axis)
        offset.x = distribution * variance;

        // Slight vertical variation
        offset.y = Mathf.Sin(lineNumber * 12.9898f) * variance * 0.5f;

        // Depth variation (Z axis)
        offset.z = Mathf.Cos(lineNumber * 78.233f) * variance * 0.3f;

        return offset;
    }

    private void AnimateWind()
    {
        if (wireConnections == null) return;

        float time = Time.time * windSpeed;
        Vector3 windDir = new Vector3(windDirection.x, 0, windDirection.y).normalized;

        foreach (WireConnection connection in wireConnections)
        {
            if (connection.lineRenderers == null || connection.originalPositions == null) continue;

            for (int wireIndex = 0; wireIndex < connection.lineRenderers.Count; wireIndex++)
            {
                if (connection.lineRenderers[wireIndex] == null) continue;

                LineRenderer lr = connection.lineRenderers[wireIndex];
                float offset = connection.randomOffsets[wireIndex];

                for (int i = 0; i < lr.positionCount; i++)
                {
                    // Calculate wind effect (more in the middle of the wire)
                    float t = i / (float)(lr.positionCount - 1);
                    float middleEffect = Mathf.Sin(Mathf.PI * t); // 0 at ends, 1 in middle

                    // Combine multiple sine waves for natural movement
                    float windWave1 = Mathf.Sin(time + offset + i * 0.3f);
                    float windWave2 = Mathf.Sin(time * 0.7f + offset + i * 0.5f);
                    float windWave3 = Mathf.Cos(time * 1.3f + offset + i * 0.2f);

                    // Calculate wind offset
                    Vector3 windOffset = windDir * windWave1 * middleEffect * windStrength;
                    windOffset.y += windWave2 * middleEffect * windStrength * 0.3f; // Vertical sway
                    windOffset += Vector3.Cross(windDir, Vector3.up) * windWave3 * middleEffect * windStrength * 0.5f; // Perpendicular sway

                    // Apply wind to original position
                    lr.SetPosition(i, connection.originalPositions[wireIndex][i] + windOffset);
                }
            }
        }
    }

    private void OnDestroy()
    {
        ClearAllWires();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (wireConnections == null) return;

        // Draw connection lines in editor for visualization
        Gizmos.color = Color.yellow;
        foreach (WireConnection connection in wireConnections)
        {
            if (!connection.IsValid()) continue;

            foreach (Transform targetPole in connection.targetPoles)
            {
                if (targetPole != null && connection.sourcePole != null)
                {
                    Vector3 start = connection.sourcePole.position + Vector3.up * connection.heightOffset;
                    Vector3 end = targetPole.position + Vector3.up * connection.heightOffset;
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
#endif
}
