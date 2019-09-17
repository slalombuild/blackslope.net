namespace BlackSlope.Api.Common.Configuration
{
    public class SerilogConfig
    {
        public string MinimumLevel { get; set; }

        public string FileName { get; set; }

        public bool WriteToConsole { get; set; }

        public bool WriteToFile { get; set; }

        public bool WriteToAppInsights { get; set; }
    }
}
