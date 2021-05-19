using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using Random = UnityEngine.Random;



namespace ModPack
{
    public class SkillRandomizer : AMod, IDelayedInit, IDevelopmentOnly
    {
        #region const
        private const string ICONS_FOLDER = @"Skills\";
        private const string WEAPON_SKILLS_TREE_NAME = "WeaponSkills";
        private const string BOONS_TREE_NAME = "Boons";
        private const string HEXES_TREE_NAME = "Hexes";
        private const string MANA_TREE_NAME = "Mana";
        private const string INNATE_TREE_NAME = "Innate";
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
                "Flamethrower".SkillID(),
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

            (0, 0),
            (1, 0),
            (2, 0),
            (3, 0),
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
            Trees = 1 << 4,
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
        static private ModSetting<bool> _execute;
        static private ModSetting<bool> _randomizeSeed;
        static private ModSetting<int> _seed;
        static private ModSetting<VanillaInput> _vanillaInput;
        static private ModSetting<TheSoroboreansInput> _theSoroboreansInput;
        static private ModSetting<TheThreeBrothersInput> _theThreeBrothersInput;
        static private ModSetting<VanillaOutput> _vanillaOutput;
        static private ModSetting<TheSoroboreansOutput> _theSoroboreansOutput;
        static private ModSetting<TheThreeBrothersOutput> _theThreeBrothersOutput;
        static private ModSetting<EqualizedTraits> _equalizedTraits;
        static private ModSetting<bool> _treatBreakthroughAsAdvanced;
        static private ModSetting<bool> _treatWeaponMasterAsAdvanced;
        override protected void Initialize()
        {
            _execute = CreateSetting(nameof(_execute), false);
            _seed = CreateSetting(nameof(_seed), 0);
            _randomizeSeed = CreateSetting(nameof(_randomizeSeed), false);
            _equalizedTraits = CreateSetting(nameof(_equalizedTraits), (EqualizedTraits)~0);
            _treatBreakthroughAsAdvanced = CreateSetting(nameof(_treatBreakthroughAsAdvanced), true);
            _treatWeaponMasterAsAdvanced = CreateSetting(nameof(_treatWeaponMasterAsAdvanced), true);

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

            // Initialize trees
            CacheSkillTreeHolder();
            CreateSideSkillTrees();
            LoadMissingSkillIcons();

            // Events
            _execute.AddEvent(() =>
            {
                if (!_execute)
                    return;

                RandomizeSkills();
                _execute.SetSilently(false);
            });
            _randomizeSeed.AddEvent(() =>
            {
                if (!_randomizeSeed)
                    return;

                RandomizeSeed();
                _randomizeSeed.SetSilently(false);
            });
        }
        override protected void SetFormatting()
        {
            _execute.Format("Execute");
            _seed.Format("Seed");
            Indent++;
            {
                _randomizeSeed.Format("Randomize seed");
                Indent--;
            }
            _equalizedTraits.Format("Try to equalize");
            _treatBreakthroughAsAdvanced.Format("Treat breakthrough as advanced");
            _treatBreakthroughAsAdvanced.Description = "Breakthrough skills will be treated as advanced skills";
            _treatWeaponMasterAsAdvanced.Format("Treat weapon master as advanced");
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
                if (setting != null)
                {
                    string name = setting == _vanillaInput ? "Input skill trees"
                                                           : setting == _vanillaOutput ? "Output skill trees" : "";
                    setting.Format(name);
                    setting.DisplayResetButton = false;
                }
        }
        override protected string Description
        => "• Randomize skills taught by trainers";
        override protected string SectionOverride
        => SECTION_SKILLS;

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
            GameObject.DontDestroyOnLoad(newSkillTreeHolder);
        }
        static private void CreateSideSkillTrees()
        {
            _sideSkillTrees = new List<SkillSchool>();
            foreach (var (Name, IDs) in SIDE_SKILLS)
            {
                GameObject skillTreeHolder = new GameObject(Name);
                skillTreeHolder.BecomeChildOf(_cachedSkillTreeHolder);
                GameObject skillBranchHolder = new GameObject("0");
                skillBranchHolder.BecomeChildOf(skillTreeHolder);

                foreach (var id in IDs)
                {
                    Skill skill = Prefabs.SkillsByID[id];
                    GameObject skillSlotHolder = new GameObject(skill.Name);
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
                Prefabs.SkillsByID[id].SkillTreeIcon = Utility.CreateSpriteFromFile(Utility.PluginFolderPath + ICONS_FOLDER + name.Replace('/', '_') + ".PNG");
            }
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
                yield return new Trait<BaseSkillSlot>("Passive", slot => GetSlotType(slot) == SlotType.Passive);
                yield return new Trait<BaseSkillSlot>("Active", slot => GetSlotType(slot) == SlotType.Active);
            }
            if (traits.HasFlag(EqualizedTraits.Levels))
            {
                yield return new Trait<BaseSkillSlot>("Basic", slot => GetSlotLevel(slot) == SlotLevel.Basic);
                yield return new Trait<BaseSkillSlot>("Advanced", slot => GetSlotLevel(slot) == SlotLevel.Advanced);
            }
            if (traits.HasFlag(EqualizedTraits.Trees))
                foreach (var tree in trees)
                    yield return new Trait<BaseSkillSlot>(tree.Name, slot => GetSlotTree(slot) == tree);

        }
        static private IEnumerable<BaseSkillSlot> GetSlotsFromTrees(IEnumerable<SkillSchool> trees)
        {
            foreach (var tree in trees)
                foreach (var slot in tree.m_skillSlots)
                    if (_treatBreakthroughAsAdvanced || GetSlotLevel(slot) != SlotLevel.Breakthrough)
                        yield return slot;
        }
        static private void ResetSkillTrees(IEnumerable<SkillSchool> trees)
        {
            foreach (var tree in trees)
            {
                tree.GetChildren().ToArray().DestroyImmediately();
                tree.m_skillSlots.Clear();
                tree.m_branches.Clear();
                tree.m_breakthroughSkillIndex = -1;
            }
        }
        static private void RandomizeSeed()
        => _seed.Value = Random.value.MapFrom01(-1f, +1f).Mul(int.MaxValue).Round();
        static private void RandomizeSkills()
        {
            // Quit
            List<SkillSchool> outputTrees = GetOutputSkillTrees().ToList();
            if (outputTrees.IsEmpty())
                return;

            // Initialize
            IEnumerable<SkillSchool> intputTrees = GetInputSkillTrees();
            TraitEqualizer<BaseSkillSlot> equalizer = new TraitEqualizer<BaseSkillSlot>(outputTrees.Count, GetTraits(intputTrees).ToArray());

            // Execute
            Random.InitState(_seed);
            foreach (var slot in GetSlotsFromTrees(intputTrees))
                equalizer.Add(slot);

            CopyEqualizedSlotsToOutputTrees(equalizer.Results, outputTrees);
        }
        static private void CopyEqualizedSlotsToOutputTrees(IEnumerable<IEnumerable<BaseSkillSlot>> equalizedTrees, IList<SkillSchool> outputTrees)
        {
            // Output
            ResetSkillTrees(outputTrees);
            foreach (var equalizedTree in equalizedTrees)
            {
                // Choose a random output
                SkillSchool randomOutputTree = outputTrees.Random();

                // Add breakthrough
                List<BaseSkillSlot> randomizedSlots = equalizedTree.ToList();
                BaseSkillSlot randomAdvancedSlot = randomizedSlots.Where(slot => GetSlotLevel(slot) == SlotLevel.Advanced).ToArray().Random();
                BaseSkillSlot breakthroughSlot = CopySlot(randomAdvancedSlot);
                breakthroughSlot.IsBreakthrough = true;
                AddSlotToTree(breakthroughSlot, randomOutputTree, 2, 3);
                randomizedSlots.Remove(randomAdvancedSlot);

                // Copy slots
                var outputAdvancedSlots = new List<BaseSkillSlot>();
                int basicIndex = 0, advancedIndex = 0;
                foreach (var slot in randomizedSlots)
                {
                    // Cache
                    bool isAdvanced = GetSlotLevel(slot) == SlotLevel.Advanced;
                    ref int index = ref (isAdvanced ? ref advancedIndex : ref basicIndex);

                    // Ignore skills
                    if (!SLOT_POSITIONS.IsIndexValid(index))
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
                foreach (var branchHolder in randomOutputTree.GetChildren())
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
                randomOutputTree.m_breakthroughSkillIndex = randomOutputTree.m_skillSlots.IndexOf(breakthroughSlot);

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
            if (!tree.FindChild(rowName).TryAssign(out var branchHolder))
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
            if (!FlagToSkillTreeName(Convert.ToInt32(flag)).TryAssign(out var treeName)
            || !skillTreeHolder.transform.TryFind(treeName, out var treeTransform)
            || !treeTransform.TryGetComponent(out SkillSchool tree))
                return null;

            return tree;
        }
        static private string FlagToSkillTreeName(int flag)
        {
            switch (flag)
            {
                case 1 << 1: return "ChersoneseEto";
                case 1 << 2: return "ChersoneseHermit";
                case 1 << 3: return "EmmerkarHunter";
                case 1 << 4: return "EmmerkarSage";
                case 1 << 5: return "HallowedMarshWarriorMonk";
                case 1 << 6: return "HallowedMarshPhilosopher";
                case 1 << 7: return "AbrassarMercenary";
                case 1 << 8: return "AbrassarRogue";
                case 1 << 9: return WEAPON_SKILLS_TREE_NAME;
                case 1 << 10: return BOONS_TREE_NAME;
                case 1 << 11: return "HarmattanSpeedster";
                case 1 << 12: return "HarmattanHexMage";
                case 1 << 13: return HEXES_TREE_NAME;
                case 1 << 14: return "CalderaThePrimalRitualist";
                case 1 << 15: return "CalderaWeaponMaster";
                default: return null;
            }
        }
        static private SlotType GetSlotType(BaseSkillSlot slot)
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
        static private SlotLevel GetSlotLevel(BaseSkillSlot slot)
        {
            if (_treatWeaponMasterAsAdvanced
            && GetSlotTree(slot) == FlagToSkillTree(TheThreeBrothersInput.WeaponMaster, true))
                return SlotLevel.Advanced;

            if (!slot.ParentBranch.ParentTree.BreakthroughSkill.TryAssign(out var breakthroughSlot))
                return SlotLevel.Basic;

            switch (slot.ParentBranch.Index.CompareTo(breakthroughSlot.ParentBranch.Index))
            {
                case -1: return SlotLevel.Basic;
                case 0: return _treatBreakthroughAsAdvanced ? SlotLevel.Advanced : SlotLevel.Breakthrough;
                case +1: return SlotLevel.Advanced;
                default: return 0;
            }
        }
        static private SkillSchool GetSlotTree(BaseSkillSlot slot)
        => slot.ParentBranch.ParentTree;
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
            Tools.Log($"");
            Tools.Log($"TRAITS");
            foreach (var trait in equalizer.Traits)
                Tools.Log($"\t{trait.Name}");

            int counter = 0;
            foreach (var tree in equalizer.Results)
            {
                Tools.Log($"");
                Tools.Log($"TREE #{counter++}");

                List<BaseSkillSlot> sortedList = tree.ToList();
                sortedList.Sort((a, b) => GetSlotLevel(a).CompareTo(GetSlotLevel(b)));
                foreach (var slot in sortedList)
                    Tools.Log($"\t{GetSlotLevel(slot)} / {slot.ParentBranch.ParentTree.Name} / {slot.name}");
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

/*
static private void TryReset()
{
    if (_cachedSkillTreeHolder == null)
        return;

    // Destroy side skill trees
    foreach (var sideSkillTree in _sideSkillTrees)
    {
        sideSkillTree.Unparent();
        _sideSkillTrees.DestroyObjects();
    }

    // Destroy real SkillTreeHolder
    SkillTreeHolder.Instance.DestroyObject();

    // Copy and destroy cached SkillTreeHolder
    CopyCachedSkillTreeHolder();
    _cachedSkillTreeHolder.DestroyObject();
}
*/