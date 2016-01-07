// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Represents that a command has been scheduled for future execution against a specific aggregate type.
    /// </summary>
    public interface IScheduledCommand<in TAggregate> :
        IScheduledCommand
        where TAggregate : IEventSourced
    {
        /// <summary>
        /// Gets the command to be applied at a later time.
        /// </summary>
        ICommand<TAggregate> Command { get; }

        /// <summary>
        /// Gets the id of the aggregate to which the command will be applied when delivered.
        /// </summary>
        Guid AggregateId { get; }

        /// <summary>
        /// Gets the sequence number of the scheduled command.
        /// </summary>
        long SequenceNumber { get; }
    }
}