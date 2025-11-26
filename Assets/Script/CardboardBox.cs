using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardboardBox : MonoBehaviour, IPointerDownHandler
{
    [Header("Box Settings")]
    public List<GameObject> itemsToSpawn = new List<GameObject>();
    public Transform spawnPoint;
    public float spawnOffset = 0.5f;
	public bool loopItems = false;
	public float temporaryIgnoreSeconds = 0.15f;
	[Tooltip("Spiral step radius to spread spawned items around the box")]
	public float spiralStep = 0.3f;
    [Tooltip("Temporarily move spawned item to IgnoreRaycast layer")]
    public bool temporaryIgnoreRaycast = true;
    
    [Header("Visual Feedback")]
    public GameObject highlightEffect;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    
    private SpriteRenderer spriteRenderer;
    private int currentItemIndex = 0;
	private int spawnedCount = 0;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spawnPoint == null)
            spawnPoint = transform;

		itemsToSpawn.RemoveAll(go => go == null);
    }
    
	public void OnPointerDown(PointerEventData eventData)
    {
        if (itemsToSpawn.Count == 0) return;
        
        SpawnNextItem();
    }
    
    private void SpawnNextItem()
    {
		if (itemsToSpawn.Count == 0) return;

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
		
		int ring = spawnedCount / 6;
		int slot = spawnedCount % 6;
		float angle = slot * Mathf.Deg2Rad * 60f;
		float radius = (ring + 1) * spiralStep;
		Vector3 spiral = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
		Vector3 spawnPosition = spawnPoint.position + Vector3.up * spawnOffset + spiral;
        newItem.transform.position = spawnPosition;
        
        DraggableItem draggableItem = newItem.GetComponent<DraggableItem>();
        if (draggableItem == null)
        {
            draggableItem = newItem.AddComponent<DraggableItem>();
        }

		if (temporaryIgnoreRaycast && temporaryIgnoreSeconds > 0f)
		{
			StartCoroutine(TemporarilyIgnoreRaycast(newItem, temporaryIgnoreSeconds));
		}
        
        draggableItem.snapMoveSpeed = 8f;
        draggableItem.lockOnSnap = true;
        
        currentItemIndex++;
		spawnedCount++;
        
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
			Collider2D col = go.GetComponent<Collider2D>();
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