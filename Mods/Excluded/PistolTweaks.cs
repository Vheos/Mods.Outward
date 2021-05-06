using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



/*
 * TO DO:
 * - cache animations by character
 * - make compatible with animationspeed changes
 */
namespace ModPack
{
    public class PistolTweaks : AMod, IDelayedInit, IDevelopmentOnly
    {
        #region const
        private const int MAX_LOADED_BULLETS = 1;   // WeaponLoadout.MaxProjectileLoaded 
        private const int BULLET_ID = 4400080;   // ResourcesPrefabManager.ITEM_PREFABS
        private const int BULLET_STACK_SIZE = 12;   // ResourcesPrefabManager.ITEM_PREFABS -> Item.MultipleUsage
        private const float BULLET_WEIGHT = 0.1f;   // ResourcesPrefabManager.ITEM_PREFABS -> Item.ItemStats
        private const int BULLET_PRICE = 3;   // ResourcesPrefabManager.ITEM_PREFABS -> Item.ItemStats
        static readonly private Character.SpellCastType[] SHOT_SPELLS =
        {
            Character.SpellCastType.PistolShot,
            Character.SpellCastType.PistolShotCheat,
            Character.SpellCastType.BloodShot,
        };
        static readonly private Character.SpellCastType[] RELOAD_SPELLS =
        {
            Character.SpellCastType.PistolBasicReload,
            Character.SpellCastType.PistolFrostReload,
            Character.SpellCastType.PislotBloodReload,
        };
        #endregion

        // Config
        static private ModSetting<float> _shotSpeed;
        static private ModSetting<float> _reloadSpeed;
        static private ModSetting<int> _bulletsPerReload;
        static private ModSetting<int> _bulletStackSize;
        static private ModSetting<float> _bulletWeight;
        static private ModSetting<int> _bulletPrice;
        override protected void Initialize()
        {
            // Settings
            _shotSpeed = CreateSetting(nameof(_shotSpeed), 1f, FloatRange(0.25f, 4f));
            _reloadSpeed = CreateSetting(nameof(_reloadSpeed), 1f, FloatRange(0.25f, 4f));
            _bulletsPerReload = CreateSetting(nameof(_bulletsPerReload), MAX_LOADED_BULLETS, IntRange(1, 12));
            _bulletStackSize = CreateSetting(nameof(_bulletStackSize), BULLET_STACK_SIZE, IntRange(1, 60));
            _bulletWeight = CreateSetting(nameof(_bulletWeight), BULLET_WEIGHT, FloatRange(0f, 1f));
            _bulletPrice = CreateSetting(nameof(_bulletPrice), BULLET_PRICE, IntRange(1, 15));

            // Events
            Item bulletPrefab = Prefabs.ItemsByID[BULLET_ID.ToString()];
            _bulletStackSize.AddEvent(() => bulletPrefab.m_stackable.m_maxStackAmount = _bulletStackSize.Value);
            _bulletWeight.AddEvent(() => bulletPrefab.Stats.m_rawWeight = _bulletWeight.Value);
            _bulletPrice.AddEvent(() => bulletPrefab.Stats.m_baseValue = _bulletPrice.Value);

            // Fields
            _overrideSpeed = float.NaN;
        }
        override protected void SetFormatting()
        {
            _shotSpeed.Format("Shot speed");
            _reloadSpeed.Format("Reload speed");
            _bulletsPerReload.Format("Bullets per reload");
            _bulletStackSize.Format("Bullet stack size");
            _bulletWeight.Format("Bullet weight");
            _bulletPrice.Format("Bullet price");
        }
        override protected string SectionOverride
        => SECTION_VARIOUS;

        // Utility
        static private float _overrideSpeed;
        static private float _originalSpeed;

        // Hooks
        [HarmonyPatch(typeof(WeaponLoadoutItem), "Load"), HarmonyPrefix]
        static bool WeaponLoadoutItem_Load_Pre(WeaponLoadoutItem __instance)
        {
            if (__instance.CompatibleAmmunition.ItemID == BULLET_ID)
                __instance.MaxProjectileLoaded = _bulletsPerReload.Value;
            return true;
        }

        [HarmonyPatch(typeof(Character), "PerformSpellCast"), HarmonyPrefix]
        static bool Character_PerformSpellCast_Pre(Character __instance)
        {
            _overrideSpeed = float.NaN;
            if (__instance.CurrentSpellCast.IsContainedIn(SHOT_SPELLS))
                _overrideSpeed = _shotSpeed.Value;
            else if (__instance.CurrentSpellCast.IsContainedIn(RELOAD_SPELLS))
                _overrideSpeed = _reloadSpeed.Value;

            if (!_overrideSpeed.IsNaN())
            {
                _originalSpeed = __instance.Animator.speed;
                __instance.Animator.speed = _overrideSpeed;
            }
            return true;
        }

        [HarmonyPatch(typeof(Character), "CastDone"), HarmonyPrefix]
        static bool Character_CastDone_Pre(Character __instance)
        {
            if (!_overrideSpeed.IsNaN())
            {
                __instance.Animator.speed = _originalSpeed;
                _overrideSpeed = float.NaN;
            }
            return true;
        }
    }
}