﻿namespace Vheos.Mods.Outward;

public class Crafting : AMod
{
    #region Constants
    private const string CRYSTAL_POWDER_RECIPE_UID = "-SEtMHRqWUmvrmyvryV8Ng";
    private static readonly int[] LANTERN_IDS =
    {
        "Explorer Lantern".ToItemID(),
        "Old Lantern".ToItemID(),
        "Glowstone Lantern".ToItemID(),
        "Firefly Lantern".ToItemID(),
        "Lantern of Souls".ToItemID(),
        "Coil Lantern".ToItemID(),
        "Virgin Lantern".ToItemID(),
        "Djinn’s Lamp".ToItemID(),
    };

    private static readonly int[] RELIC_IDS =
    {
        "Calixa’s Relic".ToItemID(),
        "Elatt’s Relic".ToItemID(),
        "Gep’s Generosity".ToItemID(),
        "Haunted Memory".ToItemID(),
        "Leyline Figment".ToItemID(),
        "Pearlbird’s Courage".ToItemID(),
        "Scourge’s Tears".ToItemID(),
        "Vendavel's Hospitality".ToItemID(),
        "Flowering Corruption".ToItemID(),
        "Metalized Bones".ToItemID(),
        "Enchanted Mask".ToItemID(),
        "Noble’s Greed".ToItemID(),
        "Scarlet Whisper".ToItemID(),
        "Calygrey’s Wisdom".ToItemID(),
    };
    private static readonly int[] HAILFROST_WEAPONS_IDS =
    {
        "Hailfrost Claymore".ToItemID(),
        "Hailfrost Mace".ToItemID(),
        "Hailfrost Hammer".ToItemID(),
        "Hailfrost Axe".ToItemID(),
        "Hailfrost Greataxe".ToItemID(),
        "Hailfrost Spear".ToItemID(),
        "Hailfrost Halberd".ToItemID(),
        "Hailfrost Pistol".ToItemID(),
        "Hailfrost Knuckles".ToItemID(),
        "Hailfrost Sword".ToItemID(),
        "Hailfrost Dagger".ToItemID(),
    };
    private static readonly int[] UNIQUE_WEAPONS_IDS =
    {
        "Mysterious Blade".ToItemID(),
        "Mysterious Long Blade".ToItemID(),
        "Ceremonial Bow".ToItemID(),
        "Cracked Red Moon".ToItemID(),
        "Compasswood Staff".ToItemID(),
        "Scarred Dagger".ToItemID(),
        "De-powered Bludgeon".ToItemID(),
        "Unusual Knuckles".ToItemID(),
        "Strange Rusted Sword".ToItemID(),
    };
    #endregion
    #region Enums
    [Flags]
    private enum CraftingExceptions
    {
        None = 0,
        Lanterns = 1 << 1,
        Relics = 1 << 2,
        HailfrostWeapons = 1 << 3,
        UniqueWeapons = 1 << 4,
    }
    #endregion

