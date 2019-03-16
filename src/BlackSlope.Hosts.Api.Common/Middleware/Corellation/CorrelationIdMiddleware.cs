using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BlackSlope.Hosts.Api.Common.Middleware.Corellation
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
            Guid correlationId = _correlationIdRequestReader.Read(context) ?? GenerateCorrelationId();
            currentCorrelationIdService.Set(correlationId);

            context.Response.OnStarting(() =>
            {
                _correlationIdResponseWriter.Write(context, correlationId);
                return Task.CompletedTask;
            });

            await _next(context);
        }

        private Guid GenerateCorrelationId()
        {
            return Guid.NewGuid();
        }
    }
}
