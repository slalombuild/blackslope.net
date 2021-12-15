using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BlackSlope.Api.HealthChecks
{
    public class MoviesHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MoviesHealthCheck(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _httpClientFactory.CreateClient("movies")
                    .GetAsync("api/version", cancellationToken).ConfigureAwait(false);
                return result.IsSuccessStatusCode
                    ? new HealthCheckResult(HealthStatus.Healthy)
                    : new HealthCheckResult(HealthStatus.Unhealthy);
            }
            catch (Exception e)
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, exception: e);
            }
        }
    }
}
