using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;



namespace ModPack
{
    public class Preset_ResetToDefaults : APreset
    {
        override public string Name
        => "Reset to defaults";
        override public int Ordering
        => 1;
        override public Type[] RequiredMods => new Type[]
        {
        };
        override public void OverrideSettings()
        {
            Presets.ResetToDefaults();
        }
    }
}
