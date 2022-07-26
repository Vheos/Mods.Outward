namespace Vheos.Mods.Outward;
using BepInEx.Configuration;
using Anim = Character.SpellCastType;

public class Needs : AMod, IDelayedInit
{
    #region Constants
    private const string ICONS_FOLDER = @"Needs\";
    private const float DEFAULT_MAX_NEED_VALUE = 1000f;
    private static readonly (Need Need, Vector2 Thresholds, float DepletionRate, string NegativeName, string ActionName, string AffectedStat)[] NEEDS_DATA =
    {
        (Need.Food, new Vector2(50f, 75f), 1000f / 15.00f, "Hungry", "eating", "health burn rate"),
        (Need.Drink, new Vector2(50f, 75f), 1000f / 27.77f, "Thirsty", "drinking", "stamina burn rate"),
        (Need.Sleep, new Vector2(25f, 50f), 1000f / 13.89f, "Tired", "sleeping", "stamina regen"),
    };
    private static readonly Item AMBRAINE = "Ambraine".ToItemPrefab();
    private static readonly Item WATERSKIN = "Waterskin".ToItemPrefab();
    private static readonly Dictionary<Item, Anim> FIXED_ANIMATIONS_BY_INGESTIBLE = new()
    {
        ["Torcrab Egg".ToItemPrefab()] = Anim.Eat,
        ["Boreo Blubber".ToItemPrefab()] = Anim.Eat,
        ["Pungent Paste".ToItemPrefab()] = Anim.Eat,
        ["Gaberry Jam".ToItemPrefab()] = Anim.Eat,
        ["Crawlberry Jam".ToItemPrefab()] = Anim.Eat,
        ["Golden Jam".ToItemPrefab()] = Anim.Eat,
        ["Raw Torcrab Meat".ToItemPrefab()] = Anim.Eat,
        ["Miner’s Omelet".ToItemPrefab()] = Anim.Eat,
        ["Turmmip Potage".ToItemPrefab()] = Anim.Eat,
        ["Meat Stew".ToItemPrefab()] = Anim.Eat,
        ["Marshmelon Jelly".ToItemPrefab()] = Anim.Eat,
        ["Blood Mushroom".ToItemPrefab()] = Anim.Eat,
        ["Food Waste".ToItemPrefab()] = Anim.Eat,
        ["Warm Boozu’s Milk".ToItemPrefab()] = Anim.DrinkWater,
    };
    private static readonly Item[] OTHER_DRINKS = new[]
    {
        "Able Tea".ToItemPrefab(),
        "Bitter Spicy Tea".ToItemPrefab(),
        "Greasy Tea".ToItemPrefab(),
        "Iced Tea".ToItemPrefab(),
        "Mineral Tea".ToItemPrefab(),
        "Needle Tea".ToItemPrefab(),
        "Soothing Tea".ToItemPrefab(),
        "Boozu’s Milk".ToItemPrefab(),
        "Warm Boozu’s Milk".ToItemPrefab(),
        "Gaberry Wine".ToItemPrefab(),
    };
    private static readonly Item[] MILKS = new[]
    {
        "Boozu’s Milk".ToItemPrefab(),
        "Warm Boozu’s Milk".ToItemPrefab(),
    };
    private static readonly string[] DOT_STATUS_EFFECTS = new[]
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
    #region Enums
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
    private static Dictionary<Need, NeedSettings> _settingsByNeed;
    private static ModSetting<int> _sleepNegativeEffect;
    private static ModSetting<bool> _sleepNegativeEffectIsPercent;
    private static ModSetting<int> _sleepBuffsDuration;
    private static ModSetting<bool> _overrideDrinkValues;
    private static ModSetting<int> _drinkValuesPotions, _drinkValuesOther;
    private static ModSetting<bool> _allowCuresWhileOverlimited;
    private static ModSetting<bool> _allowOnlyDOTCures;
    private static ModSetting<bool> _dontRestoreNeedsOnTravel;
    private static ModSetting<bool> _dontRestoreFoodDrinkOnSleep;
    protected override void Initialize()
    {
        _settingsByNeed = new Dictionary<Need, NeedSettings>();
        foreach (var data in NEEDS_DATA)
        {
            NeedSettings tmp = new();
            _settingsByNeed[data.Need] = tmp;

            string needPrefix = data.Need.ToString().ToLower();
            tmp._toggle = CreateSetting(needPrefix + nameof(tmp._toggle), false);
            tmp._thresholds = CreateSetting(needPrefix + nameof(tmp._thresholds), data.Thresholds);
            tmp._depletionRate = CreateSetting(needPrefix + nameof(tmp._depletionRate), new Vector2(100f, data.DepletionRate));
            tmp._fulfilledLimit = CreateSetting(needPrefix + nameof(tmp._fulfilledLimit), 100, IntRange(100, 200));

            int defaultValue = data.Need == Need.Sleep ? 115 : 33;
            AcceptableValueRange<int> range = data.Need == Need.Sleep ? IntRange(100, 200) : IntRange(0, 100);
            tmp._fulfilledEffectValue = CreateSetting(needPrefix + nameof(tmp._fulfilledEffectValue), defaultValue, range);
        }
        _overrideDrinkValues = CreateSetting(nameof(_overrideDrinkValues), false);
        _drinkValuesPotions = CreateSetting(nameof(_drinkValuesPotions), 10, IntRange(0, 100));
        _drinkValuesOther = CreateSetting(nameof(_drinkValuesOther), 20, IntRange(0, 100));
        _sleepNegativeEffect = CreateSetting(nameof(_sleepNegativeEffect), -12, IntRange(-100, 0));
        _sleepNegativeEffectIsPercent = CreateSetting(nameof(_sleepNegativeEffectIsPercent), false);
        _sleepBuffsDuration = CreateSetting(nameof(_sleepBuffsDuration), 40, IntRange(0, 100));
        _allowCuresWhileOverlimited = CreateSetting(nameof(_allowCuresWhileOverlimited), false);
        _allowOnlyDOTCures = CreateSetting(nameof(_allowOnlyDOTCures), false);
        _dontRestoreNeedsOnTravel = CreateSetting(nameof(_dontRestoreNeedsOnTravel), false);
        _dontRestoreFoodDrinkOnSleep = CreateSetting(nameof(_dontRestoreFoodDrinkOnSleep), false);

        // Events
        AddEventOnConfigClosed(() =>
        {
            if (!_isInitialized)
            {
                FixIngestibleAnimations();
                InitializeStatusEffectPrefabs();
                RemoveMilkFoodValues();
                _isInitialized = true;
            }

            UpdateStatusEffectPrefabsData();
            if (_overrideDrinkValues)
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
    protected override void SetFormatting()
    {
        foreach (var data in NEEDS_DATA)
        {
            NeedSettings tmp = _settingsByNeed[data.Need];

            tmp._toggle.Format(data.Need.ToString());
            tmp._toggle.Description = $"Change {data.Need}-related settings";
            using (Indent)
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
                using (Indent)
                {
                    tmp._fulfilledEffectValue.Format(data.AffectedStat, tmp._fulfilledLimit, t => t > 100);
                    if (data.Need == Need.Sleep)
                    {
                        _sleepNegativeEffect.Format("mana / min", tmp._fulfilledLimit, t => t > 100);
                        using (Indent)
                        {
                            _sleepNegativeEffectIsPercent.Format("is % of max mana", tmp._fulfilledLimit, t => t > 100);
                        }
                    }
                }

                if (data.Need == Need.Sleep)
                {
                    _sleepBuffsDuration.Format("Buffs duration", tmp._toggle);
                    _sleepBuffsDuration.Description = "Affects buffs granted by sleeping in houses, inns and tents\n" +
                                                      "(in real-time minutes)";
                }

                if (data.Need == Need.Drink)
                {
                    _overrideDrinkValues.Format("Items' drink values", tmp._toggle);
                    _overrideDrinkValues.Description = "Set how much drink is restored by each drink type";
                    using (Indent)
                    {
                        _drinkValuesPotions.Format("Potions", _overrideDrinkValues);
                        _drinkValuesPotions.Description = "potions, great potions, elixirs, Gep's Drink\n" +
                                                          "antidote, hex cleaner, invigorating potion, panacea";
                        _drinkValuesOther.Format("Other", _overrideDrinkValues);
                        _drinkValuesOther.Description = "teas, milks, gaberry wine";
                    }
                }
            }
        }

        _allowCuresWhileOverlimited.Format("Allow cures while overlimited");
        _allowCuresWhileOverlimited.Description = "Allows eating/drinking when over 100%, but only if it cures a negative status effect you have\n" +
                                                  "(receding diseases cannot be cured again)";
        using (Indent)
        {
            _allowOnlyDOTCures.Format("Only allow DoT cures", _allowCuresWhileOverlimited);
            _allowOnlyDOTCures.Description = "Same as above, but limited to curing status effects that damage you over time";
        }
        _dontRestoreNeedsOnTravel.Format("Don't restore needs when travelling");
        _dontRestoreNeedsOnTravel.Description = "Normally, travelling restores 100% needs and resets temperature\n" +
                                                "but mages may prefer to have control over their sleep level :)";
        _dontRestoreFoodDrinkOnSleep.Format("Don't restore food/drink when sleeping");
        _dontRestoreFoodDrinkOnSleep.Description = "Sleeping in beds will only stop the depletion of food and drink, not restore them";
    }
    protected override string Description
    => "• Enable \"Overlimits\" system\n" +
       "(prevents you from eating infinitely)\n" +
       "• Override negative status effects thresholds\n" +
       "• Override needs depletion rates\n" +
       "• Override drink values and sleep buffs duration";
    protected override string SectionOverride
    => ModSections.SurvivalAndImmersion;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                foreach (var data in NEEDS_DATA)
                {
                    NeedSettings needSetting = _settingsByNeed[data.Need];
                    needSetting._toggle.Value = true;
                    {
                        needSetting._thresholds.Value = new Vector2(100 / 3f, 200 / 3f);
                        needSetting._fulfilledLimit.Value = 120;
                        needSetting._fulfilledEffectValue.Value = data.Need == Need.Sleep ? 115 : 33;
                        int depletionRate = data.Need == Need.Food ? 30
                                          : data.Need == Need.Drink ? 15
                                          : 45;
                        needSetting._depletionRate.Value = new Vector2(100, depletionRate);
                    }
                };
                {
                    _drinkValuesPotions.Value = 10;
                    _drinkValuesOther.Value = 20;
                }
                {
                    _sleepNegativeEffect.Value = -12;
                    _sleepNegativeEffectIsPercent.Value = true;
                    _sleepBuffsDuration.Value = 0;
                }
                _allowCuresWhileOverlimited.Value = true;
                _allowOnlyDOTCures.Value = true;
                _dontRestoreNeedsOnTravel.Value = true;
                _dontRestoreFoodDrinkOnSleep.Value = true;
                break;
        }
    }

