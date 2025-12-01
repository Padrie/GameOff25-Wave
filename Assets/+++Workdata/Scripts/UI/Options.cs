using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class Options : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer mixer;
    public Slider audioSlider;

    [Header("Display")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;

    [Header("Graphics")]
    public Toggle dofToggle;
    public Toggle volumetricFogToggle;
    public Toggle vsyncToggle;
    public VolumeProfile volumeProfile;

    [Header("Graphics Defaults")]
    public bool fullscreenDefault = true;
    public bool dofDefault = true;
    public bool volumetricFogDefault = false;
    public bool vsyncDefault = true;

    [Header("Tabs")]
    public GameObject graphicsTab;
    public GameObject audioTab;
    public GameObject controlsTab;
    public TMP_Text graphicsTabText;
    public TMP_Text audioTabText;
    public TMP_Text controlsTabText;

    [Header("Tab Colors")]
    public Color normalTabColor;
    public Color hoverTabColor;
    public Color selectedTabColor;

    private const int NormalFontSize = 38;
    private const int SelectedFontSize = 44;

    private const string FULLSCREEN_KEY = "Display_Fullscreen";
    private const string DOF_KEY = "Graphics_DOF";
    private const string VOLUMETRIC_FOG_KEY = "Graphics_VolumetricFog";
    private const string VSYNC_KEY = "Graphics_VSync";

    private static readonly Dictionary<string, (int width, int height)> Resolutions = new()
    {
        { "2160p", (3840, 2160) },
        { "1440p", (2560, 1440) },
        { "1080p", (1920, 1080) },
        { "720p", (1280, 720) }
    };

    private enum TabState { Graphics, Audio, Controls }
    private TabState currentTab = TabState.Graphics;


    private void Awake()
    {
        LoadAndApplySettings();
    }

    private void Start()
    {
        SetupResolutionOptions();
        RegisterListeners();
        SelectTab(TabState.Graphics);
    }


    private void RegisterListeners()
    {
        audioSlider.onValueChanged.AddListener(_ => OnSliderValueChanged());
        fullscreenToggle.onValueChanged.AddListener(_ => OnFullscreenToggleChanged());
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        dofToggle.onValueChanged.AddListener(_ => OnDOFToggleChanged());
        volumetricFogToggle.onValueChanged.AddListener(_ => OnVolumetricFogToggleChanged());
        vsyncToggle.onValueChanged.AddListener(_ => OnVSyncToggleChanged());
    }

    private void LoadAndApplySettings()
    {
        fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FULLSCREEN_KEY, fullscreenDefault ? 1 : 0) == 1);
        dofToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(DOF_KEY, dofDefault ? 1 : 0) == 1);
        volumetricFogToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(VOLUMETRIC_FOG_KEY, volumetricFogDefault ? 1 : 0) == 1);
        vsyncToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(VSYNC_KEY, vsyncDefault ? 1 : 0) == 1);

        Screen.fullScreen = fullscreenToggle.isOn;
        ToggleVolumeComponent("Beautify", "depthOfField", dofToggle.isOn);
        ToggleVolumeComponent("VolumetricFog", "enabled", volumetricFogToggle.isOn);
        ApplyVSync(vsyncToggle.isOn);
    }

    #region Audio & Resolution

    public void OnSliderValueChanged()
    {
        mixer.SetFloat("MasterVolume", Mathf.Log10(audioSlider.value) * 20f);
    }

    public void OnFullscreenToggleChanged()
    {
        PlayerPrefs.SetInt(FULLSCREEN_KEY, fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
        Screen.fullScreen = fullscreenToggle.isOn;
    }

    public void OnResolutionChanged(int resolutionIndex)
    {
        if (Application.isEditor) return;

        string resolution = resolutionDropdown.options[resolutionIndex].text;
        if (Resolutions.TryGetValue(resolution, out var res))
        {
            Screen.SetResolution(res.width, res.height, fullscreenToggle.isOn);
        }
    }

    private void SetupResolutionOptions()
    {
        int width = Display.main.systemWidth;
        int height = Display.main.systemHeight;

        resolutionDropdown.ClearOptions();

        List<string> options = new();
        foreach (var res in Resolutions)
        {
            if (width >= res.Value.width && height >= res.Value.height)
                options.Add(res.Key);
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = GetCurrentResolutionIndex(options);
        resolutionDropdown.RefreshShownValue();
    }

    private int GetCurrentResolutionIndex(List<string> options)
    {
        int currentWidth = Screen.currentResolution.width;
        int currentHeight = Screen.currentResolution.height;

        foreach (var res in Resolutions)
        {
            if (currentWidth >= res.Value.width && currentHeight >= res.Value.height && options.Contains(res.Key))
                return options.IndexOf(res.Key);
        }

        return 0;
    }

    #endregion

    #region Graphics Settings

    public void OnDOFToggleChanged()
    {
        PlayerPrefs.SetInt(DOF_KEY, dofToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
        ToggleVolumeComponent("Beautify", "depthOfField", dofToggle.isOn);
    }

    public void OnVolumetricFogToggleChanged()
    {
        PlayerPrefs.SetInt(VOLUMETRIC_FOG_KEY, volumetricFogToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
        ToggleVolumeComponent("VolumetricFog", "enabled", volumetricFogToggle.isOn);
    }

    public void OnVSyncToggleChanged()
    {
        PlayerPrefs.SetInt(VSYNC_KEY, vsyncToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplyVSync(vsyncToggle.isOn);
    }

    private void ApplyVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
    }

    private void ToggleVolumeComponent(string componentName, string fieldName, bool enabled)
    {
        if (volumeProfile == null) return;

        try
        {
            VolumeComponent foundComponent = null;
            foreach (var component in volumeProfile.components)
            {
                if (component.GetType().Name.Contains(componentName))
                {
                    foundComponent = component;
                    break;
                }
            }

            if (foundComponent == null) return;

            var field = foundComponent.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (field == null) return;

            var param = field.GetValue(foundComponent);
            if (param == null) return;

            var mValueField = param.GetType().GetField("m_Value",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (mValueField != null)
            {
                mValueField.SetValue(param, enabled);
            }
            else
            {
                var valueProperty = param.GetType().GetProperty("value",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                valueProperty?.SetValue(param, enabled);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(volumeProfile);
#endif
        }
        catch (Exception) { }
    }

    #endregion

    #region Tab Management

    public void OnGraphicsSelected() => SelectTab(TabState.Graphics);
    public void OnGraphicsHover() => HoverTab(graphicsTabText);
    public void GraphicsReset() => ResetTab(graphicsTabText);

    public void OnAudioSelected() => SelectTab(TabState.Audio);
    public void OnAudioHover() => HoverTab(audioTabText);
    public void AudioReset() => ResetTab(audioTabText);

    public void OnControlsSelected() => SelectTab(TabState.Controls);
    public void OnControlsHover() => HoverTab(controlsTabText);
    public void ControlsReset() => ResetTab(controlsTabText);

    private void SelectTab(TabState tabState)
    {
        currentTab = tabState;

        graphicsTab.SetActive(tabState == TabState.Graphics);
        audioTab.SetActive(tabState == TabState.Audio);
        controlsTab.SetActive(tabState == TabState.Controls);

        ResetTab(graphicsTabText);
        ResetTab(audioTabText);
        ResetTab(controlsTabText);

        TMP_Text selectedText = tabState switch
        {
            TabState.Graphics => graphicsTabText,
            TabState.Audio => audioTabText,
            TabState.Controls => controlsTabText,
            _ => null
        };

        if (selectedText != null) HighlightTab(selectedText);
    }

    private void HoverTab(TMP_Text tabText)
    {
        if (!IsTabSelected(tabText))
            tabText.color = hoverTabColor;
    }

    private void ResetTab(TMP_Text tabText)
    {
        if (IsTabSelected(tabText)) return;
        tabText.fontSize = NormalFontSize;
        tabText.color = normalTabColor;
    }

    private void HighlightTab(TMP_Text tabText)
    {
        tabText.fontSize = SelectedFontSize;
        tabText.color = selectedTabColor;
    }

    private bool IsTabSelected(TMP_Text tabText)
    {
        return (tabText == graphicsTabText && currentTab == TabState.Graphics) ||
               (tabText == audioTabText && currentTab == TabState.Audio) ||
               (tabText == controlsTabText && currentTab == TabState.Controls);
    }

    #endregion
}