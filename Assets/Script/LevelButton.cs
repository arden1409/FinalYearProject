using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Nút chọn level trong Level Select.
/// - Gán lên từng button, điền levelId trùng với GameFlowManager.levels[i].levelId.
/// - Có thể dùng lockOverlay + label để hiển thị trạng thái.
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    [Header("Thông tin level")]
    public string levelId = "Level1";

    [Header("UI tuỳ chọn")]
    [Tooltip("Overlay xám + icon khoá (bật khi level bị khoá)")]
    public GameObject lockOverlay;

    [Tooltip("Text hiển thị tên / score")]
    public TextMeshProUGUI label;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnEnable()
    {
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (GameFlowManager.Instance == null)
        {
            // Khi test scene trực tiếp, cho phép bấm luôn
            if (lockOverlay != null) lockOverlay.SetActive(false);
            button.interactable = true;
            if (label != null) label.text = levelId;
            return;
        }

        var progress = GameFlowManager.Instance.GetProgress(levelId);
        bool unlocked = progress?.unlocked ?? false;
        bool completed = progress?.completed ?? false;
        int bestScore = progress?.bestScore ?? 0;

        button.interactable = unlocked;

        if (lockOverlay != null)
            lockOverlay.SetActive(!unlocked);

        if (label != null)
        {
            if (!unlocked)
            {
                label.text = levelId + "\nLOCKED";
            }
            else if (completed && bestScore > 0)
            {
                label.text = $"{levelId}\n★ {bestScore}";
            }
            else
            {
                label.text = levelId;
            }
        }
    }

    private void OnButtonClicked()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogWarning("[LevelButton] GameFlowManager.Instance is null.");
            return;
        }

        var progress = GameFlowManager.Instance.GetProgress(levelId);
        if (progress != null && !progress.unlocked)
        {
            Debug.Log($"[LevelButton] Level {levelId} is locked.");
            return;
        }

        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        Debug.Log($"[LevelButton] Start level {levelId}");
        GameFlowManager.Instance.StartLevel(levelId);
    }
}


