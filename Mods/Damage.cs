using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;



/* TO DO:
 * - FriendlyFire: include bows, pistols and spells
 */
namespace ModPack
{
    public class Damage : AMod
    {
        // Config
        static private ModSetting<bool> _playersToggle, _npcsToggle, _friendlyFireToggle;
        static private ModSetting<int> _playersHealthDamage, _npcsHealthDamage, _friendlyFireHealthDamage;
        static private ModSetting<int> _playersStabilityDamage, _npcsStabilityDamage, _friendlyFireStabilityDamage;
        override protected void Initialize()
        {
            _friendlyFireToggle = CreateSetting(nameof(_friendlyFireToggle), false);
            _friendlyFireHealthDamage = CreateSetting(nameof(_friendlyFireHealthDamage), 100, IntRange(0, 200));
            _friendlyFireStabilityDamage = CreateSetting(nameof(_friendlyFireStabilityDamage), 100, IntRange(0, 200));

            _playersToggle = CreateSetting(nameof(_playersToggle), false);
            _playersHealthDamage = CreateSetting(nameof(_playersHealthDamage), 100, IntRange(0, 200));
            _playersStabilityDamage = CreateSetting(nameof(_playersStabilityDamage), 100, IntRange(0, 200));

            _npcsToggle = CreateSetting(nameof(_npcsToggle), false);
            _npcsHealthDamage = CreateSetting(nameof(_npcsHealthDamage), 100, IntRange(0, 200));
            _npcsStabilityDamage = CreateSetting(nameof(_npcsStabilityDamage), 100, IntRange(0, 200));
        }
        override protected void SetFormatting()
        {
            _playersToggle.Format("Players");
            _playersToggle.Description = "Set multipliers (%) for damage dealt by players";
            Indent++;
            {
                _playersHealthDamage.Format("Health", _playersToggle);
                _playersStabilityDamage.Format("Stability", _playersToggle);
                Indent--;
            }

            _npcsToggle.Format("NPCs");
            _npcsToggle.Description = "Set multipliers (%) for damage dealt by NPCs";
            Indent++;
            {
                _npcsHealthDamage.Format("Health", _npcsToggle);
                _npcsStabilityDamage.Format("Stability", _npcsToggle);
                Indent--;
            }

            _friendlyFireToggle.Format("Friendly fire");
            _friendlyFireToggle.Description = "Allow players to hit (but not target) each other\n" +
                                              "Set multipliers (%) for friendly fire damage";
            Indent++;
            {
                _friendlyFireHealthDamage.Format("Health", _friendlyFireToggle);
                _friendlyFireStabilityDamage.Format("Stability", _friendlyFireToggle);
                Indent--;
            }
        }
        override protected string Description
        => "• Change players and NPCs damage multipliers\n" +
           "(health, stability)\n" +
           "• Affects FINAL damage, after all reductions and amplifications\n" +
           "• Enable friendly fire between players";

        // Hooks
        [HarmonyPatch(typeof(Weapon), "ElligibleFaction", new[] { typeof(Character) }), HarmonyPostfix]
        static void Weapon_ElligibleFaction_Post(ref Weapon __instance, ref bool __result, Character _character)
        {
            #region quit
            if (!_friendlyFireToggle || _character == null)
                return;
            #endregion

            __result |= _character.IsPlayer() && !_character.IsOwnerOf(__instance);
        }

        [HarmonyPatch(typeof(MeleeHitDetector), "ElligibleFaction", new[] { typeof(Character) }), HarmonyPostfix]
        static void MeleeHitDetector_ElligibleFaction_Post(ref MeleeHitDetector __instance, ref bool __result, Character _character)
        {
            #region quit
            if (!_friendlyFireToggle || _character == null)
                return;
            #endregion

            __result |= _character.IsPlayer() && !_character.IsOwnerOf(__instance);
        }

        [HarmonyPatch(typeof(Character), "VitalityHit"), HarmonyPrefix]
        static bool Character_VitalityHit_Pre(ref Character __instance, Character _dealerChar, ref float _damage)
        {
            #region quit
            if (_dealerChar == null)
                return true;
            #endregion

            if (_dealerChar.IsPlayer())
            {
                if (_playersToggle)
                    _damage *= _playersHealthDamage / 100f;
                if (_friendlyFireToggle && __instance.IsPlayer())
                    _damage *= _friendlyFireHealthDamage / 100f;
            }
            else if (_npcsToggle)
                _damage *= _npcsHealthDamage / 100f;

            return true;
        }

        [HarmonyPatch(typeof(Character), "StabilityHit"), HarmonyPrefix]
        static bool Character_StabilityHit_Pre(ref Character __instance, Character _dealerChar, ref float _knockValue)
        {
            #region quit
            if (_dealerChar == null)
                return true;
            #endregion

            if (_dealerChar.IsPlayer())
            {
                if (_playersToggle)
                    _knockValue *= _playersStabilityDamage / 100f;
                if (_friendlyFireToggle && __instance.IsPlayer())
                    _knockValue *= _friendlyFireStabilityDamage / 100f;
            }
            else if (_npcsToggle)
                _knockValue *= _npcsStabilityDamage / 100f;

            return true;
        }
    }
}