using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;



/* TO DO:
 * - hide armor extras (like scarf)
 * - prevent dodging right after hitting
 */
namespace ModPack
{
    public class Various : AMod
    {
        #region const
        private const float DEFAULT_ENEMY_HEALTH_RESET_HOURS = 24f;   // Character.HoursToHealthReset
        private const int ARMOR_TRAINING_ID = 8205220;
        private const int PRIMITIVE_SATCHEL_CAPACITY = 25;
        private const int TRADER_BACKPACK = 100;
        #endregion
        #region enum
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
        static private ModSetting<bool> _enableCheats;
        static private ModSetting<bool> _skipStartupVideos;
        static private ModSetting<ArmorSlots> _armorSlotsToHide;
        static private ModSetting<bool> _removeCoopScaling;
        static private ModSetting<bool> _removeDodgeInvulnerability;
        static private ModSetting<bool> _healEnemiesOnLoad;
        static private ModSetting<bool> _multiplicativeStacking;
        static private ModSetting<int> _armorTrainingPenaltyReduction;
        static private ModSetting<bool> _applyArmorTrainingToManaCost;
        static private ModSetting<bool> _loadArrowsFromInventory;
        static private ModSetting<Vector2> _remapBackpackCapacities;

        static private ModSetting<bool> _markItemsWithLegacyUpgrade;
        static private ModSetting<bool> _allowDodgeAnimationCancelling;
        static private ModSetting<bool> _allowPushKickRemoval;
        static private ModSetting<bool> _allowTargetingPlayers;
        override protected void Initialize()
        {
            _enableCheats = CreateSetting(nameof(_enableCheats), false);
            _skipStartupVideos = CreateSetting(nameof(_skipStartupVideos), false);
            _armorSlotsToHide = CreateSetting(nameof(_armorSlotsToHide), ArmorSlots.None);
            _removeCoopScaling = CreateSetting(nameof(_removeCoopScaling), false);
            _removeDodgeInvulnerability = CreateSetting(nameof(_removeDodgeInvulnerability), false);
            _healEnemiesOnLoad = CreateSetting(nameof(_healEnemiesOnLoad), false);
            _multiplicativeStacking = CreateSetting(nameof(_multiplicativeStacking), false);
            _armorTrainingPenaltyReduction = CreateSetting(nameof(_armorTrainingPenaltyReduction), 50, IntRange(0, 100));
            _applyArmorTrainingToManaCost = CreateSetting(nameof(_applyArmorTrainingToManaCost), false);
            _loadArrowsFromInventory = CreateSetting(nameof(_loadArrowsFromInventory), false);
            _remapBackpackCapacities = CreateSetting(nameof(_remapBackpackCapacities), new Vector2(PRIMITIVE_SATCHEL_CAPACITY, TRADER_BACKPACK));

            AddEventOnConfigClosed(() =>
            {
                Global.CheatsEnabled = _enableCheats;
            });

            // WIP
            _markItemsWithLegacyUpgrade = CreateSetting(nameof(_markItemsWithLegacyUpgrade), false);
            _allowDodgeAnimationCancelling = CreateSetting(nameof(_allowDodgeAnimationCancelling), false);
            _allowPushKickRemoval = CreateSetting(nameof(_allowPushKickRemoval), false);
            _allowTargetingPlayers = CreateSetting(nameof(_allowTargetingPlayers), false);
        }
        override protected void SetFormatting()
        {
            _enableCheats.Format("Enable cheats");
            _enableCheats.Description = "aka Debug Mode";
            _skipStartupVideos.Format("Skip startup videos");
            _skipStartupVideos.Description = "Saves ~3 seconds each time you launch the game";
            _armorSlotsToHide.Format("Armor slots to hide");
            _armorSlotsToHide.Description = "Used to hide ugly helmets (purely visual)";

            _removeCoopScaling.Format("Remove multiplayer scaling");
            _removeCoopScaling.Description = "Enemies in multiplayer will have the same stats as in singleplayer";
            _removeDodgeInvulnerability.Format("Remove dodge invulnerability");
            _removeDodgeInvulnerability.Description = "You can get hit during the dodge animation\n" +
                                                      "(even without a backpack)";
            _healEnemiesOnLoad.Format("Heal enemies on load");
            _healEnemiesOnLoad.Description = "Every loading screen fully heals all enemies";
            _multiplicativeStacking.Format("Multiplicative stacking");
            _multiplicativeStacking.Description = "Some stats will stack multiplicatively instead of additvely\n" +
                                                  "(movement speed, stamina cost, mana cost)";
            Indent++;
            {
                _armorTrainingPenaltyReduction.Format("\"Armor Training\" penalty reduction", _multiplicativeStacking);
                _armorTrainingPenaltyReduction.Description = "How much of equipment's movement speed and stamina cost penalties should \"Armor Training\" ignore";
                _applyArmorTrainingToManaCost.Format("\"Armor Training\" affects mana cost", _multiplicativeStacking);
                _applyArmorTrainingToManaCost.Description = "\"Armor Training\" will also lower equipment's mana cost penalties";
                Indent--;
            }
            _loadArrowsFromInventory.Format("Load arrows from inventory");
            _loadArrowsFromInventory.Description = "Whenever you shoot your bow, the missing arrow is automatically replace with one from backpack or pouch (in that order).";
            _remapBackpackCapacities.Format("Remap backpack capacities");
            _remapBackpackCapacities.Description = "X   -   Primitive Satchel's capacity\n" +
                                                   "Y   -   Trader Backpack's capacity\n" +
                                                   "(all other backpacks will have their capacities scaled accordingly)";

            _markItemsWithLegacyUpgrade.Format("[WIP] Mark items with legacy upgrades");
            _markItemsWithLegacyUpgrade.IsAdvanced = true;
            _allowDodgeAnimationCancelling.Format("[WIP] Allow dodge to cancel actions");
            _allowDodgeAnimationCancelling.Description = "Cancelling certain animations might lead to glitches";
            _allowDodgeAnimationCancelling.IsAdvanced = true;
            _allowPushKickRemoval.Format("[WIP] Allow \"Push Kick\" removal");
            _allowPushKickRemoval.Description = "For future skill trees mod\n" +
                                                "Normally, player data won't be saved if they don't have the \"Push Kick\" skill";
            _allowPushKickRemoval.IsAdvanced = true;
            _allowTargetingPlayers.Format("[WIP] Allow targeting players");
            _allowTargetingPlayers.Description = "For future co-op skills mod";
            _allowTargetingPlayers.IsAdvanced = true;
        }
        override protected string Description
        => "• Mods (small and big) that didn't get their own section yet :)";
        override protected string SectionOverride
        => SECTION_VARIOUS;

