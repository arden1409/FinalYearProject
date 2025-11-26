using UnityEngine;
using UnityEngine.EventSystems;

public class GridSnapZone : MonoBehaviour, IDropHandler
{
	[Header("Grid Settings")]
	public int columns = 3;
	public int rows = 3;
	public Vector2 cellSize = new Vector2(1f, 1f);
	public Vector2 cellSpacing = new Vector2(0.1f, 0.1f);
	public string acceptType = "Default";
	public bool requireExactMatch = true;

	[Header("Gizmos")]
	public bool drawGizmos = true;
	public Color gizmoOutlineColor = new Color(0f, 1f, 1f, 0.6f);
	public Color gizmoCellColor = new Color(0f, 1f, 0.5f, 0.8f);

	[Header("Isometric")]
	public bool isIsometric = false;
	[Tooltip("Tile size in world units or pixels (UI) before rotation/skew for iso look")]
	public Vector2 isoTileSize = new Vector2(1f, 0.5f);
	[Tooltip("Optional Y offset per column to create staggered look (0 = diamond aligned)")]
	public float isoColumnYOffset = 0f;

	private GridCell[,] cells;

	void Awake()
	{
		BuildGrid();
	}

	public void BuildGrid()
	{
		// Clear existing children that might be leftover runtime cells
		cells = new GridCell[columns, rows];
		for (int y = 0; y < rows; y++)
		{
			for (int x = 0; x < columns; x++)
			{
				GameObject cellObj = new GameObject($"Cell_{x}_{y}");
				cellObj.transform.SetParent(transform, false);
				cellObj.transform.localPosition = ComputeCellLocalPosition(x, y);
				var cell = cellObj.AddComponent<GridCell>();
				cells[x, y] = cell;
			}
		}
	}

	private Vector3 ComputeCellLocalPosition(int x, int y)
	{
		float width = columns * cellSize.x + (columns - 1) * cellSpacing.x;
		float height = rows * cellSize.y + (rows - 1) * cellSpacing.y;
		Vector2 origin = new Vector2(-width * 0.5f + cellSize.x * 0.5f, -height * 0.5f + cellSize.y * 0.5f);
		if (!isIsometric)
		{
			return new Vector3(
				origin.x + x * (cellSize.x + cellSpacing.x),
				origin.y + y * (cellSize.y + cellSpacing.y),
				0f
			);
		}

		// Isometric layout: diamond grid using isoTileSize, optional stagger
		float w = isoTileSize.x;
		float h = isoTileSize.y;
		float isoX = (x - y) * (w * 0.5f);
		float isoY = (x + y) * (h * 0.5f) + (x * isoColumnYOffset);
		return new Vector3(isoX, isoY, 0f);
	}

	public void OnDrop(PointerEventData eventData)
	{
		var dragged = eventData.pointerDrag;
		if (dragged == null) return;

		var di = dragged.GetComponent<DraggableItem>();
		if (di == null) return;

		if (requireExactMatch && di.itemType != acceptType)
		{
			di.ResetPosition();
			return;
		}

		GridCell targetCell = GetNearestFreeCell(di.transform.position);
		if (targetCell == null)
		{
			di.ResetPosition();
			return;
		}

		di.SnapTo(targetCell.transform);
		targetCell.SetOccupied(di);
	}

	private GridCell GetNearestFreeCell(Vector3 worldPosition)
	{
		GridCell best = null;
		float bestDist = float.MaxValue;
		for (int y = 0; y < rows; y++)
		{
			for (int x = 0; x < columns; x++)
			{
				var cell = cells[x, y];
				if (cell == null || cell.occupied) continue;
				float d = Vector3.SqrMagnitude(cell.transform.position - worldPosition);
				if (d < bestDist)
				{
					bestDist = d;
					best = cell;
				}
			}
		}
		return best;
	}

	void OnDrawGizmos()
	{
		if (!drawGizmos) return;

		// Draw in local space so positions match ComputeCellLocalPosition
		Gizmos.matrix = transform.localToWorldMatrix;

		float width = columns * cellSize.x + (columns - 1) * cellSpacing.x;
		float height = rows * cellSize.y + (rows - 1) * cellSpacing.y;

		if (!isIsometric)
		{
			// Outline of entire grid (rectangular)
			Gizmos.color = gizmoOutlineColor;
			Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, 0.01f));

			// Individual cells
			Gizmos.color = gizmoCellColor;
			Vector3 cellSize3 = new Vector3(cellSize.x, cellSize.y, 0.005f);
			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < columns; x++)
				{
					Vector3 p = ComputeCellLocalPosition(x, y);
					Gizmos.DrawWireCube(p, cellSize3);
				}
			}
		}
		else
		{
			// Isometric: draw diamond tiles
			Gizmos.color = gizmoCellColor;
			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < columns; x++)
				{
					Vector3 p = ComputeCellLocalPosition(x, y);
					DrawIsoDiamondGizmo(p, isoTileSize);
				}
			}
		}
	}

	private void DrawIsoDiamondGizmo(Vector3 center, Vector2 tile)
	{
		// Four points of a diamond (lozenge)
		Vector3 right = new Vector3(tile.x * 0.5f, 0f, 0f);
		Vector3 left = -right;
		Vector3 up = new Vector3(0f, tile.y * 0.5f, 0f);
		Vector3 down = -up;

		Vector3 p0 = center + right;
		Vector3 p1 = center + up;
		Vector3 p2 = center + left;
		Vector3 p3 = center + down;

		Gizmos.DrawLine(p0, p1);
		Gizmos.DrawLine(p1, p2);
		Gizmos.DrawLine(p2, p3);
		Gizmos.DrawLine(p3, p0);
	}

	public int GetOccupiedCellsCount()
	{
		int count = 0;
		if (cells == null) return 0;
		
		for (int y = 0; y < rows; y++)
		{
			for (int x = 0; x < columns; x++)
			{
				if (cells[x, y] != null && cells[x, y].occupied)
				{
					count++;
				}
			}
		}
		return count;
	}

	public bool IsFull()
	{
		return GetOccupiedCellsCount() >= (columns * rows);
	}

	public int GetTotalCells()
	{
		return columns * rows;
	}
}


