using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio Mixer (Optional)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [SerializeField] private string soundVolumeParameter = "SoundVolume";

    [Header("Audio Sources (Fallback if no mixer)")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource soundAudioSource;

    [Header("UI References")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle soundToggle;

    [Header("Settings")]
    [SerializeField] private bool musicOnByDefault = true;
    [SerializeField] private bool soundOnByDefault = true;

    private const string MUSIC_KEY = "Settings_Music";
    private const string SOUND_KEY = "Settings_Sound";

    public static SettingsManager Instance { get; private set; }

    private bool isMusicOn = true;
    private bool isSoundOn = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    private void Start()
    {
        SetupToggles();
        ApplySettings();
    }

    private void SetupToggles()
    {
        // Setup music toggle
        if (musicToggle != null)
        {
            musicToggle.isOn = isMusicOn;
            musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }

        // Setup sound toggle
        if (soundToggle != null)
        {
            soundToggle.isOn = isSoundOn;
            soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        }
    }

    // Method để refresh toggles khi settings panel được mở (gọi từ PauseMenuUI)
    public void RefreshToggles()
    {
        SetupToggles();
    }

    public void OnMusicToggleChanged(bool value)
    {
        isMusicOn = value;
        ApplyMusicSettings();
        SaveSettings();
    }

    public void OnSoundToggleChanged(bool value)
    {
        isSoundOn = value;
        ApplySoundSettings();
        SaveSettings();
    }

    private void ApplySettings()
    {
        ApplyMusicSettings();
        ApplySoundSettings();
    }

    private void ApplyMusicSettings()
    {
        float volume = isMusicOn ? 0f : -80f; // 0 = max, -80 = muted

        if (audioMixer != null && !string.IsNullOrEmpty(musicVolumeParameter))
        {
            audioMixer.SetFloat(musicVolumeParameter, volume);
        }
        else if (musicAudioSource != null)
        {
            musicAudioSource.mute = !isMusicOn;
        }
    }

    private void ApplySoundSettings()
    {
        float volume = isSoundOn ? 0f : -80f; // 0 = max, -80 = muted

        if (audioMixer != null && !string.IsNullOrEmpty(soundVolumeParameter))
        {
            audioMixer.SetFloat(soundVolumeParameter, volume);
        }
        else if (soundAudioSource != null)
        {
            soundAudioSource.mute = !isSoundOn;
        }
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(MUSIC_KEY, isMusicOn ? 1 : 0);
        PlayerPrefs.SetInt(SOUND_KEY, isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey(MUSIC_KEY))
        {
            isMusicOn = PlayerPrefs.GetInt(MUSIC_KEY) == 1;
        }
        else
        {
            isMusicOn = musicOnByDefault;
        }

        if (PlayerPrefs.HasKey(SOUND_KEY))
        {
            isSoundOn = PlayerPrefs.GetInt(SOUND_KEY) == 1;
        }
        else
        {
            isSoundOn = soundOnByDefault;
        }
    }

    public bool IsMusicOn()
    {
        return isMusicOn;
    }

    public bool IsSoundOn()
    {
        return isSoundOn;
    }

    public void SetMusicToggle(Toggle toggle)
    {
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
        }

        musicToggle = toggle;
        
        if (musicToggle != null)
        {
            musicToggle.isOn = isMusicOn;
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }
    }

    public void SetSoundToggle(Toggle toggle)
    {
        if (soundToggle != null)
        {
            soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
        }

        soundToggle = toggle;
        
        if (soundToggle != null)
        {
            soundToggle.isOn = isSoundOn;
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        }
    }
}
