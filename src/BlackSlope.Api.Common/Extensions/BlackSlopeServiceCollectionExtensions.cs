using System;
using System.Collections.Generic;
using System.IO;
using AutoMapper;
using BlackSlope.Api.Common.Configuration;
using BlackSlope.Api.Common.Swagger;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.Swagger;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BlackSlopeServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Swagger service to the IServiceCollection and configure it
        /// </summary>
        /// <param name="services"></param>
        /// <param name="swaggerConfig"></param>
        /// <returns></returns>
        public static IServiceCollection AddSwagger(this IServiceCollection services, SwaggerConfig swaggerConfig) => services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(swaggerConfig.Version, new Info { Title = swaggerConfig.ApplicationName, Version = swaggerConfig.Version });
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    In = "header",
                    Description = "Please insert JWT with Bearer into field",
                    Name = "Authorization",
                    Type = "apiKey",
                });
                c.DocumentFilter<DocumentFilterAddHealth>();
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "Bearer", Array.Empty<string>() },
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = swaggerConfig.XmlFile;
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

        /// <summary>
        /// Adds MVC service to the Service Collection and configure json serializer behavior
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IMvcBuilder AddMvcService(this IServiceCollection services) =>
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

        /// <summary>
        /// Add Azure service to the Service Collection and configure it
        /// </summary>
        /// <param name="services"></param>
        /// <param name="azureAdConfig"></param>
        /// <returns></returns>
        public static AuthenticationBuilder AddAzureAd(this IServiceCollection services, AzureAdConfig azureAdConfig) =>
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = string.Format(System.Globalization.CultureInfo.InvariantCulture, azureAdConfig.AadInstance, azureAdConfig.Tenant);
                options.Audience = azureAdConfig.Audience;
            });

        /// <summary>
        /// Add AutoMapper service to the Service Collection and configure it assemblies
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblyNamesToScan"></param>
        /// <returns></returns>
        public static IServiceCollection AddAutoMapper(this IServiceCollection services, IEnumerable<string> assemblyNamesToScan)
        {
            services.TryAddSingleton(GenerateMapperConfiguration(assemblyNamesToScan));
            return services;
        }

        private static IMapper GenerateMapperConfiguration(IEnumerable<string> assemblyNamesToScan)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfiles(assemblyNamesToScan);
            });
            return config.CreateMapper();
        }
    }
}
