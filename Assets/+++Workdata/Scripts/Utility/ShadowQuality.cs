using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections.Generic;
using System.Collections;

[ExecuteAlways]
public class ShadowQuality : MonoBehaviour
{
    public enum ShadowTier
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    private static List<ShadowQuality> allInstances = new List<ShadowQuality>();
    private static ShadowTier globalTier = ShadowTier.High;
    private static int globalTierVersion = 0;

    public bool autoApply = true;
    public bool includeInactiveLights = true;
    public ShadowTier directionalTier = ShadowTier.High;
    public ShadowTier spotTier = ShadowTier.High;
    public ShadowTier pointTier = ShadowTier.Medium;
    public ShadowTier areaTier = ShadowTier.Low;

    public TMP_Dropdown shadowQualityDropdown;
    public UniversalRenderPipelineAsset urpAsset;

    static FieldInfo tierField;
    static FieldInfo shadowAtlasField;

    private const string SHADOW_QUALITY_KEY = "ShadowQuality";
    private int localTierVersion = -1;
    private bool needsRefresh = false;

    void CacheField()
    {
        if (tierField != null && shadowAtlasField != null) return;

        var t = typeof(UniversalAdditionalLightData);
        tierField = t.GetField("m_AdditionalLightsShadowResolutionTier",
            BindingFlags.Instance | BindingFlags.NonPublic);

        var urpType = typeof(UniversalRenderPipelineAsset);
        shadowAtlasField = urpType.GetField("m_AdditionalLightsShadowmapResolution",
            BindingFlags.Instance | BindingFlags.NonPublic);
    }

    void OnEnable()
    {
        if (!allInstances.Contains(this))
        {
            allInstances.Add(this);
        }

        CacheField();

        if (urpAsset == null)
        {
            urpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        }

        SyncWithGlobalTier();
        if (autoApply) ApplyToAllLights();
    }

    void OnDisable()
    {
        allInstances.Remove(this);
    }

    void Start()
    {
        if (shadowQualityDropdown != null)
        {
            shadowQualityDropdown.ClearOptions();
            shadowQualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "Low", "Medium", "High" });
            shadowQualityDropdown.onValueChanged.AddListener(OnShadowQualityChanged);
        }

        LoadSettings();
    }

    void OnValidate()
    {
        CacheField();
        if (autoApply) ApplyToAllLights();
    }

    void Update()
    {
        if (localTierVersion != globalTierVersion)
        {
            SyncWithGlobalTier();
            ApplyToAllLightsWithRefresh();
            needsRefresh = false;
        }

        if (!Application.isPlaying && !autoApply) return;
        if (Application.isPlaying && autoApply && !needsRefresh)
        {
            ApplyToAllLights();
        }
    }

    void SyncWithGlobalTier()
    {
        directionalTier = globalTier;
        spotTier = globalTier;
        pointTier = globalTier;
        areaTier = globalTier;
        localTierVersion = globalTierVersion;
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

    void SetShadowAtlasResolution(int resolution)
    {
        if (urpAsset == null || shadowAtlasField == null) return;
        shadowAtlasField.SetValue(urpAsset, resolution);
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

    void ApplyToAllLightsWithRefresh()
    {
        CacheField();
        if (tierField == null) return;

        needsRefresh = true;

        switch (globalTier)
        {
            case ShadowTier.Low:
                SetShadowAtlasResolution(1024);
                break;
            case ShadowTier.Medium:
                SetShadowAtlasResolution(2048);
                break;
            case ShadowTier.High:
                SetShadowAtlasResolution(4096);
                break;
        }

        var lights = FindObjectsOfType<Light>(includeInactiveLights);
        foreach (var light in lights)
        {
            if (light == null) continue;
            var data = light.GetComponent<UniversalAdditionalLightData>();
            if (data == null) continue;
            var tier = GetTierForLight(light);
            data.usePipelineSettings = false;
            tierField.SetValue(data, (int)tier);

            if (Application.isPlaying && light.enabled)
            {
                StartCoroutine(RefreshLight(light));
            }
        }
    }

    IEnumerator RefreshLight(Light light)
    {
        light.enabled = false;
        yield return null;
        light.enabled = true;
    }

    public void OnShadowQualityChanged(int qualityIndex)
    {
        ShadowTier selectedTier = (ShadowTier)qualityIndex;

        globalTier = selectedTier;
        globalTierVersion++;

        SaveSettings(qualityIndex);
    }

    void SaveSettings(int qualityIndex)
    {
        PlayerPrefs.SetInt(SHADOW_QUALITY_KEY, qualityIndex);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        if (PlayerPrefs.HasKey(SHADOW_QUALITY_KEY))
        {
            int savedQuality = PlayerPrefs.GetInt(SHADOW_QUALITY_KEY);
            ShadowTier loadedTier = (ShadowTier)savedQuality;

            globalTier = loadedTier;
            directionalTier = loadedTier;
            spotTier = loadedTier;
            pointTier = loadedTier;
            areaTier = loadedTier;
            localTierVersion = globalTierVersion;

            if (shadowQualityDropdown != null)
            {
                shadowQualityDropdown.value = savedQuality;
                shadowQualityDropdown.RefreshShownValue();
            }

            ApplyToAllLightsWithRefresh();
        }
    }
}