using System;
using System.Collections.Generic;
using System.Net;

namespace BlackSlope.Api.Common.Exceptions
{
    public class HandledResult<T> where T : Exception
    {
        private const string BadRequestDescriptor = "Bad Request";

        private const string ServerErrorDescriptor = "Internal Server Error";

        public T Exception { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public HandledResult(T value)
        {
            Exception = value;
            StatusCode = HttpStatusCode.BadRequest;
        }

        public HandledResult(HttpStatusCode status, T value)
        {
            Exception = value;
            StatusCode = status;
        }


        /// <summary>
        /// Handles the exception.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1115:Parameter should follow comma", Justification = "Override the formatting to make this easier to follow.")]
        public HandledResponseModel HandleException()
        {
            var response = new HandledResponseModel();

            TypeSwitch.Eval(
                Exception,

                TypeSwitch.Case<HandledException>(ex =>
                {
                    response = new HandledResponseModel()
                    {
                        StatusCode = (int)ex.StatusCode,
                        Descriptor = BadRequestDescriptor,
                        Exceptions = ToExResponse(ex),
                    };
                }),

                TypeSwitch.Case<HandledExceptionCollection>((ex) =>
                {
                    response = new HandledResponseModel()
                    {
                        StatusCode = (int)ex.StatusCode,
                        Descriptor = BadRequestDescriptor,
                        Exceptions = ToExResponse(ex),
                    };
                }),

                TypeSwitch.Case<Exception>((ex) =>
                {
                    response = new HandledResponseModel()
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError,
                        Descriptor = BadRequestDescriptor,
                        Exceptions = ToExResponse(ex),
                    };
                }),

                TypeSwitch.Default(() => { })

            );

            return response;
        }

        /// <summary>
        /// To the ex response.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns></returns>
        private List<HandledResponseError> ToExResponse(HandledException ex)
        {
            return ToExResponse(new List<HandledException>() { ex });
        }

        /// <summary>
        /// To the ex response.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns></returns>
        private List<HandledResponseError> ToExResponse(Exception ex)
        {
            return ToExResponse(new List<HandledException>() { new HandledException(ExceptionType.General, ex.Message) });
        }

        /// <summary>
        /// To the ex response.
        /// </summary>
        /// <param name="exs">The exs.</param>
        /// <returns></returns>
        private List<HandledResponseError> ToExResponse(List<HandledException> exs)
        {
            var collection = new List<HandledResponseError>();
            exs.ForEach(ex =>
            {
                collection.Add(new HandledResponseError()
                {
                    Code = ex.ErrorCode,
                    Type = ex.ExceptionType.ToString(),
                    Message = ex.Message
                });
            });
            return collection;
        }

    }
}
