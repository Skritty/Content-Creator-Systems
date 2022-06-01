using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class CollectionExtensions
{
    public static T[] Add<T>(this T[] array, T item)
    {
        System.Array.Resize(ref array, array.Length + 1);
        array[array.Length - 1] = item;
        return array;
    }

    public static T[] Remove<T>(this T[] array, T item)
    {
        List<T> list = array == null ? new List<T>() : array.ToList();
        list.Remove(item);
        array = list.ToArray();
        return array;
    }
}
