using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Recipes;
using Moq;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class ConfigurationTests
    {
        [SetUp]
        public void SetUp()
        {
            Command<Order>.AuthorizeDefault = (order, command) => true;
        }

        [Test]
        public void Global_Configuration_uses_the_InProcessEventBus_Instance()
        {
#pragma warning disable 612,618
            Configuration.Global.EventBus.Should()
#pragma warning restore 612,618
                         .NotBeNull()
                         .And
                         .BeSameAs(InProcessEventBus.Instance);
        }

        [Test]
        public void New_Configuration_instances_do_not_use_the_InProcessEventBus_Instance()
        {
            new Configuration().EventBus.Should()
                               .NotBeNull()
                               .And
                               .NotBeSameAs(InProcessEventBus.Instance);
        }

        [Test]
        public void Configuration_can_specify_a_dependency_resolver_which_is_then_used_when_resolving_handlers()
        {
            var container = new PocketContainer();
            container.Register(c => "hello");

            var configuration = new Configuration()
                .UseSqlEventStore()
                .UseDependency(resolve: resolve => (IEventBus) resolve(typeof (EventBusWithDependencies)))
                .UseDependency<IEnumerable<string>>(_ => new[] { "hello" });

            var bus = configuration.EventBus;

            bus.Should().BeOfType<EventBusWithDependencies>();

            var busWithDependencies = bus as EventBusWithDependencies;

            busWithDependencies.StringValues.Single().Should().Be("hello");
        }

        [Test]
        public void UseDependencies_can_be_used_to_set_dependencies_using_an_application_owned_container()
        {
            var applicationsContainer = new PocketContainer()
                .Register<IPaymentService>(_ => new CreditCardPaymentGateway(chargeLimit: 1));

            var configuration = new Configuration()
                .UseDependencies(type =>
                {
                    if (applicationsContainer.Any(reg => reg.Key == type))
                    {
                        return () => applicationsContainer.Resolve(type);
                    }

                    return null;
                });

            using (ConfigurationContext.Establish(configuration))
            {
                var order = new Order(new CreateOrder(Any.FullName()))
                    .Apply(new AddItem
                    {
                        Price = 5m,
                        ProductName = Any.Word()
                    })
                    .Apply(new Ship())
                    .Apply(new ChargeAccount
                    {
                        AccountNumber = Any.PositiveInt().ToString()
                    });

                order.Events()
                     .Last()
                     .Should()
                     .BeOfType<Order.PaymentConfirmed>();
            }
        }

        [Test]
        public void UseDependency_can_be_used_to_override_default_implementations()
        {
            var mock = new Mock<IReservationService>().Object;

            var configuration = new Configuration().UseDependency(_ => mock);

            configuration.ReservationService.Should().BeSameAs(mock);
        }

        [Test]
        public void When_UseDependency_returns_null_then_a_default_implementation_is_used()
        {
            var configuration = new Configuration().UseDependency<IReservationService>(_ => null);

            configuration.ReservationService.GetType().Name.Should().Be("NoReservations");
        }

        public class EventBusWithDependencies : InProcessEventBus
        {
            private readonly IEnumerable<string> stringValues;

            public EventBusWithDependencies(IEnumerable<string> stringValues)
            {
                if (stringValues == null)
                {
                    throw new ArgumentNullException("stringValues");
                }
              
                this.stringValues = stringValues;
            }

            public IEnumerable<string> StringValues
            {
                get
                {
                    return stringValues;
                }
            }
        }
    }
}