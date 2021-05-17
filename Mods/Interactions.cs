using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using Random = UnityEngine.Random;



namespace ModPack
{
    public class Interactions : AMod, IDelayedInit
    {
        #region const
        public const float ANIMATED_TAKE_DELAY = 0.3f;
        public const float STANDING_TAKE_MIN_HEIGHT = 0.7f;
        public const float UNPACK_OFFSET_Y = 0.2f;
        public const float UNPACK_BUMP_FORCE = 60f;
        public const float UNPACK_USE_DELAY = 0.1f;
        public const string DISALLOW_IN_COMBAT_NOTIFICATION = "Can't while in combat!";
        #endregion
        #region enum
        [Flags]
        private enum GroundInteractions
        {
            None = 0,
            All = ~0,

            EatAndDrink = 1 << 1,
            UseBandage = 1 << 2,
            InfuseWeapon = 1 << 3,
            SwitchEquipment = 1 << 4,
            DeployKit = 1 << 5,
        }
        [Flags]
        private enum InteractionsInCombat
        {
            None = 0,

            Loot = 1 << 1,
            Travel = 1 << 2,
            Warp = 1 << 3,
            PullLever = 1 << 4,
        }
        #endregion
        #region interaction
        // Interactions
        public abstract class InteractionFromGround : ItemInteraction
        {
            // Overrides
            public override bool InstantActivationDone
            => false;
            public override bool ShowItemPreview
            => true;

            // Utility
            protected void TryTake(Item item, bool tryToEquip)
            {
                GroupContainer stack = item as GroupContainer;
                if (stack != null)
                    ItemManager.Instance.SendTakeStack(LastCharacter, stack);
                else
                    LastCharacter.Inventory.TakeItem(item, tryToEquip);

                m_item.SetBeingTaken(false, null);
                OnActivationDone();
            }
            protected void TryUse(Item item)
            {
                m_item.m_ownerCharacter = LastCharacter;
                if (item.TryUse(LastCharacter))
                {
                    // prevents m_inAnimBeingUsed from being stuck on true if the player interrupts the using animation
                    // m_inAnimBeingUsed's only purpose is to hide item from inventory when it's being used
                    item.CastInterrupted();
                    Invoke("OnActivationDone", 0.3f);
                }
                else
                    OnActivationDone();
            }
            protected void TryDeploy(Item item)
            {
                item.m_ownerCharacter = LastCharacter;
                item.GetComponent<Deployable>().TryDeploying(LastCharacter);
                OnActivationDone();
            }
            protected Transform RightHandTransform
            => LastCharacter.Inventory.GetEquipmentVisualSlotTransform(EquipmentSlot.EquipmentSlotIDs.RightHand);
        }
        public class InteractionTakeAnimated : InteractionFromGround
        {
            // Overrides
            public override void OnActivate()
            {
                // Cache
                float differenceY = m_item.transform.position.y - LastCharacter.transform.position.y;
                bool isStanding = differenceY >= STANDING_TAKE_MIN_HEIGHT;
                Character.SpellCastType animation = isStanding ? Character.SpellCastType.UseUp : Character.SpellCastType.PickupGround;

                // Execute
                if (!LastCharacter.Sheathed)
                    LastCharacter.SheatheInput();
                this.ExecuteOnceWhen(() => !LastCharacter.Sheathing, () =>
                {
                    // Sending SpellCast message to null to override default behaviour
                    // (will raise a few log messages, but no errors)
                    LastCharacter.CastSpell(animation, (GameObject)null, Character.SpellCastModifier.Immobilized, 1, -1f);
                    this.ExecuteOnceAfterDelay(ANIMATED_TAKE_DELAY, () =>
                    {
                        m_item.SetBeingTaken(true, RightHandTransform);
                        this.ExecuteOnceWhen(() => LastCharacter.CurrentSpellCast == Character.SpellCastType.NONE, () =>
                        {
                            TryTake(m_item, false);
                            base.OnActivate();
                        });
                    });
                });
            }
            public override string DefaultPressLocKey
            => "Interaction_Item_Take";
        }
        public class InteractionUse : InteractionFromGround
        {
            // Overrides
            public override void OnActivate()
            {
                TryUse(m_item);
                base.OnActivate();
            }
            public override string DefaultHoldLocKey
            => m_item is InfuseConsumable ? "Infuse" : "Use";
        }
        public class InteractionIngest : InteractionFromGround
        {
            // Overrides
            public override void OnActivate()
            {
                TryUse(m_item);
                base.OnActivate();
            }
            public override string DefaultHoldLocKey
            => m_item.IsEatable() ? "Eat" : "Drink";
        }
        public class InteractionUnpackAndIngest : InteractionFromGround
        {
            // Overrides
            public override void OnActivate()
            {
                Item firstItem = m_item.As<GroupContainer>().FirstItem();
                Unpack(firstItem, UNPACK_OFFSET_Y);
                Bump(firstItem, UNPACK_BUMP_FORCE);
                firstItem.ExecuteOnceAfterDelay(UNPACK_USE_DELAY, () =>
                {
                    TryUse(firstItem);
                    base.OnActivate();
                });
            }
            public override string DefaultHoldLocKey
            => m_item.As<GroupContainer>().FirstItem().IsEatable() ? "Eat" : "Drink";

