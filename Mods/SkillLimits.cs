using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NodeCanvas.Tasks.Conditions;



namespace ModPack
{
    public class SkillLimits : AMod
    {
        #region const
        private const int UNLEARN_ACTION_ID = -1;
        private const string UNLEARN_ACTION_TEXT = "Forget";
        static private Dictionary<SkillTypes, string> NOTIFICATION_BY_SKILL_TYPE = new Dictionary<SkillTypes, string>
        {
            [SkillTypes.Passive] = "You can't learn any more passive skills!",
            [SkillTypes.Active] = "You can't learn any more active skills!",
            [SkillTypes.Any] = "You can't learn any more skills!",
        };
        static private int[] SIDE_SKILL_IDS =
        {
            "Puncture".SkillID(),
            "Pommel Counter".SkillID(),
            "Talus Cleaver".SkillID(),
            "Execution".SkillID(),
            "Mace Infusion".SkillID(),
            "Juggernaut".SkillID(),
            "Simeon's Gambit".SkillID(),
            "Moon Swipe".SkillID(),
            "Prismatic Flurry".SkillID(),
            "Flamethrower".SkillID(),
            "Mist".SkillID(),
            "Warm".SkillID(),
            "Cool".SkillID(),
            "Blessed".SkillID(),
            "Possessed".SkillID(),
            "Haunt Hex".SkillID(),
            "Scorch Hex".SkillID(),
            "Chill Hex".SkillID(),
            "Doom Hex".SkillID(),
            "Curse Hex".SkillID(),
        };

        #endregion
        #region enum
        [Flags]
        private enum SkillTypes
        {
            None = 0,
            Any = ~0,

            Passive = 1 << 1,
            Active = 1 << 2,
        }
        [Flags]
        private enum LimitedSkillTypes
        {
            None = 0,
            All = ~0,

            Basic = 1 << 1,
            Advanced = 1 << 2,
            Side = 1 << 3,
        }
        #endregion

        // Setting
        static private ModSetting<bool> _separateLimits;
        static private ModSetting<int> _skillsLimit, _passiveSkillsLimit, _activeSkillsLimit;
        static private ModSetting<LimitedSkillTypes> _limitedSkillTypes;
        static private ModSetting<bool> _freePostBreakthroughBasicSkills;
        static private ModSetting<bool> _allowExclusiveSkills;
        static private ModSetting<bool> _formattingToggle;
        static private ModSetting<Color> _iconColor, _borderColor, _indicatorColor;
        static private ModSetting<int> _indicatorSize;
        override protected void Initialize()
        {
            _separateLimits = CreateSetting(nameof(_separateLimits), false);
            _skillsLimit = CreateSetting(nameof(_skillsLimit), 20, IntRange(1, 100));
            _passiveSkillsLimit = CreateSetting(nameof(_passiveSkillsLimit), 5, IntRange(1, 25));
            _activeSkillsLimit = CreateSetting(nameof(_activeSkillsLimit), 15, IntRange(1, 75));
            _limitedSkillTypes = CreateSetting(nameof(_limitedSkillTypes), LimitedSkillTypes.All);
            _freePostBreakthroughBasicSkills = CreateSetting(nameof(_freePostBreakthroughBasicSkills), false);
            _allowExclusiveSkills = CreateSetting(nameof(_allowExclusiveSkills), false);

            _formattingToggle = CreateSetting(nameof(_formattingToggle), false);
            _iconColor = CreateSetting(nameof(_iconColor), new Color(1f, 1f, 1f, 1 / 3f));
            _borderColor = CreateSetting(nameof(_borderColor), new Color(1 / 3f, 0f, 1f, 1f));
            _indicatorColor = CreateSetting(nameof(_indicatorColor), new Color(0.75f, 0f, 1f, 1 / 3f));
            _indicatorSize = CreateSetting(nameof(_indicatorSize), 150, IntRange(0, 200));
        }
        override protected void SetFormatting()
        {
            _separateLimits.Format("Separate passive/active limits");
            _separateLimits.Description = "Define different limits for passive and active skills";
            Indent++;
            {
                _skillsLimit.Format("Skills limit", _separateLimits, false);
                _skillsLimit.Description = "Only skills defined in \"Limited skill types\" count towards limit";
                _passiveSkillsLimit.Format("Passive skills limit", _separateLimits);
                _passiveSkillsLimit.Description = "Only passive skills defined in \"Limited skill types\" count towards this limit";
                _activeSkillsLimit.Format("Active skills limit", _separateLimits);
                _activeSkillsLimit.Description = "Only active skills defined in \"Limited skill types\" count towards this limit";
                Indent--;
            }
            _limitedSkillTypes.Format("Limited skill types");
            _limitedSkillTypes.Description = "Decide which skill types count towards limit";
            Indent++;
            {
                _freePostBreakthroughBasicSkills.Format("Basic skills are free post-break", _limitedSkillTypes, LimitedSkillTypes.Basic);
                _freePostBreakthroughBasicSkills.Description = "After you learn a breakthrough skill, basic skills from the same tree no longer count towards limit";
                Indent--;
            }
            _allowExclusiveSkills.Format("Allow exclusive skills");
            _allowExclusiveSkills.Description = "Allows you to learn both skills that are normally mutually exclusive";

            _formattingToggle.Format("Limited skills formatting");
            _formattingToggle.IsAdvanced = true;
            Indent++;
            {
                _iconColor.Format("Icon", _formattingToggle);
                _iconColor.IsAdvanced = true;
                _indicatorColor.Format("Indicator", _formattingToggle);
                _indicatorColor.IsAdvanced = true;
                _borderColor.Format("Border", _formattingToggle);
                _borderColor.IsAdvanced = true;
                _indicatorSize.Format("Border", _formattingToggle);
                _indicatorSize.IsAdvanced = true;
                Indent--;
            }
        }

