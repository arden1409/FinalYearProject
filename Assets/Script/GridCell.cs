using UnityEngine;

public class GridCell : MonoBehaviour
{
	public bool occupied = false;
	public DraggableItem currentItem;

	public void SetOccupied(DraggableItem item)
	{
		occupied = item != null;
		currentItem = item;
	}
}


