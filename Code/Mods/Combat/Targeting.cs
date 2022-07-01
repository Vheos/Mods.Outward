namespace Vheos.Mods.Outward;

public class Targeting : AMod
{

    #region Settings
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
                _targetingPitchOffset.Value = 0.25f;
                break;
        }
    }
    #endregion

    #region Formatting
    protected override string SectionOverride
        => ModSections.Combat;
    protected override string Description
        => "• Set targeting distance by weapon type\n" +
        "• Auto-target when performing chosen actions\n" +
        "• Tilt targeting camera";
    protected override void SetFormatting()
    {
        _meleeDistance.Format("Melee distance");
        _meleeDistance.Description =
            "From how far away you can target an enemy when using a melee weapon" +
            "\n\nUnit: in-game length units";
        _rangedDistance.Format("Ranged distance");
        _rangedDistance.Description =
            "From how far away you can target an enemy when using ranged equipment" +
            "\n\nUnit: in-game length units";
        using (Indent)
        {
            _huntersEyeDistance.Format("with Hunter's Eye");
            _huntersEyeDistance.Description =
                $"If you have the Huner's Eye passive skill, this value will be used in place of \"{_rangedDistance.Name}\"";
            _rangedEquipmentTypes.Format("compatible equipment");
            _rangedEquipmentTypes.Description =
                $"What equipment is considered to be ranged by the \"{_rangedDistance.Name}\" setting?";
        }
        _autoTargetActions.Format("Auto-target actions");
        _autoTargetActions.Description =
            "Allows you to automatically target the closest enemy whenever you perform any of the chosen actions while not already targeting";
        _targetingPitchOffset.Format("Targeting tilt");
        _targetingPitchOffset.Description =
            "Tilts the camera when you're targeting, giving you a bit more \"top-down\" view" +
            "\n\nUnit: arbitrary linear scale";
    }
    #endregion

    #region Utility
    private static bool HasHuntersEye(Character character)
        => character.Inventory.SkillKnowledge.IsItemLearned("Hunter's Eye".ToSkillID());
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

    #region Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(CharacterCamera), nameof(CharacterCamera.LateUpdate))]
    private static void CharacterCamera_LateUpdate_Post(CharacterCamera __instance)
    {
        if (__instance.m_targetCharacter.TargetingSystem.LockedCharacter != null)
            __instance.m_cameraVertHolder.rotation *= Quaternion.Euler(_targetingPitchOffset, 0, 0);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TargetingSystem), nameof(TargetingSystem.TrueRange), MethodType.Getter)]
    private static bool TargetingSystem_TrueRange_Pre(TargetingSystem __instance, ref float __result)
    {
        __result = !HasRangedEquipment(__instance.m_character) ? _meleeDistance
            : HasHuntersEye(__instance.m_character) ? _huntersEyeDistance
            : _rangedDistance;
        return false;
    }

    // Auto-target
    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.AttackInput))]
    private static void Character_AttackInput_Post(Character __instance)
    {
        if (!__instance.CharacterControl.TryAs(out LocalCharacterControl localCharacterControl)
        || __instance.TargetingSystem.Locked
        || !_autoTargetActions.Value.HasFlag(AutoTargetActions.Attack))
            return;

        localCharacterControl.AcquireTarget();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.SetLastUsedSkill))]
    private static void Character_SetLastUsedSkill_Post(Character __instance, ref Skill _skill)
    {
        if (!__instance.CharacterControl.TryAs(out LocalCharacterControl localCharacterControl)
        || __instance.TargetingSystem.Locked
        || !_autoTargetActions.Value.HasFlag(AutoTargetActions.CombatSkill))
            return;

        localCharacterControl.AcquireTarget();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.BlockInput))]
    private static void Character_BlockInput_Post(Character __instance, ref bool _active)
    {
        if (!__instance.CharacterControl.TryAs(out LocalCharacterControl localCharacterControl)
        || __instance.TargetingSystem.Locked
        || !_autoTargetActions.Value.HasFlag(AutoTargetActions.Block))
            return;

        localCharacterControl.AcquireTarget();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.DodgeInput), new[] { typeof(Vector3) })]
    private static void Character_DodgeInput_Post(Character __instance)
    {
        if (!__instance.CharacterControl.TryAs(out LocalCharacterControl localCharacterControl)
        || __instance.TargetingSystem.Locked
        || !_autoTargetActions.Value.HasFlag(AutoTargetActions.Dodge))
            return;

        localCharacterControl.AcquireTarget();
    }
    #endregion
}
