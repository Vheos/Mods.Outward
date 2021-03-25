using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Reflection;
using System.Text;
using System;



namespace ModPack
{
    public class Interactions : AMod, IWaitForPrefabs
    {
        #region const
        public const float ANIMATED_TAKE_DELAY = 0.3f;
        public const float STANDING_TAKE_MIN_HEIGHT = 0.7f;
        public const float UNPACK_OFFSET_Y = 0.2f;
        public const float UNPACK_BUMP_FORCE = 60f;
        public const float UNPACK_USE_DELAY = 0.1f;
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
                Vector2 directionXZ = UnityEngine.Random.insideUnitCircle.normalized;
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
        override protected void Initialize()
        {
            _holdInteractionsDuration = CreateSetting(nameof(_holdInteractionsDuration), GameInput.HOLD_THRESHOLD + GameInput.HOLD_DURATION, FloatRange(0.1f + GameInput.HOLD_THRESHOLD, 5f));
            _swapWaterInteractions = CreateSetting(nameof(_swapWaterInteractions), false);
            _singleHoldsToPresses = CreateSetting(nameof(_singleHoldsToPresses), false);
            _takeAnimations = CreateSetting(nameof(_takeAnimations), false);
            _groundInteractions = CreateSetting(nameof(_groundInteractions), GroundInteractions.None);
        }
        override protected void SetFormatting()
        {
            _holdInteractionsDuration.Format("Hold interactions duration");
            _holdInteractionsDuration.Description = "How long you want to hold the button for \"Hold\" interaction to trigger";
            _swapWaterInteractions.Format("Swap water gather/drink");
            _swapWaterInteractions.Description = "Vanilla: press to gather, hold to drink\n" +
                                                 "Custom: press to drink, hold to gather";
            _singleHoldsToPresses.Format("Instant hold-only interactions");
            _singleHoldsToPresses.Description = "Changes many objects' \"Hold\" interaction to \"Press\"\n" +
                                                "(examples: gathering, fishing, mining, opening chests";
            _takeAnimations.Format("Item take animations");
            _takeAnimations.Description = "Animates (and greatly slows down) the process of taking items";

            _groundInteractions.Format("Use items from ground");
            _groundInteractions.Description = "Items to use straight from the ground with a \"Hold\" interaction";
        }
        override protected string Description
        => "• Instant interactions\n" +
           "• \"Take item\" animations\n" +
           "• Use items lying on the ground";

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
                else if (_groundInteractions.Value.HasFlag(GroundInteractions.UseBandage) && item.ItemID == "Bandages".ID())
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
        [HarmonyPatch(typeof(InteractionBase), "HoldActivationTime", MethodType.Getter), HarmonyPrefix]
        static bool InteractionBase_HoldActivationTime_Getter_Post(ref float __result, ref float ___m_holdActivationTimeOverride)
        {
            __result = ___m_holdActivationTimeOverride != -1 ? ___m_holdActivationTimeOverride : _holdInteractionsDuration;
            return false;
        }

        [HarmonyPatch(typeof(InteractionActivator), "OnLateInit"), HarmonyPostfix]
        static void InteractionActivator_OnLateInit_Post(ref InteractionActivator __instance, ref IInteraction ___m_defaultBasicInteraction, ref IInteraction ___m_defaultHoldInteraction)
        {
            SwapBasicAndHoldInteractions(__instance, ref ___m_defaultBasicInteraction, ref ___m_defaultHoldInteraction);
            AddCustomHoldInteractions(__instance, ref ___m_defaultHoldInteraction);
            AddAnimationsToTakeInteractions(__instance, ref ___m_defaultBasicInteraction);
        }
    }
}

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