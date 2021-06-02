using BlackSlope.Api.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace BlackSlope.Api.Extensions
{
    public static class MvcExtensions
    {
        public static IServiceCollection AddMvcApi(this IServiceCollection services)
        {
            services
                .AddMvc(mvcOptions =>
                {
                    mvcOptions.Filters.Add(new ModelStateValidationFilter());
                    mvcOptions.Filters.Add(new HandledResultFilter());
                });

            return services;
        }
    }
}
