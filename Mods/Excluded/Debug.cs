using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;



namespace ModPack
{
    public class Debug : AMod, IUpdatable, IExcludeFromBuild
    {
        // Setting
        override protected void Initialize()
        {
            CollapseUnityExplorerMenus();
        }
        override protected void SetFormatting()
        {
        }
        public void OnUpdate()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Keypad0))
            {
                foreach (var ingestibleByName in Prefabs.IngestiblesByGOName)
                {
                    Tools.Log($"{ingestibleByName.Key}\t{ingestibleByName.Value.DisplayName}");
                }
                /*
                typeof(Sleepable).Dump(null, Data.Names);
                typeof(Sleepable).Dump(null, Data.Types);
                foreach (var sleepableByName in Prefabs.SleepablesByItem)
                    sleepableByName.Value.Dump(typeof(Sleepable));
                */

                /*
                string[] mismatched =
                {
                    "Torcrab Egg",
                    "Boreo Blubber",
                    "Pungent Paste",
                    "Gaberry Jam",
                    "Crawlberry Jam",
                    "Golden Jam",
                    "Raw Torcrab Meat",
                    "Miner’s Omelet",
                    "Turmmip Potage",
                    "Meat Stew",
                    "Marshmelon Jelly",
                    "Blood Mushroom",
                    "Food Waste",
                    "Warm Boozu’s Milk",
                };
                
                foreach (var itemName in mismatched)
                    Tools.Log($"{itemName}\t{Prefabs.IngestiblesByName[itemName].ActivateEffectAnimType}");
                */

                /*
                Tools.Log($"StatusEffect\tStatusData\tEffectSignature\tEffects\tCount\tEffectsData\tCount\tData\tCount");
                foreach (var statusEffectByName in Prefabs.StatusEffectsByName)
                {
                    StatusEffect statusEffect = statusEffectByName.Value;

                    StatusData statusData = null;
                    if (statusEffect != null)
                        statusData = statusEffect.StatusData;

                    EffectSignature effectSignature = null;
                    if (statusData != null)
                        effectSignature = statusData.EffectSignature;

                    List<Effect> effects = null;
                    if (effectSignature != null)
                        effects = effectSignature.Effects;

                    int? effectsCount = null;
                    if (effects != null)
                        effectsCount = effects.Count;

                    StatusData.EffectData[] effectData = null;
                    if (statusData != null)
                        effectData = statusData.EffectsData;

                    int? effectDataCount = null;
                    if (effectData != null)
                        effectDataCount = effectData.Length;

                    Tools.Log($"{statusEffectByName.Key}\t{statusEffect != null}\t{statusData != null}\t{effectSignature != null}\t{effects != null}\t{effectsCount}\t{effectData != null}\t{effectDataCount}");
                }
                */

                /*
                Dictionary<Effect, string[]> valuesByEffect = statusEffectByName.Value.GetValuesByEffect();
                string text = "";
                foreach (var valueByEffect in valuesByEffect)
                {
                   Effect effect = valueByEffect.Key;
                   string effectText = effect.GetType().Name;
                   if (effect is AffectStat affectStat)
                       effectText = affectStat.AffectedStat.Tag.TagName;

                   string valueText = "";
                   foreach (var value in valueByEffect.Value)
                       valueText += value + ", ";
                }
                Tools.Log($"{statusEffectByName.Key}\t{text}");
                */


                /*
                foreach (var ingestibleByName in Prefabs.IngestiblesByName)
                {
                    Tools.Log($"Spawning {ingestibleByName.Key}...");
                    Character character = Global.Lobby.GetLocalPlayer(0).ControlledCharacter;
                    Item item = ItemManager.Instance.GenerateItemNetwork(ingestibleByName.Value.ItemID);
                    item.transform.position = character.CenterPosition + character.transform.forward * 1.5f;
                    item.gameObject.AddComponent<SafeFalling>();
                }
                */
            }
        }

        // Utility
        private void CollapseUnityExplorerMenus()
        {
            Canvas unityExplorerCanvas = null;
            foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
                if (canvas.GONameIs("ExplorerCanvas"))
                {
                    unityExplorerCanvas = canvas;
                    break;
                }

            if (unityExplorerCanvas == null)
                return;

            GameObject hideSceneExplorer = unityExplorerCanvas.FindChild("Panel_MainMenu/Content/HorizontalLayout/HorizontalLayout/VerticalLayout/Button");
            hideSceneExplorer.GetComponent<Button>().onClick.Invoke();

            GameObject debugConsoleButtons = unityExplorerCanvas.FindChild("Panel_MainMenu/Content/VerticalLayout/HorizontalLayout/Button");
            debugConsoleButtons.GetComponent<Button>().onClick.Invoke();

            #region recursive method
            /*
            string[] buttonTexts = new[]
            {
                "Hide Scene Explorer",
                "Hide",
                "Search",
            };

            List<Button> buttons = new List<Button>();
            buttons.AddComponentsFromHierarchy(unityExplorerCanvas.transform);
            foreach (var button in buttons)
            {
                Text text = button.GetComponentInChildren<Text>();
                if (text != null && text.text.IsContainedIn(buttonTexts))
                    button.onClick.Invoke();
            }
            */
            #endregion
        }
    }
}