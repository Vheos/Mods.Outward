namespace Vheos.Mods.Outward;

public class Traps : AMod
{
    #region Settings
    private static ModSetting<float> _armingDuration;
    private static ModSetting<bool> _friendlyFire;
    private static ModSetting<float> _pressureTrapRadius, _wireTrapDepth, _runicTrapRadius;
    protected override void Initialize()
    {
        _armingDuration = CreateSetting(nameof(_armingDuration), 0f, FloatRange(0f, 10f));
        _friendlyFire = CreateSetting(nameof(_friendlyFire), false);
        _wireTrapDepth = CreateSetting(nameof(_wireTrapDepth), 0.703f, FloatRange(0f, 5f));
        _pressureTrapRadius = CreateSetting(nameof(_pressureTrapRadius), 1.1f, FloatRange(0f, 5f));
        _runicTrapRadius = CreateSetting(nameof(_runicTrapRadius), 2.5f, FloatRange(0f, 5f));

        _armingDuration.AddEvent(() =>
        {
            if (_armingDuration == 0)
                _friendlyFire.Value = false;
        });
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _armingDuration.Value = 5;
                _friendlyFire.Value = false;
                _wireTrapDepth.Value = 0.2f;
                _pressureTrapRadius.Value = 0.6f;
                _runicTrapRadius.Value = 0.8f;
                break;
        }
    }
    #endregion

    #region Formatting
    protected override string SectionOverride
        => ModSections.Combat;
    protected override string Description
        => "• Set a delay before a trap can get triggered" +
        "\n• Allow traps to damage players" +
        "\n• Adjust trigger size for each trap";
    protected override void SetFormatting()
    {
        _armingDuration.Format("Arming duration");
        _armingDuration.Description =
            "How long the trap has to stay on ground before it can get triggered" +
            "\n\nUnit: seconds";
        _friendlyFire.Format("Friendly fire", _armingDuration, t => t > 0);
        _friendlyFire.Description =
            "Allows player traps to damage players";

        CreateHeader("Trigger sizes").Description =
            "Allows you to adjust the size of each trap's trigger";
        using (Indent)
        {
            _wireTrapDepth.Format("tripwire trap");
            _wireTrapDepth.Description =
                "Depth (or thickness) of the tripwire trap" +
                "\n\nUnit: in-game length units";
            _pressureTrapRadius.Format("pressure plate");
            _wireTrapDepth.Description =
                "Radius of the pressure plates" +
                "\n\nUnit: in-game length units";
            _runicTrapRadius.Format("runic trap");
            _wireTrapDepth.Description =
                "Radius of the runic traps" +
                "\n\nUnit: in-game length units";
        }
    }
    #endregion

    #region Utility
    private static void ResetColor(DeployableTrap __instance)
    {
        if (__instance.CurrentTrapType == DeployableTrap.TrapType.Runic)
        {
            ParticleSystem.MainModule particleSystemMain = GetRunicTrapParticleSystemMainModule(__instance);
            particleSystemMain.startColor = RUNIC_TRAP_START_COLOR;
        }
        else
        {
            Material material = GetTrapMainMaterial(__instance);
            material.color = TRAP_START_COLOR;
        }
    }
    private static ParticleSystem.MainModule GetRunicTrapParticleSystemMainModule(DeployableTrap __instance)
        => __instance.CurrentVisual.GetComponentInChildren<ParticleSystem>().main;
    private static Material GetTrapMainMaterial(DeployableTrap __instance)
        => __instance.CurrentVisual.FindChild("TrapVisual").GetComponentInChildren<MeshRenderer>().material;

    private static Color TRAP_START_COLOR = Color.white;
    private static Color TRAP_TRANSITION_COLOR = Color.yellow;
    private static Color TRAP_ARMED_COLOR = Color.red;
    private static Color RUNIC_TRAP_START_COLOR = new(1f, 1f, 1f, 0f);
    private static Color RUNIC_TRAP_TRANSITION_COLOR = new(1f, 1f, 0.05f, 0.05f);
    private static Color RUNIC_TRAP_ARMED_COLOR = new(1f, 0.05f, 0f, 1f);
    #endregion

    #region Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(DeployableTrap), nameof(DeployableTrap.StartInit))]
    private static void DeployableTrap_StartInit_Post(DeployableTrap __instance)
    {
        // Friendly fire
        Character.Factions[] factions = __instance.TargetFactions;
        Character.Factions playerFaction = Character.Factions.Player;
        Character.Factions noneFaction = Character.Factions.NONE;
        if (_friendlyFire && !factions.Contains(playerFaction))
        {
            if (factions.Contains(noneFaction))
                factions[factions.IndexOf(noneFaction)] = playerFaction;
            else
            {
                Array.Resize(ref factions, factions.Length + 1);
                factions.SetLast(playerFaction);
            }
        }
        else if (!_friendlyFire && factions.Contains(playerFaction))
            factions[factions.IndexOf(playerFaction)] = noneFaction;

        // Rune trap only
        if (__instance.CurrentTrapType != DeployableTrap.TrapType.Runic)
            return;

        // Cache
        ParticleSystem.MainModule particleSystemMain = __instance.CurrentVisual.GetComponentInChildren<ParticleSystem>().main;
        SphereCollider collider = __instance.m_interactionToggle.m_interactionCollider as SphereCollider;

        // Disarm
        particleSystemMain.startColor = RUNIC_TRAP_START_COLOR;
        collider.enabled = false;
        collider.radius = _runicTrapRadius;

        // Arm
        float setupTime = Time.time;
        __instance.ExecuteUntil
        (
            () => Time.time - setupTime >= _armingDuration,
            () => particleSystemMain.startColor = Utils.Lerp3(RUNIC_TRAP_START_COLOR, RUNIC_TRAP_TRANSITION_COLOR, RUNIC_TRAP_ARMED_COLOR, (Time.time - setupTime) / _armingDuration),
            () =>
            {
                particleSystemMain.startColor = RUNIC_TRAP_ARMED_COLOR;
                collider.enabled = true;
            }
        );
    }

    [HarmonyPostfix, HarmonyPatch(typeof(DeployableTrap), nameof(DeployableTrap.OnReceiveArmTrap))]
    private static void DeployableTrap_OnReceiveArmTrap_Post(DeployableTrap __instance)
    {
        if (__instance.CurrentTrapType == DeployableTrap.TrapType.Runic)
            return;

        // Cache
        Collider collider = __instance.m_interactionToggle.m_interactionCollider;
        Material material = __instance.CurrentVisual.FindChild("TrapVisual").GetComponentInChildren<MeshRenderer>().material;

        // Disarm
        material.color = TRAP_START_COLOR;
        collider.enabled = false;
        switch (__instance.CurrentTrapType)
        {
            case DeployableTrap.TrapType.TripWireTrap: collider.As<BoxCollider>().SetSizeZ(_wireTrapDepth); break;
            case DeployableTrap.TrapType.PressurePlateTrap: collider.As<SphereCollider>().radius = _pressureTrapRadius; break;
        }

        // Arm
        float setupTime = Time.time;
        __instance.ExecuteUntil
        (
            () => Time.time - setupTime >= _armingDuration,
            () => material.color = Utils.Lerp3(TRAP_START_COLOR, TRAP_TRANSITION_COLOR, TRAP_ARMED_COLOR, (Time.time - setupTime) / _armingDuration),
            () =>
            {
                material.color = TRAP_ARMED_COLOR;
                collider.enabled = true;
            }
        );
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DeployableTrap), nameof(DeployableTrap.CleanUp))]
    private static void DeployableTrap_CleanUp_Pre(DeployableTrap __instance)
        => ResetColor(__instance);

    [HarmonyPrefix, HarmonyPatch(typeof(DeployableTrap), nameof(DeployableTrap.Disassemble))]
    private static void DeployableTrap_Disassemble_Pre(DeployableTrap __instance)
        => ResetColor(__instance);
    #endregion
}
