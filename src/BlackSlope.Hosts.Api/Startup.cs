using System.IO.Abstractions;
using AutoMapper;
using BlackSlope.Hosts.Api.Common.Configurtion;
using BlackSlope.Hosts.Api.Common.Extensions;
using BlackSlope.Hosts.Api.Common.Middleware.Corellation;
using BlackSlope.Hosts.Api.Common.Middleware.ExceptionHandling;
using BlackSlope.Hosts.Api.Common.Version.Interfaces;
using BlackSlope.Hosts.Api.Common.Version.Services;
using BlackSlope.Hosts.Api.Operations.Movies.Validators.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlackSlope.Hosts.Api
{
    public class Startup
    {

        private IConfiguration _configuration { get; }
        private HostConfig HostConfig { get; set; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            ApplicationConfiguration(services);

            services.AddSwagger(HostConfig.Swagger);
            CorsConfiguration(services);
            AuthenticationConfiguration(services);
                        
            services.AddSingleton<IMapper>(GenerateMapperConfiguration());
            services.AddTransient<ICorrelationIdRequestReader, CorrelationIdHeaderService>();
            services.AddTransient<ICorrelationIdResponseWriter, CorrelationIdHeaderService>();
            services.AddScoped<ICurrentCorrelationIdService, CurrentCorrelationIdService>();
            services.AddTransient<IFileSystem, FileSystem>();
            services.AddTransient<IVersionService, AssemblyVersionService>();

            services.AddMovieService(_configuration);
            services.AddMovieValidators();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware(typeof(CorrelationIdMiddleware));
            app.UseSwagger(HostConfig.Swagger);
            app.UseCors("AllowSpecificOrigin");
            app.UseAuthentication();
            app.UseMiddleware(typeof(ExceptionHandlingMiddleware));
            app.UseMvc();
        }

        private void ApplicationConfiguration(IServiceCollection services)
        {
            services.AddSingleton(_ => _configuration);
            services.AddSingleton(_configuration.GetSection("BlackSlope.Hosts.Api.Configuration").Get<HostConfig>());

            var serviceProvider = services.BuildServiceProvider();
            HostConfig = serviceProvider.GetService<HostConfig>();
        }

        public static IMapper GenerateMapperConfiguration()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfiles("BlackSlope.Hosts.Api");
                cfg.AddProfiles("BlackSlope.Services.MovieService");
                cfg.AddProfiles("BlackSlope.Repositories.MovieRepository");
            });
            return config.CreateMapper();
        }

        private void CorsConfiguration(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder => builder.AllowAnyOrigin()     // TODO: Replace with FE Service Host as appropriate to constrain clients
                        .AllowAnyHeader()
                        .WithHeaders(new[] { "PUT", "POST", "OPTIONS", "GET", "DELETE" }));
            });
        }

        private void AuthenticationConfiguration(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = string.Format(HostConfig.AzureAd.AadInstance, HostConfig.AzureAd.Tenant);
                options.Audience = HostConfig.AzureAd.Audience;
            });
        }
    }
}
