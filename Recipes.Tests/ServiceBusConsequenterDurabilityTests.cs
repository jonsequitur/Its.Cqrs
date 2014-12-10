using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Configuration;
using Microsoft.Its.Domain;
using Microsoft.Its.Domain.ServiceBus;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Tests;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Log.Instrumentation;
using Microsoft.Its.Recipes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Assert = NUnit.Framework.Assert;
using EventHandlingError = Microsoft.Its.Domain.EventHandlingError;
using TestContext = Microsoft.VisualStudio.TestTools.UnitTesting.TestContext;

namespace Microsoft.Its.Cqrs.Recipes.Tests
{
    [NUnit.Framework.Ignore("Deprecating functionality"), VisualStudio.TestTools.UnitTesting.Ignore]
    [TestClass, TestFixture]
    public class ServiceBusConsequenterDurabilityTests : EventStoreDbTest
    {
        private static ServiceBusSettings settings;
        private Domain.Configuration configuration;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            CommandSchedulerDbContext.NameOrConnectionString = @"Data Source=(localdb)\v11.0; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsCommandScheduler";

            Settings.Sources = new ISettingsSource[] { new ConfigDirectorySettings(@"c:\dev\.config") }.Concat(Settings.Sources);
            settings = Settings.Get<ServiceBusSettings>();
            settings.ConfigureQueue = queue => queue.AutoDeleteOnIdle = TimeSpan.FromHours(24);

#if !DEBUG
            new CommandSchedulerDbContext().Database.Delete();
#endif

            using (var readModels = new CommandSchedulerDbContext())
            {
                new CommandSchedulerDatabaseInitializer().InitializeDatabase(readModels);
            }

            Console.WriteLine(Environment.MachineName);
            Console.WriteLine(new DirectoryInfo(@"c:\dev\.config").GetFiles().Select(f => f.FullName).ToLogString());
            Settings.Sources = new ISettingsSource[] { new ConfigDirectorySettings(@"c:\dev\.config") }.Concat(Settings.Sources);
        }

        [TestFixtureSetUp]
        public void Initialize()
        {
            ClassInitialize(null);
        }

        [SetUp, TestInitialize]
        public override void SetUp()
        {
            base.SetUp();
            configuration = new Domain.Configuration();
        }

        [TearDown, TestCleanup]
        public override void TearDown()
        {
            base.TearDown();
            Settings.Reset();
            configuration.Dispose();
        }

        [Test]
        public async Task Catchup_can_be_used_with_intercepted_consequenters_to_queue_event_messages_on_the_service_bus()
        {
            var onCancelledCalled = new AsyncValue<bool>();
            var onCreatedCalled = new AsyncValue<bool>();
            var onDeliveredCalled = new AsyncValue<bool>();
            var anonymousConsequenterCalled = new AsyncValue<bool>();

            var consequenter1 = new TestConsequenter(
                onCancelled: e => onCancelledCalled.Set(true),
                onCreated: e => onCreatedCalled.Set(true),
                onDelivered: e => onDeliveredCalled.Set(true))
                .UseServiceBusForDurability(settings, configuration: configuration);

            var name = MethodBase.GetCurrentMethod().Name;
            var consequenter2 = Consequenter.Create<Order.Fulfilled>(e => anonymousConsequenterCalled.Set(true))
                                            .Named(name)
                                            .UseServiceBusForDurability(settings, configuration: configuration);

            using (var catchup = CreateReadModelCatchup<CommandSchedulerDbContext>(
                consequenter1,
                consequenter2))
            {
                Events.Write(1, i => new Order.Created());
                Events.Write(1, i => new Order.Delivered());
                Events.Write(1, i => new Order.Fulfilled());
                Events.Write(1, i => new Order.Cancelled());

                catchup.Run();

                // give the service bus messages times to be delivered
                Task.WaitAll(new Task[]
                {
                    onCancelledCalled,
                    onCreatedCalled,
                    onDeliveredCalled,
                    anonymousConsequenterCalled
                }, DefaultTimeout());

                onCancelledCalled.Result.Should().Be(true);
                onCreatedCalled.Result.Should().Be(true);
                onDeliveredCalled.Result.Should().Be(true);
                anonymousConsequenterCalled.Result.Should().Be(true);
            }
        }

