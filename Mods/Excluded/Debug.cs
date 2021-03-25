using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModPack
{
    public class Debug : AMod, IUpdatable, IWaitForPrefabs
    {
        // Setting
        override protected void Initialize()
        {
            CollapseUnityExplorerMenus();
        }
        override protected void SetFormatting()
        {
        }
        public void OnUpdate()
        {
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    foreach (var graphicRaycaster in Object.FindObjectsOfType<GraphicRaycaster>())
                    //if (graphicRaycaster.GONameIs("HUD"))
                    {
                        Tools.Log($"{graphicRaycaster.name}:");
                        PointerEventData eventData = new PointerEventData(null);
                        eventData.position = Input.mousePosition;
                        List<RaycastResult> hits = new List<RaycastResult>();
                        graphicRaycaster.Raycast(eventData, hits);
                        foreach (var hit in hits)
                            Tools.Log($" - {hit.gameObject.name}");
                    }
                    Tools.Log($"\n");
                }

                if (Input.GetKeyDown(KeyCode.Keypad0))
                {
                    foreach (var localPlayer in GameInput.LocalPlayers)
                    {
                        Transform hudHolder = localPlayer.UI.transform.Find("Canvas/GameplayPanels/HUD");
                        List<CanvasGroup> canvasGroups = hudHolder.GetAllComponentsInHierarchy<CanvasGroup>();
                        Tools.Log($"HUD CanvasGroups count: {canvasGroups.Count}");
                        foreach (var canvasGroup in hudHolder.GetAllComponentsInHierarchy<CanvasGroup>())
                            canvasGroup.blocksRaycasts = true;

                        hudHolder.gameObject.AddComponent<GraphicRaycaster>();
                    }
                }

                if (Input.GetKeyDown(KeyCode.Keypad1))
                {
                    _isEditingHUD = !_isEditingHUD;
                    GameInput.ForceCursorNavigation = _isEditingHUD;
                    //PauseMenu.Pause(_isEditingHUD);
                }
            }
        }

        // Utility
        private bool _isEditingHUD;
        private void CollapseUnityExplorerMenus()
        {
            Canvas unityExplorerCanvas = null;
            foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
                if (canvas.GONameIs("ExplorerCanvas"))
                {
                    unityExplorerCanvas = canvas;
                    break;
                }

            if (unityExplorerCanvas == null)
                return;

            GameObject hideSceneExplorer = unityExplorerCanvas.FindChild("Panel_MainMenu/Content/HorizontalLayout/HorizontalLayout/VerticalLayout/Button");
            hideSceneExplorer.GetComponent<Button>().onClick.Invoke();

            GameObject debugConsoleButtons = unityExplorerCanvas.FindChild("Panel_MainMenu/Content/VerticalLayout/HorizontalLayout/Button");
            debugConsoleButtons.GetComponent<Button>().onClick.Invoke();

            #region recursive method
            /*
            string[] buttonTexts = new[]
            {
                "Hide Scene Explorer",
                "Hide",
                "Search",
            };

            List<Button> buttons = new List<Button>();
            buttons.AddComponentsFromHierarchy(unityExplorerCanvas.transform);
            foreach (var button in buttons)
            {
                Text text = button.GetComponentInChildren<Text>();
                if (text != null && text.text.IsContainedIn(buttonTexts))
                    button.onClick.Invoke();
            }
            */
            #endregion
        }
    }
}