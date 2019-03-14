using System;
using System.Collections.Generic;
using System.IO;
using BlackSlope.Hosts.Api.Common.Configurtion;
using Swashbuckle.AspNetCore.Swagger;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BlackSlopeServiceCollectionExtensions
    {
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
    }
}
