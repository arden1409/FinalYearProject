using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Identification")]
    public string itemType = "Default";

    [Header("Behaviour")]
    public bool lockOnSnap = false;
    public int dragSortingOrder = 100;
    public float snapMoveSpeed = 8f;
    public bool autoSortByY = true;        // auto order sprites by Y (isometric/top-down)
    public int ySortFactor = 100;          // higher → more sensitivity to Y
    public int dragSortingBoost = 1000;    // added while dragging to ensure top-most

    [Header("Hover Highlight")]
    public bool showHoverOutline = true;
    public Color hoverOutlineColor = new Color(0f, 1f, 0f, 0.9f);
    [Tooltip("Outline thickness in pixels (for pixel art)")]
    public int hoverOutlinePixels = 2;

    [Header("Events")]
    public UnityEvent onPlaced;
    public UnityEvent onReset;

    // Private variables
    private Vector3 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;
    private SpriteRenderer spriteRenderer;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private int originalSortingOrder;
    private bool isSnapped = false;
    private Vector3 worldDragOffset; // preserves grab offset in world space
    private Vector2 uiDragOffset;    // preserves grab offset in UI space
    private bool isDragging = false;
    private int baseSortingOrder;
    private GameObject outlineRoot;
    private SpriteRenderer[] outlineRenderers;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Prepare pixel-outline (8-direction offsets) for crisp look
        if (showHoverOutline && spriteRenderer != null)
        {
            outlineRoot = new GameObject("HoverOutlineRoot");
            outlineRoot.transform.SetParent(transform, false);
            outlineRoot.transform.localPosition = Vector3.zero;

            outlineRenderers = new SpriteRenderer[8];
            float ppu = spriteRenderer.sprite != null && spriteRenderer.sprite.pixelsPerUnit > 0
                ? spriteRenderer.sprite.pixelsPerUnit
                : 100f;
            float step = hoverOutlinePixels / ppu; // world units per pixel

            Vector2[] dirs = new Vector2[]
            {
                new Vector2(-1,  0), new Vector2(1,  0),
                new Vector2( 0, -1), new Vector2(0,  1),
                new Vector2(-1, -1), new Vector2(-1, 1),
                new Vector2( 1, -1), new Vector2( 1, 1)
            };

            for (int i = 0; i < dirs.Length; i++)
            {
                var go = new GameObject($"Outline_{i}");
                go.transform.SetParent(outlineRoot.transform, false);
                go.transform.localPosition = (Vector3)(dirs[i] * step);
                var sr = go.AddComponent<SpriteRenderer>();
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
        if (spriteRenderer != null && autoSortByY && !isDragging)
        {
            baseSortingOrder = -(int)(transform.position.y * ySortFactor);
            spriteRenderer.sortingOrder = baseSortingOrder;
            if (outlineRenderers != null)
            {
                foreach (var sr in outlineRenderers)
                {
                    if (sr == null) continue;
                    sr.sortingLayerID = spriteRenderer.sortingLayerID;
                    sr.sortingOrder = baseSortingOrder - 1;
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isSnapped = false;
        originalPosition = transform.position;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Free current cell when starting to drag
        var parentCell = transform.parent != null ? transform.parent.GetComponent<GridCell>() : null;
        if (parentCell != null) parentCell.SetOccupied(null);

        // Setup UI blocking for drag
        if (rectTransform != null && canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.9f;
        }

        // Bring sprite to front while dragging
        if (spriteRenderer != null)
        {
            isDragging = true;
            if (autoSortByY)
            {
                baseSortingOrder = -(int)(transform.position.y * ySortFactor);
                originalSortingOrder = baseSortingOrder;
                spriteRenderer.sortingOrder = baseSortingOrder + dragSortingBoost;
                if (outlineRenderers != null)
                {
                    foreach (var sr in outlineRenderers)
                    {
                        if (sr == null) continue;
                        sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                    }
                }
            }
            else
            {
                originalSortingOrder = spriteRenderer.sortingOrder;
                spriteRenderer.sortingOrder = dragSortingOrder;
                if (outlineRenderers != null)
                {
                    foreach (var sr in outlineRenderers)
                    {
                        if (sr == null) continue;
                        sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                    }
                }
            }
        }

        // Reparent UI elements to canvas for overlay
        if (rectTransform != null && parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            transform.SetParent(parentCanvas.transform, true);
        }

        // Compute grab offset so the item doesn't snap its center to the cursor
        if (rectTransform != null && parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            RectTransform canvasRect = parentCanvas.transform as RectTransform;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, parentCanvas.worldCamera, out localPoint))
            {
                uiDragOffset = (rectTransform != null) ? (rectTransform.anchoredPosition - localPoint) : Vector2.zero;
            }
        }
        else
        {
            Camera cam = eventData.pressEventCamera ?? Camera.main;
            if (cam != null)
            {
                float z = cam.WorldToScreenPoint(transform.position).z;
                Vector3 screenPoint = new Vector3(eventData.position.x, eventData.position.y, z);
                Vector3 worldPos = cam.ScreenToWorldPoint(screenPoint);
                worldDragOffset = new Vector3(
                    transform.position.x - worldPos.x,
                    transform.position.y - worldPos.y,
                    0f
                );
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Handle UI elements
        if (rectTransform != null && parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            RectTransform canvasRect = parentCanvas.transform as RectTransform;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, parentCanvas.worldCamera, out localPoint))
            {
                rectTransform.anchoredPosition = localPoint + uiDragOffset;
            }
            return;
        }

        // Handle world space sprites
        Camera cam = eventData.pressEventCamera ?? Camera.main;
        if (cam == null) return;
        
        float z = cam.WorldToScreenPoint(transform.position).z;
        Vector3 screenPoint = new Vector3(eventData.position.x, eventData.position.y, z);
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPoint);
        transform.position = new Vector3(worldPos.x + worldDragOffset.x, worldPos.y + worldDragOffset.y, transform.position.z);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        // Wait one frame before handling snap logic
        StartCoroutine(EndDragCoroutine());
    }

    private IEnumerator EndDragCoroutine()
    {
        yield return new WaitForEndOfFrame();

        if (!isSnapped)
        {
            // Try to find and snap to nearest GridSnapZone
            GridSnapZone snapZone = FindGridSnapZoneAtPosition(transform.position);
            if (snapZone != null)
            {
                PointerEventData eventData = new PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                eventData.pointerDrag = gameObject;
                snapZone.OnDrop(eventData);
            }
            else
            {
                ResetPosition();
            }
        }
        else
        {
            onPlaced?.Invoke();
        }

        // Restore sorting order
        if (spriteRenderer != null && !isSnapped)
        {
            isDragging = false;
            spriteRenderer.sortingOrder = autoSortByY ? baseSortingOrder : originalSortingOrder;
            if (outlineRenderers != null)
            {
                foreach (var sr in outlineRenderers)
                {
                    if (sr == null) continue;
                    sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                }
            }
        }
    }

    private GridSnapZone FindGridSnapZoneAtPosition(Vector3 position)
    {
        GridSnapZone[] zones = FindObjectsOfType<GridSnapZone>();
        
        foreach (var zone in zones)
        {
            Collider2D col = zone.GetComponent<Collider2D>();
            if (col != null && col.bounds.Contains(position))
            {
                return zone;
            }
        }
        return null;
    }

    public void SnapTo(Transform snapTarget)
    {
        isSnapped = true;
        bool isUI = rectTransform != null && snapTarget.GetComponent<RectTransform>() != null;

        if (isUI)
        {
            transform.SetParent(snapTarget, false);
            if (snapMoveSpeed <= 0f)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(SmoothMoveLocalUI(Vector2.zero, 1f / snapMoveSpeed));
            }
        }
        else
        {
            transform.SetParent(snapTarget, true);
            if (snapMoveSpeed <= 0f)
            {
                transform.position = snapTarget.position;
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(SmoothMove(snapTarget.position, 1f / snapMoveSpeed));
            }
        }

        if (spriteRenderer != null)
        {
            isDragging = false;
            spriteRenderer.sortingOrder = autoSortByY ? baseSortingOrder : originalSortingOrder;
            if (outlineRenderers != null)
            {
                foreach (var sr in outlineRenderers)
                {
                    if (sr == null) continue;
                    sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                }
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

    private IEnumerator SmoothMove(Vector3 targetPos, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, targetPos, t);
            yield return null;
        }
        transform.position = targetPos;
    }

    public void ResetPosition()
    {
        isSnapped = false;
        transform.SetParent(originalParent, true);
        transform.position = originalPosition;
        transform.SetSiblingIndex(originalSiblingIndex);
        onReset?.Invoke();
    }

    private IEnumerator SmoothMoveLocalUI(Vector2 targetAnchoredPos, float duration)
    {
        Vector2 start = rectTransform.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            rectTransform.anchoredPosition = Vector2.Lerp(start, targetAnchoredPos, t);
            yield return null;
        }
        rectTransform.anchoredPosition = targetAnchoredPos;
    }
}
