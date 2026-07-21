using System;
using System.Collections.Generic;
using System.Reflection;

internal static class ModReflectionHelper
{
    private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly object CacheLock = new object();
    private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache =
        new Dictionary<Type, Dictionary<string, PropertyInfo>>(32);
    private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> FieldCache =
        new Dictionary<Type, Dictionary<string, FieldInfo>>(32);

    private static PropertyInfo GetCachedProperty(Type instanceType, string memberName)
    {
        lock (CacheLock)
        {
            Dictionary<string, PropertyInfo> members;
            if (!PropertyCache.TryGetValue(instanceType, out members))
            {
                members = new Dictionary<string, PropertyInfo>(8);
                PropertyCache.Add(instanceType, members);
            }

            PropertyInfo property;
            if (members.TryGetValue(memberName, out property))
            {
                return property;
            }

            property = instanceType.GetProperty(memberName, MemberFlags);
            members[memberName] = property;
            return property;
        }
    }

    private static FieldInfo GetCachedField(Type instanceType, string memberName)
    {
        lock (CacheLock)
        {
            Dictionary<string, FieldInfo> members;
            if (!FieldCache.TryGetValue(instanceType, out members))
            {
                members = new Dictionary<string, FieldInfo>(8);
                FieldCache.Add(instanceType, members);
            }

            FieldInfo field;
            if (members.TryGetValue(memberName, out field))
            {
                return field;
            }

            field = instanceType.GetField(memberName, MemberFlags);
            members[memberName] = field;
            return field;
        }
    }

    internal static float GetFloatMemberValue(object instance, string memberName, float fallbackValue)
    {
        if (instance == null)
        {
            return fallbackValue;
        }

        Type instanceType = instance.GetType();

        try
        {
            PropertyInfo property = GetCachedProperty(instanceType, memberName);
            if (property != null)
            {
                object propertyValue = property.GetValue(instance, null);
                if (propertyValue is float)
                {
                    return (float)propertyValue;
                }
                if (propertyValue is double)
                {
                    return (float)(double)propertyValue;
                }
                if (propertyValue is int)
                {
                    return (int)propertyValue;
                }
            }
        }
        catch
        {
        }

        try
        {
            FieldInfo field = GetCachedField(instanceType, memberName);
            if (field != null)
            {
                object fieldValue = field.GetValue(instance);
                if (fieldValue is float)
                {
                    return (float)fieldValue;
                }
                if (fieldValue is double)
                {
                    return (float)(double)fieldValue;
                }
                if (fieldValue is int)
                {
                    return (int)fieldValue;
                }
            }
        }
        catch
        {
        }

        return fallbackValue;
    }

    internal static bool SetFloatMemberValue(object instance, string memberName, float value)
    {
        if (instance == null)
        {
            return false;
        }

        Type instanceType = instance.GetType();

        try
        {
            PropertyInfo property = GetCachedProperty(instanceType, memberName);
            if (property != null && property.CanWrite && property.PropertyType == typeof(float))
            {
                property.SetValue(instance, value, null);
                return true;
            }
        }
        catch
        {
        }

        try
        {
            FieldInfo field = GetCachedField(instanceType, memberName);
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(instance, value);
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    internal static object GetMemberValue(object instance, string memberName)
    {
        if (instance == null)
        {
            return null;
        }

        Type instanceType = instance.GetType();

        try
        {
            PropertyInfo property = GetCachedProperty(instanceType, memberName);
            if (property != null)
            {
                return property.GetValue(instance, null);
            }
        }
        catch
        {
        }

        try
        {
            FieldInfo field = GetCachedField(instanceType, memberName);
            if (field != null)
            {
                return field.GetValue(instance);
            }
        }
        catch
        {
        }

        return null;
    }
}
