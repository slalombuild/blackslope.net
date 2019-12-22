using BlackSlope.Api.Operations.Movies.Validators;
using BlackSlope.Api.Operations.Movies.Validators.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MovieValidatorsServiceCollectionExtensions
    {
        public static IServiceCollection AddMovieValidators(this IServiceCollection services)
        {
            services.TryAddTransient<IUpdateMovieRequestValidator, UpdateMovieRequestValidatorCollection>();
            services.TryAddTransient<ICreateMovieRequestValidator, CreateMovieRequestValidatorCollection>();
            return services;
        }
    }
}
