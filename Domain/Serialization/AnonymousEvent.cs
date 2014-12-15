// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Serialization
{
    internal class AnonymousEvent<TAggregate> : Event<TAggregate> where TAggregate : IEventSourced
    {
        public string Body { get; set; }

        public override void Update(TAggregate aggregate)
        {
        }
    }
}