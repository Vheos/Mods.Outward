using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



namespace ModPack
{
    public class Durability : AMod
    {
        #region const
        private const int FAST_MAINTENANCE_ID = 8205140;
        static private AreaManager.AreaEnum[] OPEN_REGIONS = new[]
        {
            AreaManager.AreaEnum.CierzoOutside,
            AreaManager.AreaEnum.Emercar,
            AreaManager.AreaEnum.HallowedMarsh,
            AreaManager.AreaEnum.Abrassar,
            AreaManager.AreaEnum.AntiqueField,
            AreaManager.AreaEnum.Caldera,
        };
        static private AreaManager.AreaEnum[] CITIES = new[]
        {
            AreaManager.AreaEnum.CierzoVillage,
            AreaManager.AreaEnum.Berg,
            AreaManager.AreaEnum.Monsoon,
            AreaManager.AreaEnum.Levant,
            AreaManager.AreaEnum.Harmattan,
            AreaManager.AreaEnum.NewSirocco,
        };
        #endregion
        #region enum
        private enum MultiRepairBehaviour
        {
            UseFixedValueForAllItems = 1,
            DivideValueAmongItems = 2,
            TryToEqualizeValues = 3,
            TryToEqualizeRatios = 4,
        }
        private enum RepairPercentReference
        {
            OfMaxDurability = 1,
            OfMissingDurability = 2,
        }
        #endregion

        // Setting
        static private ModSetting<bool> _lossModifiers;
        static private ModSetting<int> _lossWeapons, _lossArmors, _lossLights, _lossIngestibles;
        static private ModSetting<bool> _campingRepairToggle;
        static private ModSetting<int> _repairDurabilityPerHour, _repairDurabilityPercentPerHour;
        static private ModSetting<RepairPercentReference> _repairPercentReference;
        static private ModSetting<MultiRepairBehaviour> _multiRepairBehaviour;
        static private ModSetting<int> _fastMaintenanceMultiplier;

        static private ModSetting<bool> _effectivenessAffectsAllStats, _effectivenessAffectsPenalties;
        static private ModSetting<bool> _linearEffectiveness;
        static private ModSetting<int> _minNonBrokenEffectiveness, _brokenEffectiveness;
        static private ModSetting<bool> _smithRepairsOnlyEquipped;
        override protected void Initialize()
        {
            _lossModifiers = CreateSetting(nameof(_lossModifiers), false);
            _lossWeapons = CreateSetting(nameof(_lossWeapons), 100, IntRange(0, 200));
            _lossArmors = CreateSetting(nameof(_lossArmors), 100, IntRange(0, 200));
            _lossLights = CreateSetting(nameof(_lossLights), 100, IntRange(0, 200));
            _lossIngestibles = CreateSetting(nameof(_lossIngestibles), 100, IntRange(0, 200));

            _campingRepairToggle = CreateSetting(nameof(_campingRepairToggle), false);
            _repairDurabilityPerHour = CreateSetting(nameof(_repairDurabilityPerHour), 0, IntRange(0, 100));
            _repairDurabilityPercentPerHour = CreateSetting(nameof(_repairDurabilityPercentPerHour), 10, IntRange(0, 100));
            _repairPercentReference = CreateSetting(nameof(_repairPercentReference), RepairPercentReference.OfMaxDurability);
            _multiRepairBehaviour = CreateSetting(nameof(_multiRepairBehaviour), MultiRepairBehaviour.UseFixedValueForAllItems);
            _fastMaintenanceMultiplier = CreateSetting(nameof(_fastMaintenanceMultiplier), 150, IntRange(100, 200));

            _effectivenessAffectsAllStats = CreateSetting(nameof(_effectivenessAffectsAllStats), false);
            _effectivenessAffectsPenalties = CreateSetting(nameof(_effectivenessAffectsPenalties), false);
            _linearEffectiveness = CreateSetting(nameof(_linearEffectiveness), false);
            _minNonBrokenEffectiveness = CreateSetting(nameof(_minNonBrokenEffectiveness), 50, IntRange(0, 100));
            _brokenEffectiveness = CreateSetting(nameof(_brokenEffectiveness), 15, IntRange(0, 100));
            _smithRepairsOnlyEquipped = CreateSetting(nameof(_smithRepairsOnlyEquipped), false);
        }
        override protected void SetFormatting()
        {
            _lossModifiers.Format("Durability loss modifiers");
            Indent++;
            {
                _lossWeapons.Format("Weapons", _lossModifiers);
                _lossArmors.Format("Armors", _lossModifiers);
                _lossLights.Format("Lights", _lossModifiers);
                _lossIngestibles.Format("Food", _lossModifiers);
                Indent--;
            }
            _campingRepairToggle.Format("Camping repair");
            Indent++;
            {
                _repairDurabilityPerHour.Format("Durability per hour", _campingRepairToggle);
                _repairDurabilityPercentPerHour.Format("Durability % per hour", _campingRepairToggle);
                _repairDurabilityPercentPerHour.Description = "By default, % of max durability (can be changed below)";
                _repairPercentReference.Format("", _repairDurabilityPercentPerHour, () => _repairDurabilityPercentPerHour > 0);
                _fastMaintenanceMultiplier.Format("\"Fast Maintenance\" repair multiplier", _campingRepairToggle);
                _multiRepairBehaviour.Format("When repairing multiple items", _campingRepairToggle);
                _multiRepairBehaviour.Description = "Use fixed value for all items   -   the same repair value will be used for all items\n" +
                                                    "Divide value among items   -   the repair value will be divided by the number of equipped items\n" +
                                                    "Try to equalize values   -   repair item with the lowest durabilty value\n" +
                                                    "Try to equalize ratios   -   repair item with the lowest durabilty ratio";
                Indent--;
            }

            _effectivenessAffectsAllStats.Format("Effectiveness affects all stats");
            Indent++;
            {
                _effectivenessAffectsPenalties.Format("affect penalties");
                Indent--;
            }
            _linearEffectiveness.Format("Linear effectiveness");
            Indent++;
            {
                _minNonBrokenEffectiveness.Format("when nearing zero durability", _linearEffectiveness);
                _brokenEffectiveness.Format("when broken", _linearEffectiveness);
                Indent--;
            }
            _smithRepairsOnlyEquipped.Format("Smith repairs only equipped items");
            _smithRepairsOnlyEquipped.Description = "Blacksmith will not repair items in your pouch and bag";
        }
        override protected string Description
        => "• Restrict camping spots to chosen places\n" +
           "• Change butterfly zones spawn chance and radius\n" +
           "• Customize repairing mechanic";
        override protected string SectionOverride
        => SECTION_SURVIVAL;

