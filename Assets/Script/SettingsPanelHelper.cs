using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script to assign toggles from settings panel in current scene to SettingsManager.
/// Attach this script to settings panel or any GameObject in the scene.
/// Manually assign Music Toggle and Sound Toggle to the fields below.
/// </summary>
public class SettingsPanelHelper : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign Music Toggle from settings panel")]
    [SerializeField] private Toggle musicToggle;
    [Tooltip("Assign Sound Toggle from settings panel")]
    [SerializeField] private Toggle soundToggle;

    private void Start()
    {
        AssignToggles();
    }

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
        {
            AssignToggles();
        }
    }

    private void AssignToggles()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("[SettingsPanelHelper] SettingsManager.Instance is null!");
            return;
        }

        if (musicToggle != null)
        {
            SettingsManager.Instance.SetMusicToggle(musicToggle);
        }

        if (soundToggle != null)
        {
            SettingsManager.Instance.SetSoundToggle(soundToggle);
        }
    }

    /// <summary>
    /// Manually assign toggles (can be called from Inspector or other code)
    /// </summary>
    public void SetToggles(Toggle music, Toggle sound)
    {
        musicToggle = music;
        soundToggle = sound;
        AssignToggles();
    }
}

