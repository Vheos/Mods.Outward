using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;

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
        static private ModSetting<int> _walkTowardsPlayerOnSpawn;
        static private ModSetting<int> _changeTargetOnHit;
        static private ModSetting<bool> _changeTargetWhenTooFar;
        static private ModSetting<int> _changeTargetChancePerSecond;
        static private ModSetting<float> _changeTargetCurrentToNearestRatio;
        static private ModSetting<bool> _changeTargetDetectAllPlayers;
        override protected void Initialize()
        {
            _enemyDetectionModifier = CreateSetting(nameof(_enemyDetectionModifier), 0, IntRange(-100, +100));
            _preventInfighting = CreateSetting(nameof(_preventInfighting), (TargetingGroups)0);
            _changeTargetOnHit = CreateSetting(nameof(_changeTargetOnHit), 50, IntRange(0, 100));
            _walkTowardsPlayerOnSpawn = CreateSetting(nameof(_walkTowardsPlayerOnSpawn), 50, IntRange(0, 100));
            _changeTargetWhenTooFar = CreateSetting(nameof(_changeTargetWhenTooFar), false);
            _changeTargetChancePerSecond = CreateSetting(nameof(_changeTargetChancePerSecond), 50, IntRange(0, 100));
            _changeTargetDetectAllPlayers = CreateSetting(nameof(_changeTargetDetectAllPlayers), false);
            _changeTargetCurrentToNearestRatio = CreateSetting(nameof(_changeTargetCurrentToNearestRatio), 2f, FloatRange(1f, 5f));
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
            _walkTowardsPlayerOnSpawn.Format("Walk towards player on spawn");
            _walkTowardsPlayerOnSpawn.Description = "Chance that enemies will walk in the nearest player's direction when spawned";
            _changeTargetOnHit.Format("Change target on hit");
            _changeTargetOnHit.Description = "Chance that enemies will target the most recent attacker\n" +
                                             "at 0%, enemies will never change their first targets";
            _changeTargetWhenTooFar.Format("Change target when too far");
            _changeTargetWhenTooFar.Description = "When enemy's current target retreats too far, the enemy will change to a closer one\n" +
                                                  "Only detected characters and actual attackers are taken into account";
            Indent++;
            {
                _changeTargetChancePerSecond.Format("Chance per second", _changeTargetWhenTooFar);
                _changeTargetChancePerSecond.Description = "at 100%, enemies will try to change target every second";
                _changeTargetCurrentToNearestRatio.Format("Minimum target-nearest ratio", _changeTargetWhenTooFar);
                _changeTargetCurrentToNearestRatio.Description = "Enemies will try to change target only if their current target is this many times further away then their nearest attacker";
                _changeTargetDetectAllPlayers.Format("Detect all players");
                _changeTargetDetectAllPlayers.Description = "When enemies detect any player, they will become aware of other players as well";
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
                    _changeTargetOnHit.Value = 0;
                    _walkTowardsPlayerOnSpawn.Value = 0;
                    _changeTargetWhenTooFar.Value = true;
                    _changeTargetChancePerSecond.Value = 50;
                    _changeTargetCurrentToNearestRatio.Value = 1.5f;
                    _changeTargetDetectAllPlayers.Value = true;
                    break;
            }
        }

        // Utility
        static public void TryRetarget(CharacterAI ai)
        {
            Character enemy = ai.Character;
            var lastAttackers = enemy.m_lastDealers;
            //Tools.Log($"{enemy.Name} - enemies:  {lastAttackers.Count}");

            if (lastAttackers.Count <= 1
            || UnityEngine.Random.value > _changeTargetChancePerSecond / 100f)
                return;

            float currentDistance = enemy.DistanceTo(enemy.TargetingSystem.LockedCharacter);
            float nearestDistance = float.MaxValue;
            Character nearestAttacker = null;
            foreach (var attackerData in lastAttackers)
                if (CharacterManager.Instance.GetCharacter(attackerData.Key).TryAssign(out var attacker))
                {
                    float distance = enemy.DistanceTo(attacker);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestAttacker = attacker;
                    }
                }

            //Tools.Log($"{enemy.Name} - distance: {currentDistance:F2} / {nearestDistance:F2} = {currentDistance / nearestDistance:F2}");
            if (currentDistance / nearestDistance >= _changeTargetCurrentToNearestRatio)
            {
                //Tools.Log($"\tswitching to {nearestAttacker.Name}");
                enemy.TargetingSystem.SwitchTarget(nearestAttacker.LockingPoint);
            }
        }
        static public IEnumerator TryRetargetCoroutine(CharacterAI ai)
        {
            //Tools.Log($"{ai.name} - START");
            while (true)
            {
                if (ai.Character.IsDead || ai.Character.IsPetrified
                || ai.CurrentAiState.IsNotAny<AISCombatMelee, AISCombatRanged>())
                {
                    //Tools.Log($"{ai.name} - STOP");
                    yield break;
                }

                TryRetarget(ai);
                yield return new WaitForSeconds(1f);
            }
        }

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
        => __instance.ChanceToWanderTowardsPlayers = _walkTowardsPlayerOnSpawn;

        [HarmonyPatch(typeof(AICEnemyDetection), "Init"), HarmonyPrefix]
        static void AICEnemyDetection_Init_Pre(AICEnemyDetection __instance)
        => __instance.ChanceToSwitchTargetOnHurt = _changeTargetOnHit;

        // Change target when far
        [HarmonyPatch(typeof(CharacterAI), "SwitchAiState"), HarmonyPostfix]
        static void CharacterAI_SwitchAiState_Post(CharacterAI __instance)
        {
            #region quit
            if (!_changeTargetWhenTooFar)
                return;
            #endregion

            AIState previousState = __instance.AiStates[__instance.m_previousStateID];
            AIState currentState = __instance.AiStates[__instance.m_currentStateID];

            // Entered combat
            if (previousState.IsNotAny<AISCombatMelee, AISCombatRanged>()
            && currentState.IsAny<AISCombatMelee, AISCombatRanged>())
                __instance.StartCoroutine(TryRetargetCoroutine(__instance));

            // Quit combat
            else if (previousState.IsAny<AISCombatMelee, AISCombatRanged>()
            && currentState.IsNotAny<AISCombatMelee, AISCombatRanged>())
                __instance.Character.m_lastDealers.Clear();
        }

        [HarmonyPatch(typeof(AICEnemyDetection), "Detected"), HarmonyPostfix]
        static void AICEnemyDetection_Detected_Post(AICEnemyDetection __instance, LockingPoint _point)
        {
            #region quit
            if (!_changeTargetWhenTooFar || !_point.OwnerChar.TryAssign(out var target))
                return;
            #endregion

            Character enemy = __instance.m_characterAI.Character;
            enemy.AddLastDealer(target.UID);
            if (_changeTargetDetectAllPlayers && target.IsPlayer())
                foreach (var player in Players.Local)
                    enemy.AddLastDealer(player.Character.UID);
        }
    }
}

/*
 *         static private ModSetting<float> _changeMinimumDistance;
        _changeMinimumDistance = CreateSetting(nameof(_changeMinimumDistance), 1f, FloatRange(HUMAN_COLLISION_RADIUS* 2, 5f));
            _changeMinimumDistance.Format("Minimum distance", _changeTargetWhenTooFar);
*/