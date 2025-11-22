using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class LightShadowTierManager : MonoBehaviour
{
    public enum ShadowTier
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public bool autoApply = true;
    public bool includeInactiveLights = true;

    public ShadowTier directionalTier = ShadowTier.High;
    public ShadowTier spotTier = ShadowTier.High;
    public ShadowTier pointTier = ShadowTier.Medium;
    public ShadowTier areaTier = ShadowTier.Low;

    static FieldInfo tierField;

    void CacheField()
    {
        if (tierField != null) return;
        var t = typeof(UniversalAdditionalLightData);
        tierField = t.GetField("m_AdditionalLightsShadowResolutionTier",
            BindingFlags.Instance | BindingFlags.NonPublic);
    }

    void OnEnable()
    {
        CacheField();
        if (autoApply) ApplyToAllLights();
    }

    void OnValidate()
    {
        CacheField();
        if (autoApply) ApplyToAllLights();
    }

    void Update()
    {
        if (!autoApply) return;
        ApplyToAllLights();
    }

    ShadowTier GetTierForLight(Light light)
    {
        switch (light.type)
        {
            case LightType.Directional: return directionalTier;
            case LightType.Spot: return spotTier;
            case LightType.Point: return pointTier;
            default: return spotTier;
        }
    }

    [ContextMenu("Apply To All Lights")]
    public void ApplyToAllLights()
    {
        CacheField();
        if (tierField == null) return;

        var lights = FindObjectsOfType<Light>(includeInactiveLights);

        foreach (var light in lights)
        {
            if (light == null) continue;

            var data = light.GetComponent<UniversalAdditionalLightData>();
            if (data == null) continue;

            var tier = GetTierForLight(light);
            data.usePipelineSettings = false;
            tierField.SetValue(data, (int)tier);
        }
    }
}
