using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSlope.Api.Common.Exceptions
{
    public class HandledResponseError
    {
        public string Code { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }
}
