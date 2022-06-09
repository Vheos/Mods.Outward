namespace Vheos.Mods.Outward;

public static class Prefabs
{
    // Publics
    public static IReadOnlyDictionary<int, Item> ItemsByID => _itemsByID;
    public static IReadOnlyDictionary<int, Skill> SkillsByID => _skillsByID;
    public static IReadOnlyDictionary<int, Item> IngestiblesByID => _ingestiblesByID;
    public static IReadOnlyList<StatusEffect> SleepBuffs => _sleepBuffs;
    public static IReadOnlyDictionary<string, StatusEffect> StatusEffectsByNameID
    => ResourcesPrefabManager.STATUSEFFECT_PREFABS;
    public static IReadOnlyDictionary<string, Recipe> RecipesByUID
    => RecipeManager.Instance.m_recipes;
    public static IReadOnlyDictionary<string, QuestEventSignature> QuestsByID
    => QuestEventDictionary.m_questEvents;
    public static bool IsInitialized
    { get; private set; }

    // Privates
    private static Dictionary<int, Item> _itemsByID;
    private static Dictionary<int, Item> _ingestiblesByID;
    private static Dictionary<int, Skill> _skillsByID;
    private static List<StatusEffect> _sleepBuffs;

    // Initializers
    public static void Initialize()
    {
        _itemsByID = new();
        _skillsByID = new();
        _ingestiblesByID = new();
        int mistakenIngestibleID = "MistakenIngestible".ToItemID();

        foreach (var itemByID in ResourcesPrefabManager.ITEM_PREFABS)
        {
            int id = itemByID.Key.ToInt();
            Item item = itemByID.Value;

            _itemsByID.Add(id, item);

            if ((item.IsEatable() || item.IsDrinkable())
            && !item.SharesPrefabWith(mistakenIngestibleID))
                _ingestiblesByID.Add(item.ItemID, item);

            if (item.TryAs(out Skill skill))
                _skillsByID.Add(skill.ItemID, skill);
        }

        _sleepBuffs = new List<StatusEffect>();
        foreach (var statusEffect in Resources.FindObjectsOfTypeAll<StatusEffect>())
            if (statusEffect.NameContains("SleepBuff"))
                _sleepBuffs.Add(statusEffect);

        IsInitialized = true;
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