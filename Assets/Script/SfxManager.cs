using UnityEngine;

/// <summary>
/// Plays short sound effects such as button clicks or placing items.
/// </summary>
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("SFX Clips")]
    public AudioClip placeItemClip;
    public AudioClip buttonClickClip;

    private AudioSource sfxSource;
    private AudioSource buttonClickSource; // Separate AudioSource for button click to reduce delay

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.priority = 0;
        
        // Separate AudioSource for button click - always ready, won't be blocked
        GameObject buttonClickObj = new GameObject("ButtonClickAudioSource");
        buttonClickObj.transform.SetParent(transform);
        buttonClickSource = buttonClickObj.AddComponent<AudioSource>();
        buttonClickSource.playOnAwake = false;
        buttonClickSource.loop = false;
        buttonClickSource.priority = 0;
        buttonClickSource.volume = 1f;
        buttonClickSource.bypassEffects = true;
        buttonClickSource.bypassListenerEffects = true;
        buttonClickSource.bypassReverbZones = true;
    }

    private void Start()
    {
        // Preload clips in Start() to ensure they're ready
        if (buttonClickClip != null)
        {
            buttonClickClip.LoadAudioData();
            buttonClickSource.clip = buttonClickClip;
        }
        if (placeItemClip != null)
        {
            placeItemClip.LoadAudioData();
        }
    }

    public void PlayPlaceItem()
    {
        PlayClip(placeItemClip, sfxSource);
    }

    public void PlayButtonClick()
    {
        // Use separate AudioSource for button click to play immediately
        if (buttonClickClip == null || buttonClickSource == null)
            return;

        if (!buttonClickClip.preloadAudioData)
        {
            buttonClickClip.LoadAudioData();
        }

        // Use PlayOneShot for button click - faster than Play() in some cases
        buttonClickSource.PlayOneShot(buttonClickClip, 1f);
    }

    public void SetVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(volume);
        }
        if (buttonClickSource != null)
        {
            buttonClickSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void Mute(bool mute)
    {
        if (sfxSource != null)
        {
            sfxSource.mute = mute;
        }
        if (buttonClickSource != null)
        {
            buttonClickSource.mute = mute;
        }
    }

    private void PlayClip(AudioClip clip, AudioSource source)
    {
        if (clip == null || source == null)
            return;

        if (!clip.preloadAudioData)
        {
            clip.LoadAudioData();
        }

        // Use Play() instead of PlayOneShot to reduce delay
        if (source.isPlaying)
        {
            source.Stop();
        }
        
        source.clip = clip;
        source.Play();
        
        // Force play immediately (in case of audio system delay)
        if (!source.isPlaying)
        {
            source.Play();
        }
    }
}

