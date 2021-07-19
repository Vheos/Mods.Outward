namespace Vheos.Mods.Outward
{
    using System;
    using System.Reflection;
    using BepInEx;
    using Vheos.Tools.ModdingCore;
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("io.mefino.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Main : BepInExEntryPoint
    {
        #region SETTINGS
        public const string GUID = "Vheos.Mods.Outward";
        public const string NAME = "Vheos Mod Pack";
        public const string VERSION = "1.13.0";
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

        // User logic
        override public Assembly CurrentAssembly
        => Assembly.GetExecutingAssembly();
        override public void Initialize()
        {
            AMod.SetOrderingList(MODS_ORDERING_LIST);
            Log.Debug("Initializing GameInput...");
            GameInput.Initialize();
            Log.Debug("Initializing Players...");
            Players.Initialize();
        }
        override public void DelayedInitialize()
        {
            Log.Debug("Initializing Prefabs...");
            Prefabs.Initialize();
            Log.Debug("Initializing Presets...");
            Presets.Initialize(_mods);
        }
        override public bool DelayedInitializeCondition
        => ResourcesPrefabManager.Instance.Loaded && UIUtilities.m_instance != null;
        override public Type[] Blacklist
        => new[] { typeof(Debug), typeof(WIP), typeof(PistolTweaks) };
    }
}