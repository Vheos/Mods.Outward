using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Linq;
using System.Collections.Generic;
using System;



/* TO DO:
 * - extend to more item types (rags, varnishes)
 * - add removed status effects
 */
namespace ModPack
{
    public class Descriptions : AMod, IDelayedInit
    {
        #region const
        static private readonly Vector2 BAR_MAX_SIZE = new Vector2(2.75f, 2.50f);
        static private readonly Vector2 BAR_PIVOT = new Vector2(0f, 1f);
        private const float DURABILITY_MAX_MAX = 777f;    // Duty (unique halberd)
        private const float FRESHNESS_LIFESPAN_MAX = 104f;   // Travel Ration
        private const int SIZE_MIN = 0;
        private const int DEFAULT_FONT_SIZE = 19;
        #endregion
        #region enum
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
            Cooldown = 1 << 7,
            Costs = 1 << 8,
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
                    if (Postfix != null)
                        formattedContent += Postfix;

                    if (Size != DEFAULT_FONT_SIZE)
                        formattedContent = $"<size={Size}>{formattedContent}</size>";

                    if (_colorsToggle && _colorSetting != null)
                        return $"<color=#{ColorUtility.ToHtmlStringRGBA(_colorSetting)}>{formattedContent}</color>";

                    return formattedContent;
                }
            }
            public Details Detail;
            public int Order
            { get; }
            public int Size
            { get; }
            public string Prefix;
            public string Postfix;

            // Private
            private string _content;
            private ModSetting<Color> _colorSetting;

            // Constructors
            public Row(string label, string content, Details detail, int order = int.MaxValue, ModSetting<Color> colorSetting = null)
            {
                Label = label;
                _content = content;
                Detail = detail;
                Order = order;
                _colorSetting = colorSetting;
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
                if (!_rowsByItem.ContainsKey(item))
                    CacheItemRows(item);
                return _rowsByItem[item];
            }

            // Privates
            private Dictionary<Item, List<Row>> _rowsByItem;
            private void CacheItemRows(Item item)
            {
                List<Row> rows = new List<Row>();

                if (item.TryAs(out Skill skill))
                    FormatSkillRows(skill, rows);
                else
                    foreach (var effect in item.GetEffects())
                    {
                        Row newRow = GetFormattedItemRow(effect);
                        if (newRow != null)
                            rows.Add(newRow);
                    }

                rows.Sort((a, b) => a.Order.CompareTo(b.Order));
                _rowsByItem.Add(item, rows);
            }

            // Constructors
            public RowsCache()
            {
                _rowsByItem = new Dictionary<Item, List<Row>>();
            }
        }
        #endregion

        // Settings
        static private ModSetting<bool> _colorsToggle, _barsToggle;
        static private ModSetting<bool> _addBackgrounds;
        static private ModSetting<Details> _details;
        static private ModSetting<Color> _healthColor, _staminaColor, _manaColor, _needsColor, _corruptionColor, _statusEffectColor, _cooldownColor;
        static private ModSetting<int> _durabilityBarSize, _freshnessBarSize, _barThickness;
        static private ModSetting<bool> _durabilityTiedToMax, _freshnessTiedToLifespan;
        override protected void Initialize()
        {
            _colorsToggle = CreateSetting(nameof(_colorsToggle), false);
            _details = CreateSetting(nameof(_details), Details.None);
            _healthColor = CreateSetting(nameof(_healthColor), new Color(0.765f, 0.522f, 0.525f, 1f));
            _staminaColor = CreateSetting(nameof(_staminaColor), new Color(0.827f, 0.757f, 0.584f, 1f));
            _manaColor = CreateSetting(nameof(_manaColor), new Color(0.529f, 0.702f, 0.816f, 1f));
            _needsColor = CreateSetting(nameof(_needsColor), new Color(0.584f, 0.761f, 0.522f, 1f));
            _corruptionColor = CreateSetting(nameof(_corruptionColor), new Color(0.655f, 0.647f, 0.282f, 1f));
            _statusEffectColor = CreateSetting(nameof(_statusEffectColor), new Color(1f, 1f, 1f, 1f));

            _barsToggle = CreateSetting(nameof(_barsToggle), false);
            _durabilityTiedToMax = CreateSetting(nameof(_durabilityTiedToMax), false);
            _durabilityBarSize = CreateSetting(nameof(_durabilityBarSize), (100 / BAR_MAX_SIZE.x).Round(), IntRange(0, 100));
            _freshnessTiedToLifespan = CreateSetting(nameof(_freshnessTiedToLifespan), false);
            _freshnessBarSize = CreateSetting(nameof(_freshnessBarSize), (100 / BAR_MAX_SIZE.x).Round(), IntRange(0, 100));
            _barThickness = CreateSetting(nameof(_barThickness), (100 / BAR_MAX_SIZE.y).Round(), IntRange(0, 100));

            _addBackgrounds = CreateSetting(nameof(_addBackgrounds), false);

            AddEventOnConfigClosed(() => SetBackgrounds(_addBackgrounds));

            _rowsCache = new RowsCache();
        }
        override protected void SetFormatting()
        {

            _details.Format("Details to display");
            _colorsToggle.Description = "Extra details to display";
            _colorsToggle.Format("Colors");
            _colorsToggle.Description = "Change colors of displayed details";
            Indent++;
            {
                _healthColor.Format("Health", _colorsToggle);
                _healthColor.Description = "Health, max health and health regen";
                _staminaColor.Format("Stamina", _colorsToggle);
                _staminaColor.Description = "Stamina, max stamina and stamina regen";
                _manaColor.Format("Mana", _colorsToggle);
                _manaColor.Description = "Mana, max mana and mana regen";
                _needsColor.Format("Needs", _colorsToggle);
                _needsColor.Description = "Food, drink and sleep";
                _corruptionColor.Format("Corruption", _colorsToggle);
                _corruptionColor.Description = "Corruption and corruption regen";
                _statusEffectColor.Format("Status effect", _colorsToggle);
                Indent--;
            }

            _barsToggle.Format("Bars");
            _barsToggle.Description = "Change sizes of durability and freshness progress bars";
            Indent++;
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
                Indent--;
            }

            _addBackgrounds.Format("Add backgrounds to foods/drinks");
            _addBackgrounds.Description = "Display a big \"potions\" icon in the background of foods' and drinks' description box (by default, only Life Potion uses it)";
        }
        protected override string Description
        => "• Display extra item details in inventory\n" +
        "(restored health/stamina/mana, granted status effects)\n" +
        "• Override durability and freshness bars\n" +
        "(automatic scaling, thickness)";

        // Utility
        static private RowsCache _rowsCache;
        static private void SetBackgrounds(bool state)
        {
            Item lifePotion = Prefabs.GetIngestibleByName("Life Potion");
            Sprite potionBackground = lifePotion.m_overrideSigil;
            foreach (var ingestibleByID in Prefabs.IngestiblesByID)
                if (ingestibleByID.Value != lifePotion)
                    ingestibleByID.Value.m_overrideSigil = state ? potionBackground : null;
        }
        static private Row GetFormattedItemRow(Effect effect)
        {
            switch (effect)
            {
                // Vitals
                case AffectHealth _:
                    return new Row("CharacterStat_Health".Localized(),
                                   FormatEffectValue(effect),
                                   Details.Vitals, 31, _healthColor);
                case AffectStamina _:
                    return new Row("CharacterStat_Stamina".Localized(),
                                   FormatEffectValue(effect),
                                   Details.Vitals, 41, _staminaColor);
                case AffectMana _:
                    return new Row("CharacterStat_Mana".Localized(),
                                   FormatEffectValue(effect),
                                   Details.Vitals, 51, _manaColor);
                // Max vitals
                case AffectBurntHealth _:
                    return new Row("General_Max".Localized() + ". " + "CharacterStat_Health".Localized(),
                                   FormatEffectValue(effect),
                                   Details.MaxVitals, 33, _healthColor);
                case AffectBurntStamina _:
                    return new Row("General_Max".Localized() + ". " + "CharacterStat_Stamina".Localized(),
                                   FormatEffectValue(effect),
                                   Details.MaxVitals, 43, _staminaColor);
                case AffectBurntMana _:
                    return new Row("General_Max".Localized() + ". " + "CharacterStat_Mana".Localized(),
                                   FormatEffectValue(effect),
                                   Details.MaxVitals, 53, _manaColor);
                // Needs
                case AffectFood _:
                    return new Row("CharacterStat_Food".Localized(),
                                   FormatEffectValue(effect, 10f, "%"),
                                   Details.Needs, 11, _needsColor);
                case AffectDrink _:
                    return new Row("CharacterStat_Drink".Localized(),
                                   FormatEffectValue(effect, 10f, "%"),
                                   Details.Needs, 12, _needsColor);
                case AffectFatigue _:
                    return new Row("CharacterStat_Sleep".Localized(),
                                   FormatEffectValue(effect, 10f, "%"),
                                   Details.Needs, 13, _needsColor);
                // Corruption
                case AffectCorruption _:
                    return new Row("CharacterStat_Corruption".Localized(),
                                   FormatEffectValue(effect, 10f, "%"),
                                   Details.Corruption, 61, _corruptionColor);
                // Status
                case AddStatusEffect addStatusEffect:
                    StatusEffect statusEffect = addStatusEffect.Status;
                    Row statusName = new Row("",
                                             $"+ {statusEffect.StatusName}",
                                             Details.StatusEffects, 100, _statusEffectColor);
                    if (addStatusEffect.ChancesToContract < 100)
                        statusName.Prefix = $"<color=silver>({addStatusEffect.ChancesToContract}%)</color> ";

                    if (!statusEffect.HasEffectsAndDatas())
                        return statusName;

                    StatusData.EffectData firstEffectData = statusEffect.GetDatas()[0];
                    if (firstEffectData.Data.IsEmpty())
                        return statusName;

                    string firstValue = firstEffectData.Data[0];
                    switch (statusEffect.GetEffects()[0])
                    {
                        case AffectHealth _:
                            return new Row("CharacterStat_Health".Localized() + " Regen",
                                           FormatStatusEffectValue(firstValue.ToFloat(), statusEffect.StartLifespan),
                                           Details.Vitals | Details.RegenRates, 32, _healthColor);
                        case AffectStamina _:
                            return new Row("CharacterStat_Stamina".Localized() + " Regen",
                                           FormatStatusEffectValue(firstValue.ToFloat(), statusEffect.StartLifespan),
                                           Details.Vitals | Details.RegenRates, 42, _staminaColor);
                        case AffectMana _:
                            return new Row("CharacterStat_Mana".Localized() + " Regen",
                                           FormatStatusEffectValue(firstValue.ToFloat(), statusEffect.StartLifespan, 1f, "%"),
                                           Details.Vitals | Details.RegenRates, 52, _manaColor);
                        case AffectCorruption _:
                            return new Row("CharacterStat_Corruption".Localized() + " Regen",
                                           FormatStatusEffectValue(firstValue.ToFloat(), statusEffect.StartLifespan, 10f, "%"),
                                           Details.Corruption | Details.RegenRates, 62, _corruptionColor);
                        default: return statusName;
                    }
                default: return null;
            }
        }
        static private void FormatSkillRows(Skill skill, List<Row> rows)
        {
            if (skill.Cooldown > 0)
                rows.Add(new Row("ItemStat_Cooldown".Localized(),
                                  skill.Cooldown.FormatSeconds(skill.Cooldown >= 60),
                                  Details.Cooldown, 11, _cooldownColor));
            if (skill.HealthCost > 0)
                rows.Add(new Row("CharacterStat_Health".Localized() + " " + "BuildingMenu_Supplier_Cost".Localized(),
                                  skill.HealthCost.ToString(),
                                  Details.Costs, 12, _healthColor));
            if (skill.StaminaCost > 0)
                rows.Add(new Row("CharacterStat_Stamina".Localized() + " " + "BuildingMenu_Supplier_Cost".Localized(),
                                  skill.StaminaCost.ToString(),
                                  Details.Costs, 13, _staminaColor));
            if (skill.ManaCost > 0)
                rows.Add(new Row("CharacterStat_Mana".Localized() + " " + "BuildingMenu_Supplier_Cost".Localized(),
                                  skill.ManaCost.ToString(),
                                  Details.Costs, 14, _manaColor));
            if (skill.DurabilityCost > 0 || skill.DurabilityCostPercent > 0)
            {
                bool isPercent = skill.DurabilityCostPercent > 0;
                rows.Add(new Row("ItemStat_Durability".Localized() + " " + "BuildingMenu_Supplier_Cost".Localized(),
                                  isPercent ? (skill.DurabilityCostPercent.ToString() + "%") : skill.DurabilityCost.ToString(),
                                  Details.Costs, 15, _needsColor));
            }

        }
        static private string FormatEffectValue(Effect effect, float divisor = 1f, string postfix = "")
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
        static private string FormatStatusEffectValue(float value, float duration, float divisor = 1f, string postfix = "")
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

        [HarmonyPatch(typeof(ItemDetailsDisplay), "ShowDetails"), HarmonyPrefix]
        static bool ItemDetailsDisplay_ShowDetails_Pre(ref ItemDetailsDisplay __instance)
        {
            Item item = __instance.m_lastItem;
            #region quit
            if (_details.Value == Details.None || item == null || !item.IsIngestible() && item.IsNot<Skill>())
                return true;
            #endregion

            int rowIndex = 0;
            foreach (var row in _rowsCache.GetRows(item))
                if (_details.Value.HasFlag(row.Detail))
                    __instance.GetRow(rowIndex++).SetInfo(row.Label, row.Content);

            List<ItemDetailRowDisplay> detailRows = __instance.m_detailRows;
            for (int i = rowIndex; i < detailRows.Count; i++)
                detailRows[i].Hide();

            return false;
        }

        [HarmonyPatch(typeof(ItemDetailsDisplay), "RefreshDetails"), HarmonyPostfix]
        static void ItemDetailsDisplay_RefreshDetails_Post(ref ItemDetailsDisplay __instance)
        {
            Item item = __instance.m_lastItem;
            GameObject durabilityHolder = __instance.m_durabilityHolder;
            #region quit
            if (!_barsToggle || item == null || durabilityHolder == null)
                return;
            #endregion

            // Cache
            RectTransform rectTransform = durabilityHolder.GetComponent<RectTransform>();
            ModSetting<int> barSize = item.IsPerishable && item.IsNot<Equipment>() ? _freshnessBarSize : _durabilityBarSize;

            // Calculate automated values
            float rawSize = float.NaN;
            if (_freshnessTiedToLifespan && barSize == _freshnessBarSize)
            {
                float decayRate = item.PerishScript.m_baseDepletionRate;
                float decayTime = 100f / (decayRate * 24f);
                rawSize = (decayTime / FRESHNESS_LIFESPAN_MAX).Sqrt();
            }
            else if (_durabilityTiedToMax && barSize == _durabilityBarSize)
                rawSize = (item.MaxDurability / DURABILITY_MAX_MAX).Sqrt();

            if (!rawSize.IsNaN())
                barSize.Value = rawSize.MapFrom01(SIZE_MIN, 100f).Round();

            // Assign
            float sizeOffset = barSize / 100f * BAR_MAX_SIZE.x - 1f;
            rectTransform.pivot = BAR_PIVOT;
            rectTransform.localScale = new Vector2(1f + sizeOffset, _barThickness / 100f * BAR_MAX_SIZE.y);
        }
    }
}