            // Utility
            private void Unpack(Item item, float offsetY)
            {
                item.ChangeParent(null);
                item.ForceUpdateParentChange();
                item.transform.position += Vector3.up * offsetY;
            }
            private void Bump(Item item, float force)
            {
                Vector2 directionXZ = Random.insideUnitCircle.normalized;
                Vector3 direction = new Vector3(directionXZ.x, 1, directionXZ.y);
                item.GetComponent<Rigidbody>().AddForce(direction * force);
            }
        }


        public class InteractionDeploy : InteractionFromGround
        {
            // Overrides
            public override void OnActivate()
            {
                TryDeploy(m_item);
                base.OnActivate();
            }
            public override string DefaultHoldLocKey
            => "Deploy";
        }

        #endregion

        // Settings
        static private ModSetting<float> _holdInteractionsDuration;
        static private ModSetting<bool> _swapWaterInteractions;
        static private ModSetting<bool> _singleHoldsToPresses;
        static private ModSetting<bool> _takeAnimations;
        static private ModSetting<GroundInteractions> _groundInteractions;
        static private ModSetting<InteractionsInCombat> _disallowedInCombat;
        override protected void Initialize()
        {
            _groundInteractions = CreateSetting(nameof(_groundInteractions), GroundInteractions.None);
            _singleHoldsToPresses = CreateSetting(nameof(_singleHoldsToPresses), false);
            _holdInteractionsDuration = CreateSetting(nameof(_holdInteractionsDuration), GameInput.HOLD_THRESHOLD + GameInput.HOLD_DURATION, FloatRange(0.1f + GameInput.HOLD_THRESHOLD, 5f));
            _takeAnimations = CreateSetting(nameof(_takeAnimations), false);
            _swapWaterInteractions = CreateSetting(nameof(_swapWaterInteractions), false);
            _disallowedInCombat = CreateSetting(nameof(_disallowedInCombat), InteractionsInCombat.None);
        }
        override protected void SetFormatting()
        {
            _groundInteractions.Format("Use items from ground");
            _groundInteractions.Description = "Items to use straight from the ground with a \"Hold\" interaction";
            _singleHoldsToPresses.Format("Instant \"Hold\" interactions");
            _singleHoldsToPresses.Description = "Changes many objects' \"Hold\" interaction to \"Press\"\n" +
                                                "(examples: gathering, fishing, mining, opening chests)";
            _holdInteractionsDuration.Format("\"Hold\" interactions duration");
            _holdInteractionsDuration.Description = "How long you want to hold the button for \"Hold\" interaction to trigger";
            _takeAnimations.Format("Item take animations");
            _takeAnimations.Description = "Animates (and greatly slows down) the process of taking items";
            _swapWaterInteractions.Format("Swap water gather/drink");
            _swapWaterInteractions.Description = "Vanilla: press to gather, hold to drink\n" +
                                                 "Custom: press to drink, hold to gather";
            _disallowedInCombat.Format("Disallowed in combat");
            _disallowedInCombat.Description = "Loot   -   opening chests, backpacks, corpses, etc.\n" +
                                              "Travel   -   move to another area with loading screen\n" +
                                              "Warp   -   enter door, climb rope or teleport without loading screen\n" +
                                              "Pull levers   -   open gates, ride elevators, etc.";
        }
        override protected string Description
        => "• Instant \"Hold\" interactions\n" +
           "• Use items straight from the ground\n" +
           "• \"Take item\" animations\n" +
           "• Disallow certain interactions while in combat";
        override protected string SectionOverride
        => SECTION_SURVIVAL;

