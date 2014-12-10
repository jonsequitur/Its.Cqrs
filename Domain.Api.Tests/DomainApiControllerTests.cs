using System;
using System.Data.Entity;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Api.Tests.Infrastructure;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Testing;
using NUnit.Framework;
using Sample.Domain.Ordering;

namespace Microsoft.Its.Domain.Api.Tests
{
    [TestFixture]
    public class DomainApiControllerTests
    {
        [SetUp]
        public void SetUp()
        {
            Command<Order>.AuthorizeDefault = (order, command) => true;
            EventStoreDbContext.NameOrConnectionString = @"Data Source=(localdb)\v11.0; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsCommandScheduler";
            Database.SetInitializer(new EventStoreDatabaseInitializer<EventStoreDbContext>());
        }

        [Test]
        public async Task ApplyBatch_can_accept_an_array_commands()
        {
            var repository = new SqlEventSourcedRepository<Order>(new FakeEventBus());
            var order = new Order();
            repository.Save(order);
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

            order = repository.GetLatest(order.Id);

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