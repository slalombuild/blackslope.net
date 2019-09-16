using System.IO;
using System.Reflection;
using BlackSlope.Api.Repositories.Movies.Context.Extensions;
using BlackSlope.Repositories.Movies.Configuration;
using BlackSlope.Repositories.Movies.DtoModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BlackSlope.Repositories.Movies.Context
{
    public class MovieContext : DbContext
    {
        private readonly MovieRepositoryConfiguration _config;

        public MovieContext(DbContextOptions<MovieContext> options)
            : base(options)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            _config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
                .Get<MovieRepositoryConfiguration>();
        }

        public virtual DbSet<MovieDtoModel> Movies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MovieDtoModel>(entity =>
            {
                entity.HasIndex(e => e.Title)
                    .HasName("IX_Movies_Title");
            });

            modelBuilder.Seed();
        }
    }
}
