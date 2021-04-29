using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;

namespace ModPack
{
    public class Camping : AMod
    {
        #region const
        private const int FAST_MAINTENANCE_ID = 8205140;
        private const string CANT_CAMP_NOTIFICATION = "You can't camp here!";
        static private AreaManager.AreaEnum[] OPEN_REGIONS = new[]
        {
            AreaManager.AreaEnum.CierzoOutside,
            AreaManager.AreaEnum.Emercar,
            AreaManager.AreaEnum.HallowedMarsh,
            AreaManager.AreaEnum.Abrassar,
            AreaManager.AreaEnum.AntiqueField,
            AreaManager.AreaEnum.Caldera,
        };
        static private AreaManager.AreaEnum[] CITIES = new[]
        {
            AreaManager.AreaEnum.CierzoVillage,
            AreaManager.AreaEnum.Berg,
            AreaManager.AreaEnum.Monsoon,
            AreaManager.AreaEnum.Levant,
            AreaManager.AreaEnum.Harmattan,
            AreaManager.AreaEnum.NewSirocco,
        };
        #endregion
        #region enum
        [Flags]
        public enum CampingSpots
        {
            None = 0,

            Cities = 1 << 1,
            OpenRegions = 1 << 2,
            Butterflies = 1 << 3,
            Dungeons = 1 << 4,
        }
        [Flags]
        public enum CampingActivities
        {
            None = 0,

            Sleep = 1 << 1,
            Guard = 1 << 2,
            Repair = 1 << 3,
        }
        public enum MultiRepairBehaviour
        {
            UseFixedValueForAllItems = 1,
            DivideValueAmongItems = 2,
            TryToEqualizeRatios = 3,
        }
        public enum RepairValueSemantic
        {
            PercentOfMaxDurability = 1,
            PercentOfMissingDurability = 2,
        }
        #endregion
        // Setting
        static public ModSetting<CampingSpots> _campingSpots;
        static public ModSetting<int> _butterfliesSpawnChance;
        static public ModSetting<int> _butterfliesRadius;
        static public ModSetting<CampingActivities> _campingActivities;
        static public ModSetting<int> _repairDurabilityPerHour;
        static public ModSetting<RepairValueSemantic> _repairValueSemantic;
        static public ModSetting<MultiRepairBehaviour> _multiRepairBehaviour;
        static public ModSetting<int> _fastMaintenanceMultiplier;
        override protected void Initialize()
        {
            _campingSpots = CreateSetting(nameof(_campingSpots), (CampingSpots)~0);
            _butterfliesSpawnChance = CreateSetting(nameof(_butterfliesSpawnChance), 100, IntRange(0, 100));
            _butterfliesRadius = CreateSetting(nameof(_butterfliesRadius), 25, IntRange(5, 50));
            _campingActivities = CreateSetting(nameof(_campingActivities), (CampingActivities)~0);
            _repairDurabilityPerHour = CreateSetting(nameof(_repairDurabilityPerHour), 10, IntRange(0, 100));
            _repairValueSemantic = CreateSetting(nameof(_repairValueSemantic), RepairValueSemantic.PercentOfMaxDurability);
            _multiRepairBehaviour = CreateSetting(nameof(_multiRepairBehaviour), MultiRepairBehaviour.UseFixedValueForAllItems);
            _fastMaintenanceMultiplier = CreateSetting(nameof(_fastMaintenanceMultiplier), 150, IntRange(100, 200));

            _campingSpots.AddEvent(() =>
            {
                if (_campingSpots.Value.HasFlag(CampingSpots.OpenRegions))
                    _campingSpots.SetSilently(_campingSpots.Value | CampingSpots.Butterflies);
            });

            AddEventOnConfigClosed(SetButterfliesRadius);

            _safeZoneColliders = new List<SphereCollider>();
        }
        override protected void SetFormatting()
        {
            _campingSpots.Format("Camping spots");
            _campingSpots.Description = "Restrict where you can camp";
            _butterfliesSpawnChance.Format("Butterflies spawn chance");
            _butterfliesSpawnChance.Description = "Each butterfly zone in the area you're entering has X% to spawn\n" +
                                                  "Allows you to randomize safe zones for more unpredictability";
            _butterfliesRadius.Format("Butterflies radius");
            _butterfliesRadius.Description = "Vanilla radius is so big that it's possible to accidently set up a camp in a safe zone\n" +
                                             "(minimum settings is still twice as big as the visuals)";
            _campingActivities.Format("Available camping activities");
            _repairDurabilityPerHour.Format("Repair value per hour");
            _repairDurabilityPerHour.Description = "By default, % of max durability (can be changed below)";
            _repairValueSemantic.Format("");
            _multiRepairBehaviour.Format("When repairing multiple items");
            _multiRepairBehaviour.Description = "Use fixed value for all items   -   the same repair value will be used for all items\n" +
                                                "Divide value among items   -   the repair value will be divided by the number of equipped items\n" +
                                                "Try to equalize ratios   -   each hour will be spent on repairing the most damaged item, so after enough time spent all items will have nearly equal durability ratios";
            _fastMaintenanceMultiplier.Format("\"Fast Maintenance\" repair multiplier");
        }
        override protected string Description
        => "• Restrict camping spots to chosen places\n" +
           "• Change butterfly zones spawn chance and radius\n" +
           "• Customize repairing mechanic";
        override protected string SectionOverride
        => Presets.SECTION_SURVIVAL;

