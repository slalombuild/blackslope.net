using System;

namespace BlackSlope.Hosts.Api.Common.Middleware.Corellation
{
    public class CorrelationId
    {
        public CorrelationId(Guid current)
        {
            Current = current;
        }

        public Guid Current { get; }
    }
}
