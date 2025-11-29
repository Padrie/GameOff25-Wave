using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TMPGlimmerEffect : MonoBehaviour
{
    [Header("Glimmer Settings")]
    [SerializeField] private Color glimmerColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField, Range(0f, 5f)] private float glimmerSpeed = 1f;
    [SerializeField, Range(0.01f, 1f)] private float glimmerWidth = 0.2f;
    [SerializeField, Range(-180f, 180f)] private float glimmerAngle = 45f;
    [SerializeField, Range(1f, 20f)] private float glimmerSharpness = 5f;
    [SerializeField, Range(0f, 2f)] private float glimmerIntensity = 1f;

    [Header("Animation")]
    [SerializeField] private bool animateOnStart = true;
    [SerializeField] private float delayBetweenGlimmers = 3f;
    [SerializeField] private bool continuousGlimmer = false;

    [Header("Face Color")]
    [SerializeField] private Color faceColor = Color.white;

    private TMP_Text tmpText;
    private Material material;
    private float nextGlimmerTime;
    private bool isGlimmering = false;

    // Shader property IDs for better performance
    private static readonly int GlimmerColorID = Shader.PropertyToID("_GlimmerColor");
    private static readonly int GlimmerSpeedID = Shader.PropertyToID("_GlimmerSpeed");
    private static readonly int GlimmerWidthID = Shader.PropertyToID("_GlimmerWidth");
    private static readonly int GlimmerAngleID = Shader.PropertyToID("_GlimmerAngle");
    private static readonly int GlimmerSharpnessID = Shader.PropertyToID("_GlimmerSharpness");
    private static readonly int GlimmerIntensityID = Shader.PropertyToID("_GlimmerIntensity");
    private static readonly int GlimmerOffsetID = Shader.PropertyToID("_GlimmerOffset");
    private static readonly int FaceColorID = Shader.PropertyToID("_FaceColor");

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        InitializeMaterial();
    }

    void Start()
    {
        if (animateOnStart)
        {
            if (continuousGlimmer)
            {
                StartContinuousGlimmer();
            }
            else
            {
                nextGlimmerTime = Time.time + delayBetweenGlimmers;
            }
        }
    }

    void InitializeMaterial()
    {
        // Create a unique material instance
        if (tmpText.fontMaterial != null)
        {
            material = new Material(tmpText.fontMaterial);
            tmpText.fontMaterial = material;
            UpdateMaterialProperties();
        }
        else
        {
            Debug.LogError("TMP Text doesn't have a material assigned!");
        }
    }

    void Update()
    {
        if (!continuousGlimmer && !isGlimmering && Time.time >= nextGlimmerTime)
        {
            TriggerGlimmer();
        }
    }

    void UpdateMaterialProperties()
    {
        if (material == null) return;

        material.SetColor(GlimmerColorID, glimmerColor);
        material.SetFloat(GlimmerSpeedID, glimmerSpeed);
        material.SetFloat(GlimmerWidthID, glimmerWidth);
        material.SetFloat(GlimmerAngleID, glimmerAngle);
        material.SetFloat(GlimmerSharpnessID, glimmerSharpness);
        material.SetFloat(GlimmerIntensityID, glimmerIntensity);
        material.SetColor(FaceColorID, faceColor);
    }

    public void TriggerGlimmer()
    {
        if (isGlimmering) return;

        StartCoroutine(GlimmerCoroutine());
    }

    private System.Collections.IEnumerator GlimmerCoroutine()
    {
        isGlimmering = true;

        // Reset offset
        material.SetFloat(GlimmerOffsetID, -glimmerWidth * 2);

        // Wait for glimmer to complete
        float duration = (2f + glimmerWidth * 4) / glimmerSpeed;
        yield return new WaitForSeconds(duration);

        isGlimmering = false;
        nextGlimmerTime = Time.time + delayBetweenGlimmers;
    }

    public void StartContinuousGlimmer()
    {
        continuousGlimmer = true;
        material.SetFloat(GlimmerSpeedID, glimmerSpeed);
    }

    public void StopGlimmer()
    {
        continuousGlimmer = false;
        StopAllCoroutines();
        isGlimmering = false;
    }

    public void SetGlimmerColor(Color color)
    {
        glimmerColor = color;
        material.SetColor(GlimmerColorID, color);
    }

    public void SetGlimmerSpeed(float speed)
    {
        glimmerSpeed = speed;
        material.SetFloat(GlimmerSpeedID, speed);
    }

    public void SetGlimmerIntensity(float intensity)
    {
        glimmerIntensity = intensity;
        material.SetFloat(GlimmerIntensityID, intensity);
    }

    // Update properties in real-time in the editor
    void OnValidate()
    {
        if (Application.isPlaying && material != null)
        {
            UpdateMaterialProperties();
        }
    }

    void OnDestroy()
    {
        // Clean up material instance
        if (material != null)
        {
            Destroy(material);
        }
    }
}