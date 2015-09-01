// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
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
        public IObserver<ICommandSchedulerActivity> Activity = Observer.Create<ICommandSchedulerActivity>(a => { });
        public Func<IScheduledCommand<TAggregate>, string> GetClockLookupKey = cmd => null;
        public Func<IEvent, string> GetClockName = cmd => null;
        private readonly CommandPreconditionVerifier commandPreconditionVerifier;
        private readonly IHaveConsequencesWhen<IScheduledCommand<TAggregate>> consequenter;
        private readonly Func<CommandSchedulerDbContext> createCommandSchedulerDbContext;
        private readonly IEventBus eventBus;
        private readonly string eventStreamName = AggregateType<TAggregate>.EventStreamName;
        private readonly Func<IEventSourcedRepository<TAggregate>> getRepository;

        public SqlCommandScheduler(
            Func<IEventSourcedRepository<TAggregate>> getRepository,
            Func<CommandSchedulerDbContext> createCommandSchedulerDbContext,
            IEventBus eventBus,
            CommandPreconditionVerifier commandPreconditionVerifier)
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
            consequenter = Consequenter.Create<IScheduledCommand<TAggregate>>(e =>
            {
                Task.Run(() => Schedule(e)).Wait();
            });
        }

        public async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
            await Deliver(scheduledCommand, true);
        }

        private async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand, bool durable)
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
                                                                    () => commandPreconditionVerifier.VerifyPrecondition(scheduledCommand));

                Activity.OnNext(result);

                scheduledCommand.IfTypeIs<CommandScheduled<TAggregate>>()
                                .ThenDo(c => c.Result = result);

                if (!durable)
                {
                    return;
                }

                using (var db = createCommandSchedulerDbContext())
                {
                    var storedCommand = await db.ScheduledCommands
                                                .SingleAsync(c => c.AggregateId == scheduledCommand.AggregateId &&
                                                                  c.SequenceNumber == scheduledCommand.SequenceNumber);

                    storedCommand.Attempts ++;

                    if (result.WasSuccessful)
                    {
                        storedCommand.AppliedTime = Domain.Clock.Now();
                    }
                    else
                    {
                        var failure = (CommandFailed) result;

                        // reschedule as appropriate
                        var now = Domain.Clock.Now();
                        if (failure.IsCanceled || failure.RetryAfter == null)
                        {
                            Debug.WriteLine("SqlCommandScheduler.Deliver (abandoning): " + Description(scheduledCommand, failure));
                            // no further retries
                            storedCommand.FinalAttemptTime = now;
                        }
                        else
                        {
                            Debug.WriteLine("SqlCommandScheduler.Deliver (scheduling retry): " + Description(scheduledCommand, failure));
                            storedCommand.DueTime = now + failure.RetryAfter;
                        }

                        db.Errors.Add(new CommandExecutionError
                                      {
                                          ScheduledCommand = storedCommand,
                                          Error = result.IfTypeIs<CommandFailed>()
                                                        .Then(f => f.Exception.ToJson()).ElseDefault()
                                      });
                    }

                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task Schedule(IScheduledCommand<TAggregate> scheduledCommandEvent)
        {
            Debug.WriteLine("SqlCommandScheduler.Schedule: " + Description(scheduledCommandEvent));

            var storedScheduledCommand = await Storage.StoreScheduledCommand(
                scheduledCommandEvent,
                createCommandSchedulerDbContext,
                (scheduledCommandEvent1, db) => ClockNameForEvent(this, scheduledCommandEvent1, db));

            var scheduledCommand = storedScheduledCommand.ToScheduledCommand<TAggregate>();

            Activity.OnNext(new CommandScheduled(scheduledCommand)
            {
                ClockName = storedScheduledCommand.Clock.Name
            });

            // deliver the command immediately if appropriate
            if (storedScheduledCommand.ShouldBeDeliveredImmediately())
            {
                // sometimes the command depends on a precondition event that hasn't been saved
                if (!await commandPreconditionVerifier.VerifyPrecondition(scheduledCommand))
                {
                    this.DeliverIfPreconditionIsSatisfiedWithin(
                        TimeSpan.FromSeconds(10),
                        scheduledCommand,
                        eventBus);
                }
                else
                {
                    await Deliver(scheduledCommand,
                                  durable: !storedScheduledCommand.NonDurable);
                }
            }
        }

        public IEnumerable<IEventHandlerBinder> GetBinders()
        {
            return new IEventHandlerBinder[] { this };
        }

        public IDisposable SubscribeToBus(object handler, IEventBus bus)
        {
            return bus.Subscribe(consequenter);
        }

        private static async Task<string> ClockNameForEvent(
            SqlCommandScheduler<TAggregate> sqlCommandScheduler, 
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

            if (clockName == null)
            {
                clockName = sqlCommandScheduler.GetClockName(scheduledCommandEvent);

                if (clockName == null)
                {
                    var lookupValue = sqlCommandScheduler.GetClockLookupKey(scheduledCommandEvent);
                    clockName = (await db.ClockMappings
                                         .Include(m => m.Clock)
                                         .SingleOrDefaultAsync(c => c.Value == lookupValue))
                        .IfNotNull()
                        .Then(c => c.Clock.Name)
                        .Else(() => SqlCommandScheduler.DefaultClockName);
                }
            }

            return clockName;
        }

        private static string Description(
            IScheduledCommand<TAggregate> scheduledCommand)
        {
            return new
            {
                Name = scheduledCommand.Command.CommandName,
                DueTime = scheduledCommand.DueTime
                                          .IfNotNull()
                                          .Then(t => t.ToString("O"))
                                          .Else(() => "[null]"),
                Clocks = Domain.Clock.Current.ToString(),
                scheduledCommand.AggregateId,
                scheduledCommand.ETag
            }.ToString();
        }
        
        private static string Description(
            IScheduledCommand<TAggregate> scheduledCommand,
            CommandFailed failure)
        {
            return new
            {
                Name = scheduledCommand.Command.CommandName,
                failure.IsCanceled,
                failure.NumberOfPreviousAttempts,
                failure.RetryAfter,
                failure.Exception,
                DueTime = scheduledCommand.DueTime
                                          .IfNotNull()
                                          .Then(t => t.ToString("O"))
                                          .Else(() => "[null]"),
                Clocks = Domain.Clock.Current.ToString(),
                scheduledCommand.AggregateId,
                scheduledCommand.ETag
            }.ToString();
        }
    }
}
