// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides methods for performing event migrations.
    /// </summary>
    public static class EventMigrations
    {
        /// <summary>
        /// Thrown when event migration fails due to sequence number than cannot be found in the stream.
        /// </summary>
        /// <seealso cref="System.ArgumentOutOfRangeException" />
        public class SequenceNumberNotFoundException : ArgumentOutOfRangeException
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SequenceNumberNotFoundException"/> class.
            /// </summary>
            /// <param name="aggregateId">The aggregate identifier.</param>
            /// <param name="sequenceNumber">The sequence number.</param>
            public SequenceNumberNotFoundException(Guid aggregateId, long sequenceNumber)
            {
                AggregateId = aggregateId;
                SequenceNumber = sequenceNumber;
            }

            /// <summary>
            /// Gets the aggregate identifier.
            /// </summary>
            public Guid AggregateId { get; }

            /// <summary>
            /// Gets the sequence number.
            /// </summary>
            public long SequenceNumber { get; }

            /// <summary>Gets the error message and the string representation of the invalid argument value, or only the error message if the argument value is null.</summary>
            public override string Message =>
                $"Migration failed, because no event with sequence number {SequenceNumber} on aggregate '{AggregateId}' was found";
        }

        /// <summary>
        /// Represents a rename operation performed during event migration.
        /// </summary>
        public class Rename
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Rename"/> class.
            /// </summary>
            /// <param name="sequenceNumber">The sequence number of the event to be renamed.</param>
            /// <param name="newName">The new name for the event.</param>
            /// <exception cref="System.ArgumentOutOfRangeException"></exception>
            public Rename(long sequenceNumber, string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    throw new ArgumentOutOfRangeException(nameof(newName));
                }
                SequenceNumber = sequenceNumber;
                NewName = newName;
            }

            /// <summary>
            /// Gets the sequence number.
            /// </summary>
            public long SequenceNumber { get; private set; }

            /// <summary>
            /// Gets the new name for the event.
            /// </summary>
            public string NewName { get; private set; }
        }
    }
}