using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System;
using BepInEx.Configuration;



namespace ModPack
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Main : BaseUnityPlugin
    {
        // Settings
        public const string GUID = "com.Vheos.ModPack";
        public const string NAME = "Vheos Mod Pack";
        public const string VERSION = "1.2.1";

        // Utility
        private List<Type> _awakeMods;
        private List<Type> _delayedMods;
        private List<IUpdatable> _updatableMods;
        private void CategorizeModsByInstantiationTime(params Type[] whitelist)
        {
            foreach (var modType in Utility.GetDerivedTypes<AMod>())
                if (modType.IsNotAssignableTo<IExcludeFromBuild>())
                    if (whitelist.Length == 0 || modType.IsContainedIn(whitelist))
                    {
                        if (modType.IsAssignableTo<IDelayedInit>())
                            _delayedMods.Add(modType);
                        else
                            _awakeMods.Add(modType);
                    }
        }
        private void TryDelayedInitialize()
        {
            if (!Prefabs.IsInitialized
            && ResourcesPrefabManager.Instance.Loaded
            && SplitScreenManager.Instance != null)
            {
                Prefabs.Initialize();
                InstantiateMods(_delayedMods);
            }
        }
        private void InstantiateMod(Type modType)
        {
            AMod newMod = (AMod)Activator.CreateInstance(modType);
            if (modType.IsAssignableTo<IUpdatable>())
                _updatableMods.Add(newMod as IUpdatable);
        }
        private void InstantiateMods(ICollection<Type> modTypes)
        {
            foreach (var modType in modTypes)
                InstantiateMod(modType);
        }
        private void UpdateMod(IUpdatable updatableMod)
        {
            if (updatableMod.IsEnabled)
                updatableMod.OnUpdate();
        }
        private void UpdateMods(ICollection<IUpdatable> updatableMods)
        {
            foreach (var updatableMod in updatableMods)
                UpdateMod(updatableMod);
        }

        // Mono
        private void Awake()
        {
            _awakeMods = new List<Type>();
            _delayedMods = new List<Type>();
            _updatableMods = new List<IUpdatable>();

            Tools.Initialize(this, Logger);
            GameInput.Initialize();
            Players.Initialize();

            CategorizeModsByInstantiationTime();
            InstantiateMods(_awakeMods);
        }
        private void Update()
        {
            TryDelayedInitialize();
            UpdateMods(_updatableMods);
            Tools.TryRedrawConfigWindow();
        }
    }
}