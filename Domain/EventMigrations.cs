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

            public Guid AggregateId { get; private set; }

            public long SequenceNumber { get; private set; }

            public override string Message
            {
                get
                {
                    return String.Format("Migration failed, because no event with sequence number {0} on aggregate '{1}' was found", SequenceNumber, AggregateId);
                }
            }
        }

        public class Rename
        {
            public Rename(long sequenceNumber, string newName)
            {
                if (String.IsNullOrWhiteSpace(newName))
                {
                    throw new ArgumentOutOfRangeException("newName");
                }
                SequenceNumber = sequenceNumber;
                NewName = newName;
            }

            public long SequenceNumber { get; private set; }

            public string NewName { get; private set; }
        }
    }
}