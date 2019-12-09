using System.Collections.Generic;
using BlackSlope.Api.Common.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;

namespace BlackSlope.Api.Common.Extensions
{
    public static class BlackSlopeBuilderExtensions
    {
        public static IApplicationBuilder UseSwagger(this IApplicationBuilder app, SwaggerConfig swaggerConfig)
        {
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" } });
            });

            return app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/" + swaggerConfig.Version + "/swagger.json", swaggerConfig.ApplicationName);
            });
        }
    }
}
