// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class ClockMapping
    {
        public long Id { get; set; }

        public Clock Clock { get; set; }

        public string Value { get; set; }
    }
}
