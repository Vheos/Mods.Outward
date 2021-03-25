using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace ModPack
{
    public class GUI : AMod, IUpdatable
    {
        #region enum
        private enum BuySellModeSetting
        {
            Disabled = 0,
            Toggle = 1,
            TwoButtons = 2,
        }
        #endregion

        // Setting
        static private ModSetting<bool> _disableQuickslotButtonIcons;
        static private ModSetting<bool> _swapBuyingSellingItemsPanels;
        static private ModSetting<BuySellModeSetting> _exclusiveBuySellMode;
        static private ModSetting<string> _buySellToggleButton, _buyButton, _sellButton;

        static private ModSetting<bool> _verticalSplitscreen;
        override protected void Initialize()
        {
            _disableQuickslotButtonIcons = CreateSetting(nameof(_disableQuickslotButtonIcons), false);
            _swapBuyingSellingItemsPanels = CreateSetting(nameof(_swapBuyingSellingItemsPanels), false);
            _exclusiveBuySellMode = CreateSetting(nameof(_exclusiveBuySellMode), BuySellModeSetting.Disabled);
            _verticalSplitscreen = CreateSetting(nameof(_verticalSplitscreen), false);
            _buySellToggleButton = CreateSetting(nameof(_buySellToggleButton), "");
            _buyButton = CreateSetting(nameof(_buyButton), "");
            _sellButton = CreateSetting(nameof(_sellButton), "");

            _swapBuyingSellingItemsPanels.AddEvent(() =>
            {
                if (SplitScreenManager.Instance != null)
                    TrySwapAllBuyingSellingItemsPanels();
            });

            AddEventOnConfigClosed(() =>
            {
                if (SplitScreenManager.Instance != null)
                    SplitScreenManager.Instance.CurrentSplitType = _verticalSplitscreen ? SplitScreenManager.SplitType.Vertical : SplitScreenManager.SplitType.Horizontal;
            });
        }
        override protected void SetFormatting()
        {
            _disableQuickslotButtonIcons.Format("Disable quickslot button icons");
            _disableQuickslotButtonIcons.Description = "You know them by heart anyway!";
            _swapBuyingSellingItemsPanels.Format("Swap buying/selling items panels");
            _swapBuyingSellingItemsPanels.Description = "Items you're buying from merchant will be shown above his stock" +
                                                        "Items you're selling will be shown above your pouch";
            _exclusiveBuySellMode.Format("Exclusive buy/sell mode");
            _exclusiveBuySellMode.Description = "Disabled - shops display player's and merchant's inventory in one panel" +
                                                "Toggle - toggle between player's and merchant's inventory with one button" +
                                                "TwoButtons - press one button for player's inventory and another for merchant's";
            Indent++;
            {
                _buySellToggleButton.Format("Toggle buy/sell mode", _exclusiveBuySellMode, BuySellModeSetting.Toggle);
                _buyButton.Format("Switch to buy mode", _exclusiveBuySellMode, BuySellModeSetting.TwoButtons);
                _sellButton.Format("Switch to sell mode", _exclusiveBuySellMode, BuySellModeSetting.TwoButtons);
                Indent--;
            }
            _verticalSplitscreen.Format("Vertical splitscreen");
            _verticalSplitscreen.Description = "For monitors that are more wide than tall";
        }
        public void OnUpdate()
        {
            foreach (var player in GameInput.LocalPlayers)
                if (player.UI.m_displayedMenuIndex == CharacterUI.MenuScreens.Shop)
                    switch (_exclusiveBuySellMode.Value)
                    {
                        case BuySellModeSetting.Toggle:
                            if (_buySellToggleButton.Value.ToKeyCode().Pressed())
                                ToggleBuySellMode(player.UI);
                            break;
                        case BuySellModeSetting.TwoButtons:
                            if (_buyButton.Value.ToKeyCode().Pressed())
                                SetBuySellMode(player.UI, true);
                            else if (_buyButton.Value.ToKeyCode().Pressed())
                                SetBuySellMode(player.UI, false);
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
        static private void SetBuySellMode(CharacterUI ui, bool isBuyMode)
        {
            GetPlayerInventoryPanel(ui).GOSetActive(!isBuyMode);
            GetMerchantInventoryPanel(ui).GOSetActive(isBuyMode);
        }
        static private void ToggleBuySellMode(CharacterUI ui)
        {
            // Cache
            Transform playerInventory = GetPlayerInventoryPanel(ui);
            Transform merchantInventory = GetMerchantInventoryPanel(ui);
            playerInventory.GOToggle();
            merchantInventory.GOSetActive(!playerInventory.GOActive());
        }
        static private void TrySwapAllBuyingSellingItemsPanels()
        {
            foreach (var cachedUI in SplitScreenManager.Instance.m_cachedUI)
                if (cachedUI != null)
                    TrySwapBuyingSellingItemsPanels(cachedUI);
        }
        static private void TrySwapBuyingSellingItemsPanels(CharacterUI ui)
        {
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

            if (_swapBuyingSellingItemsPanels == isModified)
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

        [HarmonyPatch(typeof(LocalCharacterControl), "RetrieveComponents"), HarmonyPostfix]
        static void LocalCharacterControl_RetrieveComponents_Post(LocalCharacterControl __instance)
        {
            foreach (var localPlayer in GameInput.LocalPlayers)
                localPlayer.UI.DelayedRefreshSize();

            TrySwapAllBuyingSellingItemsPanels();

        }

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

        [HarmonyPatch(typeof(ShopMenu), "Show"), HarmonyPostfix]
        static void ShopMenu_Show_Post(ref ShopMenu __instance)
        {
            if (__instance.CharacterUI == null)
                SetBuySellMode(__instance.CharacterUI, true);
        }

        // Disable quickslot button icons
        [HarmonyPatch(typeof(QuickSlotDisplay), "Update"), HarmonyPostfix]
        static void QuickSlotDisplay_Update_Post(ref QuickSlotDisplay __instance)
        {
            #region quit
            if (!_disableQuickslotButtonIcons)
                return;
            #endregion

            __instance.m_inputIcon.enabled = false;
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