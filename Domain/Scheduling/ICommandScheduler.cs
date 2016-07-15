// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Schedules commands for deferred execution.
    /// </summary>
    ///  <typeparam name="TTarget">The type of the command target.</typeparam>
    public interface ICommandScheduler<out TTarget>
    {
        /// <summary>
        /// Schedules the specified command.
        /// </summary>
        /// <param name="scheduledCommand">The scheduled command.</param>
        /// <returns>A task that is complete when the command has been successfully scheduled.</returns>
        Task Schedule(IScheduledCommand<TTarget> scheduledCommand);
    }
}