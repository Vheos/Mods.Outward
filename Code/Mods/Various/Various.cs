﻿namespace Vheos.Mods.Outward;

using NodeCanvas.Tasks.Conditions;
using System.Collections;
using Vheos.Mods.Core;

public class Various : AMod, IUpdatable
{
	private const string TotemWorkshopsUnlockedEventUid = "uWyfl7A8NU-NOGawhdduyQ";
	#region enum
	[Flags]
	private enum ItemContextAction
	{
		AssignToQuickSlot = 1 << 0,
		Equip = 1 << 1,
		Unequip = 1 << 2,
		Use = 1 << 3,
		Read = 1 << 4,
		Fill = 1 << 5,
		Deploy = 1 << 6,
		MoveToPocket = 1 << 7,
		MoveToBag = 1 << 8,
		MoveToContainer = 1 << 9,
		Empty = 1 << 10,
		Drop = 1 << 11,
		Buy = 1 << 12,
		Sell = 1 << 13,
		Remove = 1 << 14,
		ExploreStack = 1 << 15,
		Light = 1 << 16,
		PutOut = 1 << 17,
		LanternAttach = 1 << 18,
	}
	#endregion

	#region Settings
	private static ModSetting<bool> _introLogos;
	private static ModSetting<bool> _titleScreenCharacters;
	private static ModSetting<bool> _debugMode;
	private static ModSetting<string> _debugModeToggleKey;
	private static ModSetting<ArmorSlots> _visibleArmorSlots;
	private static ModSetting<bool> _multiplayerScaling;
	private static ModSetting<bool> _enemiesHealOnLoad;
	private static ModSetting<bool> _multiplicativeStatsStacking;
	private static ModSetting<int> _armorTrainingPenaltyReduction;
	private static ModSetting<bool> _armorTrainingAffectManaCost;
	private static ModSetting<bool> _refillArrowsFromInventory;
	private static ModSetting<float> _staminaRegen;
	private static ModSetting<int> _rentDuration;
	private static ModSetting<ItemContextAction> _itemContextActions;
	private static ModSetting<bool> _itemActionDropOne;
	private static ModSetting<bool> _itemActionSalvage;
	private static ModSetting<int> _openRegionsEnemyDensity;
	private static ModSetting<bool> _temperatureToggle;
	private static Dictionary<TemperatureSteps, ModSetting<Vector2>> _temperatureDataByEnum;
	private static ModSetting<bool> _overrideMaxSlope;
	private static ModSetting<float> _maxSlope;
	private static ModSetting<bool> _earlyTotemWorkshops;
	protected override void Initialize()
	{
		_introLogos = CreateSetting(nameof(_introLogos), true);
		_titleScreenCharacters = CreateSetting(nameof(_titleScreenCharacters), true);
		_debugMode = CreateSetting(nameof(_debugMode), false);
		_debugModeToggleKey = CreateSetting(nameof(_debugModeToggleKey), "");
		_visibleArmorSlots = CreateSetting(nameof(_visibleArmorSlots), ArmorSlots.All);
		_multiplayerScaling = CreateSetting(nameof(_multiplayerScaling), false);
		_enemiesHealOnLoad = CreateSetting(nameof(_enemiesHealOnLoad), false);
		_multiplicativeStatsStacking = CreateSetting(nameof(_multiplicativeStatsStacking), false);
		_armorTrainingPenaltyReduction = CreateSetting(nameof(_armorTrainingPenaltyReduction), Defaults.ArmorTrainingPenaltyReduction, IntRange(0, 100));
		_armorTrainingAffectManaCost = CreateSetting(nameof(_armorTrainingAffectManaCost), false);
		_refillArrowsFromInventory = CreateSetting(nameof(_refillArrowsFromInventory), false);
		_staminaRegen = CreateSetting(nameof(_staminaRegen), Defaults.BaseStaminaRegen, FloatRange(0, 10));
		_rentDuration = CreateSetting(nameof(_rentDuration), Defaults.InnRentTime, IntRange(1, 240));
		_itemContextActions = CreateSetting(nameof(_itemContextActions), (ItemContextAction)~0);
		_itemActionDropOne = CreateSetting(nameof(_itemActionDropOne), false);
		_itemActionSalvage = CreateSetting(nameof(_itemActionSalvage), false);
		_openRegionsEnemyDensity = CreateSetting(nameof(_openRegionsEnemyDensity), 0, IntRange(0, 100));
		_temperatureToggle = CreateSetting(nameof(_temperatureToggle), false);
		_temperatureDataByEnum = new();
		foreach (var step in Utils.TemperatureSteps)
			if (step != TemperatureSteps.Count)
				_temperatureDataByEnum.Add(step, CreateSetting(nameof(_temperatureDataByEnum) + step, Defaults.TemperateDataByStep[step]));
		_overrideMaxSlope = CreateSetting(nameof(_overrideMaxSlope), false);
		_maxSlope = CreateSetting(nameof(_maxSlope), Defaults.PlayerMaxSlope, FloatRange(15, 75));
		_earlyTotemWorkshops = CreateSetting(nameof(_earlyTotemWorkshops), false);

		_itemContextActions.AddEvent(CacheContextActionsToRemove);
		_debugMode.AddEvent(() => Global.CheatsEnabled = _debugMode);
		AddEventOnConfigClosed(() =>
		{
			foreach (var player in Players.Local)
				UpdateBaseStaminaRegen(player.Stats);
			TryUpdateTemperatureData();
		});
	}
	protected override void LoadPreset(string presetName)
	{
		switch (presetName)
		{
			case nameof(Preset.Vheos_CoopSurvival):
				ForceApply();
				_introLogos.Value = false;
				_titleScreenCharacters.Value = true;
				_debugMode.Value = false;
				_debugModeToggleKey.Value = KeyCode.Keypad0.ToString();
				_visibleArmorSlots.Value = ArmorSlots.All;
				_multiplayerScaling.Value = false;
				_enemiesHealOnLoad.Value = true;
				_multiplicativeStatsStacking.Value = true;
				_armorTrainingPenaltyReduction.Value = 50;
				_armorTrainingAffectManaCost.Value = true;
				_refillArrowsFromInventory.Value = true;
				_rentDuration.Value = 240;
				_itemActionDropOne.Value = true;
				_openRegionsEnemyDensity.Value = 50;
				_temperatureToggle.Value = true;
				{
					_temperatureDataByEnum[TemperatureSteps.Hottest].Value = new Vector2(+50, 50 + (50 + 1));
					_temperatureDataByEnum[TemperatureSteps.VeryHot].Value = new Vector2(+40, 50 + (50 - 1));
					_temperatureDataByEnum[TemperatureSteps.Hot].Value = new Vector2(+30, 50 + (25 + 1));
					_temperatureDataByEnum[TemperatureSteps.Warm].Value = new Vector2(+20, 50 + (10 + 1));
					_temperatureDataByEnum[TemperatureSteps.Neutral].Value = new Vector2(0, 50);
					_temperatureDataByEnum[TemperatureSteps.Fresh].Value = new Vector2(-20, 50 - (10 + 1));
					_temperatureDataByEnum[TemperatureSteps.Cold].Value = new Vector2(-30, 50 - (25 + 1));
					_temperatureDataByEnum[TemperatureSteps.VeryCold].Value = new Vector2(-40, 50 - (50 - 1));
					_temperatureDataByEnum[TemperatureSteps.Coldest].Value = new Vector2(-50, 50 - (50 + 1));
				}
				_overrideMaxSlope.Value = true;
				_maxSlope.Value = 30f;
				break;
		}
	}
	public void OnUpdate()
	{
		if (_debugModeToggleKey.Value.ToKeyCode().Pressed())
			_debugMode.Value = !_debugMode;
	}
	#endregion

