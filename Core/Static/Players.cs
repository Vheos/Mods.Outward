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

            // Properties
            public bool IsUsingGamepad
            => GameInput.IsUsingGamepad(ID);
        }
        #endregion

        // Publics        
        static public List<Data> Local
        { get; private set; }
        static public Data GetLocal(int playerID)
        => Local.DefaultOnInvalid(playerID);

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