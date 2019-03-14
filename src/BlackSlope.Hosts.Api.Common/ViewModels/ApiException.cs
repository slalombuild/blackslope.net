using BlackSlope.Hosts.Api.Common.Enumerators;
using System;
using System.Collections.Generic;

namespace BlackSlope.Hosts.Api.Common.ViewModels
{
    public class ApiException : Exception
    {
        public ApiHttpStatusCode ApiHttpStatusCode { get; set; }
        public IEnumerable<ApiError> ApiErrors { get; set; }
        public new object Data { get; set; }

        public ApiException(ApiHttpStatusCode httpStatusCode, object data, IEnumerable<ApiError> apiErrors)
        {
            PrepareException(httpStatusCode, data, apiErrors);
        }
        public ApiException(ApiHttpStatusCode httpStatusCode, object data, IEnumerable<ApiError> apiErrors, string message)
        : base(message)
        {
            PrepareException(httpStatusCode, data, apiErrors);
        }

        public ApiException(ApiHttpStatusCode httpStatusCode, object data, IEnumerable<ApiError> apiErrors, string message, Exception inner)
        : base(message, inner)
        {
            PrepareException(httpStatusCode, data, apiErrors);
        }

        public ApiException(ApiHttpStatusCode httpStatusCode, object data, ApiError apiError)
        {
            PrepareException(httpStatusCode, data, new List<ApiError>() { apiError });
        }
        public ApiException(ApiHttpStatusCode httpStatusCode, object data, ApiError apiError, string message)
        : base(message)
        {
            PrepareException(httpStatusCode, data, new List<ApiError>() { apiError });
        }
        public ApiException(ApiHttpStatusCode httpStatusCode, object data, ApiError apiError, string message, Exception inner)
        : base(message, inner)
        {
            PrepareException(httpStatusCode, data, new List<ApiError>() { apiError });
        }

        private void PrepareException(ApiHttpStatusCode httpStatusCode, object data, IEnumerable<ApiError> apiErrors)
        {
            ApiHttpStatusCode = httpStatusCode;
            ApiErrors = apiErrors;
            Data = data;
        }
    }
}
