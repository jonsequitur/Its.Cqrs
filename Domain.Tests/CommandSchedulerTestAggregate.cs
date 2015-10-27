// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Tests
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
        
        public class CommandThatSchedulesTwoOtherCommandsImmediately : Command<CommandSchedulerTestAggregate>
        {
            public Command NextCommand1 { get; set; }
            
            public Command NextCommand2 { get; set; }

            public Guid NextCommand1AggregateId { get; set; }
            
            public Guid NextCommand2AggregateId { get; set; }

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
            ICommandHandler<CommandSchedulerTestAggregate, CommandThatSchedulesAnotherCommandImmediately>, ICommandHandler<CommandSchedulerTestAggregate, CommandThatSchedulesTwoOtherCommandsImmediately>
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

            public async Task EnactCommand(
                CommandSchedulerTestAggregate aggregate,
                CommandThatSchedulesTwoOtherCommandsImmediately command)
            {
                await scheduler.Schedule(
                    command.NextCommand1AggregateId,
                    command.NextCommand1,
                    Clock.Now());
                await scheduler.Schedule(
                    command.NextCommand2AggregateId,
                    command.NextCommand2,
                    Clock.Now());
            }

            public async Task HandleScheduledCommandException(
                CommandSchedulerTestAggregate aggregate,
                CommandFailed<CommandThatSchedulesTwoOtherCommandsImmediately> command)
            {
            }
        }
    }
}