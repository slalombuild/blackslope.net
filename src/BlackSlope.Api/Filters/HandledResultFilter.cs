using System;
using System.Text;
using BlackSlope.Api.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace BlackSlope.Api.Filters
{
    public class HandledResultFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            context.ExceptionHandled = true;
            var error = new HandledResult<Exception>(context.Exception).HandleException();

            context.HttpContext.Response.Clear();
            context.HttpContext.Response.ContentType = new MediaTypeHeaderValue("application/json").ToString();
            context.HttpContext.Response.StatusCode = error.StatusCode;
            context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(error), Encoding.UTF8);
        }
    }
}
