// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public class CommandSucceeded : ScheduledCommandResult
    {
        public CommandSucceeded(IScheduledCommand command) : base(command)
        {
        }

        public override bool WasSuccessful
        {
            get
            {
                return true;
            }
        }
    }
}