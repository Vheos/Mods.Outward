using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using System;
using UnityEngine;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Diagnostics;



namespace ModPack
{
    static public class Tools
    {
        // Publics
        static public void Log(object text, LogLevel logLevel = LogLevel.Message)
        => _logger.Log(logLevel, text);
        static public ConfigFile ConfigFile
        => _configFile;
        static public void SetDirtyConfigWindow()
        => _isConfigWindowDirty = true;
        static public void TryRedrawConfigWindow()
        {
            if (IsConfigOpen && _isConfigWindowDirty)
            {
                _configManager.BuildSettingList();
                _isConfigWindowDirty = false;
            }
        }
        static public void AddEventOnConfigOpened(Action action)
        {
            _configManager.DisplayingWindowChanged += (sender, eventArgs) =>
            {
                if (eventArgs.NewValue)
                    action();
            };
        }
        static public void AddEventOnConfigClosed(Action action)
        {
            _configManager.DisplayingWindowChanged += (sender, eventArgs) =>
            {
                if (!eventArgs.NewValue)
                    action();
            };
        }
        static public bool IsConfigOpen
        {
            get => _configManager.DisplayingWindow;
            set => _configManager.DisplayingWindow = value;
        }
        static public bool IsStopwatchActive
        {
            get => _stopwatch.IsRunning;
            set
            {
                if (value == _stopwatch.IsRunning)
                    return;

                if (value)
                    _stopwatch.Restart();
                else
                    _stopwatch.Stop();
            }
        }
        static public int ElapsedMilliseconds
        {
            get
            {
                int elapsed = (int)_stopwatch.ElapsedMilliseconds;
                if (_stopwatch.IsRunning)
                    _stopwatch.Restart();
                return elapsed;
            }
        }

        // Privates
        static private Stopwatch _stopwatch;
        static private ManualLogSource _logger;
        static private ConfigFile _configFile;
        static private ConfigurationManager.ConfigurationManager _configManager;
        static private bool _isConfigWindowDirty;
        static private BepInPlugin _plugin;
        static private ModSetting<bool> _alwaysExpanded;

        // Initializers
        static public void Initialize(BaseUnityPlugin pluginComponent, ManualLogSource logger)
        {
            _stopwatch = new Stopwatch();
            _logger = logger;
            _configFile = pluginComponent.Config;
            _configManager = pluginComponent.GetComponent<ConfigurationManager.ConfigurationManager>();
            _plugin = pluginComponent.Info.Metadata;

            CreateAlwaysExpandedToggle();
            Harmony.CreateAndPatchAll(typeof(Tools));
        }
        static private void CreateAlwaysExpandedToggle()
        {
            _alwaysExpanded = new ModSetting<bool>("", nameof(_alwaysExpanded), true);
            _alwaysExpanded.Format("Always expanded");
            _alwaysExpanded.Description = "\"Vheos Mod Pack\" plugin will always be expanded, even if you choose to collapse all plugins." +
                                          "This prevents the plugin from collapsing when changing settings while default collapsing in enabled";
            _alwaysExpanded.IsAdvanced = true;
        }

        // Hooks
        [HarmonyPatch(typeof(ConfigurationManager.ConfigurationManager), "DrawSinglePlugin"), HarmonyPrefix]
        static bool ConfigurationManager_DrawSinglePlugin_Pre(ref ConfigurationManager.ConfigurationManager __instance, ref ConfigurationManager.ConfigurationManager.PluginSettingsData plugin)
        {
            #region quit
            if (!_alwaysExpanded)
                return true;
            #endregion

            if (plugin.Info == _plugin)
                plugin.Collapsed = false;
            return true;
        }
    }
}