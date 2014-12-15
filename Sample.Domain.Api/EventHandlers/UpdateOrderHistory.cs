// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using Microsoft.Its.Domain;
using Sample.Domain.Api.ReadModels;
using Sample.Domain.Ordering;

namespace Sample.Domain.Api.EventHandlers
{
    /// <summary>
    /// Updates the order history read model in response to various events.
    /// </summary>
    public class UpdateOrderHistory :
        IUpdateProjectionWhen<Order.Placed>,
        IUpdateProjectionWhen<Order.Cancelled>,
        IUpdateProjectionWhen<Order.Misdelivered>,
        IUpdateProjectionWhen<Order.Delivered>,
        IUpdateProjectionWhen<Order.Shipped>
    {
        public void UpdateProjection(Order.Placed @event)
        {
            using (var db = new OrderHistoryDbContext())
            {
                var entry = new OrderHistoryEntry
                {
                    CustomerId = @event.CustomerId,
                    OrderId = @event.AggregateId,
                    OrderNumber = @event.OrderNumber,
                    TotalPrice = @event.TotalPrice,
                    Items = new List<OrderHistoryItem>(@event.Items.Select(i => new OrderHistoryItem
                    {
                        Id = Guid.NewGuid(),
                        Price = i.Price,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity
                    })),
                    PlacedOn = new DateTime(@event.Timestamp.Ticks)
                };
                db.Orders.AddOrUpdate(entry);
                db.SaveChanges();
            }
        }

        public void UpdateProjection(Order.Misdelivered @event)
        {
            using (var db = new OrderHistoryDbContext())
            {
                var entry = db.Orders.Single(o => o.OrderId == @event.AggregateId);
                entry.ErrorOn = new DateTime(@event.Timestamp.Ticks);
                db.SaveChanges();
            }
        }

        public void UpdateProjection(Order.Cancelled @event)
        {
            using (var db = new OrderHistoryDbContext())
            {
                var entry = db.Orders.Single(o => o.OrderId == @event.AggregateId);
                entry.CancelledOn = new DateTime(@event.Timestamp.Ticks);
                db.SaveChanges();
            }
        }

        public void UpdateProjection(Order.Shipped @event)
        {
            using (var db = new OrderHistoryDbContext())
            {
                var entry = db.Orders.Single(o => o.OrderId == @event.AggregateId);
                entry.ShippedOn = new DateTime(@event.Timestamp.Ticks);
                db.SaveChanges();
            }
        }

        public void UpdateProjection(Order.Delivered @event)
        {
            using (var db = new OrderHistoryDbContext())
            {
                var entry = db.Orders.Single(o => o.OrderId == @event.AggregateId);
                entry.DeliveredOn = new DateTime(@event.Timestamp.Ticks);
                db.SaveChanges();
            }
        }
    }
}