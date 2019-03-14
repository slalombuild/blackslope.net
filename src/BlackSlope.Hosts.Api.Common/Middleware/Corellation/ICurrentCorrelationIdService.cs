using System;

namespace BlackSlope.Hosts.Api.Common.Middleware.Corellation
{
    public interface ICurrentCorrelationIdService
    {
        CorrelationId Current();
        void Set(Guid correlationId);
    }
}
