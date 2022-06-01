namespace Vheos.Mods.Outward;

public class KeyboardWalk : AMod, IUpdatable
{
    // Setting
    private static ModSetting<int> _walkSpeed;
    private static ModSetting<string> _key;
    private static ModSetting<bool> _doubleTapToToggle;
    private static ModSetting<int> _doubleTapWaitTime;
    protected override void Initialize()
    {
        _key = CreateSetting(nameof(_key), "LeftAlt");
        _walkSpeed = CreateSetting(nameof(_walkSpeed), 35, IntRange(0, 100));
        _doubleTapToToggle = CreateSetting(nameof(_doubleTapToToggle), true);
        _doubleTapWaitTime = CreateSetting(nameof(_doubleTapWaitTime), 500, IntRange(0, 1000));

        _modifier = 1f;
        _lastKeyPressTime = float.NegativeInfinity;
    }
    protected override void SetFormatting()
    {
        _key.Format("Key");
        _key.Description = "Use UnityEngine.KeyCode enum values\n" +
                           "(https://docs.unity3d.com/ScriptReference/KeyCode.html)";
        _walkSpeed.Format("Speed");
        _walkSpeed.Description = "% of current movement speed when walking";
        _doubleTapToToggle.Format("Double-tap to toggle");
        _doubleTapToToggle.Description = "Toggle default movement mode (between running and walking) by double-tapping the chosen key";
        using (Indent)
        {
            _doubleTapWaitTime.Format("Wait time", _doubleTapToToggle);
            _doubleTapWaitTime.Description = "Max interval between two key presses (in milliseconds)";
        }
    }
    protected override string Description
    => "• Allows keyboard players to walk\n" +
       "(can be held or toggled)";
    protected override string SectionOverride
    => ModSections.UI;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_PreferredUI):
                IsHidden = true;
                break;
        }
    }
    public void OnUpdate()
    {
        if (_key.Value.ToKeyCode().Pressed())
        {
            _modifier = ModifiedSpeed;
            if (_doubleTapToToggle && TimeSinceLastKeyPress < _doubleTapWaitTime / 1000f)
                _reverseMode = !_reverseMode;
            else
                _lastKeyPressTime = Time.unscaledTime;
        }
        else if (_key.Value.ToKeyCode().Released())
            _modifier = NormalSpeed;
    }

    // Utility
    private static float _modifier;
    private bool _reverseMode;
    private static bool _isHorizontalInput;
    private static bool _isVerticalInput;
    private float _lastKeyPressTime;
    private float NormalSpeed
    => _reverseMode ? _walkSpeed / 100f : 1f;
    private float ModifiedSpeed
    => _reverseMode ? 1f : _walkSpeed / 100f;
    private float TimeSinceLastKeyPress
    => Time.unscaledTime - _lastKeyPressTime;

    // Hooks
    [HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.MoveHorizontal)), HarmonyPostfix]
    private static void ControlsInput_MoveHorizontal_Post(ref float __result, ref int _playerID)
    {
        if (!GameInput.IsUsingGamepad(_playerID))
        {
            __result *= _modifier;
            _isHorizontalInput = __result != 0;
            if (_isVerticalInput)
                __result /= 2f.Sqrt();
        }
    }

    [HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.MoveVertical)), HarmonyPostfix]
    private static void ControlsInput_MoveVertical_Post(ref float __result, ref int _playerID)
    {
        if (!GameInput.IsUsingGamepad(_playerID))
        {
            __result *= _modifier;
            _isVerticalInput = __result != 0;
            if (_isHorizontalInput)
                __result /= 2f.Sqrt();
        }
    }
}
