namespace ModPack
{
    public class Preset_Vheos_Survival : APreset
    {
        override public string Name
        => "Vheos's Survival";
        override public int Ordering
        => 2;
        override public System.Type[] RequiredMods => new System.Type[]
        {
            typeof(Various),
        };
        override public void OverrideSettings()
        {
            Various._enableCheats.Value = false;
        }
    }
}
