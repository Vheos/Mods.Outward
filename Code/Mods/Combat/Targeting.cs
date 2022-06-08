namespace Vheos.Mods.Outward;

public class Targeting : AMod
{
    #region Constants
    public const int HUNTERS_EYE_ID = 8205160;
    #endregion
    #region Enums
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
    private static ModSetting<int> _meleeDistance, _rangedDistance, _huntersEyeDistance;
    private static ModSetting<RangedTypes> _rangedEquipmentTypes;
    private static ModSetting<AutoTargetActions> _autoTargetActions;
    private static ModSetting<float> _targetingPitchOffset;
    protected override void Initialize()
    {
        _meleeDistance = CreateSetting(nameof(_meleeDistance), 20, IntRange(0, 100));
        _rangedDistance = CreateSetting(nameof(_rangedDistance), 20, IntRange(0, 100));
        _huntersEyeDistance = CreateSetting(nameof(_huntersEyeDistance), 40, IntRange(0, 100));
        _rangedEquipmentTypes = CreateSetting(nameof(_rangedEquipmentTypes), RangedTypes.Bow);
        _autoTargetActions = CreateSetting(nameof(_autoTargetActions), AutoTargetActions.None);
        _targetingPitchOffset = CreateSetting(nameof(_targetingPitchOffset), 0f, FloatRange(0, 1));
    }
    protected override void SetFormatting()
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
    protected override string Description
    => "• Set targeting distance by weapon type\n" +
       "• Auto-target on specific actions\n" +
       "• Tilt targeting camera";
    protected override string SectionOverride
    => ModSections.Combat;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _meleeDistance.Value = 20;
                _rangedDistance.Value = 30;
                _huntersEyeDistance.Value = 45;
                _rangedEquipmentTypes.Value = (RangedTypes)~0;
                _autoTargetActions.Value = AutoTargetActions.Attack | AutoTargetActions.CombatSkill;
                _targetingPitchOffset.Value = 0.2f;
                break;
        }
    }

    // Utility
    private static bool HasHuntersEye(Character character)
    => character.Inventory.SkillKnowledge.IsItemLearned(HUNTERS_EYE_ID);
    private static bool HasRangedEquipment(Character character)
    => _rangedEquipmentTypes.Value.HasFlag(RangedTypes.Bow) && HasBow(character)
    || _rangedEquipmentTypes.Value.HasFlag(RangedTypes.Pistol) && HasPistol(character)
    || _rangedEquipmentTypes.Value.HasFlag(RangedTypes.Chakram) && HasChakram(character)
    || _rangedEquipmentTypes.Value.HasFlag(RangedTypes.Lexicon) && HasLexicon(character);
    private static bool HasBow(Character character)
    => character.m_currentWeapon != null && character.m_currentWeapon.Type == Weapon.WeaponType.Bow;
    private static bool HasPistol(Character character)
    => character.LeftHandWeapon != null && character.LeftHandWeapon.Type == Weapon.WeaponType.Pistol_OH;
    private static bool HasChakram(Character character)
    => character.LeftHandWeapon != null && character.LeftHandWeapon.Type == Weapon.WeaponType.Chakram_OH;
    private static bool HasLexicon(Character character)
    => character.LeftHandEquipment != null && character.LeftHandEquipment.IKType == Equipment.IKMode.Lexicon;

    // Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(CharacterCamera), nameof(CharacterCamera.LateUpdate))]
    private static void CharacterCamera_LateUpdate_Post(CharacterCamera __instance)
    {
        if (__instance.m_targetCharacter.TargetingSystem.LockedCharacter != null)
            __instance.m_cameraVertHolder.rotation *= Quaternion.Euler(_targetingPitchOffset, 0, 0);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TargetingSystem), nameof(TargetingSystem.TrueRange), MethodType.Getter)]
    private static bool TargetingSystem_TrueRange_Pre(TargetingSystem __instance, ref float __result)
    {
        Character character = __instance.m_character;
        __result = !HasRangedEquipment(character) ? (float)_meleeDistance
            : HasHuntersEye(character) ? (float)_huntersEyeDistance
            : (float)_rangedDistance;
        return false;
    }

    // Auto-target
    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.AttackInput))]
    private static void Character_AttackInput_Post(Character __instance)
    {
        #region quit
        if (!__instance.CharacterControl.TryAs(out LocalCharacterControl localCharacterControl) || __instance.TargetingSystem.Locked
        || !_autoTargetActions.Value.HasFlag(AutoTargetActions.Attack))
            return;
        #endregion

        localCharacterControl.AcquireTarget();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.SetLastUsedSkill))]
    private static void Character_SetLastUsedSkill_Post(Character __instance, ref Skill _skill)
    {
        #region quit
        if (!__instance.CharacterControl.TryAs(out LocalCharacterControl localCharacterControl) || __instance.TargetingSystem.Locked
        || !_autoTargetActions.Value.HasFlag(AutoTargetActions.CombatSkill))
            return;
        #endregion

        localCharacterControl.AcquireTarget();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.BlockInput))]
    private static void Character_BlockInput_Post(Character __instance, ref bool _active)
    {
        #region quit
        if (!__instance.CharacterControl.TryAs(out LocalCharacterControl localCharacterControl) || __instance.TargetingSystem.Locked
        || !_autoTargetActions.Value.HasFlag(AutoTargetActions.Block))
            return;
        #endregion

        localCharacterControl.AcquireTarget();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.DodgeInput), new[] { typeof(Vector3) })]
    private static void Character_DodgeInput_Post(Character __instance)
    {
        #region quit
        if (!__instance.CharacterControl.TryAs(out LocalCharacterControl localCharacterControl) || __instance.TargetingSystem.Locked
        || !_autoTargetActions.Value.HasFlag(AutoTargetActions.Dodge))
            return;
        #endregion

        localCharacterControl.AcquireTarget();
    }
}
