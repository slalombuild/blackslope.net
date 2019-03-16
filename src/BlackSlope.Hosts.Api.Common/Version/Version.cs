namespace BlackSlope.Hosts.Api.Common.Version
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
