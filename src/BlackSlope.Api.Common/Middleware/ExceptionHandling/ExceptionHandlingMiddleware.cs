using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BlackSlope.Api.Common.Enumerators;
using BlackSlope.Api.Common.Extensions;
using BlackSlope.Api.Common.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BlackSlope.Api.Common.Middleware.ExceptionHandling
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static ApiResponse PrepareResponse(object data, IEnumerable<ApiError> apiErrors)
        {
            var response = new ApiResponse
            {
                Data = data,
                Errors = apiErrors,
            };

            return response;
        }

        private static ApiError PrepareApiError(int code, string message)
        {
            return new ApiError
            {
                Code = code,
                Message = message,
            };
        }

        private static string Serialize(ApiResponse apiResponse)
        {
            var result = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            });

            return result;
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = ApiHttpStatusCode.InternalServerError;
            string response;
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
                    PrepareApiError((int)statusCode, statusCode.GetDescription()),
                };
                var apiResponse = PrepareResponse(null, apiErrors);
                response = Serialize(apiResponse);
            }

            _logger.LogError(exception, response);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            return context.Response.WriteAsync(response);
        }
    }
}
