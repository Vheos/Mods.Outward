namespace Vheos.Mods.Outward;
using NodeCanvas.Tasks.Conditions;
using UnityEngine.UI;

public class SkillLimits : AMod
{
    #region const
    private const int UNLEARN_ACTION_ID = -1;
    private const string UNLEARN_ACTION_TEXT = "Forget";
    private static readonly Dictionary<SkillTypes, string> NOTIFICATION_BY_SKILL_TYPE = new()
    {
        [SkillTypes.Passive] = "You can't learn any more passive skills!",
        [SkillTypes.Active] = "You can't learn any more active skills!",
        [SkillTypes.Any] = "You can't learn any more skills!",
    };
    private static readonly int[] SIDE_SKILL_IDS =
    {
        // Weapon skills
        "Puncture".ToSkillID(),
        "Pommel Counter".ToSkillID(),
        "Talus Cleaver".ToSkillID(),
        "Execution".ToSkillID(),
        "Mace Infusion".ToSkillID(),
        "Juggernaut".ToSkillID(),
        "Simeon's Gambit".ToSkillID(),
        "Moon Swipe".ToSkillID(),
        "Prismatic Flurry".ToSkillID(),
        // Boons
        "Mist".ToSkillID(),
        "Warm".ToSkillID(),
        "Cool".ToSkillID(),
        "Blessed".ToSkillID(),
        "Possessed".ToSkillID(),
        // Hexes
        "Haunt Hex".ToSkillID(),
        "Scorch Hex".ToSkillID(),
        "Chill Hex".ToSkillID(),
        "Doom Hex".ToSkillID(),
        "Curse Hex".ToSkillID(),
        // Mana
        "Flamethrower".ToSkillID(),
    };
    private static Color ICON_COLOR = new(1f, 1f, 1f, 1 / 3f);
    private static Vector2 INDICATOR_SCALE = new(1.5f, 1.5f);
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

