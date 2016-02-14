// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal class NoEventStoreETagVerifications : IETagChecker
    {
        public Task<bool> HasBeenRecorded(string scope, string etag)
        {
            throw new InvalidOperationException("No IETagChecker has been configured.");
        }
    }
}