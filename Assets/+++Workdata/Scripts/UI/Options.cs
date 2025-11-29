using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Options : MonoBehaviour
{
    public AudioMixer mixer;
    public Slider audioSlider;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    [Space(10)]
    public Toggle dofToggle;
    public Toggle volumetricFogToggle;
    [Space(10)]
    public VolumeProfile volumeProfile;
    [Space(10)]
    public GameObject graphicsTab;
    public GameObject audioTab;
    public GameObject controlsTab;

    public TMP_Text graphicsTabText;
    public TMP_Text audioTabText;
    public TMP_Text controlsTabText;

    public Color normalTabColor;
    public Color hoverTabColor;
    public Color selectedTabColor;

    int normalFontSize = 48;
    int selectedFontSize = 64;

    private enum TabState { Graphics, Audio, Controls }
    private TabState currentTab = TabState.Graphics;

    private const string DOF_SETTING_KEY = "Graphics_DOF_Enabled";
    private const string VOLUMETRIC_FOG_SETTING_KEY = "Graphics_VolumetricFog_Enabled";

    private static readonly Dictionary<string, (int width, int height)> Resolutions = new Dictionary<string, (int, int)>
    {
        { "2160p", (3840, 2160) },
        { "1440p", (2560, 1440) },
        { "1080p", (1920, 1080) },
        { "720p", (1280, 720) }
    };

    private void Start()
    {
        audioSlider.onValueChanged.AddListener(delegate { OnSliderValueChanged(); });
        fullscreenToggle.onValueChanged.AddListener(delegate { OnFullscreenToggleChanged(); });
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        dofToggle.onValueChanged.AddListener(delegate { OnDOFToggleChanged(); });
        volumetricFogToggle.onValueChanged.AddListener(delegate { OnVolumetricFogToggleChanged(); });

        SelectTab(TabState.Graphics);
        SetupResolutionOptions();
        LoadAndApplySettings();
    }

    private void LoadAndApplySettings()
    {
        dofToggle.isOn = PlayerPrefs.GetInt(DOF_SETTING_KEY, 1) == 1;
        volumetricFogToggle.isOn = PlayerPrefs.GetInt(VOLUMETRIC_FOG_SETTING_KEY, 1) == 1;
    }

    #region Audio & Resolution

    public void OnSliderValueChanged()
    {
        mixer.SetFloat("MasterVolume", Mathf.Log10(audioSlider.value) * 20f);
    }

    public void OnFullscreenToggleChanged()
    {
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

        List<string> options = new List<string>();
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

    public void OnDOFToggleChanged() => ToggleSetting(DOF_SETTING_KEY, dofToggle.isOn, "Beautify", "depthOfField");

    public void OnVolumetricFogToggleChanged() => ToggleSetting(VOLUMETRIC_FOG_SETTING_KEY, volumetricFogToggle.isOn, "VolumetricFog", "enabled");

    private void ToggleSetting(string settingKey, bool enabled, string componentName, string fieldName)
    {
        if (volumeProfile == null) return;

        PlayerPrefs.SetInt(settingKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        ToggleVolumeComponent(componentName, fieldName, enabled);
    }

    private void ToggleVolumeComponent(string componentName, string fieldName, bool enabled)
    {
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
        tabText.fontSize = normalFontSize;
        tabText.color = normalTabColor;
    }

    private void HighlightTab(TMP_Text tabText)
    {
        tabText.fontSize = selectedFontSize;
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