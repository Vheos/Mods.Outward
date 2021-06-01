using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



namespace ModPack
{
    public class Inns : AMod
    {
        #region const
        private const string INNS_QUEST_FAMILY_NAME = "Inns";
        static private readonly Dictionary<AreaManager.AreaEnum, (string UID, Vector3[] Positions)> STASH_DATA_BY_CITY = new Dictionary<AreaManager.AreaEnum, (string, Vector3[])>
        {
            [AreaManager.AreaEnum.CierzoVillage] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-367.850f, -1488.250f, 596.277f),
                                                                                      new Vector3(-373.539f, -1488.250f, 583.187f) }),
            [AreaManager.AreaEnum.Berg] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-386.620f, -1493.132f, 773.86f),
                                                                             new Vector3(-372.410f, -1493.132f, 773.86f) }),
            [AreaManager.AreaEnum.Monsoon] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-371.628f, -1493.410f, 569.910f) }),
            [AreaManager.AreaEnum.Levant] = ("ZbPXNsPvlUeQVJRks3zBzg", new[] { new Vector3(-369.280f, -1502.535f, 592.850f),
                                                                               new Vector3(-380.530f, -1502.535f, 593.080f) }),
            [AreaManager.AreaEnum.Harmattan] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-178.672f, -1515.915f, 597.934f),
                                                                                  new Vector3(-182.373f, -1515.915f, 606.291f),
                                                                                  new Vector3(-383.484f, -1504.820f, 583.343f),
                                                                                  new Vector3(-392.681f, -1504.820f, 586.551f)}),
            //[AreaManager.AreaEnum.NewSirocco] = ("???", new Vector3[0]),
        };
        #endregion

        // Settings
        static private ModSetting<int> _rentDuration;
        static private ModSetting<bool> _stashes;
        static private ModSetting<bool> _dontRestoreFoodDrinkOnSleep;
        override protected void Initialize()
        {
            _rentDuration = CreateSetting(nameof(_rentDuration), 12, IntRange(1, 168));
            _stashes = CreateSetting(nameof(_stashes), false);
            _dontRestoreFoodDrinkOnSleep = CreateSetting(nameof(_dontRestoreFoodDrinkOnSleep), false);
        }
        override protected void SetFormatting()
        {
            _rentDuration.Format("Inn rent duration");
            _rentDuration.Description = "Pay the rent once, sleep for up to a week (in hours)";
            _stashes.Format("Inn stashes");
            _stashes.Description = "Each inn room will have a player stash, linked with the one in player's house\n" +
                                  "(exceptions: the first rooms in Monsoon's inn and Harmattan's Victorious Light inn)";
            _dontRestoreFoodDrinkOnSleep.Format("Don't restore food/drink when sleeping");
            _dontRestoreFoodDrinkOnSleep.Description = "Sleeping in beds will only stop the depletion of food and drink, not restore them";
        }
        override protected string SectionOverride
        => SECTION_SURVIVAL;
        override protected string Description
        => "• Change rent duration\n" +
           "• Add player stash to each room";
        override public void LoadPreset(Presets.Preset preset)
        {
            switch (preset)
            {
                case Presets.Preset.Vheos_CoopSurvival:
                    ForceApply();
                    _rentDuration.Value = 120;
                    _stashes.Value = true;
                    _dontRestoreFoodDrinkOnSleep.Value = true;
                    break;
            }
        }

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        // Inn Stash
        [HarmonyPatch(typeof(NetworkLevelLoader), "UnPauseGameplay"), HarmonyPostfix]
        static void NetworkLevelLoader_UnPauseGameplay_Post(NetworkLevelLoader __instance, string _identifier)
        {
            #region quit
            if (!_stashes || _identifier != "Loading" || !AreaManager.Instance.CurrentArea.TryAssign(out var currentArea)
            || !STASH_DATA_BY_CITY.ContainsKey((AreaManager.AreaEnum)currentArea.ID))
                return;
            #endregion

            // Cache
            (string UID, Vector3[] Positions) = STASH_DATA_BY_CITY[(AreaManager.AreaEnum)currentArea.ID];
            TreasureChest stash = (TreasureChest)ItemManager.Instance.GetItem(UID);
            stash.GOSetActive(true);

            int counter = 0;
            foreach (var position in Positions)
            {
                // Interactions
                Transform newInteractionHolder = GameObject.Instantiate(stash.InteractionHolder.transform);
                newInteractionHolder.name = $"InnStash{counter} - Interaction";
                newInteractionHolder.ResetLocalTransform();
                newInteractionHolder.position = position;
                InteractionActivator activator = newInteractionHolder.GetFirstComponentsInHierarchy<InteractionActivator>();
                activator.UID += $"_InnStash{counter}";
                InteractionOpenChest openChest = newInteractionHolder.GetFirstComponentsInHierarchy<InteractionOpenChest>();
                openChest.m_container = stash;
                openChest.m_item = stash;
                openChest.StartInit();

                // Highlight
                Transform newHighlightHolder = GameObject.Instantiate(stash.CurrentVisual.ItemHighlightTrans);
                newHighlightHolder.name = $"InnStash{counter} - Highlight";
                newHighlightHolder.ResetLocalTransform();
                newHighlightHolder.BecomeChildOf(newInteractionHolder);
                newHighlightHolder.GetFirstComponentsInHierarchy<InteractionHighlight>().enabled = true;
                counter++;
            }
        }

        [HarmonyPatch(typeof(InteractionOpenChest), "OnActivate"), HarmonyPrefix]
        static bool InteractionOpenChest_OnActivate_Pre(InteractionOpenChest __instance)
        {
            #region quit
            if (!_stashes || !__instance.m_chest.TryAssign(out var chest))
                return true;
            #endregion

            chest.GOSetActive(true);
            return true;
        }

        // Inn rent duration
        [HarmonyPatch(typeof(QuestEventData), "HasExpired"), HarmonyPrefix]
        static bool QuestEventData_HasExpired_Pre(QuestEventData __instance, ref int _gameHourAllowed)
        {
            if (__instance.m_signature.ParentSection.Name == INNS_QUEST_FAMILY_NAME)
                _gameHourAllowed = _rentDuration;
            return true;
        }

        // Don't restore food/drink when sleeping
        [HarmonyPatch(typeof(CharacterResting), "GetFoodRestored"), HarmonyPrefix]
        static bool CharacterResting_GetFoodRestored_Pre(CharacterResting __instance)
        => !_dontRestoreFoodDrinkOnSleep;

        [HarmonyPatch(typeof(CharacterResting), "GetDrinkRestored"), HarmonyPrefix]
        static bool CharacterResting_GetDrinkRestored_Pre(CharacterResting __instance)
        => !_dontRestoreFoodDrinkOnSleep;
    }
}