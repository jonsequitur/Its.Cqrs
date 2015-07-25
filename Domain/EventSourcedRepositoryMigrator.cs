// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    public static class EventSourcedRepositoryMigrator
    {
        public class SequenceNumberNotFoundException : ArgumentOutOfRangeException
        {
            public readonly Guid AggregateId;
            public readonly long SequenceNumber;

            public SequenceNumberNotFoundException(Guid aggregateId, long sequenceNumber)
            {
                AggregateId = aggregateId;
                SequenceNumber = sequenceNumber;
            }

            public override string Message
            {
                get { return string.Format("Migration failed, because no event with sequence number {0} on aggregate '{1}' was found", SequenceNumber, AggregateId); }
            }
        }

        public class RepositoryMustSupportMigrationsException : ArgumentOutOfRangeException
        {
            private readonly Type eventSourcedRepositoryType;

            public RepositoryMustSupportMigrationsException(Type eventSourcedRepositoryType)
            {
                this.eventSourcedRepositoryType = eventSourcedRepositoryType;
            }

            public override string Message
            {
                get
                {
                    return String.Format("Repository type '{0}' cannot be used for migrations because it does not implement '{1}'",
                                         eventSourcedRepositoryType.Name,
                                         typeof (IMigratableEventSourcedRepository<>).Name);
                }
            }
        }

        public class Rename
        {
            public readonly long SequenceNumber;
            public readonly string NewName;

            public Rename(long sequenceNumber, string newName)
            {
                SequenceNumber = sequenceNumber;
                NewName = newName;
            }
        }
    }
}