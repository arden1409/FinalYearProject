using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    [System.Serializable]
    public class StoryData
    {
        public string storyId;
        
        [Header("Dialogue")]
        [Tooltip("Name of the character speaking")]
        public string speakerName = "";
        
        [TextArea(3, 10)]
        [Tooltip("Dialogue text")]
        public string storyText;
        
        [Tooltip("Portrait sprite of the speaking character")]
        public Sprite speakerPortrait;
        
        [Header("Legacy (Optional)")]
        [Tooltip("Background image (optional, for non-dialogue scenes)")]
        public Sprite storyImage;
        
        [Tooltip("Time to display each page (0 = wait for click)")]
        public float displayTime = 0f;
    }

    [Header("Story Content")]
    [Tooltip("List of all story sequences (intro/outro for each level)")]
    public StoryData[] storyDatabase;

    [Header("UI References")]
    [Tooltip("Panel containing all story UI elements")]
    public GameObject storyPanel;
    
    [Header("Dialogue UI")]
    [Tooltip("Text component to display speaker name")]
    public TextMeshProUGUI speakerNameText;
    
    [Tooltip("Text component to display dialogue text")]
    public TextMeshProUGUI storyText;
    
    [Tooltip("Image component to display speaker portrait")]
    public Image speakerPortraitImage;
    
    [Header("Legacy UI (Optional)")]
    [Tooltip("Image component to display background image (optional)")]
    public Image storyImage;
    
    [Header("Buttons")]
    [Tooltip("Button to continue/next page")]
    public Button continueButton;
    
    [Tooltip("Button to skip story")]
    public Button skipButton;

    [Header("Settings")]
    [Tooltip("Auto-advance to next page after displayTime seconds")]
    public bool autoAdvance = false;
    
    [Tooltip("Fade duration for text/image transitions")]
    public float fadeDuration = 0.3f;

    private StoryData[] currentStorySequence;
    private int currentPageIndex = 0;
    private bool isStoryActive = false;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("Manual Override (For Testing)")]
    [Tooltip("Manually set story ID for testing (leave empty to auto-detect from GameFlowManager)")]
    public string manualStoryId = "";

    private void Start()
    {
        if (storyPanel != null)
        {
            storyPanel.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipClicked);
        }

        // Wait a frame for GameFlowManager to initialize
        StartCoroutine(LoadStoryDelayed());
    }

    private IEnumerator LoadStoryDelayed()
    {
        yield return null;
        
        if (GameFlowManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        LoadStoryForCurrentState();
    }

    private void LoadStoryForCurrentState()
    {
        string storyId = "";
        
        // Detect from GameFlowManager if available
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.GameState state = GameFlowManager.Instance.CurrentState;
            Debug.Log($"[StoryManager] Current State: {state}");

            if (state == GameFlowManager.GameState.CharacterIntro)
            {
                storyId = "CharacterIntro";
            }
            else if (state == GameFlowManager.GameState.StoryIntro)
            {
                if (GameFlowManager.Instance.CurrentLevel != null)
                {
                    storyId = GameFlowManager.Instance.CurrentLevel.storyIntroId;
                    Debug.Log($"[StoryManager] Story Intro - Level ID: {GameFlowManager.Instance.CurrentLevel.levelId}, Story ID: {storyId}");
                }
                else
                {
                    Debug.LogWarning("[StoryManager] CurrentLevel is null in StoryIntro state!");
                }
            }
            else if (state == GameFlowManager.GameState.StoryOutro)
            {
                if (GameFlowManager.Instance.CurrentLevel != null)
                {
                    storyId = GameFlowManager.Instance.CurrentLevel.storyOutroId;
                    Debug.Log($"[StoryManager] Story Outro - Level ID: {GameFlowManager.Instance.CurrentLevel.levelId}, Story ID: {storyId}");
                }
                else
                {
                    Debug.LogWarning("[StoryManager] CurrentLevel is null in StoryOutro state!");
                }
            }
        }
        
        // Only use manual story ID if GameFlowManager not available (for direct scene testing)
        if (string.IsNullOrEmpty(storyId) && !string.IsNullOrEmpty(manualStoryId))
        {
            storyId = manualStoryId;
            Debug.Log($"[StoryManager] Using manual story ID (GameFlowManager not available): {storyId}");
        }
        
        if (string.IsNullOrEmpty(storyId))
        {
            Debug.LogWarning("[StoryManager] No story ID found. Skipping story.");
            FinishStory();
            return;
        }

        Debug.Log($"[StoryManager] Loading story with ID: {storyId}");
        StartStory(storyId);
    }

    public void StartStory(string storyId)
    {
        if (storyDatabase == null || storyDatabase.Length == 0)
        {
            Debug.LogWarning("[StoryManager] Story database is empty.");
            FinishStory();
            return;
        }

        Debug.Log($"[StoryManager] Searching for story ID: '{storyId}' in database with {storyDatabase.Length} entries.");
        
        // Debug: Log all story IDs in database
        System.Text.StringBuilder dbInfo = new System.Text.StringBuilder("Available story IDs in database: ");
        foreach (var story in storyDatabase)
        {
            if (story != null)
            {
                dbInfo.Append($"'{story.storyId}' ");
            }
        }
        Debug.Log(dbInfo.ToString());

        System.Collections.Generic.List<StoryData> sequence = new System.Collections.Generic.List<StoryData>();
        foreach (var story in storyDatabase)
        {
            if (story != null && story.storyId == storyId)
            {
                sequence.Add(story);
            }
        }

        if (sequence.Count == 0)
        {
            Debug.LogWarning($"[StoryManager] Story with ID '{storyId}' not found in database.");
            FinishStory();
            return;
        }

        Debug.Log($"[StoryManager] Found {sequence.Count} page(s) for story ID '{storyId}'");
        currentStorySequence = sequence.ToArray();
        currentPageIndex = 0;
        isStoryActive = true;

        if (storyPanel != null)
        {
            storyPanel.SetActive(true);
        }

        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        DisplayCurrentPage();
    }

    private void DisplayCurrentPage()
    {
        if (currentStorySequence == null || currentPageIndex >= currentStorySequence.Length)
        {
            FinishStory();
            return;
        }

        StoryData page = currentStorySequence[currentPageIndex];

        // Display speaker name
        if (speakerNameText != null)
        {
            speakerNameText.text = string.IsNullOrEmpty(page.speakerName) ? "" : page.speakerName;
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(page.speakerName));
        }

        // Display dialogue text
        if (storyText != null)
        {
            storyText.text = page.storyText;
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeInText());
        }

        // Display speaker portrait
        if (speakerPortraitImage != null)
        {
            speakerPortraitImage.sprite = page.speakerPortrait;
            speakerPortraitImage.gameObject.SetActive(page.speakerPortrait != null);
        }

        // Display background image (legacy, optional)
        if (storyImage != null)
        {
            storyImage.sprite = page.storyImage;
            storyImage.gameObject.SetActive(page.storyImage != null);
        }

        if (continueButton != null)
        {
            bool isLastPage = currentPageIndex >= currentStorySequence.Length - 1;
            TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isLastPage ? "Continue" : "Next";
            }
        }

        if (autoAdvance && page.displayTime > 0)
        {
            StartCoroutine(AutoAdvanceCoroutine(page.displayTime));
        }
    }

    private IEnumerator FadeInText()
    {
        if (storyText == null) yield break;

        Color originalColor = storyText.color;
        storyText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, originalColor.a, elapsed / fadeDuration);
            storyText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        storyText.color = originalColor;
    }

    private IEnumerator AutoAdvanceCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isStoryActive)
        {
            NextPage();
        }
    }

    public void OnContinueClicked()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        NextPage();
    }

    private void NextPage()
    {
        currentPageIndex++;
        DisplayCurrentPage();
    }

    public void OnSkipClicked()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        FinishStory();
    }

    private void FinishStory()
    {
        isStoryActive = false;
        currentPageIndex = 0;
        currentStorySequence = null;

        if (storyPanel != null)
        {
            storyPanel.SetActive(false);
        }

        if (GameFlowManager.Instance == null)
        {
            Debug.LogWarning("[StoryManager] GameFlowManager.Instance is null. Cannot notify story finished.");
            return;
        }

        GameFlowManager.GameState state = GameFlowManager.Instance.CurrentState;
        if (state == GameFlowManager.GameState.CharacterIntro)
        {
            GameFlowManager.Instance.NotifyCharacterIntroFinished();
        }
        else if (state == GameFlowManager.GameState.StoryIntro)
        {
            GameFlowManager.Instance.NotifyStoryIntroFinished();
        }
        else if (state == GameFlowManager.GameState.StoryOutro)
        {
            GameFlowManager.Instance.NotifyStoryOutroFinished();
        }
    }
}

