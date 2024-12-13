namespace Vheos.Mods.Outward;
using UnityEngine.UI;

public class GUI : AMod, IDelayedInit, IUpdatable
{
	#region Constants
	public static readonly Vector2 DEFAULT_SHOP_OFFSET_MIN = new(-1344f, -540f);
	public static readonly Vector2 DEFAULT_SHOP_OFFSET_MAX = new(-20f, -20f);
	public static readonly float CHARACTERMENU_DEFAULT_HEIGHT = 500f;
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
		public ModSetting<bool> _hideFullStabilityBar;
		public ModSetting<bool> _hideEmptyShoppingCarts;
		public ModSetting<bool> _fadingStatusEffectIcons;
		public ModSetting<int> _statusIconMaxSize, _statusIconMinSize, _statusIconMinAlpha;
		public ModSetting<bool> _quickslotHints;
		public ModSetting<bool> _otherPlayersHealth;
		public ModSetting<bool> _alternativeManaBarPlacement;
		public ModSetting<bool> _playerInventoryAlwaysOnRight;
		public ModSetting<float> _characterPanelHeight;
		public ModSetting<bool> _characterPanelHints;
		public ModSetting<Vector2> _shopAndStashSize;
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
			_quickslotHints.Value = otherPlayerSettings._quickslotHints;
			_alternativeManaBarPlacement.Value = otherPlayerSettings._alternativeManaBarPlacement;
			_shopAndStashSize.Value = otherPlayerSettings._shopAndStashSize;
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
			PerPlayerSettings pps = new();
			_perPlayerSettings[i] = pps;
			string playerPrefix = $"player{i + 1}";

			pps._toggle = CreateSetting(playerPrefix + nameof(pps._toggle), false);
			pps._copySettings = CreateSetting(playerPrefix + nameof(pps._copySettings), false);
			pps._rearrangeHUD = CreateSetting(playerPrefix + nameof(pps._rearrangeHUD), false);
			pps._startHUDEditor = CreateSetting(playerPrefix + nameof(pps._startHUDEditor), false);
			pps._hudTransparency = CreateSetting(playerPrefix + nameof(pps._hudTransparency), 0, IntRange(0, 100));
			pps._hideFullStabilityBar = CreateSetting(playerPrefix + nameof(pps._hideFullStabilityBar), false);
			pps._hideEmptyShoppingCarts = CreateSetting(playerPrefix + nameof(pps._hideEmptyShoppingCarts), false);
			pps._fadingStatusEffectIcons = CreateSetting(playerPrefix + nameof(pps._fadingStatusEffectIcons), false);
			pps._statusIconMaxSize = CreateSetting(playerPrefix + nameof(pps._statusIconMaxSize), 120, IntRange(100, 125));
			pps._statusIconMinSize = CreateSetting(playerPrefix + nameof(pps._statusIconMinSize), 60, IntRange(0, 100));
			pps._statusIconMinAlpha = CreateSetting(playerPrefix + nameof(pps._statusIconMinAlpha), 50, IntRange(0, 100));
			pps._quickslotHints = CreateSetting(playerPrefix + nameof(pps._quickslotHints), true);
			pps._otherPlayersHealth = CreateSetting(playerPrefix + nameof(pps._otherPlayersHealth), true);
			pps._alternativeManaBarPlacement = CreateSetting(playerPrefix + nameof(pps._alternativeManaBarPlacement), false);
			foreach (var hudGroup in DATA_BY_HUD_GROUP.Keys.ToArray())
				pps._hudOverridesByHUDGroup.Add(hudGroup, CreateSetting($"{playerPrefix}_hudOverride{hudGroup}", Vector3.zero));
			pps._shopAndStashSize = CreateSetting(playerPrefix + nameof(pps._shopAndStashSize), Vector2.zero);
			pps._playerInventoryAlwaysOnRight = CreateSetting(playerPrefix + nameof(pps._playerInventoryAlwaysOnRight), false);
			pps._characterPanelHeight = CreateSetting(playerPrefix + nameof(pps._characterPanelHeight), 0f, FloatRange(0f, 100f));
			pps._characterPanelHints = CreateSetting(playerPrefix + nameof(pps._characterPanelHints), true);
			pps._swapPendingBuySellPanels = CreateSetting(playerPrefix + nameof(pps._swapPendingBuySellPanels), false);
			pps._separateBuySellPanels = CreateSetting(playerPrefix + nameof(pps._separateBuySellPanels), SeperatePanelsMode.Disabled);
			pps._buySellToggle = CreateSetting(playerPrefix + nameof(pps._buySellToggle), "");
			pps._switchToBuy = CreateSetting(playerPrefix + nameof(pps._switchToBuy), "");
			pps._switchToSell = CreateSetting(playerPrefix + nameof(pps._switchToSell), "");

