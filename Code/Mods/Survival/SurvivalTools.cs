namespace Vheos.Mods.Outward;

public class SurvivalTools : AMod
{
    #region const
    private const float BED_DISTANCE_BETWEEN_PLAYERS = 0.4f;
    private static readonly int[] TORCH_IDS = new[]
    {
        "Makeshift Torch".ToItemID(),
        "Ice-Flame Torch".ToItemID(),
    };
    private const string FLINT_AND_STEEL_BREAK_NOTIFICATION = "Flint and Steel broke!";
    private const int PRIMITIVE_SATCHEL_CAPACITY = 25;
    private const int TRADER_BACKPACK = 100;
    #endregion

    // Settings
    private static ModSetting<bool> _moreGatheringTools;
    private static ModSetting<Vector2> _gatheringDurabilityCost;
    private static ModSetting<int> _chanceToBreakFlintAndSteel;
    private static ModSetting<int> _waterskinCapacity;
    private static ModSetting<Vector2> _remapBackpackCapacities;
    private static ModSetting<float> _torchesTemperatureRadius;
    private static ModSetting<bool> _torchesDecayOnGround;
    private static ModSetting<int> _lightsRange;
    private static ModSetting<bool> _twoPersonBeds;
    protected override void Initialize()
    {
        _moreGatheringTools = CreateSetting(nameof(_moreGatheringTools), false);
        _gatheringDurabilityCost = CreateSetting(nameof(_gatheringDurabilityCost), new Vector2(0f, 5f));
        _chanceToBreakFlintAndSteel = CreateSetting(nameof(_chanceToBreakFlintAndSteel), 0, IntRange(0, 100));
        _waterskinCapacity = CreateSetting(nameof(_waterskinCapacity), 5, IntRange(1, 18));
        _remapBackpackCapacities = CreateSetting(nameof(_remapBackpackCapacities), new Vector2(PRIMITIVE_SATCHEL_CAPACITY, TRADER_BACKPACK));
        _torchesTemperatureRadius = CreateSetting(nameof(_torchesTemperatureRadius), 1f, FloatRange(0, 10));
        _torchesDecayOnGround = CreateSetting(nameof(_torchesDecayOnGround), false);
        _lightsRange = CreateSetting(nameof(_lightsRange), 100, IntRange(50, 200));
        _twoPersonBeds = CreateSetting(nameof(_twoPersonBeds), false);
    }
    protected override void SetFormatting()
    {
        _moreGatheringTools.Format("More gathering tools");
        _moreGatheringTools.Description = "Any Spear can fish and any 2-Handed Mace can mine\n" +
                                          "The tool is searched for in your bag, then pouch, then equipment\n" +
                                          "If there is more than 1 valid tool, the cheapest one is chosen first";
        _gatheringDurabilityCost.Format("Gathering tools durability cost");
        _gatheringDurabilityCost.Description = "X   -   flat amount\n" +
                                               "Y   -   percent of max";
        _chanceToBreakFlintAndSteel.Format("Chance to break \"Flint and Steel\"");
        _chanceToBreakFlintAndSteel.Description = "Each time you use Flint and Steel, there's a X% chance it will break";
        _waterskinCapacity.Format("Waterskin capacity");
        _waterskinCapacity.Description = "Have one big waterskin instead of a few small ones so you don't have to swap quickslots";
        _remapBackpackCapacities.Format("Remap backpack capacities");
        _remapBackpackCapacities.Description = "X   -   Primitive Satchel's capacity\n" +
                                               "Y   -   Trader Backpack's capacity\n" +
                                               "(all other backpacks will have their capacities scaled accordingly)";
        _torchesTemperatureRadius.Format("Torches temperature radius");
        _torchesTemperatureRadius.Description = "Increase to share a torch's temperature with your friend eaiser";
        _torchesDecayOnGround.Format("Torches burn out on ground");
        _torchesDecayOnGround.Description = "Normally, torches don't burn out when on ground, even if they are lit and provide temperature";
        _lightsRange.Format("Lights range");
        _lightsRange.Description = "Multiplies torches' and lanterns' lighting range (in %)";
        _twoPersonBeds.Format("Two-person beds");
        _twoPersonBeds.Description = "All beds, tents and bedrolls will allow for 2 users at the same time";

    }
    protected override string Description
    => "• Allow gathering with more tools\n" +
       "• Control gathering tools durability cost\n" +
       "• Make Flint and Steel break randomly\n" +
       "• Change Waterskin capacity\n" +
       "• Change backpack capacities";
    protected override string SectionOverride
    => ModSections.SurvivalAndImmersion;
    protected override string ModName
    => "Tools";
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _remapBackpackCapacities.Value = new Vector2(20, 60);
                _waterskinCapacity.Value = 9;
                _chanceToBreakFlintAndSteel.Value = 25;
                _moreGatheringTools.Value = true;
                _gatheringDurabilityCost.Value = new Vector2(15, 3);
                _torchesTemperatureRadius.Value = 7.5f;
                _torchesDecayOnGround.Value = true;
                _lightsRange.Value = 133;
                _twoPersonBeds.Value = true;
                break;
        }
    }

    // Hooks
    // More gathering tools
    [HarmonyPrefix, HarmonyPatch(typeof(CharacterInventory), nameof(CharacterInventory.GetCompatibleGatherableTool))]
    private static bool GatherableInteraction_GetCompatibleGatherableTool_Pre(CharacterInventory __instance, ref Item __result, int _sourceToolID)
    {
        #region quit
        if (!_moreGatheringTools
        || _sourceToolID != "Mining Pick".ToItemID() && _sourceToolID != "Fishing Harpoon".ToItemID())
            return true;
        #endregion

        // Cache
        Weapon.WeaponType requiredType = _sourceToolID == "Fishing Harpoon".ToItemID() ? Weapon.WeaponType.Spear_2H : Weapon.WeaponType.Mace_2H;
        List<Item> potentialTools = new();

        // Search bag & pouch
        List<ItemContainer> containers = new();
        if (__instance.EquippedBag.TryNonNull(out var bag))
            containers.Add(bag.m_container);
        if (__instance.Pouch.TryNonNull(out var pouch))
            containers.Add(pouch);

        foreach (var container in containers)
            if (potentialTools.IsNullOrEmpty())
                foreach (var item in container.GetContainedItems())
                    if (item.TryAs(out Weapon weapon) && weapon.Type == requiredType && weapon.DurabilityRatio > 0)
                        potentialTools.Add(item);

        // Search equipment
        if (potentialTools.IsNullOrEmpty()
        && __instance.Equipment.m_equipmentSlots[(int)EquipmentSlot.EquipmentSlotIDs.RightHand].EquippedItem.TryAs(out Weapon mainWeapon)
        && mainWeapon.Type == requiredType && mainWeapon.DurabilityRatio > 0)
            potentialTools.Add(mainWeapon);

        // Choose tool
        __result = potentialTools.OrderBy(tool => tool.RawCurrentValue).FirstOrDefault();

        return false;
    }

    // Gathering durability cost
    [HarmonyPostfix, HarmonyPatch(typeof(GatherableInteraction), nameof(GatherableInteraction.CharSpellTakeItem))]
    private static void GatherableInteraction_CharSpellTakeItem_Post(GatherableInteraction __instance, Character _character)
    {
        #region quit
        if (!__instance.m_pendingAnims.TryGetValue(_character.UID, out var gatherInstance)
        || !gatherInstance.ValidItem.TryNonNull(out var item))
            return;
        #endregion

        item.ReduceDurability(-0.05f * item.MaxDurability);
        item.ReduceDurability(_gatheringDurabilityCost.Value.x + _gatheringDurabilityCost.Value.y / 100f * item.MaxDurability);
        return;
    }

    // Chance to break Flint and Steel
    [HarmonyPostfix, HarmonyPatch(typeof(Item), nameof(Item.OnUse))]
    private static void Item_OnUse_Post(Item __instance)
    {
        #region quit
        if (__instance.ItemID != "Flint and Steel".ToItemID()
        || UnityEngine.Random.value >= _chanceToBreakFlintAndSteel / 100f)
            return;
        #endregion

        __instance.RemoveQuantity(1);
        __instance.m_ownerCharacter.CharacterUI.ShowInfoNotification(FLINT_AND_STEEL_BREAK_NOTIFICATION);
    }

    // Waterskin capacity
    [HarmonyPrefix, HarmonyPatch(typeof(WaterContainer), nameof(WaterContainer.RefreshDisplay))]
    private static void WaterContainer_RefreshDisplay_Pre(WaterContainer __instance)
    => __instance.m_stackable.m_maxStackAmount = _waterskinCapacity;


    [HarmonyPostfix, HarmonyPatch(typeof(Item), nameof(Item.OnAwake))]
    private static void Item_Awake_Post(Item __instance)
    {
        #region quit
        if (__instance.IsNot<WaterContainer>())
            return;
        #endregion

        __instance.m_stackable.m_maxStackAmount = _waterskinCapacity;
    }

    // Remap backpack capacities
    [HarmonyPostfix, HarmonyPatch(typeof(ItemContainer), nameof(ItemContainer.ContainerCapacity), MethodType.Getter)]
    private static void ItemContainer_ContainerCapacity_Post(ItemContainer __instance, ref float __result)
    {
        if (__instance.RefBag == null || __instance.m_baseContainerCapacity <= 0)
            return;

        __result = __result.Map(PRIMITIVE_SATCHEL_CAPACITY, TRADER_BACKPACK,
                                _remapBackpackCapacities.Value.x, _remapBackpackCapacities.Value.y).Round();
    }

    // Torches temperature radius
    [HarmonyPostfix, HarmonyPatch(typeof(TemperatureSource), nameof(TemperatureSource.Start))]
    private static void TemperatureSource_Start_Post(TemperatureSource __instance)
    {
        #region quit
        if (!__instance.m_item.TryNonNull(out var item) || item.ItemID.IsNotContainedIn(TORCH_IDS))
            return;
        #endregion

        __instance.DistanceRanges = new List<Vector2> { new Vector2(0, _torchesTemperatureRadius) };
        __instance.m_maxDistance = _torchesTemperatureRadius;
        if (TemperatureSource.BiggestRangeInScene < _torchesTemperatureRadius)
            TemperatureSource.BiggestRangeInScene = _torchesTemperatureRadius;
        __instance.m_temperatureCollider.As<SphereCollider>().radius = _torchesTemperatureRadius;
        item.PerishScript.DontPerishInWorld = !_torchesDecayOnGround;
        return;
    }

    // Lights intensity
    [HarmonyPostfix, HarmonyPatch(typeof(ItemLanternVisual), nameof(ItemLanternVisual.Awake))]
    private static void ItemLanternVisual_Awake_Post(ItemLanternVisual __instance)
    {
        float modifier = _lightsRange / 100f;
        __instance.LanternLight.range *= modifier;
    }

    // Tents capacity
    [HarmonyPrefix, HarmonyPatch(typeof(Sleepable), nameof(Sleepable.RequestSleepableRoom))]
    private static void Sleepable_RequestSleepableRoom_Pre(Sleepable __instance)
    {
        #region quit
        if (!_twoPersonBeds || Global.Lobby.PlayersInLobbyCount <= 1)
            return;
        #endregion

        __instance.Capacity = 2;
        __instance.CharAnimOffset.SetX(BED_DISTANCE_BETWEEN_PLAYERS / 2f * (__instance.m_occupants.Count == 0 ? -1f : +1f));
    }
}
