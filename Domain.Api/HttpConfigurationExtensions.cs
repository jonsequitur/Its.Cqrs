// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Api
{
    /// <summary>
    ///     Provides functionality for configuring Web Api.
    /// </summary>
    public static class HttpConfigurationExtensions
    {
        /// <summary>
        /// Maps the domain API routes for the specified aggregate type.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="config">The configuration.</param>
        /// <param name="baseUrl">The base URL for commands for the specified aggregate type, if different from the convention.</param>
        /// <param name="handler">An optional delegating handler to provide authentication or other functionality over the API.</param>
        /// <returns>
        /// The updated HttpConfiguration instance.
        /// </returns>
        public static HttpConfiguration MapRoutesFor<TAggregate>(
            this HttpConfiguration config, 
            string baseUrl = null,
            HttpMessageHandler handler = null)
        {
            var apiController = typeof (TAggregate).Name + "Api";

            baseUrl = string.IsNullOrWhiteSpace(baseUrl)
                          ? typeof (TAggregate).Name.ToLower() + "s"
                          : baseUrl;

            var messageHandler = handler ?? new HttpControllerDispatcher(config);

#if DEBUG
            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Get-Aggregate",
                routeTemplate: baseUrl + "/{id}",
                defaults: new
                {
                    controller = apiController,
                    action = "GetAggregate",
                },
                constraints: new
                {
                    id = new GuidConstraint(),
                    httpMethod = new HostIndependentHttpMethodConstraint("GET")
                }, 
                handler: messageHandler);
#endif

            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Apply-Command",
                routeTemplate: baseUrl + "/{id}/{commandName}",
                defaults: new
                {
                    controller = apiController,
                    action = "apply",
                },
                constraints: new
                {
                    id = new GuidConstraint(),
                    httpMethod = new HostIndependentHttpMethodConstraint("POST")
                }, 
                handler: messageHandler);
            
            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Create-Command",
                routeTemplate: baseUrl + "/{commandName}/{id}",
                defaults: new
                {
                    controller = apiController,
                    action = "create",
                },
                constraints: new
                {
                    id = new GuidConstraint(),
                    httpMethod = new HostIndependentHttpMethodConstraint("POST")
                }, 
                handler: messageHandler);

            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Apply-Command-Batch",
                routeTemplate: baseUrl + "/{id}",
                defaults: new
                {
                    controller = apiController,
                    action = "applybatch",
                },
                constraints: new
                {
                    id = new GuidConstraint(),
                    httpMethod = new HostIndependentHttpMethodConstraint("POST")
                }, 
                handler: messageHandler);

            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Validate-Command",
                routeTemplate: baseUrl + "/{id}/{commandName}/validate",
                defaults: new
                {
                    controller = apiController,
                    action = "validate"
                },
                constraints: new
                {
                    id = new GuidConstraint()
                }, 
                handler: messageHandler);

            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Command-Validation-Rules",
                routeTemplate: baseUrl + "/commands/{commandName}/rules",
                defaults: new
                {
                    controller = apiController,
                    action = "CommandValidationRules"
                }, 
                constraints: null,
                handler: messageHandler);

            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Help-Commands",
                routeTemplate: baseUrl + "/help/commands/{name}",
                defaults: new
                {
                    controller = apiController,
                    action = "CommandDocumentation",
                    name = ""
                }, 
                constraints: null,
                handler: messageHandler);

            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Help",
                routeTemplate: baseUrl + "/help",
                defaults: new
                {
                    controller = "help"
                }, 
                constraints: null,
                handler: messageHandler);

            return config;
        }

        public static HttpConfiguration MapRouteFor<TAggregate, TCommand>(
            this HttpConfiguration config,
            string baseUrl = null)
        {
            var apiController = typeof (TAggregate).Name + "Api";
            var commandName = typeof (TCommand).Name;

            baseUrl = string.IsNullOrWhiteSpace(baseUrl)
                          ? typeof (TAggregate).Name.ToLower() + "s"
                          : baseUrl;

            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Apply-Command",
                routeTemplate: baseUrl + "/{id}/" + commandName,
                defaults: new
                {
                    controller = apiController,
                    action = "apply"
                },
                constraints: new
                {
                    id = new GuidConstraint(),
                    httpMethod = new HostIndependentHttpMethodConstraint("POST")
                });

            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Validate-Command",
                routeTemplate: baseUrl + "/{id}/" + commandName + "/validate",
                defaults: new
                {
                    controller = apiController,
                    action = "validate"
                },
                constraints: new
                {
                    id = new GuidConstraint()
                })
                ;

            config.Routes.MapHttpRoute(
                name: baseUrl + "-Api-Command-Validation-Rules",
                routeTemplate: baseUrl + "/commands/" + commandName + "/rules",
                defaults: new
                {
                    controller = apiController,
                    action = "CommandValidationRules"
                });

            return config;
        }

        internal static HttpConfiguration MapDiagnostics(this HttpConfiguration configuration,
                                                       string baseUrl = "")
        {
            var routeTemplate = baseUrl.IfNotNullOrEmptyOrWhitespace()
                                       .Else(() => "api/events/related")
                                       .AppendSegment("{aggregateid}");

            configuration
                .Routes
                .MapHttpRoute("ItsConfigurationRelatedEvents",
                              routeTemplate,
                              defaults: new
                              {
                                  controller = "Diagnostics",
                                  action = "RelatedEvents"
                              },
                              constraints: new GuidConstraint());

            return configuration;
        }

        public static IEnumerable<IDisposable> RunningEventHandlers(this HttpConfiguration configuration)
        {
            return configuration.RunningEventHandlersByType().Values;
        }

        private static ConcurrentDictionary<Type, IDisposable> RunningEventHandlersByType(this HttpConfiguration configuration)
        {
            return (ConcurrentDictionary<Type, IDisposable>) configuration.Properties.GetOrAdd(typeof (HttpConfiguration), _ => new ConcurrentDictionary<Type, IDisposable>());
        }
    }
}
