using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using BepInEx;
using BepInEx.Logging;
using System.Diagnostics;



namespace ModPack
{
    static public class Presets
    {
        #region enum
        public enum Preset
        {
            None = 0,
            ResetToDefaults = 1,
            Vheos_CoopSurvival = 2,
            Vheos_PreferredUI = 3,
            IggyTheMad_TrueHardcore = 11,
        }
        #endregion

        // Privates
        static private List<AMod> _allMods;
        static private ModSetting<string> _presetToLoad;
        static private void CreateSetting()
        {
            List<string> names = new List<string>();
            foreach (var preset in Utility.GetEnumValues<Preset>())
                names.Add(PresetToName(preset));

            _presetToLoad = new ModSetting<string>("", nameof(_presetToLoad), PresetToName(Preset.None), new AcceptableValueList<string>(names.ToArray()));
            _presetToLoad.Format("Load preset");
            _presetToLoad.IsAdvanced = true;
            _presetToLoad.DisplayResetButton = false;

            _presetToLoad.AddEvent(LoadChosenPreset);
        }
        static private void LoadChosenPreset()
        {
            Preset preset = NameToPreset(_presetToLoad);
            if (preset == Preset.ResetToDefaults)
                foreach (var mod in _allMods)
                    mod.ResetSettings(true);
            else
                foreach (var mod in _allMods)
                    mod.LoadPreset(preset);

            _presetToLoad.SetSilently(PresetToName(Preset.None));
        }
        static private string PresetToName(Preset preset)
        {
            switch (preset)
            {
                case Preset.None: return "-";
                case Preset.ResetToDefaults: return "Reset to defaults";
                case Preset.Vheos_CoopSurvival: return "Vheos's Co-op Survival";
                case Preset.Vheos_PreferredUI: return "Vheos's Preferred UI";
                case Preset.IggyTheMad_TrueHardcore: return "IggyTheMad's True Hardcore";
                default: return null;
            }
        }
        static private Preset NameToPreset(string name)
        {
            foreach (var preset in Utility.GetEnumValues<Preset>())
                if (name == PresetToName(preset))
                    return preset;
            return Preset.None;
        }

        // Initializers
        static public void Initialize(List<AMod> allMods)
        {
            _allMods = allMods;
            CreateSetting();
        }
    }
}