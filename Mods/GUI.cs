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
        private enum SeperatePanelsMode
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
            public ModSetting<bool> _swapPendingBuySellPanels;
            public ModSetting<SeperatePanelsMode> _separateBuySellPanels;
            public ModSetting<string> _buySellToggle, _switchToBuy, _switchToSell;
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
                tmp._separateBuySellPanels = CreateSetting(nameof(tmp._separateBuySellPanels) + playerPostfix, SeperatePanelsMode.Disabled);
                tmp._swapPendingBuySellPanels = CreateSetting(nameof(tmp._swapPendingBuySellPanels) + playerPostfix, false);
                tmp._buySellToggle = CreateSetting(nameof(tmp._buySellToggle) + playerPostfix, "");
                tmp._switchToBuy = CreateSetting(nameof(tmp._switchToBuy) + playerPostfix, "");
                tmp._switchToSell = CreateSetting(nameof(tmp._switchToSell) + playerPostfix, "");

                // Events
                int id = i;
                tmp._separateBuySellPanels.AddEvent(() =>
                {
                    if (tmp._separateBuySellPanels == SeperatePanelsMode.Disabled)
                        TryDisableSeparateMode(id);
                    else
                    {
                        tmp._swapPendingBuySellPanels.Value = true;
                        TrySwitchToPanel(id, true);
                    }
                });
                tmp._swapPendingBuySellPanels.AddEvent(() => TrySwapBuyingSellingItemsPanels(id));
                tmp._disableQuickslotButtonIcons.AddEvent(() => TryUpdateQuickslotButtonIcons(id));
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
            _verticalSplitscreen.Format("Vertical splitscreen");
            _verticalSplitscreen.Description = "For monitors that are more wide than tall";
        }
        public void OnUpdate()
        {
            foreach (var player in Players.Local)
                if (player.UI.m_displayedMenuIndex == CharacterUI.MenuScreens.Shop)
                    switch (_perPlayerData[player.ID]._separateBuySellPanels.Value)
                    {
                        case SeperatePanelsMode.Toggle:
                            if (_perPlayerData[player.ID]._buySellToggle.Value.ToKeyCode().Pressed())
                                TryToggleBuySellPanel(player.ID);
                            break;
                        case SeperatePanelsMode.TwoButtons:
                            if (_perPlayerData[player.ID]._switchToBuy.Value.ToKeyCode().Pressed())
                                TrySwitchToPanel(player.ID, true);
                            else if (_perPlayerData[player.ID]._switchToSell.Value.ToKeyCode().Pressed())
                                TrySwitchToPanel(player.ID, false);
                            break;
                    }
        }

        // Disable quickslot button icons
        static private void TryUpdateQuickslotButtonIcons(int playerID)
        {
            #region quit
            Players.Data player = Players.GetLocal(playerID);
            if (player == null)
                return;
            #endregion

            foreach (var quickslotDisplay in GetKeyboardQuickslotsGamePanel(player.UI).GetAllComponentsInHierarchy<QuickSlotDisplay>())
                quickslotDisplay.m_lblKeyboardInput.enabled = !_perPlayerData[player.ID]._disableQuickslotButtonIcons;
        }
        static private Transform GetKeyboardQuickslotsGamePanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/HUD/QuickSlot/Keyboard");
        // Separate buy/sell panels
        static private void TrySwitchToPanel(int playerID, bool buyPanel)
        {
            #region quit
            Players.Data player = Players.GetLocal(playerID);
            if (player == null)
                return;
            #endregion

            GetPlayerInventoryPanel(player.UI).GOSetActive(!buyPanel);
            GetMerchantInventoryPanel(player.UI).GOSetActive(buyPanel);
        }
        static private void TryToggleBuySellPanel(int playerID)
        {
            #region quit
            Players.Data player = Players.GetLocal(playerID);
            if (player == null)
                return;
            #endregion

            Transform playerInventory = GetPlayerInventoryPanel(player.UI);
            Transform merchantInventory = GetMerchantInventoryPanel(player.UI);
            playerInventory.GOToggle();
            merchantInventory.GOSetActive(!playerInventory.GOActive());
        }
        static private void TryDisableSeparateMode(int playerID)
        {
            #region quit
            Players.Data player = Players.GetLocal(playerID);
            if (player == null)
                return;
            #endregion

            GetPlayerInventoryPanel(player.UI).GOSetActive(true);
            GetMerchantInventoryPanel(player.UI).GOSetActive(true);
        }
        // Swap pending buy/sell panels
        static private void TrySwapBuyingSellingItemsPanels(int playerID)
        {
            #region quit
            Players.Data player = Players.GetLocal(playerID);
            if (player == null)
                return;
            #endregion

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

            if (_perPlayerData[playerID]._swapPendingBuySellPanels == isModified)
                return;

            // Execute
            Utility.SwapHierarchyPositions(pendingBuy, pendingSell);
            pendingBuy.SetAsFirstSibling();
            pendingSell.SetAsFirstSibling();
        }
        static private Transform GetPlayerInventoryPanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/ShopMenu/MiddlePanel/PlayerInventory");
        static private Transform GetMerchantInventoryPanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/ShopMenu/MiddlePanel/ShopInventory");
        static private Transform GetPendingBuyPanel(Transform inventory)
        => inventory.Find("SectionContent/Scroll View/Viewport/Content/PlayerPendingBuyItems");
        static private Transform GetPendingSellPanel(Transform inventory)
        => inventory.Find("SectionContent/Scroll View/Viewport/Content/PlayerPendingSellItems");
        // Vertical splitscreen
        static private void TryUpdateSplitscreenMode()
        {
            #region quit
            if (SplitScreenManager.Instance == null)
                return;
            #endregion

            SplitScreenManager.Instance.CurrentSplitType = _verticalSplitscreen && Players.Local.Count >= 2
                                                         ? SplitScreenManager.SplitType.Vertical
                                                         : SplitScreenManager.SplitType.Horizontal;
            foreach (var player in Players.Local)
                player.UI.DelayedRefreshSize();
        }

        // Hooks
        [HarmonyPatch(typeof(LocalCharacterControl), "RetrieveComponents"), HarmonyPostfix]
        static void LocalCharacterControl_RetrieveComponents_Post(LocalCharacterControl __instance)
        {
            int playerID = __instance.Character.OwnerPlayerSys.PlayerID;
            TryUpdateQuickslotButtonIcons(playerID);
            TrySwapBuyingSellingItemsPanels(playerID);
            TryUpdateSplitscreenMode();
        }

        // Vertical splitscreen
        [HarmonyPatch(typeof(RPCManager), "SendPlayerHasLeft"), HarmonyPostfix]
        static void RPCManager_SendPlayerHasLeft_Post()
        => TryUpdateSplitscreenMode();

        // Exclusive buy/sell panels
        [HarmonyPatch(typeof(ShopMenu), "Show"), HarmonyPostfix]
        static void ShopMenu_Show_Post(ref ShopMenu __instance)
        {
            int playerID = __instance.ToPlayerID();
            #region quit
            if (_perPlayerData[playerID]._separateBuySellPanels == SeperatePanelsMode.Disabled)
                return;
            #endregion

            TrySwitchToPanel(playerID, true);
        }

        // Disable quickslot button icons
        [HarmonyPatch(typeof(QuickSlotDisplay), "Update"), HarmonyPostfix]
        static void QuickSlotDisplay_Update_Post(ref QuickSlotDisplay __instance)
        {
            int playerID = __instance.ToPlayerID();
            #region quit
            if (!_perPlayerData[playerID]._disableQuickslotButtonIcons)
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