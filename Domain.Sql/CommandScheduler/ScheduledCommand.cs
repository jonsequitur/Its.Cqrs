// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// A command that has been scheduled via the command scheduler.
    /// </summary>
    public class ScheduledCommand : IScheduledCommand
    {
        /// <summary>
        /// Gets the id of the aggregate to which the command will be applied.
        /// </summary>
        public Guid AggregateId { get; set; }

        /// <summary>
        /// Gets the position of the event within the source object's event sequence.
        /// </summary>
        public long SequenceNumber { get; set; }

        public string AggregateType { get; set; }

        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        /// Gets the time at which the command is scheduled to be applied.
        /// </summary>
        /// <remarks>
        /// If this value is null, the command should be delivered as soon as possible.
        /// </remarks>
        public DateTimeOffset? DueTime { get; set; }

        public DateTimeOffset? AppliedTime { get; set; }

        public DateTimeOffset? FinalAttemptTime { get; set; }

        public string SerializedCommand { get; set; }

        public int Attempts { get; set; }

        public Clock Clock { get; set; }

        [JsonIgnore]
        public ScheduledCommandResult Result { get; set; }

        internal bool NonDurable { get; set; }

        CommandPrecondition IScheduledCommand.DeliveryPrecondition
        {
            get
            {
                return SerializedCommand.FromJsonTo<JObject>()
                                        .IfHas(d => d.DeliveryPrecondition)
                                        .ElseDefault();
            }
        }
    }
}
