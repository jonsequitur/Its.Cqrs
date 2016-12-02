// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    [DisableCommandAuthorization]
    public class ConstructorCommandTests
    {
        [Test]
        public void A_constructor_command_can_be_used_to_create_a_new_aggregate_instance()
        {
            var customerName = Any.FullName();
            var order = new Order(new CreateOrder(customerName));

            order.CustomerName.Should().Be(customerName);
            order.Version.Should().Be(2);
        }

        [Test]
        public void ConstructorCommand_AggregateId_is_used_to_specify_the_new_instances_Id()
        {
            var id = Any.Guid();
            var createOrder = new CreateOrder(id, Any.Paragraph(2));
            var order = new Order(createOrder);

            order.Id.Should().Be(id);
        }

        [Test]
        public void When_a_constructor_command_fails_validation_then_it_throws()
        {
            Action apply = () => new Order(new CreateOrder(""));

            apply.ShouldThrow<CommandValidationException>();
        }

        [Test]
        public void When_a_constructor_command_fails_authorization_then_it_throws()
        {
            Command<Order>.AuthorizeDefault = (o, c) => false;

            Action apply = () => new Order(new CreateOrder(Any.CamelCaseName()));

            apply.ShouldThrow<CommandAuthorizationException>();
        }

        [Test]
        public void Constructor_commands_cannot_be_used_on_aggregates_that_have_prior_events()
        {
            var order = new Order();
            order.Apply(new AddItem
            {
                Price = 1,
                ProductName = Any.CamelCaseName()
            });

            Action apply = () =>
                           order.Apply(new CreateOrder(Any.CamelCaseName()));

            apply.ShouldThrow<ConcurrencyException>();
        }
    }
}
