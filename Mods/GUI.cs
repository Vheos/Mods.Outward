using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;

namespace ModPack
{
    public class GUI : AMod, IDelayedInit, IUpdatable
    {
        #region const
        static public readonly Vector2 DEFAULT_SHOP_OFFSET_MIN = new Vector2(-1344f, -540f);
        static public readonly Vector2 DEFAULT_SHOP_OFFSET_MAX = new Vector2(-20f, -20f);
        static public readonly (Vector2 Default, Vector2 Alternative) MANA_BAR_POSITIONS = (new Vector2(65f, 18.6f), new Vector2(10f, 83f));
        private const float UI_RESIZE_DELAY = 0.1f;
        private const int SORTING_TYPES_COUNT = 6;
        static private readonly Dictionary<HUDGroup, (Type HUDComponentType, string PanelPath, Vector2 DefaultLocalPosition)> DATA_BY_HUD_GROUP = new Dictionary<HUDGroup, (Type, string, Vector2)>
        {
            [HUDGroup.KeyboardQuickslots] = (typeof(KeyboardQuickSlotPanel), "QuickSlot/Keyboard", new Vector2(5, -40)),
            [HUDGroup.GamepadQuickslots] = (typeof(QuickSlotPanelSwitcher), "QuickSlot/Controller", new Vector2(0, 0)),
            [HUDGroup.Vitals] = (typeof(CharacterBarListener), "MainCharacterBars", new Vector2(-947, -506)),
            [HUDGroup.StatusEffects] = (typeof(StatusEffectPanel), "StatusEffect - Panel", new Vector2(-760.81f, -532)),
            [HUDGroup.Temperature] = (typeof(TemperatureExposureDisplay), "TemperatureSensor", new Vector2(-804.602f, -490.5f)),
            [HUDGroup.Arrows] = (typeof(QuiverDisplay), "QuiverDisplay", new Vector2(-936.3f, -340)),
            [HUDGroup.Compass] = (typeof(UICompass), "Compass", new Vector2(0, 500)),
            [HUDGroup.Stability] = (typeof(StabilityDisplay_Simple), "Stability", new Vector2(0, -519)),
        };
        #endregion
        #region enum
        private enum SeperatePanelsMode
        {
            Disabled = 0,
            Toggle = 1,
            TwoButtons = 2,
        }
        private enum HUDGroup
        {
            KeyboardQuickslots,
            GamepadQuickslots,
            Vitals,
            StatusEffects,
            Temperature,
            Arrows,
            Compass,
            Stability,
            Notifications,
        }
        private enum SettingsOperation
        {
            Save,
            Load,
            Reset,
        }
        private enum Tool
        {
            Move,
            Scale,
        }
        #endregion
        #region class
        private class PerPlayerSettings
        {
            // Settings
            public ModSetting<bool> _toggle;
            public ModSetting<bool> _copySettings;
            public ModSetting<bool> _rearrangeHUD;
            public ModSetting<bool> _startHUDEditor;
            public ModSetting<int> _hudTransparency;
            public ModSetting<bool> _fadingStatusEffectIcons;
            public ModSetting<int> _statusIconMaxSize, _statusIconMinSize, _statusIconMinAlpha;
            public ModSetting<bool> _hideQuickslotHints;
            public ModSetting<bool> _alternativeManaBarPlacement;
            public ModSetting<int> _shopAndStashWidth;
            public ModSetting<bool> _improvedGamepadStashNavigation;
            public ModSetting<bool> _swapPendingBuySellPanels;
            public ModSetting<SeperatePanelsMode> _separateBuySellPanels;
            public ModSetting<string> _buySellToggle, _switchToBuy, _switchToSell;

            // Utility
            public Dictionary<HUDGroup, ModSetting<Vector3>> _hudOverridesByHUDGroup;
            public Vector2 GetPosition(HUDGroup hudGroup)
            => _hudOverridesByHUDGroup[hudGroup].Value;
            public float GetScale(HUDGroup hudGroup)
            => _hudOverridesByHUDGroup[hudGroup].Value.z;
            public void Set(HUDGroup hudGroup, Vector2 position, float scale)
            => _hudOverridesByHUDGroup[hudGroup].Value = position.Append(scale);
            public void ResetHUDOverrides()
            {
                foreach (var hudOverrideByHUDGroup in _hudOverridesByHUDGroup)
                    hudOverrideByHUDGroup.Value.Value = Vector3.zero;
            }
            public bool IsNotZero(HUDGroup hudGroup)
            => _hudOverridesByHUDGroup[hudGroup] != Vector3.zero;
            public Vector2 StatusIconScale(float progress)
            => _statusIconMinSize.Value.Lerp(_statusIconMaxSize, progress).Div(100f).ToVector2();
            public float StatusIconAlpha(float progress)
            => _statusIconMinAlpha.Value.Div(100).Lerp(1f, progress);
            public void CopySettings(PerPlayerSettings otherPlayerSettings)
            {
                _rearrangeHUD.Value = otherPlayerSettings._rearrangeHUD;
                _hudTransparency.Value = otherPlayerSettings._hudTransparency;
                _fadingStatusEffectIcons.Value = otherPlayerSettings._fadingStatusEffectIcons;
                _statusIconMaxSize.Value = otherPlayerSettings._statusIconMaxSize;
                _statusIconMinSize.Value = otherPlayerSettings._statusIconMinSize;
                _statusIconMinAlpha.Value = otherPlayerSettings._statusIconMinAlpha;
                _hideQuickslotHints.Value = otherPlayerSettings._hideQuickslotHints;
                _alternativeManaBarPlacement.Value = otherPlayerSettings._alternativeManaBarPlacement;
                _shopAndStashWidth.Value = otherPlayerSettings._shopAndStashWidth;
                _swapPendingBuySellPanels.Value = otherPlayerSettings._swapPendingBuySellPanels;
                _improvedGamepadStashNavigation = otherPlayerSettings._improvedGamepadStashNavigation;
                _separateBuySellPanels.Value = otherPlayerSettings._separateBuySellPanels;
                _buySellToggle.Value = otherPlayerSettings._buySellToggle;
                _switchToBuy.Value = otherPlayerSettings._switchToBuy;
                _switchToSell.Value = otherPlayerSettings._switchToSell;
                _hudOverridesByHUDGroup = new Dictionary<HUDGroup, ModSetting<Vector3>>(otherPlayerSettings._hudOverridesByHUDGroup);
            }

