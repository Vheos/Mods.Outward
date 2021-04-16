using BepInEx.Configuration;
using System;
using System.Collections.Generic;



namespace ModPack
{
    public abstract class AModSetting
    {
        // Publics 
        public void Format(string displayName)
        {
            Attributes.DispName = "";
            if (displayName.IsNotEmpty())
            {
                Attributes.DispName = Attributes.DispName.PadLeft(5 * _indentLevel, ' ');
                if (_indentLevel > 0)
                    Attributes.DispName += "• ";
                Attributes.DispName += displayName;
            }

            Ordering = NextPosition++;
            _visibilityCheck = () => true;
        }
        public void Format(string displayName, AModSetting controller, Func<bool> check = null)
        {
            Format(displayName);
            AddVisibilityControl(controller, check);
        }
        public void Format<T>(string displayName, ModSetting<T> controller, T value, bool positiveTest = true) where T : struct
        {
            Format(displayName);
            AddVisibilityControl(controller, value, positiveTest);
        }
        public void Format(string displayName, ModSetting<bool> toggle)
        => Format(displayName, toggle, true);
        public void AddEvent(Action action, bool callNow = false)
        {
            _events.Add(action);
            _configEntryBase.ConfigFile.SettingChanged += (sender, eventArgs) =>
            {
                if (eventArgs.ChangedSetting == _configEntryBase)
                    action();
            };
            if (callNow)
                action();
        }
        public void CallAllEvents()
        {
            foreach (var action in _events)
                action();
        }
        public void UpdateVisibility()
        {
            Tools.SetDirtyConfigWindow();

            foreach (var controller in _visibilityControllers)
                if (!controller.IsVisible)
                {
                    IsVisible = false;
                    return;
                }

            IsVisible = _visibilityCheck();
        }

        // Attributes
        public string Name
        => _configEntryBase.Definition.Key;
        public string Section
        => _configEntryBase.Definition.Section;
        public string SectionOverride
        {
            get => Attributes.Category;
            set => Attributes.Category = value;
        }
        public string NameOverride
        {
            get => Attributes.DispName;
            set => Attributes.DispName = value;
        }
        public string Description
        {
            get => Attributes.Description;
            set => Attributes.Description = value;
        }
        public int Ordering
        {
            get => -(int)Attributes.Order;
            set => Attributes.Order = -value;
        }
        public bool IsVisible
        {
            get => (bool)Attributes.Browsable;
            set => Attributes.Browsable = value;
        }
        public bool IsAdvanced
        {
            get => (bool)Attributes.IsAdvanced;
            set => Attributes.IsAdvanced = value;
        }
        public bool DisplayResetButton
        {
            get => !(bool)Attributes.HideDefaultButton;
            set => Attributes.HideDefaultButton = !value;
        }
        public bool DrawInPlaceOfName
        {
            get => (bool)Attributes.HideSettingName;
            set => Attributes.HideSettingName = value;
        }
        public bool FormatAsPercent
        {
            get => (bool)Attributes.ShowRangeAsPercent;
            set => Attributes.ShowRangeAsPercent = value;
        }
        static public int Indent
        {
            get => _indentLevel;
            set => _indentLevel = value.ClampMin(0);
        }
        static public int NextPosition;

        // Privates       
        protected ConfigEntryBase _configEntryBase;
        protected List<Action> _events;
        private ConfigurationManagerAttributes Attributes
        => _configEntryBase.Description.Tags[0] as ConfigurationManagerAttributes;
        static private int _indentLevel;
        // Visibility control
        private Func<bool> _visibilityCheck;
        private List<AModSetting> _visibilityControllers;
        private void AddVisibilityControl(AModSetting controller, Func<bool> check = null)
        {
            AddParentVisibilityControllers(controller);
            if (check != null)
                _visibilityCheck = check;

            foreach (var visibilityController in _visibilityControllers)
                _configEntryBase.ConfigFile.SettingChanged += (sender, eventArgs) =>
                {
                    if (eventArgs.ChangedSetting == visibilityController._configEntryBase)
                        UpdateVisibility();
                };
        }
        private void AddVisibilityControl<T>(ModSetting<T> controller, T value, bool positiveTest = true) where T : struct
        {
            Func<bool> check;
            if (value is Enum valueAsEnum && valueAsEnum.HasFlagsAttribute())
            {
                if (positiveTest)
                    check = () => (controller.Value as Enum).HasFlag(valueAsEnum);
                else
                    check = () => !(controller.Value as Enum).HasFlag(valueAsEnum);
            }
            else
            {
                if (positiveTest)
                    check = () => controller.Value.Equals(value);
                else
                    check = () => !controller.Value.Equals(value);
            }

            AddVisibilityControl(controller, check);
        }
        private void AddParentVisibilityControllers(AModSetting controller)
        {
            _visibilityControllers.Add(controller);
            foreach (var newParentController in controller._visibilityControllers)
                _visibilityControllers.Add(newParentController);
        }

        // Constructors
        protected AModSetting()
        {
            _events = new List<Action>();
            _visibilityControllers = new List<AModSetting>();
            _visibilityCheck = () => false;
        }
    }
}