using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BlackSlope.Api.Common.Middleware.Correlation
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ICorrelationIdRequestReader _correlationIdRequestReader;
        private readonly ICorrelationIdResponseWriter _correlationIdResponseWriter;

        public CorrelationIdMiddleware(RequestDelegate next, ICorrelationIdRequestReader correlationIdRequestReader, ICorrelationIdResponseWriter correlationIdResponseWriter)
        {
            _next = next;
            _correlationIdRequestReader = correlationIdRequestReader;
            _correlationIdResponseWriter = correlationIdResponseWriter;
        }

        public async Task Invoke(HttpContext context, ICurrentCorrelationIdService currentCorrelationIdService)
        {
            Contract.Requires(currentCorrelationIdService != null);
            var correlationId = _correlationIdRequestReader.Read(context) ?? GenerateCorrelationId();
            currentCorrelationIdService.SetId(correlationId);

            Contract.Requires(context != null);
            context.Response.OnStarting(() =>
            {
                _correlationIdResponseWriter.Write(context, correlationId);
                return Task.CompletedTask;
            });

            await _next(context);
        }

        private static Guid GenerateCorrelationId() => Guid.NewGuid();
    }
}
