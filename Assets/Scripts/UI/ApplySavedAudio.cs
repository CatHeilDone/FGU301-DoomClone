using UnityEngine;
using UnityEngine.Audio;

public class ApplySavedAudio : MonoBehaviour
{
    public AudioMixer mixer;

    void Start()
    {
        bool muted = PlayerPrefs.GetInt("Muted", 0) == 1;
        mixer.SetFloat("MasterVolume", muted ? -80f : ToDb("MasterVolume"));
        mixer.SetFloat("MusicVolume", ToDb("MusicVolume"));
        mixer.SetFloat("SFXVolume", ToDb("SFXVolume"));
    }

    float ToDb(string key)
    {
        float v = PlayerPrefs.GetFloat(key, 0.8f);
        return Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;
    }
}