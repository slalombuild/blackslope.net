using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSlope.Hosts.Api.Common.Configurtion
{
    public class SerilogConfig
    {
        public int MinimumLevel { get; set; }
        public string FileName { get; set; }
        public bool WriteToConsole { get; set; }
        public bool WriteToFile { get; set; }
        public bool WriteToAppInsights { get; set; }
    }
}
