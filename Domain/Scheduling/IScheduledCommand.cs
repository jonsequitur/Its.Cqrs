// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Represents that a command has been scheduled for future execution.
    /// </summary>
    public interface IScheduledCommand
    {
        /// <summary>
        /// Gets the time at which the command is scheduled to be applied.
        /// </summary>
        /// <remarks>If this value is null, the command should be delivered as soon as possible.</remarks>
        DateTimeOffset? DueTime { get; }

        /// <summary>
        /// Indicates a precondition for the command to be delivered. If the precondition does not exist, then command will fail, and the aggregate can decide whether to reschedule or ignore the command.
        /// </summary>
        [JsonConverter(typeof (PreconditionConverter))]
        IPrecondition DeliveryPrecondition { get; }

        /// <summary>
        /// Gets or sets the result of the scheduled command after the command scheduler has attempted to schedule or deliver it.
        /// </summary>
        ScheduledCommandResult Result { get; set; }

        /// <summary>
        /// Gets the number of times the scheduler has previously attempted to deliver the command.
        /// </summary>
        int NumberOfPreviousAttempts { get; }

        /// <summary>
        /// Gets the clock on which the command is scheduled.
        /// </summary>
        IClock Clock { get; }
    }
}
