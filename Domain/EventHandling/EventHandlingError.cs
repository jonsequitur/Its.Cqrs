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

        public long SequenceNumber { get; protected set; }

        public Guid AggregateId { get; protected set; }

        public IEvent Event { get; private set; }

        public object Handler { get; private set; }

        public Exception Exception { get; private set; }

        public string StreamName { get; protected set; }
    }
}
