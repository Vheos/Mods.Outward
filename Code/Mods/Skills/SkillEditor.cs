﻿namespace Vheos.Mods.Outward;

public class SkillEditor : AMod, IDelayedInit
{
	#region Constants
	private const string RUNIC_LANTERN_ID = "Runic Lantern";
	private const float RUNIC_LANTERN_EMISSION_RATE = 20;
	private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_DAGGER_SLASH =
	(
		new Vector3(0, 0, 0),
		new Vector3(0, 2, 0),
		new Vector3(0, 0, 0.5f)
	);
	private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_BACKSTAB =
	(
		new Vector3(1, 3, 0),
		new Vector3(0, 5, 0),
		new Vector3(0, 0, 15)
	);
	private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_EVASION_SHOT =
	(
		new Vector3(0, 0, 0),
		new Vector3(0, 12, 0),
		new Vector3(0, 0, 30)
	);
	private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_OPPORTUNIST_STAB =
	(
		new Vector3(0, 0, 0),
		new Vector3(0, 5, 0),
		new Vector3(0, 0, 10)
	);
	private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_SERPENTS_PARRY =
	(
		new Vector3(0, 0, 0),
		new Vector3(0, 7, 0),
		new Vector3(0, 0, 100)
	);
	private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_SNIPER_SHOT =
	(
		new Vector3(0, 0, 0),
		new Vector3(0, 15, 0),
		new Vector3(0, 0, 30)
	);
	private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_PIERCING_SHOT =
	(
		new Vector3(0, 0, 0),
		new Vector3(0, 15, 0),
		new Vector3(0, 0, 30)
	);
	private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_RUNE =
	(
		new Vector3(0, 0, 0),
		new Vector3(0, 0, 8),
		new Vector3(0, 0, 2)
	);
	private readonly (Vector3, Vector3, Vector3) DEFAULT_VALUES_TOTEM =
	(
		new Vector3(0, 0, 0),
		new Vector3(0, 20, 0),
		new Vector3(0, 0, 120)
	);
	#endregion
	#region class
	private class SkillData
	{
		// Settings
		public ModSetting<bool> _toggle;
		public ModSetting<Vector3> _effects, _vitalCosts, _otherCosts;
		public void CreateSettings(string skillSettingName, Skill prefab, Vector3 effects, Vector3 vitalCosts, Vector3 otherCosts)
		{
			_toggle = _mod.CreateSetting(skillSettingName + nameof(_toggle), false);
			_effects = _mod.CreateSetting(skillSettingName + nameof(_effects), effects);
			_vitalCosts = _mod.CreateSetting(skillSettingName + nameof(_vitalCosts), vitalCosts);
			_otherCosts = _mod.CreateSetting(skillSettingName + nameof(_otherCosts), otherCosts);

			_effects.AddEvent(() => TryApplyEffectsToPrefab(prefab));
			_vitalCosts.AddEvent(() => TryApplyVitalCostsToPrefab(prefab));
			_otherCosts.AddEvent(() => TryApplyOtherCostsToPrefab(prefab));
		}
		public void FormatSettings(ModSetting<bool> toggle = null)
		{
			if (toggle != null)
				_toggle.Format(_skillName, toggle);
			else
				_toggle.Format(_skillName);

			using (Indent)
			{
				// Effects description
				string text = "";
				if (_effectX.IsNotNullOrEmpty())
					text += $"X   -   {_effectX}\n";
				if (_effectY.IsNotNullOrEmpty())
					text += $"Y   -   {_effectY}\n";
				if (_effectZ.IsNotNullOrEmpty())
					text += $"Z   -   {_effectZ}\n";
				text = text.TrimEnd(['\n']);

				// Format
				if (text.IsNotNullOrEmpty())
				{
					_effects.Format("Effects", _toggle);
					_effects.Description = text;
				}
				_vitalCosts.Format("Costs (vitals)", _toggle);
				_vitalCosts.Description = "X   -   Health\n" +
										  "Y   -   Stamina\n" +
										  "Z   -   Mana";
				_otherCosts.Format("Costs (other)", _toggle);
				_otherCosts.Description = "X   -   Durability\n" +
										  "Y   -   Durability %\n" +
										  "Z   -   Cooldown";
			}
		}

		// Constructor
		public SkillData(SkillEditor mod, string skillSettingName, string skillName, (Vector3 Effects, Vector3 VitalCosts, Vector3 OtherCosts) defaultValues)
		{
			_mod = mod;
			_skillName = skillName;
			CreateSettings(skillSettingName, _skillName.ToSkillPrefab(),
						   defaultValues.Effects, defaultValues.VitalCosts, defaultValues.OtherCosts);
		}

