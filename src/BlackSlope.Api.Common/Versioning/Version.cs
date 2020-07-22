using System.Text.Json.Serialization;

namespace BlackSlope.Api.Common.Versioning
{
    [JsonConverter(typeof(VersionJsonConverter))]
    public class Version
    {
        public Version(string buildVersion)
        {
            BuildVersion = buildVersion;
        }

        [JsonPropertyName("version")]
        public string BuildVersion { get; }
    }
}
