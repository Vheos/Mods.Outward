using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using Vheos.Tools.ModdingCore;
using Vheos.Tools.Extensions.Math;
using Vheos.Tools.Extensions.General;



namespace Vheos.Mods.Outward
{
    public class Revive : AMod
    {
        #region const       
        private const float ITEM_INTERACT_RADIUS = 0.1f;   // InteractionTriggerBase.m_detectionColliderRadius
        private const float PLAYER_INTERACT_RADIUS = 1.4f;   // Character.ItemDetectionSphereRadius
        private const float BASE_MAX_NEED = 1000f;   // PlayerCharacterStats.m_maxFood
        private const float NEED_LOST_ON_REVIVE_MAX_RATIO = 1f - 100f / BASE_MAX_NEED;   // PlayerCharacterStats.MIN_FOOD_AFTER_REVIVE
        private const float HEALTH_STAMINA_BURN_MAX_RATIO = 0.9f;   // CharacterStats.MAX_BURNT_HEALTH_RATIO
        private const float MANA_BURN_MAX_RATIO = 0.5f;   // CharacterStats.BURNT_MANA_RATIO_MAX
        private const int DEFAULT_DETECTION_PRIORITY = 10;   // InteractionTriggerBase.DetectionPriority
        #endregion

        // Config
        static private ModSetting<bool> _interactionToggle, _maxVitalsToggle, _vitalsToggle, _needsToggle;
        static private ModSetting<float> _interactionDuration;
        static private ModSetting<float> _interactionDistance;
        static private ModSetting<bool> _interactionPrioritize;
        static private ModSetting<int> _maxHealthLost, _maxStaminaLost, _maxManaLost;
        static private ModSetting<int> _newHealth, _newStamina, _manaLost;
        static private ModSetting<int> _foodLost, _drinkLost, _sleepLost, _corruptionGained;
        override protected void Initialize()
        {
            _interactionToggle = CreateSetting(nameof(_interactionToggle), false);
            _interactionDuration = CreateSetting(nameof(_interactionDuration), GameInput.HOLD_THRESHOLD + GameInput.HOLD_DURATION, FloatRange(0.1f + GameInput.HOLD_THRESHOLD, 5f));
            _interactionDistance = CreateSetting(nameof(_interactionDistance), ITEM_INTERACT_RADIUS + PLAYER_INTERACT_RADIUS, FloatRange(0.1f + PLAYER_INTERACT_RADIUS, 5f));
            _interactionPrioritize = CreateSetting(nameof(_interactionPrioritize), false);

            _maxVitalsToggle = CreateSetting(nameof(_maxVitalsToggle), false);
            _maxHealthLost = CreateSetting(nameof(_maxHealthLost), -50, IntRange((int)(-100 * HEALTH_STAMINA_BURN_MAX_RATIO), -0));
            _maxStaminaLost = CreateSetting(nameof(_maxStaminaLost), -0, IntRange((int)(-100 * HEALTH_STAMINA_BURN_MAX_RATIO), -0));
            _maxManaLost = CreateSetting(nameof(_maxManaLost), -0, IntRange((int)(-100 * MANA_BURN_MAX_RATIO), -0));

            _vitalsToggle = CreateSetting(nameof(_vitalsToggle), false);
            _newHealth = CreateSetting(nameof(_newHealth), +100, IntRange(+1, +100));
            _newStamina = CreateSetting(nameof(_newStamina), +100, IntRange(+0, +100));
            _manaLost = CreateSetting(nameof(_manaLost), -0, IntRange(-100, -0));

            _needsToggle = CreateSetting(nameof(_needsToggle), false);
            _foodLost = CreateSetting(nameof(_foodLost), -0, IntRange((int)(-100 * NEED_LOST_ON_REVIVE_MAX_RATIO), -0));
            _drinkLost = CreateSetting(nameof(_drinkLost), -0, IntRange((int)(-100 * NEED_LOST_ON_REVIVE_MAX_RATIO), -0));
            _sleepLost = CreateSetting(nameof(_sleepLost), -0, IntRange((int)(-100 * NEED_LOST_ON_REVIVE_MAX_RATIO), -0));
            _corruptionGained = CreateSetting(nameof(_corruptionGained), +0, IntRange(+0, +100));
        }
        override protected void SetFormatting()
        {
            _interactionToggle.Format("Interaction settings");
            Indent++;
            {
                _interactionDuration.Format("Duration", _interactionToggle);
                _interactionDuration.Description = "How long you need to hold the interaction button to revive a player";
                _interactionDistance.Format("Distance", _interactionToggle);
                _interactionDistance.Description = "From how far away you can start reviving";
                _interactionPrioritize.Format("Prioritize", _interactionToggle);
                _interactionPrioritize.Description = "Ignore other interactions if a dead player is nearby";
                Indent--;
            }

            _maxVitalsToggle.Format("Max Vitals");
            _maxVitalsToggle.Description = "% of current max vital\n" +
                                           "(example: -10% setting and 50 max health before death will result in 5 Max Health lost after revive)";
            Indent++;
            {
                _maxHealthLost.Format("Max health", _maxVitalsToggle);
                _maxStaminaLost.Format("Max stamina", _maxVitalsToggle);
                _maxManaLost.Format("Max mana", _maxVitalsToggle);
                Indent--;
            }

            _vitalsToggle.Format("Vitals");
            _vitalsToggle.Description = "% of max vital, after revive changes\n" +
                                        "Health and stamina are set to a new value regardless of what they were before death, while mana is changed relative to its previous value";
            Indent++;
            {
                _newHealth.Format("Health (new) ", _vitalsToggle);
                _newStamina.Format("Stamina (new)", _vitalsToggle);
                _manaLost.Format("Mana", _vitalsToggle);
                Indent--;
            }

            _needsToggle.Format("Needs & Corruption");
            _needsToggle.Description = "% of remaining need / missing corruption";
            Indent++;
            {
                _foodLost.Format("Food", _needsToggle);
                _drinkLost.Format("Drink", _needsToggle);
                _sleepLost.Format("Sleep", _needsToggle);
                _corruptionGained.Format("Corruption", _needsToggle);
                Indent--;
            }
        }
        override protected string Description
        => "• Change revive speed and distance\n" +
           "• Prioritize revive over other objects\n" +
           "• Change stats after revive\n" +
           "(vitals, max vitals, needs, corruption)";
        override protected string SectionOverride
        => ModSections.SurvivalAndImmersion;
        override public void LoadPreset(int preset)
        {
            switch ((Presets.Preset)preset)
            {
                case Presets.Preset.Vheos_CoopSurvival:
                    ForceApply();
                    _interactionToggle.Value = true;
                    {
                        _interactionDuration.Value = 5;
                        _interactionDistance.Value = 3;
                        _interactionPrioritize.Value = true;
                    }
                    _maxVitalsToggle.Value = true;
                    {
                        _maxHealthLost.Value = -10;
                        _maxStaminaLost.Value = -10;
                        _maxManaLost.Value = -10;
                    }
                    _vitalsToggle.Value = true;
                    {
                        _newHealth.Value = 50;
                        _newStamina.Value = 0;
                        _manaLost.Value = -50;
                    }
                    _needsToggle.Value = true;
                    {
                        _foodLost.Value = -10;
                        _drinkLost.Value = -10;
                        _sleepLost.Value = -10;
                        _corruptionGained.Value = 3;
                    }
                    break;
            }
        }

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        [HarmonyPatch(typeof(Character), "UpdateReviveInteraction"), HarmonyPostfix]
        static void Character_UpdateReviveInteraction_Pre(Character __instance)
        {
            #region quit
            if (!_interactionToggle)
                return;
            #endregion

            Transform reviveTransform = __instance.transform.Find("ReviveInteraction");
            if (reviveTransform != null)
            {
                InteractionRevive interaction = reviveTransform.GetComponent<InteractionRevive>();
                InteractionTriggerBase triggerBase = reviveTransform.GetComponent<InteractionTriggerBase>();
                float distance = _interactionDistance.Value - PLAYER_INTERACT_RADIUS;

                // Change
                interaction.HoldActivationTime = _interactionDuration.Value;
                triggerBase.DetectionPriority = _interactionPrioritize.Value ? int.MinValue : DEFAULT_DETECTION_PRIORITY;
                triggerBase.DetectionColliderRadius = distance;
                if (triggerBase.InteractionCollider != null)
                    triggerBase.InteractionCollider.As<SphereCollider>().radius = distance;
            }
        }

