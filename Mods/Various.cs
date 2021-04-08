using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;



/* TO DO:
 * - hide armor extras (like scarf)
 * - prevent dodging right after hitting
 */
namespace ModPack
{
    public class Various : AMod
    {
        #region const
        private const string BOTH_TRIGGERS_PANEL_NAME = "LT+RT";
        private const string QUICKSLOT_12_NAME = "12";
        private const float DEFAULT_ENEMY_HEALTH_RESET_HOURS = 24f;   // Character.HoursToHealthReset
        #endregion
        #region enum
        [Flags]
        private enum ArmorSlots
        {
            None = 0,
            Head = 1 << 1,
            Chest = 1 << 2,
            Feet = 1 << 3,
        }
        #endregion

        // Settings
        static private ModSetting<bool> _enableCheats;
        static private ModSetting<bool> _skipStartupVideos;
        static private ModSetting<ArmorSlots> _armorSlotsToHide;
        static private ModSetting<bool> _extraControllerQuickslots;
        static private ModSetting<bool> _removeCoopScaling;
        static private ModSetting<bool> _removeDodgeInvulnerability;
        static private ModSetting<bool> _healEnemiesOnLoad;
        static private ModSetting<bool> _allowDodgeAnimationCancelling;
        static private ModSetting<bool> _allowPushKickRemoval;
        override protected void Initialize()
        {
            _enableCheats = CreateSetting(nameof(_enableCheats), false);
            _skipStartupVideos = CreateSetting(nameof(_skipStartupVideos), false);
            _armorSlotsToHide = CreateSetting(nameof(_armorSlotsToHide), ArmorSlots.None);
            _extraControllerQuickslots = CreateSetting(nameof(_extraControllerQuickslots), false);
            _removeCoopScaling = CreateSetting(nameof(_removeCoopScaling), false);
            _removeDodgeInvulnerability = CreateSetting(nameof(_removeDodgeInvulnerability), false);
            _healEnemiesOnLoad = CreateSetting(nameof(_healEnemiesOnLoad), false);

            AddEventOnConfigClosed(() =>
            {
                Global.CheatsEnabled = _enableCheats;
            });

            // WIP
            _allowDodgeAnimationCancelling = CreateSetting(nameof(_allowDodgeAnimationCancelling), false);
            _allowPushKickRemoval = CreateSetting(nameof(_allowPushKickRemoval), false);
        }
        override protected void SetFormatting()
        {
            _enableCheats.Format("Enable cheats");
            _enableCheats.Description = "aka Debug Mode";
            _skipStartupVideos.Format("Skip startup videos");
            _skipStartupVideos.Description = "Saves ~3 seconds each time you launch the game";
            _armorSlotsToHide.Format("Armor slots to hide");
            _armorSlotsToHide.Description = "Used to hide ugly helmets (purely visual)";
            _extraControllerQuickslots.Format("16 controller quickslots");
            _extraControllerQuickslots.Description = "Allows you to use the d-pad with LT/RT for 8 extra quickslots\n" +
                                                     "(assumes default d-pad keybinds, sorry!)";
            _removeCoopScaling.Format("Remove multiplayer scaling");
            _removeCoopScaling.Description = "Enemies in multiplayer will have the same stats as in singleplayer";
            _removeDodgeInvulnerability.Format("Remove dodge invulnerability");
            _removeDodgeInvulnerability.Description = "You can get hit during the dodge animation\n" +
                                                      "(even without a backpack)";
            _healEnemiesOnLoad.Format("Heal enemies on load");
            _healEnemiesOnLoad.Description = "Every loading screen fully heals all enemies";

            _allowDodgeAnimationCancelling.Format("Allow dodge to cancel actions");
            _allowDodgeAnimationCancelling.Description = "[WORK IN PROGRESS] Cancelling certain animations might lead to small glitches";
            _allowDodgeAnimationCancelling.IsAdvanced = true;

            _allowPushKickRemoval.Format("Allow \"Push Kick\" removal");
            _allowPushKickRemoval.Description = "[WORK IN PROGRESS] For future skill trees mod\n" +
                                                "Normally, player data won't be saved if they don't have the \"Push Kick\" skill";
            _allowPushKickRemoval.IsAdvanced = true;
        }
        override protected string Description
        => "• Mods (small and big) that didn't get their own section yet :)\n";




