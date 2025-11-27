using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script để gán toggles từ settings panel trong scene hiện tại vào SettingsManager.
/// Đặt script này vào settings panel hoặc bất kỳ GameObject nào trong scene.
/// Gán thủ công Music Toggle và Sound Toggle vào các field bên dưới.
/// </summary>
public class SettingsPanelHelper : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Gán Music Toggle từ settings panel")]
    [SerializeField] private Toggle musicToggle;
    [Tooltip("Gán Sound Toggle từ settings panel")]
    [SerializeField] private Toggle soundToggle;

    private void Start()
    {
        AssignToggles();
    }

    private void OnEnable()
    {
        // Đảm bảo toggles được gán khi GameObject được enable
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
    /// Gán toggles thủ công (có thể gọi từ Inspector hoặc code khác)
    /// </summary>
    public void SetToggles(Toggle music, Toggle sound)
    {
        musicToggle = music;
        soundToggle = sound;
        AssignToggles();
    }
}

