// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal class AnonymousCommandHandler<TTarget, TCommand> : ICommandHandler<TTarget, TCommand>
        where TCommand : class, ICommand<TTarget>
    {
        private readonly HandleScheduledCommandException<TTarget, TCommand> handleScheduledCommandException;

        private readonly EnactCommand<TTarget, TCommand> enactCommand;

        public AnonymousCommandHandler(
            EnactCommand<TTarget, TCommand> enactCommand,
            HandleScheduledCommandException<TTarget, TCommand> handleScheduledCommandException = null)
        {
            if (enactCommand == null)
            {
                throw new ArgumentNullException(nameof(enactCommand));
            }

            this.enactCommand = enactCommand;

            this.handleScheduledCommandException = handleScheduledCommandException ??
                                                   ((target, command) => Task.FromResult(Unit.Default));
        }

        public Task EnactCommand(TTarget target, TCommand command) => 
            enactCommand(target, command);

        public Task HandleScheduledCommandException(
            TTarget target,
            CommandFailed<TCommand> command) => 
            handleScheduledCommandException(target, command);
    }
}