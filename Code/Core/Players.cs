namespace Vheos.Mods.Outward;

public static class Players
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
        => new(GameInput.AxisValue(ID, ControlsInput.GameplayActions.RotateCameraHorizontal),
                       GameInput.AxisValue(ID, ControlsInput.GameplayActions.RotateCameraVertical));
        public Vector2 PlayerMovementInput
        => new(GameInput.AxisValue(ID, ControlsInput.GameplayActions.MoveHorizontal),
                       GameInput.AxisValue(ID, ControlsInput.GameplayActions.MoveVertical));
    }
    #endregion

    // Publics        
    public static List<Data> Local
    { get; private set; }
    public static Data GetFirst()
    => Local.FirstOrDefault();
    public static Data GetLocal(int playerID)
    => Local.DefaultOnInvalid(playerID);
    public static Data GetLocal(LocalCharacterControl localCharacterControl)
    => GetLocal(GetPlayerID(localCharacterControl));
    public static Data GetLocal(UIElement uiElement)
    => GetLocal(GetPlayerID(uiElement));
    public static Data GetLocal(Character character)
    => GetLocal(GetPlayerID(character));
    public static Data GetLocal(CharacterUI characterUI)
    => GetLocal(GetPlayerID(characterUI));
    public static Data GetLocal(CharacterCamera characterCamera)
    => GetLocal(GetPlayerID(characterCamera));
    public static bool TryGetFirst(out Data player)
    => GetFirst().TryNonNull(out player);
    public static bool TryGetLocal(int playerID, out Data player)
    {
        player = GetLocal(playerID);
        return player != null;
    }
    public static bool TryGetLocal(LocalCharacterControl localCharacterControl, out Data player)
    => TryGetLocal(GetPlayerID(localCharacterControl), out player);
    public static bool TryGetLocal(UIElement uiElement, out Data player)
    => TryGetLocal(GetPlayerID(uiElement), out player);
    public static bool TryGetLocal(Character character, out Data player)
    => TryGetLocal(GetPlayerID(character), out player);
    public static bool TryGetLocal(CharacterUI characterUI, out Data player)
    => TryGetLocal(GetPlayerID(characterUI), out player);
    public static bool TryGetLocal(CharacterCamera characterCamera, out Data player)
    => TryGetLocal(GetPlayerID(characterCamera), out player);

    // Privates
    private static void Recache()
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
    private static int GetPlayerID(LocalCharacterControl localCharacterControl)
    => localCharacterControl.Character.OwnerPlayerSys.PlayerID;
    private static int GetPlayerID(UIElement uiElement)
    => uiElement.LocalCharacter.OwnerPlayerSys.PlayerID;
    private static int GetPlayerID(Character character)
    => character.OwnerPlayerSys.PlayerID;
    private static int GetPlayerID(CharacterUI characterUI)
    => characterUI.TargetCharacter.OwnerPlayerSys.PlayerID;
    private static int GetPlayerID(CharacterCamera characterCamera)
    => characterCamera.TargetCharacter.OwnerPlayerSys.PlayerID;

    // Initializers
    public static void Initialize()
    {
        Local = new List<Data>();
        Harmony.CreateAndPatchAll(typeof(Players));
    }

    // Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(LocalCharacterControl), nameof(LocalCharacterControl.RetrieveComponents))]
    private static void LocalCharacterControl_RetrieveComponents_Post()
    => Recache();

    [HarmonyPostfix, HarmonyPatch(typeof(RPCManager), nameof(RPCManager.SendPlayerHasLeft))]
    private static void RPCManager_SendPlayerHasLeft_Post()
    => Recache();
}

