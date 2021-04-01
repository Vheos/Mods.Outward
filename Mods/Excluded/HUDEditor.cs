using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;

namespace ModPack
{
    public class HUDEditor : AMod, IUpdatable
    {
        #region const
        public const float LOAD_SETTINGS_DELAY = 0.1f;
        static private readonly Dictionary<HUDGroup, string> PANEL_PATHS_BY_HUD_GROUP = new Dictionary<HUDGroup, string>
        {
            [HUDGroup.KeyboardQuickslots] = "QuickSlot/Keyboard",
            [HUDGroup.GamepadQuickslots] = "QuickSlot/Controller",
            [HUDGroup.Vitals] = "MainCharacterBars",
            [HUDGroup.StatusEffects] = "StatusEffect - Panel",
            [HUDGroup.Temperature] = "TemperatureSensor",
            [HUDGroup.Arrows] = "QuiverDisplay",
            [HUDGroup.Compass] = "Compass",
            [HUDGroup.Stability] = "Stability",
        };
        static private readonly Dictionary<HUDGroup, Type> COMPONENT_TYPES_BY_HUD_GROUP = new Dictionary<HUDGroup, Type>
        {
            [HUDGroup.KeyboardQuickslots] = typeof(KeyboardQuickSlotPanel),
            [HUDGroup.GamepadQuickslots] = typeof(QuickSlotPanelSwitcher),
            [HUDGroup.Vitals] = typeof(CharacterBarListener),
            [HUDGroup.StatusEffects] = typeof(StatusEffectPanel),
            [HUDGroup.Temperature] = typeof(TemperatureExposureDisplay),
            [HUDGroup.Arrows] = typeof(QuiverDisplay),
            [HUDGroup.Compass] = typeof(UICompass),
            [HUDGroup.Stability] = typeof(StabilityDisplay_Simple),
        };
        #endregion
        #region enum
        private enum HUDGroup
        {
            KeyboardQuickslots,
            GamepadQuickslots,
            Vitals,
            StatusEffects,
            Temperature,
            Arrows,
            Compass,
            Stability,
            Notifications,
        }
        private enum SettingsOperation
        {
            Save,
            Load,
        }
        private enum EditState
        {
            Move,
            Scale,
        }
        [Flags]
        private enum Anchor
        {
            None = 0,
            Left = 1 << 1,
            Right = 1 << 2,
            Bottom = 1 << 3,
            Top = 1 << 4,
            BottomLeft = Bottom | Left,
            BottomRight = Bottom | Right,
            TopLeft = Top | Left,
            TopRight = Top | Right,
        }
        #endregion
        #region class
        private class PerPlayersSettings
        {
            // Settings
            public ModSetting<bool> _toggle;
            public ModSetting<bool> _savedDataToggle;
            public Dictionary<HUDGroup, ModSetting<Vector3>> _dataByHUDGroup;

            // Utility
            public Vector2 GetPosition(HUDGroup hudGroup)
            => _dataByHUDGroup[hudGroup].Value;
            public float GetScale(HUDGroup hudGroup)
            => _dataByHUDGroup[hudGroup].Value.z;
            public void Set(HUDGroup hudGroup, Vector2 position, float scale)
            => _dataByHUDGroup[hudGroup].Value = position.Append(scale);
            public bool IsNotZero(HUDGroup hudGroup)
            => _dataByHUDGroup[hudGroup] != Vector3.zero;

            // Constructor
            public PerPlayersSettings()
            => _dataByHUDGroup = new Dictionary<HUDGroup, ModSetting<Vector3>>();

        }
        #endregion

        // Setting
        static private ModSetting<bool> _startEditMode;
        static private PerPlayersSettings[] _perPlayerSettings;
        override protected void Initialize()
        {
            _perPlayerSettings = new PerPlayersSettings[2];
            for (int i = 0; i < 2; i++)
            {
                PerPlayersSettings tmp = new PerPlayersSettings();
                _perPlayerSettings[i] = tmp;
                string playerPostfix = (i + 1).ToString();

                tmp._toggle = CreateSetting(nameof(tmp._toggle) + playerPostfix, false);
                tmp._savedDataToggle = CreateSetting(nameof(tmp._savedDataToggle) + playerPostfix, false);
                foreach (var hudGroup in COMPONENT_TYPES_BY_HUD_GROUP.Keys.ToArray())
                    tmp._dataByHUDGroup.Add(hudGroup, CreateSetting($"_data{hudGroup}{playerPostfix}", Vector3.zero));
            }

            _startEditMode = CreateSetting(nameof(_startEditMode), false);
            _startEditMode.AddEvent(() =>
            {
                if (_startEditMode && !_isInEditMode)
                    SetEditMode(true);
            });

            AddEventOnConfigOpened(() =>
            {
                if (_isInEditMode)
                    SetEditMode(false);
            });
        }
        override protected void SetFormatting()
        {
            _startEditMode.Format("Start Edit Mode");

            for (int i = 0; i < 2; i++)
            {
                PerPlayersSettings tmp = _perPlayerSettings[i];

                tmp._toggle.Format($"Player {i + 1}");
                Indent++;
                {
                    tmp._savedDataToggle.Format("Data", tmp._toggle);
                    Indent++;
                    {
                        foreach (var hudGroup in COMPONENT_TYPES_BY_HUD_GROUP.Keys.ToArray())
                            tmp._dataByHUDGroup[hudGroup].Format(hudGroup.ToString(), tmp._savedDataToggle);
                        Indent--;
                    }
                    Indent--;
                }
            }
        }
        public void OnUpdate()
        {
            if (!_isInEditMode)
                return;

            if (_focus.Transform == null)
            {
                if (KeyCode.Mouse0.Pressed())
                    HandleHits(EditState.Move);
                else if (KeyCode.Mouse1.Pressed())
                    HandleHits(EditState.Scale);
            }
            else
                switch (_editState)
                {
                    case EditState.Move:
                        Vector2 mouseToElementOffset = _focus.EditData;
                        _focus.Transform.position = Input.mousePosition.XY() + mouseToElementOffset;
                        if (KeyCode.Mouse0.Released())
                            _focus.Transform = null;
                        break;
                    case EditState.Scale:
                        float clickY = _focus.EditData.x;
                        float clickScale = _focus.EditData.y;
                        float offset = Input.mousePosition.y - clickY;
                        _focus.Transform.localScale = (offset / Screen.height + clickScale).ToVector3();
                        if (KeyCode.Mouse1.Released())
                            _focus.Transform = null;
                        break;
                }
        }

