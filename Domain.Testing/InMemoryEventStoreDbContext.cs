// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// An in-memory implementation of <see cref="EventStoreDbContext" />.
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.Sql.EventStoreDbContext" />
    public class InMemoryEventStoreDbContext : EventStoreDbContext
    {
        private readonly InMemoryEventStream eventStream;
        private readonly DbSet<StorableEvent> sqlEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEventStoreDbContext"/> class.
        /// </summary>
        /// <param name="eventStream">The event stream in which events are saved.</param>
        public InMemoryEventStoreDbContext(InMemoryEventStream eventStream = null)
        {
            this.eventStream = eventStream ?? new InMemoryEventStream();
            eventStream = eventStream ?? Domain.Configuration
                                               .Current
                                               .Container
                                               .Resolve<InMemoryEventStream>();

            sqlEvents = new InMemoryDbSet<StorableEvent>(new HashSet<StorableEvent>(eventStream.Events.Select(e => e.ToStorableEvent())));
        }

        /// <summary>
        /// Gets a database set for the events in the store.
        /// </summary>
        public override DbSet<StorableEvent> Events
        {
            get
            {
                return sqlEvents;
            }
            set
            {
            }
        }

        /// <summary>
        /// Returns a <see cref="T:System.Data.Entity.DbSet`1" /> instance for access to entities of the given type in the context
        /// and the underlying store.
        /// </summary>
        /// <remarks>
        /// Note that Entity Framework requires that this method return the same instance each time that it is called
        /// for a given context instance and entity type. Also, the non-generic <see cref="T:System.Data.Entity.DbSet" /> returned by the
        /// <see cref="M:System.Data.Entity.DbContext.Set(System.Type)" /> method must wrap the same underlying query and set of entities. These invariants must
        /// be maintained if this method is overridden for anything other than creating test doubles for unit testing.
        /// See the <see cref="T:System.Data.Entity.DbSet`1" /> class for more details.
        /// </remarks>
        /// <typeparam name="TEntity"> The type entity for which a set should be returned. </typeparam>
        /// <returns> A set for the given entity type. </returns>
        public override DbSet<TEntity> Set<TEntity>()
        {
            return (dynamic) sqlEvents;
        }

        /// <summary>
        /// Opens the underlying database connection.
        /// </summary>
        protected internal override Task OpenAsync()
        {
            return Task.FromResult(0);
        }

        /// <summary>
        /// Saves all changes made in this context to the underlying database.
        /// </summary>
        /// <returns>
        /// The number of state entries written to the underlying database. This can include
        /// state entries for entities and/or relationships. Relationship state entries are created for
        /// many-to-many relationships and relationships where there is no foreign key property
        /// included in the entity class (often referred to as independent associations).
        /// </returns>
        /// <exception cref="T:System.Data.Entity.Infrastructure.DbUpdateException">An error occurred sending updates to the database.</exception>
        /// <exception cref="T:System.Data.Entity.Infrastructure.DbUpdateConcurrencyException">
        /// A database command did not affect the expected number of rows. This usually indicates an optimistic
        /// concurrency violation; that is, a row has been changed in the database since it was queried.
        /// </exception>
        /// <exception cref="T:System.Data.Entity.Validation.DbEntityValidationException">
        /// The save was aborted because validation of entity property values failed.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// An attempt was made to use unsupported behavior such as executing multiple asynchronous commands concurrently
        /// on the same context instance.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The context or connection have been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// Some error occurred attempting to process entities in the context either before or after sending commands
        /// to the database.
        /// </exception>
        public override int SaveChanges()
        {
            AssignIdsAndSyncStream().Wait();

            return base.SaveChanges();
        }

        /// <summary>
        /// Asynchronously saves all changes made in this context to the underlying database.
        /// </summary>
        /// <remarks>
        /// Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        /// that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <returns>
        /// A task that represents the asynchronous save operation.
        /// The task result contains the number of state entries written to the underlying database. This can include
        /// state entries for entities and/or relationships. Relationship state entries are created for
        /// many-to-many relationships and relationships where there is no foreign key property
        /// included in the entity class (often referred to as independent associations).
        /// </returns>
        /// <exception cref="T:System.Data.Entity.Infrastructure.DbUpdateException">An error occurred sending updates to the database.</exception>
        /// <exception cref="T:System.Data.Entity.Infrastructure.DbUpdateConcurrencyException">
        /// A database command did not affect the expected number of rows. This usually indicates an optimistic
        /// concurrency violation; that is, a row has been changed in the database since it was queried.
        /// </exception>
        /// <exception cref="T:System.Data.Entity.Validation.DbEntityValidationException">
        /// The save was aborted because validation of entity property values failed.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// An attempt was made to use unsupported behavior such as executing multiple asynchronous commands concurrently
        /// on the same context instance.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The context or connection have been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// Some error occurred attempting to process entities in the context either before or after sending commands
        /// to the database.
        /// </exception>
        public override async Task<int> SaveChangesAsync()
        {
            await AssignIdsAndSyncStream();

            return await base.SaveChangesAsync();
        }

        private async Task AssignIdsAndSyncStream()
        {
            var newEvents =
                sqlEvents
                    .Select(e => new
                    {
                        inMemoryEvent = e.ToInMemoryStoredEvent(),
                        sqlEvent = e
                    })
                    .Where(ee => !eventStream.Contains(ee.inMemoryEvent))
                    .ToArray();

            newEvents.ForEach(e =>
            {
                var nextId = Interlocked.Increment(ref eventStream.NextAbsoluteSequenceNumber);
                e.inMemoryEvent.Metadata.AbsoluteSequenceNumber = nextId;
                e.sqlEvent.Id = nextId;
            });

            await eventStream.Append(newEvents.Select(_ => _.inMemoryEvent).ToArray());
        }
    }

    internal class InMemoryDbSet<T> :
        DbSet<T>,
        IQueryable<T>,
        IDbAsyncEnumerable<T>
        where T : class
    {
        private readonly HashSet<T> source;

        public InMemoryDbSet(HashSet<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            this.source = source;
        }

        public override T Add(T entity)
        {
            source.Add(entity);
            return entity;
        }

        public override IEnumerable<T> AddRange(IEnumerable<T> entities)
        {
            var es = entities.ToArray();
            es.ForEach(e => Add(e));
            return es;
        }

        public override T Remove(T entity)
        {
            source.Remove(entity);
            return entity;
        }

        public override IEnumerable<T> RemoveRange(IEnumerable<T> entities)
        {
            var es = entities.ToArray();
            es.ForEach(e => Remove(e));
            return es;
        }

        public IEnumerator<T> GetEnumerator() => source.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Expression Expression => source.AsQueryable().Expression;

        public Type ElementType => source.AsQueryable().ElementType;

        public IQueryProvider Provider => new InMemoryAsyncQueryProvider<T>(source.AsQueryable().Provider);

        public IDbAsyncEnumerator<T> GetAsyncEnumerator() => new InMemoryAsyncEnumerator<T>(source.GetEnumerator());

        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator() => GetAsyncEnumerator();
    }

    internal class InMemoryAsyncEnumerator<T> :
        IDbAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> inner;

        public InMemoryAsyncEnumerator(IEnumerator<T> inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }
            this.inner = inner;
        }

        public void Dispose() => inner.Dispose();

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken) =>
            Task.FromResult(inner.MoveNext());

        T IDbAsyncEnumerator<T>.Current => inner.Current;

        public object Current => inner.Current;
    }

    internal class InMemoryAsyncQueryProvider<TEntity> : IQueryProvider
    {
        private readonly IQueryProvider inner;

        internal InMemoryAsyncQueryProvider(IQueryProvider inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }
            this.inner = inner;
        }

        public IQueryable CreateQuery(Expression expression) =>
            new InMemoryAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new InMemoryAsyncEnumerable<TElement>(expression);

        public object Execute(Expression expression) =>
            inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken) =>
            Task.FromResult(Execute(expression));

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) =>
            Task.FromResult(Execute<TResult>(expression));
    }

    internal class InMemoryAsyncEnumerable<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
    {
        public InMemoryAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        {
        }

        public InMemoryAsyncEnumerable(Expression expression)
            : base(expression)
        {
        }

        public IDbAsyncEnumerator<T> GetAsyncEnumerator() =>
            new InMemoryAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator() =>
            GetAsyncEnumerator();

        IQueryProvider IQueryable.Provider =>
            new InMemoryAsyncQueryProvider<T>(this);
    }
}