		// Utility
		private readonly SkillEditor _mod;
		private readonly string _skillName;
		private string _effectX, _effectY, _effectZ;
		private Action<Skill, float> _applyEffectX, _applyEffectY, _applyEffectZ;
		private void TryApplyEffectsToPrefab(Skill prefab)
		{
			if (!_toggle)
				return;

			_applyEffectX?.Invoke(prefab, _effects.Value.x);
			_applyEffectY?.Invoke(prefab, _effects.Value.y);
			_applyEffectZ?.Invoke(prefab, _effects.Value.z);
		}
		private void TryApplyVitalCostsToPrefab(Skill prefab)
		{
			if (!_toggle)
				return;

			prefab.HealthCost = _vitalCosts.Value.x;
			prefab.StaminaCost = _vitalCosts.Value.y;
			prefab.ManaCost = _vitalCosts.Value.z;
		}
		private void TryApplyOtherCostsToPrefab(Skill prefab)
		{
			if (!_toggle)
				return;

			prefab.DurabilityCost = _otherCosts.Value.x;
			prefab.DurabilityCostPercent = _otherCosts.Value.y;
			prefab.Cooldown = _otherCosts.Value.z;
		}
		public void InitializeEffectX(string effectName, Action<Skill, float> applyLogic)
		{
			_effectX = effectName;
			_applyEffectX = applyLogic;
		}
		public void InitializeEffectY(string effectName, Action<Skill, float> applyLogic)
		{
			_effectY = effectName;
			_applyEffectY = applyLogic;
		}
		public void InitializeEffectZ(string effectName, Action<Skill, float> applyLogic)
		{
			_effectZ = effectName;
			_applyEffectZ = applyLogic;
		}
	}
	#endregion

	// Settings
	private static ModSetting<bool> _daggerToggle, _bowToggle, _runesToggle, _totemsToggle;
	private static SkillData _daggerSlash, _backstab, _opportunistStab, _serpentsParry;
	private static SkillData _evasionShot, _sniperShot, _piercingShot;
	private static SkillData _dez, _egoth, _fal, _shim;
	private static SkillData _hauntingBeat, _welkinRing;
	private static ModSetting<int> _runeSoundEffectVolume, _runicLanternIntensity;
	protected override void Initialize()
	{
		_daggerToggle = CreateSetting(nameof(_daggerToggle), false);
		_daggerSlash = new SkillData(this, nameof(_daggerSlash), "Dagger Slash", DEFAULT_VALUES_DAGGER_SLASH);
		_backstab = new SkillData(this, nameof(_backstab), "Backstab", DEFAULT_VALUES_BACKSTAB);
		_opportunistStab = new SkillData(this, nameof(_opportunistStab), "Opportunist Stab", DEFAULT_VALUES_OPPORTUNIST_STAB);
		_serpentsParry = new SkillData(this, nameof(_serpentsParry), "Serpent's Parry", DEFAULT_VALUES_SERPENTS_PARRY);

		_bowToggle = CreateSetting(nameof(_bowToggle), false);
		_evasionShot = new SkillData(this, nameof(_evasionShot), "Evasion Shot", DEFAULT_VALUES_EVASION_SHOT);
		_sniperShot = new SkillData(this, nameof(_sniperShot), "Sniper Shot", DEFAULT_VALUES_SNIPER_SHOT);
		_piercingShot = new SkillData(this, nameof(_piercingShot), "Piercing Shot", DEFAULT_VALUES_PIERCING_SHOT);

		_runesToggle = CreateSetting(nameof(_runesToggle), false);
		_dez = new SkillData(this, nameof(_dez), "Dez", DEFAULT_VALUES_RUNE);
		_egoth = new SkillData(this, nameof(_egoth), "Egoth", DEFAULT_VALUES_RUNE);
		_fal = new SkillData(this, nameof(_fal), "Fal", DEFAULT_VALUES_RUNE);
		_shim = new SkillData(this, nameof(_shim), "Shim", DEFAULT_VALUES_RUNE);
		_runicLanternIntensity = CreateSetting(nameof(_runicLanternIntensity), 100, IntRange(0, 100));
		_runeSoundEffectVolume = CreateSetting(nameof(_runeSoundEffectVolume), 100, IntRange(0, 100));

		_totemsToggle = CreateSetting(nameof(_totemsToggle), false);
		_hauntingBeat = new SkillData(this, nameof(_hauntingBeat), "Haunting Beat", DEFAULT_VALUES_TOTEM);
		_welkinRing = new SkillData(this, nameof(_welkinRing), "Welkin Ring", DEFAULT_VALUES_TOTEM);

		// Editor effects
		_backstab.InitializeEffectX("Frontstab multiplier", (prefab, value) =>
		{
			WeaponDamage front = prefab.transform.Find("HitEffects").GetComponent<WeaponDamage>();
			front.WeaponDamageMult = front.WeaponKnockbackMult = value;
		});
		_backstab.InitializeEffectY("Backstab multiplier", (prefab, value) =>
		{
			WeaponDamage back = prefab.transform.Find("BackstabHitEffects").GetComponent<WeaponDamage>();
			back.WeaponDamageMult = back.WeaponKnockbackMult = value;
		});
	}
	protected override void SetFormatting()
	{
		_daggerToggle.Format("Dagger");
		using (Indent)
		{
			_daggerSlash.FormatSettings(_daggerToggle);
			_backstab.FormatSettings(_daggerToggle);
			_opportunistStab.FormatSettings(_daggerToggle);
			_serpentsParry.FormatSettings(_daggerToggle);
		}

		_bowToggle.Format("Bow");
		using (Indent)
		{
			_evasionShot.FormatSettings(_bowToggle);
			_sniperShot.FormatSettings(_bowToggle);
			_piercingShot.FormatSettings(_bowToggle);
		}

		_runesToggle.Format("Runes");
		using (Indent)
		{
			_dez.FormatSettings(_runesToggle);
			_egoth.FormatSettings(_runesToggle);
			_fal.FormatSettings(_runesToggle);
			_shim.FormatSettings(_runesToggle);
			_runeSoundEffectVolume.Format("Runes sound effect volume", _runesToggle);
			_runeSoundEffectVolume.Description = "If the runes are too loud for your ears";
			_runicLanternIntensity.Format("Runic Lantern intensity", _runesToggle);
			_runicLanternIntensity.Description = "If the glowing orb is too bright for your eyes";
		}

		_totemsToggle.Format("Totems");
		using (Indent)
		{
			_hauntingBeat.FormatSettings(_totemsToggle);
			_welkinRing.FormatSettings(_totemsToggle);
		}
	}
	protected override string Description
	=> "• Change effects, costs and cooldown of select skills";
	protected override string SectionOverride
	=> ModSections.Skills;
	protected override string ModName
	=> "Editor";
	protected override void LoadPreset(string presetName)
	{
		switch (presetName)
		{
			case nameof(Preset.Vheos_CoopSurvival):
				ForceApply();
				_daggerToggle.Value = true;
				{
					_daggerSlash._toggle.Value = true;
					{
						_daggerSlash._vitalCosts.Value = new Vector3(0, 3, 0);
						_daggerSlash._otherCosts.Value = new Vector3(0, 0, 1.5f);
					}
					_backstab._toggle.Value = true;
					{
						_backstab._effects.Value = new Vector3(0.5f, 3.0f, 0);
						_backstab._vitalCosts.Value = new Vector3(0, 9, 0);
						_backstab._otherCosts.Value = new Vector3(0, 0, 30);
					}
				}
				_bowToggle.Value = true;
				{
					_evasionShot._toggle.Value = true;
					{
						_evasionShot._vitalCosts.Value = new Vector3(0, 10, 0);
						_evasionShot._otherCosts.Value = new Vector3(0, 0, 20);
					}
				}
				_runesToggle.Value = true;
				{
					_dez._toggle.Value = true;
					{
						_dez._vitalCosts.Value = new Vector3(0, 0, 9);
						_dez._otherCosts.Value = new Vector3(0, 0, 0);
					}
					_egoth._toggle.Value = true;
					{
						_egoth._vitalCosts.Value = new Vector3(3, 0, 6);
						_egoth._otherCosts.Value = new Vector3(0, 0, 0);
					}
					_fal._toggle.Value = true;
					{
						_fal._vitalCosts.Value = new Vector3(3, 4.5f, 3);
						_fal._otherCosts.Value = new Vector3(0, 0, 0);
					}
					_shim._toggle.Value = true;
					{
						_shim._vitalCosts.Value = new Vector3(0, 4.5f, 6);
						_shim._otherCosts.Value = new Vector3(0, 0, 0);
					}
				}
				_runicLanternIntensity.Value = 10;
				_runeSoundEffectVolume.Value = 50;
				break;
		}
	}

