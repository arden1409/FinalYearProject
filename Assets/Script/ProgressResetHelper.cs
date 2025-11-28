using UnityEngine;

/// <summary>
/// Helper script to reset all game progress to initial state.
/// Use when you need to demo the game as completely new.
/// </summary>
public class ProgressResetHelper : MonoBehaviour
{
    [Header("Reset Options")]
    [Tooltip("Reset immediately on Start() - use for demo")]
    [SerializeField] private bool resetOnStart = false;
    
    [Tooltip("Also reset Settings (Music/Sound toggles)")]
    [SerializeField] private bool resetSettings = false;

    private void Start()
    {
        if (resetOnStart)
        {
            ResetAllProgress();
        }
    }

    /// <summary>
    /// Reset all progress - call from Inspector or other code
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ResetAllProgress();
            Debug.Log("[ProgressResetHelper] Reset all level progress!");
        }
        else
        {
            DeleteAllProgressKeys();
        }

        if (resetSettings)
        {
            ResetSettings();
        }

        Debug.Log("[ProgressResetHelper] ✅ Reset all progress! Game will be like new.");
    }

    private void DeleteAllProgressKeys()
    {
        PlayerPrefs.DeleteKey("LAST_LEVEL");
        
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
            // Fallback: delete common level IDs
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
        Debug.Log("[ProgressResetHelper] Deleted all progress keys from PlayerPrefs!");
    }

    private void ResetSettings()
    {
        PlayerPrefs.DeleteKey("Settings_Music");
        PlayerPrefs.DeleteKey("Settings_Sound");
        PlayerPrefs.Save();
        
        Debug.Log("[ProgressResetHelper] Reset settings!");
    }

    /// <summary>
    /// Reset only level progress, keep settings
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
        Debug.Log("[ProgressResetHelper] Reset level progress!");
    }
}

