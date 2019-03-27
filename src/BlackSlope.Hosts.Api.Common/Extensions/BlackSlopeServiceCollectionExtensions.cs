using System;
using System.Collections.Generic;
using System.IO;
using BlackSlope.Hosts.Api.Common.Configurtion;
using Microsoft.AspNetCore.Mvc;
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
        public static IServiceCollection AddSwagger(this IServiceCollection services, SwaggerConfig swaggerConfig)
        {
            return services.AddSwaggerGen(c =>
            {

                c.SwaggerDoc(swaggerConfig.Version, new Info { Title = swaggerConfig.ApplicationName, Version = swaggerConfig.Version });
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    In = "header",
                    Description = "Please insert JWT with Bearer into field",
                    Name = "Authorization",
                    Type = "apiKey"
                });

                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "Bearer", new string[] { } }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = swaggerConfig.XmlFile;
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        /// <summary>
        /// Adds MVC service to the IServiceCollection and configure json serializer behavior 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IMvcBuilder AddMvcService(this IServiceCollection services)
        {
            return services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });
        }
    }
}
