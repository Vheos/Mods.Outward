namespace Vheos.Mods.Outward;
internal static class Utils
{
    public static IEnumerable<TemperatureSteps> TemperatureSteps
    => Utility.GetEnumValues<TemperatureSteps>().Reverse().Skip(1);
    public static IEnumerable<Character.Factions> Factions
    => Utility.GetEnumValues<Character.Factions>().Skip(1).Reverse().Skip(1);

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

    public static float GameTime
    {
        get => (float)EnvironmentConditions.GameTime;
        set => EnvironmentConditions.GameTime = value;
    }
    public static AreaManager.AreaEnum CurrentArea
    => (AreaManager.AreaEnum)(AreaManager.Instance.CurrentArea.TryNonNull(out var area) ? area.ID : -1);
    public static bool IsInCity
    => CurrentArea.IsContainedIn(Lists.CITIES);
    public static bool IsInOpenRegion
    => CurrentArea.IsContainedIn(Lists.OPEN_REGIONS);
}
