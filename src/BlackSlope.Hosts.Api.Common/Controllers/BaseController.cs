using System.Collections.Generic;
using System.Net;
using BlackSlope.Hosts.Api.Common.ViewModels;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace BlackSlope.Hosts.Api.Common.Controllers
{
    [EnableCors("AllowSpecificOrigin")]
    public class BaseController : Controller
    {
        protected ActionResult HandleSuccessResponse(object data, HttpStatusCode status = HttpStatusCode.OK)
        {
            return StatusCode((int)status, new ApiResponse
            {
                Data = data
            });
        }

        protected ActionResult HandleCreatedResponse(object data, HttpStatusCode status = HttpStatusCode.Created)
        {
            return StatusCode((int)status, new ApiResponse
            {
                Data = data
            });
        }

        protected ActionResult HandleErrorResponse(HttpStatusCode httpStatus, string message)
        {
            var response = new ApiResponse();
            var errors = new List<ApiError>
            {
                new ApiError
                {
                    Code = (int)httpStatus,
                    Message = message
                }
            };
            return StatusCode((int)httpStatus, response);
        }

    }
}
