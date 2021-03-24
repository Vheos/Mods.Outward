using UnityEngine;
using System.Collections.Generic;



namespace ModPack
{
    static public class Extensions_Collections_IList
    {
        /// <summary> Returns the first element from this list. </summary>
        static public T First<T>(this IList<T> t)
        => t[0];
        /// <summary> Returns the last element from this list. </summary>
        static public T Last<T>(this IList<T> t)
        => t[t.Count - 1];
        /// <summary> Returns the first non-null element from this list. </summary>
        static public T FirstNonNull<T>(this IList<T> t)
        {
            for (int i = 0; i < t.Count; i++)
                if (t[i] != null)
                    return t[i];
            return default;
        }
        /// <summary> Returns the last non-null element from this list. </summary>
        static public T LastNonNull<T>(this IList<T> t)
        {
            for (int i = t.Count - 1; i >= 0; i--)
                if (t[i] != null)
                    return t[i];
            return default;
        }
        /// <summary> Sets the first element from this list to the given object. </summary>
        static public T SetFirst<T>(this IList<T> t, T element)
        => t[0] = element;
        /// <summary> Sets the last element from this list to the given value. </summary>
        static public T SetLast<T>(this IList<T> t, T element)
        => t[t.Count - 1] = element;
        /// <summary> Removes the first element from this list. </summary>
        static public void RemoveFirst<T>(this IList<T> t)
        => t.RemoveAt(0);
        /// <summary> Removes the last element from this list. </summary>
        static public void RemoveLast<T>(this IList<T> t)
        => t.RemoveAt(t.Count - 1);
        /// <summary> Inserts the given element at the beginning of this list. </summary>
        static public void InsertFirst<T>(this IList<T> t, T element)
        => t.Insert(0, element);
        /// <summary> Inserts the given element at the end of this list. </summary>
        static public void InsertLast<T>(this IList<T> t, T element)
        => t.Insert(t.Count - 1, element);

        /// <summary> Tests whether given index is within this list's bounds. </summary>
        static public bool IsIndexValid<T>(this IList<T> t, int index)
        => index >= 0 && index < t.Count;
        /// <summary> If the given index is invalid, returns default value for this list's type. </summary>
        static public T DefaultOnInvalid<T>(this IList<T> t, int index)
        => t.IsIndexValid(index) ? t[index] : default;
    }
}
