using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Microsoft.Its.Domain.Api.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.Api
{
    public class SerializationExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var exception = context.Exception as JsonSerializationException;
            if (exception != null)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(exception.Message)
                };
            }
        }
    }
}