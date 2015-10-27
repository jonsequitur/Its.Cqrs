// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain;
using Its.Validation;
using Sample.Domain.Ordering.Commands;

namespace Sample.Domain.Ordering
{
    public partial class Order
    {
        public void EnactCommand(CreateOrder create)
        {
            RecordEvent(new Created
            {
                CustomerName = create.CustomerName,
                CustomerId = create.CustomerId
            });
            RecordEvent(new CustomerInfoChanged
            {
                CustomerName = create.CustomerName
            });
        }

        public void EnactCommand(AddItem addItem)
        {
            RecordEvent(new ItemAdded
            {
                Price = addItem.Price,
                ProductName = addItem.ProductName,
                Quantity = addItem.Quantity
            });
        }

        public void EnactCommand(ChangeCustomerInfo command)
        {
            RecordEvent(new CustomerInfoChanged
            {
                CustomerName = command.CustomerName,
                Address = command.Address,
                PhoneNumber = command.PhoneNumber,
                PostalCode = command.PostalCode,
                RegionOrCountry = command.RegionOrCountry
            });
        }

        public void EnactCommand(ChangeFufillmentMethod change)
        {
            RecordEvent(new FulfillmentMethodSelected
            {
                FulfillmentMethod = change.FulfillmentMethod
            });
        }

        public class OrderCancelCommandHandler : ICommandHandler<Order, Cancel>
        {
            private readonly ICommandScheduler<CustomerAccount> scheduler;

            public OrderCancelCommandHandler(
                ICommandScheduler<CustomerAccount> scheduler)
            {
                if (scheduler == null)
                {
                    throw new ArgumentNullException("scheduler");
                }
                this.scheduler = scheduler;
            }

            public async Task EnactCommand(Order order, Cancel cancel)
            {
                var cancelled = new Cancelled();

                order.RecordEvent(cancelled);

                var command = new NotifyOrderCanceled
                {
                    OrderNumber = order.OrderNumber
                };

                await scheduler.Schedule(
                    order.CustomerId, 
                    command, 
                    deliveryDependsOn: cancelled);
            }

            public async Task HandleScheduledCommandException(Order order, CommandFailed<Cancel> command)
            {
                // Tests depend on this being an empty handler - don't modify.
                Debug.WriteLine("[HandleScheduledCommandException] " + command.ToLogString());
            }
        }

        public void EnactCommand(Deliver cancel)
        {
            RecordEvent(new Delivered());
            RecordEvent(new Fulfilled());
        }

        public void EnactCommand(ProvideCreditCardInfo command)
        {
            RecordEvent(new CreditCardInfoProvided
            {
                CreditCardCvv2 = command.CreditCardCvv2,
                CreditCardExpirationMonth = command.CreditCardExpirationMonth,
                CreditCardExpirationYear = command.CreditCardExpirationYear,
                CreditCardName = command.CreditCardName,
                CreditCardNumber = command.CreditCardNumber
            });
        }

   

        public void HandleCommandValidationFailure(ChargeCreditCard command, ValidationReport validationReport)
        {
            if (validationReport.Failures.All(failure => failure.IsRetryable()))
            {
                // this will terminate further attempts
                RecordEvent(new CreditCardChargeRejected());
            }
            else
            {
                ThrowCommandValidationException(command, validationReport);
            }
        }

        public class OrderChargeCreditCardHandler : ICommandHandler<Order, ChargeCreditCard>
        {
            public async Task EnactCommand(Order order, ChargeCreditCard command)
            {
                order.RecordEvent(new CreditCardCharged
                {
                    Amount = command.Amount
                });
            }

            public async Task HandleScheduledCommandException(Order order, CommandFailed<ChargeCreditCard> command)
            {
                if (command.NumberOfPreviousAttempts < 3)
                {
                    command.Retry(after: command.Command.ChargeRetryPeriod);
                }
                else
                {
                    order.RecordEvent(new Cancelled
                    {
                        Reason = "Final credit card charge attempt failed."
                    });
                }
            }
        }

        public void EnactCommand(ChargeCreditCardOn command)
        {
            ScheduleCommand(new ChargeCreditCard
            {
                Amount = command.Amount,
                PaymentId = command.PaymentId,
                ChargeRetryPeriod = command.ChargeRetryPeriod
            }, command.ChargeDate);
        }

        public void EnactCommand(SpecifyShippingInfo command)
        {
            RecordEvent(new ShippingMethodSelected
            {
                Address = command.Address,
                City = command.City,
                StateOrProvince = command.StateOrProvince,
                Country = command.Country,
                DeliverBy = command.DeliverBy,
                RecipientName = command.RecipientName,
            });
        }

        public void EnactCommand(Place command)
        {
            RecordEvent(new Placed(orderNumber: Guid.NewGuid().ToString().Substring(0, 8)));
        }

        public void EnactCommand(ShipOn command)
        {
            ScheduleCommand(new Ship
            {
                ShipmentId = command.ShipmentId
            }, command.ShipDate);
        }

        public void EnactCommand(RenameEvent command)
        {
            pendingRenames.Add(new EventMigrations.Rename(command.sequenceNumber, command.newName));
        }

        public class OrderShipCommandHandler : ICommandHandler<Order, Ship>
        {
            public async Task HandleScheduledCommandException(Order order, CommandFailed<Ship> command)
            {
                Debug.WriteLine("OrderShipCommandHandler.HandleScheduledCommandException");

                if (command.Exception is CommandValidationException)
                {
                    if (order.IsCancelled)
                    {
                        order.RecordEvent(new ShipmentCancelled());
                    }

                    if (order.IsShipped)
                    {
                        command.Cancel();
                    }
                }
            }

            public async Task EnactCommand(Order order, Ship command)
            {
                   Debug.WriteLine("OrderShipCommandHandler.EnactCommand");

                order.RecordEvent(new Shipped
                {
                    ShipmentId = command.ShipmentId
                });
            }
        }

        public void EnactCommand(ConfirmPayment command)
        {
            RecordEvent(new PaymentConfirmed
            {
                PaymentId = command.PaymentId
            });
        }

        public class ChargeAccountHandler : ICommandHandler<Order, ChargeAccount>
        {
            private readonly IPaymentService paymentService;

            public ChargeAccountHandler(IPaymentService paymentService)
            {
                if (paymentService == null)
                {
                    throw new ArgumentNullException("paymentService");
                }
                this.paymentService = paymentService;
            }

            public async Task EnactCommand(Order aggregate, ChargeAccount command)
            {
                try
                {
                    var paymentId = await paymentService.Charge(aggregate.Balance);

                    aggregate.RecordEvent(new PaymentConfirmed
                    {
                        PaymentId = paymentId
                    });
                }
                catch (InvalidOperationException)
                {
                    aggregate.RecordEvent(new ChargeAccountChargeRejected());
                }
            }

            public async Task HandleScheduledCommandException(Order order, CommandFailed<ChargeAccount> command)
            {
            }
        }
    }
}
