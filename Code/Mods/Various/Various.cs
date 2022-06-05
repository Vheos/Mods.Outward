/* TO DO:
 * - hide armor extras (like scarf)
 * - prevent dodging right after hitting
 */

namespace Vheos.Mods.Outward;

using System.Collections;
using UnityEngine.UI;

public class Various : AMod, IUpdatable
{
    #region const
    private const string INNS_QUEST_FAMILY_NAME = "Inns";
    private const int DROP_ONE_ACTION_ID = -2;
    private const string DROP_ONE_ACTION_TEXT = "Drop one";
    private const float DEFAULT_ENEMY_HEALTH_RESET_HOURS = 24f;   // Character.HoursToHealthReset
    private const int ARMOR_TRAINING_ID = 8205220;
    private static readonly Dictionary<TemperatureSteps, Vector2> DEFAULT_TEMPERATURE_DATA_BY_ENUM = new()
    {
        [TemperatureSteps.Coldest] = new Vector2(-45, -1),
        [TemperatureSteps.VeryCold] = new Vector2(-30, 14),
        [TemperatureSteps.Cold] = new Vector2(-20, 26),
        [TemperatureSteps.Fresh] = new Vector2(-14, 38),
        [TemperatureSteps.Neutral] = new Vector2(0, 50),
        [TemperatureSteps.Warm] = new Vector2(14, 62),
        [TemperatureSteps.Hot] = new Vector2(20, 80),
        [TemperatureSteps.VeryHot] = new Vector2(28, 92),
        [TemperatureSteps.Hottest] = new Vector2(40, 101),
    };
    #endregion
    #region enum
    private enum TitleScreenCharacterVisibility
    {
        Enable = 1,
        Disable = 2,
        Randomize = 3,
    }
    [Flags]
    private enum ArmorSlots
    {
        None = 0,
        Head = 1 << 1,
        Chest = 1 << 2,
        Feet = 1 << 3,
    }
    #endregion

