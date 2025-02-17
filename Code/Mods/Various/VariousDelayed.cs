
namespace Vheos.Mods.Outward;
public class VariousDelayed : AMod, IDelayedInit
{
	#region Constants
	private static readonly Item[] ARROWS = new[]
{
		"Arrow".ToItemPrefab(),
		"Flaming Arrow".ToItemPrefab(),
		"Poison Arrow".ToItemPrefab(),
		"Venom Arrow".ToItemPrefab(),
		"Palladium Arrow".ToItemPrefab(),
		"Explosive Arrow".ToItemPrefab(),
		"Forged Arrow".ToItemPrefab(),
		"Holy Rage Arrow".ToItemPrefab(),
		"Soul Rupture Arrow".ToItemPrefab(),
		"Mana Arrow".ToItemPrefab(),
	};

	#endregion

	// Settings
	private static ModSetting<int> _arrowStackSize;
	private static ModSetting<int> _arrowSalvageChance;
	private static ModSetting<int> _bulletStackSize;
	private static ModSetting<bool> _statusEffectFamilyMultipliersToggle;
	private static ModSetting<bool> _applyMultipliers;
	private static ModSetting<Vector2> _healthRecovery;
	private static ModSetting<Vector2> _staminaRecovery;
	private static ModSetting<Vector2> _manaRecovery;

	protected override void Initialize()
	{
		_arrowStackSize = CreateSetting(nameof(_arrowStackSize), 15, IntRange(0, 100));
		_arrowSalvageChance = CreateSetting(nameof(_arrowSalvageChance), 30, IntRange(0, 100));
		_bulletStackSize = CreateSetting(nameof(_bulletStackSize), 12, IntRange(0, 100));

		_statusEffectFamilyMultipliersToggle = CreateSetting(nameof(_statusEffectFamilyMultipliersToggle), false);
		_applyMultipliers = CreateSetting(nameof(_applyMultipliers), false);
		_applyMultipliers.IsAdvanced = true;
		_healthRecovery = CreateSetting(nameof(_healthRecovery), new Vector2(1f, 1f));
		_staminaRecovery = CreateSetting(nameof(_staminaRecovery), new Vector2(1f, 1f));
		_manaRecovery = CreateSetting(nameof(_manaRecovery), new Vector2(1f, 1f));

		// Events
		_arrowStackSize.AddEvent(UpdateArrowsStackSize);
		_bulletStackSize.AddEvent(() => "Bullet".ToItemPrefab().m_stackable.m_maxStackAmount = _bulletStackSize);

		TryOverrideStatusEffectPrefabs();
		_applyMultipliers.AddEvent(() =>
		{
			if (_applyMultipliers)
				TryOverrideStatusEffectPrefabs();

			_applyMultipliers.SetSilently(false);
		});
	}
	protected override void SetFormatting()
	{
		_arrowStackSize.Format("Arrows stack size");
		_arrowSalvageChance.Format("Arrows salvage chance");
		_bulletStackSize.Format("Bullets stack size");
		_statusEffectFamilyMultipliersToggle.Format("Override status effect families");
		_statusEffectFamilyMultipliersToggle.Description =
			"Multiplies durations (x) and effect values (y) of all status effects in the given family";
		using (Indent)
		{
			_applyMultipliers.Format("Apply multipliers");
			_healthRecovery.Format("Health Recovery", _statusEffectFamilyMultipliersToggle);
			_staminaRecovery.Format("Stamina Recovery", _statusEffectFamilyMultipliersToggle);
			_manaRecovery.Format("Mana Recovery", _statusEffectFamilyMultipliersToggle);
		}
	}
	protected override string Description
	=> "• Mods that need to run after the game is initialized";
	protected override string SectionOverride
	=> "";
	protected override string ModName
	=> "Various (delayed)";
	protected override void LoadPreset(string presetName)
	{
		switch (presetName)
		{
			case nameof(Preset.Vheos_CoopSurvival):
				ForceApply();
				_arrowStackSize.Value = 20;
				_bulletStackSize.Value = 20;
				break;
		}
	}

	// Utility
	private void UpdateArrowsStackSize()
	{
		foreach (var arrow in ARROWS)
			arrow.m_stackable.m_maxStackAmount = _arrowStackSize;
	}
	private void TryOverrideStatusEffectPrefabs()
	{
		if (!_statusEffectFamilyMultipliersToggle)
			return;

		foreach (var statusEffect in Prefabs.StatusEffectsByNameID.Values)
		{
			if (statusEffect.EffectFamily is not StatusEffectFamily family
			|| statusEffect.StatusData is not StatusData data)
				continue;

			switch (family.Name)
			{
				case "Health Recovery":
					ApplyStatusEffectMultipliers(data, _healthRecovery.Value.x, _healthRecovery.Value.y);
					break;
				case "Stamina Recovery":
					ApplyStatusEffectMultipliers(data, _staminaRecovery.Value.x, _staminaRecovery.Value.y);
					break;
				case "Mana Recovery":
					ApplyStatusEffectMultipliers(data, _manaRecovery.Value.x, _manaRecovery.Value.y);
					break;
			}
		}
	}
	private void ApplyStatusEffectMultipliers(StatusData statusData, float durationMultiplier, float valueMultiplier)
	{
		statusData.LifeSpan *= durationMultiplier;

		if (statusData.EffectsData is not StatusData.EffectData[] effectDatas)
			return;

		for (int i = 0; i < effectDatas.Length; i++)
		{
			var effectDataCopy = effectDatas[i];
			if (effectDataCopy.Data is string[] values)
				for (int j = 0; j < values.Length; j++)
					if (float.TryParse(values[j], out float parsedValue))
						values[j] = (parsedValue * valueMultiplier).ToString();

			effectDatas[i] = effectDataCopy;
		}
	}

	// Hooks
	[HarmonyPrefix, HarmonyPatch(typeof(ProjectileItem), nameof(ProjectileItem.Awake))]
	private static void ProjectileItem_Awake_Pre(ProjectileItem __instance)
		=> __instance.SalvageChance = _arrowSalvageChance;
}