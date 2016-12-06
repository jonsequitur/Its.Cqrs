// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A command target that supports idempotency of applied commands, allowing at-most-once delivery of commands.
    /// </summary>
    public interface IIdempotentCommandTarget
    {
        /// <summary>
        /// Indicates whether a command should be ignored because it has been previuosly applied.
        /// </summary>
        /// <param name="command">The command.</param>
        bool ShouldIgnore(ICommand command);
    }
}