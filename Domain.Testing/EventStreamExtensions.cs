// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Testing
{
    public static class EventStreamExtensions
    {
        public static async Task<long> NextVersion(
            this IEventStream stream,
            string id)
        {
            var latest = await stream.Latest(id);
            
            return latest.IfNotNull()
                         .Then(r => r.SequenceNumber + 1)
                         .Else(() => 1);
        }
    }
}
