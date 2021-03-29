using BlackSlope.Repositories.Movies.Context;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApiServiceCollectionExtensions
    {
        /// <summary>
        /// Adds health check service to the Service Collection
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHealthChecksService(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<MovieContext>();

            return services;
        }
    }
}
