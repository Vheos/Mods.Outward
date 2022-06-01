namespace Vheos.Mods.Outward;
using Tools.TraitEqualizer;
using Random = Tools.RandomN.Random;

public class SkillTreeRandomizer : AMod, IDelayedInit
{
    #region const
    private const string ICONS_FOLDER = @"Skills\";
    private const string WEAPON_SKILLS_TREE_NAME = "WeaponSkills";
    private const string BOONS_TREE_NAME = "Boons";
    private const string HEXES_TREE_NAME = "Hexes";
    private const string MANA_TREE_NAME = "Mana";
    private const string INNATE_TREE_NAME = "Innate";
    private const float REROLL_DELAY = 0.1f;
    private const string REROLL_STATS_FILE_NAME = "SkillTreeRandomizerStats.txt";
    static private readonly Vector2 DEFAULT_SLOT_DISPLAY_SIZE = new(128, 54);
    static private readonly Vector2 DEFAULT_TREE_LOCAL_POSITION = new(0, -24);
    static private readonly (string Name, int[] IDs)[] SIDE_SKILLS =
    {
        (WEAPON_SKILLS_TREE_NAME, new[]
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
            "Flamethrower".SkillID(), // Mana
        }),
        (BOONS_TREE_NAME, new[]
        {
            "Mist".SkillID(),
            "Warm".SkillID(),
            "Cool".SkillID(),
            "Blessed".SkillID(),
            "Possessed".SkillID(),
        }),
        (HEXES_TREE_NAME, new[]
        {
            "Haunt Hex".SkillID(),
            "Scorch Hex".SkillID(),
            "Chill Hex".SkillID(),
            "Doom Hex".SkillID(),
            "Curse Hex".SkillID(),
        }),
    };
    private static readonly string[] MISSING_ICON_SKILL_NAMES =
    {
        // Weapon skills
        "Talus Cleaver",
        "Prismatic Flurry",
        "Mace Infusion",
        "Simeon's Gambit",
        // Non-tree boons
        "Warm",
        "Cool",
        "Blessed",
        "Possessed",
        "Mist",
        // Non-tree hexes
        "Scorch Hex",
        "Chill Hex",
        "Doom Hex",
        "Curse Hex",
        "Haunt Hex",
        // Mana
        "Flamethrower",
    };
    static private readonly (int Column, int Row)[] SLOT_POSITIONS =
    {
        (2, 2),
        (1, 2),
        (3, 2),
        (2, 1),
        (1, 1),
        (3, 1),
        (0, 2),
        (4, 2),
        (0, 1),
        (4, 1),

        (2, 0),
        (1, 0),
        (3, 0),
        (0, 0),
        (4, 0),
    };
    #endregion
    #region enum
    private enum SlotLevel
    {
        Basic = 1,
        Breakthrough = 2,
        Advanced = 3,
    }
    private enum SlotType
    {
        Passive = 1,
        Active = 2,
        Mixed = 3,
    }
    [Flags]
    private enum EqualizedTraits
    {
        Count = 1 << 1,
        Types = 1 << 2,
        Levels = 1 << 3,
        Choices = 1 << 4,
        VanillaTrees = 1 << 5,
    }
    [Flags]
    private enum VanillaInput
    {
        KaziteSpellblade = 1 << 1,
        CabalHermit = 1 << 2,
        WildHunter = 1 << 3,
        RuneSage = 1 << 4,
        WarriorMonk = 1 << 5,
        Philosopher = 1 << 6,
        Mercenary = 1 << 7,
        RogueEngineer = 1 << 8,
        WeaponSkills = 1 << 9,
        Boons = 1 << 10,
    }
    [Flags]
    private enum TheSoroboreansInput
    {
        TheSpeedster = 1 << 11,
        HexMage = 1 << 12,
        Hexes = 1 << 13,
    }
    [Flags]
    private enum TheThreeBrothersInput
    {
        PrimalRitualist = 1 << 14,
        WeaponMaster = 1 << 15,
    }
    [Flags]
    private enum VanillaOutput
    {
        KaziteSpellblade = 1 << 1,
        CabalHermit = 1 << 2,
        WildHunter = 1 << 3,
        RuneSage = 1 << 4,
        WarriorMonk = 1 << 5,
        Philosopher = 1 << 6,
        Mercenary = 1 << 7,
        RogueEngineer = 1 << 8,
    }
    [Flags]
    private enum TheSoroboreansOutput
    {
        TheSpeedster = 1 << 11,
        HexMage = 1 << 12,
    }
    [Flags]
    private enum TheThreeBrothersOutput
    {
        PrimalRitualist = 1 << 14,
    }
    #endregion

    // Settings
    static private ModSetting<bool> _reroll, _rerollOnGameStart, _rerollLogResults;
    static private ModSetting<VanillaInput> _vanillaInput;
    static private ModSetting<TheSoroboreansInput> _theSoroboreansInput;
    static private ModSetting<TheThreeBrothersInput> _theThreeBrothersInput;
    static private ModSetting<VanillaOutput> _vanillaOutput;
    static private ModSetting<TheSoroboreansOutput> _theSoroboreansOutput;
    static private ModSetting<TheThreeBrothersOutput> _theThreeBrothersOutput;
    static private ModSetting<EqualizedTraits> _equalizedTraits;
    static private ModSetting<bool> _randomizeBreakthroughSkills, _preferPassiveBreakthroughs, _avoidChoiceBreakthroughs;
    static private ModSetting<bool> _treatWeaponMasterAsAdvanced;
    static private ModSetting<bool> _affectOnlyChosenOutputTrees;
    static private ModSetting<int> _seed;
    static private ModSetting<bool> _seedRandomize;
    override protected void Initialize()
    {
        _reroll = CreateSetting(nameof(_reroll), false);
        _rerollOnGameStart = CreateSetting(nameof(_rerollOnGameStart), false);
        _rerollLogResults = CreateSetting(nameof(_rerollLogResults), false);
        _seed = CreateSetting(nameof(_seed), 0);
        _seedRandomize = CreateSetting(nameof(_seedRandomize), false);
        _equalizedTraits = CreateSetting(nameof(_equalizedTraits), EqualizedTraits.Count | EqualizedTraits.Types | EqualizedTraits.Levels);
        _randomizeBreakthroughSkills = CreateSetting(nameof(_randomizeBreakthroughSkills), false);
        _preferPassiveBreakthroughs = CreateSetting(nameof(_preferPassiveBreakthroughs), false);
        _avoidChoiceBreakthroughs = CreateSetting(nameof(_avoidChoiceBreakthroughs), false);
        _treatWeaponMasterAsAdvanced = CreateSetting(nameof(_treatWeaponMasterAsAdvanced), false);
        _affectOnlyChosenOutputTrees = CreateSetting(nameof(_affectOnlyChosenOutputTrees), false);

        // Inputs/outputs
        _vanillaInput = CreateSetting(nameof(_vanillaInput), (VanillaInput)~0);
        _vanillaOutput = CreateSetting(nameof(_vanillaOutput), (VanillaOutput)~0);
        if (HasDLC(OTWStoreAPI.DLCs.Soroboreans))
        {
            _theSoroboreansInput = CreateSetting(nameof(_theSoroboreansInput), (TheSoroboreansInput)~0);
            _theSoroboreansOutput = CreateSetting(nameof(_theSoroboreansOutput), (TheSoroboreansOutput)~0);
        }
        if (HasDLC(OTWStoreAPI.DLCs.DLC2))
        {
            _theThreeBrothersInput = CreateSetting(nameof(_theThreeBrothersInput), (TheThreeBrothersInput)~0);
            _theThreeBrothersOutput = CreateSetting(nameof(_theThreeBrothersOutput), (TheThreeBrothersOutput)~0);
        }

        // Events
        AddEventOnEnabled(() =>
        {
            CacheSkillTreeHolder();
            CreateSideSkillTrees();
            LoadMissingSkillIcons();
        });
        AddEventOnDisabled(() =>
        {
            ResetSkillTreeHolders();
            _rerollOnGameStart.Value = false;
        });
        _reroll.AddEvent(() =>
        {
            if (!_reroll)
                return;
            Global.Instance.ExecuteAtTheEndOfFrame
            (
                () => Global.Instance.ExecuteOnceAfterDelay(REROLL_DELAY, RandomizeSkills)
            );
            _reroll.SetSilently(false);
        });
        _seedRandomize.AddEvent(() =>
        {
            if (!_seedRandomize)
                return;

            RandomizeSeed();
            _seedRandomize.SetSilently(false);
        });

        // Reroll on game start
        if (_rerollOnGameStart)
            _reroll.Value = true;
    }
    override protected void SetFormatting()
    {
        _reroll.Format("Reroll");
        _reroll.DisplayResetButton = false;
        using (Indent)
        {
            _rerollOnGameStart.Format("on game start");
            _rerollOnGameStart.Description = "Automatically randomize skill trees when you start the game\n" +
                                             "(as long as you don't change the seed, you will always generate the same trees)";
            _rerollOnGameStart.DisplayResetButton = false;
            _rerollLogResults.Format("log stats");
            _rerollLogResults.Description = "Create a file (in the \"Vheos\" folder) with statistics about each tree, without spoiling trainers/skills' names. " +
                                            "Use it to double-check if there aren't any broken/unwanted trees, " +
                                            "like a tree with only post-breakthrough skills, only passives, or only 1 skill";
            _rerollLogResults.DisplayResetButton = false;
        }

        AModSetting[] inputOutputSettings =
        {
            _vanillaInput,
            _theSoroboreansInput,
            _theThreeBrothersInput,
            _vanillaOutput,
            _theSoroboreansOutput,
            _theThreeBrothersOutput
        };
        foreach (var setting in inputOutputSettings)
        {
            if (setting == _vanillaInput)
            {
                setting.Format("Input skill trees");
                setting.Description = "All skills from these trees will be gathered into one big list. " +
                                      "Then, randomly selected skills will create new trees, which will override vanilla trees chosen below";
            }
            else if (setting == _vanillaOutput)
            {
                setting.Format("Output skill trees");
                setting.Description = "These vanilla trees will be overriden with random skills from the trees chosen above. This is also the total number of generated trees";
            }
            else if (setting != null)
                setting.Format("");

            setting.DisplayResetButton = false;
        }

        _equalizedTraits.Format("Try to equalize");
        _equalizedTraits.Description = "Every generated tree will have similar number of:\n" +
                                       "Count - skill slots\n" +
                                       "Types - passive and active skills\n" +
                                       "Levels - basic and advanced skills\n" +
                                       "Choices - slots with 2 mutually exclusive skills\n" +
                                       "Trees - skills from each original tree\n" +
                                       "\n" +
                                       "For example, if you choose to equalize skill types, every tree might have 3-4 passives skills and 7-8 active skills. " +
                                       "Otherwise, some trees might get zero passives, and others mostly passives. " +
                                       "Likewise, if you choose NOT to equalize skill count, some trees might have 3 skills, while others 10\n" +
                                       "(note: the more traits the algorithm is trying to equalize, the less accurate it will be overall)";
        _affectOnlyChosenOutputTrees.Format("Affect only chosen output trees");
        _affectOnlyChosenOutputTrees.Description = "Output trees which you haven't chosen will keep their current skills\n" +
                                                   "Otherwise, they will become empty (zero skills)";
        _randomizeBreakthroughSkills.Format("Randomize breakthroughs");
        _randomizeBreakthroughSkills.Description = "Breakthroughs will be randomized along with advanced skills\n" +
                                                   "Every tree will get a new breakthrough - chosen at random from all assigned advanced skills";
        using (Indent)
        {
            _preferPassiveBreakthroughs.Format("prefer passives", _randomizeBreakthroughSkills);
            _preferPassiveBreakthroughs.Description = "If possible, the new breakthrough will be a passive skill";
            _avoidChoiceBreakthroughs.Format("avoid choices", _randomizeBreakthroughSkills);
            _avoidChoiceBreakthroughs.Description = "If possible, the new breakthrough won't be a choice between 2 skills";
        }
        _treatWeaponMasterAsAdvanced.Format("Treat weapon master as advanced", _theThreeBrothersInput, TheThreeBrothersInput.WeaponMaster);
        _treatWeaponMasterAsAdvanced.Description = "Weapon master skills will be treated as advanced instead of basic\n" +
                                                   "(which they technically are as the skill tree doesn't have a breakthrough)";
        _seed.Format("Seed");
        _seed.Description = "The same number will always result in the same setup\n" +
                            "If you change it mid-playthrough, all trees will be rerolled\n" +
                            "(might result in lost breakthrough points if you're using \"Randomize breakthroughs\")";
        using (Indent)
        {
            _seedRandomize.Format("randomize");
            _seedRandomize.DisplayResetButton = false;
        }
    }
    override protected string Description
    => "• Randomize skills taught by trainers";
    override protected string SectionOverride
    => ModSections.Skills;
    override protected string ModName
    => "Tree Randomizer";
    override protected void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _vanillaInput.Value = (VanillaInput)~0;
                _theSoroboreansInput.Value = (TheSoroboreansInput)~0;
                _theThreeBrothersInput.Value = (TheThreeBrothersInput)~0;
                _vanillaOutput.Value = (VanillaOutput)~0 & ~VanillaOutput.KaziteSpellblade;
                _theSoroboreansOutput.Value = 0;
                _theThreeBrothersOutput.Value = 0;
                _equalizedTraits.Value = (EqualizedTraits)~0 & ~EqualizedTraits.Choices;
                _randomizeBreakthroughSkills.Value = true;
                _preferPassiveBreakthroughs.Value = true;
                _avoidChoiceBreakthroughs.Value = true;
                _treatWeaponMasterAsAdvanced.Value = true;
                _affectOnlyChosenOutputTrees.Value = false;
                _rerollOnGameStart.Value = true;
                _reroll.Value = true;
                break;
        }
    }

    // Utility
    static private SkillTreeHolder _cachedSkillTreeHolder;
    static private List<SkillSchool> _sideSkillTrees;
    static private void CacheSkillTreeHolder()
    {
        _cachedSkillTreeHolder = SkillTreeHolder.Instance;
        _cachedSkillTreeHolder.gameObject.name += " (cached)";
        CopyCachedSkillTreeHolder();
    }
    static private void CopyCachedSkillTreeHolder()
    {
        SkillTreeHolder newSkillTreeHolder = GameObject.Instantiate(_cachedSkillTreeHolder, _cachedSkillTreeHolder.transform.parent);
        newSkillTreeHolder.name = "Skill Trees";
    }
    static private void CreateSideSkillTrees()
    {
        _sideSkillTrees = new List<SkillSchool>();
        foreach (var (Name, IDs) in SIDE_SKILLS)
        {
            GameObject skillTreeHolder = new(Name);
            skillTreeHolder.BecomeChildOf(_cachedSkillTreeHolder);
            GameObject skillBranchHolder = new("0");
            skillBranchHolder.BecomeChildOf(skillTreeHolder);

            foreach (var id in IDs)
            {
                Skill skill = Prefabs.SkillsByID[id];
                GameObject skillSlotHolder = new(skill.Name);
                skillSlotHolder.BecomeChildOf(skillBranchHolder);
                skillSlotHolder.AddComponent<SkillSlot>().m_skill = skill;
            }

            skillBranchHolder.AddComponent<SkillBranch>();
            SkillSchool skillTree = skillTreeHolder.AddComponent<SkillSchool>();
            skillTree.m_defaultName = Name;
            _sideSkillTrees.Add(skillTree);
        }
    }
    static private void LoadMissingSkillIcons()
    {
        foreach (var name in MISSING_ICON_SKILL_NAMES)
        {
            int id = Prefabs.SkillIDsByName[name];
            Prefabs.SkillsByID[id].SkillTreeIcon = InternalUtility.CreateSpriteFromFile(InternalUtility.PluginFolderPath + ICONS_FOLDER + name.Replace('/', '_') + ".PNG");
        }
    }
    static private void ResetSkillTreeHolders()
    {
        // Destroy side skill trees
        foreach (var sideSkillTree in _sideSkillTrees)
        {
            sideSkillTree.Unparent();
            _sideSkillTrees.DestroyObject();
        }

        // Destroy real SkillTreeHolder
        SkillTreeHolder.Instance.DestroyObject();

        // Copy and destroy cached SkillTreeHolder
        CopyCachedSkillTreeHolder();
        _cachedSkillTreeHolder.DestroyObject();
    }
    //
    static private IEnumerable<SkillSchool> GetInputSkillTrees()
    {
        foreach (Enum flag in Enum.GetValues(typeof(VanillaInput)))
            if (_vanillaInput.Value.HasFlag(flag))
                yield return FlagToSkillTree(flag, true);
        if (_theSoroboreansInput != null)
            foreach (Enum flag in Enum.GetValues(typeof(TheSoroboreansInput)))
                if (_theSoroboreansInput.Value.HasFlag(flag))
                    yield return FlagToSkillTree(flag, true);
        if (_theThreeBrothersInput != null)
            foreach (Enum flag in Enum.GetValues(typeof(TheThreeBrothersInput)))
                if (_theThreeBrothersInput.Value.HasFlag(flag))
                    yield return FlagToSkillTree(flag, true);
    }
    static private IEnumerable<SkillSchool> GetOutputSkillTrees()
    {
        foreach (Enum flag in Enum.GetValues(typeof(VanillaOutput)))
            if (_vanillaOutput.Value.HasFlag(flag))
                yield return FlagToSkillTree(flag);
        if (_theSoroboreansOutput != null)
            foreach (Enum flag in Enum.GetValues(typeof(TheSoroboreansOutput)))
                if (_theSoroboreansOutput.Value.HasFlag(flag))
                    yield return FlagToSkillTree(flag);
        if (_theThreeBrothersOutput != null)
            foreach (Enum flag in Enum.GetValues(typeof(TheThreeBrothersOutput)))
                if (_theThreeBrothersOutput.Value.HasFlag(flag))
                    yield return FlagToSkillTree(flag);
    }
    static private IEnumerable<Trait<BaseSkillSlot>> GetTraits(IEnumerable<SkillSchool> trees)
    {
        EqualizedTraits traits = _equalizedTraits.Value;
        if (traits.HasFlag(EqualizedTraits.Count))
            yield return new Trait<BaseSkillSlot>("Count", slot => true);
        if (traits.HasFlag(EqualizedTraits.Types))
        {
            yield return new Trait<BaseSkillSlot>("Passive", slot => GetType(slot) == SlotType.Passive);
            yield return new Trait<BaseSkillSlot>("Active", slot => GetType(slot) == SlotType.Active);
        }
        if (traits.HasFlag(EqualizedTraits.Levels))
        {
            yield return new Trait<BaseSkillSlot>("Basic", slot => GetLevel(slot) == SlotLevel.Basic);
            yield return new Trait<BaseSkillSlot>("Advanced", slot => GetLevel(slot) == SlotLevel.Advanced);
        }
        if (traits.HasFlag(EqualizedTraits.Choices))
            yield return new Trait<BaseSkillSlot>("Choice", slot => IsChoice(slot));
        if (traits.HasFlag(EqualizedTraits.VanillaTrees))
            foreach (var tree in trees)
                yield return new Trait<BaseSkillSlot>(tree.Name, slot => GetVanillaTree(slot) == tree);
    }
    static private IEnumerable<BaseSkillSlot> GetSlotsFromTrees(IEnumerable<SkillSchool> trees)
    {
        foreach (var tree in trees)
            foreach (var slot in tree.m_skillSlots)
                if (_randomizeBreakthroughSkills || GetLevel(slot) != SlotLevel.Breakthrough)
                    yield return slot;
    }
    static private void ResetSkillTrees(IEnumerable<SkillSchool> trees)
    {
        foreach (var tree in trees)
        {
            var childObjects = new List<GameObject>();
            foreach (var child in tree.GetChildGameObjects())
                if (_randomizeBreakthroughSkills || !child.GetComponent<SkillBranch>().IsBreakthrough)
                    childObjects.Add(child);

            childObjects.DestroyInstantly();
            tree.m_skillSlots.Clear();
            tree.m_branches.Clear();
            tree.m_breakthroughSkillIndex = -1;
        }
    }
    static private void RandomizeSeed()
    => _seed.Value = UnityEngine.Random.value.MapFrom01(-1f, +1f).Mul(int.MaxValue).Round();
    static private void RandomizeSkills()
    {
        // Quit
        List<SkillSchool> outputTrees = GetOutputSkillTrees().ToList();
        if (outputTrees.IsNullOrEmpty())
            return;

        // Initialize
        IEnumerable<SkillSchool> intputTrees = GetInputSkillTrees();
        TraitEqualizer<BaseSkillSlot> equalizer = new(outputTrees.Count, GetTraits(intputTrees).ToArray());

        // Randomize (with equalization)
        Random.Initialize(_seed);
        foreach (var slot in GetSlotsFromTrees(intputTrees))
            equalizer.Add(slot);

        // Print results
        if (_rerollLogResults)
            LogResultsToFile(equalizer.GetResultsAsString());

        // Reset
        if (_affectOnlyChosenOutputTrees)
            ResetSkillTrees(outputTrees);
        else
            ResetSkillTrees(SkillTreeHolder.Instance.m_skillTrees);

        // Copy
        CopyEqualizedSlotsToOutputTrees(equalizer.Results, outputTrees);
    }
    static private void LogResultsToFile(string results)
    => System.IO.File.WriteAllText(InternalUtility.PluginFolderPath + REROLL_STATS_FILE_NAME, results);
    static private void CopyEqualizedSlotsToOutputTrees(IEnumerable<IEnumerable<BaseSkillSlot>> equalizedTrees, IList<SkillSchool> outputTrees)
    {
        foreach (var equalizedTree in equalizedTrees)
        {
            // Choose a random output
            SkillSchool randomOutputTree = outputTrees.Random();
            List<BaseSkillSlot> randomizedSlots = equalizedTree.ToList();

            // Add breakthrough
            BaseSkillSlot breakthroughSlot = randomOutputTree.BreakthroughSkill;
            if (_randomizeBreakthroughSkills)
            {
                IEnumerable<BaseSkillSlot> potentialBreakthroughs = randomizedSlots.Where(slot => GetLevel(slot) == SlotLevel.Advanced);
                if (potentialBreakthroughs.Any())
                {
                    // Prefer passives & avoid choices
                    IEnumerable<BaseSkillSlot> passives = _preferPassiveBreakthroughs
                                                        ? potentialBreakthroughs.Where(slot => GetType(slot) == SlotType.Passive)
                                                        : potentialBreakthroughs;
                    IEnumerable<BaseSkillSlot> nonChoices = _avoidChoiceBreakthroughs
                                                          ? potentialBreakthroughs.Where(slot => !IsChoice(slot))
                                                          : potentialBreakthroughs;
                    IEnumerable<BaseSkillSlot> intersection = passives.Intersect(nonChoices);
                    if (intersection.Any())
                        potentialBreakthroughs = intersection;
                    else if (passives.Any())
                        potentialBreakthroughs = passives;
                    else if (nonChoices.Any())
                        potentialBreakthroughs = nonChoices;

                    // Randomize
                    BaseSkillSlot randomAdvancedSlot = potentialBreakthroughs.ToArray().Random();
                    breakthroughSlot = CopySlot(randomAdvancedSlot);
                    breakthroughSlot.IsBreakthrough = true;
                    AddSlotToTree(breakthroughSlot, randomOutputTree, 2, 3);
                    randomizedSlots.Remove(randomAdvancedSlot);
                }
            }

            // Copy slots
            var outputAdvancedSlots = new List<BaseSkillSlot>();
            int basicIndex = 0, advancedIndex = 0;
            foreach (var slot in randomizedSlots)
            {
                // Cache
                bool isAdvanced = GetLevel(slot) == SlotLevel.Advanced;
                ref int index = ref (isAdvanced ? ref advancedIndex : ref basicIndex);

                // Ignore skills
                if (!SLOT_POSITIONS.IsValid(index))
                    continue;

                // Initialize slot
                BaseSkillSlot newSlot = CopySlot(slot);
                (int Column, int Row) = SLOT_POSITIONS[index++];
                if (isAdvanced)
                {
                    outputAdvancedSlots.Add(newSlot);
                    Row = 6 - Row;
                    Column = 4 - Column;
                }

                // Add slot
                AddSlotToTree(newSlot, randomOutputTree, Column, Row);
            }

            // Initialize branches and tree
            foreach (var branchHolder in randomOutputTree.GetChildGameObjects())
                if (!branchHolder.HasComponent<SkillBranch>())
                    branchHolder.AddComponent<SkillBranch>();
            randomOutputTree.Start();

            // Override RequiresBreakthrough and m_breakthroughSkillIndex
            foreach (var slot in outputAdvancedSlots)
            {
                slot.RequiresBreakthrough = true;
                if (slot.TryAs(out SkillSlotFork fork))
                    foreach (var forkSlot in fork.SkillsToChooseFrom)
                        forkSlot.RequiresBreakthrough = true;
            }
            randomOutputTree.m_breakthroughSkillIndex = breakthroughSlot != null ? randomOutputTree.m_skillSlots.IndexOf(breakthroughSlot) : -1;

            // remove current tree from outputs
            outputTrees.Remove(randomOutputTree);
        }
    }
    static private BaseSkillSlot CopySlot(BaseSkillSlot slot)
    {
        BaseSkillSlot newSlot = GameObject.Instantiate(slot);
        newSlot.RequiredSkillSlot = null;
        return newSlot;
    }
    static private void AddSlotToTree(BaseSkillSlot slot, SkillSchool tree, int column, int row)
    {
        slot.m_columnIndex = column;
        string rowName = row.ToString();
        if (!tree.FindChild(rowName).TryNonNull(out var branchHolder))
        {
            branchHolder = new GameObject(rowName);
            branchHolder.BecomeChildOf(tree);
        }
        slot.BecomeChildOf(branchHolder);
    }
    //
    static private bool HasDLC(OTWStoreAPI.DLCs dlc)
    => StoreManager.Instance.IsDlcInstalled(dlc);
    static private SkillSchool FlagToSkillTree(Enum flag, bool fromCache = false)
    {
        SkillTreeHolder skillTreeHolder = fromCache ? _cachedSkillTreeHolder : SkillTreeHolder.Instance;
        return !FlagToSkillTreeName(Convert.ToInt32(flag)).TryNonNull(out var treeName)
        || !skillTreeHolder.transform.TryFind(treeName, out var treeTransform)
        || !treeTransform.TryGetComponent(out SkillSchool tree) ? null : tree;
    }
    static private string FlagToSkillTreeName(int flag)
        => flag switch
        {
            1 << 1 => "ChersoneseEto",
            1 << 2 => "ChersoneseHermit",
            1 << 3 => "EmmerkarHunter",
            1 << 4 => "EmmerkarSage",
            1 << 5 => "HallowedMarshWarriorMonk",
            1 << 6 => "HallowedMarshPhilosopher",
            1 << 7 => "AbrassarMercenary",
            1 << 8 => "AbrassarRogue",
            1 << 9 => WEAPON_SKILLS_TREE_NAME,
            1 << 10 => BOONS_TREE_NAME,
            1 << 11 => "HarmattanSpeedster",
            1 << 12 => "HarmattanHexMage",
            1 << 13 => HEXES_TREE_NAME,
            1 << 14 => "CalderaThePrimalRitualist",
            1 << 15 => "CalderaWeaponMaster",
            _ => null,
        };
    static private SlotType GetType(BaseSkillSlot slot)
    {
        switch (slot)
        {
            case SkillSlot t:
                return t.Skill.IsPassive ? SlotType.Passive : SlotType.Active;
            case SkillSlotFork t:
                bool isFirstSkillPassive = t.SkillsToChooseFrom[0].Skill.IsPassive;
                if (isFirstSkillPassive == t.SkillsToChooseFrom[1].Skill.IsPassive)
                    return isFirstSkillPassive ? SlotType.Passive : SlotType.Active;
                return SlotType.Mixed;
            default: return 0;
        }
    }
    static private SlotLevel GetLevel(BaseSkillSlot slot)
        => _treatWeaponMasterAsAdvanced && GetVanillaTree(slot) == FlagToSkillTree(TheThreeBrothersInput.WeaponMaster, true) ? SlotLevel.Advanced
            : !slot.ParentBranch.ParentTree.BreakthroughSkill.TryNonNull(out var breakthroughSlot) ? SlotLevel.Basic
            : slot.ParentBranch.Index.CompareTo(breakthroughSlot.ParentBranch.Index) switch
            {
                -1 => SlotLevel.Basic,
                0 => _randomizeBreakthroughSkills ? SlotLevel.Advanced : SlotLevel.Breakthrough,
                +1 => SlotLevel.Advanced,
                _ => 0,
            };
    static private SkillSchool GetVanillaTree(BaseSkillSlot slot)
    => slot.ParentBranch.ParentTree;
    static private bool IsChoice(BaseSkillSlot slot)
    => slot is SkillSlotFork;

    // Hooks
    [HarmonyPatch(typeof(SkillTreeDisplay), nameof(SkillTreeDisplay.RefreshSkillsPosition)), HarmonyPrefix]
    static bool SkillTreeDisplay_RefreshSkillsPosition_Pre(SkillTreeDisplay __instance)
    {
        int basicCount = 0, advancedCount = 0;
        foreach (var slotDisplay in __instance.m_slotList)
            if (slotDisplay.GOActive())
                switch (GetLevel(slotDisplay.m_cachedBaseSlot))
                {
                    case SlotLevel.Basic: basicCount++; break;
                    case SlotLevel.Advanced: advancedCount++; break;
                }

        bool useCompactSpacing = basicCount.Max(advancedCount) > 10;
        bool hideTrainerName = advancedCount > 10;
        __instance.m_lblTreeName.GOSetActive(!hideTrainerName);
        __instance.RectTransform.localPosition = useCompactSpacing ? Vector2.zero : DEFAULT_TREE_LOCAL_POSITION;
        __instance.m_displaySlotSize = useCompactSpacing ? new Vector2(128, 112) : DEFAULT_SLOT_DISPLAY_SIZE;
        return true;
    }
}

