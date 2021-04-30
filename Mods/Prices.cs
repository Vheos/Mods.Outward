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
    public class Prices : AMod
    {
        #region const
        private const string ICONS_FOLDER = @"Prices\";
        static private Vector2 ALTERNATE_CURRENCY_ICON_SCALE = new Vector2(1.75f, 1.75f);
        static private Vector2 ALTERNATE_CURRENCY_ICON_PIVOT = new Vector2(-0.5f, 0.5f);
        private const int GOLD_INGOT_ID = 6300030;
        #endregion
        #region class
        private class SkillRequirement
        {
            // Fields
            public string ItemName;
            public int ItemID
            { get; private set; }
            public int Amount
            { get; private set; }
            public Sprite Icon
            { get; private set; }

            // Constructors
            public SkillRequirement(string name, int amount = 1)
            {
                ItemName = name;
                ItemID = Prefabs.ItemIDsByName[name];
                Amount = amount;
                Icon = Utility.CreateSpriteFromFile(Utility.PluginFolderPath + ICONS_FOLDER + name + ".PNG");
            }
        }
        #endregion

        // Settings
        static private ModSetting<bool> _merchantsToggle;
        static private ModSetting<int> _sellModifier, _buyModifier;
        static private ModSetting<int> _randomizePricesExtent, _randomizePricesPerDays;
        static private ModSetting<bool> _randomizePricesPerItem, _randomizePricesPerArea;
        static private ModSetting<bool> _customNonBasicSkillCosts;
        static private ModSetting<bool> _skillCostsToggle;
        static private ModSetting<int> _skillBasic, _skillBreakthrough, _skillAdvanced;
        static private ModSetting<bool> _learnMutuallyExclusiveSkills;
        static private ModSetting<bool> _exclusiveSkillCostsTsar;
        static private ModSetting<int> _exclusiveSkillCostMultiplier;
        override protected void Initialize()
        {
            _merchantsToggle = CreateSetting(nameof(_merchantsToggle), false);
            _sellModifier = CreateSetting(nameof(_sellModifier), 100, IntRange(0, 200));
            _buyModifier = CreateSetting(nameof(_buyModifier), 100, IntRange(0, 200));
            _randomizePricesExtent = CreateSetting(nameof(_randomizePricesExtent), 0, IntRange(0, 100));
            _randomizePricesPerDays = CreateSetting(nameof(_randomizePricesPerDays), 7, IntRange(1, 100));
            _randomizePricesPerItem = CreateSetting(nameof(_randomizePricesPerItem), true);
            _randomizePricesPerArea = CreateSetting(nameof(_randomizePricesPerArea), true);

            _skillCostsToggle = CreateSetting(nameof(_skillCostsToggle), false);
            _skillBasic = CreateSetting(nameof(_skillBasic), 50, IntRange(0, 1000));
            _skillBreakthrough = CreateSetting(nameof(_skillBreakthrough), 500, IntRange(0, 1000));
            _skillAdvanced = CreateSetting(nameof(_skillAdvanced), 600, IntRange(0, 1000));
            _learnMutuallyExclusiveSkills = CreateSetting(nameof(_learnMutuallyExclusiveSkills), false);
            _exclusiveSkillCostsTsar = CreateSetting(nameof(_exclusiveSkillCostsTsar), false);
            _exclusiveSkillCostMultiplier = CreateSetting(nameof(_exclusiveSkillCostMultiplier), 10, IntRange(0, 100));

            _customNonBasicSkillCosts = CreateSetting(nameof(_customNonBasicSkillCosts), false);
            _skillRequirementsByTrainerName = new Dictionary<string, SkillRequirement>()
            {
                // Vanilla
                ["Kazite Spellblade"] = new SkillRequirement("Old Legion Shield"),
                ["Cabal Hermit"] = new SkillRequirement("Boiled Azure Shrimp", 4),
                ["Wild Hunter"] = new SkillRequirement("Coralhorn Antler", 4),
                ["Rune Sage"] = new SkillRequirement("Great Astral Potion", 8),
                ["Warrior Monk"] = new SkillRequirement("Alpha Tuanosaur Tail"),
                ["Philosopher"] = new SkillRequirement("Crystal Powder", 4),
                ["Rogue Engineer"] = new SkillRequirement("Manticore Tail"),
                ["Mercenary"] = new SkillRequirement("Gold Ingot", 2),
                // DLC
                ["The Speedster"] = null,
                ["Hex Mage"] = null,
                ["Primal Ritualist"] = null,
                // No breakthrough
                ["Specialist"] = null,
                ["Weapon Master"] = null,
            };
            _exclusiveSkillRequirement = new SkillRequirement("Tsar Stone");
        }
        override protected void SetFormatting()
        {
            _merchantsToggle.Format("Merchants");
            Indent++;
            {
                _buyModifier.Format("Buying price multiplier", _merchantsToggle);
                _sellModifier.Format("Selling price multiplier", _merchantsToggle);
                _randomizePricesExtent.Format("Randomize prices", _merchantsToggle);
                _randomizePricesExtent.Description = "Prices will range from [100% - X%] to [100% + X%]\n" +
                                                     "and depend on current time, merchant and/or item";
                Indent++;
                {
                    _randomizePricesPerDays.Format("per days", _randomizePricesExtent, () => _randomizePricesExtent > 0);
                    _randomizePricesPerDays.Description = "All price modifiers will be rolled every X days";
                    _randomizePricesPerArea.Format("per city", _randomizePricesExtent, () => _randomizePricesExtent > 0);
                    _randomizePricesPerArea.Description = "Every city (and area) will have its own randomized price modifier";
                    _randomizePricesPerItem.Format("per item", _randomizePricesExtent, () => _randomizePricesExtent > 0);
                    _randomizePricesPerItem.Description = "Every item will have its own randomized price";
                    Indent--;
                }
                Indent--;
            }

            _skillCostsToggle.Format("Skill trainers");
            Indent++;
            {
                _skillBasic.Format("Basic", _skillCostsToggle);
                _skillBasic.Description = "below breakthrough in a skill tree";
                _skillBreakthrough.Format("Breakthrough", _skillCostsToggle);
                _skillAdvanced.Format("Advanced", _skillCostsToggle);
                _skillAdvanced.Description = "above breakthrough in a skill tree";

                _learnMutuallyExclusiveSkills.Format("Learn mutually exclusive skills", _skillCostsToggle);
                _learnMutuallyExclusiveSkills.Description = "Allows you to learn both skills that are normally mutually exclusive at defined price";
                Indent++;
                {
                    _exclusiveSkillCostsTsar.Format("at the cost of a Tsar Stone", _learnMutuallyExclusiveSkills);
                    _exclusiveSkillCostMultiplier.Format("at normal price multiplied by", _exclusiveSkillCostsTsar, false);
                    Indent--;
                }

                _customNonBasicSkillCosts.Format("[PERSONAL] Custom costs", _skillCostsToggle);
                _customNonBasicSkillCosts.Description = "Learning breakthrough and advanced skills will require specific items, depending on the skill tree:";
                foreach (var skillRequirementByTrainerName in _skillRequirementsByTrainerName)
                {
                    string trainer = skillRequirementByTrainerName.Key;
                    SkillRequirement requirement = skillRequirementByTrainerName.Value;
                    if (requirement != null)
                        _customNonBasicSkillCosts.Description += $"\n{trainer}   -   {requirement.Amount}x {requirement.ItemName}";
                }
                _customNonBasicSkillCosts.IsAdvanced = true;
                Indent--;
            }
        }
        override protected string Description
        => "• Change final buy/sell modifiers\n" +
           "• Randomize prices based on time, merchant and item\n" +
           "• Set price for learning mutually exclusive skills";
        override protected string SectionOverride
        => SECTION_SURVIVAL;

        // Utility
        static private Dictionary<string, SkillRequirement> _skillRequirementsByTrainerName;
        static private SkillRequirement _exclusiveSkillRequirement;
        static private bool HasMutuallyExclusiveSkill(Character character, SkillSlot skillSlot)
        => skillSlot.SiblingSlot != null && skillSlot.SiblingSlot.HasSkill(character);
        static private float GetRandomPriceModifier(Item item)
        {
            int itemSeed = _randomizePricesPerItem ? item.ItemID : 0;
            int areaSeed = _randomizePricesPerArea ? AreaManager.Instance.CurrentArea.ID : 0;
            int timeSeed = (GameTime / 24f / _randomizePricesPerDays).RoundDown();
            UnityEngine.Random.InitState(itemSeed + areaSeed + timeSeed);

            return 1f + UnityEngine.Random.Range(-_randomizePricesExtent, +_randomizePricesExtent) / 100f;
        }
        static private void TryModifyPriceAndColor(ref int finalPrice, Item item, ModSetting<int> buySellModifier)
        {
            #region quit
            if (!_merchantsToggle)
                return;
            #endregion

            // Price
            float modifier = buySellModifier / 100f;

            if (item.ItemID == GOLD_INGOT_ID)
                modifier = 1f;
            int preRandomPrice = (finalPrice * modifier).Round();

            if (_randomizePricesExtent > 0)
                modifier *= GetRandomPriceModifier(item);
            finalPrice = (finalPrice * modifier).Round();

            // Color
            if (_randomizePricesExtent == 0 || item.m_refItemDisplay == null || item.m_refItemDisplay.m_lblValue == null)
                return;

            float relativeIncrease = finalPrice == 0 ? 0f : 1f;
            if (preRandomPrice != 0)
                relativeIncrease = (float)finalPrice / preRandomPrice - 1f;

            if (item.OwnerCharacter == null)
                relativeIncrease *= -1;

            Color targetColor = relativeIncrease > 0 ? Color.green : Color.red;
            item.m_refItemDisplay.m_lblValue.color = Color.Lerp(Color.white, targetColor, relativeIncrease.Abs() * 100f / _randomizePricesExtent);
        }

        // Price modifier
        [HarmonyPatch(typeof(Item), "GetSellValue"), HarmonyPostfix]
        static void Item_GetSellValue_Post(ref Item __instance, ref int __result)
        => TryModifyPriceAndColor(ref __result, __instance, _sellModifier);

        [HarmonyPatch(typeof(Item), "GetBuyValue"), HarmonyPostfix]
        static void Item_GetBuyValue_Post(ref Item __instance, ref int __result)
        => TryModifyPriceAndColor(ref __result, __instance, _buyModifier);

        // Skill prices
        [HarmonyPatch(typeof(TrainerPanel), "OnSkillSlotSelected"), HarmonyPrefix]
        static bool TrainerPanel_OnSkillSlotSelected_Pre(ref TrainerPanel __instance, SkillTreeSlotDisplay _display)
        {
            #region quit
            if (!_skillCostsToggle)
                return true;
            #endregion

            // Cache
            SkillSlot slot = _display.FocusedSkillSlot;
            SkillSchool tree = __instance.m_trainerTree;
            Image currencyIcon = __instance.m_imgRemainingCurrency;
            Text currencyLeft = __instance.m_remainingSilver;
            Image currencyReqIcon = __instance.m_requirementDisplay.m_imgSilverIcon;
            CharacterInventory inventory = __instance.LocalCharacter.Inventory;

            // Defaults
            tree.AlternateCurrecy = -1;
            tree.AlternateCurrencyIcon = null;
            currencyIcon.overrideSprite = null;
            currencyIcon.rectTransform.pivot = 0.5f.ToVector2();
            currencyIcon.rectTransform.localScale = 1f.ToVector2();
            currencyReqIcon.rectTransform.pivot = 0.5f.ToVector2();
            currencyReqIcon.rectTransform.localScale = 1f.ToVector2();
            currencyLeft.text = inventory.ContainedSilver.ToString();

            if (slot.m_requiredMoney <= 0)
                return true;

            // Price
            if (SkillLimits.IsBasic(slot.Skill))
                slot.m_requiredMoney = _skillBasic;
            else if (SkillLimits.IsBreakthrough(slot.Skill))
                slot.m_requiredMoney = _skillBreakthrough;
            else if (SkillLimits.IsAdvanced(slot.Skill))
                slot.m_requiredMoney = _skillAdvanced;

            // Currency
            bool isCustomAdvancedCurrency = _customNonBasicSkillCosts && !SkillLimits.IsBasic(slot.Skill);
            bool isExclusive = _learnMutuallyExclusiveSkills && HasMutuallyExclusiveSkill(__instance.LocalCharacter, slot);

            SkillRequirement skillRequirement = null;
            if (isExclusive && _exclusiveSkillCostsTsar)
                skillRequirement = _exclusiveSkillRequirement;
            else if (isCustomAdvancedCurrency)
                skillRequirement = _skillRequirementsByTrainerName[__instance.m_trainerTree.Name];

            if (skillRequirement != null)
            {
                tree.AlternateCurrecy = skillRequirement.ItemID;
                tree.AlternateCurrencyIcon = skillRequirement.Icon;
                currencyIcon.overrideSprite = skillRequirement.Icon;
                currencyIcon.rectTransform.pivot = ALTERNATE_CURRENCY_ICON_PIVOT;
                currencyIcon.rectTransform.localScale = ALTERNATE_CURRENCY_ICON_SCALE;
                currencyReqIcon.rectTransform.pivot = ALTERNATE_CURRENCY_ICON_PIVOT;
                currencyReqIcon.rectTransform.localScale = ALTERNATE_CURRENCY_ICON_SCALE;
                currencyLeft.text = inventory.ItemCount(skillRequirement.ItemID).ToString();
                slot.m_requiredMoney = skillRequirement.Amount;
            }

            if (isExclusive && !_exclusiveSkillCostsTsar)
                slot.m_requiredMoney *= _exclusiveSkillCostMultiplier;

            return true;
        }

        [HarmonyPatch(typeof(SkillSlot), "IsBlocked"), HarmonyPrefix]
        static bool SkillSlot_IsBlocked_Pre(ref SkillSlot __instance)
        => !(_skillCostsToggle && _learnMutuallyExclusiveSkills);
    }
}