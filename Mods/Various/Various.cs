using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;
using System.Collections;



/* TO DO:
 * - hide armor extras (like scarf)
 * - prevent dodging right after hitting
 */
namespace ModPack
{
    public class Various : AMod, IUpdatable
    {
        #region const
        private const int DROP_ONE_ACTION_ID = -2;
        private const string DROP_ONE_ACTION_TEXT = "Drop one";
        static private readonly Dictionary<AreaManager.AreaEnum, string> STASH_UIDS_BY_CITY = new Dictionary<AreaManager.AreaEnum, string>
        {
            [AreaManager.AreaEnum.CierzoVillage] = "ImqRiGAT80aE2WtUHfdcMw",
            [AreaManager.AreaEnum.Berg] = "ImqRiGAT80aE2WtUHfdcMw",
            [AreaManager.AreaEnum.Monsoon] = "ImqRiGAT80aE2WtUHfdcMw",
            [AreaManager.AreaEnum.Levant] = "ZbPXNsPvlUeQVJRks3zBzg",
            [AreaManager.AreaEnum.Harmattan] = "ImqRiGAT80aE2WtUHfdcMw",
            [AreaManager.AreaEnum.NewSirocco] = "IqUugGqBBkaOcQdRmhnMng",
        };
        static private readonly Dictionary<AreaManager.AreaEnum, string> SOROBOREAN_CARAVANNER_UIDS_BY_CITY = new Dictionary<AreaManager.AreaEnum, string>
        {
            [AreaManager.AreaEnum.CierzoVillage] = "G_GyAVjRWkq8e2L8WP4TgA",
            [AreaManager.AreaEnum.Berg] = "-MSrkT502k63y3CV2j98TQ",
            [AreaManager.AreaEnum.Monsoon] = "9GAbQm8Ekk23M0LohPF7dg",
            [AreaManager.AreaEnum.Levant] = "Tbq1PxS_iUO6vhnr7aGUhg",
            [AreaManager.AreaEnum.Harmattan] = "WN0BVRJwtE-goNLvproxgw",
            [AreaManager.AreaEnum.NewSirocco] = "-MSrkT502k63y3CV2j98TQ",
        };
        private const float DEFAULT_ENEMY_HEALTH_RESET_HOURS = 24f;   // Character.HoursToHealthReset
        private const int ARMOR_TRAINING_ID = 8205220;
        static private readonly Dictionary<TemperatureSteps, Vector2> DEFAULT_TEMPERATURE_DATA_BY_ENUM = new Dictionary<TemperatureSteps, Vector2>
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
        [Flags]
        private enum ArmorSlots
        {
            None = 0,
            Head = 1 << 1,
            Chest = 1 << 2,
            Feet = 1 << 3,
        }
        [Flags]
        private enum TitleScreens
        {
            Vanilla = 1 << 1,
            TheSoroboreans = 1 << 2,
            TheThreeBrothers = 1 << 3,
        }
        private enum TitleScreenCharacterVisibility
        {
            Enable = 1,
            Disable = 2,
            Randomize = 3,
        }

        #endregion

