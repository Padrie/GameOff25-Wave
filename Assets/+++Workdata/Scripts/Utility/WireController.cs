using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class UtilityPoleGroup
{
    public string groupName = "Group";
    public UtilityPoleConnector[] poles;
}

[ExecuteAlways]
public class WireController : MonoBehaviour
{
    [Header("Utility Pole Groups")]
    public UtilityPoleGroup[] poleGroups;

    [Header("Wire Appearance")]
    public Material wireMaterial;
    public float wireWidth = 0.05f;

    [Header("Line Configuration")]
    [Range(1, 10)]
    public int linesPerConnector = 1;
    [Range(0f, 1f)]
    public float lineOffsetVariance = 0.2f;

    [Header("Wire Sag")]
    public bool useSag = true;
    public float sagAmount = 0.3f;
    [Range(5, 50)]
    public int sagSegments = 20;

    [Header("Wind Animation (Runtime Only)")]
    public bool enableWind = true;
    [Range(0f, 1f)]
    public float windStrength = 0.15f;
    [Range(0f, 5f)]
    public float windSpeed = 1f;
    public Vector2 windDirection = new Vector2(1f, 0.5f);

    private GameObject mainContainer;
    private List<LineRenderer> allLineRenderers = new List<LineRenderer>();
    private List<Vector3[]> allOriginalPositions = new List<Vector3[]>();
    private List<float> allRandomOffsets = new List<float>();
    private bool wiresCreated = false;

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
            CreateAllWires();
        }
    }

    private void EditorUpdate()
    {
        if (!Application.isPlaying && !wiresCreated)
        {
            CreateAllWires();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    CreateAllWires();
                }
            };
        }
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
        if (poleGroups == null || poleGroups.Length == 0)
            return;

        if (wireMaterial == null)
        {
            Debug.LogWarning("Wire material is not assigned!");
            return;
        }

        ClearAllWires();

        mainContainer = new GameObject($"WireController_{gameObject.name}");
        mainContainer.transform.SetParent(transform);
        mainContainer.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            mainContainer.hideFlags = HideFlags.DontSave;
        }
