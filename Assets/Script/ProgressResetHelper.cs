using UnityEngine;

/// <summary>
/// Helper script để reset tất cả progress của game về trạng thái ban đầu.
/// Dùng khi cần demo game như mới hoàn toàn.
/// </summary>
public class ProgressResetHelper : MonoBehaviour
{
    [Header("Reset Options")]
    [Tooltip("Reset ngay khi Start() - dùng cho demo")]
    [SerializeField] private bool resetOnStart = false;
    
    [Tooltip("Reset cả Settings (Music/Sound toggles)")]
    [SerializeField] private bool resetSettings = false;

    private void Start()
    {
        if (resetOnStart)
        {
            ResetAllProgress();
        }
    }

    /// <summary>
    /// Reset tất cả progress - gọi từ Inspector hoặc code khác
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        // Reset level progress
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ResetAllProgress();
            Debug.Log("[ProgressResetHelper] Đã reset tất cả level progress!");
        }
        else
        {
            // Nếu GameFlowManager chưa tồn tại, xóa trực tiếp PlayerPrefs
            DeleteAllProgressKeys();
        }

        // Reset settings nếu cần
        if (resetSettings)
        {
            ResetSettings();
        }

        Debug.Log("[ProgressResetHelper] ✅ Đã reset tất cả progress! Game sẽ như mới hoàn toàn.");
    }

    private void DeleteAllProgressKeys()
    {
        // Xóa tất cả keys liên quan đến progress
        PlayerPrefs.DeleteKey("LAST_LEVEL");
        
        // Xóa tất cả level progress keys
        // Lấy level IDs từ GameFlowManager nếu có
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.levels != null)
        {
            foreach (var level in GameFlowManager.Instance.levels)
            {
                if (!string.IsNullOrEmpty(level.levelId))
                {
                    PlayerPrefs.DeleteKey($"LEVEL_PROGRESS_{level.levelId}");
                }
            }
        }
        else
        {
            // Fallback: xóa các level IDs phổ biến
            for (int i = 1; i <= 10; i++)
            {
                string[] possibleIds = { $"Level{i}", $"Level_{i}", $"Level {i}", $"Level1", $"Level2", $"Level3" };
                foreach (var id in possibleIds)
                {
                    PlayerPrefs.DeleteKey($"LEVEL_PROGRESS_{id}");
                }
            }
        }
        
        PlayerPrefs.Save();
        Debug.Log("[ProgressResetHelper] Đã xóa tất cả progress keys từ PlayerPrefs!");
    }

    private void ResetSettings()
    {
        // Reset settings
        PlayerPrefs.DeleteKey("Settings_Music");
        PlayerPrefs.DeleteKey("Settings_Sound");
        PlayerPrefs.Save();
        
        // Reset SettingsManager nếu đang tồn tại
        if (SettingsManager.Instance != null)
        {
            // SettingsManager sẽ tự load lại default values khi restart
        }
        
        Debug.Log("[ProgressResetHelper] Đã reset settings!");
    }

    /// <summary>
    /// Reset chỉ level progress, giữ nguyên settings
    /// </summary>
    [ContextMenu("Reset Only Level Progress")]
    public void ResetLevelProgressOnly()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ResetAllProgress();
        }
        else
        {
            DeleteAllProgressKeys();
        }
        Debug.Log("[ProgressResetHelper] Đã reset level progress!");
    }
}

