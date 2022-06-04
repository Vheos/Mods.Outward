/* TO DO:
 * - include side quests
 * - include unique items and enemies
 */

namespace Vheos.Mods.Outward;

public class Resets : AMod
{
    #region const
    private static readonly AreaManager.AreaEnum[] ALL_CITIES = new[]
    {
        AreaManager.AreaEnum.CierzoVillage,
        AreaManager.AreaEnum.Berg,
        AreaManager.AreaEnum.Monsoon,
        AreaManager.AreaEnum.Levant,
        AreaManager.AreaEnum.Harmattan,
        AreaManager.AreaEnum.NewSirocco
    };
    private const float AREAS_RESET_HOURS = 168f;   // Area.m_resetTime
    private const float MERCHANTS_RESET_HOURS = 72f;   // MerchantPouch.InventoryRefreshRate
    private const float SIDEQUESTS_RESET_HOURS = int.MaxValue;   // ???
    private const float PICKUP_RESET_HOURS = 72f;   // Gatherable.m_drops[].m_mainDropTables[].m_itemDrops[].ChanceRegenDelay
    private const float FISHING_RESET_HOURS = 24f;   // (ditto)
    private const int FISHING_HARPOON_ID = 2130130;   //
    private const int MINING_PICK_ID = 2120050;   //
    private const float TIME_UNIT = 24f;   // Day = TIME_UNIT
    private static readonly Dictionary<WeaponSet, int[]> MELEE_WEAPON_IDS_BY_SET = new()
    {
        [WeaponSet.Junk] = new[]
        {
            2000060,
            2100210,
            2010050,
            2110040,
            2020130,
            2120050,
            2130040,
            2130130,
            2130010,
            2130030,
            2160020,
        },
        [WeaponSet.Iron] = new[]
        {
            2000010,
            2100080,
            2010000,
            2110030,
            2020010,
            2120080,
            2130110,
            2140000,
            2160000,
        },
        [WeaponSet.Fang] = new[]
        {
            2000050,
            2100020,
            2010040,
            2110010,
            2020050,
            2120020,
            2130020,
            2140020,
            2160030,
        },
        [WeaponSet.Gold] = new[]
        {
            2000061,
            2100211,
            2010051,
            2110041,
            2020131,
            2120051,
            2130041,
            2130131,
            2130011,
            2130031,
            2160021,
        },
        [WeaponSet.Savage] = new[]
        {
            2000051,
            2100021,
            2010041,
            2110011,
            2020051,
            2120021,
            2130022,
            2140021,
            2160031,
        },
    };
    private static readonly Dictionary<WeaponSet, int[]> RANGED_WEAPON_IDS_BY_SET = new()
    {
        [WeaponSet.Junk] = new[] { 2200000 },
        [WeaponSet.Iron] = new[] { 2200000 },
        [WeaponSet.Fang] = new[] { 2200000 },
        [WeaponSet.Gold] = new[] { 2200001 },
        [WeaponSet.Savage] = new[] { 2200001 },
    };
    #endregion
    #region enum
    private enum ResetMode
    {
        Always = 1,
        Timer = 2,
        Never = 3,
    }
    [Flags]
    private enum AreasResetLayers
    {
        None = 0,
        ItemsAndContainers = 1 << 1,
        Gatherables = 1 << 2,
        Enemies = 1 << 3,
        Souls = 1 << 4,
        Switches = 1 << 5,
        AmbushEvents = 1 << 6,
        DeathEvents = 1 << 7,
        Cities = 1 << 8,
    }
    private enum WeaponSet
    {
        Disabled = 0,
        Junk = 1,
        Iron = 2,
        Fang = 3,
        Gold = 4,
        Savage = 5,
    }
    #endregion

