using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class CommandScheduler
    {
        internal static ICommandScheduler<TAggregate> Wrap<TAggregate>(
            this ICommandScheduler<TAggregate> scheduler,
            ScheduledCommandInterceptor<TAggregate> schedule = null,
            ScheduledCommandInterceptor<TAggregate> deliver = null)
            where TAggregate : IEventSourced
        {
            schedule = schedule ?? (async (c, next) => await next(c));
            deliver = deliver ?? (async (c, next) => await next(c));

            return Create<TAggregate>(
                async command => await schedule(command, async c => await scheduler.Schedule(c)),
                async command => await deliver(command, async c => await scheduler.Deliver(c)));
        }

        public static ICommandScheduler<TAggregate> Create<TAggregate>(
            Func<IScheduledCommand<TAggregate>, Task> schedule,
            Func<IScheduledCommand<TAggregate>, Task> deliver)
            where TAggregate : IEventSourced
        {
            return new AnonymousCommandScheduler<TAggregate>(
                schedule,
                deliver);
        }

        internal static ScheduledCommandInterceptor<TAggregate> Compose<TAggregate>(
            this IEnumerable<ScheduledCommandInterceptor<TAggregate>> pipeline)
            where TAggregate : IEventSourced
        {
            var delegates = pipeline.OrEmpty().ToArray();

            if (!delegates.Any())
            {
                return null;
            }

            return delegates.Aggregate(
                (first, second) =>
                    (async (command, next) =>
                    await first(command,
                                async c => await second(c,
                                                        async cc => await next(cc)))));
        }

        internal static async Task DeliverImmediatelyOnConfiguredScheduler<TAggregate>(
            IScheduledCommand<TAggregate> command,
            Configuration configuration)
            where TAggregate : class, IEventSourced
        {
            var scheduler = configuration.CommandScheduler<TAggregate>();
            await scheduler.Deliver(command);
        }

        internal static void DeliverIfPreconditionIsSatisfiedSoon<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand,
            Configuration configuration,
            int timeoutInMilliseconds = 10000)
            where TAggregate : class, IEventSourced
        {
            var eventBus = configuration.EventBus;

            var timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

            eventBus.Events<IEvent>()
                    .Where(
                        e => e.AggregateId == scheduledCommand.DeliveryPrecondition.AggregateId &&
                             e.ETag == scheduledCommand.DeliveryPrecondition.ETag)
                    .Take(1)
                    .Timeout(timeout)
                    .Subscribe(
                        e =>
                        {
                            Task.Run(() => DeliverImmediatelyOnConfiguredScheduler(scheduledCommand, configuration)).Wait();
                        },
                        onError: ex =>
                        {
                            eventBus.PublishErrorAsync(new EventHandlingError(ex));
                        });
        }

        internal static IScheduledCommand<TAggregate> CreateScheduledCommand<TCommand, TAggregate>(
            Guid aggregateId,
            TCommand command,
            DateTimeOffset? dueTime,
            IEvent deliveryDependsOn = null)
            where TCommand : ICommand<TAggregate> where TAggregate : IEventSourced
        {
            ScheduledCommandPrecondition precondition = null;

            if (deliveryDependsOn != null)
            {
                if (deliveryDependsOn.AggregateId == Guid.Empty)
                {
                    throw new ArgumentException("An AggregateId must be set on the event on which the scheduled command depends.");
                }

                if (String.IsNullOrWhiteSpace(deliveryDependsOn.ETag))
                {
                    deliveryDependsOn.IfTypeIs<Event>()
                                     .ThenDo(e => e.ETag = Guid.NewGuid().ToString("N"))
                                     .ElseDo(() =>
                                     {
                                         throw new ArgumentException("An ETag must be set on the event on which the scheduled command depends.");
                                     });
                }

                precondition = new ScheduledCommandPrecondition
                {
                    AggregateId = deliveryDependsOn.AggregateId,
                    ETag = deliveryDependsOn.ETag
                };
            }

            if (String.IsNullOrEmpty(command.ETag))
            {
                command.IfTypeIs<Command>()
                       .ThenDo(c => c.ETag = CommandContext.Current
                                                           .IfNotNull()
                                                           .Then(ctx => ctx.NextETag(aggregateId.ToString("N")))
                                                           .Else(() => Guid.NewGuid().ToString("N")));
            }

            var scheduledCommand = new CommandScheduled<TAggregate>
            {
                Command = command,
                DueTime = dueTime,
                AggregateId = aggregateId,
                SequenceNumber = -DateTimeOffset.UtcNow.Ticks,
                DeliveryPrecondition = precondition
            };
            return scheduledCommand;
        }

        internal class CommandsInPipeline
        {
            private readonly ConcurrentDictionary<IScheduledCommand, DateTimeOffset> commands = new ConcurrentDictionary<IScheduledCommand, DateTimeOffset>();

            public void Add(IScheduledCommand command)
            {
                var now = Clock.Now();
                commands.AddOrUpdate(
                    command,
                    now,
                    (c, t) => now);
            }

            public void Remove(IScheduledCommand command)
            {
                DateTimeOffset _;
                commands.TryRemove(command, out _);
            }

            public async Task Done()
            {
                while (true)
                {
                    var now = Clock.Current;
                    if (!commands.Keys.Any(c => c.IsDue(now)))
                    {
                        return;
                    }
                }
            }
        }

        public static Configuration TraceCommandsFor<TAggregate>(
            this Configuration configuration)
            where TAggregate : class, IEventSourced
        {
            // resolve and register so there's only a single instance registered at any given time
            var inPipeline = configuration.Container.Resolve<CommandsInPipeline>();
            configuration.Container.Register(c => inPipeline);

            return configuration.AddToCommandSchedulerPipeline<TAggregate>(
                schedule: async (command, next) =>
                {
                    inPipeline.Add(command);
                    await next(command);
                    Trace.WriteLine(Clock.Now() + " [Schedule] " + command);
                },
                deliver: async (command, next) =>
                {
                    await next(command);
                    Trace.WriteLine(Clock.Now() + " [Deliver] " + command);
                    inPipeline.Remove(command);
                });
        }
    }
}