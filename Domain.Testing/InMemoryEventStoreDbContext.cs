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
    public class InMemoryEventStoreDbContext : EventStoreDbContext
    {
        private readonly DbSet<StorableEvent> events;

        public InMemoryEventStoreDbContext(InMemoryEventStream stream = null)
        {
            stream = stream ?? Domain.Configuration
                                     .Current
                                     .Container
                                     .Resolve<InMemoryEventStream>();

            events = new InMemoryDbSet<StorableEvent>(stream.Events.Select(e => e.ToStorableEvent()));
        }

        public override DbSet<StorableEvent> Events
        {
            get
            {
                return events;
            }
            set
            {
            }
        }

        public override DbSet<TEntity> Set<TEntity>()
        {
            return (dynamic) events;
        }

        protected internal override Task OpenAsync()
        {
            return Task.FromResult(0);
        }
    }

    internal class InMemoryDbSet<T> :
        DbSet<T>,
        IQueryable<T>,
        IDbAsyncEnumerable<T>
        where T : class
    {
        private readonly List<T> source;

        public InMemoryDbSet(IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            this.source = source.ToList();
        }

        public override T Add(T entity)
        {
            source.Add(entity);
            return entity;
        }

        public override IEnumerable<T> AddRange(IEnumerable<T> entities)
        {
            source.AddRange(entities);
            return entities;
        }

        public override T Remove(T entity)
        {
            source.Remove(entity);
            return entity;
        }

        public override IEnumerable<T> RemoveRange(IEnumerable<T> entities)
        {
            entities.ForEach(e => Remove(e));
            return entities;
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