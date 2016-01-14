// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Its.Domain.Serialization;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Its.Domain.ServiceBus
{
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    public class ServiceBusCommandQueueSender : IEventHandler,
                                                IEventHandlerBinder
    {
        private readonly ISubject<IScheduledCommand> messageSubject = new Subject<IScheduledCommand>();
        private readonly ISubject<Exception> exceptionSubject = new Subject<Exception>();
        private readonly ServiceBusSettings settings;

        private QueueClient queueClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusCommandQueueSender"/> class.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">queueClient</exception>
        public ServiceBusCommandQueueSender(ServiceBusSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            this.settings = settings;

            MessageDeliveryOffsetFromCommandDueTime = TimeSpan.FromMinutes(2);

#if DEBUG
            exceptionSubject.Subscribe(ex => Debug.WriteLine("ServiceBusCommandQueueSender error: " + ex));
#endif
        }

        public TimeSpan MessageDeliveryOffsetFromCommandDueTime { get; set; }

        public ISubject<IScheduledCommand> Messages
        {
            get
            {
                return messageSubject;
            }
        }

        IEnumerable<IEventHandlerBinder> IEventHandler.GetBinders()
        {
            return new[] { this };
        }

        /// <summary>
        /// Subscribes the specified handler to the event bus.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="bus">The bus.</param>
        public IDisposable SubscribeToBus(object handler, IEventBus bus)
        {
            queueClient = CreateQueueClient(settings);

            return new CompositeDisposable
            {
                bus.Events<IScheduledCommandEvent>().Subscribe(c => Enqueue(c)),
                exceptionSubject.Subscribe(ex => bus.PublishErrorAsync(new EventHandlingError(ex, handler))),
                Disposable.Create(() => queueClient.Close())
            };
        }
        
        internal static QueueClient CreateQueueClient(ServiceBusSettings settings)
        {
            return settings.CreateQueueClient(
                "ScheduledCommands",
                q =>
                {
                    q.SupportOrdering = true;
                    q.RequiresSession = true;
                    q.LockDuration = TimeSpan.FromMinutes(5);
                    q.MaxDeliveryCount = (int) (TimeSpan.FromDays(45).Ticks / q.LockDuration.Ticks);
                    q.EnableDeadLetteringOnMessageExpiration = false;
                });
        }

        private async Task Enqueue(IScheduledCommandEvent scheduledCommandEvent)
        {
            var message = new BrokeredMessage(scheduledCommandEvent.ToJson())
            {
                 SessionId = scheduledCommandEvent.AggregateId.ToString()
            };

            if (scheduledCommandEvent.DueTime != null)
            {
                message.ScheduledEnqueueTimeUtc = scheduledCommandEvent.DueTime.Value.UtcDateTime.Add(MessageDeliveryOffsetFromCommandDueTime);
            }

            messageSubject.OnNext(scheduledCommandEvent);

            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            {
                await queueClient.SendAsync(message);
            }
        }
    }
}
