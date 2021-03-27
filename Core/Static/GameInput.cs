using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;



namespace ModPack
{
    static public class GameInput
    {
        #region const
        static public float HOLD_DURATION = 1.25f;   // InteractionBase.HOLD_ACTIVATION_TIME
        static public float HOLD_THRESHOLD = 0.4f;   // Character.BASIC_INTERACT_THRESHOLD
        #endregion

        // Publics        
        static public bool Pressed(int playerID, ControlsInput.GameplayActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButtonDown(action.ToName());
        static public bool Released(int playerID, ControlsInput.GameplayActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButtonUp(action.ToName());
        static public bool Held(int playerID, ControlsInput.GameplayActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButton(action.ToName());
        static public float AxisValue(int playerID, ControlsInput.GameplayActions action)
        => ControlsInput.m_playerInputManager[playerID].GetAxis(action.ToName());
        static public bool Pressed(int playerID, ControlsInput.MenuActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButtonDown(action.ToName());
        static public bool Released(int playerID, ControlsInput.MenuActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButtonUp(action.ToName());
        static public bool Held(int playerID, ControlsInput.MenuActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButton(action.ToName());
        static public float AxisValue(int playerID, ControlsInput.MenuActions action)
        => ControlsInput.m_playerInputManager[playerID].GetAxis(action.ToName());
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
        [HarmonyPatch(typeof(CharacterUI), "IsMenuFocused", MethodType.Getter), HarmonyPrefix]
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
}