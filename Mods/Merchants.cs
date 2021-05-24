using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;
using Random = UnityEngine.Random;



namespace ModPack
{
    public class Merchants : AMod
    {
        #region const
        private const float DEFAULT_SELL_MODIFIER = 0.3f;
        private const int GOLD_INGOT_ID = 6300030;
        static private readonly Color DEFAULT_PRICE_COLOR = new Color(0.8235294f, 0.8877006f, 1f);
        #endregion

        // Settings
        static private ModSetting<int> _pricesCurve;
        static private ModSetting<int> _sellModifier;
        static private ModSetting<Vector2> _pricesGold;
        static private ModSetting<bool> _pricesPerTypeToggle;
        static private ModSetting<int> _pricesWeapons, _pricesArmors, _pricesIngestibles, _pricesRecipes, _pricesOther;
        static private ModSetting<int> _randomizePricesExtent, _randomizePricesPerDays;
        static private ModSetting<bool> _randomizePricesPerItem, _randomizePricesPerArea;
        override protected void Initialize()
        {
            _pricesCurve = CreateSetting(nameof(_pricesCurve), 100, IntRange(50, 100));
            _sellModifier = CreateSetting(nameof(_sellModifier), DEFAULT_SELL_MODIFIER.Mul(100f).Round(), IntRange(0, 100));

            _pricesPerTypeToggle = CreateSetting(nameof(_pricesPerTypeToggle), false);
            _pricesWeapons = CreateSetting(nameof(_pricesWeapons), 100, IntRange(0, 200));
            _pricesArmors = CreateSetting(nameof(_pricesArmors), 100, IntRange(0, 200));
            _pricesIngestibles = CreateSetting(nameof(_pricesIngestibles), 100, IntRange(0, 200));
            _pricesRecipes = CreateSetting(nameof(_pricesRecipes), 100, IntRange(0, 200));
            _pricesOther = CreateSetting(nameof(_pricesOther), 100, IntRange(0, 200));
            _pricesGold = CreateSetting(nameof(_pricesGold), 100f.ToVector2());

            _randomizePricesExtent = CreateSetting(nameof(_randomizePricesExtent), 0, IntRange(0, 100));
            _randomizePricesPerDays = CreateSetting(nameof(_randomizePricesPerDays), 7, IntRange(1, 100));
            _randomizePricesPerItem = CreateSetting(nameof(_randomizePricesPerItem), true);
            _randomizePricesPerArea = CreateSetting(nameof(_randomizePricesPerArea), true);
        }
        override protected void SetFormatting()
        {
            _pricesCurve.Format("Prices curve");
            _pricesCurve.Description = "How quickly the prices increase throughout the game\n" +
                                       "at the minimum valued (50%), all prices will be square-root'ed:\n" +
                                       "• Simple Bow: 13 -> 4\n" +
                                       "• War Bow: 1000 -> 32";
            _sellModifier.Format("Selling multiplier");
            _randomizePricesExtent.Format("Randomize prices");
            _randomizePricesExtent.Description = "Prices will range from [100% - X%] to [100% + X%]\n" +
                                                 "and depend on current time, merchant and/or item";
            Indent++;
            {
                _randomizePricesPerDays.Format("per days", _randomizePricesExtent, () => _randomizePricesExtent > 0);
                _randomizePricesPerDays.Description = "All price modifiers will be rolled every X days";
                _randomizePricesPerArea.Format("per city", _randomizePricesExtent, () => _randomizePricesExtent > 0);
                _randomizePricesPerArea.Description = "Every city (and area) will have its own randomized price modifier";
                _randomizePricesPerItem.Format("per item", _randomizePricesExtent, () => _randomizePricesExtent > 0);
                _randomizePricesPerItem.Description = "Every item will have its own randomized price";
                Indent--;
            }
            _pricesPerTypeToggle.Format("Prices by item type");
            Indent++;
            {
                _pricesWeapons.Format("Weapons", _pricesPerTypeToggle);
                _pricesArmors.Format("Armors", _pricesPerTypeToggle);
                _pricesIngestibles.Format("Food", _pricesPerTypeToggle);
                _pricesRecipes.Format("Recipes", _pricesPerTypeToggle);
                _pricesOther.Format("Other items", _pricesPerTypeToggle);
                Indent--;
            }
            _pricesGold.Format("Gold");
            _pricesGold.Description = "X   -   Gold ingot's buying price\n" +
                                      "Y   -   Gold ingot's selling price";
        }
        override protected string Description
        => "• Change final buy/sell modifiers\n" +
           "• Randomize prices based on time, merchant and item\n" +
           "• Set price for learning mutually exclusive skills";
        override protected string SectionOverride
        => SECTION_SURVIVAL;
        override public void LoadPreset(Presets.Preset preset)
        {
            switch (preset)
            {
                case Presets.Preset.Vheos_CoopSurvival:
                    ForceApply();
                    _pricesCurve.Value = 90;
                    _sellModifier.Value = 20;
                    _pricesGold.Value = new Vector2(100, 90);
                    _pricesPerTypeToggle.Value = true;
                    {
                        _pricesWeapons.Value = 50;
                        _pricesArmors.Value = 50;
                        _pricesIngestibles.Value = 100;
                        _pricesRecipes.Value = 50;
                        _pricesOther.Value = 100;
                    }
                    _randomizePricesExtent.Value = 20;
                    {
                        _randomizePricesPerDays.Value = 7;
                        _randomizePricesPerArea.Value = true;
                        _randomizePricesPerItem.Value = true;
                    }
                    break;

                case Presets.Preset.IggyTheMad_TrueHardcore:
                    break;
            }
        }

