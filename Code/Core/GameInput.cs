namespace Vheos.Mods.Outward;

static public class GameInput
{
    #region const
    static public float HOLD_DURATION = 1.25f;   // InteractionBase.HOLD_ACTIVATION_TIME
    static public float HOLD_THRESHOLD = 0.4f;   // Character.BASIC_INTERACT_THRESHOLD
    #endregion

    // Publics    
    static public bool Pressed(int playerID, string actionName)
    => ControlsInput.m_playerInputManager[playerID].GetButtonDown(actionName);
    static public bool Released(int playerID, string actionName)
    => ControlsInput.m_playerInputManager[playerID].GetButtonUp(actionName);
    static public bool Held(int playerID, string actionName)
    => ControlsInput.m_playerInputManager[playerID].GetButton(actionName);
    static public float AxisValue(int playerID, string axisnName)
    => ControlsInput.m_playerInputManager[playerID].GetAxis(axisnName);
    static public bool IsUsingGamepad(int playerID)
    => ControlsInput.IsLastActionGamepad(playerID);
    static public KeyCode ToKeyCode(string text)
    {
        if (text.IsNotEmpty())
            if (_keyCodesByName.ContainsKey(text))
                return _keyCodesByName[text];
        return KeyCode.None;
    }
    static public bool ForceCursorNavigation;

    // Shortcuts
    static public bool Pressed(int playerID, ControlsInput.GameplayActions gameplayAction)
    => Pressed(playerID, gameplayAction.ToName());
    static public bool Released(int playerID, ControlsInput.GameplayActions gameplayAction)
    => Released(playerID, gameplayAction.ToName());
    static public bool Held(int playerID, ControlsInput.GameplayActions gameplayAction)
    => Held(playerID, gameplayAction.ToName());
    static public float AxisValue(int playerID, ControlsInput.GameplayActions gameplayAxis)
    => AxisValue(playerID, gameplayAxis.ToName());
    static public bool Pressed(int playerID, ControlsInput.MenuActions menuAction)
    => Pressed(playerID, menuAction.ToName());
    static public bool Released(int playerID, ControlsInput.MenuActions menuAction)
    => Released(playerID, menuAction.ToName());
    static public bool Held(int playerID, ControlsInput.MenuActions menuAction)
    => Held(playerID, menuAction.ToName());
    static public float AxisValue(int playerID, ControlsInput.MenuActions menuAxis)
    => AxisValue(playerID, menuAxis.ToName());

    // Privates
    static private Dictionary<string, KeyCode> _keyCodesByName;

    // Initializers
    static public void Initialize()
    {
        _keyCodesByName = new Dictionary<string, KeyCode>();
        foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
        {
            string keyName = keyCode.ToString();
            if (!keyName.Contains("Joystick") && !_keyCodesByName.ContainsKey(keyName))
                _keyCodesByName.Add(keyName, keyCode);
        }

        Harmony.CreateAndPatchAll(typeof(GameInput));
    }

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006
    [HarmonyPatch(typeof(CharacterUI), nameof(CharacterUI.IsMenuFocused), MethodType.Getter), HarmonyPrefix]
    static bool CharacterUI_IsMenuFocused_Getter_Pre(ref bool __result)
    {
        #region quit
        if (!ForceCursorNavigation)
            return true;
        #endregion

        __result = true;
        return false;
    }
}
