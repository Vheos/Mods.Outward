namespace Vheos.Mods.Outward;

using UnityEngine.UI;

public class CommonHooks
{
    // Events
    public static event Action<ItemDisplay, Image, Image, Image> OnRefreshEnchantedIcon;

    // Initializers
    public static void Initialize()
    {        
        Harmony.CreateAndPatchAll(typeof(CommonHooks));
    }

    // Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.RefreshEnchantedIcon))]
    private static void ItemDisplay_RefreshEnchantedIcon_Post(ItemDisplay __instance)
    {
        if (OnRefreshEnchantedIcon is null)
            return;

        Image icon = __instance.FindChild<Image>("Icon");
        Image border = icon.FindChild<Image>("border");
        Image indicator = __instance.m_imgEnchantedIcon;

        icon.color = Color.white;
        border.color = Color.white;
        if (indicator is not null)
            indicator.color = Color.white;

        OnRefreshEnchantedIcon(__instance, icon, border, indicator);
    }
}