        // Utility
        static private bool CanLearnMoreLimitedSkills(Character character, SkillTypes skillTypes)
        => GetLimitedSkillsCount(character, skillTypes) < GetLimitingSetting(skillTypes);
        static private int GetLimitedSkillsCount(Character character, SkillTypes countedTypes)
        {
            (SkillTypes Types, Func<IList<string>> UIDsGetter)[] skillsData =
            {
                (SkillTypes.Passive, character.Inventory.SkillKnowledge.GetLearnedPassiveSkillUIDs),
                (SkillTypes.Active, character.Inventory.SkillKnowledge.GetLearnedActiveSkillUIDs),
            };

            int counter = 0;
            foreach (var data in skillsData)
                if (countedTypes.HasFlag(data.Types))
                    foreach (var skillUID in data.UIDsGetter())
                        if (ItemManager.Instance.GetItem(skillUID).TryAs(out Skill skill) && IsLimited(character, skill))
                            counter++;
            return counter;
        }
        static private bool IsLimited(Character character, Skill skill)
        => _limitedSkillTypes.Value.HasFlag(LimitedSkillTypes.Basic) && IsBasic(skill)
           && !(_freePostBreakthroughBasicSkills && IsPostBreakthrough(character, skill))
        || _limitedSkillTypes.Value.HasFlag(LimitedSkillTypes.Advanced) && IsAdvanced(skill)
        || _limitedSkillTypes.Value.HasFlag(LimitedSkillTypes.Side) && IsSide(skill);
        static private bool HasBreakthroughInTree(Character character, SkillSchool skillTree)
        => skillTree.BreakthroughSkill != null && skillTree.BreakthroughSkill.HasSkill(character);
        static private bool IsPostBreakthrough(Character character, Skill skill)
        => TryGetSkillTree(skill, out SkillSchool tree) && HasBreakthroughInTree(character, tree);
        static private bool IsBasic(Skill skill)
        {
            if (TryGetSkillTree(skill, out SkillSchool tree))
                if (tree.BreakthroughSkill == null)
                    return true;
                else
                    foreach (var slot in tree.SkillSlots)
                        if (slot.Contains(skill))
                            return slot.ParentBranch.Index < tree.BreakthroughSkill.ParentBranch.Index;
            return false;
        }
        static private bool IsBreakthrough(Skill skill)
        => TryGetSkillTree(skill, out SkillSchool tree)
        && tree.BreakthroughSkill != null && tree.BreakthroughSkill.Contains(skill);
        static private bool IsAdvanced(Skill skill)
        {
            if (TryGetSkillTree(skill, out SkillSchool tree) && tree.BreakthroughSkill != null)
                foreach (var slot in tree.SkillSlots)
                    if (slot.Contains(skill))
                        return slot.ParentBranch.Index > tree.BreakthroughSkill.ParentBranch.Index;
            return false;
        }
        static private bool IsSide(Skill skill)
        => skill.ItemID.IsContainedIn(SIDE_SKILL_IDS);
        static private bool TryGetSkillTree(Skill skill, out SkillSchool skillTree)
        {
            skillTree = SkillTreeHolder.Instance.m_skillTrees.DefaultOnInvalid(skill.SchoolIndex - 1);
            return skillTree != null;
        }
        static private ModSetting<int> GetLimitingSetting(SkillTypes skillTypes)
        {
            switch (skillTypes)
            {
                case SkillTypes.None:
                case SkillTypes.Any: return _skillsLimit;
                case SkillTypes.Passive: return _passiveSkillsLimit;
                case SkillTypes.Active: return _activeSkillsLimit;
                default: return null;
            }
        }
        static private SkillTypes GetSkillTypes(Skill skill)
        => !_separateLimits ? SkillTypes.Any
                            : skill.IsPassive ? SkillTypes.Passive
                                              : SkillTypes.Active;
        static private void InitializeCacheOfAllSkills(SkillSchool skillTree)
        {
            foreach (var slot in skillTree.SkillSlots)
                switch (slot)
                {
                    case SkillSlot t:
                        if (t.Skill.SchoolIndex <= 0)
                            t.Skill.InitCachedInfos();
                        break;
                    case SkillSlotFork t:
                        foreach (var subSlot in t.SkillsToChooseFrom)
                            if (subSlot.Skill.SchoolIndex <= 0)
                                subSlot.Skill.InitCachedInfos();
                        break;
                }
        }
        static private Sprite FoodIcon = Utility.CreateSpriteFromFile(Utility.PluginFolderPath + @"\Needs\Food.png");

