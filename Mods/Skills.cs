using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



namespace ModPack
{
    public class Skills : AMod, IDelayedInit
    {
        #region const
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_DAGGER_SLASH =
        (
            new Vector3(0, 0, 0),
            new Vector3(0, 5, 0),
            new Vector3(0, 0, 15)
        );
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_BACKSTAB =
        (
            new Vector3(1, 3, 0),
            new Vector3(0, 2, 0),
            new Vector3(0, 0, 0.5f)
        );
        private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_EVASION_SHOT =
        (
            new Vector3(0, 0, 0),
            new Vector3(0, 12, 0),
            new Vector3(0, 0, 30)
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
        #endregion
        #region class
        private class SkillData
        {
            // Settings
            private ModSetting<bool> _toggle;
            private ModSetting<Vector3> _effects, _vitalCosts, _otherCosts;
            public void CreateSettings(string skillSettingName, Vector3 effects, Vector3 vitalCosts, Vector3 otherCosts)
            {
                _toggle = _mod.CreateSetting(skillSettingName + nameof(_toggle), false);
                _effects = _mod.CreateSetting(skillSettingName + nameof(_effects), effects);
                _vitalCosts = _mod.CreateSetting(skillSettingName + nameof(_vitalCosts), vitalCosts);
                _otherCosts = _mod.CreateSetting(skillSettingName + nameof(_otherCosts), otherCosts);
            }
            public void FormatSettings()
            {
                _toggle.Format(_skillName);
                _mod.Indent++;
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
                    _mod.Indent--;
                }
            }

            // Constructor
            public SkillData(Skills mod, string skillSettingName, string skillName, (Vector3 Effects, Vector3 VitalCosts, Vector3 OtherCosts) defaultValues)
            {
                _mod = mod;
                _skillName = skillName;

                CreateSettings(skillSettingName, defaultValues.Effects, defaultValues.VitalCosts, defaultValues.OtherCosts);

                Skill prefab = Prefabs.GetSkillByName(_skillName);
                _mod.AddEventOnConfigClosed(() =>
                {
                    if (_toggle)
                        ApplySettingsToPrefab(prefab);
                });
            }

            // Utility
            private Skills _mod;
            private string _skillName;
            private string _effectX, _effectY, _effectZ;
            private Action<Skill, float> _applyEffectX, _applyEffectY, _applyEffectZ;
            private void ApplySettingsToPrefab(Skill prefab)
            {
                _applyEffectX?.Invoke(prefab, _effects.Value.x);
                _applyEffectY?.Invoke(prefab, _effects.Value.y);
                _applyEffectZ?.Invoke(prefab, _effects.Value.z);
                prefab.HealthCost = _vitalCosts.Value.x;
                prefab.StaminaCost = _vitalCosts.Value.y;
                prefab.ManaCost = _vitalCosts.Value.z;
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
        static private SkillData _daggerSlash, _backstab;
        static private SkillData _evasionShot, _sniperShot, _piercingShot;
        override protected void Initialize()
        {
            _daggerSlash = new SkillData(this, nameof(_daggerSlash), "Dagger Slash", DEFAULT_VALUES_DAGGER_SLASH);
            _backstab = new SkillData(this, nameof(_backstab), "Backstab", DEFAULT_VALUES_BACKSTAB);
            _evasionShot = new SkillData(this, nameof(_evasionShot), "Evasion Shot", DEFAULT_VALUES_EVASION_SHOT);
            _sniperShot = new SkillData(this, nameof(_sniperShot), "Sniper Shot", DEFAULT_VALUES_SNIPER_SHOT);
            _piercingShot = new SkillData(this, nameof(_piercingShot), "Piercing Shot", DEFAULT_VALUES_PIERCING_SHOT);

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
            _daggerSlash.FormatSettings();
            _backstab.FormatSettings();
            _evasionShot.FormatSettings();
            _sniperShot.FormatSettings();
            _piercingShot.FormatSettings();
        }
        override protected string Description
        => "";
        override protected string SectionOverride
        => SECTION_COMBAT;
    }
}