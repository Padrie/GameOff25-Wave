using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    public AudioMixer mixer;
    public Slider audioSlider;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
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

    bool graphicsTabSelected = true;
    bool audioTabSelected = false;
    bool controlsTabSelected = false;

    int normalFontSize = 48;
    int selectedFontSize = 64;

    Resolution[] resolutions;

    private void Start()
    {
        audioSlider.onValueChanged.AddListener(delegate { OnSliderValueChanged(); });
        fullscreenToggle.onValueChanged.AddListener(delegate { OnFullscreenToggleChanged(); });
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        OnGraphicsSelected();
        SetupResolutionOptions();
    }

    void SetupResolutionOptions()
    {
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;

            if (!options.Contains(option))
                options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void OnSliderValueChanged()
    {
        mixer.SetFloat("MasterVolume", Mathf.Log10(audioSlider.value) * 20f);
    }

    public void OnFullscreenToggleChanged()
    {
        Screen.fullScreen = fullscreenToggle.isOn;
    }

    public void OnResolutionChanged(int index)
    {
        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, fullscreenToggle.isOn);
    }

    public void OnGraphicsSelected()
    {
        graphicsTabText.fontSize = selectedFontSize;
        graphicsTabText.color = selectedTabColor;

        graphicsTabSelected = true;
        audioTabSelected = false;
        controlsTabSelected = false;

        graphicsTab.SetActive(true);
        audioTab.SetActive(false);
        controlsTab.SetActive(false);

        AudioReset();
        ControlsReset();
    }

    public void OnGraphicsHover()
    {
        if (graphicsTabSelected) return;

        graphicsTabText.color = hoverTabColor;
    }

    public void GraphicsReset()
    {
        if (graphicsTabSelected) return;

        graphicsTabText.fontSize = normalFontSize;
        graphicsTabText.color = normalTabColor;
    }

    public void OnAudioSelected()
    {
        audioTabText.fontSize = selectedFontSize;
        audioTabText.color = selectedTabColor;

        graphicsTabSelected = false;
        audioTabSelected = true;
        controlsTabSelected = false;

        graphicsTab.SetActive(false);
        audioTab.SetActive(true);
        controlsTab.SetActive(false);

        GraphicsReset();
        ControlsReset();
    }

    public void OnAudioHover()
    {
        if (audioTabSelected) return;

        audioTabText.color = hoverTabColor;
    }

    public void AudioReset()
    {
        if (audioTabSelected) return;

        audioTabText.fontSize = normalFontSize;
        audioTabText.color = normalTabColor;
    }

    public void OnControlsSelected()
    {
        controlsTabText.fontSize = selectedFontSize;
        controlsTabText.color = selectedTabColor;

        graphicsTabSelected = false;
        audioTabSelected = false;
        controlsTabSelected = true;

        graphicsTab.SetActive(false);
        audioTab.SetActive(false);
        controlsTab.SetActive(true);

        GraphicsReset();
        AudioReset();
    }

    public void OnControlsHover()
    {
        if (controlsTabSelected) return;

        controlsTabText.color = hoverTabColor;
    }

    public void ControlsReset()
    {
        if (controlsTabSelected) return;

        controlsTabText.fontSize = normalFontSize;
        controlsTabText.color = normalTabColor;
    }
}
