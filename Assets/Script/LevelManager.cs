using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Level References")]
    public CardboardBox cardboardBox;
    public List<GridSnapZone> dropZones = new List<GridSnapZone>();
    
    [Header("UI References")]
#if UNITY_TMPRO
    public TMPro.TextMeshProUGUI itemsRemainingTMPText;
#else
    public Text itemsRemainingText;
#endif
#if UNITY_TMPRO
    public TMPro.TextMeshProUGUI timerTMPText;
#endif
    public GameObject levelCompletePanel;
    public Button nextLevelButton;
    public Button restartButton;
    public Button doneButton;
    public Image[] starIcons;
    public Sprite filledStarSprite;
    public Sprite emptyStarSprite;
    
    [Header("Level Settings")]
    public int totalItems = 0;
    
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private bool levelCompleted = false;
    private bool timerRunning = false;
    private float levelStartTime = 0f;
    private bool readyToComplete = false;
    private int starsEarned = 0;
    
    void Start()
    {
        // Đếm tổng số items từ cardboard box
        if (cardboardBox != null)
        {
            totalItems = 0;
            foreach (var prefab in cardboardBox.itemsToSpawn)
            {
                if (prefab == null) continue;
                if (prefab.GetComponent<DraggableItem>() != null)
                {
                    totalItems++;
                }
            }
        }
        
        // Setup UI
        UpdateUI();
        
        // Setup buttons
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);
            
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(LoadNextLevel);

        if (doneButton != null)
        {
            doneButton.gameObject.SetActive(false);
            doneButton.onClick.AddListener(OnDoneButtonClicked);
        }

        timerRunning = true;
        levelStartTime = Time.time;
        UpdateStarUI(0);
    }
    
    void Update()
    {
        // Cập nhật UI mỗi frame
        UpdateUI();
        UpdateTimer();
        
        // Kiểm tra điều kiện hoàn thành level
        CheckLevelCompletion();
    }
    
    private void UpdateUI()
    {
#if UNITY_TMPRO
        if (itemsRemainingTMPText != null && cardboardBox != null)
        {
            int remaining = cardboardBox.GetRemainingItemsCount();
            itemsRemainingTMPText.text = $"Items Remaining: {remaining}";
        }
#else
        if (itemsRemainingText != null && cardboardBox != null)
        {
            int remaining = cardboardBox.GetRemainingItemsCount();
            itemsRemainingText.text = $"Items Remaining: {remaining}";
        }
#endif

        if (doneButton != null)
        {
            doneButton.interactable = readyToComplete;
        }
    }

    private void UpdateTimer()
    {
        if (!timerRunning) return;
        float elapsed = Time.time - levelStartTime;
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        string textValue = $"{minutes:00}:{seconds:00}";

#if UNITY_TMPRO
        if (timerTMPText != null)
        {
            timerTMPText.text = textValue;
        }
#endif
    }
    
    private void CheckLevelCompletion()
    {
        if (levelCompleted) return;
        
        if (cardboardBox != null && !cardboardBox.HasMoreItems())
        {
            int correctPlacements = CountCorrectPlacements();
            
            if (correctPlacements >= totalItems)
            {
                if (!readyToComplete)
                {
                    readyToComplete = true;
                    if (doneButton != null)
                    {
                        doneButton.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
    
    private int CountCorrectPlacements()
    {
        int correct = 0;
        
        foreach (var zone in dropZones)
        {
            if (zone == null) continue;
            
            // Đếm số cells đã occupied trong zone này
            // (Cần thêm method trong GridSnapZone để đếm occupied cells)
            correct += CountOccupiedCellsInZone(zone);
        }
        
        return correct;
    }
    
    private int CountOccupiedCellsInZone(GridSnapZone zone)
    {
        if (zone == null) return 0;
        return zone.GetOccupiedCellsCount();
    }
    
    private void CompleteLevel()
    {
        levelCompleted = true;
        timerRunning = false;
        float elapsed = Time.time - levelStartTime;
        starsEarned = CalculateStarRating(elapsed);
        UpdateStarUI(starsEarned);

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        if (doneButton != null)
        {
            doneButton.gameObject.SetActive(false);
        }

        GameFlowManager.Instance?.CompleteLevel(starsEarned, autoTransition: false);
        
        Debug.Log("Level Completed! Stars: " + starsEarned);
    }

    private int CalculateStarRating(float elapsedSeconds)
    {
        if (elapsedSeconds < 180f) return 3;
        if (elapsedSeconds < 300f) return 2;
        return 1;
    }

    private void UpdateStarUI(int stars)
    {
        if (starIcons == null || starIcons.Length == 0) return;
        for (int i = 0; i < starIcons.Length; i++)
        {
            if (starIcons[i] == null) continue;
            Sprite targetSprite = (stars > i) ? filledStarSprite : emptyStarSprite;
            if (targetSprite != null)
            {
                starIcons[i].sprite = targetSprite;
            }
            starIcons[i].enabled = targetSprite != null;
        }
    }

    private void OnDoneButtonClicked()
    {
        if (!readyToComplete || levelCompleted) return;
        CompleteLevel();
    }
    
    public void RestartLevel()
    {
        if (cardboardBox != null)
        {
            cardboardBox.ResetBox();
        }
        
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        
        levelCompleted = false;
        readyToComplete = false;
        timerRunning = true;
        levelStartTime = Time.time;
        starsEarned = 0;
        
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        if (doneButton != null)
        {
            doneButton.gameObject.SetActive(false);
        }

        UpdateStarUI(0);
    }
    
    public void LoadNextLevel()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.LoadNextLevelImmediate();
        }
        else
        {
            Scene currentScene = SceneManager.GetActiveScene();
            int nextIndex = currentScene.buildIndex + 1;
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextIndex);
            }
            else
            {
                Debug.LogWarning("No additional levels configured in Build Settings.");
            }
        }
    }
    
    public void RegisterItem(DraggableItem item)
    {
        RegisterSpawnedObject(item != null ? item.gameObject : null);
    }

    public void RegisterSpawnedObject(GameObject obj)
    {
        if (obj == null) return;
        if (!spawnedObjects.Contains(obj))
        {
            spawnedObjects.Add(obj);
        }
    }
}