        // Utility
        static private int GetFinalModifiedPrice(Item item, Character player, Merchant merchant, bool isSelling)
        {
            float price = item.RawCurrentValue;

            if (item.ItemID == GOLD_INGOT_ID)
                ApplyGoldPrice(ref price, isSelling);
            else
            {
                ApplyCurve(ref price);
                if (_pricesPerTypeToggle)
                    ApplyTypeModifier(ref price, item);
                ApplyVanillaStatModifier(ref price, item, player, merchant, isSelling);
                if (isSelling)
                    ApplySellModifier(ref price);
            }
            if (_randomizePricesExtent > 0)
                ApplyRandomModifier(ref price, item, true);

            return price.Round();
        }
        static private float GetRandomPriceModifier(Item item)
        {
            int itemSeed = _randomizePricesPerItem ? item.ItemID : 0;
            int areaSeed = _randomizePricesPerArea ? AreaManager.Instance.CurrentArea.ID : 0;
            int timeSeed = (GameTime / 24f / _randomizePricesPerDays).RoundDown();
            Random.InitState(itemSeed + areaSeed + timeSeed);

            return 1f + Random.Range(-_randomizePricesExtent, +_randomizePricesExtent) / 100f;
        }
        static private void ApplyCurve(ref float price)
        => price = price.Pow(_pricesCurve / 100f);
        static private void ApplyTypeModifier(ref float price, Item item)
        {
            float modifier = _pricesOther;
            if (item is Weapon)
                modifier = _pricesWeapons;
            else if (item is Armor)
                modifier = _pricesArmors;
            else if (item is RecipeItem)
                modifier = _pricesRecipes;
            else if (item.IsIngestible())
                modifier = _pricesIngestibles;
            price *= modifier / 100f;
        }
        static private void ApplyRandomModifier(ref float price, Item item, bool changePriceColor)
        {
            int preRandomPrice = price.Round();
            price = (price * GetRandomPriceModifier(item)).Round();

            // Color
            if (!changePriceColor
            || !item.m_refItemDisplay.TryAssign(out var itemDisplay)
            || !itemDisplay.m_lblValue.TryAssign(out var priceText))
                return;

            float relativeIncrease = price == 0 ? 0f : 1f;
            if (preRandomPrice != 0)
                relativeIncrease = (float)price / preRandomPrice - 1f;
            if (item.OwnerCharacter == null)
                relativeIncrease *= -1;

            if (relativeIncrease == 0)
                priceText.color = DEFAULT_PRICE_COLOR;
            else
                priceText.color = Color.Lerp(Color.white, relativeIncrease > 0 ? Color.green : Color.red, relativeIncrease.Abs() * 100f / _randomizePricesExtent);
        }
        static private void ApplyVanillaStatModifier(ref float price, Item item, Character player, Merchant merchant, bool isSelling)
        {
            float vanillaModifierIncrease = isSelling ? player.GetItemSellPriceModifier(merchant, item) + merchant.GetItemSellPriceModifier(player, item)
                                                       : player.GetItemBuyPriceModifier(merchant, item) + merchant.GetItemBuyPriceModifier(player, item);
            price *= 1f + vanillaModifierIncrease;
        }
        static private void ApplySellModifier(ref float price)
        => price *= _sellModifier / 100f;
        static private void ApplyGoldPrice(ref float price, bool isSelling)
        => price = isSelling ? _pricesGold.Value.y : _pricesGold.Value.x;

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        [HarmonyPatch(typeof(Item), "GetBuyValue"), HarmonyPrefix]
        static bool Item_GetBuyValue_Pre(Item __instance, ref int __result, ref Character _player, ref Merchant _merchant)
        {
            __result = GetFinalModifiedPrice(__instance, _player, _merchant, false);
            return false;
        }

        [HarmonyPatch(typeof(Item), "GetSellValue"), HarmonyPrefix]
        static bool Item_GetSellValue_Pre(Item __instance, ref int __result, ref Character _player, ref Merchant _merchant)
        {
            __result = GetFinalModifiedPrice(__instance, _player, _merchant, true);
            return false;
        }
    }
}