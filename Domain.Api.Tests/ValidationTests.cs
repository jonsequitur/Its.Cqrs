// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Its.Domain.Api.Serialization;
using Microsoft.Its.Domain.Api.Tests.Infrastructure;
using Microsoft.Its.Domain.Tests;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Test.Domain.Ordering;
using Test.Domain.Ordering.Domain.Api.Controllers;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.Its.Domain.Api.Tests
{
    [TestFixture]
    [DisableCommandAuthorization]
    [UseInMemoryCommandScheduling]
    [UseInMemoryEventStore]
    public class ValidationTests
    {
        static ValidationTests()
        {
            // this is a shim to make sure that the Test.Domain.Ordering.Api assembly is loaded into the AppDomain, otherwise Web API won't discover the controller type
            var controller = new OrderApiController();
        }

        [Test]
        public void Command_properties_can_be_validated()
        {
            var order = new Order(Guid.NewGuid())
                .Apply(new ChangeCustomerInfo { CustomerName = "Joe" })
                .Apply(new Deliver())
                .SavedToEventStore();

            var httpClient = new TestApi<Order>().GetClient();

            var result = httpClient.PostAsync(
                $"http://contoso.com/orders/{order.Id}/additem/validate",
                new JsonContent(new AddItem
                {
                    Price = 1m,
                    Quantity = 1,
                    ProductName = "Widget"
                })).Result;

            result.ShouldSucceed();

            var content = result.Content.ReadAsStringAsync().Result;

            Console.WriteLine(content);

            content.Should().Contain("\"Failures\":[{\"Message\":\"The order has already been fulfilled.\"");
        }

        [Ignore("Scenario under development")]
        [Test]
        public void Client_validation_hints_can_be_requested_from_the_API_in_order_for_validations_to_be_performed_by_the_client()
        {
            // TODO: (Client_validation_hints_can_be_requested_from_the_API_in_order_for_validations_to_be_performed_by_the_client) 
            var httpClient = new TestApi<Order>().GetClient();

            var result = httpClient.GetAsync(
                "http://contoso.com/orders/commands/additem/rules").Result;

            result.ShouldSucceed();

            var json = result.Content.ReadAsStringAsync().Result;

            Console.WriteLine(json);

            dynamic jobject = JObject.Parse(json);

            // example:
            //"[{"validator":"required","message":"An offer title is required","params":{},"valid":false}]"

            Assert.Fail("Test not written yet.");
        }
    }
}
