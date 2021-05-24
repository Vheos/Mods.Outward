using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;




namespace ModPack
{
    public class SkillPrices : AMod
    {
        #region const
        private const string ICONS_FOLDER = @"Prices\";
        static private readonly Vector2 ALTERNATE_CURRENCY_ICON_SCALE = new Vector2(1.75f, 1.75f);
        static private readonly Vector2 ALTERNATE_CURRENCY_ICON_PIVOT = new Vector2(-0.5f, 0.5f);
        #endregion
        #region enum
        private enum SlotLevel
        {
            Basic = 1,
            Breakthrough = 2,
            Advanced = 3,
        }
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
        static private ModSetting<bool> _pricesToggle;
        static private ModSetting<int> _priceBasic, _pricesBreakthrough, _pricesAdvanced;
        static private ModSetting<bool> _learnMutuallyExclusiveSkills;
        static private ModSetting<bool> _exclusiveSkillCostsTsar;
        static private ModSetting<int> _exclusiveSkillCostMultiplier;
        static private ModSetting<bool> _customNonBasicSkillCosts;
        override protected void Initialize()
        {
            _pricesToggle = CreateSetting(nameof(_pricesToggle), false);
            _priceBasic = CreateSetting(nameof(_priceBasic), 50, IntRange(0, 1000));
            _pricesBreakthrough = CreateSetting(nameof(_pricesBreakthrough), 50, IntRange(0, 1000));
            _pricesAdvanced = CreateSetting(nameof(_pricesAdvanced), 600, IntRange(0, 1000));
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
            _pricesToggle.Format("Prices by skill level");
            Indent++;
            {
                _priceBasic.Format("Basic", _pricesToggle);
                _priceBasic.Description = "below breakthrough in a skill tree";
                _pricesBreakthrough.Format("Breakthrough", _pricesToggle);
                _pricesAdvanced.Format("Advanced", _pricesToggle);
                _pricesAdvanced.Description = "above breakthrough in a skill tree";
                Indent--;
            }
            _learnMutuallyExclusiveSkills.Format("Learn mutually exclusive skills");
            _learnMutuallyExclusiveSkills.Description = "Allows you to learn both skills that are normally mutually exclusive at defined price";
            Indent++;
            {
                _exclusiveSkillCostsTsar.Format("at the cost of a Tsar Stone", _learnMutuallyExclusiveSkills);
                _exclusiveSkillCostMultiplier.Format("at normal price multiplied by", _exclusiveSkillCostsTsar, false);
                Indent--;
            }

            _customNonBasicSkillCosts.Format("[PERSONAL] Custom costs");
            _customNonBasicSkillCosts.Description = "Learning breakthrough and advanced skills will require specific items, depending on the trainer:";
            foreach (var skillRequirementByTrainerName in _skillRequirementsByTrainerName)
            {
                string trainer = skillRequirementByTrainerName.Key;
                SkillRequirement requirement = skillRequirementByTrainerName.Value;
                if (requirement != null)
                    _customNonBasicSkillCosts.Description += $"\n{trainer}   -   {requirement.Amount}x {requirement.ItemName}";
            }
            _customNonBasicSkillCosts.IsAdvanced = true;

        }
        override protected string Description
        => "• Change skill trainers' prices\n" +
           "• Set price for learning mutually exclusive skills";
        override protected string SectionOverride
        => SECTION_SKILLS;
        override protected string ModName
        => "Prices";
        override public void LoadPreset(Presets.Preset preset)
        {
            switch (preset)
            {
                case Presets.Preset.Vheos_CoopSurvival:
                    break;

                case Presets.Preset.IggyTheMad_TrueHardcore:
                    break;
            }
        }

        // Utility
        static private Dictionary<string, SkillRequirement> _skillRequirementsByTrainerName;
        static private SkillRequirement _exclusiveSkillRequirement;
        static private bool HasMutuallyExclusiveSkill(Character character, SkillSlot skillSlot)
        => skillSlot.SiblingSlot != null && skillSlot.SiblingSlot.HasSkill(character);
        static private SlotLevel GetLevel(BaseSkillSlot slot)
        {
            if (!slot.ParentBranch.ParentTree.BreakthroughSkill.TryAssign(out var breakthroughSlot))
                return SlotLevel.Basic;

            switch (slot.ParentBranch.Index.CompareTo(breakthroughSlot.ParentBranch.Index))
            {
                case -1: return SlotLevel.Basic;
                case 0: return SlotLevel.Breakthrough;
                case +1: return SlotLevel.Advanced;
                default: return 0;
            }
        }

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        [HarmonyPatch(typeof(TrainerPanel), "OnSkillSlotSelected"), HarmonyPrefix]
        static bool TrainerPanel_OnSkillSlotSelected_Pre(TrainerPanel __instance, SkillTreeSlotDisplay _display)
        {
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

            // Price
            switch (GetLevel(slot))
            {
                case SlotLevel.Basic: slot.m_requiredMoney = _priceBasic; break;
                case SlotLevel.Breakthrough: slot.m_requiredMoney = _pricesBreakthrough; break;
                case SlotLevel.Advanced: slot.m_requiredMoney = _pricesAdvanced; break;
                default: break;
            }

            // Currency
            bool isCustomAdvancedCurrency = _customNonBasicSkillCosts && GetLevel(slot) != SlotLevel.Basic;
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
        static bool SkillSlot_IsBlocked_Pre(SkillSlot __instance)
        => !_learnMutuallyExclusiveSkills;
    }
}