	#region Formatting
	protected override string SectionOverride
	=> "";
	protected override string Description
	=> "• Mods small and big that didn't find their section yet :)";
	protected override void SetFormatting()
	{
		_introLogos.Format("Intro logos");
		_introLogos.Description =
			"Allows you to skip the intro logos and save ~3 seconds of your precious life each time you launch the game" +
			"\n(requires game restart to take effect)";
		_titleScreenCharacters.Format("Title screen characters");
		_titleScreenCharacters.Description =
			"Allows you to hide characters in title screens - if you think they are ruining the view :)" +
			"\n(requires game restart to take effect)";
		_debugMode.Format("Debug mode");
		_debugMode.Description =
			"Allows you to use debug menus (aka cheats)" +
			"\nRead more at: https://outward.fandom.com/wiki/Debug_Mode";
		using (Indent)
		{
			_debugModeToggleKey.Format("toggle key");
			_debugModeToggleKey.Description =
				$"Pressing this key will enable/disable debug mode" +
				$"\n\nvalue type: case-insensitive {nameof(KeyCode)} enum" +
				$"\n(https://docs.unity3d.com/ScriptReference/KeyCode.html)";
		}

		_visibleArmorSlots.Format("Visible armor slots");
		_visibleArmorSlots.Description =
			"Allows you to hides ugly armor parts (mostly helmets)";
		_multiplayerScaling.Format("Multiplayer scaling");
		_multiplayerScaling.Description =
			"Makes enemies' stats scale up in multiplayer";
		_enemiesHealOnLoad.Format("Enemies heal on load");
		_enemiesHealOnLoad.Description =
			"Makes enemies fully heal after every loading screen";
		_multiplicativeStatsStacking.Format("Multiplicative stats stacking");
		_multiplicativeStatsStacking.Description =
			"Makes movement speed, stamina cost, mana cost modifiers stack multiplicatively instead of additvely" +
			"\nAs a result, stacking MINUS effects is less effective, while stacking PLUS effects is more effective" +
			"\n\n(enabling this will allow you to configure ArmorTraining passive skill)";
		using (Indent)
		{
			_armorTrainingPenaltyReduction.Format("\"Armor Training\" penalty reduction", _multiplicativeStatsStacking);
			_armorTrainingPenaltyReduction.Description =
				"How much of equipment's movement speed and stamina cost penalties should \"Armor Training\" ignore";
			_armorTrainingAffectManaCost.Format("\"Armor Training\" affects mana cost", _multiplicativeStatsStacking);
			_armorTrainingAffectManaCost.Description =
				"\"Armor Training\" will also lower equipment's mana cost penalties";
		}
		_refillArrowsFromInventory.Format("Refill arrows from inventory");
		_refillArrowsFromInventory.Description =
			"Automatically refills your equipped arrows with ones from your backpack or pouch (in that order)";
		_staminaRegen.Format("Stamina regen");
		_staminaRegen.Description =
			"How quickly your character regenerates stamina without any modifiers" +
			"\n\nUnit: stamina points per second";
		_rentDuration.Format("Inn rent duration");
		_rentDuration.Description =
			"How long you can stay at the inn before you have to pay again" +
			"\n\nUnit: hours";
		_itemContextActions.Format("Item context actions");
		_itemContextActions.Description =
			"Allows you to remove specific actions from item context menu (like the default \"Use\", \"Deploy\" or \"Equip\")";
		_itemActionDropOne.Format("\"Drop one\" item action");
		_itemActionDropOne.Description =
			"Adds a \"Drop one\" button to stacked items' context menu which skips the \"choose amount\" panel and drops exactly 1 of the item" +
			"\n(recommended during co-op for quick sharing)";
		_itemActionSalvage.Format("\"Salvage\" item action");
		_itemActionSalvage.Description =
			"Adds a \"Salvage\" button to items that can be salvaged";
		_openRegionsEnemyDensity.Format("Open regions enemy density");
		_openRegionsEnemyDensity.Description =
			"How densely random squads can spawn in open regions" +
			"\nIncreasing this value lowers the following spawn restrictions:" +
			"\n• spawn check interval" +
			"\n• maximum active squads" +
			"\n• minimum distance from other squads" +
			"\n• minimum distance from the player" +
			"\n\nDisclaimer: this allows you to wipe out all available squads in a region quicker than with restricted spawns, and the only way to respawn them is triggering an area reset" +
			"\n\nUnit: subjective linear scale, where 0% represents the default game settings";
		_temperatureToggle.Format("Temperature");
		_temperatureToggle.Description =
			"Overrides environmental temperature settings:" +
			"\nX   -   value; how much weather defense you need to completely ignore the effects of this temperature level" +
			"\nY   -   cap; your temperature won't go above/below this value even if you don't have any weather defense" +
			"\n\nCharacter temperatures cheatsheet:" +
			"\nVery Hot   -   75" +
			"\nHot   -   60" +
			"\nNeutral   -   50" +
			"\nCold   -   40" +
			"\nVery cold   -   25" +
			"\n\nUnit: in-game temperature unit";
		using (Indent)
		{
			foreach (var step in Utils.TemperatureSteps)
				_temperatureDataByEnum[step].Format(step.ToString(), _temperatureToggle);
		}

		_overrideMaxSlope.Format("Override max slope");
		_overrideMaxSlope.Description =
			"Override the maximum slope a character can stand on before they start sliding down. Applies to all entities, not only the player!";
		using (Indent)
		{
			_maxSlope.Format("Max slope", _overrideMaxSlope);
			_maxSlope.Description =
				"Unit: degrees";
		}

		_earlyTotemWorkshops.Format("Early totem workshops");
		_earlyTotemWorkshops.Description =
			"Allows you to use totem workshops even if you haven't talked to Sinai yet";
	}
	#endregion