        [HarmonyPatch(typeof(InteractionRevive), "OnActivate"), HarmonyPrefix]
        static bool InteractionRevive_OnActivate_Pre(ref Character __state, ref Character ___m_character)
        {
            // send m_character to postix and null it during original method
            __state = ___m_character;
            ___m_character = null;
            return true;
        }

        [HarmonyPatch(typeof(InteractionRevive), "OnActivate"), HarmonyPostfix]
        static void InteractionRevive_OnActivate_Post(ref Character __state, ref Character ___m_character)
        {
            // receive m_character from prefix
            ___m_character = __state;

            if (___m_character != null)
            {
                if (___m_character.IsPetrified)
                    ___m_character.Unpetrify();
                else
                {
                    // Common
                    PlayerSaveData data = new PlayerSaveData(___m_character);
                    PlayerCharacterStats stats = ___m_character.PlayerStats;

                    // Affect burns
                    if (_maxVitalsToggle)
                    {
                        data.BurntHealth += (stats.MaxHealth - data.BurntHealth) * -_maxHealthLost.Value / 100f;
                        data.BurntStamina += (stats.MaxStamina - data.BurntStamina) * -_maxStaminaLost.Value / 100f;
                        if (stats.MaxMana > 0)
                            data.BurntMana += (stats.MaxMana - data.BurntMana) * -_maxManaLost.Value / 100f;
                    }

                    // Affect current vitals
                    if (_vitalsToggle)
                    {
                        data.Health = (stats.MaxHealth - data.BurntHealth) * _newHealth.Value / 100f;
                        data.Stamina = (stats.MaxStamina - data.BurntStamina) * _newStamina.Value / 100f;
                        if (stats.MaxMana > 0)
                            data.Mana += stats.CurrentMana * _manaLost.Value / 100f;
                    }

                    // Affect needs
                    if (_needsToggle)
                    {
                        data.Food += stats.Food * _foodLost.Value / 100f;
                        data.Drink += stats.Drink * _drinkLost.Value / 100f;
                        data.Sleep += stats.Sleep * _sleepLost.Value / 100f;
                        data.Corruption += (1000f - stats.Corruption) * _corruptionGained.Value / 100f;
                    }

                    // Finalize
                    data.Health = data.Health.ClampMin(1f);
                    ___m_character.Resurrect(data, true);
                }
            }
        }
    }
}