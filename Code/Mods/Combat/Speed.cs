namespace Vheos.Mods.Outward;

public class Speed : AMod, IUpdatable
{
    #region Settings
    private static ModSetting<int> _engineSpeedMultiplier;
    private static ModSetting<int> _speedHackMultiplier;
    private static ModSetting<string> _speedHackKey;
    private static readonly Dictionary<Team, SpeedSettings> _settingsByTeam = new();
    private class SpeedSettings : PerValueSettings<Speed, Team>
    {
        public ModSetting<int> GlobalSpeedMultiplier;
        public ModSetting<int> MovementSpeedMultiplier;
        public ModSetting<int> AttackSpeedMultiplier;
        public SpeedSettings(Speed mod, Team team, bool isToggle = false) : base(mod, team, isToggle)
        {
            int ffMultiplier = team == Team.Players ? 0 : 100;
            GlobalSpeedMultiplier = CreateSetting(nameof(GlobalSpeedMultiplier), 100, mod.IntRange(0, 200));
            MovementSpeedMultiplier = CreateSetting(nameof(MovementSpeedMultiplier), 100, mod.IntRange(0, 200));
            AttackSpeedMultiplier = CreateSetting(nameof(AttackSpeedMultiplier), ffMultiplier, mod.IntRange(0, 200));
        }
    }
    protected override void Initialize()
    {
        _engineSpeedMultiplier = CreateSetting(nameof(_engineSpeedMultiplier), 100, IntRange(0, 200));
        _speedHackMultiplier = CreateSetting(nameof(_speedHackMultiplier), 300, IntRange(0, 500));
        _speedHackKey = CreateSetting(nameof(_speedHackKey), "");

        foreach (var team in Utility.GetEnumValues<Team>())
            _settingsByTeam[team] = new(this, team);

        AddEventOnConfigClosed(UpdateDefaultGameSpeed);
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _engineSpeedMultiplier.Value = 90;
                _speedHackMultiplier.Value = 300;
                _speedHackKey.Value = KeyCode.Keypad1.ToString();
                _settingsByTeam[Team.Players].MovementSpeedMultiplier.Value = 90;
                _settingsByTeam[Team.Enemies].GlobalSpeedMultiplier.Value = 90;
                _settingsByTeam[Team.Enemies].MovementSpeedMultiplier.Value = 125;
                break;
        }
    }
    public void OnUpdate()
    {
        if (_speedHackKey.Value.ToKeyCode().Pressed())
            ToggleSpeedHack();
    }
    #endregion

    #region Formatting
    protected override string SectionOverride
        => ModSections.Combat;
    protected override string Description
        => "• Adjust default game speed" +
        "\n• Toggle speedhack with a hotkey" +
        "\n• Adjust players' and enemies' speeds";
    protected override void SetFormatting()
    {
        _engineSpeedMultiplier.Format("Engine speed");
        _engineSpeedMultiplier.Description =
            "How fast the game runs (shouldn't affect UI)" +
            "\n\nUnit: percent multiplier";
        _speedHackMultiplier.Format("SpeedHack multiplier");
        _speedHackMultiplier.Description =
            "Additional engine speed multiplier when speedhack is enabled (using the key below)" +
            "\n\nUnit: percent multiplier";
        using (Indent)
        {
            _speedHackKey.Format("toggle key");
            _speedHackKey.Description =
                $"Pressing this key will enable/disable speed hack" +
                $"\n\nValue type: case-insensitive {nameof(KeyCode)} enum" +
                $"\n(https://docs.unity3d.com/ScriptReference/KeyCode.html)";
        }

        foreach (var settings in _settingsByTeam.Values)
        {
            settings.FormatHeader();
            string teamName = settings.Value.ToString().ToLower();

            settings.Header.Description =
                $"Multipliers for {teamName}' speeds";
            using (Indent)
            {
                settings.GlobalSpeedMultiplier.Format("Animation");
                settings.GlobalSpeedMultiplier.Description =
                    $"How fast all {teamName}' animations are" +
                    $"\n\nUnit: percent multiplier";
                settings.MovementSpeedMultiplier.Format("Movement");
                settings.MovementSpeedMultiplier.Description =
                    $"How fast {teamName}' move" +
                    $"\n\nUnit: percent multiplier";
                settings.AttackSpeedMultiplier.Format("Attack");
                settings.AttackSpeedMultiplier.Description =
                    $"How fast {teamName}' basic attacks are" +
                    $"\n\nUnit: percent multiplier";
            }
        }
    }
    #endregion

    #region Utility
    private static void UpdateDefaultGameSpeed()
    {
        if (Global.GamePaused)
            return;

        Time.timeScale = _engineSpeedMultiplier / 100f;
        Time.fixedDeltaTime = Defaults.FixedTimeDelta * Time.timeScale;
    }
    private static void ToggleSpeedHack()
    {
        if (Global.GamePaused)
            return;

        float defaultSpeed = _engineSpeedMultiplier / 100f;
        float speedHackSpeed = defaultSpeed * _speedHackMultiplier / 100f;
        Time.timeScale = Time.timeScale < speedHackSpeed ? speedHackSpeed : defaultSpeed;
        Time.fixedDeltaTime = Defaults.FixedTimeDelta * Time.timeScale;
    }
    private static void TryUpdateAnimationSpeed(Character character)
    {
        if (character.Stunned
        || character.IsPetrified)
            return;

        character.Animator.speed = GetSettingsFor(character).GlobalSpeedMultiplier / 100f;
    }
    private static SpeedSettings GetSettingsFor(Character character)
        => _settingsByTeam[character.IsAlly() ? Team.Players : Team.Enemies];

    #endregion

    #region Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.LateUpdate))]
    private static void Character_LateUpdate_Post(Character __instance)
        => TryUpdateAnimationSpeed(__instance);

    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.TempSlowDown))]
    private static void Character_TempSlowDown_Pre(Character __instance)
        => TryUpdateAnimationSpeed(__instance);

    [HarmonyPostfix, HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.MovementSpeed), MethodType.Getter)]
    private static void CharacterStats_MovementSpeed_Getter_Post(CharacterStats __instance, ref float __result)
        => __result *= GetSettingsFor(__instance.m_character).MovementSpeedMultiplier / 100f;

    [HarmonyPostfix, HarmonyPatch(typeof(Weapon), nameof(Weapon.GetAttackSpeed))]
    private static void Weapon_GetAttackSpeed_Post(Weapon __instance, ref float __result)
        => __result *= GetSettingsFor(__instance.m_ownerCharacter).AttackSpeedMultiplier / 100f;
    #endregion
}