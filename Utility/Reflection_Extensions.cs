using System;
using System.Reflection;
using System.Text;

namespace ModPack
{
    #region enum
    [Flags]
    public enum Data
    {
        None = 0,
        Names = 1 << 1,
        Types = 1 << 2,
        Values = 1 << 3,
        NamesAndTypes = Names | Types,
        NamesAndValues = Names | Values,
        TypesAndValues = Types | Values,
        All = Names | Types | Values,
    }

    [Flags]
    public enum Members
    {
        None = 0,
        Fields = 1 << 1,
        Properties = 1 << 2,
        Methods = 1 << 3,
        FieldsAndProperties = Fields | Properties,
        FieldsAndMethods = Fields | Methods,
        PropertiesAndMethods = Properties | Methods,
        All = Fields | Properties | Methods,
    }

    [Flags]
    public enum ScopeModifiers
    {
        None = 0,
        Instance = 1 << 1,
        Static = 1 << 2,
        All = Instance | Static,
    }

    [Flags]
    public enum AccessModifiers
    {
        None = 0,
        Private = 1 << 1,
        Public = 1 << 2,
        All = Private | Public,
    }
    #endregion

    static public class Reflection_Extensions
    {
        // Instance field
        static public TReturn GetField<TReturn>(this object instance, string fieldName)
        => instance.GetType().GetField(fieldName, InstancePrivates).GetValue(instance).As<TReturn>();
        static public TReturn GetField<TReturn, TBase>(this object instance, string fieldName)
        => typeof(TBase).GetField(fieldName, InstancePrivates).GetValue(instance).As<TReturn>();
        static public void SetField<TValue>(this object instance, string fieldName, TValue value)
        => instance.GetType().GetField(fieldName, InstancePrivates).SetValue(instance, value);
        static public void SetField<TValue, TBase>(this object instance, string fieldName, TValue value)
        => typeof(TBase).GetField(fieldName, InstancePrivates).SetValue(instance, value);
        // Instance property
        static public TReturn GetProperty<TReturn>(this object instance, string propName)
        => instance.GetType().GetProperty(propName, InstancePrivates).GetValue(instance).As<TReturn>();
        static public TReturn GetProperty<TReturn, TBase>(this object instance, string propName)
        => typeof(TBase).GetType().GetProperty(propName, InstancePrivates).GetValue(instance).As<TReturn>();
        static public void SetProperty<TValue>(this object instance, string propName, TValue value)
        => instance.GetType().GetProperty(propName, InstancePrivates).SetValue(instance, value);
        static public void SetProperty<TValue, TBase>(this object instance, string propName, TValue value)
        => typeof(TBase).GetProperty(propName, InstancePrivates).SetValue(instance, value);
        // Instance method
        static public TReturn InvokeMethod<TReturn>(this object instance, string methodName, params object[] methodParams)
        => instance.GetType().GetMethod(methodName, InstancePrivates).Invoke(instance, methodParams).As<TReturn>();
        static public TReturn InvokeMethod<TReturn, TBase>(this object instance, string methodName, params object[] methodParams)
        => typeof(TBase).GetMethod(methodName, InstancePrivates).Invoke(instance, methodParams).As<TReturn>();
        static public void InvokeMethodVoid(this object instance, string methodName, params object[] methodParams)
        => instance.GetType().GetMethod(methodName, InstancePrivates).Invoke(instance, methodParams);
        static public void InvokeMethodVoid<TBase>(this object instance, string methodName, params object[] methodParams)
        => typeof(TBase).GetMethod(methodName, InstancePrivates).Invoke(instance, methodParams);
        // Static field
        static public TReturn GetField<TReturn>(this Type type, string fieldName)
        => type.GetField(fieldName, StaticPrivates).GetValue(null).As<TReturn>();
        static public void SetField<TValue>(this Type type, string fieldName, TValue value)
        => type.GetField(fieldName, StaticPrivates).SetValue(null, value);
        // Static property
        static public TReturn GetProperty<TReturn>(this Type type, string propName)
        => type.GetProperty(propName, StaticPrivates).GetValue(null).As<TReturn>();
        static public void SetProperty<TValue>(this Type type, string propName, TValue value)
        => type.GetProperty(propName, StaticPrivates).SetValue(null, value);
        // Static method
        static public TReturn InvokeMethod<TReturn>(this Type type, string methodName, params object[] methodParams)
        => type.GetMethod(methodName, StaticPrivates).Invoke(null, methodParams).As<TReturn>();
        static public void InvokeMethodVoid(this Type type, string methodName, params object[] methodParams)
        => type.GetMethod(methodName, StaticPrivates).Invoke(null, methodParams);

