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
        static public bool HasBeenPressed(int playerID, ControlsInput.GameplayActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButtonDown(action.ToName());
        static public bool HasBeenReleased(int playerID, ControlsInput.GameplayActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButtonUp(action.ToName());
        static public bool IsHeldDown(int playerID, ControlsInput.GameplayActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButton(action.ToName());
        static public float AxisValue(int playerID, ControlsInput.GameplayActions action)
        => ControlsInput.m_playerInputManager[playerID].GetAxis(action.ToName());
        static public bool HasBeenPressed(int playerID, ControlsInput.MenuActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButtonDown(action.ToName());
        static public bool HasBeenReleased(int playerID, ControlsInput.MenuActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButtonUp(action.ToName());
        static public bool IsHeldDown(int playerID, ControlsInput.MenuActions action)
        => ControlsInput.m_playerInputManager[playerID].GetButton(action.ToName());
        static public float AxisValue(int playerID, ControlsInput.MenuActions action)
        => ControlsInput.m_playerInputManager[playerID].GetAxis(action.ToName());
        static public bool IsUsingKeyboard(int playerID)
        => !ControlsInput.IsLastActionGamepad(playerID);
        static public KeyCode ToKeyCode(string text)
        {
            if (!text.IsEmpty())
                if (_keyCodesByName.ContainsKey(text))
                    return _keyCodesByName[text];
            return KeyCode.None;
        }
        static public List<PlayerSystem> LocalPlayers
        { get; private set; }

        // Shortcuts
        static public Vector2 CameraMovementInput(int playerID)
        => new Vector2(AxisValue(playerID, ControlsInput.GameplayActions.RotateCameraHorizontal),
                       AxisValue(playerID, ControlsInput.GameplayActions.RotateCameraVertical));
        static public Vector2 PlayerMovementInput(int playerID)
        => new Vector2(AxisValue(playerID, ControlsInput.GameplayActions.MoveHorizontal),
                       AxisValue(playerID, ControlsInput.GameplayActions.MoveVertical));
        static public bool IsSprinting(int playerID)
        => IsHeldDown(playerID, ControlsInput.GameplayActions.Sprint);
        static public bool IsBlocking(int playerID)
        => IsHeldDown(playerID, ControlsInput.GameplayActions.Block);
        static public int KeyboardUserID
        {
            get
            {
                for (int i = 0; i < Global.Lobby.LocalPlayerCount; i++)
                    if (IsUsingKeyboard(i))
                        return i;
                return -1;
            }
        }

        // Privates
        static private Dictionary<string, KeyCode> _keyCodesByName;
        static private void RecacheLocalPlayers(string[] localPlayersUIDs)
        {
            LocalPlayers.Clear();
            foreach (var localPlayerUID in localPlayersUIDs)
                LocalPlayers.Add(Global.Lobby.GetPlayerSystem(localPlayerUID));
        }

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

            LocalPlayers = new List<PlayerSystem>();

            Harmony.CreateAndPatchAll(typeof(GameInput));
        }

        // Hooks
        [HarmonyPatch(typeof(LobbySystem), "AssignUID"), HarmonyPostfix]
        static void LobbySystem_AssignUID_Post(ref LobbySystem __instance)
        => RecacheLocalPlayers(__instance.m_cachedLocalPlayerUIDsArray);

        [HarmonyPatch(typeof(LobbySystem), "ReleaseUID"), HarmonyPostfix]
        static void LobbySystem_ReleaseUID_Post(ref LobbySystem __instance)
        => RecacheLocalPlayers(__instance.m_cachedLocalPlayerUIDsArray);
    }
}