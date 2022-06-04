namespace Vheos.Mods.Outward;

public class Quickslots : AMod
{
    #region const
    private static readonly Dictionary<SkillContext, Dictionary<Weapon.WeaponType, int>> SKILL_CONTEXT_GROUPS = new()
    {
        [SkillContext.Innate] = new Dictionary<Weapon.WeaponType, int>
        {
            [Weapon.WeaponType.Dagger_OH] = "Dagger Slash".ToSkillID(),
            [Weapon.WeaponType.Pistol_OH] = "Fire/Reload".ToSkillID(),
            [(Weapon.WeaponType)WeaponTypeExtended.Light] = "Throw Lantern".ToSkillID(),
        },

        [SkillContext.BasicA] = new Dictionary<Weapon.WeaponType, int>
        {
            [Weapon.WeaponType.Bow] = "Evasion Shot".ToSkillID(),
            [Weapon.WeaponType.Dagger_OH] = "Backstab".ToSkillID(),
            [Weapon.WeaponType.Pistol_OH] = "Shatter Bullet".ToSkillID(),
            [Weapon.WeaponType.Chakram_OH] = "Chakram Pierce".ToSkillID(),
            [Weapon.WeaponType.Shield] = "Shield Charge".ToSkillID(),
            [(Weapon.WeaponType)WeaponTypeExtended.Light] = "Flamethrower".ToSkillID(),
        },
        [SkillContext.BasicB] = new Dictionary<Weapon.WeaponType, int>
        {
            [Weapon.WeaponType.Bow] = "Sniper Shot".ToSkillID(),
            [Weapon.WeaponType.Dagger_OH] = "Opportunist Stab".ToSkillID(),
            [Weapon.WeaponType.Pistol_OH] = "Frost Bullet".ToSkillID(),
            [Weapon.WeaponType.Chakram_OH] = "Chakram Arc".ToSkillID(),
            [Weapon.WeaponType.Shield] = "Gong Strike".ToSkillID(),
        },
        [SkillContext.Advanced] = new Dictionary<Weapon.WeaponType, int>
        {
            [Weapon.WeaponType.Bow] = "Piercing Shot".ToSkillID(),
            [Weapon.WeaponType.Dagger_OH] = "Serpent's Parry".ToSkillID(),
            [Weapon.WeaponType.Pistol_OH] = "Blood Bullet".ToSkillID(),
            [Weapon.WeaponType.Chakram_OH] = "Chakram Dance".ToSkillID(),
            [Weapon.WeaponType.Shield] = "Shield Infusion".ToSkillID(),
        },
        [SkillContext.Weapon] = new Dictionary<Weapon.WeaponType, int>
        {
            [Weapon.WeaponType.Sword_1H] = "Puncture".ToSkillID(),
            [Weapon.WeaponType.Sword_2H] = "Pommel Counter".ToSkillID(),
            [Weapon.WeaponType.Axe_1H] = "Talus Cleaver".ToSkillID(),
            [Weapon.WeaponType.Axe_2H] = "Execution".ToSkillID(),
            [Weapon.WeaponType.Mace_1H] = "Mace Infusion".ToSkillID(),
            [Weapon.WeaponType.Mace_2H] = "Juggernaut".ToSkillID(),
            [Weapon.WeaponType.Spear_2H] = "Simeon's Gambit".ToSkillID(),
            [Weapon.WeaponType.Halberd_2H] = "Moon Swipe".ToSkillID(),
            [Weapon.WeaponType.FistW_2H] = "Prismatic Flurry".ToSkillID(),
        },
        [SkillContext.WeaponMaster] = new Dictionary<Weapon.WeaponType, int>
        {
            [Weapon.WeaponType.Sword_1H] = "The Technique".ToSkillID(),
            [Weapon.WeaponType.Sword_2H] = "Moment of Truth".ToSkillID(),
            [Weapon.WeaponType.Axe_1H] = "Scalp Collector".ToSkillID(),
            [Weapon.WeaponType.Axe_2H] = "Warrior's Vein".ToSkillID(),
            [Weapon.WeaponType.Mace_1H] = "Dispersion".ToSkillID(),
            [Weapon.WeaponType.Mace_2H] = "Crescendo".ToSkillID(),
            [Weapon.WeaponType.Spear_2H] = "Vicious Cycle".ToSkillID(),
            [Weapon.WeaponType.Halberd_2H] = "Splitter".ToSkillID(),
            [Weapon.WeaponType.FistW_2H] = "Vital Crash".ToSkillID(),
            [Weapon.WeaponType.Bow] = "Strafing Run".ToSkillID(),
        },
    };
    #endregion
    #region enum
    private enum WeaponTypeExtended
    {
        Empty = -1,
        Light = -2,
    }
    private enum SkillContext
    {
        Innate = 1,
        BasicA = 2,
        BasicB = 3,
        Advanced = 4,
        Weapon = 5,
        WeaponMaster = 6,
    }
    #endregion

