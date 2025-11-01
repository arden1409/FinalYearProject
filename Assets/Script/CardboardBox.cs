using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardboardBox : MonoBehaviour, IPointerDownHandler
{
    [Header("Box Settings")]
    public List<GameObject> itemsToSpawn = new List<GameObject>();
    public Transform spawnPoint;
    public float spawnOffset = 0.5f;
	public bool loopItems = false; // cycle items instead of stopping at the end
	public float temporaryIgnoreSeconds = 0.15f; // time to ignore raycasts for spawned item
	[Tooltip("Spiral step radius (world units) to spread spawned items around the box")]
	public float spiralStep = 0.3f;
    [Tooltip("Temporarily move spawned item to IgnoreRaycast layer for smoother multi-spawn")]
    public bool temporaryIgnoreRaycast = true;
    
    [Header("Visual Feedback")]
    public GameObject highlightEffect;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    
    private SpriteRenderer spriteRenderer;
    private int currentItemIndex = 0;
    private bool isOpen = false;
	private int spawnedCount = 0; // number of items spawned since start/reset
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spawnPoint == null)
            spawnPoint = transform;

		// Clean null entries so count reflects actual items
		itemsToSpawn.RemoveAll(go => go == null);
		// Optional: center box color
    }
    
	public void OnPointerDown(PointerEventData eventData)
    {
        if (itemsToSpawn.Count == 0) return;
        
        SpawnNextItem();
    }
    
    private void SpawnNextItem()
    {
		if (itemsToSpawn.Count == 0) return;

		// If reached end
		if (currentItemIndex >= itemsToSpawn.Count)
        {
			if (loopItems)
			{
				currentItemIndex = 0;
			}
			else
			{
				if (spriteRenderer != null)
				{
					spriteRenderer.color = Color.gray;
				}
				return;
			}
        }
        
		GameObject itemPrefab = itemsToSpawn[currentItemIndex];
		GameObject newItem = Instantiate(itemPrefab);
		// Place items around the box in a small spiral so they remain visible
		int ring = spawnedCount / 6;         // 6 items per ring
		int slot = spawnedCount % 6;         // slot within ring
		float angle = slot * Mathf.Deg2Rad * 60f; // 0,60,120,...
		float radius = (ring + 1) * spiralStep;
		Vector3 spiral = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
		Vector3 spawnPosition = spawnPoint.position + Vector3.up * spawnOffset + spiral;
        newItem.transform.position = spawnPosition;
        
        DraggableItem draggableItem = newItem.GetComponent<DraggableItem>();
        if (draggableItem == null)
        {
            draggableItem = newItem.AddComponent<DraggableItem>();
        }

		// Prevent spawned item from blocking subsequent clicks on the box
		if (temporaryIgnoreRaycast && temporaryIgnoreSeconds > 0f)
		{
			StartCoroutine(TemporarilyIgnoreRaycast(newItem, temporaryIgnoreSeconds));
		}
        
        draggableItem.snapMoveSpeed = 8f;
        draggableItem.lockOnSnap = true;
        
        currentItemIndex++;
		spawnedCount++;
        
		// If reached end and not looping, dim sprite
		if (!loopItems && currentItemIndex >= itemsToSpawn.Count && spriteRenderer != null)
		{
			spriteRenderer.color = Color.gray;
		}
    }

	private System.Collections.IEnumerator TemporarilyIgnoreRaycast(GameObject go, float seconds)
	{
		if (go == null) yield break;
		int originalLayer = go.layer;
		int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
		if (ignoreRaycast < 0)
		{
			// Fallback to disabling collider if layer is missing
			var col = go.GetComponent<Collider2D>();
			if (col != null)
			{
				bool orig = col.enabled;
				col.enabled = false;
				yield return new WaitForSeconds(seconds);
				if (col != null) col.enabled = orig;
			}
			yield break;
		}
		go.layer = ignoreRaycast;
		yield return new WaitForSeconds(seconds);
		if (go != null) go.layer = originalLayer;
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
