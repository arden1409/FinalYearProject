using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nút quay lại (Back) đơn giản, dùng GameFlowManager nếu có.
/// </summary>
[RequireComponent(typeof(Button))]
public class BackButton : MonoBehaviour
{
    public enum BackTarget
    {
        MainMenu,
        LevelSelect
    }

    [Tooltip("Scene/state sẽ quay lại khi ấn nút")]
    public BackTarget target = BackTarget.MainMenu;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogWarning("[BackButton] GameFlowManager.Instance is null, dùng LoadScene trực tiếp.");
            string sceneName = target == BackTarget.MainMenu ? "MainMenu" : "LevelSelect";
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            return;
        }

        switch (target)
        {
            case BackTarget.MainMenu:
                GameFlowManager.Instance.LoadMainMenu();
                break;
            case BackTarget.LevelSelect:
                GameFlowManager.Instance.LoadLevelSelect();
                break;
        }
    }
}


