// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Its.Validation;
using Its.Validation.Configuration;

namespace Sample.Domain.Ordering
{
    public partial class Order
    {
        public static readonly IValidationRule<Order> NotCancelled =
            Validate.That<Order>(o => !o.IsCancelled)
                    .WithErrorMessage("The order has been cancelled.");

        public static readonly IValidationRule<Order> NotFulfilled =
            Validate.That<Order>(o => !o.IsFulfilled)
                    .WithErrorMessage((evalidation, order) => "The order has already been fulfilled.");
        
        public static readonly IValidationRule<Order> NotShipped =
            Validate.That<Order>(o => !o.IsShipped)
                    .WithErrorMessage((evaluation, order) => "The order has already been shipped.");

        public static readonly IValidationRule<Order> HasBeenShipped =
            Validate.That<Order>(o => o.IsShipped)
                    .WithErrorMessage((evaluation, order) => "The order has not yet been shipped.");

        public static IValidationRule<Order> BalanceIsAtLeast(decimal amount)
        {
            return Validate.That<Order>(order => order.Balance >= amount)
                           .WithErrorMessage((evaluation, order) =>
                                             string.Format("The amount paid ({0}) cannot exceed the order balance ({1})",
                                                           amount,
                                                           order.Balance));
        }

        public static readonly IValidationRule<Order> FulfillmentInfoIsProvided =
            Validate.That<Order>(o => !string.IsNullOrWhiteSpace(o.CustomerName))
                    .WithErrorMessage("Please provide the recipient's name.");

        private static readonly ValidationRule<Order> PaymentInfoIsNotNull =
            Validate.That<Order>(o => o.PaymentInfo != null)
                    .WithErrorMessage("You must provide payment information.");

        public static readonly IValidationRule<Order> PaymentInfoIsProvided =
            new ValidationPlan<Order>
            {
                PaymentInfoIsNotNull,
                Validate.That<Order>(o => CreditCardInfo.IsValid.Check(((ICreditCardInfo) o.PaymentInfo)))
                        .When(PaymentInfoIsNotNull)
            };

        public static readonly IValidationRule<Order> DeliveryInfoIsProvided =
            new ValidationPlan<Order>
            {
                PaymentInfoIsNotNull,
                Validate.That<Order>(o => o.DeliveryMethod != null).WithErrorMessage("You must specify a delivery method.")
            };

        public static readonly IValidationRule<Order> AlwaysValid = Validate.That<Order>(o => true);
    }
}