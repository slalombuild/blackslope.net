using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSlope.Api.Common.Configurtion
{
    public class AzureAdConfig
    {
        public string AadInstance { get; set; }

        public string Tenant { get; set; }

        public string Audience { get; set; }
    }
}
