// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// Schedules commands durably via a SQL backing store for immediate or future application.
    /// </summary>
    public class SqlCommandScheduler : IEventHandler
    {
        internal const string DefaultClockName = "default";

        private readonly Dictionary<string, Func<ScheduledCommand, Task>> commandDispatchers = new Dictionary<string, Func<ScheduledCommand, Task>>();

        private readonly IEventHandlerBinder[] binders;

        private readonly ISubject<ICommandSchedulerActivity> activity = new Subject<ICommandSchedulerActivity>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlCommandScheduler"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public SqlCommandScheduler(Configuration configuration = null)
        {
            configuration = configuration ?? Configuration.Current;

            var container = configuration.Container;

            binders = AggregateType.KnownTypes
                                   .Select(aggregateType =>
                                   {
                                       var aggregateTypeName = AggregateType.EventStreamName(aggregateType);

                                       dynamic binder = container.Resolve(
                                           typeof (SqlCommandSchedulerBinder<>).MakeGenericType(aggregateType));

                                       binder.Scheduler.GetClockName = new Func<IEvent, string>(ClockName);
                                       binder.Scheduler.Activity = activity;

                                       var schedulerType = typeof (ICommandScheduler<>).MakeGenericType(aggregateType);

                                       if (!container.Any(t => t.Key == schedulerType))
                                       {
                                           container.Register(schedulerType,
                                                              c => binder.Scheduler);
                                       }

                                       commandDispatchers[aggregateTypeName] = async e =>
                                       {
                                           await binder.Deliver(e);
                                       };

                                       return binder;
                                   })
                                   .Cast<IEventHandlerBinder>()
                                   .ToArray();
        }

        public Func<CommandSchedulerDbContext> CreateCommandSchedulerDbContext = () => new CommandSchedulerDbContext();

        /// <summary>
        /// An observable of scheduler activity, which is updated each time a command is applied, whether successful or not.
        /// </summary>
        public IObservable<ICommandSchedulerActivity> Activity
        {
            get
            {
                return activity;
            }
        }

        /// <summary>
        /// Advances the clock by a specified amount and triggers any commands that are due by the end of that time period.
        /// </summary>
        /// <param name="clockName">Name of the clock.</param>
        /// <param name="by">The timespan by which to advance the clock.</param>
        /// <param name="query">A query that can be used to filter the commands to be applied.</param>
        /// <returns>
        /// A result summarizing the triggered commands.
        /// </returns>
        public Task<SchedulerAdvancedResult> AdvanceClock(string clockName,
                                                          TimeSpan by,
                                                          Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null)
        {
            return Advance(clockName, by: @by, query: query);
        }

        /// <summary>
        /// Advances the clock to a specified time and triggers any commands that are due by that time.
        /// </summary>
        /// <param name="clockName">Name of the clock.</param>
        /// <param name="to">The time to which to advance the clock.</param>
        /// <param name="query">A query that can be used to filter the commands to be applied.</param>
        /// <returns>
        /// A result summarizing the triggered commands.
        /// </returns>
        public Task<SchedulerAdvancedResult> AdvanceClock(string clockName,
                                                          DateTimeOffset to,
                                                          Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null)
        {
            return Advance(clockName, to, query: query);
        }

        private async Task<SchedulerAdvancedResult> Advance(string clockName,
                                                            DateTimeOffset? to = null,
                                                            TimeSpan? by = null,
                                                            Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null)
        {
            if (clockName == null)
            {
                throw new ArgumentNullException("clockName");
            }
            if (to == null && @by == null)
            {
                throw new ArgumentException("Either to or by must be specified.");
            }

            using (var db = CreateCommandSchedulerDbContext())
            {
                var clock = db.Clocks.SingleOrDefault(c => c.Name == clockName);

                if (clock == null)
                {
                    throw new ObjectNotFoundException(string.Format("No clock named {0} was found.", clockName));
                }

                to = to ?? clock.UtcNow.Add(@by.Value);

                if (to <= clock.UtcNow)
                {
                    throw new InvalidOperationException(string.Format("A clock cannot be moved backward. ({0})", new
                    {
                        Clock = clock.ToJson(),
                        RequestedTime = to
                    }));
                }

                var result = new SchedulerAdvancedResult(to.Value);

                clock.UtcNow = to.Value;
                db.SaveChanges();

                var commands = db.ScheduledCommands
                                 .Due(asOf: to)
                                 .Where(c => c.Clock.Id == clock.Id);

                if (query != null)
                {
                    commands = query(commands);
                }

                // ToArray closes the connection so that when we perform saves during the loop there are no connection errors
                foreach (var scheduled in commands.ToArray())
                {
                    //clock.UtcNow = scheduled.DueTime ?? to.Value;
                    await Trigger(scheduled, result, db);
                }

                return result;
            }
        }

        /// <summary>
        /// Triggers all commands matched by the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>
        /// A result summarizing the triggered commands.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        /// <remarks>If the query matches commands that have been successfully applied already or abandoned, they will be re-applied.</remarks>
        public async Task<SchedulerAdvancedResult> Trigger(Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query)
        {
            // QUESTION: (Trigger) re: the remarks XML comment, would it be clearer to have two methods, e.g. something like TriggerAnyCommands and TriggerEligibleCommands?
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var result = new SchedulerAdvancedResult();

            using (var db = CreateCommandSchedulerDbContext())
            {
                var commands = query(db.ScheduledCommands).ToArray();

                foreach (var scheduled in commands)
                {
                    await Trigger(scheduled, result, db);
                }
            }

            return result;
        }

        private async Task Trigger(ScheduledCommand scheduled,
                                   SchedulerAdvancedResult result,
                                   CommandSchedulerDbContext db)
        {
            var deliver = commandDispatchers.IfContains(scheduled.AggregateType)
                                            .ElseDefault();

            if (deliver == null)
            {
                // QUESTION: (Trigger) is this worth raising a warning for or is there a reasonable chance that not registering a handler was deliberate? 
                //                var error = ScheduledCommandFailure();
                //
                //                activity.OnNext(new CommandSchedulerActivity(scheduled, error));
                //
                //                result.Add(error);
                //                db.Errors.Add(error);
            }
            else
            {
                await deliver(scheduled);

                result.Add(scheduled.Result);
            }

            scheduled.Attempts++;

            db.SaveChanges();
        }

        public void CreateClock(
            string clockName,
            DateTimeOffset startTime)
        {
            if (clockName == null)
            {
                throw new ArgumentNullException("clockName");
            }

            using (var db = CreateCommandSchedulerDbContext())
            {
                db.Clocks.Add(new Clock
                {
                    Name = clockName,
                    UtcNow = startTime,
                    StartTime = startTime
                });
                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    if (ex.ToString().Contains(@"Cannot insert duplicate key row in object 'Scheduler.Clock' with unique index 'IX_Name'"))
                    {
                        throw new ConcurrencyException(string.Format("A clock named '{0}' already exists.", clockName), innerException: ex);
                    }
                    throw;
                }
            }
        }

        public void AssociateWithClock(string clockName,
                                       string lookup)
        {
            if (clockName == null)
            {
                throw new ArgumentNullException("clockName");
            }
            if (lookup == null)
            {
                throw new ArgumentNullException("lookup");
            }

            using (var db = CreateCommandSchedulerDbContext())
            {
                var clock = db.Clocks.SingleOrDefault(c => c.Name == clockName);

                if (clock == null)
                {
                    var now = Domain.Clock.Now();
                    clock = new Clock
                    {
                        Name = clockName,
                        UtcNow = now,
                        StartTime = now
                    };
                    db.Clocks.Add(clock);
                }

                db.ClockMappings.Add(new ClockMapping
                {
                    Clock = clock,
                    Value = lookup,
                });

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    if (exception.ToString().Contains(@"Cannot insert duplicate key row in object 'Scheduler.ClockMapping' with unique index 'IX_Value'"))
                    {
                        throw new InvalidOperationException(string.Format("Value '{0}' is already associated with another clock", lookup), exception);
                    }
                    throw;
                }
            }
        }

        public IEnumerable<IEventHandlerBinder> GetBinders()
        {
            return binders;
        }

        public void ClockLookupFor<TAggregate>(Func<IScheduledCommand<TAggregate>, string> lookup)
            where TAggregate : class, IEventSourced
        {
            binders.OfType<SqlCommandSchedulerBinder<TAggregate>>()
                   .Single()
                   .Scheduler
                   .GetClockLookupKey = lookup;
        }

        public Func<IEvent, string> GetClockName = cmd => null;

        internal string ClockName(IEvent @event)
        {
            return GetClockName(@event);
        }

        public DateTimeOffset ReadClock(string clockName)
        {
            using (var db = CreateCommandSchedulerDbContext())
            {
                return db.Clocks.Single(c => c.Name == clockName).UtcNow;
            }
        }
    }
}
