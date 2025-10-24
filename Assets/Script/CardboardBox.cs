using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardboardBox : MonoBehaviour, IPointerClickHandler
{
    [Header("Box Settings")]
    public List<GameObject> itemsToSpawn = new List<GameObject>();
    public Transform spawnPoint;
    public float spawnOffset = 0.5f;
    
    [Header("Visual Feedback")]
    public GameObject highlightEffect;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    
    private SpriteRenderer spriteRenderer;
    private int currentItemIndex = 0;
    private bool isOpen = false;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spawnPoint == null)
            spawnPoint = transform;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemsToSpawn.Count == 0) return;
        
        // Spawn item tiếp theo trong danh sách
        SpawnNextItem();
    }
    
    private void SpawnNextItem()
    {
        if (currentItemIndex >= itemsToSpawn.Count) return;
        
        GameObject itemPrefab = itemsToSpawn[currentItemIndex];
        if (itemPrefab == null) return;
        
        GameObject newItem = Instantiate(itemPrefab);
        Vector3 spawnPosition = spawnPoint.position + Vector3.up * spawnOffset;
        newItem.transform.position = spawnPosition;
        
        DraggableItem draggableItem = newItem.GetComponent<DraggableItem>();
        if (draggableItem == null)
        {
            draggableItem = newItem.AddComponent<DraggableItem>();
        }
        
        draggableItem.snapMoveSpeed = 8f;
        draggableItem.lockOnSnap = true;
        
        currentItemIndex++;
        
        if (currentItemIndex >= itemsToSpawn.Count)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.gray;
            }
        }
    }
    
    public void ResetBox()
    {
        currentItemIndex = 0;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }
    
    public void AddItem(GameObject itemPrefab)
    {
        if (itemPrefab != null && !itemsToSpawn.Contains(itemPrefab))
        {
            itemsToSpawn.Add(itemPrefab);
        }
    }
    
    public bool HasMoreItems()
    {
        return currentItemIndex < itemsToSpawn.Count;
    }
    
    public int GetRemainingItemsCount()
    {
        return Mathf.Max(0, itemsToSpawn.Count - currentItemIndex);
    }
}