        // Settings
        static private ModSetting<bool> _enableCheats;
        static private ModSetting<string> _enableCheatsHotkey;
        static private ModSetting<bool> _skipStartupVideos;
        static private ModSetting<TitleScreens> _titleScreenRandomize;
        static private ModSetting<TitleScreenCharacterVisibility> _titleScreenHideCharacters;
        static private ModSetting<ArmorSlots> _armorSlotsToHide;
        static private ModSetting<bool> _removeCoopScaling;
        static private ModSetting<bool> _removeDodgeInvulnerability;
        static private ModSetting<bool> _healEnemiesOnLoad;
        static private ModSetting<bool> _multiplicativeStacking;
        static private ModSetting<int> _armorTrainingPenaltyReduction;
        static private ModSetting<bool> _applyArmorTrainingToManaCost;
        static private ModSetting<bool> _loadArrowsFromInventory;
        static private ModSetting<float> _baseStaminaRegen;
        static private ModSetting<bool> _craftFromStash;
        static private ModSetting<bool> _displayStashAmount;
        static private ModSetting<bool> _displayPricesInStash;
        static private ModSetting<bool> _itemActionDropOne;
        static private ModSetting<bool> _temperatureToggle;
        static private Dictionary<TemperatureSteps, ModSetting<Vector2>> _temperatureDataByEnum;
        override protected void Initialize()
        {
            _enableCheats = CreateSetting(nameof(_enableCheats), false);
            _enableCheatsHotkey = CreateSetting(nameof(_enableCheatsHotkey), "");
            _skipStartupVideos = CreateSetting(nameof(_skipStartupVideos), false);
            _armorSlotsToHide = CreateSetting(nameof(_armorSlotsToHide), ArmorSlots.None);
            _removeCoopScaling = CreateSetting(nameof(_removeCoopScaling), false);
            _removeDodgeInvulnerability = CreateSetting(nameof(_removeDodgeInvulnerability), false);
            _healEnemiesOnLoad = CreateSetting(nameof(_healEnemiesOnLoad), false);
            _multiplicativeStacking = CreateSetting(nameof(_multiplicativeStacking), false);
            _armorTrainingPenaltyReduction = CreateSetting(nameof(_armorTrainingPenaltyReduction), 50, IntRange(0, 100));
            _applyArmorTrainingToManaCost = CreateSetting(nameof(_applyArmorTrainingToManaCost), false);
            _loadArrowsFromInventory = CreateSetting(nameof(_loadArrowsFromInventory), false);
            _baseStaminaRegen = CreateSetting(nameof(_baseStaminaRegen), 2.4f, FloatRange(0, 10));
            _titleScreenRandomize = CreateSetting(nameof(_titleScreenRandomize), (TitleScreens)0);
            _titleScreenHideCharacters = CreateSetting(nameof(_titleScreenHideCharacters), TitleScreenCharacterVisibility.Enable);
            _craftFromStash = CreateSetting(nameof(_craftFromStash), false);
            _displayStashAmount = CreateSetting(nameof(_displayStashAmount), false);
            _displayPricesInStash = CreateSetting(nameof(_displayPricesInStash), false);
            _itemActionDropOne = CreateSetting(nameof(_displayStashAmount), false);
            _temperatureToggle = CreateSetting(nameof(_temperatureToggle), false);
            _temperatureDataByEnum = new Dictionary<TemperatureSteps, ModSetting<Vector2>>();
            foreach (var step in Utility.GetEnumValues<TemperatureSteps>())
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
        override protected void SetFormatting()
        {
            _enableCheats.Format("Enable cheats");
            Indent++;
            {
                _enableCheatsHotkey.Format("Hotkey");
                Indent--;
            }
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
            _loadArrowsFromInventory.Description = "Whenever you shoot your bow, the lost arrow is instantly replaced with one from your backpack or pouch (in that order)";
            _baseStaminaRegen.Format("Base stamina regen");
            _titleScreenRandomize.Format("Randomize title screen");
            _titleScreenRandomize.Description = "Every time you start the game, one of the chosen title screens will be loaded at random (untick all for default)";
            Indent++;
            {
                _titleScreenHideCharacters.Format("Characters");
                _titleScreenHideCharacters.Description = "If you think the character are ruining the view :)\n" +
                                                         "(requires game restart)";
                Indent--;
            }
            _craftFromStash.Format("Craft with stashed items");
            _craftFromStash.Description = "When you're crafting in a city, you can use items from you stash";
            _displayStashAmount.Format("Display stashed item amounts");
            _displayStashAmount.Description = "Displays how many of each items you have stored in your stash\n" +
                                              "(shows in player/merchant inventory and crafting menu)";
            _displayPricesInStash.Format("Display prices in stash");
            _displayPricesInStash.Description = "Items in stash will have their sell prices displayed\n" +
                                                "(if prices vary among merchants, Soroborean Caravanner is taken as reference)";
            _itemActionDropOne.Format("Add \"Drop one\" item action");
            _itemActionDropOne.Description = "Adds a button to stacked items' which skips the \"choose amount\" panel and drops exactly 1 of the item\n" +
                                             "(recommended when playing co-op for quick item sharing)";
            _temperatureToggle.Format("Temperature");
            _temperatureToggle.Description = "Change each environmental temperature level's value and cap:\n" +
                                             "X   -   value; how much cold/hot weather defense you need to nullify this temperature level\n" +
                                             "Y   -   cap; min/max player temperature at this environmental temperature level\n" +
                                             "\n" +
                                             "Player temperatures cheatsheet:\n" +
                                             "Very cold   -   25\n" +
                                             "Cold   -   40\n" +
                                             "Neutral   -   50\n" +
                                             "Hot   -   60\n" +
                                             "Very Hot   -   75)";
            Indent++;
            {
                foreach (var step in Utility.GetEnumValues<TemperatureSteps>())
                    if (step != TemperatureSteps.Count)
                        _temperatureDataByEnum[step].Format(step.ToString(), _temperatureToggle);
                Indent--;
            }
        }
        override protected string Description
        => "• Mods (small and big) that didn't get their own section yet :)";
        override protected string SectionOverride
        => "";
        override public void LoadPreset(Presets.Preset preset)
        {
            switch (preset)
            {
                case Presets.Preset.Vheos_CoopSurvival:
                    ForceApply();
                    _enableCheats.Value = false;
                    _enableCheatsHotkey.Value = KeyCode.Keypad0.ToString();
                    _skipStartupVideos.Value = true;
                    _titleScreenRandomize.Value = (TitleScreens)~0;
                    _titleScreenHideCharacters.Value = TitleScreenCharacterVisibility.Randomize;
                    _removeCoopScaling.Value = true;
                    _removeDodgeInvulnerability.Value = true;
                    _healEnemiesOnLoad.Value = true;
                    _multiplicativeStacking.Value = true;
                    _armorTrainingPenaltyReduction.Value = 50;
                    _applyArmorTrainingToManaCost.Value = true;
                    _loadArrowsFromInventory.Value = true;
                    _craftFromStash.Value = true;
                    _displayStashAmount.Value = true;
                    _displayPricesInStash.Value = true;
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
        static private TreasureChest _playerStash;
        static private TreasureChest PlayerStash
        {
            get
            {
                if (_playerStash == null
                && AreaManager.Instance.CurrentArea.TryAssign(out var currentArea)
                && STASH_UIDS_BY_CITY.TryAssign((AreaManager.AreaEnum)currentArea.ID, out var uid))
                    _playerStash = (TreasureChest)ItemManager.Instance.GetItem(uid);
                return _playerStash;
            }
        }
        static private Merchant _soroboreanCaravanner;
        static private Merchant SoroboreanCaravanner
        {
            get
            {
                if (_soroboreanCaravanner == null
                && AreaManager.Instance.CurrentArea.TryAssign(out var currentArea)
                && SOROBOREAN_CARAVANNER_UIDS_BY_CITY.TryAssign((AreaManager.AreaEnum)currentArea.ID, out var uid)
                && Merchant.m_sceneMerchants.ContainsKey(uid))
                    _soroboreanCaravanner = Merchant.m_sceneMerchants[uid];
                return _soroboreanCaravanner;
            }
        }
        static private bool ShouldArmorSlotBeHidden(EquipmentSlot.EquipmentSlotIDs slot)
        => slot == EquipmentSlot.EquipmentSlotIDs.Helmet && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Head)
        || slot == EquipmentSlot.EquipmentSlotIDs.Chest && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Chest)
        || slot == EquipmentSlot.EquipmentSlotIDs.Foot && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Feet);
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
        static private void UpdateBaseStaminaRegen(CharacterStats characterStats)
        => characterStats.m_staminaRegen.BaseValue = _baseStaminaRegen;
        static private void TryUpdateTemperatureData()
        {
            #region quit
            if (!_temperatureToggle)
                return;
            #endregion

            if (EnvironmentConditions.Instance.TryAssign(out var environmentConditions))
                foreach (var step in Utility.GetEnumValues<TemperatureSteps>())
                    if (step != TemperatureSteps.Count)
                    {
                        environmentConditions.BodyTemperatureImpactPerStep[step] = _temperatureDataByEnum[step].Value.x;
                        environmentConditions.TemperatureCaps[step] = _temperatureDataByEnum[step].Value.y;
                    }
        }
        static private void TryDisplayStashAmount(ItemDisplay itemDisplay)
        {
            #region quit
            if (!_displayStashAmount || PlayerStash == null
            || !itemDisplay.m_lblQuantity.TryAssign(out var quantity)
            || !itemDisplay.RefItem.TryAssign(out var item)
            || item.OwnerCharacter == null
            && item.ParentContainer.IsNot<MerchantPouch>()
            && itemDisplay.IsNot<RecipeResultDisplay>())
                return;
            #endregion

            int stashAmount = itemDisplay is CurrencyDisplay ? PlayerStash.ContainedSilver : PlayerStash.ItemStackCount(item.ItemID);
            if (stashAmount <= 0)
                return;

            int amount = itemDisplay.m_lastQuantity;
            if (itemDisplay is RecipeResultDisplay)
                amount = itemDisplay.StackCount;
            else if (itemDisplay.m_dBarUses.TryAssign(out var dotBar) && dotBar.GOActive())
                amount = 1;

            int fontSize = (quantity.fontSize * 0.75f).Round();
            quantity.alignment = TextAnchor.UpperRight;
            quantity.lineSpacing = 0.75f;
            quantity.text = $"{amount}\n<color=#00FF00FF><size={fontSize}><b>+{stashAmount}</b></size></color>";
        }

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        // Reset static scene data
        [HarmonyPatch(typeof(NetworkLevelLoader), "UnPauseGameplay"), HarmonyPostfix]
        static void NetworkLevelLoader_UnPauseGameplay_Post(NetworkLevelLoader __instance)
        {
            _playerStash = null;
            _soroboreanCaravanner = null;
        }

