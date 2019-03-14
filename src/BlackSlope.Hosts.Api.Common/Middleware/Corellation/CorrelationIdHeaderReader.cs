using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace BlackSlope.Hosts.Api.Common.Middleware.Corellation
{
    public class CorrelationIdHeaderService : ICorrelationIdRequestReader, ICorrelationIdResponseWriter
    {
        private const string CorrelationIdHeaderKey = "CorrelationId";

        public Guid? Read(HttpContext context)
        {
            StringValues correlationId = context.Request.Headers[CorrelationIdHeaderKey];
            if (Guid.TryParse(correlationId.ToString(), out Guid result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public void Write(HttpContext context, Guid correlationId)
        {
            context.Response.Headers[CorrelationIdHeaderKey] = correlationId.ToString();
        }
    }
}
