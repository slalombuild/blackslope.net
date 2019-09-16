using System.IO;
using System.Reflection;
using BlackSlope.Repositories.Movies.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BlackSlope.Repositories.Movies.Context
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MovieContext>
    {
        public MovieContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();

            var builder = new DbContextOptionsBuilder<MovieContext>();
            var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
                            .Get<MovieRepositoryConfiguration>();
            builder.UseSqlServer(config.MoviesConnectionString);

            return new MovieContext(builder.Options);
        }
    }
}
