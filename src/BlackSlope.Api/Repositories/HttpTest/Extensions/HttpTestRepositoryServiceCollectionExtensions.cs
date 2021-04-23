using System;
using BlackSlope.Repositories.HttpTest;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpTestRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpTestRepository(
            this IServiceCollection services)
        {
            services.TryAddScoped<IHttpTestRepository, HttpTestRepository>();

            services.AddHttpClient<IHttpTestRepository, HttpTestRepository>()
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
