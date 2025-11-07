using System.IO;
using System.Linq;
using UnityEngine;

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
    private string[] bgmFiles;
    private int currentBgmIndex = 7;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllBGMs();
    }

    private void Start()
    {
        PlayBackgroundMusic(AudioConstants.BGMPath);
    }


    private void LoadAllBGMs()
    {
        bgmFiles = Resources.LoadAll<AudioClip>("Audio/BGM")
                            .Select(c => $"Audio/BGM/{c.name}")
                            .ToArray();

        Debug.Log($"[AudioManager] Loaded {bgmFiles.Length} BGM tracks.");
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

        GameUIControl.Instance.SetBGMTitle();
    }

    public void PlayClick(string sfxPath = AudioConstants.ClickSFXPath)
    {
        AudioClip sfxClip = LoadAudioFromResources(sfxPath);
        if (sfxClip == null) return;

        sfxAudio.PlayOneShot(sfxClip, AudioConstants.SFXVolume);
    }


    public void PlayNextBGM()
    {
        if (bgmFiles == null || bgmFiles.Length == 0) return;
        currentBgmIndex = (currentBgmIndex + 1) % bgmFiles.Length;
        PlayBackgroundMusic(bgmFiles[currentBgmIndex]);
    }

    public void PlayPreviousBGM()
    {
        if (bgmFiles == null || bgmFiles.Length == 0) return;
        currentBgmIndex = (currentBgmIndex - 1 + bgmFiles.Length) % bgmFiles.Length;
        PlayBackgroundMusic(bgmFiles[currentBgmIndex]);
    }

    public string GetCurrentBGMName()
    {
        if (bgmFiles == null || bgmFiles.Length == 0 || currentBgmIndex < 0)
            return "No BGM Playing";

        string fullPath = bgmFiles[currentBgmIndex];
        return Path.GetFileName(fullPath); 
    }

}