        // Utility
        static private List<SphereCollider> _safeZoneColliders;
        static private bool IsCampingAllowed(Character character, Vector3 position)
        {
            AreaManager.AreaEnum currentArea = (AreaManager.AreaEnum)AreaManager.Instance.CurrentArea.ID;
            bool result = false;
            if (currentArea.IsContainedIn(CITIES))
                result = _campingSpots.Value.HasFlag(CampingSpots.Cities);
            else if (currentArea.IsContainedIn(OPEN_REGIONS))
                result = _campingSpots.Value.HasFlag(CampingSpots.OpenRegions)
                    || _campingSpots.Value.HasFlag(CampingSpots.Butterflies) && IsNearButterflies(position);
            else
                result = _campingSpots.Value.HasFlag(CampingSpots.Dungeons);

            if (!result)
                character.CharacterUI.ShowInfoNotification(CANT_CAMP_NOTIFICATION);

            return result;
        }
        static private bool IsNearButterflies(Vector3 position)
        {
            foreach (var safeZone in _safeZoneColliders)
                if (position.DistanceTo(safeZone.transform.position) <= safeZone.radius)
                    return true;
            return false;
        }
        static private void SetButterfliesRadius()
        {
            foreach (var collider in _safeZoneColliders)
                if (collider != null)
                    collider.radius = _butterfliesRadius;
        }
        static private float CalculateNewDurabilityRatio(Item item, float repairValue)
        {
            if (_repairValueSemantic == RepairValueSemantic.PercentOfMissingDurability)
                repairValue *= 1 - item.DurabilityRatio;
            return item.DurabilityRatio + repairValue;
        }
        static private bool HasLearnedFastMaintenance(Character character)
        => character.Inventory.SkillKnowledge.IsItemLearned(FAST_MAINTENANCE_ID);

        // Hooks
        [HarmonyPatch(typeof(EnvironmentSave), "ApplyData"), HarmonyPostfix]
        static void EnvironmentSave_ApplyData_Post(ref EnvironmentSave __instance)
        {
            _safeZoneColliders.Clear();
            GameObject fxHolder = GameObject.Find("Environment/Assets/FX");
            if (fxHolder == null)
                return;

            foreach (Transform fx in fxHolder.transform)
                if (fx.GOName().ContainsSubstring("butterfly"))
                {
                    AmbienceSound ambienceSound = fx.GetComponentInChildren<AmbienceSound>();
                    if (UnityEngine.Random.value <= _butterfliesSpawnChance / 100f)
                    {
                        fx.GOSetActive(true);
                        ambienceSound.MinVolume = ambienceSound.MaxVolume = 1;
                        _safeZoneColliders.Add(fx.GetComponent<SphereCollider>());
                    }
                    else
                    {
                        fx.GOSetActive(false);
                        ambienceSound.MinVolume = ambienceSound.MaxVolume = 0;
                    }
                }

            SetButterfliesRadius();
        }

        [HarmonyPatch(typeof(BasicDeployable), "TryDeploying", new[] { typeof(Character) }), HarmonyPrefix]
        static bool BasicDeployable_TryDeploying_Pre(ref BasicDeployable __instance, Character _usingCharacter)
        => !__instance.Item.IsSleepKit || IsCampingAllowed(_usingCharacter, __instance.transform.position);

        [HarmonyPatch(typeof(Sleepable), "OnReceiveSleepRequestResult"), HarmonyPrefix]
        static bool Sleepable_OnReceiveSleepRequestResult_Pre(ref Sleepable __instance, Character _character)
        => __instance.IsInnsBed || IsCampingAllowed(_character, __instance.transform.position);

        [HarmonyPatch(typeof(OrientOnTerrain), "IsValid", MethodType.Getter), HarmonyPrefix]
        static bool OrientOnTerrain_IsValid_Pre(ref OrientOnTerrain __instance)
        {
            AreaManager.AreaEnum currentArea = (AreaManager.AreaEnum)AreaManager.Instance.CurrentArea.ID;
            #region quit
            if (!__instance.m_detectionScript.DeployedItem.IsSleepKit
            || currentArea.IsNotContainedIn(OPEN_REGIONS)
            || _campingSpots.Value.HasFlag(CampingSpots.OpenRegions)
            || !_campingSpots.Value.HasFlag(CampingSpots.Butterflies))
                return true;
            #endregion

            return IsNearButterflies(__instance.transform.position);
        }

