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
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

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

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(OnSettingsBack);
        }
    }

    private void Update()
    {
        bool escapePressed = false;
        
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#else
        escapePressed = Input.GetKeyDown(KeyCode.Escape);
#endif

        if (escapePressed)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                OnSettingsBack();
            }
            else
            {
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

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        if (pauseMenuBackground != null)
        {
            pauseMenuBackground.SetActive(true);
        }

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
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        ResumeGame();
    }

    public void OnSettings()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        
        if (settingsPanel != null)
        {
            if (pauseMenuBackground != null)
            {
                pauseMenuBackground.SetActive(false);
            }
            
            settingsPanel.SetActive(true);
            
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.RefreshToggles();
            }
        }
    }

    public void OnSettingsBack()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        if (pauseMenuBackground != null)
        {
            pauseMenuBackground.SetActive(true);
        }
    }

    public void OnBackToMenu()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        
        ResumeGame();

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
        Time.timeScale = 1f;
    }
}

