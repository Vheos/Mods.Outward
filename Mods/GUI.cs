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
        #region enum
        private enum BuySellModeSetting
        {
            Disabled = 0,
            Toggle = 1,
            TwoButtons = 2,
        }
        #endregion
        #region class
        private class PerPlayerData
        {
            // Settings
            public ModSetting<bool> _toggle;
            public ModSetting<bool> _disableQuickslotButtonIcons;
            public ModSetting<bool> _swapBuyingSellingItemsPanels;
            public ModSetting<BuySellModeSetting> _exclusiveBuySellPanels;
            public ModSetting<string> _buySellToggle, _switchToBuy, _switchToSell;

            // Utility
            public CharacterUI UI;
        }
        #endregion

        // Setting
        static private PerPlayerData[] _perPlayerData;
        static private ModSetting<bool> _verticalSplitscreen;
        override protected void Initialize()
        {
            _perPlayerData = new PerPlayerData[2];
            for (int i = 0; i < 2; i++)
            {
                PerPlayerData tmp = new PerPlayerData();
                _perPlayerData[i] = tmp;

                string playerPostfix = (i + 1).ToString();
                tmp._toggle = CreateSetting(nameof(tmp._toggle) + playerPostfix, false);
                tmp._disableQuickslotButtonIcons = CreateSetting(nameof(tmp._disableQuickslotButtonIcons) + playerPostfix, false);
                tmp._swapBuyingSellingItemsPanels = CreateSetting(nameof(tmp._swapBuyingSellingItemsPanels) + playerPostfix, false);
                tmp._exclusiveBuySellPanels = CreateSetting(nameof(tmp._exclusiveBuySellPanels) + playerPostfix, BuySellModeSetting.Disabled);
                tmp._buySellToggle = CreateSetting(nameof(tmp._buySellToggle) + playerPostfix, "");
                tmp._switchToBuy = CreateSetting(nameof(tmp._switchToBuy) + playerPostfix, "");
                tmp._switchToSell = CreateSetting(nameof(tmp._switchToSell) + playerPostfix, "");


                tmp.UI = SplitScreenManager.Instance.m_cachedUI[i];  !!!!!!!!

                tmp._swapBuyingSellingItemsPanels.AddEvent(() => TrySwapBuyingSellingItemsPanels(tmp.UI));
            }

            _verticalSplitscreen = CreateSetting(nameof(_verticalSplitscreen), false);
            AddEventOnConfigClosed(() => TryUpdateSplitscreenMode());

        }
        override protected void SetFormatting()
        {
            for (int i = 0; i < 2; i++)
            {
                PerPlayerData tmp = _perPlayerData[i];

                tmp._toggle.Format($"Player {i + 1}");
                tmp._toggle.Description = $"Change settings for local player {i + 1}";
                Indent++;
                {
                    tmp._disableQuickslotButtonIcons.Format("Disable quickslot button icons", tmp._toggle);
                    tmp._disableQuickslotButtonIcons.Description = "You know them by heart anyway!";
                    tmp._swapBuyingSellingItemsPanels.Format("Swap buying/selling items panels", tmp._toggle);
                    tmp._swapBuyingSellingItemsPanels.Description = "Items you're buying from merchant will be shown above his stock" +
                                                            "Items you're selling will be shown above your pouch";
                    tmp._exclusiveBuySellPanels.Format("Exclusive buy/sell mode", tmp._toggle);
                    tmp._exclusiveBuySellPanels.Description = "Disabled - shops display player's and merchant's inventory in one panel" +
                                                    "Toggle - toggle between player's and merchant's inventory with one button" +
                                                    "TwoButtons - press one button for player's inventory and another for merchant's";
                    Indent++;
                    {
                        tmp._buySellToggle.Format("Toggle buy/sell mode", tmp._exclusiveBuySellPanels, BuySellModeSetting.Toggle);
                        tmp._switchToBuy.Format("Switch to buy mode", tmp._exclusiveBuySellPanels, BuySellModeSetting.TwoButtons);
                        tmp._switchToSell.Format("Switch to sell mode", tmp._exclusiveBuySellPanels, BuySellModeSetting.TwoButtons);
                        Indent--;
                    }
                    Indent--;
                }
            }
            _verticalSplitscreen.Format("Vertical splitscreen");
            _verticalSplitscreen.Description = "For monitors that are more wide than tall";
        }
        public void OnUpdate()
        {
            foreach (var player in GameInput.LocalPlayers)
                if (player.UI.m_displayedMenuIndex == CharacterUI.MenuScreens.Shop)
                    switch (_perPlayerData[player.ID]._exclusiveBuySellPanels.Value)
                    {
                        case BuySellModeSetting.Toggle:
                            if (_perPlayerData[player.ID]._buySellToggle.Value.ToKeyCode().Pressed())
                                ToggleBuySellPanel(player.UI);
                            break;
                        case BuySellModeSetting.TwoButtons:
                            if (_perPlayerData[player.ID]._switchToBuy.Value.ToKeyCode().Pressed())
                                SwitchToPanel(player.UI, true);
                            else if (_perPlayerData[player.ID]._switchToBuy.Value.ToKeyCode().Pressed())
                                SwitchToPanel(player.UI, false);
                            break;
                    }

            /*
            if (_exclusiveBuySellMode.Value != BuySellModeSetting.Disabled
                player.UI.GetIsMenuDisplayed(CharacterUI.MenuScreens.Shop)
            && Input.GetKeyDown(_exclusiveBuySellMode.Value.ToKeyCode()))
                ToggleBuySellMode(player.UI);
            */
        }

        // Utility
        static private void TryUpdateSplitscreenMode()
        {
            if (SplitScreenManager.Instance == null)
                return;

            SplitScreenManager.Instance.CurrentSplitType = _verticalSplitscreen ? SplitScreenManager.SplitType.Vertical : SplitScreenManager.SplitType.Horizontal;
        }
        static private void SwitchToPanel(CharacterUI ui, bool buyPanel)
        {
            GetPlayerInventoryPanel(ui).GOSetActive(!buyPanel);
            GetMerchantInventoryPanel(ui).GOSetActive(buyPanel);
        }
        static private void ToggleBuySellPanel(CharacterUI ui)
        {
            // Cache
            Transform playerInventory = GetPlayerInventoryPanel(ui);
            Transform merchantInventory = GetMerchantInventoryPanel(ui);
            playerInventory.GOToggle();
            merchantInventory.GOSetActive(!playerInventory.GOActive());
        }
        static private void TrySwapBuyingSellingItemsPanels(CharacterUI ui)
        {
            if (ui == null)
                return;

            // Cache
            Transform playerInventory = GetPlayerInventoryPanel(ui);
            Transform merchantInventory = GetMerchantInventoryPanel(ui);

            // Determine if vanilla or custom setup
            bool isModified = false;
            Transform buyingItems = GetBuyingItemsPanel(playerInventory);
            Transform sellingItems = GetSellingItemsPanel(merchantInventory);
            if (buyingItems == null || sellingItems == null)
            {
                isModified = true;
                buyingItems = GetBuyingItemsPanel(merchantInventory);
                sellingItems = GetSellingItemsPanel(playerInventory);
                if (buyingItems == null || sellingItems == null)
                    return;
            }

            if (_perPlayerData[ui.ToPlayerID()]._swapBuyingSellingItemsPanels == isModified)
                return;

            // Execute
            Utility.SwapHierarchyPositions(buyingItems, sellingItems);
            buyingItems.SetAsFirstSibling();
            sellingItems.SetAsFirstSibling();
        }
        static private Transform GetPlayerInventoryPanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/ShopMenu/MiddlePanel/PlayerInventory");
        static private Transform GetMerchantInventoryPanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/ShopMenu/MiddlePanel/ShopInventory");
        static private Transform GetBuyingItemsPanel(Transform inventory)
        => inventory.Find("SectionContent/Scroll View/Viewport/Content/PlayerPendingBuyItems");
        static private Transform GetSellingItemsPanel(Transform inventory)
        => inventory.Find("SectionContent/Scroll View/Viewport/Content/PlayerPendingSellItems");

        // Hooks
        [HarmonyPatch(typeof(LocalCharacterControl), "RetrieveComponents"), HarmonyPostfix]
        static void LocalCharacterControl_RetrieveComponents_Post(LocalCharacterControl __instance)
        {
            foreach (var localPlayer in GameInput.LocalPlayers)
                localPlayer.UI.DelayedRefreshSize();

            TrySwapBuyingSellingItemsPanels(__instance.Character.CharacterUI);
        }

        // Exclusive buy/sell panels
        [HarmonyPatch(typeof(ShopMenu), "Show"), HarmonyPostfix]
        static void ShopMenu_Show_Post(ref ShopMenu __instance)
        {
            #region quit
            if (_perPlayerData[__instance.ToPlayerID()]._exclusiveBuySellPanels == BuySellModeSetting.Disabled)
                return;
            #endregion

            SwitchToPanel(__instance.CharacterUI, true);
        }

        // Disable quickslot button icons
        [HarmonyPatch(typeof(QuickSlotDisplay), "Update"), HarmonyPostfix]
        static void QuickSlotDisplay_Update_Post(ref QuickSlotDisplay __instance)
        {
            #region quit
            if (!_perPlayerData[__instance.ToPlayerID()]._disableQuickslotButtonIcons)
                return;
            #endregion

            __instance.m_inputIcon.enabled = false;
            __instance.m_inputKeyboardIcon.SetActive(false);
            __instance.m_lblKeyboardInput.enabled = false;
        }

        // Vertical splitscreen
        [HarmonyPatch(typeof(CharacterUI), "DelayedRefreshSize"), HarmonyPostfix]
        static void CharacterUI_RefreshSize_Post(ref CharacterUI __instance)
        {
            int localPlayersCount = GameInput.LocalPlayers.Count;
            #region quit
            if (!_verticalSplitscreen || localPlayersCount < 2)
                return;
            #endregion

            SplitScreenManager.Instance.CurrentSplitType = SplitScreenManager.SplitType.Vertical;
            __instance.m_rectTransform.localPosition *= -1;
        }
    }
}

/*
        [HarmonyPatch(typeof(MenuPanel), "Show", new Type[] { }), HarmonyPostfix]
        static void MenuPanel_Show_Post(ref MenuPanel __instance)
        {
            #region quit
            if (__instance.CharacterUI == null)
                return;
            #endregion

            string panelName = __instance.GOName();
            MenuPanelHolder parentHolderName = __instance.m_parentPanelHolder;
            float panelWidth = __instance.RectTransform.rect.width;
            float screenWidth = __instance.CharacterUI.m_rectTransform.rect.width;
            Tools.Log($"[MenuPanel.Show] {panelName} / {parentHolderName}  -  {panelWidth} / {screenWidth}");
        }
*/