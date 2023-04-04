using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace common.libs.extends
{
    public static class EnumExtends
    {
        private static ConcurrentDictionary<Type, ConcurrentDictionary<byte, string>> enumDsecs = new ConcurrentDictionary<Type, ConcurrentDictionary<byte, string>>();
        public static string GetDesc(this Enum obj, byte value)
        {
            Type type = obj.GetType();
            if (!enumDsecs.TryGetValue(type, out ConcurrentDictionary<byte, string> dic))
            {
                dic = new ConcurrentDictionary<byte, string>();
                FieldInfo[] fields = type.GetFields();
                for (int i = 0; i < fields.Length; i++)
                {
                    byte val = (byte)fields[i].GetValue(obj)!;
                    if (string.Equals(fields[i].Name, "value__") == false)
                    {
                        object[] attrs = fields[i].GetCustomAttributes(typeof(DescriptionAttribute), false);
                        if (attrs.Length > 0)
                        {
                            dic.TryAdd(val, $"{fields[i].Name} {((DescriptionAttribute)attrs[0]).Description}");
                        }
                        else
                        {
                            dic.TryAdd(val, $"{fields[i].Name}");
                        }
                    }

                }
                enumDsecs.TryAdd(type, dic);
            }
            return dic[value];
        }
    }

}
