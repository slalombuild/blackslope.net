using System;
using BlackSlope.Api.Common.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace BlackSlope.Api.Common.Extensions
{
    public static class BlackSlopeHostBuilderExtensions
    {
        public static IWebHostBuilder UseSerilog(this IWebHostBuilder webHostBuilder, string appSettingsSection) =>
            webHostBuilder.UseSerilog((ctx, config) =>
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

        private static void LogToApplicationInsights(LoggerConfiguration config, HostConfig appSettings, SerilogConfig serilogConfig)
        {
            if (serilogConfig.WriteToAppInsights)
            {
                // Use the instrumentation key directly - this is the correct approach for the latest versions
                if (!string.IsNullOrEmpty(appSettings.ApplicationInsights.InstrumentationKey))
                {
                    config.WriteTo.ApplicationInsights(appSettings.ApplicationInsights.InstrumentationKey, TelemetryConverter.Traces);
                    config.WriteTo.ApplicationInsights(appSettings.ApplicationInsights.InstrumentationKey, TelemetryConverter.Events);
                }
                else
                {
                    // Log a warning that Application Insights configuration is missing
                    config.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] Warning: Application Insights InstrumentationKey is not configured. Application Insights logging will be skipped.{NewLine}");
                }
            }
        }

        private static void LogToConsole(LoggerConfiguration config, SerilogConfig serilogConfig)
        {
            if (serilogConfig.WriteToConsole)
            {
                config.WriteTo.Console();
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
            Enum.TryParse<LogEventLevel>(serilogConfig.MinimumLevel, true, out var minimumLevel);
            var levelSwitch = new LoggingLevelSwitch
            {
                MinimumLevel = minimumLevel,
            };

            config.MinimumLevel.ControlledBy(levelSwitch);
        }
    }
}
