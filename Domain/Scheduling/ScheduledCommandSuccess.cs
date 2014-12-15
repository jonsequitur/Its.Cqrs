// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public class ScheduledCommandSuccess : ScheduledCommandResult
    {
        public ScheduledCommandSuccess(IScheduledCommand command) : base(command)
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