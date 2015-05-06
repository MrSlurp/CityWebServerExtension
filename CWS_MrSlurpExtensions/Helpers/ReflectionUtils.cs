using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CWS_MrSlurpExtensions
{
    public static class ReflectionUtil
    {
        public static FieldInfo FindField<T>(T o, string fieldName)
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                if (f.Name == fieldName)
                {
                    return f;
                }
            }
            return null;
        }
        public static T GetFieldValue<T>(FieldInfo field, object o)
        {
            return (T)field.GetValue(o);
        }

        public static void SetFieldValue(FieldInfo field, object o, object value)
        {
            field.SetValue(o, value);
        }

        public static Q GetPrivate<Q>(object o, string fieldName)
        {
            var fields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo field = null;
            foreach (var f in fields)
            {
                if (f.Name == fieldName)
                {
                    field = f;
                    break;
                }
            }
            return (Q)field.GetValue(o);
        }

        public static void SetPrivate<Q>(object o, string fieldName, object value)
        {
            var fields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo field = null;
            foreach (var f in fields)
            {
                if (f.Name == fieldName)
                {
                    field = f;
                    break;
                }
            }
            field.SetValue(o, value);
        }

        public static int? GetIntegerFieldValue<T>(T o,string fieldName)
        {
            int? finalValue = null;
            FieldInfo fieldInfo = ReflectionUtil.FindField(o, fieldName);
            if (fieldInfo != null)
            {
                if (fieldInfo.FieldType == typeof(UInt32))
                    finalValue = (int)ReflectionUtil.GetFieldValue<UInt32>(fieldInfo, o);
                else if (fieldInfo.FieldType == typeof(Int32))
                    finalValue = (int)ReflectionUtil.GetFieldValue<Int32>(fieldInfo, o);
                else if (fieldInfo.FieldType == typeof(UInt16))
                    finalValue = (int)ReflectionUtil.GetFieldValue<UInt16>(fieldInfo, o);
                else if (fieldInfo.FieldType == typeof(Byte))
                    finalValue = (int)ReflectionUtil.GetFieldValue<Byte>(fieldInfo, o);
                else
                {
                    CityInfoRequestHandler.LogMessages("Not supported value type", fieldInfo.FieldType.ToString(), "for", fieldName);
                }
            }
            else
            {
                CityInfoRequestHandler.LogMessages(string.Format("Field {0} not found in object type {1}", fieldName, o.GetType().ToString()));
            }
            return finalValue;
        }
    }
}
