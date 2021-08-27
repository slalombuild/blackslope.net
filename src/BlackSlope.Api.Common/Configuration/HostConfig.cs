namespace BlackSlope.Api.Common.Configuration
{
    public class HostConfig
    {
        public string BaseUrl { get; set; }

        public SwaggerConfig Swagger { get; set; }

        public AzureAdConfig AzureAd { get; set; }

        public SerilogConfig Serilog { get; set; }

        public ApplicationInsightsConfig ApplicationInsights { get; set; }

        public HealthChecksConfig HealthChecks { get; set; }
    }
}
