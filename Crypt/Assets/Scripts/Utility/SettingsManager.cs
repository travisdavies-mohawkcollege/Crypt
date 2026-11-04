using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public float MouseSensitivity { get; private set; }
    public float FieldOfView { get; private set; }
    public bool BobbingEnabled { get; private set; }
    public bool VSyncEnabled { get; private set; }
    public float MasterVolume { get; private set; }
    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }

    public event Action SettingsChanged;

    private void Awake()
    {
        Instance = this;
        Load();
        ApplyGlobalSettings();
    }

    public void Load()
    {
        MouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 0.65f);
        FieldOfView = PlayerPrefs.GetFloat("FieldOfView", 85f);
        BobbingEnabled = PlayerPrefs.GetInt("BobbingEnabled", 1) == 1;
        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1);
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1);
    }


    public void Save(float sensitivity, float fieldOfView, bool bobbingEnabled, float masterVolume, float musicVolume, float sfxVolume)
    {
        MouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 2f);
        FieldOfView = Mathf.Clamp(fieldOfView, 60f, 110f);
        BobbingEnabled = bobbingEnabled;
        MasterVolume = Mathf.Clamp(masterVolume, 0f, 1f);
        MusicVolume = Mathf.Clamp(musicVolume, 0f, 1f);
        SFXVolume = Mathf.Clamp(sfxVolume, 0f, 1f);

        PlayerPrefs.SetFloat("MouseSensitivity", MouseSensitivity);
        PlayerPrefs.SetFloat("FieldOfView", FieldOfView);
        PlayerPrefs.SetInt("BobbingEnabled", BobbingEnabled ? 1 : 0);
        PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
        PlayerPrefs.SetFloat("MusicVolume", MasterVolume);
        PlayerPrefs.SetFloat("SFXVolume", MasterVolume);
        PlayerPrefs.Save();

        SettingsChanged?.Invoke();
        ApplyGlobalSettings();
    }

    private void ApplyGlobalSettings()
    {
        // resolution, VSync, etc.
        AudioListener.volume = MasterVolume;
        if (VSyncEnabled) QualitySettings.vSyncCount = 1;
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}