namespace Vheos.Mods.Outward;
using Vheos.Helpers.RNG;

public class Durability : AMod
{
    #region Constants
    private const int FAST_MAINTENANCE_ID = 8205140;
    #endregion

    #region Enums
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
    private enum EffectivnessStats
    {
        None = 0,
        AttackSpeed = 1 << 1,
        ImpactDamage = 1 << 2,
        Barrier = 1 << 3,
        ImpactResistance = 1 << 4,
    }
    #endregion

    // Setting
    private static ModSetting<bool> _lossMultipliers;
    private static ModSetting<int> _lossWeapons, _lossArmors, _lossLights, _lossIngestibles;
    private static ModSetting<bool> _campingRepairToggle;
    private static ModSetting<int> _repairDurabilityPerHour, _repairDurabilityPercentPerHour;
    private static ModSetting<RepairPercentReference> _repairPercentReference;
    private static ModSetting<MultiRepairBehaviour> _multiRepairBehaviour;
    private static ModSetting<int> _fastMaintenanceMultiplier;
    private static ModSetting<bool> _effectivenessAffectsAllStats, _effectivenessAffectsPenalties;
    private static ModSetting<bool> _linearEffectiveness;
    private static ModSetting<int> _minNonBrokenEffectiveness, _brokenEffectiveness;
    private static ModSetting<bool> _smithRepairsOnlyEquipped;
    private static ModSetting<int> _minStartingDurability;
    protected override void Initialize()
    {
        _lossMultipliers = CreateSetting(nameof(_lossMultipliers), false);
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
        _minStartingDurability = CreateSetting(nameof(_minStartingDurability), 100, IntRange(0, 100));
    }
    protected override void SetFormatting()
    {
        _lossMultipliers.Format("Durability loss multipliers");
        using (Indent)
        {
            _lossWeapons.Format("Weapons", _lossMultipliers);
            _lossWeapons.Description = "Includes shields";
            _lossArmors.Format("Armors", _lossMultipliers);
            _lossLights.Format("Lights", _lossMultipliers);
            _lossLights.Description = "Torches and lanterns";
            _lossIngestibles.Format("Food", _lossMultipliers);
        }
        _campingRepairToggle.Format("Camping repair");
        using (Indent)
        {
            _repairDurabilityPerHour.Format("Durability per hour", _campingRepairToggle);
            _repairDurabilityPercentPerHour.Format("Durability % per hour", _campingRepairToggle);
            _repairDurabilityPercentPerHour.Description = "By default, % of max durability (can be changed below)";
            _repairPercentReference.Format("", _repairDurabilityPercentPerHour, t => t > 0);
            _fastMaintenanceMultiplier.Format("\"Fast Maintenance\" repair multiplier", _campingRepairToggle);
            _multiRepairBehaviour.Format("When repairing multiple items", _campingRepairToggle);
            _multiRepairBehaviour.Description = "Use fixed value for all items   -   the same repair value will be used for all items\n" +
                                                "Divide value among items   -   the repair value will be divided by the number of equipped items\n" +
                                                "Try to equalize values   -   repair item with the lowest durabilty value\n" +
                                                "Try to equalize ratios   -   repair item with the lowest durabilty ratio";
        }

        _effectivenessAffectsAllStats.Format("Durability affects all stats");
        _effectivenessAffectsAllStats.Description = "Normally, durability affects only damages, resistances (impact only for shields) and protection\n" +
                                                    "This will make all* equipment stats decrease with durability\n" +
                                                    "( * currently all except damage bonuses)";
        using (Indent)
        {
            _effectivenessAffectsPenalties.Format("affect penalties", _effectivenessAffectsAllStats);
            _effectivenessAffectsPenalties.Description = "Stat penalties (like negative movement speed on heavy armors) will also decrease with durability";
        }
        _linearEffectiveness.Format("Smooth durability effects");
        _linearEffectiveness.Description = "Normally, equipment stats change only when durability reaches certain thresholds (50%, 25% and 0%)\n" +
                                           "This will update the stats smoothly, without any thersholds";
        using (Indent)
        {
            _minNonBrokenEffectiveness.Format("when nearing zero durability", _linearEffectiveness);
            _brokenEffectiveness.Format("when broken", _linearEffectiveness);
        }
        _smithRepairsOnlyEquipped.Format("Smith repairs only equipped items");
        _smithRepairsOnlyEquipped.Description = "Blacksmith will not repair items in your pouch and bag";
        _minStartingDurability.Format("Minimum starting durability");
        _minStartingDurability.Description = "When items are spawned, their durability is randomized between this value and 100%\n" +
                                             "Only affects dynamically spawned item (containers, enemy corpses, merchant stock)\n" +
                                             "Scene-static and serialized items are unaffected";
    }
    protected override string Description
    => "• Change how quickly durability decreases per item type\n" +
       "• Tweak camping repair mechanics\n" +
       "• Change how durability affects equipment stats\n" +
       "• Randomize starting durability of spawned items";
    protected override string SectionOverride
    => ModSections.SurvivalAndImmersion;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _lossMultipliers.Value = true;
                {
                    _lossWeapons.Value = 50;
                    _lossArmors.Value = 50;
                    _lossLights.Value = 150;
                    _lossIngestibles.Value = 100;
                }
                _campingRepairToggle.Value = true;
                {
                    _repairDurabilityPerHour.Value = 0;
                    _repairDurabilityPercentPerHour.Value = 0;
                    _repairPercentReference.Value = RepairPercentReference.OfMaxDurability;
                    _multiRepairBehaviour.Value = MultiRepairBehaviour.UseFixedValueForAllItems;
                }
                _effectivenessAffectsAllStats.Value = true;
                _effectivenessAffectsPenalties.Value = true;
                _linearEffectiveness.Value = true;
                {
                    _minNonBrokenEffectiveness.Value = 50;
                    _brokenEffectiveness.Value = 25;
                }
                _smithRepairsOnlyEquipped.Value = true;
                _minStartingDurability.Value = 67;
                break;
        }
    }

    // Utility
    private static float CalculateDurabilityGain(Item item, float flat, float percent)
    {
        float percentReference = _repairPercentReference == RepairPercentReference.OfMissingDurability
                               ? item.MaxDurability - item.m_currentDurability
                               : item.MaxDurability;
        return flat + percent * percentReference;
    }
    private static bool HasLearnedFastMaintenance(Character character)
    => character.Inventory.SkillKnowledge.IsItemLearned(FAST_MAINTENANCE_ID);
    private static void TryApplyEffectiveness(ref float stat, EquipmentStats equipmentStats, bool invertedPositivity = false)
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
    [HarmonyPrefix, HarmonyPatch(typeof(Item), nameof(Item.ReduceDurability))]
    private static void Item_ReduceDurability_Pre(Item __instance, ref float _durabilityLost)
    {
        #region quit
        if (!_lossMultipliers)
            return;
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
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CharacterEquipment), nameof(CharacterEquipment.RepairEquipmentAfterRest))]
    private static bool CharacterEquipment_RepairEquipmentAfterRest_Pre(CharacterEquipment __instance)
    {
        #region quit
        if (!_campingRepairToggle)
            return false;
        #endregion

        // Cache
        List<Equipment> equippedItems = new();
        foreach (var slot in __instance.m_equipmentSlots)
            if (slot.IsAnythingEquipped()
            && !slot.IsLeftHandUsedBy2H()
            && slot.EquippedItem.RepairedInRest
            && !slot.EquippedItem.IsIndestructible
            && slot.EquippedItem.DurabilityRatio < 1f)
                equippedItems.Add(slot.EquippedItem);

        if (equippedItems.IsNullOrEmpty())
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
                float minTest = equippedItems.Min(item => equalizeValues ? item.m_currentDurability : item.DurabilityRatio);
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

    [HarmonyPrefix, HarmonyPatch(typeof(ItemContainer), nameof(ItemContainer.RepairContainedEquipment))]
    private static bool ItemContainer_RepairContainedEquipment_Pre(ItemContainer __instance)
    => !_smithRepairsOnlyEquipped;

    [HarmonyPrefix, HarmonyPatch(typeof(ItemStats), nameof(ItemStats.Effectiveness), MethodType.Getter)]
    private static bool ItemStats_Effectiveness_Pre(ItemStats __instance, ref float __result)
    {
        #region quit
        if (!_linearEffectiveness || __instance.m_item.IsNot<Equipment>())
            return true;
        #endregion

        float ratio = __instance.m_item.DurabilityRatio;
        __result = ratio > 0
            ? __instance.m_item.DurabilityRatio.MapFrom01(_minNonBrokenEffectiveness / 100f, 1f)
            : _brokenEffectiveness / 100f;

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemDropper), nameof(ItemDropper.GenerateItem))]
    private static void ItemDropper_GenerateItem_Pre(ItemDropper __instance, ref Item __state, ItemContainer _container, BasicItemDrop _itemDrop, int _spawnAmount)
    {
        #region quit
        if (_minStartingDurability >= 100
        || !_itemDrop.DroppedItem.TryNonNull(out var item)
        || !item.Prefab().TryNonNull(out var prefab)
        || !prefab.Stats.TryNonNull(out var prefabStats)
        || prefabStats.MaxDurability <= 0)
            return;
        #endregion

        prefabStats.StartingDurability = (prefab.MaxDurability * RNG.Range(_minStartingDurability / 100f, 1f)).Round();
        __state = prefab;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ItemDropper), nameof(ItemDropper.GenerateItem))]
    private static void ItemDropper_GenerateItem_Post(ItemDropper __instance, ref Item __state)
    {
        #region quit
        if (__state == default)
            return;
        #endregion

        __state.Stats.StartingDurability = -1;
    }

    // Affect all stats
    [HarmonyPrefix, HarmonyPatch(typeof(ItemDetailsDisplay), nameof(ItemDetailsDisplay.GetPenaltyDisplay))]
    private static bool ItemDetailsDisplay_GetPenaltyDisplay_Pre(ItemDetailsDisplay __instance, ref string __result, float _value, bool _negativeIsPositive, bool _showPercent)
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

    [HarmonyPrefix, HarmonyPatch(typeof(ItemDetailRowDisplay), nameof(ItemDetailRowDisplay.SetInfo), new[] { typeof(string), typeof(float) })]
    private static bool ItemDetailRowDisplay_SetInfo_Pre(ItemDetailRowDisplay __instance, string _dataName, float _dataValue)
    {
        #region quit
        if (!_effectivenessAffectsAllStats)
            return true;
        #endregion

        __instance.SetInfo(_dataName, _dataValue.Round(), false, null);
        return false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Weapon), nameof(Weapon.BaseImpact), MethodType.Getter)]
    private static void Weapon_BaseImpact_Post(Weapon __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance.Stats);

    [HarmonyPostfix, HarmonyPatch(typeof(Weapon), nameof(Weapon.BaseAttackSpeed), MethodType.Getter)]
    private static void Weapon_BaseAttackSpeed_Post(Weapon __instance, ref float __result)
    {
        float relativeAttackSpeed = __result - 1f;
        TryApplyEffectiveness(ref relativeAttackSpeed, __instance.Stats);
        __result = relativeAttackSpeed + 1f;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.BarrierProtection), MethodType.Getter)]
    private static void EquipmentStats_BarrierProtection_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance);

    [HarmonyPrefix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.ImpactResistance), MethodType.Getter)]
    private static void EquipmentStats_ImpactResistance_Pre(EquipmentStats __instance)
    => __instance.m_impactResistEfficiencyAffected =
        _effectivenessAffectsAllStats || __instance.m_item.TryAs(out Weapon weapon) && weapon.Type == Weapon.WeaponType.Shield;

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.MovementPenalty), MethodType.Getter)]
    private static void EquipmentStats_MovementPenalty_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance, true);

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.StaminaUsePenalty), MethodType.Getter)]
    private static void EquipmentStats_StaminaUsePenalty_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance, true);

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.ManaUseModifier), MethodType.Getter)]
    private static void EquipmentStats_ManaUseModifier_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance, true);

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.HeatProtection), MethodType.Getter)]
    private static void EquipmentStats_HeatProtection_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance);

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.ColdProtection), MethodType.Getter)]
    private static void EquipmentStats_ColdProtection_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance);

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.CorruptionResistance), MethodType.Getter)]
    private static void EquipmentStats_CorruptionResistance_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance);

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.CooldownReduction), MethodType.Getter)]
    private static void EquipmentStats_CooldownReduction_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance, true);

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.HealthRegenBonus), MethodType.Getter)]
    private static void EquipmentStats_HealthRegenBonus_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance);

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.ManaRegenBonus), MethodType.Getter)]
    private static void EquipmentStats_ManaRegenBonus_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance);

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.StaminaCostReduction), MethodType.Getter)]
    private static void EquipmentStats_StaminaCostReduction_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance);

    [HarmonyPostfix, HarmonyPatch(typeof(EquipmentStats), nameof(EquipmentStats.StaminaRegenModifier), MethodType.Getter)]
    private static void EquipmentStats_StaminaRegenModifier_Post(EquipmentStats __instance, ref float __result)
    => TryApplyEffectiveness(ref __result, __instance);
}
