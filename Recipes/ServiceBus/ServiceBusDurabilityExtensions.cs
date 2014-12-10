// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Transactions;
using Microsoft.Its.Domain.Serialization;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Its.Domain.ServiceBus
{
    public static class ServiceBusDurabilityExtensions
    {
        public static IEventHandler UseServiceBusForDurability(
            this object handler,
            ServiceBusSettings settings,
            string handlerCatchupAlias = null,
            Configuration configuration = null)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            configuration = configuration ?? Configuration.Global;
            var queues = new Dictionary<Type, QueueClient>();
            var binders = EventHandler.GetBinders(handler);

            // create queues
            var subscriptions = binders.Select(binder => SubscribeToReceiveMessages(binder, handler, settings, configuration, queues))
                                       .ToArray();

            configuration.RegisterDisposable(new CompositeDisposable(subscriptions));

            // intercept events for the consequenter and queue them on Service Bus
            var wrappedHandler = handler.WrapAll(SimulatedSend ?? SendMessage(queues));

            return wrappedHandler;
        }

        private static Handle<IEvent> SendMessage(Dictionary<Type, QueueClient> queues)
        {
            return (e, next) =>
            {
                var json = new StoredEvent
                {
                    AggregateName = AggregateType.EventStreamName(e.AggregateType()),
                    EventName = e.EventName(),
                    Body = e.ToJson(),
                    AggregateId = e.AggregateId,
                    SequenceNumber = e.SequenceNumber,
                    Timestamp = e.Timestamp
                }.ToJson();

                // prevent enlistment in the ambient database transaction
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var message = new BrokeredMessage(json)
                    {
                        MessageId = e.AggregateId + ":" + e.SequenceNumber
                    };
                    queues[e.GetType()].Send(message);
                }

                // we deliberately do not call next, which would call the consequenter implementation. we call that on the other end, when the message has been received from the bus
            };
        }

        public static IDisposable Simulate()
        {
            var subject = new Subject<IEvent>();
            SimulatedReceive = subject;
            SimulatedSend = (e, handler) => subject.OnNext(e);

            return Disposable.Create(() =>
            {
                SimulatedSend = null;
                SimulatedReceive = null;
            });
        }

        private static Handle<IEvent> SimulatedSend;

        private static IObservable<IEvent> SimulatedReceive;

        private static IDisposable SubscribeToReceiveMessages<THandler>(
            IEventHandlerBinder binder,
            THandler handler,
            ServiceBusSettings settings,
            Configuration configuration,
            Dictionary<Type, QueueClient> queues) where THandler : class
        {
            Type eventType = ((dynamic) binder).EventType;

            var queueName = string.Format("{0}_on_{1}.{2}",
                                          EventHandler.Name(handler),
                                          eventType.AggregateTypeForEventType().Name,
                                          eventType.EventName());

            var bus = new InProcessEventBus(errorSubject: (ISubject<EventHandlingError>) configuration.EventBus.Errors);
            var eventBusSubscription = binder.SubscribeToBus(handler, bus);

            var receive = SimulatedReceive;
            if (receive != null)
            {
                var receiveSubscription = receive.Subscribe(e => bus.PublishAsync(e).Subscribe(_ => { }, ex => bus.PublishErrorAsync(new EventHandlingError(ex, @event: e))));
                return new CompositeDisposable(eventBusSubscription,
                                               receiveSubscription);
            }

            var queueClient = settings.CreateQueueClient(
                queueName,
                settings.ConfigureQueue);

            // starting listening on the queue for incoming events
            queueClient.OnMessage(msg =>
            {
                var storedEvent = msg.GetBody<string>()
                                     .FromJsonTo<StoredEvent>();

                var @event = Serializer.DeserializeEvent(
                    aggregateName: storedEvent.AggregateName,
                    eventName: storedEvent.EventName,
                    body: storedEvent.Body,
                    aggregateId: storedEvent.AggregateId,
                    sequenceNumber: storedEvent.SequenceNumber,
                    timestamp: storedEvent.Timestamp);

                bus.PublishAsync(@event).Subscribe(
                    _ => msg.Complete(),
                    ex => bus.PublishErrorAsync(new EventHandlingError(ex, @event: @event)));
            });

            queues[((dynamic) binder).EventType] = queueClient;

            return new CompositeDisposable(eventBusSubscription, Disposable.Create(queueClient.Close));
        }

        private struct StoredEvent
        {
            public string AggregateName;
            public string EventName;
            public string Body;
            public Guid AggregateId;
            public long SequenceNumber;
            public DateTimeOffset Timestamp;
        }
    }
}