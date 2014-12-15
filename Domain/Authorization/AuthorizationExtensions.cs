// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Principal;

namespace Microsoft.Its.Domain.Authorization
{
    /// <summary>
    /// Provides methods for performing authorization checks.
    /// </summary>
    public static class AuthorizationExtensions
    {
        /// <summary>
        /// Determines whether the principal is authorized to apply a command to a resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <typeparam name="TPrincipal">The type of the principal.</typeparam>
        /// <param name="principal">The principal.</param>
        /// <param name="command">The command.</param>
        /// <param name="resource">The resource.</param>
        /// <returns>
        ///   <c>true</c> if the principal is authorized; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAuthorizedTo<TResource, TCommand, TPrincipal>(
            this TPrincipal principal,
            TCommand command,
            TResource resource)
            where TCommand : class, ICommand<TResource>
            where TPrincipal : class, IPrincipal where TResource : class
        {
            if (principal == null)
            {
                return false;
            }

            var query = AuthorizationQuery.Create(resource, command, principal);

            var policy = AuthorizationPolicy.For(resource.GetType(), command.GetType(), principal.GetType());

            var report = ((dynamic) policy).ValidationPlan.Execute((dynamic) query, haltOnFirstFailure: true);

            if (report.HasFailures)
            {
                return false;
            }

            return true;
        }
    }
}