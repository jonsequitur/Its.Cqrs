// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Authorization
{
    /// <summary>
    ///     Represents a check for whether a principal can issue a specific command pertaining to a specific resource.
    /// </summary>
    public sealed class AuthorizationQuery<TResource, TCommand, TPrincipal> :
        AuthorizationQuery,
        IAuthorizationQuery<TResource, TCommand, TPrincipal>
        where TCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationQuery{TResource,TCommand,TPrincipal}" /> class.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="command">The command.</param>
        /// <param name="principal">The principal.</param>
        /// <exception cref="System.ArgumentNullException">principal</exception>
        public AuthorizationQuery(TResource resource, TCommand command, TPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException("principal");
            }

            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }

            if (command == null)
            {
                throw new ArgumentNullException("command");
            }
            Command = command;
            Principal = principal;
            Resource = resource;
        }

        /// <summary>
        ///     Gets the command to be authorized.
        /// </summary>
        public TCommand Command { get; private set; }

        /// <summary>
        ///     Gets the resource to which the command would be applied.
        /// </summary>
        public TResource Resource { get; private set; }

        /// <summary>
        ///     Gets the principal that would apply the command.
        /// </summary>
        public TPrincipal Principal { get; private set; }
    }
}