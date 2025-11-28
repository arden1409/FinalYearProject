using System.Collections.Generic;
using UnityEngine;

public class UndoRedoManager : MonoBehaviour
{
    [System.Serializable]
    public class ItemState
    {
        public DraggableItem item;
        public FreeDraggableItem freeItem;
        public Vector3 position;
        public Transform parent;
        public int siblingIndex;
        public GridCell previousCell;
        public GridCell currentCell;

        public ItemState(DraggableItem item, Vector3 pos, Transform parent, int siblingIndex, GridCell prevCell, GridCell currCell)
        {
            this.item = item;
            this.freeItem = null;
            this.position = pos;
            this.parent = parent;
            this.siblingIndex = siblingIndex;
            this.previousCell = prevCell;
            this.currentCell = currCell;
        }

        public ItemState(FreeDraggableItem freeItem, Vector3 pos, Transform parent, int siblingIndex)
        {
            this.item = null;
            this.freeItem = freeItem;
            this.position = pos;
            this.parent = parent;
            this.siblingIndex = siblingIndex;
            this.previousCell = null;
            this.currentCell = null;
        }

        public MonoBehaviour GetItem()
        {
            if (item != null) return item;
            if (freeItem != null) return freeItem;
            return null;
        }
    }

    [System.Serializable]
    public class ActionRecord
    {
        public List<ItemState> beforeStates = new List<ItemState>();
        public List<ItemState> afterStates = new List<ItemState>();

        public ActionRecord(List<ItemState> before, List<ItemState> after)
        {
            beforeStates = new List<ItemState>(before);
            afterStates = new List<ItemState>(after);
        }
    }

    private Stack<ActionRecord> undoStack = new Stack<ActionRecord>();
    private Stack<ActionRecord> redoStack = new Stack<ActionRecord>();

    private List<DraggableItem> trackedItems = new List<DraggableItem>();

    public static UndoRedoManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterItem(DraggableItem item)
    {
        if (!trackedItems.Contains(item))
        {
            trackedItems.Add(item);
        }
    }

