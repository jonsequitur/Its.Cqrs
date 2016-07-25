// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using FluentAssertions;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using Moq;
using NUnit.Framework;
using Pocket;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    [DisableCommandAuthorization]
    public class ConfigurationTests
    {
        [Test]
        public void Root_Configuration_uses_the_InProcessEventBus_Instance()
        {
            ConfigurationContext.Current.Dispose();

            Configuration.Current
                         .EventBus
                         .Should()
                         .NotBeNull()
                         .And
                         .BeSameAs(InProcessEventBus.Instance);
        }

        [Test]
        public void New_Configuration_instances_do_not_use_the_InProcessEventBus_Instance()
        {
            new Configuration().EventBus
                               .Should()
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
                .UseInMemoryEventStore()
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

            Configuration.Current
                         .UseDependencies(type =>
                         {
                             if (applicationsContainer.Any(reg => reg.Key == type))
                             {
                                 return () => applicationsContainer.Resolve(type);
                             }

                             return null;
                         });

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

        [Test]
        public void UseDependency_can_be_used_to_override_default_implementations()
        {
            var mock = new Mock<IReservationService>().Object;

            var configuration = new Configuration().UseDependency(_ => mock);

            configuration.ReservationService().Should().BeSameAs(mock);
        }

        [Test]
        public void When_UseDependency_returns_null_then_a_default_implementation_is_used()
        {
            var configuration = new Configuration().UseDependency<IReservationService>(_ => null);

            configuration.ReservationService().GetType().Name.Should().Be("NoReservations");
        }

        [Test]
        public void RegisterForDisposal_can_be_used_to_specify_objects_that_should_be_disposed_when_the_Configuration_is_disposed()
        {
            var disposable = new BooleanDisposable();

            var configuration = new Configuration();

            configuration.RegisterForDisposal(disposable);

            disposable.IsDisposed.Should().BeFalse();

            configuration.Dispose();

            disposable.IsDisposed.Should().BeTrue();
        }

        [Test]
        public void Background_work_does_not_run_immediately_on_schedule()
        {
            var configuration = new Configuration();

            var started = false;

            configuration.QueueBackgroundWork(c => { started = true; });

            started.Should().BeFalse();
        }

        [Test]
        public void Background_work_cannot_not_be_started_more_than_once()
        {
            var configuration = new Configuration();

            var startCount = 0;

            configuration.QueueBackgroundWork(c => Interlocked.Increment(ref startCount));

            configuration.StartBackgroundWork();
            configuration.StartBackgroundWork();

            startCount.Should().Be(1);
        }

        [Test]
        public void Background_work_starts_when_StartBackgroundWork_is_called()
        {
            var configuration = new Configuration();

            var started = false;

            configuration.QueueBackgroundWork(c => { started = true; });

            configuration.StartBackgroundWork();

            started.Should().BeTrue();
        }

        [Test]
        public void Background_tasks_that_return_disposables_have_their_disposables_disposed_with_the_Configuration()
        {
            var configuration = new Configuration();
            var disposable = new BooleanDisposable();

            configuration.QueueBackgroundWork(c => disposable);

            configuration.StartBackgroundWork();

            disposable.IsDisposed.Should().BeFalse();

            configuration.Dispose();

            disposable.IsDisposed.Should().BeTrue();
        }

        public class EventBusWithDependencies : InProcessEventBus
        {
            private readonly IEnumerable<string> stringValues;

            public EventBusWithDependencies(IEnumerable<string> stringValues)
            {
                if (stringValues == null)
                {
                    throw new ArgumentNullException(nameof(stringValues));
                }
              
                this.stringValues = stringValues;
            }

            public IEnumerable<string> StringValues => stringValues;
        }
    }
}
