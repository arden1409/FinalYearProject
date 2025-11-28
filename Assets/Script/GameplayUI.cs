using UnityEngine;
using UnityEngine.UI;

public class GameplayUI : MonoBehaviour
{
    [Header("Menu Button")]
    [SerializeField] private Button menuButton;

    [Header("Pause Menu")]
    [SerializeField] private PauseMenuUI pauseMenuUI;

    [Header("Action Buttons")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;
    [SerializeField] private Button hintButton;

    [Header("Button Images")]
    [SerializeField] private Image undoButtonImage;
    [SerializeField] private Image redoButtonImage;
    [SerializeField] private Image hintButtonImage;

    [Header("Button Sprites")]
    [SerializeField] private Sprite undoSprite;
    [SerializeField] private Sprite redoSprite;
    [SerializeField] private Sprite hintSprite;

    private UndoRedoManager undoRedoManager;
    private HintManager hintManager;

    private void Start()
    {
        // Find or create UndoRedoManager
        undoRedoManager = FindFirstObjectByType<UndoRedoManager>();
        if (undoRedoManager == null)
        {
            GameObject undoRedoObj = new GameObject("UndoRedoManager");
            undoRedoManager = undoRedoObj.AddComponent<UndoRedoManager>();
        }

        // Find or create HintManager
        hintManager = FindFirstObjectByType<HintManager>();
        if (hintManager == null)
        {
            GameObject hintObj = new GameObject("HintManager");
            hintManager = hintObj.AddComponent<HintManager>();
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuButtonClicked);
        }
        if (undoButton != null)
        {
            undoButton.onClick.AddListener(OnUndoClicked);
        }

        if (redoButton != null)
        {
            redoButton.onClick.AddListener(OnRedoClicked);
        }

        if (hintButton != null)
        {
            hintButton.onClick.AddListener(OnHintClicked);
            hintManager.SetHintButton(hintButton);
        }

        SetupButtonImages();
    }

    private void SetupButtonImages()
    {
        // Undo button image
        if (undoButtonImage != null && undoSprite != null)
        {
            undoButtonImage.sprite = undoSprite;
        }
        else if (undoButton != null && undoSprite != null)
        {
            Image img = undoButton.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = undoSprite;
            }
        }

        // Redo button image
        if (redoButtonImage != null && redoSprite != null)
        {
            redoButtonImage.sprite = redoSprite;
        }
        else if (redoButton != null && redoSprite != null)
        {
            Image img = redoButton.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = redoSprite;
            }
        }

        // Hint button image
        if (hintButtonImage != null && hintSprite != null)
        {
            hintButtonImage.sprite = hintSprite;
        }
        else if (hintButton != null && hintSprite != null)
        {
            Image img = hintButton.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = hintSprite;
            }
        }
    }

    private void Update()
    {
        UpdateUndoRedoButtons();
    }

    private void UpdateUndoRedoButtons()
    {
        if (undoRedoManager == null) return;

        if (undoButton != null)
        {
            undoButton.interactable = undoRedoManager.CanUndo();
        }

        if (redoButton != null)
        {
            redoButton.interactable = undoRedoManager.CanRedo();
        }
    }

    public void OnMenuButtonClicked()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.TogglePause();
        }
        else
        {
            Debug.LogWarning("PauseMenuUI is not assigned!");
        }
    }

    public void OnUndoClicked()
    {
        if (undoRedoManager != null && undoRedoManager.CanUndo())
        {
            if (SfxManager.Instance != null)
            {
                SfxManager.Instance.PlayButtonClick();
            }
            undoRedoManager.Undo();
        }
    }

    public void OnRedoClicked()
    {
        if (undoRedoManager != null && undoRedoManager.CanRedo())
        {
            if (SfxManager.Instance != null)
            {
                SfxManager.Instance.PlayButtonClick();
            }
            undoRedoManager.Redo();
        }
    }

    public void OnHintClicked()
    {
        if (hintManager != null)
        {
            if (SfxManager.Instance != null)
            {
                SfxManager.Instance.PlayButtonClick();
            }
            hintManager.ShowHint();
        }
    }
}