    public void RecordActionBefore(DraggableItem item)
    {
        if (item == null) return;

        GridCell currentCell = null;
        if (item.transform.parent != null)
        {
            currentCell = item.transform.parent.GetComponent<GridCell>();
        }

        ItemState beforeState = new ItemState(
            item,
            item.transform.position,
            item.transform.parent,
            item.transform.GetSiblingIndex(),
            null,
            currentCell
        );
        
        ActionRecord currentRecord = null;
        if (undoStack.Count > 0)
        {
            var peek = undoStack.Peek();
            if (peek.afterStates.Count == 0)
            {
                currentRecord = peek;
            }
        }

        if (currentRecord == null)
        {
            currentRecord = new ActionRecord(new List<ItemState>(), new List<ItemState>());
            undoStack.Push(currentRecord);
        }

        bool exists = false;
        for (int i = 0; i < currentRecord.beforeStates.Count; i++)
        {
            if (currentRecord.beforeStates[i].item == item)
            {
                currentRecord.beforeStates[i] = beforeState;
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            currentRecord.beforeStates.Add(beforeState);
        }
    }

    public void RecordActionAfter(DraggableItem item)
    {
        if (undoStack.Count == 0 || item == null) return;

        ActionRecord currentRecord = undoStack.Peek();
        
        GridCell currentCell = null;
        if (item.transform.parent != null)
        {
            currentCell = item.transform.parent.GetComponent<GridCell>();
        }

        GridCell previousCell = null;
        foreach (var beforeState in currentRecord.beforeStates)
        {
            if (beforeState.item == item)
            {
                previousCell = beforeState.currentCell;
                break;
            }
        }

        ItemState afterState = new ItemState(
            item,
            item.transform.position,
            item.transform.parent,
            item.transform.GetSiblingIndex(),
            previousCell,
            currentCell
        );

        bool exists = false;
        for (int i = 0; i < currentRecord.afterStates.Count; i++)
        {
            if (currentRecord.afterStates[i].item == item)
            {
                currentRecord.afterStates[i] = afterState;
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            currentRecord.afterStates.Add(afterState);
        }

        redoStack.Clear();
    }


    public void Undo()
    {
        if (undoStack.Count == 0) return;

        ActionRecord record = undoStack.Pop();

        foreach (var beforeState in record.beforeStates)
        {
            MonoBehaviour itemObj = beforeState.GetItem();
            if (itemObj == null) continue;

            ItemState afterState = null;
            foreach (var after in record.afterStates)
            {
                if ((beforeState.item != null && after.item == beforeState.item) ||
                    (beforeState.freeItem != null && after.freeItem == beforeState.freeItem))
                {
                    afterState = after;
                    break;
                }
            }

            if (afterState != null && afterState.currentCell != null && beforeState.item != null)
            {
                afterState.currentCell.SetOccupied(null);
            }

            itemObj.transform.SetParent(beforeState.parent, true);
            itemObj.transform.position = beforeState.position;
            itemObj.transform.SetSiblingIndex(beforeState.siblingIndex);

            if (beforeState.currentCell != null && beforeState.item != null)
            {
                beforeState.currentCell.SetOccupied(beforeState.item);
            }
        }

        redoStack.Push(record);
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;

        ActionRecord record = redoStack.Pop();

        foreach (var afterState in record.afterStates)
        {
            MonoBehaviour itemObj = afterState.GetItem();
            if (itemObj == null) continue;

            ItemState beforeState = null;
            foreach (var before in record.beforeStates)
            {
                if ((afterState.item != null && before.item == afterState.item) ||
                    (afterState.freeItem != null && before.freeItem == afterState.freeItem))
                {
                    beforeState = before;
                    break;
                }
            }

            if (beforeState != null && beforeState.currentCell != null && afterState.item != null)
            {
                beforeState.currentCell.SetOccupied(null);
            }

            itemObj.transform.SetParent(afterState.parent, true);
            itemObj.transform.position = afterState.position;
            itemObj.transform.SetSiblingIndex(afterState.siblingIndex);

            if (afterState.currentCell != null && afterState.item != null)
            {
                afterState.currentCell.SetOccupied(afterState.item);
            }
        }

        undoStack.Push(record);
    }

    public bool CanUndo()
    {
        return undoStack.Count > 0;
    }

    public bool CanRedo()
    {
        return redoStack.Count > 0;
    }

    public void ClearHistory()
    {
        undoStack.Clear();
        redoStack.Clear();
    }

    public void RecordActionBefore(FreeDraggableItem item)
    {
        if (item == null) return;

        ItemState beforeState = new ItemState(
            item,
            item.transform.position,
            item.transform.parent,
            item.transform.GetSiblingIndex()
        );
        
        ActionRecord currentRecord = null;
        if (undoStack.Count > 0)
        {
            var peek = undoStack.Peek();
            if (peek.afterStates.Count == 0)
            {
                currentRecord = peek;
            }
        }

        if (currentRecord == null)
        {
            currentRecord = new ActionRecord(new List<ItemState>(), new List<ItemState>());
            undoStack.Push(currentRecord);
        }

        bool exists = false;
        for (int i = 0; i < currentRecord.beforeStates.Count; i++)
        {
            if (currentRecord.beforeStates[i].freeItem == item)
            {
                currentRecord.beforeStates[i] = beforeState;
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            currentRecord.beforeStates.Add(beforeState);
        }
    }

    public void RecordActionAfter(FreeDraggableItem item)
    {
        if (undoStack.Count == 0 || item == null) return;

        ActionRecord currentRecord = undoStack.Peek();

        ItemState afterState = new ItemState(
            item,
            item.transform.position,
            item.transform.parent,
            item.transform.GetSiblingIndex()
        );

        bool exists = false;
        for (int i = 0; i < currentRecord.afterStates.Count; i++)
        {
            if (currentRecord.afterStates[i].freeItem == item)
            {
                currentRecord.afterStates[i] = afterState;
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            currentRecord.afterStates.Add(afterState);
        }

        redoStack.Clear();
    }
}

