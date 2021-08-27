using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BlackSlope.Api.Common.Exceptions
{
    public class HandledExceptionCollection : Exception
    {
        public HttpStatusCode StatusCode { get; set; }

        public List<HandledException> InnerExceptions { get; set; }

        public override string Message => GetMessage();

        /// <summary>
        /// Constructs a message from the InnerException list.
        /// </summary>
        /// <returns></returns>
        private string GetMessage()
        {
            var data = new StringBuilder();
            InnerExceptions.ForEach(ex =>
            {
                if (ex != null && !String.IsNullOrWhiteSpace(ex.Message))
                {
                    data.AppendLine(ex.Message);
                }
            });
            return data.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandledExceptionCollection"/> class.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        public HandledExceptionCollection(HttpStatusCode statusCode = HttpStatusCode.BadRequest) : base("")
        {
            StatusCode = statusCode;
        }
    }
}
