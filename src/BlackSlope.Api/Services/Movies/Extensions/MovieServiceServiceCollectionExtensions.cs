using BlackSlope.Services.Movies;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MovieServiceServiceCollectionExtensions
    {
        public static IServiceCollection AddMovieService(this IServiceCollection services)
        {
            services.TryAddTransient<IMovieService, MovieService>();
            return services;
        }
    }
}
