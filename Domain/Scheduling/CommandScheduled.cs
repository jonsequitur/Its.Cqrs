// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    [DebuggerDisplay("{ToString()}")]
    public class CommandScheduled : ScheduledCommandResult
    {
        public CommandScheduled(IScheduledCommand command, IClock clock = null) : base(command)
        {
            Clock = clock;
        }

        public IClock Clock { get; }

        public override string ToString() =>
            $"Scheduled{Clock.IfNotNull().Then(c => " on clock " + c).ElseDefault()}";
    }
}