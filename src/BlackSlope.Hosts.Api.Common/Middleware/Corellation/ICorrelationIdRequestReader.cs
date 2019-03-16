using System;
using Microsoft.AspNetCore.Http;

namespace BlackSlope.Hosts.Api.Common.Middleware.Corellation
{
    public interface ICorrelationIdRequestReader
    {
        Guid? Read(HttpContext context);
    }
}
