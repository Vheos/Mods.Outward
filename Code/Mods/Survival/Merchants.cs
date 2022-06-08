namespace Vheos.Mods.Outward;
using Vheos.Helpers.RNG;

public class Merchants : AMod, IDelayedInit
{
    #region const
    private const float DEFAULT_SELL_MODIFIER = 0.3f;
    private static readonly Color DEFAULT_PRICE_COLOR = new(0.8235294f, 0.8877006f, 1f);
    #endregion

    // Settings
    private static ModSetting<int> _pricesCurve;
    private static ModSetting<int> _sellModifier;
    private static ModSetting<Vector2> _pricesBarter;
    private static ModSetting<bool> _pricesPerTypeToggle;
    private static ModSetting<int> _pricesWeapons, _pricesArmors, _pricesIngestibles, _pricesRecipes, _pricesOther;
    private static ModSetting<int> _randomizePricesExtent, _randomizePricesPerDays;
    private static ModSetting<bool> _randomizePricesPerItem, _randomizePricesPerArea;
    private static ModSetting<int> _goldBasePrice;
    protected override void Initialize()
    {
        _pricesCurve = CreateSetting(nameof(_pricesCurve), 100, IntRange(50, 100));
        _sellModifier = CreateSetting(nameof(_sellModifier), DEFAULT_SELL_MODIFIER.Mul(100f).Round(), IntRange(0, 100));

        _pricesPerTypeToggle = CreateSetting(nameof(_pricesPerTypeToggle), false);
        _pricesWeapons = CreateSetting(nameof(_pricesWeapons), 100, IntRange(0, 200));
        _pricesArmors = CreateSetting(nameof(_pricesArmors), 100, IntRange(0, 200));
        _pricesIngestibles = CreateSetting(nameof(_pricesIngestibles), 100, IntRange(0, 200));
        _pricesRecipes = CreateSetting(nameof(_pricesRecipes), 100, IntRange(0, 200));
        _pricesOther = CreateSetting(nameof(_pricesOther), 100, IntRange(0, 200));
        _pricesBarter = CreateSetting(nameof(_pricesBarter), 100f.ToVector2());
        _goldBasePrice = CreateSetting(nameof(_goldBasePrice), 100, IntRange(0, 1000));

        _randomizePricesExtent = CreateSetting(nameof(_randomizePricesExtent), 0, IntRange(0, 100));
        _randomizePricesPerDays = CreateSetting(nameof(_randomizePricesPerDays), 7, IntRange(1, 100));
        _randomizePricesPerItem = CreateSetting(nameof(_randomizePricesPerItem), true);
        _randomizePricesPerArea = CreateSetting(nameof(_randomizePricesPerArea), true);

        // Events
        Item goldIngot = Prefabs.ItemsByID["Gold Ingot".ToItemID().ToString()];
        _goldBasePrice.AddEvent(() => goldIngot.Stats.m_baseValue = _goldBasePrice);
    }
    protected override void SetFormatting()
    {
        _pricesCurve.Format("Prices curve");
        _pricesCurve.Description = "How quickly the prices increase throughout the game\n" +
                                   "at the minimum valued (50%), all prices will be square-root'ed:\n" +
                                   "• Simple Bow: 13 -> 4\n" +
                                   "• War Bow: 1000 -> 32";
        _randomizePricesExtent.Format("Randomize prices");
        _randomizePricesExtent.Description = "Prices will range from [100% - X%] to [100% + X%]\n" +
                                             "and depend on current time, merchant and/or item";
        using (Indent)
        {
            _randomizePricesPerDays.Format("per days", _randomizePricesExtent, t => t > 0);
            _randomizePricesPerDays.Description = "All price modifiers will be rolled every X days";
            _randomizePricesPerArea.Format("per city", _randomizePricesExtent, t => t > 0);
            _randomizePricesPerArea.Description = "Every city (and area) will have its own randomized price modifier";
            _randomizePricesPerItem.Format("per item", _randomizePricesExtent, t => t > 0);
            _randomizePricesPerItem.Description = "Every item will have its own randomized price";
        }
        _pricesPerTypeToggle.Format("Price multipliers");
        using (Indent)
        {
            _pricesWeapons.Format("Weapons", _pricesPerTypeToggle);
            _pricesArmors.Format("Armors", _pricesPerTypeToggle);
            _pricesIngestibles.Format("Food", _pricesPerTypeToggle);
            _pricesRecipes.Format("Recipes", _pricesPerTypeToggle);
            _pricesOther.Format("Other items", _pricesPerTypeToggle);
        }
        _sellModifier.Format("Selling multiplier");
        _pricesBarter.Format("Barter goods");
        _pricesBarter.Description =
            "Price modifier for items that buy and sell for the same value (gold ingot and gemstones)\n" +
            "X   -   buying price modifier\n" +
            "Y   -   selling price modifier";
        _goldBasePrice.Format("Gold ingot base price");
        _goldBasePrice.Description = "Gold ingot's price before applying \"Barter Goods\" multipliers and randomization";
    }
    protected override string Description
    => "• Change final buy/sell modifiers\n" +
       "• Randomize prices based on time, merchant and item\n" +
       "• Set price for learning mutually exclusive skills";
    protected override string SectionOverride
    => ModSections.SurvivalAndImmersion;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _pricesCurve.Value = 90;
                _sellModifier.Value = 20;
                _pricesBarter.Value = new Vector2(50, 40);
                _goldBasePrice.Value = 200;
                _pricesPerTypeToggle.Value = true;
                {
                    _pricesWeapons.Value = 67;
                    _pricesArmors.Value = 67;
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
        }
    }

    // Utility
    private static int GetFinalModifiedPrice(Item item, Character player, Merchant merchant, bool isSelling)
    {
        float price = item.RawCurrentValue;

        if (item.m_overrideSellModifier > 0)
            ApplyBarterModifier(ref price, isSelling);
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


    private static float GetRandomPriceModifier(Item item)
    {
        int itemSeed = _randomizePricesPerItem ? item.ItemID : 0;
        int areaSeed = _randomizePricesPerArea ? AreaManager.Instance.CurrentArea.ID : 0;
        int timeSeed = (Utils.GameTime / 24f / _randomizePricesPerDays).RoundDown();
        RNG.Initialize(itemSeed + areaSeed + timeSeed);

        return 1f + RNG.RangeInclusive(-_randomizePricesExtent, +_randomizePricesExtent) / 100f;
    }
    private static void ApplyCurve(ref float price)
    => price = price.Pow(_pricesCurve / 100f);
    private static void ApplyTypeModifier(ref float price, Item item)
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
    private static void ApplyRandomModifier(ref float price, Item item, bool changePriceColor)
    {
        int preRandomPrice = price.Round();
        price = (price * GetRandomPriceModifier(item)).Round();

        // Color
        if (!changePriceColor
        || !item.m_refItemDisplay.TryNonNull(out var itemDisplay)
        || !itemDisplay.m_lblValue.TryNonNull(out var priceText))
            return;

        float relativeIncrease = price == 0 ? 0f : 1f;
        if (preRandomPrice != 0)
            relativeIncrease = (float)price / preRandomPrice - 1f;
        if (item.ParentContainer is MerchantPouch)
            relativeIncrease *= -1;

        priceText.color = relativeIncrease == 0 ? DEFAULT_PRICE_COLOR
            : Color.Lerp(Color.white, relativeIncrease > 0 ? Color.green
            : Color.red, relativeIncrease.Abs() * 100f / _randomizePricesExtent);
    }
    private static void ApplyVanillaStatModifier(ref float price, Item item, Character player, Merchant merchant, bool isSelling)
    {
        float vanillaModifierIncrease = isSelling ? player.GetItemSellPriceModifier(merchant, item) + merchant.GetItemSellPriceModifier(player, item)
                                                   : player.GetItemBuyPriceModifier(merchant, item) + merchant.GetItemBuyPriceModifier(player, item);
        price *= 1f + vanillaModifierIncrease;
    }
    private static void ApplySellModifier(ref float price)
    => price *= _sellModifier / 100f;
    private static void ApplyBarterModifier(ref float price, bool isSelling)
    => price *= (isSelling ? _pricesBarter.Value.y : _pricesBarter.Value.x) / 100f;

    // Hooks
    [HarmonyPrefix, HarmonyPatch(typeof(Item), nameof(Item.GetBuyValue))]
    private static bool Item_GetBuyValue_Pre(Item __instance, ref int __result, ref Character _player, ref Merchant _merchant)
    {
        __result = GetFinalModifiedPrice(__instance, _player, _merchant, false);
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Item), nameof(Item.GetSellValue))]
    private static bool Item_GetSellValue_Pre(Item __instance, ref int __result, ref Character _player, ref Merchant _merchant)
    {
        __result = GetFinalModifiedPrice(__instance, _player, _merchant, true);
        return false;
    }
}
