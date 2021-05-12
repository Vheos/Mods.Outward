using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



/* TO DO:
 * - FriendlyFire: include bows, pistols and spells
 */
namespace ModPack
{
    public class Damage : AMod
    {
        // Config
        static private ModSetting<bool> _playersToggle, _enemiesToggle, _playersFriendlyFireToggle, _enemiesFriendlyFireToggle;
        static private ModSetting<int> _playersHealthDamage, _enemiesHealthDamage, _playersFriendlyFireHealthDamage, _enemiesFriendlyFireHealthDamage;
        static private ModSetting<int> _playersStabilityDamage, _enemiesStabilityDamage, _playersFriendlyFireStabilityDamage, _enemiesFriendlyFireStabilityDamage;
        override protected void Initialize()
        {
            _playersToggle = CreateSetting(nameof(_playersToggle), false);
            _playersHealthDamage = CreateSetting(nameof(_playersHealthDamage), 100, IntRange(0, 200));
            _playersStabilityDamage = CreateSetting(nameof(_playersStabilityDamage), 100, IntRange(0, 200));
            _playersFriendlyFireToggle = CreateSetting(nameof(_playersFriendlyFireToggle), false);
            _playersFriendlyFireHealthDamage = CreateSetting(nameof(_playersFriendlyFireHealthDamage), 100, IntRange(0, 200));
            _playersFriendlyFireStabilityDamage = CreateSetting(nameof(_playersFriendlyFireStabilityDamage), 100, IntRange(0, 200));

            _enemiesToggle = CreateSetting(nameof(_enemiesToggle), false);
            _enemiesHealthDamage = CreateSetting(nameof(_enemiesHealthDamage), 100, IntRange(0, 200));
            _enemiesStabilityDamage = CreateSetting(nameof(_enemiesStabilityDamage), 100, IntRange(0, 200));
            _enemiesFriendlyFireToggle = CreateSetting(nameof(_enemiesFriendlyFireToggle), false);
            _enemiesFriendlyFireHealthDamage = CreateSetting(nameof(_enemiesFriendlyFireHealthDamage), 100, IntRange(0, 200));
            _enemiesFriendlyFireStabilityDamage = CreateSetting(nameof(_enemiesFriendlyFireStabilityDamage), 100, IntRange(0, 200));
        }
        override protected void SetFormatting()
        {
            _playersToggle.Format("Players");
            _playersToggle.Description = "Set multipliers for damage dealt by players";
            Indent++;
            {
                _playersHealthDamage.Format("Health", _playersToggle);
                _playersStabilityDamage.Format("Stability", _playersToggle);
                _playersFriendlyFireToggle.Format("Friendly fire", _playersToggle);
                _playersFriendlyFireToggle.Description = "Set multipliers for damage dealt by players to other players\n" +
                                                         "(multiplicative with above values)";
                Indent++;
                {
                    _playersFriendlyFireHealthDamage.Format("Health", _playersFriendlyFireToggle);
                    _playersFriendlyFireStabilityDamage.Format("Stability", _playersFriendlyFireToggle);
                    Indent--;
                }
                Indent--;
            }

            _enemiesToggle.Format("Enemies");
            _enemiesToggle.Description = "Set multipliers for damage dealt by enemies";
            Indent++;
            {
                _enemiesHealthDamage.Format("Health", _enemiesToggle);
                _enemiesStabilityDamage.Format("Stability", _enemiesToggle);
                _enemiesFriendlyFireToggle.Format("Friendly fire", _enemiesToggle);
                _enemiesFriendlyFireToggle.Description = "Set multipliers for damage dealt by enemies to other enemies\n" +
                                                         "Decrease to prevent enemies from killing each other before you meet them\n" +
                                                         "(multiplicative with above values)";
                Indent++;
                {
                    _enemiesFriendlyFireHealthDamage.Format("Health", _enemiesFriendlyFireToggle);
                    _enemiesFriendlyFireStabilityDamage.Format("Stability", _enemiesFriendlyFireToggle);
                    Indent--;
                }
                Indent--;
            }
        }
        override protected string Description
        => "• Change players and NPCs damage multipliers\n" +
           "(health, stability)\n" +
           "• Affects FINAL damage, after all reductions and amplifications\n" +
           "• Enable friendly fire between players";
        override protected string SectionOverride
        => SECTION_COMBAT;

        // Hooks
        [HarmonyPatch(typeof(Weapon), "ElligibleFaction", new[] { typeof(Character) }), HarmonyPostfix]
        static void Weapon_ElligibleFaction_Post(Weapon __instance, ref bool __result, Character _character)
        {
            #region quit
            if (!_playersFriendlyFireToggle || _character == null)
                return;
            #endregion

            __result |= _character.IsPlayer() && !_character.IsOwnerOf(__instance);
        }

        [HarmonyPatch(typeof(MeleeHitDetector), "ElligibleFaction", new[] { typeof(Character) }), HarmonyPostfix]
        static void MeleeHitDetector_ElligibleFaction_Post(MeleeHitDetector __instance, ref bool __result, Character _character)
        {
            #region quit
            if (!_playersFriendlyFireToggle || _character == null)
                return;
            #endregion

            __result |= _character.IsPlayer() && !_character.IsOwnerOf(__instance);
        }

        [HarmonyPatch(typeof(Character), "OnReceiveHitCombatEngaged"), HarmonyPrefix]
        static bool Character_OnReceiveHitCombatEngaged_Pre(Character __instance, ref Character _dealerChar)
        => !_playersFriendlyFireToggle || _dealerChar == null || !_dealerChar.IsPlayer();


        [HarmonyPatch(typeof(Character), "VitalityHit"), HarmonyPrefix]
        static bool Character_VitalityHit_Pre(Character __instance, Character _dealerChar, ref float _damage)
        {
            if (_dealerChar != null && _dealerChar.IsEnemy()
            || _dealerChar == null && __instance.IsPlayer())
            {
                if (_enemiesToggle)
                    _damage *= _enemiesHealthDamage / 100f;
                if (__instance.IsEnemy())
                    _damage *= _enemiesFriendlyFireHealthDamage / 100f;
            }
            else
            {
                if (_playersToggle)
                    _damage *= _playersHealthDamage / 100f;
                if (_playersFriendlyFireToggle && __instance.IsPlayer())
                    _damage *= _playersFriendlyFireHealthDamage / 100f;
            }

            return true;
        }

        [HarmonyPatch(typeof(Character), "StabilityHit"), HarmonyPrefix]
        static bool Character_StabilityHit_Pre(Character __instance, Character _dealerChar, ref float _knockValue)
        {
            if (_dealerChar != null && _dealerChar.IsEnemy()
            || _dealerChar == null && __instance.IsPlayer())
            {
                if (_enemiesToggle)
                    _knockValue *= _enemiesStabilityDamage / 100f;
                if (__instance.IsEnemy())
                    _knockValue *= _enemiesFriendlyFireStabilityDamage / 100f;
            }
            else
            {
                if (_playersToggle)
                    _knockValue *= _playersStabilityDamage / 100f;
                if (_playersFriendlyFireToggle && __instance.IsPlayer())
                    _knockValue *= _playersFriendlyFireStabilityDamage / 100f;
            }

            return true;
        }
    }
}