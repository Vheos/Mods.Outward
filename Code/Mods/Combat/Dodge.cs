namespace Vheos.Mods.Outward;

public class Dodge : AMod
{
    #region Settings
    private static ModSetting<int> _staminaCost;
    private static ModSetting<int> _staminaCostWithAcrobatics;
    private static ModSetting<bool> _allowMidAttack;
    private static ModSetting<bool> _allowMidAttackUntilDamageDealt;
    private static ModSetting<bool> _allowMidAttackUntilDamageTaken;
    private static ModSetting<bool> _invincibility;
    protected override void Initialize()
    {
        _staminaCost = CreateSetting(nameof(_staminaCost), 6, IntRange(0, 50));
        _staminaCostWithAcrobatics = CreateSetting(nameof(_staminaCostWithAcrobatics), 9, IntRange(0, 50));
        _allowMidAttack = CreateSetting(nameof(_allowMidAttack), false);
        _allowMidAttackUntilDamageDealt = CreateSetting(nameof(_allowMidAttackUntilDamageTaken), false);
        _allowMidAttackUntilDamageTaken = CreateSetting(nameof(_allowMidAttackUntilDamageDealt), false);
        _invincibility = CreateSetting(nameof(_invincibility), true);
    }
    #endregion

    #region Formatting
    protected override string SectionOverride
=> ModSections.Combat;
    protected override string Description
        => "• Adjust stamina cost" +
        "\n• Allow dodging during attack animation" +
        "\n• Remove dodge invincibility";
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _staminaCost.Value = 6;
                _staminaCostWithAcrobatics.Value = 9;
                _allowMidAttack.Value = true;
                _allowMidAttackUntilDamageDealt.Value = true;
                _allowMidAttackUntilDamageTaken.Value = true;
                _invincibility.Value = false;
                break;
        }
    }
    protected override void SetFormatting()
    {
        _staminaCost.Format("Stamina cost");
        _staminaCost.Description =
            "How much stamina dodging costs" +
            "\n\nUnit: stamina points";
        using (Indent)
        {
            _staminaCostWithAcrobatics.Format("with acrobatics");
            _staminaCostWithAcrobatics.Description =
                "How much stamina dodging costs when you have the Acrobatics passive skill" +
                "\n\nUnit: stamina points";
        }
        _allowMidAttack.Format("Allow mid-attack");
        _allowMidAttack.Description =
            "Allows you to dodge even if you're in the middle of an attack animation";
        using (Indent)
        {
            _allowMidAttackUntilDamageDealt.Format("until you deal damage", _allowMidAttack);
            _allowMidAttackUntilDamageDealt.Description =
                "Prevents using mid-attack dodge after you deal damage" +
                "\nLasts only until you start a new attack";
            _allowMidAttackUntilDamageTaken.Format("until you take damage", _allowMidAttack);
            _allowMidAttackUntilDamageTaken.Description =
                "Prevents using mid-attack dodge after you take damage" +
                "\nLasts only until you start a new attack";
        }
        _invincibility.Format("Invincibility");
        _invincibility.Description =
            "Makes you invincible for roughly 500ms while performing a normal dodge (not slowed down by backpack)";
    }
    #endregion

    #region Hooks
    // Mid-attack dodge
    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.StartAttack))]
    private static void Character_StartAttack_Post(Character __instance, int _type, int _id)
    {
        if (!_allowMidAttack
        || !__instance.IsPlayer())
            return;

        __instance.m_dodgeAllowedInAction = (_type is 0 or 1).To01();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.OnReceiveHit))]
    private static void Character_OnReceiveHit_Post(Character __instance, Character _dealerChar)
    {
        if (!_allowMidAttack)
            return;

        if (_allowMidAttackUntilDamageTaken
        && __instance != null
        && __instance.IsPlayer())
            __instance.m_dodgeAllowedInAction = 0;

        if (_allowMidAttackUntilDamageDealt
        && _dealerChar != null
        && _dealerChar.IsPlayer())
            _dealerChar.m_dodgeAllowedInAction = 0;
    }


    [HarmonyPrefix, HarmonyPatch(typeof(Interactions.InteractionTakeAnimated), nameof(Interactions.InteractionTakeAnimated.OnActivate))]
    private static void InteractionTakeAnimated_OnActivate_Pre(Interactions.InteractionTakeAnimated __instance)
    => __instance.LastCharacter.m_dodgeAllowedInAction = 0;

    // Remove dodge invulnerability
    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.DodgeStep))]
    private static void Character_DodgeStep_Post(ref Hitbox[] ___m_hitboxes, ref int _step)
    {
        if (_invincibility)
            return;

        if (_step > 0 && ___m_hitboxes != null)
            foreach (var hitbox in ___m_hitboxes)
                hitbox.gameObject.SetActive(true);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.DodgeStamCost), MethodType.Getter)]
    private static void Character_DodgeStamCost_Getter_Post(Character __instance, ref int __result)
        => __result = __instance.Inventory.SkillKnowledge.IsItemLearned("Acrobatics".ToSkillID())
        ? _staminaCostWithAcrobatics
        : _staminaCost;
    #endregion
}
