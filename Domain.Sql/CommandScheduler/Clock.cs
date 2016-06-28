// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    [DebuggerDisplay("{ToString()}")]
    public class Clock : IClock
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset UtcNow { get; set; }

        public DateTimeOffset Now() => UtcNow;

        public override string ToString() => $"\"{Name}\": {UtcNow.ToString("O")}";

        public const string DefaultClockName = "default";
    }
}
