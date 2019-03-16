using System.Reflection;
using BlackSlope.Repositories.MovieRepository;
using BlackSlope.Repositories.MovieRepository.Configuration;
using BlackSlope.Repositories.MovieRepository.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MovieRepositoryRegistration
    {
        public static IServiceCollection AddMovieRepository(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.TryAddScoped<IMovieRepository, MovieRepository>();

            var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
                .Get<MovieRepositoryConfiguration>();
            services.TryAddSingleton<IMovieRepositoryConfiguration>(config);

            var serviceProvider = services.BuildServiceProvider();
            var movieContext = serviceProvider.GetService<MovieContext>();
            if (movieContext == null)
            {
                services.AddDbContext<MovieContext>(options => options.UseSqlServer(config.MoviesConnectionString));
            }
            return services;
        }
    }
}
