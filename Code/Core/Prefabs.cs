namespace Vheos.Mods.Outward;

static public class Prefabs
{
    #region const
    static public readonly Dictionary<string, int> ItemIDsByName = new()
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

        ["Explorer Lantern"] = 5100000,
        ["Old Lantern"] = 5100010,
        ["Glowstone Lantern"] = 5100020,
        ["Firefly Lantern"] = 5100030,
        ["Lantern of Souls"] = 5100080,
        ["Coil Lantern"] = 5100090,
        ["Virgin Lantern"] = 5100100,
        ["Djinn’s Lamp"] = 5100110,

        ["Calixa’s Relic"] = 6600225,
        ["Elatt’s Relic"] = 6600222,
        ["Gep’s Generosity"] = 6600220,
        ["Haunted Memory"] = 6600224,
        ["Leyline Figment"] = 6600226,
        ["Pearlbird’s Courage"] = 6600221,
        ["Scourge’s Tears"] = 6600223,
        ["Vendavel's Hospitality"] = 6600227,
        ["Flowering Corruption"] = 6600228,
        ["Metalized Bones"] = 6600230,
        ["Enchanted Mask"] = 6600229,
        ["Noble’s Greed"] = 6600232,
        ["Scarlet Whisper"] = 6600231,
        ["Calygrey’s Wisdom"] = 6600233,

        ["Hailfrost Claymore"] = 2100270,
        ["Hailfrost Mace"] = 2020290,
        ["Hailfrost Hammer"] = 2120240,
        ["Hailfrost Axe"] = 2010250,
        ["Hailfrost Greataxe"] = 2110230,
        ["Hailfrost Spear"] = 2130280,
        ["Hailfrost Halberd"] = 2150110,
        ["Hailfrost Pistol"] = 5110270,
        ["Hailfrost Knuckles"] = 2160200,
        ["Hailfrost Sword"] = 2000280,
        ["Hailfrost Dagger"] = 5110015,

        ["Mysterious Blade"] = 2000320,
        ["Mysterious Long Blade"] = 2100300,
        ["Ceremonial Bow"] = 2200190,
        ["Cracked Red Moon"] = 2150180,
        ["Compasswood Staff"] = 2150030,
        ["Scarred Dagger"] = 5110340,
        ["De-powered Bludgeon"] = 2120270,
        ["Unusual Knuckles"] = 2160230,
        ["Strange Rusted Sword"] = 2000151,

        ["Flint and Steel"] = 5600010,
        ["Fishing Harpoon"] = 2130130,
        ["Mining Pick"] = 2120050,

        ["Makeshift Torch"] = 5100060,
        ["Ice-Flame Torch"] = 5100070,
    };
    static public readonly Dictionary<string, int> SkillIDsByName = new()
    {
        // Weapon skills
        ["Puncture"] = 8100290,
        ["Pommel Counter"] = 8100362,
        ["Talus Cleaver"] = 8100380,
        ["Execution"] = 8100300,
        ["Mace Infusion"] = 8100270,
        ["Juggernaut"] = 8100310,
        ["Simeon's Gambit"] = 8100340,
        ["Moon Swipe"] = 8100320,
        ["Prismatic Flurry"] = 8201040,
        // Weapon Master skills
        ["The Technique"] = 8100530,
        ["Moment of Truth"] = 8100520,
        ["Scalp Collector"] = 8100540,
        ["Warrior's Vein"] = 8100500,
        ["Dispersion"] = 8100510,
        ["Crescendo"] = 8100550,
        ["Vicious Cycle"] = 8100560,
        ["Splitter"] = 8100561,
        ["Vital Crash"] = 8100570,
        ["Strafing Run"] = 8100580,
        // Boons
        ["Mist"] = 8200170,
        ["Warm"] = 8200130,
        ["Cool"] = 8200140,
        ["Blessed"] = 8200180,
        ["Possessed"] = 8200190,
        // Hexes
        ["Haunt Hex"] = 8201024,
        ["Scorch Hex"] = 8201020,
        ["Chill Hex"] = 8201021,
        ["Doom Hex"] = 8201022,
        ["Curse Hex"] = 8201023,
        // Daggers
        ["Backstab"] = 8100070,
        ["Opportunist Stab"] = 8100071,
        ["Serpent's Parry"] = 8100261,
        // Pistols
        ["Shatter Bullet"] = 8200603,
        ["Frost Bullet"] = 8200601,
        ["Blood Bullet"] = 8200602,
        // Chakram
        ["Chakram Pierce"] = 8100250,
        ["Chakram Arc"] = 8100252,
        ["Chakram Dance"] = 8100251,
        // Shields
        ["Shield Charge"] = 8100190,
        ["Gong Strike"] = 8100200,
        ["Shield Infusion"] = 8100330,
        // Bow
        ["Evasion Shot"] = 8100100,
        ["Sniper Shot"] = 8100101,
        ["Piercing Shot"] = 8100102,
        // Runes
        ["Dez"] = 8100210,
        ["Shim"] = 8100220,
        ["Egoth"] = 8100230,
        ["Fal"] = 8100240,
        // Mana
        ["Spark"] = 8200040,
        ["Flamethrower"] = 8100090,
        // Innate
        ["Push Kick"] = 8100120,
        ["Throw Lantern"] = 8100010,
        ["Dagger Slash"] = 8100072,
        ["Fire/Reload"] = 8200600,
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

/*
static public Dictionary<Item, Sleepable> SleepablesByItem
{ get; private set; }

SleepablesByItem = new Dictionary<Item, Sleepable>();

Sleepable sleepable = item.GetComponent<Sleepable>();
if (sleepable != null)
SleepablesByItem.Add(item, sleepable);
*/