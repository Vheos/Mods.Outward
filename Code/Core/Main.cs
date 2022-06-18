namespace Vheos.Mods.Outward;
using BepInEx;
using System.Reflection;
using Utility = Vheos.Helpers.Common.Utility;

[BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("io.mefino.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(GUID, NAME, VERSION)]
public class Main : BepInExEntryPoint
{
    // Metadata
    public const string GUID = "Vheos.Mods.Outward";
    public const string NAME = "VMP";
    public const string VERSION = "2.0.7";

    // User logic
    protected override Assembly CurrentAssembly
    => Assembly.GetExecutingAssembly();
    protected override void Initialize()
    {
        Log.Debug("Initializing GameInput...");
        GameInput.Initialize();
        Log.Debug("Initializing Players...");
        Players.Initialize();
    }
    protected override void DelayedInitialize()
    {
        Log.Debug("Initializing Prefabs...");
        Prefabs.Initialize();
    }
    protected override bool DelayedInitializeCondition
    => ResourcesPrefabManager.Instance.Loaded && UIUtilities.m_instance != null;
    protected override string[] PresetNames
    => Utility.GetEnumValuesAsStrings<Preset>().ToArray();
    protected override Type[] Blacklist => new[]
    {
        typeof(Debug),
        typeof(WIP),
        typeof(PistolTweaks)
    };
    protected override Type[] ModsOrderingList => new[]
    {
        // Survival & Immersion
        typeof(Needs),
        typeof(Camping),
        typeof(Crafting),
        typeof(Durability),
        typeof(Merchants),
        typeof(Stashes),
        typeof(SurvivalTools),
        typeof(Resets),
        typeof(Interactions),
        typeof(Revive),

        // Combat
        typeof(Damage),
        typeof(Speed),
        typeof(Dodge),
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
        typeof(VariousDelayed),

        // Development
        typeof(Debug),
        typeof(WIP),
        typeof(PistolTweaks),
    };
}
