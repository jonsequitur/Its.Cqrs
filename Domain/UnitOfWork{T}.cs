// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.Remoting.Messaging;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     Used to scope a unit of work and share state (the unit of work's subject) across a number of participants.
    /// </summary>
    /// <typeparam name="T">The type of the subject of the unit of work.</typeparam>
    public sealed class UnitOfWork<T> : IDisposable where T : class
    {
        private static readonly string callContextPrefix = $"{typeof (UnitOfWork<>).Assembly.GetName().Name}.UnitOfWork:{typeof (T)}";

        private readonly bool isOutermost;
        private bool canCommit;
        private readonly UnitOfWork<T> outer;
        private bool rejected;
        private bool disposed;
        private readonly Dictionary<Type, object> resources;
        private readonly CompositeDisposable disposables;
        private Exception exception;
        private readonly RejectUnitOfWork reject = Reject;
        private readonly CommitUnitOfWork commit = Commit;

        /// <summary>
        /// Occurs when a unit of work is committed.
        /// </summary>
        public static event EventHandler<T> Committed;

        /// <summary>
        /// Occurs when a unit of work is rejected.
        /// </summary>
        public static event EventHandler<T> Rejected;

        /// <summary>
        /// Initializes the <see cref="UnitOfWork{T}"/> class.
        /// </summary>
        static UnitOfWork()
        {
            if (Create == null)
            {
                ConfigureDefault();
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnitOfWork{T}" /> class.
        /// </summary>
        public UnitOfWork(params object[] resources)
        {
            outer = GetFromContext();

            Action mergeResources = () =>
            {
                if (resources != null && resources.Length > 0)
                {
                    resources.ForEach(r => AddResource(r));
                }
            };

            if (outer == null)
            {
                isOutermost = true;
                outer = this;
                this.resources = new Dictionary<Type, object>();
                disposables = new CompositeDisposable();
                mergeResources();
                Create(this, subject => subject.IfNotNull()
                                               .ThenDo(s => AddResource(subject)));
                SetInContext(this);
            }
            else
            {
                this.resources = outer.resources;
                disposables = outer.disposables;
                mergeResources();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork{T}"/> class.
        /// </summary>
        /// <param name="create">A delegate that is called when the unit of work is created.</param>
        /// <param name="reject">A delegate that is called when the unit of work is rejected.</param>
        /// <param name="commit">A delegate that is called when the unit of work is commited.</param>
        public UnitOfWork(Func<T> create,
                          Action<T> reject = null,
                          Action<T> commit = null) : this()
        {
            if (isOutermost)
            {
                AddResource(create());

                if (commit != null)
                {
                    this.commit = work => commit(work.Resource<T>());
                }
                if (reject != null)
                {
                    this.reject = work => reject(work.Resource<T>());
                }
            }
        }

        /// <summary>
        ///     Gets the subject of the current unit of work.
        /// </summary>
        public T Subject => Resource<T>();

        /// <summary>
        /// Reports an exception due to which the unit of work must be rejected.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void RejectDueTo(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }
            rejected = true;
            Exception = exception;
        }

        /// <summary>
        /// Adds a resource to the unit of work which will be accessible to nested units of work and will be disposed when the unit of work is disposed.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="dispose">if set to <c>true</c> dipose the resource when the unit of work is completed; otherwise, don't dispose it.</param>
        /// <returns>
        /// The same unit of work.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">resource</exception>
        /// <exception cref="System.InvalidOperationException">Resources cannot be added to a disposed UnitOfWork.</exception>
        public UnitOfWork<T> AddResource<TResource>(TResource resource, bool dispose = true) => 
            AddResource(typeof(TResource), resource, dispose);

        internal UnitOfWork<T> AddResource(Type resourceType, object resource, bool dispose)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }
            if (disposed)
            {
                throw new InvalidOperationException("Resources cannot be added to a disposed UnitOfWork.");
            }

            resources.Add(resourceType, resource);

            if (dispose)
            {
                resource.IfTypeIs<IDisposable>()
                        .ThenDo(d => disposables.Add(d));
            }

            return this;
        }

        /// <summary>
        /// Gets the exception, if any, that caused the unit of work to be rejected.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return outer.exception;
            }
            private set
            {
                outer.exception = value;
            }
        }

        /// <summary>
        ///     Gets a resource of the specified type, if availble, or null.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <returns></returns>
        public TResource Resource<TResource>() where TResource : class
        {
            object resource;
            
            if (resources.TryGetValue(typeof (TResource), out resource))
            {
                return (TResource) resource;
            }

            return null;
        }

        /// <summary>
        ///     Gets or sets a delegate used to instantate the subject and add resources when a new unit of work is started.
        /// </summary>
        public static CreateUnitOfWork Create { get; set; }

        /// <summary>
        ///     Gets or sets a delegate used to commit a unit of work.
        /// </summary>
        public static CommitUnitOfWork Commit { get; set; }

        /// <summary>
        ///     Gets or sets a delegate used to reject a unit of work.
        /// </summary>
        public static RejectUnitOfWork Reject { get; set; }

        /// <summary>
        ///     Gets the ambient unit of work in progress, if any, in the current context.
        /// </summary>
        public static UnitOfWork<T> Current => CallContext.LogicalGetData(callContextPrefix) as UnitOfWork<T>;

        /// <summary>
        ///     Completes the unit of work.
        /// </summary>
        /// <remarks>
        ///     By default, if the subject of the unit of work implements <see cref="IDisposable" />, then it will disposed when the root unit of work is disposed.
        /// </remarks>
        public void Dispose()
        {
            var subject = Subject;
            if (!canCommit && subject != null)
            {
                RejectAll();
            }

            if (isOutermost)
            {
                if (!rejected && subject != null)
                {
                    try
                    {
                        commit(this);

                        // check again that Commit did not fail in a way that requires rejection
                        if (!rejected)
                        {
                            Committed?.Invoke(this, subject);
                        }
                        else
                        {
                            RejectAll();
                        }
                    }
                    catch (Exception ex)
                    {
                        RejectDueTo(ex);
                        RejectAll();
                    }
                }
                SetInContext(null);

                disposables.Dispose();
                resources.Clear();
            }

            disposed = true;
        }

        private void RejectAll()
        {
            if (isOutermost)
            {
                rejected = true;
                reject(this);
                Rejected?.Invoke(this, Subject);
            }
            else
            {
                outer.RejectAll();
            }
        }

        private UnitOfWork<T> GetFromContext() => CallContext.LogicalGetData(callContextPrefix) as UnitOfWork<T>;

        private void SetInContext(UnitOfWork<T> context) => CallContext.LogicalSetData(callContextPrefix, context);

        /// <summary>
        /// Votes that the unit of work should be committed.
        /// </summary>
        /// <remarks>
        /// All participants in the unit of work must vote commit for it to actually be committed.
        /// </remarks>
        public void VoteCommit() => canCommit = true;

        /// <summary>
        ///     Sets the unit of work for type <typeparamref name="T" /> to its default behavior.
        /// </summary>
        public static void ConfigureDefault()
        {
            Create = delegate { };

            Action<UnitOfWork<T>> dispose = work => work.disposables.Dispose();

            Commit = work => dispose(work);
            Reject = work => dispose(work);
        }

        /// <summary>
        /// A delegate for specifying an action taken when a unit of work is created.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        /// <param name="setSubject">A delegate used to set the subject of the unit of work.</param>
        public delegate void CreateUnitOfWork(UnitOfWork<T> unitOfWork, Action<T> setSubject);

        /// <summary>
        /// A delegate for specifying an action taken when a unit of work is committed.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        public delegate void CommitUnitOfWork(UnitOfWork<T> unitOfWork);

        /// <summary>
        /// A delegate for specifying an action taken when a unit of work is rejected.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        public delegate void RejectUnitOfWork(UnitOfWork<T> unitOfWork);
    }
}
