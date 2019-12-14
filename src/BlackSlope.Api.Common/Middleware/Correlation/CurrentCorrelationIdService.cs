using System;

namespace BlackSlope.Api.Common.Middleware.Correlation
{
    public class CurrentCorrelationIdService : ICurrentCorrelationIdService
    {
        private Guid? _correlationId;

        public CorrelationId Current()
        {
            if (_correlationId.HasValue)
            {
                return new CorrelationId(_correlationId.Value);
            }

            throw new InvalidOperationException(FormattableString.Invariant($"CorrelationId has not been set"));
        }

        public void SetId(Guid correlationId)
        {
            _correlationId = correlationId;
        }
    }
}
