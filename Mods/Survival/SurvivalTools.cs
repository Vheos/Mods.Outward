using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



namespace ModPack
{
    public class SurvivalTools : AMod
    {
        #region const
        private const float BED_DISTANCE_BETWEEN_PLAYERS = 0.4f;
        static private readonly int[] TORCH_IDS = new[]
        {
            "Makeshift Torch".ItemID(),
            "Ice-Flame Torch".ItemID(),
        };
        private const string FLINT_AND_STEEL_BREAK_NOTIFICATION = "Flint and Steel broke!";
        private const int PRIMITIVE_SATCHEL_CAPACITY = 25;
        private const int TRADER_BACKPACK = 100;
        #endregion

        // Settings
        static private ModSetting<bool> _moreGatheringTools;
        static private ModSetting<Vector2> _gatheringDurabilityCost;
        static private ModSetting<int> _chanceToBreakFlintAndSteel;
        static private ModSetting<int> _waterskinCapacity;
        static private ModSetting<Vector2> _remapBackpackCapacities;
        static private ModSetting<float> _torchesTemperatureRadius;
        static private ModSetting<bool> _torchesDecayOnGround;
        static private ModSetting<int> _lightsRange;
        static private ModSetting<bool> _twoPersonBeds;
        override protected void Initialize()
        {
            _moreGatheringTools = CreateSetting(nameof(_moreGatheringTools), false);
            _gatheringDurabilityCost = CreateSetting(nameof(_gatheringDurabilityCost), new Vector2(0f, 5f));
            _chanceToBreakFlintAndSteel = CreateSetting(nameof(_chanceToBreakFlintAndSteel), 0, IntRange(0, 100));
            _waterskinCapacity = CreateSetting(nameof(_waterskinCapacity), 5, IntRange(1, 18));
            _remapBackpackCapacities = CreateSetting(nameof(_remapBackpackCapacities), new Vector2(PRIMITIVE_SATCHEL_CAPACITY, TRADER_BACKPACK));
            _torchesTemperatureRadius = CreateSetting(nameof(_torchesTemperatureRadius), 1f, FloatRange(0, 10));
            _torchesDecayOnGround = CreateSetting(nameof(_torchesDecayOnGround), false);
            _lightsRange = CreateSetting(nameof(_lightsRange), 100, IntRange(50, 200));
            _twoPersonBeds = CreateSetting(nameof(_twoPersonBeds), false);
        }
        override protected void SetFormatting()
        {
            _moreGatheringTools.Format("More gathering tools");
            _moreGatheringTools.Description = "Any Spear can fish and any 2-Handed Mace can mine\n" +
                                              "The tool is searched for in your bag, then pouch, then equipment\n" +
                                              "If there is more than 1 valid tool, the cheapest one is chosen first";
            _gatheringDurabilityCost.Format("Gathering tools durability cost");
            _gatheringDurabilityCost.Description = "X   -   flat amount\n" +
                                                   "Y   -   percent of max";
            _chanceToBreakFlintAndSteel.Format("Chance to break \"Flint and Steel\"");
            _chanceToBreakFlintAndSteel.Description = "Each time you use Flint and Steel, there's a X% chance it will break";
            _waterskinCapacity.Format("Waterskin capacity");
            _waterskinCapacity.Description = "Have one big waterskin instead of a few small ones so you don't have to swap quickslots";
            _remapBackpackCapacities.Format("Remap backpack capacities");
            _remapBackpackCapacities.Description = "X   -   Primitive Satchel's capacity\n" +
                                                   "Y   -   Trader Backpack's capacity\n" +
                                                   "(all other backpacks will have their capacities scaled accordingly)";
            _torchesTemperatureRadius.Format("Torches temperature radius");
            _torchesTemperatureRadius.Description = "Increase to share a torch's temperature with your friend eaiser";
            _torchesDecayOnGround.Format("Torches burn out on ground");
            _torchesDecayOnGround.Description = "Normally, torches don't burn out when on ground, even if they are lit and provide temperature";
            _lightsRange.Format("Lights range");
            _lightsRange.Description = "Multiplies torches' and lanterns' lighting range (in %)";
            _twoPersonBeds.Format("Two-person beds");
            _twoPersonBeds.Description = "All beds, tents and bedrolls will allow for 2 users at the same time";

        }
        override protected string Description
        => "• Allow gathering with more tools\n" +
           "• Control gathering tools durability cost\n" +
           "• Make Flint and Steel break randomly\n" +
           "• Change Waterskin capacity\n" +
           "• Change backpack capacities";
        override protected string SectionOverride
        => SECTION_SURVIVAL;
        override protected string ModName
        => "Tools";
        override public void LoadPreset(Presets.Preset preset)
        {
            switch (preset)
            {
                case Presets.Preset.Vheos_CoopSurvival:
                    ForceApply();
                    _remapBackpackCapacities.Value = new Vector2(20, 60);
                    _waterskinCapacity.Value = 9;
                    _chanceToBreakFlintAndSteel.Value = 25;
                    _moreGatheringTools.Value = true;
                    _gatheringDurabilityCost.Value = new Vector2(15, 3);
                    _torchesTemperatureRadius.Value = 5;
                    _torchesDecayOnGround.Value = true;
                    _lightsRange.Value = 133;
                    _twoPersonBeds.Value = true;
                    break;
            }
        }

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        // More gathering tools
        [HarmonyPatch(typeof(GatherableInteraction), "GetValidItem"), HarmonyPrefix]
        static bool GatherableInteraction_GetValidItem_Pre(GatherableInteraction __instance, ref Item __result, Character _character)
        {
            #region quit
            if (!_moreGatheringTools || !__instance.Gatherable.RequiredItem.TryAssign(out var requiredItem)
            || requiredItem.ItemID != "Mining Pick".ItemID() && requiredItem.ItemID != "Fishing Harpoon".ItemID())
                return true;
            #endregion

            // Cache
            Weapon.WeaponType requiredType = requiredItem.ItemID == "Fishing Harpoon".ItemID() ? Weapon.WeaponType.Spear_2H : Weapon.WeaponType.Mace_2H;
            List<Item> potentialTools = new List<Item>();

            // Search bag & pouch
            List<ItemContainer> containers = new List<ItemContainer>();
            if (_character.Inventory.EquippedBag.TryAssign(out var bag))
                containers.Add(bag.m_container);
            if (_character.Inventory.Pouch.TryAssign(out var pouch))
                containers.Add(pouch);

            foreach (var container in containers)
                if (potentialTools.IsEmpty())
                    foreach (var item in container.GetContainedItems())
                        if (item.TryAs(out Weapon weapon) && weapon.Type == requiredType && weapon.DurabilityRatio > 0)
                            potentialTools.Add(item);

            // Search equipment
            if (potentialTools.IsEmpty()
            && _character.Inventory.Equipment.m_equipmentSlots[(int)EquipmentSlot.EquipmentSlotIDs.RightHand].EquippedItem.TryAs(out Weapon mainWeapon)
            && mainWeapon.Type == requiredType && mainWeapon.DurabilityRatio > 0)
                potentialTools.Add(mainWeapon);

            // Choose tool
            Item chosenTool = null;
            if (potentialTools.IsNotEmpty())
            {
                int minValue = potentialTools.Min(tool => tool.RawCurrentValue);
                chosenTool = potentialTools.First(tool => tool.RawCurrentValue == minValue);
            }

            // Finalize
            __instance.m_validItem = chosenTool;
            __instance.m_isCurrentWeapon = chosenTool != null && chosenTool.IsEquipped;
            __result = chosenTool;
            return false;
        }

