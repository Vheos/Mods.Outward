using Vheos.Helpers.RNG;
namespace Vheos.Mods.Outward;

public class Damage : AMod
{
	#region Settings
	private static ModSetting<bool> _customStaggerSystem;
	private static ModSetting<Vector4> _staggerCurve;
	private static ModSetting<Vector4> _knockdownCurve;
	private static readonly Dictionary<Team, DamageSettings> _settingsByTeam = new();
	private class DamageSettings : PerValueSettings<Damage, Team>
	{
		public ModSetting<int> HealthDamageMultiplier, FFHealthDamageMultiplier;
		public ModSetting<int> StabilityDamageMultiplier, FFStabilityDamageMultiplier;
		public DamageSettings(Damage mod, Team team, bool isToggle = false) : base(mod, team, isToggle)
		{
			int ffMultiplier = team == Team.Players ? 0 : 100;
			HealthDamageMultiplier = CreateSetting(nameof(HealthDamageMultiplier), 100, mod.IntRange(0, 200));
			StabilityDamageMultiplier = CreateSetting(nameof(StabilityDamageMultiplier), 100, mod.IntRange(0, 200));
			FFHealthDamageMultiplier = CreateSetting(nameof(FFHealthDamageMultiplier), ffMultiplier, mod.IntRange(0, 200));
			FFStabilityDamageMultiplier = CreateSetting(nameof(FFStabilityDamageMultiplier), ffMultiplier, mod.IntRange(0, 200));
		}
	}
	protected override void Initialize()
	{
		_customStaggerSystem = CreateSetting(nameof(_customStaggerSystem), false);
		_staggerCurve = CreateSetting(nameof(_staggerCurve), new Vector4(50f, 50f, 100f, 0f));
		_knockdownCurve = CreateSetting(nameof(_knockdownCurve), new Vector4(0f, 0f, 100f, 0f));
		foreach (var team in Utility.GetEnumValues<Team>())
			_settingsByTeam[team] = new(this, team);
	}
	protected override void LoadPreset(string presetName)
	{
		switch (presetName)
		{
			case nameof(Preset.Vheos_CoopSurvival):
				ForceApply();
				_settingsByTeam[Team.Players].HealthDamageMultiplier.Value = 50;
				_settingsByTeam[Team.Players].StabilityDamageMultiplier.Value = 50;
				_settingsByTeam[Team.Players].FFHealthDamageMultiplier.Value = 20;
				_settingsByTeam[Team.Players].FFStabilityDamageMultiplier.Value = 40;

				_settingsByTeam[Team.Enemies].HealthDamageMultiplier.Value = 80;
				_settingsByTeam[Team.Enemies].StabilityDamageMultiplier.Value = 120;
				_settingsByTeam[Team.Enemies].FFHealthDamageMultiplier.Value = 20;
				_settingsByTeam[Team.Enemies].FFStabilityDamageMultiplier.Value = 40;
				break;
		}
	}
	#endregion