        [HarmonyPatch(typeof(RestingMenu), "Show"), HarmonyPostfix]
        static void RestingMenu_Show_Post(ref RestingMenu __instance)
        {
            foreach (Transform child in __instance.m_restingActivitiesHolder.transform)
                foreach (var campingActivity in new[] { CampingActivities.Sleep, CampingActivities.Guard, CampingActivities.Repair })
                    if (child.GOName().ContainsSubstring(campingActivity.ToString()))
                        child.GOSetActive(_campingActivities.Value.HasFlag(campingActivity));
        }

        [HarmonyPatch(typeof(CharacterEquipment), "RepairEquipmentAfterRest"), HarmonyPrefix]
        static bool CharacterEquipment_RepairEquipmentAfterRest_Pre(ref CharacterEquipment __instance)
        {
            // Cache
            List<Equipment> equippedItems = new List<Equipment>();
            foreach (var slot in __instance.m_equipmentSlots)
                if (Various.IsAnythingEquipped(slot) && Various.IsNotLeftHandUsedBy2H(slot) && slot.EquippedItem.RepairedInRest)
                    equippedItems.Add(slot.EquippedItem);

            #region quit
            if (equippedItems.IsEmpty())
                return false;
            #endregion

            // Repair value
            float repairValue = _repairDurabilityPerHour / 100f;
            if (HasLearnedFastMaintenance(__instance.m_character))
                repairValue *= _fastMaintenanceMultiplier / 100f;
            if (_multiRepairBehaviour == MultiRepairBehaviour.DivideValueAmongItems)
                repairValue /= equippedItems.Count;

            // Execute
            for (int i = 0; i < __instance.m_character.CharacterResting.GetRepairLength(); i++)
                if (_multiRepairBehaviour == MultiRepairBehaviour.TryToEqualizeRatios)
                {
                    float minRatio = equippedItems.Min(item => item.DurabilityRatio);
                    Equipment minItem = equippedItems.Find(item => item.DurabilityRatio == minRatio);
                    minItem.SetDurabilityRatio(CalculateNewDurabilityRatio(minItem, repairValue));
                }
                else
                    foreach (var item in equippedItems)
                        item.SetDurabilityRatio(CalculateNewDurabilityRatio(item, repairValue));

            // Clamp
            foreach (var item in equippedItems)
                if (item.DurabilityRatio > 1f)
                    item.SetDurabilityRatio(1f);

            return false;
        }
    }
}

/*
 *         // Setting
        static public ModSetting<int> _currentVal, _maxVal, _activeMax;
        static public ModSetting<bool> _forceSet;
        override protected void Initialize()
        {
            _currentVal = CreateSetting(nameof(_currentVal), 0, IntRange(0, 100));
            _maxVal = CreateSetting(nameof(_maxVal), 100, IntRange(0, 100));
            _activeMax = CreateSetting(nameof(_activeMax), 75, IntRange(0, 75));
            _forceSet = CreateSetting(nameof(_forceSet), false);

            _currentVal.AddEvent(TryUpdateCustomBar);
            _maxVal.AddEvent(TryUpdateCustomBar);
            _activeMax.AddEvent(TryUpdateCustomBar);
        }
        override protected void SetFormatting()
        {
            _currentVal.Format("Current");
            _maxVal.Format("Max");
            _activeMax.Format("ActiveMax");
        }
        public void OnUpdate()
        {
            if (KeyCode.LeftAlt.Held())
            {
                if (KeyCode.Keypad0.Pressed())
                {
                }
            }
        }

        // Utility
        static private Bar _customBar;
        static private void TryUpdateCustomBar()
        {
            if (_customBar != null)
                _customBar.UpdateBar(_currentVal, _maxVal, _activeMax, _forceSet);
        }

        // Hooks
        [HarmonyPatch(typeof(LocalCharacterControl), "RetrieveComponents"), HarmonyPostfix]
        static void LocalCharacterControl_RetrieveComponents_Post(LocalCharacterControl __instance)
        {
            if (_customBar != null)
                return;

            Transform manaBar = Players.GetLocal(0).UI.transform.Find("Canvas/GameplayPanels/HUD/MainCharacterBars/Mana");
            _customBar = GameObject.Instantiate(manaBar).GetComponent<Bar>();
            _customBar.name = "CustomBar";
            _customBar.BecomeSiblingOf(manaBar);
            GameObject.DontDestroyOnLoad(_customBar);
        }
*/