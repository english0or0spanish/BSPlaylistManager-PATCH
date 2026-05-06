using System;
using System.Linq;
using System.Reflection;

namespace PlaylistManager.Utilities
{
    internal static class ReflectionUtils
    {
        private const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static FieldInfo GetFieldInfo(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, AllFlags);
                if (field != null)
                {
                    return field;
                }
            }
            return null;
        }

        public static PropertyInfo GetPropertyInfo(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                PropertyInfo property = current.GetProperty(name, AllFlags);
                if (property != null)
                {
                    return property;
                }
            }
            return null;
        }

        public static object GetPrivateField(this object instance, string fieldName)
        {
            if (instance == null || string.IsNullOrEmpty(fieldName))
            {
                return null;
            }
            FieldInfo field = GetFieldInfo(instance.GetType(), fieldName);
            return field?.GetValue(instance);
        }

        public static T GetPrivateField<T>(this object instance, string fieldName)
        {
            object value = GetPrivateField(instance, fieldName);
            return value is T t ? t : default;
        }

        public static void SetPrivateField(this object instance, string fieldName, object value)
        {
            if (instance == null || string.IsNullOrEmpty(fieldName))
            {
                return;
            }
            FieldInfo field = GetFieldInfo(instance.GetType(), fieldName);
            field?.SetValue(instance, value);
        }

        public static object GetPrivateProperty(this object instance, string propertyName)
        {
            if (instance == null || string.IsNullOrEmpty(propertyName))
            {
                return null;
            }
            PropertyInfo property = GetPropertyInfo(instance.GetType(), propertyName);
            return property?.GetValue(instance);
        }

        public static T GetPrivateProperty<T>(this object instance, string propertyName)
        {
            object value = GetPrivateProperty(instance, propertyName);
            return value is T t ? t : default;
        }

        public static void SetPrivateProperty(this object instance, string propertyName, object value)
        {
            if (instance == null || string.IsNullOrEmpty(propertyName))
            {
                return;
            }
            PropertyInfo property = GetPropertyInfo(instance.GetType(), propertyName);
            property?.SetValue(instance, value);
        }

        public static object GetMemberValue(this object instance, string memberName)
        {
            if (instance == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }
            object value = GetPrivateProperty(instance, memberName);
            if (value != null)
            {
                return value;
            }
            return GetPrivateField(instance, memberName);
        }

        public static T GetMemberValue<T>(this object instance, string memberName)
        {
            object value = GetMemberValue(instance, memberName);
            return value is T t ? t : default;
        }

        public static void SetMemberValue(this object instance, string memberName, object value)
        {
            if (instance == null || string.IsNullOrEmpty(memberName))
            {
                return;
            }
            if (GetPropertyInfo(instance.GetType(), memberName) is PropertyInfo property)
            {
                property.SetValue(instance, value);
                return;
            }
            FieldInfo field = GetFieldInfo(instance.GetType(), memberName);
            field?.SetValue(instance, value);
        }

        public static MethodInfo GetMethodInfo(Type type, string methodName, Type[] parameterTypes = null)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                MethodInfo method = null;
                if (parameterTypes != null)
                {
                    method = current.GetMethod(methodName, AllFlags, null, parameterTypes, null);
                    if (method != null)
                    {
                        return method;
                    }
                }
                MethodInfo[] candidates = current.GetMethods(AllFlags).Where(m => m.Name == methodName).ToArray();
                if (parameterTypes == null)
                {
                    if (candidates.Length == 1)
                    {
                        return candidates[0];
                    }
                }
                else
                {
                    foreach (MethodInfo candidate in candidates)
                    {
                        ParameterInfo[] parameters = candidate.GetParameters();
                        if (parameters.Length != parameterTypes.Length)
                        {
                            continue;
                        }
                        bool match = true;
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (!parameters[i].ParameterType.IsAssignableFrom(parameterTypes[i]) && parameterTypes[i] != typeof(object))
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            return candidate;
                        }
                    }
                }
            }
            return null;
        }

        public static object InvokeMethod(this object instance, string methodName, params object[] args)
        {
            if (instance == null || string.IsNullOrEmpty(methodName))
            {
                return null;
            }
            Type[] argTypes = args?.Select(arg => arg?.GetType() ?? typeof(object)).ToArray() ?? Type.EmptyTypes;
            MethodInfo method = GetMethodInfo(instance.GetType(), methodName, argTypes) ?? GetMethodInfo(instance.GetType(), methodName, null);
            return method?.Invoke(instance, args);
        }

        public static object InvokeMethod(this object instance, Type[] parameterTypes, string methodName, params object[] args)
        {
            if (instance == null || string.IsNullOrEmpty(methodName))
            {
                return null;
            }
            MethodInfo method = GetMethodInfo(instance.GetType(), methodName, parameterTypes);
            return method?.Invoke(instance, args);
        }

        public static Type GetNestedType(this Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                Type nested = current.GetNestedType(name, AllFlags);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }

        public static object CreateEnumValue(Type enumType, string valueName)
        {
            if (enumType == null || string.IsNullOrEmpty(valueName) || !enumType.IsEnum)
            {
                return null;
            }
            return Enum.Parse(enumType, valueName);
        }
    }
}
