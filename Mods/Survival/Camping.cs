﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using Random = UnityEngine.Random;



namespace ModPack
{
    public class Camping : AMod
    {
        #region const
        private const string CANT_CAMP_NOTIFICATION = "You can't camp here!";
        static private readonly AreaManager.AreaEnum[] OPEN_REGIONS = new[]
        {
            AreaManager.AreaEnum.CierzoOutside,
            AreaManager.AreaEnum.Emercar,
            AreaManager.AreaEnum.HallowedMarsh,
            AreaManager.AreaEnum.Abrassar,
            AreaManager.AreaEnum.AntiqueField,
            AreaManager.AreaEnum.Caldera,
        };
        static private readonly AreaManager.AreaEnum[] CITIES = new[]
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

            Cities = 1 << 1,
            OpenRegions = 1 << 2,
            Butterflies = 1 << 3,
            Dungeons = 1 << 4,
        }
        [Flags]
        private enum CampingActivities
        {
            None = 0,

            Sleep = 1 << 1,
            Guard = 1 << 2,
            Repair = 1 << 3,
        }
        #endregion

        // Setting
        static private ModSetting<CampingSpots> _campingSpots;
        static private ModSetting<int> _butterfliesSpawnChance;
        static private ModSetting<int> _butterfliesRadius;
        static private ModSetting<CampingActivities> _campingActivities;
        override protected void Initialize()
        {
            _campingSpots = CreateSetting(nameof(_campingSpots), (CampingSpots)~0);
            _butterfliesSpawnChance = CreateSetting(nameof(_butterfliesSpawnChance), 100, IntRange(0, 100));
            _butterfliesRadius = CreateSetting(nameof(_butterfliesRadius), 25, IntRange(5, 50));
            _campingActivities = CreateSetting(nameof(_campingActivities), (CampingActivities)~0);

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
        }
        override protected string Description
        => "• Restrict camping spots to chosen places\n" +
           "• Change butterfly zones spawn chance and radius\n" +
           "• Customize repairing mechanic";
        override protected string SectionOverride
        => SECTION_SURVIVAL;
        override public void LoadPreset(Presets.Preset preset)
        {
            switch (preset)
            {
                case Presets.Preset.Vheos_CoopSurvival:
                    ForceApply();
                    _campingSpots.Value = CampingSpots.Butterflies | CampingSpots.Dungeons;
                    _butterfliesSpawnChance.Value = 50;
                    _butterfliesRadius.Value = 5;
                    _campingActivities.Value = CampingActivities.Sleep | CampingActivities.Repair;
                    break;
            }
        }

        // Utility
        static private List<SphereCollider> _safeZoneColliders;
        static private bool IsCampingAllowed(Character character, Vector3 position)
        {
            AreaManager.AreaEnum currentArea = (AreaManager.AreaEnum)AreaManager.Instance.CurrentArea.ID;
            bool result;
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

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        [HarmonyPatch(typeof(EnvironmentSave), "ApplyData"), HarmonyPostfix]
        static void EnvironmentSave_ApplyData_Post(EnvironmentSave __instance)
        {
            _safeZoneColliders.Clear();
            GameObject fxHolder = GameObject.Find("Environment/Assets/FX");
            if (fxHolder == null)
                return;

            foreach (Transform fx in fxHolder.transform)
                if (fx.GOName().ContainsSubstring("butterfly"))
                {
                    AmbienceSound ambienceSound = fx.GetComponentInChildren<AmbienceSound>();
                    if (Random.value <= _butterfliesSpawnChance / 100f)
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
        static bool BasicDeployable_TryDeploying_Pre(BasicDeployable __instance, Character _usingCharacter)
        => !__instance.Item.IsSleepKit || IsCampingAllowed(_usingCharacter, __instance.transform.position);

        [HarmonyPatch(typeof(Sleepable), "OnReceiveSleepRequestResult"), HarmonyPrefix]
        static bool Sleepable_OnReceiveSleepRequestResult_Pre(Sleepable __instance, Character _character)
        => __instance.IsInnsBed || IsCampingAllowed(_character, __instance.transform.position);

        [HarmonyPatch(typeof(OrientOnTerrain), "IsValid", MethodType.Getter), HarmonyPrefix]
        static bool OrientOnTerrain_IsValid_Pre(OrientOnTerrain __instance)
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
        static void RestingMenu_Show_Post(RestingMenu __instance)
        {
            foreach (Transform child in __instance.m_restingActivitiesHolder.transform)
                foreach (var campingActivity in new[] { CampingActivities.Sleep, CampingActivities.Guard, CampingActivities.Repair })
                    if (child.GOName().ContainsSubstring(campingActivity.ToString()))
                        child.GOSetActive(_campingActivities.Value.HasFlag(campingActivity));
        }
    }
}