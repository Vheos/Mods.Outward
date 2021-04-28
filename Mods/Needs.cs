using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using System;



/* TO DO:
 * - add drink effects to potions
 * - display effect values in status effect description
 */
namespace ModPack
{
    public class Needs : AMod, IDelayedInit
    {
        #region const
        private const string ICONS_FOLDER = @"Needs\";
        private const float DEFAULT_MAX_NEED_VALUE = 1000f;
        static private readonly (Need Need, Vector2 Thresholds, float DepletionRate, string NegativeName, string ActionName, string AffectedStat)[] NEEDS_DATA =
        {
            (Need.Food, new Vector2(50f, 75f), 1000f / 15.00f, "Hungry", "eating", "health burn rate"),
            (Need.Drink, new Vector2(50f, 75f), 1000f / 27.77f, "Thirsty", "drinking", "stamina burn rate"),
            (Need.Sleep, new Vector2(25f, 50f), 1000f / 13.89f, "Tired", "sleeping", "stamina regen"),
        };
        static private readonly Dictionary<int, (Character.SpellCastType Vanilla, Character.SpellCastType Custom)> ANIMATION_PAIRS_BY_INGESTIBLE_ID
        = new Dictionary<int, (Character.SpellCastType, Character.SpellCastType)>
        {
            ["Torcrab Egg".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Boreo Blubber".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Pungent Paste".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Gaberry Jam".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Crawlberry Jam".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Golden Jam".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Raw Torcrab Meat".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Miner’s Omelet".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Turmmip Potage".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Meat Stew".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Marshmelon Jelly".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Blood Mushroom".ItemID()] = (Character.SpellCastType.Potion, Character.SpellCastType.Eat),
            ["Food Waste".ItemID()] = (Character.SpellCastType.DrinkWater, Character.SpellCastType.Eat),
            ["Warm Boozu’s Milk".ItemID()] = (Character.SpellCastType.Potion, Character.SpellCastType.DrinkWater),
        };
        static private int[] CURE_DRINK_IDS = new[]
        {
            "Panacea".ItemID(),
            "Antidote".ItemID(),
            "Hex Cleaner".ItemID(),
            "Invigorating Potion".ItemID(),
        };
        static private int[] TEA_DRINK_IDS = new[]
        {
            "Able Tea".ItemID(),
            "Bitter Spicy Tea".ItemID(),
            "Greasy Tea".ItemID(),
            "Iced Tea".ItemID(),
            "Mineral Tea".ItemID(),
            "Needle Tea".ItemID(),
            "Soothing Tea".ItemID(),
            "Boozu’s Milk".ItemID(),
            "Warm Boozu’s Milk".ItemID(),
            "Gaberry Wine".ItemID(),
            "Gep's Drink".ItemID(),
        };
        static private int[] OTHER_DRINK_IDS = new[]
        {
            "Boozu’s Milk".ItemID(),
            "Warm Boozu’s Milk".ItemID(),
            "Gaberry Wine".ItemID(),
            "Gep's Drink".ItemID(),
        };
        static private int[] MILK_IDS = new[]
        {
            "Boozu’s Milk".ItemID(),
            "Warm Boozu’s Milk".ItemID(),
        };
        static private string[] DOT_STATUS_EFFECTS = new[]
        {
            "Poisoned",
            "Poisoned +",
            "Food Poisoned",
            "Food Poisoned +",
            "Hallowed Marsh Poison Lvl1",
            "Hallowed Marsh Poison Lvl2",
            "SulphurPoison",
            "Blaze",
            "Burning",
            "HolyBlaze",
            "Immolate",
            "Plague",
            "Bleeding",
            "Bleeding +",
            "Blood Leech Victim",
            "Gift of Blood",
            "Freezing",
            "Infection1",
            "Infection2",
            "Infection3",
        };
        #endregion
        #region enum
        private enum Need
        {
            Food = 0,
            Drink = 1,
            Sleep = 2,
        }
        #endregion
        #region struct
        private struct FulfilledData
        {
            // Fields
            public string Name;
            public string Description;
            public string StatusNotification;
            public string PreventedNotification;
            public StatusEffect StatusEffect;


            // Constructors
            public FulfilledData(StatusEffect statusEffect, string name, string description, string statusNotification = null, string preventedNotification = null)
            {
                StatusEffect = statusEffect;
                Name = name;
                Description = description;
                StatusNotification = statusNotification ?? description;
                PreventedNotification = preventedNotification ?? statusNotification;
            }
        }
        #endregion
        #region class

        private class NeedSettings
        {
            // Settings
            public ModSetting<bool> _toggle;
            public ModSetting<Vector2> _thresholds;
            public ModSetting<Vector2> _depletionRate;
            public ModSetting<int> _fulfilledLimit;
            public ModSetting<int> _fulfilledEffectValue;

            // Utility
            public bool LimitingEnabled
            => _fulfilledLimit > 100;
            public float DepletionPerHour
            => _depletionRate.Value.x / _depletionRate.Value.y * 10f;
        }
        #endregion

        // Settings
        static private Dictionary<Need, NeedSettings> _settingsByNeed;
        static private ModSetting<int> _sleepNegativeEffect;
        static private ModSetting<bool> _sleepNegativeEffectIsPercent;
        static private ModSetting<int> _sleepBuffsDuration;
        static private ModSetting<bool> _drinkValuesToggle;
        static private ModSetting<int> _drinkValuesPotions, _drinkValuesCures, _drinkValuesTeas, _drinkValuesOther;
        static private ModSetting<bool> _allowCuresWhileOverlimited;
        static private ModSetting<bool> _allowOnlyDOTCures;
        static private ModSetting<bool> _noFoodOrDrinkOverlimitAfterSleep;
        static private ModSetting<bool> _dontRestoreNeedsOnTravel;
        override protected void Initialize()
        {
            _settingsByNeed = new Dictionary<Need, NeedSettings>();
            foreach (var data in NEEDS_DATA)
            {
                NeedSettings tmp = new NeedSettings();
                _settingsByNeed[data.Need] = tmp;

                string needPrefix = $"{data.Need} ";
                tmp._toggle = CreateSetting(needPrefix + nameof(tmp._toggle), false);
                tmp._thresholds = CreateSetting(needPrefix + nameof(tmp._thresholds), data.Thresholds);
                tmp._depletionRate = CreateSetting(needPrefix + nameof(tmp._depletionRate), new Vector2(100f, data.DepletionRate));
                tmp._fulfilledLimit = CreateSetting(needPrefix + nameof(tmp._fulfilledLimit), 120, IntRange(100, 200));

                int defaultValue = data.Need == Need.Sleep ? 115 : 33;
                AcceptableValueRange<int> range = data.Need == Need.Sleep ? IntRange(100, 200) : IntRange(0, 100);
                tmp._fulfilledEffectValue = CreateSetting(needPrefix + nameof(tmp._fulfilledEffectValue), defaultValue, range);
            }
            _drinkValuesToggle = CreateSetting(nameof(_drinkValuesToggle), false);
            _drinkValuesPotions = CreateSetting(nameof(_drinkValuesPotions), 10, IntRange(0, 100));
            _drinkValuesCures = CreateSetting(nameof(_drinkValuesCures), 10, IntRange(0, 100));
            _drinkValuesTeas = CreateSetting(nameof(_drinkValuesTeas), 20, IntRange(0, 100));
            _drinkValuesOther = CreateSetting(nameof(_drinkValuesOther), 20, IntRange(0, 100));
            _sleepNegativeEffect = CreateSetting(nameof(_sleepNegativeEffect), -12, IntRange(-100, 0));
            _sleepNegativeEffectIsPercent = CreateSetting(nameof(_sleepNegativeEffectIsPercent), false);
            _sleepBuffsDuration = CreateSetting(nameof(_sleepBuffsDuration), 40, IntRange(0, 100));
            _allowCuresWhileOverlimited = CreateSetting(nameof(_allowCuresWhileOverlimited), false);
            _allowOnlyDOTCures = CreateSetting(nameof(_allowOnlyDOTCures), false);
            _noFoodOrDrinkOverlimitAfterSleep =  CreateSetting(nameof(_noFoodOrDrinkOverlimitAfterSleep), false);
            _dontRestoreNeedsOnTravel = CreateSetting(nameof(_dontRestoreNeedsOnTravel), false);

            // Events
            AddEventOnConfigClosed(() =>
            {
                if (!_isInitialized)
                {
                    UpdateIngestibleAnimations(true);
                    InitializeStatusEffectPrefabs();
                    RemoveMilkFoodValues();
                    _isInitialized = true;
                }

                UpdateStatusEffectPrefabsData();
                UpdateDrinkValues();
                UpdateSleepBuffsDuration();
                foreach (var player in Players.Local)
                {
                    PlayerCharacterStats stats = player.Character.PlayerStats;
                    UpdateThresholds(stats);
                    UpdateDepletionRates(stats);
                }
            });
        }
        override protected void SetFormatting()
        {
            foreach (var data in NEEDS_DATA)
            {
                NeedSettings tmp = _settingsByNeed[data.Need];

                tmp._toggle.Format(data.Need.ToString());
                tmp._toggle.Description = $"Change {data.Need}-related settings";
                Indent++;
                {
                    tmp._thresholds.Format("Thresholds", tmp._toggle);
                    tmp._thresholds.Description = $"When your {data.Need} falls below Y%, you become {data.NegativeName}\n" +
                                                 $"When your {data.Need} falls below X%, you become Very {data.NegativeName}";
                    tmp._depletionRate.Format("Depletion rate", tmp._toggle);
                    tmp._depletionRate.Description = $"You lose X% of {data.Need} per Y hours";
                    tmp._fulfilledLimit.Format("Overlimit", tmp._toggle);
                    string decOrInc = data.Need == Need.Sleep ? "increases" : "decreases";
                    tmp._fulfilledLimit.Description = $"Allows your {data.Need} to go over 100%\n" +
                                                     $"You will receive a special status effect that {decOrInc} your {data.AffectedStat} but " +
                                                     $"prevents you from {data.ActionName} until your {data.Need} falls below 100% again";
                    Indent++;
                    {
                        tmp._fulfilledEffectValue.Format(data.AffectedStat, tmp._fulfilledLimit, () => tmp._fulfilledLimit > 100);
                        if (data.Need == Need.Sleep)
                        {
                            _sleepNegativeEffect.Format("mana / min", tmp._fulfilledLimit, () => tmp._fulfilledLimit > 100);
                            _sleepNegativeEffectIsPercent.Format("is % of max mana");
                        }
                        Indent--;
                    }

                    if (data.Need == Need.Sleep)
                    {
                        _sleepBuffsDuration.Format("Buffs duration", tmp._toggle);
                        _sleepBuffsDuration.Description = "Affects buffs granted by sleeping in houses, inns and tents\n" +
                                                          "(in real-time minutes)";
                    }

                    if (data.Need == Need.Drink)
                    {
                        _drinkValuesToggle.Format("Items' drink values", tmp._toggle);
                        _drinkValuesToggle.Description = "Set how much drink is restored by each drink type";
                        Indent++;
                        {
                            _drinkValuesPotions.Format("Potions", _drinkValuesToggle);
                            _drinkValuesPotions.Description = "potions, great potions, elixirs";
                            _drinkValuesCures.Format("Cures", _drinkValuesToggle);
                            _drinkValuesCures.Description = "antidote, hex cleaner, invigorating potion, panacea";
                            _drinkValuesTeas.Format("Teas", _drinkValuesToggle);
                            _drinkValuesOther.Format("Other", _drinkValuesToggle);
                            _drinkValuesOther.Description = "milks, gaberry wine, Gep's drink";
                            Indent--;
                        }
                    }
                    Indent--;
                }
            }

            _allowCuresWhileOverlimited.Format("Allow cures while overlimited");
            _allowCuresWhileOverlimited.Description = "Allows eating/drinking when over 100%, but only if it cures a negative status effect you have\n" +
                                                      "(receding diseases cannot be cured again)";
            Indent++;
            {
                _allowOnlyDOTCures.Format("Only allow DoT cures", _allowCuresWhileOverlimited);
                _allowOnlyDOTCures.Description = "Same as above, but limited to curing status effects that damage you over time";
                Indent--;
            }
            _noFoodOrDrinkOverlimitAfterSleep.Format("Limit food/drink to 100% after sleep");
            _noFoodOrDrinkOverlimitAfterSleep.Description = "Allows you to eat/drink at least 1 meal before setting out on an adventure";
            _dontRestoreNeedsOnTravel.Format("Don't restore needs when travelling");
            _dontRestoreNeedsOnTravel.Description = "Normally, travelling restores 100% needs and resets temperature\n" +
                                                    "but mages may prefer to have control over their sleep level :)";
        }
        override protected string Description
        => "• Enable \"Overlimits\" system\n" +
           "(prevents you from eating infinitely)\n" +
           "• Override negative status effects thresholds\n" +
           "• Override needs depletion rates\n" +
           "• Override drink values and sleep buffs duration";

        // Utility
        static private bool _isInitialized;
        static private Dictionary<Need, FulfilledData> _fulfilledDataByNeed;
        static private void InitializeStatusEffectPrefabs()
        {
            _fulfilledDataByNeed = new Dictionary<Need, FulfilledData>
            {
                [Need.Food] = new FulfilledData
                (
                    Prefabs.StatusEffectsByID["Full"],
                    "Well-fed",
                    $"Your max health burns more slowly, but you can't eat any more.",
                    "You're well-fed!",
                    "You're too full to eat more!"
                ),
                [Need.Drink] = new FulfilledData
                (
                    Prefabs.StatusEffectsByID["Refreshed"],
                    "Well-hydrated",
                    $"Your max stamina burns more slowly, but you can't eat any more.",
                    "You're well-hydrated!",
                    "You're too full to drink more!"
                ),
                [Need.Sleep] = new FulfilledData
                (
                    Prefabs.StatusEffectsByID["DEPRECATED_Energized"],
                    "Well-rested",
                    $"Your stamina regens faster, but you lose mana and can't sleep any more.",
                    "You're well-rested!",
                    "You're too awake to sleep at this time!"
                ),
            };

            foreach (var element in _fulfilledDataByNeed)
            {
                StatusEffect statusEffect = element.Value.StatusEffect;
                statusEffect.m_nameLocKey = element.Value.Name;
                statusEffect.m_descriptionLocKey = element.Value.Description;
                statusEffect.OverrideIcon = Utility.CreateSpriteFromFile(Utility.PluginFolderPath + ICONS_FOLDER + element.Key + ".PNG");
                statusEffect.IsMalusEffect = false;
                statusEffect.RefreshRate = 1f;
            }
        }
        static private void UpdateStatusEffectPrefabsData()
        {
            foreach (var element in _fulfilledDataByNeed)
            {
                StatusEffect statusEffect = element.Value.StatusEffect;
                statusEffect.RemoveAllEffects();
                List<Effect> newEffects = new List<Effect>();
                List<StatusData.EffectData> newEffectDatas = new List<StatusData.EffectData>();

                float effectValue = _settingsByNeed[element.Key]._fulfilledEffectValue - 100f;
                switch (element.Key)
                {
                    case Need.Food:
                        // Health burn
                        AffectStat healthBurnModifier = statusEffect.AddEffect<AffectStat>();
                        healthBurnModifier.AffectedStat = new TagSourceSelector("HealthBurn".ToTag());
                        healthBurnModifier.IsModifier = true;
                        newEffects.Add(healthBurnModifier);
                        newEffectDatas.Add(new StatusData.EffectData() { Data = new[] { effectValue.ToString() } });
                        break;
                    case Need.Drink:
                        // Stamina burn
                        AffectStat staminaBurnModifier = statusEffect.AddEffect<AffectStat>();
                        staminaBurnModifier.AffectedStat = new TagSourceSelector("StaminaBurn".ToTag());
                        staminaBurnModifier.IsModifier = true;
                        newEffects.Add(staminaBurnModifier);
                        newEffectDatas.Add(new StatusData.EffectData() { Data = new[] { effectValue.ToString() } });
                        break;
                    case Need.Sleep:
                        // Stamina regen
                        AffectStat staminaRegen = statusEffect.AddEffect<AffectStat>();
                        staminaRegen.AffectedStat = new TagSourceSelector("StaminaRegen".ToTag());
                        staminaRegen.IsModifier = true;
                        newEffects.Add(staminaRegen);
                        newEffectDatas.Add(new StatusData.EffectData() { Data = new[] { effectValue.ToString() } });
                        // Mana
                        AffectMana manaRegen = statusEffect.AddEffect<AffectMana>();
                        manaRegen.AffectType = AffectMana.AffectTypes.Restaure;
                        manaRegen.IsModifier = _sleepNegativeEffectIsPercent;
                        newEffects.Add(manaRegen);
                        newEffectDatas.Add(new StatusData.EffectData() { Data = new[] { _sleepNegativeEffect.Value.Div(60).ToString() } });
                        break;
                }

                statusEffect.StatusData.EffectSignature.Effects = newEffects;
                statusEffect.StatusData.EffectsData = newEffectDatas.ToArray();
            }
        }
        static private void UpdateThresholds(PlayerCharacterStats stats)
        {
            (Need Need, StatThreshold Threshold)[] needThresholdPairs =
            {
                (Need.Food, stats.m_hunger),
                (Need.Drink, stats.m_thirst),
                (Need.Sleep, stats.m_fatigue),
            };

            foreach (var pair in needThresholdPairs)
            {
                StatThresholdLimit[] limits = pair.Threshold.m_thresholdLimits;

                // Common
                for (int i = 0; i < limits.Length; i++)
                {
                    limits[i].LimitLowAcceptEqual = false;
                    limits[i].LimitHighAcceptEqual = true;
                }
                limits[0].LimitSeverity = StatThresholdLimit.Severity.Critic;
                limits[1].LimitSeverity = StatThresholdLimit.Severity.Critic;
                limits[2].LimitSeverity = StatThresholdLimit.Severity.Moderate;
                limits[3].LimitSeverity = StatThresholdLimit.Severity.Normal;
                limits[4].LimitSeverity = StatThresholdLimit.Severity.Positive;
                limits[5].LimitSeverity = StatThresholdLimit.Severity.Positive;
                limits[0].Limit = new Vector2(float.NegativeInfinity, DeathThreshold(pair.Need));
                limits[1].Limit = new Vector2(DeathThreshold(pair.Need), VeryNegativeThreshold(pair.Need));
                limits[2].Limit = new Vector2(VeryNegativeThreshold(pair.Need), NegativeThreshold(pair.Need));
                limits[3].Limit = new Vector2(NegativeThreshold(pair.Need), NeutralThreshold(pair.Need));
                limits[4].Limit = new Vector2(NeutralThreshold(pair.Need), float.PositiveInfinity);
                limits[5].Limit = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

                // Unique
                FulfilledData customAttributes = _fulfilledDataByNeed[pair.Need];
                limits[4].StatusEffecPrefabs = new StatusEffect[] { customAttributes.StatusEffect };
                limits[4].m_nameLocKey = customAttributes.Name;
                limits[4].m_notificationLocKey = customAttributes.StatusNotification;
            }
        }
        static private void UpdateDepletionRates(PlayerCharacterStats stats)
        {
            stats.m_foodDepletionRate.BaseValue = _settingsByNeed[Need.Food].DepletionPerHour;
            stats.m_drinkDepletionRate.BaseValue = _settingsByNeed[Need.Drink].DepletionPerHour;
            stats.m_sleepDepletionRate.BaseValue = _settingsByNeed[Need.Sleep].DepletionPerHour;
        }
        static private void UpdateIngestibleAnimations(bool fix)
        {
            foreach (var animationPairByIngestibleID in ANIMATION_PAIRS_BY_INGESTIBLE_ID)
            {
                int id = animationPairByIngestibleID.Key;
                var (vanillaAnim, customAnim) = animationPairByIngestibleID.Value;
                Character.SpellCastType animation = fix ? customAnim : vanillaAnim;
                Prefabs.IngestiblesByID[id].m_activateEffectAnimType = animation;
            }
        }
        static private void UpdateDrinkValues()
        {
            foreach (var ingestibleByID in Prefabs.IngestiblesByID)
            {
                Item ingestible = ingestibleByID.Value;
                if (!ingestible.IsDrinkable()
                || ingestible.ItemID == "Ambraine".ItemID()
                || ingestible.ItemID == "Waterskin".ItemID())
                    continue;

                AffectDrink affectDrink = ingestible.GetEffect<AffectDrink>();
                if (affectDrink == null)
                    affectDrink = ingestible.AddEffect<AffectDrink>();

                float drinkValue = _drinkValuesPotions;
                if (ingestible.ItemID.IsContainedIn(CURE_DRINK_IDS))
                    drinkValue = _drinkValuesCures;
                else if (ingestible.ItemID.IsContainedIn(TEA_DRINK_IDS))
                    drinkValue = _drinkValuesTeas;
                else if (ingestible.ItemID.IsContainedIn(OTHER_DRINK_IDS))
                    drinkValue = _drinkValuesOther;

                affectDrink.SetAffectDrinkQuantity(drinkValue * 10f);
            }
        }
        static private void RemoveMilkFoodValues()
        {
            foreach (var milkID in MILK_IDS)
                Prefabs.IngestiblesByID[milkID].GetEffect<AffectFood>().Destroy();
        }
        static private void UpdateSleepBuffsDuration()
        {
            foreach (var sleepBuff in Prefabs.AllSleepBuffs)
                sleepBuff.StatusData.LifeSpan = _sleepBuffsDuration * 60f;
        }
        static private void DisplayPreventedNotification(Character character, Need need)
        => character.CharacterUI.ShowInfoNotification(_fulfilledDataByNeed[need].PreventedNotification);
        // Thresholds
        static private float DeathThreshold(Need need)
        => 0f;
        static private float VeryNegativeThreshold(Need need)
        => _settingsByNeed[need]._thresholds.Value.x;
        static private float NegativeThreshold(Need need)
        => _settingsByNeed[need]._thresholds.Value.y;
        static private float NeutralThreshold(Need need)
        => 100f;
        static private float FulfilledThreshold(Need need)
        => _settingsByNeed[need]._fulfilledLimit;
        static private float MaxNeedValue(Need need)
        => FulfilledThreshold(need) * 10f;
        // Checks
        static private bool HasDOT(Character character)
        {
            foreach (var statusEffect in character.StatusEffectMngr.Statuses)
                if (statusEffect.IdentifierName.IsContainedIn(DOT_STATUS_EFFECTS))
                    return true;
            return false;
        }
        static private bool HasStatusEffectCuredBy(Character character, Item item)
        {
            if (_allowOnlyDOTCures && !HasDOT(character))
                return false;

            if (item.ItemID == "Waterskin".ItemID())
                return character.IsBurning();

            foreach (var removeStatusEffect in item.GetEffects<RemoveStatusEffect>())
                switch (removeStatusEffect.CleanseType)
                {
                    case RemoveStatusEffect.RemoveTypes.StatusSpecific: return character.StatusEffectMngr.HasStatusEffect(removeStatusEffect.StatusEffect.IdentifierName);
                    case RemoveStatusEffect.RemoveTypes.StatusType: return character.StatusEffectMngr.HasStatusEffect(removeStatusEffect.StatusType);
                    case RemoveStatusEffect.RemoveTypes.StatusFamily:
                        Disease disease = character.GetDiseaseOfFamily(removeStatusEffect.StatusFamily);
                        if (disease != null)
                            return !disease.IsReceding;
                        return character.StatusEffectMngr.HasStatusEffect(removeStatusEffect.StatusFamily);
                    case RemoveStatusEffect.RemoveTypes.NegativeStatuses: return character.HasAnyPurgeableNegativeStatusEffect();
                }
            return false;
        }
        static private bool CanIngest(Character character, Item item)
        => (item.IsEatable() && !IsLimited(character, Need.Food)
           || item.IsDrinkable() && !IsLimited(character, Need.Drink)
           || _allowCuresWhileOverlimited && HasStatusEffectCuredBy(character, item))
        || item.ItemID == "Ambraine".ItemID();
        static private bool IsLimited(Character character, Need need)
        => _settingsByNeed[need].LimitingEnabled && HasLimitingStatusEffect(character, need);
        static private bool HasLimitingStatusEffect(Character character, Need need)
        => character.HasStatusEffect(NeedToLimitingStatusEffect(need));
        static private StatusEffect NeedToLimitingStatusEffect(Need need)
        => _fulfilledDataByNeed[need].StatusEffect;

        // Initialize
        [HarmonyPatch(typeof(PlayerCharacterStats), "OnStart"), HarmonyPostfix]
        static private void PlayerCharacterStats_OnAwake_Post(ref PlayerCharacterStats __instance)
        {
            UpdateThresholds(__instance);
            UpdateDepletionRates(__instance);
        }

        // Prevent use
        [HarmonyPatch(typeof(Item), "TryUse"), HarmonyPrefix]
        static bool Item_TryUse_Pre(ref Item __instance, ref bool __result, Character _character)
        {
            if (!_character.IsPlayer() || !__instance.IsIngestible() || CanIngest(_character, __instance))
                return true;

            DisplayPreventedNotification(_character, __instance.IsEatable() ? Need.Food : Need.Drink);
            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(Item), "TryQuickSlotUse"), HarmonyPrefix]
        static bool Item_TryQuickSlotUse_Pre(ref Item __instance)
        {
            Character character = __instance.OwnerCharacter;
            if (!character.IsPlayer() || !__instance.IsIngestible() || CanIngest(character, __instance))
                return true;

            DisplayPreventedNotification(character, __instance.IsEatable() ? Need.Food : Need.Drink);
            return false;
        }

