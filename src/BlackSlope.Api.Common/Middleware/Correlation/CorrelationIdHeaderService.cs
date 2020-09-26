using System;
using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.Http;

namespace BlackSlope.Api.Common.Middleware.Correlation
{
    public class CorrelationIdHeaderService : ICorrelationIdRequestReader, ICorrelationIdResponseWriter
    {
        private const string CorrelationIdHeaderKey = "CorrelationId";

        public Guid? Read(HttpContext context)
        {
            Contract.Requires(context != null);
            var correlationId = context.Request.Headers[CorrelationIdHeaderKey];
            if (Guid.TryParse(correlationId.ToString(), out var result))
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
