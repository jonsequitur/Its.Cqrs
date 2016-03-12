// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Web;
using System.Web.Http.Routing;
using System.Web.Routing;

namespace Microsoft.Its.Domain.Api
{
    [DebuggerStepThrough]
    public class GuidConstraint : IRouteConstraint, IHttpRouteConstraint
    {
        public bool Match(
            HttpRequestMessage request,
            IHttpRoute route,
            string parameterName,
            IDictionary<string, object> values,
            HttpRouteDirection routeDirection) =>
                Match(parameterName, values);

        public bool Match(
            HttpContextBase httpContext,
            Route route,
            string parameterName,
            RouteValueDictionary values,
            RouteDirection routeDirection) =>
                Match(parameterName, values);

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private static bool Match(string parameterName, IDictionary<string, object> values)
        {
            object result;
            if (values.TryGetValue(parameterName, out result))
            {
                Guid guid;
                if (!(result is string) || Guid.TryParse((string) result, out guid))
                {
                    return true;
                }
            }

            return false;
        }
    }
}