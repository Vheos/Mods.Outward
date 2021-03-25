using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System;



namespace ModPack
{
    public class Camera : AMod, IDelayedInit, IUpdatable
    {
        #region const
        static private readonly Vector3 DEFAULT_OFFSET = new Vector3(0f, 1f, -3f);
        private const float DEFAULT_FOV = 50f;
        private const float DEFAULT_FOLLOW_SPEED = 4.5f;
        private const float AIM_COROUTINE_UPDATE_SPEED = 0.2f;
        private const float AIM_COROUTINE_MARGIN = 0.01f;
        #endregion
        #region enum
        #endregion
        #region class
        private class PerPlayerData
        {
            // Settings
            public ModSetting<bool> _toggle;
            public ModSetting<float> _zoomControlAmount;
            public ModSetting<float> _zoomControlSpeed;
            public ModSetting<bool> _offsetToggle, _variousToggle;
            public ModSetting<Vector3> _offsetMin, _offsetAvg, _offsetMax;
            public ModSetting<Vector3> _variousMin, _variousAvg, _variousMax;

            // Utility
            public void StartAimCoroutine(MonoBehaviour owner, bool isEnteringAimMode)
            {
                // Choose zoom in or out
                float targetZoom = 0f;
                Func<bool> test = () => _zoomControlAmount.Value < targetZoom + AIM_COROUTINE_MARGIN;
                if (isEnteringAimMode)
                {
                    targetZoom = 1f;
                    test = () => _zoomControlAmount.Value > targetZoom - AIM_COROUTINE_MARGIN;
                }

                // Update over time
                if (_aimCoroutine != null)
                    owner.StopCoroutine(_aimCoroutine);
                _aimCoroutine = owner.ExecuteUntil
                (
                    test,
                    () => _zoomControlAmount.Value = _zoomControlAmount.Value.Lerp(targetZoom, AIM_COROUTINE_UPDATE_SPEED),
                    () => _zoomControlAmount.Value = targetZoom
                );
            }
            public Vector3 CurrentOffset
            {
                get
                {
                    if (_zoomControlAmount < 0f)
                        return Vector3.LerpUnclamped(_offsetMin, _offsetAvg, _zoomControlAmount + 1f);
                    if (_zoomControlAmount > 0f)
                        return Vector3.LerpUnclamped(_offsetAvg, _offsetMax, _zoomControlAmount);
                    return _offsetAvg;
                }
            }
            public Vector3 CurrentVarious
            {
                get
                {
                    if (_zoomControlAmount < 0f)
                        return Vector3.LerpUnclamped(_variousMin, _variousAvg, _zoomControlAmount + 1f);
                    if (_zoomControlAmount > 0f)
                        return Vector3.LerpUnclamped(_variousAvg, _variousMax, _zoomControlAmount);
                    return _variousAvg;
                }
            }
            public float Sensitivity
            {
                get => IgnoreAxes ? 0f : _cachedSensitivity;
                set => _cachedSensitivity = value;
            }
            public bool IgnoreAxes;
            private float _cachedSensitivity;
            private Coroutine _aimCoroutine;
        }
        #endregion

