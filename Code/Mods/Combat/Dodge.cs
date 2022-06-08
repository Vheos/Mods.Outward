namespace Vheos.Mods.Outward;

public class Dodge : AMod
{
    #region const
    private const int ACROBATICS_SKILLID = 8205450;
    #endregion

    // Settings
    private static ModSetting<int> _staminaCost;
    private static ModSetting<int> _staminaCostAcrobatics;
    private static ModSetting<bool> _allowMidAttack;
    private static ModSetting<bool> _allowMidAttackUntilDamageDealt;
    private static ModSetting<bool> _allowMidAttackeUntilDamageTaken;
    private static ModSetting<bool> _removeInvulnerability;
    protected override void Initialize()
    {
        _staminaCost = CreateSetting(nameof(_staminaCost), 6, IntRange(0, 50));
        _staminaCostAcrobatics = CreateSetting(nameof(_staminaCostAcrobatics), 9, IntRange(0, 50));
        _allowMidAttack = CreateSetting(nameof(_allowMidAttack), false);
        _allowMidAttackUntilDamageDealt = CreateSetting(nameof(_allowMidAttackeUntilDamageTaken), false);
        _allowMidAttackeUntilDamageTaken = CreateSetting(nameof(_allowMidAttackUntilDamageDealt), false);
        _removeInvulnerability = CreateSetting(nameof(_removeInvulnerability), false);
    }
    protected override void SetFormatting()
    {
        _staminaCost.Format("Stamina cost");
        using (Indent)
        {
            _staminaCostAcrobatics.Format("with acrobatics");
        }
        _allowMidAttack.Format("Allow mid-attack dodge");
        using (Indent)
        {
            _allowMidAttackUntilDamageDealt.Format("until you deal damage", _allowMidAttack);
            _allowMidAttackeUntilDamageTaken.Format("until you take damage", _allowMidAttack);
        }
        _removeInvulnerability.Format("Remove dodge invulnerability");
        _removeInvulnerability.Description =
            "You can get hit during the dodge animation\n" +
            "(even without a backpack)";
    }
    protected override string Description
    => "";
    protected override string SectionOverride
    => ModSections.Combat;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _staminaCost.Value = 6;
                _staminaCostAcrobatics.Value = 9;
                _allowMidAttack.Value = true;
                _allowMidAttackUntilDamageDealt.Value = true;
                _allowMidAttackeUntilDamageTaken.Value = true;
                _removeInvulnerability.Value = true;
                break;
        }
    }


    // Hooks
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

        if (_allowMidAttackeUntilDamageTaken
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
        #region quit
        if (!_removeInvulnerability)
            return;
        #endregion

        if (_step > 0 && ___m_hitboxes != null)
            foreach (var hitbox in ___m_hitboxes)
                hitbox.gameObject.SetActive(true);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.DodgeStamCost), MethodType.Getter)]
    private static void Character_DodgeStamCost_Getter_Post(Character __instance, ref int __result)
    => __result = __instance.Inventory.SkillKnowledge.IsItemLearned(ACROBATICS_SKILLID)
        ? _staminaCostAcrobatics
        : _staminaCost;

}
