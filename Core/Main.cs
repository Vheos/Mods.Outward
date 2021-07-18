using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using Vheos.Tools.ModdingCore;
using BepInEx;
using Vheos.Tools.Extensions.General;
using Vheos.Tools.Extensions.Collections;



namespace ModPack
{
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("io.mefino.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Main : BaseUnityPlugin
    {
        #region SETTINGS
        public const bool IS_DEVELOPMENT_VERSION = false;
        public const string GUID = "Vheos.Mods.Outward";
        public const string NAME = "Vheos Mod Pack" + (IS_DEVELOPMENT_VERSION ? " [DEVELOPMENT]" : "");
        public const string VERSION = "1.13.0";
        static public Type[] BLACKLIST = { typeof(Debug), typeof(WIP), typeof(PistolTweaks) };
        static private readonly Type[] MODS_ORDERING_LIST = new[]
        {
            // Survival & Immersion
            typeof(Needs),
            typeof(Camping),
            typeof(Crafting),
            typeof(Durability),
            typeof(Merchants),
            typeof(Inns),
            typeof(SurvivalTools),
            typeof(Resets),
            typeof(Interactions),
            typeof(Revive),

            // Combat
            typeof(Damage),
            typeof(Speed),
            typeof(Targeting),
            typeof(AI),
            typeof(Traps),
            typeof(Quickslots),

            // Skills
            typeof(SkillEditor),
            typeof(SkillPrices),
            typeof(SkillLimits),
            typeof(SkillTreeRandomizer),

            // UI
            typeof(GUI),
            typeof(Descriptions),
            typeof(Camera),
            typeof(KeyboardWalk),
            typeof(Gamepad),

            // Various
            typeof(Various),

            // Development
            typeof(Debug),
            typeof(WIP),
            typeof(PistolTweaks),
        };
        #endregion

        // Utility
        private List<Type> _awakeModTypes;
        private List<Type> _delayedModTypes;
        private List<IUpdatable> _updatableMods;
        private List<AMod> _mods;
        private void CategorizeModsByInstantiationTime(Type[] whitelist = null, Type[] blacklist = null)
        {
            foreach (var modType in Utility.GetDerivedTypes<AMod>())
                if ((blacklist.IsNullOrEmpty() || modType.IsNotContainedIn(blacklist))
                && (whitelist.IsNullOrEmpty() || modType.IsContainedIn(whitelist)))
                    if (modType.IsAssignableTo<IDelayedInit>())
                        _delayedModTypes.Add(modType);
                    else
                        _awakeModTypes.Add(modType);
        }
        private void TryDelayedInitialize()
        {
            if (Prefabs.IsInitialized || !IsGameInitialized)
                return;

            Tools.Log($"Finished waiting");
            Tools.Log("");

            Tools.Log("Initializing prefabs...");
            Prefabs.Initialize();
            Tools.Log("Instantiating delayed mods...");
            InstantiateMods(_delayedModTypes);

            Tools.Log("Initializing Presets...");
            Presets.Initialize(_mods);

            Tools.Log($"Finished DelayedInit");
        }
        private void InstantiateMods(ICollection<Type> modTypes)
        {
            foreach (var modType in modTypes)
            {
                AMod newMod = (AMod)Activator.CreateInstance(modType);
                _mods.Add(newMod);
                if (modType.IsAssignableTo<IUpdatable>())
                    _updatableMods.Add(newMod as IUpdatable);
            }
        }
        private void UpdateMods(ICollection<IUpdatable> updatableMods)
        {
            foreach (var updatableMod in updatableMods)
                if (updatableMod.IsEnabled)
                    updatableMod.OnUpdate();
        }
        private bool IsGameInitialized
        => ResourcesPrefabManager.Instance.Loaded && UIUtilities.m_instance != null;

        // Mono
#pragma warning disable IDE0051 // Remove unused private members
        private void Awake()
        {
            _awakeModTypes = new List<Type>();
            _delayedModTypes = new List<Type>();
            _updatableMods = new List<IUpdatable>();
            _mods = new List<AMod>();

            AMod.SetOrderingList(MODS_ORDERING_LIST);
            Tools.Initialize(this, Logger);

            Tools.Log("Initializing GameInput...");
            GameInput.Initialize();
            Tools.Log("Initializing Players...");
            Players.Initialize();

            Tools.Log("Categorizing mods by instantiation time...");
            CategorizeModsByInstantiationTime(null, BLACKLIST);
            Tools.Log("Awake:");
            foreach (var modType in _awakeModTypes)
                Tools.Log($"\t{modType.Name}");
            Tools.Log("Delayed:");
            foreach (var modType in _delayedModTypes)
                Tools.Log($"\t{modType.Name}");

            Tools.Log("Instantiating awake mods...");
            InstantiateMods(_awakeModTypes);

            Tools.Log($"Finished AwakeInit");
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