	#region Utility
	private const int DropOneActionID = -2;
	private const string DropOneActionText = "Drop one";
	private const int SalvageActionID = -3;
	private const string SalvageActionText = "Salvage";
	private const string ContextItemActionPrefix = "Context_Item_";
	private static HashSet<int> _itemContextActionsToRemove = [];
	private static bool ShouldArmorSlotBeHidden(EquipmentSlot.EquipmentSlotIDs slot)
		=> slot == EquipmentSlot.EquipmentSlotIDs.Helmet && !_visibleArmorSlots.Value.HasFlag(ArmorSlots.Head)
		|| slot == EquipmentSlot.EquipmentSlotIDs.Chest && !_visibleArmorSlots.Value.HasFlag(ArmorSlots.Chest)
		|| slot == EquipmentSlot.EquipmentSlotIDs.Foot && !_visibleArmorSlots.Value.HasFlag(ArmorSlots.Feet);
	private static bool HasLearnedArmorTraining(Character character)
		=> character.Inventory.SkillKnowledge.IsItemLearned("Armor Training".ToSkillID());
	private static bool TryApplyMultiplicativeStacking(CharacterEquipment equipment, ref float result, Func<EquipmentSlot, float> getStatValue, bool invertedPositivity = false, bool applyArmorTraining = false)
	{
		if (!_multiplicativeStatsStacking)
			return true;

		float invCoeff = invertedPositivity ? -1f : +1f;
		bool canApplyArmorTraining = applyArmorTraining && HasLearnedArmorTraining(equipment.m_character);

		result = 1f;
		foreach (var slot in equipment.m_equipmentSlots)
			if (slot.IsAnythingEquipped()
			&& !slot.IsLeftHandUsedBy2H())
			{
				float armorTrainingCoeff = canApplyArmorTraining && getStatValue(slot) > 0f ? 1f - _armorTrainingPenaltyReduction / 100f : 1f;
				result *= 1f + getStatValue(slot) / 100f * invCoeff * armorTrainingCoeff;
			}
		result -= 1f;
		result *= invCoeff;
		return false;
	}
	private static void UpdateBaseStaminaRegen(CharacterStats characterStats)
		=> characterStats.m_staminaRegen.BaseValue = _staminaRegen;
	private static void TryUpdateTemperatureData()
	{
		if (!_temperatureToggle)
			return;

		if (EnvironmentConditions.Instance.TryNonNull(out var environmentConditions))
			foreach (var step in Utils.TemperatureSteps)
				if (step != TemperatureSteps.Count)
				{
					environmentConditions.BodyTemperatureImpactPerStep[step] = _temperatureDataByEnum[step].Value.x;
					environmentConditions.TemperatureCaps[step] = _temperatureDataByEnum[step].Value.y;
				}
	}
	private static void CacheContextActionsToRemove()
		=> _itemContextActionsToRemove = _itemContextActions.Value.UnsetFlags()
		.Select(flag => (int)Math.Log((int)flag, 2))
		.ToHashSet();
	#endregion

