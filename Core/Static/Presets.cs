using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System;
using System.Linq;
using System.Collections.Generic;



namespace ModPack
{
    static public class Presets
    {
        #region const
        public const string SECTION_SURVIVAL = "    \nSURVIVAL & IMMERSION";
        public const string SECTION_COMBAT = "   \nCOMBAT";
        public const string SECTION_UI = "  \nUSER INTERFACE";
        public const string SECTION_VARIOUS = " \nVARIOUS";
        private const string DEFAULT_SETTING_TEXT = "(select a preset to load)";
        #endregion
        #region ordering
        static public readonly Type[] MODS_ORDERING = new[]
        {
            // Survival & Immersion
            typeof(Needs),
            typeof(Camping),
            typeof(SkillLimits),
            typeof(Prices),
            typeof(Resets),
            typeof(Interactions),
            typeof(Revive),

            // Combat
            typeof(Damage),
            typeof(Speed),
            typeof(Targeting),
            typeof(Traps),

            // UI
            typeof(GUI),
            typeof(Descriptions),
            typeof(Camera),
            typeof(KeyboardWalk),
            typeof(Gamepad),

            // Various
            typeof(Various),
            typeof(PistolTweaks),
            typeof(Debug),
        };
        #endregion

        // Publics
        static public void RegisterMod(AMod mod)
        {
            Type type = mod.GetType();
            if (!_modsByType.ContainsKey(type))
                _modsByType.Add(type, mod);
        }
        static public void ResetToDefaults()
        {
            foreach (var modByType in _modsByType)
                modByType.Value.Reset();
        }

        // Utility
        static private ModSetting<string> _preset;
        static private Dictionary<string, APreset> _presetsByName;
        static private Dictionary<Type, AMod> _modsByType;
        static private void LoadPreset(string presetName)
        {
            APreset preset = _presetsByName[presetName];
            foreach (var modType in preset.RequiredMods)
                _modsByType[modType].EnableAndCollapse();

            _presetsByName[presetName].OverrideSettings();
            _preset.SetSilently(DEFAULT_SETTING_TEXT);
        }

        // Initializers
        static public void Initialize()
        {
            _presetsByName = new Dictionary<string, APreset>();
            _modsByType = new Dictionary<Type, AMod>();

            InstantiatePresets();
            CreateSetting();
        }
        static private void InstantiatePresets()
        {
            foreach (var presetType in Utility.GetDerivedTypes<APreset>())
            {
                APreset newPreset = (APreset)Activator.CreateInstance(presetType);
                _presetsByName.Add(newPreset.Name, newPreset);
            }
        }
        static private void CreateSetting()
        {
            _preset = new ModSetting<string>("", nameof(_preset), "", new AcceptableValueList<string>(_presetsByName.Keys.ToArray()))
            {
                SectionOverride = SECTION_VARIOUS,
                DisplayResetButton = false,
                IsAdvanced = true,
            };

            _preset.Format("Load preset");
            _preset.Ordering = int.MaxValue;

            _preset.SetSilently(DEFAULT_SETTING_TEXT);
            _preset.AddEvent(() => LoadPreset(_preset));
        }
    }
}