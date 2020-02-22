using System.Diagnostics.Contracts;
using System.Reflection;
using BlackSlope.Repositories.Movies;
using BlackSlope.Repositories.Movies.Configuration;
using BlackSlope.Repositories.Movies.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MovieRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddMovieRepository(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            Contract.Requires(configuration != null);

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
