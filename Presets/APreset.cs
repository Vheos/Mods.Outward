namespace ModPack
{
    abstract public class APreset
    {
        abstract public string Name { get; }
        abstract public int Ordering { get; }
        abstract public System.Type[] RequiredMods { get; }
        abstract public void OverrideSettings();
    }
}
