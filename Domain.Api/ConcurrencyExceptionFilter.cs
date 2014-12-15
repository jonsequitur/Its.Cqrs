// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace Microsoft.Its.Domain.Api
{
    public class ConcurrencyExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var exception = context.Exception as ConcurrencyException;
            if (exception != null)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.Conflict);
            }
        }
    }
}