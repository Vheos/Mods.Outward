using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;



/* TO DO:
 * - fix gatherables stuck in NEVER reset mode
 * - include side quests
 * - include unique items and enemies
 */
namespace ModPack
{
    public class Resets : AMod
    {
        #region constants
        private const float AREAS_RESET_HOURS = 168f;   // Area.m_resetTime
        private const float MERCHANTS_RESET_HOURS = 72f;   // MerchantPouch.InventoryRefreshRate
        private const float SIDEQUESTS_RESET_HOURS = int.MaxValue;   // ???
        private const float PICKUP_RESET_HOURS = 72f;   // Gatherable.m_drops[].m_mainDropTables[].m_itemDrops[].ChanceRegenDelay
        private const float FISHING_RESET_HOURS = 24f;   // (ditto)
        private const int FISHING_HARPOON_ID = 2130130;   //
        private const int MINING_PICK_ID = 2120050;   //
        private const float TIME_UNIT = 24f;   // Day = TIME_UNIT
        #endregion
        #region enums
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
        #endregion

        // Config
        static private ModSetting<bool> _areasToggle, _gatherablesToggle, _merchantsToggle;
        static private ModSetting<ResetMode> _areasMode, _gatheringMode, _fishingMode, _miningMode, _merchantsMode;
        static private ModSetting<int> _areasTimer, _gatheringTimer, _fishingTimer, _miningTimer, _merchantsTimer;
        static private ModSetting<int> _areasTimerSinceReset;
        static private ModSetting<AreasResetLayers> _areasResetLayers;
        override protected void Initialize()
        {
            _areasToggle = CreateSetting(nameof(_areasToggle), false);
            _areasMode = CreateSetting(nameof(_areasMode), ResetMode.Timer);
            _areasTimer = CreateSetting(nameof(_areasTimer), AREAS_RESET_HOURS.Div(TIME_UNIT).Round(), IntRange(0, 100));
            _areasTimerSinceReset = CreateSetting(nameof(_areasTimerSinceReset), AREAS_RESET_HOURS.Div(TIME_UNIT).Round(), IntRange(0, 100));
            _areasResetLayers = CreateSetting(nameof(_areasResetLayers), (AreasResetLayers)((1 << 8) - 1));

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
        override protected void SetFormatting()
        {
            _areasToggle.Format("Areas");
            _areasToggle.Description = "Change areas (scenes) reset settings";
            Indent++;
            {
                _areasMode.Format("Reset mode", _areasToggle);
                _areasTimer.Format("Days since last visit", _areasMode, ResetMode.Timer);
                _areasTimerSinceReset.Format("Days since last reset", _areasMode, ResetMode.Timer);
                _areasResetLayers.Format("Layers to reset", _areasMode, ResetMode.Never, false);
                _areasResetLayers.Description = "Cities  -  makes cities reset just like any other area";
                Indent--;
            }

            _gatherablesToggle.Format("Gatherables");
            _gatherablesToggle.Description = "Change gatherables respawn settings";
            Indent++;
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
                Indent--;
            }

            _merchantsToggle.Format("Merchants");
            _merchantsToggle.Description = "Change merchant restock settings";
            Indent++;
            {
                _merchantsMode.Format("", _merchantsToggle);
                _merchantsTimer.Format("", _merchantsMode, ResetMode.Timer);
                Indent--;
            }
        }
        override protected string Description
        => "• Area resets (with fine-tuning)\n" +
           "• Gatherable respawns (for each type)\n" +
           "• Merchant restocks";

        // Areas
        [HarmonyPatch(typeof(EnvironmentSave), "ApplyData"), HarmonyPrefix]
        static bool EnvironmentSave_ApplyData_Pre(ref EnvironmentSave __instance)
        {
            #region quit
            if (!_areasToggle)
                return true;
            #endregion

            // Initialize game time
            if (GameTime < (float)__instance.GameTime)
                GameTime = (float)__instance.GameTime;

            // Persistent areas
            AreaManager.AreaEnum areaEnum = (AreaManager.AreaEnum)AreaManager.Instance.GetAreaFromSceneName(__instance.AreaName).ID;
            bool isAreaPermanent = AreaManager.Instance.PermenantAreas.Contains(areaEnum);
            bool resetArea = _areasResetLayers.Value.HasFlag(AreasResetLayers.Cities) || !isAreaPermanent;

            // Area modes
            float sinceLastVisit = GameTime - (float)__instance.GameTime;
            float sinceLastReset = GameTime - __instance.SaveCreationGameTime;
            resetArea &= _areasMode == ResetMode.Always
                      || _areasMode == ResetMode.Timer
                                   && sinceLastVisit >= _areasTimer * TIME_UNIT
                                   && sinceLastReset >= _areasTimerSinceReset * TIME_UNIT;
            // Execute
            if (resetArea)
                __instance.SaveCreationGameTime = GameTime.RoundDown();
            if (!resetArea || !_areasResetLayers.Value.HasFlag(AreasResetLayers.ItemsAndContainers))
                ItemManager.Instance.LoadItems(__instance.ItemList, true);
            if (!resetArea || !_areasResetLayers.Value.HasFlag(AreasResetLayers.Enemies))
                CharacterManager.Instance.LoadAiCharactersFromSave(__instance.CharList.ToArray());
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

        // Gatherables
        [HarmonyPatch(typeof(Gatherable), "StartInit"), HarmonyPostfix]
        static void Gatherable_StartInit_Post(ref Gatherable __instance)
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
        [HarmonyPatch(typeof(MerchantPouch), "RefreshInventory"), HarmonyPrefix]
        static bool MerchantPouch_RefreshInventory_Pre(ref MerchantPouch __instance, ref double ___m_nextRefreshTime)
        {
            #region quit
            if (!_merchantsToggle)
                return true;
            #endregion

            if (_merchantsMode == ResetMode.Always)
                ___m_nextRefreshTime = 0d;
            else if (_merchantsMode == ResetMode.Never && !__instance.IsEmpty)
                ___m_nextRefreshTime = double.PositiveInfinity;
            else if (_merchantsMode == ResetMode.Timer)
            {
                __instance.InventoryRefreshRate = _merchantsTimer * TIME_UNIT;
                if (___m_nextRefreshTime == double.PositiveInfinity)
                    ___m_nextRefreshTime = GameTime + __instance.InventoryRefreshRate;
            }

            return true;
        }
    }
}