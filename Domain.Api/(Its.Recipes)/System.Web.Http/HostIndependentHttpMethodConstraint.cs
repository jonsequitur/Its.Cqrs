// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using System.Web.Http.Routing;
using System.Web.Routing;
using HttpMethodConstraint = System.Web.Routing.HttpMethodConstraint;

namespace Microsoft.Its.Domain.Api
{
    /// <summary>
    /// Provides a fix for the problem that different constraint interfaces are used depending on the hosting environment.
    /// </summary>
#if !RecipesProject
    [global::System.Diagnostics.DebuggerStepThrough]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal class HostIndependentHttpMethodConstraint : IRouteConstraint, IHttpRouteConstraint
    {
        private readonly HttpMethodConstraint aspNetConstraint;
        private readonly global::System.Web.Http.Routing.HttpMethodConstraint selfHostConstraint;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostIndependentHttpMethodConstraint"/> class.
        /// </summary>
        /// <param name="method">The method.</param>
        public HostIndependentHttpMethodConstraint(string method)
        {
            aspNetConstraint = new HttpMethodConstraint(method);
            selfHostConstraint = new global::System.Web.Http.Routing.HttpMethodConstraint(new HttpMethod(method));
        }

        /// <summary>
        /// Determines whether the URL parameter contains a valid value for this constraint.
        /// </summary>
        /// <returns>
        /// true if the URL parameter contains a valid value; otherwise, false.
        /// </returns>
        /// <param name="httpContext">An object that encapsulates information about the HTTP request.</param><param name="route">The object that this constraint belongs to.</param><param name="parameterName">The name of the parameter that is being checked.</param><param name="values">An object that contains the parameters for the URL.</param><param name="routeDirection">An object that indicates whether the constraint check is being performed when an incoming request is being handled or when a URL is being generated.</param>
        public bool Match(
            HttpContextBase httpContext,
            Route route,
            string parameterName,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            return ((IRouteConstraint) aspNetConstraint).Match(httpContext, route, parameterName, values, routeDirection);
        }

        /// <summary>
        /// Determines whether this instance equals a specified route.
        /// </summary>
        /// <returns>
        /// True if this instance equals a specified route; otherwise, false.
        /// </returns>
        /// <param name="request">The request.</param><param name="route">The route to compare.</param><param name="parameterName">The name of the parameter.</param><param name="values">A list of parameter values.</param><param name="routeDirection">The route direction.</param>
        public bool Match(
            HttpRequestMessage request,
            IHttpRoute route,
            string parameterName,
            IDictionary<string, object> values,
            HttpRouteDirection routeDirection)
        {
            return ((IHttpRouteConstraint) selfHostConstraint).Match(request, route, parameterName, values, routeDirection);
        }
    }
}
