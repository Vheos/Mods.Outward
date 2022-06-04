namespace Vheos.Mods.Outward;
using UnityEngine.UI;

public class WIP : AMod
{
    private static ModSetting<Vector2> _temperatureMultiplier;
    private static ModSetting<bool> _markItemsWithLegacyUpgrade;
    private static ModSetting<Color> _legacyItemUpgradeColor;

    private static ModSetting<bool> _allowDodgeAnimationCancelling;
    private static ModSetting<bool> _allowPushKickRemoval;
    private static ModSetting<bool> _allowTargetingPlayers;
    protected override void Initialize()
    {
        _temperatureMultiplier = CreateSetting(nameof(_temperatureMultiplier), 1f.ToVector2());
        _markItemsWithLegacyUpgrade = CreateSetting(nameof(_markItemsWithLegacyUpgrade), false);
        _legacyItemUpgradeColor = CreateSetting(nameof(_legacyItemUpgradeColor), new Color(1f, 0.5f, 0f));

        _allowDodgeAnimationCancelling = CreateSetting(nameof(_allowDodgeAnimationCancelling), false);
        _allowPushKickRemoval = CreateSetting(nameof(_allowPushKickRemoval), false);
        _allowTargetingPlayers = CreateSetting(nameof(_allowTargetingPlayers), false);
    }
    protected override void SetFormatting()
    {
        _temperatureMultiplier.Format("Temperature multiplier");
        _temperatureMultiplier.Description = "How strongly the environment temperature affects you when your temperature is:\n" +
                                             "X   -   neutral\n" +
                                             "Y   -   approaching either extreme\n" +
                                             "(set X and Y to the same value for a flat, linear multiplier)";
        _markItemsWithLegacyUpgrade.Format("Mark items with legacy upgrades");
        using (Indent)
        {
            _legacyItemUpgradeColor.Format("color");
        }
        _allowDodgeAnimationCancelling.Format("Allow dodge to cancel actions");
        _allowDodgeAnimationCancelling.Description = "Cancelling certain animations might lead to glitches";
        _allowPushKickRemoval.Format("Allow \"Push Kick\" removal");
        _allowPushKickRemoval.Description = "For future skill trees mod\n" +
                                            "Normally, player data won't be saved if they don't have the \"Push Kick\" skill";
        _allowTargetingPlayers.Format("Allow targeting players");
        _allowTargetingPlayers.Description = "For future co-op skills mod";
    }

    protected override string SectionOverride
    => ModSections.Development;

    // Utility
    private static HashSet<Character> _dodgeAllowances = new();

    // Hooks

    // Temperature multiplier
    [HarmonyPostfix, HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.TemperatureModifier), MethodType.Getter)]
    private static void CharacterStats_TemperatureModifier_Getter_Post(PlayerCharacterStats __instance, ref float __result)
    {
        float progress = __instance.Temperature.DistanceTo(50f).Div(50f);
        __result *= _temperatureMultiplier.Value.x.Lerp(_temperatureMultiplier.Value.y, progress);
    }

    // Mark items with legacy upgrades
    [HarmonyPrefix, HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.RefreshEnchantedIcon))]
    private static bool ItemDisplay_RefreshEnchantedIcon_Pre(ItemDisplay __instance)
    {
        #region quit
        if (!_markItemsWithLegacyUpgrade
        || __instance.m_refItem == null
        || __instance.m_imgEnchantedIcon == null
        || __instance.m_refItem is Skill)
            return true;
        #endregion

        // Cache
        Image indicator = __instance.m_imgEnchantedIcon;

        // Default
        indicator.GOSetActive(false);

        // Quit
        if (__instance.m_refItem.LegacyItemID <= 0)
            return true;

        // Custom
        indicator.color = _legacyItemUpgradeColor.Value.AlphaMultiplied(1 / 3f);
        indicator.rectTransform.pivot = 1f.ToVector2();
        indicator.rectTransform.localScale = new Vector2(1.5f, 1.5f);
        indicator.GOSetActive(true);
        return false;
    }

    // Dodge animation cancelling
    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.DodgeInput), new[] { typeof(Vector3) })]
    private static void Character_SpellCastAnim_Post(Character __instance, ref int ___m_dodgeAllowedInAction, ref Character.HurtType ___m_hurtType)
    {
        #region quit
        if (!_allowDodgeAnimationCancelling
        || !__instance.IsPlayer()
        || !_dodgeAllowances.Contains(__instance))
            return;
        #endregion

        ___m_dodgeAllowedInAction = 1;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.StartAttack))]
    static private void Character_StartAttack_Post(Character __instance, int _type, int _id)
    {
        #region quit
        if (_type is not 0 and not 1)
            return;
        #endregion

        _dodgeAllowances.Add(__instance);
    }

    // Push kick removal
    [HarmonyPrefix, HarmonyPatch(typeof(CharacterSave), nameof(CharacterSave.IsValid), MethodType.Getter)]
    private static bool CharacterSave_IsValid_Getter_Pre(ref bool __result)
    {
        #region quit
        if (!_allowPushKickRemoval.Value)
            return true;
        #endregion

        __result = true;
        return false;
    }

    // Target other players
    [HarmonyPrefix, HarmonyPatch(typeof(TargetingSystem), nameof(TargetingSystem.IsTargetable), new[] { typeof(Character) })]
    private static bool TargetingSystem_IsTargetable_Pre(TargetingSystem __instance, ref bool __result, ref Character _char)
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
}
