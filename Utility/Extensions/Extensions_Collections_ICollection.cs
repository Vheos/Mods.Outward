using UnityEngine;
using System.Collections.Generic;



namespace ModPack
{
    static public class Extensions_Collections_ICollection
    {
        /// <summary> Adds given elements to this collection. </summary>
        static public void Add<T>(this ICollection<T> t, ICollection<T> elements)
        {
            foreach (var element in elements)
                t.Add(element);
        }
        /// <summary> Adds given elements to this collection. </summary>
        static public void Add<T>(this ICollection<T> t, params T[] elements)
        => t.Add((ICollection<T>)elements);
        /// <summary> Removes given elements from this collection. </summary>
        static public void Remove<T>(this ICollection<T> t, ICollection<T> elements)
        {
            List<T> toRemoves = new List<T>();
            foreach (var element in elements)
                if (t.Contains(element))
                    toRemoves.Add(element);
            foreach (var toRemove in toRemoves)
                t.Remove(toRemove);
        }
        /// <summary> Removes given elements from this collection. </summary>
        static public void Remove<T>(this ICollection<T> t, params T[] elements)
        => t.Remove(elements);

        /// <summary> Adds given element if this collection doesn't contain it. Returns whether the collection was modified or not. </summary>
        static public bool TryAddUnique<T>(this ICollection<T> t, T element)
        {
            if (!t.Contains(element))
            {
                t.Add(element);
                return true;
            }
            return false;
        }
        /// <summary> Removes given element if this collection doesn't contain it. Returns whether the collection was modified or not. </summary>
        static public bool TryRemove<T>(this ICollection<T> t, T element)
        {
            if (t.Contains(element))
            {
                t.Remove(element);
                return true;
            }
            return false;
        }
        /// <summary> Tests whether this collection contains any element. </summary>
        static public bool IsEmpty<T>(this ICollection<T> t)
        => t.Count == 0;
        /// <summary> Tests whether this collection contains zero elements. </summary>
        static public bool IsNotEmpty<T>(this ICollection<T> t)
        => t.Count != 0;
        /// <summary> Tests whether this object is contained within the given collection. </summary>
        static public bool IsContainedIn<T>(this T t, ICollection<T> collection)
        => collection.Contains(t);
        /// <summary> Tests whether this object is not contained within the given collection. </summary>
        static public bool IsNotContainedIn<T>(this T t, ICollection<T> collection)
        => !collection.Contains(t);
    }
}