        // 16 controller quickslots
        static private void TryOverrideVanillaQuickslotInput(ref bool input, int playerID)
        {
            #region quit
            if (!_extraControllerQuickslots)
                return;
            #endregion

            input &= !ControlsInput.QuickSlotToggle1(playerID) && !ControlsInput.QuickSlotToggle2(playerID);
        }
        static private void TryHandleCustomQuickslotInput(Character character)
        {
            #region quit
            if (!_extraControllerQuickslots)
                return;
            #endregion

            if (character == null || character.QuickSlotMngr == null || character.CharacterUI.IsMenuFocused)
                return;

            int playerID = character.OwnerPlayerSys.PlayerID;
            if (!ControlsInput.QuickSlotToggle1(playerID) && !ControlsInput.QuickSlotToggle2(playerID))
                return;

            int quickslotID = -1;
            if (GameInput.Pressed(playerID, ControlsInput.GameplayActions.Sheathe))
                quickslotID = 8;
            else if (GameInput.Pressed(playerID, ControlsInput.MenuActions.ToggleMapMenu))
                quickslotID = 9;
            else if (GameInput.Pressed(playerID, ControlsInput.GameplayActions.ToggleLights))
                quickslotID = 10;
            else if (GameInput.Pressed(playerID, ControlsInput.GameplayActions.HandleBag))
                quickslotID = 11;

            if (quickslotID < 0)
                return;

            if (ControlsInput.QuickSlotToggle1(playerID))
                quickslotID += 4;

            character.QuickSlotMngr.QuickSlotInput(quickslotID);
        }
        static private void SetupQuickslots(Transform quickslotsHolder)
        {
            Transform quickslotTemplate = quickslotsHolder.Find("1");
            for (int i = quickslotsHolder.childCount; i < 16; i++)
                GameObject.Instantiate(quickslotTemplate, quickslotsHolder);

            QuickSlot[] quickslots = quickslotsHolder.GetComponentsInChildren<QuickSlot>();
            for (int i = 0; i < quickslots.Length; i++)
            {
                quickslots[i].GOSetName((i + 1).ToString());
                quickslots[i].ItemQuickSlot = false;
            }
        }
        static private void SetupQuickslotPanels(CharacterUI ui)
        {
            // Cache
            Transform menuPanelsHolder = GetMenuPanelsHolder(ui);
            Transform gamePanelsHolder = GetGamePanelsHolder(ui);
            Component[] menuSlotsLT = menuPanelsHolder.Find("LT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
            Component[] menuSlotsRT = menuPanelsHolder.Find("RT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
            Component[] gameSlotsLT = gamePanelsHolder.Find("LT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
            Component[] gameSlotsRT = gamePanelsHolder.Find("RT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
            // Copy game 
            for (int i = 0; i < menuSlotsLT.Length; i++)
                menuSlotsLT[i].transform.localPosition = gameSlotsLT[i].transform.localPosition;
            for (int i = 0; i < menuSlotsRT.Length; i++)
                menuSlotsRT[i].transform.localPosition = gameSlotsRT[i].transform.localPosition;

            gamePanelsHolder.Find("imgLT").localPosition = new Vector3(-195f, +170f);
            gamePanelsHolder.Find("imgRT").localPosition = new Vector3(-155f, +170f);

            menuPanelsHolder.Find("LT").localPosition = new Vector3(-90f, +50f);
            menuPanelsHolder.Find("RT").localPosition = new Vector3(+340f, -100f);
            menuPanelsHolder.Find("LT/imgLT").localPosition = new Vector3(-125f, 125f);
            menuPanelsHolder.Find("RT/imgRT").localPosition = new Vector3(-125f, 125f);
            menuPanelsHolder.Find("LeftDecoration").gameObject.SetActive(false);
            menuPanelsHolder.Find("RightDecoration").gameObject.SetActive(false);

            DuplicateQuickslotsInPanel(gamePanelsHolder.Find("LT"), +8, new Vector3(-250f, 0f));
            DuplicateQuickslotsInPanel(gamePanelsHolder.Find("RT"), +8, new Vector3(-250f, 0f));
            DuplicateQuickslotsInPanel(menuPanelsHolder.Find("LT"), +8, new Vector3(-250f, 0f));
            DuplicateQuickslotsInPanel(menuPanelsHolder.Find("RT"), +8, new Vector3(-250f, 0f));
        }
        static private void DuplicateQuickslotsInPanel(Transform panelHolder, int idOffset, Vector3 posOffset)
        {
            Transform quickslotsHolder = panelHolder.Find("QuickSlots");
            foreach (var editorPlacer in quickslotsHolder.GetComponentsInChildren<EditorQuickSlotDisplayPlacer>())
            {
                // Instantiate
                editorPlacer.IsTemplate = true;
                Transform newSlot = GameObject.Instantiate(editorPlacer.transform);
                editorPlacer.IsTemplate = false;
                // Setup
                newSlot.SetParent(quickslotsHolder);
                newSlot.localPosition = editorPlacer.transform.localPosition + posOffset;
                EditorQuickSlotDisplayPlacer newEditorPlacer = newSlot.GetComponent<EditorQuickSlotDisplayPlacer>();
                newEditorPlacer.RefSlotID += idOffset;
                newEditorPlacer.IsTemplate = false;
            }
        }
        static private Transform GetGamePanelsHolder(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/HUD/QuickSlot/Controller/LT-RT");
        static private Transform GetMenuPanelsHolder(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/CharacterMenus/MainPanel/Content/MiddlePanel/QuickSlotPanel/PanelSwitcher/Controller/LT-RT");

        [HarmonyPatch(typeof(ControlsInput), "Sheathe"), HarmonyPostfix]
        static void ControlsInput_Sheathe_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

        [HarmonyPatch(typeof(ControlsInput), "ToggleMap"), HarmonyPostfix]
        static void ControlsInput_ToggleMap_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

        [HarmonyPatch(typeof(ControlsInput), "ToggleLights"), HarmonyPostfix]
        static void ControlsInput_ToggleLights_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

        [HarmonyPatch(typeof(ControlsInput), "HandleBackpack"), HarmonyPostfix]
        static void ControlsInput_HandleBackpack_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

        [HarmonyPatch(typeof(LocalCharacterControl), "UpdateQuickSlots"), HarmonyPostfix]
        static void LocalCharacterControl_UpdateQuickSlots_Pre(ref Character ___m_character)
        => TryHandleCustomQuickslotInput(___m_character);

        [HarmonyPatch(typeof(SplitScreenManager), "Awake"), HarmonyPostfix]
        static void SplitScreenManager_Awake_Post(ref SplitScreenManager __instance)
        {
            #region quit
            if (!_extraControllerQuickslots)
                return;
            #endregion

            CharacterUI charUIPrefab = __instance.m_charUIPrefab;
            GameObject.DontDestroyOnLoad(charUIPrefab);
            SetupQuickslotPanels(charUIPrefab);
        }

        [HarmonyPatch(typeof(CharacterQuickSlotManager), "Awake"), HarmonyPrefix]
        static bool CharacterQuickSlotManager_Awake_Pre(ref CharacterQuickSlotManager __instance)
        {
            #region quit
            if (!_extraControllerQuickslots)
                return true;
            #endregion

            SetupQuickslots(__instance.transform.Find("QuickSlots"));
            return true;
        }

        // Skip startup video
        [HarmonyPatch(typeof(StartupVideo), "Awake"), HarmonyPrefix]
        static bool StartupVideo_Awake_Pre()
        {
            StartupVideo.HasPlayedOnce = _skipStartupVideos.Value;
            return true;
        }

        // Hide armor slots
        static private bool ShouldArmorSlotBeHidden(EquipmentSlot.EquipmentSlotIDs slot)
        => slot == EquipmentSlot.EquipmentSlotIDs.Helmet && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Head)
        || slot == EquipmentSlot.EquipmentSlotIDs.Chest && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Chest)
        || slot == EquipmentSlot.EquipmentSlotIDs.Foot && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Feet);

        [HarmonyPatch(typeof(CharacterVisuals), "EquipVisuals"), HarmonyPrefix]
        static bool CharacterVisuals_EquipVisuals_Pre(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
        {
            #region quit
            if (_armorSlotsToHide == ArmorSlots.None)
                return true;
            #endregion

            // save original hide flags for postfix
            __state = new bool[3];
            __state[0] = _visuals.HideFace;
            __state[1] = _visuals.HideHair;
            __state[2] = _visuals.DisableDefaultVisuals;
            // override hide flags
            if (ShouldArmorSlotBeHidden(_slotID))
            {
                _visuals.HideFace = false;
                _visuals.HideHair = false;
                _visuals.DisableDefaultVisuals = false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CharacterVisuals), "EquipVisuals"), HarmonyPostfix]
        static void CharacterVisuals_EquipVisuals_Post(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
        {
            #region quit
            if (_armorSlotsToHide == ArmorSlots.None)
                return;
            #endregion

            // hide chosen pieces of armor
            if (ShouldArmorSlotBeHidden(_slotID))
                _visuals.Hide();

            // restore original hide flags
            _visuals.HideFace = __state[0];
            _visuals.HideHair = __state[1];
            _visuals.DisableDefaultVisuals = __state[2];
        }

        // Remove co-op scaling
        [HarmonyPatch(typeof(CoopStats), "ApplyToCharacter"), HarmonyPrefix]
        static bool CoopStats_ApplyToCharacter_Pre()
    => !_removeCoopScaling;

        [HarmonyPatch(typeof(CoopStats), "RemoveFromCharacter"), HarmonyPrefix]
        static bool CoopStats_RemoveFromCharacter_Pre()
        => !_removeCoopScaling;

        // Remove dodge invulnerability
        [HarmonyPatch(typeof(Character), "DodgeStep"), HarmonyPostfix]
        static void Character_DodgeStep_Post(ref Hitbox[] ___m_hitboxes, ref int _step)
        {
            #region quit
            if (!_removeDodgeInvulnerability)
                return;
            #endregion

            if (_step > 0 && ___m_hitboxes != null)
                foreach (var hitbox in ___m_hitboxes)
                    hitbox.gameObject.SetActive(true);
        }

        // Enemy health reset time
        [HarmonyPatch(typeof(Character), "LoadCharSave"), HarmonyPrefix]
        static bool Character_LoadCharSave_Pre(ref Character __instance)
        {
            __instance.HoursToHealthReset = _healEnemiesOnLoad ? 0 : DEFAULT_ENEMY_HEALTH_RESET_HOURS;
            return true;
        }

        // Dodge animation cancelling
        [HarmonyPatch(typeof(Character), "DodgeInput", new[] { typeof(Vector3) }), HarmonyPrefix]
        static bool Character_SpellCastAnim_Post(ref int ___m_dodgeAllowedInAction, ref Character.HurtType ___m_hurtType)
        {
            #region quit
            if (!_allowDodgeAnimationCancelling)
                return true;
            #endregion

            if (___m_hurtType == Character.HurtType.NONE)
                ___m_dodgeAllowedInAction = 1;
            return true;
        }

        // Push kick removal
        [HarmonyPatch(typeof(CharacterSave), "IsValid", MethodType.Getter), HarmonyPrefix]
        static bool CharacterSave_IsValid_Getter_Pre(ref bool __result)
        {
            #region quit
            if (!_allowPushKickRemoval.Value)
                return true;
            #endregion

            __result = true;
            return false;
        }
    }
}

/* POUCH
private const float POUCH_CAPACITY = 10f;
static private ModSetting<bool> _pouchToggle;
static private ModSetting<int> _pouchCapacity;
static private ModSetting<bool> _allowOverCapacity;

_pouchToggle = CreateSetting(nameof(_pouchToggle), false);
_pouchCapacity = CreateSetting(nameof(_pouchCapacity), POUCH_CAPACITY.Round(), IntRange(0, 100));
_allowOverCapacity = CreateSetting(nameof(_allowOverCapacity), true);

_pouchToggle.Format("Pouch");
Indent++;
{
    _pouchCapacity.Format("Pouch size", _pouchToggle);
    _allowOverCapacity.Format("Allow over capacity", _pouchToggle);
    Indent--;
}

[HarmonyPatch(typeof(CharacterInventory), "ProcessStart"), HarmonyPostfix]
static void CharacterInventory_ProcessStart_Post(ref CharacterInventory __instance, ref Character ___m_character)
{
    #region quit
    if (!_pouchToggle)
        return;
    #endregion

    ItemContainer pouch = __instance.Pouch;
    if (___m_character.IsPlayer() && pouch != null)
    {
        pouch.SetField("m_baseContainerCapacity", _pouchCapacity.Value, typeof(ItemContainer));
        pouch.AllowOverCapacity = _allowOverCapacity;
    }
}
*/

/* Extra Controller Quickslots
[HarmonyPatch(typeof(QuickSlotPanel), "InitializeQuickSlotDisplays"), HarmonyPostfix]
static void QuickSlotPanel_InitializeQuickSlotDisplays_Post(ref QuickSlotPanel __instance, ref QuickSlotDisplay[] ___m_quickSlotDisplays)
{
    #region quit
    if (!_extraControllerQuickslots)
        return;
    #endregion

    if (__instance.name == BOTH_TRIGGERS_PANEL_NAME)
        for (int i = 0; i < ___m_quickSlotDisplays.Length; i++)
            ___m_quickSlotDisplays[i].RefSlotID = i + 8;
}

[HarmonyPatch(typeof(LocalCharacterControl), "UpdateQuickSlots"), HarmonyPostfix]
static void LocalCharacterControl_UpdateQuickSlots_Pre(ref Character ___m_character)
{
    #region quit
    if (!_extraControllerQuickslots)
        return;
    if (___m_character == null || ___m_character.QuickSlotMngr == null || ___m_character.CharacterUI.IsMenuFocused)
        return;
    #endregion

    int playerID = ___m_character.OwnerPlayerSys.PlayerID;

    if (QuickSlotInstant9(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(8);
    else if (QuickSlotInstant10(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(9);
    else if (QuickSlotInstant11(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(10);
    else if (QuickSlotInstant12(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(11);
}

        static void AddQuickSlot12()
{
    foreach (var localPlayer in Utility.LocalPlayers)
    {
        Transform quickSlotsHolder = localPlayer.ControlledCharacter.GetComponent<CharacterQuickSlotManager>().QuickslotTrans;
        if (quickSlotsHolder.Find(QUICKSLOT_12_NAME) != null)
            continue;

        QuickSlot newQuickSlot = GameObject.Instantiate(quickSlotsHolder.Find("1"), quickSlotsHolder).GetComponent<QuickSlot>();
        newQuickSlot.name = QUICKSLOT_12_NAME;
        foreach (var quickSlot in quickSlotsHolder.GetComponents<QuickSlot>())
            quickSlot.ItemQuickSlot = false;
        localPlayer.ControlledCharacter.QuickSlotMngr.Awake();
    }
}

static void DrawExtraControllerQuickslotSwitcher()
{
    foreach (var localPlayer in Utility.LocalPlayers)
    {
        Transform panel = localPlayer.ControlledCharacter.CharacterUI.QuickSlotMenu.FindChild("PanelSwitcher/Controller/LT-RT").transform;
        if (panel.Find(BOTH_TRIGGERS_PANEL_NAME) != null)
            continue;

        // Instantiate
        Transform LT = panel.Find("LT").transform;
        Transform RT = panel.Find("RT").transform;
        foreach (var quickslotPlacer in LT.GetComponentsInChildren<EditorQuickSlotDisplayPlacer>())
            quickslotPlacer.IsTemplate = true;
        Transform LTRT = GameObject.Instantiate(LT.gameObject, panel).transform;
        LTRT.name = BOTH_TRIGGERS_PANEL_NAME;
        Transform imgLT = LTRT.Find("imgLT");
        Transform imgRT = GameObject.Instantiate(RT.Find("imgRT"), LTRT).transform;
        imgRT.name = "imgRT";

        // Change
        panel.Find("LeftDecoration").gameObject.SetActive(false);
        panel.Find("RightDecoration").gameObject.SetActive(false);
        LT.localPosition = new Vector2(-300f, 0);
        RT.localPosition = new Vector2(0f, 0);
        LTRT.localPosition = new Vector2(+300f, 0);
        imgLT.localPosition = new Vector2(-22.5f, 22.5f);
        imgRT.localPosition = new Vector2(+22.5f, 22.5f);
    }
}

*/
