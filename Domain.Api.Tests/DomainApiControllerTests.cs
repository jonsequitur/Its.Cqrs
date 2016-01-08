// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Api.Tests.Infrastructure;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.Tests;
using Microsoft.Its.Domain.Testing;
using NUnit.Framework;
using Sample.Domain.Api.Controllers;
using Sample.Domain.Ordering;

namespace Microsoft.Its.Domain.Api.Tests
{
    [TestFixture]
    public class DomainApiControllerTests : EventStoreDbTest
    {
        // this is a shim to make sure that the Sample.Domain.Api assembly is loaded into the AppDomain, otherwise Web API won't discover the controller type
        private static object workaround = typeof (OrderApiController);

        [SetUp]
        public void SetUp()
        {
            TestSetUp.InitializeEventStore();
        }

        [Test]
        public async Task ApplyBatch_can_accept_an_array_of_commands()
        {
            var repository = new SqlEventSourcedRepository<Order>(new FakeEventBus());
            var order = new Order();
            await repository.Save(order);
            
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

            var request = new HttpRequestMessage(HttpMethod.Post, string.Format("http://contoso.com/orders/{0}", order.Id))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            response.ShouldSucceed();

            order = await repository.GetLatest(order.Id);

            order.Items.Count.Should().Be(2);
            order.Balance.Should().Be(3);
        }
    }

    public class OrderController : DomainApiController<Order>
    {
        public OrderController(SqlEventSourcedRepository<Order> sqlEventSourcedRepository) : base(sqlEventSourcedRepository)
        {
        }
    }
}
