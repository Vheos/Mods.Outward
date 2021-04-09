using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace ModPack
{
    public class Targeting : AMod
    {
        #region const
        public const int HUNTERS_EYE_ID = 8205160;
        #endregion
        #region enum
        [Flags]
        private enum RangedTypes
        {
            None = 0,
            Bow = 1 << 1,
            Pistol = 1 << 2,
            Chakram = 1 << 3,
            Lexicon = 1 << 4,
        }
        [Flags]
        private enum AutoTargetEvents
        {
            None = 0,
            Attack = 1 << 1,
            CombatSkill = 1 << 2,
            Block = 1 << 3,
            Dodge = 1 << 4,
        }
        #endregion

        // Setting
        static private ModSetting<int> _meleeDistance, _rangedDistance, _huntersEyeDistance;
        static private ModSetting<RangedTypes> _rangedEquipmentTypes;
        static private ModSetting<AutoTargetEvents> _autoTargetEvents;
        static private ModSetting<float> _targetingPitchOffset;
        static private ModSetting<bool> _allowTargetingPlayers;
        override protected void Initialize()
        {
            _meleeDistance = CreateSetting(nameof(_meleeDistance), 20, IntRange(0, 100));
            _rangedDistance = CreateSetting(nameof(_rangedDistance), 20, IntRange(0, 100));
            _huntersEyeDistance = CreateSetting(nameof(_huntersEyeDistance), 40, IntRange(0, 100));
            _rangedEquipmentTypes = CreateSetting(nameof(_rangedEquipmentTypes), RangedTypes.Bow);
            _autoTargetEvents = CreateSetting(nameof(_autoTargetEvents), AutoTargetEvents.None);
            _targetingPitchOffset = CreateSetting(nameof(_targetingPitchOffset), 0f, FloatRange(0, 1));
            _allowTargetingPlayers = CreateSetting(nameof(_allowTargetingPlayers), false);
        }
        override protected void SetFormatting()
        {
            _meleeDistance.Format("Melee distance");
            _rangedDistance.Format("Ranged distance");
            _huntersEyeDistance.Format("Hunter's Eye distance");
            _rangedEquipmentTypes.Format("Ranged equipment types");
            _autoTargetEvents.Format("Auto-target events");
            _targetingPitchOffset.Format("Targeting pitch offset");
            _allowTargetingPlayers.Format("Allow targeting players");
        }

        // Utility
        static private bool HasHuntersEye(Character character)
        => character.Inventory.SkillKnowledge.IsItemLearned(HUNTERS_EYE_ID);
        static private bool HasRangedEquipment(Character character)
        => _rangedEquipmentTypes.Value.HasFlag(RangedTypes.Bow) && HasBow(character)
        || _rangedEquipmentTypes.Value.HasFlag(RangedTypes.Pistol) && HasPistol(character)
        || _rangedEquipmentTypes.Value.HasFlag(RangedTypes.Chakram) && HasChakram(character)
        || _rangedEquipmentTypes.Value.HasFlag(RangedTypes.Lexicon) && HasLexicon(character);
        static private bool HasBow(Character character)
        => character.m_currentWeapon != null && character.m_currentWeapon.Type == Weapon.WeaponType.Bow;
        static private bool HasPistol(Character character)
        => character.LeftHandWeapon != null && character.LeftHandWeapon.Type == Weapon.WeaponType.Pistol_OH;
        static private bool HasChakram(Character character)
        => character.LeftHandWeapon != null && character.LeftHandWeapon.Type == Weapon.WeaponType.Chakram_OH;
        static private bool HasLexicon(Character character)
        => character.LeftHandEquipment != null && character.LeftHandEquipment.IKType == Equipment.IKMode.Lexicon;

        // Hooks
        [HarmonyPatch(typeof(CharacterCamera), "LateUpdate"), HarmonyPostfix]
        static void CharacterCamera_LateUpdate_Post(ref CharacterCamera __instance)
        {
            if (__instance.m_targetCharacter.TargetingSystem.Locked)
                __instance.m_cameraVertHolder.rotation *= Quaternion.Euler(_targetingPitchOffset, 0, 0);
        }

        [HarmonyPatch(typeof(TargetingSystem), "TrueRange", MethodType.Getter), HarmonyPrefix]
        static bool TargetingSystem_TrueRange_Pre(ref TargetingSystem __instance, ref float __result)
        {
            Character character = __instance.m_character;
            if (HasRangedEquipment(character))
                if (HasHuntersEye(character))
                    __result = _huntersEyeDistance;
                else
                    __result = _rangedDistance;
            else
                __result = _meleeDistance;
            return false;
        }

        [HarmonyPatch(typeof(TargetingSystem), "IsTargetable", new[] { typeof(Character) }), HarmonyPrefix]
        static bool TargetingSystem_IsTargetable_Pre(ref TargetingSystem __instance, ref bool __result, ref Character _char)
        {
            #region quit
            if (!_allowTargetingPlayers)
                return true;
            #endregion

            if (_char.Faction == Character.Factions.Player && _char != __instance.m_character)
            {
                __result = true;
                return false;
            }
            return true;
        }

        // Auto-target
        [HarmonyPatch(typeof(Character), "AttackInput"), HarmonyPostfix]
        static void Character_AttackInput_Post(ref Character __instance)
        {
            #region quit
            if (!_autoTargetEvents.Value.HasFlag(AutoTargetEvents.Attack) || __instance.TargetingSystem.Locked)
                return;
            #endregion

            __instance.CharacterControl.As<LocalCharacterControl>().AcquireTarget();
        }

        [HarmonyPatch(typeof(Character), "SetLastUsedSkill"), HarmonyPostfix]
        static void Character_SetLastUsedSkill_Post(ref Character __instance, ref Skill _skill)
        {
            #region quit
            if (!_autoTargetEvents.Value.HasFlag(AutoTargetEvents.CombatSkill) || __instance.TargetingSystem.Locked || _skill.IsNot<AttackSkill>())
                return;
            #endregion

            __instance.CharacterControl.As<LocalCharacterControl>().AcquireTarget();
        }

        [HarmonyPatch(typeof(Character), "BlockInput"), HarmonyPostfix]
        static void Character_BlockInput_Post(ref Character __instance, ref bool _active)
        {
            #region quit
            if (!_autoTargetEvents.Value.HasFlag(AutoTargetEvents.Block) || __instance.TargetingSystem.Locked || !_active)
                return;
            #endregion

            __instance.CharacterControl.As<LocalCharacterControl>().AcquireTarget();
        }

        [HarmonyPatch(typeof(Character), "DodgeInput", new[] { typeof(Vector3) }), HarmonyPostfix]
        static void Character_DodgeInput_Post(ref Character __instance)
        {
            #region quit
            if (!_autoTargetEvents.Value.HasFlag(AutoTargetEvents.Dodge) || __instance.TargetingSystem.Locked)
                return;
            #endregion

            __instance.CharacterControl.As<LocalCharacterControl>().AcquireTarget();
        }
    }
}