    // Setting
    private static ModSetting<bool> _contextualSkillQuickslots;
    private static ModSetting<bool> _replaceQuickslotsOnEquip;
    private static ModSetting<bool> _assingByUsingFreeQuickslot;
    private static ModSetting<bool> _extraGamepadQuickslots;
    protected override void Initialize()
    {
        _contextualSkillQuickslots = CreateSetting(nameof(_contextualSkillQuickslots), false);
        _replaceQuickslotsOnEquip = CreateSetting(nameof(_replaceQuickslotsOnEquip), false);
        _assingByUsingFreeQuickslot = CreateSetting(nameof(_assingByUsingFreeQuickslot), false);
        _extraGamepadQuickslots = CreateSetting(nameof(_extraGamepadQuickslots), false);

        _skillContextsByID = new Dictionary<int, SkillContext>();
        foreach (var group in SKILL_CONTEXT_GROUPS)
            foreach (var skillByType in group.Value)
                _skillContextsByID.Add(skillByType.Value, group.Key);
    }
    protected override void SetFormatting()
    {
        _contextualSkillQuickslots.Format("Contextual skills");
        _contextualSkillQuickslots.Description = "When switching a weapon, skills that required the previous weapons type will be replaced by skills that require the new one." +
                                                 "This allows for using the same set of quickslots for various skills, depending on the weapon you're currently using.";
        _replaceQuickslotsOnEquip.Format("Replace quickslots on equip");
        _replaceQuickslotsOnEquip.Description = "When switching a weapon, if it's assigned to any quickslot, it will be replaced by the new weapon." +
                                                "This allows for toggling between 2 weapons with just one quickslot.";
        _assingByUsingFreeQuickslot.Format("Assign by using free quickslot");
        _assingByUsingFreeQuickslot.Description = "When you use a free (empty) quickslot, your mainhand or offhand weapon will get assigned to it " +
                                                  "(if it's not assigned to any quickslot yet)";
        _extraGamepadQuickslots.Format("16 gamepad quickslots");
        _extraGamepadQuickslots.Description = "Allows you to use the d-pad with LT/RT for 8 extra quickslots\n" +
                                              "(requires default d-pad keybinds AND game restart)";
    }
    protected override string Description
    => "• Contextual skills\n" +
       "• Toggle between 2 weapons with 1 quickslot\n" +
       "• 16 gamepad quickslots";
    protected override string SectionOverride
    => ModSections.Combat;
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _contextualSkillQuickslots.Value = true;
                _replaceQuickslotsOnEquip.Value = true;
                _assingByUsingFreeQuickslot.Value = false;
                _extraGamepadQuickslots.Value = true;
                break;
        }
    }

    // Utility
    private static Dictionary<int, SkillContext> _skillContextsByID;
    private static Weapon.WeaponType GetExtendedWeaponType(Item item)
    {
        if (item != null)
            if (item.TryAs(out Weapon weapon))
                return weapon.Type;
            else if (item.LitStatus != Item.Lit.Unlightable)
                return (Weapon.WeaponType)WeaponTypeExtended.Light;
        return (Weapon.WeaponType)WeaponTypeExtended.Empty;
    }
    private static bool HasItemAssignedToAnyQuickslot(Character character, Item item)
    {
        foreach (var quickslot in character.QuickSlotMngr.m_quickSlots)
            if (quickslot.ActiveItem == item)
                return true;
        return false;
    }
    private static Item GetLearnedSkillByID(Character character, int id)
    => character.Inventory.SkillKnowledge.GetLearnedItems().FirstOrDefault(skill => skill.ItemID == id);
    private static void TryOverrideVanillaQuickslotInput(ref bool input, int playerID)
    {
        #region quit
        if (!_extraGamepadQuickslots)
            return;
        #endregion

        input &= !ControlsInput.QuickSlotToggle1(playerID) && !ControlsInput.QuickSlotToggle2(playerID);
    }
    private static void TryHandleCustomQuickslotInput(Character character)
    {
        #region quit
        if (!_extraGamepadQuickslots)
            return;
        #endregion

        if (character == null || character.QuickSlotMngr == null || character.CharacterUI.IsMenuFocused)
            return;

        int playerID = character.OwnerPlayerSys.PlayerID;
        if (!ControlsInput.QuickSlotToggle1(playerID) && !ControlsInput.QuickSlotToggle2(playerID))
            return;

        int quickslotID = -1;
        if (GameInput.Pressed(playerID, ControlsInput.GameplayActions.Sheathe))
            quickslotID = 8;
        else if (GameInput.Pressed(playerID, ControlsInput.MenuActions.ToggleMapMenu))
            quickslotID = 9;
        else if (GameInput.Pressed(playerID, ControlsInput.GameplayActions.ToggleLights))
            quickslotID = 10;
        else if (GameInput.Pressed(playerID, ControlsInput.GameplayActions.HandleBag))
            quickslotID = 11;

        if (quickslotID < 0)
            return;

        if (ControlsInput.QuickSlotToggle1(playerID))
            quickslotID += 4;

        character.QuickSlotMngr.QuickSlotInput(quickslotID);
    }
    private static void SetupQuickslots(Transform quickslotsHolder)
    {
        Transform quickslotTemplate = quickslotsHolder.Find("1");
        for (int i = quickslotsHolder.childCount; i < 16; i++)
            GameObject.Instantiate(quickslotTemplate, quickslotsHolder);

        QuickSlot[] quickslots = quickslotsHolder.GetComponentsInChildren<QuickSlot>();
        for (int i = 0; i < quickslots.Length; i++)
        {
            quickslots[i].GOSetName((i + 1).ToString());
            quickslots[i].ItemQuickSlot = false;
        }
    }
    private static void SetupQuickslotPanels(CharacterUI ui)
    {
        // Cache
        Transform menuPanelsHolder = GetMenuPanelsHolder(ui);
        Transform gamePanelsHolder = GetGamePanelsHolder(ui);
        Component[] menuSlotsLT = menuPanelsHolder.Find("LT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
        Component[] menuSlotsRT = menuPanelsHolder.Find("RT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
        Component[] gameSlotsLT = gamePanelsHolder.Find("LT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
        Component[] gameSlotsRT = gamePanelsHolder.Find("RT/QuickSlots").GetComponentsInChildren<EditorQuickSlotDisplayPlacer>();
        // Copy game 
        for (int i = 0; i < menuSlotsLT.Length; i++)
            menuSlotsLT[i].transform.localPosition = gameSlotsLT[i].transform.localPosition;
        for (int i = 0; i < menuSlotsRT.Length; i++)
            menuSlotsRT[i].transform.localPosition = gameSlotsRT[i].transform.localPosition;

        gamePanelsHolder.Find("imgLT").localPosition = new Vector3(-195f, +170f);
        gamePanelsHolder.Find("imgRT").localPosition = new Vector3(-155f, +170f);

        menuPanelsHolder.Find("LT").localPosition = new Vector3(-90f, +50f);
        menuPanelsHolder.Find("RT").localPosition = new Vector3(+340f, -100f);
        menuPanelsHolder.Find("LT/imgLT").localPosition = new Vector3(-125f, 125f);
        menuPanelsHolder.Find("RT/imgRT").localPosition = new Vector3(-125f, 125f);
        menuPanelsHolder.Find("LeftDecoration").gameObject.SetActive(false);
        menuPanelsHolder.Find("RightDecoration").gameObject.SetActive(false);

        DuplicateQuickslotsInPanel(gamePanelsHolder.Find("LT"), +8, new Vector3(-250f, 0f));
        DuplicateQuickslotsInPanel(gamePanelsHolder.Find("RT"), +8, new Vector3(-250f, 0f));
        DuplicateQuickslotsInPanel(menuPanelsHolder.Find("LT"), +8, new Vector3(-250f, 0f));
        DuplicateQuickslotsInPanel(menuPanelsHolder.Find("RT"), +8, new Vector3(-250f, 0f));
    }
    private static void DuplicateQuickslotsInPanel(Transform panelHolder, int idOffset, Vector3 posOffset)
    {
        Transform quickslotsHolder = panelHolder.Find("QuickSlots");
        foreach (var editorPlacer in quickslotsHolder.GetComponentsInChildren<EditorQuickSlotDisplayPlacer>())
        {
            // Instantiate
            editorPlacer.IsTemplate = true;
            Transform newSlot = GameObject.Instantiate(editorPlacer.transform);
            editorPlacer.IsTemplate = false;
            // Setup
            newSlot.SetParent(quickslotsHolder);
            newSlot.localPosition = editorPlacer.transform.localPosition + posOffset;
            EditorQuickSlotDisplayPlacer newEditorPlacer = newSlot.GetComponent<EditorQuickSlotDisplayPlacer>();
            newEditorPlacer.RefSlotID += idOffset;
            newEditorPlacer.IsTemplate = false;
        }
    }
    // Find
    private static Transform GetGamePanelsHolder(CharacterUI ui)
    => ui.transform.Find("Canvas/GameplayPanels/HUD/QuickSlot/Controller/LT-RT");
    private static Transform GetMenuPanelsHolder(CharacterUI ui)
    => ui.transform.Find("Canvas/GameplayPanels/Menus/CharacterMenus/MainPanel/Content/MiddlePanel/QuickSlotPanel/PanelSwitcher/Controller/LT-RT");

    // Hooks
    [HarmonyPatch(typeof(Item), nameof(Item.PerformEquip)), HarmonyPrefix]
    private static void Item_PerformEquip_Pre2(Item __instance, EquipmentSlot _slot)
    {
        Character character = _slot.Character;
        if (!_contextualSkillQuickslots || !character.IsPlayer())
            return;

        Weapon.WeaponType previousType = GetExtendedWeaponType(_slot.EquippedItem);
        Weapon.WeaponType currentType = GetExtendedWeaponType(__instance);
        if (currentType == previousType)
            return;

        foreach (var quickslot in character.QuickSlotMngr.m_quickSlots)
            if (quickslot.ActiveItem.TryAs(out Skill quickslotSkill)
            && _skillContextsByID.TryGet(quickslotSkill.ItemID, out var context)
            && SKILL_CONTEXT_GROUPS.TryGet(context, out var contextSkillGroup)
            && contextSkillGroup.TryGet(currentType, out var newContextSkillID)
            && GetLearnedSkillByID(character, newContextSkillID).TryNonNull(out var newContextSkill))
                quickslot.SetQuickSlot(newContextSkill, true);
    }

    [HarmonyPatch(typeof(Item), nameof(Item.PerformEquip)), HarmonyPrefix]
    private static void Item_PerformEquip_Pre(Item __instance, EquipmentSlot _slot)
    {
        Character character = _slot.Character;
        if (!_replaceQuickslotsOnEquip || !character.IsPlayer())
            return;

        // If equipping a 2h weapon, also check offhand
        Item previousItem = _slot.EquippedItem;
        if (__instance.TryAs(out Weapon weapon) && weapon.TwoHanded)
        {
            EquipmentSlot[] slots = character.Inventory.Equipment.EquipmentSlots;
            if (!slots[(int)EquipmentSlot.EquipmentSlotIDs.RightHand].EquippedItem.TryNonNull(out var rightHandWeapon)
            || HasItemAssignedToAnyQuickslot(character, rightHandWeapon))
                previousItem = slots[(int)EquipmentSlot.EquipmentSlotIDs.LeftHand].EquippedItem;
        }

        if (previousItem == null || HasItemAssignedToAnyQuickslot(character, previousItem))
            return;

        foreach (var quickslot in _slot.Character.QuickSlotMngr.m_quickSlots)
            if (quickslot.ActiveItem == __instance)
            {
                quickslot.SetQuickSlot(previousItem, true);
                break;
            }
    }

    [HarmonyPatch(typeof(QuickSlot), nameof(QuickSlot.Activate)), HarmonyPostfix]
    private static void QuickSlot_Activate_Post(QuickSlot __instance)
    {
        #region quit
        if (!_assingByUsingFreeQuickslot || __instance.ActiveItem != null)
            return;
        #endregion

        Character character = __instance.OwnerCharacter;
        EquipmentSlot[] slots = character.Inventory.Equipment.EquipmentSlots;
        foreach (var slotID in new[] { EquipmentSlot.EquipmentSlotIDs.RightHand, EquipmentSlot.EquipmentSlotIDs.LeftHand })
            if (slots[(int)slotID].EquippedItem.TryNonNull(out var item) && !HasItemAssignedToAnyQuickslot(character, item))
            {
                __instance.SetQuickSlot(item);
                break;
            }
    }

    // 16 controller quickslots
    [HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.Sheathe)), HarmonyPostfix]
    private static void ControlsInput_Sheathe_Post(ref bool __result, ref int _playerID)
    => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

    [HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.ToggleMap)), HarmonyPostfix]
    private static void ControlsInput_ToggleMap_Post(ref bool __result, ref int _playerID)
    => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

    [HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.ToggleLights)), HarmonyPostfix]
    private static void ControlsInput_ToggleLights_Post(ref bool __result, ref int _playerID)
    => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

    [HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.HandleBackpack)), HarmonyPostfix]
    private static void ControlsInput_HandleBackpack_Post(ref bool __result, ref int _playerID)
    => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

    [HarmonyPatch(typeof(LocalCharacterControl), nameof(LocalCharacterControl.UpdateQuickSlots)), HarmonyPostfix]
    private static void LocalCharacterControl_UpdateQuickSlots_Pre(ref Character ___m_character)
    => TryHandleCustomQuickslotInput(___m_character);

    [HarmonyPatch(typeof(SplitScreenManager), nameof(SplitScreenManager.Awake)), HarmonyPostfix]
    private static void SplitScreenManager_Awake_Post(SplitScreenManager __instance)
    {
        #region quit
        if (!_extraGamepadQuickslots)
            return;
        #endregion

        CharacterUI charUIPrefab = __instance.m_charUIPrefab;
        GameObject.DontDestroyOnLoad(charUIPrefab);
        SetupQuickslotPanels(charUIPrefab);
    }

    [HarmonyPatch(typeof(CharacterQuickSlotManager), nameof(CharacterQuickSlotManager.Awake)), HarmonyPrefix]
    private static void CharacterQuickSlotManager_Awake_Pre(CharacterQuickSlotManager __instance)
    {
        #region quit
        if (!_extraGamepadQuickslots)
            return;
        #endregion

        SetupQuickslots(__instance.transform.Find("QuickSlots"));
    }
}
