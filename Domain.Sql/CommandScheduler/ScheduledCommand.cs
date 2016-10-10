// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
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

        /// <summary>
        /// Gets or sets a string identifying the type of the aggregate that the command targets.
        /// </summary>
        [MaxLength(100)]
        public string AggregateType { get; set; }

        /// <summary>
        /// Gets or sets the time at which the scheduled command was created.
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        /// Gets the time at which the command is scheduled to be applied.
        /// </summary>
        /// <remarks>
        /// If this value is null, the command should be delivered as soon as possible.
        /// </remarks>
        public DateTimeOffset? DueTime { get; set; }

        /// <summary>
        /// Gets or sets the time at which the command was successfully applied.
        /// </summary>
        public DateTimeOffset? AppliedTime { get; set; }

        /// <summary>
        /// Gets or sets the final attempt time in the event that the command is abandoned.
        /// </summary>
        public DateTimeOffset? FinalAttemptTime { get; set; }

        /// <summary>
        /// Gets or sets the serialized command.
        /// </summary>
        public string SerializedCommand { get; set; }

        /// <summary>
        /// Gets or sets the number of attempts that have been made to deliver the command.
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// Gets the clock on which the command is scheduled.
        /// </summary>
        public Clock Clock { get; set; }

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        [MaxLength(100)]
        public string CommandName { get; set; }

        /// <summary>
        /// Gets or sets the result of the scheduled command after the command scheduler has attempted to schedule or deliver it.
        /// </summary>
        [JsonIgnore]
        public ScheduledCommandResult Result { get; set; }

        internal bool NonDurable { get; set; }

        int IScheduledCommand.NumberOfPreviousAttempts => Attempts;

        IPrecondition IScheduledCommand.DeliveryPrecondition =>
            SerializedCommand.FromJsonTo<JObject>()
                             .IfHas(d => d.DeliveryPrecondition)
                             .ElseDefault();

        IClock IScheduledCommand.Clock => Clock;
    }
}