namespace BlackSlope.Api.Common.Versioning
{
    public class Version
    {
        public Version(string buildVersion)
        {
            BuildVersion = buildVersion;
        }

        public string BuildVersion { get; }
    }
}
