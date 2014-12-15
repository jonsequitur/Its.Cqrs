// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Api.Tests.Infrastructure
{
    public class TestApi<T>
        where T : class, IEventSourced
    {
        private static HttpServer server;
        public readonly HttpConfiguration HttpConfiguration;
        internal readonly PocketContainer Container = new PocketContainer();

        public TestApi()
        {
            HttpConfiguration = new HttpConfiguration
            {
                IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always
            }.ResolveDependenciesUsing(Container);

            HttpConfiguration.MapRoutesFor<T>();

            Container.Register<IEventSourcedRepository<T>>(c => new SqlEventSourcedRepository<T>());
        }

        public HttpClient GetClient()
        {
            server = new HttpServer(HttpConfiguration);
            var httpClient = new HttpClient(server);
            return httpClient;
        }
    }
}