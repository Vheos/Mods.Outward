using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;



namespace ModPack
{
    public abstract class AMod
    {
        #region const
        private const int MAX_SETTINGS_PER_MOD = 1000;
        public const string SECTION_SURVIVAL = "     \nSURVIVAL & IMMERSION";
        public const string SECTION_COMBAT = "    \nCOMBAT";
        public const string SECTION_SKILLS = "   \nSKILLS";
        public const string SECTION_UI = "  \nUSER INTERFACE";
        public const string SECTION_VARIOUS = " \nVARIOUS";
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
            // Survival & Immersion
            typeof(Needs),
            typeof(Camping),
            typeof(Crafting),
            typeof(Durability),
            typeof(Prices),
            typeof(Resets),
            typeof(Interactions),
            typeof(Revive),

            // Combat
            typeof(Damage),
            typeof(Speed),
            typeof(Targeting),
            typeof(Traps),
            
            // Skills
            typeof(SkillEditor),
            typeof(SkillRandomizer),
            typeof(SkillLimits),

            // UI
            typeof(GUI),
            typeof(Descriptions),
            typeof(Camera),
            typeof(KeyboardWalk),
            typeof(Gamepad),

            // Various
            typeof(Various),
        };

        // Privates
        private readonly Harmony _patcher;
        private readonly List<AModSetting> _settings;
        private readonly List<Action> _onConfigClosedEvents;
        private bool _isInitialized;
        private string SectionName
        => GetType().Name;
        private int ModOrderingOffset
        {
            get
            {
                int index = Array.IndexOf(MODS_ORDERING, GetType());
                if (index < 0)
                    return int.MaxValue;
                return index.Add(1).Mul(MAX_SETTINGS_PER_MOD);
            }
        }
        virtual protected string SectionOverride
        => "";
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

                Tools.Log($"\t[{GetType().Name}] Initializing...");
                Initialize();
                Indent++;
                Tools.Log($"\t[{GetType().Name}] Formatting...");
                SetFormatting();
                Indent--;
            }

            Tools.Log($"\t[{GetType().Name}] Patching...");
            _patcher.PatchAll(GetType());

            Tools.Log($"\t[{GetType().Name}] Calling events...");
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
            Tools.Log($"\t[{GetType().Name}] Main toggle: {_mainToggle.Value}");

            if (IsEnabled)
                OnEnable();
            if (IsCollapsed)
                OnCollapse();
            if (IsHidden)
                OnHide();
        }
        abstract protected void Initialize();
        abstract protected void SetFormatting();
        virtual public void LoadPreset(Presets.Preset preset)
        { }

        // Utility     
        public bool IsEnabled
        {
            get => _mainToggle.Value.HasFlag(Toggles.Apply);
            protected set
            {
                if (value)
                    _mainToggle.Value |= Toggles.Apply;
                else
                    _mainToggle.Value &= ~Toggles.Apply;
            }
        }
        protected bool IsCollapsed
        {
            get => _mainToggle.Value.HasFlag(Toggles.Collapse);
            set
            {
                if (value)
                    _mainToggle.Value |= Toggles.Collapse;
                else
                    _mainToggle.Value &= ~Toggles.Collapse;
            }
        }
        protected bool IsHidden
        {
            get => _mainToggle.Value.HasFlag(Toggles.Hide);
            set
            {
                if (value)
                    _mainToggle.Value |= Toggles.Hide;
                else
                    _mainToggle.Value &= ~Toggles.Hide;
            }
        }
        protected void ForceApply()
        {
            IsHidden = false;
            IsEnabled = true;
            IsCollapsed = true;
        }
        public void ResetSettings(bool disableSelf = false)
        {
            foreach (var setting in _settings)
                setting.Reset();
            if (disableSelf)
                IsEnabled = false;
        }
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