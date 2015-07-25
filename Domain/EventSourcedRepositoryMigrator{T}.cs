// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// If a repository implements the optional <see cref="IMigratableEventSourcedRepository{TAggregate}"/> 
    /// interface, then this class offers a "side channel" by which clients can rename events in the stored 
    /// event stream.
    /// </summary>
    /// <remarks>Note that existing snapshots and in-memory aggregates are not affected by these migrations.</remarks>
    public class EventSourcedRepositoryMigrator<TAggregate>
        where TAggregate : EventSourcedAggregate<TAggregate>
    {
        private readonly IMigratableEventSourcedRepository<TAggregate> repository;

        public EventSourcedRepositoryMigrator(IEventSourcedRepository<TAggregate> repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            this.repository = repository as IMigratableEventSourcedRepository<TAggregate>;
            if (this.repository == null)
            {
                throw new EventSourcedRepositoryMigrator.RepositoryMustSupportMigrationsException(repository.GetType());
            }
        }

        public class PendingRenameList : List<Tuple<TAggregate, EventSourcedRepositoryMigrator.RenameRequest>>
        {
            public void Add(TAggregate aggregate, long sequenceNumber, string newName)
            {
                base.Add(Tuple.Create(aggregate, new EventSourcedRepositoryMigrator.RenameRequest(sequenceNumber, newName)));
            }
        }

        public readonly PendingRenameList PendingRenames = new PendingRenameList();

        public async Task Save(TAggregate aggregate)
        {
            var lookup = PendingRenames.ToLookup(_ => _.Item1, _ => _.Item2);
            foreach (var aggregateRename in lookup)
            {
                await repository.SaveWithRenames(aggregateRename.Key, aggregateRename.ToList());
            }
            PendingRenames.Clear();
        }
    }
}
