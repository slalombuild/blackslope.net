using BlackSlope.Api.Common.Middleware.Correlation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CorrelationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Correlation middleware to the IServiceCollection and configure it
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCorrelation(this IServiceCollection services)
        {
            services.TryAddTransient<ICorrelationIdRequestReader, CorrelationIdHeaderService>();
            services.TryAddTransient<ICorrelationIdResponseWriter, CorrelationIdHeaderService>();
            services.TryAddScoped<ICurrentCorrelationIdService, CurrentCorrelationIdService>();

            return services;
        }
    }
}
