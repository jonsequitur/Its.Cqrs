// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// If a repository implements the optional <see cref="IMigratableEventSourcedRepository{TAggregate}"/> 
    /// interface, then this class offers a "side channel" by which clients can rename events in the stored 
    /// event stream.
    /// </summary>
    /// <remarks>Note that existing snapshots and in-memory aggregates are not affected by these migrations.</remarks>
    public class EventMigrator<TAggregate>
        where TAggregate : EventSourcedAggregate<TAggregate>
    {
        private readonly IMigratableEventSourcedRepository<TAggregate> repository;

        public EventMigrator(IEventSourcedRepository<TAggregate> repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            this.repository = repository as IMigratableEventSourcedRepository<TAggregate>;
            if (this.repository == null)
            {
                throw new EventMigrator.RepositoryMustSupportMigrationsException(repository.GetType());
            }
        }

        public class PendingRenameList
        {
            private readonly List<Tuple<TAggregate, EventMigrator.Rename>> renames = new List<Tuple<TAggregate, EventMigrator.Rename>>();

            public void Add(TAggregate aggregate, long sequenceNumber, string newName)
            {
                renames.Add(Tuple.Create(aggregate, new EventMigrator.Rename(sequenceNumber, newName)));
            }

            public ILookup<TAggregate, EventMigrator.Rename> ToLookup()
            {
                return renames.ToLookup(_ => _.Item1, _ => _.Item2);
            }

            public void Clear()
            {
                renames.Clear();
            }
        }

        public readonly PendingRenameList PendingRenames = new PendingRenameList();

        /// <summary>
        /// Save any pending migrations + other pending changes on this aggreage.
        /// </summary>
        /// <returns></returns>
        public async Task SaveAll()
        {
            var lookup = PendingRenames.ToLookup();
            foreach (var aggregateRename in lookup)
            {
                await repository.SaveWithRenames(aggregateRename.Key, aggregateRename);
            }
            PendingRenames.Clear();
        }
    }
}