        // Remap backpack capacities
        [HarmonyPatch(typeof(ItemContainer), "ContainerCapacity", MethodType.Getter), HarmonyPostfix]
        static void ItemContainer_ContainerCapacity_Post(ref ItemContainer __instance, ref float __result)
        {
            if (__instance.RefBag == null || __instance.m_baseContainerCapacity <= 0)
                return;

            __result = __result.Map(PRIMITIVE_SATCHEL_CAPACITY, TRADER_BACKPACK,
                                    _remapBackpackCapacities.Value.x, _remapBackpackCapacities.Value.y).Round();
        }

        // Mark items with legacy upgrades
        [HarmonyPatch(typeof(ItemDisplay), "RefreshEnchantedIcon"), HarmonyPrefix]
        static bool ItemDisplay_RefreshEnchantedIcon_Pre(ItemDisplay __instance)
        {
            #region quit
            if (!_markItemsWithLegacyUpgrade || __instance.m_refItem == null || __instance.m_imgEnchantedIcon == null)
                return true;
            #endregion

            // Cache
            //Image icon = __instance.FindChild<Image>("Icon");
            //Image border = icon.FindChild<Image>("border");
            Image indicator = __instance.m_imgEnchantedIcon;

            // Default
            indicator.GOSetActive(false);

            // Quit
            if (__instance.m_refItem.LegacyItemID <= 0)
                return true;

            // Custom
            indicator.color = Color.red;
            indicator.rectTransform.pivot = 1f.ToVector2();
            indicator.rectTransform.localScale = new Vector2(1.5f, 1.5f);
            indicator.GOSetActive(true);
            return false;
        }

