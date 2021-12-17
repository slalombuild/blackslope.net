using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using AutoMapper;
using BlackSlope.Api.Common.Configuration;
using BlackSlope.Api.Common.Swagger;
using BlackSlope.Api.Common.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        public static IServiceCollection AddSwagger(this IServiceCollection services, SwaggerConfig swaggerConfig) =>
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(swaggerConfig.Version, new OpenApiInfo { Title = swaggerConfig.ApplicationName, Version = swaggerConfig.Version });
                options.DocumentFilter<DocumentFilterAddHealth>();
                AddSecurityDefinition(options);
                AddSecurityRequirement(options);
                SetDocumentPath(swaggerConfig, options);
            });

        /// <summary>
        /// Adds MVC service to the Service Collection and configure json serializer behavior
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IMvcBuilder AddMvcService(this IServiceCollection services) =>
            services.AddMvc()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.Converters
                    .Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters
                    .Add(new VersionJsonConverter());
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
        /// <param name="assemblyProfilesToScan"></param>
        /// <returns></returns>
        public static IServiceCollection AddAutoMapper(this IServiceCollection services, IEnumerable<Assembly> assemblyProfilesToScan)
        {
            services.TryAddSingleton(GenerateMapperConfiguration(assemblyProfilesToScan));
            return services;
        }

        private static IMapper GenerateMapperConfiguration(IEnumerable<Assembly> assemblyProfilesToScan)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(assemblyProfilesToScan);
            });

            return config.CreateMapper();
        }

        private static void SetDocumentPath(SwaggerConfig swaggerConfig, SwaggerGenOptions options)
        {
            // Set the comments path for the Swagger JSON and UI.
            var xmlFile = swaggerConfig.XmlFile;
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
        }

        private static void AddSecurityDefinition(SwaggerGenOptions options) =>
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Description = "Please insert JWT with Bearer into field",
            });

        private static void AddSecurityRequirement(SwaggerGenOptions options) =>
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
                    },
                    new[] { "readAccess", "writeAccess" }
                },
            });
    }
}
