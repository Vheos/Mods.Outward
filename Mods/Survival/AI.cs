using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



namespace ModPack
{
    public class AI : AMod
    {
        #region const
        private const float HUMAN_COLLISION_RADIUS = 0.4f;
        static private readonly Dictionary<TargetingGroups, Character.Factions[]> NEUTRAL_FACTION_GROUPS = new Dictionary<TargetingGroups, Character.Factions[]>
        {
            [TargetingGroups.HumansAndNonHostileMonsters] = new[]
        {
                Character.Factions.Bandits,
                Character.Factions.Deer,
            },
            [TargetingGroups.HostileMonsters] = new[]
        {
                Character.Factions.Mercs,
                Character.Factions.Tuanosaurs,
                Character.Factions.Hounds,
                Character.Factions.Merchants,
                Character.Factions.Golden,
                Character.Factions.CorruptionSpirit,
            },
        };
        #endregion
        #region quit
        [Flags]
        private enum TargetingGroups
        {
            HumansAndNonHostileMonsters = 1 << 1,
            HostileMonsters = 1 << 2,
        }
        #endregion

        // Settings
        static private ModSetting<int> _enemyDetectionModifier;
        static private ModSetting<TargetingGroups> _preventInfighting;
        static private ModSetting<int> _changeTargetOnHit;
        static private ModSetting<int> _walkTowardsPlayerOnSpawn;
        static private ModSetting<bool> _changeTargetWhenTooFar;
        static private ModSetting<float> _changeInterval;
        static private ModSetting<float> _changeMinimumDistance;
        static private ModSetting<float> _changeFarToNearRatio;
        override protected void Initialize()
        {
            _enemyDetectionModifier = CreateSetting(nameof(_enemyDetectionModifier), 0, IntRange(-100, +100));
            _preventInfighting = CreateSetting(nameof(_preventInfighting), (TargetingGroups)0);
            _changeTargetOnHit = CreateSetting(nameof(_changeTargetOnHit), 50, IntRange(0, 100));
            _walkTowardsPlayerOnSpawn = CreateSetting(nameof(_walkTowardsPlayerOnSpawn), 50, IntRange(0, 100));
            _changeTargetWhenTooFar = CreateSetting(nameof(_changeTargetWhenTooFar), false);

            _changeInterval = CreateSetting(nameof(_changeInterval), 2f, FloatRange(0.1f, 10f));
            _changeMinimumDistance = CreateSetting(nameof(_changeMinimumDistance), 1f, FloatRange(HUMAN_COLLISION_RADIUS * 2, 5f));
            _changeFarToNearRatio = CreateSetting(nameof(_changeFarToNearRatio), 2f, FloatRange(1f, 5f));
        }
        override protected void SetFormatting()
        {
            _enemyDetectionModifier.Format("Enemy detection modifier");
            _enemyDetectionModifier.Description = "at +100% enemies will detect you from twice the vanilla distance, and in a 90 degrees cone\n" +
                                                  "at -50% all ranges and angles will be halved\n" +
                                                  "at -100% they will be effectively blind and deaf";
            _preventInfighting.Format("Prevent infighting between");
            _preventInfighting.Description = "Humans and non hostile monsters:\n" +
                                             "most human enemies (bandits) and monster enemies that aren't hostile at first sight (pearlbirds, deers) will ignore each other\n" +
                                             "\n" +
                                             "Hostile monsters\n" +
                                             "most hostile monsters (mantises, hive lords, assassin bugs) will ignore each other";
            _changeTargetOnHit.Format("Change target on hit");
            _changeTargetOnHit.Description = "Chance that enemies will target the most recent attacker\n" +
                                             "at 0%, enemies will never change their first targets";
            _walkTowardsPlayerOnSpawn.Format("Walk towards player on spawn");
            _walkTowardsPlayerOnSpawn.Description = "Chance that enemies will walk in the nearest player's direction when spawned";

            _changeTargetWhenTooFar.Format("Change target when too far");
            Indent++;
            {
                _changeInterval.Format("Interval", _changeTargetWhenTooFar);
                _changeMinimumDistance.Format("Minimum distance", _changeTargetWhenTooFar);
                _changeFarToNearRatio.Format("Minimum ratio", _changeTargetWhenTooFar);
                Indent--;
            }
        }
        override protected string SectionOverride
        => SECTION_COMBAT;
        override public void LoadPreset(Presets.Preset preset)
        {
            switch (preset)
            {
                case Presets.Preset.Vheos_CoopSurvival:
                    ForceApply();
                    _enemyDetectionModifier.Value = +33;
                    _preventInfighting.Value = (TargetingGroups)~0;
                    break;
            }
        }

