namespace Vheos.Mods.Outward;

public class Traps : AMod
{
    #region const
    private static Color TRAP_START_COLOR = Color.white;
    private static Color TRAP_TRANSITION_COLOR = Color.yellow;
    private static Color TRAP_ARMED_COLOR = Color.red;
    private static Color RUNIC_TRAP_START_COLOR = new(1f, 1f, 1f, 0f);
    private static Color RUNIC_TRAP_TRANSITION_COLOR = new(1f, 1f, 0.05f, 0.05f);
    private static Color RUNIC_TRAP_ARMED_COLOR = new(1f, 0.05f, 0f, 1f);
    #endregion

    // Settings
    private static ModSetting<float> _trapsArmDelay;
    private static ModSetting<bool> _trapsFriendlyFire;
    private static ModSetting<float> _pressureTrapRadius, _wireTrapDepth, _runicTrapRadius;
    protected override void Initialize()
    {

        _trapsArmDelay = CreateSetting(nameof(_trapsArmDelay), 0f, FloatRange(0f, 5f));
        _trapsFriendlyFire = CreateSetting(nameof(_trapsFriendlyFire), false);
        _wireTrapDepth = CreateSetting(nameof(_wireTrapDepth), 0.703f, FloatRange(0f, 5f));
        _pressureTrapRadius = CreateSetting(nameof(_pressureTrapRadius), 1.1f, FloatRange(0f, 5f));
        _runicTrapRadius = CreateSetting(nameof(_runicTrapRadius), 2.5f, FloatRange(0f, 5f));

        _trapsArmDelay.AddEvent(() =>
        {
            if (_trapsArmDelay == 0)
                _trapsFriendlyFire.Value = false;
        });
    }
    protected override void SetFormatting()
    {
        _trapsArmDelay.Format("Traps arming delay");
        _trapsArmDelay.Description = "How long the trap has to stay on ground before it can explode (in seconds)";
        using (Indent)
        {
            _trapsFriendlyFire.Format("Friendly fire", _trapsArmDelay, t => t > 0);
            _trapsFriendlyFire.Description = "The trap will also explode in contact with you and other players";
        }
        _wireTrapDepth.Format("Tripwire trap trigger depth");
        _pressureTrapRadius.Format("Presure plate trigger radius");
        _runicTrapRadius.Format("Runic trap trigger radius");
    }
    protected override string Description
    => "• Set a delay before a trap can explode\n" +
       "• Make traps explode in contact with players\n" +
       "• Change trigger size for each trap";
    protected override string SectionOverride
    => ModSections.Combat;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _trapsArmDelay.Value = 5;
                _trapsFriendlyFire.Value = false;
                _wireTrapDepth.Value = 0.2f;
                _pressureTrapRadius.Value = 0.6f;
                _runicTrapRadius.Value = 0.8f;
                break;
        }
    }

    // Utility
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

    // Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(DeployableTrap), nameof(DeployableTrap.StartInit))]
    private static void DeployableTrap_StartInit_Post(DeployableTrap __instance)
    {
        // Friendly fire
        Character.Factions[] factions = __instance.TargetFactions;
        Character.Factions playerFaction = Character.Factions.Player;
        Character.Factions noneFaction = Character.Factions.NONE;
        if (_trapsFriendlyFire && !factions.Contains(playerFaction))
        {
            if (factions.Contains(noneFaction))
                factions[factions.IndexOf(noneFaction)] = playerFaction;
            else
            {
                Array.Resize(ref factions, factions.Length + 1);
                factions.SetLast(playerFaction);
            }
        }
        else if (!_trapsFriendlyFire && factions.Contains(playerFaction))
            factions[factions.IndexOf(playerFaction)] = noneFaction;

        // Rune trap only
        #region quit
        if (__instance.CurrentTrapType != DeployableTrap.TrapType.Runic)
            return;
        #endregion

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
            () => Time.time - setupTime >= _trapsArmDelay,
            () => particleSystemMain.startColor = Utils.Lerp3(RUNIC_TRAP_START_COLOR, RUNIC_TRAP_TRANSITION_COLOR, RUNIC_TRAP_ARMED_COLOR, (Time.time - setupTime) / _trapsArmDelay),
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
        #region quit
        if (__instance.CurrentTrapType == DeployableTrap.TrapType.Runic)
            return;
        #endregion

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
            () => Time.time - setupTime >= _trapsArmDelay,
            () => material.color = Utils.Lerp3(TRAP_START_COLOR, TRAP_TRANSITION_COLOR, TRAP_ARMED_COLOR, (Time.time - setupTime) / _trapsArmDelay),
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
}
