// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public static class EventMigrator
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
                get { return String.Format("Migration failed, because no event with sequence number {0} on aggregate '{1}' was found", SequenceNumber, AggregateId); }
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
                if (String.IsNullOrWhiteSpace(newName))
                {
                    throw new ArgumentOutOfRangeException("newName");
                }
                SequenceNumber = sequenceNumber;
                NewName = newName;
            }
        }

        public static async Task SaveWithRenames<TAggregate>(IMigratableEventSourcedRepository<TAggregate> repository, TAggregate aggregate, IEnumerable<Rename> renames)
            where TAggregate : EventSourcedAggregate<TAggregate>
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            await repository.SaveWithRenames(aggregate, renames);
        }
    }
}