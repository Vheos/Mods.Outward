using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine.EventSystems;



namespace ModPack
{
    public class Gamepad : AMod, IUpdatable
    {
        // Setting
        static private ModSetting<bool> _extraGamepadQuickslots;
        public ModSetting<bool> _betterStashNavigation;
        override protected void Initialize()
        {
            _betterStashNavigation = CreateSetting(nameof(_betterStashNavigation), false);
            _extraGamepadQuickslots = CreateSetting(nameof(_extraGamepadQuickslots), false);
        }
        override protected void SetFormatting()
        {
            _extraGamepadQuickslots.Format("16 quickslots");
            _extraGamepadQuickslots.Description = "Allows you to use the d-pad with LT/RT for 8 extra quickslots\n" +
                                                     "(requires default d-pad keybinds AND game restart)";
            _betterStashNavigation.Format("Better stash navigation");
            _betterStashNavigation.Description = "LB = switch to (or scroll down in) the player's bag\n" +
                                                              "RB = switch to (or scroll downn in) the chest contents\n" +
                                                              "LT = change sorting (default, by weight, by durability)\n" +
                                                              "RT = find currently focused item in the other panel";
        }
        override protected string Description
        => "• 16 quickslots\n" +
           "• Better stash navigation";
        override protected string SectionOverride
        => SECTION_UI;
        public void OnUpdate()
        {
            #region quit
            if (!_betterStashNavigation)
                return;
            #endregion

            foreach (var player in Players.Local)
                if (player.IsUsingGamepad && player.UI.m_displayedMenuIndex == CharacterUI.MenuScreens.Stash)
                    if (player.Pressed(ControlsInput.MenuActions.GoToNextMenu))
                        SwitchToStash(player);
                    else if (player.Pressed(ControlsInput.MenuActions.GoToPreviousMenu))
                        SwitchToInventory(player);
                    else if (player.Pressed(ControlsInput.MenuActions.PreviousFilter))
                        ChangeSorting(player);
                    else if (player.Pressed(ControlsInput.MenuActions.NextFilter))
                        FindSameItemInOtherPanel(player);

        }

