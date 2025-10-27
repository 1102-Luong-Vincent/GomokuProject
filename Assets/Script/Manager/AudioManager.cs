using UnityEngine;
using System.IO;

public static class AudioConstants
{
    public const float BGMVolume = 0.5f;
    public const float SFXVolume = 1f;

    public const string BGMPath = "Audio/BGM/08-Bittersweet Village Theme"; 
    public const string ClickSFXPath = "Audio/SFX/SFX_FastUiClickWood03"; 
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmAudio;
    [SerializeField] private AudioSource sfxAudio;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayBackgroundMusic(AudioConstants.BGMPath);
    }

    private AudioClip LoadAudioFromResources(string relativePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(relativePath);
        return clip;
    }

    public void PlayBackgroundMusic(string bgmPath)
    {
        AudioClip bgmClip = LoadAudioFromResources(bgmPath);
        if (bgmClip == null) return;

        bgmAudio.clip = bgmClip;
        bgmAudio.volume = AudioConstants.BGMVolume;
        bgmAudio.loop = true;
        bgmAudio.Play();
    }

    public void PlayClick(string sfxPath = AudioConstants.ClickSFXPath)
    {
        AudioClip sfxClip = LoadAudioFromResources(sfxPath);
        if (sfxClip == null) return;

        sfxAudio.PlayOneShot(sfxClip, AudioConstants.SFXVolume);
    }
}
