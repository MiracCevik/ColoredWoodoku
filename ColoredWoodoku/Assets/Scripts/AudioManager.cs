using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource soundSource;

    private float musicVolume = 0.5f;
    private float soundVolume = 0.5f;
    
    private bool isSoundOn = true;
    private bool isMusicOn = true;

    private bool isSoundMuted = false;
    private bool isMusicMuted = false;

    [Header("Sahne Müzikleri")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private AudioClip onlineMusic;

    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Image musicButtonImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = true;
            }
            
            if (soundSource == null)
            {
                soundSource = gameObject.AddComponent<AudioSource>();
                soundSource.loop = false;
                soundSource.playOnAwake = false;
            }

            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateVolume();
        PlaySceneMusic(SceneManager.GetActiveScene().name);
    }

    private void LoadSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        soundVolume = PlayerPrefs.GetFloat("SoundVolume", 0.5f);
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        isSoundMuted = PlayerPrefs.GetInt("SoundMuted", 0) == 1;
        isMusicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SoundVolume", soundVolume);
        PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.SetInt("MusicOn", isMusicOn ? 1 : 0);
        PlayerPrefs.SetInt("SoundMuted", isSoundMuted ? 1 : 0);
        PlayerPrefs.SetInt("MusicMuted", isMusicMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        UpdateVolume();
        SaveSettings();
    }

    public void SetSoundVolume(float volume)
    {
        soundVolume = volume;
        UpdateVolume();
        SaveSettings();
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetSoundVolume()
    {
        return soundVolume;
    }
    
    public bool GetSoundState()
    {
        return isSoundOn;
    }
    
    public bool GetMusicState()
    {
        return isMusicOn;
    }
    
    public bool ToggleSound()
    {
        isSoundOn = !isSoundOn;
        soundSource.mute = !isSoundOn;
        SaveSettings();
        return isSoundOn;
    }
    
    public bool ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        musicSource.mute = !isMusicOn;
        SaveSettings();
        return isMusicOn;
    }

    private void UpdateVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
            musicSource.mute = !isMusicOn;
        }
        
        if (soundSource != null)
        {
            soundSource.volume = soundVolume;
            soundSource.mute = !isSoundOn;
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (soundSource != null && clip != null && isSoundOn)
        {
            soundSource.PlayOneShot(clip);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource != null && clip != null)
        {
            musicSource.clip = clip;
            if (isMusicOn)
            {
                musicSource.Play();
            }
        }
    }

    public bool ToggleMuteSound()
    {
        isSoundMuted = !isSoundMuted;
        soundSource.mute = isSoundMuted;
        PlayerPrefs.SetInt("SoundMuted", isSoundMuted ? 1 : 0);
        return isSoundMuted;
    }

    public bool ToggleMuteMusic()
    {
        isMusicMuted = !isMusicMuted;
        musicSource.mute = isMusicMuted;
        PlayerPrefs.SetInt("MusicMuted", isMusicMuted ? 1 : 0);
        return isMusicMuted;
    }

    public bool IsSoundMuted() => isSoundMuted;
    public bool IsMusicMuted() => isMusicMuted;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneMusic(scene.name);
    }

    private void PlaySceneMusic(string sceneName)
    {
        musicSource.Stop();
        switch(sceneName)
        {
            case "Main Menu":
                PlayMusic(mainMenuMusic);
                break;
            case "Game":
                PlayMusic(gameMusic);
                break;
            case "Online":
                PlayMusic(onlineMusic);
                break;
            default:
                PlayMusic(mainMenuMusic);
                break;
        }
    }

    public void ToggleMusicPlayback()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
            musicButtonImage.sprite = musicOffSprite;
        }
        else
        {
            musicSource.Play();
            musicButtonImage.sprite = musicOnSprite;
        }
    }

    IEnumerator FadeMusic(AudioClip newClip)
    {
        float duration = 1f;
        float currentTime = 0;
        
        float startVolume = musicSource.volume;
        
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0, currentTime/duration);
            yield return null;
        }
        
        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.volume = startVolume;
        musicSource.Play();
    }

    public void LoadOnlineScene()
    {
        SceneManager.LoadScene("Online");
    }

    public void LoadOfflineScene()
    {
        SceneManager.LoadScene("Game");
    }

    public void LoadScene(string sceneName)
    {
        // Temizlik yapmaya gerek yok, yeni sahne yüklendiğinde otomatik bağlanacak
        SceneManager.LoadScene(sceneName);
    }
} 