#endif

        allLineRenderers.Clear();
        allOriginalPositions.Clear();
        allRandomOffsets.Clear();

        for (int groupIndex = 0; groupIndex < poleGroups.Length; groupIndex++)
        {
            UtilityPoleGroup group = poleGroups[groupIndex];
            if (group.poles == null || group.poles.Length < 2)
                continue;

            GameObject groupContainer = new GameObject($"Group_{groupIndex}_{group.groupName}");
            groupContainer.transform.SetParent(mainContainer.transform);

            for (int poleIndex = 0; poleIndex < group.poles.Length - 1; poleIndex++)
            {
                UtilityPoleConnector currentPole = group.poles[poleIndex];
                UtilityPoleConnector nextPole = group.poles[poleIndex + 1];

                if (currentPole == null || nextPole == null)
                    continue;

                currentPole.FindConnectors();
                nextPole.FindConnectors();

                for (int connectorIndex = 0; connectorIndex < 2; connectorIndex++)
                {
                    Transform sourceConnector = currentPole.GetConnector(connectorIndex);
                    Transform targetConnector = nextPole.GetConnector(connectorIndex);

                    if (sourceConnector == null || targetConnector == null)
                        continue;

                    for (int lineNum = 0; lineNum < linesPerConnector; lineNum++)
                    {
                        CreateSingleWire(groupContainer.transform, sourceConnector, targetConnector, groupIndex, poleIndex, connectorIndex, lineNum);
                    }
                }
            }
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
                Destroy(mainContainer);
            else
                DestroyImmediate(mainContainer);
#else
            Destroy(mainContainer);
#endif
        }

        mainContainer = null;
        allLineRenderers.Clear();
        allOriginalPositions.Clear();
        allRandomOffsets.Clear();
        wiresCreated = false;
    }

    private void CreateSingleWire(Transform parent, Transform source, Transform target, int groupIndex, int poleIndex, int connectorIndex, int lineNumber)
    {
        string connectorName = connectorIndex == 0 ? "A" : "B";
        GameObject wireObj = new GameObject($"Wire_Pole{poleIndex}_Connector{connectorName}_Line{lineNumber}");
        wireObj.transform.SetParent(parent);

        LineRenderer lr = wireObj.AddComponent<LineRenderer>();
        allLineRenderers.Add(lr);

        lr.material = wireMaterial;
        lr.startWidth = wireWidth;
        lr.endWidth = wireWidth;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        Vector3 startOffset = GetLineOffset(lineNumber, linesPerConnector, lineOffsetVariance);
        Vector3 endOffset = GetLineOffset(lineNumber, linesPerConnector, lineOffsetVariance);

        Vector3 startPos = source.position + startOffset;
        Vector3 endPos = target.position + endOffset;

        if (useSag)
        {
            lr.positionCount = sagSegments + 1;
            Vector3[] positions = new Vector3[sagSegments + 1];

            for (int i = 0; i <= sagSegments; i++)
            {
                float t = i / (float)sagSegments;
                Vector3 point = Vector3.Lerp(startPos, endPos, t);
                float sag = sagAmount * (1f - Mathf.Pow(2f * t - 1f, 2f));
                point.y -= sag;
                lr.SetPosition(i, point);
                positions[i] = point;
            }

            allOriginalPositions.Add(positions);
        }
        else
        {
            lr.positionCount = 2;
            Vector3[] positions = new Vector3[2];
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);
            positions[0] = startPos;
            positions[1] = endPos;
            allOriginalPositions.Add(positions);
        }

        allRandomOffsets.Add(Random.Range(0f, 100f));
    }

    private Vector3 GetLineOffset(int lineNumber, int totalLines, float variance)
    {
        if (totalLines <= 1 || variance == 0)
            return Vector3.zero;

        float distribution = (lineNumber - (totalLines - 1) * 0.5f) / Mathf.Max(1, totalLines - 1);
        Vector3 offset = Vector3.zero;
        offset.x = distribution * variance;
        offset.y = Mathf.Sin(lineNumber * 12.9898f) * variance * 0.5f;
        offset.z = Mathf.Cos(lineNumber * 78.233f) * variance * 0.3f;
        return offset;
    }

    private void AnimateWind()
    {
        float time = Time.time * windSpeed;
        Vector3 windDir = new Vector3(windDirection.x, 0, windDirection.y).normalized;

        for (int wireIndex = 0; wireIndex < allLineRenderers.Count; wireIndex++)
        {
            if (allLineRenderers[wireIndex] == null)
                continue;

            LineRenderer lr = allLineRenderers[wireIndex];
            float offset = allRandomOffsets[wireIndex];

            for (int i = 0; i < lr.positionCount; i++)
            {
                float t = i / (float)(lr.positionCount - 1);
                float middleEffect = Mathf.Sin(Mathf.PI * t);

                float windWave1 = Mathf.Sin(time + offset + i * 0.3f);
                float windWave2 = Mathf.Sin(time * 0.7f + offset + i * 0.5f);
                float windWave3 = Mathf.Cos(time * 1.3f + offset + i * 0.2f);

                Vector3 windOffset = windDir * windWave1 * middleEffect * windStrength;
                windOffset.y += windWave2 * middleEffect * windStrength * 0.3f;
                windOffset += Vector3.Cross(windDir, Vector3.up) * windWave3 * middleEffect * windStrength * 0.5f;

                lr.SetPosition(i, allOriginalPositions[wireIndex][i] + windOffset);
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
        if (poleGroups == null)
            return;

        Color[] groupColors = { Color.yellow, Color.cyan, Color.magenta, Color.green, Color.red };

        for (int g = 0; g < poleGroups.Length; g++)
        {
            UtilityPoleGroup group = poleGroups[g];
            if (group.poles == null || group.poles.Length < 2)
                continue;

            Gizmos.color = groupColors[g % groupColors.Length];

            for (int i = 0; i < group.poles.Length - 1; i++)
            {
                if (group.poles[i] == null || group.poles[i + 1] == null)
                    continue;

                for (int c = 0; c < 2; c++)
                {
                    Transform source = group.poles[i].GetConnector(c);
                    Transform target = group.poles[i + 1].GetConnector(c);
                    if (source != null && target != null)
                        Gizmos.DrawLine(source.position, target.position);
                }
            }
        }
    }
#endif
}