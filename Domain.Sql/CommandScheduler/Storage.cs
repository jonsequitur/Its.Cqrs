// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal static class Storage
    {
        internal static async Task<ScheduledCommand> StoreScheduledCommand<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommandEvent,
            Func<CommandSchedulerDbContext> createDbContext,
            Func<IScheduledCommand<TAggregate>, CommandSchedulerDbContext, Task<string>> clockNameForEvent) where TAggregate : class, IEventSourced
        {
            ScheduledCommand storedScheduledCommand;

            using (var db = createDbContext())
            {
                var domainTime = Domain.Clock.Now();

                // get or create a clock to schedule the command on
                var clockName = await clockNameForEvent(scheduledCommandEvent, db);
                var schedulerClock = await db.Clocks.SingleOrDefaultAsync(c => c.Name == clockName);

                if (schedulerClock == null)
                {
                    Debug.WriteLine(String.Format("SqlCommandScheduler: Creating clock '{0}' @ {1}", clockName, domainTime));

                    schedulerClock = new Clock
                    {
                        Name = clockName,
                        UtcNow = domainTime,
                        StartTime = domainTime
                    };
                    db.Clocks.Add(schedulerClock);
                    await db.SaveChangesAsync();
                }

                storedScheduledCommand = new ScheduledCommand
                {
                    AggregateId = scheduledCommandEvent.AggregateId,
                    SequenceNumber = scheduledCommandEvent.SequenceNumber,
                    AggregateType = AggregateType<TAggregate>.EventStreamName,
                    SerializedCommand = scheduledCommandEvent.ToJson(),
                    CreatedTime = domainTime,
                    DueTime = scheduledCommandEvent.DueTime,
                    Clock = schedulerClock
                };

                if (storedScheduledCommand.ShouldBeDeliveredImmediately() &&
                    !scheduledCommandEvent.Command.RequiresDurableScheduling)
                {
                    storedScheduledCommand.NonDurable = true;
                    return storedScheduledCommand;
                }

                Debug.WriteLine(String.Format("SqlCommandScheduler: Storing command '{0}' ({1}:{2}) on clock '{3}'",
                                              scheduledCommandEvent.Command.CommandName,
                                              scheduledCommandEvent.AggregateId,
                                              scheduledCommandEvent.SequenceNumber,
                                              clockName));

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
                                // this is not a scheduler-assigned sequence number, so the concurrency exception indicates an actual issue
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

            return storedScheduledCommand;
        }

        public static async Task DeserializeAndDeliverScheduledCommand<TAggregate>(
            ScheduledCommand scheduled,
            ICommandScheduler<TAggregate> scheduler)
            where TAggregate : IEventSourced
        {
            var command = scheduled.ToScheduledCommand<TAggregate>();

            //here we are setting the command.SequenceNumber to the scheduled.SequenceNumber because when
            //multiple commands are scheduled simultaniously against the same aggregate we were decrementing the 
            //scheduled.SequenceNumber correctly, however we were not updating the command.SequenceNumber.
            //this is to prevent any side effects that may have been caused by that bug
            command.SequenceNumber = scheduled.SequenceNumber;

            await scheduler.Deliver(command);

            scheduled.Result = command.Result;
        }

        internal static async Task UpdateScheduledCommand<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand,
            Func<CommandSchedulerDbContext> createDbContext) where TAggregate : class, IEventSourced
        {
            using (var db = createDbContext())
            {
                var storedCommand = await db.ScheduledCommands
                                            .SingleAsync(c => c.AggregateId == scheduledCommand.AggregateId &&
                                                              c.SequenceNumber == scheduledCommand.SequenceNumber);

                storedCommand.Attempts ++;

                var result = scheduledCommand.Result();

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

        private static string Description<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand,
            CommandFailed failure) where TAggregate : IEventSourced
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