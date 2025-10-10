using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identification")]
    public string itemType = "Default";   // Used to match with SnapZone

    [Header("Behaviour")]
    public bool lockOnSnap = true;        // Lock the object after a successful snap
    public int dragSortingOrder = 100;    // Sorting order when dragging above others
    public float snapMoveSpeed = 0f;      // >0 = smooth move (Lerp), =0 = instant snap

    [Header("Events (optional)")]
    public UnityEvent onPlaced;
    public UnityEvent onReset;

    // Internals
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

		// If currently in a SnapZone, free it when starting to drag out
		var parentZone = transform.parent != null ? transform.parent.GetComponent<SnapZone>() : null;
		if (parentZone != null) parentZone.occupied = false;

        // Ensure CanvasGroup exists for UI-based raycast blocking
        if (rectTransform != null && canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.9f;
        }

        // Bring the sprite to the front while dragging
        if (spriteRenderer != null)
        {
            originalSortingOrder = spriteRenderer.sortingOrder;
            spriteRenderer.sortingOrder = dragSortingOrder;
        }

        // For UI elements, reparent temporarily to the root canvas for overlay
        if (rectTransform != null && parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            transform.SetParent(parentCanvas.transform, true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // UI drag path (for Canvas overlay mode)
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

        // World-space drag path (for sprites)
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
        // Wait a frame to allow SnapZone's OnDrop to process first
        yield return new WaitForEndOfFrame();

        if (!isSnapped)
        {
            // If not snapped, return to original position
            ResetPosition();
        }
        else
        {
            // Successfully placed
            if (lockOnSnap) enabled = false;
            onPlaced?.Invoke();
        }

        // Restore sorting order
        if (spriteRenderer != null && !isSnapped)
            spriteRenderer.sortingOrder = originalSortingOrder;
    }

    // Called by SnapZone when a valid snap occurs
    public void SnapTo(Transform snapTarget)
    {
        isSnapped = true;

		bool isUI = rectTransform != null && snapTarget.GetComponent<RectTransform>() != null;

		if (isUI)
		{
			// Adopt local space of target for UI
			transform.SetParent(snapTarget, false);
			if (snapMoveSpeed <= 0f)
			{
				// Instant snap in UI local space
				rectTransform.anchoredPosition = Vector2.zero;
			}
			else
			{
				// Smooth move in UI local space
				StopCoroutine(nameof(SmoothMove));
				StopCoroutine(nameof(SmoothMoveLocalUI));
				StartCoroutine(SmoothMoveLocalUI(Vector2.zero, 1f / snapMoveSpeed));
			}
		}
		else
		{
			// World-space objects keep world position logic
			transform.SetParent(snapTarget, true);
			if (snapMoveSpeed <= 0f)
			{
				transform.position = snapTarget.position;
			}
			else
			{
				StopCoroutine(nameof(SmoothMove));
				StopCoroutine(nameof(SmoothMoveLocalUI));
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

    // Reset object to original position/parent
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
