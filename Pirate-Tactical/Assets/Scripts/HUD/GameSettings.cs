using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class GameSettings : MonoBehaviour
{
    Resolution[] resolutions;

    [Header("Graphics")]
    [SerializeField] TMP_Dropdown resolutionDropDown;
    [SerializeField] TMP_Dropdown qualityDropDown;
    [SerializeField] Toggle fullscreenToggle;

    [Header("Audio")]
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] Slider SFXSlider;
    [SerializeField] Slider musicSlider;

    [Header("Text")]
    [SerializeField] TMP_Text masterVolumeTxt;
    [SerializeField] TMP_Text SFXVolumeTxt;
    [SerializeField] TMP_Text musicVolumeTxt;

    float masterSound;
    float SFXSound;
    float musicSound;

    private void Awake()
    {
        CheckAllRes();
    }

    void CheckAllRes()
    {
        resolutions = Screen.resolutions;

        resolutionDropDown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolution = 0;


        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " / " + resolutions[i].refreshRateRatio + " hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolution = i;
            }
        }

        resolutionDropDown.AddOptions(options);
        resolutionDropDown.value = currentResolution;
        resolutionDropDown.RefreshShownValue();
    }

    public void Graphics(int graphicsIndex)
    {
        QualitySettings.SetQualityLevel(graphicsIndex);
    }

    public void Resolution(int resoltionIndex)
    {
        Resolution resolution = resolutions[resoltionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void Fullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetSFXVolume(float sliderValue)
    {
        audioMixer.SetFloat("SFX", Mathf.Log10(sliderValue) * 20);

        float displayNumber = sliderValue * 100;
        SFXVolumeTxt.text = "SFX : " + displayNumber.ToString("F0");

        SFXSound = sliderValue;
    }

    public void SetMusicVolume(float sliderValue)
    {
        audioMixer.SetFloat("Music", Mathf.Log10(sliderValue) * 20);

        float displayNumber = sliderValue * 100;
        musicVolumeTxt.text = "musique : " + displayNumber.ToString("F0");

        musicSound = sliderValue;
    }
}
