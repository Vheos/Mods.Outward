﻿namespace Vheos.Mods.Outward;
using UnityEngine.UI;

public class GUI : AMod, IDelayedInit, IUpdatable
{
    #region Constants
    public static readonly Vector2 DEFAULT_SHOP_OFFSET_MIN = new(-1344f, -540f);
    public static readonly Vector2 DEFAULT_SHOP_OFFSET_MAX = new(-20f, -20f);
    public static readonly (Vector2 Default, Vector2 Alternative) MANA_BAR_POSITIONS = (new Vector2(65f, 18.6f), new Vector2(10f, 83f));
    private const float UI_RESIZE_DELAY = 0.1f;
    private static readonly Dictionary<HUDGroup, (Type HUDComponentType, string PanelPath, Vector2 DefaultLocalPosition)> DATA_BY_HUD_GROUP = new()
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
    #region Enums
    private enum SeperatePanelsMode
    {
        Disabled,
        Toggle,
        TwoButtons,
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
        => (_statusIconMinAlpha.Value / 100f).Lerp(1f, progress);
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
            _separateBuySellPanels.Value = otherPlayerSettings._separateBuySellPanels;
            _buySellToggle.Value = otherPlayerSettings._buySellToggle;
            _switchToBuy.Value = otherPlayerSettings._switchToBuy;
            _switchToSell.Value = otherPlayerSettings._switchToSell;
            _hudOverridesByHUDGroup = new Dictionary<HUDGroup, ModSetting<Vector3>>(otherPlayerSettings._hudOverridesByHUDGroup);
        }