	#region Formatting
	protected override string SectionOverride
		=> ModSections.Combat;
	protected override string Description
		=> "• Adjust players' and enemies' damages" +
		"\n• Enable friendly fire between players" +
		"\n• Adjust friendly fire between enemies";
	protected override void SetFormatting()
	{
		_customStaggerSystem.Format("Custom stagger system");
		_customStaggerSystem.Description =
			"Overrides the vanilla stagger system and exposes additional settings" +
			"\nSimply put, allows you to change what happens when a character takes a hit to their stability";
		using (Indent)
		{
			_staggerCurve.Format("Stagger curve", _customStaggerSystem);
			_staggerCurve.Description =
				"When below X stability, characters have Y% to get staggered" +
				"\nWhen above Z stability, characters have W% to get staggered" +
				"\nBetween X and Z stability, the stagger chance is linearily interpolated";
			_knockdownCurve.Format("Knockdown curve", _staggerCurve);
			_knockdownCurve.Description =
				"When below X stability, characters have Y% to get knocked down" +
				"\nWhen above Z stability, characters have W% to get knocked down" +
				"\nBetween X and Z stability, the knockdown chance is linearily interpolated";
		}

		foreach (var settings in _settingsByTeam.Values)
		{
			settings.FormatHeader();
			string teamName = settings.Value.ToString().ToLower();

			settings.Header.Description =
				$"Multipliers for damages dealt by {teamName}";
			using (Indent)
			{
				settings.HealthDamageMultiplier.Format("Health");
				settings.HealthDamageMultiplier.Description =
					$"How much health damage {teamName} deal" +
					$"\n\nUnit: percent multiplier";
				settings.StabilityDamageMultiplier.Format("Stability");
				settings.StabilityDamageMultiplier.Description =
					$"How much stability damage {teamName} deal" +
					$"\n\nUnit: percent multiplier";
				CreateHeader("Friendly Fire").Description =
					$"Additional multipliers for damages dealt by {teamName} to other {teamName}";
				using (Indent)
				{
					settings.FFHealthDamageMultiplier.Format("Health");
					settings.FFHealthDamageMultiplier.Description =
						$"How much health damage {teamName} deal to other {teamName}" +
						$"\n\nUnit: percent multiplier";
					settings.FFStabilityDamageMultiplier.Format("Stability");
					settings.FFStabilityDamageMultiplier.Description =
						$"How much stability damage {teamName} deal to other {teamName}" +
						$"\n\nUnit: percent multiplier";
				}
			}
		}
	}
	#endregion

	#region Utility
	private static bool IsPlayersFriendlyFireEnabled
		=> _settingsByTeam[Team.Players].FFHealthDamageMultiplier > 0
		|| _settingsByTeam[Team.Players].FFStabilityDamageMultiplier > 0;
	private static void TryOverrideElligibleFaction(ref bool result, Character defender, Character attacker)
	{
		if (result
		|| defender == null
		|| defender == attacker
		|| !defender.IsAlly()
		|| !IsPlayersFriendlyFireEnabled)
			return;

		result = true;
	}
	private static void CustomStabilityHit(Character @this, float damage, float angle, bool isBlocking, Character dealer)
	{
		if (@this.PlayerType != PlayerSystem.PlayerTypes.None && (CharacterManager.Instance.IsSleepPending || CharacterManager.Instance.IsStartRestSent)
		|| damage <= 0f || @this.IsPetrified || @this.m_impactImmune || @this.m_pendingDeath)
			return;

		// No stamina
		if (@this.Stats.CurrentStamina < 1f)
			damage = damage.ClampMin(@this.m_shieldStability + @this.m_stability - 49f);

		// Blocked hit
		if (isBlocking && @this.m_shieldStability > 0f)
		{
			if (damage > @this.m_shieldStability)
				@this.m_stability -= damage - @this.m_shieldStability;

			@this.m_shieldStability = Mathf.Clamp(@this.m_shieldStability - damage, 0f, 50f);
		}
		// Unblocked hit
		else
			@this.m_stability = Mathf.Clamp(@this.m_stability - damage, 0f, 100f);

		var curve = _staggerCurve.Value;
		var staggerChance = @this.m_stability.Map(curve.x, curve.z, curve.y, curve.w);
		curve = _knockdownCurve.Value;
		var knockdownChance = @this.m_stability.Map(curve.x, curve.z, curve.y, curve.w);
		var roll = Rng.Float * 100f;

		// Knockdown
		if (knockdownChance > roll)
		{
			Knock(true);
		}
		// Stagger
		else if (staggerChance > roll)
		{
			Knock(false);
		}
		// Still blocking
		else if (isBlocking)
		{
			@this.m_hurtType = Character.HurtType.NONE;
			if (@this.InLocomotion)
				@this.m_animator.SetTrigger("BlockHit");
		}
		// Normal hit
		else if (@this.m_knockHurtAllowed)
		{
			@this.m_hurtType = Character.HurtType.Hurt;
			if (@this.m_currentlyChargingAttack)
				@this.CancelCharging();

			@this.m_animator.SetTrigger("Knockhurt");
			if (@this.knockhurt != null)
				@this.StopCoroutine(@this.knockhurt);

			@this.knockhurt = @this.StartCoroutine(@this.KnockhurtRoutine(damage));
		}

		// Shake camera
		if (@this.CharacterCamera != null)
			@this.CharacterCamera.Hit(damage * 6f);

		@this.m_timeOfLastStabilityHit = Time.time;
		@this.m_animator.SetInteger("KnockAngle", (int)angle);
		@this.StabilityHitCall?.Invoke();

		// Local methods
		void Knock(bool down)
		{
			if (!@this.IsAI && @this.photonView.isMine
			|| @this.IsAI && (dealer == null || dealer.photonView.isMine))
				@this.photonView.RPC("SendKnock", PhotonTargets.All, [down, @this.m_stability]);
			else
				@this.Knock(down);

			if (@this.IsPhotonPlayerLocal)
				@this.BlockInput(false);

			if (down)
			{
				@this.m_stability = 0f;
				@this.Invoke("DelayedCheckFootStep", 0.1f);
			}
		}
	}
	#endregion

