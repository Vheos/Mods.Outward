using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
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
        #endregion
        #region class
        private class SkillRequirement
        {
            // Fields
            public int ItemID
            { get; private set; }
            public int Amount
            { get; private set; }
            public Sprite Icon
            { get; private set; }

            // Constructors
            public SkillRequirement(string name, int amount = 1)
            {
                ItemID = Prefabs.ItemIDsByName[name];
                Amount = amount;
                Icon = Utility.CreateSpriteFromFile(Utility.PluginFolderPath + ICONS_FOLDER + name + ".PNG");
            }
        }
        #endregion 

        // Settings
        static private ModSetting<int> _sellModifier, _buyModifier;
        static private ModSetting<bool> _customNonBasicSkillCosts;
        static private ModSetting<int> _costBasic, _costBreakthrough, _costAdvanced;
        static private ModSetting<bool> _allowExclusiveSkills;
        override protected void Initialize()
        {
            _sellModifier = CreateSetting(nameof(_sellModifier), 100, IntRange(0, 200));
            _buyModifier = CreateSetting(nameof(_buyModifier), 100, IntRange(0, 200));
            _customNonBasicSkillCosts = CreateSetting(nameof(_customNonBasicSkillCosts), false);
            _costBasic = CreateSetting(nameof(_costBasic), 50, IntRange(0, 1000));
            _costBreakthrough = CreateSetting(nameof(_costBreakthrough), 0, IntRange(0, 1000));
            _costAdvanced = CreateSetting(nameof(_costAdvanced), 100, IntRange(0, 1000));
            _allowExclusiveSkills = CreateSetting(nameof(_allowExclusiveSkills), false);

            _skillRequirementsByTrainerName = new Dictionary<string, SkillRequirement>()
            {
                ["Kazite Spellblade"] = new SkillRequirement("Old Legion Shield"),
                ["Cabal Hermit"] = new SkillRequirement("Boiled Azure Shrimp", 3),
                ["Wild Hunter"] = new SkillRequirement("Coralhorn Antler", 2),
                ["Rune Sage"] = new SkillRequirement("Great Astral Potion", 3),
                ["Warrior Monk"] = new SkillRequirement("Alpha Tuanosaur Tail"),
                ["Philosopher"] = new SkillRequirement("Crystal Powder", 3),
                ["Rogue Engineer"] = new SkillRequirement("Manticore Tail"),
                ["Mercenary"] = new SkillRequirement("Gold Ingot", 2),

                ["The Speedster"] = null,
                ["Hex Mage"] = null,
                ["Primal Ritualist"] = null,

                ["Specialist"] = null,
                ["Weapon Master"] = null,
            };
            _exclusiveSkillRequirement = new SkillRequirement("Tsar Stone");
        }
        override protected void SetFormatting()
        {
            _sellModifier.Format("Sell modifier");
            _buyModifier.Format("Buy modifier");
            _customNonBasicSkillCosts.Format("Custom prices for non-basic skills");
            _costBasic.Format("Basic skills");
            _costBreakthrough.Format("Breakthrough skills");
            _costAdvanced.Format("Advanced skills", _customNonBasicSkillCosts, false);
            _allowExclusiveSkills.Format("Allow exclusive skills");
            _allowExclusiveSkills.Description = "Allows you to learn both skills that are normally mutually exclusive\n" +
                                                "for a small fee of one Tsar Stone :)";
        }

        // Utility
        static private Dictionary<string, SkillRequirement> _skillRequirementsByTrainerName;
        static private SkillRequirement _exclusiveSkillRequirement;
        static private bool HasMutuallyExclusiveSkill(Character character, SkillSlot skillSlot)
        => skillSlot.SiblingSlot != null && skillSlot.SiblingSlot.HasSkill(character);

        // Price modifier
        [HarmonyPatch(typeof(Item), "GetSellValue"), HarmonyPostfix]
        static void Item_GetSellValue_Post(ref Item __instance, ref int __result)
        => __result = (__result * _sellModifier / 100f).Round();

        [HarmonyPatch(typeof(Item), "GetBuyValue"), HarmonyPostfix]
        static void Item_GetBuyValue_Post(ref Item __instance, ref int __result)
        => __result = (__result * _buyModifier / 100f).Round();

        // Skill prices
        [HarmonyPatch(typeof(TrainerPanel), "OnSkillSlotSelected"), HarmonyPrefix]
        static bool TrainerPanel_OnSkillSlotSelected_Pre(ref TrainerPanel __instance, SkillTreeSlotDisplay _display)
        {
            // Cache
            SkillSlot slot = _display.FocusedSkillSlot;
            SkillSchool tree = __instance.m_trainerTree;
            Image currencyIcon = __instance.m_imgRemainingCurrency;
            Text currencyLeft = __instance.m_remainingSilver;
            Image currencyReqIcon = __instance.m_requirementDisplay.m_imgSilverIcon;
            CharacterInventory inventory = __instance.LocalCharacter.Inventory;

            // Price
            if (SkillLimits.IsBasic(slot.Skill))
                slot.m_requiredMoney = _costBasic;
            else if (SkillLimits.IsBreakthrough(slot.Skill))
                slot.m_requiredMoney = _costBreakthrough;
            else if (SkillLimits.IsAdvanced(slot.Skill))
                slot.m_requiredMoney = _costAdvanced;

            // Defaults
            tree.AlternateCurrecy = -1;
            tree.AlternateCurrencyIcon = null;
            currencyIcon.overrideSprite = null;
            currencyIcon.rectTransform.pivot = 0.5f.ToVector2();
            currencyIcon.rectTransform.localScale = 1f.ToVector2();
            currencyReqIcon.rectTransform.pivot = 0.5f.ToVector2();
            currencyReqIcon.rectTransform.localScale = 1f.ToVector2();
            currencyLeft.text = inventory.ContainedSilver.ToString();

            // Currency
            bool isCustomAdvancedCurrency = _customNonBasicSkillCosts && !SkillLimits.IsBasic(slot.Skill);
            bool isExclusiveCurrency = _allowExclusiveSkills && HasMutuallyExclusiveSkill(__instance.LocalCharacter, slot);

            SkillRequirement skillRequirement = null;
            if (isExclusiveCurrency)
                skillRequirement = _exclusiveSkillRequirement;
            else if (isCustomAdvancedCurrency)
                skillRequirement = _skillRequirementsByTrainerName[__instance.m_trainerTree.Name];

            if (skillRequirement != null)
            {
                tree.AlternateCurrecy = skillRequirement.ItemID;
                tree.AlternateCurrencyIcon = skillRequirement.Icon;
                currencyIcon.overrideSprite = skillRequirement.Icon;
                currencyIcon.rectTransform.pivot = new Vector2(-0.5f, 0.5f);
                currencyIcon.rectTransform.localScale = 1.75f.ToVector2();
                currencyReqIcon.rectTransform.pivot = new Vector2(-0.5f, 0.5f);
                currencyReqIcon.rectTransform.localScale = 1.75f.ToVector2();
                currencyLeft.text = inventory.ItemCount(skillRequirement.ItemID).ToString();
                slot.m_requiredMoney = skillRequirement.Amount;
            }

            return true;
        }

        [HarmonyPatch(typeof(SkillSlot), "IsBlocked"), HarmonyPrefix]
        static bool SkillSlot_IsBlocked_Pre(ref SkillSlot __instance)
        => !_allowExclusiveSkills;
    }
}