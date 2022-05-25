namespace Vheos.Mods.Outward
{
    using UnityEngine;
    using UnityEngine.UI;
    using HarmonyLib;
    using Mods.Core;
    using Tools.Extensions.Math;
    public class WIP : AMod
    {
        static private ModSetting<Vector2> _temperatureMultiplier;
        static private ModSetting<bool> _markItemsWithLegacyUpgrade;
        static private ModSetting<bool> _allowDodgeAnimationCancelling;
        static private ModSetting<bool> _allowPushKickRemoval;
        static private ModSetting<bool> _allowTargetingPlayers;
        override protected void Initialize()
        {
            _temperatureMultiplier = CreateSetting(nameof(_temperatureMultiplier), 1f.ToVector2());
            _markItemsWithLegacyUpgrade = CreateSetting(nameof(_markItemsWithLegacyUpgrade), false);
            _allowDodgeAnimationCancelling = CreateSetting(nameof(_allowDodgeAnimationCancelling), false);
            _allowPushKickRemoval = CreateSetting(nameof(_allowPushKickRemoval), false);
            _allowTargetingPlayers = CreateSetting(nameof(_allowTargetingPlayers), false);
        }
        override protected void SetFormatting()
        {
            _temperatureMultiplier.Format("Temperature multiplier");
            _temperatureMultiplier.Description = "How strongly the environment temperature affects you when your temperature is:\n" +
                                                 "X   -   neutral\n" +
                                                 "Y   -   approaching either extreme\n" +
                                                 "(set X and Y to the same value for a flat, linear multiplier)";
            _markItemsWithLegacyUpgrade.Format("Mark items with legacy upgrades");
            _allowDodgeAnimationCancelling.Format("Allow dodge to cancel actions");
            _allowDodgeAnimationCancelling.Description = "Cancelling certain animations might lead to glitches";
            _allowPushKickRemoval.Format("Allow \"Push Kick\" removal");
            _allowPushKickRemoval.Description = "For future skill trees mod\n" +
                                                "Normally, player data won't be saved if they don't have the \"Push Kick\" skill";
            _allowTargetingPlayers.Format("Allow targeting players");
            _allowTargetingPlayers.Description = "For future co-op skills mod";
        }

        override protected string SectionOverride
        => ModSections.Development;

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Temperature multiplier
        [HarmonyPatch(typeof(CharacterStats), "TemperatureModifier", MethodType.Getter), HarmonyPostfix]
        static void CharacterStats_TemperatureModifier_Getter_Post(PlayerCharacterStats __instance, ref float __result)
        {
            float progress = __instance.Temperature.DistanceTo(50f).Div(50f);
            __result *= _temperatureMultiplier.Value.x.Lerp(_temperatureMultiplier.Value.y, progress);
        }

        // Mark items with legacy upgrades
        [HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.RefreshEnchantedIcon)), HarmonyPrefix]
        static bool ItemDisplay_RefreshEnchantedIcon_Pre(ItemDisplay __instance)
        {
            #region quit
            if (!_markItemsWithLegacyUpgrade || __instance.m_refItem == null || __instance.m_imgEnchantedIcon == null)
                return true;
            #endregion

            // Cache
            //Image icon = __instance.FindChild<Image>("Icon");
            //Image border = icon.FindChild<Image>("border");
            Image indicator = __instance.m_imgEnchantedIcon;

            // Default
            indicator.GOSetActive(false);

            // Quit
            if (__instance.m_refItem.LegacyItemID <= 0)
                return true;

            // Custom
            indicator.color = Color.red;
            indicator.rectTransform.pivot = 1f.ToVector2();
            indicator.rectTransform.localScale = new Vector2(1.5f, 1.5f);
            indicator.GOSetActive(true);
            return false;
        }

        // Dodge animation cancelling
        [HarmonyPatch(typeof(Character), "DodgeInput", new[] { typeof(Vector3) }), HarmonyPrefix]
        static bool Character_SpellCastAnim_Post(ref int ___m_dodgeAllowedInAction, ref Character.HurtType ___m_hurtType)
        {
            #region quit
            if (!_allowDodgeAnimationCancelling)
                return true;
            #endregion

            if (___m_hurtType == Character.HurtType.NONE)
                ___m_dodgeAllowedInAction = 1;
            return true;
        }

        // Push kick removal
        [HarmonyPatch(typeof(CharacterSave), "IsValid", MethodType.Getter), HarmonyPrefix]
        static bool CharacterSave_IsValid_Getter_Pre(ref bool __result)
        {
            #region quit
            if (!_allowPushKickRemoval.Value)
                return true;
            #endregion

            __result = true;
            return false;
        }

        // Target other players
        [HarmonyPatch(typeof(TargetingSystem), "IsTargetable", new[] { typeof(Character) }), HarmonyPrefix]
        static bool TargetingSystem_IsTargetable_Pre(TargetingSystem __instance, ref bool __result, ref Character _char)
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
}