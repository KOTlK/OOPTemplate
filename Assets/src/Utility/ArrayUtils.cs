using static Assertions;

public static class ArrayUtils {
    public static void Resize<T>(ref T[] arr, uint newSize) {
        Assert(arr.Length < newSize);
        var newArr = new T[newSize];
        for(var i = 0; i < arr.Length; ++i) {
            newArr[i] = arr[i];
        }

        arr = newArr;
    }
}