/* TO DO:
 * - extend to more item types (rags, varnishes)
 */

namespace Vheos.Mods.Outward;
using UnityEngine.UI;

public class Descriptions : AMod, IDelayedInit
{
    #region Constants
    private static readonly Vector2 BAR_MAX_SIZE = new(2.75f, 2.50f);
    private static readonly Vector2 BAR_PIVOT = new(0f, 1f);
    private const float DURABILITY_MAX_MAX = 777f;    // Duty (unique halberd)
    private const float DURABILITY_BAR_SCALING_CURVE = 0.75f;
    private const float FRESHNESS_LIFESPAN_MAX = 104f;   // Travel Ration
    private const float FRESHNESS_BAR_SCALING_CURVE = 2 / 3f;
    private const int DEFAULT_FONT_SIZE = 19;
    private const int WATER_ITEMS_FIRST_ID = 5600000;
    private static Color HEALTH_COLOR = new(0.765f, 0.522f, 0.525f, 1f);
    private static Color STAMINA_COLOR = new(0.827f, 0.757f, 0.584f, 1f);
    private static Color MANA_COLOR = new(0.529f, 0.702f, 0.816f, 1f);
    private static Color NEEDS_COLOR = new(0.584f, 0.761f, 0.522f, 1f);
    private static Color CORRUPTION_COLOR = new(0.655f, 0.647f, 0.282f, 1f);
    private static Color STATUSEFFECT_COLOR = new(0.780f, 1f, 0.702f, 1f);
    private static Color STATUSCURES_COLOR = new(1f, 0.702f, 0.706f);
    private static readonly Dictionary<AreaManager.AreaEnum, string> SOROBOREAN_CARAVANNER_UIDS_BY_CITY = new()
    {
        [AreaManager.AreaEnum.CierzoVillage] = "G_GyAVjRWkq8e2L8WP4TgA",
        [AreaManager.AreaEnum.Berg] = "-MSrkT502k63y3CV2j98TQ",
        [AreaManager.AreaEnum.Monsoon] = "9GAbQm8Ekk23M0LohPF7dg",
        [AreaManager.AreaEnum.Levant] = "Tbq1PxS_iUO6vhnr7aGUhg",
        [AreaManager.AreaEnum.Harmattan] = "WN0BVRJwtE-goNLvproxgw",
        [AreaManager.AreaEnum.NewSirocco] = "-MSrkT502k63y3CV2j98TQ",
    };
    #endregion
    #region Enums
    [Flags]
    private enum Details
    {
        None = 0,
        All = ~0,

        Vitals = 1 << 1,
        MaxVitals = 1 << 2,
        Needs = 1 << 3,
        Corruption = 1 << 4,
        RegenRates = 1 << 5,
        StatusEffects = 1 << 6,
        StatusCures = 1 << 7,
        Cooldown = 1 << 8,
        Costs = 1 << 9,
    }
    #endregion
    #region class
    private class Row
    {
        // Publics
        public string Label
        { get; }
        public string Content
        {
            get
            {
                string formattedContent = "";
                if (Prefix != null)
                    formattedContent += Prefix;
                formattedContent += _content;

                if (Size != DEFAULT_FONT_SIZE)
                    formattedContent = $"<size={Size}>{formattedContent}</size>";

                return $"<color=#{ColorUtility.ToHtmlStringRGBA(_color)}>{formattedContent}</color>";
            }
        }
        public Details Detail;
        public int Order
        { get; }
        public int Size
        { get; }
        public string Prefix;

        // Private
        private readonly string _content;
        private Color _color;

        // Constructors
        public Row(string label, string content, Details detail, int order = int.MaxValue, Color color = default)
        {
            Label = label;
            _content = content;
            Detail = detail;
            Order = order;
            _color = color;
            Prefix = "";

            Size = DEFAULT_FONT_SIZE;
            if (_content.Length >= 20)
                Size--;
            if (_content.Length >= 25)
                Size--;
            if (_content.Length >= 30)
                Size--;
        }
    }
    private class RowsCache
    {
        // Publics
        public List<Row> GetRows(Item item)
        {
            if (!_rowsByItem.ContainsKey(item.ItemID))
                CacheItemRows(item);
            return _rowsByItem[item.ItemID];
        }

