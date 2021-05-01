using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



namespace ModPack
{
    static public class Prefabs
    {
        #region const
        static public readonly Dictionary<string, int> ItemIDsByName = new Dictionary<string, int>
        {
            ["Torcrab Egg"] = 4000480,
            ["Boreo Blubber"] = 4000500,
            ["Pungent Paste"] = 4100190,
            ["Gaberry Jam"] = 4100030,
            ["Crawlberry Jam"] = 4100710,
            ["Golden Jam"] = 4100800,
            ["Raw Torcrab Meat"] = 4000470,
            ["Miner’s Omelet"] = 4100280,
            ["Turmmip Potage"] = 4100270,
            ["Meat Stew"] = 4100220,
            ["Marshmelon Jelly"] = 4100420,
            ["Blood Mushroom"] = 4000150,
            ["Food Waste"] = 4100000,
            ["Warm Boozu’s Milk"] = 4100680,

            ["Panacea"] = 4300370,
            ["Antidote"] = 4300110,
            ["Hex Cleaner"] = 4300190,
            ["Invigorating Potion"] = 4300280,

            ["Able Tea"] = 4200090,
            ["Bitter Spicy Tea"] = 4200050,
            ["Greasy Tea"] = 4200110,
            ["Iced Tea"] = 4200100,
            ["Mineral Tea"] = 4200080,
            ["Needle Tea"] = 4200070,
            ["Soothing Tea"] = 4200060,
            ["Boozu’s Milk"] = 4000380,
            ["Gaberry Wine"] = 4100590,
            ["Gep's Drink"] = 4300040,

            ["Waterskin"] = 4200040,
            ["Ambraine"] = 4000430,
            ["Bandages"] = 4400010,
            ["Life Potion"] = 4300010,

            ["MistakenIngestible"] = 4500020,

            ["Old Legion Shield"] = 2300070,
            ["Boiled Azure Shrimp"] = 4100540,
            ["Coralhorn Antler"] = 6600060,
            ["Great Astral Potion"] = 4300250,
            ["Alpha Tuanosaur Tail"] = 6600190,
            ["Crystal Powder"] = 6600040,
            ["Manticore Tail"] = 6600150,
            ["Gold Ingot"] = 6300030,
            ["Tsar Stone"] = 6200010,
        };
        static public readonly Dictionary<string, int> SkillIDsByName = new Dictionary<string, int>
        {
            ["Puncture"] = 8100290,
            ["Pommel Counter"] = 8100362,
            ["Talus Cleaver"] = 8100380,
            ["Execution"] = 8100300,
            ["Mace Infusion"] = 8100270,
            ["Juggernaut"] = 8100310,
            ["Simeon's Gambit"] = 8100340,
            ["Moon Swipe"] = 8100320,
            ["Prismatic Flurry"] = 8201040,
            ["Flamethrower"] = 8100090,
            ["Mist"] = 8200170,
            ["Warm"] = 8200130,
            ["Cool"] = 8200140,
            ["Blessed"] = 8200180,
            ["Possessed"] = 8200190,
            ["Haunt Hex"] = 8201024,
            ["Scorch Hex"] = 8201020,
            ["Chill Hex"] = 8201021,
            ["Doom Hex"] = 8201022,
            ["Curse Hex"] = 8201023,

            ["Dagger Slash"] = 8100072,
            ["Backstab"] = 8100070,
            ["Opportunist Stab"] = 8100071,
            ["Serpent's Parry"] = 8100261,

            ["Evasion Shot"] = 8100100,
            ["Sniper Shot"] = 8100101,
            ["Piercing Shot"] = 8100102,

            ["Dez"] = 8100210,
            ["Shim"] = 8100220,
            ["Egoth"] = 8100230,
            ["Fal"] = 8100240,
        };
        #endregion

        // Publics
        static public Dictionary<string, Item> ItemsByID
        => ResourcesPrefabManager.ITEM_PREFABS;
        static public Dictionary<string, StatusEffect> StatusEffectsByID
        => ResourcesPrefabManager.STATUSEFFECT_PREFABS;
        static public Dictionary<string, QuestEventSignature> QuestsByID
        => QuestEventDictionary.m_questEvents;
        static public Dictionary<int, Skill> SkillsByID
        { get; private set; }
        static public Dictionary<int, Item> IngestiblesByID
        { get; private set; }
        static public List<StatusEffect> AllSleepBuffs
        { get; private set; }
        static public bool IsInitialized
        { get; private set; }
        static public Skill GetSkillByName(string name)
        => SkillsByID[SkillIDsByName[name]];
        static public Item GetIngestibleByName(string name)
        => IngestiblesByID[ItemIDsByName[name]];

        // Initializers
        static public void Initialize()
        {
            SkillsByID = new Dictionary<int, Skill>();
            IngestiblesByID = new Dictionary<int, Item>();
            foreach (var itemByID in ItemsByID)
            {
                Item item = itemByID.Value;

                if (item.TryAs(out Skill skill))
                    SkillsByID.Add(skill.ItemID, skill);

                if (item.IsUsable
                && (item.IsEatable() || item.IsDrinkable())
                && item.ItemID != "MistakenIngestible".ItemID())
                    IngestiblesByID.Add(item.ItemID, item);
            }

            AllSleepBuffs = new List<StatusEffect>();
            foreach (var statusEffect in Resources.FindObjectsOfTypeAll<StatusEffect>())
                if (statusEffect.GOName().ContainsSubstring("SleepBuff"))
                    AllSleepBuffs.Add(statusEffect);

            IsInitialized = true;
        }
    }
}

/*
static public Dictionary<Item, Sleepable> SleepablesByItem
{ get; private set; }

SleepablesByItem = new Dictionary<Item, Sleepable>();

Sleepable sleepable = item.GetComponent<Sleepable>();
if (sleepable != null)
SleepablesByItem.Add(item, sleepable);
*/