using System;

namespace BlackSlope.Api.Common.Middleware.Correlation
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
