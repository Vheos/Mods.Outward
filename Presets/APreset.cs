using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;



namespace ModPack
{
    abstract public class APreset
    {
        abstract public string Name { get; }
        abstract public int Ordering { get; }
        abstract public Type[] RequiredMods { get; }
        abstract public void OverrideSettings();
    }
}