        // Privates
        private readonly Dictionary<int, List<Row>> _rowsByItem;
        private void CacheItemRows(Item item)
        {
            List<Row> rows = new();

            if (item.TryAs(out Skill skill))
                FormatSkillRows(skill, rows);
            else
            {
                Effect[] effects = item is WaterItem ? GetWaterEffects(item.ItemID) : item.GetEffects();
                foreach (var effect in effects)
                {
                    Row newRow = GetFormattedItemRow(effect);
                    if (newRow != null)
                        rows.Add(newRow);
                }
            }

            rows.Sort((a, b) => a.Order.CompareTo(b.Order));
            _rowsByItem.Add(item.ItemID, rows);
        }

        // Constructors
        public RowsCache()
        {
            _rowsByItem = new Dictionary<int, List<Row>>();
        }
    }
    #endregion

    // Settings
    private static ModSetting<bool> _barsToggle, _equipmentToggle;
    private static ModSetting<bool> _addBackgrounds;
    private static ModSetting<Details> _details;
    private static ModSetting<bool> _displayRelativeAttackSpeed, _normalizeImpactDisplay, _moveBarrierBelowProtection, _hideNumericalDurability;
    private static ModSetting<int> _durabilityBarSize, _freshnessBarSize, _barThickness;
    private static ModSetting<bool> _durabilityTiedToMax, _freshnessTiedToLifespan;
    private static ModSetting<bool> _displaySellPricesInCities;
    private static ModSetting<bool> _displayRawValuesOutsideCities;
    private static ModSetting<bool> _displayStashAmount;
    private static ModSetting<bool> _recolorLearnedRecipes;
    private static ModSetting<Color> _learnedRecipeColor;
    private static ModSetting<bool> _highlightItemsWithLegacyUpgrade;
    private static ModSetting<Color> _legacyItemUpgradeColor;
    protected override void Initialize()
    {
        _details = CreateSetting(nameof(_details), Details.None);

        _equipmentToggle = CreateSetting(nameof(_equipmentToggle), false);
        _displayRelativeAttackSpeed = CreateSetting(nameof(_displayRelativeAttackSpeed), false);
        _normalizeImpactDisplay = CreateSetting(nameof(_normalizeImpactDisplay), false);
        _moveBarrierBelowProtection = CreateSetting(nameof(_moveBarrierBelowProtection), false);
        _hideNumericalDurability = CreateSetting(nameof(_hideNumericalDurability), false);

        _barsToggle = CreateSetting(nameof(_barsToggle), false);
        _durabilityTiedToMax = CreateSetting(nameof(_durabilityTiedToMax), false);
        _durabilityBarSize = CreateSetting(nameof(_durabilityBarSize), (100 / BAR_MAX_SIZE.x).Round(), IntRange(0, 100));
        _freshnessTiedToLifespan = CreateSetting(nameof(_freshnessTiedToLifespan), false);
        _freshnessBarSize = CreateSetting(nameof(_freshnessBarSize), (100 / BAR_MAX_SIZE.x).Round(), IntRange(0, 100));
        _barThickness = CreateSetting(nameof(_barThickness), (100 / BAR_MAX_SIZE.y).Round(), IntRange(0, 100));
        _addBackgrounds = CreateSetting(nameof(_addBackgrounds), false);

        _displaySellPricesInCities = CreateSetting(nameof(_displaySellPricesInCities), false);
        _displayRawValuesOutsideCities = CreateSetting(nameof(_displayRawValuesOutsideCities), false);
        _displayStashAmount = CreateSetting(nameof(_displayStashAmount), false);
        _recolorLearnedRecipes = CreateSetting(nameof(_recolorLearnedRecipes), false);
        _learnedRecipeColor = CreateSetting(nameof(_learnedRecipeColor), new Color(0.5f, 0.5f, 0.5f, 0.5f));
        _highlightItemsWithLegacyUpgrade = CreateSetting(nameof(_highlightItemsWithLegacyUpgrade), false);
        _legacyItemUpgradeColor = CreateSetting(nameof(_legacyItemUpgradeColor), new Color(1f, 0.75f, 0f, 0.5f));

        AddEventOnConfigClosed(() => SetBackgrounds(_addBackgrounds));
    }
    protected override void SetFormatting()
    {
        _details.Format("Details to display");
        _equipmentToggle.Format("Equipment");
        using (Indent)
        {
            _displayRelativeAttackSpeed.Format("Display relative attack speed", _equipmentToggle);
            _displayRelativeAttackSpeed.Description = "Attack speed will be displayedas +/- X%\n" +
                                                      "If the weapon has default attack speed (1), it won't be displayed";
            _normalizeImpactDisplay.Format("Normalize impact display", _equipmentToggle);
            _normalizeImpactDisplay.Description = "Impact damage/resistance will be displayed in the damages/resistances list and will have its own icon, just like all the other damage/resistance types";
            _moveBarrierBelowProtection.Format("Move barrier below protection", _equipmentToggle);
            _moveBarrierBelowProtection.Description = "Barrier will be displayed right under protection instead of between resistances and impact resistance";
            _hideNumericalDurability.Format("Hide numerical durability display", _equipmentToggle);
            _hideNumericalDurability.Description = "Hides the \"Durability: XXX/YYY\" row so the only indicator is the durability bar";
        }
        _barsToggle.Format("Bars");
        _barsToggle.Description = "Change sizes of durability and freshness progress bars";
        using (Indent)
        {
            _durabilityTiedToMax.Format("Durability proportional to max", _barsToggle);
            _durabilityTiedToMax.Description = "Items that are hard to break will have a longer bar\n" +
                                               "Items that break easily will have a shorter bar";
            _durabilityBarSize.Format("Durability length", _durabilityTiedToMax, false);
            _durabilityBarSize.Description = "Displayed on weapon, armors, lanterns and tools";
            _freshnessTiedToLifespan.Format("Freshness proportional to lifespan", _barsToggle);
            _freshnessTiedToLifespan.Description = "Foods that stays fresh for a long time will have a longer bar\n" +
                                                   "Foods that decay quickly will have a shorter bar";
            _freshnessBarSize.Format("Freshness length", _freshnessTiedToLifespan, false);
            _freshnessBarSize.Description = "Displayed on food and drinks";
            _barThickness.Format("Thickness", _barsToggle);
        }

        _addBackgrounds.Format("Add backgrounds to foods/drinks");
        _addBackgrounds.Description = "Display a big \"potions\" icon in the background of foods' and drinks' description box (by default, only Life Potion uses it)";

        CreateHeader("Display item prices");
        using (Indent)
        {
            _displaySellPricesInCities.Format("in cities");
            _displaySellPricesInCities.Description = "Display actual sell prices when in city (if prices vary by merchant, Soroborean Caravanner is taken as a reference)";
            _displayRawValuesOutsideCities.Format("outside cities");
            _displayRawValuesOutsideCities.Description = "Display base buy values when not in a city";
        }
        _displayStashAmount.Format("Display stashed item amounts");
        _displayStashAmount.Description = "Displays how many of each items you have stored in your stash";
        _recolorLearnedRecipes.Format("Recolor learned recipes");
        using (Indent)
        {
            _learnedRecipeColor.Format("color");
        }
        _highlightItemsWithLegacyUpgrade.Format("Highlight items with legacy upgrades");
        using (Indent)
        {
            _legacyItemUpgradeColor.Format("color", _highlightItemsWithLegacyUpgrade);
        }
    }
    protected override string Description
    => "• Display extra item details in inventory\n" +
    "(restored health/stamina/mana, granted status effects)\n" +
    "• Override durability and freshness bars\n" +
    "(automatic scaling, thickness)";
    protected override string SectionOverride
    => ModSections.UI;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_PreferredUI):
                ForceApply();
                _details.Value = Details.All;
                _equipmentToggle.Value = true;
                {
                    _displayRelativeAttackSpeed.Value = true;
                    _normalizeImpactDisplay.Value = true;
                    _moveBarrierBelowProtection.Value = true;
                    _hideNumericalDurability.Value = true;
                }
                _barsToggle.Value = true;
                {
                    _durabilityTiedToMax.Value = true;
                    _freshnessTiedToLifespan.Value = true;
                    _barThickness.Value = 60;
                }
                _addBackgrounds.Value = true;
                _displaySellPricesInCities.Value = true;
                _displayRawValuesOutsideCities.Value = false;
                _displayStashAmount.Value = true;
                _recolorLearnedRecipes.Value = true;
                break;
        }
    }

    // Utility
    private static Sprite _impactIcon;
    private static readonly RowsCache _rowsCache = new();
    private static Merchant _soroboreanCaravanner;
    private static Merchant SoroboreanCaravanner
    {
        get
        {
            if (_soroboreanCaravanner == null
            && AreaManager.Instance.CurrentArea.TryNonNull(out var currentArea)
            && SOROBOREAN_CARAVANNER_UIDS_BY_CITY.TryGetValue((AreaManager.AreaEnum)currentArea.ID, out var uid)
            && Merchant.m_sceneMerchants.ContainsKey(uid))
                _soroboreanCaravanner = Merchant.m_sceneMerchants[uid];
            return _soroboreanCaravanner;
        }
    }
    private static void TryCacheImpactIcon(CharacterUI characterUI)
    {
        if (_impactIcon == null
        && characterUI.m_menus[(int)CharacterUI.MenuScreens.Equipment].TryAs(out EquipmentMenu equipmentMenu)
        && equipmentMenu.transform.GetFirstComponentsInHierarchy<EquipmentOverviewPanel>().TryNonNull(out var equipmentOverview)
        && equipmentOverview.m_lblImpactAtk.TryNonNull(out var impactDisplay)
        && impactDisplay.m_imgIcon.TryNonNull(out var impactImage))
            _impactIcon = impactImage.sprite;
    }
    private static void SetBackgrounds(bool state)
    {
        Item lifePotion = Prefabs.GetIngestibleByName("Life Potion");
        Sprite potionBackground = lifePotion.m_overrideSigil;
        foreach (var ingestibleByID in Prefabs.IngestiblesByID)
            if (ingestibleByID.Value != lifePotion)
                ingestibleByID.Value.m_overrideSigil = state ? potionBackground : null;
    }
    private static Row GetFormattedItemRow(Effect effect)
    {
        switch (effect)
        {
            // Vitals
            case AffectHealth _:
                return new Row("CharacterStat_Health".Localized(),
                               FormatEffectValue(effect),
                               Details.Vitals, 21, HEALTH_COLOR);
            case AffectStamina _:
                return new Row("CharacterStat_Stamina".Localized(),
                               FormatEffectValue(effect),
                               Details.Vitals, 31, STAMINA_COLOR);
            case AffectMana _:
                return new Row("CharacterStat_Mana".Localized(),
                               FormatEffectValue(effect),
                               Details.Vitals, 41, MANA_COLOR);
            // Max vitals
            case AffectBurntHealth _:
                return new Row("General_Max".Localized() + ". " + "CharacterStat_Health".Localized(),
                               FormatEffectValue(effect),
                               Details.MaxVitals, 23, HEALTH_COLOR);
            case AffectBurntStamina _:
                return new Row("General_Max".Localized() + ". " + "CharacterStat_Stamina".Localized(),
                               FormatEffectValue(effect),
                               Details.MaxVitals, 33, STAMINA_COLOR);
            case AffectBurntMana _:
                return new Row("General_Max".Localized() + ". " + "CharacterStat_Mana".Localized(),
                               FormatEffectValue(effect),
                               Details.MaxVitals, 43, MANA_COLOR);
            // Needs
            case AffectFood _:
                return new Row("CharacterStat_Food".Localized(),
                               FormatEffectValue(effect, 10f, "%"),
                               Details.Needs, 11, NEEDS_COLOR);
            case AffectDrink _:
                return new Row("CharacterStat_Drink".Localized(),
                               FormatEffectValue(effect, 10f, "%"),
                               Details.Needs, 12, NEEDS_COLOR);
            case AffectFatigue _:
                return new Row("CharacterStat_Sleep".Localized(),
                               FormatEffectValue(effect, 10f, "%"),
                               Details.Needs, 13, NEEDS_COLOR);
            // Corruption
            case AffectCorruption _:
                return new Row("CharacterStat_Corruption".Localized(),
                               FormatEffectValue(effect, 10f, "%"),
                               Details.Corruption, 51, CORRUPTION_COLOR);
            // Cure
            case RemoveStatusEffect removeStatusEffect:
                string text = "";
                switch (removeStatusEffect.CleanseType)
                {
                    case RemoveStatusEffect.RemoveTypes.StatusSpecific: text = removeStatusEffect.StatusEffect.StatusName; break;
                    case RemoveStatusEffect.RemoveTypes.StatusType: text = removeStatusEffect.StatusType.Tag.TagName; break;
                    case RemoveStatusEffect.RemoveTypes.StatusFamily: text = removeStatusEffect.StatusFamily.Get().Name; break;
                    case RemoveStatusEffect.RemoveTypes.NegativeStatuses: text = "All Negative Status Effects"; break;
                }
                return new Row("",
                               $"- {text}",
                               Details.StatusCures, 71, STATUSCURES_COLOR);
            // Status
            case AddStatusEffect addStatusEffect:
                StatusEffect statusEffect = addStatusEffect.Status;
                Row statusName = new("",
                                         $"+ {statusEffect.StatusName}",
                                         Details.StatusEffects, 61, STATUSEFFECT_COLOR);
                if (addStatusEffect.ChancesToContract < 100)
                    statusName.Prefix = $"<color=silver>({addStatusEffect.ChancesToContract}%)</color> ";

                if (!statusEffect.HasEffectsAndDatas())
                    return statusName;

                StatusData.EffectData firstEffectData = statusEffect.GetDatas()[0];
                if (firstEffectData.Data.IsNullOrEmpty())
                    return statusName;

                string firstValue = firstEffectData.Data[0];

                return statusEffect.GetEffects()[0] switch
                {
                    AffectHealth _ => new Row
                    (
                        "CharacterStat_Health".Localized() + " Regen",
                        FormatStatusEffectValue(firstValue.ToFloat(), statusEffect.StartLifespan),
                        Details.Vitals | Details.RegenRates, 22, HEALTH_COLOR
                    ),
                    AffectStamina _ => new Row
                    (
                        "CharacterStat_Stamina".Localized() + " Regen",
                        FormatStatusEffectValue(firstValue.ToFloat(), statusEffect.StartLifespan),
                        Details.Vitals | Details.RegenRates, 32, STAMINA_COLOR
                    ),
                    AffectMana _ => new Row
                    (
                        "CharacterStat_Mana".Localized() + " Regen",
                        FormatStatusEffectValue(firstValue.ToFloat(), statusEffect.StartLifespan, 1f, "%"),
                        Details.Vitals | Details.RegenRates, 42, MANA_COLOR
                    ),
                    AffectCorruption _ => new Row
                    (
                        "CharacterStat_Corruption".Localized() + " Regen",
                        FormatStatusEffectValue(firstValue.ToFloat(), statusEffect.StartLifespan, 10f, "%"),
                        Details.Corruption | Details.RegenRates, 52, CORRUPTION_COLOR
                    ),
                    _ => statusName,
                };

            default:
                return null;
        }
    }
    private static void FormatSkillRows(Skill skill, List<Row> rows)
    {
        if (skill.Cooldown > 0)
            rows.Add(new Row("ItemStat_Cooldown".Localized(),
                              skill.Cooldown.FormatSeconds(skill.Cooldown >= 60),
                              Details.Cooldown, 11, NEEDS_COLOR));
        if (skill.HealthCost > 0)
            rows.Add(new Row("CharacterStat_Health".Localized() + " " + "BuildingMenu_Supplier_Cost".Localized(),
                              skill.HealthCost.ToString(),
                              Details.Costs, 12, HEALTH_COLOR));
        if (skill.StaminaCost > 0)
            rows.Add(new Row("CharacterStat_Stamina".Localized() + " " + "BuildingMenu_Supplier_Cost".Localized(),
                              skill.StaminaCost.ToString(),
                              Details.Costs, 13, STAMINA_COLOR));
        if (skill.ManaCost > 0)
            rows.Add(new Row("CharacterStat_Mana".Localized() + " " + "BuildingMenu_Supplier_Cost".Localized(),
                              skill.ManaCost.ToString(),
                              Details.Costs, 14, MANA_COLOR));
        if (skill.DurabilityCost > 0 || skill.DurabilityCostPercent > 0)
        {
            bool isPercent = skill.DurabilityCostPercent > 0;
            rows.Add(new Row("ItemStat_Durability".Localized() + " " + "BuildingMenu_Supplier_Cost".Localized(),
                              isPercent ? (skill.DurabilityCostPercent.ToString() + "%") : skill.DurabilityCost.ToString(),
                              Details.Costs, 15, NEEDS_COLOR));
        }
    }
    private static string FormatEffectValue(Effect effect, float divisor = 1f, string postfix = "")
    {
        string content = "";
        if (effect != null)
        {
            float value = effect.GetValue();
            if (value != 0)
                content = $"{value.Div(divisor).Round()}{postfix}";
            if (value > 0)
                content = $"+{content}";
        }
        return content;
    }
    private static string FormatStatusEffectValue(float value, float duration, float divisor = 1f, string postfix = "")
    {
        string content = "";

        float totalValue = value * duration;
        string formattedDuration = duration < 60 ? $"{duration.Mod(60).RoundDown()}sec" : $"{duration.Div(60).RoundDown()}min";
        if (value != 0)
            content = $"{totalValue.Div(divisor).Round()}{postfix} / {formattedDuration}";
        if (value > 0)
            content = $"+{content}";

        return content;
    }
    private static Effect[] GetWaterEffects(WaterType waterType)
        => waterType switch
        {
            WaterType.Clean => Global.WaterDistributor.m_cleanWaterEffects,
            WaterType.Fresh => Global.WaterDistributor.m_freshWaterEffects,
            WaterType.Salt => Global.WaterDistributor.m_saltWaterEffects,
            WaterType.Rancid => Global.WaterDistributor.m_rancidWaterEffects,
            WaterType.Magic => Global.WaterDistributor.m_magicWaterEffects,
            WaterType.Pure => Global.WaterDistributor.m_pureWaterEffects,
            WaterType.Healing => Global.WaterDistributor.m_healingWaterEffects,
            _ => null,
        };
    private static Effect[] GetWaterEffects(int waterID)
    => GetWaterEffects((WaterType)(waterID - WATER_ITEMS_FIRST_ID));
    private static void TrySwapProtectionWithResistances(Item item)
    {
        #region quit
        if (!_moveBarrierBelowProtection || !item.TryAs(out Equipment equipment) || equipment.BarrierProt <= 0)
            return;
        #endregion

        int resistancesIndex = item.m_displayedInfos.IndexOf(ItemDetailsDisplay.DisplayedInfos.DamageResistance);
        int barrierIndex = item.m_displayedInfos.IndexOf(ItemDetailsDisplay.DisplayedInfos.BarrierProtection);
        if (resistancesIndex < 0 || barrierIndex < 0 || barrierIndex < resistancesIndex)
            return;

        Utility.Swap(ref item.m_displayedInfos[resistancesIndex], ref item.m_displayedInfos[barrierIndex]);
    }
    private static void TryDisplayStashAmount(ItemDisplay itemDisplay)
    {
        #region quit
        if (!_displayStashAmount
        || !itemDisplay.m_lblQuantity.TryNonNull(out var quantity)
        || !itemDisplay.RefItem.TryNonNull(out var item)
        || !item.ParentContainer.TryNonNull(out var container)
        || container.SpecialType == ItemContainer.SpecialContainerTypes.Stash
        || !Stashes.TryGetStash(itemDisplay.LocalCharacter, out var stash))
            return;
        #endregion

        int stashAmount = itemDisplay is CurrencyDisplay
            ? stash.ContainedSilver
            : stash.ItemStackCount(item.ItemID);

        if (stashAmount <= 0)
            return;

        if (itemDisplay is not RecipeResultDisplay)
            quantity.text = itemDisplay.m_lastQuantity.ToString();
        else if (itemDisplay.m_dBarUses.TryNonNull(out var dotBar) && dotBar.IsActive())
            quantity.text = "1";

        int fontSize = (quantity.fontSize * 0.75f).Round();
        quantity.alignment = TextAnchor.UpperRight;
        quantity.lineSpacing = 0.75f;
        quantity.text += $"\n<color=#00FF00FF><size={fontSize}><b>+{stashAmount}</b></size></color>";
    }


    // Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(NetworkLevelLoader), nameof(NetworkLevelLoader.UnPauseGameplay))]
    private static void NetworkLevelLoader_UnPauseGameplay_Post(NetworkLevelLoader __instance)
    => _soroboreanCaravanner = null;

    [HarmonyPrefix, HarmonyPatch(typeof(ItemDetailsDisplay), nameof(ItemDetailsDisplay.ShowDetails))]
    private static bool ItemDetailsDisplay_ShowDetails_Pre(ItemDetailsDisplay __instance)
    {
        TrySwapProtectionWithResistances(__instance.m_lastItem);

        #region quit
        if (_details.Value == Details.None || !__instance.m_lastItem.TryNonNull(out var item) || !item.IsIngestible() && item.IsNot<Skill>())
            return true;
        #endregion

        if (item.TryAs(out WaterContainer waterskin)
        && waterskin.GetWaterItem().TryNonNull(out var waterItem))
            item = waterItem;

        int rowIndex = 0;
        foreach (var row in _rowsCache.GetRows(item))
            if (_details.Value.HasFlag(row.Detail))
                __instance.GetRow(rowIndex++).SetInfo(row.Label, row.Content);

        List<ItemDetailRowDisplay> detailRows = __instance.m_detailRows;
        for (int i = rowIndex; i < detailRows.Count; i++)
            detailRows[i].Hide();

        return false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ItemDetailsDisplay), nameof(ItemDetailsDisplay.RefreshDetails))]
    private static void ItemDetailsDisplay_RefreshDetails_Post(ItemDetailsDisplay __instance)
    {
        Item item = __instance.m_lastItem;
        GameObject durabilityHolder = __instance.m_durabilityHolder;
        #region quit
        if (!_barsToggle || item == null || durabilityHolder == null)
            return;
        #endregion

        // Cache
        RectTransform rectTransform = durabilityHolder.GetComponent<RectTransform>();
        bool isFood = item.m_perishScript != null && item.IsNot<Equipment>();
        ModSetting<int> barSize = isFood ? _freshnessBarSize : _durabilityBarSize;
        float curve = isFood ? FRESHNESS_BAR_SCALING_CURVE : DURABILITY_BAR_SCALING_CURVE;

        // Calculate automated values
        float rawSize = float.NaN;
        if (_freshnessTiedToLifespan && isFood)
        {
            float decayRate = item.PerishScript.m_baseDepletionRate;
            float decayTime = 100f / (decayRate * 24f);
            rawSize = decayTime / FRESHNESS_LIFESPAN_MAX;
        }
        else if (_durabilityTiedToMax && !isFood)
            rawSize = item.MaxDurability / DURABILITY_MAX_MAX;

        if (!rawSize.IsNaN())
            barSize.Value = rawSize.Pow(curve).MapFrom01(0, 100f).Round();

        // Assign
        float sizeOffset = barSize / 100f * BAR_MAX_SIZE.x - 1f;
        rectTransform.pivot = BAR_PIVOT;
        rectTransform.localScale = new Vector2(1f + sizeOffset, _barThickness / 100f * BAR_MAX_SIZE.y);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemDetailsDisplay), nameof(ItemDetailsDisplay.RefreshDetail))]
    private static bool ItemDetailsDisplay_RefreshDetail_Post(ItemDetailsDisplay __instance, ref bool __result, int _rowIndex, ItemDetailsDisplay.DisplayedInfos _infoType)
    {
        if (_infoType == ItemDetailsDisplay.DisplayedInfos.AttackSpeed
        && _displayRelativeAttackSpeed)
        {
            float attackSpeedOffset = __instance.cachedWeapon.BaseAttackSpeed - 1f;
            Weapon.WeaponType weaponType = __instance.cachedWeapon.Type;
            if (attackSpeedOffset == 0 || weaponType == Weapon.WeaponType.Shield || weaponType == Weapon.WeaponType.Bow)
                return false;

            string text = (attackSpeedOffset > 0 ? "+" : "") + attackSpeedOffset.ToString("P0");
            Color color = attackSpeedOffset > 0 ? Global.LIGHT_GREEN : Global.LIGHT_RED;
            __instance.GetRow(_rowIndex).SetInfo(LocalizationManager.Instance.GetLoc("ItemStat_AttackSpeed"), Global.SetTextColor(text, color));
            __result = true;
            return false;
        }
        else if ((_infoType == ItemDetailsDisplay.DisplayedInfos.Impact || _infoType == ItemDetailsDisplay.DisplayedInfos.ImpactResistance) && _normalizeImpactDisplay)
        {
            float value = _infoType == ItemDetailsDisplay.DisplayedInfos.Impact
                                     ? __instance.cachedWeapon.Impact
                                     : __instance.cachedEquipment.ImpactResistance;
            if (value <= 0)
                return false;

            TryCacheImpactIcon(__instance.CharacterUI);
            __instance.GetRow(_rowIndex).SetInfo("", value.Round(), _impactIcon);
            __result = true;
            return false;
        }
        else if (_infoType == ItemDetailsDisplay.DisplayedInfos.Durability && _hideNumericalDurability)
            return false;

        return true;
    }

    // Display prices in stash
    [HarmonyPrefix, HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.UpdateValueDisplay))]
    private static bool ItemDisplay_UpdateValueDisplay_Pre(ItemDisplay __instance)
    {
        #region quit
        if (!__instance.CharacterUI.TryNonNull(out var characterUI)
        || characterUI.GetIsMenuDisplayed(CharacterUI.MenuScreens.Shop)
        || !__instance.m_lblValue.TryNonNull(out var priceText)
        || !__instance.RefItem.TryNonNull(out var item)
        || item is WaterContainer
        || !item.IsSellable)
            return true;
        #endregion

        priceText.text =
            _displaySellPricesInCities && SoroboreanCaravanner != null ? item.GetSellValue(characterUI.TargetCharacter, SoroboreanCaravanner).ToString()
            : _displayRawValuesOutsideCities && SoroboreanCaravanner == null ? item.RawBaseValue.ToString()
            : null;

        if (__instance.m_valueHolder.activeSelf != Helpers.Common.Extensions.IsNotEmpty(priceText.text))
            __instance.m_valueHolder.SetActive(!__instance.m_valueHolder.activeSelf);

        return false;
    }

    // Display stash amount
    [HarmonyPostfix, HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.UpdateQuantityDisplay))]
    private static void ItemDisplay_UpdateQuantityDisplay_Post(ItemDisplay __instance)
    => TryDisplayStashAmount(__instance);

    [HarmonyPostfix, HarmonyPatch(typeof(CurrencyDisplay), nameof(CurrencyDisplay.UpdateQuantityDisplay))]
    private static void CurrencyDisplay_UpdateQuantityDisplay_Post(CurrencyDisplay __instance)
    => TryDisplayStashAmount(__instance);

    [HarmonyPostfix, HarmonyPatch(typeof(RecipeResultDisplay), nameof(RecipeResultDisplay.UpdateQuantityDisplay))]
    private static void RecipeResultDisplay_UpdateQuantityDisplay_Post(RecipeResultDisplay __instance)
    => TryDisplayStashAmount(__instance);

    [HarmonyPrefix, HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.RefreshEnchantedIcon))]
    private static void ItemDisplay_RefreshEnchantedIcon_Pre(ItemDisplay __instance)
    {
        #region quit
        if (!__instance.m_refItem.TryAs(out RecipeItem recipe))
            return;
        #endregion

        // Cache
        Image icon = __instance.FindChild<Image>("Icon");
        Image border = icon.FindChild<Image>("border");

        //Defaults
        icon.color = Color.white;
        border.color = Color.white;

        // Quit
        if (!__instance.LocalCharacter.HasLearnedRecipe(recipe.Recipe))
            return;

        // Custom
        icon.color = _learnedRecipeColor;
        border.color = _learnedRecipeColor;
    }

    // Mark items with legacy upgrades
    [HarmonyPrefix, HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.RefreshEnchantedIcon))]
    private static bool ItemDisplay_RefreshEnchantedIcon_Pre2(ItemDisplay __instance)
    {
        #region quit
        if (!_highlightItemsWithLegacyUpgrade
        || __instance.m_refItem == null
        || __instance.m_imgEnchantedIcon == null
        || __instance.m_refItem is Skill)
            return true;
        #endregion

        // Cache
        Image icon = __instance.FindChild<Image>("Icon");
        Image border = icon.FindChild<Image>("border");
        Image indicator = __instance.m_imgEnchantedIcon;

        //Defaults
        icon.color = Color.white;
        border.color = Color.white;
        indicator.Deactivate();

        // Quit
        if (__instance.m_refItem.LegacyItemID <= 0)
            return true;

        // Custom
        border.color = _legacyItemUpgradeColor.Value.NewA(1f);
        indicator.color = _legacyItemUpgradeColor;
        indicator.rectTransform.pivot = 1f.ToVector2();
        indicator.rectTransform.localScale = new Vector2(1.5f, 1.5f);
        indicator.Activate();
        return false;
    }
}
