// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.Its.Domain.Tests
{
    public class NonEventSourcedIdempotentCommandTarget : 
        NonEventSourcedCommandTarget, IIdempotentCommandTarget
    {
        public bool ShouldIgnore(ICommand command) => CommandsEnacted.Any(c => c.ETag == command.ETag);
    }
}