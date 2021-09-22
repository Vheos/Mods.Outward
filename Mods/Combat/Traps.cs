namespace Vheos.Mods.Outward
{
    using System;
    using UnityEngine;
    using HarmonyLib;
    using Tools.ModdingCore;
    using Tools.Extensions.UnityObjects;
    using Tools.Extensions.Collections;
    using Tools.Extensions.General;
    public class Traps : AMod
    {
        #region const
        static private Color TRAP_START_COLOR = Color.white;
        static private Color TRAP_TRANSITION_COLOR = Color.yellow;
        static private Color TRAP_ARMED_COLOR = Color.red;
        static private Color RUNIC_TRAP_START_COLOR = new Color(1f, 1f, 1f, 0f);
        static private Color RUNIC_TRAP_TRANSITION_COLOR = new Color(1f, 1f, 0.05f, 0.05f);
        static private Color RUNIC_TRAP_ARMED_COLOR = new Color(1f, 0.05f, 0f, 1f);
        #endregion

        // Settings
        static private ModSetting<float> _trapsArmDelay;
        static private ModSetting<bool> _trapsFriendlyFire;
        static private ModSetting<float> _pressureTrapRadius, _wireTrapDepth, _runicTrapRadius;
        override protected void Initialize()
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
        override protected void SetFormatting()
        {
            _trapsArmDelay.Format("Traps arming delay");
            _trapsArmDelay.Description = "How long the trap has to stay on ground before it can explode (in seconds)";
            Indent++;
            {
                _trapsFriendlyFire.Format("Friendly fire", _trapsArmDelay, () => _trapsArmDelay > 0);
                _trapsFriendlyFire.Description = "The trap will also explode in contact with you and other players";
                Indent--;
            }
            _wireTrapDepth.Format("Tripwire trap trigger depth");
            _pressureTrapRadius.Format("Presure plate trigger radius");
            _runicTrapRadius.Format("Runic trap trigger radius");
        }
        override protected string Description
        => "• Set a delay before a trap can explode\n" +
           "• Make traps explode in contact with players\n" +
           "• Change trigger size for each trap";
        override protected string SectionOverride
        => ModSections.Combat;
        override public void LoadPreset(int preset)
        {
            switch ((Presets.Preset)preset)
            {
                case Presets.Preset.Vheos_CoopSurvival:
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
        static private void ResetColor(DeployableTrap __instance)
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
        static private ParticleSystem.MainModule GetRunicTrapParticleSystemMainModule(DeployableTrap __instance)
        => __instance.CurrentVisual.GetComponentInChildren<ParticleSystem>().main;
        static private Material GetTrapMainMaterial(DeployableTrap __instance)
        => __instance.CurrentVisual.FindChild("TrapVisual").GetComponentInChildren<MeshRenderer>().material;

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        [HarmonyPatch(typeof(DeployableTrap), "StartInit"), HarmonyPostfix]
        static void DeployableTrap_StartInit_Post(DeployableTrap __instance)
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
                () => particleSystemMain.startColor = InternalUtility.Lerp3(RUNIC_TRAP_START_COLOR, RUNIC_TRAP_TRANSITION_COLOR, RUNIC_TRAP_ARMED_COLOR, (Time.time - setupTime) / _trapsArmDelay),
                () => { particleSystemMain.startColor = RUNIC_TRAP_ARMED_COLOR; collider.enabled = true; }
            );
        }

        [HarmonyPatch(typeof(DeployableTrap), "OnReceiveArmTrap"), HarmonyPostfix]
        static void DeployableTrap_OnReceiveArmTrap_Post(DeployableTrap __instance)
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
                () => material.color = InternalUtility.Lerp3(TRAP_START_COLOR, TRAP_TRANSITION_COLOR, TRAP_ARMED_COLOR, (Time.time - setupTime) / _trapsArmDelay),
                () => { material.color = TRAP_ARMED_COLOR; collider.enabled = true; }
            );
        }

        [HarmonyPatch(typeof(DeployableTrap), "CleanUp"), HarmonyPrefix]
        static bool DeployableTrap_CleanUp_Pre(DeployableTrap __instance)
        {
            ResetColor(__instance);
            return true;
        }

        [HarmonyPatch(typeof(DeployableTrap), "Disassemble"), HarmonyPrefix]
        static bool DeployableTrap_Disassemble_Pre(DeployableTrap __instance)
        {
            ResetColor(__instance);
            return true;
        }
    }
}