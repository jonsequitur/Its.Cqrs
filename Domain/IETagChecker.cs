// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Verifies whether an etag has been recorded.
    /// </summary>
    public interface IETagChecker
    {
        /// <summary>
        /// Determines whether the specified etag has been recorded within the specified scope.
        /// </summary>
        /// <param name="scope">The scope within which the etag is unique.</param>
        /// <param name="etag">The etag.</param>
        Task<bool> HasBeenRecorded(string scope, string etag);
    }
}