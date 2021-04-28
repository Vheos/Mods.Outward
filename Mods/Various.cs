using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;



/* TO DO:
 * - hide armor extras (like scarf)
 * - prevent dodging right after hitting
 */
namespace ModPack
{
    public class Various : AMod
    {
        #region const
        private const float DEFAULT_ENEMY_HEALTH_RESET_HOURS = 24f;   // Character.HoursToHealthReset
        private const int ARMOR_TRAINING_ID = 8205220;
        static private Color TRAP_START_COLOR = Color.white;
        static private Color TRAP_TRANSITION_COLOR = Color.yellow;
        static private Color TRAP_ARMED_COLOR = Color.red;
        static private Color RUNIC_TRAP_START_COLOR = new Color(1f, 1f, 1f, 0f);
        static private Color RUNIC_TRAP_TRANSITION_COLOR = new Color(1f, 1f, 0.05f, 0.05f);
        static private Color RUNIC_TRAP_ARMED_COLOR = new Color(1f, 0.05f, 0f, 1f);
        #endregion
        #region enum
        [Flags]
        private enum ArmorSlots
        {
            None = 0,
            Head = 1 << 1,
            Chest = 1 << 2,
            Feet = 1 << 3,
        }
        #endregion

        // Settings
        static private ModSetting<bool> _enableCheats;
        static private ModSetting<bool> _skipStartupVideos;
        static private ModSetting<ArmorSlots> _armorSlotsToHide;
        static private ModSetting<bool> _extraControllerQuickslots;
        static private ModSetting<bool> _removeCoopScaling;
        static private ModSetting<bool> _removeDodgeInvulnerability;
        static private ModSetting<bool> _healEnemiesOnLoad;
        static private ModSetting<bool> _repairOnlyEquipped;
        static private ModSetting<bool> _loadArrowsFromInventory;
        static private ModSetting<float> _trapsArmDelay;
        static private ModSetting<bool> _trapsFriendlyFire;
        static private ModSetting<float> _pressureTrapRadius, _wireTrapDepth, _runicTrapRadius;
        static private ModSetting<bool> _multiplicativeStacking;
        static private ModSetting<int> _armorTrainingPenaltyReduction;
        static private ModSetting<bool> _applyArmorTrainingToManaCost;
        static private ModSetting<bool> _allowDodgeAnimationCancelling;
        static private ModSetting<bool> _allowPushKickRemoval;
        static private ModSetting<bool> _allowTargetingPlayers;
        override protected void Initialize()
        {
            _enableCheats = CreateSetting(nameof(_enableCheats), false);
            _skipStartupVideos = CreateSetting(nameof(_skipStartupVideos), false);
            _armorSlotsToHide = CreateSetting(nameof(_armorSlotsToHide), ArmorSlots.None);
            _extraControllerQuickslots = CreateSetting(nameof(_extraControllerQuickslots), false);
            _removeCoopScaling = CreateSetting(nameof(_removeCoopScaling), false);
            _removeDodgeInvulnerability = CreateSetting(nameof(_removeDodgeInvulnerability), false);
            _healEnemiesOnLoad = CreateSetting(nameof(_healEnemiesOnLoad), false);
            _repairOnlyEquipped = CreateSetting(nameof(_repairOnlyEquipped), false);
            _multiplicativeStacking = CreateSetting(nameof(_multiplicativeStacking), false);
            _armorTrainingPenaltyReduction = CreateSetting(nameof(_armorTrainingPenaltyReduction), 50, IntRange(0, 100));
            _applyArmorTrainingToManaCost = CreateSetting(nameof(_applyArmorTrainingToManaCost), false);
            _loadArrowsFromInventory = CreateSetting(nameof(_loadArrowsFromInventory), false);

            // Traps
            _trapsArmDelay = CreateSetting(nameof(_trapsArmDelay), 0f, FloatRange(0f, 5f));
            _trapsFriendlyFire = CreateSetting(nameof(_trapsFriendlyFire), false);
            _wireTrapDepth = CreateSetting(nameof(_wireTrapDepth), 0.703f, FloatRange(0f, 5f));
            _pressureTrapRadius = CreateSetting(nameof(_pressureTrapRadius), 1.1f, FloatRange(0f, 5f));
            _runicTrapRadius = CreateSetting(nameof(_runicTrapRadius), 2.5f, FloatRange(0f, 5f));

            AddEventOnConfigClosed(() =>
            {
                Global.CheatsEnabled = _enableCheats;
            });

            _trapsArmDelay.AddEvent(() =>
            {
                if (_trapsArmDelay == 0)
                    _trapsFriendlyFire.Value = false;
            });

            // WIP
            _allowDodgeAnimationCancelling = CreateSetting(nameof(_allowDodgeAnimationCancelling), false);
            _allowPushKickRemoval = CreateSetting(nameof(_allowPushKickRemoval), false);
            _allowTargetingPlayers = CreateSetting(nameof(_allowTargetingPlayers), false);
        }
        override protected void SetFormatting()
        {
            _enableCheats.Format("Enable cheats");
            _enableCheats.Description = "aka Debug Mode";
            _skipStartupVideos.Format("Skip startup videos");
            _skipStartupVideos.Description = "Saves ~3 seconds each time you launch the game";
            _armorSlotsToHide.Format("Armor slots to hide");
            _armorSlotsToHide.Description = "Used to hide ugly helmets (purely visual)";
            _extraControllerQuickslots.Format("16 controller quickslots");
            _extraControllerQuickslots.Description = "Allows you to use the d-pad with LT/RT for 8 extra quickslots\n" +
                                                     "(requires default d-pad keybinds AND game restart)";
            _removeCoopScaling.Format("Remove multiplayer scaling");
            _removeCoopScaling.Description = "Enemies in multiplayer will have the same stats as in singleplayer";
            _removeDodgeInvulnerability.Format("Remove dodge invulnerability");
            _removeDodgeInvulnerability.Description = "You can get hit during the dodge animation\n" +
                                                      "(even without a backpack)";
            _healEnemiesOnLoad.Format("Heal enemies on load");
            _healEnemiesOnLoad.Description = "Every loading screen fully heals all enemies";
            _repairOnlyEquipped.Format("Smith repairs only equipment");
            _repairOnlyEquipped.Description = "Blacksmith will not repair items in your pouch and bag";
            _multiplicativeStacking.Format("Multiplicative stacking");
            _multiplicativeStacking.Description = "Some stats will stack multiplicatively instead of additvely\n" +
                                                  "(movement speed, stamina cost, mana cost)";
            Indent++;
            {
                _armorTrainingPenaltyReduction.Format("Armor training penalty reduction", _multiplicativeStacking);
                _armorTrainingPenaltyReduction.Description = "How much of equipment's movement speed and stamina cost penalties should \"Armor Training\" ignore";
                _applyArmorTrainingToManaCost.Format("Apply armor training to mana cost", _multiplicativeStacking);
                _applyArmorTrainingToManaCost.Description = "\"Armor Training\" will also lower equipment's mana cost penalties";
                Indent--;
            }
            _loadArrowsFromInventory.Format("Load arrows from inventory");
            _loadArrowsFromInventory.Description = "Whenever you shoot your bow, the missing arrow is automatically replace with one from backpack or pouch (in that order).";

            _trapsArmDelay.Format("Traps arm delay");
            _trapsArmDelay.Description = "How long the trap has to stay on ground before it can explode (in seconds)";
            Indent++;
            {
                _trapsFriendlyFire.Format("Friendly fire", _trapsArmDelay, () => _trapsArmDelay > 0);
                _trapsFriendlyFire.Description = "The trap will also explode in contact with you and other players";
                Indent--;
            }
            _wireTrapDepth.Format("Tripwire Trap depth");
            _pressureTrapRadius.Format("Presure Plate radius");
            _runicTrapRadius.Format("Runic Trap radius");

            _allowDodgeAnimationCancelling.Format("[WIP] Allow dodge to cancel actions");
            _allowDodgeAnimationCancelling.Description = "Cancelling certain animations might lead to glitches";
            _allowDodgeAnimationCancelling.IsAdvanced = true;
            _allowPushKickRemoval.Format("[WIP] Allow \"Push Kick\" removal");
            _allowPushKickRemoval.Description = "For future skill trees mod\n" +
                                                "Normally, player data won't be saved if they don't have the \"Push Kick\" skill";
            _allowPushKickRemoval.IsAdvanced = true;
            _allowTargetingPlayers.Format("[WIP] Allow targeting players");
            _allowTargetingPlayers.Description = "For future co-op skills mod";
            _allowTargetingPlayers.IsAdvanced = true;
        }
        override protected string Description
        => "• Mods (small and big) that didn't get their own section yet :)";

