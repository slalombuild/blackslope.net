using System;
using System.Collections.Generic;
using BlackSlope.Api.Common.Enumerators;

namespace BlackSlope.Api.Common.ViewModels
{
    public class ApiException : Exception
    {
        public ApiException()
            : base()
        {
        }

        public ApiException(string message)
            : base(message)
        {
        }

        public ApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class.
        /// </summary>
        /// <param name="httpStatusCode"></param>
        /// <param name="data"></param>
        /// <param name="apiErrors"></param>
        public ApiException(ApiHttpStatusCode httpStatusCode, object data, IEnumerable<ApiError> apiErrors)
        {
            PrepareException(httpStatusCode, data, apiErrors);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class.
        /// </summary>
        /// <param name="httpStatusCode"></param>
        /// <param name="data"></param>
        /// <param name="apiErrors"></param>
        /// <param name="message"></param>
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

        public ApiHttpStatusCode ApiHttpStatusCode { get; set; }

        public IEnumerable<ApiError> ApiErrors { get; set; }

        public new object Data { get; set; }

        private void PrepareException(ApiHttpStatusCode httpStatusCode, object data, IEnumerable<ApiError> apiErrors)
        {
            ApiHttpStatusCode = httpStatusCode;
            ApiErrors = apiErrors;
            Data = data;
        }
    }
}
