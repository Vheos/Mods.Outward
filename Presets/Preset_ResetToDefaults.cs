namespace ModPack
{
    public class Preset_ResetToDefaults: APreset
    {
        override public string Name
        => "Reset to defaults";
        override public int Ordering
        => 1;
        override public System.Type[] RequiredMods => new System.Type[]
        {
        };
        override public void OverrideSettings()
        {
            Presets.ResetToDefaults();
        }
    }
}
