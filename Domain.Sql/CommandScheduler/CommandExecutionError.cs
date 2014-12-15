// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class CommandExecutionError
    {
        public long Id { get; set; }

        public string Error { get; set; }

        public ScheduledCommand ScheduledCommand { get; set; }
    }
}