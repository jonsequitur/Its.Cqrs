// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.ServiceBus
{
#if !RecipesProject
    /// <summary>
    /// Receives messages from a service bus queue and attempts to trigger corresponding commands via the command scheduler.
    /// </summary>
    [DebuggerStepThrough]
    [ExcludeFromCodeCoverage]
#endif
    public class ServiceBusCommandQueueReceiver : IMessageSessionAsyncHandlerFactory,
                                                  IDisposable
    {
        private readonly ServiceBusSettings settings;
        private readonly ISchedulerClockTrigger clockTrigger;
        private readonly Subject<Exception> exceptionSubject = new Subject<Exception>();
        private QueueClient queueClient;
        private readonly Subject<IScheduledCommand> messageSubject = new Subject<IScheduledCommand>();
        private readonly Func<CommandSchedulerDbContext> createCommandSchedulerDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusCommandQueueReceiver"/> class.
        /// </summary>
        /// <param name="settings">The service bus settings.</param>
        /// <param name="clockTrigger">The command clockTrigger.</param>
        /// <exception cref="System.ArgumentNullException">
        /// settings
        /// or
        /// clockTrigger
        /// </exception>
        public ServiceBusCommandQueueReceiver(
            ServiceBusSettings settings, 
            ISchedulerClockTrigger clockTrigger, 
            Func<CommandSchedulerDbContext> createCommandSchedulerDbContext)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            if (clockTrigger == null)
            {
                throw new ArgumentNullException("clockTrigger");
            }
            if (createCommandSchedulerDbContext == null)
            {
                throw new ArgumentNullException("createCommandSchedulerDbContext");
            }
            this.settings = settings;
            this.clockTrigger = clockTrigger;
            this.createCommandSchedulerDbContext = createCommandSchedulerDbContext;

#if DEBUG
            exceptionSubject
                .Where(ex => !(ex is OperationCanceledException))
                .Subscribe(ex => Debug.WriteLine("ServiceBusCommandQueueReceiver error: " + ex));
#endif
        }

        /// <summary>
        /// Creates an instance of the handler factory.
        /// </summary>
        /// <returns>
        /// The created instance.
        /// </returns>
        /// <param name="session">The message session.</param>
        /// <param name="message">The message.</param>
        IMessageSessionAsyncHandler IMessageSessionAsyncHandlerFactory.CreateInstance(MessageSession session, BrokeredMessage message)
        {
            return new SessionHandler(
                msg => messageSubject.OnNext(msg),
                ex => exceptionSubject.OnNext(ex),
                clockTrigger,
                createCommandSchedulerDbContext);
        }

        /// <summary>
        /// Releases the resources associated with the handler factory instance.
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        void IMessageSessionAsyncHandlerFactory.DisposeInstance(IMessageSessionAsyncHandler handler)
        {
        }

        /// <summary>
        /// Gets an observable sequence of the messages that the receiver dequeues from the service bus.
        /// </summary>
        public Subject<IScheduledCommand> Messages
        {
            get
            {
                return messageSubject;
            }
        }

        public Task StartReceivingMessages()
        {
            queueClient = ServiceBusCommandQueueSender.CreateQueueClient(settings);

            var options = new SessionHandlerOptions
            {
                AutoComplete = false,
                MessageWaitTimeout = TimeSpan.FromSeconds(3)
            };

            if (settings.MaxConcurrentSessions != null)
            {
                options.MaxConcurrentSessions = settings.MaxConcurrentSessions.Value;
            }

            options.ExceptionReceived += (sender, e) => exceptionSubject.OnNext(e.Exception);

            return queueClient.RegisterSessionHandlerFactoryAsync(this, options);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            var client = queueClient;
            if (client != null)
            {
                client.Close();
            }
        }

        private class SessionHandler : IMessageSessionAsyncHandler
        {
            private readonly ISchedulerClockTrigger clockTrigger;
            private readonly Action<IScheduledCommand> onMessage;
            private readonly Action<Exception> onError;
            private readonly Func<CommandSchedulerDbContext> createCommandSchedulerDbContext;

            public SessionHandler(
                Action<IScheduledCommand> onMessage,
                Action<Exception> onError,
                ISchedulerClockTrigger clockTrigger, 
                Func<CommandSchedulerDbContext> createCommandSchedulerDbContext)
            {
                this.onMessage = onMessage;
                this.onError = onError;
                this.clockTrigger = clockTrigger;
                this.createCommandSchedulerDbContext = createCommandSchedulerDbContext;
            }

            /// <summary>
            /// Raises an event that occurs when a message has been brokered.
            /// </summary>
            /// <returns>
            /// The task object representing the asynchronous operation.
            /// </returns>
            /// <param name="session">The message session.</param>
            /// <param name="message">The brokered message.</param>
            public async Task OnMessageAsync(MessageSession session, BrokeredMessage message)
            {
                var json = message.GetBody<string>();

                var @event = json.FromJsonTo<ServiceBusScheduledCommand>();
                @event.BrokeredMessage = message;

                onMessage(@event);

                var result = await clockTrigger.Trigger(commands => commands.Due(@event.DueTime)
                                                                         .Where(c => c.AggregateId == @event.AggregateId));

                if (!result.FailedCommands.Any())
                {
                    if (result.SuccessfulCommands.Any())
                    {
                        Debug.WriteLine("ServiceBusCommandQueueReceiver: completing on success: " + @event.AggregateId);
                        await message.CompleteAsync();
                        return;
                    }

                    using (var db = createCommandSchedulerDbContext())
                    {
                        // if the command was already applied, we can complete the message. its job is done.
                        if (db.ScheduledCommands
                              .Where(cmd => cmd.AppliedTime != null || cmd.FinalAttemptTime != null)
                              .Where(cmd => cmd.SequenceNumber == @event.SequenceNumber)
                              .Any(cmd => cmd.AggregateId == @event.AggregateId))
                        {
                            Debug.WriteLine("ServiceBusCommandQueueReceiver: completing because command was previously applied: " + @event.AggregateId);

                            await message.CompleteAsync();
                        }
                    }
                }
            }

            /// <summary>
            /// Raises an event that occurs when the session has been asynchronously closed.
            /// </summary>
            /// <returns>
            /// The task object representing the asynchronous operation.
            /// </returns>
            /// <param name="session">The closed session.</param>
            public async Task OnCloseSessionAsync(MessageSession session)
            {
            }

            /// <summary>
            /// Raises an event that occurs when the session has been lost.
            /// </summary>
            /// <returns>
            /// The task object representing the asynchronous operation.
            /// </returns>
            /// <param name="exception">The exception that occurred that caused the lost session.</param>
            public Task OnSessionLostAsync(Exception exception)
            {
                return Task.Run(() => onError(exception));
            }
        }

        /// <summary>
        /// Serves as a deserialization target for commands queued to the service bus. These commands contain the full command JSON, but ServiceBusScheduledCommand is actually only used to look up the command from the command clockTrigger database.
        /// </summary>
        private class ServiceBusScheduledCommand : Event, IScheduledCommand
        {
            public DateTimeOffset? DueTime { get; private set; }

            public EventHasBeenRecordedPrecondition DeliveryPrecondition { get; set; }

            IPrecondition IScheduledCommand.DeliveryPrecondition => DeliveryPrecondition;

            [JsonIgnore]
            public ScheduledCommandResult Result { get; set; }

            private int NumberOfPreviousAttempts { get; set; }

            [JsonIgnore]
            IClock IScheduledCommand.Clock { get; }

            int IScheduledCommand.NumberOfPreviousAttempts => this.NumberOfPreviousAttempts;

            public BrokeredMessage BrokeredMessage { get; internal set; }
        }
    }
}
