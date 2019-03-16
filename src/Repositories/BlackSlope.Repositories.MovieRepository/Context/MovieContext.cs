using Microsoft.EntityFrameworkCore;
using BlackSlope.Repositories.MovieRepository.DtoModels;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;
using BlackSlope.Repositories.MovieRepository.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;

namespace BlackSlope.Repositories.MovieRepository.Context
{
    public class MovieContext : DbContext
    {
        public virtual DbSet<MovieDtoModel> Movies { get; set; }

        public MovieContext(DbContextOptions<MovieContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MovieDtoModel>(entity =>
            {
                entity.HasIndex(e => e.Title)
                    .HasName("IX_Movies_Title");
            });
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MovieContext>
    {
        public MovieContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
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