        static private PerPlayerData[] _perPlayerData;
        override protected void Initialize()
        {
            _perPlayerData = new PerPlayerData[2];
            for (int i = 0; i < 2; i++)
            {
                PerPlayerData tmp = new PerPlayerData();
                _perPlayerData[i] = tmp;

                string playerPostfix = (i + 1).ToString();
                tmp._toggle = CreateSetting(nameof(tmp._toggle) + playerPostfix, false);
                tmp._zoomControlAmount = CreateSetting(nameof(tmp._zoomControlAmount) + playerPostfix, 0f, FloatRange(-1f, 1f));
                tmp._zoomControlSpeed = CreateSetting(nameof(tmp._zoomControlSpeed) + playerPostfix, 0.5f, FloatRange(0f, 1f));

                tmp._offsetToggle = CreateSetting(nameof(tmp._offsetToggle) + playerPostfix, false);
                tmp._offsetMin = CreateSetting(nameof(tmp._offsetMin) + playerPostfix, DEFAULT_OFFSET.Add(0, 1f, -3f));
                tmp._offsetAvg = CreateSetting(nameof(tmp._offsetAvg) + playerPostfix, DEFAULT_OFFSET);
                tmp._offsetMax = CreateSetting(nameof(tmp._offsetMax) + playerPostfix, DEFAULT_OFFSET.Add(0.75f, -0.25f, 1f));

                Vector3 otherDefault = new Vector3(DEFAULT_FOV, DEFAULT_FOLLOW_SPEED, 1f);
                tmp._variousToggle = CreateSetting(nameof(tmp._variousToggle) + playerPostfix, false);
                tmp._variousMin = CreateSetting(nameof(tmp._variousMin) + playerPostfix, otherDefault.Mul(1.2f, 2 / 3f, 1f));
                tmp._variousAvg = CreateSetting(nameof(tmp._variousAvg) + playerPostfix, otherDefault);
                tmp._variousMax = CreateSetting(nameof(tmp._variousMax) + playerPostfix, otherDefault.Mul(0.8f, 2.0f, 0.75f));

                int id = i;
                tmp._zoomControlAmount.AddEvent(() => UpdateCameraSettings(id));

                AddEventOnConfigClosed(() =>
                {
                    foreach (var localPlayer in GameInput.LocalPlayers)
                        UpdateCameraSettings(localPlayer.ID);
                });

                tmp.Sensitivity = 1f;
            }
        }
        override protected void SetFormatting()
        {
            for (int i = 0; i < 2; i++)
            {
                PerPlayerData tmp = _perPlayerData[i];

                // Settings
                tmp._toggle.Format($"Player {i + 1}");
                tmp._toggle.Description = $"Change settings for local player {i + 1}";
                Indent++;
                {
                    tmp._zoomControlAmount.Format("Zoom amount", tmp._toggle);
                    tmp._zoomControlAmount.Description = "-1  -  max zoom out\n" +
                                                        "0  -  default zoom\n" +
                                                        "+1  -  max zoom in";
                    tmp._zoomControlSpeed.Format("Zoom control speed", tmp._toggle);
                    tmp._zoomControlSpeed.Description = "How quickly you want to zoom using mouse/gamepad\n" +
                        "Mouse: use mouse scroll wheel\n" +
                        "Gamepad: use right stick while holding Sprint and Block buttons";
                    tmp._offsetToggle.Format("Offset", tmp._toggle);
                    tmp._offsetToggle.Description = "Change camera position (XYZ) presets";
                    Indent++;
                    {
                        tmp._offsetMin.Format("at zoom =  -1", tmp._offsetToggle);
                        tmp._offsetAvg.Format("at zoom =  0", tmp._offsetToggle);
                        tmp._offsetMax.Format("at zoom = +1", tmp._offsetToggle);
                        Indent--;
                    }

                    tmp._variousToggle.Format("FOV, FollowSpeed, Sensitivity", tmp._toggle);
                    tmp._variousToggle.Description = "Change other settings presets:\n" +
                        "X  -  field of view (vertical)\n" +
                        "Y  -  how quickly camera follows your character\n" +
                        "Z  -  how quickly camera rotates";
                    Indent++;
                    {
                        tmp._variousMin.Format("at zoom =  -1", tmp._variousToggle);
                        tmp._variousAvg.Format("at zoom =  0", tmp._variousToggle);
                        tmp._variousMax.Format("at zoom = +1", tmp._variousToggle);
                        Indent--;
                    }
                    Indent--;
                }
            }
        }
        override protected string Description
        => "• Override camera settings\n" +
           "(position, fov, follow speed, sensitivity)\n" +
           "• Control camera using mouse/gamepad\n" +
           "• Define presets and smoothly interpolate between them";
        public void OnUpdate()
        {
            foreach (var localPlayer in GameInput.LocalPlayers)
            {
                if (localPlayer.UI.IsMenuFocused)
                    continue;

                // Cache
                int id = localPlayer.ID;
                PerPlayerData settings = _perPlayerData[id];

                settings.IgnoreAxes = false;
                if (settings._zoomControlSpeed > 0)
                {
                    float zoomDelta = 0f;
                    if (GameInput.IsUsingKeyboard(id))
                        zoomDelta = Input.mouseScrollDelta.y * 2f;
                    else if (GameInput.IsSprinting(id) && GameInput.IsBlocking(id))
                    {
                        Vector2 cameraInput = GameInput.CameraMovementInput(id);
                        if (cameraInput.y.Abs() > cameraInput.x.Abs())
                            zoomDelta = cameraInput.y;
                        settings.IgnoreAxes = true;
                    }

                    if (zoomDelta != 0)
                        settings._zoomControlAmount.Value += zoomDelta * settings._zoomControlSpeed * 10f * Time.unscaledDeltaTime;
                }
            }
        }

