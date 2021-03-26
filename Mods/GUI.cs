using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace ModPack
{
    public class GUI : AMod, IDelayedInit, IUpdatable
    {
        #region const
        static public readonly Vector2 DEFAULT_SHOP_OFFSET_MIN = new Vector2(-1344f, -540f);
        static public readonly Vector2 DEFAULT_SHOP_OFFSET_MAX = new Vector2(-20f, -20f);
        public const float SHOP_MENU_RESIZE_DELAY = 0.2f;
        #endregion
        #region enum
        private enum SeperatePanelsMode
        {
            Disabled = 0,
            Toggle = 1,
            TwoButtons = 2,
        }
        #endregion
        #region class
        private class PerPlayerSettings
        {
            // Settings
            public ModSetting<bool> _toggle;
            public ModSetting<bool> _disableQuickslotButtonIcons;
            public ModSetting<int> _shopMenuWidth;
            public ModSetting<bool> _swapPendingBuySellPanels;
            public ModSetting<SeperatePanelsMode> _separateBuySellPanels;
            public ModSetting<string> _buySellToggle, _switchToBuy, _switchToSell;
        }
        #endregion

        // Setting
        static private PerPlayerSettings[] _perPlayerSettings;
        static private ModSetting<bool> _verticalSplitscreen;
        override protected void Initialize()
        {
            _perPlayerSettings = new PerPlayerSettings[2];
            for (int i = 0; i < 2; i++)
            {
                PerPlayerSettings tmp = new PerPlayerSettings();
                _perPlayerSettings[i] = tmp;

                string playerPostfix = (i + 1).ToString();
                tmp._toggle = CreateSetting(nameof(tmp._toggle) + playerPostfix, false);
                tmp._disableQuickslotButtonIcons = CreateSetting(nameof(tmp._disableQuickslotButtonIcons) + playerPostfix, false);
                tmp._shopMenuWidth = CreateSetting(nameof(tmp._shopMenuWidth) + playerPostfix, 0, IntRange(0, 100));
                tmp._swapPendingBuySellPanels = CreateSetting(nameof(tmp._swapPendingBuySellPanels) + playerPostfix, false);
                tmp._separateBuySellPanels = CreateSetting(nameof(tmp._separateBuySellPanels) + playerPostfix, SeperatePanelsMode.Disabled);
                tmp._buySellToggle = CreateSetting(nameof(tmp._buySellToggle) + playerPostfix, "");
                tmp._switchToBuy = CreateSetting(nameof(tmp._switchToBuy) + playerPostfix, "");
                tmp._switchToSell = CreateSetting(nameof(tmp._switchToSell) + playerPostfix, "");

                // Events
                int id = i;
                tmp._separateBuySellPanels.AddEvent(() =>
                {
                    if (Players.TryGetLocal(id, out Players.Data player))
                        UpdateSeparateBuySellPanels(player);
                });

                tmp._swapPendingBuySellPanels.AddEvent(() =>
                {
                    if (Players.TryGetLocal(id, out Players.Data player))
                        UpdatePendingBuySellPanels(player);
                });
                tmp._disableQuickslotButtonIcons.AddEvent(() =>
                {
                    if (Players.TryGetLocal(id, out Players.Data player))
                        UpdateQuickslotButtonIcons(player);
                });
            }

            _verticalSplitscreen = CreateSetting(nameof(_verticalSplitscreen), false);
            AddEventOnConfigClosed(() => UpdateSplitscreenMode());
            AddEventOnConfigClosed(() => UpdateShopMenusWidths());
        }
        override protected void SetFormatting()
        {
            _verticalSplitscreen.Format("Vertical splitscreen");
            _verticalSplitscreen.Description = "For monitors that are more wide than tall";

            for (int i = 0; i < 2; i++)
            {
                PerPlayerSettings tmp = _perPlayerSettings[i];

                tmp._toggle.Format($"Player {i + 1}");
                tmp._toggle.Description = $"Change settings for local player {i + 1}";
                Indent++;
                {
                    tmp._disableQuickslotButtonIcons.Format("Disable quickslot button icons", tmp._toggle);
                    tmp._disableQuickslotButtonIcons.Description = "You know them by heart anyway!";
                    tmp._shopMenuWidth.Format("Shop menu width");
                    tmp._shopMenuWidth.Description = "% of screen size, 0 for default width";
                    tmp._separateBuySellPanels.Format("Separate buy/sell panels", tmp._toggle);
                    tmp._separateBuySellPanels.Description = "Disabled - shops display player's and merchant's inventory in one panel" +
                                                    "Toggle - toggle between player's and merchant's inventory with one button" +
                                                    "TwoButtons - press one button for player's inventory and another for merchant's";
                    tmp._swapPendingBuySellPanels.Format("Swap pending buy/sell panels", tmp._separateBuySellPanels, SeperatePanelsMode.Disabled);
                    tmp._swapPendingBuySellPanels.Description = "Items you're buying from merchant will be shown above his stock" +
                                                                "Items you're selling will be shown above your pouch";
                    Indent++;
                    {
                        tmp._buySellToggle.Format("Toggle buy/sell mode", tmp._separateBuySellPanels, SeperatePanelsMode.Toggle);
                        tmp._switchToBuy.Format("Switch to buy mode", tmp._separateBuySellPanels, SeperatePanelsMode.TwoButtons);
                        tmp._switchToSell.Format("Switch to sell mode", tmp._separateBuySellPanels, SeperatePanelsMode.TwoButtons);
                        Indent--;
                    }
                    Indent--;
                }
            }

        }
        public void OnUpdate()
        {
            foreach (var player in Players.Local)
                if (player.UI.m_displayedMenuIndex == CharacterUI.MenuScreens.Shop)
                {
                    PerPlayerSettings settings = _perPlayerSettings[player.ID];
                    switch (settings._separateBuySellPanels.Value)
                    {
                        case SeperatePanelsMode.Toggle:
                            if (player.IsUsingGamepad && (player.Pressed(ControlsInput.MenuActions.GoToPreviousMenu) || player.Pressed(ControlsInput.MenuActions.GoToNextMenu))
                            || !player.IsUsingGamepad && settings._buySellToggle.Value.ToKeyCode().Pressed())
                                ToggleBuySellPanel(player);
                            break;
                        case SeperatePanelsMode.TwoButtons:
                            if (player.IsUsingGamepad && player.Pressed(ControlsInput.MenuActions.GoToNextMenu)
                            || !player.IsUsingGamepad && settings._switchToBuy.Value.ToKeyCode().Pressed())
                                SwitchToPanel(player, true);
                            else if (player.IsUsingGamepad && player.Pressed(ControlsInput.MenuActions.GoToPreviousMenu)
                            || !player.IsUsingGamepad && settings._switchToSell.Value.ToKeyCode().Pressed())
                                SwitchToPanel(player, false);
                            break;
                    }
                }
        }

        // Utility
        static private void UpdateSplitscreenMode()
        {
            if (SplitScreenManager.Instance != null)
                SplitScreenManager.Instance.CurrentSplitType = _verticalSplitscreen && Players.Local.Count >= 2
                                                             ? SplitScreenManager.SplitType.Vertical
                                                             : SplitScreenManager.SplitType.Horizontal;
        }
        static private void UpdateShopMenusWidths()
        {
            foreach (var player in Players.Local)
            {
                // Choose
                PerPlayerSettings playerData = _perPlayerSettings[player.ID];
                Vector2 offsetMin = DEFAULT_SHOP_OFFSET_MIN;
                Vector2 offsetMax = DEFAULT_SHOP_OFFSET_MAX;
                if (playerData._shopMenuWidth > 0)
                {
                    float maxWidth = player.UI.m_rectTransform.rect.width.Neg();
                    float multiplier = playerData._shopMenuWidth / 100f;
                    offsetMin.SetX(maxWidth * multiplier);
                    offsetMax = Vector2.zero;
                }
                // Set
                RectTransform shopMenuRectTransform = GetShopMenuPanel(player.UI).GetComponent<RectTransform>();
                shopMenuRectTransform.offsetMin = offsetMin;
                shopMenuRectTransform.offsetMax = offsetMax;
            }
        }
        static private void UpdateQuickslotButtonIcons(Players.Data player)
        {
            foreach (var quickslotDisplay in GetKeyboardQuickslotsGamePanel(player.UI).GetAllComponentsInHierarchy<QuickSlotDisplay>())
                quickslotDisplay.m_lblKeyboardInput.enabled = !_perPlayerSettings[player.ID]._disableQuickslotButtonIcons;
        }
        static private void UpdateSeparateBuySellPanels(Players.Data player)
        {
            if (_perPlayerSettings[player.ID]._separateBuySellPanels == SeperatePanelsMode.Disabled)
                DisableSeparateMode(player);
            else
                SwitchToPanel(player, true);
        }
        static private void SwitchToPanel(Players.Data player, bool buyPanel)
        {
            GetPlayerInventoryPanel(player.UI).GOSetActive(!buyPanel);
            GetMerchantInventoryPanel(player.UI).GOSetActive(buyPanel);
            GetShopMenuPanel(player.UI).GetComponent<ShopMenu>().GetFirstSelectable().Select();
        }
        static private void ToggleBuySellPanel(Players.Data player)
        {
            bool isBuyPanel = GetMerchantInventoryPanel(player.UI).GOActive();
            SwitchToPanel(player, !isBuyPanel);
        }
        static private void DisableSeparateMode(Players.Data player)
        {
            GetPlayerInventoryPanel(player.UI).GOSetActive(true);
            GetMerchantInventoryPanel(player.UI).GOSetActive(true);
        }
        static private void UpdatePendingBuySellPanels(Players.Data player)
        {
            // Cache
            Transform playerInventory = GetPlayerInventoryPanel(player.UI);
            Transform merchantInventory = GetMerchantInventoryPanel(player.UI);

            // Determine if vanilla or custom setup
            bool isModified = false;
            Transform pendingBuy = GetPendingBuyPanel(playerInventory);
            Transform pendingSell = GetPendingSellPanel(merchantInventory);
            if (pendingBuy == null || pendingSell == null)
            {
                isModified = true;
                pendingBuy = GetPendingBuyPanel(merchantInventory);
                pendingSell = GetPendingSellPanel(playerInventory);
                if (pendingBuy == null || pendingSell == null)
                    return;
            }

            if (_perPlayerSettings[player.ID]._swapPendingBuySellPanels == isModified)
                return;

            // Execute
            Utility.SwapHierarchyPositions(pendingBuy, pendingSell);
            pendingBuy.SetAsFirstSibling();
            pendingSell.SetAsFirstSibling();
        }
        // Find
        static private Transform GetKeyboardQuickslotsGamePanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/HUD/QuickSlot/Keyboard");
        static private Transform GetShopMenuPanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/ShopMenu");
        static private Transform GetPlayerInventoryPanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/ShopMenu/MiddlePanel/PlayerInventory");
        static private Transform GetMerchantInventoryPanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/ShopMenu/MiddlePanel/ShopInventory");
        static private Transform GetPendingBuyPanel(Transform inventory)
        => inventory.Find("SectionContent/Scroll View/Viewport/Content/PlayerPendingBuyItems");
        static private Transform GetPendingSellPanel(Transform inventory)
        => inventory.Find("SectionContent/Scroll View/Viewport/Content/PlayerPendingSellItems");

        // Hooks
        [HarmonyPatch(typeof(LocalCharacterControl), "RetrieveComponents"), HarmonyPostfix]
        static void LocalCharacterControl_RetrieveComponents_Post(LocalCharacterControl __instance)
        {
            UpdateSplitscreenMode();
            __instance.ExecuteOnceAfterDelay(SHOP_MENU_RESIZE_DELAY, UpdateShopMenusWidths);

            Players.Data player = Players.GetLocal(__instance.Character.OwnerPlayerSys.PlayerID);
            UpdateQuickslotButtonIcons(player);
            UpdatePendingBuySellPanels(player);
        }

        [HarmonyPatch(typeof(RPCManager), "SendPlayerHasLeft"), HarmonyPostfix]
        static void RPCManager_SendPlayerHasLeft_Post(RPCManager __instance)
        {
            UpdateSplitscreenMode();
            __instance.ExecuteOnceAfterDelay(SHOP_MENU_RESIZE_DELAY, UpdateShopMenusWidths);
        }

        // Exclusive buy/sell panels
        [HarmonyPatch(typeof(ShopMenu), "Show"), HarmonyPostfix]
        static void ShopMenu_Show_Post(ref ShopMenu __instance)
        => UpdateSeparateBuySellPanels(__instance.ToPlayerData());

        // Disable quickslot button icons
        [HarmonyPatch(typeof(QuickSlotDisplay), "Update"), HarmonyPostfix]
        static void QuickSlotDisplay_Update_Post(ref QuickSlotDisplay __instance)
        {
            Players.Data player = __instance.ToPlayerData();
            #region quit
            if (!_perPlayerSettings[player.ID]._disableQuickslotButtonIcons)
                return;
            #endregion

            __instance.m_inputIcon.enabled = false;
            __instance.m_inputKeyboardIcon.SetActive(false);
        }

        // Vertical splitscreen
        [HarmonyPatch(typeof(CharacterUI), "DelayedRefreshSize"), HarmonyPostfix]
        static void CharacterUI_RefreshSize_Post(ref CharacterUI __instance)
        {
            #region quit
            if (SplitScreenManager.Instance.CurrentSplitType != SplitScreenManager.SplitType.Vertical)
                return;
            #endregion

            __instance.m_rectTransform.localPosition *= -1;
        }
    }
}