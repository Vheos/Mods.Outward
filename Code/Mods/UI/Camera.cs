namespace Vheos.Mods.Outward;

public class Camera : AMod, IDelayedInit, IUpdatable
{
	#region Constants
	private static readonly Vector3 DEFAULT_OFFSET = new(0f, 1f, -3f);
	private const float DEFAULT_FOV = 50f;
	private const float DEFAULT_FOLLOW_SPEED = 4.5f;
	private const float AIM_COROUTINE_UPDATE_SPEED = 0.2f;
	private const float AIM_COROUTINE_MARGIN = 0.01f;
	#endregion
	#region Enums
	[Flags]
	private enum GamepadInputs
	{
		None = 0,

		LeftQS = 1 << 1,
		RightQS = 1 << 2,
		Sprint = 1 << 3,
		Block = 1 << 4,
	}
	private enum AxisOverride
	{
		None = 0,
		Normal = 1,
		Inverted = 2,
	}
	#endregion
	#region class
	private class PerPlayerSettings
	{
		// Settings
		public ModSetting<bool> _toggle;
		public ModSetting<float> _zoomControlAmount;
		public ModSetting<float> _zoomControlSpeed;
		public ModSetting<GamepadInputs> _gamepadInputs;
		public ModSetting<bool> _offsetToggle, _variousToggle;
		public ModSetting<Vector3> _offsetMin, _offsetAvg, _offsetMax;
		public ModSetting<Vector3> _variousMin, _variousAvg, _variousMax;
		public ModSetting<bool> _rotateWhenMovingWithGamepad;
		public ModSetting<AxisOverride> _overrideInvertX, _overrideInvertY;

		// Utility
		public void StartAimCoroutine(MonoBehaviour owner, bool isEnteringAimMode)
		{
			// Choose zoom in or out
			float targetZoom = 0f;
			Func<bool> test = () => _zoomControlAmount.Value < targetZoom + AIM_COROUTINE_MARGIN;
			if (isEnteringAimMode)
			{
				targetZoom = 1f;
				test = () => _zoomControlAmount.Value > targetZoom - AIM_COROUTINE_MARGIN;
			}

			// Update over time
			if (_aimCoroutine != null)
				owner.StopCoroutine(_aimCoroutine);
			_aimCoroutine = owner.ExecuteUntil
			(
				test,
				() => _zoomControlAmount.Value = _zoomControlAmount.Value.Lerp(targetZoom, AIM_COROUTINE_UPDATE_SPEED),
				() => _zoomControlAmount.Value = targetZoom
			);
		}
		public Vector3 CurrentOffset
			=> _zoomControlAmount < 0f ? Vector3.LerpUnclamped(_offsetMin, _offsetAvg, _zoomControlAmount + 1f)
			: _zoomControlAmount > 0f ? Vector3.LerpUnclamped(_offsetAvg, _offsetMax, _zoomControlAmount)
			: (Vector3)_offsetAvg;
		public Vector3 CurrentVarious
			=> _zoomControlAmount < 0f ? Vector3.LerpUnclamped(_variousMin, _variousAvg, _zoomControlAmount + 1f)
			: _zoomControlAmount > 0f ? Vector3.LerpUnclamped(_variousAvg, _variousMax, _zoomControlAmount)
			: (Vector3)_variousAvg;
		public float Sensitivity
		{
			get => IgnoreAxes ? 0f : _cachedSensitivity;
			set => _cachedSensitivity = value;
		}
		public bool IgnoreAxes;
		private float _cachedSensitivity;
		private Coroutine _aimCoroutine;
	}
	#endregion

