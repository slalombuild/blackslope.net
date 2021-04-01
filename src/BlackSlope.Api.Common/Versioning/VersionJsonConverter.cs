using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlackSlope.Api.Common.Versioning
{
    public class VersionJsonConverter : JsonConverter<Version>
    {
        private const string _versionProperty = "version";
        private readonly JsonEncodedText _buildVersionName = JsonEncodedText.Encode(_versionProperty);

        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            // confirm version property from JSON
            string buildVersion;
            if (reader.ValueTextEquals(_buildVersionName.EncodedUtf8Bytes))
            {
                reader.Read();
                buildVersion = reader.GetString();
            }
            else
            {
                throw new JsonException();
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }

            return new Version(buildVersion);
        }

        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
        {
            writer?.WriteStartObject();
            writer.WritePropertyName(_buildVersionName);
            writer.WriteStringValue(value?.BuildVersion);
            writer.WriteEndObject();

            writer.Flush();
        }
    }
}
