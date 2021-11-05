using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using BlackSlope.Api.Common.Configuration;
using BlackSlope.Api.HealthChecks;
using BlackSlope.Repositories.Movies.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BlackSlope.Api
{
    [ExcludeFromCodeCoverage]
    public class HealthCheckStartup
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
                .Get<MovieRepositoryConfiguration>();

            services.AddHealthChecks()
                .AddSqlServer(config.MoviesConnectionString, name: "MOVIES.DB", tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Database })
                .AddCheck<MoviesHealthCheck>("MOVIES.API", tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Api });
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env, HostConfig hostConfig)
        {
            if (env is null)
            {
                throw new System.ArgumentNullException(nameof(env));
            }

            var endpoint = hostConfig.HealthChecks.Endpoint;
            app.UseHealthChecks(endpoint, new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = HealthCheckResponseWriter,
            });

            // Now: /health/{tag}
            foreach (var tag in HealthCheckTag.All)
            {
                app.UseHealthChecks($"{endpoint}/{tag}", new HealthCheckOptions()
                {
                    Predicate = registration => registration.Tags.Contains(tag),
                    ResponseWriter = HealthCheckResponseWriter,
                });
            }
        }

        private static async Task HealthCheckResponseWriter(HttpContext c, HealthReport r)
        {
            c.Response.ContentType = MediaTypeNames.Application.Json;

            var result = JsonSerializer.Serialize(new
            {
                status = r.Status.ToString(),
                details = r.Entries.Select(e => new
                {
                    key = e.Key,
                    value = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration,
                    exception = e.Value.Exception,
                }),
            });

            await c.Response.WriteAsync(result).ConfigureAwait(false);
        }
    }
}