        // Utility            
        static private void UpdateCameraSettings(int playerID)
        {
            PlayerSystem player = Global.Lobby.GetLocalPlayer(playerID);
            #region quit
            if (player == null || !_perPlayerData[playerID]._toggle)
                return;
            #endregion

            // Cache
            Vector3 currentVarious = _perPlayerData[playerID].CurrentVarious;
            CharacterCamera characterCamera = player.ControlledCharacter.CharacterCamera;

            // Overrides
            characterCamera.Offset = _perPlayerData[playerID].CurrentOffset;
            characterCamera.CameraScript.fieldOfView = currentVarious.x;
            characterCamera.DefaultFollowSpeed.SetX(currentVarious.y);
            _perPlayerData[playerID].Sensitivity = currentVarious.z;
        }

        // Hooks
        [HarmonyPatch(typeof(SplitScreenManager), "DelayedRefreshSplitScreen"), HarmonyPostfix]
        static void SplitScreenManager_DelayedRefreshSplitScreen_Post()
        {
            for (int i = 0; i < Global.Lobby.LocalPlayerCount; i++)
            {
                #region quit
                if (!_perPlayerData[i]._toggle)
                    continue;
                #endregion
                UpdateCameraSettings(i);
            }
        }

        [HarmonyPatch(typeof(Character), "SetZoomMode"), HarmonyPostfix]
        static void Character_SetZoomMode_Pre(ref Character __instance, ref bool _zoomed)
        {
            PlayerSystem player = __instance.OwnerPlayerSys;
            #region quit
            if (player == null || !_perPlayerData[player.PlayerID]._toggle)
                return;
            #endregion

            __instance.CharacterCamera.ZoomOffsetSplitScreenH = __instance.CharacterCamera.Offset;
            __instance.CharacterCamera.ZoomSensModifier = 1f;
            _perPlayerData[player.PlayerID].StartAimCoroutine(__instance, _zoomed);
        }

        [HarmonyPatch(typeof(CharacterCamera), "UpdateZoom"), HarmonyPrefix]
        static bool CharacterCamera_UpdateZoom_Pre(ref CharacterCamera __instance)
        {
            PlayerSystem player = __instance.TargetCharacter.OwnerPlayerSys;
            #region quit
            if (player == null || !_perPlayerData[player.PlayerID]._toggle)
                return true;
            #endregion

            return false;
        }

        [HarmonyPatch(typeof(ControlsInput), "RotateCameraVertical"), HarmonyPostfix]
        static void ControlsInput_RotateCameraVertical_Post(int _playerID, ref float __result)
        {
            #region quit
            if (!_perPlayerData[_playerID]._toggle)
                return;
            #endregion
            __result *= _perPlayerData[_playerID].Sensitivity;
        }

        [HarmonyPatch(typeof(ControlsInput), "RotateCameraHorizontal"), HarmonyPostfix]
        static void ControlsInput_RotateCameraHorizontal_Post(int _playerID, ref float __result)
        {
            #region quit
            if (!_perPlayerData[_playerID]._toggle)
                return;
            #endregion
            __result *= _perPlayerData[_playerID].Sensitivity;
        }
    }
}