    private static ModSetting<bool> _preserveDurability;
    private static ModSetting<int> _restoreMissingDurability;
    private static ModSetting<bool> _autoLearnCrystalPowderRecipe;
    private static ModSetting<bool> _limitedManualCrafting;
    private static ModSetting<CraftingExceptions> _limitedManulCraftingExceptions;
    private static ModSetting<int> _extraResultsMultiplier;
    private static ModSetting<float> _craftingDuration;
    protected override void Initialize()
    {
        _preserveDurability = CreateSetting(nameof(_preserveDurability), false);
        _autoLearnCrystalPowderRecipe = CreateSetting(nameof(_autoLearnCrystalPowderRecipe), true);
        _restoreMissingDurability = CreateSetting(nameof(_restoreMissingDurability), 50, IntRange(0, 100));
        _limitedManualCrafting = CreateSetting(nameof(_limitedManualCrafting), false);
        _limitedManulCraftingExceptions = CreateSetting(nameof(CraftingExceptions), (CraftingExceptions)~0);
        _extraResultsMultiplier = CreateSetting(nameof(_extraResultsMultiplier), 100, IntRange(0, 200));
		_craftingDuration = CreateSetting(nameof(_craftingDuration), 0.5f, FloatRange(0f, 10f));
	}
    protected override void SetFormatting()
    {
        _preserveDurability.Format("Preserve durability ratios");
        _preserveDurability.Description = "Crafted items' durability will be based on the average of all ingredients (instead of 100%)";
        using (Indent)
        {
            _restoreMissingDurability.Format("Restore % of missing durability", _preserveDurability);
            _restoreMissingDurability.Description = "Increase to make broken/rotten ingredients still useful (instead of just lowering the durability ratio)";
        }

        _limitedManualCrafting.Format("Limited manual crafting");
        _limitedManualCrafting.Description = "Manual crafting will be limited to 1 ingredient\n" +
                                             "Advanced crafting will require learning recipes first";
        using (Indent)
        {

            _limitedManulCraftingExceptions.Format("Exceptions", _limitedManualCrafting);
            _limitedManulCraftingExceptions.Description = "If you use any of these items in manual crafting, you will be able to use all 4 ingredient slots\n" +
                                                          "This allows you craft items whose recipes can't be learned in advance";
            _autoLearnCrystalPowderRecipe.Format("Auto-learn \"Crystal Powder\" recipe", _limitedManualCrafting);
            _autoLearnCrystalPowderRecipe.Description = "Normally, \"Crystal Powder\" recipe can only be learned via crafting\n" +
                                                        "This will give you the recipe when you interact with the alchemy kit";
        }
        _extraResultsMultiplier.Format("Extra results multiplier");
        _extraResultsMultiplier.Description = "Multiplies the extra (over one) amount of crafting results\n" +
                                              "For example, Gaberry Tartine gives 3 (1+2) items, so the extra amount is 2\n" +
                                              "at 0% extra amount, it will give 1 (1+0) item, and at 200% - 5 (1+4) items";
		_craftingDuration.Format("Crafting duration");
	}
    protected override string Description
    => "• Make crafted items' durability relative to ingredients\n" +
       "• Require recipes for advanced crafting\n" +
       "• Multiply amount of crafted items\n" +
       "• Randomize starting durability of spawned items";
    protected override string SectionOverride
    => ModSections.SurvivalAndImmersion;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _preserveDurability.Value = true;
                _restoreMissingDurability.Value = 50;
                _limitedManualCrafting.Value = true;
                _limitedManulCraftingExceptions.Value = (CraftingExceptions)~0;
                _autoLearnCrystalPowderRecipe.Value = true;
                _extraResultsMultiplier.Value = 50;
                break;
        }
    }

    // Utility
    private static List<Item> GetDestructibleIngredients(CraftingMenu craftingMenu)
    {
        List<Item> destructibleIngredients = new();
        foreach (var ingredientSelector in craftingMenu.m_ingredientSelectors)
            if (ingredientSelector.AssignedIngredient.TryNonNull(out var ingredient))
                foreach (var itemAmountByUID in ingredient.GetConsumedItems(false, out _))
                    if (ItemManager.Instance.GetItem(itemAmountByUID.Key).TryNonNull(out var item)
                    && item.MaxDurability > 0)
                        destructibleIngredients.TryAddUnique(item);
        return destructibleIngredients;
    }
    private static List<Item> GetDestructibleResults(CraftingMenu craftingMenu)
    {
        // Choose recipe index
        int selectorIndex = craftingMenu.m_lastRecipeIndex;
        int recipeIndex = selectorIndex >= 0 ? craftingMenu.m_complexeRecipes[selectorIndex].Key : craftingMenu.m_lastFreeRecipeIndex;
        List<Item> desctructibleResults = new();

        // Execute
        if (recipeIndex >= 0)
        {
            Recipe recipe = craftingMenu.m_allRecipes[recipeIndex];
            foreach (var result in recipe.Results)
                if (result.RefItem.TryNonNull(out var item)
                && item.MaxDurability > 0)
                    desctructibleResults.TryAddUnique(item);
        }
        return desctructibleResults;
    }
    private static void SetSingleIngredientCrafting(CraftingMenu __instance, bool enabled)
    {
        __instance.m_multipleIngrenentsBrackground.SetAlpha(enabled ? 0f : 1f);
        __instance.m_singleIngredientBackground.SetAlpha(enabled ? 1f : 0f);
        for (int i = 1; i < __instance.m_ingredientSelectors.Length; i++)
            __instance.m_ingredientSelectors[i].SetActive(!enabled);
    }
    private static int GetModifiedResultsAmount(ItemReferenceQuantity result)
    => 1 + (result.Quantity - 1f).Mul(_extraResultsMultiplier / 100f).Round();

    // Hooks
    [HarmonyPrefix, HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.CraftingDone))]
    private static void CraftingMenu_CraftingDone_Pre(CraftingMenu __instance, ref List<Item> __state)
    {
        List<Item> ingredients = GetDestructibleIngredients(__instance);
        List<Item> results = GetDestructibleResults(__instance);
        #region quit
        if (!_preserveDurability || ingredients.IsNullOrEmpty() || ingredients.IsNullOrEmpty())
            return;
        #endregion

        float averageRatio = 0;
        foreach (var item in ingredients)
            averageRatio += item.DurabilityRatio;
        averageRatio /= ingredients.Count;

        foreach (var item in results)
            if (item.Stats.TryNonNull(out var stats))
                stats.StartingDurability = (stats.MaxDurability * averageRatio.Lerp(1f, _restoreMissingDurability / 100f)).Round();

        __state = results;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.CraftingDone))]
    private static void CraftingMenu_CraftingDone_Post(CraftingMenu __instance, ref List<Item> __state)
    {
        #region quit
        if (__state == null)
            return;
        #endregion

        foreach (var item in __state)
            if (item.Stats.TryNonNull(out var stats))
                stats.StartingDurability = -1;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.OnRecipeSelected))]
    private static void CraftingMenu_OnRecipeSelected_Post(CraftingMenu __instance)
    {
        #region quit
        if (!_limitedManualCrafting)
            return;
        #endregion

        SetSingleIngredientCrafting(__instance, __instance.m_lastRecipeIndex == -1);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.IngredientSelectorHasChanged))]
    private static void CraftingMenu_IngredientSelectorHasChanged_Post(CraftingMenu __instance, int _selectorIndex, int _itemID)
    {
        #region quit
        if (!_limitedManualCrafting 
		|| __instance.m_lastRecipeIndex >= 0
		|| __instance?.m_ingredientSelectors[0]?.m_itemDisplay?.m_refItem is not Item itemInFirstSlot)
            return;
		#endregion

		bool isMulti = _limitedManulCraftingExceptions.Value.HasFlag(CraftingExceptions.Lanterns) && itemInFirstSlot.ItemID.IsContainedIn(LANTERN_IDS)
                    || _limitedManulCraftingExceptions.Value.HasFlag(CraftingExceptions.Relics) && itemInFirstSlot.ItemID.IsContainedIn(RELIC_IDS)
                    || _limitedManulCraftingExceptions.Value.HasFlag(CraftingExceptions.HailfrostWeapons) && itemInFirstSlot.ItemID.IsContainedIn(HAILFROST_WEAPONS_IDS)
                    || _limitedManulCraftingExceptions.Value.HasFlag(CraftingExceptions.UniqueWeapons) && itemInFirstSlot.ItemID.IsContainedIn(UNIQUE_WEAPONS_IDS);

        SetSingleIngredientCrafting(__instance, !isMulti);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.Show), new[] { typeof(CraftingStation) })]
    private static void CraftingMenu_Show_Pre(CraftingMenu __instance, CraftingStation _ustensil)
    {
        #region quit
        if (!_limitedManualCrafting
        || _ustensil.StationType != Recipe.CraftingType.Alchemy
        || !RecipeManager.Instance.m_recipes.TryGetValue(CRYSTAL_POWDER_RECIPE_UID, out var crystalPowderRecipe)
        || __instance.LocalCharacter.HasLearnedRecipe(crystalPowderRecipe))
            return;
        #endregion

        __instance.LocalCharacter.LearnRecipe(crystalPowderRecipe);
        return;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.GenerateResult))]
    private static void CraftingMenu_GenerateResult_Pre(CraftingMenu __instance, ref int __state, ItemReferenceQuantity _result)
    {
        #region quit
        if (_extraResultsMultiplier == 100)
            return;
        #endregion

        __state = _result.Quantity;
        _result.Quantity = GetModifiedResultsAmount(_result);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.GenerateResult))]
    private static void CraftingMenu_GenerateResult_Post(CraftingMenu __instance, ref int __state, ItemReferenceQuantity _result)
    {
        #region quit
        if (_extraResultsMultiplier == 100)
            return;
        #endregion

        _result.Quantity = __state;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RecipeResultDisplay), nameof(RecipeResultDisplay.UpdateQuantityDisplay))]
    private static bool RecipeResultDisplay_UpdateQuantityDisplay_Pre(RecipeResultDisplay __instance)
    {
        #region quit
        if (_extraResultsMultiplier == 100 || __instance.StackCount <= 1)
            return true;
        #endregion

        int amount = GetModifiedResultsAmount(__instance.m_result);
        __instance.m_lblQuantity.text = amount > 0 ? amount.ToString() : "";
        return false;
    }

	[HarmonyPrefix, HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.TryCraft))]
	private static void CraftingMenu_TryCraft_Pre(CraftingMenu __instance)
		=> __instance.CraftingTime = _craftingDuration;
}
