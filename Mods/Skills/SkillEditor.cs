namespace Vheos.Mods.Outward
{
    using System.Linq;
    using HarmonyLib;
    using UnityEngine;
    using Mods.Core;
    using Tools.Extensions.Math;
    using Tools.Extensions.Collections;
    public class SkillEditor : AMod, IDelayedInit
    {
        #region const
        private const string RUNIC_LANTERN_ID = "Runic Lantern";
        private const float RUNIC_LANTERN_EMISSION_RATE = 20;
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_DAGGER_SLASH =
        (
            new Vector3(0, 0, 0),
            new Vector3(0, 2, 0),
            new Vector3(0, 0, 0.5f)
        );
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_BACKSTAB =
        (
            new Vector3(1, 3, 0),
            new Vector3(0, 5, 0),
            new Vector3(0, 0, 15)
        );
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_EVASION_SHOT =
        (
            new Vector3(0, 0, 0),
            new Vector3(0, 12, 0),
            new Vector3(0, 0, 30)
        );
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_OPPORTUNIST_STAB =
        (
            new Vector3(0, 0, 0),
            new Vector3(0, 5, 0),
            new Vector3(0, 0, 10)
        );
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_SERPENTS_PARRY =
        (
            new Vector3(0, 0, 0),
            new Vector3(0, 7, 0),
            new Vector3(0, 0, 100)
        );
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_SNIPER_SHOT =
        (
            new Vector3(0, 0, 0),
            new Vector3(0, 15, 0),
            new Vector3(0, 0, 30)
        );
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_PIERCING_SHOT =
        (
            new Vector3(0, 0, 0),
            new Vector3(0, 15, 0),
            new Vector3(0, 0, 30)
        );
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_RUNE =
        (
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 8),
            new Vector3(0, 0, 2)
        );
        #endregion
        #region class
        private class SkillData
        {
            // Settings
            public ModSetting<bool> _toggle;
            public ModSetting<Vector3> _effects, _vitalCosts, _otherCosts;
            public void CreateSettings(string skillSettingName, Skill prefab, Vector3 effects, Vector3 vitalCosts, Vector3 otherCosts)
            {
                _toggle = _mod.CreateSetting(skillSettingName + nameof(_toggle), false);
                _effects = _mod.CreateSetting(skillSettingName + nameof(_effects), effects);
                _vitalCosts = _mod.CreateSetting(skillSettingName + nameof(_vitalCosts), vitalCosts);
                _otherCosts = _mod.CreateSetting(skillSettingName + nameof(_otherCosts), otherCosts);

                _effects.AddEvent(() => TryApplyEffectsToPrefab(prefab));
                _vitalCosts.AddEvent(() => TryApplyVitalCostsToPrefab(prefab));
                _otherCosts.AddEvent(() => TryApplyOtherCostsToPrefab(prefab));
            }
            public void FormatSettings(ModSetting<bool> toggle = null)
            {
                if (toggle != null)
                    _toggle.Format(_skillName, toggle);
                else
                    _toggle.Format(_skillName);

                using(Indent)
                {
                    // Effects description
                    string text = "";
                    if (_effectX.IsNotEmpty()) text += $"X   -   {_effectX}\n";
                    if (_effectY.IsNotEmpty()) text += $"Y   -   {_effectY}\n";
                    if (_effectZ.IsNotEmpty()) text += $"Z   -   {_effectZ}\n";
                    text = text.TrimEnd('\n');

                    // Format
                    if (text.IsNotEmpty())
                    {
                        _effects.Format("Effects", _toggle);
                        _effects.Description = text;
                    }
                    _vitalCosts.Format("Costs (vitals)", _toggle);
                    _vitalCosts.Description = "X   -   Health\n" +
                                              "Y   -   Stamina\n" +
                                              "Z   -   Mana";
                    _otherCosts.Format("Costs (other)", _toggle);
                    _otherCosts.Description = "X   -   Durability\n" +
                                              "Y   -   Durability %\n" +
                                              "Z   -   Cooldown";
                }
            }

            // Constructor
            public SkillData(SkillEditor mod, string skillSettingName, string skillName, (Vector3 Effects, Vector3 VitalCosts, Vector3 OtherCosts) defaultValues)
            {
                _mod = mod;
                _skillName = skillName;
                CreateSettings(skillSettingName, Prefabs.GetSkillByName(_skillName),
                               defaultValues.Effects, defaultValues.VitalCosts, defaultValues.OtherCosts);
            }

            // Utility
            private readonly SkillEditor _mod;
            private readonly string _skillName;
            private string _effectX, _effectY, _effectZ;
            private Action<Skill, float> _applyEffectX, _applyEffectY, _applyEffectZ;
            private void TryApplyEffectsToPrefab(Skill prefab)
            {
                if (!_toggle)
                    return;

                _applyEffectX?.Invoke(prefab, _effects.Value.x);
                _applyEffectY?.Invoke(prefab, _effects.Value.y);
                _applyEffectZ?.Invoke(prefab, _effects.Value.z);
            }
            private void TryApplyVitalCostsToPrefab(Skill prefab)
            {
                if (!_toggle)
                    return;

                prefab.HealthCost = _vitalCosts.Value.x;
                prefab.StaminaCost = _vitalCosts.Value.y;
                prefab.ManaCost = _vitalCosts.Value.z;
            }
            private void TryApplyOtherCostsToPrefab(Skill prefab)
            {
                if (!_toggle)
                    return;

                prefab.DurabilityCost = _otherCosts.Value.x;
                prefab.DurabilityCostPercent = _otherCosts.Value.y;
                prefab.Cooldown = _otherCosts.Value.z;
            }
            public void InitializeEffectX(string effectName, Action<Skill, float> applyLogic)
            {
                _effectX = effectName;
                _applyEffectX = applyLogic;
            }
            public void InitializeEffectY(string effectName, Action<Skill, float> applyLogic)
            {
                _effectY = effectName;
                _applyEffectY = applyLogic;
            }
            public void InitializeEffectZ(string effectName, Action<Skill, float> applyLogic)
            {
                _effectZ = effectName;
                _applyEffectZ = applyLogic;
            }
        }
        #endregion

        // Settings
        static private ModSetting<bool> _daggerToggle, _bowToggle, _runesToggle;
        static private SkillData _daggerSlash, _backstab, _opportunistStab, _serpentsParry;
        static private SkillData _evasionShot, _sniperShot, _piercingShot;
        static private SkillData _dez, _egoth, _fal, _shim;
        static private ModSetting<int> _runeSoundEffectVolume, _runicLanternIntensity;
        override protected void Initialize()
        {
            _daggerToggle = CreateSetting(nameof(_daggerToggle), false);
            _daggerSlash = new SkillData(this, nameof(_daggerSlash), "Dagger Slash", DEFAULT_VALUES_DAGGER_SLASH);
            _backstab = new SkillData(this, nameof(_backstab), "Backstab", DEFAULT_VALUES_BACKSTAB);
            _opportunistStab = new SkillData(this, nameof(_opportunistStab), "Opportunist Stab", DEFAULT_VALUES_OPPORTUNIST_STAB);
            _serpentsParry = new SkillData(this, nameof(_serpentsParry), "Serpent's Parry", DEFAULT_VALUES_SERPENTS_PARRY);

            _bowToggle = CreateSetting(nameof(_bowToggle), false);
            _evasionShot = new SkillData(this, nameof(_evasionShot), "Evasion Shot", DEFAULT_VALUES_EVASION_SHOT);
            _sniperShot = new SkillData(this, nameof(_sniperShot), "Sniper Shot", DEFAULT_VALUES_SNIPER_SHOT);
            _piercingShot = new SkillData(this, nameof(_piercingShot), "Piercing Shot", DEFAULT_VALUES_PIERCING_SHOT);

            _runesToggle = CreateSetting(nameof(_runesToggle), false);
            _dez = new SkillData(this, nameof(_dez), "Dez", DEFAULT_VALUES_RUNE);
            _egoth = new SkillData(this, nameof(_egoth), "Egoth", DEFAULT_VALUES_RUNE);
            _fal = new SkillData(this, nameof(_fal), "Fal", DEFAULT_VALUES_RUNE);
            _shim = new SkillData(this, nameof(_shim), "Shim", DEFAULT_VALUES_RUNE);
            _runicLanternIntensity = CreateSetting(nameof(_runicLanternIntensity), 100, IntRange(0, 100));
            _runeSoundEffectVolume = CreateSetting(nameof(_runeSoundEffectVolume), 100, IntRange(0, 100));

            // Editor effects
            _backstab.InitializeEffectX("Frontstab multiplier", (prefab, value) =>
            {
                WeaponDamage front = prefab.transform.Find("HitEffects").GetComponent<WeaponDamage>();
                front.WeaponDamageMult = front.WeaponKnockbackMult = value;
            });
            _backstab.InitializeEffectY("Backstab multiplier", (prefab, value) =>
            {
                WeaponDamage back = prefab.transform.Find("BackstabHitEffects").GetComponent<WeaponDamage>();
                back.WeaponDamageMult = back.WeaponKnockbackMult = value;
            });
        }
        override protected void SetFormatting()
        {
            _daggerToggle.Format("Dagger");
            using(Indent)
            {
                _daggerSlash.FormatSettings(_daggerToggle);
                _backstab.FormatSettings(_daggerToggle);
                _opportunistStab.FormatSettings(_daggerToggle);
                _serpentsParry.FormatSettings(_daggerToggle);
            }

            _bowToggle.Format("Bow");
            using(Indent)
            {
                _evasionShot.FormatSettings(_bowToggle);
                _sniperShot.FormatSettings(_bowToggle);
                _piercingShot.FormatSettings(_bowToggle);
            }

            _runesToggle.Format("Runes");
            using(Indent)
            {
                _dez.FormatSettings(_runesToggle);
                _egoth.FormatSettings(_runesToggle);
                _fal.FormatSettings(_runesToggle);
                _shim.FormatSettings(_runesToggle);
                _runeSoundEffectVolume.Format("Runes sound effect volume", _runesToggle);
                _runeSoundEffectVolume.Description = "If the runes are too loud for your ears";
                _runicLanternIntensity.Format("Runic Lantern intensity", _runesToggle);
                _runicLanternIntensity.Description = "If the glowing orb is too bright for your eyes";
            }
        }
        override protected string Description
        => "• Change effects, costs and cooldown of select skills";
        override protected string SectionOverride
        => ModSections.Skills;
        override protected string ModName
        => "Editor";
        override protected void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(Preset.Vheos_CoopSurvival):
                    ForceApply();
                    _daggerToggle.Value = true;
                    {
                        _daggerSlash._toggle.Value = true;
                        {
                            _daggerSlash._vitalCosts.Value = new Vector3(0, 3, 0);
                            _daggerSlash._otherCosts.Value = new Vector3(0, 0, 1.5f);
                        }
                        _backstab._toggle.Value = true;
                        {
                            _backstab._effects.Value = new Vector3(0.5f, 3.0f, 0);
                            _backstab._vitalCosts.Value = new Vector3(0, 9, 0);
                            _backstab._otherCosts.Value = new Vector3(0, 0, 30);
                        }
                    }
                    _bowToggle.Value = true;
                    {
                        _evasionShot._toggle.Value = true;
                        {
                            _evasionShot._vitalCosts.Value = new Vector3(0, 10, 0);
                            _evasionShot._otherCosts.Value = new Vector3(0, 0, 20);
                        }
                    }
                    _runesToggle.Value = true;
                    {
                        _dez._toggle.Value = true;
                        {
                            _dez._vitalCosts.Value = new Vector3(0, 0, 9);
                            _dez._otherCosts.Value = new Vector3(0, 0, 0);
                        }
                        _egoth._toggle.Value = true;
                        {
                            _egoth._vitalCosts.Value = new Vector3(3, 0, 6);
                            _egoth._otherCosts.Value = new Vector3(0, 0, 0);
                        }
                        _fal._toggle.Value = true;
                        {
                            _fal._vitalCosts.Value = new Vector3(3, 4.5f, 3);
                            _fal._otherCosts.Value = new Vector3(0, 0, 0);
                        }
                        _shim._toggle.Value = true;
                        {
                            _shim._vitalCosts.Value = new Vector3(0, 4.5f, 6);
                            _shim._otherCosts.Value = new Vector3(0, 0, 0);
                        }
                    }
                    _runicLanternIntensity.Value = 10;
                    _runeSoundEffectVolume.Value = 50;
                    break;
            }
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006
        [HarmonyPatch(typeof(AddStatusEffect), "ActivateLocally"), HarmonyPrefix]
        static bool AddStatusEffect_ActivateLocally_Pre(AddStatusEffect __instance)
        {
            #region quit
            if (!_runesToggle || __instance.Status == null || __instance.Status.IdentifierName != RUNIC_LANTERN_ID)
                return true;
            #endregion

            // Cache
            ParticleSystem particleSystem = __instance.Status.FXPrefab.GetFirstComponentsInHierarchy<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            Color newColor = main.startColor.color;
            float intensity = _runicLanternIntensity / 100f;

            // Execute
            newColor.a = intensity;
            main.startColor = newColor;
            emission.rateOverTime = intensity.MapFrom01(2, RUNIC_LANTERN_EMISSION_RATE);
            return true;
        }

        [HarmonyPatch(typeof(PlaySoundEffect), "ActivateLocally"), HarmonyPrefix]
        static bool PlaySoundEffect_ActivateLocally_Pre(PlaySoundEffect __instance)
        {
            #region quit
            if (!_runesToggle || __instance.Sounds.IsNullOrEmpty() || __instance.Sounds.First() != GlobalAudioManager.Sounds.SFX_SKILL_RuneSpell)
                return true;
            #endregion

            float volume = _runeSoundEffectVolume / 100f;
            Global.AudioManager.PlaySoundAtPosition(GlobalAudioManager.Sounds.SFX_SKILL_RuneSpell, __instance.transform, 0f, volume, volume, __instance.MinPitch, __instance.MaxPitch);
            return false;
        }
    }
}