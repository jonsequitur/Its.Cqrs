// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class Projector<T> :
        IEntityModelProjector,
        IUpdateProjectionWhen<T>
        where T : IEvent
    {
        private readonly Func<DbContext> createDbContext;

        public Projector(Func<DbContext> createDbContext = null)
        {
            this.createDbContext = createDbContext ?? (() => ReadModelDbContext());
        }

        public int CallCount { get; set; }

        public void UpdateProjection(T @event)
        {
            using (var work = this.Update())
            {
                CallCount++;
                OnUpdate(work, @event);
                work.VoteCommit();
            }
        }

        public Action<UnitOfWork<ReadModelUpdate>, T> OnUpdate = (work, @event) => { };

        public DbContext CreateDbContext() => createDbContext();
    }
}