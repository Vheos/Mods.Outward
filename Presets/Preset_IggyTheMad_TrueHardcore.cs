using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;



namespace ModPack
{
    public class Preset_IggyTheMad_TrueHardcore : APreset
    {
        override public string Name
        => "IggyTheMad's TrueHardcore";
        override public int Ordering
        => 3;
        override public Type[] RequiredMods => new Type[]
        {
            typeof(Various),
        };
        override public void OverrideSettings()
        {
            Various._enableCheats.Value = true;
        }
    }
}