	// Hooks
	[HarmonyPrefix, HarmonyPatch(typeof(AddStatusEffect), nameof(AddStatusEffect.ActivateLocally))]
	private static void AddStatusEffect_ActivateLocally_Pre(AddStatusEffect __instance)
	{
		#region quit
		if (!_runesToggle || __instance.Status == null || __instance.Status.IdentifierName != RUNIC_LANTERN_ID)
			return;
		#endregion

		// Cache
		ParticleSystem particleSystem = __instance.Status.FXPrefab.GetFirstComponentsInHierarchy<ParticleSystem>();
		ParticleSystem.MainModule main = particleSystem.main;
		ParticleSystem.EmissionModule emission = particleSystem.emission;
		Color newColor = main.startColor.color;
		float intensity = _runicLanternIntensity / 100f;

		// Execute
		newColor.a = intensity;
		main.startColor = newColor;
		emission.rateOverTime = intensity.MapFrom01(2, RUNIC_LANTERN_EMISSION_RATE);
	}

	[HarmonyPrefix, HarmonyPatch(typeof(PlaySoundEffect), nameof(PlaySoundEffect.ActivateLocally))]
	private static bool PlaySoundEffect_ActivateLocally_Pre(PlaySoundEffect __instance)
	{
		#region quit
		if (!_runesToggle || __instance.Sounds.IsNullOrEmpty() || __instance.Sounds.First() != GlobalAudioManager.Sounds.SFX_SKILL_RuneSpell)
			return true;
		#endregion

		float volume = _runeSoundEffectVolume / 100f;
		Global.AudioManager.PlaySoundAtPosition(GlobalAudioManager.Sounds.SFX_SKILL_RuneSpell, __instance.transform, 0f, volume, volume, __instance.MinPitch, __instance.MaxPitch);
		return false;
	}
}
