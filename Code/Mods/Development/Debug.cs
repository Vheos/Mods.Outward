namespace Vheos.Mods.Outward;
using Tools.Extensions.DumpN;

public class Debug : AMod, IDelayedInit, IUpdatable
{
    // Settings
    override protected void Initialize()
    {
    }
    override protected void SetFormatting()
    {
    }
    public void OnUpdate()
    {
        if (KeyCode.LeftAlt.Down())
        {
            if (KeyCode.Keypad0.Pressed())
            {
                foreach (var presetSetup in AISceneManager.Instance.PresetSetups)
                {
                    Log.Debug($"NameContains: {presetSetup.NameContains}");
                    typeof(AIPresetDetection).Dump();
                    foreach (var settings in presetSetup.AIPreset.DetectionSettings)
                        settings.Dump();
                }
            }

            if (KeyCode.Keypad1.Pressed())
            {
                foreach (var preset in Resources.FindObjectsOfTypeAll<AIPreset>())
                {
                    Log.Debug($"{preset.name}");
                    typeof(AIPresetDetection).Dump();
                    foreach (var settings in preset.DetectionSettings)
                        settings.Dump();
                    Log.Debug($"");
                }
            }
        }
    }
    override protected string SectionOverride
    => ModSections.Development;

    // Utility

}
/*
*           static private List<QuestEventFamily> _families;
        static private Dictionary<string, QuestEventSignature> _allQuests;
        static private DictionaryExt<string, QuestEventData> _currentQuests;
        if (KeyCode.LeftAlt.Held())
            if (KeyCode.Alpha1.Pressed())
            {
                Log.Debug($"FAMILIES: {_families.Count}");
                int counter = 0;
                foreach (var family in _families)
                    Log.Debug($"{counter++}\t{family.Name}\t{family.Events.Count}");
            }
            else if (KeyCode.Alpha2.Pressed())
            {
                Log.Debug($"ALL: {_allQuests.Count}");
                foreach (var questSignatureByUID in _allQuests)
                {
                    string UID = questSignatureByUID.Key;
                    QuestEventSignature sig = questSignatureByUID.Value;
                    Log.Debug($"{UID}\t{sig.EventName}");
                }
            }
            else if (KeyCode.Alpha3.Pressed())
            {
                Log.Debug($"CURRENT: {_currentQuests.Count}");
                for (int i = 0; i < _currentQuests.Count; i++)
                {
                    string UID = _currentQuests.Keys[i];
                    QuestEventData data = _currentQuests.Values[i];
                    Log.Debug($"{UID}\t{data.m_signature.EventName}\t{data.m_activationTime}");
                }
            }
            else if (KeyCode.Alpha4.Pressed())
            {
                Log.Debug($"INNS: {_innRentQuestFamily.Events.Count}");
                foreach (var questEvent in _innRentQuestFamily.Events)
                    Log.Debug($"{questEvent.EventUID}\t{questEvent.EventName}");
            }
*/

/*
// Find any working lantern slot
GameObject lanternBagVisuals = GetVisuals(5300000);
GameObject lanternSlot = lanternBagVisuals.FindChild("LanternSlotAnchor");

// Cache the visuals of your bag (so the game reuses them instead of reloading)
GameObject primitiveSatchelVisuals = GetVisuals(5300120);
GameObject.DontDestroyOnLoad(primitiveSatchelVisuals);

// Create new lantern slot and attach it to Primitive Satchel
GameObject newLanternSlot = GameObject.Instantiate(lanternSlot);
newLanternSlot.transform.parent = primitiveSatchelVisuals.transform;

    static private GameObject GetVisuals(int itemID)
{
string visualsPath = ResourcesPrefabManager.ITEM_PREFABS[itemID.ToString()].m_visualPrefabPath;
return ResourcesPrefabManager.Instance.m_itemVisualsBundle.LoadAsset<GameObject>(visualsPath + ".prefab");
}
*/