        // Utility
        static private void SwapBasicAndHoldInteractions(InteractionActivator activator, ref IInteraction vanillaBasic, ref IInteraction vanillaHold)
        {
            InteractionBase basic = vanillaBasic as InteractionBase;
            InteractionBase hold = vanillaHold as InteractionBase;
            if (_swapWaterInteractions && basic.Is<GatherWaterInteraction>() && hold.Is<DrinkWaterInteraction>()
            || _singleHoldsToPresses && activator.CanDoHold && !activator.CanDoBasic && hold.IsNot<InteractionDisassemble>())
            {
                if (basic != null)
                    basic.SetOverrideHoldLocKey(basic.DefaultPressLocKey);
                if (hold != null)
                    hold.SetOverridePressLocKey(hold.DefaultHoldLocKey);
                Utility.Swap(ref vanillaBasic, ref vanillaHold);

                InteractionTriggerBase triggerBase = activator.GetComponent<InteractionTriggerBase>();
                if (triggerBase != null)
                    Utility.Swap(ref triggerBase.m_basicConditions, ref triggerBase.m_holdConditions);
            }
        }
        static private void AddCustomHoldInteractions(InteractionActivator activator, ref IInteraction vanillaHold)
        {
            #region quit
            if (_groundInteractions == GroundInteractions.None)
                return;
            #endregion

            Item item = activator.GetComponentInParent<Item>();
            GroupContainer stack = item as GroupContainer;
            if (item != null && vanillaHold == null)
            {
                if (_groundInteractions.Value.HasFlag(GroundInteractions.EatAndDrink) && item.IsIngestible())
                    vanillaHold = activator.gameObject.AddComponent<InteractionIngest>();
                else if (_groundInteractions.Value.HasFlag(GroundInteractions.EatAndDrink) && stack != null && stack.FirstItem().IsIngestible())
                    vanillaHold = activator.gameObject.AddComponent<InteractionUnpackAndIngest>();
                else if (_groundInteractions.Value.HasFlag(GroundInteractions.UseBandage) && item.ItemID == "Bandages".ItemID())
                    vanillaHold = activator.gameObject.AddComponent<InteractionUse>();
                else if (_groundInteractions.Value.HasFlag(GroundInteractions.InfuseWeapon) && item is InfuseConsumable)
                    vanillaHold = activator.gameObject.AddComponent<InteractionUse>();
                else if (_groundInteractions.Value.HasFlag(GroundInteractions.SwitchEquipment) && item is Equipment)
                    vanillaHold = activator.gameObject.AddComponent<InteractionSwitchItem>();
                else if (_groundInteractions.Value.HasFlag(GroundInteractions.DeployKit) && item.HasComponent<Deployable>())
                    vanillaHold = activator.gameObject.AddComponent<InteractionDeploy>();
            }
        }
        static private void AddAnimationsToTakeInteractions(InteractionActivator activator, ref IInteraction vanillaBasic)
        {
            #region quit
            if (!_takeAnimations)
                return;
            #endregion

            if (vanillaBasic is InteractionTake)
            {
                InteractionTake oldInteraction = vanillaBasic as InteractionTake;
                vanillaBasic = activator.gameObject.AddComponent<InteractionTakeAnimated>();
                oldInteraction.Destroy();
            }
        }

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        [HarmonyPatch(typeof(InteractionBase), "HoldActivationTime", MethodType.Getter), HarmonyPrefix]
        static bool InteractionBase_HoldActivationTime_Getter_Post(ref float __result, ref float ___m_holdActivationTimeOverride)
        {
            __result = ___m_holdActivationTimeOverride != -1 ? ___m_holdActivationTimeOverride : _holdInteractionsDuration;
            return false;
        }

        [HarmonyPatch(typeof(InteractionActivator), "OnLateInit"), HarmonyPostfix]
        static void InteractionActivator_OnLateInit_Post(InteractionActivator __instance, ref IInteraction ___m_defaultBasicInteraction, ref IInteraction ___m_defaultHoldInteraction)
        {
            SwapBasicAndHoldInteractions(__instance, ref ___m_defaultBasicInteraction, ref ___m_defaultHoldInteraction);
            AddCustomHoldInteractions(__instance, ref ___m_defaultHoldInteraction);
            AddAnimationsToTakeInteractions(__instance, ref ___m_defaultBasicInteraction);
        }