        Basic = 1 << 1,
        Advanced = 1 << 2,
        Side = 1 << 3,
    }
    #endregion

    // Setting
    private static ModSetting<bool> _separateLimits;
    private static ModSetting<int> _skillsLimit, _passiveSkillsLimit, _activeSkillsLimit;
    private static ModSetting<LimitedSkillTypes> _limitedSkillTypes;
    private static ModSetting<bool> _freePostBreakthroughBasicSkills;
    private static ModSetting<Color> _limitedSkillColor;
    protected override void Initialize()
    {
        _separateLimits = CreateSetting(nameof(_separateLimits), false);
        _skillsLimit = CreateSetting(nameof(_skillsLimit), 20, IntRange(1, 100));
        _passiveSkillsLimit = CreateSetting(nameof(_passiveSkillsLimit), 5, IntRange(1, 25));
        _activeSkillsLimit = CreateSetting(nameof(_activeSkillsLimit), 15, IntRange(1, 75));
        _limitedSkillTypes = CreateSetting(nameof(_limitedSkillTypes), (LimitedSkillTypes)~0);
        _freePostBreakthroughBasicSkills = CreateSetting(nameof(_freePostBreakthroughBasicSkills), false);
        _limitedSkillColor = CreateSetting(nameof(_limitedSkillColor), new Color(0.5f, 0f, 0.25f, 0.5f));
    }
    protected override void SetFormatting()
    {
        _separateLimits.Format("Separate passive/active limits");
        _separateLimits.Description = "Define different limits for passive and active skills";
        using (Indent)
        {
            _skillsLimit.Format("Skills limit", _separateLimits, false);
            _skillsLimit.Description = "Only skills defined in \"Limited skill types\" count towards limit";
            _passiveSkillsLimit.Format("Passive skills limit", _separateLimits);
            _passiveSkillsLimit.Description = "Only passive skills defined in \"Limited skill types\" count towards this limit";
            _activeSkillsLimit.Format("Active skills limit", _separateLimits);
            _activeSkillsLimit.Description = "Only active skills defined in \"Limited skill types\" count towards this limit";
        }
        _limitedSkillTypes.Format("Limited skill types");
        _limitedSkillTypes.Description = "Decide which skill types count towards limit:\n" +
                                         "Basic - below breakthrough in a skill tree\n" +
                                         "Advanced - above breakthrough in a skill tree\n" +
                                         "Side - not found in any vanilla skill tree\n" +
                                         "(weapon skills, boons, hexes and Flamethrower)";
        using (Indent)
        {
            _freePostBreakthroughBasicSkills.Format("Basic skills are free post-break", _limitedSkillTypes, LimitedSkillTypes.Basic);
            _freePostBreakthroughBasicSkills.Description = "After you learn a breakthrough skill, basic skills from the same tree no longer count towards limit";
        }
        _limitedSkillColor.Format("Limited skill color");
    }
    protected override string Description
    => "• Set limit on how many skills you can learn\n" +
       "• Decide which skills count towards the limit";
    protected override string SectionOverride
    => ModSections.Skills;
    protected override string ModName
    => "Limits";
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _separateLimits.Value = true;
                {
                    _passiveSkillsLimit.Value = 5;
                    _activeSkillsLimit.Value = 15;
                }
                _limitedSkillTypes.Value = LimitedSkillTypes.Basic | LimitedSkillTypes.Advanced;
                _freePostBreakthroughBasicSkills.Value = true;
                break;
        }
    }

    // Utility
    private static bool CanLearnMoreLimitedSkills(Character character, SkillTypes skillTypes)
    => GetLimitedSkillsCount(character, skillTypes) < GetLimitingSetting(skillTypes);
    private static int GetLimitedSkillsCount(Character character, SkillTypes countedTypes)
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
    private static bool IsLimited(Character character, Skill skill)
    => _limitedSkillTypes.Value.HasFlag(LimitedSkillTypes.Basic) && IsBasic(skill)
       && !(_freePostBreakthroughBasicSkills && IsPostBreakthrough(character, skill))
    || _limitedSkillTypes.Value.HasFlag(LimitedSkillTypes.Advanced) && IsAdvanced(skill)
    || _limitedSkillTypes.Value.HasFlag(LimitedSkillTypes.Side) && IsSide(skill);
    private static bool HasBreakthroughInTree(Character character, SkillSchool skillTree)
    => skillTree.BreakthroughSkill != null && skillTree.BreakthroughSkill.HasSkill(character);
    private static bool IsPostBreakthrough(Character character, Skill skill)
    => TryGetSkillTree(skill, out SkillSchool tree) && HasBreakthroughInTree(character, tree);
    private static bool IsBasic(Skill skill)
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
    private static bool IsBreakthrough(Skill skill)
    => TryGetSkillTree(skill, out SkillSchool tree)
    && tree.BreakthroughSkill != null && tree.BreakthroughSkill.Contains(skill);
    private static bool IsAdvanced(Skill skill)
    {
        if (TryGetSkillTree(skill, out SkillSchool tree) && tree.BreakthroughSkill != null)
            foreach (var slot in tree.SkillSlots)
                if (slot.Contains(skill))
                    return slot.ParentBranch.Index > tree.BreakthroughSkill.ParentBranch.Index;
        return false;
    }
    private static bool IsSide(Skill skill)
    => skill.ItemID.IsContainedIn(SIDE_SKILL_IDS);
    private static bool TryGetSkillTree(Skill skill, out SkillSchool skillTree)
    {
        skillTree = SkillTreeHolder.Instance.m_skillTrees.DefaultOnInvalid(skill.SchoolIndex - 1);
        return skillTree != null;
    }
    private static ModSetting<int> GetLimitingSetting(SkillTypes skillTypes)
        => skillTypes switch
        {
            SkillTypes.None or SkillTypes.Any => _skillsLimit,
            SkillTypes.Passive => _passiveSkillsLimit,
            SkillTypes.Active => _activeSkillsLimit,
            _ => null,
        };
    private static SkillTypes GetSkillTypes(Skill skill)
    => !_separateLimits ? SkillTypes.Any
                        : skill.IsPassive ? SkillTypes.Passive
                                          : SkillTypes.Active;
    private static void InitializeCacheOfAllSkills(SkillSchool skillTree)
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

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006
    [HarmonyPostfix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.GetActiveActions))]
    private static void ItemDisplayOptionPanel_GetActiveActions_Post(ItemDisplayOptionPanel __instance, ref List<int> __result)
    {
        #region quit
        if (!__instance.m_pendingItem.TryAs(out Skill skill) || !IsLimited(__instance.LocalCharacter, skill))
            return;
        #endregion

        __result.Add(UNLEARN_ACTION_ID);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.GetActionText))]
    private static bool ItemDisplayOptionPanel_GetActionText_Pre(ItemDisplayOptionPanel __instance, ref string __result, ref int _actionID)
    {
        #region quit
        if (_actionID != UNLEARN_ACTION_ID)
            return true;
        #endregion

        __result = UNLEARN_ACTION_TEXT;
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.ActionHasBeenPressed))]
    private static bool ItemDisplayOptionPanel_ActionHasBeenPressed_Pre(ItemDisplayOptionPanel __instance, ref int _actionID)
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

    [HarmonyPrefix, HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.RefreshEnchantedIcon))]
    private static bool ItemDisplay_RefreshEnchantedIcon_Pre(ItemDisplay __instance)
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
        icon.color = ICON_COLOR;
        border.color = _limitedSkillColor.Value.NewA(1f);
        indicator.color = _limitedSkillColor.Value;
        indicator.rectTransform.pivot = 1f.ToVector2();
        indicator.rectTransform.localScale = INDICATOR_SCALE;
        indicator.GOSetActive(true);
        return false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(TrainerPanel), nameof(TrainerPanel.Show))]
    private static void TrainerPanel_Show_Post(TrainerPanel __instance)
    => InitializeCacheOfAllSkills(__instance.m_trainerTree);

    [HarmonyPrefix, HarmonyPatch(typeof(TrainerPanel), nameof(TrainerPanel.OnSkillSlotClicked))]
    private static bool TrainerPanel_OnSkillSlotClicked_Pre(TrainerPanel __instance, ref SkillTreeSlotDisplay _slotDisplay)
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

    [HarmonyPostfix, HarmonyPatch(typeof(Condition_KnowSkill), nameof(Condition_KnowSkill.OnCheck))]
    private static void Condition_KnowSkill_OnCheck_Post(Condition_KnowSkill __instance, ref bool __result)
    {
        Character character = __instance.character.value;
        Skill skill = __instance.skill.value;
        if (character == null || skill == null || !IsLimited(character, skill))
            return;

        __result |= !CanLearnMoreLimitedSkills(character, GetSkillTypes(skill));
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
*             Log.Debug($"{skill.DisplayName}\t{skill.ItemID}\t{skill.SchoolIndex}\t{(TryGetSkillTree(skill, out SkillSchool tree) ? tree.Name : "")}\n" +
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