        // Gathering durability cost
        [HarmonyPatch(typeof(GatherableInteraction), "CharSpellTakeItem"), HarmonyPrefix]
        static bool GatherableInteraction_CharSpellTakeItem_Pre(GatherableInteraction __instance)
        {
            #region quit
            if (!__instance.m_validItem.TryAssign(out var item))
                return true;
            #endregion

            item.ReduceDurability(_gatheringDurabilityCost.Value.x + (_gatheringDurabilityCost.Value.y - 5) / 100f * item.MaxDurability);
            return true;
        }

        // Chance to break Flint and Steel
        [HarmonyPatch(typeof(Item), "OnUse"), HarmonyPostfix]
        static void Item_OnUse_Post(Item __instance)
        {
            #region quit
            if (__instance.ItemID != "Flint and Steel".ItemID()
            || UnityEngine.Random.value >= _chanceToBreakFlintAndSteel / 100f)
                return;
            #endregion

            __instance.RemoveQuantity(1);
            __instance.m_ownerCharacter.CharacterUI.ShowInfoNotification(FLINT_AND_STEEL_BREAK_NOTIFICATION);
        }

        // Waterskin capacity
        [HarmonyPatch(typeof(WaterContainer), "RefreshDisplay"), HarmonyPrefix]
        static bool WaterContainer_RefreshDisplay_Pre(WaterContainer __instance)
        {
            __instance.m_stackable.m_maxStackAmount = _waterskinCapacity;
            return true;
        }

