using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public float sensitivity;
    public int fieldOfView;
    public bool viewBobbing;
    public bool vSync;
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;
    [SerializeField] private Slider sensSlider;
    [SerializeField] private Slider fovSlider;
    [SerializeField] private Toggle bobbleToggle;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sensLabel;
    [SerializeField] private TextMeshProUGUI fovLabel;
    [SerializeField] private TextMeshProUGUI masterLabel;
    [SerializeField] private TextMeshProUGUI musicLabel;
    [SerializeField] private TextMeshProUGUI sfxLabel;

    public void Awake()
    {
        sensSlider.value = SettingsManager.Instance.MouseSensitivity;
        
        fovSlider.value = SettingsManager.Instance.FieldOfView;
        
        bobbleToggle.isOn = SettingsManager.Instance.BobbingEnabled;
        vsyncToggle.isOn = SettingsManager.Instance.VSyncEnabled;
        masterSlider.value = SettingsManager.Instance.MasterVolume;

        musicSlider.value = SettingsManager.Instance.MusicVolume;
        sfxSlider.value = SettingsManager.Instance.SFXVolume;
    }

    public void Update()
    {
        sensLabel.text = $"{sensSlider.value}";
        fovLabel.text = $"{fovSlider.value}";
        int masterPercentage = Mathf.RoundToInt(masterSlider.value*100f);
        masterLabel.text = $"{masterPercentage}%";
        int musicPercentage = Mathf.RoundToInt(musicSlider.value*100f);
        musicLabel.text = $"{musicPercentage}%";
        int sfxPercentage = Mathf.RoundToInt(sfxSlider.value*100f);
        sfxLabel.text = $"{sfxPercentage}%";
    }

    public void ApplySettings()
    {
        sensitivity = sensSlider.value;
        fieldOfView = Mathf.RoundToInt(fovSlider.value);
        viewBobbing = bobbleToggle.isOn;
        vSync = vsyncToggle.isOn;
        masterVolume = masterSlider.value;
        musicVolume = musicSlider.value;
        sfxVolume = sfxSlider.value;
        SettingsManager.Instance.Save(sensitivity, fieldOfView, viewBobbing, masterVolume, musicVolume, sfxVolume);
    }

    



}
