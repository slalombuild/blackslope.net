using BlackSlope.Hosts.Api.Operations.Movies.Validators.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlackSlope.Hosts.Api.Operations.Movies.Validators.Extensions
{
    public static class MovieValidatorsExtensions
    {
        public static IServiceCollection AddMovieValidators(this IServiceCollection services)
        {
            services.TryAddTransient<IUpdateMovieRequestValidator, UpdateMovieRequestValidator>();
            services.TryAddTransient<ICreateMovieRequestValidator, CreateMovieRequestValidator>();
            return services;
        }
    }
}
