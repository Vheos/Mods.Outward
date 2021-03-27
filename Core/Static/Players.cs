using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;



namespace ModPack
{
    static public class Players
    {
        #region class
        public class Data
        {
            // Fields
            public SplitPlayer Split;
            public Character Character;
            public CharacterCamera Camera;
            public CharacterUI UI;
            public PlayerSystem System;
            public int ID;
            public string UID;

            // Input
            public bool IsUsingGamepad
            => GameInput.IsUsingGamepad(ID);
            public bool Pressed(ControlsInput.GameplayActions action)
            => GameInput.Pressed(ID, action);
            public bool Released(ControlsInput.GameplayActions action)
            => GameInput.Released(ID, action);
            public bool Held(ControlsInput.GameplayActions action)
            => GameInput.Held(ID, action);
            public float AxisValue(ControlsInput.GameplayActions action)
            => GameInput.AxisValue(ID, action);
            public bool Pressed(ControlsInput.MenuActions action)
            => GameInput.Pressed(ID, action);
            public bool Released(ControlsInput.MenuActions action)
            => GameInput.Released(ID, action);
            public bool Held(ControlsInput.MenuActions action)
            => GameInput.Held(ID, action);
            public float AxisValue(ControlsInput.MenuActions action)
            => GameInput.AxisValue(ID, action);

            // Shortcuts
            public Vector2 CameraMovementInput
            => new Vector2(GameInput.AxisValue(ID, ControlsInput.GameplayActions.RotateCameraHorizontal),
                           GameInput.AxisValue(ID, ControlsInput.GameplayActions.RotateCameraVertical));
            public Vector2 PlayerMovementInput
            => new Vector2(GameInput.AxisValue(ID, ControlsInput.GameplayActions.MoveHorizontal),
                           GameInput.AxisValue(ID, ControlsInput.GameplayActions.MoveVertical));
        }
        #endregion

        // Publics        
        static public List<Data> Local
        { get; private set; }
        static public Data GetLocal(int playerID)
        => Local.DefaultOnInvalid(playerID);
        static public bool TryGetLocal(int playerID, out Data player)
        {
            player = GetLocal(playerID);
            return player != null;
        }
        static public bool LocalExists(int playerID)
        => playerID < Local.Count;

        // Privates
        static private void Recache()
        {
            Local.Clear();
            foreach (var splitPlayer in SplitScreenManager.Instance.LocalPlayers)
            {
                Character character = splitPlayer.AssignedCharacter;
                PlayerSystem playerSystem = character.OwnerPlayerSys;
                Local.Add(new Data()
                {
                    Split = splitPlayer,
                    Character = character,
                    Camera = character.CharacterCamera,
                    UI = character.CharacterUI,
                    System = playerSystem,
                    ID = playerSystem.PlayerID,
                    UID = playerSystem.UID,
                });
            }
        }

        // Initializers
        static public void Initialize()
        {
            Local = new List<Data>();
            Harmony.CreateAndPatchAll(typeof(Players));
        }

        // Hooks
        [HarmonyPatch(typeof(LocalCharacterControl), "RetrieveComponents"), HarmonyPostfix]
        static void LocalCharacterControl_RetrieveComponents_Post()
        => Recache();

        [HarmonyPatch(typeof(RPCManager), "SendPlayerHasLeft"), HarmonyPostfix]
        static void RPCManager_SendPlayerHasLeft_Post()
        => Recache();
    }
}