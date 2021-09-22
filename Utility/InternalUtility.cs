namespace Vheos.Mods.Outward
{
    using System;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Collections;
    using UnityEngine;
    using Tools.Extensions.UnityObjects;
    using Tools.Extensions.Collections;
    static internal class InternalUtility
    {
        static public Type CallerType
        => new StackFrame(1).GetMethod().DeclaringType;
        static public string AssemblyName
        => Assembly.GetCallingAssembly().GetName().Name;
        static public string PluginFolderPath
        => @"BepInEx\plugins\Vheos\";
        static public Sprite CreateSpriteFromFile(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                byte[] byteData = System.IO.File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(0, 0, TextureFormat.RGBA32, false);
                texture.LoadImage(byteData, true);
                Rect textureRect = new Rect(0, 0, texture.width, texture.height);
                Sprite newSprite = Sprite.Create(texture, textureRect, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
                return newSprite;
            }
            return null;
        }
        static public T[] CreateArray<T>(int count, T value)
        {
            T[] array = new T[count];
            for (int i = 0; i < count; i++)
                array[i] = value;
            return array;
        }
        static public List<List<T>> CreateList2D<T>(int count)
        {
            List<List<T>> list2D = new List<List<T>>();
            for (int i = 0; i < count; i++)
                list2D.Add(new List<T>());
            return list2D;
        }
        static public T[] GetEnumValues<T>()
        => (T[])Enum.GetValues(typeof(T));
        static public BindingFlags AllBindingFlags
        => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        static public void AppendChildrenRecurisvely<T>(Transform root, List<T> components) where T : Component
        {
            foreach (Transform child in root)
            {
                components.Add(child.GetComponents<T>());
                AppendChildrenRecurisvely(child, components);
            }
        }
        static public void AppendChildrenRecurisvely<T1, T2>(Transform root, List<Component> components) where T1 : Component where T2 : Component
        {
            foreach (Transform child in root)
            {
                components.Add(child.GetComponents<T1>());
                components.Add(child.GetComponents<T2>());
                AppendChildrenRecurisvely(child, components);
            }
        }
        static public void SwapHierarchyPositions<T>(T t, T a) where T : Component
        {
            Transform temp = new GameObject().transform;
            temp.BecomeSiblingOf(t);
            t.BecomeSiblingOf(a);
            a.BecomeSiblingOf(temp);
            temp.DestroyObject();
        }
        static public Color Lerp3(Color a, Color b, Color c, float t)
        => t < 0.5f
           ? Color.Lerp(a, b, t * 2)
           : Color.Lerp(b, c, t * 2 - 1);
        static public float Lerp3(float a, float b, float c, float t)
        => t < 0.5f
           ? Mathf.Lerp(a, b, t * 2)
           : Mathf.Lerp(b, c, t * 2 - 1);
        static public IEnumerator CoroutineWaitUntilEndOfFrame(Action action)
        {
            yield return new WaitForEndOfFrame();
            action();
        }
        static public IEnumerator CoroutineWaitForSeconds(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action();
        }
        static public IEnumerator CoroutineWaitUntil(Func<bool> test, Action action)
        {
            yield return new WaitUntil(test);
            action();
        }
        static public IEnumerator CoroutineWhile(Func<bool> test, Action action, Action finalAction = null)
        {
            while (test())
            {
                action();
                yield return null;
            }
            finalAction?.Invoke();
        }
        static public IEnumerator CoroutineDoUntil(Func<bool> test, Action action, Action finalAction = null)
        {
            do
            {
                action();
                yield return null;
            }
            while (!test());
            finalAction?.Invoke();

        }
        static public List<T> Intersect<T>(IEnumerable<IEnumerable<T>> lists)
        {
            if (lists == null || !lists.Any())
                return new List<T>();

            HashSet<T> hashSet = new HashSet<T>(lists.First());
            foreach (var list in lists.Skip(1))
                hashSet.IntersectWith(list);
            return hashSet.ToList();
        }
        static public float GameTime
        {
            get => (float)EnvironmentConditions.GameTime;
            set => EnvironmentConditions.GameTime = value;
        }
    }
}