        // Load arrows from inventory
        [HarmonyPatch(typeof(WeaponLoadoutItem), "ReduceShotAmount"), HarmonyPrefix]
        static bool WeaponLoadoutItem_ReduceShotAmount_Pre(WeaponLoadoutItem __instance)
        {
            #region quit
            if (!_loadArrowsFromInventory
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

        [HarmonyPatch(typeof(CharacterInventory), "GetAmmunitionCount"), HarmonyPostfix]
        static void CharacterInventory_GetAmmunitionCount_Post(ref CharacterInventory __instance, ref int __result)
        {
            #region quit
            if (!_loadArrowsFromInventory || __result == 0)
                return;
            #endregion

            __result += __instance.ItemCount(__instance.GetEquippedAmmunition().ItemID);
        }

        // Multiplicative stacking
        static private bool HasLearnedArmorTraining(Character character)
        => character.Inventory.SkillKnowledge.IsItemLearned(ARMOR_TRAINING_ID);
        static public bool IsAnythingEquipped(EquipmentSlot slot)
        => slot != null && slot.HasItemEquipped;
        static public bool IsNotLeftHandUsedBy2H(EquipmentSlot slot)
        => !(slot.SlotType == EquipmentSlot.EquipmentSlotIDs.LeftHand && slot.EquippedItem.TwoHanded);
        static private bool TryApplyMultiplicativeStacking(CharacterEquipment equipment, ref float result, Func<EquipmentSlot, float> getStatValue, bool invertedPositivity = false, bool applyArmorTraining = false)
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

        [HarmonyPatch(typeof(Stat), "GetModifier"), HarmonyPrefix]
        static bool Stat_GetModifier_Pre(ref Stat __instance, ref float __result, ref IList<Tag> _tags, ref int baseModifier)
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
                        __result *= (1f + value);
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalMovementModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalMovementModifier_Pre(ref CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.MovementPenalty, true, true);

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalStaminaUseModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalStaminaUseModifier_Pre(ref CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.StaminaUsePenalty, false, true);

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalManaUseModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalManaUseModifier_Pre(ref CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.ManaUseModifier, false, _applyArmorTrainingToManaCost);

        // Skip startup video
        [HarmonyPatch(typeof(StartupVideo), "Awake"), HarmonyPrefix]
        static bool StartupVideo_Awake_Pre()
        {
            StartupVideo.HasPlayedOnce = _skipStartupVideos.Value;
            return true;
        }

        // Hide armor slots
        static private bool ShouldArmorSlotBeHidden(EquipmentSlot.EquipmentSlotIDs slot)
        => slot == EquipmentSlot.EquipmentSlotIDs.Helmet && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Head)
        || slot == EquipmentSlot.EquipmentSlotIDs.Chest && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Chest)
        || slot == EquipmentSlot.EquipmentSlotIDs.Foot && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Feet);

