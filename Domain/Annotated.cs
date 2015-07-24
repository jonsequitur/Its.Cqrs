// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    // Store the event name as something that stands out, and is not a valid .Net identifier.
    [EventName("* Annotated")]
    public class Annotated<TAggregate> : Event<TAggregate>
        where TAggregate : IEventSourced
    {
        public Annotated(string message)
        {
            Message = message;
            // Use true now, so that artificially-set timestamps in the event history don't obscure the truth.
            Timestamp = DateTimeOffset.UtcNow;
        }

        public string Message { get; private set; }

        public override void Update(TAggregate aggregate)
        {
        }
    }
}