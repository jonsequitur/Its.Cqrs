// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;
#pragma warning disable 618

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class SqlCommandScheduler<TAggregate> :
        ICommandScheduler<TAggregate>,
        IEventHandler,
        IEventHandlerBinder where TAggregate : class, IEventSourced
    {
        public IObserver<ICommandSchedulerActivity> Activity = Observer.Create<ICommandSchedulerActivity>(a => { });
        public Func<IScheduledCommand<TAggregate>, string> GetClockLookupKey = cmd => null;
        public GetClockName GetClockName = cmd => null;
        private readonly CommandPreconditionVerifier commandPreconditionVerifier;
        private readonly IHaveConsequencesWhen<CommandScheduled<TAggregate>> consequenter;
        private readonly Func<CommandSchedulerDbContext> createCommandSchedulerDbContext;
        private readonly IEventBus eventBus;
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
            consequenter = Consequenter.Create<CommandScheduled<TAggregate>>(e =>
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
                var repository = getRepository();

                await repository.ApplyScheduledCommand(scheduledCommand,
                                                       commandPreconditionVerifier);

                Activity.OnNext(scheduledCommand.Result);

                if (!durable)
                {
                    return;
                }

                await Storage.UpdateScheduledCommand(
                    scheduledCommand, 
                    createCommandSchedulerDbContext);
            }
        }

        public async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
        {
            var storedScheduledCommand = await Storage.StoreScheduledCommand(
                scheduledCommand,
                createCommandSchedulerDbContext,
                (scheduled, db) => ClockNameForEvent(this, scheduled, db));

            Activity.OnNext(new CommandScheduled(scheduledCommand, storedScheduledCommand.Clock));

            // deliver the command immediately if appropriate
            if (scheduledCommand.IsDue(storedScheduledCommand.Clock))
            {
                // sometimes the command depends on a precondition event that hasn't been saved
                if (!await commandPreconditionVerifier.IsPreconditionSatisfied(scheduledCommand))
                {
                    this.DeliverIfPreconditionIsSatisfiedWithin(
                        TimeSpan.FromSeconds(10),
                        scheduledCommand,
                        eventBus);
                }
                else
                {
                    var scheduler = Configuration.Current.CommandScheduler<TAggregate>();
                    await scheduler.Deliver(scheduledCommand);
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
            var clockName = sqlCommandScheduler.GetClockName(scheduledCommandEvent);

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
                scheduledCommand.Command.ETag
            }.ToString();
        }
    }
}
