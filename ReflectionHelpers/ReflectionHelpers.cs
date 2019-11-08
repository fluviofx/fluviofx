using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;

[assembly : InternalsVisibleTo("FluvioFX.Editor")]
[assembly : InternalsVisibleTo("FluvioFX.Install")]

namespace FluvioFX.Editor
{
    #region Parameters
    struct Parameter
    {
        #region Public API
        public object obj;
        public Type parameterType;
        public Parameter(Object obj)
        {
            this.obj = obj;
            parameterType = obj == null ? typeof(object) : obj.GetType();
        }
        public Parameter(Object obj, Type type)
        {
            this.obj = obj;
            parameterType = type;
        }
        #endregion
    }
    #endregion

    static class ReflectionHelpers
    {
        private static Dictionary<string, Type> _editorTypeMap = new Dictionary<string, Type>();
        private static Dictionary<string, Type> _runtimeTypeMap = new Dictionary<string, Type>();
        static ReflectionHelpers()
        {
            // HACK: Total hack :)
            var types =
                from a in AppDomain.CurrentDomain.GetAssemblies()
            from t in a.GetTypes()
            orderby a.FullName.Length descending
            select t;

            foreach (var type in types)
            {
                var assembly = type.Assembly;
                if (assembly.FullName.StartsWith("UnityEngine"))
                {
                    _runtimeTypeMap[type.Name] = type;
                }
                else if (assembly.FullName.StartsWith("UnityEditor"))
                {
                    _editorTypeMap[type.Name] = type;
                }
            }
        }
        internal static T Cast<T>(this object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            var type = typeof(T);
            return (T) Cast(obj, type);
        }
        internal static object Cast(this object obj, Type type)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (type == null) throw new ArgumentNullException("type");
            if (type.IsEnum)
            {
                return Enum.ToObject(type, obj);
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return Convert.ChangeType(obj, Nullable.GetUnderlyingType(type));
            }
            return Convert.ChangeType(obj, type);
        }
        internal static Type GetEditorType(string typeName)
        {
            if (typeName == null) throw new ArgumentNullException("typeName");

            Type type;
            if (_editorTypeMap.TryGetValue(typeName, out type))
            {
                return type;
            }

            // Fallback
            return GetTypeInAssembly(typeof(UnityEditor.Editor), string.Format("UnityEditor.{0}", typeName));
        }
        internal static FieldInfo GetField(this object obj, string fieldName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (fieldName == null) throw new ArgumentNullException("fieldName");

            var t = obj.GetType();

            while (t != null)
            {
                var candidate = t.GetField(fieldName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public);

                if (candidate != null) return candidate;
                t = t.BaseType;
            }
            return null;

        }
        internal static Type GetFieldType(this Type type, string fieldName)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (fieldName == null) throw new ArgumentNullException("fieldName");

            var t = type;