        // Runic Trap arm delay
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

        [HarmonyPatch(typeof(DeployableTrap), "StartInit"), HarmonyPostfix]
        static void DeployableTrap_StartInit_Post(ref DeployableTrap __instance)
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
                () => particleSystemMain.startColor = Utility.Lerp3(RUNIC_TRAP_START_COLOR, RUNIC_TRAP_TRANSITION_COLOR, RUNIC_TRAP_ARMED_COLOR, (Time.time - setupTime) / _trapsArmDelay),
                () => { particleSystemMain.startColor = RUNIC_TRAP_ARMED_COLOR; collider.enabled = true; }
            );
        }

        [HarmonyPatch(typeof(DeployableTrap), "OnReceiveArmTrap"), HarmonyPostfix]
        static void DeployableTrap_OnReceiveArmTrap_Post(ref DeployableTrap __instance)
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
                () => material.color = Utility.Lerp3(TRAP_START_COLOR, TRAP_TRANSITION_COLOR, TRAP_ARMED_COLOR, (Time.time - setupTime) / _trapsArmDelay),
                () => { material.color = TRAP_ARMED_COLOR; collider.enabled = true; }
            );
        }

        [HarmonyPatch(typeof(DeployableTrap), "CleanUp"), HarmonyPrefix]
        static bool DeployableTrap_CleanUp_Pre(ref DeployableTrap __instance)
        {
            ResetColor(__instance);
            return true;
        }

        [HarmonyPatch(typeof(DeployableTrap), "Disassemble"), HarmonyPrefix]
        static bool DeployableTrap_Disassemble_Pre(ref DeployableTrap __instance)
        {
            ResetColor(__instance);
            return true;
        }


        // Load arrows from inventory
        [HarmonyPatch(typeof(WeaponLoadoutItem), "ReduceShotAmount"), HarmonyPrefix]
        static bool WeaponLoadoutItem_ReduceShotAmount_Pre(ref WeaponLoadoutItem __instance)
        {
            #region quit
            if (!_loadArrowsFromInventory
            || __instance.AmunitionType != WeaponLoadout.CompatibleAmmunitionType.WeaponType
            || __instance.CompatibleEquipment != Weapon.WeaponType.Arrow)
                return true;
            #endregion

            CharacterInventory inventory = __instance.m_projectileWeapon.OwnerCharacter.Inventory;
            int ammoID = inventory.GetEquippedAmmunition().ItemID;

            Item ammo = null;
            if (ammo == null && inventory.EquippedBag != null)
                ammo = inventory.EquippedBag.Container.GetItemFromID(ammoID);
            if (ammo == null)
                ammo = inventory.Pouch.GetItemFromID(ammoID);
            if (ammo == null)
                return true;

            ammo.RemoveQuantity(1);
            return false;
        }

        [HarmonyPatch(typeof(CharacterInventory), "GetAmmunitionCount"), HarmonyPostfix]
        static void CharacterInventory_GetAmmunitionCount_Post(ref CharacterInventory __instance, ref int __result)
        {
            #region quit
            if (!_loadArrowsFromInventory || __result == 0)
                return;
            #endregion

            __result += __instance.ItemCount(__instance.GetEquippedAmmunition().ItemID);
        }

        // Multiplicative stacking
        static private bool HasLearnedArmorTraining(Character character)
        => character.Inventory.SkillKnowledge.IsItemLearned(ARMOR_TRAINING_ID);
        static private bool IsAnythingEquipped(EquipmentSlot slot)
        => slot != null && slot.HasItemEquipped;
        static private bool IsNotLeftHandUsedBy2H(EquipmentSlot slot)
        => !(slot.SlotType == EquipmentSlot.EquipmentSlotIDs.LeftHand && slot.EquippedItem.TwoHanded);
        static private bool TryApplyMultiplicativeStacking(CharacterEquipment equipment, ref float result, Func<EquipmentSlot, float> getStatValue, bool invertedValue = false, bool applyArmorTraining = false)
        {
            #region quit
            if (!_multiplicativeStacking)
                return true;
            #endregion

            float invCoeff = invertedValue ? -1f : +1f;
            bool canApplyArmorTraining = applyArmorTraining && HasLearnedArmorTraining(equipment.m_character);

            result = 1f;
            foreach (var slot in equipment.m_equipmentSlots)
                if (IsAnythingEquipped(slot) && IsNotLeftHandUsedBy2H(slot))
                {
                    float armorTrainingCoeff = canApplyArmorTraining && getStatValue(slot) > 0f ? 1f - _armorTrainingPenaltyReduction / 100f : 1f;
                    result *= 1f + getStatValue(slot) / 100f * invCoeff * armorTrainingCoeff;
                }
            result -= 1f;
            result *= invCoeff;
            return false;
        }

        [HarmonyPatch(typeof(Stat), "GetModifier"), HarmonyPrefix]
        static bool Stat_GetModifier_Pre(ref Stat __instance, ref float __result, ref IList<Tag> _tags, ref int baseModifier)
        {
            #region quit
            if (!_multiplicativeStacking)
                return true;
            #endregion

            DictionaryExt<string, StatStack> multipliers = __instance.m_multiplierStack;
            __result = baseModifier;
            for (int i = 0; i < multipliers.Count; i++)
            {
                if (multipliers.Values[i].HasEnded)
                    multipliers.RemoveAt(i--);
                else if (multipliers.Values[i].SameTags(_tags))
                {
                    float value = multipliers.Values[i].EffectiveValue;
                    if (!__instance.NullifyPositiveStat || value <= 0f)
                        __result *= (1f + value);
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalMovementModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalMovementModifier_Pre(ref CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.MovementPenalty, true, true);

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalStaminaUseModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalStaminaUseModifier_Pre(ref CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.StaminaUsePenalty, false, true);

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalManaUseModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalManaUseModifier_Pre(ref CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.ManaUseModifier, false, _applyArmorTrainingToManaCost);

        // Blacksmith repair nerf
        [HarmonyPatch(typeof(ItemContainer), "RepairContainedEquipment"), HarmonyPrefix]
        static bool ItemContainer_RepairContainedEquipment_Pre(ref ItemContainer __instance)
        => !_repairOnlyEquipped;

        // Skip startup video
        [HarmonyPatch(typeof(StartupVideo), "Awake"), HarmonyPrefix]
        static bool StartupVideo_Awake_Pre()
        {
            StartupVideo.HasPlayedOnce = _skipStartupVideos.Value;
            return true;
        }

        // Hide armor slots
        static private bool ShouldArmorSlotBeHidden(EquipmentSlot.EquipmentSlotIDs slot)
        => slot == EquipmentSlot.EquipmentSlotIDs.Helmet && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Head)
        || slot == EquipmentSlot.EquipmentSlotIDs.Chest && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Chest)
        || slot == EquipmentSlot.EquipmentSlotIDs.Foot && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Feet);

        [HarmonyPatch(typeof(CharacterVisuals), "EquipVisuals"), HarmonyPrefix]
        static bool CharacterVisuals_EquipVisuals_Pre(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
        {
            #region quit
            if (_armorSlotsToHide == ArmorSlots.None)
                return true;
            #endregion

            // save original hide flags for postfix
            __state = new bool[3];
            __state[0] = _visuals.HideFace;
            __state[1] = _visuals.HideHair;
            __state[2] = _visuals.DisableDefaultVisuals;
            // override hide flags
            if (ShouldArmorSlotBeHidden(_slotID))
            {
                _visuals.HideFace = false;
                _visuals.HideHair = false;
                _visuals.DisableDefaultVisuals = false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CharacterVisuals), "EquipVisuals"), HarmonyPostfix]
        static void CharacterVisuals_EquipVisuals_Post(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
        {
            #region quit
            if (_armorSlotsToHide == ArmorSlots.None)
                return;
            #endregion

            // hide chosen pieces of armor
            if (ShouldArmorSlotBeHidden(_slotID))
                _visuals.Hide();

            // restore original hide flags
            _visuals.HideFace = __state[0];
            _visuals.HideHair = __state[1];
            _visuals.DisableDefaultVisuals = __state[2];
        }

        // Remove co-op scaling
        [HarmonyPatch(typeof(CoopStats), "ApplyToCharacter"), HarmonyPrefix]
        static bool CoopStats_ApplyToCharacter_Pre()
    => !_removeCoopScaling;

        [HarmonyPatch(typeof(CoopStats), "RemoveFromCharacter"), HarmonyPrefix]
        static bool CoopStats_RemoveFromCharacter_Pre()
        => !_removeCoopScaling;

        // Remove dodge invulnerability
        [HarmonyPatch(typeof(Character), "DodgeStep"), HarmonyPostfix]
        static void Character_DodgeStep_Post(ref Hitbox[] ___m_hitboxes, ref int _step)
        {
            #region quit
            if (!_removeDodgeInvulnerability)
                return;
            #endregion

            if (_step > 0 && ___m_hitboxes != null)
                foreach (var hitbox in ___m_hitboxes)
                    hitbox.gameObject.SetActive(true);
        }

        // Enemy health reset time
        [HarmonyPatch(typeof(Character), "LoadCharSave"), HarmonyPrefix]
        static bool Character_LoadCharSave_Pre(ref Character __instance)
        {
            __instance.HoursToHealthReset = _healEnemiesOnLoad ? 0 : DEFAULT_ENEMY_HEALTH_RESET_HOURS;
            return true;
        }

        // Dodge animation cancelling
        [HarmonyPatch(typeof(Character), "DodgeInput", new[] { typeof(Vector3) }), HarmonyPrefix]
        static bool Character_SpellCastAnim_Post(ref int ___m_dodgeAllowedInAction, ref Character.HurtType ___m_hurtType)
        {
            #region quit
            if (!_allowDodgeAnimationCancelling)
                return true;
            #endregion

            if (___m_hurtType == Character.HurtType.NONE)
                ___m_dodgeAllowedInAction = 1;
            return true;
        }

        // Push kick removal
        [HarmonyPatch(typeof(CharacterSave), "IsValid", MethodType.Getter), HarmonyPrefix]
        static bool CharacterSave_IsValid_Getter_Pre(ref bool __result)
        {
            #region quit
            if (!_allowPushKickRemoval.Value)
                return true;
            #endregion

            __result = true;
            return false;
        }

        // Target other players
        [HarmonyPatch(typeof(TargetingSystem), "IsTargetable", new[] { typeof(Character) }), HarmonyPrefix]
        static bool TargetingSystem_IsTargetable_Pre(ref TargetingSystem __instance, ref bool __result, ref Character _char)
        {
            #region quit
            if (!_allowTargetingPlayers)
                return true;
            #endregion

            if (_char.Faction == Character.Factions.Player && _char != __instance.m_character)
            {
                __result = true;
                return false;
            }
            return true;
        }

        // 16 controller quickslots
        static private void TryOverrideVanillaQuickslotInput(ref bool input, int playerID)
        {
            #region quit
            if (!_extraControllerQuickslots)
                return;
            #endregion

            input &= !ControlsInput.QuickSlotToggle1(playerID) && !ControlsInput.QuickSlotToggle2(playerID);
        }
        static private void TryHandleCustomQuickslotInput(Character character)
        {
            #region quit
            if (!_extraControllerQuickslots)
                return;
            #endregion

            if (character == null || character.QuickSlotMngr == null || character.CharacterUI.IsMenuFocused)
                return;

            int playerID = character.OwnerPlayerSys.PlayerID;
            if (!ControlsInput.QuickSlotToggle1(playerID) && !ControlsInput.QuickSlotToggle2(playerID))
                return;

            int quickslotID = -1;
            if (GameInput.Pressed(playerID, ControlsInput.GameplayActions.Sheathe))
                quickslotID = 8;
            else if (GameInput.Pressed(playerID, ControlsInput.MenuActions.ToggleMapMenu))
                quickslotID = 9;
            else if (GameInput.Pressed(playerID, ControlsInput.GameplayActions.ToggleLights))
                quickslotID = 10;
            else if (GameInput.Pressed(playerID, ControlsInput.GameplayActions.HandleBag))
                quickslotID = 11;

            if (quickslotID < 0)
                return;

            if (ControlsInput.QuickSlotToggle1(playerID))
                quickslotID += 4;

            character.QuickSlotMngr.QuickSlotInput(quickslotID);
        }
        static private void SetupQuickslots(Transform quickslotsHolder)
        {
            Transform quickslotTemplate = quickslotsHolder.Find("1");
            for (int i = quickslotsHolder.childCount; i < 16; i++)
                GameObject.Instantiate(quickslotTemplate, quickslotsHolder);

            QuickSlot[] quickslots = quickslotsHolder.GetComponentsInChildren<QuickSlot>();
            for (int i = 0; i < quickslots.Length; i++)
            {
                quickslots[i].GOSetName((i + 1).ToString());
                quickslots[i].ItemQuickSlot = false;
            }
        }
        static private void SetupQuickslotPanels(CharacterUI ui)
        {
            // Cache
            Transform menuPanelsHolder = GetMenuPanelsHolder(ui);
            Transform gamePanelsHolder = GetGamePanelsHolder(ui);
            Component[] menuSlotsLT = menuPanelsHolder.Find("LT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
            Component[] menuSlotsRT = menuPanelsHolder.Find("RT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
            Component[] gameSlotsLT = gamePanelsHolder.Find("LT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
            Component[] gameSlotsRT = gamePanelsHolder.Find("RT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
            // Copy game 
            for (int i = 0; i < menuSlotsLT.Length; i++)
                menuSlotsLT[i].transform.localPosition = gameSlotsLT[i].transform.localPosition;
            for (int i = 0; i < menuSlotsRT.Length; i++)
                menuSlotsRT[i].transform.localPosition = gameSlotsRT[i].transform.localPosition;

            gamePanelsHolder.Find("imgLT").localPosition = new Vector3(-195f, +170f);
            gamePanelsHolder.Find("imgRT").localPosition = new Vector3(-155f, +170f);

            menuPanelsHolder.Find("LT").localPosition = new Vector3(-90f, +50f);
            menuPanelsHolder.Find("RT").localPosition = new Vector3(+340f, -100f);
            menuPanelsHolder.Find("LT/imgLT").localPosition = new Vector3(-125f, 125f);
            menuPanelsHolder.Find("RT/imgRT").localPosition = new Vector3(-125f, 125f);
            menuPanelsHolder.Find("LeftDecoration").gameObject.SetActive(false);
            menuPanelsHolder.Find("RightDecoration").gameObject.SetActive(false);

            DuplicateQuickslotsInPanel(gamePanelsHolder.Find("LT"), +8, new Vector3(-250f, 0f));
            DuplicateQuickslotsInPanel(gamePanelsHolder.Find("RT"), +8, new Vector3(-250f, 0f));
            DuplicateQuickslotsInPanel(menuPanelsHolder.Find("LT"), +8, new Vector3(-250f, 0f));
            DuplicateQuickslotsInPanel(menuPanelsHolder.Find("RT"), +8, new Vector3(-250f, 0f));
        }
        static private void DuplicateQuickslotsInPanel(Transform panelHolder, int idOffset, Vector3 posOffset)
        {
            Transform quickslotsHolder = panelHolder.Find("QuickSlots");
            foreach (var editorPlacer in quickslotsHolder.GetComponentsInChildren<EditorQuickSlotDisplayPlacer>())
            {
                // Instantiate
                editorPlacer.IsTemplate = true;
                Transform newSlot = GameObject.Instantiate(editorPlacer.transform);
                editorPlacer.IsTemplate = false;
                // Setup
                newSlot.SetParent(quickslotsHolder);
                newSlot.localPosition = editorPlacer.transform.localPosition + posOffset;
                EditorQuickSlotDisplayPlacer newEditorPlacer = newSlot.GetComponent<EditorQuickSlotDisplayPlacer>();
                newEditorPlacer.RefSlotID += idOffset;
                newEditorPlacer.IsTemplate = false;
            }
        }
        static private Transform GetGamePanelsHolder(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/HUD/QuickSlot/Controller/LT-RT");
        static private Transform GetMenuPanelsHolder(CharacterUI ui)
        => ui.transform.Find("Canvas/GameplayPanels/Menus/CharacterMenus/MainPanel/Content/MiddlePanel/QuickSlotPanel/PanelSwitcher/Controller/LT-RT");

        [HarmonyPatch(typeof(ControlsInput), "Sheathe"), HarmonyPostfix]
        static void ControlsInput_Sheathe_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

        [HarmonyPatch(typeof(ControlsInput), "ToggleMap"), HarmonyPostfix]
        static void ControlsInput_ToggleMap_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

        [HarmonyPatch(typeof(ControlsInput), "ToggleLights"), HarmonyPostfix]
        static void ControlsInput_ToggleLights_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

        [HarmonyPatch(typeof(ControlsInput), "HandleBackpack"), HarmonyPostfix]
        static void ControlsInput_HandleBackpack_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

        [HarmonyPatch(typeof(LocalCharacterControl), "UpdateQuickSlots"), HarmonyPostfix]
        static void LocalCharacterControl_UpdateQuickSlots_Pre(ref Character ___m_character)
        => TryHandleCustomQuickslotInput(___m_character);

        [HarmonyPatch(typeof(SplitScreenManager), "Awake"), HarmonyPostfix]
        static void SplitScreenManager_Awake_Post(ref SplitScreenManager __instance)
        {
            #region quit
            if (!_extraControllerQuickslots)
                return;
            #endregion

            CharacterUI charUIPrefab = __instance.m_charUIPrefab;
            GameObject.DontDestroyOnLoad(charUIPrefab);
            SetupQuickslotPanels(charUIPrefab);
        }

        [HarmonyPatch(typeof(CharacterQuickSlotManager), "Awake"), HarmonyPrefix]
        static bool CharacterQuickSlotManager_Awake_Pre(ref CharacterQuickSlotManager __instance)
        {
            #region quit
            if (!_extraControllerQuickslots)
                return true;
            #endregion

            SetupQuickslots(__instance.transform.Find("QuickSlots"));
            return true;
        }

    }
}
/*
 *         [Flags]
        private enum EquipmentStats
        {
            None = 0,
            All = ~0,

            Damage = 1 << 1,
            ImpactDamage = 1 << 2,
            Resistance = 1 << 3,
            ImpactResistance = 1 << 4,
            CorruptionResistance = 1 << 5,
            MovementSpeed = 1 << 6,
            StaminaCost = 1 << 7,
            ManaCost = 1 << 8,
            CooldownReduction = 1 << 9,
        }
 */

/* POUCH
private const float POUCH_CAPACITY = 10f;
static private ModSetting<bool> _pouchToggle;
static private ModSetting<int> _pouchCapacity;
static private ModSetting<bool> _allowOverCapacity;

_pouchToggle = CreateSetting(nameof(_pouchToggle), false);
_pouchCapacity = CreateSetting(nameof(_pouchCapacity), POUCH_CAPACITY.Round(), IntRange(0, 100));
_allowOverCapacity = CreateSetting(nameof(_allowOverCapacity), true);

_pouchToggle.Format("Pouch");
Indent++;
{
    _pouchCapacity.Format("Pouch size", _pouchToggle);
    _allowOverCapacity.Format("Allow over capacity", _pouchToggle);
    Indent--;
}

[HarmonyPatch(typeof(CharacterInventory), "ProcessStart"), HarmonyPostfix]
static void CharacterInventory_ProcessStart_Post(ref CharacterInventory __instance, ref Character ___m_character)
{
    #region quit
    if (!_pouchToggle)
        return;
    #endregion

    ItemContainer pouch = __instance.Pouch;
    if (___m_character.IsPlayer() && pouch != null)
    {
        pouch.SetField("m_baseContainerCapacity", _pouchCapacity.Value, typeof(ItemContainer));
        pouch.AllowOverCapacity = _allowOverCapacity;
    }
}
*/

/* Extra Controller Quickslots
[HarmonyPatch(typeof(QuickSlotPanel), "InitializeQuickSlotDisplays"), HarmonyPostfix]
static void QuickSlotPanel_InitializeQuickSlotDisplays_Post(ref QuickSlotPanel __instance, ref QuickSlotDisplay[] ___m_quickSlotDisplays)
{
    #region quit
    if (!_extraControllerQuickslots)
        return;
    #endregion

    if (__instance.name == BOTH_TRIGGERS_PANEL_NAME)
        for (int i = 0; i < ___m_quickSlotDisplays.Length; i++)
            ___m_quickSlotDisplays[i].RefSlotID = i + 8;
}

[HarmonyPatch(typeof(LocalCharacterControl), "UpdateQuickSlots"), HarmonyPostfix]
static void LocalCharacterControl_UpdateQuickSlots_Pre(ref Character ___m_character)
{
    #region quit
    if (!_extraControllerQuickslots)
        return;
    if (___m_character == null || ___m_character.QuickSlotMngr == null || ___m_character.CharacterUI.IsMenuFocused)
        return;
    #endregion

    int playerID = ___m_character.OwnerPlayerSys.PlayerID;

    if (QuickSlotInstant9(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(8);
    else if (QuickSlotInstant10(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(9);
    else if (QuickSlotInstant11(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(10);
    else if (QuickSlotInstant12(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(11);
}

        static void AddQuickSlot12()
{
    foreach (var localPlayer in Utility.LocalPlayers)
    {
        Transform quickSlotsHolder = localPlayer.ControlledCharacter.GetComponent<CharacterQuickSlotManager>().QuickslotTrans;
        if (quickSlotsHolder.Find(QUICKSLOT_12_NAME) != null)
            continue;

        QuickSlot newQuickSlot = GameObject.Instantiate(quickSlotsHolder.Find("1"), quickSlotsHolder).GetComponent<QuickSlot>();
        newQuickSlot.name = QUICKSLOT_12_NAME;
        foreach (var quickSlot in quickSlotsHolder.GetComponents<QuickSlot>())
            quickSlot.ItemQuickSlot = false;
        localPlayer.ControlledCharacter.QuickSlotMngr.Awake();
    }
}

static void DrawExtraControllerQuickslotSwitcher()
{
    foreach (var localPlayer in Utility.LocalPlayers)
    {
        Transform panel = localPlayer.ControlledCharacter.CharacterUI.QuickSlotMenu.FindChild("PanelSwitcher/Controller/LT-RT").transform;
        if (panel.Find(BOTH_TRIGGERS_PANEL_NAME) != null)
            continue;

        // Instantiate
        Transform LT = panel.Find("LT").transform;
        Transform RT = panel.Find("RT").transform;
        foreach (var quickslotPlacer in LT.GetComponentsInChildren<EditorQuickSlotDisplayPlacer>())
            quickslotPlacer.IsTemplate = true;
        Transform LTRT = GameObject.Instantiate(LT.gameObject, panel).transform;
        LTRT.name = BOTH_TRIGGERS_PANEL_NAME;
        Transform imgLT = LTRT.Find("imgLT");
        Transform imgRT = GameObject.Instantiate(RT.Find("imgRT"), LTRT).transform;
        imgRT.name = "imgRT";

        // Change
        panel.Find("LeftDecoration").gameObject.SetActive(false);
        panel.Find("RightDecoration").gameObject.SetActive(false);
        LT.localPosition = new Vector2(-300f, 0);
        RT.localPosition = new Vector2(0f, 0);
        LTRT.localPosition = new Vector2(+300f, 0);
        imgLT.localPosition = new Vector2(-22.5f, 22.5f);
        imgRT.localPosition = new Vector2(+22.5f, 22.5f);
    }
}

*/
