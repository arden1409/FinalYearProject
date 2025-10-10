using UnityEngine;
using UnityEngine.EventSystems;

public class SnapZone : MonoBehaviour, IDropHandler
{
    public string acceptType = "Default";
    public bool occupied = false;
    public bool requireExactMatch = true; // if false, accepts any item

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dragged = eventData.pointerDrag;
        if (dragged == null) return;

        DraggableItem di = dragged.GetComponent<DraggableItem>();
        if (di == null) return;

        if (occupied)
        {
            di.ResetPosition();
            return;
        }

        if (requireExactMatch && di.itemType != acceptType)
        {
            di.ResetPosition();
            return;
        }

        // Accept the item and snap it into position
        di.SnapTo(transform);
        occupied = true;
    }

	private void OnTransformChildrenChanged()
	{
		occupied = transform.childCount > 0;
	}
}
