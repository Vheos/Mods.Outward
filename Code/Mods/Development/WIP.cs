namespace Vheos.Mods.Outward;
using UnityEngine.UI;

public class WIP : AMod
{
    private static ModSetting<Vector2> _temperatureMultiplier;
    private static ModSetting<bool> _allowPushKickRemoval;
    private static ModSetting<bool> _allowTargetingPlayers;
    protected override void Initialize()
    {
        _temperatureMultiplier = CreateSetting(nameof(_temperatureMultiplier), 1f.ToVector2());
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

        _allowPushKickRemoval.Format("Allow \"Push Kick\" removal");
        _allowPushKickRemoval.Description = "For future skill trees mod\n" +
                                            "Normally, player data won't be saved if they don't have the \"Push Kick\" skill";
        _allowTargetingPlayers.Format("Allow targeting players");
        _allowTargetingPlayers.Description = "For future co-op skills mod";
    }

    protected override string SectionOverride
    => ModSections.Development;

    // Hooks
    // Temperature multiplier
    [HarmonyPostfix, HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.TemperatureModifier), MethodType.Getter)]
    private static void CharacterStats_TemperatureModifier_Getter_Post(PlayerCharacterStats __instance, ref float __result)
    {
        float progress = __instance.Temperature.DistanceTo(50f).Div(50f);
        __result *= _temperatureMultiplier.Value.x.Lerp(_temperatureMultiplier.Value.y, progress);
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
