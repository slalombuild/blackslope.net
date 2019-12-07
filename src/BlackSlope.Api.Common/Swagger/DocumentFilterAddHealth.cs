using System.Collections.Generic;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BlackSlope.Api.Common.Swagger
{
    public class DocumentFilterAddHealth : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context) =>
            swaggerDoc.Paths.Add("/health", HealthPathItem());

        private PathItem HealthPathItem()
        {
            var pathItem = new PathItem();
            pathItem.Get = new Operation
            {
                Tags = new[] { "Health" },
                OperationId = "Health_Get",
                Consumes = null,
                Produces = new[] { "application/json", "text/json" },
            };
            pathItem.Get.Responses = new Dictionary<string, Response>();
            pathItem.Get.Responses.Add("200", new Response
            {
                Description = "OK",
                Schema = new Schema
                {
                    Type = "string",
                },
            });
            return pathItem;
        }
    }
}