        // Display prices in stash
        [HarmonyPatch(typeof(ItemDisplay), "UpdateValueDisplay"), HarmonyPrefix]
        static bool ItemDisplay_UpdateValueDisplay_Pre(ItemDisplay __instance)
        {
            #region quit
            if (!_displayPricesInStash
            || !__instance.CharacterUI.TryAssign(out var characterUI) || !characterUI.GetIsMenuDisplayed(CharacterUI.MenuScreens.Stash)
            || !__instance.RefItem.TryAssign(out var item) || item.OwnerCharacter != null
            || !__instance.m_lblValue.TryAssign(out var priceText)
            || SoroboreanCaravanner == null)
                return true;
            #endregion

            if (!__instance.m_valueHolder.activeSelf)
                __instance.m_valueHolder.SetActive(true);
            priceText.text = item.GetSellValue(characterUI.TargetCharacter, SoroboreanCaravanner).ToString();
            return false;
        }

        // Drop one
        [HarmonyPatch(typeof(ItemDisplayOptionPanel), "GetActiveActions"), HarmonyPostfix]
        static void ItemDisplayOptionPanel_GetActiveActions_Post(ItemDisplayOptionPanel __instance, ref List<int> __result)
        {
            #region quit
            //!itemDisplay.RefItem.TryAssign(out var item) || item.MoveStackAsOne  
            if (!_itemActionDropOne || __instance == null ||
            !__instance.m_activatedItemDisplay.TryAssign(out var itemDisplay)
            || itemDisplay.StackCount <= 1)
                return;
            #endregion

            __result.Add(DROP_ONE_ACTION_ID);
        }

