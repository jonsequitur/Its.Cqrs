// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Microsoft.Its.Domain.Api.Serialization;

namespace Microsoft.Its.Domain.Api
{
    /// <summary>
    /// Converts unhandled <see cref="CommandAuthorizationException" />s into 403 Forbidden responses with a description of the validation failure in the response body.
    /// </summary>
    public class CommandAuthorizationExceptionFilterAttribute : ExceptionFilterAttribute 
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is CommandAuthorizationException)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
#if DEBUG
                    Content = new JsonContent(context.Exception)
#endif
                };
            }
        }
    }
}