// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Principal;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     A command that can be applied to an aggregate to trigger some action.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        ///     Gets the name of the command.
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Gets the ETag for the command.
        /// </summary>
        string ETag { get; }

        /// <summary>
        ///     Gets or sets the principal on whose behalf the command will be authorized.
        /// </summary>
        IPrincipal Principal { get; }

        /// <summary>
        /// Gets a value indicating whether the command requires durability, even when scheduled for immediate delivery.
        /// </summary>
        bool RequiresDurableScheduling { get; }
    }
}
