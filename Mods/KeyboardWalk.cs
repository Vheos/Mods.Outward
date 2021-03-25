using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;



namespace ModPack
{
    public class KeyboardWalk : AMod, IUpdatable
    {
        // Setting
        static private ModSetting<int> _walkSpeed;
        static private ModSetting<string> _key;
        static private ModSetting<bool> _doubleTapToToggle;
        static private ModSetting<int> _doubleTapWaitTime;
        override protected void Initialize()
        {
            _key = CreateSetting(nameof(_key), "LeftAlt");
            _walkSpeed = CreateSetting(nameof(_walkSpeed), 35, IntRange(0, 100));
            _doubleTapToToggle = CreateSetting(nameof(_doubleTapToToggle), true);
            _doubleTapWaitTime = CreateSetting(nameof(_doubleTapWaitTime), 500, IntRange(0, 1000));

            _modifier = 1f;
            _lastKeyPressTime = float.NegativeInfinity;
        }
        override protected void SetFormatting()
        {
            _key.Format("Key");
            _key.Description = "Use UnityEngine.KeyCode enum values\n" +
                               "(https://docs.unity3d.com/ScriptReference/KeyCode.html)";
            _walkSpeed.Format("Speed");
            _walkSpeed.Description = "% of current movement speed when walking";
            _doubleTapToToggle.Format("Double-tap to toggle");
            _doubleTapToToggle.Description = "Toggle default movement mode (between running and walking) by double-tapping the chosen key";
            Indent++;
            {
                _doubleTapWaitTime.Format("Wait time", _doubleTapToToggle);
                _doubleTapWaitTime.Description = "Max interval between two key presses (in milliseconds)";
                Indent--;
            }
        }
        override protected string Description
        => "• Allows keyboard players to walk\n" +
           "(can be held or toggled)";
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
        static private float _modifier;
        private bool _reverseMode;
        static private bool _isHorizontalInput;
        static private bool _isVerticalInput;
        private float _lastKeyPressTime;
        private float NormalSpeed
        => _reverseMode ? _walkSpeed / 100f : 1f;
        private float ModifiedSpeed
        => _reverseMode ? 1f : _walkSpeed / 100f;
        private float TimeSinceLastKeyPress
        => Time.unscaledTime - _lastKeyPressTime;

        // Hooks
        [HarmonyPatch(typeof(ControlsInput), "MoveHorizontal"), HarmonyPostfix]
        static void ControlsInput_MoveHorizontal_Post(ref float __result, ref int _playerID)
        {
            if (GameInput.IsUsingKeyboard(_playerID))
            {
                __result *= _modifier;
                _isHorizontalInput = __result != 0;
                if (_isVerticalInput)
                    __result /= 2f.Sqrt();
            }
        }

        [HarmonyPatch(typeof(ControlsInput), "MoveVertical"), HarmonyPostfix]
        static void ControlsInput_MoveVertical_Post(ref float __result, ref int _playerID)
        {
            if (GameInput.IsUsingKeyboard(_playerID))
            {
                __result *= _modifier;
                _isVerticalInput = __result != 0;
                if (_isHorizontalInput)
                    __result /= 2f.Sqrt();
            }
        }
    }
}