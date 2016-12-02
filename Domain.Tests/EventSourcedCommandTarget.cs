// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Tests
{
    public class EventSourcedCommandTarget : EventSourcedAggregate<EventSourcedCommandTarget>
    {
        public Func<EventSourcedCommandTarget, CommandFailed<TestCommand>, Task> OnHandleScheduledCommandError;
        public Func<EventSourcedCommandTarget, TestCommand, Task> OnEnactCommand;

        public EventSourcedCommandTarget(Guid? id = null) : base(id)
        {
        }

        public EventSourcedCommandTarget(ConstructorCommand<EventSourcedCommandTarget> createCommand) : base(createCommand)
        {
        }

        public EventSourcedCommandTarget(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
        {
        }

        public bool IsValid { get; set; } = true;

        public ConcurrentBag<ICommand<EventSourcedCommandTarget>> CommandsEnacted { get; } = new ConcurrentBag<ICommand<EventSourcedCommandTarget>>();

        public ConcurrentBag<CommandFailed> CommandsFailed { get; } = new ConcurrentBag<CommandFailed>();

        public class CommandTargetCommandHandler :
            ICommandHandler<EventSourcedCommandTarget, TestCommand>,
            ICommandHandler<EventSourcedCommandTarget, SendRequests>,
            ICommandHandler<EventSourcedCommandTarget, RequestReply>,
            ICommandHandler<EventSourcedCommandTarget, Reply>
        {
            private readonly ICommandScheduler<EventSourcedCommandTarget> scheduler;

            public CommandTargetCommandHandler(ICommandScheduler<EventSourcedCommandTarget> scheduler)
            {
                if (scheduler == null)
                {
                    throw new ArgumentNullException(nameof(scheduler));
                }
                this.scheduler = scheduler;
            }

            public async Task EnactCommand(EventSourcedCommandTarget target, TestCommand command)
            {
                target.CommandsEnacted.Add(command);

                if (target.OnEnactCommand != null)
                {
                    await target.OnEnactCommand(target, command);
                }
            }

            public async Task HandleScheduledCommandException(EventSourcedCommandTarget target, CommandFailed<TestCommand> failed)
            {
                target.CommandsFailed.Add(failed);

                target.OnHandleScheduledCommandError
                      .IfNotNull()
                      .ThenDo(enact => enact(target, failed));
            }

            public async Task EnactCommand(EventSourcedCommandTarget requestor, SendRequests command)
            {
                requestor.CommandsEnacted.Add(command);

                foreach (var aggregateId in command.TargetIds)
                {
                    await scheduler.Schedule(aggregateId, new RequestReply(requestor.Id)
                    {
                        RequestorId = requestor.Id
                    });
                }
            }

            public async Task HandleScheduledCommandException(EventSourcedCommandTarget target, CommandFailed<SendRequests> command)
            {
                target.CommandsFailed.Add(command);
            }

            public async Task EnactCommand(EventSourcedCommandTarget replier, RequestReply command)
            {
                replier.CommandsEnacted.Add(command);

                await scheduler.Schedule(command.RequestorId, new Reply(replier.Id));
            }

            public async Task HandleScheduledCommandException(EventSourcedCommandTarget target, CommandFailed<RequestReply> command)
            {
                target.CommandsFailed.Add(command);
            }

            public async Task EnactCommand(EventSourcedCommandTarget replier, Reply command)
            {
                replier.CommandsEnacted.Add(command);
            }

            public async Task HandleScheduledCommandException(EventSourcedCommandTarget target, CommandFailed<Reply> command)
            {
                target.CommandsFailed.Add(command);
            }
        }

        public class CreateCommandTarget : ConstructorCommand<EventSourcedCommandTarget>
        {
            public CreateCommandTarget(Guid aggregateId, string etag = null) : base(aggregateId, etag)
            {
            }
        }

        public class TestCommand : Command<EventSourcedCommandTarget>, ISpecifySchedulingBehavior
        {
            public TestCommand(string etag = null, bool isValid = true) : base(etag)
            {
                IsValid = isValid;

                RequiresDurableScheduling = true;
                CanBeDeliveredDuringScheduling = true;
            }

            public bool IsValid { get; set; }

            public override IValidationRule CommandValidator =>
                Validate.That<TestCommand>(cmd => cmd.IsValid);

            public override IValidationRule<EventSourcedCommandTarget> Validator =>
                Validate.That<EventSourcedCommandTarget>(_ => _.IsValid);

            public bool CanBeDeliveredDuringScheduling { get; set; }

            public bool RequiresDurableScheduling { get; set; }
        }

        public class SendRequests : Command<EventSourcedCommandTarget>
        {
            public SendRequests(
                Guid[] targetIds,
                string etag = null) : base(etag)
            {
                if (targetIds == null)
                {
                    throw new ArgumentNullException(nameof(targetIds));
                }
                if (!targetIds.Any())
                {
                    throw new ArgumentException("There must be at least one target id");
                }

                TargetIds = targetIds;
            }

            public Guid[] TargetIds { get; }
        }

        public class RequestReply : Command<EventSourcedCommandTarget>
        {
            public RequestReply(
                Guid requestorId,
                string etag = null) : base(etag)
            {
                RequestorId = requestorId;
            }

            public Guid RequestorId { get; set; }
        }

        public class Reply : Command<EventSourcedCommandTarget>
        {
            public Reply(
                Guid replierId,
                string etag = null) : base(etag)
            {
                ReplierId = replierId;
            }

            public Guid ReplierId { get; set; }
        }
    }
}