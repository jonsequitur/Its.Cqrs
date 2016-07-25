// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Tests
{
    public class NonEventSourcedCommandTarget
    {
        public Func<NonEventSourcedCommandTarget, CommandFailed<TestCommand>, Task> OnHandleScheduledCommandError;
        public Func<NonEventSourcedCommandTarget, TestCommand, Task> OnEnactCommand;
        
        public NonEventSourcedCommandTarget(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("id");
            }
            Id = id;
        }

        public NonEventSourcedCommandTarget(CreateCommandTarget create = null)
        {
            Id = create?.Id ?? Any.Word();
        }

        public string Id { get; }

        public bool IsValid { get; set; } = true;

        public ConcurrentBag<ICommand<NonEventSourcedCommandTarget>> CommandsEnacted { get; } = new ConcurrentBag<ICommand<NonEventSourcedCommandTarget>>();

        public ConcurrentBag<CommandFailed> CommandsFailed { get; } = new ConcurrentBag<CommandFailed>();
    }

    public class CommandTargetCommandHandler :
        ICommandHandler<NonEventSourcedCommandTarget, TestCommand>,
        ICommandHandler<NonEventSourcedCommandTarget, SendRequests>,
        ICommandHandler<NonEventSourcedCommandTarget, RequestReply>,
        ICommandHandler<NonEventSourcedCommandTarget, Reply>
    {
        private readonly ICommandScheduler<NonEventSourcedCommandTarget> scheduler;

        public CommandTargetCommandHandler(ICommandScheduler<NonEventSourcedCommandTarget> scheduler)
        {
            if (scheduler == null)
            {
                throw new ArgumentNullException(nameof(scheduler));
            }
            this.scheduler = scheduler;
        }

        public async Task EnactCommand(NonEventSourcedCommandTarget target, TestCommand command)
        {
            target.CommandsEnacted.Add(command);

            if (target.OnEnactCommand != null)
            {
                await target.OnEnactCommand(target, command);
            }
        }

        public async Task HandleScheduledCommandException(NonEventSourcedCommandTarget target, CommandFailed<TestCommand> failed)
        {
            target.CommandsFailed.Add(failed);

            target.OnHandleScheduledCommandError
                  .IfNotNull()
                  .ThenDo(enact => enact(target, failed));
        }

        public async Task EnactCommand(NonEventSourcedCommandTarget requestor, SendRequests command)
        {
            requestor.CommandsEnacted.Add(command);

            foreach (var targetId in command.TargetIds)
            {
                await scheduler.Schedule(targetId, new RequestReply(requestor.Id)
                {
                    RequestorId = requestor.Id
                });
            }
        }

        public async Task HandleScheduledCommandException(NonEventSourcedCommandTarget target, CommandFailed<SendRequests> command)
        {
            target.CommandsFailed.Add(command);
        }

        public async Task EnactCommand(NonEventSourcedCommandTarget replier, RequestReply command)
        {
            replier.CommandsEnacted.Add(command);

            await scheduler.Schedule(command.RequestorId, new Reply(replier.Id));
        }

        public async Task HandleScheduledCommandException(NonEventSourcedCommandTarget target, CommandFailed<RequestReply> command)
        {
            target.CommandsFailed.Add(command);
        }

        public async Task EnactCommand(NonEventSourcedCommandTarget replier, Reply command)
        {
            replier.CommandsEnacted.Add(command);
        }

        public async Task HandleScheduledCommandException(NonEventSourcedCommandTarget target, CommandFailed<Reply> command)
        {
            target.CommandsFailed.Add(command);
        }
    }

    public class CreateCommandTarget : ConstructorCommand<NonEventSourcedCommandTarget>
    {
        public CreateCommandTarget(string id, string etag = null) : base(etag)
        {
            Id = id;
        }

        public string Id { get; }
    }

    public class TestCommand : Command<NonEventSourcedCommandTarget>, ISpecifySchedulingBehavior
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

        public override IValidationRule<NonEventSourcedCommandTarget> Validator =>
            Validate.That<NonEventSourcedCommandTarget>(_ => _.IsValid);

        public bool CanBeDeliveredDuringScheduling { get; set; }

        public bool RequiresDurableScheduling { get; set; }
    }

    public class SendRequests : Command<NonEventSourcedCommandTarget>
    {
        public SendRequests(
            string[] targetIds,
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

        public string[] TargetIds { get; }
    }

    public class RequestReply : Command<NonEventSourcedCommandTarget>
    {
        public RequestReply(
            string requestorId,
            string etag = null) : base(etag)
        {
            RequestorId = requestorId;
        }

        public string RequestorId { get; set; }
    }

    public class Reply : Command<NonEventSourcedCommandTarget>
    {
        public Reply(
            string replierId,
            string etag = null) : base(etag)
        {
            ReplierId = replierId;
        }

        public string ReplierId { get; set; }
    }
}