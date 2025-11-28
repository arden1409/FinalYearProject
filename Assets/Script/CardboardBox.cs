using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    
    [Header("Room Background")]
    public SpriteRenderer roomBackground;
    public Collider2D roomBackgroundCollider;
    public bool spawnOutsideRoomOnly = true;
    public float minDistanceFromRoom = 0.5f;
    
    [Header("UI Exclusion")]
    public List<RectTransform> uiPanelsToAvoid = new List<RectTransform>();
    public List<GameObject> uiGameObjectsToAvoid = new List<GameObject>();
    public float minDistanceFromUI = 0.3f;
    
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
		
        Vector3 randomPosition = GetSpawnPositionOutsideRoom();
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
    
    private Bounds GetRoomBounds()
    {
        Bounds roomBounds = new Bounds();
        
        if (roomBackgroundCollider != null)
        {
            roomBounds = roomBackgroundCollider.bounds;
        }
        else if (roomBackground != null)
        {
            if (roomBackground.sprite != null)
            {
                roomBounds = roomBackground.bounds;
            }
            else
            {
                roomBounds = new Bounds(roomBackground.transform.position, Vector3.one * 5f);
            }
        }
        else
        {
            SpriteRenderer foundRoom = FindFirstObjectByType<SpriteRenderer>();
            if (foundRoom != null && foundRoom.gameObject.name.ToLower().Contains("room"))
            {
                roomBounds = foundRoom.bounds;
            }
            else
            {
                return GetScreenBounds();
            }
        }
        
        return roomBounds;
    }
    
    private Vector3 GetSpawnPositionOutsideRoom()
    {
        Bounds screenBounds = GetScreenBounds();
        Bounds roomBounds = GetRoomBounds();
        roomBounds.Expand(minDistanceFromRoom * 2f);
        
        List<Bounds> uiBoundsList = GetUIBounds();
        
        if (!spawnOutsideRoomOnly)
        {
            return GetRandomPositionInScreenButOutsideRoom(screenBounds, roomBounds, uiBoundsList);
        }
        
        List<Vector3> candidatePositions = new List<Vector3>();
        if (screenBounds.max.y > roomBounds.max.y)
        {
            float x = Random.Range(screenBounds.min.x, screenBounds.max.x);
            float y = Random.Range(roomBounds.max.y + minDistanceFromRoom, screenBounds.max.y);
            Vector3 pos = new Vector3(x, y, 0f);
            if (IsPositionValid(pos, roomBounds, uiBoundsList))
            {
                candidatePositions.Add(pos);
            }
        }
        
        if (screenBounds.min.y < roomBounds.min.y)
        {
            float x = Random.Range(screenBounds.min.x, screenBounds.max.x);
            float y = Random.Range(screenBounds.min.y, roomBounds.min.y - minDistanceFromRoom);
            Vector3 pos = new Vector3(x, y, 0f);
            if (IsPositionValid(pos, roomBounds, uiBoundsList))
            {
                candidatePositions.Add(pos);
            }
        }
        
        if (screenBounds.min.x < roomBounds.min.x)
        {
            float x = Random.Range(screenBounds.min.x, roomBounds.min.x - minDistanceFromRoom);
            float y = Random.Range(screenBounds.min.y, screenBounds.max.y);
            Vector3 pos = new Vector3(x, y, 0f);
            if (IsPositionValid(pos, roomBounds, uiBoundsList))
            {
                candidatePositions.Add(pos);
            }
        }
        
        if (screenBounds.max.x > roomBounds.max.x)
        {
            float x = Random.Range(roomBounds.max.x + minDistanceFromRoom, screenBounds.max.x);
            float y = Random.Range(screenBounds.min.y, screenBounds.max.y);
            Vector3 pos = new Vector3(x, y, 0f);
            if (IsPositionValid(pos, roomBounds, uiBoundsList))
            {
                candidatePositions.Add(pos);
            }
        }
        
        if (candidatePositions.Count > 0)
        {
            return candidatePositions[Random.Range(0, candidatePositions.Count)];
        }
        
        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = Random.Range(screenBounds.min.x, screenBounds.max.x);
            float y = Random.Range(screenBounds.min.y, screenBounds.max.y);
            Vector3 pos = new Vector3(x, y, 0f);
            
            if (IsPositionValid(pos, roomBounds, uiBoundsList))
            {
                return pos;
            }
        }
        Vector3 roomCenter = roomBounds.center;
        Vector3[] corners = new Vector3[]
        {
            new Vector3(screenBounds.min.x, screenBounds.min.y, 0f),
            new Vector3(screenBounds.max.x, screenBounds.min.y, 0f),
            new Vector3(screenBounds.min.x, screenBounds.max.y, 0f),
            new Vector3(screenBounds.max.x, screenBounds.max.y, 0f)
        };
        
        Vector3 bestCorner = corners[0];
        float bestScore = float.MinValue;
        
        foreach (var corner in corners)
        {
            float distFromRoom = Vector3.Distance(corner, roomCenter);
            float distFromUI = float.MaxValue;
            
            foreach (var uiBounds in uiBoundsList)
            {
                float dist = Vector3.Distance(corner, uiBounds.center);
                if (dist < distFromUI)
                {
                    distFromUI = dist;
                }
            }
            
            float score = distFromRoom + distFromUI;
            if (score > bestScore)
            {
                bestScore = score;
                bestCorner = corner;
            }
        }
        
        return bestCorner;
    }
    
    private List<Bounds> GetUIBounds()
    {
        List<Bounds> uiBoundsList = new List<Bounds>();
        
        foreach (var rect in uiPanelsToAvoid)
        {
            if (rect != null && rect.gameObject.activeInHierarchy)
            {
                Bounds bounds = GetRectTransformWorldBounds(rect);
                bounds.Expand(minDistanceFromUI * 2f);
                uiBoundsList.Add(bounds);
            }
        }
        
        foreach (var go in uiGameObjectsToAvoid)
        {
            if (go != null && go.activeInHierarchy)
            {
                RectTransform rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Bounds bounds = GetRectTransformWorldBounds(rect);
                    bounds.Expand(minDistanceFromUI * 2f);
                    uiBoundsList.Add(bounds);
                }
            }
        }
        
        return uiBoundsList;
    }
    
    private Bounds GetRectTransformWorldBounds(RectTransform rect)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }
        
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        
        Vector3 min = corners[0];
        Vector3 max = corners[0];
        
        for (int i = 1; i < 4; i++)
        {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }
        
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            if (mainCamera != null && mainCamera.orthographic)
            {
                float screenHeight = Screen.height;
                float screenWidth = Screen.width;
                float camHeight = mainCamera.orthographicSize * 2f;
                float camWidth = camHeight * mainCamera.aspect;
                Vector3 camPos = mainCamera.transform.position;
                
                float worldXMin = camPos.x - camWidth * 0.5f + (min.x / screenWidth) * camWidth;
                float worldXMax = camPos.x - camWidth * 0.5f + (max.x / screenWidth) * camWidth;
                float worldYMin = camPos.y - camHeight * 0.5f + (min.y / screenHeight) * camHeight;
                float worldYMax = camPos.y - camHeight * 0.5f + (max.y / screenHeight) * camHeight;
                
                min = new Vector3(worldXMin, worldYMin, 0f);
                max = new Vector3(worldXMax, worldYMax, 0f);
            }
            else if (mainCamera != null)
            {
                min = mainCamera.ScreenToWorldPoint(new Vector3(min.x, min.y, mainCamera.nearClipPlane + 1f));
                max = mainCamera.ScreenToWorldPoint(new Vector3(max.x, max.y, mainCamera.nearClipPlane + 1f));
            }
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
        {
            min = new Vector3(min.x, min.y, 0f);
            max = new Vector3(max.x, max.y, 0f);
        }
        
        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        bounds.center = new Vector3(bounds.center.x, bounds.center.y, 0f);
        
        return bounds;
    }
    
    private bool IsPositionValid(Vector3 position, Bounds roomBounds, List<Bounds> uiBoundsList)
    {
        if (roomBounds.Contains(position))
        {
            return false;
        }
        
        foreach (var uiBounds in uiBoundsList)
        {
            if (uiBounds.Contains(position))
            {
                return false;
            }
            
            Vector3 closestPoint = uiBounds.ClosestPoint(position);
            float distance = Vector3.Distance(position, closestPoint);
            if (distance < minDistanceFromUI)
            {
                return false;
            }
            
            Bounds expandedBounds = uiBounds;
            expandedBounds.Expand(minDistanceFromUI * 2f);
            if (expandedBounds.Contains(position))
            {
                return false;
            }
        }
        
        return true;
    }
    
    private Vector3 GetRandomPositionInScreenButOutsideRoom(Bounds screenBounds, Bounds roomBounds, List<Bounds> uiBoundsList)
    {
        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = Random.Range(screenBounds.min.x, screenBounds.max.x);
            float y = Random.Range(screenBounds.min.y, screenBounds.max.y);
            Vector3 pos = new Vector3(x, y, 0f);
            
            if (IsPositionValid(pos, roomBounds, uiBoundsList))
            {
                return pos;
            }
        }
        
        return GetSpawnPositionOutsideRoom();
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