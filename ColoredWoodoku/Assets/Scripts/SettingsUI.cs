using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Toggle Buttons")]
    [SerializeField] private GameObject soundButton;
    [SerializeField] private GameObject musicButton;
    [SerializeField] private Image soundImage;
    [SerializeField] private Image musicImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite offSprite;
    
    private AudioManager audioManager;
    
    private void Awake()
    {
        if(Application.isEditor==false)
        {
            Debug.unityLogger.logEnabled = false;
        }
    }
    
    private void Start()
    {
        audioManager = AudioManager.Instance;
        settingsPanel.SetActive(false);
        
        if (soundButton != null) soundButton.SetActive(false);
        if (musicButton != null) musicButton.SetActive(false);
        
        if (audioManager != null)
        {
            audioManager.SetMusicVolume(0.5f);
            audioManager.SetSoundVolume(0.5f);
            audioManager.ToggleMusic();
            audioManager.ToggleSound();

            UpdateSoundButtonSprite(false);
            UpdateMusicButtonSprite(false);
        }
    }
    
    public void ToggleSettingsPanel()
    {
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
        
        if (soundButton != null) soundButton.SetActive(true);
        if (musicButton != null) musicButton.SetActive(true);
    }
    
    public void CloseSettingsPanel()
    {
        settingsPanel.SetActive(false);
        
        if (soundButton != null) soundButton.SetActive(false);
        if (musicButton != null) musicButton.SetActive(false);
    }
    
    public void ToggleSound()
    {
        if (audioManager != null)
        {
            bool isMuted = audioManager.ToggleMuteSound();
            UpdateSoundButtonSprite(isMuted);
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
} 