        // Constructor
        public PerPlayerSettings()
        {
            _hudOverridesByHUDGroup = new Dictionary<HUDGroup, ModSetting<Vector3>>();
        }
    }
    #endregion

    // Setting
    private static PerPlayerSettings[] _perPlayerSettings;
    private static ModSetting<bool> _verticalSplitscreen;
    private static ModSetting<bool> _separateMaps;
    private static ModSetting<int> _textScale;
    protected override void Initialize()
    {
        _verticalSplitscreen = CreateSetting(nameof(_verticalSplitscreen), false);
        _separateMaps = CreateSetting(nameof(_separateMaps), false);
        _textScale = CreateSetting(nameof(_textScale), 100, IntRange(50, 150));

        _allHUDComponentTypes = new Type[DATA_BY_HUD_GROUP.Count];
        var hudData = DATA_BY_HUD_GROUP.Values.ToArray();
        for (int i = 0; i < hudData.Length; i++)
            _allHUDComponentTypes[i] = hudData[i].HUDComponentType;

        _perPlayerSettings = new PerPlayerSettings[2];
        for (int i = 0; i < 2; i++)
        {
            PerPlayerSettings tmp = new();
            _perPlayerSettings[i] = tmp;
            string playerPrefix = $"player{i + 1}";

            tmp._toggle = CreateSetting(playerPrefix + nameof(tmp._toggle), false);
            tmp._copySettings = CreateSetting(playerPrefix + nameof(tmp._copySettings), false);
            tmp._rearrangeHUD = CreateSetting(playerPrefix + nameof(tmp._rearrangeHUD), false);
            tmp._startHUDEditor = CreateSetting(playerPrefix + nameof(tmp._startHUDEditor), false);
            tmp._hudTransparency = CreateSetting(playerPrefix + nameof(tmp._hudTransparency), 0, IntRange(0, 100));
            tmp._fadingStatusEffectIcons = CreateSetting(playerPrefix + nameof(tmp._fadingStatusEffectIcons), false);
            tmp._statusIconMaxSize = CreateSetting(playerPrefix + nameof(tmp._statusIconMaxSize), 120, IntRange(100, 125));
            tmp._statusIconMinSize = CreateSetting(playerPrefix + nameof(tmp._statusIconMinSize), 60, IntRange(0, 100));
            tmp._statusIconMinAlpha = CreateSetting(playerPrefix + nameof(tmp._statusIconMinAlpha), 50, IntRange(0, 100));
            tmp._hideQuickslotHints = CreateSetting(playerPrefix + nameof(tmp._hideQuickslotHints), false);
            tmp._alternativeManaBarPlacement = CreateSetting(playerPrefix + nameof(tmp._alternativeManaBarPlacement), false);
            foreach (var hudGroup in DATA_BY_HUD_GROUP.Keys.ToArray())
                tmp._hudOverridesByHUDGroup.Add(hudGroup, CreateSetting($"{playerPrefix}_hudOverride{hudGroup}", Vector3.zero));
            tmp._shopAndStashWidth = CreateSetting(playerPrefix + nameof(tmp._shopAndStashWidth), 0, IntRange(0, 100));
            tmp._swapPendingBuySellPanels = CreateSetting(playerPrefix + nameof(tmp._swapPendingBuySellPanels), false);
            tmp._separateBuySellPanels = CreateSetting(playerPrefix + nameof(tmp._separateBuySellPanels), SeperatePanelsMode.Disabled);
            tmp._buySellToggle = CreateSetting(playerPrefix + nameof(tmp._buySellToggle), "");
            tmp._switchToBuy = CreateSetting(playerPrefix + nameof(tmp._switchToBuy), "");
            tmp._switchToSell = CreateSetting(playerPrefix + nameof(tmp._switchToSell), "");

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

        AddEventOnConfigClosed(() => UpdateSplitscreenMode());
        AddEventOnConfigClosed(() => UpdateShopAndStashPanelsWidths());
        TryScaleText();
    }
    protected override void SetFormatting()
    {
        _verticalSplitscreen.Format("Vertical splitscreen");
        _verticalSplitscreen.Description = "For monitors that are more wide than tall";
        _separateMaps.Format("Co-op map");
        _textScale.Format("Text scale");
        _textScale.Description = "Scale all game text by this value\n" +
                                 "(in %, requires game restart)";
        for (int i = 0; i < 2; i++)
        {
            PerPlayerSettings tmp = _perPlayerSettings[i];

            tmp._toggle.Format($"Player {i + 1}");
            tmp._toggle.Description = $"Change settings for local player {i + 1}";
            using (Indent)
            {
                tmp._copySettings.Format($"Copy settings from player {1 - i + 1}", tmp._toggle);
                tmp._copySettings.IsAdvanced = true;
                tmp._rearrangeHUD.Format("Rearrange HUD", tmp._toggle);
                tmp._rearrangeHUD.Description = "Change HUD elements position and scale";
                using (Indent)
                {
                    tmp._startHUDEditor.Format("Edit mode", tmp._rearrangeHUD);
                    tmp._startHUDEditor.Description = "Pause the game and start rearranging HUD elements:\n" +
                                                      "Left mouse button - move\n" +
                                                      "Right muse button - scale\n" +
                                                      "Open ConfigManager - save settings";
                }
                tmp._hudTransparency.Format("HUD transparency", tmp._toggle);
                tmp._fadingStatusEffectIcons.Format("Fading status effect icons", tmp._toggle);
                using (Indent)
                {
                    tmp._statusIconMaxSize.Format("Max size", tmp._fadingStatusEffectIcons);
                    tmp._statusIconMaxSize.Description = "Icon size at maximum status effect duration";
                    tmp._statusIconMinSize.Format("Min size", tmp._fadingStatusEffectIcons);
                    tmp._statusIconMinSize.Description = "Icon size right before the status effect expires";
                    tmp._statusIconMinAlpha.Format("Min opacity", tmp._fadingStatusEffectIcons);
                    tmp._statusIconMinAlpha.Description = "Icon opacity right before the status effect expires";
                }
                tmp._hideQuickslotHints.Format("Hide quickslot hints", tmp._toggle);
                tmp._hideQuickslotHints.Description = "Keyboard - hides the key names above quickslots\n" +
                                                      "Gamepad - hides the button icons below quickslots";
                tmp._alternativeManaBarPlacement.Format("Alternative mana bar placement", tmp._toggle);
                tmp._alternativeManaBarPlacement.Description = "Move mana bar right below health bar to form a triangle out of the vitals";
                tmp._shopAndStashWidth.Format("Shop/stash panel width", tmp._toggle);
                tmp._shopAndStashWidth.Description = "% of screen size, 0% = default\n" +
                                                 "(recommended when using vertical splitscreen)";
                tmp._separateBuySellPanels.Format("Separate buy/sell panels", tmp._toggle);
                tmp._separateBuySellPanels.Description = "Disabled - shops display player's and merchant's inventory in one panel\n" +
                                                         "Toggle - toggle between player's / merchant's inventories with one button\n" +
                                                         "TwoButtons - press one button for player's inventory and another for merchant's\n" +
                                                         "(recommended when using vertical splitscreen)";
                tmp._swapPendingBuySellPanels.Format("Swap pending buy/sell panels", tmp._separateBuySellPanels, SeperatePanelsMode.Disabled);
                tmp._swapPendingBuySellPanels.Description = "Items you're buying will be shown above the merchant's stock\n" +
                                                            "Items you're selling will be shown above your pouch";
                using (Indent)
                {
                    tmp._buySellToggle.Format("Toggle buy/sell panels", tmp._separateBuySellPanels, SeperatePanelsMode.Toggle);
                    tmp._switchToBuy.Format("Switch to buy panel", tmp._separateBuySellPanels, SeperatePanelsMode.TwoButtons);
                    tmp._switchToSell.Format("Switch to sell panel", tmp._separateBuySellPanels, SeperatePanelsMode.TwoButtons);
                }
            }
        }
    }
    protected override string Description
    => "• Rearrange HUD elements\n" +
       "• Vertical splitscreen (with shop tweaks)";
    protected override string SectionOverride
    => ModSections.UI;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_PreferredUI):
                ForceApply();
                _verticalSplitscreen.Value = true;
                _textScale.Value = 110;
                _separateMaps.Value = true;
                foreach (var settings in _perPlayerSettings)
                {
                    settings._toggle.Value = true;
                    {
                        settings._rearrangeHUD.Value = true;
                        settings._hudTransparency.Value = 10;
                        settings._fadingStatusEffectIcons.Value = true;
                        {
                            settings._statusIconMaxSize.Value = 120;
                            settings._statusIconMinSize.Value = 60;
                            settings._statusIconMinAlpha.Value = 50;
                        }
                        settings._hideQuickslotHints.Value = true;
                        settings._alternativeManaBarPlacement.Value = true;
                        settings._shopAndStashWidth.Value = 80;
                        settings._separateBuySellPanels.Value = SeperatePanelsMode.Disabled;
                        settings._swapPendingBuySellPanels.Value = true;
                    }
                }
                break;
        }
    }
    public void OnUpdate()
    {
        foreach (var player in Players.Local)
        {
            // Shop/Stash panels
            PerPlayerSettings settings = _perPlayerSettings[player.ID];
            if (player.UI.m_displayedMenuIndex == CharacterUI.MenuScreens.Shop)
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


            // HUD Editor
            if (settings._startHUDEditor)
                if (_hudEditFocus.Transform == null)
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
    private static void UpdateSplitscreenMode()
    {
        if (SplitScreenManager.Instance != null)
            SplitScreenManager.Instance.CurrentSplitType = _verticalSplitscreen && Players.Local.Count >= 2
                                                         ? SplitScreenManager.SplitType.Vertical
                                                         : SplitScreenManager.SplitType.Horizontal;
    }
    private static void UpdateShopAndStashPanelsWidths()
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
                offsetMin.x = maxWidth * multiplier;
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
    private static void UpdateQuickslotButtonIcons(Players.Data player)
    {
        foreach (var quickslotDisplay in GetKeyboardQuickslotsGamePanel(player.UI).GetAllComponentsInHierarchy<QuickSlotDisplay>())
            quickslotDisplay.m_lblKeyboardInput.enabled = !_perPlayerSettings[player.ID]._hideQuickslotHints;
    }
    private static void UpdateSeparateBuySellPanels(Players.Data player)
    {
        if (_perPlayerSettings[player.ID]._separateBuySellPanels == SeperatePanelsMode.Disabled)
            DisableSeparateMode(player);
        else
            SwitchToBuySellPanel(player, true);
    }
    private static void SwitchToBuySellPanel(Players.Data player, bool buyPanel)
    {
        Transform shopPanelHolder = GetShopPanel(player.UI);
        GetPlayerShopInventoryPanel(shopPanelHolder).SetActive(!buyPanel);
        GetMerchantShopInventoryPanel(shopPanelHolder).SetActive(buyPanel);
        shopPanelHolder.GetComponent<ShopMenu>().GetFirstSelectable().Select();

    }
    private static void ToggleBuySellPanel(Players.Data player)
    {
        bool isBuyPanel = GetMerchantShopInventoryPanel(GetShopPanel(player.UI)).IsActive();
        SwitchToBuySellPanel(player, !isBuyPanel);
    }
    private static void DisableSeparateMode(Players.Data player)
    {
        GetPlayerShopInventoryPanel(GetShopPanel(player.UI)).SetActive(true);
        GetMerchantShopInventoryPanel(GetShopPanel(player.UI)).SetActive(true);
    }
    private static void UpdatePendingBuySellPanels(Players.Data player)
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
        Utils.SwapHierarchyPositions(pendingBuy, pendingSell);
        pendingBuy.SetAsFirstSibling();
        pendingSell.SetAsFirstSibling();
    }
    private static void UpdateManaBarPlacement(Players.Data player)
    {
        Transform manaBarHolder = GetManaBarHolder(GetHUDHolder(player.UI));
        bool altPlacement = _perPlayerSettings[player.ID]._alternativeManaBarPlacement;
        manaBarHolder.localPosition = altPlacement ? MANA_BAR_POSITIONS.Alternative : MANA_BAR_POSITIONS.Default;
    }
    private static void UpdateHUDTransparency(Players.Data player)
    => GetHUDHolder(player.UI).GetComponent<CanvasGroup>().alpha = 1f - _perPlayerSettings[player.ID]._hudTransparency / 100f;
    private static void ResetStatusEffectIcons(Players.Data player)
    {
        Transform statusEffectsPanel = GetHUDHolder(player.UI).Find("StatusEffect - Panel");
        foreach (var image in statusEffectsPanel.GetAllComponentsInHierarchy<Image>())
        {
            image.SetAlpha(1f);
            image.rectTransform.localScale = Vector2.one;
        }
    }
    private static void TryScaleText()
    {
        #region quit
        if (_textScale == 100)
            return;
        #endregion

        foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
            if (text.transform.root.name != "ExplorerCanvas")
            {
                text.fontSize = (text.fontSize * _textScale / 100f).Round();
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }
    }
    // HUD editor      
    private static void SetHUDEditor(Players.Data player, bool state)
    {
        PauseMenu.Pause(state);
        GameInput.ForceCursorNavigation = state;
        SetHUDTemplates(player, state);

        if (state)
        {
            ConfigHelper.IsConfigOpen = false;
            SetupHUDElements(player);
            _hudEditFocus.Transform = null;
        }
        else
        {
            SaveLoadHUDOverrides(player, SettingsOperation.Save);
            _perPlayerSettings[player.ID]._startHUDEditor.SetSilently(false);
        }
    }
    private static void SetupHUDElements(Players.Data player)
    {
        foreach (var dataByHUDGroup in DATA_BY_HUD_GROUP)
            foreach (var uiElement in GetHUDHolder(player.UI).Find(dataByHUDGroup.Value.PanelPath).GetAllComponentsInHierarchy<CanvasGroup, Image>())
                switch (uiElement)
                {
                    case CanvasGroup t: t.blocksRaycasts = true; break;
                    case Image t: t.raycastTarget = true; break;
                }
    }
    private static void SetHUDTemplates(Players.Data player, bool state)
    {
        Transform hudHolder = GetHUDHolder(player.UI);
        Transform temperature = hudHolder.Find("TemperatureSensor/Display");
        Transform statusEffect = hudHolder.Find("StatusEffect - Panel/Icon");
        Transform quickslotsHolder = hudHolder.Find("QuickSlot");
        Transform arrowsHolder = hudHolder.Find("QuiverDisplay");
        Transform pauseHolder = player.UI.transform.Find("Canvas/Paused");

        temperature.SetActive(state);
        statusEffect.GetComponent<StatusEffectIcon>().enabled = !state;
        statusEffect.SetActive(state);
        quickslotsHolder.GetComponent<QuickSlotControllerSwitcher>().enabled = !state;
        if (state)
            foreach (Transform child in quickslotsHolder.transform)
                child.Activate();
        arrowsHolder.GetComponent<QuiverDisplay>().enabled = !state;
        arrowsHolder.GetComponent<QuiverDisplay>().m_canvasGroup.alpha = 1f;
        pauseHolder.GetComponent<Image>().enabled = !state;

    }
    private static void SaveLoadHUDOverrides(Players.Data player, SettingsOperation operation)
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
    private static void HandleHUDHits(Players.Data player, Tool tool)
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
    private static Tool _hudEditTool;
    private static (Transform Transform, Vector2 EditData) _hudEditFocus;
    private static Type[] _allHUDComponentTypes;
    // Find
    private static Transform GetKeyboardQuickslotsGamePanel(CharacterUI ui)
    => ui.transform.Find("Canvas/GameplayPanels/HUD/QuickSlot/Keyboard");
    private static RectTransform GetStashPanel(CharacterUI ui)
    => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/Stash - Panel") as RectTransform;
    private static RectTransform GetShopPanel(CharacterUI ui)
    => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/ShopMenu") as RectTransform;
    private static Transform GetPlayerShopInventoryPanel(Transform shopPanel)
    => shopPanel.Find("MiddlePanel/PlayerInventory");
    private static Transform GetMerchantShopInventoryPanel(Transform shopPanel)
    => shopPanel.Find("MiddlePanel/ShopInventory");
    private static Transform GetPendingBuyPanel(Transform inventory)
    => inventory.Find("SectionContent/Scroll View/Viewport/Content/PlayerPendingBuyItems");
    private static Transform GetPendingSellPanel(Transform inventory)
    => inventory.Find("SectionContent/Scroll View/Viewport/Content/PlayerPendingSellItems");
    private static Transform GetHUDHolder(CharacterUI ui)
    => ui.transform.Find("Canvas/GameplayPanels/HUD");
    private static Transform GetManaBarHolder(Transform hudHolder)
    => hudHolder.Find("MainCharacterBars/Mana");

    // Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(MapDisplay), nameof(MapDisplay.Show), new[] { typeof(CharacterUI) })]
    static private void MapDisplay_Show_Post(MapDisplay __instance, CharacterUI _owner)
    {
        __instance.RectTransform.anchoredPosition = _separateMaps
            ? _owner.m_rectTransform.anchoredPosition
            : Vector2.zero;

        __instance.RectTransform.localScale = _separateMaps
            ? (_owner.m_rectTransform.rect.size.CompMin() / MenuManager.Instance.m_characterUIHolder.rect.size.CompMin()).ToVector2()
            : Vector2.one;

        __instance.GetComponent<Image>().enabled = !_separateMaps;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CharacterManager), nameof(CharacterManager.UpdateActiveMapCategories))]
    static private bool CharacterManager_UpdateActiveMapCategories_Pre(CharacterManager __instance)
    {
        if (!_separateMaps)
            return true;

        foreach (var player in Players.Local)
        {
            bool common = player.Character
                && MenuManager.Instance.IsApplicationFocused
                && !NetworkLevelLoader.Instance.IsGameplayPaused
                && !player.UI.GetIsMenuDisplayed(CharacterUI.MenuScreens.PauseMenu)
                && !MenuManager.Instance.IsConnectionScreenDisplayed
                && !MenuManager.Instance.InFade
                && !player.UI.IsMapDisplayed;
            bool canMoveCamera = common && !player.UI.IsMenuFocused;
            bool canMove = canMoveCamera && !player.UI.IsDialogueInProgress;
            bool canPerformAction = canMove && !player.Character.Deploying;
            bool canDeploy = canMove && player.Character.Deploying;
            bool canUseQuickSlots = common
                && (!player.UI.IsMenuFocused || player.UI.GetIsMenuDisplayed(CharacterUI.MenuScreens.QuickSlotAssignation))
                && !player.UI.IsDialogueInProgress;

            ControlsInput.SetMovementActive(player.UI.RewiredID, canMove);
            ControlsInput.SetCameraActive(player.UI.RewiredID, canMoveCamera);
            ControlsInput.SetActionActive(player.UI.RewiredID, canPerformAction);
            ControlsInput.SetDeployActive(player.UI.RewiredID, canDeploy);
            ControlsInput.SetQuickSlotActive(player.UI.RewiredID, canUseQuickSlots);
        }

        return false;
    }


    [HarmonyPostfix, HarmonyPatch(typeof(LocalCharacterControl), nameof(LocalCharacterControl.RetrieveComponents))]
    private static void LocalCharacterControl_RetrieveComponents_Post(LocalCharacterControl __instance)
    {
        UpdateSplitscreenMode();
        __instance.ExecuteAfterSeconds(UI_RESIZE_DELAY, UpdateShopAndStashPanelsWidths);

        Players.Data player = Players.GetLocal(__instance);
        UpdateQuickslotButtonIcons(player);
        UpdatePendingBuySellPanels(player);
        UpdateManaBarPlacement(player);
        UpdateHUDTransparency(player);
        if (_perPlayerSettings[player.ID]._rearrangeHUD)
            __instance.ExecuteAfterSeconds(UI_RESIZE_DELAY, () => SaveLoadHUDOverrides(player, SettingsOperation.Load));
    }

    [HarmonyPostfix, HarmonyPatch(typeof(RPCManager), nameof(RPCManager.SendPlayerHasLeft))]
    private static void RPCManager_SendPlayerHasLeft_Post(RPCManager __instance)
    {
        UpdateSplitscreenMode();
        __instance.ExecuteAfterSeconds(UI_RESIZE_DELAY, UpdateShopAndStashPanelsWidths);
    }

    // Sort by weight    
    [HarmonyPrefix, HarmonyPatch(typeof(ItemListDisplay), nameof(ItemListDisplay.ByWeight))]
    private static bool ItemListDisplay_ByWeight_Pre(ItemListDisplay __instance, ref int __result, ItemDisplay _item1, ItemDisplay _item2)
    {
        float weight1 = _item1.TryAs(out ItemGroupDisplay group1) ? group1.TotalWeight : _item1.m_refItem.Weight;
        float weight2 = _item2.TryAs(out ItemGroupDisplay group2) ? group2.TotalWeight : _item2.m_refItem.Weight;
        __result = -weight1.CompareTo(weight2);
        return false;
    }

    // Sort by durability    
    [HarmonyPrefix, HarmonyPatch(typeof(ItemListDisplay), nameof(ItemListDisplay.ByDurability))]
    private static bool ItemListDisplay_ByDurability_Pre(ref int __result, ItemDisplay _display1, ItemDisplay _display2)
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
    [HarmonyPostfix, HarmonyPatch(typeof(ShopMenu), nameof(ShopMenu.Show))]
    private static void ShopMenu_Show_Post(ShopMenu __instance)
    => UpdateSeparateBuySellPanels(Players.GetLocal(__instance));

    // Disable quickslot button icons
    [HarmonyPostfix, HarmonyPatch(typeof(QuickSlotDisplay), nameof(QuickSlotDisplay.Update))]
    private static void QuickSlotDisplay_Update_Post(QuickSlotDisplay __instance)
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
    [HarmonyPostfix, HarmonyPatch(typeof(CharacterUI), nameof(CharacterUI.DelayedRefreshSize))]
    private static void CharacterUI_DelayedRefreshSize_Post(CharacterUI __instance)
    {
        #region quit
        if (SplitScreenManager.Instance.CurrentSplitType != SplitScreenManager.SplitType.Vertical)
            return;
        #endregion

        __instance.m_rectTransform.localPosition *= -1;
    }

    // Status effect duration
    [HarmonyPostfix, HarmonyPatch(typeof(StatusEffectPanel), nameof(StatusEffectPanel.GetStatusIcon))]
    private static void StatusEffectPanel_GetStatusIcon_Post(StatusEffectPanel __instance, ref StatusEffectIcon __result)
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
            float elapsed = Utils.GameTime - disease.m_healedGameTime;
            float duration = DiseaseLibrary.Instance.GetRecedingTime(disease.m_diseasesType);
            progress = 1f - elapsed / duration;
        }
        else if (statusEffect.Permanent)
            progress = 1f;
        else
        {
            float maxDuration = Prefabs.StatusEffectsByNameID[statusEffect.IdentifierName].StartLifespan;
            float remainingDuration = statusEffect.RemainingLifespan;
            progress = remainingDuration / maxDuration;
        }

        __result.m_icon.SetAlpha(settings.StatusIconAlpha(progress));
        __result.m_icon.rectTransform.localScale = settings.StatusIconScale(progress);
    }
}