        // Utility
        static private float CalculateDurabilityGain(Item item, float flat, float percent)
        {
            float percentReference = _repairPercentReference == RepairPercentReference.OfMissingDurability
                                   ? item.MaxDurability - item.m_currentDurability
                                   : item.m_currentDurability;
            return flat + percent * percentReference;
        }
        static private bool HasLearnedFastMaintenance(Character character)
        => character.Inventory.SkillKnowledge.IsItemLearned(FAST_MAINTENANCE_ID);
        static private void TryApplyEffectiveness(ref float stat, EquipmentStats equipmentStats, bool invertedPositivity = false)
        {
            #region quit
            if (!_effectivenessAffectsAllStats)
                return;
            #endregion

            bool isNegative = stat < 0 && !invertedPositivity
                           || stat > 0 && invertedPositivity;
            if (!isNegative || _effectivenessAffectsPenalties)
                stat *= equipmentStats.Effectiveness;
        }

        // Hooks
        [HarmonyPatch(typeof(Item), "ReduceDurability"), HarmonyPrefix]
        static bool Item_ReduceDurability_Pre(ref Item __instance, ref float _durabilityLost)
        {
            #region quit
            if (!_lossModifiers)
                return true;
            #endregion

            int modifier = 100;
            if (__instance is Weapon)
                modifier = _lossWeapons;
            else if (__instance is Armor)
                modifier = _lossArmors;
            else if (__instance.LitStatus != Item.Lit.Unlightable)
                modifier = _lossLights;
            else if (__instance.IsIngestible())
                modifier = _lossIngestibles;

            _durabilityLost *= modifier / 100f;
            return true;
        }