            while (t != null)
            {
                var candidate = t.GetField(fieldName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public);

                if (candidate != null) return candidate.FieldType;
                t = t.BaseType;
            }
            return null;
        }
        internal static Type GetFieldType(this object obj, string fieldName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (fieldName == null) throw new ArgumentNullException("fieldName");
            return GetFieldType(obj.GetType(), fieldName);
        }
        internal static object GetFieldValue(this Type type, string fieldName)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (fieldName == null) throw new ArgumentNullException("fieldName");
            return GetFieldValue<object>(type, null, fieldName);
        }
        internal static T GetFieldValue<T>(this Type type, string fieldName)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (fieldName == null) throw new ArgumentNullException("fieldName");
            return GetFieldValue<T>(type, null, fieldName);
        }
        internal static object GetFieldValue(this object obj, string fieldName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (fieldName == null) throw new ArgumentNullException("fieldName");
            return GetFieldValue<object>(obj.GetType(), obj, fieldName);
        }
        internal static T GetFieldValue<T>(this object obj, string fieldName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (fieldName == null) throw new ArgumentNullException("fieldName");
            return GetFieldValue<T>(obj.GetType(), obj, fieldName);
        }
        internal static MethodInfo GetMethod(this object obj, string methodName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (methodName == null) throw new ArgumentNullException("methodName");

            var t = obj.GetType();

            while (t != null)
            {
                var candidate = t.GetMethod(methodName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public);

                if (candidate != null) return candidate;
                t = t.BaseType;
            }

            return null;
        }
        internal static PropertyInfo GetProperty(this object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            var t = obj.GetType();

            while (t != null)
            {
                var candidate = t.GetProperty(propertyName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public);

                if (candidate != null) return candidate;
                t = t.BaseType;
            }
            return null;
        }
        internal static Type GetPropertyType(this Type type, string propertyName)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            var t = type;

            while (t != null)
            {
                var candidate = t.GetProperty(propertyName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public);

                if (candidate != null) return candidate.PropertyType;
                t = t.BaseType;
            }
            return null;
        }
        internal static Type GetPropertyType(this object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            return GetPropertyType(obj.GetType(), propertyName);
        }
        internal static int GetLength(this object obj)
        {
            return GetPropertyValue<int>(obj.GetType(), obj, "Length");
        }
        internal static object GetPropertyValue(this Type type, string propertyName)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            return GetPropertyValue<object>(type, null, propertyName);
        }
        internal static T GetPropertyValue<T>(this Type type, string propertyName)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            return GetPropertyValue<T>(type, null, propertyName);
        }
        internal static object GetPropertyValue(this object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            return GetPropertyValue<object>(obj.GetType(), obj, propertyName);
        }
        internal static T GetPropertyValue<T>(this object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            return GetPropertyValue<T>(obj.GetType(), obj, propertyName);
        }
        internal static Type GetRuntimeType(string typeName)
        {
            if (typeName == null) throw new ArgumentNullException("typeName");

            Type type;
            if (_runtimeTypeMap.TryGetValue(typeName, out type))
            {
                return type;
            }

            // Fallback
            return GetTypeInAssembly(typeof(UnityEngine.Object), string.Format("UnityEngine.{0}", typeName));
        }
        internal static Type GetTypeInAssembly(this Type type, string typeName)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (typeName == null) throw new ArgumentNullException("typeName");
            return type.Assembly.GetType(typeName);
        }
        internal static object Invoke(this Type type, string methodName, params Parameter[] parameters)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (methodName == null) throw new ArgumentNullException("methodName");
            return Invoke<object>(type, null, methodName, parameters);
        }
        internal static T Invoke<T>(this Type type, string methodName, params Parameter[] parameters)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (methodName == null) throw new ArgumentNullException("methodName");
            return Invoke<T>(type, null, methodName, parameters);
        }
        internal static object Invoke(this object obj, string methodName, params Parameter[] parameters)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (methodName == null) throw new ArgumentNullException("methodName");
            return Invoke<object>(obj.GetType(), obj, methodName, parameters);
        }
        internal static T Invoke<T>(this object obj, string methodName, params Parameter[] parameters)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (methodName == null) throw new ArgumentNullException("methodName");
            return Invoke<T>(obj.GetType(), obj, methodName, parameters);
        }
        internal static Type NestedType(this Type type, string typeName)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (typeName == null) throw new ArgumentNullException("typeName");
            return type.GetNestedType(typeName,
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.Public);
        }
        internal static Parameter Param(this object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            return new Parameter(obj);
        }
        internal static void SetFieldValue(this Type type, string fieldName, object value)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (fieldName == null) throw new ArgumentNullException("fieldName");
            SetFieldValue(type, null, fieldName, value);
        }
        internal static void SetFieldValue(this object obj, string fieldName, object value)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (fieldName == null) throw new ArgumentNullException("fieldName");
            SetFieldValue(obj.GetType(), obj, fieldName, value);
        }
        internal static void SetPropertyValue(this Type type, string propertyName, object value)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            SetPropertyValue(type, null, propertyName, value);
        }
        internal static void SetPropertyValue(this object obj, string propertyName, object value)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            SetPropertyValue(obj.GetType(), obj, propertyName, value);
        }
        internal static object Create(this Type type, params Parameter[] parameters)
        {
            return Create<object>(type, parameters);
        }
        internal static T Create<T>(this Type type, params Parameter[] parameters)
        {
            if (type == null) throw new ArgumentNullException("type");
            return (T)
            type.GetConstructor(
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    parameters.Select(p => p.parameterType).ToArray(), null)
                .Invoke(parameters.Select(p => p.obj).ToArray());
        }
        static T GetFieldValue<T>(Type type, object obj, string fieldName)
        {
            var t = type;

            while (t != null)
            {
                var candidate = t.GetField(fieldName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public);

                if (candidate != null) return (T) candidate.GetValue(obj);
                t = t.BaseType;
            }
            return default(T);
        }
        static T GetPropertyValue<T>(Type type, object obj, string propertyName)
        {
            var t = type;

            while (t != null)
            {
                var candidate = t.GetProperty(propertyName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public);

                if (candidate != null) return (T) candidate.GetValue(obj, null);
                t = t.BaseType;
            }

            return default(T);
        }
        static T Invoke<T>(Type type, object obj, string methodName, params Parameter[] parameters)
        {
            var t = type;

            while (t != null)
            {
                var candidate = t.GetMethod(methodName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public, null, parameters.Select(p => p.parameterType).ToArray(),
                    null);

                if (candidate != null) return (T) candidate.Invoke(obj, parameters.Select(p => p.obj).ToArray());
                t = t.BaseType;
            }

            return default(T);
        }
        static void SetFieldValue(Type type, object obj, string fieldName, object value)
        {
            var t = type;

            while (t != null)
            {
                var candidate = t.GetField(fieldName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public);
                if (candidate != null)
                {
                    candidate.SetValue(obj, value);
                    return;
                }
                t = t.BaseType;
            }
        }
        static void SetPropertyValue(Type type, object obj, string propertyName, object value)
        {
            var t = type;

            while (t != null)
            {
                var candidate = t.GetProperty(propertyName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.Public);

                if (candidate != null)
                {
                    candidate.SetValue(obj, value, null);
                    return;
                }
                t = t.BaseType;
            }
        }
    }
}
