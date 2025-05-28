using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance; // Singleton instance ekliyoruz

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Toggle Buttons")]
    [SerializeField] private GameObject soundButtonObject;
    [SerializeField] private GameObject musicButtonObject;
    [SerializeField] private Image soundImage;
    [SerializeField] private Image musicImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite offSprite;
    
    [Header("UI Elements")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button musicButton;
    
    private AudioManager audioManager;
    
    private void Awake()
    {
        // Singleton pattern implementasyonu
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if(Application.isEditor==false)
        {
            Debug.unityLogger.logEnabled = false;
        }
    }
    
    private void Start()
    {
        audioManager = AudioManager.Instance;
        settingsPanel.SetActive(false);
        
        if (soundButtonObject != null) soundButtonObject.SetActive(false);
        if (musicButtonObject != null) musicButtonObject.SetActive(false);
        
        // Buton referanslarını otomatik bul
        if(settingsButton == null) settingsButton = GameObject.FindGameObjectWithTag("SettingsButton").GetComponent<Button>();
        if(soundButton == null) soundButton = GameObject.FindGameObjectWithTag("SoundButton").GetComponent<Button>();
        if(musicButton == null) musicButton = GameObject.FindGameObjectWithTag("MusicButton").GetComponent<Button>();

        // Eventleri dinamik olarak bağla
        settingsButton?.onClick.AddListener(ToggleSettingsPanel);
        soundButton?.onClick.AddListener(ToggleSound);
        musicButton?.onClick.AddListener(ToggleMusic);

        // Başlangıç durumunu ayarla
        UpdateButtonStates();
    }
    
    private void UpdateButtonStates()
    {
        if(audioManager == null) audioManager = AudioManager.Instance;
        if(audioManager != null)
        {
            UpdateSoundButtonSprite(audioManager.IsSoundMuted());
            UpdateMusicButtonSprite(audioManager.IsMusicMuted());
        }
    }
    
    private void OnDestroy()
    {
        // Event bağlantılarını temizle
        if(settingsButton != null) settingsButton.onClick.RemoveListener(ToggleSettingsPanel);
        if(soundButton != null) soundButton.onClick.RemoveListener(ToggleSound);
        if(musicButton != null) musicButton.onClick.RemoveListener(ToggleMusic);
    }
    
    public void ToggleSettingsPanel()
    {
        if(settingsPanel == null) settingsPanel = GameObject.Find("SettingsPanel");
        bool isActive = settingsPanel.activeSelf;
        
        if (isActive)
        {
            CloseSettingsPanel();
        }
        else
        {
            OpenSettingsPanel();
        }
    }
    
    public void OpenSettingsPanel()
    {
        settingsPanel.SetActive(true);
        
        if (soundButtonObject != null) soundButtonObject.SetActive(true);
        if (musicButtonObject != null) musicButtonObject.SetActive(true);
    }
    
    public void CloseSettingsPanel()
    {
        settingsPanel.SetActive(false);
        
        if (soundButtonObject != null) soundButtonObject.SetActive(false);
        if (musicButtonObject != null) musicButtonObject.SetActive(false);
    }
    
    public void ToggleSound()
    {
        if(audioManager != null && soundButton != null)
        {
            bool isMuted = audioManager.ToggleMuteSound();
            UpdateSoundButtonSprite(isMuted);
        }
        else
        {
            Debug.LogWarning("AudioManager veya soundButton referansı eksik!");
        }
    }
    
    public void ToggleMusic()
    {
        if (audioManager != null)
        {
            bool isMuted = audioManager.ToggleMuteMusic();
            UpdateMusicButtonSprite(isMuted);
        }
    }
    
    private void UpdateSoundButtonSprite(bool isMuted)
    {
        if (soundImage != null)
        {
            soundImage.sprite = isMuted ? offSprite : soundOnSprite;
        }
    }
    
    private void UpdateMusicButtonSprite(bool isMuted)
    {
        if (musicImage != null)
        {
            musicImage.sprite = isMuted ? offSprite : musicOnSprite;
        }
    }

    // Scene değişimlerinde çalışacak metod
    public void ReinitializeUI()
    {
        StartCoroutine(ReinitializeUICoroutine());
    }

    private IEnumerator ReinitializeUICoroutine()
    {
        yield return new WaitForEndOfFrame();
        Start(); // UI elementleri yeniden başlat
    }

    public void ClearButtonReferences()
    {
        // Tüm buton referanslarını ve eventlerini temizle
        if(settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton = null;
        }
        if(soundButton != null)
        {
            soundButton.onClick.RemoveAllListeners();
            soundButton = null;
        }
        if(musicButton != null)
        {
            musicButton.onClick.RemoveAllListeners();
            musicButton = null;
        }
        
        // UI elementlerini yeniden bul
        InitializeUI();
    }

    public void InitializeUI()
    {
        settingsButton = GameObject.FindGameObjectWithTag("SettingsButton")?.GetComponent<Button>();
        soundButton = GameObject.FindGameObjectWithTag("SoundButton")?.GetComponent<Button>();
        musicButton = GameObject.FindGameObjectWithTag("MusicButton")?.GetComponent<Button>();
        
        settingsButton?.onClick.AddListener(ToggleSettingsPanel);
        soundButton?.onClick.AddListener(ToggleSound);
        musicButton?.onClick.AddListener(ToggleMusic);
    }

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
        InitializeUIElements();
        UpdateButtonStates();
    }

    private void InitializeUIElements()
    {
        StartCoroutine(InitializeUIAfterFrame());
    }

    private IEnumerator InitializeUIAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        
        // Settings Panel
        if(settingsPanel == null)
        {
            settingsPanel = GameObject.Find("SettingsPanel");
            if(settingsPanel == null) Debug.LogWarning("SettingsPanel bulunamadı!");
        }

        // Butonları yeniden bağla
        BindButton(ref settingsButton, "SettingsButton", ToggleSettingsPanel);
        BindButton(ref soundButton, "SoundButton", ToggleSound);
        BindButton(ref musicButton, "MusicButton", ToggleMusic);

        // Görsel elementleri kontrol et
        CheckVisualComponents();
    }

    private void BindButton(ref Button button, string tag, UnityEngine.Events.UnityAction action)
    {
        if(button == null)
        {
            GameObject btnObj = GameObject.FindGameObjectWithTag(tag);
            if(btnObj != null)
            {
                button = btnObj.GetComponent<Button>();
                if(button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(action);
                }
            }
        }
    }

    private void CheckVisualComponents()
    {
        if(soundImage == null) soundImage = GameObject.Find("SoundImage")?.GetComponent<Image>();
        if(musicImage == null) musicImage = GameObject.Find("MusicImage")?.GetComponent<Image>();
        
        UpdateButtonStates();
    }
} 