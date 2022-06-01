namespace Vheos.Mods.Outward;

using System.Runtime.CompilerServices;

public class Stashes : AMod
{
    #region const
    private static readonly Dictionary<AreaManager.AreaEnum, (string UID, Vector3[] Positions)> STASH_DATA_BY_CITY = new()
    {
        [AreaManager.AreaEnum.CierzoVillage] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-367.850f, -1488.250f, 596.277f),
                                                                                  new Vector3(-373.539f, -1488.250f, 583.187f) }),
        [AreaManager.AreaEnum.Berg] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-386.620f, -1493.132f, 773.86f),
                                                                         new Vector3(-372.410f, -1493.132f, 773.86f) }),
        [AreaManager.AreaEnum.Monsoon] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-371.628f, -1493.410f, 569.910f) }),
        [AreaManager.AreaEnum.Levant] = ("ZbPXNsPvlUeQVJRks3zBzg", new[] { new Vector3(-369.280f, -1502.535f, 592.850f),
                                                                           new Vector3(-380.530f, -1502.535f, 593.080f) }),
        [AreaManager.AreaEnum.Harmattan] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-178.672f, -1515.915f, 597.934f),
                                                                              new Vector3(-182.373f, -1515.915f, 606.291f),
                                                                              new Vector3(-383.484f, -1504.820f, 583.343f),
                                                                              new Vector3(-392.681f, -1504.820f, 586.551f)}),
        //[AreaManager.AreaEnum.NewSirocco] = ("???", new Vector3[0]),
    };
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

    // Settings
    private static ModSetting<bool> _innStashes;
    private static ModSetting<bool> _cityBoundStashes;
    private static ModSetting<bool> _playerSharedStash;
    private static ModSetting<bool> _craftFromStash;
    private static ModSetting<bool> _displayStashAmount;
    private static ModSetting<bool> _displayPricesInStash;
    private static ModSetting<bool> _stashesStartEmpty;
    protected override void Initialize()
    {
        _innStashes = CreateSetting(nameof(_innStashes), false);
        _cityBoundStashes = CreateSetting(nameof(_cityBoundStashes), false);
        _playerSharedStash = CreateSetting(nameof(_playerSharedStash), false);
        _craftFromStash = CreateSetting(nameof(_craftFromStash), false);
        _displayStashAmount = CreateSetting(nameof(_displayStashAmount), false);
        _displayPricesInStash = CreateSetting(nameof(_displayPricesInStash), false);
        _stashesStartEmpty = CreateSetting(nameof(_stashesStartEmpty), false);
    }
    protected override void SetFormatting()
    {
        _innStashes.Format("Inn stashes");
        _innStashes.Description = "Each inn room will have a player stash, linked with the one in player's house\n" +
                              "(exceptions: the first rooms in Monsoon's inn and Harmattan's Victorious Light inn)";
        _cityBoundStashes.Format("City-bound stashes");
        _cityBoundStashes.Description = "Reverts to pre-DE behaviour - each city will have its own separate stash";
        using (Indent)
        {
            _playerSharedStash.Format("Player-shared stash", _cityBoundStashes, false);
            _playerSharedStash.Description = "Reverts to pre-DE behaviour - all players will access the first player's stash";
        }
        _craftFromStash.Format("Craft with stashed items");
        _craftFromStash.Description = "When you're crafting in a city, you can use items from you stash";
        _displayStashAmount.Format("Display stashed item amounts");
        _displayStashAmount.Description = "Displays how many of each items you have stored in your stash\n" +
                                          "(shows in player/merchant inventory and crafting menu)";
        _displayPricesInStash.Format("Display prices in stash");
        _displayPricesInStash.Description = "Items in stash will have their sell prices displayed\n" +
                                            "(if prices vary among merchants, Soroborean Caravanner is taken as reference)";
        _stashesStartEmpty.Format("Stashes start empty");
        _stashesStartEmpty.Description = "Stashes won't have any items inside the first time you open them";

    }
    protected override string SectionOverride
    => ModSections.SurvivalAndImmersion;
    protected override string Description
    => "";
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _innStashes.Value = true;
                _cityBoundStashes.Value = true;
                _playerSharedStash.Value = true;
                _craftFromStash.Value = true;
                _displayStashAmount.Value = true;
                _displayPricesInStash.Value = true;
                _stashesStartEmpty.Value = true;
                break;
        }
    }

    // Utility
    private static ItemContainer _cachedStash;
    private static Character GetStashCharacter(Character character) => _playerSharedStash ? Players.GetLocal(0).Character : character;
    private static ItemContainer GetStash(Character character)
    {
        if (_cityBoundStashes)
        {
            if (_cachedStash == null
            && AreaManager.Instance.CurrentArea.TryNonNull(out var currentArea)
            && STASH_DATA_BY_CITY.TryGet((AreaManager.AreaEnum)currentArea.ID, out var data))
                _cachedStash = (TreasureChest)ItemManager.Instance.GetItem(data.UID);
            return _cachedStash;
        }

        return GetStashCharacter(character).Inventory.Stash;
    }
    private static Merchant _soroboreanCaravanner;
    private static Merchant SoroboreanCaravanner
    {
        get
        {
            if (_soroboreanCaravanner == null
            && AreaManager.Instance.CurrentArea.TryNonNull(out var currentArea)
            && SOROBOREAN_CARAVANNER_UIDS_BY_CITY.TryGet((AreaManager.AreaEnum)currentArea.ID, out var uid)
            && Merchant.m_sceneMerchants.ContainsKey(uid))
                _soroboreanCaravanner = Merchant.m_sceneMerchants[uid];
            return _soroboreanCaravanner;
        }
    }
    private static void TryDisplayStashAmount(ItemDisplay itemDisplay)
    {
        #region quit
        if (!_displayStashAmount
        || !GetStash(itemDisplay.LocalCharacter).TryNonNull(out var stash)
        || !itemDisplay.m_lblQuantity.TryNonNull(out var quantity)
        || !itemDisplay.RefItem.TryNonNull(out var item)
        || item.OwnerCharacter == null
        || item.ParentContainer.IsNot<MerchantPouch>() && itemDisplay.IsNot<RecipeResultDisplay>())
            return;
        #endregion

        int stashAmount = itemDisplay is CurrencyDisplay
            ? stash.ContainedSilver
            : stash.ItemStackCount(item.ItemID);

        if (stashAmount <= 0)
            return;

        if (itemDisplay.IsNot<RecipeResultDisplay>())
            quantity.text = itemDisplay.m_lastQuantity.ToString();
        else if (itemDisplay.m_dBarUses.TryNonNull(out var dotBar) && dotBar.GOActive())
            quantity.text = "1";

        int fontSize = (quantity.fontSize * 0.75f).Round();
        quantity.alignment = TextAnchor.UpperRight;
        quantity.lineSpacing = 0.75f;
        quantity.text += $"\n<color=#00FF00FF><size={fontSize}><b>+{stashAmount}</b></size></color>";
    }

    // Hooks
    // Reset static scene data
    [HarmonyPatch(typeof(NetworkLevelLoader), nameof(NetworkLevelLoader.UnPauseGameplay)), HarmonyPostfix]
    private static void NetworkLevelLoader_UnPauseGameplay_Post(NetworkLevelLoader __instance)
    {
        _cachedStash = null;
        _soroboreanCaravanner = null;
    }

    // City-bound stashes
    [HarmonyPatch(typeof(ItemContainer), nameof(ItemContainer.ShowContent)), HarmonyReversePatch]
    public static void ItemContainer_ShowContent(ItemContainer instance, Character _character)
    { }

    [HarmonyPatch(typeof(TreasureChest), nameof(TreasureChest.ShowContent)), HarmonyPrefix]
    static private bool TreasureChest_ShowContent_Pre(TreasureChest __instance, Character _character)
    {
        ItemContainer stash = GetStash(_character);
        if (__instance.SpecialType == ItemContainer.SpecialContainerTypes.Stash)
        {
            if (!_stashesStartEmpty)
                if (__instance.m_playerReceivedUIDs.TryAddUnique(GetStashCharacter(_character).UID))
                    for (int i = 0; i < __instance.m_drops.Count; i++)
                        if (__instance.m_drops[i])
                            __instance.m_drops[i].GenerateContents(stash);

            _character.CharacterUI.StashPanel.SetStash(stash);
        }

        ItemContainer_ShowContent(__instance, _character);
        return false;
    }

    // Display prices in stash
    [HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.UpdateValueDisplay)), HarmonyPrefix]
    private static bool ItemDisplay_UpdateValueDisplay_Pre(ItemDisplay __instance)
    {
        #region quit
        if (!_displayPricesInStash
        || !__instance.CharacterUI.TryNonNull(out var characterUI) || !characterUI.GetIsMenuDisplayed(CharacterUI.MenuScreens.Stash)
        || !__instance.RefItem.TryNonNull(out var item) || item.OwnerCharacter != null
        || !__instance.m_lblValue.TryNonNull(out var priceText)
        || SoroboreanCaravanner == null)
            return true;
        #endregion

        if (!__instance.m_valueHolder.activeSelf)
            __instance.m_valueHolder.SetActive(true);
        priceText.text = item.GetSellValue(characterUI.TargetCharacter, SoroboreanCaravanner).ToString();
        return false;
    }

    // Inn Stash
    [HarmonyPatch(typeof(NetworkLevelLoader), nameof(NetworkLevelLoader.UnPauseGameplay)), HarmonyPostfix]
    private static void NetworkLevelLoader_UnPauseGameplay_Post(NetworkLevelLoader __instance, string _identifier)
    {
        #region quit
        if (!_innStashes || _identifier != "Loading" || !AreaManager.Instance.CurrentArea.TryNonNull(out var currentArea)
        || !STASH_DATA_BY_CITY.ContainsKey((AreaManager.AreaEnum)currentArea.ID))
            return;
        #endregion

        // Cache
        (string UID, Vector3[] Positions) = STASH_DATA_BY_CITY[(AreaManager.AreaEnum)currentArea.ID];
        TreasureChest stash = (TreasureChest)ItemManager.Instance.GetItem(UID);
        stash.GOSetActive(true);

        int counter = 0;
        foreach (var position in Positions)
        {
            // Interactions
            Transform newInteractionHolder = GameObject.Instantiate(stash.InteractionHolder.transform);
            newInteractionHolder.name = $"InnStash{counter} - Interaction";
            newInteractionHolder.ResetLocalTransform();
            newInteractionHolder.position = position;
            InteractionActivator activator = newInteractionHolder.GetFirstComponentsInHierarchy<InteractionActivator>();
            activator.UID += $"_InnStash{counter}";
            InteractionOpenChest openChest = newInteractionHolder.GetFirstComponentsInHierarchy<InteractionOpenChest>();
            openChest.m_container = stash;
            openChest.m_item = stash;
            openChest.StartInit();

            // Highlight
            Transform newHighlightHolder = GameObject.Instantiate(stash.CurrentVisual.ItemHighlightTrans);
            newHighlightHolder.name = $"InnStash{counter} - Highlight";
            newHighlightHolder.ResetLocalTransform();
            newHighlightHolder.BecomeChildOf(newInteractionHolder);
            newHighlightHolder.GetFirstComponentsInHierarchy<InteractionHighlight>().enabled = true;
            counter++;
        }
    }

    [HarmonyPatch(typeof(InteractionOpenChest), nameof(InteractionOpenChest.OnActivate)), HarmonyPrefix]
    private static bool InteractionOpenChest_OnActivate_Pre(InteractionOpenChest __instance)
    {
        #region quit
        if (!_innStashes || !__instance.m_chest.TryNonNull(out var chest))
            return true;
        #endregion

        chest.GOSetActive(true);
        return true;
    }

    // Craft from stash
    [HarmonyPatch(typeof(CharacterInventory), nameof(CharacterInventory.InventoryIngredients),
        new[] { typeof(Tag), typeof(DictionaryExt<int, CompatibleIngredient>) },
        new[] { ArgumentType.Normal, ArgumentType.Ref }),
        HarmonyPostfix]
    private static void CharacterInventory_InventoryIngredients_Post(CharacterInventory __instance, Tag _craftingStationTag, ref DictionaryExt<int, CompatibleIngredient> _sortedIngredient)
    {
        #region quit
        if (!_craftFromStash
        || !GetStash(__instance.m_character).TryNonNull(out var stash))
            return;
        #endregion

        __instance.InventoryIngredients(_craftingStationTag, ref _sortedIngredient, stash.GetContainedItems());
    }

    // Display stash amount
    [HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.UpdateQuantityDisplay)), HarmonyPostfix]
    private static void ItemDisplay_UpdateQuantityDisplay_Post(ItemDisplay __instance)
    => TryDisplayStashAmount(__instance);

    [HarmonyPatch(typeof(CurrencyDisplay), nameof(CurrencyDisplay.UpdateQuantityDisplay)), HarmonyPostfix]
    private static void CurrencyDisplay_UpdateQuantityDisplay_Post(CurrencyDisplay __instance)
    => TryDisplayStashAmount(__instance);

    [HarmonyPatch(typeof(RecipeResultDisplay), nameof(RecipeResultDisplay.UpdateQuantityDisplay)), HarmonyPostfix]
    private static void RecipeResultDisplay_UpdateQuantityDisplay_Post(RecipeResultDisplay __instance)
    => TryDisplayStashAmount(__instance);
}