    // Settings
    private static ModSetting<bool> _skipStartupVideos;
    private static ModSetting<TitleScreenCharacterVisibility> _titleScreenHideCharacters;
    private static ModSetting<bool> _enableCheats;
    private static ModSetting<string> _enableCheatsHotkey;
    private static ModSetting<ArmorSlots> _armorSlotsToHide;
    private static ModSetting<bool> _removeCoopScaling;
    private static ModSetting<bool> _healEnemiesOnLoad;
    private static ModSetting<bool> _multiplicativeStacking;
    private static ModSetting<int> _armorTrainingPenaltyReduction;
    private static ModSetting<bool> _applyArmorTrainingToManaCost;
    private static ModSetting<bool> _refillArrowsFromInventory;
    private static ModSetting<float> _baseStaminaRegen;
    private static ModSetting<int> _rentDuration;
    private static ModSetting<bool> _itemActionDropOne;
    private static ModSetting<bool> _highlightItemsWithLegacyUpgrade;
    private static ModSetting<Color> _legacyItemUpgradeColor;
    private static ModSetting<bool> _temperatureToggle;
    private static Dictionary<TemperatureSteps, ModSetting<Vector2>> _temperatureDataByEnum;
    protected override void Initialize()
    {
        _skipStartupVideos = CreateSetting(nameof(_skipStartupVideos), false);
        _titleScreenHideCharacters = CreateSetting(nameof(_titleScreenHideCharacters), TitleScreenCharacterVisibility.Enable);
        _enableCheats = CreateSetting(nameof(_enableCheats), false);
        _enableCheatsHotkey = CreateSetting(nameof(_enableCheatsHotkey), "");
        _armorSlotsToHide = CreateSetting(nameof(_armorSlotsToHide), ArmorSlots.None);
        _removeCoopScaling = CreateSetting(nameof(_removeCoopScaling), false);
        _healEnemiesOnLoad = CreateSetting(nameof(_healEnemiesOnLoad), false);
        _multiplicativeStacking = CreateSetting(nameof(_multiplicativeStacking), false);
        _armorTrainingPenaltyReduction = CreateSetting(nameof(_armorTrainingPenaltyReduction), 50, IntRange(0, 100));
        _applyArmorTrainingToManaCost = CreateSetting(nameof(_applyArmorTrainingToManaCost), false);
        _refillArrowsFromInventory = CreateSetting(nameof(_refillArrowsFromInventory), false);
        _baseStaminaRegen = CreateSetting(nameof(_baseStaminaRegen), 2.4f, FloatRange(0, 10));
        _rentDuration = CreateSetting(nameof(_rentDuration), 12, IntRange(1, 168));
        _itemActionDropOne = CreateSetting(nameof(_itemActionDropOne), false);
        _highlightItemsWithLegacyUpgrade = CreateSetting(nameof(_highlightItemsWithLegacyUpgrade), false);
        _legacyItemUpgradeColor = CreateSetting(nameof(_legacyItemUpgradeColor), new Color(1f, 0.75f, 0f, 0.5f));
        _temperatureToggle = CreateSetting(nameof(_temperatureToggle), false);
        _temperatureDataByEnum = new Dictionary<TemperatureSteps, ModSetting<Vector2>>();
        foreach (var step in InternalUtility.GetEnumValues<TemperatureSteps>())
            if (step != TemperatureSteps.Count)
                _temperatureDataByEnum.Add(step, CreateSetting(nameof(_temperatureDataByEnum) + step, DEFAULT_TEMPERATURE_DATA_BY_ENUM[step]));

        _enableCheats.AddEvent(() => Global.CheatsEnabled = _enableCheats);
        AddEventOnConfigClosed(() =>
        {
            foreach (var player in Players.Local)
                UpdateBaseStaminaRegen(player.Stats);
            TryUpdateTemperatureData();
        });

    }
    protected override void SetFormatting()
    {
        _skipStartupVideos.Format("Skip startup videos");
        _skipStartupVideos.Description =
            "Saves ~3 seconds each time you launch the game";
        _titleScreenHideCharacters.Format("Title screen characters");
        _titleScreenHideCharacters.Description =
            "If you think they are ruining the view :)\n" +
            "(requires game restart)";

        _enableCheats.Format("Enable cheats");
        using (Indent)
        {
            _enableCheatsHotkey.Format("Hotkey");
        }
        _enableCheats.Description = "aka Debug Mode";
        _armorSlotsToHide.Format("Armor slots to hide");
        _armorSlotsToHide.Description = "Used to hide ugly helmets (purely visual)";

        _removeCoopScaling.Format("Remove multiplayer scaling");
        _removeCoopScaling.Description = "Enemies in multiplayer will have the same stats as in singleplayer";
        _healEnemiesOnLoad.Format("Heal enemies on load");
        _healEnemiesOnLoad.Description = "Every loading screen fully heals all enemies";
        _multiplicativeStacking.Format("Multiplicative stacking");
        _multiplicativeStacking.Description = "Some stats will stack multiplicatively instead of additvely\n" +
                                              "(movement speed, stamina cost, mana cost)";
        using (Indent)
        {
            _armorTrainingPenaltyReduction.Format("\"Armor Training\" penalty reduction", _multiplicativeStacking);
            _armorTrainingPenaltyReduction.Description = "How much of equipment's movement speed and stamina cost penalties should \"Armor Training\" ignore";
            _applyArmorTrainingToManaCost.Format("\"Armor Training\" affects mana cost", _multiplicativeStacking);
            _applyArmorTrainingToManaCost.Description = "\"Armor Training\" will also lower equipment's mana cost penalties";
        }
        _refillArrowsFromInventory.Format("Refill arrows from inventory");
        _refillArrowsFromInventory.Description = "Whenever you shoot your bow, the lost arrow is instantly replaced with one from your backpack or pouch (in that order)";
        _baseStaminaRegen.Format("Base stamina regen");
        _rentDuration.Format("Inn rent duration");
        _rentDuration.Description = "Pay the rent once, sleep for up to a week (in hours)";

        _itemActionDropOne.Format("Add \"Drop one\" item action");
        _itemActionDropOne.Description = "Adds a button to stacked items' which skips the \"choose amount\" panel and drops exactly 1 of the item\n" +
                                         "(recommended when playing co-op for quick item sharing)";
        _highlightItemsWithLegacyUpgrade.Format("Highlight items with legacy upgrades");
        using (Indent)
        {
            _legacyItemUpgradeColor.Format("color", _highlightItemsWithLegacyUpgrade);
        }
        _temperatureToggle.Format("Temperature");
        _temperatureToggle.Description =
            "Change each environmental temperature level's value and cap:\n" +
            "X   -   value; how much cold/hot weather defense you need to nullify this temperature level\n" +
            "Y   -   cap; min/max player temperature at this environmental temperature level\n" +
            "\n" +
            "Player temperatures cheatsheet:\n" +
            "Very cold   -   25\n" +
            "Cold   -   40\n" +
            "Neutral   -   50\n" +
            "Hot   -   60\n" +
            "Very Hot   -   75)";
        using (Indent)
        {
            foreach (var step in InternalUtility.GetEnumValues<TemperatureSteps>())
                if (step != TemperatureSteps.Count)
                    _temperatureDataByEnum[step].Format(step.ToString(), _temperatureToggle);
        }
    }
    protected override string Description
    => "• Mods (small and big) that didn't get their own section yet :)";
    protected override string SectionOverride
    => "";
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _skipStartupVideos.Value = true;
                _titleScreenHideCharacters.Value = TitleScreenCharacterVisibility.Randomize;
                _enableCheats.Value = false;
                _enableCheatsHotkey.Value = KeyCode.Keypad0.ToString();
                _removeCoopScaling.Value = true;
                _healEnemiesOnLoad.Value = true;
                _multiplicativeStacking.Value = true;
                _armorTrainingPenaltyReduction.Value = 50;
                _applyArmorTrainingToManaCost.Value = true;
                _refillArrowsFromInventory.Value = true;
                _rentDuration.Value = 120;
                _itemActionDropOne.Value = true;
                _temperatureToggle.Value = true;
                {
                    _temperatureDataByEnum[TemperatureSteps.Coldest].Value = new Vector2(-50, 50 - (50 + 1));
                    _temperatureDataByEnum[TemperatureSteps.VeryCold].Value = new Vector2(-40, 50 - (50 - 1));
                    _temperatureDataByEnum[TemperatureSteps.Cold].Value = new Vector2(-30, 50 - (25 + 1));
                    _temperatureDataByEnum[TemperatureSteps.Fresh].Value = new Vector2(-20, 50 - (10 + 1));
                    _temperatureDataByEnum[TemperatureSteps.Neutral].Value = new Vector2(0, 50);
                    _temperatureDataByEnum[TemperatureSteps.Warm].Value = new Vector2(+20, 50 + (10 + 1));
                    _temperatureDataByEnum[TemperatureSteps.Hot].Value = new Vector2(+30, 50 + (25 + 1));
                    _temperatureDataByEnum[TemperatureSteps.VeryHot].Value = new Vector2(+40, 50 + (50 - 1));
                    _temperatureDataByEnum[TemperatureSteps.Hottest].Value = new Vector2(+50, 50 + (50 + 1));
                }
                break;
        }
    }
    public void OnUpdate()
    {
        if (_enableCheatsHotkey.Value.ToKeyCode().Pressed())
            _enableCheats.Value = !_enableCheats;
    }

    // Utility
    private static bool ShouldArmorSlotBeHidden(EquipmentSlot.EquipmentSlotIDs slot)
    => slot == EquipmentSlot.EquipmentSlotIDs.Helmet && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Head)
    || slot == EquipmentSlot.EquipmentSlotIDs.Chest && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Chest)
    || slot == EquipmentSlot.EquipmentSlotIDs.Foot && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Feet);
    private static bool HasLearnedArmorTraining(Character character)
    => character.Inventory.SkillKnowledge.IsItemLearned(ARMOR_TRAINING_ID);
    public static bool IsAnythingEquipped(EquipmentSlot slot)
    => slot != null && slot.HasItemEquipped;
    public static bool IsNotLeftHandUsedBy2H(EquipmentSlot slot)
    => !(slot.SlotType == EquipmentSlot.EquipmentSlotIDs.LeftHand && slot.EquippedItem.TwoHanded);
    private static bool TryApplyMultiplicativeStacking(CharacterEquipment equipment, ref float result, Func<EquipmentSlot, float> getStatValue, bool invertedPositivity = false, bool applyArmorTraining = false)
    {
        #region quit
        if (!_multiplicativeStacking)
            return true;
        #endregion

        float invCoeff = invertedPositivity ? -1f : +1f;
        bool canApplyArmorTraining = applyArmorTraining && HasLearnedArmorTraining(equipment.m_character);

        result = 1f;
        foreach (var slot in equipment.m_equipmentSlots)
            if (IsAnythingEquipped(slot) && IsNotLeftHandUsedBy2H(slot))
            {
                float armorTrainingCoeff = canApplyArmorTraining && getStatValue(slot) > 0f ? 1f - _armorTrainingPenaltyReduction / 100f : 1f;
                result *= 1f + getStatValue(slot) / 100f * invCoeff * armorTrainingCoeff;
            }
        result -= 1f;
        result *= invCoeff;
        return false;
    }
    private static void UpdateBaseStaminaRegen(CharacterStats characterStats)
    => characterStats.m_staminaRegen.BaseValue = _baseStaminaRegen;
    private static void TryUpdateTemperatureData()
    {
        #region quit
        if (!_temperatureToggle)
            return;
        #endregion

        if (EnvironmentConditions.Instance.TryNonNull(out var environmentConditions))
            foreach (var step in InternalUtility.GetEnumValues<TemperatureSteps>())
                if (step != TemperatureSteps.Count)
                {
                    environmentConditions.BodyTemperatureImpactPerStep[step] = _temperatureDataByEnum[step].Value.x;
                    environmentConditions.TemperatureCaps[step] = _temperatureDataByEnum[step].Value.y;
                }
    }

    // Hooks
    // Title screen
    [HarmonyPostfix, HarmonyPatch(typeof(TitleScreenLoader), nameof(TitleScreenLoader.LoadTitleScreenCoroutine))]
    private static IEnumerator TitleScreenLoader_LoadTitleScreenCoroutine_Post(IEnumerator original, TitleScreenLoader __instance)
    {
        while (original.MoveNext())
            yield return original.Current;

        #region quit
        if (_titleScreenHideCharacters.Value == TitleScreenCharacterVisibility.Enable)
            yield break;
        #endregion

        bool state = true;
        switch (_titleScreenHideCharacters.Value)
        {
            case TitleScreenCharacterVisibility.Disable: state = false; break;
            case TitleScreenCharacterVisibility.Randomize: state = System.DateTime.Now.Ticks % 2 == 0; break;
        }

        foreach (var characterVisuals in __instance.transform.GetAllComponentsInHierarchy<CharacterVisuals>())
            characterVisuals.GOSetActive(state);
    }

    // Skip startup video
    [HarmonyPrefix, HarmonyPatch(typeof(StartupVideo), nameof(StartupVideo.Awake))]
    private static void StartupVideo_Awake_Pre()
    => StartupVideo.HasPlayedOnce = _skipStartupVideos.Value;
    // Drop one
    [HarmonyPostfix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.GetActiveActions))]
    private static void ItemDisplayOptionPanel_GetActiveActions_Post(ItemDisplayOptionPanel __instance, ref List<int> __result)
    {
        #region quit
        //!itemDisplay.RefItem.TryNonNull(out var item) || item.MoveStackAsOne  
        if (!_itemActionDropOne || __instance == null ||
        !__instance.m_activatedItemDisplay.TryNonNull(out var itemDisplay)
        || itemDisplay.StackCount <= 1)
            return;
        #endregion

        __result.Add(DROP_ONE_ACTION_ID);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.GetActionText))]
    private static bool ItemDisplayOptionPanel_GetActionText_Pre(ItemDisplayOptionPanel __instance, ref string __result, ref int _actionID)
    {
        #region quit
        if (_actionID != DROP_ONE_ACTION_ID)
            return true;
        #endregion

        __result = DROP_ONE_ACTION_TEXT;
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.ActionHasBeenPressed))]
    private static bool ItemDisplayOptionPanel_ActionHasBeenPressed_Pre(ItemDisplayOptionPanel __instance, ref int _actionID)
    {
        #region quit
        if (_actionID != DROP_ONE_ACTION_ID)
            return true;
        #endregion

        __instance.m_activatedItemDisplay.OnConfirmDropStack(1);
        return false;
    }

    // Temperature data
    [HarmonyPostfix, HarmonyPatch(typeof(EnvironmentConditions), nameof(EnvironmentConditions.Start))]
    private static void EnvironmentConditions_Start_Post(EnvironmentConditions __instance)
    => TryUpdateTemperatureData();

    // Stamina regen
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterStats), nameof(PlayerCharacterStats.OnStart))]
    private static void PlayerCharacterStats_OnStart_Post(PlayerCharacterStats __instance)
    => UpdateBaseStaminaRegen(__instance);

    // Load arrows from inventory
    [HarmonyPrefix, HarmonyPatch(typeof(WeaponLoadoutItem), nameof(WeaponLoadoutItem.ReduceShotAmount))]
    private static bool WeaponLoadoutItem_ReduceShotAmount_Pre(WeaponLoadoutItem __instance)
    {
        #region quit
        if (!_refillArrowsFromInventory
        || __instance.AmunitionType != WeaponLoadout.CompatibleAmmunitionType.WeaponType
        || __instance.CompatibleEquipment != Weapon.WeaponType.Arrow)
            return true;
        #endregion

        CharacterInventory inventory = __instance.m_projectileWeapon.OwnerCharacter.Inventory;
        int ammoID = inventory.GetEquippedAmmunition().ItemID;

        Item ammo = null;
        if (ammo == null && inventory.EquippedBag != null)
            ammo = inventory.EquippedBag.Container.GetItemFromID(ammoID);
        if (ammo == null)
            ammo = inventory.Pouch.GetItemFromID(ammoID);
        if (ammo == null)
            return true;

        ammo.RemoveQuantity(1);
        return false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CharacterInventory), nameof(CharacterInventory.GetAmmunitionCount))]
    private static void CharacterInventory_GetAmmunitionCount_Post(CharacterInventory __instance, ref int __result)
    {
        #region quit
        if (!_refillArrowsFromInventory || __result == 0)
            return;
        #endregion

        __result += __instance.ItemCount(__instance.GetEquippedAmmunition().ItemID);
    }

    // Inn rent duration
    [HarmonyPrefix, HarmonyPatch(typeof(QuestEventData), nameof(QuestEventData.HasExpired))]
    private static void QuestEventData_HasExpired_Pre(QuestEventData __instance, ref int _gameHourAllowed)
    {
        if (__instance.m_signature.ParentSection.Name == INNS_QUEST_FAMILY_NAME)
            _gameHourAllowed = _rentDuration;
    }

    // Multiplicative stacking
    [HarmonyPrefix, HarmonyPatch(typeof(Stat), nameof(Stat.GetModifier))]
    private static bool Stat_GetModifier_Pre(Stat __instance, ref float __result, ref IList<Tag> _tags, ref int baseModifier)
    {
        #region quit
        if (!_multiplicativeStacking)
            return true;
        #endregion

        DictionaryExt<string, StatStack> multipliers = __instance.m_multiplierStack;
        __result = baseModifier;
        for (int i = 0; i < multipliers.Count; i++)
        {
            if (multipliers.Values[i].HasEnded)
                multipliers.RemoveAt(i--);
            else if (multipliers.Values[i].SameTags(_tags))
            {
                float value = multipliers.Values[i].EffectiveValue;
                if (!__instance.NullifyPositiveStat || value <= 0f)
                    __result *= 1f + value;
            }
        }
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CharacterEquipment), nameof(CharacterEquipment.GetTotalMovementModifier))]
    private static bool CharacterEquipment_GetTotalMovementModifier_Pre(CharacterEquipment __instance, ref float __result)
    => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.MovementPenalty, true, true);

    [HarmonyPrefix, HarmonyPatch(typeof(CharacterEquipment), nameof(CharacterEquipment.GetTotalStaminaUseModifier))]
    private static bool CharacterEquipment_GetTotalStaminaUseModifier_Pre(CharacterEquipment __instance, ref float __result)
    => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.StaminaUsePenalty, false, true);

    [HarmonyPrefix, HarmonyPatch(typeof(CharacterEquipment), nameof(CharacterEquipment.GetTotalManaUseModifier))]
    private static bool CharacterEquipment_GetTotalManaUseModifier_Pre(CharacterEquipment __instance, ref float __result)
    => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.ManaUseModifier, false, _applyArmorTrainingToManaCost);

    // Hide armor slots
    [HarmonyPrefix, HarmonyPatch(typeof(CharacterVisuals), nameof(CharacterVisuals.EquipVisuals))]
    private static void CharacterVisuals_EquipVisuals_Pre(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
    {
        #region quit
        if (_armorSlotsToHide == ArmorSlots.None)
            return;
        #endregion

        // save original hide flags for postfix
        __state = new bool[3];
        __state[0] = _visuals.HideFace;
        __state[1] = _visuals.HideHair;
        __state[2] = _visuals.DisableDefaultVisuals;
        // override hide flags
        if (ShouldArmorSlotBeHidden(_slotID))
        {
            _visuals.HideFace = false;
            _visuals.HideHair = false;
            _visuals.DisableDefaultVisuals = false;
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CharacterVisuals), nameof(CharacterVisuals.EquipVisuals))]
    private static void CharacterVisuals_EquipVisuals_Post(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
    {
        #region quit
        if (_armorSlotsToHide == ArmorSlots.None)
            return;
        #endregion

        // hide chosen pieces of armor
        if (ShouldArmorSlotBeHidden(_slotID))
            _visuals.Hide();

        // restore original hide flags
        _visuals.HideFace = __state[0];
        _visuals.HideHair = __state[1];
        _visuals.DisableDefaultVisuals = __state[2];
    }

    // Remove co-op scaling
    [HarmonyPrefix, HarmonyPatch(typeof(CoopStats), nameof(CoopStats.ApplyToCharacter))]
    private static bool CoopStats_ApplyToCharacter_Pre()
    => !_removeCoopScaling;

    [HarmonyPrefix, HarmonyPatch(typeof(CoopStats), nameof(CoopStats.RemoveFromCharacter))]
    private static bool CoopStats_RemoveFromCharacter_Pre()
    => !_removeCoopScaling;

    // Enemy health reset time
    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.LoadCharSave))]
    private static void Character_LoadCharSave_Pre(Character __instance)
    {
        #region quit
        if (!__instance.IsEnemy())
            return;
        #endregion

        __instance.HoursToHealthReset = _healEnemiesOnLoad ? 0 : DEFAULT_ENEMY_HEALTH_RESET_HOURS;
    }
    // Mark items with legacy upgrades
    [HarmonyPrefix, HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.RefreshEnchantedIcon))]
    private static bool ItemDisplay_RefreshEnchantedIcon_Pre(ItemDisplay __instance)
    {
        #region quit
        if (!_highlightItemsWithLegacyUpgrade
        || __instance.m_refItem == null
        || __instance.m_imgEnchantedIcon == null
        || __instance.m_refItem is Skill)
            return true;
        #endregion

        // Cache
        Image icon = __instance.FindChild<Image>("Icon");
        Image border = icon.FindChild<Image>("border");
        Image indicator = __instance.m_imgEnchantedIcon;

        //Defaults
        icon.color = Color.white;
        border.color = Color.white;
        indicator.GOSetActive(false);

        // Quit
        if (__instance.m_refItem.LegacyItemID <= 0)
            return true;

        // Custom
        border.color = _legacyItemUpgradeColor.Value.NewA(1f);
        indicator.color = _legacyItemUpgradeColor;
        indicator.rectTransform.pivot = 1f.ToVector2();
        indicator.rectTransform.localScale = new Vector2(1.5f, 1.5f);
        indicator.GOSetActive(true);
        return false;
    }

}



