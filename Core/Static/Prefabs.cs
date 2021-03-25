using System.Collections.Generic;
using UnityEngine;



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

            ["Ambraine"] = 4000430,
            ["Bandages"] = 4400010,
            ["Life Potion"] = 4300010,

            ["MistakenIngestible"] = 4500020,
        };
        #endregion

        // Publics
        static public Dictionary<string, Item> ItemsByID
        => ResourcesPrefabManager.ITEM_PREFABS;
        static public Dictionary<string, StatusEffect> StatusEffectsByID
        => ResourcesPrefabManager.STATUSEFFECT_PREFABS;
        static public Dictionary<string, QuestEventSignature> QuestsByID
        => QuestEventDictionary.m_questEvents;
        static public Dictionary<int, Item> IngestiblesByID
        { get; private set; }
        static public List<StatusEffect> AllSleepBuffs
        { get; private set; }
        static public bool IsInitialized
        { get; private set; }
        static public Item GetIngestibleByName(string name)
        => IngestiblesByID[ItemIDsByName[name]];

        // Initializers
        static public void Initialize()
        {
            IngestiblesByID = new Dictionary<int, Item>();
            foreach (var itemByID in ItemsByID)
            {
                Item item = itemByID.Value;

                if (item.IsUsable
                && (item.IsEatable() || item.IsDrinkable())
                && item.ItemID != "MistakenIngestible".ID())
                    IngestiblesByID.Add(itemByID.Value.ItemID, item);
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