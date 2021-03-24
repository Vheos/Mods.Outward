using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System;



namespace ModPack
{
    public class Camera : AMod, IWaitForPrefabs, IUpdatable
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
        private class CameraSettings
        {
            // Settings
            public ModSetting<bool> Toggle;
            public ModSetting<float> ZoomControlAmount;
            public ModSetting<float> ZoomControlSpeed;
            public ModSetting<bool> OffsetToggle, VariousToggle;
            public ModSetting<Vector3> OffsetMin, OffsetAvg, OffsetMax;
            public ModSetting<Vector3> VariousMin, VariousAvg, VariousMax;

            // Utility
            public void StartAimCoroutine(MonoBehaviour owner, bool isEnteringAimMode)
            {
                // Choose zoom in or out
                float targetZoom = 0f;
                Func<bool> test = () => ZoomControlAmount.Value < targetZoom + AIM_COROUTINE_MARGIN;
                if (isEnteringAimMode)
                {
                    targetZoom = 1f;
                    test = () => ZoomControlAmount.Value > targetZoom - AIM_COROUTINE_MARGIN;
                }

                // Update over time
                if (_aimCoroutine != null)
                    owner.StopCoroutine(_aimCoroutine);
                _aimCoroutine = owner.ExecuteUntil
                (
                    test,
                    () => ZoomControlAmount.Value = ZoomControlAmount.Value.Lerp(targetZoom, AIM_COROUTINE_UPDATE_SPEED),
                    () => ZoomControlAmount.Value = targetZoom
                );
            }
            public Vector3 CurrentOffset
            {
                get
                {
                    if (ZoomControlAmount < 0f)
                        return Vector3.LerpUnclamped(OffsetMin, OffsetAvg, ZoomControlAmount + 1f);
                    if (ZoomControlAmount > 0f)
                        return Vector3.LerpUnclamped(OffsetAvg, OffsetMax, ZoomControlAmount);
                    return OffsetAvg;
                }
            }
            public Vector3 CurrentVarious
            {
                get
                {
                    if (ZoomControlAmount < 0f)
                        return Vector3.LerpUnclamped(VariousMin, VariousAvg, ZoomControlAmount + 1f);
                    if (ZoomControlAmount > 0f)
                        return Vector3.LerpUnclamped(VariousAvg, VariousMax, ZoomControlAmount);
                    return VariousAvg;
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

        static private CameraSettings[] _settingsByPlayerID;
        override protected void Initialize()
        {
            _settingsByPlayerID = new CameraSettings[2];
            for (int i = 0; i < 2; i++)
            {
                CameraSettings tmp = new CameraSettings();
                _settingsByPlayerID[i] = tmp;

                string playerPrefix = $"Player{i + 1} ";
                tmp.Toggle = CreateSetting(playerPrefix + nameof(tmp.Toggle), false);
                tmp.ZoomControlAmount = CreateSetting(playerPrefix + nameof(tmp.ZoomControlAmount), 0f, FloatRange(-1f, 1f));
                tmp.ZoomControlSpeed = CreateSetting(playerPrefix + nameof(tmp.ZoomControlSpeed), 0.5f, FloatRange(0f, 1f));

                tmp.OffsetToggle = CreateSetting(playerPrefix + nameof(tmp.OffsetToggle), false);
                tmp.OffsetMin = CreateSetting(playerPrefix + nameof(tmp.OffsetMin), DEFAULT_OFFSET.Add(0, 1f, -3f));
                tmp.OffsetAvg = CreateSetting(playerPrefix + nameof(tmp.OffsetAvg), DEFAULT_OFFSET);
                tmp.OffsetMax = CreateSetting(playerPrefix + nameof(tmp.OffsetMax), DEFAULT_OFFSET.Add(0.75f, -0.25f, 1f));

                Vector3 otherDefault = new Vector3(DEFAULT_FOV, DEFAULT_FOLLOW_SPEED, 1f);
                tmp.VariousToggle = CreateSetting(playerPrefix + nameof(tmp.VariousToggle), false);
                tmp.VariousMin = CreateSetting(playerPrefix + nameof(tmp.VariousMin), otherDefault.Mul(1.2f, 2 / 3f, 1f));
                tmp.VariousAvg = CreateSetting(playerPrefix + nameof(tmp.VariousAvg), otherDefault);
                tmp.VariousMax = CreateSetting(playerPrefix + nameof(tmp.VariousMax), otherDefault.Mul(0.8f, 2.0f, 0.75f));

                int id = i;
                tmp.ZoomControlAmount.AddEvent(() => UpdateCameraSettings(id));

                AddEventOnConfigClosed(() =>
                {
                    foreach (var localPlayer in GameInput.LocalPlayers)
                        UpdateCameraSettings(localPlayer.PlayerID);
                });

                tmp.Sensitivity = 1f;
            }
        }
        override protected void SetFormatting()
        {
            for (int i = 0; i < 2; i++)
            {
                CameraSettings tmp = _settingsByPlayerID[i];

                // Settings
                tmp.Toggle.Format($"Player {i + 1}");
                tmp.Toggle.Description = $"Change settings for local player {i + 1}";
                Indent++;
                {
                    tmp.ZoomControlAmount.Format("Zoom amount", tmp.Toggle);
                    tmp.ZoomControlAmount.Description = "-1  -  max zoom out\n" +
                                                        "0  -  default zoom\n" +
                                                        "+1  -  max zoom in";
                    tmp.ZoomControlSpeed.Format("Zoom control speed", tmp.Toggle);
                    tmp.ZoomControlSpeed.Description = "How quickly you want to zoom using mouse/gamepad\n" +
                        "Mouse: use mouse scroll wheel\n" +
                        "Gamepad: use right stick while holding Sprint and Block buttons";
                    tmp.OffsetToggle.Format("Offset", tmp.Toggle);
                    tmp.OffsetToggle.Description = "Change camera position (XYZ) presets";
                    Indent++;
                    {
                        tmp.OffsetMin.Format("at zoom =  -1", tmp.OffsetToggle);
                        tmp.OffsetAvg.Format("at zoom =  0", tmp.OffsetToggle);
                        tmp.OffsetMax.Format("at zoom = +1", tmp.OffsetToggle);
                        Indent--;
                    }

                    tmp.VariousToggle.Format("FOV, FollowSpeed, Sensitivity", tmp.Toggle);
                    tmp.VariousToggle.Description = "Change other settings presets:\n" +
                        "X  -  field of view (vertical)\n" +
                        "Y  -  how quickly camera follows your character\n" +
                        "Z  -  how quickly camera rotates";
                    Indent++;
                    {
                        tmp.VariousMin.Format("at zoom =  -1", tmp.VariousToggle);
                        tmp.VariousAvg.Format("at zoom =  0", tmp.VariousToggle);
                        tmp.VariousMax.Format("at zoom = +1", tmp.VariousToggle);
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
                if (localPlayer.ControlledCharacter.CharacterUI.IsMenuFocused)
                    continue;

                // Cache
                int id = localPlayer.PlayerID;
                CameraSettings settings = _settingsByPlayerID[id];

                settings.IgnoreAxes = false;
                if (settings.ZoomControlSpeed > 0)
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
                        settings.ZoomControlAmount.Value += zoomDelta * settings.ZoomControlSpeed * 10f * Time.unscaledDeltaTime;
                }
            }
        }

        // Utility            
        static private void UpdateCameraSettings(int playerID)
        {
            PlayerSystem player = Global.Lobby.GetLocalPlayer(playerID);
            #region quit
            if (player == null || !_settingsByPlayerID[playerID].Toggle)
                return;
            #endregion

            // Cache
            Vector3 currentVarious = _settingsByPlayerID[playerID].CurrentVarious;
            CharacterCamera characterCamera = player.ControlledCharacter.CharacterCamera;

            // Overrides
            characterCamera.Offset = _settingsByPlayerID[playerID].CurrentOffset;
            characterCamera.CameraScript.fieldOfView = currentVarious.x;
            characterCamera.DefaultFollowSpeed.SetX(currentVarious.y);
            _settingsByPlayerID[playerID].Sensitivity = currentVarious.z;
        }

        // Hooks
        [HarmonyPatch(typeof(SplitScreenManager), "DelayedRefreshSplitScreen"), HarmonyPostfix]
        static void SplitScreenManager_DelayedRefreshSplitScreen_Post()
        {
            for (int i = 0; i < Global.Lobby.LocalPlayerCount; i++)
            {
                #region quit
                if (!_settingsByPlayerID[i].Toggle)
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
            if (player == null || !_settingsByPlayerID[player.PlayerID].Toggle)
                return;
            #endregion

            __instance.CharacterCamera.ZoomOffsetSplitScreenH = __instance.CharacterCamera.Offset;
            __instance.CharacterCamera.ZoomSensModifier = 1f;
            _settingsByPlayerID[player.PlayerID].StartAimCoroutine(__instance, _zoomed);
        }

        [HarmonyPatch(typeof(CharacterCamera), "UpdateZoom"), HarmonyPrefix]
        static bool CharacterCamera_UpdateZoom_Pre(ref CharacterCamera __instance)
        {
            PlayerSystem player = __instance.TargetCharacter.OwnerPlayerSys;
            #region quit
            if (player == null || !_settingsByPlayerID[player.PlayerID].Toggle)
                return true;
            #endregion

            return false;
        }

        [HarmonyPatch(typeof(ControlsInput), "RotateCameraVertical"), HarmonyPostfix]
        static void ControlsInput_RotateCameraVertical_Post(int _playerID, ref float __result)
        {
            #region quit
            if (!_settingsByPlayerID[_playerID].Toggle)
                return;
            #endregion
            __result *= _settingsByPlayerID[_playerID].Sensitivity;
        }

        [HarmonyPatch(typeof(ControlsInput), "RotateCameraHorizontal"), HarmonyPostfix]
        static void ControlsInput_RotateCameraHorizontal_Post(int _playerID, ref float __result)
        {
            #region quit
            if (!_settingsByPlayerID[_playerID].Toggle)
                return;
            #endregion
            __result *= _settingsByPlayerID[_playerID].Sensitivity;
        }
    }
}