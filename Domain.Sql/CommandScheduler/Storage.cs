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
            IScheduledCommand<TAggregate> scheduledCommand,
            Func<CommandSchedulerDbContext> createDbContext,
            Func<IScheduledCommand<TAggregate>, CommandSchedulerDbContext, Task<string>> clockNameForEvent) where TAggregate : class
        {
            ScheduledCommand storedScheduledCommand;

            using (var db = createDbContext())
            {
                var domainTime = Domain.Clock.Now();

                // get or create a clock to schedule the command on
                var clockName = await clockNameForEvent(scheduledCommand, db);
                var schedulerClock = await db.Clocks.SingleOrDefaultAsync(c => c.Name == clockName);

                if (schedulerClock == null)
                {
                    Debug.WriteLine(String.Format("Creating clock '{0}' @ {1}", clockName, domainTime));

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
                    AggregateId = ScheduledCommand<TAggregate>.TargetGuid(scheduledCommand),
                    SequenceNumber = scheduledCommand
                        .IfTypeIs<IEvent>()
                        .Then(e => e.SequenceNumber)
                        .Else(() => -DateTimeOffset.UtcNow.Ticks),
                    AggregateType = Command.TargetNameFor(scheduledCommand.Command.GetType()),
                    SerializedCommand = scheduledCommand.ToJson(),
                    CreatedTime = domainTime,
                    DueTime = scheduledCommand.DueTime,
                    Clock = schedulerClock
                };

                if (scheduledCommand.IsDue(storedScheduledCommand.Clock) &&
                    !scheduledCommand.Command.RequiresDurableScheduling())
                {
                    storedScheduledCommand.NonDurable = true;
                    return storedScheduledCommand;
                }

                Debug.WriteLine(String.Format("Storing command '{0}' ({1}:{2}) on clock '{3}'",
                                              scheduledCommand.Command.CommandName,
                                              scheduledCommand.TargetId,
                                              storedScheduledCommand.SequenceNumber,
                                              clockName));

                db.ScheduledCommands.Add(storedScheduledCommand);

                while (true)
                {
                    try
                    {
                        db.SaveChanges();
                        scheduledCommand.Result = new CommandScheduled(scheduledCommand, schedulerClock);
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

            scheduledCommand.IfTypeIs<ScheduledCommand<TAggregate>>()
                            .ThenDo(c => c.SequenceNumber = storedScheduledCommand.SequenceNumber);

            return storedScheduledCommand;
        }

        public static async Task DeserializeAndDeliverScheduledCommand<TAggregate>(
            ScheduledCommand scheduled,
            ICommandScheduler<TAggregate> scheduler)
            where TAggregate : IEventSourced
        {
            var command = scheduled.ToScheduledCommand<TAggregate>();

            // FIX: (DeserializeAndDeliverScheduledCommand) is this still needed?
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
            Func<CommandSchedulerDbContext> createDbContext) where TAggregate : class
        {
            using (var db = createDbContext())
            {
                var scheduledCommandGuid = ScheduledCommand<TAggregate>.TargetGuid(scheduledCommand);

                var storedCommand = await GetStoredScheduledCommand(
                    scheduledCommand,
                    db,
                    scheduledCommandGuid);

                if (storedCommand == null)
                {
                    if (!scheduledCommand.Command.RequiresDurableScheduling())
                    {
                        return;
                    }

                    throw new InvalidOperationException("Scheduled command not found");
                }

                storedCommand.Attempts ++;

                var result = scheduledCommand.Result;

                if (result is CommandSucceeded)
                {
                    storedCommand.AppliedTime = Domain.Clock.Now();
                }
                else
                {
                    RescheduleIfAppropriate(storedCommand, result, db);
                }

                await db.SaveChangesAsync();
            }
        }

        private static async Task<ScheduledCommand> GetStoredScheduledCommand<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand, 
            CommandSchedulerDbContext db, 
            Guid scheduledCommandGuid) where TAggregate : class
        {
            var sequenceNumber = scheduledCommand
                .IfTypeIs<IEvent>()
                .Then(c => c.SequenceNumber)
                .Else(() => scheduledCommand
                    .IfTypeIs<ScheduledCommand<TAggregate>>()
                    .Then(c => c.SequenceNumber)     )
                .ElseThrow(() => new InvalidOperationException("Cannot look up stored scheduled command based on a " + scheduledCommand.GetType()));

            var storedCommand = await db.ScheduledCommands
                                        .SingleOrDefaultAsync(
                                            c => c.AggregateId == scheduledCommandGuid &&
                                                 c.SequenceNumber == sequenceNumber);
            return storedCommand;
        }

        private static void RescheduleIfAppropriate(
            ScheduledCommand storedCommand,
            ScheduledCommandResult result,
            CommandSchedulerDbContext db)
        {
            var failure = (CommandFailed) result;

            var now = Domain.Clock.Now();
            if (failure.IsCanceled || failure.RetryAfter == null)
            {
                // no further retries
                storedCommand.FinalAttemptTime = now;
            }
            else
            {
                storedCommand.DueTime = now + failure.RetryAfter;
            }

            db.Errors.Add(new CommandExecutionError
            {
                ScheduledCommand = storedCommand,
                Error = result.IfTypeIs<CommandFailed>()
                              .Then(f => f.Exception.ToJson())
                              .ElseDefault()
            });
        }
    }
}