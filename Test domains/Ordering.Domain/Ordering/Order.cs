// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class Order : EventSourcedAggregate<Order>, IEventMigratingAggregate
    {
        private readonly IList<OrderItem> items = new List<OrderItem>();

        public Order(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
        {
        }

        public Order(Guid? id = null) : base(id)
        {
            RecordEvent(new Created
            {
                CustomerId = Guid.NewGuid(),
                ETag = null
            });
        }

        public Order(CreateOrder create) : base(create)
        {
        }

        public string CustomerName { get; private set; }

        public FulfillmentMethod FulfillmentMethod { get; private set; }

        public string OrderNumber { get; private set; }

        public bool IsFulfilled { get; private set; }
        
        public bool IsShipped { get; private set; }

        public bool IsCancelled { get; private set; }

        public bool IsPaid { get; }

        public IDeliveryMethod DeliveryMethod { get; private set; }

        public DateTimeOffset? MustBeDeliveredBy { get; set; }

        public IList<OrderItem> Items => items;

        public decimal Balance { get; private set; }

        public IPaymentInfo PaymentInfo { get; private set; }

        public Guid CustomerId { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string StateOrProvince { get; set; }

        public string Country { get; set; }

        public string RecipientName { get; set; }

        public string ShipmentId { get; set; }

        private readonly List<EventMigrations.Rename> pendingRenames = new List<EventMigrations.Rename>();

        IEnumerable<EventMigrations.Rename> IEventMigratingAggregate.PendingRenames =>
            pendingRenames;

        public override void ConfirmSave()
        {
            base.ConfirmSave();
            pendingRenames.Clear();
        }
    }
}