	#region Hooks
	[HarmonyPostfix, HarmonyPatch(typeof(Weapon), nameof(Weapon.ElligibleFaction), new[] { typeof(Character) })]
	private static void Weapon_ElligibleFaction_Post(Weapon __instance, ref bool __result, Character _character)
		=> TryOverrideElligibleFaction(ref __result, _character, __instance.OwnerCharacter);

	[HarmonyPostfix, HarmonyPatch(typeof(MeleeHitDetector), nameof(MeleeHitDetector.ElligibleFaction), new[] { typeof(Character) })]
	private static void MeleeHitDetector_ElligibleFaction_Post(MeleeHitDetector __instance, ref bool __result, Character _character)
		=> TryOverrideElligibleFaction(ref __result, _character, __instance.OwnerCharacter);

	[HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.OnReceiveHitCombatEngaged))]
	private static bool Character_OnReceiveHitCombatEngaged_Pre(Character __instance, Character _dealerChar)
		=> _dealerChar == null
		|| !_dealerChar.IsAlly()
		|| !IsPlayersFriendlyFireEnabled;

	[HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.VitalityHit))]
	private static void Character_VitalityHit_Pre(Character __instance, Character _dealerChar, ref float _damage)
	{
		if (_dealerChar != null && _dealerChar.IsEnemy()
		|| _dealerChar == null && __instance.IsAlly())
		{
			_damage *= _settingsByTeam[Team.Enemies].HealthDamageMultiplier / 100f;
			if (__instance.IsEnemy())
				_damage *= _settingsByTeam[Team.Enemies].FFHealthDamageMultiplier / 100f;
		}
		else
		{
			_damage *= _settingsByTeam[Team.Players].HealthDamageMultiplier / 100f;
			if (__instance.IsAlly())
				_damage *= _settingsByTeam[Team.Players].FFHealthDamageMultiplier / 100f;
		}
	}

	[HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.StabilityHit))]
	private static bool Character_StabilityHit_Pre(Character __instance, ref float _knockValue, float _angle, bool _block, Character _dealerChar)
	{
		if (_dealerChar != null && _dealerChar.IsEnemy()
		|| _dealerChar == null && __instance.IsAlly())
		{
			_knockValue *= _settingsByTeam[Team.Enemies].StabilityDamageMultiplier / 100f;
			if (__instance.IsEnemy())
				_knockValue *= _settingsByTeam[Team.Enemies].FFStabilityDamageMultiplier / 100f;
		}
		else
		{
			_knockValue *= _settingsByTeam[Team.Players].StabilityDamageMultiplier / 100f;
			if (__instance.IsAlly())
				_knockValue *= _settingsByTeam[Team.Players].FFStabilityDamageMultiplier / 100f;
		}

		if (!_customStaggerSystem)
			return true;

		CustomStabilityHit(__instance, _knockValue, _angle, _block, _dealerChar);
		return false;
	}


	#endregion
}