	private static PerPlayerSettings[] _perPlayerSettings;
	protected override void Initialize()
	{
		_perPlayerSettings = new PerPlayerSettings[2];
		for (int i = 0; i < 2; i++)
		{
			PerPlayerSettings pps = new();
			_perPlayerSettings[i] = pps;
			string playerPrefix = $"player{i + 1}";

			pps._toggle = CreateSetting(playerPrefix + nameof(pps._toggle), false);
			pps._zoomControlAmount = CreateSetting(playerPrefix + nameof(pps._zoomControlAmount), 0f, FloatRange(-1f, 1f));
			pps._zoomControlSpeed = CreateSetting(playerPrefix + nameof(pps._zoomControlSpeed), 0.5f, FloatRange(0f, 1f));
			pps._gamepadInputs = CreateSetting(playerPrefix + nameof(pps._gamepadInputs), GamepadInputs.LeftQS | GamepadInputs.RightQS);

			pps._offsetToggle = CreateSetting(playerPrefix + nameof(pps._offsetToggle), false);
			pps._offsetMin = CreateSetting(playerPrefix + nameof(pps._offsetMin), DEFAULT_OFFSET.Add(0, 1f, -3f));
			pps._offsetAvg = CreateSetting(playerPrefix + nameof(pps._offsetAvg), DEFAULT_OFFSET);
			pps._offsetMax = CreateSetting(playerPrefix + nameof(pps._offsetMax), DEFAULT_OFFSET.Add(0.75f, -0.25f, 1f));

			Vector3 otherDefault = new(DEFAULT_FOV, DEFAULT_FOLLOW_SPEED, 1f);
			pps._variousToggle = CreateSetting(playerPrefix + nameof(pps._variousToggle), false);
			pps._variousMin = CreateSetting(playerPrefix + nameof(pps._variousMin), otherDefault.Mul(1.2f, 2 / 3f, 1f));
			pps._variousAvg = CreateSetting(playerPrefix + nameof(pps._variousAvg), otherDefault);
			pps._variousMax = CreateSetting(playerPrefix + nameof(pps._variousMax), otherDefault.Mul(0.8f, 2.0f, 0.75f));

			pps._rotateWhenMovingWithGamepad = CreateSetting(playerPrefix + nameof(pps._rotateWhenMovingWithGamepad), true);
			pps._overrideInvertX = CreateSetting(playerPrefix + nameof(pps._overrideInvertX), AxisOverride.None);
			pps._overrideInvertY = CreateSetting(playerPrefix + nameof(pps._overrideInvertY), AxisOverride.None);

			int id = i;
			pps._zoomControlAmount.AddEvent(() =>
			{
				if (Players.TryGetLocal(id, out Players.Data player))
					UpdateCameraSettings(player);
			});

			AddEventOnConfigClosed(() =>
			{
				foreach (var player in Players.Local)
					UpdateCameraSettings(player);
			});

			pps.Sensitivity = 1f;
		}
	}
	protected override void SetFormatting()
	{
		for (int i = 0; i < 2; i++)
		{
			PerPlayerSettings pps = _perPlayerSettings[i];

			// Settings
			pps._toggle.Format($"Player {i + 1}");
			pps._toggle.Description = $"Change settings for local player {i + 1}";
			using (Indent)
			{
				pps._zoomControlAmount.Format("Zoom amount", pps._toggle);
				pps._zoomControlAmount.Description = "-1  -  max zoom out\n" +
													"0  -  default zoom\n" +
													"+1  -  max zoom in";
				pps._zoomControlSpeed.Format("Zoom control speed", pps._toggle);
				pps._zoomControlSpeed.Description = "How quickly you want to zoom using mouse/gamepad\n" +
					"Mouse: use mouse scroll wheel\n" +
					"Gamepad: use right stick while holding defined actions";
				using (Indent)
				{
					pps._gamepadInputs.Format("Gamepad hotkey", pps._zoomControlSpeed, t => t > 0);
					pps._gamepadInputs.Description = "Gamepad actions you need to hold to control camera. Defaults:\n" +
													 "LeftQS = LT\n" +
													 "RightQS = RT\n" +
													 "Sprint = LB\n" +
													 "Block = RB";
				}
				pps._offsetToggle.Format("Offset", pps._toggle);
				pps._offsetToggle.Description = "Change camera position (XYZ) presets";
				using (Indent)
				{
					pps._offsetMin.Format("at zoom =  -1", pps._offsetToggle);
					pps._offsetAvg.Format("at zoom =  0", pps._offsetToggle);
					pps._offsetMax.Format("at zoom = +1", pps._offsetToggle);
				}

				pps._variousToggle.Format("FOV, FollowSpeed, Sensitivity", pps._toggle);
				pps._variousToggle.Description = "Change other settings presets:\n" +
					"X  -  field of view (vertical)\n" +
					"Y  -  how quickly camera follows your character\n" +
					"Z  -  how quickly camera rotates";
				using (Indent)
				{
					pps._variousMin.Format("at zoom =  -1", pps._variousToggle);
					pps._variousAvg.Format("at zoom =  0", pps._variousToggle);
					pps._variousMax.Format("at zoom = +1", pps._variousToggle);
				}

				pps._rotateWhenMovingWithGamepad.Format("Rotate when moving with gamepad", pps._toggle);
				pps._overrideInvertX.Format("Override \"Invert X Camera\"", pps._toggle);
				pps._overrideInvertY.Format("Override \"Invert Y Camera\"", pps._toggle);
			}
		}
	}
	protected override string Description
	=> "• Override camera settings\n" +
	   "(position, fov, follow speed, sensitivity)\n" +
	   "• Control camera using mouse/gamepad\n" +
	   "• Define presets and smoothly interpolate between them";
	protected override string SectionOverride
	=> ModSections.UI;
	protected override void LoadPreset(string presetName)
	{
		switch (presetName)
		{
			case nameof(Preset.Vheos_PreferredUI):
				ForceApply();
				foreach (var settings in _perPlayerSettings)
				{
					settings._toggle.Value = true;
					{
						settings._zoomControlAmount.Value = 0;
						settings._zoomControlSpeed.Value = 0.25f;
						settings._gamepadInputs.Value = GamepadInputs.LeftQS | GamepadInputs.RightQS;
						settings._offsetToggle.Value = true;
						{
							settings._offsetMin.Value = new Vector3(0, 4, -12);
							settings._offsetAvg.Value = new Vector3(0, 1, -4);
							settings._offsetMax.Value = new Vector3(0.75f, 0.75f, -2);
						}
						settings._variousToggle.Value = true;
						{
							settings._variousMin.Value = new Vector3(60, 2, 1);
							settings._variousAvg.Value = new Vector3(50, 6, 1);
							settings._variousMax.Value = new Vector3(25, 18, 0.4f);
						}
						settings._rotateWhenMovingWithGamepad.Value = false;
						settings._overrideInvertX.Value = AxisOverride.Normal;
						settings._overrideInvertY.Value = AxisOverride.Inverted;
					}
				}
				break;
		}
	}
	public void OnUpdate()
	{
		foreach (var player in Players.Local)
		{
			if (player.UI.IsMenuFocused)
				continue;

			PerPlayerSettings settings = _perPlayerSettings[player.ID];
			settings.IgnoreAxes = false;
			if (settings._zoomControlSpeed > 0)
			{
				float zoomDelta = 0f;
				if (player.IsUsingGamepad && CheckGamepadHotkey(player))
				{
					Vector2 cameraInput = player.CameraMovementInput;
					if (cameraInput.y.Abs() > cameraInput.x.Abs())
						zoomDelta = cameraInput.y;
					settings.IgnoreAxes = true;
				}
				else if (!player.IsUsingGamepad)
					zoomDelta = Input.mouseScrollDelta.y * 2f;

				if (zoomDelta != 0)
					settings._zoomControlAmount.Value += zoomDelta * settings._zoomControlSpeed * 10f * Time.unscaledDeltaTime;
			}
		}
	}

