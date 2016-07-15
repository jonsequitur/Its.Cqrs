// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FluentAssertions;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;
using TraceListener = Its.Log.Instrumentation.TraceListener;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public class CommandSchedulerPipelineTests
    {
        private CompositeDisposable disposables;
        private Configuration configuration;

        [SetUp]
        public void SetUp()
        {
            disposables = new CompositeDisposable();
            // disable authorization
            Command<Order>.AuthorizeDefault = (o, c) => true;
            Command<CustomerAccount>.AuthorizeDefault = (o, c) => true;

            disposables.Add(VirtualClock.Start());

            configuration = new Configuration()
                .UseInMemoryCommandScheduling()
                .UseInMemoryEventStore()
                .TraceScheduledCommands();

            disposables.Add(ConfigurationContext.Establish(configuration));
            disposables.Add(configuration);

            Formatter.ListExpansionLimit = 100;
            Console.WriteLine("Command.KnownTypes: \n" + Command.KnownTypes.Select(t => t.FullName).ToLogString());
            Console.WriteLine("Command.KnownTargetTypes: \n" + Command.KnownTargetTypes.Select(t => t.FullName).ToLogString());
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public async Task CommandSchedulerPipeline_can_be_used_to_specify_command_scheduler_behavior_on_schedule()
        {
            var scheduled = false;
            var configuration = new Configuration()
                .UseInMemoryEventStore()
                .AddToCommandSchedulerPipeline<Order>(
                    schedule: async (cmd, next) => scheduled = true);

            var scheduler = configuration.CommandScheduler<Order>();

            await scheduler.Schedule(Any.Guid(), new CreateOrder(Any.FullName()));

            scheduled.Should().BeTrue();
        }

        [Test]
        public async Task CommandSchedulerPipeline_can_be_used_to_specify_command_scheduler_behavior_on_deliver()
        {
            var delivered = false;
            configuration
                .AddToCommandSchedulerPipeline<Order>(
                    deliver: async (cmd, next) => delivered = true);

            var scheduler = configuration.CommandDeliverer<Order>();

            await scheduler.Deliver(new CommandScheduled<Order>());

            delivered.Should().BeTrue();
        }

        [Test]
        public async Task CommandSchedulerPipeline_can_be_composed_using_several_calls_prior_to_the_scheduler_being_resolved()
        {
            var checkpoints = new List<string>();

            configuration
                .AddToCommandSchedulerPipeline<Order>(
                    schedule: async (cmd, next) =>
                    {
                        checkpoints.Add("two");
                        await next(cmd);
                        checkpoints.Add("three");
                    })
                .AddToCommandSchedulerPipeline<Order>(
                    schedule: async (cmd, next) =>
                    {
                        checkpoints.Add("one");
                        await next(cmd);
                        checkpoints.Add("four");
                    });

            var scheduler = configuration.CommandScheduler<Order>();

            await scheduler.Schedule(Any.Guid(), new CreateOrder(Any.FullName()));

            checkpoints.Should()
                       .ContainInOrder("one", "two", "three", "four")
                       .And
                       .HaveCount(4);
        }

        [Test]
        public async Task CommandSchedulerPipeline_can_be_composed_using_additional_calls_after_the_scheduler_has_been_resolved()
        {
            var checkpoints = new List<string>();

            configuration
                .AddToCommandSchedulerPipeline<Order>(
                    schedule: async (cmd, next) =>
                    {
                        checkpoints.Add("two");
                        await next(cmd);
                        checkpoints.Add("three");
                    });

            // make sure to trigger a resolve
            var scheduler = configuration.CommandScheduler<Order>();

            configuration
                .AddToCommandSchedulerPipeline<Order>(
                    schedule: async (cmd, next) =>
                    {
                        checkpoints.Add("one");
                        await next(cmd);
                        checkpoints.Add("four");
                    });

            scheduler = configuration.CommandScheduler<Order>();

            await scheduler.Schedule(Any.Guid(), new CreateOrder(Any.FullName()));

            checkpoints.Should()
                       .ContainInOrder("one", "two", "three", "four")
                       .And
                       .HaveCount(4);
        }

        [Test]
        public async Task When_CommandSchedulerPipeline_tracing_is_enabled_then_by_default_trace_output_goes_to_SystemDiagnosticsTrace()
        {
            configuration.UseInMemoryCommandTargetStore();

            var log = new List<string>();
            using (LogTraceOutputTo(log))
            {
                await configuration.CommandScheduler<NonEventSourcedCommandTarget>()
                                   .Schedule(Any.Guid(), new CreateCommandTarget(Any.Word()));
            }

            log.Count.Should().Be(4);
            log.Should().ContainSingle(e => e.Contains("[Scheduling]") &&
                                            e.Contains("NonEventSourcedCommandTarget.CreateCommandTarget"));
            log.Should().ContainSingle(e => e.Contains("[Scheduled]") &&
                                            e.Contains("NonEventSourcedCommandTarget.CreateCommandTarget"));
            log.Should().ContainSingle(e => e.Contains("[Delivering]") &&
                                            e.Contains("NonEventSourcedCommandTarget.CreateCommandTarget"));
            log.Should().ContainSingle(e => e.Contains("[Delivered]") &&
                                            e.Contains("NonEventSourcedCommandTarget.CreateCommandTarget"));
        }

        [Test]
        public async Task CommandSchedulerPipeline_tracing_can_specify_tracing_behaviors()
        {
            var onSchedulingWasCalled = false;
            var onScheduledWasCalled = false;
            var onDeliveringWasCalled = false;
            var onDeliveredWasCalled = false;

            configuration.TraceScheduledCommands(
                onScheduling: _ => onSchedulingWasCalled = true,
                onScheduled: _ => onScheduledWasCalled = true,
                onDelivering: _ => onDeliveringWasCalled = true,
                onDelivered: _ => onDeliveredWasCalled = true);

            await configuration.CommandScheduler<Order>()
                               .Schedule(Any.Guid(), new CreateOrder(Any.FullName()));

            onSchedulingWasCalled.Should().BeTrue();
            onScheduledWasCalled.Should().BeTrue();
            onDeliveringWasCalled.Should().BeTrue();
            onDeliveredWasCalled.Should().BeTrue();
        }

        [Test]
        public async Task When_pipeline_tracing_is_enabled_twice_with_different_behavior_then_it_both_behaviors_are_applied()
        {
            var commandsScheduled = new List<IScheduledCommand>();
            var commandsDelivered = new List<IScheduledCommand>();
            configuration.TraceScheduledCommands()
                         .TraceScheduledCommands(onScheduled: cmd => { commandsScheduled.Add(cmd); },
                                                 onDelivered: cmd => { },
                                                 onScheduling: cmd => { },
                                                 onDelivering: cmd => { })
                         .TraceScheduledCommands(onDelivered: cmd => { commandsDelivered.Add(cmd); },
                                                 onScheduled: cmd => { },
                                                 onScheduling: cmd => { },
                                                 onDelivering: cmd => { });

            var log = new List<string>();
            using (LogTraceOutputTo(log))
            {
                // send a command
                await configuration.CommandScheduler<Order>().Schedule(Any.Guid(), new CreateOrder(Any.FullName()));
            }

            log.Should().ContainSingle(e => e.Contains("[Scheduled]"));
            log.Should().ContainSingle(e => e.Contains("[Scheduling]"));
            log.Should().ContainSingle(e => e.Contains("[Delivered]"));
            log.Should().ContainSingle(e => e.Contains("[Delivering]"));
            commandsScheduled.Count.Should().Be(1);
            commandsDelivered.Count.Should().Be(1);
        }

        [Test]
        public async Task When_pipeline_tracing_is_enabled_multiple_times_with_default_behavior_then_it_does_not_produce_redundant_trace_output()
        {
            configuration.TraceScheduledCommands()
                         .TraceScheduledCommands()
                         .TraceScheduledCommands();

            var log = new List<string>();
            using (LogTraceOutputTo(log))
            {
                // send a command
                await configuration.CommandScheduler<Order>().Schedule(Any.Guid(), new CreateOrder(Any.FullName()));
            }

            log.Should().ContainSingle(e => e.Contains("[Scheduled]"));
            log.Should().ContainSingle(e => e.Contains("[Scheduling]"));
            log.Should().ContainSingle(e => e.Contains("[Delivered]"));
            log.Should().ContainSingle(e => e.Contains("[Delivering]"));
        }

        [Test]
        public async Task CommandSchedulerPipelineInitializer_Initialize_is_idempotent()
        {
            var commandsScheduled = new List<IScheduledCommand>();

            // initialize twice
            new AnonymousCommandSchedulerPipelineInitializer(cmd => commandsScheduled.Add(cmd))
                .Initialize(configuration);

            new AnonymousCommandSchedulerPipelineInitializer(cmd => commandsScheduled.Add(cmd))
                .Initialize(configuration);

            // send a command
            await configuration.CommandScheduler<Order>().Schedule(Any.Guid(), new CreateOrder(Any.FullName()));

            commandsScheduled.Count.Should().Be(1);
        }

        public class AnonymousCommandSchedulerPipelineInitializer : CommandSchedulerPipelineInitializer
        {
            private readonly Action<IScheduledCommand> onSchedule;

            public AnonymousCommandSchedulerPipelineInitializer(Action<IScheduledCommand> onSchedule)
            {
                if (onSchedule == null)
                {
                    throw new ArgumentNullException(nameof(onSchedule));
                }
                this.onSchedule = onSchedule;
            }

            protected override void InitializeFor<TAggregate>(Configuration configuration)
            {
                configuration.AddToCommandSchedulerPipeline<TAggregate>(
                    schedule: async (cmd, next) =>
                    {
                        onSchedule(cmd);
                        await next(cmd);
                    });
            }

            public IEnumerable<IScheduledCommand> ScheduledCommands { get; private set; }

            public IEnumerable<IScheduledCommand> DeliveredCommands { get; private set; }
        }

        public IDisposable LogTraceOutputTo(List<string> log)
        {
            var listener = new TraceListener();
            Trace.Listeners.Add(listener);

            return new CompositeDisposable
            {
                Log.Events().Subscribe(e => log.Add(e.ToLogString())),
                Disposable.Create(() => Trace.Listeners.Remove(listener))
            };
        }
    }
}