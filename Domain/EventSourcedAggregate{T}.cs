// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Its.Validation;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     Represents an aggregate whose state can be persisted as a sequence of events.
    /// </summary>
    public abstract class EventSourcedAggregate<T> : EventSourcedAggregate where T : EventSourcedAggregate<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourcedAggregate{T}"/> class.
        /// </summary>
        /// <param name="id">The aggregate's unique id.</param>
        protected EventSourcedAggregate(Guid? id = null) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourcedAggregate{T}"/> class by applying the specified command.
        /// </summary>
        /// <param name="createCommand">The create command.</param>
        protected EventSourcedAggregate(ConstructorCommand<T> createCommand) : base(createCommand.AggregateId)
        {
            createCommand.ApplyTo((dynamic)this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourcedAggregate{T}"/> class.
        /// </summary>
        /// <param name="id">The aggregate's unique id.</param>
        /// <param name="eventHistory">The event history.</param>
        protected EventSourcedAggregate(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
        {
           BuildUpStateFromEventHistory();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourcedAggregate{T}"/> class.
        /// </summary>
        /// <param name="snapshot">A snapshot of the aggregate's built-up state.</param>
        /// <param name="eventHistory">The event history.</param>
        /// <remarks>After building up its internal state from the snapshot, the constructor that calls into this constructor should call <see cref="BuildUpStateFromEventHistory" />.</remarks>
        protected EventSourcedAggregate(ISnapshot snapshot, IEnumerable<IEvent> eventHistory = null) : base(snapshot, eventHistory)
        {
        }

        protected void BuildUpStateFromEventHistory()
        {
            foreach (var @event in EventHistory.OfType<IEvent<T>>())
            {
                @event.Update((T) this);
            }
        }

        /// <summary>
        ///     Records an event, updating the aggregate's state and adding the event to PendingEvents.
        /// </summary>
        /// <remarks>It is not necessary to specify the AggregateId or SequenceNumber properties on the recorded event. The <see cref="EventSourcedAggregate" /> class handles this.</remarks>
        protected IEvent<T> RecordEvent(IEvent<T> e)
        {
            AddPendingEvent(e);
            e.Update((T)this);
            return e;
        }

        /// <summary>
        /// Schedules a command for asynchronous and, optionally, deferred delivery.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="due">The time when the command should be delivered. If this is null, the scheduler will deliver it as soon as possible.</param>
        /// <exception cref="System.ArgumentNullException">command</exception>
        protected void ScheduleCommand<TCommand>(TCommand command,
                                                 DateTimeOffset? due = null)
            where TCommand : class, ICommand<T>
        {
            Task.Run(() => ScheduleCommandAsync(command, due)).Wait();
        }

        /// <summary>
        /// Schedules a command for asynchronous and, optionally, deferred delivery.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="due">The time when the command should be delivered. If this is null, the scheduler will deliver it as soon as possible.</param>
        /// <exception cref="System.ArgumentNullException">command</exception>
        protected async Task ScheduleCommandAsync<TCommand>(TCommand command,
                                                            DateTimeOffset? due = null)
            where TCommand : class, ICommand<T>
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            var scheduled = CommandScheduler.CreateScheduledCommand<TCommand, T>(
                Id,
                command,
                due);

            var commandScheduledEvent = scheduled.ToEvent();

            if (Configuration.Current.IsUsingCommandSchedulerPipeline())
            {
                scheduled.DeliveryPrecondition = new CommandPrecondition
                {
                    AggregateId = Id,
                    ETag = commandScheduledEvent.ETag
                };
                await Configuration.Current.CommandScheduler<T>().Schedule(scheduled);
            }

            RecordEvent(commandScheduledEvent);
        }

        public void EnactCommand(Annotate<T> command)
        {
            RecordEvent(new Annotated<T>(command.Message));
        }
    }
}