	#region Hooks
	[HarmonyPostfix, HarmonyPatch(typeof(TitleScreenLoader), nameof(TitleScreenLoader.LoadTitleScreenCoroutine))]
	private static IEnumerator TitleScreenLoader_LoadTitleScreenCoroutine_Post(IEnumerator original, TitleScreenLoader __instance)
	{
		while (original.MoveNext())
			yield return original.Current;

		if (!_titleScreenCharacters)
			foreach (var characterVisuals in __instance.transform.GetAllComponentsInHierarchy<CharacterVisuals>())
				characterVisuals.Deactivate();
	}

	// Skip startup video
	[HarmonyPrefix, HarmonyPatch(typeof(StartupVideo), nameof(StartupVideo.Awake))]
	private static void StartupVideo_Awake_Pre()
		=> StartupVideo.HasPlayedOnce = !_introLogos.Value;

	// Remove redundant item action
	[HarmonyPostfix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.GetActiveActions))]
	private static void ItemDisplayOptionPanel_GetActiveActions_Post2(ItemDisplayOptionPanel __instance, ref List<int> __result)
	{
		if (!_itemContextActionsToRemove.Any()
		|| __instance?.m_activatedItemDisplay?.m_refItem is Skill)
			return;

		__result.RemoveAll(id => id.IsContainedIn(_itemContextActionsToRemove));
	}

	// Drop one
	[HarmonyPostfix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.GetActiveActions))]
	private static void ItemDisplayOptionPanel_GetActiveActions_Post(ItemDisplayOptionPanel __instance, ref List<int> __result)
	{
		if (!_itemActionDropOne || __instance == null ||
		!__instance.m_activatedItemDisplay.TryNonNull(out var itemDisplay)
		|| itemDisplay.StackCount <= 1
		|| itemDisplay.m_refItem is WaterContainer)
			return;

		__result.Add(DropOneActionID);
	}

	[HarmonyPrefix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.GetActionText))]
	private static bool ItemDisplayOptionPanel_GetActionText_Pre(ItemDisplayOptionPanel __instance, ref string __result, ref int _actionID)
	{
		if (_actionID != DropOneActionID)
			return true;

		__result = DropOneActionText;
		return false;
	}

	[HarmonyPrefix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.ActionHasBeenPressed))]
	private static bool ItemDisplayOptionPanel_ActionHasBeenPressed_Pre(ItemDisplayOptionPanel __instance, ref int _actionID)
	{
		if (_actionID != DropOneActionID)
			return true;

		__instance.m_activatedItemDisplay.OnConfirmDropStack(1);
		return false;
	}

	// Temperature data
	[HarmonyPostfix, HarmonyPatch(typeof(EnvironmentConditions), nameof(EnvironmentConditions.Start))]
	private static void EnvironmentConditions_Start_Post(EnvironmentConditions __instance)
		=> TryUpdateTemperatureData();

	// Stamina regen
	[HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterStats), nameof(PlayerCharacterStats.OnStart))]
	private static void PlayerCharacterStats_OnStart_Post(PlayerCharacterStats __instance)
		=> UpdateBaseStaminaRegen(__instance);

	// Load arrows from inventory
	[HarmonyPrefix, HarmonyPatch(typeof(WeaponLoadoutItem), nameof(WeaponLoadoutItem.ReduceShotAmount))]
	private static bool WeaponLoadoutItem_ReduceShotAmount_Pre(WeaponLoadoutItem __instance)
	{
		if (!_refillArrowsFromInventory
		|| __instance.AmunitionType != WeaponLoadout.CompatibleAmmunitionType.WeaponType
		|| __instance.CompatibleEquipment != Weapon.WeaponType.Arrow)
			return true;

		CharacterInventory inventory = __instance.m_projectileWeapon.OwnerCharacter.Inventory;
		int ammoID = inventory.GetEquippedAmmunition().ItemID;

		Item ammo = null;
		if (ammo == null && inventory.EquippedBag != null)
			ammo = inventory.EquippedBag.Container.GetItemFromID(ammoID);
		if (ammo == null)
			ammo = inventory.Pouch.GetItemFromID(ammoID);
		if (ammo == null)
			return true;

		ammo.RemoveQuantity(1);
		return false;
	}

	[HarmonyPostfix, HarmonyPatch(typeof(CharacterInventory), nameof(CharacterInventory.GetAmmunitionCount))]
	private static void CharacterInventory_GetAmmunitionCount_Post(CharacterInventory __instance, ref int __result)
	{
		if (!_refillArrowsFromInventory || __result == 0)
			return;

		__result += __instance.ItemCount(__instance.GetEquippedAmmunition().ItemID);
	}

	// Inn rent duration
	[HarmonyPrefix, HarmonyPatch(typeof(QuestEventData), nameof(QuestEventData.HasExpired))]
	private static void QuestEventData_HasExpired_Pre(QuestEventData __instance, ref int _gameHourAllowed)
	{
		if (__instance.m_signature.ParentSection.Name == Defaults.InnQuestsFamilyName)
			_gameHourAllowed = _rentDuration;
	}

	// Multiplicative stacking
	[HarmonyPrefix, HarmonyPatch(typeof(Stat), nameof(Stat.GetModifier))]
	private static bool Stat_GetModifier_Pre(Stat __instance, ref float __result, ref IList<Tag> _tags, ref int baseModifier)
	{
		if (!_multiplicativeStatsStacking)
			return true;

		DictionaryExt<string, StatStack> multipliers = __instance.m_multiplierStack;
		__result = baseModifier;
		for (int i = 0; i < multipliers.Count; i++)
		{
			if (multipliers.Values[i].HasEnded)
				multipliers.RemoveAt(i--);
			else if (multipliers.Values[i].SameTags(_tags))
			{
				float value = multipliers.Values[i].EffectiveValue;
				if (!__instance.NullifyPositiveStat || value <= 0f)
					__result *= 1f + value;
			}
		}
		return false;
	}

	[HarmonyPrefix, HarmonyPatch(typeof(CharacterEquipment), nameof(CharacterEquipment.GetTotalMovementModifier))]
	private static bool CharacterEquipment_GetTotalMovementModifier_Pre(CharacterEquipment __instance, ref float __result)
		=> TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.MovementPenalty, true, true);

	[HarmonyPrefix, HarmonyPatch(typeof(CharacterEquipment), nameof(CharacterEquipment.GetTotalStaminaUseModifier))]
	private static bool CharacterEquipment_GetTotalStaminaUseModifier_Pre(CharacterEquipment __instance, ref float __result)
		=> TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.StaminaUsePenalty, false, true);

	[HarmonyPrefix, HarmonyPatch(typeof(CharacterEquipment), nameof(CharacterEquipment.GetTotalManaUseModifier))]
	private static bool CharacterEquipment_GetTotalManaUseModifier_Pre(CharacterEquipment __instance, ref float __result)
		=> TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.ManaUseModifier, false, _armorTrainingAffectManaCost);

	// Hide armor slots
	[HarmonyPrefix, HarmonyPatch(typeof(CharacterVisuals), nameof(CharacterVisuals.EquipVisuals))]
	private static void CharacterVisuals_EquipVisuals_Pre(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
	{
		if (_visibleArmorSlots == ArmorSlots.All)
			return;

		// save original hide flags for postfix
		__state = new bool[3];
		__state[0] = _visuals.HideFace;
		__state[1] = _visuals.HideHair;
		__state[2] = _visuals.DisableDefaultVisuals;
		// override hide flags
		if (ShouldArmorSlotBeHidden(_slotID))
		{
			_visuals.HideFace = false;
			_visuals.HideHair = false;
			_visuals.DisableDefaultVisuals = false;
		}
	}

	[HarmonyPostfix, HarmonyPatch(typeof(CharacterVisuals), nameof(CharacterVisuals.EquipVisuals))]
	private static void CharacterVisuals_EquipVisuals_Post(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
	{
		if (_visibleArmorSlots == ArmorSlots.All)
			return;

		// hide chosen pieces of armor
		if (ShouldArmorSlotBeHidden(_slotID))
			_visuals.Hide();

		// restore original hide flags
		_visuals.HideFace = __state[0];
		_visuals.HideHair = __state[1];
		_visuals.DisableDefaultVisuals = __state[2];
	}

	// Remove co-op scaling
	[HarmonyPrefix, HarmonyPatch(typeof(CoopStats), nameof(CoopStats.ApplyToCharacter))]
	private static bool CoopStats_ApplyToCharacter_Pre()
		=> _multiplayerScaling;

	[HarmonyPrefix, HarmonyPatch(typeof(CoopStats), nameof(CoopStats.RemoveFromCharacter))]
	private static bool CoopStats_RemoveFromCharacter_Pre()
		=> _multiplayerScaling;

	// Enemy health reset time
	[HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.LoadCharSave))]
	private static void Character_LoadCharSave_Pre(Character __instance)
	{
		if (!__instance.IsEnemy())
			return;

		__instance.HoursToHealthReset = _enemiesHealOnLoad ? 0 : Defaults.EnemyHealthResetTime;
	}

	// Open region spawns
	[HarmonyPrefix, HarmonyPatch(typeof(AISquadManager), nameof(AISquadManager.Awake))]
	private static void AISquadManager_Awake_Pre(AISquadManager __instance)
	{
		if (_openRegionsEnemyDensity.Value == 0
		|| !Defaults.SquadCountsByRegion.TryGetValue(Utils.CurrentArea, out var targetSquadsCount))
			return;

		float alpha = _openRegionsEnemyDensity / 100f;
		__instance.MaxSquadCount = __instance.MaxSquadCount.Lerp(targetSquadsCount, alpha).Round();
		__instance.SpawnTime = __instance.SpawnTime.Lerp(1, alpha);
		__instance.SquadSpacing = __instance.SquadSpacing.Lerp(1, alpha);
		__instance.SpawnRange.x = __instance.SpawnRange.x.Lerp(1, alpha);
	}

	// Max Slope
	[HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.UpdateSlopeRotation))]
	private static void Character_UpdateSlopeRotation_Pre(Character __instance)
	{
		if (!_overrideMaxSlope)
			return;

		__instance.SlideSlope = _maxSlope;
	}

	// Early totem workshops
	[HarmonyPostfix, HarmonyPatch(typeof(Condition_QuestEventOccured), nameof(Condition_QuestEventOccured.OnCheck))]
	private static void EnvironmentConditions_Start_Post(Condition_QuestEventOccured __instance, ref bool __result)
	{
		if (!_earlyTotemWorkshops
		|| __instance.m_eventUID != TotemWorkshopsUnlockedEventUid)
			return;

		__result = true;
	}

	#endregion
}