        [Test]
        public void When_the_same_message_is_handled_by_multiple_consequenters_it_is_delivered_to_each_only_once()
        {
            var aggregateId = Any.Guid();
            var consequenter1WasCalled = new AsyncValue<bool>();
            var consequenter2WasCalled = new AsyncValue<bool>();
            var consequenter1CallCount = 0;
            var consequenter2CallCount = 0;
            var consequenter1 = Consequenter.Create<Order.Fulfilled>(e =>
            {
                if (e.AggregateId == aggregateId)
                {
                    Interlocked.Increment(ref consequenter1CallCount);
                    consequenter1WasCalled.Set(true);
                }
            })
                                            .Named(MethodBase.GetCurrentMethod().Name + "-1")
                                            .UseServiceBusForDurability(settings, configuration: configuration);
            var consequenter2 = Consequenter.Create<Order.Fulfilled>(e =>
            {
                if (e.AggregateId == aggregateId)
                {
                    Interlocked.Increment(ref consequenter2CallCount);
                    consequenter2WasCalled.Set(true);
                }
            })
                                            .Named(MethodBase.GetCurrentMethod().Name + "-2")
                                            .UseServiceBusForDurability(settings, configuration: configuration);

            using (var catchup = CreateReadModelCatchup<CommandSchedulerDbContext>(consequenter1, consequenter2))
            {
                Events.Write(1, i => new Order.Fulfilled { AggregateId = aggregateId });

                catchup.Run();

                // give the service bus messages times to be delivered
                Task.WaitAll(new Task[]
                {
                    consequenter1WasCalled,
                    consequenter2WasCalled
                }, DefaultTimeout());

                consequenter1CallCount.Should().Be(1);
                consequenter2CallCount.Should().Be(1);
            }
        }

        [NUnit.Framework.Ignore("Test not finished"), VisualStudio.TestTools.UnitTesting.Ignore]
        [Test]
        public void When_an_exception_is_thrown_by_the_consequenter_then_it_is_retried()
        {
            

            // TODO (When_an_exception_is_thrown_by_the_consequenter_then_it_is_retried) write test
            Assert.Fail("Test not written yet.");
        }

        [NUnit.Framework.Ignore("Test not finished"), VisualStudio.TestTools.UnitTesting.Ignore]
        [Test]
        public void Errors_during_catchup_result_in_retries()
        {
            var consequenter = Consequenter.Create<Order.CreditCardCharged>(e => { })
                                           .UseServiceBusForDurability(new ServiceBusSettings
                                           {
                                               ConnectionString = "this will never work"
                                           });

            var errors = new List<EventHandlingError>();
            var aggregateId = Any.Guid();
            Events.Write(1, i => new Order.CreditCardCharged
            {
                AggregateId = aggregateId
            });

            using (Domain.Configuration.Global.EventBus.Errors.Subscribe(errors.Add))
            using (var catchup = CreateReadModelCatchup<CommandSchedulerDbContext>(consequenter))
            {
                catchup.Run();

                errors.Should().Contain(e => e.AggregateId == aggregateId &&
                                             e.Exception.ToString().Contains("excelsior!"));
            }

            // TODO (Errors_during_catchup_result_in_retries) this would require a slightly different catchup mechanism or a way to retry events found in the EventHandlingErrors table
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public void Errors_during_event_handling_are_published_back_to_the_global_bus_on_delivery()
        {
            var consequenter = Consequenter.Create<Order.CreditCardCharged>(e => { throw new Exception("whoa that's bad..."); })
                                           .UseServiceBusForDurability(settings, configuration: configuration);

            var errors = new List<EventHandlingError>();
            var aggregateId = Any.Guid();
            Events.Write(1, i => new Order.CreditCardCharged
            {
                AggregateId = aggregateId
            });

            using (configuration.EventBus.Errors.Subscribe(errors.Add))
            using (var catchup = CreateReadModelCatchup<CommandSchedulerDbContext>(consequenter))
            {
                catchup.Run();

                errors.Should().Contain(e => e.AggregateId == aggregateId &&
                                             e.Exception.ToString().Contains("whoa that's bad..."));
            }
        }

        [Test]
        public void Durable_consequenters_can_be_simulated_for_unit_testing_and_receive_messages()
        {
            var consequenter1WasCalled = false;
            var consequenter2WasCalled = false;

            var uselessSettings = new ServiceBusSettings();

            using (ServiceBusDurabilityExtensions.Simulate())
            {
                var consequenter1 = Consequenter.Create<Order.Cancelled>(
                    e => { consequenter1WasCalled = true; });
                var consequenter2 = Consequenter.Create<Order.CreditCardCharged>(
                    e => { consequenter2WasCalled = true; });

                var bus = new InProcessEventBus();
                bus.Subscribe(
                    consequenter1.UseServiceBusForDurability(uselessSettings),
                    consequenter2.UseServiceBusForDurability(uselessSettings));

                bus.PublishAsync(new Order.Cancelled()).Wait();

                consequenter1WasCalled.Should().BeTrue();
                consequenter2WasCalled.Should().BeFalse();
            }
        }

        private TimeSpan DefaultTimeout()
        {
            if (Debugger.IsAttached)
            {
                return TimeSpan.FromMinutes(30);
            }
            return TimeSpan.FromSeconds(30);
        }

        public class AsyncValue<T>
        {
            private readonly TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            public void Set(T value)
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult(value);
                }
            }

            public TaskAwaiter<T> GetAwaiter()
            {
                return tcs.Task.GetAwaiter();
            }

            public T Result
            {
                get
                {
                    return tcs.Task.Result;
                }
            }

            public static implicit operator Task<T>(AsyncValue<T> asyncValue)
            {
                return asyncValue.tcs.Task;
            }
        }
    }
}