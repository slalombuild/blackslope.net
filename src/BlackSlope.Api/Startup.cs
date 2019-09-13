using System.IO.Abstractions;
using System.Reflection;
using BlackSlope.Api.Common.Configurtion;
using BlackSlope.Api.Common.Extensions;
using BlackSlope.Api.Common.Middleware.Correlation;
using BlackSlope.Api.Common.Middleware.ExceptionHandling;
using BlackSlope.Api.Common.Version.Interfaces;
using BlackSlope.Api.Common.Version.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlackSlope.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private HostConfig HostConfig { get; set; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcService();
            ApplicationConfiguration(services);
            CorsConfiguration(services);

            services.AddSwagger(HostConfig.Swagger);
            services.AddAzureAd(HostConfig.AzureAd);
            services.AddAutoMapper(GetAssemblyNamesToScanForMapperProfiles());
            services.AddCorrelation();
            services.AddTransient<IFileSystem, FileSystem>();
            services.AddTransient<IVersionService, AssemblyVersionService>();
            services.AddHealthChecksService();

            services.AddMovieService();
            services.AddMovieRepository(_configuration);
            services.AddMovieValidators();
        }


        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
            app.UseCors("AllowSpecificOrigin");
            app.UseAuthentication();
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMvc();
        }

        private void ApplicationConfiguration(IServiceCollection services)
        {
            services.AddSingleton(_ => _configuration);
            services.AddSingleton(_configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name).Get<HostConfig>());

            var serviceProvider = services.BuildServiceProvider();
            HostConfig = serviceProvider.GetService<HostConfig>();
        }

        private void CorsConfiguration(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder => builder.AllowAnyOrigin()     // TODO: Replace with FE Service Host as appropriate to constrain clients
                        .AllowAnyHeader()
                        .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE"));
            });
        }

        // make a list of projects in the solution which must be scanned for mapper profiles
        private static string[] GetAssemblyNamesToScanForMapperProfiles() =>
            new string[] { Assembly.GetExecutingAssembly().GetName().Name };
    }
}
