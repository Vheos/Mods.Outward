using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using BepInEx;



namespace ModPack
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Main : BaseUnityPlugin
    {
        // Settings
        public const bool IS_DEVELOPMENT_VERSION = true;
        public const string GUID = "com.Vheos.ModPack";
        public const string NAME = "Vheos Mod Pack" + (IS_DEVELOPMENT_VERSION ? " [DEVELOPMENT]" : "");
        public const string VERSION = "1.8.0";

        // Utility
        private List<Type> _awakeModTypes;
        private List<Type> _delayedModTypes;
        private List<IUpdatable> _updatableMods;
        private void CategorizeModsByInstantiationTime(Type[] whitelist = null, Type[] blacklist = null)
        {
            foreach (var modType in Utility.GetDerivedTypes<AMod>())
                if ((IS_DEVELOPMENT_VERSION || modType.IsNotAssignableTo<IDevelopmentOnly>())
                && (blacklist.IsEmpty() || modType.IsNotContainedIn(blacklist))
                && (whitelist.IsEmpty() || modType.IsContainedIn(whitelist)))
                    if (modType.IsAssignableTo<IDelayedInit>())
                        _delayedModTypes.Add(modType);
                    else
                        _awakeModTypes.Add(modType);
        }
        private void TryDelayedInitialize()
        {
            if (Prefabs.IsInitialized || !IsGameInitialized)
                return;
     
            Tools.Log($"Finished waiting ({Tools.ElapsedMilliseconds}ms)");
            Tools.Log("");

            Tools.Log("Initializing prefabs...");
            Prefabs.Initialize();
            Tools.Log("Instantiating delayed mods...");
            InstantiateMods(_delayedModTypes);
            Tools.Log($"Finished DelayedInit ({Tools.ElapsedMilliseconds}ms)");
            Tools.Log("");
            Tools.IsStopwatchActive = false;
        }
        private void InstantiateMods(ICollection<Type> modTypes)
        {
            foreach (var modType in modTypes)
                InstantiateMod(modType);
        }
        private void InstantiateMod(Type modType)
        {
            AMod newMod = (AMod)Activator.CreateInstance(modType);
            if (modType.IsAssignableTo<IUpdatable>())
                _updatableMods.Add(newMod as IUpdatable);
        }
        private void UpdateMods(ICollection<IUpdatable> updatableMods)
        {
            foreach (var updatableMod in updatableMods)
                UpdateMod(updatableMod);
        }
        private void UpdateMod(IUpdatable updatableMod)
        {
            if (updatableMod.IsEnabled)
                updatableMod.OnUpdate();
        }
        private bool IsGameInitialized
        => SplitScreenManager.Instance != null
        && ResourcesPrefabManager.Instance.Loaded
        && ItemManager.m_prefabLoaded
        && ItemManager.m_recipeLoaded
        && ItemManager.m_diseaseLoaded;

        // Mono
#pragma warning disable IDE0051 // Remove unused private members
        private void Awake()
        {
            _awakeModTypes = new List<Type>();
            _delayedModTypes = new List<Type>();
            _updatableMods = new List<IUpdatable>();

            Tools.Initialize(this, Logger);
            Tools.IsStopwatchActive = true;

            Tools.Log("Initializing GameInput...");
            GameInput.Initialize();
            Tools.Log("Initializing Players...");
            Players.Initialize();

            Tools.Log("Categorizing mods by instantiation time...");
            CategorizeModsByInstantiationTime();
            Tools.Log("Awake:");
            foreach (var modType in _awakeModTypes)
                Tools.Log($"\t{modType.Name}");
            Tools.Log("Delayed:");
            foreach (var modType in _delayedModTypes)
                Tools.Log($"\t{modType.Name}");

            Tools.Log("Instantiating awake mods...");
            InstantiateMods(_awakeModTypes);

            Tools.Log($"Finished AwakeInit ({Tools.ElapsedMilliseconds}ms)");
            Tools.Log("");

            Tools.Log($"Waiting for game initialization...");
        }
        private void Update()
        {
            TryDelayedInitialize();
            UpdateMods(_updatableMods);
            Tools.TryRedrawConfigWindow();
        }
    }
}