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
        static private ModSetting<int> _lossWeapons, _lossArmors, _lossLights, _lossFood;
        static private ModSetting<bool> _campingRepairToggle;
        static private ModSetting<int> _repairDurabilityPerHour, _repairDurabilityPercentPerHour;
        static private ModSetting<RepairPercentReference> _repairPercentReference;
        static private ModSetting<MultiRepairBehaviour> _multiRepairBehaviour;
        static private ModSetting<int> _fastMaintenanceMultiplier;
        static private ModSetting<bool> _smithRepairsOnlyEquipped;
        override protected void Initialize()
        {
            _lossModifiers = CreateSetting(nameof(_lossModifiers), false);
            _lossWeapons = CreateSetting(nameof(_lossWeapons), 100, IntRange(0, 200));
            _lossArmors = CreateSetting(nameof(_lossArmors), 100, IntRange(0, 200));
            _lossLights = CreateSetting(nameof(_lossLights), 100, IntRange(0, 200));
            _lossFood = CreateSetting(nameof(_lossFood), 100, IntRange(0, 200));

            _campingRepairToggle = CreateSetting(nameof(_campingRepairToggle), false);
            _repairDurabilityPerHour = CreateSetting(nameof(_repairDurabilityPerHour), 0, IntRange(0, 100));
            _repairDurabilityPercentPerHour = CreateSetting(nameof(_repairDurabilityPercentPerHour), 10, IntRange(0, 100));
            _repairPercentReference = CreateSetting(nameof(_repairPercentReference), RepairPercentReference.OfMaxDurability);
            _multiRepairBehaviour = CreateSetting(nameof(_multiRepairBehaviour), MultiRepairBehaviour.UseFixedValueForAllItems);
            _fastMaintenanceMultiplier = CreateSetting(nameof(_fastMaintenanceMultiplier), 150, IntRange(100, 200));

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
                _lossFood.Format("Food", _lossModifiers);
                Indent--;
            }
            _smithRepairsOnlyEquipped.Description = "Blacksmith will not repair items in your pouch and bag";
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
            _smithRepairsOnlyEquipped.Format("Smith repairs only equipped items");
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
                modifier = _lossFood;

            _durabilityLost *= modifier / 100f;
            return true;
        }

        [HarmonyPatch(typeof(CharacterEquipment), "RepairEquipmentAfterRest"), HarmonyPrefix]
        static bool CharacterEquipment_RepairEquipmentAfterRest_Pre(ref CharacterEquipment __instance)
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
    }
}