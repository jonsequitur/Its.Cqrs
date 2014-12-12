﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     Represents an aggregate whose state can be persisted as a sequence of events.
    /// </summary>
    public abstract class EventSourcedAggregate : IEventSourced
    {
        private readonly Guid id;
        private readonly EventSequence eventHistory;
        private readonly EventSequence pendingEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourcedAggregate"/> class.
        /// </summary>
        /// <param name="id">The id for the new aggregate. If this is not specified, a new <see cref="Guid" /> is created.</param>
        protected EventSourcedAggregate(Guid? id = null)
        {
            this.id = id ?? Guid.NewGuid();
            eventHistory = new EventSequence(this.id);
            pendingEvents = new EventSequence(this.id);
        }

        /// <summary>
        /// Materializes an instance of the <see cref="EventSourcedAggregate"/> class from its event history.
        /// </summary>
        /// <param name="id">The id of the aggregate.</param>
        /// <param name="eventHistory">The aggregate's event history.</param>
        /// <exception cref="System.ArgumentException">The aggregate's id cannot be <see cref="Guid.Empty" />.</exception>
        /// <exception cref="System.ArgumentNullException">eventHistory</exception>
        protected EventSourcedAggregate(Guid id, IEnumerable<IEvent> eventHistory) : this(id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("The aggregate's id cannot be Guid.Empty.");
            }
            if (eventHistory == null)
            {
                throw new ArgumentNullException("eventHistory");
            }

            this.eventHistory.AddRange(eventHistory);
            pendingEvents.StartSequenceFrom(this.eventHistory.Version);
            this.id = this.eventHistory.AggregateId;
        }

        /// <summary>
        ///     Gets the globally unique id for this aggregate.
        /// </summary>
        public Guid Id
        {
            get
            {
                return id;
            }
        }

        /// <summary>
        /// Gets the version of the aggregate, which is equivalent to the sequence number of the last event.
        /// </summary>
        public long Version
        {
            get
            {
                return this.Version();
            }
        }

        /// <summary>
        ///     Gets any events for this aggregate that have not yet been committed to the event store.
        /// </summary>
        public IEnumerable<IEvent> PendingEvents
        {
            get
            {
                return pendingEvents;
            }
        }

        /// <summary>
        ///     Gets the complete event history for the aggregate.
        /// </summary>
        public IEnumerable<IEvent> EventHistory
        {
            get
            {
                return eventHistory;
            }
        }

        /// <summary>
        /// Adds an event to the pending list.
        /// </summary>
        /// <param name="e">The event.</param>
        /// <remarks>Until <see cref="ConfirmSave" /> is called, the event is not moved to event history. <see cref="ConfirmSave" /> should be called to indicate that the event has been successfully committed to the store.</remarks>
        protected internal void AddPendingEvent(IEvent e)
        {
            pendingEvents.Add(e);
        }

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
                    @"No command of type {0} is accepted by the {1} aggregate. If you wish to add support for this command, either:\n
    1) add the following to {1}:

          public void EnactCommand({0} command)
          {{
              RecordEvent( /* NEW_EVENT */ );
          }}

    or 

    2) create an implementation of ICommandHandler<{1}, {0}>.",
                    command.GetType(),
                    GetType()));
        }

        /// <summary>
        /// Confirms that a save operation has been successfully completed and that the aggregate should move all pending events to its event history.
        /// </summary>
        public void ConfirmSave()
        {
            pendingEvents.TransferTo(eventHistory);
        }
    }
}