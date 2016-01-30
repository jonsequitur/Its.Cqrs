// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Verifies whether a command's precondition has been satisfied.
    /// </summary>
    public interface ICommandPreconditionVerifier
    {
        Task<bool> HasBeenApplied(string scope, string etag);
    }
}