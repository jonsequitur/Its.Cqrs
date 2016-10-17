// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// Storage model representing an error that occurred in the command scheduling pipeline.
    /// </summary>
    public class CommandExecutionError
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the scheduled command that encountered the error.
        /// </summary>
        public ScheduledCommand ScheduledCommand { get; set; }
    }
}