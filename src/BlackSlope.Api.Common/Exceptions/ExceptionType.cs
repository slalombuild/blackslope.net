using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSlope.Api.Common.Exceptions
{
    public enum ExceptionType
    {
        General,
        Service,
        Validation,
        Warning,
        Authentication,
        Security,
    }
}
