using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



namespace ModPack
{
    public class Debug : AMod, IUpdatable, IDevelopmentOnly
    {
        override protected void Initialize()
        {
        }
        override protected void SetFormatting()
        {
        }
        public void OnUpdate()
        {
            if (KeyCode.LeftAlt.Held() && KeyCode.Keypad0.Pressed())
            {
                string[] blacklist =
                {
                   "m_localizedDescription", "Description",
                };

                foreach (var itemByID in Prefabs.ItemsByID)
                    if (itemByID.Value.TryAs(out Equipment equipment))
                        Tools.Log(itemByID.Key);

                Tools.Log($"~~~~");

                typeof(Equipment).Dump(blacklist, Data.Names, Members.FieldsAndProperties);
                typeof(Equipment).Dump(blacklist, Data.Types, Members.FieldsAndProperties);
                foreach (var itemByID in Prefabs.ItemsByID)
                    if (itemByID.Value.TryAs(out Equipment equipment))
                        equipment.Dump(typeof(Equipment), blacklist, Data.Values, Members.FieldsAndProperties);

                Tools.Log($"~~~~");

                typeof(EquipmentStats).Dump(blacklist, Data.Names, Members.FieldsAndProperties);
                typeof(EquipmentStats).Dump(blacklist, Data.Types, Members.FieldsAndProperties);
                foreach (var itemByID in Prefabs.ItemsByID)
                    if (itemByID.Value.TryAs(out Equipment equipment))
                        if (equipment.Stats != null)
                            equipment.Stats.Dump(typeof(EquipmentStats), blacklist, Data.Values, Members.FieldsAndProperties);
                        else
                            Tools.Log($"null");
            }

        }
        override protected string SectionOverride
        => SECTION_VARIOUS;


    }
}

/*
 *                     foreach(var chakramSkillID in new[] { 8100250, 8100251, 8100252 })
                        if(Prefabs.ItemsByID[chakramSkillID.ToString()].TryAs(out AttackSkill chakramSkill))
                        {
                            chakramSkill.RequiredOffHandTypes.Clear();//Add(Weapon.WeaponType.Shield);
                            chakramSkill.Cooldown = 1;
                        }    
 */

/*

*/

/*
 * foreach (var tree in SkillTreeHolder.Instance.m_skillTrees)
                    {
                        if (tree.BreakthroughSkill == null)
                            break;

                        int basic = 0;
                        int advanced = 0;
                        int breakthroughRow = tree.BreakthroughSkill.ParentBranch.Index;
                        foreach (var slot in tree.m_skillSlots)
                        {
                            int slotRow = slot.ParentBranch.Index;
                            if (slotRow < breakthroughRow)
                                basic++;
                            else if (slotRow > breakthroughRow)
                                advanced++;
                        }
                        Tools.Log($"{tree.Name}\t{basic}\t{advanced}");
                    }
 */

/*
 * foreach (var gatherable in GameObject.FindObjectsOfType<Gatherable>())
                {
                    Tools.Log($"{gatherable.DisplayName}");
                    // Execute
                    foreach (var dropable in gatherable.m_drops)
                    {
                        Tools.Log($"\t{dropable.name}");
                        foreach (var dropTable in dropable.m_mainDropTables)
                        {
                            SimpleRandomChance dropAmount = dropTable.m_dropAmount;
                            Tools.Log($"\t\t{dropTable.ItemGenatorName}\t" +
                                $"MaxRoll: {dropTable.m_maxDiceValue}\t" +
                                $"Regen: {dropAmount.ChanceRegenQty}");

                            foreach (var itemDropChance in dropTable.m_itemDrops)
                            {
                                Tools.Log($"\t\t\t{itemDropChance.DroppedItem.DisplayName}\t" +
                                    $"OnRolls: {itemDropChance.MinDiceRollValue}-{itemDropChance.MaxDiceRollValue}\t" +
                                    $"Qty: {itemDropChance.MinDropCount}-{itemDropChance.MaxDropCount}\t" +
                                    $"Regen: {itemDropChance.ChanceRegenDelay}");
                            }
                        }
                    }
                    Tools.Log($"\n");
                }
 */

/*
    if (KeyCode.Keypad0.Pressed())
    {
        foreach (var ingestibleByID in Prefabs.IngestiblesByID)
        {
            Item item = ingestibleByID.Value;
            Players.GetLocal(0).Character.Inventory.GenerateItem(item, 1, false);
        }
    }
    if (KeyCode.Keypad1.Pressed())
        Players.GetLocal(0).Character.StatusEffectMngr.AddStatusEffect("Burning");
    if (KeyCode.Keypad2.Pressed())
        Players.GetLocal(0).Character.StatusEffectMngr.AddStatusEffect("Poisoned +");
    if (KeyCode.Keypad3.Pressed())
        Players.GetLocal(0).Character.StatusEffectMngr.AddStatusEffect("Bleeding +");
    if (KeyCode.Keypad4.Pressed())
        Players.GetLocal(0).Character.StatusEffectMngr.AddStatusEffect("Blaze");
    if (KeyCode.Keypad5.Pressed())
        Players.GetLocal(0).Character.StatusEffectMngr.AddStatusEffect("HolyBlaze");
    if (KeyCode.Keypad6.Pressed())
        Players.GetLocal(0).Character.StatusEffectMngr.AddStatusEffect("Plague");
    if (KeyCode.Keypad7.Pressed())
        Players.GetLocal(0).Character.StatusEffectMngr.AddStatusEffect("Infection1");
 */

/*
 *         // Setting
        static public ModSetting<int> _currentVal, _maxVal, _activeMax;
        static public ModSetting<bool> _forceSet;
        override protected void Initialize()
        {
            _currentVal = CreateSetting(nameof(_currentVal), 0, IntRange(0, 100));
            _maxVal = CreateSetting(nameof(_maxVal), 100, IntRange(0, 100));
            _activeMax = CreateSetting(nameof(_activeMax), 75, IntRange(0, 75));
            _forceSet = CreateSetting(nameof(_forceSet), false);

            _currentVal.AddEvent(TryUpdateCustomBar);
            _maxVal.AddEvent(TryUpdateCustomBar);
            _activeMax.AddEvent(TryUpdateCustomBar);
        }
        override protected void SetFormatting()
        {
            _currentVal.Format("Current");
            _maxVal.Format("Max");
            _activeMax.Format("ActiveMax");
        }
        public void OnUpdate()
        {
            if (KeyCode.LeftAlt.Held())
            {
                if (KeyCode.Keypad0.Pressed())
                {
                }
            }
        }

        // Utility
        static private Bar _customBar;
        static private void TryUpdateCustomBar()
        {
            if (_customBar != null)
                _customBar.UpdateBar(_currentVal, _maxVal, _activeMax, _forceSet);
        }

        // Hooks
        [HarmonyPatch(typeof(LocalCharacterControl), "RetrieveComponents"), HarmonyPostfix]
        static void LocalCharacterControl_RetrieveComponents_Post(LocalCharacterControl __instance)
        {
            if (_customBar != null)
                return;

            Transform manaBar = Players.GetLocal(0).UI.transform.Find("Canvas/GameplayPanels/HUD/MainCharacterBars/Mana");
            _customBar = GameObject.Instantiate(manaBar).GetComponent<Bar>();
            _customBar.name = "CustomBar";
            _customBar.BecomeSiblingOf(manaBar);
            GameObject.DontDestroyOnLoad(_customBar);
        }
*/