        // Utility

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members

        // Prevent infighting
        [HarmonyPatch(typeof(TargetingSystem), "InitTargetableFaction"), HarmonyPrefix]
        static bool TargetingSystem_InitTargetableFaction_Pre(TargetingSystem __instance)
        {
            #region quit
            if (_preventInfighting.Value == 0)
                return true;
            #endregion

            // Cache
            Character.Factions characterFaction = __instance.m_character.Faction;

            // Gather ignored factions
            List<Character.Factions> ignoreFactions = new List<Character.Factions>();
            foreach (var group in NEUTRAL_FACTION_GROUPS)
                if (group.Value.Contains(characterFaction))
                    foreach (var faction in group.Value)
                        if (faction != characterFaction)
                            ignoreFactions.TryAddUnique(faction);

            // Initialize
            List<Character.Factions> targetableFactions = new List<Character.Factions>();
            foreach (var faction in Utility.GetEnumValues<Character.Factions>())
            {
                if (faction == Character.Factions.NONE
                || faction == Character.Factions.COUNT
                || faction.IsContainedIn(ignoreFactions)
                || faction.IsContainedIn(__instance.StartAlliedFactions)
                || __instance.AlliedToSameFaction && faction == characterFaction)
                    continue;

                targetableFactions.Add(faction);
            }

            __instance.TargetableFactions = targetableFactions.ToArray();
            return false;
        }

        // Enemy detection modifier
        [HarmonyPatch(typeof(AIPreset), "ApplyToCharAI"), HarmonyPostfix]
        static void AIPreset_ApplyToCharAI_Post(AIPreset __instance, CharacterAI _charAI)
        {
            #region quit
            if (_enemyDetectionModifier == 0)
                return;
            #endregion

            float multiplier = 1 + _enemyDetectionModifier / 100f;
            foreach (var detection in _charAI.GetComponentsInChildren<AICEnemyDetection>(true))
            {
                // View angles
                detection.GoodViewAngle = Utility.Lerp3(0, detection.GoodViewAngle, 90, multiplier / 2);
                detection.ViewAngle = Utility.Lerp3(0, detection.ViewAngle, 180, multiplier / 2);

                // View ranges
                detection.ViewRange *= multiplier;
                detection.LowViewRange *= multiplier;

                // Hearing range
                detection.HearingDetectRange *= multiplier;

                // View and hearing detectability
                //detection.ViewVisDetect *= multiplier;
                //detection.HearingDetect *= multiplier;
            }
        }

        [HarmonyPatch(typeof(AISquadSpawnPoint), "SpawnSquad"), HarmonyPrefix]
        static void AISquadSpawnPoint_SpawnSquad_Pre(AISquadSpawnPoint __instance)
        => __instance.ChanceToWanderTowardsPlayers = _changeTargetOnHit;

        [HarmonyPatch(typeof(AICEnemyDetection), "Init"), HarmonyPrefix]
        static void AICEnemyDetection_Init_Pre(AICEnemyDetection __instance)
        => __instance.ChanceToSwitchTargetOnHurt = _walkTowardsPlayerOnSpawn;
    }
}