namespace BlackSlope.Hosts.Api.Common.Configurtion
{
    public class HostConfig
    {
        public SwaggerConfig Swagger { get; set; }

        public AzureAdConfig AzureAd { get; set; }

        public SerilogConfig Serilog { get; set; }

        public ApplicationInsightsConfig ApplicationInsights { get; set; }

    }
}