        // Hooks
        [HarmonyPatch(typeof(ItemDisplayOptionPanel), "GetActiveActions"), HarmonyPostfix]
        static void ItemDisplayOptionPanel_GetActiveActions_Post(ref ItemDisplayOptionPanel __instance, ref List<int> __result)
        {
            #region quit
            if (!__instance.m_pendingItem.TryAs(out Skill skill) || !IsLimited(__instance.LocalCharacter, skill))
                return;
            #endregion

            __result.Add(UNLEARN_ACTION_ID);
        }

        [HarmonyPatch(typeof(ItemDisplayOptionPanel), "GetActionText"), HarmonyPrefix]
        static bool ItemDisplayOptionPanel_GetActionText_Pre(ref ItemDisplayOptionPanel __instance, ref string __result, ref int _actionID)
        {
            #region quit
            if (_actionID != UNLEARN_ACTION_ID)
                return true;
            #endregion

            __result = UNLEARN_ACTION_TEXT;
            return false;
        }

        [HarmonyPatch(typeof(ItemDisplayOptionPanel), "ActionHasBeenPressed"), HarmonyPrefix]
        static bool ItemDisplayOptionPanel_ActionHasBeenPressed_Pre(ref ItemDisplayOptionPanel __instance, ref int _actionID)
        {
            #region quit
            if (_actionID != UNLEARN_ACTION_ID)
                return true;
            #endregion

            Item item = __instance.m_pendingItem;
            ItemManager.Instance.DestroyItem(item.UID);
            item.m_refItemDisplay.Hide();
            __instance.m_characterUI.ContextMenu.Hide();
            return false;
        }

        [HarmonyPatch(typeof(ItemDisplay), "RefreshEnchantedIcon"), HarmonyPrefix]
        static bool ItemDisplay_RefreshEnchantedIcon_Pre(ref ItemDisplay __instance)
        {
            #region quit
            if (!__instance.m_refItem.TryAs(out Skill skill))
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
            if (!IsLimited(__instance.LocalCharacter, skill))
                return true;

            // Custom
            icon.color = _iconColor;
            border.color = _borderColor;
            indicator.color = _indicatorColor;
            indicator.rectTransform.pivot = 1f.ToVector2();
            indicator.rectTransform.localScale = _indicatorSize.Value.Div(100).ToVector2();
            indicator.GOSetActive(true);
            return false;
        }

