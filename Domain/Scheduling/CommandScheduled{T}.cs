// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An event that indicates that a command was scheduled.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    [EventName("Scheduled")]
    [DebuggerDisplay("{ToString()}")]
    public class CommandScheduled<TAggregate> :
        Event<TAggregate>,
        IScheduledCommand<TAggregate>
        where TAggregate : IEventSourced
    {
        [JsonConverter(typeof (CommandConverter))]
        public ICommand<TAggregate> Command { get; set; }

        public DateTimeOffset? DueTime { get; set; }

        /// <summary>
        /// Indicates a precondition ETag for a specific aggregate. If no event on the target aggregate exists with this ETag, the command will fail, and the aggregate can decide whether to reschedule or ignore the command.
        /// </summary>
        public ScheduledCommandPrecondition DeliveryPrecondition { get; set; }

        public ScheduledCommandResult Result { get; set; }

        public override void Update(TAggregate aggregate)
        {
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}",
                                 Command,
                                 DueTime.IfNotNull()
                                        .Then(due => " @ " + due)
                                        .ElseDefault(),
                                 DeliveryPrecondition.IfNotNull()
                                                     .Then(p => " depends on " + p)
                                                     .ElseDefault());
        }
    }
}
