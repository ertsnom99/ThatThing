public static class FastAlgorithms
{
    // Algorithm from https://www.tutorialspoint.com/chash-program-to-perform-quick-sort-using-recursion with small modifications to
    // keep arrays aligned and handle case where two value are the same
    // O(n * log(n))
    public static void QuickSortAlignedArrays(float[] toSort, int[] aligned, int left, int right)
    {
        int pivot;

        if (left < right)
        {
            pivot = PartitionAlignedArrays(toSort, aligned, left, right);

            if (pivot > 1)
            {
                QuickSortAlignedArrays(toSort, aligned, left, pivot - 1);
            }

            if (pivot + 1 < right)
            {
                QuickSortAlignedArrays(toSort, aligned, pivot + 1, right);
            }
        }
    }

    private static int PartitionAlignedArrays(float[] toSort, int[] aligned, int left, int right)
    {
        float pivot;
        pivot = toSort[left];

        while (true)
        {
            while (toSort[left] < pivot)
            {
                left++;
            }

            while (toSort[right] > pivot)
            {
                right--;
            }

            if (left < right)
            {
                if (toSort[left] == toSort[right])
                {
                    right--;
                }

                float temp = toSort[right];
                toSort[right] = toSort[left];
                toSort[left] = temp;

                temp = aligned[right];
                aligned[right] = aligned[left];
                aligned[left] = (int)temp;
            }
            else
            {
                return right;
            }
        }
    }

    // Algorithm from https://en.wikipedia.org/wiki/Binary_search_algorithm
    // O(log(n))
    public static int BinarySearch(int[] path, int x)
    {
        int l = 0;
        int r = path.Length - 1;

        while (l <= r)
        {
            int mid = l + (r - l) / 2;

            if (path[mid] == x)
            {
                return mid;
            }

            if (path[mid] > x)
            {
                // element is in left subarray
                r = mid - 1;
            }
            else
            {
                // element is in right subarray
                l = mid + 1;
            }
        }

        return -1;
    }

    public static int BinarySearch(float[] path, float x)
    {
        int l = 0;
        int r = path.Length - 1;

        while (l <= r)
        {
            int mid = l + (r - l) / 2;

            if (path[mid] == x)
            {
                return mid;
            }

            if (path[mid] > x)
            {
                // element is in left subarray
                r = mid - 1;
            }
            else
            {
                // element is in right subarray
                l = mid + 1;
            }
        }

        return -1;
    }
}
