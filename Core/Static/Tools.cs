using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using BepInEx;
using BepInEx.Logging;
using System.Diagnostics;



namespace ModPack
{
    static public class Tools
    {
        // Publics
        static public void Log(object text)
        => _logger.Log(Main.IS_DEVELOPMENT_VERSION ? LogLevel.Message : LogLevel.Debug, text);
        static public ConfigFile ConfigFile
        { get; private set; }
        static public BaseUnityPlugin PluginComponent
        { get; private set; }

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
        static private ConfigurationManager.ConfigurationManager _configManager;
        static private bool _isConfigWindowDirty;

        // Initializers
        static public void Initialize(BaseUnityPlugin pluginComponent, ManualLogSource logger)
        {
            _stopwatch = new Stopwatch();
            _logger = logger;
            PluginComponent = pluginComponent;
            ConfigFile = PluginComponent.Config;
            _configManager = PluginComponent.GetComponent<ConfigurationManager.ConfigurationManager>();
        }
    }
}