using System;

namespace BlackSlope.Api.Common.Middleware.Correlation
{
    public interface ICurrentCorrelationIdService
    {
        CorrelationId Current();
        void Set(Guid correlationId);
    }
}
