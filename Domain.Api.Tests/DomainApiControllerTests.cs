// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Api.Tests.Infrastructure;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Tests;
using NUnit.Framework;
using Test.Domain.Ordering;
using Test.Domain.Ordering.Domain.Api.Controllers;

namespace Microsoft.Its.Domain.Api.Tests
{
    [TestFixture]
    [DisableCommandAuthorization]
    [UseInMemoryCommandScheduling]
    [UseInMemoryEventStore]
    public class DomainApiControllerTests
    {
        static DomainApiControllerTests()
        {
            // this is a shim to make sure that the Test.Domain.Ordering.Api assembly is loaded into the AppDomain, otherwise Web API won't discover the controller type
            var controller = new OrderApiController();
        }

        [Test]
        public async Task ApplyBatch_can_accept_an_array_of_commands()
        {
            var order = new Order().SavedToEventStore();

            var json = new[]
            {
                new
                {
                    AddItem = new
                    {
                        Quantity = 1,
                        Price = 1,
                        ProductName = "Sprocket"
                    }
                },
                new
                {
                    AddItem = new
                    {
                        Quantity = 1,
                        Price = 2,
                        ProductName = "Cog"
                    }
                }
            }.ToJson();

            var testApi = new TestApi<Order>();
            var client = testApi.GetClient();

            var request = new HttpRequestMessage(HttpMethod.Post, $"http://contoso.com/orders/{order.Id}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            response.ShouldSucceed();

            order = await Configuration.Current.Repository<Order>().GetLatest(order.Id);

            order.Items.Count.Should().Be(2);
            order.Balance.Should().Be(3);
        }
    }
}