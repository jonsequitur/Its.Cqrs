// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public abstract class ScheduledCommandResult : ICommandSchedulerActivity
    {
        private readonly IScheduledCommand command;

        protected ScheduledCommandResult(IScheduledCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            this.command = command;
        }

        public IScheduledCommand ScheduledCommand => command;
    }
}
