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
        IScheduledCommand<TAggregate>,
#pragma warning disable 618
        IScheduledCommandEvent
#pragma warning restore 618
        where TAggregate : IEventSourced
    {
        private ScheduledCommandResult result;

        public CommandScheduled()
        {
            if (string.IsNullOrWhiteSpace(ETag))
            {
                ETag = Guid.NewGuid().ToString("N").ToETag();
            }
        }

        /// <summary>
        /// Gets the command to be applied at a later time.
        /// </summary>
        [JsonConverter(typeof (CommandConverter))]
        public ICommand<TAggregate> Command { get; set; }

        string IScheduledCommand<TAggregate>.TargetId
        {
            get
            {
                return AggregateId.ToString();
            }
        }

        /// <summary>
        /// Gets the time at which the command is scheduled to be applied.
        /// </summary>
        /// <remarks>
        /// If this value is null, the command should be delivered as soon as possible.
        /// </remarks>
        public DateTimeOffset? DueTime { get; set; }

        /// <summary>
        /// Indicates a precondition ETag for a specific aggregate. If no event on the target aggregate exists with this ETag, the command will fail, and the aggregate can decide whether to reschedule or ignore the command.
        /// </summary>
        public IPrecondition DeliveryPrecondition { get; set; }

        [JsonIgnore]
        public ScheduledCommandResult Result    
        {
            get
            {
                return result;
            }
            set
            {
                result = value;
            }
        }

        /// <summary>
        /// Updates an aggregate to a new state.
        /// </summary>
        /// <param name="aggregate">The aggregate to be updated.</param>
        /// <remarks>
        /// This method is called when materializing an aggregate from an event stream.
        /// </remarks>
        public override void Update(TAggregate aggregate)
        {
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}{1}{2}{3}",
                                 Command,
                                 DueTime.IfNotNull()
                                        .Then(due => " due " + due)
                                        .ElseDefault(),
                                 DeliveryPrecondition.IfNotNull()
                                                     .Then(p => ", depends on " + p)
                                                     .ElseDefault(),
                                 Result.IfNotNull()
                                       .Then(r => ", " + r)
                                       .ElseDefault());
        }
    }
}