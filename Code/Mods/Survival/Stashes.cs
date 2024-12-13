namespace Vheos.Mods.Outward;
public class Stashes : AMod, IUpdatable
{
	#region Constants
	private static readonly Dictionary<AreaManager.AreaEnum, (string UID, Vector3[] Positions)> STASH_DATA_BY_CITY = new()
	{
		[AreaManager.AreaEnum.CierzoVillage] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-367.850f, -1488.250f, 596.277f),
																				  new Vector3(-373.539f, -1488.250f, 583.187f) }),
		[AreaManager.AreaEnum.Berg] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-386.620f, -1493.132f, 773.86f),
																		 new Vector3(-372.410f, -1493.132f, 773.86f) }),
		[AreaManager.AreaEnum.Monsoon] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-371.628f, -1493.410f, 569.910f) }),
		[AreaManager.AreaEnum.Levant] = ("ZbPXNsPvlUeQVJRks3zBzg", new[] { new Vector3(-369.280f, -1502.535f, 592.850f),
																		   new Vector3(-380.530f, -1502.535f, 593.080f) }),
		[AreaManager.AreaEnum.Harmattan] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-178.672f, -1515.915f, 597.934f),
																			  new Vector3(-182.373f, -1515.915f, 606.291f),
																			  new Vector3(-383.484f, -1504.820f, 583.343f),
																			  new Vector3(-392.681f, -1504.820f, 586.551f)}),
		[AreaManager.AreaEnum.NewSirocco] = ("IqUugGqBBkaOcQdRmhnMng", new Vector3[0]),
	};

	private static readonly Dictionary<AreaManager.AreaEnum, HashSet<string>> EVENT_UIDS_BY_CITY = new()
	{
		[AreaManager.AreaEnum.CierzoVillage] = ["yIodGB0Erkyny0mtzhVrfw"],
		[AreaManager.AreaEnum.Berg] = ["klxo5f5B1kSLie07umtopw"],
		[AreaManager.AreaEnum.Monsoon] = ["EiTTfHmko0-hOi4CnzGMgQ"],
		[AreaManager.AreaEnum.Levant] = ["HH4bWup4TkORnQGwGKFwDA"],
		[AreaManager.AreaEnum.Harmattan] = ["vFyEJACDuUinN-Go6f2lnQ", "MPDpYTTdKkKtHkRzQE12KQ"],
		[AreaManager.AreaEnum.NewSirocco] = ["TD3INfzB7UaUtrJRou00ig"],
	};
	#endregion

	#region Enums
	private enum StashType
	{
		PlayerBound,
		CityBound,
	}
	#endregion

	// Settings
	private static ModSetting<bool> _innStashes;
	private static ModSetting<StashType> _stashType;
	private static ModSetting<string> _openStashKey;
	private static ModSetting<bool> _playerSharedStash;
	private static ModSetting<bool> _craftFromStash;
	private static ModSetting<bool> _craftFromStashOutside;
	private static ModSetting<bool> _stashesStartEmpty;
	protected override void Initialize()
	{
		_innStashes = CreateSetting(nameof(_innStashes), false);
		_stashType = CreateSetting(nameof(_stashType), StashType.PlayerBound);
		_openStashKey = CreateSetting(nameof(_openStashKey), "");
		_playerSharedStash = CreateSetting(nameof(_playerSharedStash), false);
		_craftFromStash = CreateSetting(nameof(_craftFromStash), false);
		_craftFromStashOutside = CreateSetting(nameof(_craftFromStashOutside), false);
		_stashesStartEmpty = CreateSetting(nameof(_stashesStartEmpty), false);
	}
	protected override void SetFormatting()
	{
		_innStashes.Format("Inn stashes");
		_innStashes.Description = "Each inn room will have a player stash, linked with the one in player's house\n" +
							  "(exceptions: the first rooms in Monsoon's inn and Harmattan's Victorious Light inn)";
		_stashType.Format("Stash type");
		_stashType.Description =
			"What is actually displayed in the text pop-up\n" +
			$"\n• {StashType.PlayerBound} - one global stash, no matter which city you're in" +
			$"\n• {StashType.CityBound} - pre-DE behaviour: each city has its own stash";

		using (Indent)
		{
			_openStashKey.Format("open hotkey", _stashType, StashType.PlayerBound);
			_openStashKey.Description =
				"Allows you to open your stash anywhere - even outside cities\n" +
				"Use UnityEngine.KeyCode enum values\n" +
				"(https://docs.unity3d.com/ScriptReference/KeyCode.html)";
			_playerSharedStash.Format("player-shared", _stashType, StashType.PlayerBound);
			_playerSharedStash.Description = "pre-DE behaviour: all players will access the first player's stash";
		}
		_craftFromStash.Format("Craft with stashed items");
		_craftFromStash.Description = "When you're crafting in a city, you can use items from you stash";
		using (Indent)
		{
			_craftFromStashOutside.Format("outside of cities", _stashType, StashType.PlayerBound);
			_craftFromStashOutside.Description =
				"Allows you to craft from stash anywhere - even outside cities";
		}
		_stashesStartEmpty.Format("Stashes start empty");
		_stashesStartEmpty.Description = "Stashes won't have any items inside the first time you open them";

	}
	protected override string SectionOverride
	=> ModSections.SurvivalAndImmersion;
	protected override string Description
	=> "";
	protected override void LoadPreset(string presetName)
	{
		switch (presetName)
		{
			case nameof(Preset.Vheos_CoopSurvival):
				ForceApply();
				_innStashes.Value = true;
				_stashType.Value = StashType.CityBound;
				_craftFromStash.Value = true;
				_stashesStartEmpty.Value = true;
				break;
		}
	}
	public void OnUpdate()
	{
		if (_openStashKey.Value.ToKeyCode().Pressed()
		&& Players.TryGetFirst(out var firstPlayer)
		&& TryGetStash(firstPlayer.Character, out var stash))
		{
			firstPlayer.Character.CharacterUI.StashPanel.SetStash(stash);
			stash.ShowContent(firstPlayer.Character);
		}
	}

	// Utility
	private static ItemContainer _cachedStash;
	private static bool IsInCity()
	=> AreaManager.Instance.CurrentArea.TryNonNull(out var currentArea)
	&& STASH_DATA_BY_CITY.ContainsKey((AreaManager.AreaEnum)currentArea.ID);
	public static bool TryGetStash(Character character, out ItemContainer stash)
	{
		if (_stashType == StashType.CityBound)
		{
			if (_cachedStash == null
			&& AreaManager.Instance.CurrentArea.TryNonNull(out var currentArea)
			&& STASH_DATA_BY_CITY.TryGetValue((AreaManager.AreaEnum)currentArea.ID, out var data))
				_cachedStash = (TreasureChest)ItemManager.Instance.GetItem(data.UID);

			stash = _cachedStash;
		}
		else
		{
			if (_playerSharedStash)
				character = Players.GetFirst().Character;

			stash = character != null
				? character.Inventory.Stash
				: null;
		}

		return stash != null;
	}


	// Hooks
	// Reset static scene data
	[HarmonyPostfix, HarmonyPatch(typeof(NetworkLevelLoader), nameof(NetworkLevelLoader.UnPauseGameplay))]
	private static void NetworkLevelLoader_UnPauseGameplay_Post(NetworkLevelLoader __instance)
	=> _cachedStash = null;

	// City-bound stashes
	[HarmonyReversePatch, HarmonyPatch(typeof(ItemContainer), nameof(ItemContainer.ShowContent))]
	public static void ItemContainer_ShowContent(ItemContainer instance, Character _character)
	{ }

	[HarmonyPrefix, HarmonyPatch(typeof(TreasureChest), nameof(TreasureChest.ShowContent))]
	private static bool TreasureChest_ShowContent_Pre(TreasureChest __instance, Character _character)
	{
		if (__instance.SpecialType == ItemContainer.SpecialContainerTypes.Stash
		&& TryGetStash(_character, out var stash))
		{
			if (!_stashesStartEmpty)
				if (__instance.m_playerReceivedUIDs.TryAddUnique(_character.UID))
					for (int i = 0; i < __instance.m_drops.Count; i++)
						if (__instance.m_drops[i])
							__instance.m_drops[i].GenerateContents(stash);

			_character.CharacterUI.StashPanel.SetStash(stash);
		}

		ItemContainer_ShowContent(__instance, _character);
		return false;
	}

	[HarmonyPostfix, HarmonyPatch(typeof(TreasureChest), nameof(TreasureChest.InitDrops))]
	private static void TreasureChest_InitDrops_Post(TreasureChest __instance)
	{
		if (!_stashesStartEmpty
		|| __instance.SpecialType != ItemContainer.SpecialContainerTypes.Stash)
			return;

		__instance.m_hasGeneratedContent = true;
		__instance.m_ignoreHasGeneratedContent = false;
	}

	// Inn Stash
	[HarmonyPostfix, HarmonyPatch(typeof(NetworkLevelLoader), nameof(NetworkLevelLoader.UnPauseGameplay))]
	private static void NetworkLevelLoader_UnPauseGameplay_Post(NetworkLevelLoader __instance, string _identifier)
	{
		#region quit
		if (!_innStashes
		|| _identifier != "Loading"
		|| !AreaManager.Instance.CurrentArea.TryNonNull(out var currentArea)
		|| !STASH_DATA_BY_CITY.TryGetValue((AreaManager.AreaEnum)currentArea.ID, out var stashData)
		|| stashData.Positions.Length == 0)
			return;
		#endregion

		// Cache
		TreasureChest stash = (TreasureChest)ItemManager.Instance.GetItem(stashData.UID);
		stash.Activate();

		int counter = 0;
		foreach (var position in stashData.Positions)
		{
			// Interactions
			Transform newInteractionHolder = GameObject.Instantiate(stash.InteractionHolder.transform);
			newInteractionHolder.name = $"InnStash{counter} - Interaction";
			newInteractionHolder.ResetLocalTransform();
			newInteractionHolder.position = position;
			InteractionActivator activator = newInteractionHolder.GetFirstComponentsInHierarchy<InteractionActivator>();
			activator.UID += $"_InnStash{counter}";
			InteractionOpenChest openChest = newInteractionHolder.GetFirstComponentsInHierarchy<InteractionOpenChest>();
			openChest.m_container = stash;
			openChest.m_item = stash;
			openChest.StartInit();

			// Highlight
			Transform newHighlightHolder = GameObject.Instantiate(stash.CurrentVisual.ItemHighlightTrans);
			newHighlightHolder.name = $"InnStash{counter} - Highlight";
			newHighlightHolder.ResetLocalTransform();
			newHighlightHolder.BecomeChildOf(newInteractionHolder);
			newHighlightHolder.GetFirstComponentsInHierarchy<InteractionHighlight>().enabled = true;
			counter++;
		}
	}

	[HarmonyPrefix, HarmonyPatch(typeof(InteractionOpenChest), nameof(InteractionOpenChest.OnActivate))]
	private static void InteractionOpenChest_OnActivate_Pre(InteractionOpenChest __instance)
	{
		#region quit
		if (!_innStashes || !__instance.m_chest.TryNonNull(out var chest))
			return;
		#endregion

		chest.Activate();
	}

	// Craft from stash
	[HarmonyPostfix, HarmonyPatch(typeof(CharacterInventory), nameof(CharacterInventory.InventoryIngredients),
		new[] { typeof(Tag), typeof(DictionaryExt<int, CompatibleIngredient>) },
		new[] { ArgumentType.Normal, ArgumentType.Ref })]
	private static void CharacterInventory_InventoryIngredients_Post(CharacterInventory __instance, Tag _craftingStationTag, ref DictionaryExt<int, CompatibleIngredient> _sortedIngredient)
	{
		#region quit
		if (!_craftFromStash
		|| !TryGetStash(__instance.m_character, out var stash)
		|| !_craftFromStashOutside && !IsInCity())
			return;
		#endregion

		__instance.InventoryIngredients(_craftingStationTag, ref _sortedIngredient, stash.GetContainedItems());
	}
}
