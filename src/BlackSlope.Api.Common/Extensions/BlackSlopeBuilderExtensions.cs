using BlackSlope.Api.Common.Configuration;
using Microsoft.AspNetCore.Builder;

namespace BlackSlope.Api.Common.Extensions
{
    public static class BlackSlopeBuilderExtensions
    {
        public static IApplicationBuilder UseSwagger(this IApplicationBuilder app, SwaggerConfig swaggerConfig)
        {
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);
            });
            return app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/" + swaggerConfig.Version + "/swagger.json", swaggerConfig.ApplicationName);
            });
        }
    }
}
