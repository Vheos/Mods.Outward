using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



namespace ModPack
{
    public class Speed : AMod, IUpdatable
    {
        #region const
        private const float FIXED_TIME_DELTA = 0.022f;   // Global.Update(), PauseMenu.Pause(), PauseMenu.TogglePause()
        #endregion

        // Config
        static private ModSetting<bool> _gameToggle, _playersToggle, _npcsToggle;
        static private ModSetting<int> _defaultGameSpeed, _speedHackMultiplier;
        static private ModSetting<string> _speedHackKey;
        static private ModSetting<int> _playersAnimationSpeed, _playersMovementSpeed, _playersAttackSpeed;
        static private ModSetting<int> _npcsAnimationSpeed, _npcMovementSpeed, _npcAttackSpeed;
        override protected void Initialize()
        {
            _gameToggle = CreateSetting(nameof(_gameToggle), false);
            _defaultGameSpeed = CreateSetting(nameof(_defaultGameSpeed), 100, IntRange(0, 200));
            _speedHackMultiplier = CreateSetting(nameof(_speedHackMultiplier), 300, IntRange(0, 500));
            _speedHackKey = CreateSetting(nameof(_speedHackKey), "");

            _playersToggle = CreateSetting(nameof(_playersToggle), false);
            _playersAnimationSpeed = CreateSetting(nameof(_playersAnimationSpeed), 100, IntRange(0, 200));
            _playersMovementSpeed = CreateSetting(nameof(_playersMovementSpeed), 100, IntRange(0, 200));
            _playersAttackSpeed = CreateSetting(nameof(_playersAttackSpeed), 100, IntRange(0, 200));

            _npcsToggle = CreateSetting(nameof(_npcsToggle), false);
            _npcsAnimationSpeed = CreateSetting(nameof(_npcsAnimationSpeed), 100, IntRange(0, 200));
            _npcMovementSpeed = CreateSetting(nameof(_npcMovementSpeed), 100, IntRange(0, 200));
            _npcAttackSpeed = CreateSetting(nameof(_npcAttackSpeed), 100, IntRange(0, 200));

            AddEventOnConfigClosed(UpdateDefaultGameSpeed);
        }
        override protected void SetFormatting()
        {
            _gameToggle.Format("Game");
            _gameToggle.Description = "Set multipliers (%) for the whole game world";
            Indent++;
            {
                _defaultGameSpeed.Format("Default game speed", _gameToggle);
                _speedHackMultiplier.Format("SpeedHack multiplier", _gameToggle);
                _speedHackMultiplier.Description = "Default game speed is multiplied by this value when speedhack is enabled";
                _speedHackKey.Format("SpeedHack key", _gameToggle);
                _speedHackKey.Description = "Use UnityEngine.KeyCode enum values\n" +
                                            "(https://docs.unity3d.com/ScriptReference/KeyCode.html)";
                Indent--;
            }

            _playersToggle.Format("Players");
            _playersToggle.Description = "Set multipliers (%) players' speeds";
            Indent++;
            {
                _playersAnimationSpeed.Format("All animations", _playersToggle);
                _playersAnimationSpeed.Description = "Includes animations other than moving and attacking\n" +
                                                     "(using skills, using items, dodging, gathering)";
                _playersMovementSpeed.Format("Movement", _playersToggle);
                _playersAttackSpeed.Format("Attack", _playersToggle);
                Indent--;
            }

            _npcsToggle.Format("NPCs");
            _npcsToggle.Description = "Set multipliers (%) NPCs' speeds";
            Indent++;
            {
                _npcsAnimationSpeed.Format("All animations", _npcsToggle);
                _npcsAnimationSpeed.Description = _playersAnimationSpeed.Description;
                _npcMovementSpeed.Format("Movement", _npcsToggle);
                _npcAttackSpeed.Format("Attack", _npcsToggle);
                Indent--;
            }

        }
        override protected string Description
        => "• Change players/NPCs speed multipliers\n" +
           "(all animations, movement, attack)\n" +
           "• Affects FINAL speed, after all reductions and amplifications\n" +
           "• Override default game speed\n" +
           "• Toggle speedhack with a hotkey";
        override protected string SectionOverride
        => SECTION_COMBAT;