        [HarmonyPatch(typeof(CharacterVisuals), "EquipVisuals"), HarmonyPrefix]
        static bool CharacterVisuals_EquipVisuals_Pre(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
        {
            #region quit
            if (_armorSlotsToHide == ArmorSlots.None)
                return true;
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
            return true;
        }

        [HarmonyPatch(typeof(CharacterVisuals), "EquipVisuals"), HarmonyPostfix]
        static void CharacterVisuals_EquipVisuals_Post(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
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
        [HarmonyPatch(typeof(CoopStats), "ApplyToCharacter"), HarmonyPrefix]
        static bool CoopStats_ApplyToCharacter_Pre()
    => !_removeCoopScaling;

        [HarmonyPatch(typeof(CoopStats), "RemoveFromCharacter"), HarmonyPrefix]
        static bool CoopStats_RemoveFromCharacter_Pre()
        => !_removeCoopScaling;

        // Remove dodge invulnerability
        [HarmonyPatch(typeof(Character), "DodgeStep"), HarmonyPostfix]
        static void Character_DodgeStep_Post(ref Hitbox[] ___m_hitboxes, ref int _step)
        {
            #region quit
            if (!_removeDodgeInvulnerability)
                return;
            #endregion

            if (_step > 0 && ___m_hitboxes != null)
                foreach (var hitbox in ___m_hitboxes)
                    hitbox.gameObject.SetActive(true);
        }

        // Enemy health reset time
        [HarmonyPatch(typeof(Character), "LoadCharSave"), HarmonyPrefix]
        static bool Character_LoadCharSave_Pre(Character __instance)
        {
            #region quit
            if (!__instance.IsEnemy())
                return true;
            #endregion

            __instance.HoursToHealthReset = _healEnemiesOnLoad ? 0 : DEFAULT_ENEMY_HEALTH_RESET_HOURS;
            return true;
        }

        // Dodge animation cancelling
        [HarmonyPatch(typeof(Character), "DodgeInput", new[] { typeof(Vector3) }), HarmonyPrefix]
        static bool Character_SpellCastAnim_Post(ref int ___m_dodgeAllowedInAction, ref Character.HurtType ___m_hurtType)
        {
            #region quit
            if (!_allowDodgeAnimationCancelling)
                return true;
            #endregion

            if (___m_hurtType == Character.HurtType.NONE)
                ___m_dodgeAllowedInAction = 1;
            return true;
        }

        // Push kick removal
        [HarmonyPatch(typeof(CharacterSave), "IsValid", MethodType.Getter), HarmonyPrefix]
        static bool CharacterSave_IsValid_Getter_Pre(ref bool __result)
        {
            #region quit
            if (!_allowPushKickRemoval.Value)
                return true;
            #endregion

            __result = true;
            return false;
        }

        // Target other players
        [HarmonyPatch(typeof(TargetingSystem), "IsTargetable", new[] { typeof(Character) }), HarmonyPrefix]
        static bool TargetingSystem_IsTargetable_Pre(ref TargetingSystem __instance, ref bool __result, ref Character _char)
        {
            #region quit
            if (!_allowTargetingPlayers)
                return true;
            #endregion

            if (_char.Faction == Character.Factions.Player && _char != __instance.m_character)
            {
                __result = true;
                return false;
            }
            return true;
        }
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
Indent++;
{
    _pouchCapacity.Format("Pouch size", _pouchToggle);
    _allowOverCapacity.Format("Allow over capacity", _pouchToggle);
    Indent--;
}

[HarmonyPatch(typeof(CharacterInventory), "ProcessStart"), HarmonyPostfix]
static void CharacterInventory_ProcessStart_Post(ref CharacterInventory __instance, ref Character ___m_character)
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

/* Extra Controller Quickslots
[HarmonyPatch(typeof(QuickSlotPanel), "InitializeQuickSlotDisplays"), HarmonyPostfix]
static void QuickSlotPanel_InitializeQuickSlotDisplays_Post(ref QuickSlotPanel __instance, ref QuickSlotDisplay[] ___m_quickSlotDisplays)
{
    #region quit
    if (!_extraControllerQuickslots)
        return;
    #endregion

    if (__instance.name == BOTH_TRIGGERS_PANEL_NAME)
        for (int i = 0; i < ___m_quickSlotDisplays.Length; i++)
            ___m_quickSlotDisplays[i].RefSlotID = i + 8;
}

[HarmonyPatch(typeof(LocalCharacterControl), "UpdateQuickSlots"), HarmonyPostfix]
static void LocalCharacterControl_UpdateQuickSlots_Pre(ref Character ___m_character)
{
    #region quit
    if (!_extraControllerQuickslots)
        return;
    if (___m_character == null || ___m_character.QuickSlotMngr == null || ___m_character.CharacterUI.IsMenuFocused)
        return;
    #endregion

    int playerID = ___m_character.OwnerPlayerSys.PlayerID;

    if (QuickSlotInstant9(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(8);
    else if (QuickSlotInstant10(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(9);
    else if (QuickSlotInstant11(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(10);
    else if (QuickSlotInstant12(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(11);
}

        static void AddQuickSlot12()
{
    foreach (var localPlayer in Utility.LocalPlayers)
    {
        Transform quickSlotsHolder = localPlayer.ControlledCharacter.GetComponent<CharacterQuickSlotManager>().QuickslotTrans;
        if (quickSlotsHolder.Find(QUICKSLOT_12_NAME) != null)
            continue;

        QuickSlot newQuickSlot = GameObject.Instantiate(quickSlotsHolder.Find("1"), quickSlotsHolder).GetComponent<QuickSlot>();
        newQuickSlot.name = QUICKSLOT_12_NAME;
        foreach (var quickSlot in quickSlotsHolder.GetComponents<QuickSlot>())
            quickSlot.ItemQuickSlot = false;
        localPlayer.ControlledCharacter.QuickSlotMngr.Awake();
    }
}

static void DrawExtraControllerQuickslotSwitcher()
{
    foreach (var localPlayer in Utility.LocalPlayers)
    {
        Transform panel = localPlayer.ControlledCharacter.CharacterUI.QuickSlotMenu.FindChild("PanelSwitcher/Controller/LT-RT").transform;
        if (panel.Find(BOTH_TRIGGERS_PANEL_NAME) != null)
            continue;

        // Instantiate
        Transform LT = panel.Find("LT").transform;
        Transform RT = panel.Find("RT").transform;
        foreach (var quickslotPlacer in LT.GetComponentsInChildren<EditorQuickSlotDisplayPlacer>())
            quickslotPlacer.IsTemplate = true;
        Transform LTRT = GameObject.Instantiate(LT.gameObject, panel).transform;
        LTRT.name = BOTH_TRIGGERS_PANEL_NAME;
        Transform imgLT = LTRT.Find("imgLT");
        Transform imgRT = GameObject.Instantiate(RT.Find("imgRT"), LTRT).transform;
        imgRT.name = "imgRT";

        // Change
        panel.Find("LeftDecoration").gameObject.SetActive(false);
        panel.Find("RightDecoration").gameObject.SetActive(false);
        LT.localPosition = new Vector2(-300f, 0);
        RT.localPosition = new Vector2(0f, 0);
        LTRT.localPosition = new Vector2(+300f, 0);
        imgLT.localPosition = new Vector2(-22.5f, 22.5f);
        imgRT.localPosition = new Vector2(+22.5f, 22.5f);
    }
}

*/
