using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class FreeDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Identification")]
    public string itemType = "Default";

    [Header("Behaviour")]
    public int dragSortingOrder = 100;
    public bool autoSortByY = false;
    public int ySortFactor = 100;
    public int dragSortingBoost = 1000;

    [Header("Hover Highlight")]
    public bool showHoverOutline = true;
    public Color hoverOutlineColor = new Color(0f, 1f, 0f, 0.9f);
    [Tooltip("Outline thickness in pixels (for pixel art)")]
    public int hoverOutlinePixels = 2;

    [Header("Events")]
    public UnityEvent onDragStart;
    public UnityEvent onDragEnd;
    public UnityEvent onPlaced;

    [Header("Free Placement")]
    [Tooltip("If true, item stays at dropped position")]
    public bool keepDroppedPosition = true;

    [Header("Sorting")]
    [Tooltip("Move this item to the top sorting order when clicked (requires autoSortByY = false).")]
    public bool bringToFrontOnClick = true;

    private Vector3 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;
    private SpriteRenderer spriteRenderer;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private int originalSortingOrder;
    private Vector3 worldDragOffset;
    private Vector2 uiDragOffset;
    private bool isDragging = false;
    private bool isPositionLocked = false;
    private int baseSortingOrder;
    private GameObject outlineRoot;
    private SpriteRenderer[] outlineRenderers;
    [SerializeField] private LevelManager levelManager;

    void Awake()
    {
        DraggableItem draggableItem = GetComponent<DraggableItem>();
        if (draggableItem != null)
        {
            Debug.LogError($"[FreeDraggableItem] {name} has both FreeDraggableItem and DraggableItem scripts! This will cause conflicts. Please remove one of them.");
        }

        rectTransform = GetComponent<RectTransform>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        levelManager?.RegisterSpawnedObject(gameObject);

        if (showHoverOutline && spriteRenderer != null)
        {
            outlineRoot = new GameObject("HoverOutlineRoot");
            outlineRoot.transform.SetParent(transform, false);
            outlineRoot.transform.localPosition = Vector3.zero;

            outlineRenderers = new SpriteRenderer[8];
            float ppu = spriteRenderer.sprite != null && spriteRenderer.sprite.pixelsPerUnit > 0
                ? spriteRenderer.sprite.pixelsPerUnit
                : 100f;
            float step = hoverOutlinePixels / ppu;

            Vector2[] dirs = new Vector2[]
            {
                new Vector2(-1,  0), new Vector2(1,  0),
                new Vector2( 0, -1), new Vector2(0,  1),
                new Vector2(-1, -1), new Vector2(-1, 1),
                new Vector2( 1, -1), new Vector2( 1, 1)
            };

            for (int i = 0; i < dirs.Length; i++)
            {
                GameObject go = new GameObject($"Outline_{i}");
                go.transform.SetParent(outlineRoot.transform, false);
                go.transform.localPosition = (Vector3)(dirs[i] * step);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spriteRenderer.sprite;
                sr.color = hoverOutlineColor;
                sr.sortingLayerID = spriteRenderer.sortingLayerID;
                sr.sortingOrder = (autoSortByY ? baseSortingOrder : spriteRenderer.sortingOrder) - 1;
                outlineRenderers[i] = sr;
            }

            outlineRoot.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (isPositionLocked && !isDragging)
        {
            float distance = Vector3.Distance(transform.position, originalPosition);
            if (distance > 0.1f)
            {
                transform.position = originalPosition;
            }
        }

        if (spriteRenderer != null && autoSortByY && !isDragging)
        {
            baseSortingOrder = -(int)(transform.position.y * ySortFactor);
            spriteRenderer.sortingOrder = baseSortingOrder;
            if (outlineRenderers != null)
            {
                foreach (SpriteRenderer sr in outlineRenderers)
                {
                    if (sr == null) continue;
                    sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        BringToFrontIfNeeded();
        isPositionLocked = false;
        originalPosition = transform.position;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Ghi lại trạng thái trước khi di chuyển cho undo/redo
        if (UndoRedoManager.Instance != null)
        {
            UndoRedoManager.Instance.RecordActionBefore(this);
        }

        if (rectTransform != null && parentCanvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, parentCanvas.worldCamera, out uiDragOffset);
            uiDragOffset = rectTransform.anchoredPosition - uiDragOffset;
        }
        else if (spriteRenderer != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            worldPos.z = transform.position.z;
            worldDragOffset = transform.position - worldPos;
        }
        if (spriteRenderer != null)
        {
            originalSortingOrder = spriteRenderer.sortingOrder;
            if (autoSortByY)
            {
                baseSortingOrder = -(int)(transform.position.y * ySortFactor);
                spriteRenderer.sortingOrder = baseSortingOrder + dragSortingBoost;
            }
            else
            {
                spriteRenderer.sortingOrder = dragSortingOrder;
            }
            
            if (outlineRenderers != null)
            {
                foreach (SpriteRenderer sr in outlineRenderers)
                {
                    if (sr == null) continue;
                    sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                }
            }
        }

        isDragging = true;
        onDragStart?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform != null && parentCanvas != null)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform, eventData.position, parentCanvas.worldCamera, out localPoint))
            {
                rectTransform.anchoredPosition = localPoint + uiDragOffset;
            }
        }
        else
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            worldPos.z = transform.position.z;
            transform.position = worldPos + worldDragOffset;

            if (spriteRenderer != null && autoSortByY)
            {
                baseSortingOrder = -(int)(transform.position.y * ySortFactor);
                spriteRenderer.sortingOrder = baseSortingOrder + dragSortingBoost;
                if (outlineRenderers != null)
                {
                    foreach (SpriteRenderer sr in outlineRenderers)
                    {
                        if (sr == null) continue;
                        sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                    }
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        Vector3 droppedPosition = transform.position;

        if (keepDroppedPosition)
        {
            originalPosition = droppedPosition;
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            isPositionLocked = true;
            StartCoroutine(MonitorPosition(droppedPosition));
        }
        else
        {
            transform.SetParent(originalParent, true);
            transform.position = originalPosition;
            transform.SetSiblingIndex(originalSiblingIndex);
        }
        if (spriteRenderer != null)
        {
            if (autoSortByY)
            {
                baseSortingOrder = -(int)(transform.position.y * ySortFactor);
                spriteRenderer.sortingOrder = baseSortingOrder;
            }
            else
            {
                spriteRenderer.sortingOrder = originalSortingOrder;
            }
            
            if (outlineRenderers != null)
            {
                foreach (SpriteRenderer sr in outlineRenderers)
                {
                    if (sr == null) continue;
                    sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                }
            }
        }

        onDragEnd?.Invoke();
        onPlaced?.Invoke();

        // Ghi lại trạng thái sau khi thả cho undo/redo
        if (UndoRedoManager.Instance != null)
        {
            UndoRedoManager.Instance.RecordActionAfter(this);
        }
        
        // Phát âm thanh khi đặt item thành công
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayPlaceItem();
        }
    }

    private IEnumerator MonitorPosition(Vector3 expectedPosition)
    {
        float threshold = 0.1f;
        for (int i = 0; i < 10; i++)
        {
            yield return null;
            float distance = Vector3.Distance(transform.position, expectedPosition);
            if (distance > threshold)
            {
                transform.position = expectedPosition;
                originalPosition = expectedPosition;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outlineRoot != null && !isDragging) outlineRoot.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (outlineRoot != null) outlineRoot.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        BringToFrontIfNeeded();
    }

    public void ResetPosition()
    {
        isPositionLocked = false;
        transform.SetParent(originalParent, true);
        transform.position = originalPosition;
        transform.SetSiblingIndex(originalSiblingIndex);
    }

    private void BringToFrontIfNeeded()
    {
        if (!bringToFrontOnClick)
            return;

        if (rectTransform != null && parentCanvas != null)
        {
            transform.SetAsLastSibling();
        }

        if (spriteRenderer == null || autoSortByY)
            return;

        int newOrder = DragSortingUtility.GetNextSortingOrder();
        baseSortingOrder = newOrder;
        spriteRenderer.sortingOrder = newOrder;
        originalSortingOrder = newOrder;

        if (outlineRenderers == null) return;
        foreach (SpriteRenderer sr in outlineRenderers)
        {
            if (sr == null) continue;
            sr.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }
}



