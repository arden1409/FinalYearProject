using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseMenuBackground;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsBackButton;

    private bool isPaused = false;

    private void Start()
    {
        // Ẩn panel pause ban đầu
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Setup buttons
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinue);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettings);
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(OnBackToMenu);
        }

        // Ẩn settings panel ban đầu
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // Setup settings back button
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(OnSettingsBack);
        }
    }

    private void Update()
    {
        // Kiểm tra phím ESC để pause (hỗ trợ cả Input System mới và cũ)
        bool escapePressed = false;
        
#if ENABLE_INPUT_SYSTEM
        // Sử dụng Input System mới
        if (Keyboard.current != null)
        {
            escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#else
        // Sử dụng Input Manager cũ
        escapePressed = Input.GetKeyDown(KeyCode.Escape);
#endif

        if (escapePressed)
        {
            // Nếu đang ở Settings Panel, quay về Pause Panel
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                OnSettingsBack();
            }
            else
            {
                // Nếu đang ở Pause Panel, toggle pause
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        // Hiện Pause Panel (overlay đen) và Pause Menu Background
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        if (pauseMenuBackground != null)
        {
            pauseMenuBackground.SetActive(true);
        }

        // Ẩn settings panel nếu đang mở
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OnContinue()
    {
        ResumeGame();
    }

    public void OnSettings()
    {
        // Luôn mở Settings Panel: Ẩn Pause Menu Background, hiện Settings Panel
        // Giữ PausePanel (overlay đen) để SettingsPanel có thể hiển thị
        if (settingsPanel != null)
        {
            // Ẩn Pause Menu Background (menu chính với 3 nút)
            if (pauseMenuBackground != null)
            {
                pauseMenuBackground.SetActive(false);
            }
            
            // Hiện Settings Panel
            settingsPanel.SetActive(true);
            
            // Refresh toggles khi mở settings panel
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.RefreshToggles();
            }
        }
    }

    public void OnSettingsBack()
    {
        // Đóng settings panel và hiện lại pause menu background
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        // Hiện lại Pause Menu Background (menu chính)
        if (pauseMenuBackground != null)
        {
            pauseMenuBackground.SetActive(true);
        }
        
        // PausePanel (overlay đen) vẫn giữ nguyên active
    }

    public void OnBackToMenu()
    {
        // Resume game trước khi quay về menu
        ResumeGame();

        // Load main menu
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.LoadMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    private void OnDestroy()
    {
        // Đảm bảo time scale được reset khi destroy
        Time.timeScale = 1f;
    }
}

