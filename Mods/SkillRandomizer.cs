using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using Random = UnityEngine.Random;
using SlotList = System.Collections.Generic.List<BaseSkillSlot>;
using SlotList2D = System.Collections.Generic.List<System.Collections.Generic.List<BaseSkillSlot>>;
using SlotList3D = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<BaseSkillSlot>>>;



namespace ModPack
{
    public class SkillRandomizer : AMod, IDelayedInit
    {
        #region const
        private const string WEAPON_SKILLS_TREE_NAME = "WeaponSkills";
        private const string BOONS_TREE_NAME = "Boons";
        private const string HEXES_TREE_NAME = "Hexes";
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
        private enum EqualizerTypes
        {
            Slots = 1 << 1,
            Trees = 1 << 2,
            Levels = 1 << 3,
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
        static private ModSetting<bool> _randomToggle;
        static private ModSetting<bool> _randomExecute;
        static private ModSetting<bool> _randomRandomizeSeed;
        static private ModSetting<int> _randomSeed;
        static private ModSetting<VanillaInput> _vanillaInput;
        static private ModSetting<TheSoroboreansInput> _theSoroboreansInput;
        static private ModSetting<TheThreeBrothersInput> _theThreeBrothersInput;
        static private ModSetting<VanillaOutput> _vanillaOutput;
        static private ModSetting<TheSoroboreansOutput> _theSoroboreansOutput;
        static private ModSetting<TheThreeBrothersOutput> _theThreeBrothersOutput;
        static private ModSetting<EqualizerTypes> _randomEqualizerTypes;
        static private ModSetting<bool> _randomAdvancedRequiresBasic;
        static private ModSetting<bool> _randomRandomizeBreakthrough;
        static private ModSetting<bool> _randomTreatBreakthroughAsAdvanced;
        override protected void Initialize()
        {
            _randomToggle = CreateSetting(nameof(_randomToggle), false);
            _randomExecute = CreateSetting(nameof(_randomExecute), false);
            _randomSeed = CreateSetting(nameof(_randomSeed), 0, IntRange(-int.MaxValue, +int.MaxValue));
            _randomRandomizeSeed = CreateSetting(nameof(_randomRandomizeSeed), false);
            _randomEqualizerTypes = CreateSetting(nameof(_randomEqualizerTypes), (EqualizerTypes)~0);
            _randomAdvancedRequiresBasic = CreateSetting(nameof(_randomAdvancedRequiresBasic), true);
            _randomRandomizeBreakthrough = CreateSetting(nameof(_randomRandomizeBreakthrough), true);
            _randomTreatBreakthroughAsAdvanced = CreateSetting(nameof(_randomTreatBreakthroughAsAdvanced), true);

            // inputs/outputs
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
            _randomToggle.AddEvent(() =>
            {
                if (_randomToggle)
                {
                    CacheSkillTreeHolder();
                    CreateSideSkillTrees();
                }
                else
                    TryReset();
            });
            _randomExecute.AddEvent(() =>
            {
                if (!_randomExecute)
                    return;

                RandomizeSkills();
                _randomExecute.SetSilently(false);
            });
            _randomRandomizeSeed.AddEvent(() =>
            {
                if (!_randomRandomizeSeed)
                    return;

                RandomizeSeed();
                _randomRandomizeSeed.SetSilently(false);
            });
        }
        override protected void SetFormatting()
        {
            _randomToggle.Format("Randomizer");
            Indent++;
            {
                _randomExecute.Format("Execute", _randomToggle);
                _randomSeed.Format("Seed", _randomToggle);
                Indent++;
                {
                    _randomRandomizeSeed.Format("Randomize seed", _randomToggle);
                    Indent--;
                }
                _randomEqualizerTypes.Format("Try to equalize", _randomToggle);
                _randomAdvancedRequiresBasic.Format("Pair advanced skills with basic", _randomToggle);
                _randomRandomizeBreakthrough.Format("Randomize breakthrough skills", _randomToggle);
                Indent++;
                {
                    _randomTreatBreakthroughAsAdvanced.Format("Treat as advanced", _randomRandomizeBreakthrough);
                    Indent--;
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
                    string name = setting == _vanillaInput ? "Input skill trees"
                                                           : setting == _vanillaOutput ? "Output skill trees" : "";
                    setting.Format(name, _randomToggle);
                    setting.DisplayResetButton = false;
                }
                Indent--;
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
        static private List<SkillSchool> GetInputSkillTrees()
        {
            List<SkillSchool> trees = new List<SkillSchool>();
            foreach (Enum flag in Enum.GetValues(typeof(VanillaInput)))
                if (_vanillaInput.Value.HasFlag(flag))
                    trees.Add(FlagToSkillTree(flag, true));
            foreach (Enum flag in Enum.GetValues(typeof(TheSoroboreansInput)))
                if (_theSoroboreansInput.Value.HasFlag(flag))
                    trees.Add(FlagToSkillTree(flag, true));
            foreach (Enum flag in Enum.GetValues(typeof(TheThreeBrothersInput)))
                if (_theThreeBrothersInput.Value.HasFlag(flag))
                    trees.Add(FlagToSkillTree(flag, true));
            return trees;
        }
        static private List<SkillSchool> GetOutputSkillTrees()
        {
            List<SkillSchool> trees = new List<SkillSchool>();
            foreach (Enum flag in Enum.GetValues(typeof(VanillaOutput)))
                if (_vanillaOutput.Value.HasFlag(flag))
                    trees.Add(FlagToSkillTree(flag));
            foreach (Enum flag in Enum.GetValues(typeof(TheSoroboreansOutput)))
                if (_theSoroboreansOutput.Value.HasFlag(flag))
                    trees.Add(FlagToSkillTree(flag));
            foreach (Enum flag in Enum.GetValues(typeof(TheThreeBrothersOutput)))
                if (_theThreeBrothersOutput.Value.HasFlag(flag))
                    trees.Add(FlagToSkillTree(flag));
            return trees;
        }
        static private SlotList3D GetSlotListsByTreeAndByType(List<SkillSchool> trees, bool shuffleSlotLists = false, bool shuffleTrees = false, bool shuffleTypes = false)
        {
            SlotList3D slotListsByTreeAndByType = new SlotList3D();
            foreach (var skillType in new[] { SlotLevel.Basic, SlotLevel.Breakthrough, SlotLevel.Advanced })
            {
                SlotList2D slotListsByTree = new SlotList2D();
                foreach (var tree in trees)
                {
                    SlotList slotList = new SlotList();
                    foreach (var slot in tree.m_skillSlots)
                    {
                        if (GetSlotLevel(slot, true) == skillType)
                            slotList.Add(slot);
                    }
                    if (shuffleSlotLists)
                        slotList.Shuffle();
                    slotListsByTree.Add(slotList);
                }
                if (shuffleTrees)
                    slotListsByTree.Shuffle();
                slotListsByTreeAndByType.Add(slotListsByTree);
            }
            if (shuffleTypes)
                slotListsByTreeAndByType.Shuffle();
            return slotListsByTreeAndByType;
        }
        static private void ResetSkillTrees(List<SkillSchool> trees)
        {
            foreach (var tree in trees)
            {
                tree.GetChildren().DestroyImmediately();
                tree.m_skillSlots.Clear();
                tree.m_branches.Clear();
                tree.m_breakthroughSkillIndex = -1;
            }
        }
        static private void RandomizeSeed()
        => _randomSeed.Value = Random.value.MapFrom01(-1f, +1f).Mul(int.MaxValue).Round();
        static private void RandomizeSkills()
        {
            // Quit
            List<SkillSchool> outputTrees = GetOutputSkillTrees();
            if (outputTrees.IsEmpty())
                return;

            Tools.IsStopwatchActive = true;

            // Initialize random generator, input/output lists and equalizers
            Random.InitState(_randomSeed);
            SlotList3D inputSlotListsByTreeAndByType = GetSlotListsByTreeAndByType(GetInputSkillTrees(), true);
            SlotList2D outputSlotLists = Utility.CreateList2D<BaseSkillSlot>(outputTrees.Count);
            var equalizersByType = new Dictionary<EqualizerTypes, SlotList2D>
            {
                [EqualizerTypes.Slots] = new SlotList2D(),
                [EqualizerTypes.Trees] = new SlotList2D(),
                [EqualizerTypes.Levels] = new SlotList2D(),
            };
            List<EqualizerTypes> activeEqualizerTypes = new List<EqualizerTypes>();
            foreach (var type in Utility.GetEnumValues<EqualizerTypes>())
                if (_randomEqualizerTypes.Value.HasFlag(type))
                    activeEqualizerTypes.Add(type);

            // Randomization logic
            foreach (var inputSlotListsByTree in inputSlotListsByTreeAndByType)
            {
                equalizersByType[EqualizerTypes.Levels] = new SlotList2D(outputSlotLists);
                foreach (var inputSlotList in inputSlotListsByTree)
                {
                    equalizersByType[EqualizerTypes.Trees] = new SlotList2D(outputSlotLists);
                    foreach (var inputSlot in inputSlotList)
                    {
                        // skip breakthrough slots if the setting isn't enabled
                        if (GetSlotLevel(inputSlot, true) == SlotLevel.Breakthrough && !_randomRandomizeBreakthrough)
                            continue;

                        // reset empty equalizers and find intersection
                        IEnumerable<SlotList> intersection = new SlotList2D(outputSlotLists);
                        foreach (var type in activeEqualizerTypes)
                        {
                            if (equalizersByType[type].IsEmpty())
                                equalizersByType[type] = new SlotList2D(outputSlotLists);
                            intersection = intersection.Intersect(equalizersByType[type]);
                        }

                        // pair advanced slots with basic if the setting is enabled
                        if (GetSlotLevel(inputSlot, true) == SlotLevel.Advanced && _randomAdvancedRequiresBasic)
                        {
                            SlotList2D allowedLists = new SlotList2D();
                            foreach (var outputSlotList in outputSlotLists)
                                if (ContainsBasicSkillFromTree(outputSlotList, inputSlot.ParentBranch.ParentTree))
                                {
                                    allowedLists.Add(outputSlotList);
                                    continue;
                                }

                            // if no intersection found, override equalization
                            intersection = intersection.Intersect(allowedLists);
                            if (!intersection.Any())
                                intersection = allowedLists;
                        }

                        // add slot & remove lists from equalizers
                        SlotList randomOutputSlotList = intersection.ToArray().Random();
                        randomOutputSlotList.Add(inputSlot);
                        foreach (var equalizerType in activeEqualizerTypes)
                            equalizersByType[equalizerType].Remove(randomOutputSlotList);
                    }
                }
            }

            // Log
            int counter = 0;
            foreach (var tree in outputSlotLists)
                if (!tree.IsEmpty())
                {
                    Tools.Log($"");
                    Tools.Log($"TREE #{counter++}");
                    foreach (var slot in tree)
                        Tools.Log($"\t{GetSlotLevel(slot)} / {slot.ParentBranch.ParentTree.Name} / {slot.name}");
                }

            // Output
            ResetSkillTrees(outputTrees);

            foreach (var outputTree in outputTrees)
            {
                int row = 0, column = 0;
                GameObject skillBranchHolder = new GameObject(row++.ToString());
                SlotList randomOutputSlotList = outputSlotLists.Random();
                foreach (var outputSlot in randomOutputSlotList)
                {
                    BaseSkillSlot newSlot = GameObject.Instantiate(outputSlot, skillBranchHolder.transform);
                    newSlot.m_columnIndex = column++;
                    newSlot.RequiredSkillSlot = null;
                    newSlot.IsBreakthrough = GetSlotLevel(outputSlot) == SlotLevel.Breakthrough;
                    newSlot.RequiresBreakthrough = GetSlotLevel(outputSlot) == SlotLevel.Advanced;

                    if (column > 4 || outputSlot == randomOutputSlotList.Last())
                    {
                        skillBranchHolder.BecomeChildOf(outputTree);
                        skillBranchHolder.AddComponent<SkillBranch>();
                        skillBranchHolder = new GameObject(row++.ToString());
                        column = 0;
                    }
                }

                outputTree.Start();
                outputSlotLists.Remove(randomOutputSlotList);
            }

            // Diagnostics
            Tools.Log($"");
            Tools.Log($"Execution time: {Tools.ElapsedMilliseconds}ms");
            Tools.IsStopwatchActive = false;
        }
        //
        static private bool HasDLC(OTWStoreAPI.DLCs dlc)
=> StoreManager.Instance.IsDlcInstalled(dlc);
        static private SkillSchool FlagToSkillTree(Enum flag, bool fromCached = false)
        {
            SkillTreeHolder skillTreeHolder = fromCached ? _cachedSkillTreeHolder : SkillTreeHolder.Instance;
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
        static public bool ContainsBasicSkillFromTree(SlotList slots, SkillSchool tree)
        {
            foreach (var slot in slots)
                if (GetSlotLevel(slot) == SlotLevel.Basic && slot.ParentBranch.ParentTree.name == tree.name)
                    return true;
            return false;
        }
        static private SlotLevel GetSlotLevel(BaseSkillSlot slot, bool respectBreakthroughAsAdvanced = false)
        {
            if (!slot.ParentBranch.ParentTree.BreakthroughSkill.TryAssign(out var breakthroughSlot))
                return SlotLevel.Basic;

            bool treatAsAdvanced = respectBreakthroughAsAdvanced && _randomTreatBreakthroughAsAdvanced;
            switch (slot.ParentBranch.Index.CompareTo(breakthroughSlot.ParentBranch.Index))
            {
                case -1: return SlotLevel.Basic;
                case 0: return treatAsAdvanced ? SlotLevel.Advanced : SlotLevel.Breakthrough;
                case +1: return SlotLevel.Advanced;
                default: return 0;
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
    }
}