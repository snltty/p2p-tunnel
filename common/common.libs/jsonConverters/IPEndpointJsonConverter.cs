using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace common.libs.jsonConverters
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class IPEndpointJsonConverter : JsonConverter<IPEndPoint>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return IPEndPoint.Parse(reader.GetString());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
