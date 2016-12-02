// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
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
            Func<IScheduledCommand<TAggregate>, CommandSchedulerDbContext, Task<string>> getClockName)
            where TAggregate : class
        {
            ScheduledCommand storedScheduledCommand;

            using (var db = createDbContext())
            {
                var domainTime = Domain.Clock.Now();

                // get or create a clock to schedule the command on
                var clock = TryToGetSqlClockToScheduleOn(scheduledCommand);
                if (clock == null)
                {
                    var clockName = await getClockName(scheduledCommand, db);

                    var clockStartTime = scheduledCommand.Clock?.Now() ??
                                         domainTime;

                    clock = await GetOrAddSchedulerClock(
                        db,
                        clockName,
                        clockStartTime);
                }

                storedScheduledCommand = CreateStoredScheduledCommand(
                    scheduledCommand,
                    domainTime,
                    clock);

                if (scheduledCommand.IsDue() &&
                    !scheduledCommand.Command.RequiresDurableScheduling())
                {
                    storedScheduledCommand.NonDurable = true;
                    return storedScheduledCommand;
                }

                Debug.WriteLine(
                    $"Storing command '{scheduledCommand.CommandName}' ({scheduledCommand.TargetId}:{storedScheduledCommand.SequenceNumber}) on clock '{clock.Name}'");

                await SaveScheduledCommandToDatabase(db,
                                                     storedScheduledCommand,
                                                     scheduledCommand);
     
            }

            scheduledCommand.IfTypeIs<ScheduledCommand<TAggregate>>()
                            .ThenDo(c => c.SequenceNumber = storedScheduledCommand.SequenceNumber);

            return storedScheduledCommand;
        }

        private static Clock TryToGetSqlClockToScheduleOn<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand) where TAggregate : class =>
            scheduledCommand.Clock as Clock ??
            scheduledCommand.Clock
                            .IfTypeIs<AnonymousClock>()
                            .Then(c => c.ParentClock as Clock)
                            .ElseDefault();

        internal static async Task DeserializeAndDeliverScheduledCommand<TAggregate>(
            ScheduledCommand scheduled,
            ICommandDeliverer<TAggregate> scheduler)
            where TAggregate : class
        {
            try
            {
                var command = scheduled.ToScheduledCommand<TAggregate>();
                await scheduler.Deliver(command);
                scheduled.Result = command.Result;
            }
            catch (Exception exception)
            {
                scheduled.Result = new CommandFailed(scheduled, exception);
            }
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

        private static ScheduledCommand CreateStoredScheduledCommand<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand,
            DateTimeOffset domainTime,
            Clock schedulerClock)
            where TAggregate : class =>
                new ScheduledCommand
                {
                    AggregateId = ScheduledCommand<TAggregate>.TargetGuid(scheduledCommand),
                    SequenceNumber = scheduledCommand
                        .IfTypeIs<IEvent>()
                        .Then(e => e.SequenceNumber)
                        .Else(() => -DateTimeOffset.UtcNow.Ticks),
                    AggregateType = Command.TargetNameFor(scheduledCommand.Command.GetType()),
                    SerializedCommand = scheduledCommand.ToJson(),
                    CreatedTime = domainTime,
                    DueTime = scheduledCommand.DueTime ?? schedulerClock.Now(),
                    Clock = schedulerClock,
                    CommandName = scheduledCommand.CommandName
                };

        private static async Task<Clock> GetOrAddSchedulerClock(
            CommandSchedulerDbContext db,
            string clockName,
            DateTimeOffset startTime)
        {
            var schedulerClock = await db.Clocks.SingleOrDefaultAsync(c => c.Name == clockName);

            if (schedulerClock != null)
            {
                return schedulerClock;
            }

            Debug.WriteLine($"Creating clock '{clockName}' @ {startTime}");

            schedulerClock = new Clock
            {
                Name = clockName,
                UtcNow = startTime,
                StartTime = startTime
            };

            db.Clocks.Add(schedulerClock);
            await db.SaveChangesAsync();

            return schedulerClock;
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
                                .Then(c => c.SequenceNumber))
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

        private static async Task SaveScheduledCommandToDatabase<TAggregate>(
            CommandSchedulerDbContext db,
            ScheduledCommand storedScheduledCommand,
            IScheduledCommand<TAggregate> scheduledCommand)
        {
            var etag = new ETag
            {
                Scope = scheduledCommand.TargetId,
                ETagValue = scheduledCommand.Command.ETag,
                CreatedDomainTime = Domain.Clock.Now(),
                CreatedRealTime = DateTimeOffset.UtcNow
            };

            while (true)
            {
                try
                {
                    await InsertScheduledCommandAndETag(db, storedScheduledCommand, etag);
                    break;
                }
                catch (Exception exception)
                {
                    if (!exception.IsConcurrencyException())
                    {
                        throw;
                    }

                    if (exception.ToString().Contains(@"object 'Scheduler.ScheduledCommand'"))
                    {
                        if (storedScheduledCommand.SequenceNumber < 0)
                        {
                            // for scheduler-assigned sequence numbers, decrement and retry
                            storedScheduledCommand.SequenceNumber--;
                        }
                        else
                        {
                            // this is not a scheduler-assigned sequence number
                            break;
                        }
                    }
                    else if (exception.ToString().Contains(@"object 'Scheduler.ETag'"))
                    {
                        scheduledCommand.Result = new CommandDeduplicated(
                            scheduledCommand,
                            "Schedule");
                        return;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            scheduledCommand.Result = new CommandScheduled(
                scheduledCommand,
                storedScheduledCommand.Clock);
        }

        private static async Task InsertScheduledCommandAndETag(CommandSchedulerDbContext db, ScheduledCommand storedScheduledCommand, ETag etag)
        {
            var sql = @"
INSERT [Scheduler].[ScheduledCommand] 
(
    [AggregateId], 
    [SequenceNumber], 
    [AggregateType],
    [CommandName], 
    [CreatedTime], 
    [DueTime], 
    [SerializedCommand], 
    [Attempts], 
    [Clock_Id]
)
VALUES 
(
    @aggregateId,
    @sequenceNumber,
    @aggregateType,
    @commandName,
    @createdTime,
    @dueTime,
    @serializedCommand,
    @attempts,
    @clock_Id
)

INSERT [Scheduler].[ETag]
(
    [Scope], 
    [ETagValue], 
    [CreatedDomainTime], 
    [CreatedRealTime]
)
VALUES 
(
    @scope,
    @eTagValue,
    @createdDomainTime,
    @createdRealTime
)";

            var parameters = new[]
            {
                // ScheduledCommand
                new SqlParameter("@aggregateId", storedScheduledCommand.AggregateId),
                new SqlParameter("@sequenceNumber", storedScheduledCommand.SequenceNumber),
                new SqlParameter("@aggregateType", storedScheduledCommand.AggregateType),
                new SqlParameter("@commandName", storedScheduledCommand.CommandName),
                new SqlParameter("@createdTime", storedScheduledCommand.CreatedTime),
                new SqlParameter("@dueTime", storedScheduledCommand.DueTime),
                new SqlParameter("@serializedCommand", storedScheduledCommand.SerializedCommand),
                new SqlParameter("@attempts", storedScheduledCommand.Attempts),
                new SqlParameter("@clock_Id", storedScheduledCommand.Clock.Id),
                // ETag
                new SqlParameter("@scope", etag.Scope),
                new SqlParameter("@eTagValue", etag.ETagValue),
                new SqlParameter("@createdDomainTime", etag.CreatedDomainTime),
                new SqlParameter("@createdRealTime", etag.CreatedRealTime)
            };

            await db.Database.ExecuteSqlCommandAsync(
                TransactionalBehavior.EnsureTransaction,
                sql,
                parameters);
        }
    }
}