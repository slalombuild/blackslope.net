using BlackSlope.Services.MovieService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MovieServiceRegistration
    {
        public static IServiceCollection AddMovieService(this IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddTransient<IMovieService, MovieService>();
            return services.AddMovieRepository(configuration);
        }
    }
}
