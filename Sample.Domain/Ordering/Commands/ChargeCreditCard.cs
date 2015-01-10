// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain;
using Newtonsoft.Json;

namespace Sample.Domain.Ordering.Commands
{
    public class ChargeCreditCard : Command<Order>
    {
        public ChargeCreditCard()
        {
            ChargeRetryPeriod = TimeSpan.FromHours(12);
        }

        [Range(.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public PaymentId PaymentId { get; set; }

        public override IValidationRule<Order> Validator
        {
            get
            {
                var chargeSuccessful = Validate.That<Order>(t =>
                {
                    try
                    {
                        PaymentId = CallPaymentService(this);
                        ETag = Guid.NewGuid().ToString();
                    }
                    catch (Exception)
                    {
                    }

                    return PaymentId != null;
                })
                                               .WithErrorMessage("Credit card charge failed.")
                                               .Retryable();

                var balanceIsAtLeastAmount = Order.BalanceIsAtLeast(Amount);
                var hasBeenShipped = Order.HasBeenShipped;

                return new ValidationPlan<Order>
                {
                    balanceIsAtLeastAmount,
                    hasBeenShipped,
                    chargeSuccessful.When(balanceIsAtLeastAmount, hasBeenShipped)
                };
            }
        }

        public TimeSpan ChargeRetryPeriod { get; set; }

        [JsonIgnore]
        public Func<ChargeCreditCard, PaymentId> CallPaymentService = cmd => new CreditCardPaymentGateway().Charge(cmd.Amount).Result;
    }
}
