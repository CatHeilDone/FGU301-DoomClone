using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsMenu : MonoBehaviour
{
    public AudioMixer mixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle muteToggle;

    void Start()
    {
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        muteToggle.isOn = PlayerPrefs.GetInt("Muted", 0) == 1;

        masterSlider.onValueChanged.AddListener(SetMaster);
        musicSlider.onValueChanged.AddListener(SetMusic);
        sfxSlider.onValueChanged.AddListener(SetSFX);
        muteToggle.onValueChanged.AddListener(SetMute);

        SetMaster(masterSlider.value);
        SetMusic(musicSlider.value);
        SetSFX(sfxSlider.value);
        SetMute(muteToggle.isOn);
    }

    float ToDecibels(float value)
    {
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
    }

    public void SetMaster(float v)
    {
        PlayerPrefs.SetFloat("MasterVolume", v);
        if (!muteToggle.isOn) mixer.SetFloat("MasterVolume", ToDecibels(v));
    }

    public void SetMusic(float v)
    {
        PlayerPrefs.SetFloat("MusicVolume", v);
        mixer.SetFloat("MusicVolume", ToDecibels(v));
    }

    public void SetSFX(float v)
    {
        PlayerPrefs.SetFloat("SFXVolume", v);
        mixer.SetFloat("SFXVolume", ToDecibels(v));
    }

    public void SetMute(bool muted)
    {
        PlayerPrefs.SetInt("Muted", muted ? 1 : 0);
        mixer.SetFloat("MasterVolume", muted ? -80f : ToDecibels(masterSlider.value));
    }

    void OnDisable()
    {
        PlayerPrefs.Save();
    }
}