    // Utility
    private static bool _isInitialized;
    private static Dictionary<Need, FulfilledData> _fulfilledDataByNeed;
    private static void InitializeStatusEffectPrefabs()
    {
        _fulfilledDataByNeed = new Dictionary<Need, FulfilledData>
        {
            [Need.Food] = new FulfilledData
            (
                Prefabs.StatusEffectsByNameID["Full"],
                "Well-fed",
                $"Your max health burns more slowly, but you can't eat any more.",
                "You're well-fed!",
                "You're too full to eat more!"
            ),
            [Need.Drink] = new FulfilledData
            (
                Prefabs.StatusEffectsByNameID["Refreshed"],
                "Well-hydrated",
                $"Your max stamina burns more slowly, but you can't drink any more.",
                "You're well-hydrated!",
                "You're too full to drink more!"
            ),
            [Need.Sleep] = new FulfilledData
            (
                Prefabs.StatusEffectsByNameID["DEPRECATED_Energized"],
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
            statusEffect.OverrideIcon = Utils.CreateSpriteFromFile(Utils.PluginFolderPath + ICONS_FOLDER + element.Key + ".PNG");
            statusEffect.IsMalusEffect = false;
            statusEffect.RefreshRate = 1f;
        }
    }
    private static void UpdateStatusEffectPrefabsData()
    {
        foreach (var element in _fulfilledDataByNeed)
        {
            StatusEffect statusEffect = element.Value.StatusEffect;
            statusEffect.RemoveAllEffects();
            List<Effect> newEffects = new();
            List<StatusData.EffectData> newEffectDatas = new();

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
    private static void UpdateThresholds(PlayerCharacterStats stats)
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
    private static void UpdateDepletionRates(PlayerCharacterStats stats)
    {
        stats.m_foodDepletionRate.BaseValue = _settingsByNeed[Need.Food].DepletionPerHour;
        stats.m_drinkDepletionRate.BaseValue = _settingsByNeed[Need.Drink].DepletionPerHour;
        stats.m_sleepDepletionRate.BaseValue = _settingsByNeed[Need.Sleep].DepletionPerHour;
    }
    private static void FixIngestibleAnimations()
    {
        foreach (var animByIngestible in FIXED_ANIMATIONS_BY_INGESTIBLE)
            animByIngestible.Key.m_activateEffectAnimType = animByIngestible.Value;
    }
    private static void UpdateDrinkValues()
    {
        foreach (var ingestibleByID in Prefabs.IngestiblesByID)
        {
            Item ingestible = ingestibleByID.Value;
            if (!ingestible.IsDrinkable()
            || ingestible == AMBRAINE
            || ingestible == WATERSKIN)
                continue;

            AffectDrink affectDrink = ingestible.GetEffect<AffectDrink>();
            if (affectDrink == null)
                affectDrink = ingestible.AddEffect<AffectDrink>();

            float drinkValue = ingestible.IsContainedIn(OTHER_DRINKS)
                ? _drinkValuesOther
                : _drinkValuesPotions;

            affectDrink.SetAffectDrinkQuantity(drinkValue * 10f);
        }
    }
    private static void RemoveMilkFoodValues()
    {
        foreach (var milk in MILKS)
            milk.GetEffect<AffectFood>().Destroy();
    }
    private static void UpdateSleepBuffsDuration()
    {
        foreach (var sleepBuff in Prefabs.SleepBuffs)
            sleepBuff.StatusData.LifeSpan = _sleepBuffsDuration * 60f;
    }
    private static void DisplayPreventedNotification(Character character, Need need)
    => character.CharacterUI.ShowInfoNotification(_fulfilledDataByNeed[need].PreventedNotification);

    // Thresholds
    private static float DeathThreshold(Need need)
    => 0f;
    private static float VeryNegativeThreshold(Need need)
    => _settingsByNeed[need]._thresholds.Value.x;
    private static float NegativeThreshold(Need need)
    => _settingsByNeed[need]._thresholds.Value.y;
    private static float NeutralThreshold(Need need)
    => 100f;
    private static float FulfilledThreshold(Need need)
    => _settingsByNeed[need]._fulfilledLimit;
    private static float MaxNeedValue(Need need)
    => FulfilledThreshold(need) * 10f;

    // Checks
    private static bool HasDOT(Character character)
    {
        foreach (var statusEffect in character.StatusEffectMngr.Statuses)
            if (statusEffect.IdentifierName.IsContainedIn(DOT_STATUS_EFFECTS))
                return true;
        return false;
    }
    private static bool HasStatusEffectCuredBy(Character character, Item item)
    {
        if (_allowOnlyDOTCures && !HasDOT(character))
            return false;

        if (item is WaterContainer)
            return character.IsBurning();

        foreach (var removeStatusEffect in item.GetEffects<RemoveStatusEffect>())
            switch (removeStatusEffect.CleanseType)
            {
                case RemoveStatusEffect.RemoveTypes.StatusSpecific:
                    return character.StatusEffectMngr.HasStatusEffect(removeStatusEffect.StatusEffect.IdentifierName);
                case RemoveStatusEffect.RemoveTypes.StatusType:
                    return character.StatusEffectMngr.HasStatusEffect(removeStatusEffect.StatusType);
                case RemoveStatusEffect.RemoveTypes.StatusFamily:
                    return character.GetDiseaseOfFamily(removeStatusEffect.StatusFamily).TryNonNull(out var disease)
                        ? !disease.IsReceding
                        : character.StatusEffectMngr.HasStatusEffect(removeStatusEffect.StatusFamily);
                case RemoveStatusEffect.RemoveTypes.NegativeStatuses:
                    return character.HasAnyPurgeableNegativeStatusEffect();
            }
        return false;
    }
    private static bool CanIngest(Character character, Item item)
    => item.IsEatable() && !IsLimited(character, Need.Food)
       || item.IsDrinkable() && !IsLimited(character, Need.Drink)
       || _allowCuresWhileOverlimited && HasStatusEffectCuredBy(character, item)
       || item.SharesPrefabWith(AMBRAINE);
    private static bool IsLimited(Character character, Need need)
    => _settingsByNeed[need].LimitingEnabled && HasLimitingStatusEffect(character, need);
    private static bool HasLimitingStatusEffect(Character character, Need need)
    => character.HasStatusEffect(NeedToLimitingStatusEffect(need));
    private static StatusEffect NeedToLimitingStatusEffect(Need need)
    => _fulfilledDataByNeed[need].StatusEffect;

    // Hooks
    // Initialize
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterStats), nameof(PlayerCharacterStats.OnStart))]
    private static void PlayerCharacterStats_OnAwake_Post(PlayerCharacterStats __instance)
    {
        UpdateThresholds(__instance);
        UpdateDepletionRates(__instance);
    }

