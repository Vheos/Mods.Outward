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
        public const string VERSION = "1.1.1";

        // Utility
        private List<Type> _awakeMods;
        private List<Type> _prefabMods;
        private List<IUpdatable> _updatableMods;
        private void CategorizeModsByInstantiationTime(params Type[] whitelist)
        {
            foreach (var modType in Utility.GetDerivedTypes<AMod>())
                if (modType.IsNotAssignableTo<IExcludeFromBuild>())
                    if (whitelist.Length == 0 || modType.IsContainedIn(whitelist))
                    {
                        if (modType.IsAssignableTo<IWaitForPrefabs>())
                            _prefabMods.Add(modType);
                        else
                            _awakeMods.Add(modType);
                    }
        }
        private void TryInitializePrefabs()
        {
            if (!Prefabs.IsInitialized && ResourcesPrefabManager.Instance.Loaded)
            {
                Prefabs.Initialize();
                InstantiateMods(_prefabMods);
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
        => updatableMod.OnUpdate();
        private void UpdateMods(ICollection<IUpdatable> updatableMods)
        {
            foreach (var updatableMod in updatableMods)
                UpdateMod(updatableMod);
        }

        // Mono
        private void Awake()
        {
            _awakeMods = new List<Type>();
            _prefabMods = new List<Type>();
            _updatableMods = new List<IUpdatable>();
            Tools.Initialize(this, Logger);
            GameInput.Initialize();
            CategorizeModsByInstantiationTime();
            InstantiateMods(_awakeMods);
        }
        private void Update()
        {
            TryInitializePrefabs();
            UpdateMods(_updatableMods);
            Tools.TryRedrawConfigWindow();
        }
    }
}