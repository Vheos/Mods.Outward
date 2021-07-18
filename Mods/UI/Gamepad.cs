using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using Vheos.Tools.ModdingCore;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Vheos.Tools.Extensions.UnityObjects;
using Vheos.Tools.Extensions.General;
using Vheos.Tools.Extensions.Collections;



namespace ModPack
{
    public class Gamepad : AMod, IUpdatable
    {
        // Setting
        public ModSetting<bool> _betterStashNavigation;
        override protected void Initialize()
        {
            _betterStashNavigation = CreateSetting(nameof(_betterStashNavigation), false);
        }
        override protected void SetFormatting()
        {
            _betterStashNavigation.Format("Better stash navigation");
            _betterStashNavigation.Description = "LB = switch to (or scroll down in) the player's bag\n" +
                                                              "RB = switch to (or scroll downn in) the chest contents\n" +
                                                              "LT = change sorting (default, by weight, by durability)\n" +
                                                              "RT = find currently focused item in the other panel";
        }
        override protected string Description
        => "• Better stash navigation";
        override protected string SectionOverride
        => ModSections.UI;
        override public void LoadPreset(int preset)
        {
            switch ((Presets.Preset)preset)
            {
                case Presets.Preset.Vheos_PreferredUI:
                    ForceApply();
                    _betterStashNavigation.Value = true;
                    break;
            }
        }
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
                if (bagItems.IsValid(nextID))
                    bagItems[nextID].OnSelect();
                else
                    bagItems.Last().OnSelect();
            }
            else if (bagItems.IsNotNullOrEmpty())
                bagItems.First().OnSelect();
            else if (pouchItems.IsNotNullOrEmpty())
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
                if (chestItems.IsValid(nextID))
                    chestItems[nextID].OnSelect();
                else
                    chestItems.Last().OnSelect();
            }
            else if (chestItems.IsNotNullOrEmpty())
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

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        [HarmonyPatch(typeof(StashPanel), "Show"), HarmonyPostfix]
        static void StashPanel_Show_Post(StashPanel __instance)
        => UpdateStashName(Players.GetLocal(__instance));
    }
}