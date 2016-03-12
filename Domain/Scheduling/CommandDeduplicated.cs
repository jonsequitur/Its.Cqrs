// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    [DebuggerDisplay("{ToString()}")]
    public class CommandDeduplicated : ScheduledCommandResult
    {
        private readonly string when;

        internal CommandDeduplicated(IScheduledCommand command, string when) : base(command)
        {
            this.when = when;
        }

        public override string ToString() => $"Deduplicated on {when}";
    }
}