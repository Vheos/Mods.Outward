namespace Vheos.Mods.Outward;

public static class GameInput
{
    #region Constants
    public static float HOLD_DURATION = 1.25f;   // InteractionBase.HOLD_ACTIVATION_TIME
    public static float HOLD_THRESHOLD = 0.4f;   // Character.BASIC_INTERACT_THRESHOLD
    #endregion

    // Publics    
    public static bool Pressed(int playerID, string actionName)
    => ControlsInput.m_playerInputManager[playerID].GetButtonDown(actionName);
    public static bool Released(int playerID, string actionName)
    => ControlsInput.m_playerInputManager[playerID].GetButtonUp(actionName);
    public static bool Held(int playerID, string actionName)
    => ControlsInput.m_playerInputManager[playerID].GetButton(actionName);
    public static float AxisValue(int playerID, string axisnName)
    => ControlsInput.m_playerInputManager[playerID].GetAxis(axisnName);
    public static bool IsUsingGamepad(int playerID)
    => ControlsInput.IsLastActionGamepad(playerID);
    public static KeyCode ToKeyCode(string text)
    {
        if (text.IsNotEmpty())
            if (_keyCodesByName.ContainsKey(text))
                return _keyCodesByName[text];
        return KeyCode.None;
    }
    public static bool ForceCursorNavigation;

    // Shortcuts
    public static bool Pressed(int playerID, ControlsInput.GameplayActions gameplayAction)
    => Pressed(playerID, gameplayAction.ToName());
    public static bool Released(int playerID, ControlsInput.GameplayActions gameplayAction)
    => Released(playerID, gameplayAction.ToName());
    public static bool Held(int playerID, ControlsInput.GameplayActions gameplayAction)
    => Held(playerID, gameplayAction.ToName());
    public static float AxisValue(int playerID, ControlsInput.GameplayActions gameplayAxis)
    => AxisValue(playerID, gameplayAxis.ToName());
    public static bool Pressed(int playerID, ControlsInput.MenuActions menuAction)
    => Pressed(playerID, menuAction.ToName());
    public static bool Released(int playerID, ControlsInput.MenuActions menuAction)
    => Released(playerID, menuAction.ToName());
    public static bool Held(int playerID, ControlsInput.MenuActions menuAction)
    => Held(playerID, menuAction.ToName());
    public static float AxisValue(int playerID, ControlsInput.MenuActions menuAxis)
    => AxisValue(playerID, menuAxis.ToName());

    // Privates
    private static Dictionary<string, KeyCode> _keyCodesByName;

    // Initializers
    public static void Initialize()
    {
        _keyCodesByName = new Dictionary<string, KeyCode>();
        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            string keyName = keyCode.ToString();
            if (!keyName.Contains("Joystick") && !_keyCodesByName.ContainsKey(keyName))
                _keyCodesByName.Add(keyName, keyCode);
        }

        Harmony.CreateAndPatchAll(typeof(GameInput));
    }

    // Hooks
#pragma warning disable IDE0051
    [HarmonyPrefix, HarmonyPatch(typeof(CharacterUI), nameof(CharacterUI.IsMenuFocused), MethodType.Getter)]
    private static bool CharacterUI_IsMenuFocused_Getter_Pre(ref bool __result)
    {
        #region quit
        if (!ForceCursorNavigation)
            return true;
        #endregion

        __result = true;
        return false;
    }
}