			// Events
			int id = i;
			pps._copySettings.AddEvent(() =>
			{
				if (pps._copySettings)
					pps.CopySettings(_perPlayerSettings[1 - id]);
				pps._copySettings.SetSilently(false);
			});
			pps._rearrangeHUD.AddEvent(() =>
			{
				if (Players.TryGetLocal(id, out Players.Data player))
					SaveLoadHUDOverrides(player, pps._rearrangeHUD ? SettingsOperation.Load : SettingsOperation.Reset);
				if (!pps._rearrangeHUD)
					_perPlayerSettings[id].ResetHUDOverrides();
			});
			pps._startHUDEditor.AddEvent(() =>
			{
				if (pps._startHUDEditor && Players.TryGetLocal(id, out Players.Data player))
					SetHUDEditor(player, true);
			});
			pps._hudTransparency.AddEvent(() =>
			{
				if (Players.TryGetLocal(id, out Players.Data player))
					UpdateHUDTransparency(player);
			});
			pps._fadingStatusEffectIcons.AddEvent(() =>
			{
				if (!pps._fadingStatusEffectIcons && Players.TryGetLocal(id, out Players.Data player))
					ResetStatusEffectIcons(player);
			});
			pps._quickslotHints.AddEvent(() =>
			{
				if (Players.TryGetLocal(id, out Players.Data player))
					UpdateQuickslotButtonIcons(player);
			});
			pps._otherPlayersHealth.AddEvent(() =>
			{
				if (Players.TryGetLocal(id, out Players.Data player))
					UpdateOtherPlayersHealthBars(player);
			});
			pps._alternativeManaBarPlacement.AddEvent(() =>
			{
				if (Players.TryGetLocal(id, out Players.Data player))
					UpdateManaBarPlacement(player);
			});
			AddEventOnConfigOpened(() =>
			{
				if (pps._startHUDEditor && Players.TryGetLocal(id, out Players.Data player))
					SetHUDEditor(player, false);
			});
			pps._separateBuySellPanels.AddEvent(() =>
			{
				if (Players.TryGetLocal(id, out Players.Data player))
					UpdateSeparateBuySellPanels(player);
			});
			pps._playerInventoryAlwaysOnRight.AddEvent(() =>
			{
				if (!Players.TryGetLocal(id, out Players.Data player))
					return;

				UpdatePlayerInventoryOrderInShop(player);
				UpdatePlayerInventoryOrderInStash(player);
			});
			pps._swapPendingBuySellPanels.AddEvent(() =>
			{
				if (Players.TryGetLocal(id, out Players.Data player))
					UpdatePendingBuySellPanels(player);
			});
		}

		AddEventOnConfigClosed(UpdateSplitscreenMode);
		AddEventOnConfigClosed(UpdateShopAndStashPanelsSize);
		AddEventOnConfigClosed(UpdateCharacterPanel);
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
			PerPlayerSettings pps = _perPlayerSettings[i];

			pps._toggle.Format($"Player {i + 1}");
			pps._toggle.Description = $"Change settings for local player {i + 1}";
			using (Indent)
			{
				pps._copySettings.Format($"Copy settings from player {1 - i + 1}", pps._toggle);
				pps._copySettings.IsAdvanced = true;
				pps._rearrangeHUD.Format("Rearrange HUD", pps._toggle);
				pps._rearrangeHUD.Description = "Change HUD elements position and scale";
				using (Indent)
				{
					pps._startHUDEditor.Format("Edit mode", pps._rearrangeHUD);
					pps._startHUDEditor.Description = "Pause the game and start rearranging HUD elements:\n" +
													  "Left mouse button - move\n" +
													  "Right muse button - scale\n" +
													  "Open ConfigManager - save settings";
				}
				pps._hudTransparency.Format("HUD transparency", pps._toggle);
				pps._hideFullStabilityBar.Format("Hide full stability bar");
				pps._hideEmptyShoppingCarts.Format("Hide empty shopping carts");
				pps._fadingStatusEffectIcons.Format("Fading status effect icons", pps._toggle);
				using (Indent)
				{
					pps._statusIconMaxSize.Format("Max size", pps._fadingStatusEffectIcons);
					pps._statusIconMaxSize.Description = "Icon size at maximum status effect duration";
					pps._statusIconMinSize.Format("Min size", pps._fadingStatusEffectIcons);
					pps._statusIconMinSize.Description = "Icon size right before the status effect expires";
					pps._statusIconMinAlpha.Format("Min opacity", pps._fadingStatusEffectIcons);
					pps._statusIconMinAlpha.Description = "Icon opacity right before the status effect expires";
				}
				pps._quickslotHints.Format("Quickslot hints", pps._toggle);
				pps._quickslotHints.Description = "Keyboard - hides the key names above quickslots\n" +
													  "Gamepad - hides the button icons below quickslots";
				pps._otherPlayersHealth.Format("Other players' health bars", pps._toggle);
				pps._alternativeManaBarPlacement.Format("Alternative mana bar placement", pps._toggle);
				pps._alternativeManaBarPlacement.Description = "Move mana bar right below health bar to form a triangle out of the vitals";
				pps._shopAndStashSize.Format("Shop & stash panel size", pps._toggle);
				pps._shopAndStashSize.Description = "% of screen size, 0% = default\n" +
												 "(recommended when using vertical splitscreen)";
				pps._playerInventoryAlwaysOnRight.Format("Player inventory always on right", pps._toggle);
				pps._playerInventoryAlwaysOnRight.Description = "Player inventory will be displayed on the right in shop and stash panels";
				pps._characterPanelHeight.Format("Character panel height", pps._toggle);
				pps._characterPanelHeight.Description =
					"Controls height of the top-right character panel (with inventory, skills, crafting, etc)" +
					"\n% of screen height, 0% = default";
				pps._characterPanelHints.Format("Character panel hints", pps._toggle);
				pps._swapPendingBuySellPanels.Format("Swap pending buy/sell panels", pps._toggle);
				pps._swapPendingBuySellPanels.Description = "Items you're buying will be shown above the merchant's stock\n" +
															"Items you're selling will be shown above your pouch";
				pps._separateBuySellPanels.Format("Separate buy/sell panels", pps._toggle);
				pps._separateBuySellPanels.Description = "Disabled - shops display player's and merchant's inventory in one panel\n" +
														 "Toggle - toggle between player's / merchant's inventories with one button\n" +
														 "TwoButtons - press one button for player's inventory and another for merchant's\n" +
														 "(recommended when using vertical splitscreen)";
				using (Indent)
				{
					pps._buySellToggle.Format("Toggle buy/sell panels", pps._separateBuySellPanels, SeperatePanelsMode.Toggle);
					pps._switchToBuy.Format("Switch to buy panel", pps._separateBuySellPanels, SeperatePanelsMode.TwoButtons);
					pps._switchToSell.Format("Switch to sell panel", pps._separateBuySellPanels, SeperatePanelsMode.TwoButtons);
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
						settings._quickslotHints.Value = true;
						settings._alternativeManaBarPlacement.Value = true;
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
		if (SplitScreenManager.Instance == null)
			return;

		SplitScreenManager.Instance.CurrentSplitType =
			_verticalSplitscreen && Players.Local.Count >= 2
			? SplitScreenManager.SplitType.Vertical
			: SplitScreenManager.SplitType.Horizontal;
	}
	private static void UpdateShopAndStashPanelsSize()
	{
		foreach (var player in Players.Local)
		{
			// Choose
			PerPlayerSettings pps = _perPlayerSettings[player.ID];
			Vector2 offsetMin = DEFAULT_SHOP_OFFSET_MIN;
			Vector2 offsetMax = DEFAULT_SHOP_OFFSET_MAX;
			if (pps._shopAndStashSize.Value.x > 0f)
			{
				float maxWidth = player.UI.m_rectTransform.rect.width.Neg();
				float multiplier = pps._shopAndStashSize.Value.x / 100f;
				offsetMin.x = maxWidth * multiplier;
				offsetMax.x = 0f;
			}
			if (pps._shopAndStashSize.Value.y > 0f)
			{
				float maxHeight = player.UI.m_rectTransform.rect.height.Neg();
				float multiplier = pps._shopAndStashSize.Value.y / 100f;
				offsetMin.y = maxHeight * multiplier;
				offsetMax.y = 0f;
			}
			// Set
			foreach (var rectTransform in new[] { GetShopPanel(player.UI), GetStashPanel(player.UI) })
			{
				rectTransform.offsetMin = offsetMin;
				rectTransform.offsetMax = offsetMax;
			}
		}
	}
	private static void UpdateCharacterPanel()
	{
		foreach (var player in Players.Local)
		{
			if (GetCharacterPanel(player.UI) is not Transform characterPanel
			|| !characterPanel.TryGetComponent(out LayoutElement characterPanelLayoutElement)
			|| characterPanel.Find("Content/MiddlePanel") is not RectTransform middlePanel
			|| characterPanel.Find("Content/BottomPanel") is not RectTransform bottomPanel)
				continue;

			PerPlayerSettings pps = _perPlayerSettings[player.ID];
			float screenHeight = player.UI.m_rectTransform.rect.height;
			float bottomPanelMargin = screenHeight * 0.02f;
			float middlePanelMargin = screenHeight * 0.03f;
			if (pps._characterPanelHints)
				middlePanelMargin *= 2f;

			float targetHeight = pps._characterPanelHeight > 0f
				? screenHeight * pps._characterPanelHeight / 100f
				: CHARACTERMENU_DEFAULT_HEIGHT;

			var previousOffsets = (middlePanel.offsetMin, middlePanel.offsetMax);
			middlePanel.pivot = new(middlePanel.pivot.x, 1f);
			(middlePanel.offsetMin, middlePanel.offsetMax) = previousOffsets;
			middlePanel.offsetMin = new(middlePanel.offsetMin.x, -targetHeight + middlePanelMargin);

			characterPanelLayoutElement.minHeight = targetHeight;
			bottomPanel.anchoredPosition = new(bottomPanel.anchoredPosition.x, -targetHeight + bottomPanelMargin);

			bottomPanel.SetActive(pps._characterPanelHints);
		}
	}
	private static void UpdateQuickslotButtonIcons(Players.Data player)
	{
		foreach (var quickslotDisplay in GetKeyboardQuickslotsGamePanel(player.UI).GetAllComponentsInHierarchy<QuickSlotDisplay>())
			quickslotDisplay.m_lblKeyboardInput.enabled = _perPlayerSettings[player.ID]._quickslotHints;
	}
	private static void UpdateOtherPlayersHealthBars(Players.Data player)
	{
		if (player?.UI?.m_characterBarsHolder?.m_activeListener is not List<CharacterBarListener> barListeners)
			return;

		var healthBarsVisible = _perPlayerSettings[player.ID]._otherPlayersHealth;
		foreach (var barListener in barListeners)
		{
			barListener.enabled = healthBarsVisible;
			if (barListener.m_healthBar is Bar healthBar)
				healthBar.SetActive(healthBarsVisible);
		}
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
	private static void UpdatePlayerInventoryOrderInShop(Players.Data player)
	{
		if (GetShopPanel(player.UI) is not RectTransform shopPanel
		|| shopPanel.Find("MiddlePanel") is not Transform shopMiddlePanel
		|| shopMiddlePanel.Find("ShopInventory") is not Transform shopInventory
		|| shopMiddlePanel.Find("PlayerInventory") is not Transform playerInventory
		|| shopPanel.Find("TopPanel/Shop PanelTop") is not Transform shopTopPanel
		|| shopTopPanel.Find("lblShopName") is not RectTransform shopInventoryLabel
		|| shopTopPanel.Find("lblPlayerInventory") is not RectTransform playerInventoryLabel)
			return;

		var isOnRight = playerInventory.GetSiblingIndex() != 0;
		var shouldBeOnRight = _perPlayerSettings[player.ID]._playerInventoryAlwaysOnRight;

		if (isOnRight != shouldBeOnRight)
			(playerInventoryLabel.anchoredPosition, shopInventoryLabel.anchoredPosition)
				= (shopInventoryLabel.anchoredPosition, playerInventoryLabel.anchoredPosition);

		if (shouldBeOnRight)
		{
			shopInventory.SetAsFirstSibling();
			playerInventory.SetAsLastSibling();
		}
		else
		{
			playerInventory.SetAsFirstSibling();
			shopInventory.SetAsLastSibling();
		}
	}
	private static void UpdatePlayerInventoryOrderInStash(Players.Data player)
	{
		if (GetStashPanel(player.UI) is not RectTransform stashPanel
		|| stashPanel.Find("Content/MiddlePanel") is not Transform stashMiddlePanel
		|| stashMiddlePanel.Find("StashInventory") is not Transform stashInventory
		|| stashMiddlePanel.Find("PlayerInventory") is not Transform playerInventory
		|| stashPanel.Find("Content/TopPanel/Shop PanelTop") is not Transform stashTopPanel
		|| stashTopPanel.Find("lblShopName") is not RectTransform stashInventoryLabel
		|| stashTopPanel.Find("lblPlayerInventory") is not RectTransform playerInventoryLabel)
			return;

		var isOnRight = playerInventory.GetSiblingIndex() != 0;
		var shouldBeOnRight = _perPlayerSettings[player.ID]._playerInventoryAlwaysOnRight;

		if (isOnRight != shouldBeOnRight)
			(playerInventoryLabel.anchoredPosition, stashInventoryLabel.anchoredPosition)
				= (stashInventoryLabel.anchoredPosition, playerInventoryLabel.anchoredPosition);

		if (_perPlayerSettings[player.ID]._playerInventoryAlwaysOnRight)
		{
			stashInventory.SetAsFirstSibling();
			stashInventoryLabel.SetAsFirstSibling();
			playerInventory.SetAsLastSibling();
			playerInventoryLabel.SetAsLastSibling();
		}
		else
		{
			playerInventory.SetAsFirstSibling();
			playerInventoryLabel.SetAsFirstSibling();
			stashInventory.SetAsLastSibling();
			stashInventoryLabel.SetAsLastSibling();
		}
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
	private static Transform GetCharacterPanel(CharacterUI ui)
	=> ui.transform.Find("Canvas/GameplayPanels/Menus/CharacterMenus/MainPanel");
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
		__instance.ExecuteAfterSeconds(UI_RESIZE_DELAY, UpdateShopAndStashPanelsSize);
		__instance.ExecuteAfterSeconds(UI_RESIZE_DELAY, UpdateCharacterPanel);

		Players.Data player = Players.GetLocal(__instance);
		UpdateQuickslotButtonIcons(player);
		UpdateOtherPlayersHealthBars(player);
		UpdatePendingBuySellPanels(player);
		UpdateManaBarPlacement(player);
		UpdateHUDTransparency(player);
		UpdatePlayerInventoryOrderInShop(player);
		UpdatePlayerInventoryOrderInStash(player);
		if (_perPlayerSettings[player.ID]._rearrangeHUD)
			__instance.ExecuteAfterSeconds(UI_RESIZE_DELAY, () => SaveLoadHUDOverrides(player, SettingsOperation.Load));
	}

	[HarmonyPostfix, HarmonyPatch(typeof(RPCManager), nameof(RPCManager.SendPlayerHasLeft))]
	private static void RPCManager_SendPlayerHasLeft_Post(RPCManager __instance)
	{
		UpdateSplitscreenMode();
		__instance.ExecuteAfterSeconds(UI_RESIZE_DELAY, UpdateShopAndStashPanelsSize);
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
		if (_perPlayerSettings[player.ID]._quickslotHints)
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

	// Hide full stability bar
	[HarmonyPostfix, HarmonyPatch(typeof(StabilityDisplay_Simple), nameof(StabilityDisplay_Simple.StartInit))]
	private static void StabilityDisplay_Simple_StartInit_Post(StabilityDisplay_Simple __instance)
	{
		if (__instance.m_stabilityBar is not Bar stabilityBar
		|| !Players.TryGetLocal(__instance.LocalCharacter, out var player)
		|| _perPlayerSettings[player.ID] is not PerPlayerSettings pps
		|| !pps._hideFullStabilityBar)
			return;

		stabilityBar.HideIfFull = true;
		stabilityBar.SelfUpdateVisiblity = true;
	}

	// Hide empty buy/sell panel
	[HarmonyPostfix, HarmonyPatch(typeof(ShopMenu), nameof(ShopMenu.RefreshTransactionOverview))]
	private static void ShopMenu_RefreshTransactionOverview_Post(ShopMenu __instance)
	{
		if (Players.GetLocal(__instance) is not Players.Data player
		|| !_perPlayerSettings[player.ID]._hideEmptyShoppingCarts
		|| __instance.m_buyCartDisplay is not ShoppingCartDisplay buyCart
		|| __instance.m_sellCartDisplay is not ShoppingCartDisplay sellCart)
			return;

		buyCart.SetActive(buyCart.ItemCount != 0);
		sellCart.SetActive(sellCart.ItemCount != 0);
	}

	/*

	private static ModSetting<int> _vitalBarsDisplayedMax;
	_vitalBarsDisplayedMax = CreateSetting(nameof(_vitalBarsDisplayedMax), 0, IntRange(0, 300));	
	_vitalBarsDisplayedMax.Format("Displayed max vitals");
	_vitalBarsDisplayedMax.Description = "";
	private static void TryOverrideDisplayedStatMax(ref float __result)
	{
		if (!_isInCharacterBarListenerUpdateDisplay
		|| _vitalBarsDisplayedMax == 0)
			return;

		__result = __result.ClampMin(_vitalBarsDisplayedMax);
	}

	// Displayed max vitals
	[HarmonyPrefix, HarmonyPatch(typeof(CharacterBarListener), nameof(CharacterBarListener.UpdateDisplay))]
	private static void CharacterBarListener_UpdateDisplay_Pre()
	=> _isInCharacterBarListenerUpdateDisplay = true;

	[HarmonyPostfix, HarmonyPatch(typeof(CharacterBarListener), nameof(CharacterBarListener.UpdateDisplay))]
	private static void CharacterBarListener_UpdateDisplay_Post()
	=> _isInCharacterBarListenerUpdateDisplay = false;

	[HarmonyPostfix, HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.MaxHealth), MethodType.Getter)]
	private static void CharacterStats_MaxHealth_Getter_Post(ref float __result)
	=> TryOverrideDisplayedStatMax(ref __result);

	[HarmonyPostfix, HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.MaxStamina), MethodType.Getter)]
	private static void CharacterStats_MaxStamina_Getter_Post(ref float __result)
	=> TryOverrideDisplayedStatMax(ref __result);

	[HarmonyPostfix, HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.MaxMana), MethodType.Getter)]
	private static void CharacterStats_MaxMana_Getter_Post(ref float __result)
	=> TryOverrideDisplayedStatMax(ref __result);
	*/
}
