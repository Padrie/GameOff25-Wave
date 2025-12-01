using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class WireNode
{
    [Tooltip("Drag any GameObject here - can be a UtilityPoleConnector OR RooftopElectrical")]
    public GameObject target;

    // Cached connectors
    [HideInInspector] public Transform connectorA;
    [HideInInspector] public Transform connectorB;
    [HideInInspector] public Transform connectorC; // For connecting to RooftopElectrical
    [HideInInspector] public bool isSingleConnector;

    public void FindConnectors()
    {
        connectorA = null;
        connectorB = null;
        connectorC = null;
        isSingleConnector = false;

        if (target == null)
            return;

        // First, try to find UtilityPoleConnector component
        UtilityPoleConnector poleConnector = target.GetComponent<UtilityPoleConnector>();
        if (poleConnector != null)
        {
            poleConnector.FindConnectors();
            connectorA = poleConnector.GetConnector(0);
            connectorB = poleConnector.GetConnector(1);
            // Look for Wire Connector C
            connectorC = target.transform.Find("Wire Connector C");
            isSingleConnector = false;
            return;
        }

        // Check for dual/triple connectors (Wire Connector A / B / C)
        Transform foundA = target.transform.Find("Wire Connector A");
        Transform foundB = target.transform.Find("Wire Connector B");
        Transform foundC = target.transform.Find("Wire Connector C");

        if (foundA != null || foundB != null)
        {
            connectorA = foundA;
            connectorB = foundB;
            connectorC = foundC;
            isSingleConnector = false;
            return;
        }

        // Check for single connector (RooftopElectrical style - "Wire Connector")
        Transform singleConnector = target.transform.Find("Wire Connector");
        if (singleConnector != null)
        {
            connectorA = singleConnector;
            connectorB = singleConnector;
            connectorC = singleConnector;
            isSingleConnector = true;
            return;
        }

        // Fallback: use the target transform itself
        connectorA = target.transform;
        connectorB = target.transform;
        connectorC = target.transform;
        isSingleConnector = true;
    }

    public Transform GetConnector(int index)
    {
        if (index == 2) return connectorC;
        return index == 0 ? connectorA : connectorB;
    }
}

[System.Serializable]
public class WirePoleGroup
{
    public string groupName = "Group";
    public WireNode[] nodes;
}

[ExecuteAlways]
public class WireController : MonoBehaviour
{
    [Header("Pole Groups (Supports Both Utility Poles & Rooftop Electrical)")]
    public WirePoleGroup[] poleGroups;

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
        if (wireMaterial == null)
        {
            Debug.LogWarning("Wire material is not assigned!");
            return;
        }

        if (poleGroups == null || poleGroups.Length == 0)
            return;

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

        CreateWiresFromPoleGroups();