            // Constructor
            public PerPlayerSettings()
            => _hudOverridesByHUDGroup = new Dictionary<HUDGroup, ModSetting<Vector3>>();
        }
        #endregion

        // Setting
        static private PerPlayerSettings[] _perPlayerSettings;
        static private ModSetting<bool> _verticalSplitscreen;
        override protected void Initialize()
        {
            _verticalSplitscreen = CreateSetting(nameof(_verticalSplitscreen), false);
            AddEventOnConfigClosed(() => UpdateSplitscreenMode());
            AddEventOnConfigClosed(() => UpdateShopAndStashPanelsWidths());

            _allHUDComponentTypes = new Type[DATA_BY_HUD_GROUP.Count];
            var hudData = DATA_BY_HUD_GROUP.Values.ToArray();
            for (int i = 0; i < hudData.Length; i++)
                _allHUDComponentTypes[i] = hudData[i].HUDComponentType;

            _perPlayerSettings = new PerPlayerSettings[2];
            for (int i = 0; i < 2; i++)
            {
                PerPlayerSettings tmp = new PerPlayerSettings();
                _perPlayerSettings[i] = tmp;
                string playerPostfix = (i + 1).ToString();

                tmp._toggle = CreateSetting(nameof(tmp._toggle) + playerPostfix, false);
                tmp._copySettings = CreateSetting(nameof(tmp._copySettings) + playerPostfix, false);
                tmp._rearrangeHUD = CreateSetting(nameof(tmp._rearrangeHUD) + playerPostfix, false);
                tmp._startHUDEditor = CreateSetting(nameof(tmp._startHUDEditor) + playerPostfix, false);
                tmp._hudTransparency = CreateSetting(nameof(tmp._hudTransparency) + playerPostfix, 0, IntRange(0, 100));
                tmp._fadingStatusEffectIcons = CreateSetting(nameof(tmp._fadingStatusEffectIcons) + playerPostfix, false);
                tmp._statusIconMaxSize = CreateSetting(nameof(tmp._statusIconMaxSize) + playerPostfix, 120, IntRange(100, 125));
                tmp._statusIconMinSize = CreateSetting(nameof(tmp._statusIconMinSize) + playerPostfix, 60, IntRange(0, 100));
                tmp._statusIconMinAlpha = CreateSetting(nameof(tmp._statusIconMinAlpha) + playerPostfix, 50, IntRange(0, 100));
                tmp._hideQuickslotHints = CreateSetting(nameof(tmp._hideQuickslotHints) + playerPostfix, false);
                tmp._alternativeManaBarPlacement = CreateSetting(nameof(tmp._alternativeManaBarPlacement) + playerPostfix, false);
                foreach (var hudGroup in DATA_BY_HUD_GROUP.Keys.ToArray())
                    tmp._hudOverridesByHUDGroup.Add(hudGroup, CreateSetting($"_hudOverride{hudGroup}{playerPostfix}", Vector3.zero));
                tmp._shopAndStashWidth = CreateSetting(nameof(tmp._shopAndStashWidth) + playerPostfix, 0, IntRange(0, 100));
                tmp._improvedGamepadStashNavigation = CreateSetting(nameof(tmp._improvedGamepadStashNavigation) + playerPostfix, false);
                tmp._swapPendingBuySellPanels = CreateSetting(nameof(tmp._swapPendingBuySellPanels) + playerPostfix, false);
                tmp._separateBuySellPanels = CreateSetting(nameof(tmp._separateBuySellPanels) + playerPostfix, SeperatePanelsMode.Disabled);
                tmp._buySellToggle = CreateSetting(nameof(tmp._buySellToggle) + playerPostfix, "");
                tmp._switchToBuy = CreateSetting(nameof(tmp._switchToBuy) + playerPostfix, "");
                tmp._switchToSell = CreateSetting(nameof(tmp._switchToSell) + playerPostfix, "");

                // Events
                int id = i;
                tmp._copySettings.AddEvent(() =>
                {
                    if (tmp._copySettings)
                        tmp.CopySettings(_perPlayerSettings[1 - id]);
                    tmp._copySettings.SetSilently(false);
                });
                tmp._rearrangeHUD.AddEvent(() =>
                {
                    if (Players.TryGetLocal(id, out Players.Data player))
                        SaveLoadHUDOverrides(player, tmp._rearrangeHUD ? SettingsOperation.Load : SettingsOperation.Reset);
                    if (!tmp._rearrangeHUD)
                        _perPlayerSettings[id].ResetHUDOverrides();
                });
                tmp._startHUDEditor.AddEvent(() =>
                {
                    if (tmp._startHUDEditor && Players.TryGetLocal(id, out Players.Data player))
                        SetHUDEditor(player, true);
                });
                tmp._hudTransparency.AddEvent(() =>
                {
                    if (Players.TryGetLocal(id, out Players.Data player))
                        UpdateHUDTransparency(player);
                });
                tmp._fadingStatusEffectIcons.AddEvent(() =>
                {
                    if (!tmp._fadingStatusEffectIcons && Players.TryGetLocal(id, out Players.Data player))
                        ResetStatusEffectIcons(player);
                });
                tmp._hideQuickslotHints.AddEvent(() =>
                {
                    if (Players.TryGetLocal(id, out Players.Data player))
                        UpdateQuickslotButtonIcons(player);
                });
                tmp._alternativeManaBarPlacement.AddEvent(() =>
                {
                    if (Players.TryGetLocal(id, out Players.Data player))
                        UpdateManaBarPlacement(player);
                });
                AddEventOnConfigOpened(() =>
                {
                    if (tmp._startHUDEditor && Players.TryGetLocal(id, out Players.Data player))
                        SetHUDEditor(player, false);
                });
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
            }
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
                    tmp._copySettings.Format($"Copy settings from player {1 - i + 1}", tmp._toggle);
                    tmp._copySettings.IsAdvanced = true;
                    tmp._rearrangeHUD.Format("Rearrange HUD", tmp._toggle);
                    tmp._rearrangeHUD.Description = "Change HUD elements position and scale";
                    Indent++;
                    {
                        tmp._startHUDEditor.Format("Edit mode", tmp._rearrangeHUD);
                        tmp._startHUDEditor.Description = "Pause the game and start rearranging HUD elements:\n" +
                                                          "Left mouse button - move\n" +
                                                          "Right muse button - scale\n" +
                                                          "Enter - save settings";
                        Indent--;
                    }
                    tmp._hudTransparency.Format("HUD transparency", tmp._toggle);
                    tmp._fadingStatusEffectIcons.Format("Fading status effect icons", tmp._toggle);
                    Indent++;
                    {
                        tmp._statusIconMaxSize.Format("Max size", tmp._fadingStatusEffectIcons);
                        tmp._statusIconMaxSize.Description = "Icon size at maximum status effect duration";
                        tmp._statusIconMinSize.Format("Min size", tmp._fadingStatusEffectIcons);
                        tmp._statusIconMinSize.Description = "Icon size right before the status effect expires";
                        tmp._statusIconMinAlpha.Format("Min opacity", tmp._fadingStatusEffectIcons);
                        tmp._statusIconMinAlpha.Description = "Icon opacity right before the status effect expires";
                        Indent--;
                    }
                    tmp._hideQuickslotHints.Format("Hide quickslot hints", tmp._toggle);
                    tmp._hideQuickslotHints.Description = "Keyboard - hides the key names above quickslots\n" +
                                                          "Gamepad - hides the button icons below quickslots";
                    tmp._alternativeManaBarPlacement.Format("Alternative mana bar placement", tmp._toggle);
                    tmp._alternativeManaBarPlacement.Description = "Move mana bar right below health bar to form a triangle out of the vitals";
                    tmp._shopAndStashWidth.Format("Shop/stash panel width", tmp._toggle);
                    tmp._shopAndStashWidth.Description = "% of screen size, 0% = default\n" +
                                                     "(recommended when using vertical splitscreen)";
                    tmp._improvedGamepadStashNavigation.Format("Better gamepad stash navigation");
                    tmp._improvedGamepadStashNavigation.Description = "LB = switch to (or scroll down in) the player's bag\n" +
                                                                      "RB = switch to (or scroll downn in) the chest contents\n" +
                                                                      "LT = change sorting (default, by weight, by durability)\n" +
                                                                      "RT = find currently focused item in the other panel";
                    tmp._separateBuySellPanels.Format("Separate buy/sell panels", tmp._toggle);
                    tmp._separateBuySellPanels.Description = "Disabled - shops display player's and merchant's inventory in one panel\n" +
                                                             "Toggle - toggle between player's / merchant's inventories with one button\n" +
                                                             "TwoButtons - press one button for player's inventory and another for merchant's\n" +
                                                             "(recommended when using vertical splitscreen)";
                    tmp._swapPendingBuySellPanels.Format("Swap pending buy/sell panels", tmp._separateBuySellPanels, SeperatePanelsMode.Disabled);
                    tmp._swapPendingBuySellPanels.Description = "Items you're buying will be shown above the merchant's stock\n" +
                                                                "Items you're selling will be shown above your pouch";
                    Indent++;
                    {
                        tmp._buySellToggle.Format("Toggle buy/sell panels", tmp._separateBuySellPanels, SeperatePanelsMode.Toggle);
                        tmp._switchToBuy.Format("Switch to buy panel", tmp._separateBuySellPanels, SeperatePanelsMode.TwoButtons);
                        tmp._switchToSell.Format("Switch to sell panel", tmp._separateBuySellPanels, SeperatePanelsMode.TwoButtons);
                        Indent--;
                    }

                    Indent--;
                }
            }
        }
        override protected string Description
        => "• Rearrange HUD elements\n" +
           "• Vertical splitscreen (with shop tweaks)";
        public void OnUpdate()
        {
            foreach (var player in Players.Local)
            {
                // Shop/Stash panels
                PerPlayerSettings settings = _perPlayerSettings[player.ID];
                switch (player.UI.m_displayedMenuIndex)
                {
                    case CharacterUI.MenuScreens.Shop:
                        switch (settings._separateBuySellPanels.Value)
                        {
                            case SeperatePanelsMode.Toggle:
                                if (player.IsUsingGamepad && (player.Pressed(ControlsInput.MenuActions.GoToPreviousMenu) || player.Pressed(ControlsInput.MenuActions.GoToNextMenu))
                                || !player.IsUsingGamepad && settings._buySellToggle.Value.ToKeyCode().Pressed())
                                    ToggleBuySellPanel(player);
                                break;
                            case SeperatePanelsMode.TwoButtons:
                                if (player.IsUsingGamepad && player.Pressed(ControlsInput.MenuActions.GoToPreviousMenu)
                                || !player.IsUsingGamepad && settings._switchToSell.Value.ToKeyCode().Pressed())
                                    SwitchToBuySellPanel(player, false);
                                else if (player.IsUsingGamepad && player.Pressed(ControlsInput.MenuActions.GoToNextMenu)
                                || !player.IsUsingGamepad && settings._switchToBuy.Value.ToKeyCode().Pressed())
                                    SwitchToBuySellPanel(player, true);
                                break;
                        }
                        break;

                    case CharacterUI.MenuScreens.Stash:
                        if (settings._improvedGamepadStashNavigation && player.IsUsingGamepad)
                            if (player.Pressed(ControlsInput.MenuActions.GoToNextMenu))
                                SwitchToStash(player);
                            else if (player.Pressed(ControlsInput.MenuActions.GoToPreviousMenu))
                                SwitchToInventory(player);
                            else if (player.Pressed(ControlsInput.MenuActions.PreviousFilter))
                                ChangeSorting(player);
                            else if (player.Pressed(ControlsInput.MenuActions.NextFilter))
                                FindSameItemInOtherPanel(player);
                        break;
                }

                // HUD Editor
                if (settings._startHUDEditor)
                    if (KeyCode.Return.Pressed())
                    {
                        SetHUDEditor(player, false);
                        continue;
                    }
                    else if (_hudEditFocus.Transform == null)
                    {
                        if (KeyCode.Mouse0.Pressed())
                            HandleHUDHits(player, Tool.Move);
                        else if (KeyCode.Mouse1.Pressed())
                            HandleHUDHits(player, Tool.Scale);
                    }
                    else
                        switch (_hudEditTool)
                        {
                            case Tool.Move:
                                Vector2 mouseToElementOffset = _hudEditFocus.EditData;
                                _hudEditFocus.Transform.position = Input.mousePosition.XY() + mouseToElementOffset;
                                if (KeyCode.Mouse0.Released())
                                    _hudEditFocus.Transform = null;
                                break;
                            case Tool.Scale:
                                float clickY = _hudEditFocus.EditData.x;
                                float clickScale = _hudEditFocus.EditData.y;
                                float offset = Input.mousePosition.y - clickY;
                                _hudEditFocus.Transform.localScale = (offset / Screen.height + clickScale).ToVector3();
                                if (KeyCode.Mouse1.Released())
                                    _hudEditFocus.Transform = null;
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
        static private void UpdateShopAndStashPanelsWidths()
        {
            foreach (var player in Players.Local)
            {
                // Choose
                PerPlayerSettings playerData = _perPlayerSettings[player.ID];
                Vector2 offsetMin = DEFAULT_SHOP_OFFSET_MIN;
                Vector2 offsetMax = DEFAULT_SHOP_OFFSET_MAX;
                if (playerData._shopAndStashWidth > 0)
                {
                    float maxWidth = player.UI.m_rectTransform.rect.width.Neg();
                    float multiplier = playerData._shopAndStashWidth / 100f;
                    offsetMin.SetX(maxWidth * multiplier);
                    offsetMax = Vector2.zero;
                }
                // Set
                foreach (var rectTransform in new[] { GetShopPanel(player.UI), GetStashPanel(player.UI) })
                {
                    rectTransform.offsetMin = offsetMin;
                    rectTransform.offsetMax = offsetMax;
                }
            }
        }
        static private void UpdateQuickslotButtonIcons(Players.Data player)
        {
            foreach (var quickslotDisplay in GetKeyboardQuickslotsGamePanel(player.UI).GetAllComponentsInHierarchy<QuickSlotDisplay>())
                quickslotDisplay.m_lblKeyboardInput.enabled = !_perPlayerSettings[player.ID]._hideQuickslotHints;
        }
        static private void SwitchToInventory(Players.Data player)
        {
            if (EventSystem.current.currentSelectedGameObject.TryGetComponent(out ItemDisplay currentItem)
            && currentItem.m_refItem == null)
                return;

            // Cache
            InventoryContentDisplay inventory = GetPlayerStashInventoryPanel(GetStashPanel(player.UI)).GetComponent<InventoryContentDisplay>();
            List<ItemDisplay> pouchItems = inventory.m_pouchDisplay.m_assignedDisplays;
            List<ItemDisplay> bagItems = inventory.m_bagDisplay.m_assignedDisplays;
            int currentID = bagItems.IndexOf(currentItem);

            // Execute
            if (currentID >= bagItems.Count - 1)
                bagItems.First().OnSelect();
            else if (currentID >= 0)
            {
                int nextID = currentID + bagItems.Count / 2;
                if (bagItems.IsIndexValid(nextID))
                    bagItems[nextID].OnSelect();
                else
                    bagItems.Last().OnSelect();
            }
            else if (bagItems.IsNotEmpty())
                bagItems.First().OnSelect();
            else if (pouchItems.IsNotEmpty())
                pouchItems.First().OnSelect();
        }
        static private void SwitchToStash(Players.Data player)
        {
            if (EventSystem.current.currentSelectedGameObject.TryGetComponent(out ItemDisplay currentItem)
            && currentItem.m_refItem == null)
                return;

            // Cache
            ContainerDisplay chest = GetChestStashInventoryPanel(GetStashPanel(player.UI)).GetComponent<ContainerDisplay>();
            List<ItemDisplay> chestItems = chest.m_assignedDisplays;
            int currentID = chestItems.IndexOf(currentItem);

            // Execute
            if (currentID >= chestItems.Count - 1)
                chestItems.First().OnSelect();
            else if (currentID >= 0)
            {
                int nextID = currentID + chestItems.Count / 2;
                if (chestItems.IsIndexValid(nextID))
                    chestItems[nextID].OnSelect();
                else
                    chestItems.Last().OnSelect();
            }
            else if (chestItems.IsNotEmpty())
                chestItems.First().OnSelect();
        }
        static private void ChangeSorting(Players.Data player)
        {
            // Cache
            Transform stashPanelHolder = GetStashPanel(player.UI);
            InventoryContentDisplay inventory = GetPlayerStashInventoryPanel(stashPanelHolder).GetComponent<InventoryContentDisplay>();
            ContainerDisplay chestDisplay = GetChestStashInventoryPanel(stashPanelHolder).GetComponent<ContainerDisplay>();

            // Sort
            ItemListDisplay.SortingType nextSorting = default;
            switch (chestDisplay.m_lastSortingType)
            {
                case ItemListDisplay.SortingType.ByList: nextSorting = ItemListDisplay.SortingType.ByWeight; break;
                case ItemListDisplay.SortingType.ByWeight: nextSorting = ItemListDisplay.SortingType.ByDurability; break;
                case ItemListDisplay.SortingType.ByDurability: nextSorting = ItemListDisplay.SortingType.ByList; break;
            }
            foreach (var containerDisplay in new[] { inventory.m_pouchDisplay, inventory.m_bagDisplay, chestDisplay })
                containerDisplay.SortBy(nextSorting);
            UpdateStashName(player);

            // Select first
            if (EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.TryGetComponent(out ItemDisplay currentItem)
            && currentItem.ParentItemListDisplay != null)
                currentItem.ParentItemListDisplay.m_assignedDisplays.First().OnSelect();

        }
        static private void UpdateStashName(Players.Data player)
        {
            // Cache
            Transform stashPanelHolder = GetStashPanel(player.UI);
            ContainerDisplay chestDisplay = GetChestStashInventoryPanel(stashPanelHolder).GetComponent<ContainerDisplay>();
            Text stashTitle = GetChestStashTitle(stashPanelHolder).GetComponent<Text>();

            // Execute
            stashTitle.text = "Stash";
            switch (chestDisplay.m_lastSortingType)
            {
                case ItemListDisplay.SortingType.ByList: break;
                case ItemListDisplay.SortingType.ByWeight: stashTitle.text += "<color=lime> (sorted by Weight)</color>"; break;
                case ItemListDisplay.SortingType.ByDurability: stashTitle.text += "<color=orange> (sorted by Durability)</color>"; break;
            }
        }
        static private void FindSameItemInOtherPanel(Players.Data player)
        {
            if (EventSystem.current.currentSelectedGameObject.TryGetComponent(out ItemDisplay currentItem) && currentItem.m_refItem == null)
                return;

            // Cache
            Transform stashPanelHolder = GetStashPanel(player.UI);
            InventoryContentDisplay inventory = GetPlayerStashInventoryPanel(stashPanelHolder).GetComponent<InventoryContentDisplay>();
            List<ItemDisplay> chestItems = GetChestStashInventoryPanel(stashPanelHolder).GetComponent<ContainerDisplay>().m_assignedDisplays;
            List<ItemDisplay> bagItems = inventory.m_bagDisplay.m_assignedDisplays;
            List<ItemDisplay> pouchItems = inventory.m_pouchDisplay.m_assignedDisplays;

            // Execute
            ItemDisplay foundItem;
            if (currentItem.IsContainedIn(chestItems))
            {
                foundItem = FindItemInContainerDisplay(currentItem, bagItems);
                if (foundItem == null)
                    foundItem = FindItemInContainerDisplay(currentItem, pouchItems);
            }
            else if (currentItem.IsContainedIn(bagItems) || currentItem.IsContainedIn(pouchItems))
                foundItem = FindItemInContainerDisplay(currentItem, chestItems);
            else
                return;

            foundItem.OnSelect();
        }
        static private ItemDisplay FindItemInContainerDisplay(ItemDisplay item, List<ItemDisplay> otherContainerItems)
        {
            foreach (var otherItem in otherContainerItems)
                if (otherItem.m_refItem.ItemID == item.m_refItem.ItemID)
                    return otherItem;
            return null;
        }
        static private void UpdateSeparateBuySellPanels(Players.Data player)
        {
            if (_perPlayerSettings[player.ID]._separateBuySellPanels == SeperatePanelsMode.Disabled)
                DisableSeparateMode(player);
            else
                SwitchToBuySellPanel(player, true);
        }
        static private void SwitchToBuySellPanel(Players.Data player, bool buyPanel)
        {
            Transform shopPanelHolder = GetShopPanel(player.UI);
            GetPlayerShopInventoryPanel(shopPanelHolder).GOSetActive(!buyPanel);
            GetMerchantShopInventoryPanel(shopPanelHolder).GOSetActive(buyPanel);
            shopPanelHolder.GetComponent<ShopMenu>().GetFirstSelectable().Select();

        }
        static private void ToggleBuySellPanel(Players.Data player)
        {
            bool isBuyPanel = GetMerchantShopInventoryPanel(GetShopPanel(player.UI)).GOActive();
            SwitchToBuySellPanel(player, !isBuyPanel);
        }
        static private void DisableSeparateMode(Players.Data player)
        {
            GetPlayerShopInventoryPanel(GetShopPanel(player.UI)).GOSetActive(true);
            GetMerchantShopInventoryPanel(GetShopPanel(player.UI)).GOSetActive(true);
        }
        static private void UpdatePendingBuySellPanels(Players.Data player)
        {
            // Cache
            Transform playerInventory = GetPlayerShopInventoryPanel(GetShopPanel(player.UI));
            Transform merchantInventory = GetMerchantShopInventoryPanel(GetShopPanel(player.UI));

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
        static private void UpdateManaBarPlacement(Players.Data player)
        {
            Transform manaBarHolder = GetManaBarHolder(GetHUDHolder(player.UI));
            bool altPlacement = _perPlayerSettings[player.ID]._alternativeManaBarPlacement;
            manaBarHolder.localPosition = altPlacement ? MANA_BAR_POSITIONS.Alternative : MANA_BAR_POSITIONS.Default;
        }
        static private void UpdateHUDTransparency(Players.Data player)
        => GetHUDHolder(player.UI).GetComponent<CanvasGroup>().alpha = 1f - _perPlayerSettings[player.ID]._hudTransparency / 100f;
        static private void ResetStatusEffectIcons(Players.Data player)
        {
            Transform statusEffectsPanel = GetHUDHolder(player.UI).Find("StatusEffect - Panel");
            foreach (var image in statusEffectsPanel.GetAllComponentsInHierarchy<Image>())
            {
                image.SetAlpha(1f);
                image.rectTransform.localScale = Vector2.one;
            }
        }
        // HUD editor      
        static private void SetHUDEditor(Players.Data player, bool state)
        {
            PauseMenu.Pause(state);
            GameInput.ForceCursorNavigation = state;
            SetHUDTemplates(player, state);

            if (state)
            {
                Tools.IsConfigOpen = false;
                SetupHUDElements(player);
                _hudEditFocus.Transform = null;
            }
            else
            {
                SaveLoadHUDOverrides(player, SettingsOperation.Save);
                _perPlayerSettings[player.ID]._startHUDEditor.SetSilently(false);
            }
        }
        static private void SetupHUDElements(Players.Data player)
        {
            foreach (var dataByHUDGroup in DATA_BY_HUD_GROUP)
                foreach (var uiElement in GetHUDHolder(player.UI).Find(dataByHUDGroup.Value.PanelPath).GetAllComponentsInHierarchy<CanvasGroup, Image>())
                    switch (uiElement)
                    {
                        case CanvasGroup t: t.blocksRaycasts = true; break;
                        case Image t: t.raycastTarget = true; break;
                    }
        }
        static private void SetHUDTemplates(Players.Data player, bool state)
        {
            Transform hudHolder = GetHUDHolder(player.UI);
            Transform temperature = hudHolder.Find("TemperatureSensor/Display");
            Transform statusEffect = hudHolder.Find("StatusEffect - Panel/Icon");
            Transform quickslotsHolder = hudHolder.Find("QuickSlot");
            Transform arrowsHolder = hudHolder.Find("QuiverDisplay");
            Transform pauseHolder = player.UI.transform.Find("Canvas/Paused");

            temperature.GOSetActive(state);
            statusEffect.GetComponent<StatusEffectIcon>().enabled = !state;
            statusEffect.GOSetActive(state);
            quickslotsHolder.GetComponent<QuickSlotControllerSwitcher>().enabled = !state;
            if (state)
                foreach (Transform child in quickslotsHolder.transform)
                    child.GOSetActive(true);
            arrowsHolder.GetComponent<QuiverDisplay>().enabled = !state;
            arrowsHolder.GetComponent<QuiverDisplay>().m_canvasGroup.alpha = 1f;
            pauseHolder.GetComponent<Image>().enabled = !state;

        }
        static private void SaveLoadHUDOverrides(Players.Data player, SettingsOperation operation)
        {
            PerPlayerSettings settings = _perPlayerSettings[player.ID];
            player.UI.m_rectTransform.sizeDelta = Vector2.zero;
            player.UI.m_rectTransform.anchoredPosition = Vector2.zero;
            foreach (var dataByHUDGroup in DATA_BY_HUD_GROUP)
            {
                HUDGroup group = dataByHUDGroup.Key;
                Transform panel = GetHUDHolder(player.UI).Find(dataByHUDGroup.Value.PanelPath);
                switch (operation)
                {
                    case SettingsOperation.Save:
                        settings.Set(group, panel.localPosition, panel.localScale.x);
                        break;
                    case SettingsOperation.Load:
                        if (settings.IsNotZero(group))
                        {
                            panel.localPosition = settings.GetPosition(group);
                            panel.localScale = settings.GetScale(group).ToVector3();
                        }
                        break;
                    case SettingsOperation.Reset:
                        panel.localPosition = dataByHUDGroup.Value.DefaultLocalPosition;
                        panel.localScale = Vector2.one;
                        break;
                }
            }
            player.UI.DelayedRefreshSize();
        }
        static private void HandleHUDHits(Players.Data player, Tool tool)
        {
            foreach (var hit in GetHUDHolder(player.UI).GetOrAddComponent<GraphicRaycaster>().GetMouseHits())
            {
                Transform hudGroupHolder = hit.gameObject.FindAncestorWithComponent(_allHUDComponentTypes);
                if (hudGroupHolder == null)
                    continue;

                _hudEditFocus.Transform = hudGroupHolder;
                _hudEditTool = tool;
                if (tool == Tool.Move)
                    _hudEditFocus.EditData = Input.mousePosition.OffsetTo(hudGroupHolder.position).XY();
                else if (tool == Tool.Scale)
                    _hudEditFocus.EditData = new Vector2(Input.mousePosition.y, hudGroupHolder.localScale.x);
                break;
            }
        }
        static private Tool _hudEditTool;
        static private (Transform Transform, Vector2 EditData) _hudEditFocus;
        static private Type[] _allHUDComponentTypes;
        // Find
        static private Transform GetKeyboardQuickslotsGamePanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/HUD/QuickSlot/Keyboard");
        static private RectTransform GetStashPanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/Stash - Panel") as RectTransform;
        static private Transform GetPlayerStashInventoryPanel(Transform stashPanel)
        => stashPanel.Find("Content/MiddlePanel/PlayerInventory/InventoryContent");
        static private Transform GetChestStashInventoryPanel(Transform stashPanel)
        => stashPanel.Find("Content/MiddlePanel/StashInventory/SectionContent/Scroll View/Viewport/Content/ContainerDisplay_Simple");
        static private Transform GetChestStashTitle(Transform stashPanel)
        => stashPanel.Find("Content/TopPanel/Shop PanelTop/lblShopName");
        static private RectTransform GetShopPanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/ShopMenu") as RectTransform;
        static private Transform GetPlayerShopInventoryPanel(Transform shopPanel)
        => shopPanel.Find("MiddlePanel/PlayerInventory");
        static private Transform GetMerchantShopInventoryPanel(Transform shopPanel)
        => shopPanel.Find("MiddlePanel/ShopInventory");
        static private Transform GetPendingBuyPanel(Transform inventory)
        => inventory.Find("SectionContent/Scroll View/Viewport/Content/PlayerPendingBuyItems");
        static private Transform GetPendingSellPanel(Transform inventory)
        => inventory.Find("SectionContent/Scroll View/Viewport/Content/PlayerPendingSellItems");
        static private Transform GetHUDHolder(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/HUD");
        static private Transform GetManaBarHolder(Transform hudHolder)
        => hudHolder.Find("MainCharacterBars/Mana");

        // Hooks
        [HarmonyPatch(typeof(LocalCharacterControl), "RetrieveComponents"), HarmonyPostfix]
        static void LocalCharacterControl_RetrieveComponents_Post(LocalCharacterControl __instance)
        {
            UpdateSplitscreenMode();
            __instance.ExecuteOnceAfterDelay(UI_RESIZE_DELAY, UpdateShopAndStashPanelsWidths);

            Players.Data player = Players.GetLocal(__instance);
            UpdateQuickslotButtonIcons(player);
            UpdatePendingBuySellPanels(player);
            UpdateManaBarPlacement(player);
            UpdateHUDTransparency(player);
            if (_perPlayerSettings[player.ID]._rearrangeHUD)
                __instance.ExecuteOnceAfterDelay(UI_RESIZE_DELAY, () => SaveLoadHUDOverrides(player, SettingsOperation.Load));
        }

        [HarmonyPatch(typeof(RPCManager), "SendPlayerHasLeft"), HarmonyPostfix]
        static void RPCManager_SendPlayerHasLeft_Post(RPCManager __instance)
        {
            UpdateSplitscreenMode();
            __instance.ExecuteOnceAfterDelay(UI_RESIZE_DELAY, UpdateShopAndStashPanelsWidths);
        }

        // Stash panel name
        [HarmonyPatch(typeof(StashPanel), "Show"), HarmonyPostfix]
        static void StashPanel_Show_Post(ref StashPanel __instance)
        => UpdateStashName(Players.GetLocal(__instance));

        // Sort by weight    
        [HarmonyPatch(typeof(ItemListDisplay), "ByWeight"), HarmonyPrefix]
        static bool ItemListDisplay_ByWeight_Pre(ref ItemListDisplay __instance, ref int __result, ItemDisplay _item1, ItemDisplay _item2)
        {
            float weight1 = _item1.TryAs(out ItemGroupDisplay group1) ? group1.TotalWeight : _item1.m_refItem.Weight;
            float weight2 = _item2.TryAs(out ItemGroupDisplay group2) ? group2.TotalWeight : _item2.m_refItem.Weight;
            __result = -weight1.CompareTo(weight2);
            return false;
        }

        // Sort by durability    
        [HarmonyPatch(typeof(ItemListDisplay), "ByDurability"), HarmonyPrefix]
        static bool ItemListDisplay_ByDurability_Pre(ref int __result, ItemDisplay _display1, ItemDisplay _display2)
        {
            Item item1 = _display1.m_refItem;
            Item item2 = _display2.m_refItem;

            if (!item1.IsEquippable && item1.IsPerishable
            && !item2.IsEquippable && item2.IsPerishable)
            {
                float remainingTime1 = item1.CurrentDurability / item1.PerishScript.m_baseDepletionRate;
                float remainingTime2 = item2.CurrentDurability / item2.PerishScript.m_baseDepletionRate;
                __result = remainingTime1.CompareTo(remainingTime2);
                return false;
            }

            __result = -item1.IsPerishable.CompareTo(item2.IsPerishable);
            if (__result != 0)
                return false;

            __result = -(item1.MaxDurability >= 0).CompareTo(item2.MaxDurability >= 0);
            if (__result != 0)
                return false;

            __result = item1.CurrentDurability.CompareTo(item2.CurrentDurability);
            return false;
        }

        // Exclusive buy/sell panels
        [HarmonyPatch(typeof(ShopMenu), "Show"), HarmonyPostfix]
        static void ShopMenu_Show_Post(ref ShopMenu __instance)
        => UpdateSeparateBuySellPanels(Players.GetLocal(__instance));

        // Disable quickslot button icons
        [HarmonyPatch(typeof(QuickSlotDisplay), "Update"), HarmonyPostfix]
        static void QuickSlotDisplay_Update_Post(ref QuickSlotDisplay __instance)
        {
            Players.Data player = Players.GetLocal(__instance);
            #region quit
            if (!_perPlayerSettings[player.ID]._hideQuickslotHints)
                return;
            #endregion

            __instance.m_inputIcon.enabled = false;
            __instance.m_inputKeyboardIcon.SetActive(false);
        }

        // Vertical splitscreen
        [HarmonyPatch(typeof(CharacterUI), "DelayedRefreshSize"), HarmonyPostfix]
        static void CharacterUI_DelayedRefreshSize_Post(ref CharacterUI __instance)
        {
            #region quit
            if (SplitScreenManager.Instance.CurrentSplitType != SplitScreenManager.SplitType.Vertical)
                return;
            #endregion

            __instance.m_rectTransform.localPosition *= -1;
        }

        // Status effect duration
        [HarmonyPatch(typeof(StatusEffectPanel), "GetStatusIcon"), HarmonyPostfix]
        static void StatusEffectPanel_GetStatusIcon_Post(ref StatusEffectPanel __instance, ref StatusEffectIcon __result)
        {
            PerPlayerSettings settings = _perPlayerSettings[Players.GetLocal(__instance.LocalCharacter).ID];
            #region quit
            if (!settings._fadingStatusEffectIcons)
                return;
            #endregion

            StatusEffect statusEffect = __instance.m_cachedStatus;
            float progress;
            if (statusEffect.TryAs(out Disease disease) && disease.IsReceding)
            {
                float elapsed = GameTime - disease.m_healedGameTime;
                float duration = DiseaseLibrary.Instance.GetRecedingTime(disease.m_diseasesType);
                progress = 1f - elapsed / duration;
            }
            else if (statusEffect.Permanent)
                progress = 1f;
            else
            {
                float maxDuration = Prefabs.StatusEffectsByID[statusEffect.IdentifierName].StartLifespan;
                float remainingDuration = statusEffect.RemainingLifespan;
                progress = remainingDuration / maxDuration;
            }

            __result.m_icon.SetAlpha(settings.StatusIconAlpha(progress));
            __result.m_icon.rectTransform.localScale = settings.StatusIconScale(progress);
        }
    }
}