        [HarmonyPatch(typeof(Sleepable), "OnReceiveSleepRequestResult"), HarmonyPrefix]
        static bool Sleepable_OnReceiveSleepRequestResult_Pre(ref Character _character)
        {
            if (!_character.IsPlayer() || !IsLimited(_character, Need.Sleep))
                return true;

            DisplayPreventedNotification(_character, Need.Sleep);
            return false;
        }

        [HarmonyPatch(typeof(DrinkWaterInteraction), "OnActivate"), HarmonyPrefix]
        static bool DrinkWaterInteraction_OnActivate_Pre(ref DrinkWaterInteraction __instance)
        {
            Character character = __instance.LastCharacter;
            if (!character.IsPlayer() || !IsLimited(character, Need.Drink) || character.IsBurning())
                return true;

            DisplayPreventedNotification(character, Need.Drink);
            return false;
        }

        // New limits
        [HarmonyPatch(typeof(PlayerCharacterStats), "UpdateNeeds"), HarmonyPrefix]
        static bool PlayerCharacterStats_UpdateNeeds_Pre(ref Stat ___m_maxFood, ref Stat ___m_maxDrink, ref Stat ___m_maxSleep)
        {
            if (_settingsByNeed[Need.Food].LimitingEnabled)
                ___m_maxFood.m_currentValue = MaxNeedValue(Need.Food);
            if (_settingsByNeed[Need.Drink].LimitingEnabled)
                ___m_maxDrink.m_currentValue = MaxNeedValue(Need.Drink);
            if (_settingsByNeed[Need.Sleep].LimitingEnabled)
                ___m_maxSleep.m_currentValue = MaxNeedValue(Need.Sleep);
            return true;
        }