        wiresCreated = true;
    }

    private void CreateWiresFromPoleGroups()
    {
        for (int groupIndex = 0; groupIndex < poleGroups.Length; groupIndex++)
        {
            WirePoleGroup group = poleGroups[groupIndex];
            if (group.nodes == null || group.nodes.Length < 2)
                continue;

            GameObject groupContainer = new GameObject($"Group_{groupIndex}_{group.groupName}");
            groupContainer.transform.SetParent(mainContainer.transform);

            // Find all connectors first
            foreach (var node in group.nodes)
            {
                node.FindConnectors();
            }

            for (int nodeIndex = 0; nodeIndex < group.nodes.Length - 1; nodeIndex++)
            {
                WireNode currentNode = group.nodes[nodeIndex];
                WireNode nextNode = group.nodes[nodeIndex + 1];

                if (currentNode.target == null || nextNode.target == null)
                    continue;

                if (currentNode.connectorA == null || nextNode.connectorA == null)
                    continue;

                // Determine connection strategy
                bool currentIsSingle = currentNode.isSingleConnector;
                bool nextIsSingle = nextNode.isSingleConnector;

                if (!currentIsSingle && !nextIsSingle)
                {
                    // Both have dual connectors - connect A to A, B to B
                    for (int connectorIndex = 0; connectorIndex < 2; connectorIndex++)
                    {
                        Transform sourceConnector = currentNode.GetConnector(connectorIndex);
                        Transform targetConnector = nextNode.GetConnector(connectorIndex);

                        if (sourceConnector == null || targetConnector == null)
                            continue;

                        string connName = connectorIndex == 0 ? "A" : "B";
                        for (int lineNum = 0; lineNum < linesPerConnector; lineNum++)
                        {
                            CreateSingleWire(groupContainer.transform, sourceConnector, targetConnector,
                                $"Wire_Node{nodeIndex}_Conn{connName}_Line{lineNum}");
                        }
                    }
                }
                else
                {
                    // At least one is single connector - use Connector C from utility poles
                    Transform sourceConnector;
                    Transform targetConnector;

                    if (currentIsSingle && nextIsSingle)
                    {
                        // Both single (rooftop to rooftop) - connect them directly
                        sourceConnector = currentNode.connectorA;
                        targetConnector = nextNode.connectorA;
                    }
                    else if (currentIsSingle)
                    {
                        // Current is single (rooftop), next is dual (pole) - use pole's connector C
                        sourceConnector = currentNode.connectorA;
                        targetConnector = nextNode.connectorC;
                    }
                    else
                    {
                        // Current is dual (pole), next is single (rooftop) - use pole's connector C
                        sourceConnector = currentNode.connectorC;
                        targetConnector = nextNode.connectorA;
                    }

                    if (sourceConnector != null && targetConnector != null)
                    {
                        for (int lineNum = 0; lineNum < linesPerConnector; lineNum++)
                        {
                            CreateSingleWire(groupContainer.transform, sourceConnector, targetConnector,
                                $"Wire_Node{nodeIndex}_ConnC_Line{lineNum}");
                        }
                    }
                }
            }
        }
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

    private void CreateSingleWire(Transform parent, Transform source, Transform target, string wireName)
    {
        GameObject wireObj = new GameObject(wireName);
        wireObj.transform.SetParent(parent);

        LineRenderer lr = wireObj.AddComponent<LineRenderer>();
        allLineRenderers.Add(lr);

        lr.material = wireMaterial;
        lr.startWidth = wireWidth;
        lr.endWidth = wireWidth;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        int lineNumber = 0;
        if (wireName.Contains("_Line"))
        {
            string[] parts = wireName.Split(new string[] { "_Line" }, System.StringSplitOptions.None);
            if (parts.Length > 1)
                int.TryParse(parts[1], out lineNumber);
        }

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
        Color[] groupColors = { Color.yellow, Color.cyan, Color.magenta, Color.green, Color.red };

        if (poleGroups == null)
            return;

        for (int g = 0; g < poleGroups.Length; g++)
        {
            WirePoleGroup group = poleGroups[g];
            if (group.nodes == null || group.nodes.Length < 2)
                continue;

            Gizmos.color = groupColors[g % groupColors.Length];

            foreach (var node in group.nodes)
            {
                node.FindConnectors();
            }

            for (int i = 0; i < group.nodes.Length - 1; i++)
            {
                WireNode current = group.nodes[i];
                WireNode next = group.nodes[i + 1];

                if (current.target == null || next.target == null)
                    continue;

                bool currentIsSingle = current.isSingleConnector;
                bool nextIsSingle = next.isSingleConnector;

                if (!currentIsSingle && !nextIsSingle)
                {
                    // Both dual - draw A-A and B-B lines
                    for (int c = 0; c < 2; c++)
                    {
                        Transform source = current.GetConnector(c);
                        Transform target = next.GetConnector(c);
                        if (source != null && target != null)
                            Gizmos.DrawLine(source.position, target.position);
                    }
                }
                else
                {
                    // At least one single - use connector C from poles
                    Transform source, target;
                    if (currentIsSingle && nextIsSingle)
                    {
                        source = current.connectorA;
                        target = next.connectorA;
                    }
                    else if (currentIsSingle)
                    {
                        source = current.connectorA;
                        target = next.connectorC;
                    }
                    else
                    {
                        source = current.connectorC;
                        target = next.connectorA;
                    }

                    if (source != null && target != null)
                    {
                        Gizmos.DrawLine(source.position, target.position);
                        // Draw spheres to indicate single connectors
                        if (currentIsSingle)
                            Gizmos.DrawWireSphere(source.position, 0.15f);
                        if (nextIsSingle)
                            Gizmos.DrawWireSphere(target.position, 0.15f);
                    }
                }
            }
        }
    }
#endif
}