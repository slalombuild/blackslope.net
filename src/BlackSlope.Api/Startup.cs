using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reflection;
using AutoMapper;
using BlackSlope.Api.Common.Configuration;
using BlackSlope.Api.Common.Extensions;
using BlackSlope.Api.Common.Middleware.Correlation;
using BlackSlope.Api.Common.Middleware.ExceptionHandling;
using BlackSlope.Api.Common.Versioning.Interfaces;
using BlackSlope.Api.Common.Versioning.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlackSlope.Api
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private HostConfig HostConfig { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcService();
            ApplicationConfiguration(services);
            CorsConfiguration(services);

            services.AddSwagger(HostConfig.Swagger);
            services.AddAzureAd(HostConfig.AzureAd);
            services.AddAutoMapper(GetAssembliesToScanForMapperProfiles());
            services.AddCorrelation();
            services.AddTransient<IFileSystem, FileSystem>();
            services.AddTransient<IVersionService, AssemblyVersionService>();
            services.AddHealthChecksService();

            services.AddMovieService();
            services.AddMovieRepository(_configuration);

            services.AddValidators();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHealthChecks("/health");
            app.UseHttpsRedirection();

            app.UseSwagger(HostConfig.Swagger);

            app.UseRouting();
            app.UseCors("AllowSpecificOrigin");

            app.UseAuthentication();

            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static void CorsConfiguration(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowSpecificOrigin",
                    builder => builder.AllowAnyOrigin() // TODO: Replace with FE Service Host as appropriate to constrain clients
                        .AllowAnyHeader()
                        .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE"));
            });
        }

        // make a list of assemblies in the solution which must be scanned for mapper profiles
        private static IEnumerable<Assembly> GetAssembliesToScanForMapperProfiles() =>
            new Assembly[] { Assembly.GetExecutingAssembly() };

        private void ApplicationConfiguration(IServiceCollection services)
        {
            services.AddSingleton(_ => _configuration);
            services.AddSingleton(_configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name).Get<HostConfig>());

            var serviceProvider = services.BuildServiceProvider();
            HostConfig = serviceProvider.GetService<HostConfig>();
        }
    }
}
