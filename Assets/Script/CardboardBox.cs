using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardboardBox : MonoBehaviour, IPointerDownHandler
{
    [Header("Box Settings")]
    public List<GameObject> itemsToSpawn = new List<GameObject>();
    public Transform spawnPoint;
	public bool loopItems = false;
	public float temporaryIgnoreSeconds = 0.15f;
    public bool temporaryIgnoreRaycast = true;
    [SerializeField] private LevelManager levelManager;
    
    [Header("Animation")]
    public Animator boxAnimator;
    public string animationTriggerName = "Open";
    
    [Header("Spawn Settings")]
    public float spawnMargin = 0.1f;
    
    [Header("Spawn Area")]
    public Collider2D spawnAreaCollider;
    public GameObject spawnAreaObject;
    public bool useSpawnArea = false;
    
    private SpriteRenderer spriteRenderer;
    private int currentItemIndex = 0;
    private Camera mainCamera;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spawnPoint == null)
            spawnPoint = transform;

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        
        if (boxAnimator == null)
        {
            boxAnimator = GetComponent<Animator>();
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

		itemsToSpawn.RemoveAll(go => go == null);
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (itemsToSpawn.Count == 0) return;
        
        SpawnNextItem();
        
        if (boxAnimator != null && !string.IsNullOrEmpty(animationTriggerName))
        {
            boxAnimator.SetTrigger(animationTriggerName);
        }
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
		
        Vector3 randomPosition = GetSpawnPosition();
        newItem.transform.position = randomPosition;
        
		DraggableItem draggableItem = newItem.GetComponent<DraggableItem>();

		if (temporaryIgnoreRaycast && temporaryIgnoreSeconds > 0f)
		{
			StartCoroutine(TemporarilyIgnoreRaycast(newItem, temporaryIgnoreSeconds));
		}
        
        if (draggableItem != null)
        {
            draggableItem.snapMoveSpeed = 8f;
            draggableItem.lockOnSnap = true;
        }

        if (levelManager != null)
        {
            levelManager.RegisterSpawnedObject(newItem);
        }
        
        currentItemIndex++;
        
		if (!loopItems && currentItemIndex >= itemsToSpawn.Count && spriteRenderer != null)
		{
			spriteRenderer.color = Color.gray;
		}
    }
    
    private Bounds GetScreenBounds()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        if (mainCamera != null && mainCamera.orthographic)
        {
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            
            Vector3 center = mainCamera.transform.position;
            center.z = 0f;
            
            Bounds bounds = new Bounds(center, new Vector3(width, height, 0f));
            
            float marginX = width * spawnMargin;
            float marginY = height * spawnMargin;
            bounds.size = new Vector3(width - marginX * 2f, height - marginY * 2f, 0f);
            
            return bounds;
        }
        
        return new Bounds(spawnPoint.position, new Vector3(10f, 10f, 0f));
    }
    
    private Bounds GetSpawnAreaBounds()
    {
        if (spawnAreaCollider != null)
        {
            return spawnAreaCollider.bounds;
        }
        
        if (spawnAreaObject != null)
        {
            Collider2D col = spawnAreaObject.GetComponent<Collider2D>();
            if (col != null)
            {
                return col.bounds;
            }
            
            SpriteRenderer sr = spawnAreaObject.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                return sr.bounds;
            }
            
            RectTransform rect = spawnAreaObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Vector3 min = corners[0];
                Vector3 max = corners[0];
                for (int i = 1; i < 4; i++)
                {
                    min = Vector3.Min(min, corners[i]);
                    max = Vector3.Max(max, corners[i]);
                }
                Bounds bounds = new Bounds();
                bounds.SetMinMax(min, max);
                return bounds;
            }
        }
        
        return new Bounds();
    }
    
    private Vector3 GetSpawnPosition()
    {
        Bounds spawnAreaBounds = GetSpawnAreaBounds();
        bool hasSpawnArea = useSpawnArea && spawnAreaBounds.size.magnitude > 0.1f;
        
        if (hasSpawnArea)
        {
            float x = Random.Range(spawnAreaBounds.min.x, spawnAreaBounds.max.x);
            float y = Random.Range(spawnAreaBounds.min.y, spawnAreaBounds.max.y);
            return new Vector3(x, y, 0f);
        }
        
        Bounds screenBounds = GetScreenBounds();
        float screenX = Random.Range(screenBounds.min.x, screenBounds.max.x);
        float screenY = Random.Range(screenBounds.min.y, screenBounds.max.y);
        return new Vector3(screenX, screenY, 0f);
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
            spriteRenderer.color = Color.white;
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