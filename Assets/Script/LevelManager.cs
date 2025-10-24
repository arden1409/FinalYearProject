using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [Header("Level References")]
    public CardboardBox cardboardBox;
    public List<GridSnapZone> dropZones = new List<GridSnapZone>();
    
    [Header("UI References")]
    public Text itemsRemainingText;
    public Text scoreText;
    public GameObject levelCompletePanel;
    public Button nextLevelButton;
    public Button restartButton;
    
    [Header("Level Settings")]
    public int totalItems = 0;
    public int itemsPlacedCorrectly = 0;
    public int score = 0;
    public int pointsPerCorrectPlacement = 10;
    
    private List<DraggableItem> allItems = new List<DraggableItem>();
    private bool levelCompleted = false;
    
    void Start()
    {
        // Đếm tổng số items từ cardboard box
        if (cardboardBox != null)
        {
            totalItems = cardboardBox.itemsToSpawn.Count;
        }
        
        // Setup UI
        UpdateUI();
        
        // Setup buttons
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);
            
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(LoadNextLevel);
    }
    
    void Update()
    {
        // Cập nhật UI mỗi frame
        UpdateUI();
        
        // Kiểm tra điều kiện hoàn thành level
        CheckLevelCompletion();
    }
    
    private void UpdateUI()
    {
        if (itemsRemainingText != null && cardboardBox != null)
        {
            int remaining = cardboardBox.GetRemainingItemsCount();
            itemsRemainingText.text = $"Items Remaining: {remaining}";
        }
        
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }
    
    private void CheckLevelCompletion()
    {
        if (levelCompleted) return;
        
        if (cardboardBox != null && !cardboardBox.HasMoreItems())
        {
            int correctPlacements = CountCorrectPlacements();
            
            if (correctPlacements >= totalItems)
            {
                CompleteLevel();
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
        score = itemsPlacedCorrectly * pointsPerCorrectPlacement;
        
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        
        Debug.Log("Level Completed! Score: " + score);
    }
    
    public void RestartLevel()
    {
        if (cardboardBox != null)
        {
            cardboardBox.ResetBox();
        }
        
        foreach (var item in allItems)
        {
            if (item != null)
            {
                item.ResetPosition();
            }
        }
        
        itemsPlacedCorrectly = 0;
        score = 0;
        levelCompleted = false;
        
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }
    
    public void LoadNextLevel()
    {
        Debug.Log("Loading next level...");
    }
    
    public void RegisterItem(DraggableItem item)
    {
        if (!allItems.Contains(item))
        {
            allItems.Add(item);
        }
    }
    
    public void OnItemPlacedCorrectly()
    {
        itemsPlacedCorrectly++;
        score += pointsPerCorrectPlacement;
    }
}