        // Utility  
        static private void SetupHUDElements()
        {
            foreach (var player in Players.Local)
                foreach (var panelPathByHUDGroup in PANEL_PATHS_BY_HUD_GROUP)
                    foreach (var uiElement in GetHUDHolder(player.UI).Find(panelPathByHUDGroup.Value).GetAllComponentsInHierarchy<CanvasGroup, Image>())
                        switch (uiElement)
                        {
                            case CanvasGroup t: t.blocksRaycasts = true; break;
                            case Image t: t.raycastTarget = true; break;
                        }
        }
        static private void SetEditMode(bool state)
        {
            PauseMenu.Pause(state);
            GameInput.ForceCursorNavigation = state;
            SetTemplates(state);
            _isInEditMode = state;

            if (state)
            {
                _startEditMode.SetSilently(false);
                Tools.IsConfigOpen = false;
                SetupHUDElements();
                _focus.Transform = null;
            }
            else
                SaveLoadSettings(SettingsOperation.Save);
        }
        static private void SetTemplates(bool state)
        {
            foreach (var player in Players.Local)
            {
                Transform hudHolder = GetHUDHolder(player.UI);
                Transform temperature = hudHolder.Find("TemperatureSensor/Display");
                Transform statusEffect = hudHolder.Find("StatusEffect - Panel/Icon");
                Transform quickslotsHolder = hudHolder.Find("QuickSlot");

                temperature.GOSetActive(state);
                statusEffect.GetComponent<StatusEffectIcon>().enabled = !state;
                statusEffect.GOSetActive(state);
                quickslotsHolder.GetComponent<QuickSlotControllerSwitcher>().enabled = !state;
                if (state)
                    foreach (Transform child in quickslotsHolder.transform)
                        child.GOSetActive(true);
            }
        }
        static private void SaveLoadSettings(SettingsOperation operation)
        {
            foreach (var player in Players.Local)
                foreach (var panelPathByHUDGroup in PANEL_PATHS_BY_HUD_GROUP)
                {
                    PerPlayersSettings settings = _perPlayerSettings[player.ID];
                    HUDGroup group = panelPathByHUDGroup.Key;
                    Transform panel = GetHUDHolder(player.UI).Find(panelPathByHUDGroup.Value);
                    switch (operation)
                    {
                        case SettingsOperation.Save:
                            settings.Set(group, panel.localPosition, panel.localScale.x);
                            break;
                        case SettingsOperation.Load:
                            if (settings.IsNotZero(group))
                            {
                                panel.localPosition = settings.GetPosition(group);
                                panel.localScale = settings.GetScale(group).ToVector3();
                            }
                            break;
                    }
                }
        }
        static private void HandleHits(EditState state)
        {
            foreach (var player in Players.Local)
                foreach (var hit in GetHUDHolder(player.UI).GetOrAddComponent<GraphicRaycaster>().GetMouseHits())
                {
                    Transform hudGroupHolder = hit.gameObject.FindAncestorWithComponent(COMPONENT_TYPES_BY_HUD_GROUP.Values.ToArray());
                    if (hudGroupHolder == null)
                        continue;

                    _focus.Transform = hudGroupHolder;
                    _editState = state;
                    if (state == EditState.Move)
                        _focus.EditData = Input.mousePosition.XY().OffsetTo(hudGroupHolder.position);
                    else if (state == EditState.Scale)
                        _focus.EditData = new Vector2(Input.mousePosition.y, hudGroupHolder.localScale.x);
                    break;
                }
        }
        static private Transform GetHUDHolder(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/HUD");
        static private Transform GetKeyboardQuickslotsGamePanel(Transform hudHolder)
        => hudHolder.Find("QuickSlot/Keyboard");
        static private Transform GetGamepadQuickslotsGamePanel(Transform hudHolder)
        => hudHolder.Find("QuickSlot/Controller");
        static private EditState _editState;
        static private bool _isInEditMode;
        static private (Transform Transform, Vector2 EditData) _focus;

        // Hooks
        [HarmonyPatch(typeof(LocalCharacterControl), "RetrieveComponents"), HarmonyPostfix]
        static void LocalCharacterControl_RetrieveComponents_Post(LocalCharacterControl __instance)
        => __instance.ExecuteOnceAfterDelay(LOAD_SETTINGS_DELAY, () => SaveLoadSettings(SettingsOperation.Load));
    }
}