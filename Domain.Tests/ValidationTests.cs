// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using Its.Validation.Configuration;
using NUnit.Framework;
using Test.Domain.Ordering;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class ValidationTests
    {
        [SetUp]
        public void SetUp()
        {
            // disable authorization
            Command<Order>.AuthorizeDefault = (o, c) => true;
        }

        [Test]
        public void Command_applicability_can_be_validated_by_the_command_class()
        {
            var cancel = new Cancel();

            var order = new Order();

            order.IsValidTo(cancel).Should().Be(true);

            order.Apply(new Deliver());

            order.IsValidTo(cancel).Should().Be(false);
        }

        [Test]
        public void ReferTo_can_be_used_to_specify_a_mitigating_command()
        {
            var validate = Validate.That<AddItem>(t => t.Price >= 0).WithMitigation(
                ReferTo.Command<AddItem>());

            var report = validate.Execute(new AddItem { Price = -1 });

            report.Failures.Single().Result<CommandReference>()
                .CommandName.Should().Be("AddItem");
        }

        [Test]
        public void ReferTo_can_be_used_to_specify_a_field_on_a_mitigating_command()
        {
            var validate = Validate.That<AddItem>(t => t.Price >= 0).WithMitigation(
                ReferTo.Command<AddItem>(c => c.Price));

            var report = validate.Execute(new AddItem { Price = -1 });

            report.Failures.Single().Result<CommandReference>()
                .CommandField.Should().Be("Price");
        }

        [Test]
        public void IsValidTo_throws_if_the_caller_is_unauthorized()
        {
            Command<Order>.AuthorizeDefault = (o, c) => false;

            Action validate = () => new Order().IsValidTo(new Cancel());

            validate.ShouldThrow<CommandAuthorizationException>();
        }

        [Test]
        public void Validate_throws_if_the_caller_is_unauthorized()
        {
            Command<Order>.AuthorizeDefault = (o, c) => false;

            Action validate = () => new Order().Validate(new Cancel());

            validate.ShouldThrow<CommandAuthorizationException>();
        }
    }
}