        [HarmonyPatch(typeof(ItemDisplayOptionPanel), "GetActionText"), HarmonyPrefix]
        static bool ItemDisplayOptionPanel_GetActionText_Pre(ItemDisplayOptionPanel __instance, ref string __result, ref int _actionID)
        {
            #region quit
            if (_actionID != DROP_ONE_ACTION_ID)
                return true;
            #endregion

            __result = DROP_ONE_ACTION_TEXT;
            return false;
        }

        [HarmonyPatch(typeof(ItemDisplayOptionPanel), "ActionHasBeenPressed"), HarmonyPrefix]
        static bool ItemDisplayOptionPanel_ActionHasBeenPressed_Pre(ItemDisplayOptionPanel __instance, ref int _actionID)
        {
            #region quit
            if (_actionID != DROP_ONE_ACTION_ID)
                return true;
            #endregion

            __instance.m_activatedItemDisplay.OnConfirmDropStack(1);
            return false;
        }

        // Craft from stash
        [HarmonyPatch(typeof(CharacterInventory), "InventoryIngredients",
            new[] { typeof(Tag), typeof(DictionaryExt<int, CompatibleIngredient>) },
            new[] { ArgumentType.Normal, ArgumentType.Ref }),
            HarmonyPostfix]
        static void CharacterInventory_InventoryIngredients_Post(CharacterInventory __instance, Tag _craftingStationTag, ref DictionaryExt<int, CompatibleIngredient> _sortedIngredient)
        {
            #region quit
            if (!_craftFromStash || PlayerStash == null)
                return;
            #endregion

            __instance.InventoryIngredients(_craftingStationTag, ref _sortedIngredient, PlayerStash.GetContainedItems());
        }

