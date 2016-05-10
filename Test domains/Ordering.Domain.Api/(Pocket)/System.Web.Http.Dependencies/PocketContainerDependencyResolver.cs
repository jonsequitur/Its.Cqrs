// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// It has been imported using NuGet from the PocketContainer project (https://github.com/jonsequitur/PocketContainer). 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Web.Http.Dependencies;

namespace Pocket
{
    /// <summary>
    /// A Web API dependency resolver that uses PocketContainer to resolve dependencies.
    /// </summary>
#if !SourceProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal class PocketContainerDependencyResolver : IDependencyResolver
    {
        private readonly PocketContainer container;

        public PocketContainerDependencyResolver(PocketContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            this.container = container;
        }

        /// <summary>
        ///     Starts a resolution scope.
        /// </summary>
        /// <returns>
        ///     The dependency scope.
        /// </returns>
        public IDependencyScope BeginScope()
        {
            return this;
        }

        /// <summary>
        ///     Retrieves a service from the scope.
        /// </summary>
        /// <returns>
        ///     The retrieved service.
        /// </returns>
        /// <param name="serviceType">The service to be retrieved.</param>
        public object GetService(Type serviceType)
        {
            try
            {
                return container.Resolve(serviceType);
            }
            catch (Exception exception)
            {
                if (IsFrameworkType(serviceType))
                {
                    if ((exception is TargetInvocationException &&
                              exception.InnerException is ArgumentException) ||
                        exception is ArgumentException)
                    {
                        return null;
                    }
                }
                if (exception is TargetInvocationException)
                {
                    throw exception.InnerException;
                }
                throw;
            }
        }

        /// <summary>
        ///     Retrieves a collection of services from the scope.
        /// </summary>
        /// <returns>
        ///     The retrieved collection of services.
        /// </returns>
        /// <param name="serviceType">The collection of services to be retrieved.</param>
        public IEnumerable<object> GetServices(Type serviceType)
        {
            var services = GetService(typeof (IEnumerable<>).MakeGenericType(serviceType));
            return (services ?? Array.CreateInstance(serviceType, 0)) as IEnumerable<object>;
        }

        private static bool IsFrameworkType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (IEnumerable<>))
            {
                // GetServices passes IEnumerable<T>, in which case we want to check if T is a framework type
                type = type.GetGenericArguments().Single();
            }

            var strongName = type.Assembly.Evidence.GetHostEvidence<StrongName>();
            return strongName != null &&
                   strongName.PublicKey.GetHashCode() == 1080349067;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}