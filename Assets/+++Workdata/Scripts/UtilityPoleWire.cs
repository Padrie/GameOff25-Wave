using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class UtilityPoleWire : MonoBehaviour
{
    [Header("Connections")]
    [Tooltip("The poles to connect to")]
    public Transform[] targetPoles;

    [Header("Wire Appearance")]
    public Material wireMaterial;
    public float wireWidth = 0.05f;

    [Header("Sag")]
    public bool useSag = true;
    public float sagAmount = 0.3f;
    [Range(5, 50)]
    public int sagSegments = 20;

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

    private GameObject wireContainer;
    private LineRenderer[] lineRenderers;
    private Vector3[][] originalPositions;
    private float[] randomOffsets;
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
            CreateWires();
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
            CreateWires();
        }
    }

#if UNITY_EDITOR
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        //Clean up play mode wires when exiting play mode
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (wireContainer != null)
            {
                DestroyImmediate(wireContainer);
                wireContainer = null;
                wiresCreated = false;
            }
        }
        //Recreate wires when entering edit mode
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            isPlayMode = false;
            CreateWires();
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            isPlayMode = true;
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

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        //Update wires when values change in editor
        if (!Application.isPlaying && HasChanged())
        {
            CreateWires();
        }
    }

    private void OnValidate()
    {
        //Recreate wires when inspector values change
        if (!Application.isPlaying)
        {
            //Delay the wire creation to avoid issues with OnValidate
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) //Check if object still exists
                {
                    CreateWires();
                }
            };
        }
    }

    private bool HasChanged()
    {
        //Check if pole moved or settings changed
        if (targetPoles == null || targetPoles.Length == 0) return false;
        if (!wiresCreated) return true;

        //Simple check - you can expand this
        return false;
    }
#endif

    [ContextMenu("Create Wires")]
    public void CreateWires()
    {
        if (targetPoles == null || targetPoles.Length == 0)
        {
            return;
        }

        if (wireMaterial == null)
        {
            return;
        }

        //Clear existing wires
        ClearWires();

        //Count valid poles (non-null)
        int validPoleCount = 0;
        foreach (Transform pole in targetPoles)
        {
            if (pole != null) validPoleCount++;
        }

        if (validPoleCount == 0)
        {
            return;
        }

        //Create container
        wireContainer = new GameObject($"Wires_{gameObject.name}");
        wireContainer.transform.SetParent(transform);
        wireContainer.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
        //Mark as DontSave in edit mode so it doesn't get saved to the scene
        if (!Application.isPlaying)
        {
            wireContainer.hideFlags = HideFlags.DontSave;
        }
#endif

        //Initialize arrays
        lineRenderers = new LineRenderer[validPoleCount];
        originalPositions = new Vector3[validPoleCount][];
        randomOffsets = new float[validPoleCount];

        //Create wires for each valid pole
        int wireIndex = 0;
        for (int i = 0; i < targetPoles.Length; i++)
        {
            if (targetPoles[i] != null)
            {
                CreateWire(wireIndex, targetPoles[i]);
                randomOffsets[wireIndex] = Random.Range(0f, 100f);
                wireIndex++;
            }
        }

        wiresCreated = true;
    }

    [ContextMenu("Clear Wires")]
    public void ClearWires()
    {
        if (wireContainer != null)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Destroy(wireContainer);
            }
            else
            {
                DestroyImmediate(wireContainer);
            }
#else
            Destroy(wireContainer);
#endif
        }

        wireContainer = null;
        lineRenderers = null;
        originalPositions = null;
        randomOffsets = null;
        wiresCreated = false;
    }

    private void CreateWire(int wireIndex, Transform targetPole)
    {
        GameObject wireObj = new GameObject($"Wire_{wireIndex}_to_{targetPole.name}");
        wireObj.transform.SetParent(wireContainer.transform);

        LineRenderer lr = wireObj.AddComponent<LineRenderer>();
        lineRenderers[wireIndex] = lr;

        //Setup line renderer
        lr.material = wireMaterial;
        lr.startWidth = wireWidth;
        lr.endWidth = wireWidth;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        float currentHeight = 0;

        Vector3 startPos = transform.position + Vector3.up * currentHeight;
        Vector3 endPos = targetPole.position + Vector3.up * currentHeight;

        //Create the wire
        if (useSag)
        {
            lr.positionCount = sagSegments + 1;
            originalPositions[wireIndex] = new Vector3[sagSegments + 1];

            for (int i = 0; i <= sagSegments; i++)
            {
                float t = i / (float)sagSegments;
                Vector3 point = Vector3.Lerp(startPos, endPos, t);

                //Add sag using parabolic curve
                float sag = sagAmount * (1f - Mathf.Pow(2f * t - 1f, 2f));
                point.y -= sag;

                lr.SetPosition(i, point);
                originalPositions[wireIndex][i] = point;
            }
        }
        else
        {
            lr.positionCount = 2;
            originalPositions[wireIndex] = new Vector3[2];

            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);
            originalPositions[wireIndex][0] = startPos;
            originalPositions[wireIndex][1] = endPos;
        }
    }

    private void AnimateWind()
    {
        if (lineRenderers == null || originalPositions == null) return;

        float time = Time.time * windSpeed;
        Vector3 windDir = new Vector3(windDirection.x, 0, windDirection.y).normalized;

        for (int wireIndex = 0; wireIndex < lineRenderers.Length; wireIndex++)
        {
            if (lineRenderers[wireIndex] == null) continue;

            LineRenderer lr = lineRenderers[wireIndex];
            float offset = randomOffsets[wireIndex];

            for (int i = 0; i < lr.positionCount; i++)
            {
                //Calculate wind effect (more in the middle of the wire)
                float t = i / (float)(lr.positionCount - 1);
                float middleEffect = Mathf.Sin(Mathf.PI * t); //0 at ends, 1 in middle

                //Combine multiple sine waves for natural movement
                float windWave1 = Mathf.Sin(time + offset + i * 0.3f);
                float windWave2 = Mathf.Sin(time * 0.7f + offset + i * 0.5f);
                float windWave3 = Mathf.Cos(time * 1.3f + offset + i * 0.2f);

                //Calculate wind offset
                Vector3 windOffset = windDir * windWave1 * middleEffect * windStrength;
                windOffset.y += windWave2 * middleEffect * windStrength * 0.3f; //Vertical sway
                windOffset += Vector3.Cross(windDir, Vector3.up) * windWave3 * middleEffect * windStrength * 0.5f; //Perpendicular sway

                //Apply wind to original position
                lr.SetPosition(i, originalPositions[wireIndex][i] + windOffset);
            }
        }
    }

    private void OnDestroy()
    {
        ClearWires();
    }
}