	// Utility            
	private static void UpdateCameraSettings(Players.Data player)
	{
		#region quit
		if (!_perPlayerSettings[player.ID]._toggle)
			return;
		#endregion

		// Cache
		Vector3 currentVarious = _perPlayerSettings[player.ID].CurrentVarious;
		CharacterCamera characterCamera = player.Character.CharacterCamera;

		// Overrides
		if (characterCamera.InZoomMode)
			characterCamera.ZoomOffsetSplitScreenH = _perPlayerSettings[player.ID].CurrentOffset;
		else
			characterCamera.Offset = _perPlayerSettings[player.ID].CurrentOffset;
		characterCamera.CameraScript.fieldOfView = currentVarious.x;
		characterCamera.DefaultFollowSpeed.x = currentVarious.y;
		_perPlayerSettings[player.ID].Sensitivity = currentVarious.z;
	}
	private static bool CheckGamepadHotkey(Players.Data player)
	{
		GamepadInputs inputs = _perPlayerSettings[player.ID]._gamepadInputs;
		return inputs != GamepadInputs.None
			&& (!inputs.HasFlag(GamepadInputs.LeftQS) || player.Held("QuickSlotToggle1"))
			&& (!inputs.HasFlag(GamepadInputs.RightQS) || player.Held("QuickSlotToggle2"))
			&& (!inputs.HasFlag(GamepadInputs.Sprint) || player.Held(ControlsInput.GameplayActions.Sprint))
			&& (!inputs.HasFlag(GamepadInputs.Block) || player.Held(ControlsInput.GameplayActions.Block));
	}
	private static bool _isInCharacterCameraUpdate;

