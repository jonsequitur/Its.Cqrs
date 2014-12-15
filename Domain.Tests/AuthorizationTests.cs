// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using FluentAssertions;
using Microsoft.Its.Domain.Authorization;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;
using AuthorizationPolicy = Microsoft.Its.Domain.Authorization.AuthorizationPolicy;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class AuthorizationTests
    {
        [Test]
        public void AuthorizationPolicy_For_returns_the_same_instance_via_generic_and_non_generic_overloads()
        {
            var viaGeneric = AuthorizationPolicy.For<Order, AddItem, Customer>();
            var viaNonGeneric = AuthorizationPolicy.For(typeof (Order), typeof (AddItem), typeof (Customer));

            viaGeneric.Should().Be(viaNonGeneric);
        }

        [Test]
        public void AuthorizationPolicy_For_returns_an_instance_based_on_the_actual_rather_than_declared_type_of_the_principal()
        {
            AuthorizationFor<Customer>.ToApply<Cancel>.ToA<Order>
                                      .Requires((a, b, c) => true);
            IPrincipal iprincipal = new Customer();
            var customer = new Customer();
            var cancel = new Cancel();
            var order = new Order();

            iprincipal.IsAuthorizedTo(cancel, order).Should().Be(true);
            customer.IsAuthorizedTo(cancel, order).Should().Be(true);
        }

        [Test]
        public void AuthorizationFor_throws_when_command_is_not_applicable_to_resource()
        {
            Action configure = () => AuthorizationFor<Customer>.ToApply<Cancel>.ToA<Customer>
                                                               .Requires((a, b, c) => true);

            configure.ShouldThrow<ArgumentException>()
                .And
                .Message.Should().Be("Command type Sample.Domain.Ordering.Commands.Cancel is not applicable to resource type Sample.Domain.Customer");
        }

        [Test]
        public void When_no_authorization_rules_have_been_configured_for_a_given_command_then_checks_are_unauthorized()
        {
            var customer = new Customer();
            var account = new CustomerAccount();
            var addEmail = new ChangeEmailAddress();

            customer.IsAuthorizedTo(addEmail, account)
                    .Should().Be(false);
        }

        [Test]
        public void IsDenied_overrides_other_authorizations()
        {
            AuthorizationFor<Customer>.ToApply<SuspendAccount>.ToA<CustomerAccount>.IsDenied();
            AuthorizationFor<Customer>.ToApplyAnyCommand.ToA<CustomerAccount>.Requires((c1, c2) => c1.Id == c2.Id);
           
            Guid customerId = Guid.NewGuid();
            var customerAccount = new CustomerAccount(customerId);
            var customerPrincipal = new Customer
            {
                Id = customerId,
                IsAuthenticated = true
            };

            customerPrincipal.IsAuthorizedTo(new ChangeEmailAddress(), customerAccount)
                             .Should().BeTrue();

            customerPrincipal.IsAuthorizedTo(new SuspendAccount(), customerAccount)
                             .Should().BeFalse();
        }

        [Test]
        public void IsDenied_does_not_need_to_specify_a_resource_type_for_the_command()
        {
            AuthorizationFor<Customer>.ToApply<UnsuspendAccount>.IsDenied();
            AuthorizationFor<Customer>.ToApplyAnyCommand.ToA<CustomerAccount>.Requires((c1, c2) => c1.Id == c2.Id);

            Guid customerId = Guid.NewGuid();
            var customerAccount = new CustomerAccount(customerId);
            var customerPrincipal = new Customer
            {
                Id = customerId,
                IsAuthenticated = true
            };

            customerPrincipal.IsAuthorizedTo(new ChangeEmailAddress(), customerAccount)
                             .Should().BeTrue();

            customerPrincipal.IsAuthorizedTo(new UnsuspendAccount(), customerAccount)
                             .Should().BeFalse();
        }

        [Test]
        public void ToApplyAnyCommand_allows_an_authorization_rule_to_be_declared_for_all_commands_for_a_given_resource_type()
        {
            var principals = new List<IPrincipal>();
            var resources = new List<EventSourcedAggregate>();

            AuthorizationFor<Customer>.ToApplyAnyCommand.ToA<Order>
                                      .Requires((principal, resource) =>
                                      {
                                          principals.Add(principal);
                                          resources.Add(resource);
                                          return true;
                                      });

            var customer = new Customer();
            var order1 = new Order();
            var order2 = new Order();

            customer.IsAuthorizedTo(new Cancel(), order1)
                    .Should().BeTrue();
            customer.IsAuthorizedTo(new Place(), order2)
                    .Should().BeTrue();

            principals.Should().Contain(customer);
            resources.Should().Contain(order1);
            resources.Should().Contain(order2);
        }
    }
}