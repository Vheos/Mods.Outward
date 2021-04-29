using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



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
        private enum AutoTargetActions
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
        static private ModSetting<AutoTargetActions> _autoTargetActions;
        static private ModSetting<float> _targetingPitchOffset;
        override protected void Initialize()
        {
            _meleeDistance = CreateSetting(nameof(_meleeDistance), 20, IntRange(0, 100));
            _rangedDistance = CreateSetting(nameof(_rangedDistance), 20, IntRange(0, 100));
            _huntersEyeDistance = CreateSetting(nameof(_huntersEyeDistance), 40, IntRange(0, 100));
            _rangedEquipmentTypes = CreateSetting(nameof(_rangedEquipmentTypes), RangedTypes.Bow);
            _autoTargetActions = CreateSetting(nameof(_autoTargetActions), AutoTargetActions.None);
            _targetingPitchOffset = CreateSetting(nameof(_targetingPitchOffset), 0f, FloatRange(0, 1));
        }
        override protected void SetFormatting()
        {
            _meleeDistance.Format("Melee distance");
            _meleeDistance.Description = "Targeting distance for all melee weapons";
            _rangedDistance.Format("Ranged distance");
            _rangedDistance.Description = "Targeting distance for all weapons specified in the \"Ranged equipment\" setting";
            _huntersEyeDistance.Format("Hunter's Eye distance");
            _huntersEyeDistance.Description = "If you have Hunter's Eye, this is used instead of \"Ranged distance\"";
            _rangedEquipmentTypes.Format("Ranged equipment");
            _rangedEquipmentTypes.Description = "What equipment should use the \"Ranged distance\" setting and benefit from Hunter's Eye?";
            _autoTargetActions.Format("Auto-target actions");
            _autoTargetActions.Description = "If you do any of these actions while not locked-on, you will automatically target the closest enemy";
            _targetingPitchOffset.Format("Targeting tilt");
            _targetingPitchOffset.Description = "When you're targeting, the camera will be tilted a little to give more \"top-down\" view\n" +
                                                "This way the enemy won't be obscured by your character, especially if you're wearing a big helmet";
        }
        override protected string Description
        => "• Set targeting distance by weapon type\n" +
           "• Auto-target on specific actions\n" +
           "• Tilt targeting camera";
        override protected string SectionOverride
        => SECTION_COMBAT;

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
            if (__instance.m_targetCharacter.TargetingSystem.LockedCharacter != null)
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

        // Auto-target
        [HarmonyPatch(typeof(Character), "AttackInput"), HarmonyPostfix]
        static void Character_AttackInput_Post(ref Character __instance)
        {
            #region quit
            if (!_autoTargetActions.Value.HasFlag(AutoTargetActions.Attack) || __instance.TargetingSystem.Locked)
                return;
            #endregion

            __instance.CharacterControl.As<LocalCharacterControl>().AcquireTarget();
        }

        [HarmonyPatch(typeof(Character), "SetLastUsedSkill"), HarmonyPostfix]
        static void Character_SetLastUsedSkill_Post(ref Character __instance, ref Skill _skill)
        {
            #region quit
            if (!_autoTargetActions.Value.HasFlag(AutoTargetActions.CombatSkill) || __instance.TargetingSystem.Locked || _skill.IsNot<AttackSkill>())
                return;
            #endregion

            __instance.CharacterControl.As<LocalCharacterControl>().AcquireTarget();
        }

        [HarmonyPatch(typeof(Character), "BlockInput"), HarmonyPostfix]
        static void Character_BlockInput_Post(ref Character __instance, ref bool _active)
        {
            #region quit
            if (!_autoTargetActions.Value.HasFlag(AutoTargetActions.Block) || __instance.TargetingSystem.Locked || !_active || HasBow(__instance))
                return;
            #endregion

            __instance.CharacterControl.As<LocalCharacterControl>().AcquireTarget();
        }

        [HarmonyPatch(typeof(Character), "DodgeInput", new[] { typeof(Vector3) }), HarmonyPostfix]
        static void Character_DodgeInput_Post(ref Character __instance)
        {
            #region quit
            if (!_autoTargetActions.Value.HasFlag(AutoTargetActions.Dodge) || __instance.TargetingSystem.Locked)
                return;
            #endregion

            __instance.CharacterControl.As<LocalCharacterControl>().AcquireTarget();
        }
    }
}