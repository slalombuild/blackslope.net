using System;
using BlackSlope.Repositories.FakeApi;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FakeApiRepositoryWithPollyServiceCollectionExtensions
    {
        public static IServiceCollection AddFakeApiRepository(
            this IServiceCollection services)
        {
            services.TryAddScoped<IFakeApiRepository, FakeApiRepository>();

            services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(3)) // 3 min request lifecycle
                .AddPolicyHandler(_ => // Retry x3 w/ Exponential Backoff
                    HttpPolicyExtensions.HandleTransientHttpError()
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                    .WaitAndRetryAsync(2, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            return services;
        }
    }
}