    // Prevent use
    [HarmonyPrefix, HarmonyPatch(typeof(Item), nameof(Item.TryUse))]
    private static bool Item_TryUse_Pre(Item __instance, ref bool __result, Character _character)
    {
        if (!_character.IsPlayer() || !__instance.IsIngestible() || CanIngest(_character, __instance))
            return true;

        DisplayPreventedNotification(_character, __instance.IsEatable() ? Need.Food : Need.Drink);
        __result = false;
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Item), nameof(Item.TryQuickSlotUse))]
    private static bool Item_TryQuickSlotUse_Pre(Item __instance)
    {
        Character character = __instance.OwnerCharacter;
        if (!character.IsPlayer() || !__instance.IsIngestible() || CanIngest(character, __instance))
            return true;

        DisplayPreventedNotification(character, __instance.IsEatable() ? Need.Food : Need.Drink);
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Sleepable), nameof(Sleepable.OnReceiveSleepRequestResult))]
    private static bool Sleepable_OnReceiveSleepRequestResult_Pre(ref Character _character)
    {
        if (!_character.IsPlayer() || !IsLimited(_character, Need.Sleep))
            return true;

        DisplayPreventedNotification(_character, Need.Sleep);
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DrinkWaterInteraction), nameof(DrinkWaterInteraction.OnActivate))]
    private static bool DrinkWaterInteraction_OnActivate_Pre(DrinkWaterInteraction __instance)
    {
        Character character = __instance.LastCharacter;
        if (!character.IsPlayer() || !IsLimited(character, Need.Drink) || character.IsBurning())
            return true;

        DisplayPreventedNotification(character, Need.Drink);
        return false;
    }

    // New limits
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterStats), nameof(PlayerCharacterStats.UpdateNeeds))]
    private static void PlayerCharacterStats_UpdateNeeds_Pre(ref Stat ___m_maxFood, ref Stat ___m_maxDrink, ref Stat ___m_maxSleep)
    {
        if (_settingsByNeed[Need.Food].LimitingEnabled)
            ___m_maxFood.m_currentValue = MaxNeedValue(Need.Food);
        if (_settingsByNeed[Need.Drink].LimitingEnabled)
            ___m_maxDrink.m_currentValue = MaxNeedValue(Need.Drink);
        if (_settingsByNeed[Need.Sleep].LimitingEnabled)
            ___m_maxSleep.m_currentValue = MaxNeedValue(Need.Sleep);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterStats), nameof(PlayerCharacterStats.UpdateNeeds))]
    private static void PlayerCharacterStats_UpdateNeeds_Post(ref Stat ___m_maxFood, ref Stat ___m_maxDrink, ref Stat ___m_maxSleep)
    {
        ___m_maxFood.m_currentValue = DEFAULT_MAX_NEED_VALUE;
        ___m_maxDrink.m_currentValue = DEFAULT_MAX_NEED_VALUE;
        ___m_maxSleep.m_currentValue = DEFAULT_MAX_NEED_VALUE;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterStats), nameof(PlayerCharacterStats.Food), MethodType.Setter)]
    private static bool PlayerCharacterStats_Food_Setter_Pre(ref float value, ref float ___m_food)
    {
        #region quit
        if (!_settingsByNeed[Need.Food].LimitingEnabled)
            return true;
        #endregion

        ___m_food = value.ClampMax(MaxNeedValue(Need.Food));
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterStats), nameof(PlayerCharacterStats.Drink), MethodType.Setter)]
    private static bool PlayerCharacterStats_Drink_Setter_Pre(ref float value, ref float ___m_drink)
    {
        #region quit
        if (!_settingsByNeed[Need.Drink].LimitingEnabled)
            return true;
        #endregion

        ___m_drink = value.ClampMax(MaxNeedValue(Need.Drink));
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterStats), nameof(PlayerCharacterStats.Sleep), MethodType.Setter)]
    private static bool PlayerCharacterStats_Sleep_Setter_Pre(ref float value, ref float ___m_sleep)
    {
        #region quit
        if (!_settingsByNeed[Need.Sleep].LimitingEnabled)
            return true;
        #endregion

        ___m_sleep = value.ClampMax(MaxNeedValue(Need.Sleep));
        return false;
    }

    // Don't restore needs when travelling
    [HarmonyPrefix, HarmonyPatch(typeof(FastTravelMenu), nameof(FastTravelMenu.OnConfirmFastTravel))]
    private static bool FastTravelMenu_OnConfirmFastTravel_Pre(FastTravelMenu __instance)
    {
        #region quit
        if (!_dontRestoreNeedsOnTravel)
            return true;
        #endregion

        __instance.Hide();
        return false;
    }

    // Don't restore food/drink when sleeping
    [HarmonyPrefix, HarmonyPatch(typeof(CharacterResting), nameof(CharacterResting.GetFoodRestored))]
    private static bool CharacterResting_GetFoodRestored_Pre(CharacterResting __instance)
    => !_dontRestoreFoodDrinkOnSleep;

    [HarmonyPrefix, HarmonyPatch(typeof(CharacterResting), nameof(CharacterResting.GetDrinkRestored))]
    private static bool CharacterResting_GetDrinkRestored_Pre(CharacterResting __instance)
    => !_dontRestoreFoodDrinkOnSleep;
}

/*
// No food/drink overlimit after bed sleep
[HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterStats), nameof(PlayerCharacterStats.UpdateStatsAfterRest))]
static void PlayerCharacterStats_UpdateStatsAfterRest_Post(PlayerCharacterStats __instance)
{
#region MyRegion
if (!_dontRestoreFoodDrinkOnSleep)
    return;
#endregion

__instance.m_food = __instance.m_food.ClampMax(1000f);
__instance.m_drink = __instance.m_drink.ClampMax(1000f);
}
*/
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