        [HarmonyPatch(typeof(PlayerCharacterStats), "UpdateNeeds"), HarmonyPostfix]
        static void PlayerCharacterStats_UpdateNeeds_Post(ref Stat ___m_maxFood, ref Stat ___m_maxDrink, ref Stat ___m_maxSleep)
        {
            ___m_maxFood.m_currentValue = DEFAULT_MAX_NEED_VALUE;
            ___m_maxDrink.m_currentValue = DEFAULT_MAX_NEED_VALUE;
            ___m_maxSleep.m_currentValue = DEFAULT_MAX_NEED_VALUE;
        }

        [HarmonyPatch(typeof(PlayerCharacterStats), "Food", MethodType.Setter), HarmonyPrefix]
        static bool PlayerCharacterStats_Food_Setter_Pre(ref float value, ref float ___m_food)
        {
            #region quit
            if (!_settingsByNeed[Need.Food].LimitingEnabled)
                return true;
            #endregion

            ___m_food = value.ClampMax(MaxNeedValue(Need.Food));
            return false;
        }

        [HarmonyPatch(typeof(PlayerCharacterStats), "Drink", MethodType.Setter), HarmonyPrefix]
        static bool PlayerCharacterStats_Drink_Setter_Pre(ref float value, ref float ___m_drink)
        {
            #region quit
            if (!_settingsByNeed[Need.Drink].LimitingEnabled)
                return true;
            #endregion

            ___m_drink = value.ClampMax(MaxNeedValue(Need.Drink));
            return false;
        }

