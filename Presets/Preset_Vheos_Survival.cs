using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;



namespace ModPack
{
    public class Preset_Vheos_Survival : APreset
    {
        override public string Name
        => "Vheos's Survival";
        override public int Ordering
        => 2;
        override public Type[] RequiredMods => new Type[]
        {
            typeof(Various),
        };
        override public void OverrideSettings()
        {
            
            foreach (var settingByNeed in Needs._settingsByNeed)
            {

                Needs.NeedSettings settings = settingByNeed.Value;
                settings._toggle.Value = true;
                settings._thresholds.Value = new Vector2(20, 60);
                settings._depletionRate.Value = new Vector2(100, 40);
                settings._fulfilledLimit.Value = 120;
                switch (settingByNeed.Key)
                {
                    case Needs.Need.Food:
                        settings._fulfilledEffectValue.Value = 33; 
                        break;
                    case Needs.Need.Drink:
                        settings._fulfilledEffectValue.Value = 33;
                        Needs._overrideDrinkValues.Value = true;
                        Needs._drinkValuesPotions.Value = 10;
                        Needs._drinkValuesOther.Value = 20;
                        break;
                    case Needs.Need.Sleep: 
                        settings._fulfilledEffectValue.Value = 115;
                        Needs._sleepNegativeEffect.Value = -12;
                        Needs._sleepNegativeEffectIsPercent.Value = true;
                        Needs._sleepBuffsDuration.Value = 0;
                        break;
                }
                Needs._allowCuresWhileOverlimited.Value = true;
                Needs._allowOnlyDOTCures.Value = true;

            }
        }
    }
}
