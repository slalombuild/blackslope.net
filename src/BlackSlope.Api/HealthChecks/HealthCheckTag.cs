using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BlackSlope.Api.HealthChecks
{
    [ExcludeFromCodeCoverage]
    public static class HealthCheckTag
    {
        public const string Movies = "movies";
        public const string Database = "database";
        public const string Api = "api";

        public static IEnumerable<string> All
        {
            get
            {
                return new List<string>()
                {
                    Movies,
                    Database,
                    Api,
                };
            }
        }
    }
}
