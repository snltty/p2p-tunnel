using common.libs.jsonConverters;
using System.Text.Json;
using System.Text.Unicode;

namespace common.libs.extends
{
    /// <summary>
    /// 
    /// </summary>
    public static class SerialzeExtends
    {
        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true,
            Converters = { new IPAddressJsonConverter(), new IPEndpointJsonConverter() }
        };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToJson(this object obj)
        {
            return JsonSerializer.Serialize(obj, options: jsonSerializerOptions);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T DeJson<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json, options: jsonSerializerOptions);
        }
    }
}
