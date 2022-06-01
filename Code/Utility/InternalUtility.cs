namespace Vheos.Mods.Outward;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

internal static class InternalUtility
{
    public static Type CallerType
    => new StackFrame(1).GetMethod().DeclaringType;
    public static string AssemblyName
    => Assembly.GetCallingAssembly().GetName().Name;
    public static string PluginFolderPath
    => @"BepInEx\plugins\Vheos\";
    public static Sprite CreateSpriteFromFile(string filePath)
    {
        if (System.IO.File.Exists(filePath))
        {
            byte[] byteData = System.IO.File.ReadAllBytes(filePath);
            Texture2D texture = new(0, 0, TextureFormat.RGBA32, false);
            texture.LoadImage(byteData, true);
            Rect textureRect = new(0, 0, texture.width, texture.height);
            Sprite newSprite = Sprite.Create(texture, textureRect, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
            return newSprite;
        }
        return null;
    }
    public static T[] CreateArray<T>(int count, T value)
    {
        T[] array = new T[count];
        for (int i = 0; i < count; i++)
            array[i] = value;
        return array;
    }
    public static List<List<T>> CreateList2D<T>(int count)
    {
        List<List<T>> list2D = new();
        for (int i = 0; i < count; i++)
            list2D.Add(new List<T>());
        return list2D;
    }
    public static T[] GetEnumValues<T>()
    => (T[])Enum.GetValues(typeof(T));
    public static BindingFlags AllBindingFlags
    => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    public static void AppendChildrenRecurisvely<T>(Transform root, List<T> components) where T : Component
    {
        foreach (Transform child in root)
        {
            components.Add(child.GetComponents<T>());
            AppendChildrenRecurisvely(child, components);
        }
    }
    public static void AppendChildrenRecurisvely<T1, T2>(Transform root, List<Component> components) where T1 : Component where T2 : Component
    {
        foreach (Transform child in root)
        {
            components.Add(child.GetComponents<T1>());
            components.Add(child.GetComponents<T2>());
            AppendChildrenRecurisvely(child, components);
        }
    }
    public static void SwapHierarchyPositions<T>(T t, T a) where T : Component
    {
        Transform temp = new GameObject().transform;
        temp.BecomeSiblingOf(t);
        t.BecomeSiblingOf(a);
        a.BecomeSiblingOf(temp);
        temp.DestroyObject();
    }
    public static Color Lerp3(Color a, Color b, Color c, float t)
    => t < 0.5f
       ? Color.Lerp(a, b, t * 2)
       : Color.Lerp(b, c, t * 2 - 1);
    public static float Lerp3(float a, float b, float c, float t)
    => t < 0.5f
       ? Mathf.Lerp(a, b, t * 2)
       : Mathf.Lerp(b, c, t * 2 - 1);
    public static IEnumerator CoroutineWaitUntilEndOfFrame(Action action)
    {
        yield return new WaitForEndOfFrame();
        action();
    }
    public static IEnumerator CoroutineWaitForSeconds(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action();
    }
    public static IEnumerator CoroutineWaitUntil(Func<bool> test, Action action)
    {
        yield return new WaitUntil(test);
        action();
    }
    public static IEnumerator CoroutineWhile(Func<bool> test, Action action, Action finalAction = null)
    {
        while (test())
        {
            action();
            yield return null;
        }
        finalAction?.Invoke();
    }
    public static IEnumerator CoroutineDoUntil(Func<bool> test, Action action, Action finalAction = null)
    {
        do
        {
            action();
            yield return null;
        }
        while (!test());
        finalAction?.Invoke();

    }
    public static List<T> Intersect<T>(IEnumerable<IEnumerable<T>> lists)
    {
        if (lists == null || !lists.Any())
            return new List<T>();

        HashSet<T> hashSet = new(lists.First());
        foreach (var list in lists.Skip(1))
            hashSet.IntersectWith(list);
        return hashSet.ToList();
    }
    public static float GameTime
    {
        get => (float)EnvironmentConditions.GameTime;
        set => EnvironmentConditions.GameTime = value;
    }
}
