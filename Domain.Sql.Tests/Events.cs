// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Transactions;
using Microsoft.Its.Recipes;
using Pocket;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public static class Events
    {
#pragma warning disable CS0436 // Type conflicts with imported type
        private static readonly PocketContainer container = new PocketContainer()
#pragma warning restore CS0436 // Type conflicts with imported type
            .AutoMockInterfacesAndAbstractClasses()
            .Register(c => Recipes.Any.Decimal(.01m))
            .Register(c => Recipes.Any.Bool())
            .Register(c => Recipes.Any.PositiveInt())
            .Register(c => Recipes.Any.Word());

        private static readonly Func<IEvent>[] events = {
            () => container.Resolve<Order.Cancelled>(),
            () => container.Resolve<Order.Created>(),
            () => container.Resolve<Order.CreditCardCharged>(),
            () => container.Resolve<Order.CustomerInfoChanged>(),
            () => container.Resolve<Order.Delivered>(),
            () => container.Resolve<Order.Fulfilled>(),
            () => container.Resolve<Order.FulfillmentMethodSelected>(),
            () => container.Resolve<Order.ItemAdded>(),
            () => container.Resolve<Order.ItemRemoved>(),
            () => container.Resolve<Order.Misdelivered>(),
            () => container.Resolve<Order.Paid>(),
            () => container.Resolve<Order.Placed>(),
            () => container.Resolve<Order.ShipmentCancelled>(),
            () => container.Resolve<Order.Shipped>(),
            () => container.Resolve<Order.ShippingMethodSelected>(),
            () => container.Resolve<CustomerAccount.Created>(),
            () => container.Resolve<CustomerAccount.EmailAddressChanged>(),
            () => container.Resolve<CustomerAccount.OrderShipConfirmationEmailSent>(),
            () => container.Resolve<CustomerAccount.RequestedNoSpam>(),
            () => container.Resolve<CustomerAccount.RequestedSpam>(),
            () => container.Resolve<CustomerAccount.MarketingEmailSent>(),
            () => container.Resolve<CustomerAccount.UserNameAcquired>()
        };

        public static IEvent Any()
        {
            return events.RandomSequence(1).Select(e => e()).Single();
        }

        public static long Write(
            int howMany,
            Func<int, IEvent> createEvent = null,
            Func<EventStoreDbContext> createEventStore = null)
        {
            createEvent = createEvent ?? (i => new Order.ItemAdded
            {
                SequenceNumber = i,
                AggregateId = Recipes.Any.Guid(),
                Price = 1.99m,
                ProductName = Recipes.Any.Paragraph(3),
                Quantity = 1
            });

            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var eventStore = createEventStore.IfNotNull()
                                                    .Then(c => c())
                                                    .Else(() => EventStoreDbContext()))
            {
                Enumerable.Range(1, howMany).ForEach(i =>
                {
                    var e = createEvent(i);

                    e.IfTypeIs<Event>()
                     .ThenDo(ev =>
                     {
                         if (ev.AggregateId == Guid.Empty)
                         {
                             ev.AggregateId = Guid.NewGuid();
                         }

                         if (ev.SequenceNumber == 0)
                         {
                             ev.SequenceNumber = i;
                         }
                     });

                    var storableEvent = e.ToStorableEvent();

                    eventStore.Events.Add(storableEvent);
                });

                eventStore.SaveChanges();

                return eventStore.Events.Max(e => e.Id);
            }
        }
    }
}