        public void OnUpdate()
        {
            if (IsEnabled)
                if (_speedHackKey.Value.ToKeyCode().Pressed())
                    ToggleSpeedHack();
        }

        // Utility
        static private void UpdateDefaultGameSpeed()
        {
            if (Global.GamePaused)
                return;

            Time.timeScale = _defaultGameSpeed / 100f;
            Time.fixedDeltaTime = FIXED_TIME_DELTA * Time.timeScale;
        }
        static private void ToggleSpeedHack()
        {
            if (Global.GamePaused)
                return;

            float defaultSpeed = _defaultGameSpeed / 100f;
            float speedHackSpeed = defaultSpeed * _speedHackMultiplier / 100f;
            if (Time.timeScale < speedHackSpeed)
                Time.timeScale = speedHackSpeed;
            else
                Time.timeScale = defaultSpeed;
            Time.fixedDeltaTime = FIXED_TIME_DELTA * Time.timeScale;
        }
        static private void UpdateAnimationSpeed(Character character)
        {
            if (_playersToggle && character.IsPlayer())
                character.Animator.speed = _playersAnimationSpeed / 100f;
            else if (_npcsToggle && !character.IsPlayer())
                character.Animator.speed = _npcsAnimationSpeed / 100f;
        }

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        [HarmonyPatch(typeof(Character), "LateUpdate"), HarmonyPostfix]
        static void Character_LateUpdate_Post(Character __instance)
        {
            #region quit
            if (!_playersToggle && !_npcsToggle)
                return;
            #endregion
            #region quit2
            if (__instance.Stunned || __instance.IsPetrified)
                return;
            #endregion

            UpdateAnimationSpeed(__instance);
        }

        [HarmonyPatch(typeof(Character), "TempSlowDown"), HarmonyPrefix]
        static bool Character_TempSlowDown_Pre(Character __instance)
        {
            #region quit
            if (!_playersToggle && !_npcsToggle)
                return true;
            #endregion
            #region quit2
            if (__instance.Stunned || __instance.IsPetrified)
                return true;
            #endregion

            UpdateAnimationSpeed(__instance);
            return true;
        }

        [HarmonyPatch(typeof(CharacterStats), "MovementSpeed", MethodType.Getter), HarmonyPostfix]
        static void CharacterStats_MovementSpeed_Getter_Post(ref float __result, Character ___m_character)
        {
            if (_playersToggle && ___m_character.IsPlayer())
                __result *= _playersMovementSpeed / 100f;
            else if (_npcsToggle && !___m_character.IsPlayer())
                __result *= _npcMovementSpeed / 100f;
        }

        [HarmonyPatch(typeof(Weapon), "GetAttackSpeed"), HarmonyPostfix]
        static void Weapon_GetAttackSpeed_Post(Weapon __instance, ref float __result)
        {
            Character owner = __instance.OwnerCharacter;
            if (_playersToggle && owner.IsPlayer())
                __result *= _playersAttackSpeed / 100f;
            else if (_npcsToggle && !owner.IsPlayer())
                __result *= _npcAttackSpeed / 100f;
        }
    }
}

/*

static private ModSetting<string> _pauseKey,

_pauseKey = CreateSetting(nameof(_pauseKey), "");

_pauseKey.Format("Pause key", _gameToggle);
_pauseKey.Description = _speedHackKey.Description;

if (Input.GetKeyDown(_pauseKey.Value.ToKeyCode()))
    PauseMenu.Pause(!Global.GamePaused);

[HarmonyPatch(typeof(PauseMenu), "Pause"), HarmonyPostfix]
static void PauseMenu_Pause_Post()
{
    #region quit
    if (!_gameToggle)
        return;
    #endregion

    UpdateDefaultGameSpeed();
}

[HarmonyPatch(typeof(PauseMenu), "TogglePause"), HarmonyPostfix]
static void PauseMenu_TogglePause_Post()
{
    #region quit
    if (!_gameToggle)
        return;
    #endregion

    UpdateDefaultGameSpeed();
}
*/