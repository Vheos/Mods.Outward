namespace Vheos.Mods.Outward
{
    using System;
    using System.Collections.Generic;
    using HarmonyLib;
    using Mods.Core;
    using Tools.Extensions.Math;
    using Tools.Extensions.General;
    using Tools.Extensions.Collections;
    public class Crafting : AMod, IDelayedInit
    {
        #region const
        private const int CRYSTAL_POWDER_ID = 6600040;
        private static readonly int[] LANTERN_IDS =
        {
            "Explorer Lantern".ItemID(),
            "Old Lantern".ItemID(),
            "Glowstone Lantern".ItemID(),
            "Firefly Lantern".ItemID(),
            "Lantern of Souls".ItemID(),
            "Coil Lantern".ItemID(),
            "Virgin Lantern".ItemID(),
            "Djinn’s Lamp".ItemID(),
        };

        private static readonly int[] RELIC_IDS =
        {
            "Calixa’s Relic".ItemID(),
            "Elatt’s Relic".ItemID(),
            "Gep’s Generosity".ItemID(),
            "Haunted Memory".ItemID(),
            "Leyline Figment".ItemID(),
            "Pearlbird’s Courage".ItemID(),
            "Scourge’s Tears".ItemID(),
            "Vendavel's Hospitality".ItemID(),
            "Flowering Corruption".ItemID(),
            "Metalized Bones".ItemID(),
            "Enchanted Mask".ItemID(),
            "Noble’s Greed".ItemID(),
            "Scarlet Whisper".ItemID(),
            "Calygrey’s Wisdom".ItemID(),
        };
        private static readonly int[] HAILFROST_WEAPONS_IDS =
        {
            "Hailfrost Claymore".ItemID(),
            "Hailfrost Mace".ItemID(),
            "Hailfrost Hammer".ItemID(),
            "Hailfrost Axe".ItemID(),
            "Hailfrost Greataxe".ItemID(),
            "Hailfrost Spear".ItemID(),
            "Hailfrost Halberd".ItemID(),
            "Hailfrost Pistol".ItemID(),
            "Hailfrost Knuckles".ItemID(),
            "Hailfrost Sword".ItemID(),
            "Hailfrost Dagger".ItemID(),
        };
        private static readonly int[] UNIQUE_WEAPONS_IDS =
        {
            "Mysterious Blade".ItemID(),
            "Mysterious Long Blade".ItemID(),
            "Ceremonial Bow".ItemID(),
            "Cracked Red Moon".ItemID(),
            "Compasswood Staff".ItemID(),
            "Scarred Dagger".ItemID(),
            "De-powered Bludgeon".ItemID(),
            "Unusual Knuckles".ItemID(),
            "Strange Rusted Sword".ItemID(),
        };
        #endregion
        #region enum
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

        static private ModSetting<bool> _preserveDurability;
        static private ModSetting<int> _restoreMissingDurability;
        static private ModSetting<bool> _autoLearnCrystalPowderRecipe;
        static private ModSetting<bool> _limitedManualCrafting;
        static private ModSetting<CraftingExceptions> _limitedManulCraftingExceptions;
        static private ModSetting<int> _extraResultsMultiplier;
        override protected void Initialize()
        {
            _preserveDurability = CreateSetting(nameof(_preserveDurability), false);
            _autoLearnCrystalPowderRecipe = CreateSetting(nameof(_autoLearnCrystalPowderRecipe), true);
            _restoreMissingDurability = CreateSetting(nameof(_restoreMissingDurability), 50, IntRange(0, 100));
            _limitedManualCrafting = CreateSetting(nameof(_limitedManualCrafting), false);
            _limitedManulCraftingExceptions = CreateSetting(nameof(CraftingExceptions), (CraftingExceptions)~0);
            _extraResultsMultiplier = CreateSetting(nameof(_extraResultsMultiplier), 100, IntRange(0, 200));

            _crystalPowderRecipe = FindCrystalPowderRecipe();
        }
        override protected void SetFormatting()
        {
            _preserveDurability.Format("Preserve durability ratios");
            _preserveDurability.Description = "Crafted items' durability will be based on the average of all ingredients (instead of 100%)";
            using(Indent)
            {
                _restoreMissingDurability.Format("Restore % of missing durability", _preserveDurability);
                _restoreMissingDurability.Description = "Increase to make broken/rotten ingredients still useful (instead of just lowering the durability ratio)";
            }

            _limitedManualCrafting.Format("Limited manual crafting");
            _limitedManualCrafting.Description = "Manual crafting will be limited to 1 ingredient\n" +
                                                 "Advanced crafting will require learning recipes first";
            using(Indent)
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
        }
        override protected string Description
        => "• Make crafted items' durability relative to ingredients\n" +
           "• Require recipes for advanced crafting\n" +
           "• Multiply amount of crafted items\n" +
           "• Randomize starting durability of spawned items";
        override protected string SectionOverride
        => ModSections.SurvivalAndImmersion;
        override protected void LoadPreset(string presetName)
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
        static private Recipe _crystalPowderRecipe;
        static private Recipe FindCrystalPowderRecipe()
        {
            foreach (var recipeByUID in RecipeManager.Instance.m_recipes)
                foreach (var result in recipeByUID.Value.m_results)
                    if (result.m_itemID == CRYSTAL_POWDER_ID)
                        return recipeByUID.Value;
            return null;
        }
        static private List<Item> GetDestructibleIngredients(CraftingMenu craftingMenu)
        {
            List<Item> destructibleIngredients = new List<Item>();
            foreach (var ingredientSelector in craftingMenu.m_ingredientSelectors)
                if (ingredientSelector.AssignedIngredient.TryNonNull(out var ingredient))
                    foreach (var itemAmountByUID in ingredient.GetConsumedItems(false, out _))
                        if (ItemManager.Instance.GetItem(itemAmountByUID.Key).TryNonNull(out var item)
                        && item.MaxDurability > 0)
                            destructibleIngredients.TryAddUnique(item);
            return destructibleIngredients;
        }
        static private List<Item> GetDestructibleResults(CraftingMenu craftingMenu)
        {
            // Choose recipe index
            int selectorIndex = craftingMenu.m_lastRecipeIndex;
            int recipeIndex = selectorIndex >= 0 ? craftingMenu.m_complexeRecipes[selectorIndex].Key : craftingMenu.m_lastFreeRecipeIndex;
            List<Item> desctructibleResults = new List<Item>();

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
        static private void SetSingleIngredientCrafting(CraftingMenu __instance, bool enabled = true)
        {
            __instance.m_multipleIngrenentsBrackground.SetAlpha(enabled ? 0f : 1f);
            __instance.m_singleIngredientBackground.SetAlpha(enabled ? 1f : 0f);
            for (int i = 1; i < __instance.m_ingredientSelectors.Length; i++)
                __instance.m_ingredientSelectors[i].GOSetActive(!enabled);
        }
        static private bool HasLearnedRecipe(Character character, Recipe recipe)
        => character.Inventory.RecipeKnowledge.IsRecipeLearned(recipe.UID);
        static private void LearnRecipe(Character character, Recipe recipe)
        => character.Inventory.RecipeKnowledge.LearnRecipe(recipe);
        static private int GetModifiedResultsAmount(ItemReferenceQuantity result)
        => 1 + (result.Quantity - 1f).Mul(_extraResultsMultiplier / 100f).Round();

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006
        [HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.CraftingDone)), HarmonyPrefix]
        static bool CraftingMenu_CraftingDone_Pre(CraftingMenu __instance, ref List<Item> __state)
        {
            List<Item> ingredients = GetDestructibleIngredients(__instance);
            List<Item> results = GetDestructibleResults(__instance);
            #region quit
            if (!_preserveDurability || ingredients.IsNullOrEmpty() || ingredients.IsNullOrEmpty())
                return true;
            #endregion

            float averageRatio = 0;
            foreach (var item in ingredients)
                averageRatio += item.DurabilityRatio;
            averageRatio /= ingredients.Count;

            foreach (var item in results)
                if (item.Stats.TryNonNull(out var stats))
                    stats.StartingDurability = (stats.MaxDurability * averageRatio.Lerp(1f, _restoreMissingDurability / 100f)).Round();

            __state = results;
            return true;
        }

        [HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.CraftingDone)), HarmonyPostfix]
        static void CraftingMenu_CraftingDone_Post(CraftingMenu __instance, ref List<Item> __state)
        {
            #region quit
            if (__state == null)
                return;
            #endregion

            foreach (var item in __state)
                if (item.Stats.TryNonNull(out var stats))
                    stats.StartingDurability = -1;
        }

        [HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.OnRecipeSelected)), HarmonyPostfix]
        static void CraftingMenu_OnRecipeSelected_Post(CraftingMenu __instance)
        {
            #region quit
            if (!_limitedManualCrafting)
                return;
            #endregion

            SetSingleIngredientCrafting(__instance, __instance.m_lastRecipeIndex == -1);
        }

        [HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.IngredientSelectorHasChanged)), HarmonyPostfix]
        static void CraftingMenu_IngredientSelectorHasChanged_Post(CraftingMenu __instance, int _selectorIndex, int _itemID)
        {
            #region quit
            if (!_limitedManualCrafting || __instance.m_lastRecipeIndex >= 0)
                return;
            #endregion

            bool isMulti = _limitedManulCraftingExceptions.Value.HasFlag(CraftingExceptions.Lanterns) && _itemID.IsContainedIn(LANTERN_IDS)
                        || _limitedManulCraftingExceptions.Value.HasFlag(CraftingExceptions.Relics) && _itemID.IsContainedIn(RELIC_IDS)
                        || _limitedManulCraftingExceptions.Value.HasFlag(CraftingExceptions.HailfrostWeapons) && _itemID.IsContainedIn(HAILFROST_WEAPONS_IDS)
                        || _limitedManulCraftingExceptions.Value.HasFlag(CraftingExceptions.UniqueWeapons) && _itemID.IsContainedIn(UNIQUE_WEAPONS_IDS);
            SetSingleIngredientCrafting(__instance, !isMulti);
        }

        [HarmonyPatch(typeof(CraftingMenu), "Show", new Type[] { }), HarmonyPrefix]
        static bool CraftingMenu_Show_Post(CraftingMenu __instance)
        {
            #region quit
            if (!_limitedManualCrafting
            || __instance.m_craftingStation.StationType != Recipe.CraftingType.Alchemy
            || HasLearnedRecipe(__instance.LocalCharacter, _crystalPowderRecipe))
                return true;
            #endregion

            LearnRecipe(__instance.LocalCharacter, _crystalPowderRecipe);
            return true;
        }

        [HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.GenerateResult)), HarmonyPrefix]
        static bool CraftingMenu_GenerateResult_Pre(CraftingMenu __instance, ref int __state, ItemReferenceQuantity _result)
        {
            #region quit
            if (_extraResultsMultiplier == 100)
                return true;
            #endregion

            __state = _result.Quantity;
            _result.Quantity = GetModifiedResultsAmount(_result);
            return true;
        }

        [HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.GenerateResult)), HarmonyPostfix]
        static void CraftingMenu_GenerateResult_Post(CraftingMenu __instance, ref int __state, ItemReferenceQuantity _result)
        {
            #region quit
            if (_extraResultsMultiplier == 100)
                return;
            #endregion

            _result.Quantity = __state;
        }

        [HarmonyPatch(typeof(RecipeResultDisplay), nameof(RecipeResultDisplay.UpdateQuantityDisplay)), HarmonyPrefix]
        static bool RecipeResultDisplay_UpdateQuantityDisplay_Pre(RecipeResultDisplay __instance)
        {
            #region quit
            if (_extraResultsMultiplier == 100 || __instance.StackCount <= 1)
                return true;
            #endregion

            int amount = GetModifiedResultsAmount(__instance.m_result);
            __instance.m_lblQuantity.text = amount > 0 ? amount.ToString() : "";
            return false;
        }
    }
}