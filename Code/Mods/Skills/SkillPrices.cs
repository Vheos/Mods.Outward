namespace Vheos.Mods.Outward;
using UnityEngine.UI;

public class SkillPrices : AMod
{
    #region const
    private const string ICONS_FOLDER = @"Prices\";
    static private readonly Vector2 ALTERNATE_CURRENCY_ICON_SCALE = new(1.75f, 1.75f);
    static private readonly Vector2 ALTERNATE_CURRENCY_ICON_PIVOT = new(-0.5f, 0.5f);
    #endregion
    #region enum
    private enum SlotLevel
    {
        Basic = 1,
        Breakthrough = 2,
        Advanced = 3,
    }
    private enum FormulaType
    {
        Linear = 1,
        Exponential = 2,
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
            Icon = InternalUtility.CreateSpriteFromFile(InternalUtility.PluginFolderPath + ICONS_FOLDER + name + ".PNG");
        }
    }
    #endregion

    // Settings       
    static private ModSetting<bool> _formulaToggle;
    static private Dictionary<SlotLevel, ModSetting<Vector4>> _formulaCoeffsByLevel;
    static private ModSetting<FormulaType> _formulaType;
    static private ModSetting<bool> _learnMutuallyExclusiveSkills;
    static private ModSetting<bool> _exclusiveSkillCostsTsar;
    static private ModSetting<int> _exclusiveSkillCostMultiplier;
    override protected void Initialize()
    {
        _formulaToggle = CreateSetting(nameof(_formulaToggle), false);
        _formulaCoeffsByLevel = new Dictionary<SlotLevel, ModSetting<Vector4>>();
        foreach (var level in InternalUtility.GetEnumValues<SlotLevel>())
        {
            Vector4 initialPrice = Vector4.zero;
            switch (level)
            {
                case SlotLevel.Basic: initialPrice.x = 50; break;
                case SlotLevel.Breakthrough: initialPrice.x = 500; break;
                case SlotLevel.Advanced: initialPrice.x = 600; break;
            }
            _formulaCoeffsByLevel.Add(level, CreateSetting(nameof(_formulaCoeffsByLevel) + level, initialPrice));
        }
        _formulaType = CreateSetting(nameof(_formulaType), FormulaType.Linear);
        _learnMutuallyExclusiveSkills = CreateSetting(nameof(_learnMutuallyExclusiveSkills), false);
        _exclusiveSkillCostsTsar = CreateSetting(nameof(_exclusiveSkillCostsTsar), false);
        _exclusiveSkillCostMultiplier = CreateSetting(nameof(_exclusiveSkillCostMultiplier), 300, IntRange(100, 500));

        _exclusiveSkillRequirement = new SkillRequirement("Tsar Stone");
    }
    override protected void SetFormatting()
    {
        _formulaToggle.Format("Formulas");
        _formulaToggle.Description = "Define a price formula for skills of each level";
        using (Indent)
        {
            foreach (var priceCoeffByLevel in _formulaCoeffsByLevel)
                priceCoeffByLevel.Value.Format(priceCoeffByLevel.Key.ToString(), _formulaToggle);
            _formulaCoeffsByLevel[SlotLevel.Basic].Description = "below the breakthrough skill in a tree";
            _formulaCoeffsByLevel[SlotLevel.Advanced].Description = "above breakthrough in a tree";
            _formulaType.Format("Type", _formulaToggle);
            _formulaType.Description = "Linear   -        X   +      Y x B     +      Z x C     +      W x D  \n" +
                                   "Exponential   -   X  x  (1+Y%) ^ B  x  (1+Z%) ^ C  x  (1+W%) ^ D\n" +
                                   "where:\n" +
                                   "B   -   number of all unlocked skills\n" +
                                   "C   -   number of unlocked skills at current trainer\n" +
                                   "D   -   number of used breakthrough points";
        }
        _learnMutuallyExclusiveSkills.Format("Learn mutually exclusive skills");
        _learnMutuallyExclusiveSkills.Description = "Allows you to learn both skills that are normally mutually exclusive at defined price";
        using (Indent)
        {
            _exclusiveSkillCostsTsar.Format("at the cost of a Tsar Stone", _learnMutuallyExclusiveSkills);
            _exclusiveSkillCostMultiplier.Format("at normal price multiplied by (%)", _exclusiveSkillCostsTsar, false);
        }
    }
    override protected string Description
    => "• Change skill trainers' prices\n" +
       "• Set price for learning mutually exclusive skills";
    override protected string SectionOverride
    => ModSections.Skills;
    override protected string ModName
    => "Prices";
    override protected void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _formulaToggle.Value = true;
                {
                    _formulaCoeffsByLevel[SlotLevel.Basic].Value = new Vector4(0, 1, 1, 0);
                    _formulaCoeffsByLevel[SlotLevel.Breakthrough].Value = new Vector4(0, 1, 1, 0);
                    _formulaCoeffsByLevel[SlotLevel.Advanced].Value = new Vector4(0, 1, 1, 0);
                    _formulaType.Value = FormulaType.Linear;
                }
                _learnMutuallyExclusiveSkills.Value = true;
                _exclusiveSkillCostsTsar.Value = true;
                break;
        }
    }

    // Utility
    static private SkillRequirement _exclusiveSkillRequirement;
    static private bool HasMutuallyExclusiveSkill(Character character, SkillSlot skillSlot)
    => skillSlot.SiblingSlot != null && skillSlot.SiblingSlot.HasSkill(character);
    static private SlotLevel GetLevel(BaseSkillSlot slot)
        => !slot.ParentBranch.ParentTree.BreakthroughSkill.TryNonNull(out var breakthroughSlot)
            ? SlotLevel.Basic
            : slot.ParentBranch.Index.CompareTo(breakthroughSlot.ParentBranch.Index) switch
            {
                -1 => SlotLevel.Basic,
                0 => SlotLevel.Breakthrough,
                +1 => SlotLevel.Advanced,
                _ => default,
            };
    static private int GetPrice(Character character, SkillSlot slot)
    {
        // Cache
        CharacterSkillKnowledge characterSkills = character.Inventory.SkillKnowledge;
        Vector4 coeffs = _formulaCoeffsByLevel[GetLevel(slot)];
        int allSkillsCount = characterSkills.m_activeSkillUIDs.Count + characterSkills.m_passiveSkillUIDs.Count;
        int currentSkillsCount = slot.ParentBranch.ParentTree.SkillSlots.Count(t => t.HasSkill(character));
        int breakthroughsCount = character.PlayerStats.m_usedBreakthroughCount;

        float price = _formulaType == FormulaType.Linear
                    ? coeffs.x + coeffs.y * allSkillsCount
                               + coeffs.z * currentSkillsCount
                               + coeffs.w * breakthroughsCount
                    : coeffs.x * coeffs.y.Div(100f).Add(1).Pow(allSkillsCount)
                               * coeffs.z.Div(100f).Add(1).Pow(currentSkillsCount)
                               * coeffs.w.Div(100f).Add(1).Pow(breakthroughsCount);
        return price.Round();
    }

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006
    [HarmonyPatch(typeof(TrainerPanel), nameof(TrainerPanel.OnSkillSlotSelected)), HarmonyPrefix]
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
        if (_formulaToggle)
            slot.m_requiredMoney = GetPrice(__instance.LocalCharacter, slot);

        // Currency
        if (_learnMutuallyExclusiveSkills && HasMutuallyExclusiveSkill(__instance.LocalCharacter, slot))
            if (_exclusiveSkillCostsTsar)
            {
                tree.AlternateCurrecy = SkillPrices._exclusiveSkillRequirement.ItemID;
                tree.AlternateCurrencyIcon = _exclusiveSkillRequirement.Icon;
                currencyIcon.overrideSprite = _exclusiveSkillRequirement.Icon;
                currencyIcon.rectTransform.pivot = ALTERNATE_CURRENCY_ICON_PIVOT;
                currencyIcon.rectTransform.localScale = ALTERNATE_CURRENCY_ICON_SCALE;
                currencyReqIcon.rectTransform.pivot = ALTERNATE_CURRENCY_ICON_PIVOT;
                currencyReqIcon.rectTransform.localScale = ALTERNATE_CURRENCY_ICON_SCALE;
                currencyLeft.text = inventory.ItemCount(_exclusiveSkillRequirement.ItemID).ToString();
                slot.m_requiredMoney = _exclusiveSkillRequirement.Amount;
            }
            else
                slot.m_requiredMoney = (slot.m_requiredMoney * _exclusiveSkillCostMultiplier / 100f).Round();

        return true;
    }

    [HarmonyPatch(typeof(SkillSlot), nameof(SkillSlot.IsBlocked)), HarmonyPrefix]
    static bool SkillSlot_IsBlocked_Pre(SkillSlot __instance)
    => !_learnMutuallyExclusiveSkills;
}

/*
static private ModSetting<bool> _customNonBasicSkillCosts;



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
*/