/*
*         [Flags]
    private enum EquipmentStats
    {
        None = 0,
        All = ~0,

        Damage = 1 << 1,
        ImpactDamage = 1 << 2,
        Resistance = 1 << 3,
        ImpactResistance = 1 << 4,
        CorruptionResistance = 1 << 5,
        MovementSpeed = 1 << 6,
        StaminaCost = 1 << 7,
        ManaCost = 1 << 8,
        CooldownReduction = 1 << 9,
    }
*/

/* POUCH
private const float POUCH_CAPACITY = 10f;
static private ModSetting<bool> _pouchToggle;
static private ModSetting<int> _pouchCapacity;
static private ModSetting<bool> _allowOverCapacity;

_pouchToggle = CreateSetting(nameof(_pouchToggle), false);
_pouchCapacity = CreateSetting(nameof(_pouchCapacity), POUCH_CAPACITY.Round(), IntRange(0, 100));
_allowOverCapacity = CreateSetting(nameof(_allowOverCapacity), true);

_pouchToggle.Format("Pouch");
using(Indent)
{
_pouchCapacity.Format("Pouch size", _pouchToggle);
_allowOverCapacity.Format("Allow over capacity", _pouchToggle);
}

[HarmonyPostfix, HarmonyPatch(typeof(CharacterInventory), nameof(CharacterInventory.ProcessStart))]
static void CharacterInventory_ProcessStart_Post(CharacterInventory __instance, ref Character ___m_character)
{
#region quit
if (!_pouchToggle)
    return;
#endregion

ItemContainer pouch = __instance.Pouch;
if (___m_character.IsPlayer() && pouch != null)
{
    pouch.SetField("m_baseContainerCapacity", _pouchCapacity.Value, typeof(ItemContainer));
    pouch.AllowOverCapacity = _allowOverCapacity;
}
}
*/