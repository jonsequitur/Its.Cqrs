// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An annotation in the event stream.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <seealso cref="Microsoft.Its.Domain.Event{TAggregate}" />
    [EventName("* Annotated") /*  Store the event name as something that stands out, and is not a valid .Net identifier. */  ]
    public class Annotated<TAggregate> : Event<TAggregate>
        where TAggregate : IEventSourced
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Annotated{TAggregate}"/> class.
        /// </summary>
        /// <param name="message">The message contained by the annotation.</param>
        public Annotated(string message)
        {
            Message = message;
            // Use true now, so that artificially-set timestamps in the event history don't obscure the truth.
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the message contained by the annotation..
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        ///     Updates an aggregate to a new state.
        /// </summary>
        /// <param name="aggregate">The aggregate to be updated.</param>
        /// <remarks>This method is called when materializing an aggregate from an event stream.</remarks>
        public override void Update(TAggregate aggregate)
        {
        }
    }
}