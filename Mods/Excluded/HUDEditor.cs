using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace ModPack
{
    public class HUDEditor : AMod, IUpdatable
    {
        #region const
        static public readonly string[] FORCE_RAYCASTABLE_PANEL_NAMES = new[]
        {
            "StatusEffect - Panel",
            "MainCharacterBars/Mana",
        };
        #endregion
        #region class
        private class PerPlayersSettings
        {
            // Settings
            static private ModSetting<Vector3> _quickslotsPosition;
            static private ModSetting<Vector3> _quickslotsScale;
            public GraphicRaycaster HUDRaycaster;
            public Transform HUDHolder
            => HUDRaycaster.transform;
        }
        #endregion

        // Setting
        static private ModSetting<bool> _startEditMode;
        static private PerPlayersSettings[] _perPlayerSettings;
        override protected void Initialize()
        {
            _perPlayerSettings = new PerPlayersSettings[2];
            _attachedTransformsByOffset = new Dictionary<Transform, Vector3>();

            _startEditMode = CreateSetting(nameof(_startEditMode), false);
            _startEditMode.AddEvent(() =>
            {
                if (_startEditMode)
                {
                    _startEditMode.SetSilently(false);
                    Tools.IsConfigOpen = false;
                    foreach (var player in Players.Local)
                        InitializeHUDCanvasGroups(player);
                    StartEditMode();
                }
            });

            AddEventOnConfigOpened(() =>
            {
                if (_isInEditMode)
                    StopEditMode();
            });
        }
        override protected void SetFormatting()
        {
            _startEditMode.Format("Start Edit Mode");
        }
        public void OnUpdate()
        {
            if (_isInEditMode)
            {
                if (KeyCode.Mouse0.Pressed())
                    HandleHits();

                if (KeyCode.Mouse0.Released())
                    _attachedTransformsByOffset.Clear();

                foreach (var offsetsByDraggedTransform in _attachedTransformsByOffset)
                {
                    Transform transform = offsetsByDraggedTransform.Key;
                    Vector3 offset = offsetsByDraggedTransform.Value;
                    transform.position = Input.mousePosition + offset;
                }
            }
        }

        // Utility  
        static private void InitializeHUDCanvasGroups(Players.Data player)
        {
            foreach (var canvasGroup in GetHUDHolder(player.UI).GetAllComponentsInHierarchy<CanvasGroup>())
                canvasGroup.blocksRaycasts = true;
        }
        static private void StartEditMode()
        {
            _isInEditMode = true;
            GameInput.ForceCursorNavigation = true;

            foreach (var player in Players.Local)
                foreach (var panelName in FORCE_RAYCASTABLE_PANEL_NAMES)
                    foreach (var image in GetHUDHolder(player.UI).Find(panelName).GetAllComponentsInHierarchy<Image>())
                        image.raycastTarget = true;
        }
        static private void StopEditMode()
        {
            _isInEditMode = false;
            GameInput.ForceCursorNavigation = false;

            _attachedTransformsByOffset.Clear();
        }
        static private void HandleHits()
        {
            foreach (var player in Players.Local)
            {
                Tools.Log($"{player.Character.Name}:");
                foreach (var hit in GetHUDHolder(player.UI).GetOrAddComponent<GraphicRaycaster>().GetMouseHits())
                {
                    Transform transform = hit.gameObject.transform;
                    Vector3 offset = Input.mousePosition.OffsetTo(transform.position);
                    _attachedTransformsByOffset.Add(transform, offset);
                    Tools.Log($" - {hit.gameObject.name}");
                }
                Tools.Log($"\n");
            }
        }
        static private Transform GetHUDHolder(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/HUD");
        static private bool _isInEditMode;
        static private Dictionary<Transform, Vector3> _attachedTransformsByOffset;

        // Hooks
        [HarmonyPatch(typeof(LocalCharacterControl), "RetrieveComponents"), HarmonyPostfix]
        static void LocalCharacterControl_RetrieveComponents_Post(LocalCharacterControl __instance)
        {
            Players.Data player = Players.GetLocal(__instance);
            InitializeHUDCanvasGroups(player);
        }

    }
}