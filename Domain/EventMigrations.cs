// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    public static class EventMigrations
    {
        public class SequenceNumberNotFoundException : ArgumentOutOfRangeException
        {
            public SequenceNumberNotFoundException(Guid aggregateId, long sequenceNumber)
            {
                AggregateId = aggregateId;
                SequenceNumber = sequenceNumber;
            }

            public Guid AggregateId { get; }

            public long SequenceNumber { get; }

            public override string Message =>
                $"Migration failed, because no event with sequence number {SequenceNumber} on aggregate '{AggregateId}' was found";
        }

        public class Rename
        {
            public Rename(long sequenceNumber, string newName)
            {
                if (String.IsNullOrWhiteSpace(newName))
                {
                    throw new ArgumentOutOfRangeException(nameof(newName));
                }
                SequenceNumber = sequenceNumber;
                NewName = newName;
            }

            public long SequenceNumber { get; private set; }

            public string NewName { get; private set; }
        }
    }
}