        // Utility
        static private void TryOverrideVanillaQuickslotInput(ref bool input, int playerID)
        {
            #region quit
            if (!_extraGamepadQuickslots)
                return;
            #endregion

            input &= !ControlsInput.QuickSlotToggle1(playerID) && !ControlsInput.QuickSlotToggle2(playerID);
        }
        static private void TryHandleCustomQuickslotInput(Character character)
        {
            #region quit
            if (!_extraGamepadQuickslots)
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
        static private void SwitchToInventory(Players.Data player)
        {
            if (EventSystem.current.GetCurrentSelectedGameObject(player.ID).TryGetComponent(out ItemDisplay currentItem)
            && currentItem.m_refItem == null)
                return;

            // Cache
            InventoryContentDisplay inventory = GetPlayerStashInventoryPanel(GetStashPanel(player.UI)).GetComponent<InventoryContentDisplay>();
            List<ItemDisplay> pouchItems = inventory.m_pouchDisplay.m_assignedDisplays;
            List<ItemDisplay> bagItems = inventory.m_bagDisplay.m_assignedDisplays;
            int currentID = bagItems.IndexOf(currentItem);

            // Execute
            if (currentID >= bagItems.Count - 1)
                bagItems.First().OnSelect();
            else if (currentID >= 0)
            {
                int nextID = currentID + bagItems.Count / 2;
                if (bagItems.IsIndexValid(nextID))
                    bagItems[nextID].OnSelect();
                else
                    bagItems.Last().OnSelect();
            }
            else if (bagItems.IsNotEmpty())
                bagItems.First().OnSelect();
            else if (pouchItems.IsNotEmpty())
                pouchItems.First().OnSelect();
        }
        static private void SwitchToStash(Players.Data player)
        {
            if (EventSystem.current.GetCurrentSelectedGameObject(player.ID).TryGetComponent(out ItemDisplay currentItem)
            && currentItem.m_refItem == null)
                return;

            // Cache
            ContainerDisplay chest = GetChestStashInventoryPanel(GetStashPanel(player.UI)).GetComponent<ContainerDisplay>();
            List<ItemDisplay> chestItems = chest.m_assignedDisplays;
            int currentID = chestItems.IndexOf(currentItem);

            // Execute
            if (currentID >= chestItems.Count - 1)
                chestItems.First().OnSelect();
            else if (currentID >= 0)
            {
                int nextID = currentID + chestItems.Count / 2;
                if (chestItems.IsIndexValid(nextID))
                    chestItems[nextID].OnSelect();
                else
                    chestItems.Last().OnSelect();
            }
            else if (chestItems.IsNotEmpty())
                chestItems.First().OnSelect();
        }
        static private void ChangeSorting(Players.Data player)
        {
            // Cache
            Transform stashPanelHolder = GetStashPanel(player.UI);
            InventoryContentDisplay inventory = GetPlayerStashInventoryPanel(stashPanelHolder).GetComponent<InventoryContentDisplay>();
            ContainerDisplay chestDisplay = GetChestStashInventoryPanel(stashPanelHolder).GetComponent<ContainerDisplay>();

            // Sort
            ItemListDisplay.SortingType nextSorting = default;
            switch (chestDisplay.m_lastSortingType)
            {
                case ItemListDisplay.SortingType.ByList: nextSorting = ItemListDisplay.SortingType.ByWeight; break;
                case ItemListDisplay.SortingType.ByWeight: nextSorting = ItemListDisplay.SortingType.ByDurability; break;
                case ItemListDisplay.SortingType.ByDurability: nextSorting = ItemListDisplay.SortingType.ByList; break;
            }
            foreach (var containerDisplay in new[] { inventory.m_pouchDisplay, inventory.m_bagDisplay, chestDisplay })
                containerDisplay.SortBy(nextSorting);
            UpdateStashName(player);

            // Select first
            GameObject selectedObject = EventSystem.current.GetCurrentSelectedGameObject(player.ID);
            if (selectedObject != null
            && selectedObject.TryGetComponent(out ItemDisplay currentItem)
            && currentItem.ParentItemListDisplay != null)
                currentItem.ParentItemListDisplay.m_assignedDisplays.First().OnSelect();

        }
        static private void UpdateStashName(Players.Data player)
        {
            // Cache
            Transform stashPanelHolder = GetStashPanel(player.UI);
            ContainerDisplay chestDisplay = GetChestStashInventoryPanel(stashPanelHolder).GetComponent<ContainerDisplay>();
            Text stashTitle = GetChestStashTitle(stashPanelHolder).GetComponent<Text>();

            // Execute
            stashTitle.text = "Stash";
            switch (chestDisplay.m_lastSortingType)
            {
                case ItemListDisplay.SortingType.ByList: break;
                case ItemListDisplay.SortingType.ByWeight: stashTitle.text += "<color=lime> (sorted by Weight)</color>"; break;
                case ItemListDisplay.SortingType.ByDurability: stashTitle.text += "<color=orange> (sorted by Durability)</color>"; break;
            }
        }
        static private void FindSameItemInOtherPanel(Players.Data player)
        {
            if (EventSystem.current.GetCurrentSelectedGameObject(player.ID).TryGetComponent(out ItemDisplay currentItem) && currentItem.m_refItem == null)
                return;

            // Cache
            Transform stashPanelHolder = GetStashPanel(player.UI);
            InventoryContentDisplay inventory = GetPlayerStashInventoryPanel(stashPanelHolder).GetComponent<InventoryContentDisplay>();
            List<ItemDisplay> chestItems = GetChestStashInventoryPanel(stashPanelHolder).GetComponent<ContainerDisplay>().m_assignedDisplays;
            List<ItemDisplay> bagItems = inventory.m_bagDisplay.m_assignedDisplays;
            List<ItemDisplay> pouchItems = inventory.m_pouchDisplay.m_assignedDisplays;

            // Execute
            ItemDisplay foundItem;
            if (currentItem.IsContainedIn(chestItems))
            {
                foundItem = FindItemInContainerDisplay(currentItem, bagItems);
                if (foundItem == null)
                    foundItem = FindItemInContainerDisplay(currentItem, pouchItems);
            }
            else if (currentItem.IsContainedIn(bagItems) || currentItem.IsContainedIn(pouchItems))
                foundItem = FindItemInContainerDisplay(currentItem, chestItems);
            else
                return;

            foundItem.OnSelect();
        }
        static private ItemDisplay FindItemInContainerDisplay(ItemDisplay item, List<ItemDisplay> otherContainerItems)
        {
            foreach (var otherItem in otherContainerItems)
                if (otherItem.m_refItem.ItemID == item.m_refItem.ItemID)
                    return otherItem;
            return null;
        }
        // Find
        static private Transform GetGamePanelsHolder(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/HUD/QuickSlot/Controller/LT-RT");
        static private Transform GetMenuPanelsHolder(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/CharacterMenus/MainPanel/Content/MiddlePanel/QuickSlotPanel/PanelSwitcher/Controller/LT-RT");
        static private RectTransform GetStashPanel(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/ModalMenus/Stash - Panel") as RectTransform;
        static private Transform GetPlayerStashInventoryPanel(Transform stashPanel)
        => stashPanel.Find("Content/MiddlePanel/PlayerInventory/InventoryContent");
        static private Transform GetChestStashInventoryPanel(Transform stashPanel)
        => stashPanel.Find("Content/MiddlePanel/StashInventory/SectionContent/Scroll View/Viewport/Content/ContainerDisplay_Simple");
        static private Transform GetChestStashTitle(Transform stashPanel)
        => stashPanel.Find("Content/TopPanel/Shop PanelTop/lblShopName");

        // 16 controller quickslots
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
            if (!_extraGamepadQuickslots)
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
            if (!_extraGamepadQuickslots)
                return true;
            #endregion

            SetupQuickslots(__instance.transform.Find("QuickSlots"));
            return true;
        }

        // Better stash navigation
        [HarmonyPatch(typeof(StashPanel), "Show"), HarmonyPostfix]
        static void StashPanel_Show_Post(ref StashPanel __instance)
        => UpdateStashName(Players.GetLocal(__instance));
    }
}