using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace common.libs.extends
{
    /// <summary>
    /// 
    /// </summary>
    public static class EnumExtends
    {
        private static ConcurrentDictionary<Type, ConcurrentDictionary<byte, string>> enumDsecs = new ConcurrentDictionary<Type, ConcurrentDictionary<byte, string>>();
        private static string enumFirstName = "value__";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDesc(this Enum obj, byte value)
        {
            Type type = obj.GetType();
            if (!enumDsecs.TryGetValue(type, out ConcurrentDictionary<byte, string> dic))
            {
                dic = new ConcurrentDictionary<byte, string>();
                FieldInfo[] fields = type.GetFields();
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].Name != enumFirstName)
                    {
                        object[] attrs = fields[i].GetCustomAttributes(typeof(DescriptionAttribute), false);
                        byte key = (byte)fields[i].GetValue(obj)!;
                        if (attrs.Length > 0)
                        {
                            dic.TryAdd(key, $"{fields[i].Name} {((DescriptionAttribute)attrs[0]).Description}");
                        }
                        else
                        {
                            dic.TryAdd(key, $"{fields[i].Name}");
                        }
                    }
                }
                enumDsecs.TryAdd(type, dic);
            }
            return dic[value];
        }
    }

}
