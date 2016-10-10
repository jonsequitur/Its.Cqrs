// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Principal;

namespace Microsoft.Its.Domain.Authorization
{
    /// <summary>
    ///     Represents the query for an authorization check.
    /// </summary>
    public class AuthorizationQuery
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationQuery"/> class.
        /// </summary>
        protected internal AuthorizationQuery()
        {
        }

        /// <summary>
        ///     Creates an AuthorizationQuery.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <typeparam name="TPrincipal">The type of the principal.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="command">The command.</param>
        /// <param name="principal">The principal.</param>
        /// <returns></returns>
        public static AuthorizationQuery Create<TResource, TCommand, TPrincipal>(TResource resource, TCommand command, TPrincipal principal)
            where TPrincipal : class, IPrincipal
            where TCommand : class, ICommand
            where TResource : class
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            var queryType = typeof (AuthorizationQuery<,,>)
                .MakeGenericType(resource.GetType(), command.GetType(), principal.GetType());

            // TODO: (Create) optimize using expression compilation
            return (AuthorizationQuery) Activator.CreateInstance(queryType, resource, command, principal);
        }
    }
}
