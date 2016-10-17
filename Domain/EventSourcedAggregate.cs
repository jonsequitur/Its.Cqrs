// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Its.Validation;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     Represents an aggregate whose state can be persisted as a sequence of events.
    /// </summary>
    public abstract class EventSourcedAggregate : IEventSourced
    {
        [JsonIgnore]
        private readonly EventSequence eventHistory;

        [JsonIgnore]
        private readonly EventSequence pendingEvents;

        [JsonIgnore]
        internal readonly ISnapshot sourceSnapshot;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourcedAggregate"/> class.
        /// </summary>
        /// <param name="id">The id for the new aggregate. If this is not specified, a new <see cref="Guid" /> is created.</param>
        protected EventSourcedAggregate(Guid? id = null)
        {
            Id = id ?? Guid.NewGuid();
            eventHistory = new EventSequence(Id);
            pendingEvents = new EventSequence(Id);
        }

        /// <summary>
        /// Materializes an instance of the <see cref="EventSourcedAggregate"/> class from its event history.
        /// </summary>
        /// <param name="id">The id of the aggregate.</param>
        /// <param name="eventHistory">The aggregate's event history.</param>
        /// <exception cref="System.ArgumentException">The aggregate's id cannot be <see cref="Guid.Empty" />.</exception>
        /// <exception cref="System.ArgumentNullException">eventHistory</exception>
        protected internal EventSourcedAggregate(Guid id, IEnumerable<IEvent> eventHistory) : this(id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("The aggregate's id cannot be Guid.Empty.");
            }
            if (eventHistory == null)
            {
                throw new ArgumentNullException(nameof(eventHistory));
            }

            InitializeEventHistory(eventHistory);

            if (this.eventHistory.Count == 0)
            {
                throw new ArgumentException("Event history is empty");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourcedAggregate{T}"/> class.
        /// </summary>
        /// <param name="snapshot">A snapshot of the aggregate's built-up state.</param>
        protected internal EventSourcedAggregate(ISnapshot snapshot)
            : this(snapshot.IfNotNull()
                           .Then(s => s.AggregateId)
                           .ElseThrow(() => new ArgumentNullException(nameof(snapshot))))
        {
            sourceSnapshot = snapshot;
        }

        /// <summary>
        /// Initializes the event history.
        /// </summary>
        /// <param name="sourceEvents">The source events.</param>
        /// <exception cref="System.InvalidOperationException">Event history has already been initialized.</exception>
        /// <exception cref="System.ArgumentException">Event history does not match specified aggregate id</exception>
        protected internal void InitializeEventHistory(IEnumerable<IEvent> sourceEvents)
        {
            if (eventHistory.Count > 0)
            {
                throw new InvalidOperationException("Event history has already been initialized.");
            }

            eventHistory.AddRange(sourceEvents);

            var version = Math.Max(eventHistory.Version, sourceSnapshot?.Version ?? 0);

            pendingEvents.SetVersion(version);

            if (eventHistory.AggregateId != Id)
            {
                throw new ArgumentException("Event history does not match specified aggregate id");
            }
        }

        /// <summary>
        ///     Gets the globally unique id for this aggregate.
        /// </summary>
        [JsonIgnore]
        public Guid Id { get; }

        /// <summary>
        /// Gets the version of the aggregate, which is equivalent to the sequence number of the last event.
        /// </summary>
        [JsonIgnore]
        public long Version => this.Version();

        /// <summary>
        ///     Gets any events for this aggregate that have not yet been committed to the event store.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<IEvent> PendingEvents => pendingEvents;

        /// <summary>
        ///     Gets the complete event history for the aggregate.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<IEvent> EventHistory => eventHistory;

        /// <summary>
        /// Adds an event to the pending list.
        /// </summary>
        /// <param name="e">The event.</param>
        /// <remarks>Until <see cref="ConfirmSave" /> is called, the event is not moved to event history. <see cref="ConfirmSave" /> should be called to indicate that the event has been successfully committed to the store.</remarks>
        protected internal void AddPendingEvent(IEvent e) => pendingEvents.Add(e);

        /// <summary>
        /// Enacts the command once all validations and authorizations have passed.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <exception cref="System.NotImplementedException">By default, this exception is thrown if an EnactCommand overload for the specific command type has not been implemented.</exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal virtual void EnactCommand(object command)
        {
            throw new NotImplementedException(
                string.Format(
                    @"No command of type {0} is accepted by the {1} aggregate. If you wish to add support for this command, either:

    1) create an implementation of ICommandHandler<{1}, {0}>.

    or

    2) add the following to {1}:

          public void EnactCommand({0} command)
          {{
              RecordEvent( /* NEW_EVENT */ );
          }}",
                    command.GetType(),
                    GetType()));
        }

        /// <summary>
        /// Confirms that a save operation has been successfully completed and that the aggregate should move all pending events to its event history.
        /// </summary>
        public virtual void ConfirmSave() => pendingEvents.TransferTo(eventHistory);

        internal ProbabilisticAnswer HasETag(string etag)
        {
            if (WasSourcedFromSnapshot)
            {
                if (sourceSnapshot.ETags.MayContain(etag))
                {
                    return ProbabilisticAnswer.Maybe;
                } 

                return ProbabilisticAnswer.No;
            }
            
            return eventHistory.Select(e => e.ETag)
                               .Any(e => e == etag)
                ? ProbabilisticAnswer.Yes
                : ProbabilisticAnswer.No;
        }

        internal bool WasSourcedFromSnapshot => sourceSnapshot != null;

        internal virtual void HandleCommandValidationFailure(ICommand command, ValidationReport validationReport)
        {
            throw new CommandValidationException(
                $"Validation error while applying {command.CommandName} to a {GetType().Name}.",
                validationReport);
        }

        /// <summary>
        /// Throws a command validation exception.
        /// </summary>
        /// <param name="command">The command that failed validation.</param>
        /// <param name="validationReport">The validation report.</param>
        protected void ThrowCommandValidationException(ICommand command, ValidationReport validationReport) => HandleCommandValidationFailure(command, validationReport);
    }
}