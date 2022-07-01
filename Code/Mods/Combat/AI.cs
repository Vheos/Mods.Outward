namespace Vheos.Mods.Outward;
using System.Collections;

public class AI : AMod
{
    #region Settings
    private static ModSetting<int> _enemyDetectionModifier;
    private static ModSetting<FactionGroup> _preventInfighting;
    private static ModSetting<int> _walkTowardsPlayerOnSpawn;
    private static ModSetting<int> _retargetOnHit;
    private static ModSetting<bool> _retargetWhenTooFar;
    private static ModSetting<Vector2> _retargetCheckInterval;
    private static ModSetting<float> _retargetCurrentToNearestRatio;
    private static ModSetting<bool> _retargetDetectAllPlayers;
    protected override void Initialize()
    {
        _enemyDetectionModifier = CreateSetting(nameof(_enemyDetectionModifier), 0, IntRange(-100, +100));
        _preventInfighting = CreateSetting(nameof(_preventInfighting), (FactionGroup)0);
        _retargetOnHit = CreateSetting(nameof(_retargetOnHit), 50, IntRange(0, 100));
        _walkTowardsPlayerOnSpawn = CreateSetting(nameof(_walkTowardsPlayerOnSpawn), 50, IntRange(0, 100));
        _retargetWhenTooFar = CreateSetting(nameof(_retargetWhenTooFar), false);
        _retargetCheckInterval = CreateSetting(nameof(_retargetCheckInterval), new Vector2(0f, 2f));
        _retargetCurrentToNearestRatio = CreateSetting(nameof(_retargetCurrentToNearestRatio), 1.5f, FloatRange(1f, 2f));
        _retargetDetectAllPlayers = CreateSetting(nameof(_retargetDetectAllPlayers), true);
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _enemyDetectionModifier.Value = +33;
                _preventInfighting.Value = (FactionGroup)~0;
                _retargetOnHit.Value = 0;
                _walkTowardsPlayerOnSpawn.Value = 0;
                _retargetWhenTooFar.Value = true;
                _retargetCheckInterval.Value = new(0f, 2f);
                _retargetCurrentToNearestRatio.Value = 1.2f;
                _retargetDetectAllPlayers.Value = true;
                break;
        }
    }
    #endregion

    #region Formatting
    protected override string SectionOverride
        => ModSections.Combat;
    protected override string Description
        => "• Adjust enemy detection" +
        "\n• Stop infighting between enemies" +
        "\n• Adjust enemies' retargeting behaviour";
    protected override void SetFormatting()
    {
        _enemyDetectionModifier.Format("Enemy detection modifier");
        _enemyDetectionModifier.Description =
            "How good the enemies are at detecting you" +
            "\nat +100% enemies will detect you from twice the original distance, and in a 90 degrees cone" +
            "\nat -50% all enemies' detectionranges and angles will be halved" +
            "\nat -100% enemies will be effectively blind and deaf" +
            "\n\nUnit: arbitrary linear scale";
        _preventInfighting.Format("Prevent infighting between...");
        _preventInfighting.Description =
            $"{FactionGroup.HumansAndNonHostileMonsters}:" +
            $"\nmost humans (eg. bandits) and non-hostile monsters (eg. pearlbirds, deers) will ignore each other" +
            $"\n\n{FactionGroup.HostileMonsters}:" +
            $"\nmost hostile monsters (eg. mantises, hive lords, assassin bugs) will ignore each other";
        _walkTowardsPlayerOnSpawn.Format("Chance to approach player on spawn");
        _walkTowardsPlayerOnSpawn.Description =
            "How likely enemies are to walk towards the nearest player when they spawn" +
            "\n\nUnit: percent chance";
        _retargetOnHit.Format("Chance to retarget on hit");
        _retargetOnHit.Description =
            "How likely enemies are to start targeting the attacker when attacked" +
            "\n\nUnit: percent chance";
        _retargetWhenTooFar.Format("Retarget when too far");
        _retargetWhenTooFar.Description =
            "Allows enemies to change their current target when it's too far away" +
            "\nOnly detected characters and actual attackers are taken into account";
        using (Indent)
        {
            _retargetCurrentToNearestRatio.Format("Current-to-nearest ratio", _retargetWhenTooFar);
            _retargetCurrentToNearestRatio.Description =
                "How far away enemies have to be from their current target to switch to a closer one" +
                "\nEnemies will switch only if their current target is this many times further away then the closest one" +
                "\n\nUnit: ratio between distance to the current target and to the closest valid target";
            _retargetCheckInterval.Format("Check interval", _retargetWhenTooFar);
            _retargetCheckInterval.Description =
                "How often enemies will attempt to switch to a closer target" +
                "\nEach interval is a random value between x and y" +
                "\n\nUnit: seconds";
            _retargetDetectAllPlayers.Format("Detect all players", _retargetWhenTooFar);
            _retargetDetectAllPlayers.Description =
                "Allows enemies to detect all players when they detect any";
        }
    }
    #endregion

    #region Utility
    public static void TryRetarget(CharacterAI ai)
    {
        Character enemy = ai.Character;
        var lastAttackers = enemy.m_lastDealers;
        //Log.Debug($"{enemy.Name} - enemies:  {lastAttackers.Count}");

        if (lastAttackers.Count <= 1)
            return;

        float currentDistance = enemy.DistanceTo(enemy.TargetingSystem.LockedCharacter);
        float nearestDistance = float.MaxValue;
        Character nearestAttacker = null;
        foreach (var attackerData in lastAttackers)
            if (CharacterManager.Instance.GetCharacter(attackerData.Key).TryNonNull(out var attacker))
            {
                float distance = enemy.DistanceTo(attacker);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestAttacker = attacker;
                }
            }

        //Log.Debug($"{enemy.Name} - distance: {currentDistance:F2} / {nearestDistance:F2} = {currentDistance / nearestDistance:F2}");
        if (currentDistance / nearestDistance >= _retargetCurrentToNearestRatio)
        {
            //Log.Debug($"\tswitching to {nearestAttacker.Name}");
            enemy.TargetingSystem.SwitchTarget(nearestAttacker.LockingPoint);
        }
    }
    public static IEnumerator TryRetargetCoroutine(CharacterAI ai)
    {
        //Log.Debug($"{ai.name} - START");
        while (true)
        {
            if (ai.Character.IsDead || ai.Character.IsPetrified
            || ai.CurrentAiState.IsNotAny<AISCombatMelee, AISCombatRanged>())
            {
                //Log.Debug($"{ai.name} - STOP");
                yield break;
            }

            TryRetarget(ai);
            yield return new WaitForSeconds(_retargetCheckInterval.Value.RandomRange());
        }
    }

    private static readonly Dictionary<FactionGroup, Character.Factions[]> FACTION_GROUPS_BY_ENUM = new()
    {
        [FactionGroup.HumansAndNonHostileMonsters] = new[]
        {
            Character.Factions.Bandits,
            Character.Factions.Deer,
        },
        [FactionGroup.HostileMonsters] = new[]
        {
            Character.Factions.Mercs,
            Character.Factions.Tuanosaurs,
            Character.Factions.Hounds,
            Character.Factions.Merchants,
            Character.Factions.Golden,
            Character.Factions.CorruptionSpirit,
        },
    };
    [Flags]
    private enum FactionGroup
    {
        HumansAndNonHostileMonsters = 1 << 1,
        HostileMonsters = 1 << 2,
    }
    #endregion

    #region Hooks
    // Prevent infighting
    [HarmonyPrefix, HarmonyPatch(typeof(TargetingSystem), nameof(TargetingSystem.InitTargetableFaction))]
    private static bool TargetingSystem_InitTargetableFaction_Pre(TargetingSystem __instance)
    {
        if (_preventInfighting.Value == 0)
            return true;

        // Cache
        Character.Factions characterFaction = __instance.m_character.Faction;

        // Gather ignored factions
        List<Character.Factions> ignoreFactions = new();
        foreach (var group in FACTION_GROUPS_BY_ENUM)
            if (group.Value.Contains(characterFaction))
                foreach (var faction in group.Value)
                    if (faction != characterFaction)
                        ignoreFactions.TryAddUnique(faction);

        // Initialize
        List<Character.Factions> targetableFactions = new();
        foreach (var faction in Utils.Factions)
        {
            if (faction.IsContainedIn(ignoreFactions)
            || faction.IsContainedIn(__instance.StartAlliedFactions)
            || __instance.AlliedToSameFaction && faction == characterFaction)
                continue;

            targetableFactions.Add(faction);
        }

        __instance.TargetableFactions = targetableFactions.ToArray();
        return false;
    }

    // Enemy detection modifier
    [HarmonyPostfix, HarmonyPatch(typeof(AIPreset), nameof(AIPreset.ApplyToCharAI))]
    private static void AIPreset_ApplyToCharAI_Post(AIPreset __instance, CharacterAI _charAI)
    {
        if (_enemyDetectionModifier == 0)
            return;

        float multiplier = 1 + _enemyDetectionModifier / 100f;
        foreach (var detection in _charAI.GetComponentsInChildren<AICEnemyDetection>(true))
        {
            // View angles
            detection.GoodViewAngle = Utils.Lerp3(0, detection.GoodViewAngle, 90, multiplier / 2);
            detection.ViewAngle = Utils.Lerp3(0, detection.ViewAngle, 180, multiplier / 2);

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

    [HarmonyPrefix, HarmonyPatch(typeof(AISquadSpawnPoint), nameof(AISquadSpawnPoint.SpawnSquad))]
    private static void AISquadSpawnPoint_SpawnSquad_Pre(AISquadSpawnPoint __instance)
        => __instance.ChanceToWanderTowardsPlayers = _walkTowardsPlayerOnSpawn;

    [HarmonyPrefix, HarmonyPatch(typeof(AICEnemyDetection), nameof(AICEnemyDetection.Init))]
    private static void AICEnemyDetection_Init_Pre(AICEnemyDetection __instance)
        => __instance.ChanceToSwitchTargetOnHurt = _retargetOnHit;

    // Change target when far
    [HarmonyPostfix, HarmonyPatch(typeof(CharacterAI), nameof(CharacterAI.SwitchAiState))]
    private static void CharacterAI_SwitchAiState_Post(CharacterAI __instance)
    {
        if (!_retargetWhenTooFar)
            return;

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

    [HarmonyPostfix, HarmonyPatch(typeof(AICEnemyDetection), nameof(AICEnemyDetection.Detected))]
    private static void AICEnemyDetection_Detected_Post(AICEnemyDetection __instance, LockingPoint _point)
    {
        if (!_retargetWhenTooFar
        || !_point.OwnerChar.TryNonNull(out var target))
            return;

        Character enemy = __instance.m_characterAI.Character;
        enemy.AddLastDealer(target.UID);
        if (_retargetDetectAllPlayers && target.IsPlayer())
            foreach (var player in Players.Local)
                enemy.AddLastDealer(player.Character.UID);
    }
    #endregion
}