using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identification")]
    public string itemType = "Default";

    [Header("Behaviour")]
    public bool lockOnSnap = false;
    public int dragSortingOrder = 100;
    public float snapMoveSpeed = 8f;

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

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
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
            originalSortingOrder = spriteRenderer.sortingOrder;
            spriteRenderer.sortingOrder = dragSortingOrder;
        }

        // Reparent UI elements to canvas for overlay
        if (rectTransform != null && parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            transform.SetParent(parentCanvas.transform, true);
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
                rectTransform.anchoredPosition = localPoint;
            }
            return;
        }

        // Handle world space sprites
        Camera cam = eventData.pressEventCamera ?? Camera.main;
        if (cam == null) return;
        
        float z = cam.WorldToScreenPoint(transform.position).z;
        Vector3 screenPoint = new Vector3(eventData.position.x, eventData.position.y, z);
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPoint);
        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
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
            spriteRenderer.sortingOrder = originalSortingOrder;
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
            spriteRenderer.sortingOrder = originalSortingOrder;
        }
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
