// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Represents a precondition for a command.
    /// </summary>
    public interface IPrecondition
    {
        /// <summary>
        /// Gets the etag, which must be unique within the precondition's <see cref="Scope" />.
        /// </summary>
        string ETag { get; }

        /// <summary>
        /// Gets the scope within which the <see cref="ETag" /> is unique.
        /// </summary>
        string Scope { get; }
    }
}