        [HarmonyPatch(typeof(PlayerCharacterStats), "Sleep", MethodType.Setter), HarmonyPrefix]
        static bool PlayerCharacterStats_Sleep_Setter_Pre(ref float value, ref float ___m_sleep)
        {
            #region quit
            if (!_settingsByNeed[Need.Sleep].LimitingEnabled)
                return true;
            #endregion

            ___m_sleep = value.ClampMax(MaxNeedValue(Need.Sleep));
            return false;
        }

        // No food/drink overlimit after bed sleep
        [HarmonyPatch(typeof(PlayerCharacterStats), "UpdateStatsAfterRest"), HarmonyPostfix]
        static void PlayerCharacterStats_UpdateStatsAfterRest_Post(ref PlayerCharacterStats __instance)
        {
            #region MyRegion
            if (!_noFoodOrDrinkOverlimitAfterSleep)
                return;
            #endregion

            __instance.m_food = __instance.m_food.ClampMax(1000f);
            __instance.m_drink = __instance.m_drink.ClampMax(1000f);
        }

        // Don't restore needs when travelling
        [HarmonyPatch(typeof(FastTravelMenu), "OnConfirmFastTravel"), HarmonyPrefix]
        static bool FastTravelMenu_OnConfirmFastTravel_Pre(ref FastTravelMenu __instance)
        {
            #region quit
            if (!_dontRestoreNeedsOnTravel)
                return true;
            #endregion

            __instance.Hide();
            return false;
        }
    }
}

/*
static private float MaxAllowedSleepValue
{
    get
    {
        float linProgress = EnvironmentConditions.Instance.TimeOfDay.DistanceTo(12f) / 12f;
        float cosProgress = 0.5f - 0.5f * Mathf.Cos(linProgress * Mathf.PI);
        return NegativeThreshold(Need.Sleep).Lerp(NeutralThreshold(Need.Sleep), cosProgress) * 10f;
    }
}
*/
//      && !(_daylightLimitsSleep && character.PlayerStats.Sleep > MaxAllowedSleepValue);