        [HarmonyPatch(typeof(Item), "OnAwake"), HarmonyPostfix]
        static void Item_Awake_Post(Item __instance)
        {
            #region quit
            if (__instance.IsNot<WaterContainer>())
                return;
            #endregion

            __instance.m_stackable.m_maxStackAmount = _waterskinCapacity;
        }

        // Remap backpack capacities
        [HarmonyPatch(typeof(ItemContainer), "ContainerCapacity", MethodType.Getter), HarmonyPostfix]
        static void ItemContainer_ContainerCapacity_Post(ItemContainer __instance, ref float __result)
        {
            if (__instance.RefBag == null || __instance.m_baseContainerCapacity <= 0)
                return;

            __result = __result.Map(PRIMITIVE_SATCHEL_CAPACITY, TRADER_BACKPACK,
                                    _remapBackpackCapacities.Value.x, _remapBackpackCapacities.Value.y).Round();
        }

        // Torches temperature radius
        [HarmonyPatch(typeof(TemperatureSource), "Start"), HarmonyPostfix]
        static void TemperatureSource_Start_Post(TemperatureSource __instance)
        {
            #region quit
            if (!__instance.m_item.TryAssign(out var item) || item.ItemID.IsNotContainedIn(TORCH_IDS))
                return;
            #endregion

            __instance.DistanceRanges = new List<Vector2> { new Vector2(0, _torchesTemperatureRadius) };
            __instance.m_maxDistance = _torchesTemperatureRadius;
            if (TemperatureSource.BiggestRangeInScene < _torchesTemperatureRadius)
                TemperatureSource.BiggestRangeInScene = _torchesTemperatureRadius;
            __instance.m_temperatureCollider.As<SphereCollider>().radius = _torchesTemperatureRadius;
            item.PerishScript.DontPerishInWorld = !_torchesDecayOnGround;
            return;
        }

        // Lights intensity
        [HarmonyPatch(typeof(ItemLanternVisual), "Awake"), HarmonyPostfix]
        static void ItemLanternVisual_Awake_Post(ItemLanternVisual __instance)
        {
            float modifier = _lightsRange / 100f;
            __instance.LanternLight.range *= modifier;
        }

        // Tents capacity
        [HarmonyPatch(typeof(Sleepable), "RequestSleepableRoom"), HarmonyPrefix]
        static bool Sleepable_RequestSleepableRoom_Pre(Sleepable __instance)
        {
            #region quit
            if (!_twoPersonBeds || Global.Lobby.PlayersInLobbyCount <= 1)
                return true;
            #endregion

            __instance.Capacity = 2;
            __instance.CharAnimOffset.SetX(BED_DISTANCE_BETWEEN_PLAYERS / 2f * (__instance.m_occupants.Count == 0 ? -1f : +1f));
            return true;
        }
    }
}