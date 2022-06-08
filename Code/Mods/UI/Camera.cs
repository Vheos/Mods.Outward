namespace Vheos.Mods.Outward;

public class Camera : AMod, IDelayedInit, IUpdatable
{
    #region const
    private static readonly Vector3 DEFAULT_OFFSET = new(0f, 1f, -3f);
    private const float DEFAULT_FOV = 50f;
    private const float DEFAULT_FOLLOW_SPEED = 4.5f;
    private const float AIM_COROUTINE_UPDATE_SPEED = 0.2f;
    private const float AIM_COROUTINE_MARGIN = 0.01f;
    #endregion
    #region enum
    [Flags]
    private enum GamepadInputs
    {
        None = 0,

        LeftQS = 1 << 1,
        RightQS = 1 << 2,
        Sprint = 1 << 3,
        Block = 1 << 4,
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
            PerPlayerSettings tmp = new();
            _perPlayerSettings[i] = tmp;
            string playerPrefix = $"player{i + 1}";

            tmp._toggle = CreateSetting(playerPrefix + nameof(tmp._toggle), false);
            tmp._zoomControlAmount = CreateSetting(playerPrefix + nameof(tmp._zoomControlAmount), 0f, FloatRange(-1f, 1f));
            tmp._zoomControlSpeed = CreateSetting(playerPrefix + nameof(tmp._zoomControlSpeed), 0.5f, FloatRange(0f, 1f));
            tmp._gamepadInputs = CreateSetting(playerPrefix + nameof(tmp._gamepadInputs), GamepadInputs.LeftQS | GamepadInputs.RightQS);

            tmp._offsetToggle = CreateSetting(playerPrefix + nameof(tmp._offsetToggle), false);
            tmp._offsetMin = CreateSetting(playerPrefix + nameof(tmp._offsetMin), DEFAULT_OFFSET.Add(0, 1f, -3f));
            tmp._offsetAvg = CreateSetting(playerPrefix + nameof(tmp._offsetAvg), DEFAULT_OFFSET);
            tmp._offsetMax = CreateSetting(playerPrefix + nameof(tmp._offsetMax), DEFAULT_OFFSET.Add(0.75f, -0.25f, 1f));

            Vector3 otherDefault = new(DEFAULT_FOV, DEFAULT_FOLLOW_SPEED, 1f);
            tmp._variousToggle = CreateSetting(playerPrefix + nameof(tmp._variousToggle), false);
            tmp._variousMin = CreateSetting(playerPrefix + nameof(tmp._variousMin), otherDefault.Mul(1.2f, 2 / 3f, 1f));
            tmp._variousAvg = CreateSetting(playerPrefix + nameof(tmp._variousAvg), otherDefault);
            tmp._variousMax = CreateSetting(playerPrefix + nameof(tmp._variousMax), otherDefault.Mul(0.8f, 2.0f, 0.75f));

            int id = i;
            tmp._zoomControlAmount.AddEvent(() =>
            {
                if (Players.TryGetLocal(id, out Players.Data player))
                    UpdateCameraSettings(player);
            });

            AddEventOnConfigClosed(() =>
            {
                foreach (var player in Players.Local)
                    UpdateCameraSettings(player);
            });

            tmp.Sensitivity = 1f;
        }
    }
    protected override void SetFormatting()
    {
        for (int i = 0; i < 2; i++)
        {
            PerPlayerSettings tmp = _perPlayerSettings[i];

            // Settings
            tmp._toggle.Format($"Player {i + 1}");
            tmp._toggle.Description = $"Change settings for local player {i + 1}";
            using (Indent)
            {
                tmp._zoomControlAmount.Format("Zoom amount", tmp._toggle);
                tmp._zoomControlAmount.Description = "-1  -  max zoom out\n" +
                                                    "0  -  default zoom\n" +
                                                    "+1  -  max zoom in";
                tmp._zoomControlSpeed.Format("Zoom control speed", tmp._toggle);
                tmp._zoomControlSpeed.Description = "How quickly you want to zoom using mouse/gamepad\n" +
                    "Mouse: use mouse scroll wheel\n" +
                    "Gamepad: use right stick while holding defined actions";
                using (Indent)
                {
                    tmp._gamepadInputs.Format("Gamepad hotkey", tmp._zoomControlSpeed, t => t > 0);
                    tmp._gamepadInputs.Description = "Gamepad actions you need to hold to control camera. Defaults:\n" +
                                                     "LeftQS = LT\n" +
                                                     "RightQS = RT\n" +
                                                     "Sprint = LB\n" +
                                                     "Block = RB";
                }
                tmp._offsetToggle.Format("Offset", tmp._toggle);
                tmp._offsetToggle.Description = "Change camera position (XYZ) presets";
                using (Indent)
                {
                    tmp._offsetMin.Format("at zoom =  -1", tmp._offsetToggle);
                    tmp._offsetAvg.Format("at zoom =  0", tmp._offsetToggle);
                    tmp._offsetMax.Format("at zoom = +1", tmp._offsetToggle);
                }

                tmp._variousToggle.Format("FOV, FollowSpeed, Sensitivity", tmp._toggle);
                tmp._variousToggle.Description = "Change other settings presets:\n" +
                    "X  -  field of view (vertical)\n" +
                    "Y  -  how quickly camera follows your character\n" +
                    "Z  -  how quickly camera rotates";
                using (Indent)
                {
                    tmp._variousMin.Format("at zoom =  -1", tmp._variousToggle);
                    tmp._variousAvg.Format("at zoom =  0", tmp._variousToggle);
                    tmp._variousMax.Format("at zoom = +1", tmp._variousToggle);
                }
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
}
