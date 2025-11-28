using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OnPlay()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        // Continue from most recent level instead of resetting to level 1
        GameFlowManager.Instance?.ContinueGame();
    }

    public void OnLevelSelect()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        GameFlowManager.Instance?.LoadLevelSelect();
    }

    public void OnSettings()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        
        if (settingsPanel != null)
        {
            bool isOpening = !settingsPanel.activeSelf;
            settingsPanel.SetActive(!settingsPanel.activeSelf);
            
            if (isOpening && SettingsManager.Instance != null)
            {
                SettingsManager.Instance.RefreshToggles();
            }
        }
    }

    public void OnCloseSettings()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OnQuitGame()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

