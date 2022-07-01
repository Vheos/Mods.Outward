namespace Vheos.Mods.Outward;

public class Quickslots : AMod
{
    #region Settings
    private static ModSetting<bool> _weaponTypeBoundQuickslots;
    private static ModSetting<bool> _replaceQuickslotsOnEquip;
    private static ModSetting<bool> _assignByUsingEmptyQuickslot;
    private static ModSetting<bool> _extraGamepadQuickslots;
    protected override void Initialize()
    {
        _weaponTypeBoundQuickslots = CreateSetting(nameof(_weaponTypeBoundQuickslots), false);
        _replaceQuickslotsOnEquip = CreateSetting(nameof(_replaceQuickslotsOnEquip), false);
        _assignByUsingEmptyQuickslot = CreateSetting(nameof(_assignByUsingEmptyQuickslot), false);
        _extraGamepadQuickslots = CreateSetting(nameof(_extraGamepadQuickslots), false);

        foreach (var group in SKILL_CONTEXT_GROUPS)
            foreach (var skillByType in group.Value)
                _skillContextsByID.Add(skillByType.Value, group.Key);
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _weaponTypeBoundQuickslots.Value = true;
                _replaceQuickslotsOnEquip.Value = true;
                _assignByUsingEmptyQuickslot.Value = false;
                _extraGamepadQuickslots.Value = true;
                break;
        }
    }
    #endregion

    #region Formatting
    protected override string SectionOverride
        => ModSections.Combat;
    protected override string Description
        => "• Change skills when changing weapon" +
        "\n• Switch between 2 weapons with 1 quickslot" +
        "\n• Enable 16 gamepad quickslots";
    protected override void SetFormatting()
    {
        _weaponTypeBoundQuickslots.Format("Weapon-bound skills");
        _weaponTypeBoundQuickslots.Description =
            "Makes your weapon-specific skills change whenever you change your weapon" +
            "\nLet's assume you have the \"Dagger Slash\" skill assigned to a quickslot " +
            "and you have a dagger equipped in your offhand. If you switch your dagger to:" +
            "\n• a pistol, this quickslot will become \"Fire/Reload\"" +
            "\n• a lantern, this quickslot will become a \"Throw Lantern\"" +
            "\nAnd when you switch back to a dagger, this quickslot will become \"Dagger Slash\" again" +
            "\n\nWorks for all weapon types - 1-handed, 2-handed and offhand";
        _replaceQuickslotsOnEquip.Format("2 weapons, 1 quickslot");
        _replaceQuickslotsOnEquip.Description =
            "Allows you to switch between 2 weapons using 1 quickslot" +
            "\n\nHow to set it up:" +
            "\nAssign weapon A to a quickslot, then equip weapon B manually" +
            "\nNow whenver you switch to A, your quickslot will become B - and vice versa";
        _assignByUsingEmptyQuickslot.Format("Assign by using empty quickslot");
        _assignByUsingEmptyQuickslot.Description =
            "Allows you to assign your current weapon to an empty quickslot when you activate it" +
            "\nIf your mainhand is already quickslotted, your offhand will be assigned" +
            "\nIf both your mainhand and offhand are quickslotted, nothing will happen";
        _extraGamepadQuickslots.Format("16 gamepad quickslots");
        _extraGamepadQuickslots.Description =
            "Allows you to combine the LT/RT with d-pad buttons (in addition to face buttons) for 8 extra quickslots" +
            "\n(requires default d-pad keybinds and game restart)";
    }
    #endregion

    #region Utility
    private static Dictionary<int, SkillContext> _skillContextsByID = new();
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
    => character.Inventory.SkillKnowledge.GetLearnedItems().FirstOrDefault(skill => skill.SharesPrefabWith(id));
    private static void TryOverrideVanillaQuickslotInput(ref bool input, int playerID)
    {
        if (!_extraGamepadQuickslots)
            return;

        input &= !ControlsInput.QuickSlotToggle1(playerID) && !ControlsInput.QuickSlotToggle2(playerID);
    }
    private static void TryHandleCustomQuickslotInput(Character character)
    {
        if (!_extraGamepadQuickslots)
            return;

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
            quickslots[i].name = (i + 1).ToString();
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

    #region Hooks
    [HarmonyPrefix, HarmonyPatch(typeof(Item), nameof(Item.PerformEquip))]
    private static void Item_PerformEquip_Pre2(Item __instance, EquipmentSlot _slot)
    {
        Character character = _slot.Character;
        if (!_weaponTypeBoundQuickslots || !character.IsPlayer())
            return;

        Weapon.WeaponType previousType = GetExtendedWeaponType(_slot.EquippedItem);
        Weapon.WeaponType currentType = GetExtendedWeaponType(__instance);
        if (currentType == previousType)
            return;

        foreach (var quickslot in character.QuickSlotMngr.m_quickSlots)
            if (quickslot.ActiveItem.TryAs(out Skill quickslotSkill)
            && _skillContextsByID.TryGetValue(quickslotSkill.ItemID, out var context)
            && SKILL_CONTEXT_GROUPS.TryGetValue(context, out var contextSkillGroup)
            && contextSkillGroup.TryGetValue(currentType, out var newContextSkillID)
            && GetLearnedSkillByID(character, newContextSkillID).TryNonNull(out var newContextSkill))
                quickslot.SetQuickSlot(newContextSkill, true);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Item), nameof(Item.PerformEquip))]
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

    [HarmonyPostfix, HarmonyPatch(typeof(QuickSlot), nameof(QuickSlot.Activate))]
    private static void QuickSlot_Activate_Post(QuickSlot __instance)
    {
        if (!_assignByUsingEmptyQuickslot || __instance.ActiveItem != null)
            return;

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
    [HarmonyPostfix, HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.Sheathe))]
    private static void ControlsInput_Sheathe_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

    [HarmonyPostfix, HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.ToggleMap))]
    private static void ControlsInput_ToggleMap_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

    [HarmonyPostfix, HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.ToggleLights))]
    private static void ControlsInput_ToggleLights_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

    [HarmonyPostfix, HarmonyPatch(typeof(ControlsInput), nameof(ControlsInput.HandleBackpack))]
    private static void ControlsInput_HandleBackpack_Post(ref bool __result, ref int _playerID)
        => TryOverrideVanillaQuickslotInput(ref __result, _playerID);

    [HarmonyPostfix, HarmonyPatch(typeof(LocalCharacterControl), nameof(LocalCharacterControl.UpdateQuickSlots))]
    private static void LocalCharacterControl_UpdateQuickSlots_Pre(ref Character ___m_character)
        => TryHandleCustomQuickslotInput(___m_character);

    [HarmonyPostfix, HarmonyPatch(typeof(SplitScreenManager), nameof(SplitScreenManager.Awake))]
    private static void SplitScreenManager_Awake_Post(SplitScreenManager __instance)
    {
        if (!_extraGamepadQuickslots)
            return;

        GameObject.DontDestroyOnLoad(__instance.m_charUIPrefab);
        SetupQuickslotPanels(__instance.m_charUIPrefab);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CharacterQuickSlotManager), nameof(CharacterQuickSlotManager.Awake))]
    private static void CharacterQuickSlotManager_Awake_Pre(CharacterQuickSlotManager __instance)
    {
        if (!_extraGamepadQuickslots)
            return;

        SetupQuickslots(__instance.transform.Find("QuickSlots"));
    }
    #endregion
}
