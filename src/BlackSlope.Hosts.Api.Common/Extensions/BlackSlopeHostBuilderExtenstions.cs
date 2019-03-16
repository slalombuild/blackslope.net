using BlackSlope.Hosts.Api.Common.Configurtion;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace BlackSlope.Hosts.Api.Common.Extensions
{
    public static class BlackSlopeHostBuilderExtenstions
    {
        public static IWebHostBuilder UseSerilog(this IWebHostBuilder webHostBuilder, string appSettingsSection)
        {
            return webHostBuilder.UseSerilog(
                 (ctx, config) =>
                 {
                     var appSettings = ctx.Configuration.GetSection(appSettingsSection).Get<HostConfig>();
                     var serilogConfig = appSettings.Serilog;

                     // If they follow conventions for Serilog in config it will be read here.
                     config.ReadFrom.Configuration(ctx.Configuration);

                     SetLogLevel(config, serilogConfig);
                     LogToFile(config, serilogConfig);
                     LogToConsole(config, serilogConfig);
                     LogToApplicationInsights(config, appSettings, serilogConfig);
                 });
        }

        private static void LogToApplicationInsights(LoggerConfiguration config, HostConfig appSettings, SerilogConfig serilogConfig)
        {
            if (serilogConfig.WriteToAppInsights)
            {
                // TODO: TelemetryConverter should be configurable (defaulted to Trace)
                // Note: best practice is to use the existing Telemetry 
                if (string.IsNullOrEmpty(TelemetryConfiguration.Active.InstrumentationKey))
                {
                    // TODO get the instrumentation key from the config file
                    // webHostBuilder.UseApplicationInsights("ba3d25ea-5b60-4d5d-9340-06326585e663");
                    config.WriteTo.ApplicationInsights(appSettings.ApplicationInsights.InstrumentationKey, TelemetryConverter.Traces);
                    config.WriteTo.ApplicationInsights(appSettings.ApplicationInsights.InstrumentationKey, TelemetryConverter.Events);
                }
                else
                {
                    config.WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces);
                    config.WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Events);
                }
            }
        }

        private static void LogToConsole(LoggerConfiguration config, SerilogConfig serilogConfig)
        {
            if (serilogConfig.WriteToConsole)
            {
                config.WriteTo.ColoredConsole();
            }
        }

        private static void LogToFile(LoggerConfiguration config, SerilogConfig serilogConfig)
        {
            if (serilogConfig.WriteToFile)
            {
                // TODO: Rolling Interval day should be configurable (defaulted to day)
                config.WriteTo.File(serilogConfig.FileName, rollingInterval: RollingInterval.Day);
            }
        }

        private static void SetLogLevel(LoggerConfiguration config, SerilogConfig serilogConfig)
        {
            // TODO add check to skip this if the minimum level is not defined
            var levelSwitch = new LoggingLevelSwitch
            {
                MinimumLevel = (LogEventLevel)serilogConfig.MinimumLevel
            };

            config.MinimumLevel.ControlledBy(levelSwitch);
        }
    }
}