        // Disallow interactions in combat
        [HarmonyPatch(typeof(InteractionTriggerBase), "TryActivateBasicAction", new[] { typeof(Character), typeof(int) }), HarmonyPrefix]
        static bool InteractionTriggerBase_TryActivate_Pre(InteractionTriggerBase __instance, ref Character _character)
        {
            InteractionsInCombat flags = _disallowedInCombat.Value;
            #region quit
            if (_character == null || !_character.InCombat || !__instance.CurrentTriggerManager.TryAs<InteractionActivator>(out var interactionActivator)
            || (interactionActivator.BasicInteraction.IsNot<InteractionOpenContainer>() || !flags.HasFlag(InteractionsInCombat.Loot))
            && (interactionActivator.BasicInteraction.IsNot<InteractionSwitchArea>() || !flags.HasFlag(InteractionsInCombat.Travel))
            && (interactionActivator.BasicInteraction.IsNot<InteractionWarp>() || !flags.HasFlag(InteractionsInCombat.Warp))
            && (interactionActivator.BasicInteraction.IsNot<InteractionToggleContraption>() || !flags.HasFlag(InteractionsInCombat.PullLever))
            && (interactionActivator.BasicInteraction.IsNot<InteractionRevive>() || !flags.HasFlag(InteractionsInCombat.PullLever)))
                return true;
            #endregion

            _character.CharacterUI.ShowInfoNotification(DISALLOW_IN_COMBAT_NOTIFICATION);
            return false;
        }
    }
}

/*
static private ModSetting<bool> _overrideIsInCombat;
_overrideIsInCombat = CreateSetting(nameof(_overrideIsInCombat), false);
_overrideIsInCombat.Format("Override \"InCombat\"");

[HarmonyPatch(typeof(Character), "InCombat", MethodType.Getter), HarmonyPrefix]
static bool Character_InCombat_Pre(Character __instance, ref bool __result)
{
    __result = _overrideIsInCombat;
    return false;
}
*/

/*
static private ModSetting<bool> _mobileUseToggle;
static private ModSetting<int> _mobileEatSpeed, _mobileDrinkSpeed, _mobileInfuseSpeed;

_mobileUseToggle = CreateSetting(nameof(_mobileUseToggle), false);
_mobileEatSpeed = CreateSetting(nameof(_mobileEatSpeed), 50, IntRange(0, 100));
_mobileDrinkSpeed = CreateSetting(nameof(_mobileDrinkSpeed), 50, IntRange(0, 100));
_mobileInfuseSpeed = CreateSetting(nameof(_mobileInfuseSpeed), 50, IntRange(0, 100));

_mobileUseToggle.Format("Use items while moving");
Indent++;
{
    _mobileEatSpeed.Format("Eat move speed", _mobileUseToggle);
    _mobileDrinkSpeed.Format("Drink move speed", _mobileUseToggle);
    _mobileInfuseSpeed.Format("Infuse move speed", _mobileUseToggle);
    Indent--;
}

static private void TryUpdateMobileUse()
{
    #region MyRegion
    if (!_mobileUseToggle)
        return;
    #endregion

    foreach (var ingestibleByName in Prefabs.IngestiblesByName)
    {
        Item item = ingestibleByName.Value;
        if (item.IsEatable())
            item.MobileCastMovementMult = _mobileEatSpeed / 100f;
        else if (item.IsDrinkable())
            item.MobileCastMovementMult = _mobileDrinkSpeed / 100f;

        item.CastModifier = item.MobileCastMovementMult > 0 ? Character.SpellCastModifier.Mobile : Character.SpellCastModifier.Immobilized;
    }

    foreach (var infusableByName in Prefabs.InfusablesByName)
    {
        Item item = infusableByName.Value;
        item.MobileCastMovementMult = _mobileInfuseSpeed / 100f;

        item.CastModifier = item.MobileCastMovementMult > 0 ? Character.SpellCastModifier.Mobile : Character.SpellCastModifier.Immobilized;
    }
}
*/