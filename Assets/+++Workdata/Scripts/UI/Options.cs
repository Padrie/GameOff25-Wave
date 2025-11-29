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
        int width = Display.main.systemWidth;
        int height = Display.main.systemHeight;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        if (width >= 3840 && height >= 2160) options.Add("2160p");
        if (width >= 2560 && height >= 1440) options.Add("1440p");
        if (width >= 1920 && height >= 1080) options.Add("1080p");
        if (width >= 1280 && height >= 720) options.Add("720p");

        resolutionDropdown.AddOptions(options);

        int currentResolutionIndex = GetCurrentResolutionIndex(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    int GetCurrentResolutionIndex(List<string> options)
    {
        int currentWidth = Screen.currentResolution.width;
        int currentHeight = Screen.currentResolution.height;

        if (currentWidth >= 3840 && currentHeight >= 2160 && options.Contains("2160p"))
            return options.IndexOf("2160p");
        else if (currentWidth >= 2560 && currentHeight >= 1440 && options.Contains("1440p"))
            return options.IndexOf("1440p");
        else if (currentWidth >= 1920 && currentHeight >= 1080 && options.Contains("1080p"))
            return options.IndexOf("1080p");
        else if (currentWidth >= 1280 && currentHeight >= 720 && options.Contains("720p"))
            return options.IndexOf("720p");

        return 0;
    }

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
        if (!Application.isEditor)
        {
            string resolution = resolutionDropdown.options[resolutionIndex].text;

            switch (resolution)
            {
                case "2160p":
                    Screen.SetResolution(3840, 2160, fullscreenToggle.isOn);
                    break;
                case "1440p":
                    Screen.SetResolution(2560, 1440, fullscreenToggle.isOn);
                    break;
                case "1080p":
                    Screen.SetResolution(1920, 1080, fullscreenToggle.isOn);
                    break;
                case "720p":
                    Screen.SetResolution(1280, 720, fullscreenToggle.isOn);
                    break;
            }
        }
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