/*
*             /*
        (MANA_TREE_NAME, new[]
        {
            "Spark".SkillID(),
            "Flamethrower".SkillID(),
        }),
        (INNATE_TREE_NAME, new[]
        {
            "Push Kick".SkillID(),
            "Throw Lantern".SkillID(),
            "Dagger Slash".SkillID(),
            "Fire/Reload".SkillID(),
        }),
        */

/*
*             // Log
        Log.Debug($"");
        Log.Debug($"TRAITS");
        foreach (var trait in equalizer.Traits)
            Log.Debug($"\t{trait.Name}");

        int counter = 0;
        foreach (var tree in equalizer.Results)
        {
            Log.Debug($"");
            Log.Debug($"TREE #{counter++}");

            List<BaseSkillSlot> sortedList = tree.ToList();
            sortedList.Sort((a, b) => GetSlotLevel(a).CompareTo(GetSlotLevel(b)));
            foreach (var slot in sortedList)
                Log.Debug($"\t{GetSlotLevel(slot)} / {slot.ParentBranch.ParentTree.Name} / {slot.name}");
        }
*/

/*
static public bool ContainsBasicSkillFromTree(SlotList slots, SkillSchool tree)
{
foreach (var slot in slots)
    if (GetSlotLevel(slot) == SlotLevel.Basic && slot.ParentBranch.ParentTree.name == tree.name)
        return true;
return false;
}
*/