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

        // Lưu trạng thái trước khi di chuyển
        // Tìm cell hiện tại (trước khi di chuyển)
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
            null, // previousCell sẽ được set sau
            currentCell // currentCell là cell hiện tại trước khi di chuyển
        );
        
        // Tìm action record hiện tại hoặc tạo mới
        ActionRecord currentRecord = null;
        if (undoStack.Count > 0)
        {
            var peek = undoStack.Peek();
            // Nếu action record mới nhất chưa có afterStates, đó là action đang diễn ra
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

        // Kiểm tra xem item này đã có trong beforeStates chưa
        bool exists = false;
        for (int i = 0; i < currentRecord.beforeStates.Count; i++)
        {
            if (currentRecord.beforeStates[i].item == item)
            {
                // Cập nhật state hiện có
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
        
        // Tìm cell hiện tại (sau khi di chuyển)
        GridCell currentCell = null;
        if (item.transform.parent != null)
        {
            currentCell = item.transform.parent.GetComponent<GridCell>();
        }

        // Tìm previousCell từ beforeState của cùng item (chỉ cho DraggableItem)
        GridCell previousCell = null;
        foreach (var beforeState in currentRecord.beforeStates)
        {
            if (beforeState.item == item)
            {
                previousCell = beforeState.currentCell; // currentCell trong beforeState là cell trước đó
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

        // Kiểm tra xem item này đã có trong afterStates chưa
        bool exists = false;
        for (int i = 0; i < currentRecord.afterStates.Count; i++)
        {
            if (currentRecord.afterStates[i].item == item)
            {
                // Cập nhật state hiện có
                currentRecord.afterStates[i] = afterState;
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            currentRecord.afterStates.Add(afterState);
        }

        // Xóa redo stack khi có action mới
        redoStack.Clear();
    }


    public void Undo()
    {
        if (undoStack.Count == 0) return;

        ActionRecord record = undoStack.Pop();

        // Áp dụng trạng thái before cho tất cả items
        foreach (var beforeState in record.beforeStates)
        {
            MonoBehaviour itemObj = beforeState.GetItem();
            if (itemObj == null) continue;

            // Tìm afterState tương ứng để lấy cell hiện tại cần giải phóng
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

            // Giải phóng cell hiện tại (từ afterState) nếu có (chỉ cho DraggableItem)
            if (afterState != null && afterState.currentCell != null && beforeState.item != null)
            {
                afterState.currentCell.SetOccupied(null);
            }

            // Khôi phục vị trí và parent từ beforeState
            itemObj.transform.SetParent(beforeState.parent, true);
            itemObj.transform.position = beforeState.position;
            itemObj.transform.SetSiblingIndex(beforeState.siblingIndex);

            // Đặt lại cell từ beforeState (cell trước khi di chuyển) - chỉ cho DraggableItem
            if (beforeState.currentCell != null && beforeState.item != null)
            {
                beforeState.currentCell.SetOccupied(beforeState.item);
            }
        }

        // Đưa vào redo stack
        redoStack.Push(record);
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;

        ActionRecord record = redoStack.Pop();

        // Áp dụng trạng thái after cho tất cả items
        foreach (var afterState in record.afterStates)
        {
            MonoBehaviour itemObj = afterState.GetItem();
            if (itemObj == null) continue;

            // Tìm beforeState tương ứng để lấy cell hiện tại cần giải phóng
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

            // Giải phóng cell hiện tại (từ beforeState) nếu có (chỉ cho DraggableItem)
            if (beforeState != null && beforeState.currentCell != null && afterState.item != null)
            {
                beforeState.currentCell.SetOccupied(null);
            }

            // Áp dụng vị trí và parent mới từ afterState
            itemObj.transform.SetParent(afterState.parent, true);
            itemObj.transform.position = afterState.position;
            itemObj.transform.SetSiblingIndex(afterState.siblingIndex);

            // Đặt lại cell mới từ afterState - chỉ cho DraggableItem
            if (afterState.currentCell != null && afterState.item != null)
            {
                afterState.currentCell.SetOccupied(afterState.item);
            }
        }

        // Đưa lại vào undo stack
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

    // Methods for FreeDraggableItem
    public void RecordActionBefore(FreeDraggableItem item)
    {
        if (item == null) return;

        ItemState beforeState = new ItemState(
            item,
            item.transform.position,
            item.transform.parent,
            item.transform.GetSiblingIndex()
        );
        
        // Tìm action record hiện tại hoặc tạo mới
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

        // Kiểm tra xem item này đã có trong beforeStates chưa
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

        // Kiểm tra xem item này đã có trong afterStates chưa
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

        // Xóa redo stack khi có action mới
        redoStack.Clear();
    }
}

