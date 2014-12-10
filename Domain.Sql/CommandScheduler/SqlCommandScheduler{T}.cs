using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class SqlCommandScheduler<TAggregate> :
        ICommandScheduler<TAggregate>,
        IEventHandler,
        IEventHandlerBinder where TAggregate : class, IEventSourced
    {
        private readonly Func<IEventSourcedRepository<TAggregate>> getRepository;
        private readonly CommandPreconditionVerifier<TAggregate> commandPreconditionVerifier;
        private readonly Func<CommandSchedulerDbContext> createCommandSchedulerDbContext;
        private readonly IEventBus eventBus;
        private readonly IHaveConsequencesWhen<IScheduledCommand<TAggregate>> consequenter;

        private readonly string eventStreamName = AggregateType<TAggregate>.EventStreamName;

        public SqlCommandScheduler(
            Func<IEventSourcedRepository<TAggregate>> getRepository,
            Func<CommandSchedulerDbContext> createCommandSchedulerDbContext,
            IEventBus eventBus, CommandPreconditionVerifier<TAggregate> commandPreconditionVerifier)
        {
            if (getRepository == null)
            {
                throw new ArgumentNullException("getRepository");
            }
            if (createCommandSchedulerDbContext == null)
            {
                throw new ArgumentNullException("createCommandSchedulerDbContext");
            }
            if (eventBus == null)
            {
                throw new ArgumentNullException("eventBus");
            }
            if (commandPreconditionVerifier == null)
            {
                throw new ArgumentNullException("commandPreconditionVerifier");
            }
            this.getRepository = getRepository;
            this.createCommandSchedulerDbContext = createCommandSchedulerDbContext;
            this.eventBus = eventBus;
            this.commandPreconditionVerifier = commandPreconditionVerifier;
            consequenter = Consequenter.Create<IScheduledCommand<TAggregate>>(async e => await Schedule(e));
        }

        public Func<IEvent, string> GetClockName = cmd => null;

        public Func<IScheduledCommand<TAggregate>, string> GetClockLookupKey = cmd => null;

        public IObserver<ScheduledCommandResult> Activity = Observer.Create<ScheduledCommandResult>(a => { });

        public async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
            IClock clock = null;
            if (scheduledCommand.DueTime != null)
            {
                clock = Domain.Clock.Create(() => scheduledCommand.DueTime.Value);
            }

            using (CommandContext.Establish(scheduledCommand.Command, clock))
            {
                Debug.WriteLine("SqlCommandScheduler.Deliver: " + Description(scheduledCommand));

                var repository = getRepository();

                var result = await repository.ApplyScheduledCommand(scheduledCommand,
                                                                    async () => await commandPreconditionVerifier.VerifyPrecondition(scheduledCommand));

                Activity.OnNext(result);

                scheduledCommand.IfTypeIs<CommandScheduled<TAggregate>>()
                                .ThenDo(c => c.Result = result);

                using (var db = createCommandSchedulerDbContext())
                {
                    var storedCommand = db.ScheduledCommands
                                          .Single(c => c.AggregateId == scheduledCommand.AggregateId &&
                                                       c.SequenceNumber == scheduledCommand.SequenceNumber);

                    if (result.WasSuccessful)
                    {
                        storedCommand.AppliedTime = Domain.Clock.Now();
                    }
                    else
                    {
                        var failure = result as ScheduledCommandFailure;
                        storedCommand.Attempts ++;

                        // reschedule as appropriate
                        var now = Domain.Clock.Now();
                        if (failure.RetryAfter != null)
                        {
                            Debug.WriteLine("SqlCommandScheduler.Deliver (scheduling retry): " + Description(scheduledCommand));
                            storedCommand.DueTime = now + failure.RetryAfter;
                        }
                        else
                        {
                            Debug.WriteLine("SqlCommandScheduler.Deliver (abandoning): " + Description(scheduledCommand));
                            // no further retries
                            storedCommand.FinalAttemptTime = now;
                        }

                        db.Errors.Add(new CommandExecutionError
                        {
                            ScheduledCommand = storedCommand,
                            Error = result.IfTypeIs<ScheduledCommandFailure>()
                                          .Then(f => f.Exception.ToJson()).ElseDefault()
                        });
                    }

                    db.SaveChanges();
                }
            }
        }

        public async Task Schedule(IScheduledCommand<TAggregate> scheduledCommandEvent)
        {
            var domainTime = Domain.Clock.Now();

            Clock schedulerClock;
            ScheduledCommand storedScheduledCommand;

            Debug.WriteLine("SqlCommandScheduler.Schedule: " + Description(scheduledCommandEvent));

            using (var db = createCommandSchedulerDbContext())
            {
                // store the scheduled command
                var clockName = ClockNameForEvent(scheduledCommandEvent, db);
                schedulerClock = db.Clocks.SingleOrDefault(c => c.Name == clockName);

                if (schedulerClock == null)
                {
                    schedulerClock = new Clock
                    {
                        Name = clockName,
                        UtcNow = domainTime,
                        StartTime = domainTime
                    };
                    db.Clocks.Add(schedulerClock);
                    db.SaveChanges();
                }

                storedScheduledCommand = new ScheduledCommand
                {
                    AggregateId = scheduledCommandEvent.AggregateId,
                    SequenceNumber = scheduledCommandEvent.SequenceNumber,
                    AggregateType = eventStreamName,
                    SerializedCommand = scheduledCommandEvent.ToJson(),
                    CreatedTime = domainTime,
                    DueTime = scheduledCommandEvent.DueTime,
                    Clock = schedulerClock
                };

                db.ScheduledCommands.Add(storedScheduledCommand);

                while (true)
                {
                    try
                    {
                        db.SaveChanges();
                        break;
                    }
                    catch (DbUpdateException exception)
                    {
                        if (exception.IsConcurrencyException())
                        {
                            if (storedScheduledCommand.SequenceNumber < 0)
                            {
                                // for scheduler-assigned sequence numbers, decrement and retry
                                storedScheduledCommand.SequenceNumber--;
                            }
                            else
                            {
                                // this is not a scheduler-assigned sequence number, so the concurrency exception indicates
                                break;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            // deliver the command immediately if appropriate
            if (scheduledCommandEvent.DueTime == null ||
                scheduledCommandEvent.DueTime <= schedulerClock.UtcNow)
            {
                var scheduledCommand = storedScheduledCommand.ToScheduledCommand<TAggregate>();

                // sometimes the command depends on a precondition even that hasn't been saved
                if (!await commandPreconditionVerifier.VerifyPrecondition(scheduledCommand))
                {
                    eventBus.Events<IEvent>()
                            .Where(
                                e => e.AggregateId == scheduledCommand.DeliveryPrecondition.AggregateId &&
                                     e.ETag == scheduledCommand.DeliveryPrecondition.ETag)
                            .Take(1)
                            .Timeout(TimeSpan.FromSeconds(10))
                            .Subscribe(
                                async e => { await Deliver(scheduledCommand); },
                                onError: ex =>
                                {
                                    // FIX: (Schedule) this should probably go somewhere else
                                    eventBus.PublishErrorAsync(new Domain.EventHandlingError(ex, this));
                                });
                }
                else
                {
                    await Deliver(scheduledCommand);
                }
            }
        }

        private string ClockNameForEvent(
            IScheduledCommand<TAggregate> scheduledCommandEvent,
            CommandSchedulerDbContext db)
        {
            // TODO: (ClockNameForEvent) clean this up
            var clockName =
                scheduledCommandEvent.IfTypeIs<IHaveExtensibleMetada>()
                                     .Then(e => ((object) e.Metadata)
                                                    .IfTypeIs<IDictionary<string, object>>()
                                                    .Then(m => m.IfContains("ClockName")
                                                                .Then(v => v.ToString())))
                                     .ElseDefault();

            if (clockName != null)
            {
                return clockName;
            }

            clockName = GetClockName(scheduledCommandEvent);
            if (clockName != null)
            {
                return clockName;
            }

            var lookupValue = GetClockLookupKey(scheduledCommandEvent);
            return db.ClockMappings
                     .Include(m => m.Clock)
                     .SingleOrDefault(c => c.Value == lookupValue)
                     .IfNotNull()
                     .Then(c => c.Clock.Name)
                     .Else(() => SqlCommandScheduler.DefaultClockName);
        }

        private static string Description(IScheduledCommand<TAggregate> scheduledCommand)
        {
            return new
            {
                Name = scheduledCommand.Command.CommandName,
                DueTime = scheduledCommand.DueTime.IfNotNull()
                                          .Then(t => t.ToString("O"))
                                          .Else(() => "[null]"),
                Clocks = Domain.Clock.Current.ToString(),
                scheduledCommand.AggregateId,
                scheduledCommand.ETag,
            }.ToString();
        }

        public IEnumerable<IEventHandlerBinder> GetBinders()
        {
            return new IEventHandlerBinder[] { this };
        }

        public IDisposable SubscribeToBus(object handler, IEventBus bus)
        {
            return bus.Subscribe(consequenter);
        }
    }
}