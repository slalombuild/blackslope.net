using System.ComponentModel;

namespace BlackSlope.Hosts.Api.Common.Enumerators
{
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
        InternalServerError = 500
    }
}