    // Config
    private static ModSetting<bool> _areasToggle, _gatherablesToggle, _merchantsToggle;
    private static ModSetting<ResetMode> _areasMode, _gatheringMode, _fishingMode, _miningMode, _merchantsMode;
    private static ModSetting<int> _areasTimer, _gatheringTimer, _fishingTimer, _miningTimer, _merchantsTimer;
    private static ModSetting<int> _areasTimerSinceReset;
    private static ModSetting<AreasResetLayers> _areasResetLayers;
    private static ModSetting<WeaponSet> _fixUnarmedBandits;
    private static ModSetting<int> _fixUnarmedBanditsDurabilityRatio;
    protected override void Initialize()
    {
        _areasToggle = CreateSetting(nameof(_areasToggle), false);
        _areasMode = CreateSetting(nameof(_areasMode), ResetMode.Timer);
        _areasTimer = CreateSetting(nameof(_areasTimer), AREAS_RESET_HOURS.Div(TIME_UNIT).Round(), IntRange(0, 100));
        _areasTimerSinceReset = CreateSetting(nameof(_areasTimerSinceReset), AREAS_RESET_HOURS.Div(TIME_UNIT).Round(), IntRange(0, 100));
        _areasResetLayers = CreateSetting(nameof(_areasResetLayers), (AreasResetLayers)((1 << 8) - 1));
        _fixUnarmedBandits = CreateSetting(nameof(_fixUnarmedBandits), WeaponSet.Disabled);
        _fixUnarmedBanditsDurabilityRatio = CreateSetting(nameof(_fixUnarmedBanditsDurabilityRatio), 100, IntRange(0, 100));

        _gatherablesToggle = CreateSetting(nameof(_gatherablesToggle), false);
        _gatheringMode = CreateSetting(nameof(_gatheringMode), ResetMode.Timer);
        _gatheringTimer = CreateSetting(nameof(_gatheringTimer), PICKUP_RESET_HOURS.Div(TIME_UNIT).Round(), IntRange(1, 100));
        _miningMode = CreateSetting(nameof(_miningMode), ResetMode.Timer);
        _miningTimer = CreateSetting(nameof(_miningTimer), PICKUP_RESET_HOURS.Div(TIME_UNIT).Round(), IntRange(1, 100));
        _fishingMode = CreateSetting(nameof(_fishingMode), ResetMode.Timer);
        _fishingTimer = CreateSetting(nameof(_fishingTimer), FISHING_RESET_HOURS.Div(TIME_UNIT).Round(), IntRange(1, 100));

        _merchantsToggle = CreateSetting(nameof(_merchantsToggle), false);
        _merchantsMode = CreateSetting(nameof(_merchantsMode), ResetMode.Timer);
        _merchantsTimer = CreateSetting(nameof(_merchantsTimer), MERCHANTS_RESET_HOURS.Div(TIME_UNIT).Round(), IntRange(1, 100));

        _areasTimer.AddEvent(() => _areasTimerSinceReset.Value = _areasTimerSinceReset.Value.ClampMin(_areasTimer));
        _areasTimerSinceReset.AddEvent(() => _areasTimer.Value = _areasTimer.Value.ClampMax(_areasTimerSinceReset));
    }
    protected override void SetFormatting()
    {
        _areasToggle.Format("Areas");
        _areasToggle.Description = "Change areas (scenes) reset settings";
        using (Indent)
        {
            _areasMode.Format("Reset mode", _areasToggle);
            _areasTimer.Format("Days since last visit", _areasMode, ResetMode.Timer);
            _areasTimerSinceReset.Format("Days since last reset", _areasMode, ResetMode.Timer);
            _areasResetLayers.Format("Layers to reset", _areasMode, ResetMode.Never, false);
            _areasResetLayers.Description = "Cities  -  makes cities reset just like any other area";
            _fixUnarmedBandits.Format("Fix unarmed bandits", _areasResetLayers, AreasResetLayers.Enemies);
            _fixUnarmedBandits.IsAdvanced = true;
            using (Indent)
            {
                _fixUnarmedBanditsDurabilityRatio.Format("New weapons' durability", _fixUnarmedBandits, WeaponSet.Disabled, false);
                _fixUnarmedBanditsDurabilityRatio.IsAdvanced = true;
            }
        }

        _gatherablesToggle.Format("Gatherables");
        _gatherablesToggle.Description = "Change gatherables respawn settings";
        using (Indent)
        {
            _gatheringMode.Format("Gathering spots", _gatherablesToggle);
            _gatheringMode.Description = "Gatherables that don't require any tool";
            _gatheringTimer.Format("", _gatheringMode, ResetMode.Timer);
            _miningMode.Format("Mining spots", _gatherablesToggle);
            _miningMode.Description = "Gatherables that require Mining Pick";
            _miningTimer.Format("", _miningMode, ResetMode.Timer);
            _fishingMode.Format("Fishing spots", _gatherablesToggle);
            _fishingMode.Description = "Gatherables that require Fishing Harpoon";
            _fishingTimer.Format("", _fishingMode, ResetMode.Timer);
        }

        _merchantsToggle.Format("Merchants");
        _merchantsToggle.Description = "Change merchant restock settings";
        using (Indent)
        {
            _merchantsMode.Format("", _merchantsToggle);
            _merchantsTimer.Format("", _merchantsMode, ResetMode.Timer);
        }
    }
    protected override string Description
    => "• Area resets (with fine-tuning)\n" +
       "• Gatherable respawns (for each type)\n" +
       "• Merchant restocks";
    protected override string SectionOverride
    => ModSections.SurvivalAndImmersion;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _areasToggle.Value = true;
                {
                    _areasMode.Value = ResetMode.Timer;
                    _areasTimer.Value = 4;
                    _areasTimerSinceReset.Value = 14;
                    _areasResetLayers.Value = AreasResetLayers.Enemies;
                    _fixUnarmedBandits.Value = WeaponSet.Fang;
                    _fixUnarmedBanditsDurabilityRatio.Value = 67;
                }
                _gatherablesToggle.Value = true;
                {
                    _gatheringMode.Value = ResetMode.Timer;
                    _gatheringTimer.Value = 7;
                    _miningMode.Value = ResetMode.Timer;
                    _miningTimer.Value = 11;
                    _fishingMode.Value = ResetMode.Timer;
                    _fishingTimer.Value = 4;
                }
                _merchantsToggle.Value = true;
                _merchantsMode.Value = ResetMode.Never;
                break;
        }
    }

    // Utility
    private static void RemovePouchItemsFromSaveData(List<BasicSaveData> saveDataList)
    {
        List<BasicSaveData> saveDatasToRemove = new();
        foreach (var saveData in saveDataList)
            if (saveData.SyncData.ContainsSubstring("Pouch_") && !saveData.SyncData.ContainsSubstring("MerchantPouch_"))
                saveDatasToRemove.Add(saveData);

        saveDataList.Remove(saveDatasToRemove);
    }
    private static bool HasAnyMeleeWeapon(Character character)
    => character.CurrentWeapon is MeleeWeapon || character.Inventory.Pouch.GetContainedItems().Any(item => item is MeleeWeapon);
    private static bool HasAnyRangedWeapon(Character character)
    => character.CurrentWeapon is ProjectileWeapon || character.Inventory.Pouch.GetContainedItems().Any(item => item is ProjectileWeapon);
    private static void GenerateAndEquipPouchItem(Character character, int itemID, float durabilityRatio = 1f)
    {
        Item newItem = ItemManager.Instance.GenerateItem(itemID);
        newItem.ForceStartInit();
        newItem.ChangeParent(character.Inventory.Pouch.transform);
        newItem.ForceUpdateParentChange();
        newItem.SetDurabilityRatio(durabilityRatio / 100f);
        newItem.TryQuickSlotUse();
    }

    // Hooks
    // Areas
    [HarmonyPatch(typeof(EnvironmentSave), nameof(EnvironmentSave.ApplyData)), HarmonyPrefix]
    private static bool EnvironmentSave_ApplyData_Pre(EnvironmentSave __instance)
    {
        #region quit
        if (!_areasToggle)
            return true;
        #endregion

        // Initialize game time
        if (InternalUtility.GameTime < (float)__instance.GameTime)
            InternalUtility.GameTime = (float)__instance.GameTime;

        // Persistent areas

        AreaManager.AreaEnum areaEnum = (AreaManager.AreaEnum)AreaManager.Instance.GetAreaFromSceneName(__instance.AreaName).ID;
        bool resetArea = _areasResetLayers.Value.HasFlag(AreasResetLayers.Cities) || areaEnum.IsNotContainedIn(ALL_CITIES);

        // Area modes
        float sinceLastVisit = InternalUtility.GameTime - (float)__instance.GameTime;
        float sinceLastReset = InternalUtility.GameTime - __instance.SaveCreationGameTime;
        resetArea &= _areasMode == ResetMode.Always
                  || _areasMode == ResetMode.Timer
                                && sinceLastVisit >= _areasTimer * TIME_UNIT
                                && sinceLastReset >= _areasTimerSinceReset * TIME_UNIT;
        // Execute
        if (resetArea)
            __instance.SaveCreationGameTime = InternalUtility.GameTime.RoundDown();

        if (!resetArea || !_areasResetLayers.Value.HasFlag(AreasResetLayers.Enemies))
            CharacterManager.Instance.LoadAiCharactersFromSave(__instance.CharList.ToArray());
        else
            RemovePouchItemsFromSaveData(__instance.ItemList);

        if (!resetArea || !_areasResetLayers.Value.HasFlag(AreasResetLayers.ItemsAndContainers))
            ItemManager.Instance.LoadItems(__instance.ItemList, true);
        if (!resetArea || !_areasResetLayers.Value.HasFlag(AreasResetLayers.Switches))
            SceneInteractionManager.Instance.LoadInteractableStates(__instance.InteractionActivatorList);
        if (!resetArea || !_areasResetLayers.Value.HasFlag(AreasResetLayers.Gatherables))
            SceneInteractionManager.Instance.LoadDropTableStates(__instance.DropTablesList);
        if (!resetArea || !_areasResetLayers.Value.HasFlag(AreasResetLayers.AmbushEvents))
            CampingEventManager.Instance.LoadEventTableData(__instance.CampingEventSaveData);
        if (!resetArea || !_areasResetLayers.Value.HasFlag(AreasResetLayers.DeathEvents))
            DefeatScenariosManager.Instance.LoadSaveData(__instance.DefeatScenarioSaveData);
        if (!resetArea || !_areasResetLayers.Value.HasFlag(AreasResetLayers.Souls))
            EnvironmentConditions.Instance.LoadSoulSpots(__instance.UsedSoulSpots);

        return false;
    }

    [HarmonyPatch(typeof(Item), nameof(Item.IsInPermanentZone), MethodType.Getter), HarmonyPostfix]
    private static void ItemContainer_ContainerCapacity_Post(Item __instance, ref bool __result)
    => __result &= !_areasToggle;

    // Gatherables
    [HarmonyPatch(typeof(Gatherable), nameof(Gatherable.StartInit)), HarmonyPostfix]
    private static void Gatherable_StartInit_Post(Gatherable __instance)
    {
        #region quit
        if (!_gatherablesToggle)
            return;
        #endregion

        // Choose appropriate mode and timer
        ModSetting<ResetMode> mode = _gatheringMode;
        ModSetting<int> timer = _gatheringTimer;
        if (__instance.RequiredItem != null)
            if (__instance.RequiredItem.ItemID == MINING_PICK_ID)
            {
                mode = _miningMode;
                timer = _miningTimer;
            }
            else if (__instance.RequiredItem.ItemID == FISHING_HARPOON_ID)
            {
                mode = _fishingMode;
                timer = _fishingTimer;
            }

        // Calculat reset time based on mode
        float resetTime = 1 / 300f;   // 500ms in realtime
        if (mode == ResetMode.Never)
            resetTime = float.PositiveInfinity;
        else if (mode == ResetMode.Timer)
            resetTime = timer * TIME_UNIT;

        // Execute
        foreach (var dropable in __instance.m_drops)
            foreach (var dropTable in dropable.m_mainDropTables)
            {
                SimpleRandomChance dropAmount = dropTable.m_dropAmount;
                if (dropAmount.ChanceRegenDelay > 1)
                    dropAmount.m_chanceRegenDelay = resetTime;

                foreach (var itemDropChance in dropTable.m_itemDrops)
                    if (itemDropChance.ChanceRegenDelay > 1)
                        itemDropChance.ChanceRegenDelay = resetTime;
            }
    }

    // Merchants
    [HarmonyPatch(typeof(MerchantPouch), nameof(MerchantPouch.RefreshInventory)), HarmonyPrefix]
    private static void MerchantPouch_RefreshInventory_Pre(MerchantPouch __instance, ref double ___m_nextRefreshTime)
    {
        #region quit
        if (!_merchantsToggle)
            return;
        #endregion

        if (_merchantsMode == ResetMode.Always)
            ___m_nextRefreshTime = 0d;
        else if (_merchantsMode == ResetMode.Never && !__instance.IsEmpty)
            ___m_nextRefreshTime = double.PositiveInfinity;
        else if (_merchantsMode == ResetMode.Timer)
        {
            __instance.InventoryRefreshRate = _merchantsTimer * TIME_UNIT;
            if (___m_nextRefreshTime == double.PositiveInfinity)
                ___m_nextRefreshTime = InternalUtility.GameTime + __instance.InventoryRefreshRate;
        }
    }

    // Bandits fix
    [HarmonyPatch(typeof(AISCombat), nameof(AISCombat.UpdateMed)), HarmonyPrefix]
    private static void AISCombat_UpdateMed_Pre(AISCombat __instance)
    {
        Character character = __instance.m_character;
        #region quit
        if (_fixUnarmedBandits == WeaponSet.Disabled
        || character.Faction != Character.Factions.Bandits
        || character.m_animatorIsHumanDefault == false
        || !character.name.ToLowerInvariant().Contains("bandit"))
            return;
        #endregion

        if (__instance is AISCombatMelee && !HasAnyMeleeWeapon(character))
            GenerateAndEquipPouchItem(character, MELEE_WEAPON_IDS_BY_SET[_fixUnarmedBandits].Random(), _fixUnarmedBanditsDurabilityRatio);
        if (__instance is AISCombatRanged && !HasAnyRangedWeapon(character))
            GenerateAndEquipPouchItem(character, RANGED_WEAPON_IDS_BY_SET[_fixUnarmedBandits].Random(), _fixUnarmedBanditsDurabilityRatio);
    }
}
