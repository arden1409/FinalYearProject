using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Identification")]
    public string itemType = "Default";

    [Header("Behaviour")]
    public bool lockOnSnap = false;
    public int dragSortingOrder = 100;
    public float snapMoveSpeed = 8f;
    public bool autoSortByY = false;
    public int ySortFactor = 100;
    public int dragSortingBoost = 1000;

    [Header("Hover Highlight")]
    public bool showHoverOutline = true;
    public Color hoverOutlineColor = new Color(0f, 1f, 0f, 0.9f);
    [Tooltip("Outline thickness in pixels (for pixel art)")]
    public int hoverOutlinePixels = 2;

    [Header("Events")]
    public UnityEvent onPlaced;
    public UnityEvent onReset;

    [Header("Sorting")]
    [Tooltip("Move this item to the top sorting order when clicked (requires autoSortByY = false).")]
    public bool bringToFrontOnClick = true;

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
    private Vector3 worldDragOffset;
    private Vector2 uiDragOffset;
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
        if (spriteRenderer != null && autoSortByY && !isDragging)
        {
            baseSortingOrder = -(int)(transform.position.y * ySortFactor);
            spriteRenderer.sortingOrder = baseSortingOrder;
            if (outlineRenderers != null)
            {
                foreach (SpriteRenderer sr in outlineRenderers)
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
        BringToFrontIfNeeded();
        isSnapped = false;
        originalPosition = transform.position;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        GridCell parentCell = transform.parent != null ? transform.parent.GetComponent<GridCell>() : null;
        if (parentCell != null) parentCell.SetOccupied(null);

        // Record state before moving for undo/redo
        if (UndoRedoManager.Instance != null)
        {
            UndoRedoManager.Instance.RecordActionBefore(this);
        }

        if (rectTransform != null && canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.9f;
        }
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
                    foreach (SpriteRenderer sr in outlineRenderers)
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
                    foreach (SpriteRenderer sr in outlineRenderers)
                    {
                        if (sr == null) continue;
                        sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                    }
                }
            }
        }

        if (rectTransform != null && parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            transform.SetParent(parentCanvas.transform, true);
            RectTransform canvasRect = parentCanvas.transform as RectTransform;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, parentCanvas.worldCamera, out localPoint))
            {
                uiDragOffset = rectTransform.anchoredPosition - localPoint;
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

        StartCoroutine(EndDragCoroutine());
    }

    private IEnumerator EndDragCoroutine()
    {
        yield return new WaitForEndOfFrame();

        if (!isSnapped)
        {
            GridSnapZone snapZone = FindGridSnapZoneAtPosition(transform.position);
            if (snapZone != null)
            {
                PointerEventData eventData = new PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                eventData.pointerDrag = gameObject;
                snapZone.OnDrop(eventData);
            }
            else
            {
                originalPosition = transform.position;
                originalParent = transform.parent;
                originalSiblingIndex = transform.GetSiblingIndex();
            }
        }
        else
        {
            onPlaced?.Invoke();
        }

        if (spriteRenderer != null && !isSnapped)
        {
            isDragging = false;
            spriteRenderer.sortingOrder = autoSortByY ? baseSortingOrder : originalSortingOrder;
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

    private GridSnapZone FindGridSnapZoneAtPosition(Vector3 position)
    {
        GridSnapZone[] zones = FindObjectsByType<GridSnapZone>(FindObjectsSortMode.None);

        foreach (GridSnapZone zone in zones)
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
                foreach (SpriteRenderer sr in outlineRenderers)
                {
                    if (sr == null) continue;
                    sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                }
            }
        }

        // Record state after snap for undo/redo
        if (UndoRedoManager.Instance != null)
        {
            UndoRedoManager.Instance.RecordActionAfter(this);
        }
        
        // Play sound when item is placed successfully
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayPlaceItem();
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

    private void BringToFrontIfNeeded()
    {
        if (!bringToFrontOnClick)
            return;

        if (rectTransform != null && parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
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