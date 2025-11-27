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
    }

    public void PlayPlaceItem()
    {
        PlayClip(placeItemClip);
    }

    public void PlayButtonClick()
    {
        PlayClip(buttonClickClip);
    }

    public void SetVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void Mute(bool mute)
    {
        if (sfxSource != null)
        {
            sfxSource.mute = mute;
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }
}

