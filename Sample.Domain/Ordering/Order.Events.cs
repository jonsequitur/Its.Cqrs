// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain;

namespace Sample.Domain.Ordering
{
    public partial class Order
    {
        public class Cancelled : Event<Order>
        {
            public string Reason { get; set; }

            public override void Update(Order order)
            {
                order.IsCancelled = true;
            }
        }

        public class Fulfilled : Event<Order>
        {
            public override void Update(Order order)
            {
                order.IsFulfilled = true;
            }
        }

        public class CreditCardCharged : Event<Order>
        {
            public decimal Amount { get; set; }

            public PaymentId PaymentId { get; set; }

            public override void Update(Order aggregate)
            {
            }
        }

        public class CustomerInfoChanged : Event<Order>
        {
            public string CustomerName { get; set; }

            public Optional<string> Address { get; set; }

            public Optional<string> PostalCode { get; set; }

            public Optional<string> RegionOrCountry { get; set; }

            public Optional<string> PhoneNumber { get; set; }

            public override void Update(Order order)
            {
                order.CustomerName = CustomerName;
            }
        }

        public class FulfillmentMethodSelected : Event<Order>
        {
            public FulfillmentMethod FulfillmentMethod { get; set; }

            public override void Update(Order order)
            {
                order.FulfillmentMethod = FulfillmentMethod;
            }
        }

        public class ItemAdded2 : ItemAdded
        {
        }

        public class ItemAdded : Event<Order>
        {
            public ItemAdded()
            {
                Quantity = 1;
            }

            public decimal Price { get; set; }

            public string ProductName { get; set; }

            public int Quantity { get; set; }

            public override void Update(Order order)
            {
                var existingItem = order.Items
                                        .SingleOrDefault(i => i.Price == Price &&
                                                              i.ProductName == ProductName);

                if (existingItem != null)
                {
                    existingItem.Quantity += Quantity;
                }
                else
                {
                    order.Items.Add(new OrderItem
                    {
                        Price = Price,
                        Quantity = Quantity,
                        ProductName = ProductName
                    });
                }

                order.Balance += (Price*Quantity);
            }
        }

        public class ItemRemoved : Event<Order>
        {
            public decimal Price { get; set; }

            public string ProductName { get; set; }

            public int Quantity { get; set; }

            public override void Update(Order order)
            {
                order.Items
                     .Single(i => i.Price == Price &&
                                  i.ProductName == ProductName)
                     .Quantity -= Quantity;
            }
        }

        public class Misdelivered : Event<Order>
        {
            public string Details { get; set; }

            public override void Update(Order order)
            {
            }
        }

        public class Delivered : Event<Order>
        {
            public override void Update(Order order)
            {
            }
        }

        public class CreditCardInfoProvided : Event<Order>, ICreditCardInfo
        {
            public override void Update(Order order)
            {
                order.PaymentInfo = new CreditCardInfo
                {
                    CreditCardNumber = CreditCardNumber,
                    CreditCardName = CreditCardName,
                    CreditCardCvv2 = CreditCardCvv2,
                    CreditCardExpirationMonth = CreditCardExpirationMonth,
                    CreditCardExpirationYear = CreditCardExpirationYear
                };
            }

            public string CreditCardNumber { get; set; }
            public string CreditCardName { get; set; }
            public string CreditCardCvv2 { get; set; }
            public string CreditCardExpirationMonth { get; set; }
            public string CreditCardExpirationYear { get; set; }
        }

        public class Placed : Event<Order>
        {
            public Placed(string orderNumber = null)
            {
                OrderNumber = orderNumber ?? Guid.NewGuid().ToString().Substring(1, 10);
            }

            public string OrderNumber { get; private set; }

            public IEnumerable<OrderItem> Items { get; private set; }

            public Guid CustomerId { get; set; }

            public decimal TotalPrice { get; private set; }

            public override void Update(Order aggregate)
            {
                Items = aggregate.Items.ToArray();
                TotalPrice = aggregate.Balance;
            }
        }

        public class Paid : Event<Order>
        {
            public decimal Amount { get; private set; }

            public Paid(decimal amount)
            {
                if (amount <= 0)
                {
                    throw new ArgumentException("Amount must be at least 0.");
                }
                Amount = amount;
            }

            public override void Update(Order order)
            {
                order.Balance -= Amount;
            }
        }

        public class ShippingMethodSelected : Event<Order>
        {
            public string Address { get; set; }
            public string City { get; set; }
            public string StateOrProvince { get; set; }
            public string Country { get; set; }
            public DateTimeOffset? DeliverBy { get; set; }
            public string RecipientName { get; set; }
            public string ServiceMethod { get; set; }
            public decimal Price { get; set; }
            public string Carrier { get; set; }

            public override void Update(Order aggregate)
            {
                aggregate.DeliveryMethod = new ShippingMethod
                {
                    Carrier = Carrier,
                    Price = Price,
                    ServiceMethod = ServiceMethod
                };

                aggregate.MustBeDeliveredBy = DeliverBy;
                aggregate.Address = Address;
                aggregate.City = City;
                aggregate.StateOrProvince = StateOrProvince;
                aggregate.Country = Country;
                aggregate.RecipientName = RecipientName;
            }
        }

        public class Shipped : Event<Order>
        {
            public override void Update(Order aggregate)
            {
                aggregate.IsShipped = true;
                aggregate.ShipmentId = ShipmentId;
            }

            public string ShipmentId { get; set; }
        }

        public class Created : Event<Order>
        {
            public string OrderNumber { get; set; }

            public string CustomerName { get; set; }

            public Guid CustomerId { get; set; }

            public override void Update(Order aggregate)
            {
                aggregate.CustomerName = CustomerName;
                aggregate.CustomerId = CustomerId;
                aggregate.OrderNumber = OrderNumber;
            }
        }

        public class ShipmentCancelled : Event<Order>
        {
            public override void Update(Order aggregate)
            {
            }
        }

        public class CreditCardChargeRejected : Event<Order>
        {
            public override void Update(Order aggregate)
            {
            }
        }

        public class ChargeAccountChargeRejected : Event<Order>
        {
            public override void Update(Order aggregate)
            {
            }
        }

        public class PaymentConfirmed : Event<Order>
        {
            public PaymentId PaymentId { get; set; }

            public override void Update(Order aggregate)
            {
            }
        }
    }
}
