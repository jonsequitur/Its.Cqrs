// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Represents work done by the command scheduler.
    /// </summary>
    public interface ICommandSchedulerActivity
    {
        /// <summary>
        /// Gets the scheduled command being operated upon.
        /// </summary>
        IScheduledCommand ScheduledCommand { get; }
    }
}