        // Dump
        static public void Dump
        (
            this object instance,
            Type type = null,
            string[] blacklist = null,
            Data data = Data.Values,
            Members members = Members.FieldsAndProperties,
            ScopeModifiers scopeModifiers = ScopeModifiers.Instance,
            AccessModifiers accessModifiers = AccessModifiers.All
        )
        {
            // Cache
            if (type == null)
                type = instance.GetType();
            BindingFlags bindingFlags = accessModifiers.ToBindingFlags() | scopeModifiers.ToBindingFlags();
            StringBuilder builder = new StringBuilder();

            // Fields
            if (members.HasFlag(Members.Fields))
                foreach (var fieldInfo in type.GetFields(bindingFlags))
                    if (blacklist == null || !fieldInfo.Name.IsContainedIn(blacklist))
                    {
                        if (data.HasFlag(Data.Names))
                            builder.Append(fieldInfo.Name, "\t");
                        if (data.HasFlag(Data.Types))
                            builder.Append(fieldInfo.FieldType.Name, "\t");
                        if (data.HasFlag(Data.Values))
                        {
                            object value = fieldInfo.GetValue(instance);
                            builder.Append((value != null ? value.ToString() : "null"), "\t");
                        }
                    }

            // Properties
            if (members.HasFlag(Members.Properties))
                foreach (var propInfo in type.GetProperties(bindingFlags))
                    if (blacklist == null || !propInfo.Name.IsContainedIn(blacklist))
                    {
                        if (data.HasFlag(Data.Names))
                            builder.Append(propInfo.Name, "\t");
                        if (data.HasFlag(Data.Types))
                            builder.Append(propInfo.PropertyType.Name, "\t");
                        if (data.HasFlag(Data.Values))
                        {
                            object value;
                            try
                            { value = propInfo.GetValue(instance); }
                            catch
                            { value = "[EXCEPTION]"; }
                            builder.Append((value != null ? value.ToString() : "null"), "\t");
                        }
                    }

            // Print
            Tools.Log(builder.ToString());
        }
        static public void Dump
        (
            this Type type,
            string[] blacklist = null,
            Data data = Data.NamesAndTypes,
            Members members = Members.FieldsAndProperties,
            ScopeModifiers scopeModifiers = ScopeModifiers.Instance,
            AccessModifiers accessModifiers = AccessModifiers.All
        )
        {
            Dump(null, type, blacklist, data, members, scopeModifiers, accessModifiers);
        }

        // Helpers
        static private BindingFlags InstancePrivates
        => BindingFlags.NonPublic | BindingFlags.Instance;
        static private BindingFlags StaticPrivates
        => BindingFlags.NonPublic | BindingFlags.Static;
        static public BindingFlags ToBindingFlags(this AccessModifiers accessModifiers)
        {
            BindingFlags flags = 0;
            if (accessModifiers.HasFlag(AccessModifiers.Private))
                flags |= BindingFlags.NonPublic;
            if (accessModifiers.HasFlag(AccessModifiers.Public))
                flags |= BindingFlags.Public;
            return flags;
        }
        static public BindingFlags ToBindingFlags(this ScopeModifiers scopeModifiers)
        {
            BindingFlags flags = 0;
            if (scopeModifiers.HasFlag(ScopeModifiers.Instance))
                flags |= BindingFlags.Instance;
            if (scopeModifiers.HasFlag(ScopeModifiers.Static))
                flags |= BindingFlags.Static;
            return flags;
        }
    }


}