using System;
using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace BlackSlope.Api.Common.Middleware.Correlation
{
    public class CorrelationIdHeaderService : ICorrelationIdRequestReader, ICorrelationIdResponseWriter
    {
        private const string CorrelationIdHeaderKey = "CorrelationId";

        public Guid? Read(HttpContext context)
        {
            Contract.Requires(context != null);
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
            Contract.Requires(context != null);
            context.Response.Headers[CorrelationIdHeaderKey] = correlationId.ToString();
        }
    }
}
