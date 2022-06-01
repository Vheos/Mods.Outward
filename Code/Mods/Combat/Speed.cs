﻿namespace Vheos.Mods.Outward;

public class Speed : AMod, IUpdatable
{
    #region const
    private const float FIXED_TIME_DELTA = 0.022f;   // Global.Update(), PauseMenu.Pause(), PauseMenu.TogglePause()
    #endregion

    // Config
    private static ModSetting<bool> _gameToggle, _playersToggle, _enemiesToggle;
    private static ModSetting<int> _defaultGameSpeed, _speedHackMultiplier;
    private static ModSetting<string> _speedHackKey;
    private static ModSetting<int> _playersAnimationSpeed, _playersMovementSpeed, _playersAttackSpeed;
    private static ModSetting<int> _enemiesAnimationSpeed, _enemiesMovementSpeed, _enemiesAttackSpeed;
    protected override void Initialize()
    {
        _gameToggle = CreateSetting(nameof(_gameToggle), false);
        _defaultGameSpeed = CreateSetting(nameof(_defaultGameSpeed), 100, IntRange(0, 200));
        _speedHackMultiplier = CreateSetting(nameof(_speedHackMultiplier), 300, IntRange(0, 500));
        _speedHackKey = CreateSetting(nameof(_speedHackKey), "");

        _playersToggle = CreateSetting(nameof(_playersToggle), false);
        _playersAnimationSpeed = CreateSetting(nameof(_playersAnimationSpeed), 100, IntRange(0, 200));
        _playersMovementSpeed = CreateSetting(nameof(_playersMovementSpeed), 100, IntRange(0, 200));
        _playersAttackSpeed = CreateSetting(nameof(_playersAttackSpeed), 100, IntRange(0, 200));

        _enemiesToggle = CreateSetting(nameof(_enemiesToggle), false);
        _enemiesAnimationSpeed = CreateSetting(nameof(_enemiesAnimationSpeed), 100, IntRange(0, 200));
        _enemiesMovementSpeed = CreateSetting(nameof(_enemiesMovementSpeed), 100, IntRange(0, 200));
        _enemiesAttackSpeed = CreateSetting(nameof(_enemiesAttackSpeed), 100, IntRange(0, 200));

        AddEventOnConfigClosed(UpdateDefaultGameSpeed);
    }
    protected override void SetFormatting()
    {
        _gameToggle.Format("Game");
        _gameToggle.Description = "Set multipliers (%) for the whole game world";
        using (Indent)
        {
            _defaultGameSpeed.Format("Default game speed", _gameToggle);
            _speedHackMultiplier.Format("SpeedHack multiplier", _gameToggle);
            _speedHackMultiplier.Description = "Default game speed is multiplied by this value when speedhack is enabled";
            _speedHackKey.Format("SpeedHack key", _gameToggle);
            _speedHackKey.Description = "Use UnityEngine.KeyCode enum values\n" +
                                        "(https://docs.unity3d.com/ScriptReference/KeyCode.html)";
        }

        _playersToggle.Format("Players");
        _playersToggle.Description = "Set multipliers (%) players' speeds";
        using (Indent)
        {
            _playersAnimationSpeed.Format("All animations", _playersToggle);
            _playersAnimationSpeed.Description = "Includes animations other than moving and attacking\n" +
                                                 "(using skills, using items, dodging, gathering)";
            _playersMovementSpeed.Format("Movement", _playersToggle);
            _playersAttackSpeed.Format("Attack", _playersToggle);
        }

        _enemiesToggle.Format("NPCs");
        _enemiesToggle.Description = "Set multipliers (%) NPCs' speeds";
        using (Indent)
        {
            _enemiesAnimationSpeed.Format("All animations", _enemiesToggle);
            _enemiesAnimationSpeed.Description = _playersAnimationSpeed.Description;
            _enemiesMovementSpeed.Format("Movement", _enemiesToggle);
            _enemiesAttackSpeed.Format("Attack", _enemiesToggle);
        }
    }
    protected override string Description
    => "• Change players/NPCs speed multipliers\n" +
       "(all animations, movement, attack)\n" +
       "• Affects FINAL speed, after all reductions and amplifications\n" +
       "• Override default game speed\n" +
       "• Toggle speedhack with a hotkey";
    protected override string SectionOverride
    => ModSections.Combat;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _gameToggle.Value = true;
                {
                    _defaultGameSpeed.Value = 90;
                    _speedHackMultiplier.Value = 250;
                    _speedHackKey.Value = KeyCode.Keypad1.ToString();
                }
                _playersToggle.Value = true;
                _playersMovementSpeed.Value = 90;
                _enemiesToggle.Value = true;
                {
                    _enemiesAnimationSpeed.Value = 90;
                    _enemiesMovementSpeed.Value = 125;
                }
                break;
        }
    }
    public void OnUpdate()
    {
        if (IsEnabled)
            if (_speedHackKey.Value.ToKeyCode().Pressed())
                ToggleSpeedHack();
    }

    // Utility
    private static void UpdateDefaultGameSpeed()
    {
        if (Global.GamePaused)
            return;

        Time.timeScale = _defaultGameSpeed / 100f;
        Time.fixedDeltaTime = FIXED_TIME_DELTA * Time.timeScale;
    }
    private static void ToggleSpeedHack()
    {
        if (Global.GamePaused)
            return;

        float defaultSpeed = _defaultGameSpeed / 100f;
        float speedHackSpeed = defaultSpeed * _speedHackMultiplier / 100f;
        Time.timeScale = Time.timeScale < speedHackSpeed ? speedHackSpeed : defaultSpeed;
        Time.fixedDeltaTime = FIXED_TIME_DELTA * Time.timeScale;
    }
    private static bool TryUpdateAnimationSpeed(Character character)
    {
        #region quit
        if (!_playersToggle && !_enemiesToggle
        || character.Stunned || character.IsPetrified)
            return true;
        #endregion

        if (_playersToggle && character.IsAlly())
            character.Animator.speed = _playersAnimationSpeed / 100f;
        else if (_enemiesToggle && character.IsEnemy())
            character.Animator.speed = _enemiesAnimationSpeed / 100f;
        return true;
    }

    // Hooks
    [HarmonyPatch(typeof(Character), nameof(Character.LateUpdate)), HarmonyPostfix]
    private static void Character_LateUpdate_Post(Character __instance)
    => TryUpdateAnimationSpeed(__instance);

    [HarmonyPatch(typeof(Character), nameof(Character.TempSlowDown)), HarmonyPrefix]
    private static bool Character_TempSlowDown_Pre(Character __instance)
    => TryUpdateAnimationSpeed(__instance);

    [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.MovementSpeed), MethodType.Getter), HarmonyPostfix]
    private static void CharacterStats_MovementSpeed_Getter_Post(CharacterStats __instance, ref float __result)
    {
        Character character = __instance.m_character;
        if (_playersToggle && character.IsAlly())
            __result *= _playersMovementSpeed / 100f;
        else if (_enemiesToggle && character.IsEnemy())
            __result *= _enemiesMovementSpeed / 100f;
    }

    [HarmonyPatch(typeof(Weapon), nameof(Weapon.GetAttackSpeed)), HarmonyPostfix]
    private static void Weapon_GetAttackSpeed_Post(Weapon __instance, ref float __result)
    {
        Character owner = __instance.OwnerCharacter;
        if (_playersToggle && owner.IsAlly())
            __result *= _playersAttackSpeed / 100f;
        else if (_enemiesToggle && owner.IsEnemy())
            __result *= _enemiesAttackSpeed / 100f;
    }
}

/*

static private ModSetting<string> _pauseKey,

_pauseKey = CreateSetting(nameof(_pauseKey), "");

_pauseKey.Format("Pause key", _gameToggle);
_pauseKey.Description = _speedHackKey.Description;

if (Input.GetKeyDown(_pauseKey.Value.ToKeyCode()))
PauseMenu.Pause(!Global.GamePaused);

[HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.Pause)), HarmonyPostfix]
static void PauseMenu_Pause_Post()
{
#region quit
if (!_gameToggle)
    return;
#endregion

UpdateDefaultGameSpeed();
}

[HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.TogglePause)), HarmonyPostfix]
static void PauseMenu_TogglePause_Post()
{
#region quit
if (!_gameToggle)
    return;
#endregion

UpdateDefaultGameSpeed();
}
*/