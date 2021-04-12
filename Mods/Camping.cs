using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace ModPack
{
    public class Camping : AMod
    {
        #region const
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
        private enum CampingSpots
        {
            None = 0,
            All = ~0,

            Cities = 1 << 1,
            OpenRegions = 1 << 2,
            Butterflies = 1 << 3,
            Dungeons = 1 << 4,
        }
        #endregion
        // Setting
        static private ModSetting<CampingSpots> _campingSpots;
        static private ModSetting<int> _butterfliesSpawnChance;
        static private ModSetting<int> _butterfliesRadius;
        override protected void Initialize()
        {
            _campingSpots = CreateSetting(nameof(_campingSpots), CampingSpots.All);
            _butterfliesSpawnChance = CreateSetting(nameof(_butterfliesSpawnChance), 100, IntRange(0, 100));
            _butterfliesRadius = CreateSetting(nameof(_butterfliesRadius), 25, IntRange(5, 50));
            _campingSpots.AddEvent(() =>
            {
                if (_campingSpots.Value.HasFlag(CampingSpots.OpenRegions))
                    _campingSpots.SetSilently(_campingSpots.Value | CampingSpots.Butterflies);
            });

            AddEventOnConfigClosed(SetButterfliesRadius);

            _areaSafeZones = new List<SphereCollider>();
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
                                             "(mMinimum settings is still twice as big as the visuals)";
        }
        override protected string Description
        => "• Restrict camping spots to chosen places\n" +
           "• Change butterfly zones spawn chance and radius";

        // Utility
        static private List<SphereCollider> _areaSafeZones;
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
            foreach (var safeZone in _areaSafeZones)
                if (position.DistanceTo(safeZone.transform.position) <= safeZone.radius)
                    return true;
            return false;
        }
        static private void SetButterfliesRadius()
        {
            foreach (var safeZone in _areaSafeZones)
                safeZone.radius = _butterfliesRadius;
        }
        // Hooks
        [HarmonyPatch(typeof(EnvironmentSave), "ApplyData"), HarmonyPostfix]
        static void EnvironmentSave_ApplyData_Post(ref EnvironmentSave __instance)
        {
            _areaSafeZones.Clear();
            GameObject fxHolder = GameObject.Find("Environment/Assets/FX");
            if (fxHolder == null)
                return;

            foreach (Transform fx in fxHolder.transform)
                if (fx.GOName().ContainsSubstring("butterfly"))
                {
                    bool isActive = UnityEngine.Random.value <= _butterfliesSpawnChance / 100f;
                    fx.GOSetActive(isActive);
                    if (isActive)
                        _areaSafeZones.Add(fx.GetComponent<SphereCollider>());
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
    }
}

/*
 *         // Setting
        static private ModSetting<int> _currentVal, _maxVal, _activeMax;
        static private ModSetting<bool> _forceSet;
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