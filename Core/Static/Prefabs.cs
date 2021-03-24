using System.Collections.Generic;
using UnityEngine;



namespace ModPack
{
    static public class Prefabs
    {
        // Publics
        static public Dictionary<string, Item> ItemsByID
        => ResourcesPrefabManager.ITEM_PREFABS;
        static public Dictionary<string, StatusEffect> StatusEffectsByID
        => ResourcesPrefabManager.STATUSEFFECT_PREFABS;
        static public Dictionary<string, QuestEventSignature> QuestsByID
        => QuestEventDictionary.m_questEvents;
        static public Dictionary<string, Item> IngestiblesByGOName
        { get; private set; }
        static public Dictionary<string, Item> InfusablesByGOName
        { get; private set; }
        static public List<StatusEffect> AllSleepBuffs
        { get; private set; }
        static public bool IsInitialized
        { get; private set; }

        // Initializers
        static public void Initialize()
        {
            IngestiblesByGOName = new Dictionary<string, Item>();
            InfusablesByGOName = new Dictionary<string, Item>();

            foreach (var itemByID in ItemsByID)
            {
                Item item = itemByID.Value;

                if (item.IsUsable
                && (item.IsEatable() || item.IsDrinkable())
                && item.GONameIsNot("4500020_ThunderPaper"))
                    IngestiblesByGOName.Add(item.GOName(), item);

                if (item is InfuseConsumable)
                    InfusablesByGOName.Add(item.GOName(), item);
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