	// Hooks
	[HarmonyPostfix, HarmonyPatch(typeof(SplitScreenManager), nameof(SplitScreenManager.DelayedRefreshSplitScreen))]
	private static void SplitScreenManager_DelayedRefreshSplitScreen_Post()
	{
		foreach (var player in Players.Local)
		{
			#region quit
			if (!_perPlayerSettings[player.ID]._toggle)
				continue;
			#endregion
			UpdateCameraSettings(player);
		}
	}

	[HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.SetZoomMode))]
	private static void Character_SetZoomMode_Post(Character __instance, ref bool _zoomed)
	{
		Players.Data player = Players.GetLocal(__instance);
		#region quit
		if (!_perPlayerSettings[player.ID]._toggle)
			return;
		#endregion

		__instance.CharacterCamera.ZoomSensModifier = 1f;
		_perPlayerSettings[player.ID].StartAimCoroutine(__instance, _zoomed);
	}

	[HarmonyPrefix, HarmonyPatch(typeof(CharacterCamera), nameof(CharacterCamera.UpdateZoom))]
	private static bool CharacterCamera_UpdateZoom_Pre(CharacterCamera __instance)
	=> !Players.TryGetLocal(__instance, out Players.Data player) || !_perPlayerSettings[player.ID]._toggle;

	[HarmonyPostfix, HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.RotateCameraVertical))]
	private static void ControlsInput_RotateCameraVertical_Post(int _playerID, ref float __result)
	{
		#region quit
		if (!_perPlayerSettings[_playerID]._toggle)
			return;
		#endregion
		__result *= _perPlayerSettings[_playerID].Sensitivity;
	}

	[HarmonyPostfix, HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.RotateCameraHorizontal))]
	private static void ControlsInput_RotateCameraHorizontal_Post(int _playerID, ref float __result)
	{
		#region quit
		if (!_perPlayerSettings[_playerID]._toggle)
			return;
		#endregion
		__result *= _perPlayerSettings[_playerID].Sensitivity;
	}

	[HarmonyPrefix, HarmonyPatch(typeof(CharacterCamera), nameof(CharacterCamera.Update))]
	private static void CharacterCamera_Update_Pre()
	{
		_isInCharacterCameraUpdate = true;


	}

	[HarmonyPostfix, HarmonyPatch(typeof(CharacterCamera), nameof(CharacterCamera.Update))]
	private static void CharacterCamera_Update_Post()
	=> _isInCharacterCameraUpdate = false;

	[HarmonyPostfix, HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.IsLastActionGamepad))]
	private static void ControlsInput_IsLastActionGamepad_Post(int _playerID, ref bool __result)
	{
		if (_isInCharacterCameraUpdate
		&& _perPlayerSettings[_playerID]._toggle
		&& !_perPlayerSettings[_playerID]._rotateWhenMovingWithGamepad)
			__result = false;
	}

	[HarmonyPostfix, HarmonyPatch(typeof(CharacterCamera), nameof(CharacterCamera.SetTargetCharacter))]
	private static void CharacterCamera_SetTargetCharacter_Post(CharacterCamera __instance, Character _character)
	{
		var pps = _perPlayerSettings[_character.OwnerPlayerSys.PlayerID];
		if (!pps._toggle)
			return;

		if (pps._overrideInvertX == AxisOverride.Normal)
			__instance.m_invertedHor = false;
		else if (pps._overrideInvertX == AxisOverride.Inverted)
			__instance.m_invertedHor = true;

		if (pps._overrideInvertY == AxisOverride.Normal)
			__instance.m_invertedVer = false;
		else if (pps._overrideInvertY == AxisOverride.Inverted)
			__instance.m_invertedVer = true;
	}
}
