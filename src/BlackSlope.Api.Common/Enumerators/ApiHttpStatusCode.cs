using System.ComponentModel;

namespace BlackSlope.Api.Common.Enumerators
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Enum names are self documenting.")]
    public enum ApiHttpStatusCode
    {
        [Description("OK.")]
        OK = 200,
        [Description("Created.")]
        Created = 201,
        [Description("Bad Request.")]
        BadRequest = 400,
        [Description("Unauthorized.")]
        Unauthorized = 401,
        [Description("Internal Server Error.")]
        InternalServerError = 500,
    }
}
