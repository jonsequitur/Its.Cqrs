// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal static class Storage
    {
        internal static async Task<ScheduledCommand> StoredScheduledCommand<TAggregate>(
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
    }
}