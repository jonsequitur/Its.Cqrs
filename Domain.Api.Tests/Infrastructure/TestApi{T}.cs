// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace Microsoft.Its.Domain.Api.Tests.Infrastructure
{
    public class TestApi<T>
        where T : class, IEventSourced
    {
        private static HttpServer server;
        public readonly HttpConfiguration HttpConfiguration;

        public TestApi()
        {
            HttpConfiguration = new HttpConfiguration
            {
                IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always
            };

            HttpConfiguration.MapRoutesFor<T>();
        }

        public HttpClient GetClient()
        {
            server = new HttpServer(HttpConfiguration);
            var httpClient = new HttpClient(server);
            return httpClient;
        }
    }
}