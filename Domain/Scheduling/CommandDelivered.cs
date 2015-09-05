// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public abstract class CommandDelivered : ScheduledCommandResult
    {
        protected CommandDelivered(IScheduledCommand command) : base(command)
        {
        }
    }
}