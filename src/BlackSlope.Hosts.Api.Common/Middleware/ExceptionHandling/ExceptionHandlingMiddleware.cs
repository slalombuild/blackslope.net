using BlackSlope.Hosts.Api.Common.ViewModels;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BlackSlope.Hosts.Api.Common.Extensions;
using BlackSlope.Hosts.Api.Common.Enumerators;

namespace BlackSlope.Hosts.Api.Common.Middleware.ExceptionHandling
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = "";
            var statusCode = ApiHttpStatusCode.InternalServerError;

            if (exception is ApiException)
            {
                var apiException = exception as ApiException;
                statusCode = apiException.ApiHttpStatusCode;

                var apiErrors = new List<ApiError>();
                foreach (var error in apiException.ApiErrors)
                {
                    apiErrors.Add(PrepareApiError(error.Code, error.Message));
                }

                var apiResponse = PrepareResponse(apiException.Data, apiErrors);
                response = Serialize(apiResponse);
            }
            else
            {
                var apiErrors = new List<ApiError>
                {
                    PrepareApiError((int)statusCode, statusCode.Description())
                };
                var apiResponse = PrepareResponse(null, apiErrors);
                response = Serialize(apiResponse);
            }

            Log.Error(exception, response);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(response);

        }

        private static ApiResponse PrepareResponse(object data, IEnumerable<ApiError> apiErrors)
        {
            var response = new ApiResponse
            {
                Data = data,
                Errors = apiErrors
            };

            return response;
        }

        private static ApiError PrepareApiError(int code, string message)
        {
            return new ApiError
            {
                Code = code,
                Message = message
            };
        }

        private static string Serialize(ApiResponse apiResponse)
        {
            var result = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            return result;
        }
    }
}