/*
	// Salvage
	[HarmonyPostfix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.GetActiveActions))]
	private static void ItemDisplayOptionPanel_GetActiveActions_Post2(ItemDisplayOptionPanel __instance, ref List<int> __result)
	{
		if (!_itemActionSalvage
		|| __instance?.m_activatedItemDisplay?.m_refItem is not Item item
		|| !item.IsChildToPlayer
		|| item.IsEquipped
		|| !item.HasTag(TagSourceManager.GetCraftingIngredient(Recipe.CraftingType.Survival)))
			return;

		__result.Add(SalvageActionID);
	}

	[HarmonyPrefix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.GetActionText))]
	private static bool ItemDisplayOptionPanel_GetActionText_Pre2(ItemDisplayOptionPanel __instance, ref string __result, ref int _actionID)
	{
		if (_actionID != SalvageActionID)
			return true;

		__result = SalvageActionText;
		return false;
	}

	[HarmonyPrefix, HarmonyPatch(typeof(ItemDisplayOptionPanel), nameof(ItemDisplayOptionPanel.ActionHasBeenPressed))]
	private static bool ItemDisplayOptionPanel_ActionHasBeenPressed_Pre2(ItemDisplayOptionPanel __instance, ref int _actionID)
	{
		if (_actionID != SalvageActionID)
			return true;

		var craftingMenu = __instance.CharacterUI.CraftingMenu;
		craftingMenu.OnRecipeSelected(-1, true);
		craftingMenu.RefreshAutoRecipe();
		craftingMenu.IngredientSelectorHasChanged(0, __instance.m_activatedItemDisplay.m_refItem.ItemID);
		if (craftingMenu.m_lastFreeRecipeIndex == -1)
			return false;

		craftingMenu.OnCookButtonClicked();
		return false;
	} 
*/