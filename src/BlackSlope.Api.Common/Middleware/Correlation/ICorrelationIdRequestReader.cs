using System;
using Microsoft.AspNetCore.Http;

namespace BlackSlope.Api.Common.Middleware.Correlation
{
    public interface ICorrelationIdRequestReader
    {
        Guid? Read(HttpContext context);
    }
}
