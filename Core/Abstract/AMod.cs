﻿using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;



namespace ModPack
{
    public abstract class AMod
    {
        #region const
        private const int MAX_SETTINGS_PER_MOD = 1000;
        #endregion
        #region enum
        [Flags]
        protected enum Toggles
        {
            None = 0,
            Apply = 1 << 1,
            Collapse = 1 << 2,
            Hide = 1 << 3,
        }
        #endregion

        // Order
        static private readonly Type[] MODS_ORDERING = new[]
        {
            // Various
            typeof(Various),

            // UI
            typeof(GUI),
            typeof(Descriptions),
            typeof(Camera),
            typeof(Targeting),
            typeof(KeyboardWalk),

            // Gameplay
            typeof(Damage),
            typeof(Speed),
            typeof(Needs),
            typeof(SkillLimits),
            typeof(Camping),
            typeof(Resets),
            typeof(Interactions),
            typeof(Revive),

            // Private
            typeof(PistolTweaks),
            typeof(Prices),
            typeof(Debug),
        };

        // Privates
        private readonly Harmony _patcher;
        private readonly List<AModSetting> _settings;
        private readonly List<Action> _onConfigClosedEvents;
        private bool _isInitialized;
        private string SectionName
        => GetType().Name;
        private string SectionOverride
        => "";
        private int ModOrderingOffset
        => Array.IndexOf(MODS_ORDERING, GetType()).Add(1) * MAX_SETTINGS_PER_MOD;
        virtual protected string Description
        => "";

        // Toggles
        private ModSetting<Toggles> _mainToggle;
        private Toggles _previousMainToggle;
        private void CreateMainToggle()
        {
            _mainToggle = new ModSetting<Toggles>(SectionName, nameof(_mainToggle), Toggles.None)
            {
                SectionOverride = SectionOverride,
                DisplayResetButton = false,
                Description = Description,
            };
            _mainToggle.Format(SectionName.SplitCamelCase());
            _mainToggle.AddEvent(OnTogglesChanged);
            _previousMainToggle = _mainToggle;
        }
        private void OnTogglesChanged()
        {
            Toggles option = _mainToggle ^ _previousMainToggle;
            bool newState = _mainToggle.Value.HasFlag(option);
            switch (option)
            {
                case Toggles.Apply:
                    if (IsHidden)
                        ResetApplySilently();
                    else if (newState)
                        OnEnable();
                    else
                        OnDisable();
                    break;

                case Toggles.Collapse:
                    if (!IsEnabled || IsHidden)
                        ResetCollapseSilently();
                    else if (newState)
                        OnCollapse();
                    else
                        OnExpand();
                    break;

                case Toggles.Hide:
                    if (newState)
                        OnHide();
                    else
                        OnUnhide();
                    break;
            }

            _previousMainToggle = _mainToggle;
            Tools.SetDirtyConfigWindow();
        }
        private void OnEnable()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                ResetSettingPosition();

                Initialize();
                Indent++;
                SetFormatting();
                Indent--;
            }

            _patcher.PatchAll(GetType());

            foreach (var setting in _settings)
                setting.CallAllEvents();
            foreach (var onConfigClosed in _onConfigClosedEvents)
                onConfigClosed.Invoke();

            OnExpand();
        }
        private void OnDisable()
        {
            _patcher.UnpatchSelf();
            OnCollapse();
            ResetCollapseSilently();
        }
        private void OnCollapse()
        {
            foreach (var setting in _settings)
                setting.IsVisible = false;
        }
        private void OnExpand()
        {
            foreach (var setting in _settings)
                setting.UpdateVisibility();
        }
        private void OnHide()
        {
            _mainToggle.IsAdvanced = true;
            OnDisable();
            ResetApplySilently();
        }
        private void OnUnhide()
        {
            _mainToggle.IsAdvanced = false;
        }
        private void ResetApplySilently()
        => _mainToggle.SetSilently(_mainToggle & ~Toggles.Apply);
        private void ResetCollapseSilently()
        => _mainToggle.SetSilently(_mainToggle & ~Toggles.Collapse);
        private void ResetHideSilently()
        => _mainToggle.SetSilently(_mainToggle & ~Toggles.Hide);

        // Constructors
        protected AMod()
        {
            _patcher = new Harmony(GetType().Name);
            _settings = new List<AModSetting>();
            _onConfigClosedEvents = new List<Action>();

            ResetSettingPosition(-1);
            CreateMainToggle();

            if (IsEnabled)
                OnEnable();
            if (IsCollapsed)
                OnCollapse();
            if (IsHidden)
                OnHide();
        }
        abstract protected void Initialize();
        abstract protected void SetFormatting();

        // Utility     
        public bool IsEnabled
        => _mainToggle.Value.HasFlag(Toggles.Apply);
        protected bool IsCollapsed
        => _mainToggle.Value.HasFlag(Toggles.Collapse);
        protected bool IsHidden
        => _mainToggle.Value.HasFlag(Toggles.Hide);
        protected void ResetSettingPosition(int offset = 0)
        => AModSetting.NextPosition = ModOrderingOffset + offset;
        protected int Indent
        {
            get => AModSetting.Indent;
            set => AModSetting.Indent = value;
        }
        protected void AddEventOnConfigOpened(Action action)
        {
            _onConfigClosedEvents.Add(action);
            Tools.AddEventOnConfigOpened(() =>
            {
                if (IsEnabled)
                    action();
            });
        }
        protected void AddEventOnConfigClosed(Action action)
        {
            _onConfigClosedEvents.Add(action);
            Tools.AddEventOnConfigClosed(() =>
            {
                if (IsEnabled)
                    action();
            });
        }
        protected ModSetting<T> CreateSetting<T>(string name, T defaultValue = default, AcceptableValueBase acceptableValues = null)
        {
            ModSetting<T> newSetting = new ModSetting<T>(SectionName, name, defaultValue, acceptableValues)
            {
                SectionOverride = SectionOverride,
                FormatAsPercent = false,
            };
            _settings.Add(newSetting);
            return newSetting;
        }
        protected AcceptableValueRange<int> IntRange(int from, int to)
        => new AcceptableValueRange<int>(from, to);
        protected AcceptableValueRange<float> FloatRange(float from, float to)
        => new AcceptableValueRange<float>(from, to);
        static protected float GameTime
        {
            get => (float)EnvironmentConditions.GameTime;
            set => EnvironmentConditions.GameTime = value;
        }
    }
}