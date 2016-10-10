// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides information about an error that occurs while handling an event.
    /// </summary>
    [DebuggerStepThrough]
    public class EventHandlingError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandlingError"/> class.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="event">The event that was being handled when the exception occurred.</param>
        public EventHandlingError(
            Exception exception,
            object handler = null,
            IEvent @event = null)
        {
            Exception = exception;
            exception.Data["ActivityId"] = Trace.CorrelationManager.ActivityId;
            Handler = handler.InnerHandler();
            Event = @event;

            if (@event != null)
            {
                AggregateId = @event.AggregateId;
                SequenceNumber = @event.SequenceNumber;
                StreamName = @event.EventStreamName();
            }
        }

        /// <summary>
        /// Gets or sets the sequence number of the event that was being handled when the error occurred.
        /// </summary>
        public long SequenceNumber { get; protected set; }

        /// <summary>
        /// Gets or sets the aggregate id of the event that was being handled when the error occurred.
        /// </summary>
        public Guid AggregateId { get; protected set; }

        /// <summary>
        /// Gets the event that was being handled when the error occurred.
        /// </summary>
        public IEvent Event { get; private set; }

        /// <summary>
        /// Gets handler in which the error occurred.
        /// </summary>
        public object Handler { get; private set; }

        /// <summary>
        /// Gets the exception that was thrown within the event handler.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets or sets the name of the stream containing the event that was being handled when the error occurred.
        /// </summary>
        public string StreamName { get; protected set; }
    }
}