// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class Clock : IClock
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset UtcNow { get; set; }

        public DateTimeOffset Now()
        {
            return UtcNow;
        }

        public override string ToString()
        {
            return GetType() + ": " + Now().ToString("O");
        }
    }
}