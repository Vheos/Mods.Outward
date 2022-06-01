namespace Vheos.Mods.Outward;
using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using Mods.Core;
using Utility = Tools.Utilities.Utility;

[BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("io.mefino.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(GUID, NAME, VERSION)]
public class Main : BepInExEntryPoint
{
    // Metadata
    public const string GUID = "Vheos.Mods.Outward";
    public const string NAME = "Vheos Mod Pack";
    public const string VERSION = "2.0.0";

    // User logic
    override protected Assembly CurrentAssembly
    => Assembly.GetExecutingAssembly();
    override protected void Initialize()
    {
        Log.Debug("Initializing GameInput...");
        GameInput.Initialize();
        Log.Debug("Initializing Players...");
        Players.Initialize();
    }
    override protected void DelayedInitialize()
    {
        Log.Debug("Initializing Prefabs...");
        Prefabs.Initialize();
    }
    override protected bool DelayedInitializeCondition
    => ResourcesPrefabManager.Instance.Loaded && UIUtilities.m_instance != null;
    override protected string[] PresetNames
    => Utility.GetEnumValuesAsStrings<Preset>().ToArray();
    override protected Type[] Blacklist => new[]
    {
        typeof(Debug),
        typeof(WIP),
        typeof(PistolTweaks)
    };
    override protected Type[] ModsOrderingList => new[]
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
}
