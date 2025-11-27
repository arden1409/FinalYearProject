using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Global music controller that switches background tracks per scene/level.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Clips")]
    public AudioClip mainMenuMusic;
    public AudioClip level1Music;
    public AudioClip level2Music;
    public AudioClip level3Music;
    public AudioClip defaultMusic;

    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
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
        UpdateMusicForScene(scene.name);
    }

    /// <summary>
    /// Call this manually if you want to change music without scene load.
    /// </summary>
    public void UpdateMusicForScene(string sceneName)
    {
        AudioClip targetClip = ResolveClipForScene(sceneName);
        if (targetClip == null || musicSource == null)
            return;

        if (musicSource.clip == targetClip && musicSource.isPlaying)
            return;

        musicSource.clip = targetClip;
        musicSource.Play();
    }

    private AudioClip ResolveClipForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return defaultMusic ?? mainMenuMusic;

        string lower = sceneName.ToLower();
        if (lower.Contains("mainmenu"))
            return mainMenuMusic ?? defaultMusic;
        if (lower.Contains("level1"))
            return level1Music ?? defaultMusic;
        if (lower.Contains("level2"))
            return level2Music ?? defaultMusic;
        if (lower.Contains("level3"))
            return level3Music ?? defaultMusic;

        return defaultMusic ?? mainMenuMusic;
    }

    public void SetVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void Mute(bool mute)
    {
        if (musicSource != null)
        {
            musicSource.mute = mute;
        }
    }
}