        // Display stash amount
        [HarmonyPatch(typeof(ItemDisplay), "UpdateQuantityDisplay"), HarmonyPostfix]
        static void ItemDisplay_UpdateQuantityDisplay_Post(ItemDisplay __instance)
        => TryDisplayStashAmount(__instance);

        [HarmonyPatch(typeof(CurrencyDisplay), "UpdateQuantityDisplay"), HarmonyPostfix]
        static void CurrencyDisplay_UpdateQuantityDisplay_Post(CurrencyDisplay __instance)
        => TryDisplayStashAmount(__instance);

        [HarmonyPatch(typeof(RecipeResultDisplay), "UpdateQuantityDisplay"), HarmonyPostfix]
        static void RecipeResultDisplay_UpdateQuantityDisplay_Post(RecipeResultDisplay __instance)
        => TryDisplayStashAmount(__instance);

        // Override title screen
        [HarmonyPatch(typeof(TitleScreenLoader), "LoadTitleScreen", new[] { typeof(OTWStoreAPI.DLCs) }), HarmonyPrefix]
        static bool TitleScreenLoader_LoadTitleScreen_Pre(TitleScreenLoader __instance, ref OTWStoreAPI.DLCs _dlc)
        {
            #region quit
            if (_titleScreenRandomize.Value == 0)
                return true;
            #endregion

            var DLCs = new List<OTWStoreAPI.DLCs>();
            foreach (var flag in Utility.GetEnumValues<TitleScreens>())
                if (_titleScreenRandomize.Value.HasFlag(flag))
                    switch (flag)
                    {
                        case TitleScreens.Vanilla: DLCs.Add(OTWStoreAPI.DLCs.None); break;
                        case TitleScreens.TheSoroboreans: DLCs.Add(OTWStoreAPI.DLCs.Soroboreans); break;
                        case TitleScreens.TheThreeBrothers: DLCs.Add(OTWStoreAPI.DLCs.DLC2); break;
                    }

            _dlc = DLCs.Random();
            return true;
        }

        [HarmonyPatch(typeof(TitleScreenLoader), "LoadTitleScreenCoroutine"), HarmonyPostfix]
        static IEnumerator TitleScreenLoader_LoadTitleScreenCoroutine_Post(IEnumerator original, TitleScreenLoader __instance)
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

        // Temperature data
        [HarmonyPatch(typeof(EnvironmentConditions), "Start"), HarmonyPostfix]
        static void EnvironmentConditions_Start_Post(EnvironmentConditions __instance)
        => TryUpdateTemperatureData();

        // Stamina regen
        [HarmonyPatch(typeof(PlayerCharacterStats), "OnStart"), HarmonyPostfix]
        static void PlayerCharacterStats_OnStart_Post(PlayerCharacterStats __instance)
        => UpdateBaseStaminaRegen(__instance);

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
        static void CharacterInventory_GetAmmunitionCount_Post(CharacterInventory __instance, ref int __result)
        {
            #region quit
            if (!_loadArrowsFromInventory || __result == 0)
                return;
            #endregion

            __result += __instance.ItemCount(__instance.GetEquippedAmmunition().ItemID);
        }

        // Multiplicative stacking
        [HarmonyPatch(typeof(Stat), "GetModifier"), HarmonyPrefix]
        static bool Stat_GetModifier_Pre(Stat __instance, ref float __result, ref IList<Tag> _tags, ref int baseModifier)
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
        static bool CharacterEquipment_GetTotalMovementModifier_Pre(CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.MovementPenalty, true, true);

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalStaminaUseModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalStaminaUseModifier_Pre(CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.StaminaUsePenalty, false, true);

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalManaUseModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalManaUseModifier_Pre(CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.ManaUseModifier, false, _applyArmorTrainingToManaCost);

        // Skip startup video
        [HarmonyPatch(typeof(StartupVideo), "Awake"), HarmonyPrefix]
        static bool StartupVideo_Awake_Pre()
        {
            StartupVideo.HasPlayedOnce = _skipStartupVideos.Value;
            return true;
        }

        // Hide armor slots
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

/* Extra Controller Quickslots
[HarmonyPatch(typeof(QuickSlotPanel), "InitializeQuickSlotDisplays"), HarmonyPostfix]
static void QuickSlotPanel_InitializeQuickSlotDisplays_Post(QuickSlotPanel __instance, ref QuickSlotDisplay[] ___m_quickSlotDisplays)
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
