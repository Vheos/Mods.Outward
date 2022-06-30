namespace Vheos.Mods.Outward;

public class Damage : AMod
{
    #region Settings
    private static readonly Dictionary<Team, DamageSettings> _settingsByTeam = new();
    private class DamageSettings : PerValueSettings<Damage, Team>
    {
        public ModSetting<int> HealthDamageMultiplier, FFHealthDamageMultiplier;
        public ModSetting<int> StabilityDamageMultiplier, FFStabilityDamageMultiplier;
        public DamageSettings(Damage mod, Team value, bool isToggle = false) : base(mod, value, isToggle)
        { }
    }
    protected override void Initialize()
    {
        foreach (var team in Utility.GetEnumValues<Team>())
        {
            DamageSettings settings = new(this, team);
            _settingsByTeam[team] = settings;
            int ffMultiplier = team == Team.Players ? 0 : 100;

            settings.HealthDamageMultiplier = CreateSetting(nameof(settings.HealthDamageMultiplier), 100, IntRange(0, 200));
            settings.StabilityDamageMultiplier = CreateSetting(nameof(settings.StabilityDamageMultiplier), 100, IntRange(0, 200));
            settings.FFHealthDamageMultiplier = CreateSetting(nameof(settings.FFHealthDamageMultiplier), ffMultiplier, IntRange(0, 200));
            settings.FFStabilityDamageMultiplier = CreateSetting(nameof(settings.FFStabilityDamageMultiplier), ffMultiplier, IntRange(0, 200));
        }
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _settingsByTeam[Team.Players].HealthDamageMultiplier.Value = 50;
                _settingsByTeam[Team.Players].StabilityDamageMultiplier.Value = 50;
                _settingsByTeam[Team.Players].FFHealthDamageMultiplier.Value = 20;
                _settingsByTeam[Team.Players].FFStabilityDamageMultiplier.Value = 40;

                _settingsByTeam[Team.Enemies].HealthDamageMultiplier.Value = 80;
                _settingsByTeam[Team.Enemies].StabilityDamageMultiplier.Value = 120;
                _settingsByTeam[Team.Enemies].FFHealthDamageMultiplier.Value = 20;
                _settingsByTeam[Team.Enemies].FFStabilityDamageMultiplier.Value = 40;
                break;
        }
    }
    #endregion

    #region Formatting
    protected override string SectionOverride
        => ModSections.Combat;
    protected override string Description
        => "• Adjust players' and enemies' damages" +
        "\n• Enable friendly fire between players" +
        "\n• Adjust friendly fire between enemies";
    protected override void SetFormatting()
    {
        foreach (var settings in _settingsByTeam.Values)
        {
            settings.FormatHeader();
            string teamName = settings.Value.ToString().ToLower();

            settings.Header.Description =
                $"Multipliers for damages dealt by {teamName}";
            using (Indent)
            {
                settings.HealthDamageMultiplier.Format("Health");
                settings.HealthDamageMultiplier.Description =
                    $"How much health damage {teamName} deal" +
                    $"\n\nUnit: percent multiplier";
                settings.StabilityDamageMultiplier.Format("Stability");
                settings.StabilityDamageMultiplier.Description =
                    $"How much stability damage {teamName} deal" +
                    $"\n\nUnit: percent multiplier";
                CreateHeader("Friendly Fire").Description =
                    $"Additional multipliers for damages dealt by {teamName} to other {teamName}";
                using (Indent)
                {
                    settings.FFHealthDamageMultiplier.Format("Health");
                    settings.FFHealthDamageMultiplier.Description =
                        $"How much health damage {teamName} deal to other {teamName}" +
                        $"\n\nUnit: percent multiplier";
                    settings.FFStabilityDamageMultiplier.Format("Stability");
                    settings.FFStabilityDamageMultiplier.Description =
                        $"How much stability damage {teamName} deal to other {teamName}" +
                        $"\n\nUnit: percent multiplier";
                }
            }
        }
    }
    #endregion

    #region Utility
    private static bool IsPlayersFriendlyFireEnabled
        => _settingsByTeam[Team.Players].FFHealthDamageMultiplier > 0
        || _settingsByTeam[Team.Players].FFStabilityDamageMultiplier > 0;
    private static void TryOverrideElligibleFaction(ref bool result, Character defender, Character attacker)
    {
        if (result
        || defender == null
        || defender == attacker
        || !defender.IsAlly()
        || !IsPlayersFriendlyFireEnabled)
            return;

        result = true;
    }
    #endregion

    #region Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(Weapon), nameof(Weapon.ElligibleFaction), new[] { typeof(Character) })]
    private static void Weapon_ElligibleFaction_Post(Weapon __instance, ref bool __result, Character _character)
        => TryOverrideElligibleFaction(ref __result, _character, __instance.OwnerCharacter);

    [HarmonyPostfix, HarmonyPatch(typeof(MeleeHitDetector), nameof(MeleeHitDetector.ElligibleFaction), new[] { typeof(Character) })]
    private static void MeleeHitDetector_ElligibleFaction_Post(MeleeHitDetector __instance, ref bool __result, Character _character)
        => TryOverrideElligibleFaction(ref __result, _character, __instance.OwnerCharacter);

    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.OnReceiveHitCombatEngaged))]
    private static bool Character_OnReceiveHitCombatEngaged_Pre(Character __instance, Character _dealerChar)
        => _dealerChar == null
        || !_dealerChar.IsAlly()
        || !IsPlayersFriendlyFireEnabled;

    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.VitalityHit))]
    private static void Character_VitalityHit_Pre(Character __instance, Character _dealerChar, ref float _damage)
    {
        if (_dealerChar != null && _dealerChar.IsEnemy()
        || _dealerChar == null && __instance.IsAlly())
        {
            _damage *= _settingsByTeam[Team.Enemies].HealthDamageMultiplier / 100f;
            if (__instance.IsEnemy())
                _damage *= _settingsByTeam[Team.Enemies].FFHealthDamageMultiplier / 100f;
        }
        else
        {
            _damage *= _settingsByTeam[Team.Players].HealthDamageMultiplier / 100f;
            if (__instance.IsAlly())
                _damage *= _settingsByTeam[Team.Players].FFHealthDamageMultiplier / 100f;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.StabilityHit))]
    private static void Character_StabilityHit_Pre(Character __instance, Character _dealerChar, ref float _knockValue)
    {
        if (_dealerChar != null && _dealerChar.IsEnemy()
        || _dealerChar == null && __instance.IsAlly())
        {
            _knockValue *= _settingsByTeam[Team.Enemies].StabilityDamageMultiplier / 100f;
            if (__instance.IsEnemy())
                _knockValue *= _settingsByTeam[Team.Enemies].FFStabilityDamageMultiplier / 100f;
        }
        else
        {
            _knockValue *= _settingsByTeam[Team.Players].StabilityDamageMultiplier / 100f;
            if (__instance.IsAlly())
                _knockValue *= _settingsByTeam[Team.Players].FFStabilityDamageMultiplier / 100f;
        }
    }
    #endregion
}