        [HarmonyPatch(typeof(CharacterEquipment), "RepairEquipmentAfterRest"), HarmonyPrefix]
        static bool CharacterEquipment_RepairEquipmentAfterRest_Pre(CharacterEquipment __instance)
        {
            #region quit
            if (!_campingRepairToggle)
                return false;
            #endregion

            // Cache
            List<Equipment> equippedItems = new List<Equipment>();
            foreach (var slot in __instance.m_equipmentSlots)
                if (Various.IsAnythingEquipped(slot) && Various.IsNotLeftHandUsedBy2H(slot)
                && slot.EquippedItem.RepairedInRest && !slot.EquippedItem.IsIndestructible && slot.EquippedItem.DurabilityRatio < 1f)
                    equippedItems.Add(slot.EquippedItem);

            if (equippedItems.IsEmpty())
                return false;

            // Repair values
            float flat = _repairDurabilityPerHour;
            float percent = _repairDurabilityPercentPerHour / 100f;
            if (HasLearnedFastMaintenance(__instance.m_character))
            {
                flat *= _fastMaintenanceMultiplier / 100f;
                percent *= _fastMaintenanceMultiplier / 100f;
            }
            if (_multiRepairBehaviour == MultiRepairBehaviour.DivideValueAmongItems)
            {
                flat /= equippedItems.Count;
                percent /= equippedItems.Count;
            }

            // Execute
            for (int i = 0; i < __instance.m_character.CharacterResting.GetRepairLength(); i++)
                if (_multiRepairBehaviour == MultiRepairBehaviour.TryToEqualizeValues
                || _multiRepairBehaviour == MultiRepairBehaviour.TryToEqualizeRatios)
                {
                    bool equalizeValues = _multiRepairBehaviour == MultiRepairBehaviour.TryToEqualizeValues;
                    float minTest = equippedItems.Min(item => (equalizeValues ? item.m_currentDurability : item.DurabilityRatio));
                    Item minItem = equippedItems.Find(item => (equalizeValues ? item.m_currentDurability : item.DurabilityRatio) == minTest);
                    minItem.m_currentDurability += CalculateDurabilityGain(minItem, flat, percent);
                }
                else
                    foreach (var item in equippedItems)
                        item.m_currentDurability += CalculateDurabilityGain(item, flat, percent);

            // Clamp
            foreach (var item in equippedItems)
                if (item.DurabilityRatio > 1f)
                    item.SetDurabilityRatio(1f);

            return false;
        }

        [HarmonyPatch(typeof(ItemContainer), "RepairContainedEquipment"), HarmonyPrefix]
        static bool ItemContainer_RepairContainedEquipment_Pre(ItemContainer __instance)
        => !_smithRepairsOnlyEquipped;

        [HarmonyPatch(typeof(ItemStats), "Effectiveness", MethodType.Getter), HarmonyPrefix]
        static bool ItemStats_Effectiveness_Pre(ref ItemStats __instance, ref float __result)
        {
            #region quit
            if (!_linearEffectiveness || __instance.m_item.IsNot<Equipment>())
                return true;
            #endregion

            float ratio = __instance.m_item.DurabilityRatio;
            if (ratio > 0)
                __result = __instance.m_item.DurabilityRatio.MapFrom01(_minNonBrokenEffectiveness / 100f, 1f);
            else
                __result = _brokenEffectiveness / 100f;

            return false;
        }

        // Affect all stats
        [HarmonyPatch(typeof(ItemDetailsDisplay), "GetPenaltyDisplay"), HarmonyPrefix]
        static bool ItemDetailsDisplay_GetPenaltyDisplay_Pre(ref ItemDetailsDisplay __instance, ref string __result, float _value, bool _negativeIsPositive, bool _showPercent)
        {
            #region quit
            if (!_effectivenessAffectsAllStats)
                return true;
            #endregion

            string text = _value.Round().ToString();
            if (_value > 0)
                text = "+" + text;
            if (_showPercent)
                text += "%";

            Color color = Global.LIGHT_GREEN;
            if (_value < 0 && !_negativeIsPositive
            || _value > 0 && _negativeIsPositive)
                color = Global.LIGHT_RED;

            __result = Global.SetTextColor(text, color);
            return false;
        }

        [HarmonyPatch(typeof(EquipmentStats), "MovementPenalty", MethodType.Getter), HarmonyPostfix]
        static void EquipmentStats_MovementPenalty_Post(ref EquipmentStats __instance, ref float __result)
        => TryApplyEffectiveness(ref __result, __instance, true);

        [HarmonyPatch(typeof(EquipmentStats), "StaminaUsePenalty", MethodType.Getter), HarmonyPostfix]
        static void EquipmentStats_StaminaUsePenalty_Post(ref EquipmentStats __instance, ref float __result)
        => TryApplyEffectiveness(ref __result, __instance, true);

        [HarmonyPatch(typeof(EquipmentStats), "ManaUseModifier", MethodType.Getter), HarmonyPostfix]
        static void EquipmentStats_ManaUseModifier_Post(ref EquipmentStats __instance, ref float __result)
        => TryApplyEffectiveness(ref __result, __instance, true);

        [HarmonyPatch(typeof(Weapon), "BaseAttackSpeed", MethodType.Getter), HarmonyPostfix]
        static void Weapon_BaseAttackSpeed_Post(Weapon __instance, ref float __result)
        {
            float relativeAttackSpeed = __result - 1f;
            TryApplyEffectiveness(ref relativeAttackSpeed, __instance.Stats);
            __result = relativeAttackSpeed + 1f;
        }
    }
}