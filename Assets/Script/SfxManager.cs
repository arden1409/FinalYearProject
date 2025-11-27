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
    private AudioSource buttonClickSource; // AudioSource riêng cho button click để giảm delay

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource chung cho các SFX khác
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.priority = 0;
        
        // AudioSource riêng cho button click - luôn sẵn sàng, không bị block
        GameObject buttonClickObj = new GameObject("ButtonClickAudioSource");
        buttonClickObj.transform.SetParent(transform);
        buttonClickSource = buttonClickObj.AddComponent<AudioSource>();
        buttonClickSource.playOnAwake = false;
        buttonClickSource.loop = false;
        buttonClickSource.priority = 0; // Highest priority
        buttonClickSource.volume = 1f;
        buttonClickSource.bypassEffects = true; // Bypass effects để giảm latency
        buttonClickSource.bypassListenerEffects = true;
        buttonClickSource.bypassReverbZones = true;
    }

    private void Start()
    {
        // Preload clips trong Start() để đảm bảo đã sẵn sàng
        if (buttonClickClip != null)
        {
            buttonClickClip.LoadAudioData();
            // Assign clip sẵn để giảm delay khi play
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
        // Dùng AudioSource riêng cho button click để phát ngay lập tức
        if (buttonClickClip == null || buttonClickSource == null)
            return;

        // Đảm bảo clip đã được load
        if (!buttonClickClip.preloadAudioData)
        {
            buttonClickClip.LoadAudioData();
        }

        // Dùng PlayOneShot cho button click - nhanh hơn Play() trong một số trường hợp
        // PlayOneShot không cần stop/clip assignment, phát ngay lập tức
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

        // Đảm bảo clip đã được load
        if (!clip.preloadAudioData)
        {
            clip.LoadAudioData();
        }

        // Dùng Play() thay vì PlayOneShot để giảm delay
        // Stop trước để đảm bảo phát ngay
        if (source.isPlaying)
        {
            source.Stop();
        }
        
        source.clip = clip;
        source.Play();
        
        // Force play ngay lập tức (nếu có delay do audio system)
        if (!source.isPlaying)
        {
            source.Play();
        }
    }
}

