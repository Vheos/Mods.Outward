using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections;



namespace ModPack
{
    static public class Utility
    {
        static public string CallerName
        => new StackFrame(2).GetMethod().Name;
        static public Type CallerType
        => new StackFrame(1).GetMethod().DeclaringType;
        static public IEnumerable<Type> GetDerivedTypes<T>()
        => Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(T)));
        static public string AssemblyName
        => Assembly.GetCallingAssembly().GetName().Name;
        static public string PluginFolderPath
        => @"BepInEx\plugins\Vheos\";
        static public void PrintStack(bool skipCurrent = true)
        {
            // initialize
            StackFrame[] stackFrames = new StackTrace(skipCurrent ? 2 : 1).GetFrames();
            string text = "Stack:\n";

            // find longest type
            int longestType = 0;
            foreach (var frame in stackFrames)
            {
                MethodBase method = frame.GetMethod();
                int typeLength = method.DeclaringType.ToString().Length + (method.IsStatic ? 7 : 0);
                if (typeLength > longestType)
                    longestType = typeLength;
            }

            // append methods
            foreach (var frame in stackFrames)
            {
                MethodBase method = frame.GetMethod();
                string typeText = (method.IsStatic ? "static " : "");
                typeText += method.DeclaringType;
                typeText = typeText.PadLeft(longestType, ' ');
                text += typeText + "." + method.Name + "\n";
            }

            // print
            Tools.Log(text);
        }
        static public Sprite CreateSpriteFromFile(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                byte[] byteData = System.IO.File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(0, 0);
                ImageConversion.LoadImage(texture, byteData);
                Rect textureRect = new Rect(0, 0, texture.width, texture.height);
                Sprite newSprite = Sprite.Create(texture, textureRect, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
                return newSprite;
            }
            return null;
        }
        static public void Swap<T>(ref T t, ref T a)
        {
            T temp = t;
            t = a;
            a = temp;
        }
        static public T[] CreateArray<T>(int count, T value)
        {
            T[] array = new T[count];
            for (int i = 0; i < count; i++)
                array[i] = value;
            return array;
        }
        static public BindingFlags AllBindingFlags
        => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        static public void AddComponentsFromHierarchy<T>(List<T> components, Transform hierarchyRoot) where T : Component
        {
            foreach (Transform child in hierarchyRoot)
            {
                components.Add(child.GetComponents<T>());
                AddComponentsFromHierarchy<T>(components, child);
            }
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
    }
}