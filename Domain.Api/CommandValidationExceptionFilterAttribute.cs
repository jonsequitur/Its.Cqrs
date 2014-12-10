using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Filters;
using Microsoft.Its.Domain.Api.Serialization;

namespace Microsoft.Its.Domain.Api
{
    public class CommandValidationExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var exception = context.Exception;

            if (exception is TargetInvocationException || exception is AggregateException)
            {
                exception = exception.InnerException;
            }

            var commandValidationException = exception as CommandValidationException;
            if (commandValidationException != null)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                if (commandValidationException.ValidationReport != null)
                {
                    context.Response.Content = new JsonContent(
                        new ValidationReportModel(commandValidationException.ValidationReport));
                }
            }
        }
    }
}