// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Api.Tests.Infrastructure;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Recipes;
using Moq;
using NUnit.Framework;
using Sample.Domain.Api.Controllers;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Api.Tests
{
    [TestFixture]
    public class CommandDispatchTests
    {
        // this is a shim to make sure that the Sample.Domain.Api assembly is loaded into the AppDomain, otherwise Web API won't discover the controller type
        private static object workaround = typeof (OrderApiController);

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            TestSetUp.InitializeEventStore();
        }

        [SetUp]
        public void SetUp()
        {
            Command<Order>.AuthorizeDefault = (order, command) => true;
        }

        [Test]
        public async Task Posting_command_JSON_applies_a_command_with_the_specified_name_to_an_aggregate_with_the_specified_id()
        {
            var order = new Order(Guid.NewGuid(),
                                  new Order.CustomerInfoChanged { CustomerName = "Joe" });
            await order.SaveToEventStore();
            var json = new AddItem
            {
                Quantity = 5,
                Price = 19.99m,
                ProductName = "Bag o' Treats"
            }.ToJson();

            var request = new HttpRequestMessage(HttpMethod.Post, string.Format("http://contoso.com/orders/{0}/additem", order.Id))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var testApi = new TestApi<Order>();
            var client = testApi.GetClient();

            var response = client.SendAsync(request).Result;

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedOrder = await new SqlEventSourcedRepository<Order>().GetLatest(order.Id);

            updatedOrder.Items.Single().Quantity.Should().Be(5);
        }

        [Test]
        public async Task Posting_an_invalid_command_does_not_affect_the_aggregate_state()
        {
            var order = new Order(Guid.NewGuid(),
                                  new Order.CustomerInfoChanged { CustomerName = "Joe" },
                                  new Order.Fulfilled());
            await order.SaveToEventStore();
            var json = new AddItem
            {
                Quantity = 5,
                Price = 19.99m,
                ProductName = "Bag o' Treats"
            }.ToJson();

            var request = new HttpRequestMessage(HttpMethod.Post, string.Format("http://contoso.com/orders/{0}/additem", order.Id))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var testApi = new TestApi<Order>();
            var client = testApi.GetClient();

            var response = client.SendAsync(request).Result;

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var updatedOrder = await new SqlEventSourcedRepository<Order>().GetLatest(order.Id);

            updatedOrder.Items.Count().Should().Be(0);
        }

        [Test]
        public async Task Posting_an_invalid_create_command_returns_400_Bad_request()
        {
            var testApi = new TestApi<Order>();
            var response = await testApi.GetClient()
                                        .PostAsJsonAsync(string.Format("http://contoso.com/orders/createorder/{0}", Any.Guid()), new object());

            response.ShouldFailWith(HttpStatusCode.BadRequest);
        }

        [Test]
        public void Posting_command_JSON_to_a_nonexistent_aggregate_returns_404_Not_found()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, string.Format("http://contoso.com/orders/{0}/cancel", Guid.NewGuid()))
            {
                Content = new StringContent(new Cancel().ToJson(), Encoding.UTF8, "application/json")
            };

            var client = new TestApi<Order>().GetClient();

            var response = client.SendAsync(request).Result;

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Posting_unauthorized_command_JSON_returns_403_Forbidden()
        {
            var order = new Order(Guid.NewGuid(),
                                  new Order.CustomerInfoChanged { CustomerName = "Joe" },
                                  new Order.Fulfilled());
            await order.SaveToEventStore();

            Command<Order>.AuthorizeDefault = (o, command) => false;
            var request = new HttpRequestMessage(HttpMethod.Post, string.Format("http://contoso.com/orders/{0}/cancel", order.Id))
            {
                Content = new StringContent(new Cancel().ToJson(), Encoding.UTF8, "application/json")
            };

            var client = new TestApi<Order>().GetClient();

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task Posting_a_command_that_causes_a_concurrency_error_returns_409_Conflict()
        {
            var order = new Order(Guid.NewGuid(), new Order.CustomerInfoChanged { CustomerName = "Joe" });

            var testApi = new TestApi<Order>();
            var repository = new Mock<IEventSourcedRepository<Order>>();
            repository.Setup(r => r.GetLatest(It.IsAny<Guid>()))
                      .Returns(Task.FromResult(order));
            repository.Setup(r => r.Save(It.IsAny<Order>()))
                      .Throws(new ConcurrencyException("oops!", new IEvent[0], new Exception("inner oops")));
            testApi.Container.Register(c => repository.Object);

            var client = testApi.GetClient();

            var response = await client.PostAsJsonAsync(string.Format("http://contoso.com/orders/{0}/additem", order.Id), new { Price = 3m, ProductName = Any.Word() });

            response.ShouldFailWith(HttpStatusCode.Conflict);
        }

        [Test]
        public async Task Posting_a_constructor_command_creates_a_new_aggregate_instance()
        {
            var testApi = new TestApi<Order>();

            var orderId = Any.Guid();

            var response = await testApi.GetClient()
                                        .PostAsJsonAsync(string.Format("http://contoso.com/orders/createorder/{0}", orderId), new CreateOrder(Any.FullName()));

            response.ShouldSucceed(HttpStatusCode.Created);
        }

        [Test]
        public async Task Posting_a_second_constructor_command_with_the_same_aggregate_id_results_in_a_409_Conflict()
        {
            // arrange
            var testApi = new TestApi<Order>();

            var orderId = Any.Guid();
            await testApi.GetClient()
                         .PostAsJsonAsync(string.Format("http://contoso.com/orders/createorder/{0}", orderId), new CreateOrder(Any.FullName()));

            // act
            var response = await testApi.GetClient()
                                        .PostAsJsonAsync(string.Format("http://contoso.com/orders/createorder/{0}", orderId), new CreateOrder(Any.FullName()));

            // assert
            response.ShouldFailWith(HttpStatusCode.Conflict);
        }

        [Test]
        public async Task An_ETag_header_is_applied_to_the_command()
        {
            var order = new Order(Guid.NewGuid(),
                                  new Order.CustomerInfoChanged { CustomerName = "Joe" });
            await order.SaveToEventStore();
            var json = new AddItem
            {
                Quantity = 5,
                Price = 19.99m,
                ProductName = "Bag o' Treats"
            }.ToJson();
            
            var etag = new EntityTagHeaderValue("\"" + Any.Guid() + "\"");

            Func<HttpRequestMessage> createRequest = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, string.Format("http://contoso.com/orders/{0}/additem", order.Id))
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                request.Headers.IfNoneMatch.Add(etag);
                return request;
            };

            var testApi = new TestApi<Order>();
            var client = testApi.GetClient();

            // act: send the request twice
            var response1 = await client.SendAsync(createRequest());
            var response2 = await client.SendAsync(createRequest());

            // assert
            response1.ShouldSucceed(HttpStatusCode.OK);
            response2.ShouldFailWith(HttpStatusCode.NotModified);

            var updatedOrder = await new SqlEventSourcedRepository<Order>().GetLatest(order.Id);
            updatedOrder.Items.Single().Quantity.Should().Be(5);
        }
    }
}