/*
*         GameObject _cachedLanternSlot;
    bool _isPrefabManagerInitialized;
    public void OnUpdate()
    {
        if (!_isPrefabManagerInitialized)
        {
            _isPrefabManagerInitialized = true;
            string adventurerBagPath = ResourcesPrefabManager.ITEM_PREFABS["5300000"].m_visualPrefabPath;
            GameObject adventurerBagVisuals = ResourcesPrefabManager.Instance.m_itemVisualsBundle.LoadAsset<GameObject>(adventurerBagPath + ".prefab");
            _cachedLanternSlot = GameObject.Instantiate(adventurerBagVisuals.FindChild("LanternSlotAnchor"));
            // now you have your own copy of a working lantern slot, you can copy it whenever you want to

            GameObject.Instantiate(_cachedLanternSlot, YOUR_BACKPACK_VISUALS.transform)
        }
    }
*/

/*
*                 foreach (var itemByID in Prefabs.ItemsByID)
                if (itemByID.Value.TryAs(out RecipeItem recipeItem)
                && recipeItem.Recipe.TryNonNull(out Recipe recipe))
                    Log.Debug($"{recipeItem.Name.TrimEnd('\r', '\n')}\t{recipe.Name}\t{recipe.RecipeID}");
*/

/*
*            if (KeyCode.LeftAlt.Held() && KeyCode.Keypad0.Pressed())
        {
            string[] blacklist =
            {
               "m_localizedDescription", "Description",
            };

            foreach (var itemByID in Prefabs.ItemsByID)
                if (itemByID.Value.TryAs(out Equipment equipment))
                    Log.Debug(itemByID.Key);

            Log.Debug($"~~~~");

            typeof(Equipment).Dump(blacklist, Data.Names, Members.FieldsAndProperties);
            typeof(Equipment).Dump(blacklist, Data.Types, Members.FieldsAndProperties);
            foreach (var itemByID in Prefabs.ItemsByID)
                if (itemByID.Value.TryAs(out Equipment equipment))
                    equipment.Dump(typeof(Equipment), blacklist, Data.Values, Members.FieldsAndProperties);

            Log.Debug($"~~~~");

            typeof(EquipmentStats).Dump(blacklist, Data.Names, Members.FieldsAndProperties);
            typeof(EquipmentStats).Dump(blacklist, Data.Types, Members.FieldsAndProperties);
            foreach (var itemByID in Prefabs.ItemsByID)
                if (itemByID.Value.TryAs(out Equipment equipment))
                    if (equipment.Stats != null)
                        equipment.Stats.Dump(typeof(EquipmentStats), blacklist, Data.Values, Members.FieldsAndProperties);
                    else
                        Log.Debug($"null");
        } 
*/

/*
*                     foreach(var chakramSkillID in new[] { 8100250, 8100251, 8100252 })
                    if(Prefabs.ItemsByID[chakramSkillID.ToString()].TryAs(out AttackSkill chakramSkill))
                    {
                        chakramSkill.RequiredOffHandTypes.Clear();//Add(Weapon.WeaponType.Shield);
                        chakramSkill.Cooldown = 1;
                    }    
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
                    Log.Debug($"{tree.Name}\t{basic}\t{advanced}");
                }
*/

/*
* foreach (var gatherable in GameObject.FindObjectsOfType<Gatherable>())
            {
                Log.Debug($"{gatherable.DisplayName}");
                // Execute
                foreach (var dropable in gatherable.m_drops)
                {
                    Log.Debug($"\t{dropable.name}");
                    foreach (var dropTable in dropable.m_mainDropTables)
                    {
                        SimpleRandomChance dropAmount = dropTable.m_dropAmount;
                        Log.Debug($"\t\t{dropTable.ItemGenatorName}\t" +
                            $"MaxRoll: {dropTable.m_maxDiceValue}\t" +
                            $"Regen: {dropAmount.ChanceRegenQty}");

                        foreach (var itemDropChance in dropTable.m_itemDrops)
                        {
                            Log.Debug($"\t\t\t{itemDropChance.DroppedItem.DisplayName}\t" +
                                $"OnRolls: {itemDropChance.MinDiceRollValue}-{itemDropChance.MaxDiceRollValue}\t" +
                                $"Qty: {itemDropChance.MinDropCount}-{itemDropChance.MaxDropCount}\t" +
                                $"Regen: {itemDropChance.ChanceRegenDelay}");
                        }
                    }
                }
                Log.Debug($"\n");
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
    [HarmonyPatch(typeof(LocalCharacterControl), nameof(LocalCharacterControl.RetrieveComponents)), HarmonyPostfix]
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