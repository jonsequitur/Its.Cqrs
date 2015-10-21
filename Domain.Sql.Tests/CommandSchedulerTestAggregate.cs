// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class CommandSchedulerTestAggregate : EventSourcedAggregate<CommandSchedulerTestAggregate>
    {
        public CommandSchedulerTestAggregate(Guid? id = null) : base(id)
        {
            RecordEvent(new Created());
        }

        public CommandSchedulerTestAggregate(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
        {
        }

        public class Created : Event<CommandSchedulerTestAggregate>
        {
            public override void Update(CommandSchedulerTestAggregate aggregate)
            {
            }
        }

        public class Command : Command<CommandSchedulerTestAggregate>
        {
            public string CommandId { get; set; }

            public override bool Authorize(CommandSchedulerTestAggregate aggregate)
            {
                return true;
            }
        }

        public class CommandThatSchedulesAnotherCommandImmediately : Command<CommandSchedulerTestAggregate>
        {
            public Command NextCommand { get; set; }

            public Guid NextCommandAggregateId { get; set; }

            public override bool Authorize(CommandSchedulerTestAggregate aggregate)
            {
                return true;
            }
        }

        public class CommandSucceeded : Event<CommandSchedulerTestAggregate>
        {
            public Command Command { get; set; }

            public override void Update(CommandSchedulerTestAggregate aggregate)
            {
            }
        }

        public class CommandFailed : Event<CommandSchedulerTestAggregate>
        {
            public Command<CommandSchedulerTestAggregate> Command { get; set; }

            public override void Update(CommandSchedulerTestAggregate aggregate)
            {
            }
        }

        public class CommandHandler :
            ICommandHandler<CommandSchedulerTestAggregate, Command>,
            ICommandHandler<CommandSchedulerTestAggregate, CommandThatSchedulesAnotherCommandImmediately>
        {
            private readonly ICommandScheduler<CommandSchedulerTestAggregate> scheduler;

            public CommandHandler(ICommandScheduler<CommandSchedulerTestAggregate> scheduler)
            {
                if (scheduler == null)
                {
                    throw new ArgumentNullException("scheduler");
                }
                this.scheduler = scheduler;
            }

            public async Task EnactCommand(
                CommandSchedulerTestAggregate aggregate,
                Command command)
            {
                aggregate.RecordEvent(new CommandSucceeded
                {
                    Command = command
                });
            }

            public async Task HandleScheduledCommandException(
                CommandSchedulerTestAggregate aggregate,
                CommandFailed<Command> command)
            {
                aggregate.RecordEvent(new CommandFailed
                {
                    Command = command.Command
                });
            }

            public async Task EnactCommand(
                CommandSchedulerTestAggregate aggregate,
                CommandThatSchedulesAnotherCommandImmediately command)
            {
                await scheduler.Schedule(
                    command.NextCommandAggregateId, 
                    command.NextCommand,
                    Clock.Now());
            }

            public async Task HandleScheduledCommandException(
                CommandSchedulerTestAggregate aggregate,
                CommandFailed<CommandThatSchedulesAnotherCommandImmediately> command)
            {
                aggregate.RecordEvent(new CommandFailed
                {
                    Command = command.Command
                });
            }
        }
    }
}