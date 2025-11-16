public static class DragSortingUtility
{
    private static int globalSortingOrder = 0;

    public static int GetNextSortingOrder()
    {
        globalSortingOrder++;
        return globalSortingOrder;
    }
}



