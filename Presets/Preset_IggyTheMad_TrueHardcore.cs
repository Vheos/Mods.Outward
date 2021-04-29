namespace ModPack
{
    public class Preset_IggyTheMad_TrueHardcore : APreset
    {
        override public string Name
        => "IggyTheMad's TrueHardcore";
        override public int Ordering
        => 3;
        override public System.Type[] RequiredMods => new System.Type[]
        {
            typeof(Various),
        };
        override public void OverrideSettings()
        {
            Various._enableCheats.Value = true;
        }
    }
}