        [HarmonyPatch(typeof(TrainerPanel), "Show"), HarmonyPostfix]
        static void TrainerPanel_Show_Post(ref TrainerPanel __instance)
        => InitializeCacheOfAllSkills(__instance.m_trainerTree);

        [HarmonyPatch(typeof(TrainerPanel), "OnSkillSlotSelected"), HarmonyPrefix]
        static bool TrainerPanel_OnSkillSlotSelected_Pre(ref TrainerPanel __instance, SkillTreeSlotDisplay _display)
        {
            // Cache
            SkillSlot slot = _display.FocusedSkillSlot;
            SkillSchool tree = __instance.m_trainerTree;
            Image currencyIcon = __instance.m_imgRemainingCurrency;
            Text currencyLeft = __instance.m_remainingSilver;
            CharacterInventory inventory = __instance.LocalCharacter.Inventory;

            // Defaults
            tree.AlternateCurrecy = -1;
            tree.AlternateCurrencyIcon = null;
            currencyIcon.overrideSprite = null;
            currencyLeft.text = inventory.ContainedSilver.ToString(); 

            // Overrides
            if (IsBasic(slot.Skill))
            {
                tree.AlternateCurrecy = "Meat Stew".ItemID();
                tree.AlternateCurrencyIcon = FoodIcon;
                currencyIcon.overrideSprite = FoodIcon;
                currencyLeft.text = inventory.ItemCount(tree.AlternateCurrecy).ToString();
                slot.m_requiredMoney = 3;
            }

            if (IsBreakthrough(slot.Skill))
                slot.m_requiredMoney = 0;

            if (IsAdvanced(slot.Skill))
                slot.m_requiredMoney = 11;

            return true;
        }

        [HarmonyPatch(typeof(TrainerPanel), "OnSkillSlotClicked"), HarmonyPrefix]
        static bool TrainerPanel_OnSkillSlotClicked_Pre(ref TrainerPanel __instance, ref SkillTreeSlotDisplay _slotDisplay)
        {
            Skill skill = _slotDisplay.FocusedSkillSlot.Skill;
            if (IsLimited(__instance.LocalCharacter, skill))
            {
                SkillTypes types = GetSkillTypes(skill);
                if (!CanLearnMoreLimitedSkills(_slotDisplay.LocalCharacter, types))
                {
                    _slotDisplay.CharacterUI.ShowInfoNotification(NOTIFICATION_BY_SKILL_TYPE[types]);
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(Condition_KnowSkill), "OnCheck"), HarmonyPostfix]
        static void Condition_KnowSkill_OnCheck_Post(ref Condition_KnowSkill __instance, ref bool __result)
        {
            Character character = __instance.character.value;
            Skill skill = __instance.skill.value;
            if (character == null || skill == null || !IsLimited(character, skill))
                return;

            __result |= !CanLearnMoreLimitedSkills(character, GetSkillTypes(skill));
        }

        [HarmonyPatch(typeof(SkillSlot), "IsBlocked"), HarmonyPrefix]
        static bool SkillSlot_IsBlocked_Pre(ref SkillSlot __instance)
        => !_allowExclusiveSkills;


    }
}

/*
 *         static private int GetSkillRowIndexInTree(Skill skill, SkillSchool tree)
        {
            int rowIndex = -1;
            foreach (var skillSlot in tree.SkillSlots)
                if (skillSlot.Contains(skill))
                {
                    rowIndex = skillSlot.ParentBranch.Index;
                    break;
                }
            return rowIndex;
        }
 */

/*
 *             Tools.Log($"{skill.DisplayName}\t{skill.ItemID}\t{skill.SchoolIndex}\t{(TryGetSkillTree(skill, out SkillSchool tree) ? tree.Name : "")}\n" +
                $"{IsBasic(skill)}\t{IsBreakthrough(skill)}\t{IsAdvanced(skill)}\t{IsSide(skill)}\t{IsLimited(__instance.LocalCharacter, skill)}\n");
 */

/*
 *         static private int GetSkillRowIndexInTree(Skill skill, SkillSchool tree)
        {
            int rowIndex = -1;
            foreach (var skillSlot in tree.SkillSlots)
                if (skillSlot.Contains(skill))
                {
                    rowIndex = skillSlot.ParentBranch.Index;
                    break;
                }
            return rowIndex;
        }

 */