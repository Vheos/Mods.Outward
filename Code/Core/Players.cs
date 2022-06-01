namespace Vheos.Mods.Outward;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Tools.Extensions.Collections;

static public class Players
{
    #region class
    public class Data
    {
        // Fields
        public SplitPlayer Split;
        public Character Character;
        public PlayerCharacterStats Stats;
        public CharacterCamera Camera;
        public CharacterUI UI;
        public PlayerSystem System;
        public int ID;
        public string UID;

        // Input
        public bool IsUsingGamepad
        => GameInput.IsUsingGamepad(ID);
        public bool Pressed(string actionName)
        => GameInput.Pressed(ID, actionName);
        public bool Released(string actionName)
        => GameInput.Released(ID, actionName);
        public bool Held(string actionName)
        => GameInput.Held(ID, actionName);
        public float AxisValue(string axisName)
        => GameInput.AxisValue(ID, axisName);
        public bool Pressed(ControlsInput.GameplayActions gameplayAction)
        => GameInput.Pressed(ID, gameplayAction);
        public bool Released(ControlsInput.GameplayActions gameplayAction)
        => GameInput.Released(ID, gameplayAction);
        public bool Held(ControlsInput.GameplayActions gameplayAction)
        => GameInput.Held(ID, gameplayAction);
        public float AxisValue(ControlsInput.GameplayActions gameplayAxis)
        => GameInput.AxisValue(ID, gameplayAxis);
        public bool Pressed(ControlsInput.MenuActions menuAction)
        => GameInput.Pressed(ID, menuAction);
        public bool Released(ControlsInput.MenuActions menuAction)
        => GameInput.Released(ID, menuAction);
        public bool Held(ControlsInput.MenuActions menuAction)
        => GameInput.Held(ID, menuAction);
        public float AxisValue(ControlsInput.MenuActions menuAxis)
        => GameInput.AxisValue(ID, menuAxis);

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
    static public Data GetLocal(LocalCharacterControl localCharacterControl)
    => GetLocal(GetPlayerID(localCharacterControl));
    static public Data GetLocal(UIElement uiElement)
    => GetLocal(GetPlayerID(uiElement));
    static public Data GetLocal(Character character)
    => GetLocal(GetPlayerID(character));
    static public Data GetLocal(CharacterUI characterUI)
    => GetLocal(GetPlayerID(characterUI));
    static public Data GetLocal(CharacterCamera characterCamera)
    => GetLocal(GetPlayerID(characterCamera));
    static public bool TryGetLocal(int playerID, out Data player)
    {
        player = GetLocal(playerID);
        return player != null;
    }
    static public bool TryGetLocal(LocalCharacterControl localCharacterControl, out Data player)
    => TryGetLocal(GetPlayerID(localCharacterControl), out player);
    static public bool TryGetLocal(UIElement uiElement, out Data player)
     => TryGetLocal(GetPlayerID(uiElement), out player);
    static public bool TryGetLocal(Character character, out Data player)
    => TryGetLocal(GetPlayerID(character), out player);
    static public bool TryGetLocal(CharacterUI characterUI, out Data player)
    => TryGetLocal(GetPlayerID(characterUI), out player);
    static public bool TryGetLocal(CharacterCamera characterCamera, out Data player)
    => TryGetLocal(GetPlayerID(characterCamera), out player);

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
                Stats = character.PlayerStats,
                Camera = character.CharacterCamera,
                UI = character.CharacterUI,
                System = playerSystem,
                ID = playerSystem.PlayerID,
                UID = playerSystem.UID,
            });
        }
    }
    static private int GetPlayerID(LocalCharacterControl localCharacterControl)
    => localCharacterControl.Character.OwnerPlayerSys.PlayerID;
    static private int GetPlayerID(UIElement uiElement)
    => uiElement.LocalCharacter.OwnerPlayerSys.PlayerID;
    static private int GetPlayerID(Character character)
    => character.OwnerPlayerSys.PlayerID;
    static private int GetPlayerID(CharacterUI characterUI)
    => characterUI.TargetCharacter.OwnerPlayerSys.PlayerID;
    static private int GetPlayerID(CharacterCamera characterCamera)
    => characterCamera.TargetCharacter.OwnerPlayerSys.PlayerID;

    // Initializers
    static public void Initialize()
    {
        Local = new List<Data>();
        Harmony.CreateAndPatchAll(typeof(Players));
    }

    // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006
    [HarmonyPatch(typeof(LocalCharacterControl), nameof(LocalCharacterControl.RetrieveComponents)), HarmonyPostfix]
    static void LocalCharacterControl_RetrieveComponents_Post()
    => Recache();

    [HarmonyPatch(typeof(RPCManager), nameof(RPCManager.SendPlayerHasLeft)), HarmonyPostfix]
    static void RPCManager_SendPlayerHasLeft_Post()
    => Recache();
}

