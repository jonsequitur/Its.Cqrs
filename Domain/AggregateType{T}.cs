// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    public static class AggregateType<TAggregate> where TAggregate : IEventSourced
    {
        public static Func<Guid, IEnumerable<IEvent>, TAggregate> Factory = DefaultFactory();
        
        private static string eventStreamName = typeof(TAggregate).Name;

        public static string EventStreamName
        {
            get
            {
                return eventStreamName;
            }
            set
            {
                eventStreamName = value;
            }
        }

        private static Func<Guid, IEnumerable<IEvent>, TAggregate> DefaultFactory()
        {
            // TODO: (DefaultFactory) use a compiled expression
            var constructor = typeof (TAggregate).GetConstructor(new[] { typeof (Guid), typeof (IEnumerable<IEvent>) });

            if (constructor == null)
            {
                throw new ArgumentException(
                    string.Format(
                        "No constructor found for type '{0}' having the signature {0}(Guid id, IEnumerable<IEvent> eventHistory), which is required sourcing from events.",
                        typeof (TAggregate).Name));
            }
            
            return (id, events) => (TAggregate) constructor.Invoke(new object[] { id, events });
        }
    }
}